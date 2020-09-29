using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcAnzs
    {
        public AbcAnzs()
        {
            AbcAnzHdrProps = new HashSet<AbcAnzHdrProps>();
            AbcBlendIntervalProps = new HashSet<AbcBlendIntervalProps>();
        }

        public double Id { get; set; }
        public string Type { get; set; }
        public double PrdgrpId { get; set; }
        public string Name { get; set; }
        public double? TransportTime { get; set; }
        public double? CycleTime { get; set; }
        public double? ResultTimeLimit { get; set; }
        public double? CalibrateTimeLimit { get; set; }
        public string State { get; set; }
        public double? StateTagId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public string ResetRequestFlag { get; set; }
        public string AbcServiceFlag { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcTags StateTag { get; set; }
        public virtual ICollection<AbcAnzHdrProps> AbcAnzHdrProps { get; set; }
        public virtual ICollection<AbcBlendIntervalProps> AbcBlendIntervalProps { get; set; }
    }
}
