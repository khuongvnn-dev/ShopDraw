using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ShopDraw.Actions;

namespace ShopDraw.Commands
{
    [TransactionAttribute(TransactionMode.Manual)]
    internal class CreateThreeDimCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            new CreateThreeDimAction(commandData.Application.ActiveUIDocument).Execute();
            return Result.Succeeded;
        }
    }
}
