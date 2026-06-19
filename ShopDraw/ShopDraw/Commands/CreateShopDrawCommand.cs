using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ShopDraw.Commands
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreateShopDrawCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog.Show("Thông báo", "Lệnh tạo Shop Drawing đang chạy!");
            return Result.Succeeded;
        }
    }
}
