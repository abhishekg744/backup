using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcProdLineups
    {
        public AbcProdLineups()
        {
            AbcBlendDest = new HashSet<AbcBlendDest>();
            AbcBlenderDest = new HashSet<AbcBlenderDest>();
            AbcProdLineupEqp = new HashSet<AbcProdLineupEqp>();
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
        public double? SelectionFbTid { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? PreselectionTid { get; set; }
        public double? TransferLineId { get; set; }
        public double? DcsLineupNum { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcTanks Destination { get; set; }
        public virtual AbcTags PreselectionT { get; set; }
        public virtual AbcTags SelectionFbT { get; set; }
        public virtual AbcTags SelectionT { get; set; }
        public virtual AbcBlenders Source { get; set; }
        public virtual AbcTanks TransferLine { get; set; }
        public virtual ICollection<AbcBlendDest> AbcBlendDest { get; set; }
        public virtual ICollection<AbcBlenderDest> AbcBlenderDest { get; set; }
        public virtual ICollection<AbcProdLineupEqp> AbcProdLineupEqp { get; set; }
    }
}
