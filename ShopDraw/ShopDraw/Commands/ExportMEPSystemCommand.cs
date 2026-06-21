using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ShopDraw.Actions;
using System;
namespace ShopDraw.Commands
{
    [TransactionAttribute(TransactionMode.ReadOnly)]

    internal class ExportMEPSystemCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            new ExportMEPSystemAction(commandData.Application.ActiveUIDocument).Execute();
            return Result.Succeeded;
        }
    }
}
