using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcPrdAdditives
    {
        public double PrdgrpId { get; set; }
        public double ProductId { get; set; }
        public double AdditiveId { get; set; }
        public double? DefaultSetpoint { get; set; }
        public double? UomId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? UnitFactor { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcMaterials Additive { get; set; }
        public virtual AbcPrdgrps Prdgrp { get; set; }
        public virtual AbcMaterials Product { get; set; }
    }
}
