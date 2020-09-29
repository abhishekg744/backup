using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcTanks
    {
        public AbcTanks()
        {
            AbcBlendDest = new HashSet<AbcBlendDest>();
            AbcBlendSources = new HashSet<AbcBlendSources>();
            AbcBlendSwingsFromTk = new HashSet<AbcBlendSwings>();
            AbcBlendSwingsToTk = new HashSet<AbcBlendSwings>();
            AbcBlenderComps = new HashSet<AbcBlenderComps>();
            AbcLabTankData = new HashSet<AbcLabTankData>();
            AbcProdLineupsDestination = new HashSet<AbcProdLineups>();
            AbcProdLineupsTransferLine = new HashSet<AbcProdLineups>();
            AbcTankComposition = new HashSet<AbcTankComposition>();
            AbcTankProps = new HashSet<AbcTankProps>();
        }

        public double Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double MatId { get; set; }
        public double PrdgrpId { get; set; }
        public double? RundnId { get; set; }
        public double? MaxVolTid { get; set; }
        public double? MinVolTid { get; set; }
        public double? DcsServiceTid { get; set; }
        public string AbcServiceFlag { get; set; }
        public double? PreVol { get; set; }
        public string InSerFlag { get; set; }
        public double? AvailVolId { get; set; }
        public double? VolUomId { get; set; }
        public double? FlowUomId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public string SourceDestnType { get; set; }
        public string AllowAutoSwing { get; set; }
        public string LimsTankName { get; set; }
        public string SharedName { get; set; }
        public double? DcsTankNum { get; set; }
        public double? Volume { get; set; }
        public string TqiDone { get; set; }
        public DateTime? TqiDate { get; set; }
        public string PlanTankId { get; set; }
        public double? LevelTid { get; set; }
        public double? MinLevelTid { get; set; }
        public double? MaxLevelTid { get; set; }
        public double? OutletVolTid { get; set; }
        public double? OrderId { get; set; }
        public string OrderSource { get; set; }
        public string LimsApprovalFlag { get; set; }
        public string SampleName { get; set; }
        public double? SampleBlendId { get; set; }
        public double? SampleStartVolume { get; set; }
        public double? SampleStopVolume { get; set; }
        public DateTime? SampleStartDate { get; set; }
        public DateTime? SampleStopDate { get; set; }
        public string LimsEthanolFlag { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcTags AvailVol { get; set; }
        public virtual AbcTags DcsServiceT { get; set; }
        public virtual AbcUom FlowUom { get; set; }
        public virtual AbcTags LevelT { get; set; }
        public virtual AbcMaterials Mat { get; set; }
        public virtual AbcTags MaxLevelT { get; set; }
        public virtual AbcTags MaxVolT { get; set; }
        public virtual AbcTags MinLevelT { get; set; }
        public virtual AbcTags MinVolT { get; set; }
        public virtual AbcTags OutletVolT { get; set; }
        public virtual AbcTags Rundn { get; set; }
        public virtual AbcBlends SampleBlend { get; set; }
        public virtual AbcUom VolUom { get; set; }
        public virtual AbcBlenders AbcBlendersCompositeTank { get; set; }
        public virtual AbcBlenders AbcBlendersSpotTank { get; set; }
        public virtual ICollection<AbcBlendDest> AbcBlendDest { get; set; }
        public virtual ICollection<AbcBlendSources> AbcBlendSources { get; set; }
        public virtual ICollection<AbcBlendSwings> AbcBlendSwingsFromTk { get; set; }
        public virtual ICollection<AbcBlendSwings> AbcBlendSwingsToTk { get; set; }
        public virtual ICollection<AbcBlenderComps> AbcBlenderComps { get; set; }
        public virtual ICollection<AbcLabTankData> AbcLabTankData { get; set; }
        public virtual ICollection<AbcProdLineups> AbcProdLineupsDestination { get; set; }
        public virtual ICollection<AbcProdLineups> AbcProdLineupsTransferLine { get; set; }
        public virtual ICollection<AbcTankComposition> AbcTankComposition { get; set; }
        public virtual ICollection<AbcTankProps> AbcTankProps { get; set; }
    }
}
