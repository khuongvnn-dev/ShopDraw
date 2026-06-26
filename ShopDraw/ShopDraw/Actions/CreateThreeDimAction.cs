using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ShopDraw.Actions.Helpers;
using ShopDraw.Commons;
using ShopDraw.Models._3D;
using ShopDraw.Views;
using System;

namespace ShopDraw.Actions
{
    internal class CreateThreeDimAction
    {
        private UIDocument activeUIDocument;
        private Document _document;
        private ProgressBarView _progressBar;

        public CreateThreeDimAction(UIDocument activeUIDocument)
        {
            RevitBaseData.Init(activeUIDocument);
            _document = RevitBaseData.Document;
            _progressBar = new ProgressBarView();
        }

        internal void Execute()
        {
            _progressBar.Show();

            MepSystemModel model = CreateThreeDimHelper.GetData(_progressBar);

            if (model == null)
            {
                _progressBar.Close();
                TaskDialogUtil.ShowError("Failed to retrieve data for 3D model creation.");
                return;
            }

            Logger.CurrentMethod();
            Logger.Infor($"Summary: {model.Curves} curves, {model.Fittings} fittings");
            using (Transaction create3dTrans = new Transaction(_document, "Create 3D Model"))
            {
                FailureHandlingOptions options = create3dTrans.GetFailureHandlingOptions();
                options.SetFailuresPreprocessor(new WarningSwallower());
                create3dTrans.SetFailureHandlingOptions(options);

                try
                {
                    create3dTrans.Start();
                    CreateThreeDimHelper.Create3d(_document, model, _progressBar);
                    create3dTrans.Commit();
                }
                catch (Exception ex)
                {
                    create3dTrans.RollBack();
                    TaskDialogUtil.ShowError($"An error occurred while creating the 3D model: {ex.Message}");
                }
                finally
                {
                    _progressBar.Close();
                }
            }
        }
    }
}
