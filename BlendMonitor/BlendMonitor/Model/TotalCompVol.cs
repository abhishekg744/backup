using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class TotalCompVol
    {
        public double? TotCompVolTid { get; set; }
        public double MatId { get; set; }
        public string TotalCompTag { get; set; }
        public double? ReadValue { get; set; }
        public DateTime? ValueTime { get; set; }
        public string ValueQuality { get; set; }
        public string ReadEnabledFlag { get; set; }
        public string ScanEnabledFlag { get; set; }
        public string ScanGroupName { get; set; }
        public double? ScanGroupId { get; set; }
        public double? Scanrate { get; set; }
    }
}
