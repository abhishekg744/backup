﻿using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcPropSources
    {
        public AbcPropSources()
        {
            AbcBlendProps = new HashSet<AbcBlendProps>();
            AbcTankProps = new HashSet<AbcTankProps>();
        }

        public double Id { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual ICollection<AbcBlendProps> AbcBlendProps { get; set; }
        public virtual ICollection<AbcTankProps> AbcTankProps { get; set; }
    }
}
