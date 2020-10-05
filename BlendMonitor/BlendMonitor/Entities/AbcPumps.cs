using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcPumps
    {
        public AbcPumps()
        {
            AbcCompLineupEqp = new HashSet<AbcCompLineupEqp>();
        }

        public double Id { get; set; }
        public double? StatusTagId { get; set; }
        public double? PrdgrpId { get; set; }
        public string Name { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? InuseTagId { get; set; }
        public string InSerFlag { get; set; }
        public double? FlowUomId { get; set; }
        public double? ModeTid { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? DcsPumpId { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcUom FlowUom { get; set; }
        public virtual AbcTags InuseTag { get; set; }
        public virtual AbcTags ModeT { get; set; }
        public virtual AbcPrdgrps Prdgrp { get; set; }
        public virtual AbcTags StatusTag { get; set; }
        public virtual ICollection<AbcCompLineupEqp> AbcCompLineupEqp { get; set; }
    }
}
