using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlendIntervals
    {
        public AbcBlendIntervals()
        {
            AbcBlendIntervalComps = new HashSet<AbcBlendIntervalComps>();
            AbcBlendIntervalProps = new HashSet<AbcBlendIntervalProps>();
        }

        public double BlendId { get; set; }
        public double Sequence { get; set; }
        public double? OptRunId { get; set; }
        public DateTime? Starttime { get; set; }
        public DateTime? Stoptime { get; set; }
        public double? Volume { get; set; }
        public double? Cost { get; set; }
        public double? OptimizerSetting { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? UnfiltBias { get; set; }
        public double? BlendVolume { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlends Blend { get; set; }
        public virtual ICollection<AbcBlendIntervalComps> AbcBlendIntervalComps { get; set; }
        public virtual ICollection<AbcBlendIntervalProps> AbcBlendIntervalProps { get; set; }
    }
}
