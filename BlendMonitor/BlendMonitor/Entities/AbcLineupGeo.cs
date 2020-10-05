using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcLineupGeo
    {
        public AbcLineupGeo()
        {
            AbcCompLineups = new HashSet<AbcCompLineups>();
        }

        public double Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double? IconId { get; set; }
        public double? NumOfPumps { get; set; }
        public double? NumOfStations { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcIcons Icon { get; set; }
        public virtual ICollection<AbcCompLineups> AbcCompLineups { get; set; }
    }
}
