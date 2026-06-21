using System;

namespace ShopDraw.Commons
{
    public static class UnitConverter
    {
        // 1 foot = 304.8 mm
        private const double MmPerFoot = 304.8;

        #region Configurable Properties (Các cấu hình có thể điều chỉnh)

        /// <summary>
        /// Số chữ số thập phân dùng để làm tròn sau khi chuyển đổi.
        /// Mặc định là 4 (Ví dụ: 300.1234)
        /// </summary>
        public static int MidpointRoundingDecimals { get; set; } = 4;

        /// <summary>
        /// Độ sai số cho phép khi so sánh hai giá trị double (đơn vị: feet).
        /// Thường dùng trong các bộ lọc cao độ Level hoặc so sánh tọa độ Revit.
        /// </summary>
        public static double Epsilon { get; set; } = 0.001;

        #endregion

        #region Conversion Methods (Phương thức chuyển đổi)

        /// <summary>
        /// Chuyển đổi từ milimét (mm) sang Feet (đơn vị chuẩn của Revit).
        /// </summary>
        public static double MmToFeet(double mm)
        {
            double feet = mm / MmPerFoot;
            return Math.Round(feet, MidpointRoundingDecimals);
        }

        /// <summary>
        /// Chuyển đổi từ Feet (Revit) sang milimét (mm).
        /// </summary>
        public static double FeetToMm(double feet)
        {
            double mm = feet * MmPerFoot;
            return Math.Round(mm, MidpointRoundingDecimals);
        }

        #endregion

        #region Comparison Methods (Phương thức so sánh hình học)

        /// <summary>
        /// So sánh hai giá trị cao độ/tọa độ (kiểu double) dựa trên cấu hình Epsilon.
        /// Trả về true nếu khoảng cách giữa chúng nhỏ hơn Epsilon.
        /// </summary>
        public static bool IsAlmostEqual(double value1, double value2)
        {
            return Math.Abs(value1 - value2) < Epsilon;
        }

        /// <summary>
        /// So sánh hai giá trị mm (chuyển về feet trước rồi so sánh theo Epsilon).
        /// </summary>
        public static bool IsAlmostEqualMm(double mm1, double mm2)
        {
            return IsAlmostEqual(MmToFeet(mm1), MmToFeet(mm2));
        }

        #endregion
    }
}