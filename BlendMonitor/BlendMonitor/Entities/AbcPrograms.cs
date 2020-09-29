using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcPrograms
    {
        public string Name { get; set; }
        public string Alias { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }
        public string CommandLineArgs { get; set; }
        public double StartupSequence { get; set; }
        public string State { get; set; }
        public string EnabledFlag { get; set; }
        public double? RestartCounter { get; set; }
        public double? RestartLimit { get; set; }
        public string RestartRequestFlag { get; set; }
        public double? CycleTime { get; set; }
        public DateTime? LastStartTime { get; set; }
        public DateTime? LastRunTime { get; set; }
        public string DebugFlag { get; set; }
        public double? DebugLevel { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Guid Rowid { get; set; }
    }
}
