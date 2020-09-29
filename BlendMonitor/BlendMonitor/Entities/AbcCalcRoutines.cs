using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcCalcRoutines
    {
        public AbcCalcRoutines()
        {
            AbcBlendProps = new HashSet<AbcBlendProps>();
            AbcCalcCoefficients = new HashSet<AbcCalcCoefficients>();
            AbcPrdgrpProps = new HashSet<AbcPrdgrpProps>();
        }

        public double Id { get; set; }
        public string Name { get; set; }
        public double? OutputUomId { get; set; }
        public string CoefType { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcUom OutputUom { get; set; }
        public virtual ICollection<AbcBlendProps> AbcBlendProps { get; set; }
        public virtual ICollection<AbcCalcCoefficients> AbcCalcCoefficients { get; set; }
        public virtual ICollection<AbcPrdgrpProps> AbcPrdgrpProps { get; set; }
    }
}
