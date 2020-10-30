using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcMaterials
    {
        public AbcMaterials()
        {
            AbcBlendComps = new HashSet<AbcBlendComps>();
            AbcBlendIntervalComps = new HashSet<AbcBlendIntervalComps>();
            AbcBlendStations = new HashSet<AbcBlendStations>();
            AbcBlenderComps = new HashSet<AbcBlenderComps>();
            AbcBlends = new HashSet<AbcBlends>();
            AbcPrdAdditivesAdditive = new HashSet<AbcPrdAdditives>();
            AbcPrdAdditivesProduct = new HashSet<AbcPrdAdditives>();
            AbcPrdPropSpecs = new HashSet<AbcPrdPropSpecs>();
            AbcPrdgrpMatProps = new HashSet<AbcPrdgrpMatProps>();
            AbcPrdgrpUsages = new HashSet<AbcPrdgrpUsages>();
            AbcTankComposition = new HashSet<AbcTankComposition>();
            AbcTanks = new HashSet<AbcTanks>();
        }

        public double Id { get; set; }
        public string Name { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? DcsMatNum { get; set; }
        public string PlanMatId { get; set; }
        public Guid Rowid { get; set; }

        public virtual ICollection<AbcBlendComps> AbcBlendComps { get; set; }
        public virtual ICollection<AbcBlendIntervalComps> AbcBlendIntervalComps { get; set; }
        public virtual ICollection<AbcBlendStations> AbcBlendStations { get; set; }
        public virtual ICollection<AbcBlenderComps> AbcBlenderComps { get; set; }
        public virtual ICollection<AbcBlends> AbcBlends { get; set; }
        public virtual ICollection<AbcPrdAdditives> AbcPrdAdditivesAdditive { get; set; }
        public virtual ICollection<AbcPrdAdditives> AbcPrdAdditivesProduct { get; set; }
        public virtual ICollection<AbcPrdPropSpecs> AbcPrdPropSpecs { get; set; }
        public virtual ICollection<AbcPrdgrpMatProps> AbcPrdgrpMatProps { get; set; }
        public virtual ICollection<AbcPrdgrpUsages> AbcPrdgrpUsages { get; set; }
        public virtual ICollection<AbcTankComposition> AbcTankComposition { get; set; }
        public virtual ICollection<AbcTanks> AbcTanks { get; set; }
    }
}
