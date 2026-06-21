namespace ShopDraw.Models._3D
{
    internal class CurveModel
    {
        public string Id { get; set; }
        public string FamilyName { get; set; }
        public string TypeName { get; set; }
        public string LevelName { get; set; }
        public RvtXYZ StartPoint { get; set; }
        public RvtXYZ EndPoint { get; set; }
        public double Diameter { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string StartConnectedToId { get; set; }
        public string EndConnectedToId { get; set; }
    }
}