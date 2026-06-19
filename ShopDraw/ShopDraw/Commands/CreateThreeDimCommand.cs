using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ShopDraw.Commands
{
    [TransactionAttribute(TransactionMode.Manual)]
    internal class CreateThreeDimCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog.Show("Thông báo", "Lệnh tạo 3D đang chạy!");
            return Result.Succeeded;
        }
    }
}
