using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace ShopDraw.Models._3D
{
    internal class FittingModel
    {
        public string Id { get; set; }
        public RvtXYZ Origin { get; set; }
        public RvtXYZ BasisX { get; set; }
        public RvtXYZ BasisY { get; set; }
        public RvtXYZ BasisZ { get; set; }
        public string FamilyName { get; set; }
        public string TypeName { get; set; }
        public string LevelName { get; set; }
        public List<ConnectorModel> Connectors { get; set; }
    }
}