using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class SampleIntvProps
    {
        public double? FeedbackPred { get; set; }
        public double? Bias { get; set; }
        public double? FbPredBias { get; set; }
        public string BiascalcCurrent { get; set; }
        public string BiascalcDefault { get; set; }
        public string BiascalcAnzFallback { get; set; }
        public double? SpotFilter { get; set; }
        public double? CompositeFilter { get; set; }
        public double? SpotBiasClamp { get; set; }
        public double? CompositeBiasClamp { get; set; }
    }
}
