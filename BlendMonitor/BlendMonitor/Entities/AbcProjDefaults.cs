using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcProjDefaults
    {
        public double Id { get; set; }
        public string Name { get; set; }
        public double RecipeTolerance { get; set; }
        public double? ProgCycleTime { get; set; }
        public double? FrozenOpLim { get; set; }
        public double? CorrectionFactor { get; set; }
        public double? MinIntervalLen { get; set; }
        public double? MaxIntervalLen { get; set; }
        public double? StartTimeout { get; set; }
        public double? MinMatCost { get; set; }
        public double? MaxMatCost { get; set; }
        public double? VolTolerance { get; set; }
        public double CycleTime { get; set; }
        public double? RefreshRate { get; set; }
        public double VolumeUomId { get; set; }
        public string FlowDenominator { get; set; }
        public string AllowRateAndVolUpdsFlag { get; set; }
        public string AllowStartAndStopFlag { get; set; }
        public string AllowCompUpdates { get; set; }
        public string OptValveConstraint { get; set; }
        public double? MaxValveOpening { get; set; }
        public double ErrTolerance { get; set; }
        public double? WdogTid { get; set; }
        public double? WdogLimit { get; set; }
        public double LogMsgKeepDays { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double PropViolationTolerance { get; set; }
        public double? RecipeViolationTolerance { get; set; }
        public double? UserMonitorTid1 { get; set; }
        public double? UserMonitorTid2 { get; set; }
        public double? SwingTimeOut { get; set; }
        public string DcsCommFlag { get; set; }
        public string LimsServer { get; set; }
        public string LimsId { get; set; }
        public string LimsPwd { get; set; }
        public string LimsViewName { get; set; }
        public string LimsTankField { get; set; }
        public string LimsPropField { get; set; }
        public string LimsSampleField { get; set; }
        public string LimsTimestpField { get; set; }
        public string LimsStatusField { get; set; }
        public string LimsValStrg { get; set; }
        public string LimsApprovedFlag { get; set; }
        public string DownloadType { get; set; }
        public string VarTargetRateFlag { get; set; }
        public string Reserved01 { get; set; }
        public string LinePropertiesFlag { get; set; }
        public string Reserved03 { get; set; }
        public string Reserved02 { get; set; }
        public string StarblendInstPath { get; set; }
        public string Reserved04 { get; set; }
        public double? TankFractionTol { get; set; }
        public string Version { get; set; }
        public string PlanPrdgrpField { get; set; }
        public string PlanMtypeField { get; set; }
        public string PlanManagerField { get; set; }
        public string PlanServer { get; set; }
        public string PlanId { get; set; }
        public string PlanPwd { get; set; }
        public string PlanDbType { get; set; }
        public string PlanDbPath { get; set; }
        public string PlanViewName { get; set; }
        public string PlanSourceField { get; set; }
        public string PlanDestField { get; set; }
        public string PlanMatField { get; set; }
        public string PlanBatchField { get; set; }
        public string PlanStartField { get; set; }
        public string PlanDbName { get; set; }
        public double? SystemId { get; set; }
        public string OpnEngine { get; set; }
        public string ReportPaperType { get; set; }
        public string HdrPropConstraints { get; set; }
        public string ZeroRcpConstraintFlag { get; set; }
        public string LimsAllowCompSampling { get; set; }
        public string LimsCompSampleIdField { get; set; }
        public string LimsCompBlendIdField { get; set; }
        public string LimsCompStartField { get; set; }
        public string LimsCompStopField { get; set; }
        public string LimsCompStartStopType { get; set; }
        public string MessageType { get; set; }
        public string BatchBlendVolFlag { get; set; }
        public double? ScaleOptVol { get; set; }
        public string ScannerTagnameEscape { get; set; }
        public string CopyInitrcpPrevFlag { get; set; }
        public string LimsDbType { get; set; }
        public string LimsDbName { get; set; }
        public string LimsDbPath { get; set; }
        public string LimsLatestTimestamp { get; set; }
        public double LimsMaxRecords { get; set; }
        public string AllowApDl { get; set; }
        public double BmonSleepTime { get; set; }
        public double OmonSleepTime { get; set; }
        public double TmonSleepTime { get; set; }
        public double TotalizerTimestampTolerance { get; set; }
        public string BoiBlend { get; set; }
        public string BoiDescription { get; set; }
        public string BoiBlender { get; set; }
        public string BoiProduct { get; set; }
        public string BoiGrade { get; set; }
        public string BoiTank { get; set; }
        public string BoiStart { get; set; }
        public string BoiRate { get; set; }
        public string BoiVolume { get; set; }
        public string BoiComments { get; set; }
        public string BoiComp { get; set; }
        public string BoiCompMin { get; set; }
        public string BoiCompMax { get; set; }
        public string BoiCompRcp { get; set; }
        public string BoiCompCost { get; set; }
        public string BoiCompDevcost { get; set; }
        public string BoiProp { get; set; }
        public string BoiPropMin { get; set; }
        public string BoiPropMax { get; set; }
        public string BoiPropHdrPred { get; set; }
        public string BoiPropTkFinalpred { get; set; }
        public string BoiPropGivecost { get; set; }
        public string BoiActionAdd { get; set; }
        public string BoiActionReplace { get; set; }
        public string BoiActionDelete { get; set; }
        public double? FgeEtoh { get; set; }
        public double? MinEtoh { get; set; }
        public string LimsSeparatePropsFlag { get; set; }
        public double? EtohPropsLabLimit { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcTags UserMonitorTid1Navigation { get; set; }
        public virtual AbcTags UserMonitorTid2Navigation { get; set; }
        public virtual AbcUom VolumeUom { get; set; }
        public virtual AbcTags WdogT { get; set; }
    }
}
