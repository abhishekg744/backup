using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcBlenderComps
    {
        public double BlenderId { get; set; }
        public double MatId { get; set; }
        public double? SerialNo { get; set; }
        public double? RecipeSpTid { get; set; }
        public double? RecipeMeasTid { get; set; }
        public double? SelectCompTid { get; set; }
        public double? TotCompVolTid { get; set; }
        public double? DefaultTkId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? WildFlagTid { get; set; }
        public double? SwingOccurredTid { get; set; }
        public double? SwingTid { get; set; }
        public double? TotflowControlTid { get; set; }
        public double? LineupSelTid { get; set; }
        public double? LineupPreselTid { get; set; }
        public double? LineupFeedbackTid { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlenders Blender { get; set; }
        public virtual AbcTanks DefaultTk { get; set; }
        public virtual AbcTags LineupFeedbackT { get; set; }
        public virtual AbcTags LineupPreselT { get; set; }
        public virtual AbcTags LineupSelT { get; set; }
        public virtual AbcMaterials Mat { get; set; }
        public virtual AbcTags RecipeMeasT { get; set; }
        public virtual AbcTags RecipeSpT { get; set; }
        public virtual AbcTags SelectCompT { get; set; }
        public virtual AbcTags SwingOccurredT { get; set; }
        public virtual AbcTags SwingT { get; set; }
        public virtual AbcTags TotCompVolT { get; set; }
        public virtual AbcTags WildFlagT { get; set; }
    }
}
