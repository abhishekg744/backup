using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcTags
    {
        public AbcTags()
        {
            AbcAnzHdrPropsResAvail = new HashSet<AbcAnzHdrProps>();
            AbcAnzHdrPropsResStatus = new HashSet<AbcAnzHdrProps>();
            AbcAnzHdrPropsRstResAvail = new HashSet<AbcAnzHdrProps>();
            AbcAnzs = new HashSet<AbcAnzs>();
            AbcBlenderCompsLineupFeedbackT = new HashSet<AbcBlenderComps>();
            AbcBlenderCompsLineupPreselT = new HashSet<AbcBlenderComps>();
            AbcBlenderCompsLineupSelT = new HashSet<AbcBlenderComps>();
            AbcBlenderCompsRecipeMeasT = new HashSet<AbcBlenderComps>();
            AbcBlenderCompsRecipeSpT = new HashSet<AbcBlenderComps>();
            AbcBlenderCompsSelectCompT = new HashSet<AbcBlenderComps>();
            AbcBlenderCompsSwingOccurredT = new HashSet<AbcBlenderComps>();
            AbcBlenderCompsSwingT = new HashSet<AbcBlenderComps>();
            AbcBlenderCompsTotCompVolT = new HashSet<AbcBlenderComps>();
            AbcBlenderCompsWildFlagT = new HashSet<AbcBlenderComps>();
            AbcBlendersBlendDescT = new HashSet<AbcBlenders>();
            AbcBlendersBlendIdT = new HashSet<AbcBlenders>();
            AbcBlendersDcsBlnameFbT = new HashSet<AbcBlenders>();
            AbcBlendersDownloadOkT = new HashSet<AbcBlenders>();
            AbcBlendersDownloadingT = new HashSet<AbcBlenders>();
            AbcBlendersGradeT = new HashSet<AbcBlenders>();
            AbcBlendersLineupFeedbackT = new HashSet<AbcBlenders>();
            AbcBlendersLineupPreselT = new HashSet<AbcBlenders>();
            AbcBlendersLineupSelT = new HashSet<AbcBlenders>();
            AbcBlendersPauseT = new HashSet<AbcBlenders>();
            AbcBlendersProductT = new HashSet<AbcBlenders>();
            AbcBlendersPumpaSelT = new HashSet<AbcBlenders>();
            AbcBlendersPumpbSelT = new HashSet<AbcBlenders>();
            AbcBlendersPumpcSelT = new HashSet<AbcBlenders>();
            AbcBlendersPumpdSelT = new HashSet<AbcBlenders>();
            AbcBlendersRbcModeT = new HashSet<AbcBlenders>();
            AbcBlendersRbcRateSpFbT = new HashSet<AbcBlenders>();
            AbcBlendersRbcStateT = new HashSet<AbcBlenders>();
            AbcBlendersRbcVolSpFbT = new HashSet<AbcBlenders>();
            AbcBlendersRbcWdogT = new HashSet<AbcBlenders>();
            AbcBlendersRestartT = new HashSet<AbcBlenders>();
            AbcBlendersStartOkT = new HashSet<AbcBlenders>();
            AbcBlendersStartT = new HashSet<AbcBlenders>();
            AbcBlendersStopT = new HashSet<AbcBlenders>();
            AbcBlendersSwingExistT = new HashSet<AbcBlenders>();
            AbcBlendersSwingExitT = new HashSet<AbcBlenders>();
            AbcBlendersSwingOccurredT = new HashSet<AbcBlenders>();
            AbcBlendersSwingT = new HashSet<AbcBlenders>();
            AbcBlendersSwingVolT = new HashSet<AbcBlenders>();
            AbcBlendersTankFeedbackT = new HashSet<AbcBlenders>();
            AbcBlendersTankPreselT = new HashSet<AbcBlenders>();
            AbcBlendersTankSelT = new HashSet<AbcBlenders>();
            AbcBlendersTargRateT = new HashSet<AbcBlenders>();
            AbcBlendersTargVolT = new HashSet<AbcBlenders>();
            AbcBlendersTotalFlowT = new HashSet<AbcBlenders>();
            AbcBlendersTotalVolT = new HashSet<AbcBlenders>();
            AbcProdLineupsPreselectionT = new HashSet<AbcProdLineups>();
            AbcProdLineupsSelectionFbT = new HashSet<AbcProdLineups>();
            AbcProdLineupsSelectionT = new HashSet<AbcProdLineups>();
            AbcProjDefaultsUserMonitorTid1Navigation = new HashSet<AbcProjDefaults>();
            AbcProjDefaultsUserMonitorTid2Navigation = new HashSet<AbcProjDefaults>();
            AbcProjDefaultsWdogT = new HashSet<AbcProjDefaults>();
            AbcStationsFlowMeasTag = new HashSet<AbcStations>();
            AbcStationsFlowOpTag = new HashSet<AbcStations>();
            AbcStationsFlowSpTag = new HashSet<AbcStations>();
            AbcStationsLineupFeedbackT = new HashSet<AbcStations>();
            AbcStationsLineupPreselT = new HashSet<AbcStations>();
            AbcStationsLineupSelT = new HashSet<AbcStations>();
            AbcStationsMatNumT = new HashSet<AbcStations>();
            AbcStationsPaceMeFlagT = new HashSet<AbcStations>();
            AbcStationsPumpaSelT = new HashSet<AbcStations>();
            AbcStationsPumpbSelT = new HashSet<AbcStations>();
            AbcStationsPumpcSelT = new HashSet<AbcStations>();
            AbcStationsPumpdSelT = new HashSet<AbcStations>();
            AbcStationsRcpMeasTag = new HashSet<AbcStations>();
            AbcStationsRcpSpTag = new HashSet<AbcStations>();
            AbcStationsSelectStationT = new HashSet<AbcStations>();
            AbcStationsTankFeedbackT = new HashSet<AbcStations>();
            AbcStationsTankPreselectNumT = new HashSet<AbcStations>();
            AbcStationsTankSelectNumT = new HashSet<AbcStations>();
            AbcStationsTotalFlowControlT = new HashSet<AbcStations>();
            AbcStationsTotalStationVolT = new HashSet<AbcStations>();
            AbcStationsWildFlagT = new HashSet<AbcStations>();
            AbcTanksAvailVol = new HashSet<AbcTanks>();
            AbcTanksDcsServiceT = new HashSet<AbcTanks>();
            AbcTanksLevelT = new HashSet<AbcTanks>();
            AbcTanksMaxLevelT = new HashSet<AbcTanks>();
            AbcTanksMaxVolT = new HashSet<AbcTanks>();
            AbcTanksMinLevelT = new HashSet<AbcTanks>();
            AbcTanksMinVolT = new HashSet<AbcTanks>();
            AbcTanksOutletVolT = new HashSet<AbcTanks>();
            AbcTanksRundn = new HashSet<AbcTanks>();
        }

        public double Id { get; set; }
        public double SystemId { get; set; }
        public string BlockType { get; set; }
        public string Name { get; set; }
        public string Attribute { get; set; }
        public double? ScanGroupId { get; set; }
        public string Description { get; set; }
        public string ReadEnabledFlag { get; set; }
        public string WriteNowFlag { get; set; }
        public double? ReadValue { get; set; }
        public double? WriteValue { get; set; }
        public string ReadString { get; set; }
        public string WriteString { get; set; }
        public string ValueQuality { get; set; }
        public DateTime? ValueTime { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcScanGroups ScanGroup { get; set; }
        public virtual AbcAnzHdrProps AbcAnzHdrPropsResTag { get; set; }
        public virtual ICollection<AbcAnzHdrProps> AbcAnzHdrPropsResAvail { get; set; }
        public virtual ICollection<AbcAnzHdrProps> AbcAnzHdrPropsResStatus { get; set; }
        public virtual ICollection<AbcAnzHdrProps> AbcAnzHdrPropsRstResAvail { get; set; }
        public virtual ICollection<AbcAnzs> AbcAnzs { get; set; }
        public virtual ICollection<AbcBlenderComps> AbcBlenderCompsLineupFeedbackT { get; set; }
        public virtual ICollection<AbcBlenderComps> AbcBlenderCompsLineupPreselT { get; set; }
        public virtual ICollection<AbcBlenderComps> AbcBlenderCompsLineupSelT { get; set; }
        public virtual ICollection<AbcBlenderComps> AbcBlenderCompsRecipeMeasT { get; set; }
        public virtual ICollection<AbcBlenderComps> AbcBlenderCompsRecipeSpT { get; set; }
        public virtual ICollection<AbcBlenderComps> AbcBlenderCompsSelectCompT { get; set; }
        public virtual ICollection<AbcBlenderComps> AbcBlenderCompsSwingOccurredT { get; set; }
        public virtual ICollection<AbcBlenderComps> AbcBlenderCompsSwingT { get; set; }
        public virtual ICollection<AbcBlenderComps> AbcBlenderCompsTotCompVolT { get; set; }
        public virtual ICollection<AbcBlenderComps> AbcBlenderCompsWildFlagT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersBlendDescT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersBlendIdT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersDcsBlnameFbT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersDownloadOkT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersDownloadingT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersGradeT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersLineupFeedbackT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersLineupPreselT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersLineupSelT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersPauseT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersProductT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersPumpaSelT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersPumpbSelT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersPumpcSelT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersPumpdSelT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersRbcModeT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersRbcRateSpFbT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersRbcStateT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersRbcVolSpFbT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersRbcWdogT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersRestartT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersStartOkT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersStartT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersStopT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersSwingExistT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersSwingExitT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersSwingOccurredT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersSwingT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersSwingVolT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersTankFeedbackT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersTankPreselT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersTankSelT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersTargRateT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersTargVolT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersTotalFlowT { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlendersTotalVolT { get; set; }
        public virtual ICollection<AbcProdLineups> AbcProdLineupsPreselectionT { get; set; }
        public virtual ICollection<AbcProdLineups> AbcProdLineupsSelectionFbT { get; set; }
        public virtual ICollection<AbcProdLineups> AbcProdLineupsSelectionT { get; set; }
        public virtual ICollection<AbcProjDefaults> AbcProjDefaultsUserMonitorTid1Navigation { get; set; }
        public virtual ICollection<AbcProjDefaults> AbcProjDefaultsUserMonitorTid2Navigation { get; set; }
        public virtual ICollection<AbcProjDefaults> AbcProjDefaultsWdogT { get; set; }
        public virtual ICollection<AbcStations> AbcStationsFlowMeasTag { get; set; }
        public virtual ICollection<AbcStations> AbcStationsFlowOpTag { get; set; }
        public virtual ICollection<AbcStations> AbcStationsFlowSpTag { get; set; }
        public virtual ICollection<AbcStations> AbcStationsLineupFeedbackT { get; set; }
        public virtual ICollection<AbcStations> AbcStationsLineupPreselT { get; set; }
        public virtual ICollection<AbcStations> AbcStationsLineupSelT { get; set; }
        public virtual ICollection<AbcStations> AbcStationsMatNumT { get; set; }
        public virtual ICollection<AbcStations> AbcStationsPaceMeFlagT { get; set; }
        public virtual ICollection<AbcStations> AbcStationsPumpaSelT { get; set; }
        public virtual ICollection<AbcStations> AbcStationsPumpbSelT { get; set; }
        public virtual ICollection<AbcStations> AbcStationsPumpcSelT { get; set; }
        public virtual ICollection<AbcStations> AbcStationsPumpdSelT { get; set; }
        public virtual ICollection<AbcStations> AbcStationsRcpMeasTag { get; set; }
        public virtual ICollection<AbcStations> AbcStationsRcpSpTag { get; set; }
        public virtual ICollection<AbcStations> AbcStationsSelectStationT { get; set; }
        public virtual ICollection<AbcStations> AbcStationsTankFeedbackT { get; set; }
        public virtual ICollection<AbcStations> AbcStationsTankPreselectNumT { get; set; }
        public virtual ICollection<AbcStations> AbcStationsTankSelectNumT { get; set; }
        public virtual ICollection<AbcStations> AbcStationsTotalFlowControlT { get; set; }
        public virtual ICollection<AbcStations> AbcStationsTotalStationVolT { get; set; }
        public virtual ICollection<AbcStations> AbcStationsWildFlagT { get; set; }
        public virtual ICollection<AbcTanks> AbcTanksAvailVol { get; set; }
        public virtual ICollection<AbcTanks> AbcTanksDcsServiceT { get; set; }
        public virtual ICollection<AbcTanks> AbcTanksLevelT { get; set; }
        public virtual ICollection<AbcTanks> AbcTanksMaxLevelT { get; set; }
        public virtual ICollection<AbcTanks> AbcTanksMaxVolT { get; set; }
        public virtual ICollection<AbcTanks> AbcTanksMinLevelT { get; set; }
        public virtual ICollection<AbcTanks> AbcTanksMinVolT { get; set; }
        public virtual ICollection<AbcTanks> AbcTanksOutletVolT { get; set; }
        public virtual ICollection<AbcTanks> AbcTanksRundn { get; set; }
    }
}
