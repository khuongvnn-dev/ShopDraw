using Autodesk.Revit.DB;

namespace ShopDraw.Models._3D
{
    public class ConnectorModel
    {
        public string ConnectorToId { get; set; }
        public RvtXYZ Origin { get; set; }
    }
}