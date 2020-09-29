using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlendIntervalComps
    {
        public double BlendId { get; set; }
        public double Sequence { get; set; }
        public double MatId { get; set; }
        public double? SpRecipe { get; set; }
        public double? Volume { get; set; }
        public double? HighTarget { get; set; }
        public double? LowTarget { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? IntRecipe { get; set; }
        public double? AvgHighTarget { get; set; }
        public double? AvgLowTarget { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlendIntervals AbcBlendIntervals { get; set; }
        public virtual AbcMaterials Mat { get; set; }
    }
}
