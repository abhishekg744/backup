using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class CompTanksData
    {
        public double MatId { get; set; }
        public string CompName { get; set; }
        public double TankId { get; set; }
        public string TankName { get; set; }
        public double? RundnId { get; set; }
        public double? AvailVolId { get; set; }
        public double? MinVolTid { get; set; }
        public double? DcsServiceTid { get; set; }
        public string AbcServiceFlag { get; set; }
        public double? LineupId { get; set; }
        public double? CurRecipe { get; set; }
        public double? MaxVolTid { get; set; }
        public string SourceDestnType { get; set; }
        public double? UsageId { get; set; }
    }
}
