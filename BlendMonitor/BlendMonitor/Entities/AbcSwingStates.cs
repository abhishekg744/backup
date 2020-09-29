using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcSwingStates
    {
        public AbcSwingStates()
        {
            AbcBlendSwings = new HashSet<AbcBlendSwings>();
        }

        public string Name { get; set; }
        public string Alias { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual ICollection<AbcBlendSwings> AbcBlendSwings { get; set; }
    }
}
