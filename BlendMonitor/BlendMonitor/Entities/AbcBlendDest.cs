using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlendDest
    {
        public AbcBlendDest()
        {
            AbcBlendDestProps = new HashSet<AbcBlendDestProps>();
        }

        public double BlendId { get; set; }
        public double TankId { get; set; }
        public double? HeelVolume { get; set; }
        public string InUseFlag { get; set; }
        public string FixHeelFlag { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double LineupId { get; set; }
        public string DestSelectName { get; set; }
        public string FlushTkFlag { get; set; }
        public string EndLinefillTkFlag { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlends Blend { get; set; }
        public virtual AbcProdLineups Lineup { get; set; }
        public virtual AbcTanks Tank { get; set; }
        public virtual ICollection<AbcBlendDestProps> AbcBlendDestProps { get; set; }
    }
}
