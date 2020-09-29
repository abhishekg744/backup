using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcUsages
    {
        public AbcUsages()
        {
            AbcBlendComps = new HashSet<AbcBlendComps>();
            AbcLabTankData = new HashSet<AbcLabTankData>();
            AbcPrdgrpMatProps = new HashSet<AbcPrdgrpMatProps>();
        }

        public double Id { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual ICollection<AbcBlendComps> AbcBlendComps { get; set; }
        public virtual ICollection<AbcLabTankData> AbcLabTankData { get; set; }
        public virtual ICollection<AbcPrdgrpMatProps> AbcPrdgrpMatProps { get; set; }
    }
}
