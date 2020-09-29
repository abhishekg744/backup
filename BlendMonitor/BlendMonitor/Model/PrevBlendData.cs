using System;
using System.Collections.Generic;
using System.Text;
using static BlendMonitor.Constans;

namespace BlendMonitor.Model
{
    class PrevBlendData
    {
        public double lngID;
        public BlendCmds? enumCmd;
        public DateTime[] arCmdTime = new DateTime[Enum.GetNames(typeof(BlendCmds)).Length - 1];
        public string strState;
        public int intCurIntv;
        public long lngPrevBldId;
        public string strPrevBldDescr;
        public double? sngPrevBldTargVol;
        public double? sngPrevBldTargRate;
    }
}
