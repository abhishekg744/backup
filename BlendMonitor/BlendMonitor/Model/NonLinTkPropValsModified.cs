using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BlendMonitor.Model
{
    public class NonLinTkPropValsModified
    {
        public double Id { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public double? Value { get; set; }
        public DateTime? ValueTime { get; set; }
        public string GoodFlag { get; set; }
        public string SelectedFlag { get; set; }
        public string Calcrtn { get; set; }        
    }
}
