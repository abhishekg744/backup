﻿using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlendSourceSeq
    {
        public double BlendId { get; set; }
        public double MatId { get; set; }
        public double TankId { get; set; }
        public double SwingSequence { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public double? VolUsed { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlendSources AbcBlendSources { get; set; }
    }
}
