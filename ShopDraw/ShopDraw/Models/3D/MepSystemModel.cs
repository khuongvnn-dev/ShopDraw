using System.Collections.Generic;

namespace ShopDraw.Models._3D
{
    internal class MepSystemModel
    {
        public List<LevelModel> Levels { get; set; }
        public List<CurveModel> Curves { get; set; }
        public List<FittingModel> Fittings { get; set; }
    }
}
