﻿using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcGrades
    {
        public AbcGrades()
        {
            AbcPrdPropSpecs = new HashSet<AbcPrdPropSpecs>();
        }

        public double Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }

        public virtual ICollection<AbcPrdPropSpecs> AbcPrdPropSpecs { get; set; }
    }
}
