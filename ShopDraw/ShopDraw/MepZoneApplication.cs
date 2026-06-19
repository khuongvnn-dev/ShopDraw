using Autodesk.Revit.UI;
using ShopDraw.Commands;
using System;

namespace ShopDraw
{
    public class MepZoneApplication : IExternalApplication
    {
        private UIApplication _uiApp;
        private string assemblyPath = typeof(MepZoneApplication).Assembly.Location;

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            //registry an event document changed
            if (application != null)
            {
                try
                {
                    //Setup Logger
                    Logger.Setup();

                    //Init Ribbon
                    InitMepZoneRibbon(application);
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex);
                    return Result.Failed;
                }
            }

            return Result.Succeeded;
        }

        private void InitMepZoneRibbon(UIControlledApplication application)
        {
            string instructionFile = @"https://dub.sh/kieran_web";
            ContextualHelp contextualHelp = new ContextualHelp(ContextualHelpType.Url, instructionFile);

            var panelUser = RibbonUtils.CreatePanel(application, "ShopDraw", "Tools");

            var ThreeDimBtn = new PushButtonData("HaweeDefinitions.CMD_SHOW_AGENT", "Show Agent", assemblyPath, typeof(CreateThreeDimCommand).FullName);
            ThreeDimBtn.LargeImage = RibbonUtils.ConvertFromBitmap(Properties.Resources._3d_32);
            ThreeDimBtn.Image = RibbonUtils.ConvertFromBitmap(Properties.Resources._3d_16);
            panelUser.AddItem(ThreeDimBtn);
            ThreeDimBtn.SetContextualHelp(contextualHelp);

            var ShopDrawBtn = new PushButtonData("HaweeDefinitions.CMD_CREATE_SHOP_DRAWING", "Shop Drawing", assemblyPath, typeof(CreateShopDrawCommand).FullName);
            ShopDrawBtn.LargeImage = RibbonUtils.ConvertFromBitmap(Properties.Resources.document_32);
            ShopDrawBtn.Image = RibbonUtils.ConvertFromBitmap(Properties.Resources.document_16);
            panelUser.AddItem(ShopDrawBtn);
            ShopDrawBtn.SetContextualHelp(contextualHelp);
        }
    }
}
