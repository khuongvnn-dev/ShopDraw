using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ShopDraw.Actions.Helpers;
using ShopDraw.Commons;
using ShopDraw.Views;
using System;

namespace ShopDraw.Actions
{
    internal class CreateShopDrawAction
    {
        private Document _doc;
        private ProgressBarView _progressBar;

        public CreateShopDrawAction(UIDocument uiDoc)
        {
            RevitBaseData.Init(uiDoc);
            _doc = RevitBaseData.Document;
            _progressBar = new ProgressBarView();
        }

        internal void Execute()
        {
            string filePath = CreateShopDrawHelper.GetFile();
            if (string.IsNullOrEmpty(filePath)) return;

            var data = CreateShopDrawHelper.ReadCsv(filePath);
            if (data == null || data.Count == 0) return;

            CreateShopDrawHelper.GenerateShopDocs(_doc, data, _progressBar);
        }
    }
}
