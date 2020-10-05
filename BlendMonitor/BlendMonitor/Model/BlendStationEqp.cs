using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class BlendStationEqp
    {
        public double StationId { get; set; }
        public double? MinFlow { get; set; }
        public double? MaxFlow { get; set; }
        public double LineEqpOrder { get; set; }
    }
}
