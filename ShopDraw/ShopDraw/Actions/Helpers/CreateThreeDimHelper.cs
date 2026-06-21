using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using ShopDraw.Commons;
using ShopDraw.Models._3D;
using ShopDraw.Models.Reports;
using ShopDraw.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ShopDraw.Actions.Helpers
{
    internal class CreateThreeDimHelper
    {
        internal static void Create3d(Document document, MepSystemModel model, ProgressBarView progressBar)
        {
            List<MissingFittingReport> missingFittings = ValidateFittings(document, model.Fittings, progressBar, out List<FamilySymbol> cachedSymbols);
            if (missingFittings.Any())
            {
                string reportPath = CsvExporter.ExportToCsv(missingFittings, baseFileName: "missing_fittings");

                Logger.Infor($"Process aborted. {missingFittings.Count} missing fitting types found. Report exported to: {reportPath}");

                // Hiển thị hộp thoại thông báo ngắn gọn cho người dùng
                TaskDialog.Show(
                    "Missing Elements Detected",
                    $"The process has been stopped because {missingFittings.Count} fitting types are missing from the document.\n\n" +
                    $"A detailed report has been exported to:\n{reportPath}"
                );

                return;
            }
            var levelInfos = CreateLevels(document, model, progressBar);
            var createdFittings = PlaceFittings(document, model.Fittings, progressBar, cachedSymbols, levelInfos);
            var createdCurves = PlaceCurves(document, model.Curves, progressBar, levelInfos);
            ExportImportResultReport(model, createdFittings, createdCurves);
        }

        private static Dictionary<string, ElementId> PlaceCurves(Document document,
                                                                 List<CurveModel> curves,
                                                                 ProgressBarView progressBar,
                                                                 Dictionary<string, Level> levelInfos)
        {
            var result = new Dictionary<string, ElementId>();
            Logger.CurrentMethod();

            if (curves.Count == 0) return result;

            // 1. Cache lại các Type để tránh query Database liên tục trong vòng lặp (Tối ưu performance)
            var allPipeTypes = new FilteredElementCollector(document)
                .OfClass(typeof(PipeType))
                .Cast<PipeType>()
                .ToList();

            var allSystemTypes = new FilteredElementCollector(document)
                .OfClass(typeof(PipingSystemType))
                .Cast<PipingSystemType>()
                .ToList();

            // Lấy SystemType mặc định đầu tiên phục vụ tham số bắt buộc của hàm Pipe.Create
            ElementId defaultSysTypeId = allSystemTypes.FirstOrDefault()?.Id ?? ElementId.InvalidElementId;

            using (var placeCurveTrans = new SubTransaction(document))
            {
                try
                {
                    placeCurveTrans.Start();
                    int count = 0;

                    foreach (var curve in curves)
                    {
                        count++;
                        progressBar.UpdateNumber2(count, curves.Count, "Placing curves...", false);

                        if (!progressBar.Flag) break;

                        // 2. Kiểm tra tính hợp lệ của Level
                        if (string.IsNullOrEmpty(curve.LevelName) || !levelInfos.TryGetValue(curve.LevelName, out Level level))
                        {
                            Logger.Infor($"Curve #{curve.Id}: Invalid or missing LevelName ('{curve.LevelName}'). Skipping.");
                            continue;
                        }

                        // 3. Tìm PipeType khớp với TypeName trong file JSON
                        PipeType pipeType = allPipeTypes.FirstOrDefault(x =>
                            x.Name.Equals(curve.TypeName, StringComparison.OrdinalIgnoreCase));

                        if (pipeType == null)
                        {
                            Logger.Infor($"Curve #{curve.Id}: PipeType '{curve.TypeName}' not found in current document. Skipping.");
                            continue;
                        }

                        // 4. Lấy trực tiếp tọa độ hình học tuyệt đối từ self-contained model
                        if (curve.StartPoint == null || curve.EndPoint == null)
                        {
                            Logger.Infor($"Curve #{curve.Id}: Missing geometrical points (StartPoint/EndPoint). Skipping.");
                            continue;
                        }

                        XYZ p1 = curve.StartPoint.ToXYZ();
                        XYZ p2 = curve.EndPoint.ToXYZ();

                        // Tránh lỗi Revit crash nếu độ dài ống nhỏ hơn sai số cho phép (~0.8mm)
                        if (p1.DistanceTo(p2) < document.Application.ShortCurveTolerance)
                        {
                            Logger.Infor($"Curve #{curve.Id}: Curve length is shorter than Revit's tolerance. Skipping.");
                            continue;
                        }

                        // 5. Khởi tạo đối tượng Pipe thực tế trong Revit
                        Pipe newPipe = Pipe.Create(document, defaultSysTypeId, pipeType.Id, level.Id, p1, p2);

                        // 6. Gán kích thước đường kính trực tiếp từ JSON sang ống
                        if (curve.Diameter > 0)
                        {
                            Parameter diameterParam = newPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                            if (diameterParam != null && !diameterParam.IsReadOnly)
                            {
                                diameterParam.Set(curve.Diameter);
                            }
                        }

                        // Lưu lại Map ID kết quả đáp ứng đầu ra để chuẩn bị cho bước MakeConnections kế tiếp
                        result[curve.Id] = newPipe.Id;
                        Logger.Infor($"Successfully placed curve {curve.Id} (Revit ID: {newPipe.Id}).");
                    }

                    placeCurveTrans.Commit();
                }
                catch (System.Exception ex)
                {
                    if (placeCurveTrans.HasStarted() && placeCurveTrans.GetStatus() == TransactionStatus.Started)
                    {
                        placeCurveTrans.RollBack();
                    }
                    Logger.Fatal($"Error in PlaceCurves: {ex.Message}");
                }
            }

            return result;
        }

        private static List<MissingFittingReport> ValidateFittings(Document document,
                                                                   List<FittingModel> fittings,
                                                                   ProgressBarView progressBar,
                                                                   out List<FamilySymbol> existingSymbols)
        {
            Logger.CurrentMethod();
            var missingList = new List<MissingFittingReport>();

            // Khởi tạo danh sách trả về qua tham số out để tránh lỗi NullReference nếu return sớm
            existingSymbols = new List<FamilySymbol>();

            if (fittings == null || !fittings.Any())
            {
                Logger.Infor("No fittings in JSON to validate.");
                progressBar.UpdateNumber2(100, 100, "Validation completed (No data).", true);
                return missingList;
            }

            progressBar.UpdateNumber2(0, 100, "Collecting existing Family Symbols...", true);

            // 1. Quét toàn bộ Pipe Fitting Symbols đang có trong Document và gán ra biến out
            existingSymbols = new FilteredElementCollector(document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .Cast<FamilySymbol>()
                .ToList();

            // 2. Tạo HashSet chứa định dạng "FamilyName|TypeName" để tra cứu (O(1))
            var availableTypesLookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var symbol in existingSymbols)
            {
                availableTypesLookup.Add($"{symbol.FamilyName}|{symbol.Name}");
            }

            // 3. Đối chiếu dữ liệu JSON
            int total = fittings.Count;
            int count = 0;

            foreach (var fitting in fittings)
            {
                count++;
                // Cập nhật Progress Bar cho từng cấu kiện
                progressBar.UpdateNumber2(count, total, "Validating fittings against Revit document...", false);

                // Kiểm tra xem người dùng có bấm Cancel trên ProgressBar không
                if (!progressBar.Flag)
                {
                    Logger.Infor("User canceled the validation process.");
                    break;
                }

                // Bỏ qua nếu dữ liệu JSON bị rỗng thông tin cơ bản
                if (string.IsNullOrEmpty(fitting.FamilyName) || string.IsNullOrEmpty(fitting.TypeName))
                {
                    missingList.Add(new MissingFittingReport
                    {
                        JsonId = fitting.Id ?? "Unknown",
                        SystemTypeName = "N/A", // Thay bằng fitting.SystemTypeName nếu class có hỗ trợ
                        MissingFamilyName = fitting.FamilyName ?? "[Empty]",
                        MissingTypeName = fitting.TypeName ?? "[Empty]",
                        Note = "Invalid JSON data: Missing FamilyName or TypeName"
                    });
                    continue;
                }

                string searchKey = $"{fitting.FamilyName}|{fitting.TypeName}";

                // Nếu Revit không có loại này, thêm vào báo cáo
                if (!availableTypesLookup.Contains(searchKey))
                {
                    missingList.Add(new MissingFittingReport
                    {
                        JsonId = fitting.Id,
                        SystemTypeName = "N/A", // Thay bằng fitting.SystemTypeName nếu class có hỗ trợ
                        MissingFamilyName = fitting.FamilyName,
                        MissingTypeName = fitting.TypeName,
                        Note = "Not found in Revit Document"
                    });
                }
            }

            Logger.Infor($"Validation completed. Found {missingList.Count} missing fitting types.");
            progressBar.UpdateNumber2(100, 100, "Validation completed.", false);

            return missingList;
        }

        private static Dictionary<string, Level> CreateLevels(Document document, MepSystemModel model, ProgressBarView progressBar)
        {
            Logger.CurrentMethod();

            // Dictionary để trả về kết quả: Key = Tên Level (chuẩn từ JSON), Value = Object Level trong Revit
            // Sử dụng StringComparer.OrdinalIgnoreCase để đảm bảo tìm kiếm tên Level không phân biệt hoa/thường
            var levelDict = new Dictionary<string, Level>(StringComparer.OrdinalIgnoreCase);

            if (model == null)
            {
                Logger.Infor("MEP System Model is null. Skipping level creation.");
                return levelDict;
            }

            if (model.Levels == null || model.Levels.Count == 0)
            {
                Logger.Infor("No level data found in the MEP System Model.");
                return levelDict;
            }

            progressBar.UpdateNumber2(0, 100, "Reading levels from JSON...", true);
            Logger.Infor("Step 1: Reading and checking existing levels in the Revit document.");

            const double epsilon = 0.001; // Sai số nhỏ để so sánh cao độ

            // Lấy tất cả các Level hiện có trong bản vẽ Revit để đối chiếu
            var existingLevels = new FilteredElementCollector(document)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            var levelsToCreate = new List<LevelModel>();

            foreach (var jsonLevel in model.Levels)
            {
                // 1. Kiểm tra xem có Level nào trùng cả Tên hoặc trùng Cao độ (sai số epsilon) không
                var matchedLevel = existingLevels.FirstOrDefault(el =>
                    el.Name.Equals(jsonLevel.LevelName, StringComparison.OrdinalIgnoreCase) ||
                    Math.Abs(el.Elevation - jsonLevel.Elevation) < epsilon);

                if (matchedLevel != null)
                {
                    // Nếu đã tồn tại, map Tên gốc trong JSON với Level tìm được
                    if (!levelDict.ContainsKey(jsonLevel.LevelName))
                    {
                        levelDict[jsonLevel.LevelName] = matchedLevel;
                    }
                    Logger.Infor($"Level '{jsonLevel.LevelName}' matched with existing Revit level '{matchedLevel.Name}' (ID: {matchedLevel.Id}).");
                }
                else
                {
                    // Nếu chưa có thì thêm vào danh sách chuẩn bị tạo mới
                    levelsToCreate.Add(jsonLevel);
                }
            }

            Logger.Infor($"{levelsToCreate.Count} new levels need to be created from JSON data.");

            if (levelsToCreate.Count == 0)
            {
                progressBar.UpdateNumber2(100, 100, "All levels already exist.", true);
                Logger.Infor("All levels from JSON already exist in the document. No new levels created.");
                return levelDict;
            }

            progressBar.UpdateNumber2(50, 100, "Creating new levels from JSON...", true);
            Logger.Infor("Step 2: Starting SubTransaction to create missing levels.");

            using (SubTransaction subTx = new SubTransaction(document))
            {
                try
                {
                    subTx.Start();

                    foreach (var jsonLevel in levelsToCreate)
                    {
                        // Tạo level mới
                        Level newLevel = Level.Create(document, jsonLevel.Elevation);

                        string finalName = jsonLevel.LevelName;
                        int suffix = 1;

                        // Xử lý trùng tên
                        while (new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().Any(l => l.Name.Equals(finalName, StringComparison.OrdinalIgnoreCase)))
                        {
                            finalName = $"{jsonLevel.LevelName}_{suffix++}";
                        }

                        newLevel.Name = finalName;

                        // LƯU Ý QUAN TRỌNG: Key luôn là jsonLevel.LevelName (Tên gốc từ file JSON).
                        // Dù Revit có đổi tên thành "Level 1_1" do bị trùng đi nữa, thì các Fitting/Curve 
                        // ở các hàm sau vẫn tìm đúng bằng "Level 1" (chuỗi lưu trong JSON).
                        levelDict[jsonLevel.LevelName] = newLevel;

                        Logger.Infor($"Successfully created level: '{newLevel.Name}' at elevation: {jsonLevel.Elevation} ft.");
                    }

                    subTx.Commit();
                    Logger.Infor("SubTransaction committed successfully.");
                }
                catch (Exception ex)
                {
                    subTx.RollBack();
                    Logger.Fatal($"Failed to create levels from JSON. SubTransaction rolled back. Error: {ex.Message}");
                    throw;
                }
            }

            progressBar.UpdateNumber2(100, 100, "Level creation completed.", true);
            return levelDict;
        }

        private static Dictionary<string, ElementId> PlaceFittings(Document document,
                                  List<FittingModel> fittings,
                                  ProgressBarView progressBar,
                                  List<FamilySymbol> cachedSymbols,
                                  Dictionary<string, Level> levelInfos)
        {
            var result = new Dictionary<string, ElementId>();
            Logger.CurrentMethod();

            if (fittings.Count == 0)
            {
                var dialogResult = TaskDialog.Show(
                    "Warning",
                    "No fittings found. The process of placing fittings will be skipped.\nDo you want to continue?"
                );

                // Kiểm tra xem người dùng có bấm "Yes" (hoặc OK) hay không
                if (dialogResult == TaskDialogResult.Yes || dialogResult == TaskDialogResult.Ok)
                    Logger.Infor("User allowed skipping the fitting placement process.");
                else
                {
                    Logger.Infor("User canceled. Aborting process.");
                    return result;
                }
            }

            using (var placeFittingTrans = new SubTransaction(document))
            {
                try
                {
                    // Bắt buộc phải Start SubTransaction trước khi thao tác làm thay đổi Document
                    placeFittingTrans.Start();
                    foreach (var fitting in fittings)
                    {
                        progressBar.Update(fittings.Count, "Placing fittings...", false);

                        if (!progressBar.Flag)
                        {
                            Logger.Infor("User canceled the fitting placement process.");
                            break;
                        }

                        // Kiểm tra an toàn cho LevelName
                        if (string.IsNullOrEmpty(fitting.LevelName) || !levelInfos.ContainsKey(fitting.LevelName))
                        {
                            Logger.Infor($"Fitting #{fitting.Id} has invalid or missing LevelName ('{fitting.LevelName}'). Skipping.");
                            continue;
                        }

                        Level level = levelInfos[fitting.LevelName];
                        XYZ trueOrigin = fitting.Origin.ToXYZ();

                        // Lấy FamilySymbol
                        FamilySymbol symbol = cachedSymbols.FirstOrDefault(x =>
                            x.FamilyName.Equals(fitting.FamilyName, StringComparison.OrdinalIgnoreCase) &&
                            x.Name.Equals(fitting.TypeName, StringComparison.OrdinalIgnoreCase));

                        if (symbol == null)
                        {
                            Logger.Infor($"FamilySymbol '{fitting.FamilyName} - {fitting.TypeName}' not found for fitting #{fitting.Id}. Skipping.");
                            continue;
                        }

                        if (!symbol.IsActive)
                        {
                            symbol.Activate();
                            document.Regenerate();
                        }

                        // Tính toán vị trí và khởi tạo
                        double levelElevation = level.Elevation;
                        XYZ insertPoint = new XYZ(trueOrigin.X, trueOrigin.Y, levelElevation);
                        double offsetValue = trueOrigin.Z - levelElevation;

                        FamilyInstance instance = document.Create.NewFamilyInstance(
                            insertPoint,
                            symbol,
                            level,
                            Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                        document.Regenerate();

                        // Chỉnh lại Offset chuẩn và xoay hướng 3D
                        //SetFittingOffset(document, instance, offsetValue, trueOrigin);
                        //AlignFittingOrientation(document, instance, fitting);

                        // Lưu lại kết quả mapping ID
                        result[fitting.Id] = instance.Id;
                        Logger.Infor($"Successfully placed fitting {fitting.Id} (Revit ID: {instance.Id}).");
                    }

                    placeFittingTrans.Commit();
                }
                catch (Exception ex)
                {
                    if (placeFittingTrans.HasStarted() && placeFittingTrans.GetStatus() == TransactionStatus.Started)
                    {
                        placeFittingTrans.RollBack();
                    }
                    Logger.Fatal($"Error in PlaceFittings: {ex.Message}");
                }
            }

            return result;
        }
        internal static MepSystemModel GetData(ProgressBarView progressBar)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select JSON file"
            };

            MepSystemModel model = null;
            progressBar.UpdateNumber2(0, 100, "Loading JSON file...", true);
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string jsonContent = File.ReadAllText(openFileDialog.FileName);
                    model = Newtonsoft.Json.JsonConvert.DeserializeObject<MepSystemModel>(jsonContent);
                }
                catch (Exception ex)
                {
                    Logger.CurrentMethod();
                    Logger.Fatal(ex.Message);
                }
            }
            if (model == null)
                progressBar.Close();
            return model;
        }
    }
}
