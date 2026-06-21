namespace ShopDraw.Models.Reports
{
    public class ImportFailureRow
    {
        public string ElementType { get; set; } // "Fitting" hoặc "Curve"
        public string JsonId { get; set; }
        public string FamilyName { get; set; }
        public string TypeName { get; set; }
        public string LevelName { get; set; }
        public string Reason { get; set; }
    }
}