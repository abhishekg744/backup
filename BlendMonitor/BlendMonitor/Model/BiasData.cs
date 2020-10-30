using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class BiasData
    {
        public double Sequence { get; set; }
        public double PropId { get; set; }
        public double? FeedbackPred { get; set; }
        public double? AnzRes { get; set; }
        public double? FbPredBias { get; set; }
        public double? Bias { get; set; }
        public double? BiasFilter { get; set; }
        public double? Offset { get; set; }
        public double? MinBias { get; set; }
        public double? MaxBias { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? StopTime { get; set; }
        public double? ModelErrThrsh { get; set; }
        public double? RateLmt { get; set; }        
    }
}
