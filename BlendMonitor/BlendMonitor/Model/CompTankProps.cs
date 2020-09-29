using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class CompTankProps
    {
        public double MatId { get; set; }
        public double TankId { get; set; }
        public double PropId { get; set; }
        public string SourceName { get; set; }
        public double? Value { get; set; }
        public DateTime? ValueTime { get; set; }
        public string GoodFlag { get; set; }
    }
}
