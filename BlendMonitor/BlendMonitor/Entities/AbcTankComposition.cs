﻿using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcTankComposition
    {
        public double TankId { get; set; }
        public double MatId { get; set; }
        public double? Fraction { get; set; }
        public double? PrevFraction { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcMaterials Mat { get; set; }
        public virtual AbcTanks Tank { get; set; }
    }
}
