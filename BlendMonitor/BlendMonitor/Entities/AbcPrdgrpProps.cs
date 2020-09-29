using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcPrdgrpProps
    {
        public AbcPrdgrpProps()
        {
            AbcPrdgrpMatProps = new HashSet<AbcPrdgrpMatProps>();
        }

        public double PrdgrpId { get; set; }
        public double PropId { get; set; }
        public double? CalcId { get; set; }
        public string PrefSpec { get; set; }
        public double? MinBias { get; set; }
        public double? MaxBias { get; set; }
        public double ValidPrdMin { get; set; }
        public double ValidPrdMax { get; set; }
        public double ValidCompMin { get; set; }
        public double ValidCompMax { get; set; }
        public double? Giveawaycost { get; set; }
        public double? MaxPosErr { get; set; }
        public double? MaxNegErr { get; set; }
        public double? SortOrder { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public string BiascalcDefault { get; set; }
        public string BiascalcAnzFallback { get; set; }
        public double? SpotFilter { get; set; }
        public double? CompositeFilter { get; set; }
        public double? SpotBiasClamp { get; set; }
        public double? CompositeBiasClamp { get; set; }
        public string AltBiascalcDefault { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcCalcRoutines Calc { get; set; }
        public virtual AbcProperties Prop { get; set; }
        public virtual ICollection<AbcPrdgrpMatProps> AbcPrdgrpMatProps { get; set; }
    }
}
