﻿using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcTankProps
    {
        public double TankId { get; set; }
        public double PropId { get; set; }
        public double SourceId { get; set; }
        public double? Value { get; set; }
        public string GoodFlag { get; set; }
        public string SelectedFlag { get; set; }
        public DateTime? ValueTime { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcProperties Prop { get; set; }
        public virtual AbcPropSources Source { get; set; }
        public virtual AbcTanks Tank { get; set; }
    }
}
