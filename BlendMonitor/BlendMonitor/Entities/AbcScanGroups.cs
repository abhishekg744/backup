using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcScanGroups
    {
        public AbcScanGroups()
        {
            AbcTags = new HashSet<AbcTags>();
        }

        public double Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ReadOrWriteFlag { get; set; }
        public double? Scanrate { get; set; }
        public string ScanEnabledFlag { get; set; }
        public string RefreshNowFlag { get; set; }
        public DateTime? LastRefreshTime { get; set; }
        public double? SkipScans { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public string DebugFlag { get; set; }
        public Guid Rowid { get; set; }

        public virtual ICollection<AbcTags> AbcTags { get; set; }
    }
}
