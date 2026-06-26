
using Autodesk.Revit.UI;

namespace ShopDraw.Commons
{
    public static class TaskDialogUtil
    {
        /// <summary>
        /// 1. Hộp thoại Thông báo (Information) - Chỉ có nút Close
        /// </summary>
        public static void ShowInfo(
            string mainInstruction,
            string mainContent = "",
            string expandedContent = "",
            string title = "Information")
        {
            TaskDialog dialog = new TaskDialog(title)
            {
                MainInstruction = mainInstruction,
                MainContent = mainContent,
                ExpandedContent = expandedContent,
                MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                CommonButtons = TaskDialogCommonButtons.Close
            };
            dialog.Show();
        }

        /// <summary>
        /// 2. Hộp thoại Báo lỗi (Error) - Gặp sự cố nghiêm trọng, chỉ có nút Close
        /// </summary>
        public static void ShowError(
            string mainInstruction,
            string mainContent = "",
            string expandedContent = "",
            string title = "Error Detected")
        {
            TaskDialog dialog = new TaskDialog(title)
            {
                MainInstruction = mainInstruction,
                MainContent = mainContent,
                ExpandedContent = expandedContent,
                MainIcon = TaskDialogIcon.TaskDialogIconError,
                CommonButtons = TaskDialogCommonButtons.Close
            };
            dialog.Show();
        }

        /// <summary>
        /// 3. Hộp thoại Cảnh báo (Warning) - Chỉ mang tính chất cảnh báo, không chặn chương trình
        /// </summary>
        public static void ShowWarning(
            string mainInstruction,
            string mainContent = "",
            string expandedContent = "",
            string title = "Warning")
        {
            TaskDialog dialog = new TaskDialog(title)
            {
                MainInstruction = mainInstruction,
                MainContent = mainContent,
                ExpandedContent = expandedContent,
                MainIcon = TaskDialogIcon.TaskDialogIconWarning,
                CommonButtons = TaskDialogCommonButtons.Close
            };
            dialog.Show();
        }

        /// <summary>
        /// 4. Hộp thoại Xác nhận (Confirm) - Hỏi Yes/No để quyết định tiếp tục hay dừng lại.
        /// </summary>
        /// <param name="defaultToNo">Nếu true, nút mặc định khi ấn Enter sẽ là 'No' để an toàn</param>
        /// <returns>Trả về true nếu chọn Yes, ngược lại trả về false</returns>
        public static bool AskConfirm(
            string mainInstruction,
            string mainContent = "",
            string expandedContent = "",
            bool defaultToNo = true,
            string title = "Confirmation")
        {
            TaskDialog dialog = new TaskDialog(title)
            {
                MainInstruction = mainInstruction,
                MainContent = mainContent,
                ExpandedContent = expandedContent,
                MainIcon = TaskDialogIcon.TaskDialogIconWarning,
                CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
                DefaultButton = defaultToNo ? TaskDialogResult.No : TaskDialogResult.Yes
            };

            TaskDialogResult result = dialog.Show();
            return result == TaskDialogResult.Yes;
        }
    }
}
