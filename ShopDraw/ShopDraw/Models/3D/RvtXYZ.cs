using Autodesk.Revit.DB;
using System.Globalization;

namespace ShopDraw.Models._3D
{
    public class RvtXYZ
    {
        public string X { get; set; }
        public string Y { get; set; }
        public string Z { get; set; }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", X, Y, Z);
        }

        public XYZ ToXYZ()
        {
            return new XYZ(
                double.Parse(X, CultureInfo.InvariantCulture),
                double.Parse(Y, CultureInfo.InvariantCulture),
                double.Parse(Z, CultureInfo.InvariantCulture)
            );
        }

        public RvtXYZ(double x, double y, double z)
        {
            X = x.ToString("R", CultureInfo.InvariantCulture);
            Y = y.ToString("R", CultureInfo.InvariantCulture);
            Z = z.ToString("R", CultureInfo.InvariantCulture);
        }

        public RvtXYZ() { }

        public RvtXYZ(XYZ xyz)
        {
            X = xyz.X.ToString("R", CultureInfo.InvariantCulture);
            Y = xyz.Y.ToString("R", CultureInfo.InvariantCulture);
            Z = xyz.Z.ToString("R", CultureInfo.InvariantCulture);
        }

        public static double GetWidthFromBBox(RvtXYZ minDto, RvtXYZ maxDto)
        {
            var min = minDto.ToXYZ();
            var max = maxDto.ToXYZ();
            return max.X - min.X;
        }

        public static double GetHeightFromBBox(RvtXYZ minDto, RvtXYZ maxDto)
        {
            var min = minDto.ToXYZ();
            var max = maxDto.ToXYZ();
            return max.Y - min.Y;
        }
    }
}