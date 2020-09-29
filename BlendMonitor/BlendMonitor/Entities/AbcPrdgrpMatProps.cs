using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcPrdgrpMatProps
    {
        public double PrdgrpId { get; set; }
        public double MatId { get; set; }
        public double UsageId { get; set; }
        public double PropId { get; set; }
        public double ValidMin { get; set; }
        public double ValidMax { get; set; }
        public double DefVal { get; set; }
        public double? PoolVal { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? CorrelationBias { get; set; }
        public string LabDisplay { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcMaterials Mat { get; set; }
        public virtual AbcPrdgrpProps Pr { get; set; }
        public virtual AbcProperties Prop { get; set; }
        public virtual AbcUsages Usage { get; set; }
    }
}
