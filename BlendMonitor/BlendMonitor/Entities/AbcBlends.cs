using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlends
    {
        public AbcBlends()
        {
            AbcBlendComps = new HashSet<AbcBlendComps>();
            AbcBlendDest = new HashSet<AbcBlendDest>();
            AbcBlendIntervals = new HashSet<AbcBlendIntervals>();
            AbcBlendProps = new HashSet<AbcBlendProps>();
            AbcBlendSampleProps = new HashSet<AbcBlendSampleProps>();
            AbcBlendSamples = new HashSet<AbcBlendSamples>();
            AbcBlendStations = new HashSet<AbcBlendStations>();
            AbcBlendSwings = new HashSet<AbcBlendSwings>();
            AbcTanks = new HashSet<AbcTanks>();
        }

        public double Id { get; set; }
        public double BlenderId { get; set; }
        public string Name { get; set; }
        public double ProductId { get; set; }
        public double? VolUomId { get; set; }
        public double? FlowUomId { get; set; }
        public double? CostUomId { get; set; }
        public double? TargetVol { get; set; }
        public double? TargetRate { get; set; }
        public string Batch { get; set; }
        public string Description { get; set; }
        public double GradeId { get; set; }
        public double? MinVol { get; set; }
        public double? MaxVol { get; set; }
        public double? MinRate { get; set; }
        public double? MaxRate { get; set; }
        public string Objective { get; set; }
        public double? Cost { get; set; }
        public double? RateSpOp { get; set; }
        public double? RateSpFb { get; set; }
        public string SimulatedFlag { get; set; }
        public string MaximizeBlendRateFlag { get; set; }
        public string PacingActFlag { get; set; }
        public string RampingActFlag { get; set; }
        public string BlendState { get; set; }
        public string PendingState { get; set; }
        public DateTime? LastOptimizedTime { get; set; }
        public double? CorrectionFactor { get; set; }
        public string ControlMode { get; set; }
        public double? CurrentVol { get; set; }
        public DateTime? ActualStart { get; set; }
        public DateTime? ActualEnd { get; set; }
        public DateTime? PlannedStart { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? DesOnspecVol { get; set; }
        public double? PreviousBlendId { get; set; }
        public string Comments { get; set; }
        public string IgnoreLineConstraints { get; set; }
        public DateTime? RcpDownloadTime { get; set; }
        public string VolumeConstraints { get; set; }
        public string LocalGlobalFlag { get; set; }
        public DateTime? ExpectedEnd { get; set; }
        public double? BlendUsers { get; set; }
        public string TqiNowFlag { get; set; }
        public string PoolingFlag { get; set; }
        public string HdrPropConstraints { get; set; }
        public string BiasOverrideFlag { get; set; }
        public string UpdateHeelFlag { get; set; }
        public string HeelUpdOccurredFlag { get; set; }
        public double? BatchTargetVol { get; set; }
        public string UseWildFlowFlag { get; set; }
        public string EthanolBldgReqdFlag { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlenders Blender { get; set; }
        public virtual AbcUom CostUom { get; set; }
        public virtual AbcUom FlowUom { get; set; }
        public virtual AbcMaterials Product { get; set; }
        public virtual AbcUom VolUom { get; set; }
        public virtual ICollection<AbcBlendComps> AbcBlendComps { get; set; }
        public virtual ICollection<AbcBlendDest> AbcBlendDest { get; set; }
        public virtual ICollection<AbcBlendIntervals> AbcBlendIntervals { get; set; }
        public virtual ICollection<AbcBlendProps> AbcBlendProps { get; set; }
        public virtual ICollection<AbcBlendSampleProps> AbcBlendSampleProps { get; set; }
        public virtual ICollection<AbcBlendSamples> AbcBlendSamples { get; set; }
        public virtual ICollection<AbcBlendStations> AbcBlendStations { get; set; }
        public virtual ICollection<AbcBlendSwings> AbcBlendSwings { get; set; }
        public virtual ICollection<AbcTanks> AbcTanks { get; set; }
    }
}
