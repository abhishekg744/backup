using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlendIntervalProps
    {
        public double BlendId { get; set; }
        public double Sequence { get; set; }
        public double PropId { get; set; }
        public double? AnzId { get; set; }
        public double? SetpointPred { get; set; }
        public double? FeedbackPred { get; set; }
        public double? HeaderMin { get; set; }
        public double? HeaderMax { get; set; }
        public double? AnzRes { get; set; }
        public double? Bias { get; set; }
        public double? ResultCnt { get; set; }
        public string AnzGoodFlag { get; set; }
        public double? HighTarget { get; set; }
        public double? LowTarget { get; set; }
        public string CalcPropertyFlag { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? DestPred { get; set; }
        public double? FbPredBias { get; set; }
        public double? UnfiltBias { get; set; }
        public string BiascalcCurrent { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlendIntervals AbcBlendIntervals { get; set; }
        public virtual AbcAnzs Anz { get; set; }
        public virtual AbcProperties Prop { get; set; }
    }
}
