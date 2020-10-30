using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class PropNameModel
    {
        public double PropId { get; set; }
        public string PropName { get; set; }
        public double UomId { get; set; }
        public string UnitsName { get; set; }
        public string UnitsAlias { get; set; }
        public double? Value { get; set; }
    }
}
