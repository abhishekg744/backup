using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcPrdPropSpecs
    {
        public double PrdgrpId { get; set; }
        public double MatId { get; set; }
        public double PropId { get; set; }
        public double GradeId { get; set; }
        public double? ControlMin { get; set; }
        public double? ControlMax { get; set; }
        public double? SalesMin { get; set; }
        public double? SalesMax { get; set; }
        public double? Giveawaycost { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? ControlSafetyMarginMin { get; set; }
        public double? ControlSafetyMarginMax { get; set; }
        public string AnalysisMethod { get; set; }
        public string BoDisplay { get; set; }
        public double? HlimMin { get; set; }
        public double? HlimMax { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcGrades Grade { get; set; }
        public virtual AbcMaterials Mat { get; set; }
        public virtual AbcPrdgrps Prdgrp { get; set; }
        public virtual AbcProperties Prop { get; set; }
    }
}
