using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class DenaturantProps
    {
        public double Id { get; set; }
        public string Name { get; set; }
        public double DefVal { get; set; }
        public string UnitsName { get; set; }
        public string Dimension { get; set; }
        public string CalcRtn { get; set; }
    }
}
