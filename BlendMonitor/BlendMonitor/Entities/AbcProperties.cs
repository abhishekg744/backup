using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcProperties
    {
        public AbcProperties()
        {
            AbcAnzHdrProps = new HashSet<AbcAnzHdrProps>();
            AbcBlendIntervalProps = new HashSet<AbcBlendIntervalProps>();
            AbcBlendProps = new HashSet<AbcBlendProps>();
            AbcCalcCoefficients = new HashSet<AbcCalcCoefficients>();
            AbcLabTankData = new HashSet<AbcLabTankData>();
            AbcPrdgrpMatProps = new HashSet<AbcPrdgrpMatProps>();
            AbcPrdgrpProps = new HashSet<AbcPrdgrpProps>();
            AbcTankProps = new HashSet<AbcTankProps>();
        }

        public double Id { get; set; }
        public double UomId { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public double AbsMin { get; set; }
        public double AbsMax { get; set; }
        public double? DisplayDigits { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public string LimsPropName { get; set; }
        public string StarblendSupportedFlag { get; set; }
        public string OfflineName { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcUom Uom { get; set; }
        public virtual ICollection<AbcAnzHdrProps> AbcAnzHdrProps { get; set; }
        public virtual ICollection<AbcBlendIntervalProps> AbcBlendIntervalProps { get; set; }
        public virtual ICollection<AbcBlendProps> AbcBlendProps { get; set; }
        public virtual ICollection<AbcCalcCoefficients> AbcCalcCoefficients { get; set; }
        public virtual ICollection<AbcLabTankData> AbcLabTankData { get; set; }
        public virtual ICollection<AbcPrdgrpMatProps> AbcPrdgrpMatProps { get; set; }
        public virtual ICollection<AbcPrdgrpProps> AbcPrdgrpProps { get; set; }
        public virtual ICollection<AbcTankProps> AbcTankProps { get; set; }
    }
}
