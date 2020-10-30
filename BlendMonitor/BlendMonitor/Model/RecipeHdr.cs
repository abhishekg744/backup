using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class RecipeHdr
    {
        public double BlendId { get; set; }
        public double MatId { get; set; }
        public string Component { get; set; }
        public double TankId { get; set; }
        public string Tank { get; set; }
        public double? Minimum { get; set; }
        public double? CurRecipe { get; set; }
        public double? Actual { get; set; }
        public double? Maximum { get; set; }
        public double? Pacing { get; set; }
        public double? Cost { get; set; }
        public double? LineupId { get; set; }
        public double? UsageId { get; set; }
    }
}
