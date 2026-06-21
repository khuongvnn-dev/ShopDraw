using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ShopDraw.Commons
{
    public static class CsvExporter
    {
        /// <summary>
        /// Xuất danh sách đối tượng ra file CSV linh hoạt.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng</typeparam>
        /// <param name="data">Danh sách dữ liệu cần xuất</param>
        /// <param name="baseFileName">Tên gốc của file (mặc định: "Report")</param>
        /// <param name="subFolder">Thư mục con nằm trong LocalAppData (mặc định: "ShopDrawReport")</param>
        /// <param name="appendTimestamp">Có gắn thêm thời gian vào tên file để tránh trùng lặp không (mặc định: true)</param>
        /// <param name="customFullPath">Đường dẫn chỉ định (NẾU CÓ, hàm sẽ bỏ qua subFolder và dùng luôn đường dẫn này)</param>
        /// <returns>Đường dẫn tuyệt đối của file đã lưu, hoặc chuỗi rỗng ("") nếu thất bại.</returns>
        public static string ExportToCsv<T>(
            IEnumerable<T> data,
            string baseFileName = "Report",
            string subFolder = "ShopDrawReport",
            bool appendTimestamp = true,
            string customFullPath = "")
        {
            if (data == null || !data.Any())
            {
                Logger.Infor("No data to export to CSV.");
                return string.Empty;
            }

            try
            {
                string finalPath = customFullPath;

                // Nếu người dùng không chỉ định đường dẫn cụ thể (như từ SaveFileDialog), hệ thống tự sinh đường dẫn
                if (string.IsNullOrWhiteSpace(finalPath))
                {
                    // Lấy thư mục %LocalAppData% giống cách Logger đang làm
                    string appFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string directoryPath = Path.Combine(appFolder, subFolder);

                    // Đảm bảo thư mục luôn tồn tại
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    // Quy định tên file
                    string fileName = appendTimestamp
                        ? $"{baseFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                        : $"{baseFileName}.csv";

                    finalPath = Path.Combine(directoryPath, fileName);

                    // Xử lý chống trùng lặp nếu KHÔNG dùng timestamp mà file đã tồn tại
                    if (!appendTimestamp)
                    {
                        int suffix = 1;
                        while (File.Exists(finalPath))
                        {
                            finalPath = Path.Combine(directoryPath, $"{baseFileName}_{suffix++}.csv");
                        }
                    }
                }

                // Lấy danh sách các Properties (cột) của Class T
                PropertyInfo[] properties = typeof(T).GetProperties();

                // Bắt đầu ghi file
                using (var writer = new StreamWriter(finalPath, false, Encoding.UTF8))
                {
                    // Thêm BOM để Excel mở lên không bị lỗi font Tiếng Việt
                    writer.Write("\ufeff");

                    // Ghi Header
                    var headers = properties.Select(p => p.Name);
                    writer.WriteLine(string.Join(",", headers));

                    // Ghi dữ liệu
                    foreach (var item in data)
                    {
                        var values = properties.Select(p =>
                        {
                            var value = p.GetValue(item, null);
                            string strValue = value?.ToString() ?? "";

                            // Xử lý các chuỗi chứa dấu phẩy, ngoặc kép, hoặc xuống dòng (CSV Injection protection)
                            if (strValue.Contains(",") || strValue.Contains("\"") || strValue.Contains("\n") || strValue.Contains("\r"))
                            {
                                return $"\"{strValue.Replace("\"", "\"\"")}\"";
                            }
                            return strValue;
                        });

                        writer.WriteLine(string.Join(",", values));
                    }
                }

                Logger.Infor($"Successfully exported data to CSV: {finalPath}");
                return finalPath;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Failed to export CSV: {ex.Message}");
                return string.Empty;
            }
        }
    }
}