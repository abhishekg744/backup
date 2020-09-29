using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlendDestProps
    {
        public double BlendId { get; set; }
        public double TankId { get; set; }
        public double PropId { get; set; }
        public double? HeelValue { get; set; }
        public double? CurrentValue { get; set; }
        public string OnSpecFlag { get; set; }
        public double? LabValue { get; set; }
        public DateTime? LabTime { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlendDest AbcBlendDest { get; set; }
    }
}
