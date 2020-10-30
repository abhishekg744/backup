using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcPrdgrpUsages
    {
        public double PrdgrpId { get; set; }
        public double MatId { get; set; }
        public double UsageId { get; set; }
        public double? PosDevCost { get; set; }
        public double? NegDevCost { get; set; }
        public double? Cost { get; set; }
        public double? CostUomId { get; set; }
        public string StarblendProductType { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcUom CostUom { get; set; }
        public virtual AbcMaterials Mat { get; set; }
        public virtual AbcPrdgrps Prdgrp { get; set; }
        public virtual AbcUsages Usage { get; set; }
    }
}
