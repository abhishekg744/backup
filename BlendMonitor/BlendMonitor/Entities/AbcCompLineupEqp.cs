using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcCompLineupEqp
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
        public Guid Rowid { get; set; }

        public virtual AbcCompLineups Line { get; set; }
        public virtual AbcPumps Pump { get; set; }
        public virtual AbcTags SelectionT { get; set; }
        public virtual AbcStations Station { get; set; }
    }
}
