using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ShopDraw.Commons
{
    public class RevitBaseData
    {
        public static Application Application { get; set; }
        public static UIApplication UIApplication { get; set; }
        public static UIDocument UIDocument { get; set; }
        public static Document Document { get; set; }

        public static void Init(UIDocument uidoc)
        {
            UIDocument = uidoc;
            Document = uidoc.Document;
            Application = uidoc.Application.Application;
            UIApplication = uidoc.Application;
        }
    }
}
