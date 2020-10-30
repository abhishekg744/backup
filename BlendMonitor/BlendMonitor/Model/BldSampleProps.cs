using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class BldSampleProps
    {
        public double BlendId { get; set; }
        public string SampleName { get; set; }
        public string ProcessSampleFlag { get; set; }
        public double PropId { get; set; }
        public double? Value { get; set; }
        public string SampleType{ get; set; }
        public string UsedFlag{ get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? StopDate { get; set; }
        public double? StartVolume { get; set; }
        public double? StopVolume { get; set; }
    }
}
