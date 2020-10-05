using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcStations
    {
        public AbcStations()
        {
            AbcBlendStations = new HashSet<AbcBlendStations>();
            AbcCompLineupEqp = new HashSet<AbcCompLineupEqp>();
        }

        public double Id { get; set; }
        public double BlenderId { get; set; }
        public string Name { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public string InUseFlag { get; set; }
        public double? FlowSpTagId { get; set; }
        public double? FlowMeasTagId { get; set; }
        public double? FlowOpTagId { get; set; }
        public double? RcpSpTagId { get; set; }
        public double? RcpMeasTagId { get; set; }
        public double? RcpSp { get; set; }
        public double? FlowUomId { get; set; }
        public double? VolUomId { get; set; }
        public double? RecipeUomId { get; set; }
        public double? PaceMeFlagTid { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public double? MatNumTid { get; set; }
        public double? TankSelectNumTid { get; set; }
        public double? TankPreselectNumTid { get; set; }
        public double DcsStationNum { get; set; }
        public double? SelectStationTid { get; set; }
        public double? TotalStationVolTid { get; set; }
        public double? WildFlagTid { get; set; }
        public double? TotalFlowControlTid { get; set; }
        public double? LineupSelTid { get; set; }
        public double? LineupPreselTid { get; set; }
        public double? LineupFeedbackTid { get; set; }
        public double? PumpaSelTid { get; set; }
        public double? PumpbSelTid { get; set; }
        public double? PumpcSelTid { get; set; }
        public double? PumpdSelTid { get; set; }
        public double? TankFeedbackTid { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcBlenders Blender { get; set; }
        public virtual AbcTags FlowMeasTag { get; set; }
        public virtual AbcTags FlowOpTag { get; set; }
        public virtual AbcTags FlowSpTag { get; set; }
        public virtual AbcUom FlowUom { get; set; }
        public virtual AbcTags LineupFeedbackT { get; set; }
        public virtual AbcTags LineupPreselT { get; set; }
        public virtual AbcTags LineupSelT { get; set; }
        public virtual AbcTags MatNumT { get; set; }
        public virtual AbcTags PaceMeFlagT { get; set; }
        public virtual AbcTags PumpaSelT { get; set; }
        public virtual AbcTags PumpbSelT { get; set; }
        public virtual AbcTags PumpcSelT { get; set; }
        public virtual AbcTags PumpdSelT { get; set; }
        public virtual AbcTags RcpMeasTag { get; set; }
        public virtual AbcTags RcpSpTag { get; set; }
        public virtual AbcUom RecipeUom { get; set; }
        public virtual AbcTags SelectStationT { get; set; }
        public virtual AbcTags TankFeedbackT { get; set; }
        public virtual AbcTags TankPreselectNumT { get; set; }
        public virtual AbcTags TankSelectNumT { get; set; }
        public virtual AbcTags TotalFlowControlT { get; set; }
        public virtual AbcTags TotalStationVolT { get; set; }
        public virtual AbcUom VolUom { get; set; }
        public virtual AbcTags WildFlagT { get; set; }
        public virtual ICollection<AbcBlendStations> AbcBlendStations { get; set; }
        public virtual ICollection<AbcCompLineupEqp> AbcCompLineupEqp { get; set; }
    }
}
