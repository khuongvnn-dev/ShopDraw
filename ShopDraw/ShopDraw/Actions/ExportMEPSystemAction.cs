using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ShopDraw.Actions.Helpers;
using ShopDraw.Commons;
using ShopDraw.Views;

namespace ShopDraw.Actions
{
    internal class ExportMEPSystemAction
    {
        private ProgressBarView _progressBar;
        private Document _doc;
        public ExportMEPSystemAction(UIDocument uiDoc)
        {
            RevitBaseData.Init(uiDoc);
            _doc = RevitBaseData.Document;
            _progressBar = new ProgressBarView();
        }
        public void Execute()
        {
            _progressBar.Show();

            var elements = ExportMEPSystemHelper.GetElementsWCategory(_doc, _progressBar);

            var data = ExportMEPSystemHelper.ParseData(elements, _progressBar);

            ExportMEPSystemHelper.Export2Json(data, _progressBar);
            _progressBar.Close();
        }
    }
}
