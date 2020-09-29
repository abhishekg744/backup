using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlendSources
    {
        public AbcBlendSources()
        {
            AbcBlendCompProps = new HashSet<AbcBlendCompProps>();
        }

        public double BlendId { get; set; }
        public double MatId { get; set; }
        public double TankId { get; set; }
        public double? LineupId { get; set; }
        public string InUseFlag { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? MasterLineupId { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlendComps AbcBlendComps { get; set; }
        public virtual AbcTanks Tank { get; set; }
        public virtual ICollection<AbcBlendCompProps> AbcBlendCompProps { get; set; }
    }
}
