using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class RecipeBlend
    {
        public double BlendId { get; set; }
        public double MatId { get; set; }
        public string Component { get; set; }
        public double TankId { get; set; }
        public string Tank { get; set; }
        public double? TankMin { get; set; }
        public double? Preferred { get; set; }
        public double? Planned { get; set; }
        public double? Average { get; set; }
        public double? TankMax { get; set; }
        public double? Volume { get; set; }
        public double? LineupId { get; set; }        
    }
}
