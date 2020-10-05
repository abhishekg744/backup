using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcPrdgrps
    {
        public AbcPrdgrps()
        {
            AbcPrdAdditives = new HashSet<AbcPrdAdditives>();
            AbcPumps = new HashSet<AbcPumps>();
        }

        public double Id { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public double? IconId { get; set; }
        public double VolumeUomId { get; set; }
        public string FlowDenominator { get; set; }
        public double? CycleTime { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public string PlanPrdgrpId { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcIcons Icon { get; set; }
        public virtual AbcUom VolumeUom { get; set; }
        public virtual ICollection<AbcPrdAdditives> AbcPrdAdditives { get; set; }
        public virtual ICollection<AbcPumps> AbcPumps { get; set; }
    }
}
