using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcUnitConversion
    {
        public double FromUnit { get; set; }
        public double ToUnit { get; set; }
        public double Factor { get; set; }
        public string FunctionName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }

        public virtual AbcUom FromUnitNavigation { get; set; }
        public virtual AbcUom ToUnitNavigation { get; set; }
    }
}
