using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlendComps
    {
        public AbcBlendComps()
        {
            AbcBlendSources = new HashSet<AbcBlendSources>();
        }

        public double BlendId { get; set; }
        public double MatId { get; set; }
        public double? PrefRecipe { get; set; }
        public double? PlanRecipe { get; set; }
        public double? CurRecipe { get; set; }
        public double? ActRecipe { get; set; }
        public double? OptRecipe { get; set; }
        public double? FlowUomId { get; set; }
        public double? CostUomId { get; set; }
        public double? UsageId { get; set; }
        public double? Cost { get; set; }
        public double Wild { get; set; }
        public double? PosDevCost { get; set; }
        public double? NegDevCost { get; set; }
        public double? MinFlow { get; set; }
        public double? MaxFlow { get; set; }
        public double? HighCons { get; set; }
        public double? LowCons { get; set; }
        public double? PacingFactor { get; set; }
        public double? TankMin { get; set; }
        public double? TankMax { get; set; }
        public double? HighTarget { get; set; }
        public double? LowTarget { get; set; }
        public double? Volume { get; set; }
        public double? VolOffset { get; set; }
        public double? AvgRecipe { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public string RcpConstraintType { get; set; }
        public double? RequiredVolume { get; set; }
        public double? ReqFlowRate { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlends Blend { get; set; }
        public virtual AbcUom CostUom { get; set; }
        public virtual AbcUom FlowUom { get; set; }
        public virtual AbcMaterials Mat { get; set; }
        public virtual AbcUsages Usage { get; set; }
        public virtual ICollection<AbcBlendSources> AbcBlendSources { get; set; }
    }
}
