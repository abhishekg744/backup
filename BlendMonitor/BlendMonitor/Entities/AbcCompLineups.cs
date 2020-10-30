using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcCompLineups
    {
        public AbcCompLineups()
        {
            AbcBlenderSources = new HashSet<AbcBlenderSources>();
            AbcCompLineupEqp = new HashSet<AbcCompLineupEqp>();
        }

        public double Id { get; set; }
        public double? LineGeoId { get; set; }
        public double SourceId { get; set; }
        public double DestinationId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double? MinFlow { get; set; }
        public double? MaxFlow { get; set; }
        public double? Volume { get; set; }
        public double? SelectionTid { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? DcsLineupNum { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlenders Destination { get; set; }
        public virtual AbcLineupGeo LineGeo { get; set; }
        public virtual AbcTags SelectionT { get; set; }
        public virtual AbcTanks Source { get; set; }
        public virtual ICollection<AbcBlenderSources> AbcBlenderSources { get; set; }
        public virtual ICollection<AbcCompLineupEqp> AbcCompLineupEqp { get; set; }
    }
}
