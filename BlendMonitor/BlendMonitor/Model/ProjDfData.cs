using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class ProjDfData
    {
        public double dblCmdTimeout;
        public string strAllowStartStop;
        public string strAllowRateVolUpds;
        public string strAllowCompUpds;
        public double? vntVolTolr;
        public double? vntRcpTolr;
        public double? vntMaxIntvLen;
        public double? vntMinIntvLen;
        public double? vntMaxMatCost;
        public double? vntMinMatCost;
        public string strLimsSampleStartStopType;
        public string strAllowSCSampling;
        public double dblTotalizerTimestampTolerance;
        public double dblSleepTime;
        public double? sngFGEEtoh;
        public double? sngMinEtoh;
        public string strProjName;
        public string strLIMSSeparateProps;
        // --- RW 25-Jan-17 Gasoline Ethanol blending remedial ---
        // RW 14-Oct-16 Gasoline Ethanol blending
    }
}
