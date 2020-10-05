using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlendSampleProps
    {
        public double BlendId { get; set; }
        public string SampleName { get; set; }
        public double PropId { get; set; }
        public double? Value { get; set; }
        public double? AnzValue { get; set; }
        public string UsedFlag { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? Feedback { get; set; }
        public double? SetpointPred { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlends Blend { get; set; }
        public virtual AbcProperties Prop { get; set; }
    }
}
