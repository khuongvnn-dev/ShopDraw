using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace ShopDraw.Commons
{
    internal class DebugTools
    {
        /// <summary>
        /// Hàm hỗ trợ in chi tiết thông tin đã quét trong mô hình (Chỉ chạy ở chế độ Debug)
        /// </summary>
        internal static void LogDebugInfo(
            HashSet<string> sheets,
            HashSet<string> views,
            Dictionary<string, ElementId> templates,
            Dictionary<string, ElementId> levels)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine("======================= [DEBUG SCAN SUMMARY] =======================");

            // 1. Log thông tin Levels
            sb.AppendLine($"[LEVELS] Total count: {levels.Count}");
            foreach (var kvp in levels)
            {
                sb.AppendLine($"   -> Name: {kvp.Key.PadRight(25)} | Id: {kvp.Value}");
            }
            sb.AppendLine("--------------------------------------------------------------------");

            // 2. Log thông tin View Templates
            sb.AppendLine($"[VIEW TEMPLATES] Total count: {templates.Count}");
            foreach (var kvp in templates)
            {
                sb.AppendLine($"   -> Name: {kvp.Key.PadRight(35)} | Id: {kvp.Value}");
            }
            sb.AppendLine("--------------------------------------------------------------------");

            // 3. Log thông tin Sheet Numbers (In tối đa 20 cái đầu tiên để tránh tràn Log nếu file quá nặng)
            int maxPreview = 20;
            sb.AppendLine($"[SHEET NUMBERS] Total count: {sheets.Count}");
            var sheetList = sheets.Take(maxPreview).ToList();
            foreach (var sheetNum in sheetList)
            {
                sb.AppendLine($"   -> {sheetNum}");
            }
            if (sheets.Count > maxPreview)
                sb.AppendLine($"   -> ... and {sheets.Count - maxPreview} more sheets.");
            sb.AppendLine("--------------------------------------------------------------------");

            // 4. Log thông tin View Names (In tối đa 20 cái đầu tiên)
            sb.AppendLine($"[VIEW NAMES] Total count: {views.Count}");
            var viewList = views.Take(maxPreview).ToList();
            foreach (var viewName in viewList)
            {
                sb.AppendLine($"   -> {viewName}");
            }
            if (views.Count > maxPreview)
                sb.AppendLine($"   -> ... and {views.Count - maxPreview} more views.");

            sb.AppendLine("====================================================================");

            // Ghi toàn bộ nội dung đã gom gọn vào Logger một lần duy nhất (Tránh spam file log)
            Logger.Infor(sb.ToString());
        }
    }
}
