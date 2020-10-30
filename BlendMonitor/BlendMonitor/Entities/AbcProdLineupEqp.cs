using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcProdLineupEqp
    {
        public double LineId { get; set; }
        public double LineEqpOrder { get; set; }
        public double? PumpId { get; set; }
        public double? StationId { get; set; }
        public double? SelectionTid { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? TransferLineId { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcProdLineups Line { get; set; }
        public virtual AbcPumps Pump { get; set; }
        public virtual AbcTags SelectionT { get; set; }
        public virtual AbcStations Station { get; set; }
        public virtual AbcTanks TransferLine { get; set; }
    }
}
