using System;
using System.Collections.Generic;

namespace BlendMonitor.Entities
{
    public partial class AbcAnzHdrProps
    {
        public double BlenderId { get; set; }
        public double PropId { get; set; }
        public double AnzId { get; set; }
        public double? ResTagId { get; set; }
        public double? ResAvailId { get; set; }
        public double? RstResAvailId { get; set; }
        public double? ResStatusId { get; set; }
        public double? UpperLmt { get; set; }
        public double? LowerLmt { get; set; }
        public double? RateLmt { get; set; }
        public double? NoiseLevel { get; set; }
        public double? FrozenOpLmt { get; set; }
        public string GoodFlag { get; set; }
        public double? FilterFactor { get; set; }
        public double? BiasFilter { get; set; }
        public double? ModelErr { get; set; }
        public double? ModelErrThrsh { get; set; }
        public double? Offset { get; set; }
        public DateTime? OffsetTime { get; set; }
        public double? PrevRes { get; set; }
        public double? CurRes { get; set; }
        public double? CurOp { get; set; }
        public DateTime? ResTime { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public string CurStatus { get; set; }
        public string InUseFlag { get; set; }
        public double? ResultTimeLimit { get; set; }
        public double? TransportTime { get; set; }
        public double? CalibrationAccuracy { get; set; }
        public Guid Rowid { get; set; }

        public virtual AbcAnzs Anz { get; set; }
        public virtual AbcBlenders Blender { get; set; }
        public virtual AbcProperties Prop { get; set; }
        public virtual AbcTags ResAvail { get; set; }
        public virtual AbcTags ResStatus { get; set; }
        public virtual AbcTags ResTag { get; set; }
        public virtual AbcTags RstResAvail { get; set; }
    }
}
