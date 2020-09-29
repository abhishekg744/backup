using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlendStations
    {
        public double BlendId { get; set; }
        public double StationId { get; set; }
        public double MatId { get; set; }
        public string InUseFlag { get; set; }
        public double? MinFlow { get; set; }
        public double? MaxFlow { get; set; }
        public double? MeasFlow { get; set; }
        public double? PrevVol { get; set; }
        public double? CurVol { get; set; }
        public double? CurSetpoint { get; set; }
        public double? ActSetpoint { get; set; }
        public double? MatFraction { get; set; }
        public DateTime? LastReadtime { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlends Blend { get; set; }
        public virtual AbcMaterials Mat { get; set; }
        public virtual AbcStations Station { get; set; }
    }
}
