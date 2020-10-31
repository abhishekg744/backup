using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor.Model
{
    public class CurBlendData
    {
        public double lngID;
        public string strName;
        public double? sngTgtVol;
        public double? sngTgtRate;
        public int intGrdID;
        public double? vntMinVol;
        public double? vntMaxVol;
        public double? vntMinRate;
        public double? vntMaxRate;
        public string strState = "";
        public double? dblCorrFac;
        public string strCtlMode;
        public double? sngCurVol;
        public DateTime dteActualStart;
        public int intProdID;
        public string vntPendSt;
        public int intCurIntv;
        public double? vntIntvLen;
        public string strBldDesc;
        public double? lngPrevBldId;
        public string strIgnLineConstr;
        public string strRampingActFlag;
        public string strBiasOverrideFlag;
        //this flag is used to force the update of sample (composite/spot) bias calc all the way to intv 1
        public string vntEtohBldgReqd;
    }
}
