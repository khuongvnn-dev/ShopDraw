using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using Newtonsoft.Json;
using ShopDraw.Commons;
using ShopDraw.Models._3D;
using ShopDraw.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace ShopDraw.Actions.Helpers
{
    internal class ExportMEPSystemHelper
    {
        public static List<Element> GetElementsWCategory(Document doc, ProgressBarView progressBar)
        {
            progressBar.UpdateNumber2(0, 100, "Scanning MEP System...", true);

            var categories = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_PipeFitting
            };

            var multiCategoryFilter = new ElementMulticategoryFilter(categories);

            var rawElements = new FilteredElementCollector(doc)
                .WherePasses(multiCategoryFilter)
                .WhereElementIsNotElementType()
                .ToList();

            progressBar.UpdateNumber2(100, 100, "Scanning MEP System...");
            return rawElements;
        }

        internal static void Export2Json(MepSystemModel data, ProgressBarView progressBar)
        {
            if (data == null || (data.Curves.Count == 0 && data.Fittings.Count == 0))
            {
                Logger.Infor("No MEP system data found to export.");
                TaskDialogUtil.ShowInfo( "No MEP system data found to export!");
                return;
            }

            // 1. Kiểm tra nếu người dùng đã hủy tiến trình trước đó
            if (!progressBar.Flag)
            {
                Logger.Infor("Export process was canceled. Skipping file save.");
                return;
            }

            // 2. Khởi tạo hộp thoại lưu file JSON tương tự code mẫu
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                Title = "Select where to save the MEP System JSON file",
                FileName = "mep_system_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
            };

            // 3. Hiển thị hộp thoại và xử lý lưu file
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Cập nhật trạng thái trên thanh Progress
                    progressBar.UpdateNumber2(0, 100, "Structuring JSON file...", false);

                    // Serialize đối tượng MepSystemModel thành chuỗi JSON định dạng đẹp (Indented)
                    string jsonData = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);

                    progressBar.UpdateNumber2(80, 100, "Writing data to disk...", false);

                    // Ghi chuỗi JSON vào đường dẫn người dùng đã chọn
                    File.WriteAllText(saveFileDialog.FileName, jsonData);

                    // Cập nhật hoàn thành tiến trình
                    progressBar.UpdateNumber2(100, 100, "Export completed successfully!", false);

                    // Ghi log và hiển thị thông báo kết quả
                    Logger.Infor($"Export completed successfully! The drawing includes - Curves: {data.Curves.Count}, Fittings: {data.Fittings.Count}");
                    TaskDialogUtil.ShowInfo( "Export completed successfully!");
                }
                catch (Exception ex)
                {
                    // Ghi log lỗi nếu quá trình ghi file thất bại
                    Logger.Fatal(ex.Message);
                    TaskDialogUtil.ShowError("An error occurred while exporting and saving the JSON file!");
                }
            }
        }

        internal static MepSystemModel ParseData(List<Element> elements, ProgressBarView progressBar)
        {
            var model = new MepSystemModel
            {
                Levels = new List<LevelModel>(),
                Curves = new List<CurveModel>(),
                Fittings = new List<FittingModel>()
            };

            var uniqueLevels = new Dictionary<string, LevelModel>();
            int total = elements.Count;
            foreach (var element in elements)
            {
                if (!progressBar.Flag)
                {
                    Logger.Infor("Export MEP System process has been canceled by user.");
                    return model;
                }

                progressBar.Update(total, "Extracting 3D data...", false);

                Level elementLevel = element.Document.GetElement(element.LevelId) as Level;
                if (elementLevel != null && !uniqueLevels.ContainsKey(elementLevel.Id.ToString()))
                {
                    uniqueLevels.Add(elementLevel.Id.ToString(), new LevelModel
                    {
                        Id = elementLevel.Id.ToString(),
                        LevelName = elementLevel.Name,
                        Elevation = elementLevel.Elevation
                    });
                }

                if (element is MEPCurve mepCurve)
                {
                    var typeEl = element.Document.GetElement(element.GetTypeId()) as ElementType;
                    var curveModel = new CurveModel
                    {
                        Id = element.Id.ToString(),
                        TypeName = typeEl?.Name,
                        FamilyName = element.Category?.Name, // Ống hệ thống không có FamilyName chuẩn như Fitting, ta dùng Category Name
                        LevelName = elementLevel?.Name // Gán thông tin LevelName
                    };
                    ExtractCurveData(mepCurve, curveModel);
                    model.Curves.Add(curveModel);
                }
                else if (element is FamilyInstance fi)
                {
                    var typeEl = element.Document.GetElement(element.GetTypeId()) as ElementType;
                    Transform transform = fi.GetTotalTransform(); // Lấy ma trận tọa độ không gian 3D

                    var fittingModel = new FittingModel
                    {
                        Id = element.Id.ToString(),
                        TypeName = typeEl?.Name,
                        FamilyName = fi.Symbol?.Family?.Name,
                        LevelName = elementLevel?.Name,
                        // Chuyển đổi tọa độ XYZ sang RvtXYZ của bạn
                        Origin = new RvtXYZ(transform.Origin),
                        BasisX = new RvtXYZ(transform.BasisX),
                        BasisY = new RvtXYZ(transform.BasisY),
                        BasisZ = new RvtXYZ(transform.BasisZ)
                    };
                    model.Fittings.Add(fittingModel);
                }
            }
            model.Levels = uniqueLevels.Values.ToList();
            return model;
        }
        private static void ExtractCurveData(MEPCurve mepCurve, CurveModel curveModel)
        {
            if (mepCurve == null || curveModel == null) return;

            // 1. Trích xuất tọa độ điểm đầu và điểm cuối hình học tuyệt đối của ống
            if (mepCurve.Location is LocationCurve locCurve && locCurve.Curve != null)
            {
                XYZ startPoint = locCurve.Curve.GetEndPoint(0);
                XYZ endPoint = locCurve.Curve.GetEndPoint(1);

                curveModel.StartPoint = new RvtXYZ(startPoint);
                curveModel.EndPoint = new RvtXYZ(endPoint);

                // 2. Trích xuất kích thước tự thân (Diameter hoặc Width/Height) an toàn
                try { curveModel.Diameter = mepCurve.Diameter; } catch { }
                try
                {
                    curveModel.Width = mepCurve.Width;
                    curveModel.Height = mepCurve.Height;
                }
                catch { }

                // 3. Tìm kết nối ở 2 đầu ống để gán mối liên kết Đồ thị (Graph Connection)
                ConnectorManager cm = mepCurve.ConnectorManager;
                if (cm != null)
                {
                    foreach (Connector conn in cm.Connectors)
                    {
                        // Chỉ xử lý các Connector ở đầu mút của ống (End)
                        if (conn.ConnectorType == ConnectorType.End)
                        {
                            // Kiểm tra xem vị trí của Connector này đang khớp với điểm Start hay điểm End của ống
                            bool isStart = conn.Origin.DistanceTo(startPoint) < 0.01;

                            if (conn.IsConnected)
                            {
                                foreach (Connector refConn in conn.AllRefs)
                                {
                                    // Tìm đối tượng kết nối bên ngoài (không phải chính cái ống này)
                                    if (refConn.Owner.Id != mepCurve.Id)
                                    {
                                        string targetId = refConn.Owner.Id.ToString();

                                        // Kiểm tra xem đối tượng đó thuộc về cụm Assembly nào không (Giải pháp mở rộng hệ thống)
                                        if (refConn.Owner.AssemblyInstanceId != ElementId.InvalidElementId)
                                        {
                                            targetId = $"{refConn.Owner.AssemblyInstanceId}.{refConn.Owner.Id}";
                                        }

                                        if (isStart)
                                            curveModel.StartConnectedToId = targetId;
                                        else
                                            curveModel.EndConnectedToId = targetId;

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
