using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class DestProps
    {
        public double PropId { get; set; }
        public double? Value { get; set; }
        public string Controlled { get; set; }
        public double AbsMin { get; set; }
        public double AbsMax { get; set; }
        public string Alias { get; set; }

    }
}
