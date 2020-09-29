using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcIcons
    {
        public AbcIcons()
        {
            AbcPrdgrps = new HashSet<AbcPrdgrps>();
        }

        public double Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string VisibleFlag { get; set; }
        public byte[] Icon { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual ICollection<AbcPrdgrps> AbcPrdgrps { get; set; }
    }
}
