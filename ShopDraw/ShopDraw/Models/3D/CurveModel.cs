using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace ShopDraw.Models._3D
{
    internal class CurveModel
    {
        public string Id { get; set; }
        public double Diameter { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string FamilyName { get; set; }
        public string TypeName { get; set; }
        public List<ConnectorModel> Connectors { get; set; }
    }
}