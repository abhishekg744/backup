using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlendCompProps
    {
        public double BlendId { get; set; }
        public double MatId { get; set; }
        public double TankId { get; set; }
        public double PropId { get; set; }
        public string Source { get; set; }
        public double? Value { get; set; }
        public string GoodFlag { get; set; }
        public DateTime? ValueTime { get; set; }
        public DateTime? RecCreatedTime { get; set; }
        public double? ValidMin { get; set; }
        public double? ValidMax { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlendSources AbcBlendSources { get; set; }
    }
}
