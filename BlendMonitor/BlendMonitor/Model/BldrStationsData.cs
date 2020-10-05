using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class BldrStationsData
    {
        public double? StationId { get; set; }
        public string StationName { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public string InUseFlag { get; set; }
        public double? RcpSpTagId { get; set; }
        public double? RcpMeasTagId { get; set; }
        public double? MatNumTid { get; set; }
        public double? TankSelectNumTid { get; set; }
        public double? TankPreSelectNumTid { get; set; }
        public double DcsStationNum { get; set; }
        public double? SelectStationTid { get; set; }
        public double? TotalStationVolTid { get; set; }
        public double? WildFlagTid { get; set; }
        public double? TotalFlowControlTid { get; set; }
        public double? LineupSelTid { get; set; }
        public double? LineupPreSelTid { get; set; }
        public double? PumpASelTid { get; set; }
        public double? PumpBSelTid { get; set; }

        public double? PumpCSelTid { get; set; }
        public double? PumpDSelTid { get; set; }
        public double? LineupFeedbackTid { get; set; }
        public double? TankFeedbackTid { get; set; }
        public double? LineEqpOrder { get; set; }
        
    }
}
