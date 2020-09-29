using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class BldCompUsage
    {
        public double? UsageId { get; set; }
        public string UsageName { get; set; }
        public string UsageAlias { get; set; }
        public string RcpConstraintType { get; set; }
    }
}
