using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlendSwings
    {
        public double BlendId { get; set; }
        public double FromTkId { get; set; }
        public double ToTkId { get; set; }
        public DateTime DoneAt { get; set; }
        public string SwingType { get; set; }
        public double? CriteriaId { get; set; }
        public double? CriteriaNumLmt { get; set; }
        public DateTime? CriteriaTimLmt { get; set; }
        public string SwingState { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public string AutoSwingFlag { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlends Blend { get; set; }
        public virtual AbcTanks FromTk { get; set; }
        public virtual AbcSwingStates SwingStateNavigation { get; set; }
        public virtual AbcTanks ToTk { get; set; }
    }
}
