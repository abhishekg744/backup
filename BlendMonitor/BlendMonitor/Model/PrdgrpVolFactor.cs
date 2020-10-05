using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class PrdgrpVolFactor
    {
        public double VolumeUomId { get; set; }
        public string PrdgrpVolUnits { get; set; }
        public double? UomId { get; set; }
        public string AddVolUnits { get; set; }
        public double? UnitFactor { get; set; }        
    }
}
