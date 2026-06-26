using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ShopDraw.Actions;

namespace ShopDraw.Commands
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreateShopDrawCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            new CreateShopDrawAction(commandData.Application.ActiveUIDocument).Execute();
            return Result.Succeeded;
        }
    }
}
