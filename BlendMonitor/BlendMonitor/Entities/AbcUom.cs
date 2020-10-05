using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcUom
    {
        public AbcUom()
        {
            AbcBlendCompsCostUom = new HashSet<AbcBlendComps>();
            AbcBlendCompsFlowUom = new HashSet<AbcBlendComps>();
            AbcBlenders = new HashSet<AbcBlenders>();
            AbcBlendsCostUom = new HashSet<AbcBlends>();
            AbcBlendsFlowUom = new HashSet<AbcBlends>();
            AbcBlendsVolUom = new HashSet<AbcBlends>();
            AbcCalcRoutines = new HashSet<AbcCalcRoutines>();
            AbcPrdgrps = new HashSet<AbcPrdgrps>();
            AbcProjDefaults = new HashSet<AbcProjDefaults>();
            AbcProperties = new HashSet<AbcProperties>();
            AbcPumps = new HashSet<AbcPumps>();
            AbcStationsFlowUom = new HashSet<AbcStations>();
            AbcStationsRecipeUom = new HashSet<AbcStations>();
            AbcStationsVolUom = new HashSet<AbcStations>();
            AbcTanksFlowUom = new HashSet<AbcTanks>();
            AbcTanksVolUom = new HashSet<AbcTanks>();
            AbcUnitConversionFromUnitNavigation = new HashSet<AbcUnitConversion>();
            AbcUnitConversionToUnitNavigation = new HashSet<AbcUnitConversion>();
        }

        public double Id { get; set; }
        public string UnitsName { get; set; }
        public string Alias { get; set; }
        public string Dimension { get; set; }
        public string Nationality { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual ICollection<AbcBlendComps> AbcBlendCompsCostUom { get; set; }
        public virtual ICollection<AbcBlendComps> AbcBlendCompsFlowUom { get; set; }
        public virtual ICollection<AbcBlenders> AbcBlenders { get; set; }
        public virtual ICollection<AbcBlends> AbcBlendsCostUom { get; set; }
        public virtual ICollection<AbcBlends> AbcBlendsFlowUom { get; set; }
        public virtual ICollection<AbcBlends> AbcBlendsVolUom { get; set; }
        public virtual ICollection<AbcCalcRoutines> AbcCalcRoutines { get; set; }
        public virtual ICollection<AbcPrdgrps> AbcPrdgrps { get; set; }
        public virtual ICollection<AbcProjDefaults> AbcProjDefaults { get; set; }
        public virtual ICollection<AbcProperties> AbcProperties { get; set; }
        public virtual ICollection<AbcPumps> AbcPumps { get; set; }
        public virtual ICollection<AbcStations> AbcStationsFlowUom { get; set; }
        public virtual ICollection<AbcStations> AbcStationsRecipeUom { get; set; }
        public virtual ICollection<AbcStations> AbcStationsVolUom { get; set; }
        public virtual ICollection<AbcTanks> AbcTanksFlowUom { get; set; }
        public virtual ICollection<AbcTanks> AbcTanksVolUom { get; set; }
        public virtual ICollection<AbcUnitConversion> AbcUnitConversionFromUnitNavigation { get; set; }
        public virtual ICollection<AbcUnitConversion> AbcUnitConversionToUnitNavigation { get; set; }
    }
}
