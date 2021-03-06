﻿using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlenderDest
    {
        public double BlenderId { get; set; }
        public double TankId { get; set; }
        public double? SelectionTid { get; set; }
        public double? SelectionFbTid { get; set; }
        public double? PreselectionTid { get; set; }
        public double DefaultLineupId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? DestSelectNameTid { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlenders Blender { get; set; }
        public virtual AbcProdLineups DefaultLineup { get; set; }
        public virtual AbcTags DestSelectNameT { get; set; }
        public virtual AbcTags PreselectionT { get; set; }
        public virtual AbcTags SelectionFbT { get; set; }
        public virtual AbcTags SelectionT { get; set; }
        public virtual AbcTanks Tank { get; set; }
    }
}
