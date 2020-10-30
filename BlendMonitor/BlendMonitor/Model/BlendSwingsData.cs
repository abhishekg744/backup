using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class BlendSwingsData
    {
        public double BlendId { get; set; }
        public double FromTkId { get; set; }
        public double ToTkId { get; set; }
        public DateTime DoneAt { get; set; }
        public double FromTkMatId { get; set; }
        public string SwingType { get; set; }
        public double? CriteriaId { get; set; }
        public string CriteriaName { get; set; }
        public double? CriteriaNumLmt { get; set; }
        public DateTime? CriteriaTimLmt { get; set; }
        public string SwingState { get; set; }
        public string AutoSwingFlag { get; set; }        
    }
}
