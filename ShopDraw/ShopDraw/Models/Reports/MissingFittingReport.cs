namespace ShopDraw.Models.Reports
{
    public class MissingFittingReport
    {
        public string JsonId { get; set; }
        public string SystemTypeName { get; set; }
        public string MissingFamilyName { get; set; }
        public string MissingTypeName { get; set; }
        public string Note { get; set; }
    }
}
