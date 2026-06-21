using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using Newtonsoft.Json;
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
                TaskDialog.Show("Notification", "No MEP system data found to export!");
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
                    TaskDialog.Show("Notification", "Export completed successfully!");
                }
                catch (Exception ex)
                {
                    // Ghi log lỗi nếu quá trình ghi file thất bại
                    Logger.Fatal(ex.Message);
                    TaskDialog.Show("Error", "An error occurred while exporting and saving the JSON file!");
                }
            }
        }

        internal static MepSystemModel ParseData(List<Element> elements, ProgressBarView progressBar)
        {
            var model = new MepSystemModel
            {
                Curves = new List<CurveModel>(),
                Fittings = new List<FittingModel>()
            };

            int total = elements.Count;
            foreach (var element in elements)
            {
                if (!progressBar.Flag)
                {
                    Logger.Infor("Export MEP System process has been canceled by user.");
                    return model;
                }

                progressBar.Update(total, "Extracting 3D data...", false);

                if (element is MEPCurve mepCurve)
                {
                    var typeEl = element.Document.GetElement(element.GetTypeId()) as ElementType;
                    var curveModel = new CurveModel
                    {
                        Id = element.Id.ToString(),
                        TypeName = typeEl?.Name,
                        FamilyName = element.Category?.Name, // Ống hệ thống không có FamilyName chuẩn như Fitting, ta dùng Category Name
                        Diameter = (element as Pipe)?.Diameter ?? 0, // Ép kiểu an toàn lấy đường kính
                        Connectors = GetConnectorsData(mepCurve.ConnectorManager, element.Id)
                    };
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
                        // Chuyển đổi tọa độ XYZ sang RvtXYZ của bạn
                        Origin = new RvtXYZ(transform.Origin),
                        BasisX = new RvtXYZ(transform.BasisX),
                        BasisY = new RvtXYZ(transform.BasisY),
                        BasisZ = new RvtXYZ(transform.BasisZ)
                    };
                    model.Fittings.Add(fittingModel);
                }
            }
            return model;
        }

        private static List<ConnectorModel> GetConnectorsData(ConnectorManager cm, ElementId ownerId)
        {
            var result = new List<ConnectorModel>();
            if (cm == null) return result;

            foreach (Connector conn in cm.Connectors)
            {
                var model = new ConnectorModel
                {
                    Origin = new RvtXYZ(conn.Origin)
                };

                // Tìm Id của phần tử đang được kết nối tới
                if (conn.IsConnected)
                {
                    foreach (Connector refConn in conn.AllRefs)
                    {
                        if (refConn.Owner.Id != ownerId &&
                           (refConn.ConnectorType == ConnectorType.End || refConn.ConnectorType == ConnectorType.Curve))
                        {
                            model.ConnectorToId = refConn.Owner.Id.ToString();
                            break;
                        }
                    }
                }
                result.Add(model);
            }
            return result;
        }
    }
}
