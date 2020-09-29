using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcLabTankData
    {
        public double TankId { get; set; }
        public double PropId { get; set; }
        public double? LabValue { get; set; }
        public DateTime? SampleTime { get; set; }
        public string GoodFlag { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? UsageId { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcProperties Prop { get; set; }
        public virtual AbcTanks Tank { get; set; }
        public virtual AbcUsages Usage { get; set; }
    }
}
