using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcCalcCoefficients
    {
        public double CalcId { get; set; }
        public double CoefOrder { get; set; }
        public double PropId { get; set; }
        public double PrdgrpId { get; set; }
        public double? Coef { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcCalcRoutines Calc { get; set; }
        public virtual AbcProperties Prop { get; set; }
    }
}
