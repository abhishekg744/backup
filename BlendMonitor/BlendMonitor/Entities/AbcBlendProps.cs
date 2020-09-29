using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlendProps
    {
        public double BlendId { get; set; }
        public double PropId { get; set; }
        public double? AnzResTagId { get; set; }
        public string Controlled { get; set; }
        public double? Giveawaycost { get; set; }
        public double? Value { get; set; }
        public double? SourceId { get; set; }
        public double? ValidMin { get; set; }
        public double? ValidMax { get; set; }
        public string ModelErrExistsFlag { get; set; }
        public string ModelErrClrdFlag { get; set; }
        public double? AnzOffset { get; set; }
        public double? CorrectionFactor { get; set; }
        public double? HdrMin { get; set; }
        public double? HdrMax { get; set; }
        public double? CalcId { get; set; }
        public double? ControlMin { get; set; }
        public double? ControlMax { get; set; }
        public double? SalesMin { get; set; }
        public double? SalesMax { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? InitialBias { get; set; }
        public string AnalysisMethod { get; set; }
        public double? HlimMin { get; set; }
        public double? HlimMax { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlends Blend { get; set; }
        public virtual AbcCalcRoutines Calc { get; set; }
        public virtual AbcProperties Prop { get; set; }
        public virtual AbcPropSources Source { get; set; }
    }
}
