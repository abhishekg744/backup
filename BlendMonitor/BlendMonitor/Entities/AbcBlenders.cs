using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlenders
    {
        public AbcBlenders()
        {
            AbcAnzHdrProps = new HashSet<AbcAnzHdrProps>();
            AbcBlenderComps = new HashSet<AbcBlenderComps>();
            AbcBlends = new HashSet<AbcBlends>();
            AbcCompLineups = new HashSet<AbcCompLineups>();
            AbcProdLineups = new HashSet<AbcProdLineups>();
            AbcStations = new HashSet<AbcStations>();
        }

        public double Id { get; set; }
        public string Name { get; set; }
        public double PrdgrpId { get; set; }
        public double? TotalFlowId { get; set; }
        public double? FlowUomId { get; set; }
        public double? DebugLevel { get; set; }
        public double? DebugDepth { get; set; }
        public string DebugFlag { get; set; }
        public string InSerFlag { get; set; }
        public double? RbcStateTid { get; set; }
        public double? RbcModeTid { get; set; }
        public double? DownloadOkTid { get; set; }
        public string LocalGlobalFlag { get; set; }
        public string ProgramError { get; set; }
        public string StarblendBiasType { get; set; }
        public double? BlendIdTid { get; set; }
        public double? BlendDescTid { get; set; }
        public double? ProductTid { get; set; }
        public double? TargVolTid { get; set; }
        public double? RbcVolSpFbTid { get; set; }
        public double? TotalVolTid { get; set; }
        public double? TargRateTid { get; set; }
        public double? RbcRateSpFbTid { get; set; }
        public double? TotalFlowTid { get; set; }
        public double? StartTid { get; set; }
        public double? StopTid { get; set; }
        public double? PauseTid { get; set; }
        public double? RestartTid { get; set; }
        public double? DownloadingTid { get; set; }
        public double? RbcWdogTid { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? StartOkTid { get; set; }
        public string RundnFlag { get; set; }
        public double? SwingOccuredTid { get; set; }
        public double? SwingTid { get; set; }
        public double? SwingOccurredTid { get; set; }
        public double? VolFudgeFactor { get; set; }
        public double? OnSpecVol { get; set; }
        public double? Ectf { get; set; }
        public string CommErrFlag { get; set; }
        public string DownloadType { get; set; }
        public string OptimizeFlag { get; set; }
        public string CalcpropFlag { get; set; }
        public double? AnzrStartDelay { get; set; }
        public double DeltaRecipe { get; set; }
        public double? SwingExitTid { get; set; }
        public double? SwingVolTid { get; set; }
        public double? SwingExistTid { get; set; }
        public double? HdrVolume { get; set; }
        public double? GlPrdgrpId { get; set; }
        public double? DcsBlnameFbTid { get; set; }
        public double? StopOptVol { get; set; }
        public double? LineupSelTid { get; set; }
        public double? LineupPreselTid { get; set; }
        public double? LineupFeedbackTid { get; set; }
        public double? PumpaSelTid { get; set; }
        public double? PumpbSelTid { get; set; }
        public double? PumpcSelTid { get; set; }
        public double? PumpdSelTid { get; set; }
        public double? TankSelTid { get; set; }
        public double? TankPreselTid { get; set; }
        public double? TankFeedbackTid { get; set; }
        public double? CompositeTankId { get; set; }
        public double? SpotTankId { get; set; }
        public double? GradeTid { get; set; }
        public string OfflineName { get; set; }
        public string EthanolFlag { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcTags BlendDescT { get; set; }
        public virtual AbcTags BlendIdT { get; set; }
        public virtual AbcTanks CompositeTank { get; set; }
        public virtual AbcTags DcsBlnameFbT { get; set; }
        public virtual AbcTags DownloadOkT { get; set; }
        public virtual AbcTags DownloadingT { get; set; }
        public virtual AbcUom FlowUom { get; set; }
        public virtual AbcTags GradeT { get; set; }
        public virtual AbcTags LineupFeedbackT { get; set; }
        public virtual AbcTags LineupPreselT { get; set; }
        public virtual AbcTags LineupSelT { get; set; }
        public virtual AbcTags PauseT { get; set; }
        public virtual AbcTags ProductT { get; set; }
        public virtual AbcTags PumpaSelT { get; set; }
        public virtual AbcTags PumpbSelT { get; set; }
        public virtual AbcTags PumpcSelT { get; set; }
        public virtual AbcTags PumpdSelT { get; set; }
        public virtual AbcTags RbcModeT { get; set; }
        public virtual AbcTags RbcRateSpFbT { get; set; }
        public virtual AbcTags RbcStateT { get; set; }
        public virtual AbcTags RbcVolSpFbT { get; set; }
        public virtual AbcTags RbcWdogT { get; set; }
        public virtual AbcTags RestartT { get; set; }
        public virtual AbcTanks SpotTank { get; set; }
        public virtual AbcTags StartOkT { get; set; }
        public virtual AbcTags StartT { get; set; }
        public virtual AbcTags StopT { get; set; }
        public virtual AbcTags SwingExistT { get; set; }
        public virtual AbcTags SwingExitT { get; set; }
        public virtual AbcTags SwingOccurredT { get; set; }
        public virtual AbcTags SwingT { get; set; }
        public virtual AbcTags SwingVolT { get; set; }
        public virtual AbcTags TankFeedbackT { get; set; }
        public virtual AbcTags TankPreselT { get; set; }
        public virtual AbcTags TankSelT { get; set; }
        public virtual AbcTags TargRateT { get; set; }
        public virtual AbcTags TargVolT { get; set; }
        public virtual AbcTags TotalFlowT { get; set; }
        public virtual AbcTags TotalVolT { get; set; }
        public virtual ICollection<AbcAnzHdrProps> AbcAnzHdrProps { get; set; }
        public virtual ICollection<AbcBlenderComps> AbcBlenderComps { get; set; }
        public virtual ICollection<AbcBlends> AbcBlends { get; set; }
        public virtual ICollection<AbcCompLineups> AbcCompLineups { get; set; }
        public virtual ICollection<AbcProdLineups> AbcProdLineups { get; set; }
        public virtual ICollection<AbcStations> AbcStations { get; set; }
    }
}
