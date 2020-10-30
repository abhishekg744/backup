using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class CheckAnzrMeasure
    {
        public double AnzId { get; set; }
        public double? CycleTime { get; set; }
        public DateTime? ResTime { get; set; }
        public double? TransportTime { get; set; }
        public double? FrozenOpLmt { get; set; }       
    }
}
