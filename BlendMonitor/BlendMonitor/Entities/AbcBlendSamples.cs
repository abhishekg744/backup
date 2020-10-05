using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlendSamples
    {
        public double BlendId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public double? StartVolume { get; set; }
        public double? StopVolume { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? StopDate { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public string ProcessSampleFlag { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlends Blend { get; set; }
    }
}
