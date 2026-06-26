using Autodesk.Revit.DB;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Win32;
using ShopDraw.Models.Reports;
using ShopDraw.Models.Shop;
using ShopDraw.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ShopDraw.Actions.Helpers
{
    internal class CreateShopDrawHelper
    {
        internal static void GenerateShopDocs(Document doc, List<CsvWorksheetModel> data, ProgressBarView progressBar)
        {
            var existingSheetNumber = CollectExistingSheetNumbers(doc);
            var existingViewName = CollectExistingViewNames(doc);
            var existingViewTemplate = CollectExistingViewTemplates(doc);
            var existingLevel = CollectExistingLevels(doc);

            var validate = ValidateCsv(doc, data, existingSheetNumber, existingViewTemplate, existingLevel, progressBar);


        }

        private static Dictionary<string, ElementId> CollectExistingLevels(Document doc)
        {
            return new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .ToDictionary(
                        lvl => lvl.Name,       // Key: Tên của Level (Ví dụ: "Tầng 1")
                        lvl => lvl.Id,         // Value: ID của Level (nhẹ bộ nhớ)
                        StringComparer.OrdinalIgnoreCase // Bỏ qua phân biệt hoa thường khi tra cứu tên
                    );
        }

        private static Dictionary<string, ElementId> CollectExistingViewTemplates(Document doc)
        {
            return new FilteredElementCollector(doc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .Where(v => v.IsTemplate) // Chỉ lấy các View là Template
                    .ToDictionary(
                        v => v.Name,          // Key: Tên của View Template
                        v => v.Id,            // Value: ID của View Template (nhẹ bộ nhớ)
                        StringComparer.OrdinalIgnoreCase // Bỏ qua phân biệt hoa thường khi tra cứu tên
                    );
        }

        private static ValidationResult ValidateCsv(Document doc,
                                            List<CsvWorksheetModel> data,
                                            HashSet<string> existingSheetNumber,
                                            Dictionary<string, ElementId> existingViewTemplate,
                                            Dictionary<string, ElementId> existingLevel,
                                            ProgressBarView progressBar)
        {
            var result = new ValidationResult();

            if (data == null || data.Count == 0)
            {
                result.IsValid = false;
                result.Errors.Add("CSV data is empty.");
                return result;
            }

            int totalRecords = data.Count;

            // Duyệt qua từng dòng dữ liệu trong danh sách CSV
            for (int i = 0; i < totalRecords; i++)
            {
                var row = data[i];
                int currentIndex = i + 1;
                string currentSheetNum = row.SheetNumber?.Trim();

                // SỬ DỤNG HÀM UPDATE(): 
                // Vòng lặp đầu tiên (i == 0) sẽ truyền true để reset pb.Value về 0 và đặt pb.Maximum = totalRecords.
                // Bên trong hàm Update() sẽ tự động chạy lệnh pb.Value++ nên giá trị hiển thị sẽ tự tăng lên 1, 2, 3... rất khớp.
                string progressTitle = "Validating data";
                progressBar.Update(totalRecords, progressTitle, i == 0);


                // --- LOGIC 1: Kiểm tra Level (Nếu không tồn tại -> Dừng chương trình) ---
                if (!string.IsNullOrEmpty(row.LevelName))
                {
                    if (!existingLevel.ContainsKey(row.LevelName.Trim()))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"[Row {currentIndex}] Level '{row.LevelName}' does not exist in the Revit model. Execution stopped.");
                        return result;
                    }
                }

                // --- LOGIC 2: Kiểm tra View Template (Nếu không tồn tại -> Vẫn tiếp tục nhưng Cảnh báo) ---
                if (!string.IsNullOrEmpty(row.ViewTemplateName))
                {
                    if (!existingViewTemplate.ContainsKey(row.ViewTemplateName.Trim()))
                    {
                        result.Warnings.Add($"[Row {currentIndex}] View Template '{row.ViewTemplateName}' was not found. System will proceed without template.");
                    }
                }

                // --- LOGIC 3: Kiểm tra Sheet Number trùng khít hoàn toàn (Trùng -> Dừng chương trình) ---
                if (!string.IsNullOrEmpty(currentSheetNum))
                {
                    if (existingSheetNumber.Contains(currentSheetNum))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"[Row {currentIndex}] Sheet Number '{currentSheetNum}' already exists in the Revit project. Execution stopped.");
                        return result;
                    }

                    // --- LOGIC 4: Kiểm tra Sheet Number trùng phần đầu (Starts With -> Cảnh báo) ---
                    bool isPrefixMatched = existingSheetNumber.Any(existingNum =>
                        existingNum.StartsWith(currentSheetNum + ".", StringComparison.OrdinalIgnoreCase) ||
                        existingNum.StartsWith(currentSheetNum + "-", StringComparison.OrdinalIgnoreCase) ||
                        existingNum.Equals(currentSheetNum, StringComparison.OrdinalIgnoreCase)
                    );

                    if (isPrefixMatched)
                    {
                        result.Warnings.Add($"[Row {currentIndex}] Sheet Number prefix '{currentSheetNum}' partially conflicts with an existing sheet number structure in the project.");
                    }
                }
            }

            return result;
        }

        // Thu thâp toàn bộ sheet number hiện có trong dự án để tránh trùng lặp khi tạo mới
        private static HashSet<string> CollectExistingSheetNumbers(Document doc)
        {
            return new HashSet<string>(
                new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .Select(s => s.SheetNumber),
                StringComparer.OrdinalIgnoreCase);
        }

        // Thu thập tên tất cả các view hiện có trong dự án để tránh trùng lặp khi tạo mới
        private static HashSet<string> CollectExistingViewNames(Document doc)
        {
            return new HashSet<string>(
                new FilteredElementCollector(doc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .Where(v => !v.IsTemplate)
                    .Select(v => v.Name),
                StringComparer.OrdinalIgnoreCase);
        }

        internal static string GetFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*", // Chỉ lọc file CSV
                FilterIndex = 1,
                Title = "Chọn file danh mục bản vẽ CSV cho dự án BIM",
                Multiselect = false // Chỉ cho phép chọn 1 file
            };

            // Hiển thị hộp thoại và kiểm tra nếu người dùng bấm OK
            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName; // Trả về đường dẫn đầy đủ của file đã chọn
            }

            return string.Empty;
        }

        internal static List<CsvWorksheetModel> ReadCsv(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                throw new FileNotFoundException($"Không tìm thấy file CSV tại đường dẫn: {path}");
            }

            // Cấu hình CsvHelper: Chấp nhận không phân biệt hoa thường ở tên cột (Header)
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
                HeaderValidated = null, // Bỏ qua kiểm tra nếu file CSV thừa/thiếu cột không quan trọng
                MissingFieldFound = null // Bỏ qua nếu có trường bị trống
            };

            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, config))
            {
                // GetRecords trả về IEnumerable (đọc dạng Stream nên rất nhanh và tiết kiệm RAM)
                // .ToList() để nạp dữ liệu hoàn chỉnh vào bộ nhớ trước khi đóng Stream
                return csv.GetRecords<CsvWorksheetModel>().ToList();
            }
        }
    }
}
