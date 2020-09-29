using BlendMonitor.Entities;
using BlendMonitor.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static BlendMonitor.Constans;

namespace BlendMonitor.Repository
{
    public interface IBlendMonitorRepository
    {
        Task<List<AbcBlenders>> GetBlenders();
        Task<AbcPrograms> ProcessEnabled();
        double? GetCycleTime(string name);
        Task<int> SetStartTime();
        Task<(ProjDfData, double)> getProjDefaults(ProjDfData gProjDfs);
        Task<int> ChkPendingOnBldr(double blenderId);
        Task<(DebugLevels, string, string)> GetBlenderDebugs(string strPrgName, string strDebugFlag, DebugLevels enumDebugLevel, int intBlenderID,
                                             string strBlenderDebugFlag, DebugLevels enumBlenderDebugLevel, string strBlenderName);
        Task<List<AbcBlends>> GetActvBldsData(double blenderId);
        Task<List<AbcBlendDest>> GetDestTkFlags(double lngBlendId);
        Task<List<AbcBlendSwings>> GetBldSwgTransferVol(double lngBlendId, double? lngFlushTankId, double? lngDestTkId);
        Task<List<AbcBlends>> GetReadyBlds(double blenderId);
        Task<int> SetPendingState(string state, double blendId);
        Task<int> SetBlenderErrorFlag(string flag, string name);
        Task<int> SetBlenderErrFlag(string prgmError, double belnderId, string text);
        Task<string> GetCommWDTag();
        Task<AbcProjDefaults> SwgDefTimeOut();
        Task<(string, double, DateTime, string, string, string, string)> GetTagValAndFlags(double? RBC_WDOG_TID, string vntDummy, double vntTagVal, DateTime? vntTagValTime, string vntTagValQlt,
             string readEnabled, string scanEnabled, string vntScanRateName);
        Task<double?> GetScanRate(string name);
        Task<int> SetWriteTagVal(int intUpperLimit, string flag, double? RBC_WDOG_TID);
        Task<List<AbcBlends>> CheckBlds(double blenderId);
        Task<List<AbcBlends>> GetBlendReturning(double blenderId);
        Task<AbcTags> GetTagNameAndVal(double? tagId);
        Task<double?> GetPrdgrpCycleTime(double prdgrpId);
        Task<List<AbcProperties>> GetEtohPropIds();
        Task<List<AbcTranstxt>> GetTranstxtData(string text);
        Task<AbcTags> GetStrTagNameAndVal(double? tagId);
        Task<DateTime?> GetIntvStartTime(double blendId, int sequence);
        Task<string> GetRbcStName(string val);
        Task<int> SetBlendEndTime(double id);
        Task<DateTime> GetCurTime();
        Task<DateTime> GetLastRunTime(string programeName);
        Task<int> SetPaceActFlag(string flag, double blendId);
        Task<double> GetDestTankId(double blendId);
        Task<List<MatSwingId>> GetMatSwingId(int intPrdgrpID);
        Task<int> CheckBlendsCount(double MatId, double BlenderId);
        Task<List<AbcBlends>> GetBlendState(double blendId);
        Task<List<DCSProdLineupNum>> GetDCSProdLineupNum(double? lineUpId);
        Task<double?> GetHeelVol(double? tankId);
        Task<int> SetHeelVol(double? volume, double blendId, double? tankId);
        Task<int> SetHeelUpdated(double blendId);
        Task<int> SetIgnoreLineCOnstraint(double blendId);
        Task<int> SetBiasOverrideFlag(double blendId);
        Task<List<HdrAnzrsData>> GetHdrAnzrsData(double blenderId);
        Task<int> SetRampingActFlag(double blendId, string value);
        Task<int> SetBlendStartTime(double blendId);
        Task<List<AbcBlendIntervals>> GetBlendIntvs(double blendId);
        Task<int> CopyPrevBias(int sequence1, int sequence2, int sequence3, double blendId, int sequence4);
        Task<string> GetCompName(double MatId);
        Task<string> GetGradeName(double gradeId);
        Task<int> CheckIntv(double blendId, int intervalNum);
        Task<int> AddNewBldIntv(double lngBldID, int intIntvNum, DateTime dteCurTime);
        Task<int> AddNewIntvComps(int intIntvNum, double lngBldID);
        Task<int> AddNewIntvProps(int intIntvNum, double lngBldID);
        Task<int> SetNewIntv(int volume, double lngBldID, int intIntvNum);
        Task<int> SetBiasCalcCurrent(double lngBldID, int intIntvNum);
        Task<int> SetBiasCalcCurrent2(double lngBldID, int intIntvNum);
        Task<int> SetBiasCalcCurrent3(double lngBldID, int intIntvNum);      
        Task<List<string>> GetPrdTankType(double blendId);
        Task<int> SetIntvEndTime(DateTime gDteCurTime, double blendId, double vntIntvNum);
        Task<List<IntComps>> GetIntComps(double blendId, int curIntrvl);
        Task<List<BldCompUsage>> GetBldCompUsage(double lngBlendId, double? sngMatId);
        Task<List<CompTankProps>> GetCompTankProps(double lngBlendId);
        Task<int> SetCompProp(string sourceName, double? value, double blendId, double matId, double tankId, double propId);
        Task<string> GetPropAlias(int intPropID);
        Task<double> GetDfPropVal(int intPrdgrpID, double matId, double propId);
        Task<List<CompVolTids>> GetCompStatVolTids(double blendId);
        Task<List<TotalCompVol>> GetTotalCompVol(double blendId, double blenderId);
        Task<List<TotalStatVol>> GetTotalStatVol(double blendId, double blenderId);
        Task<MxMnValTime> GetMxMnValTime(double blendId, double blenderId);
        Task<List<TotalizerScanTimes>> GetTotalizerScanTimes(double blendId, double blenderId);
        Task<List<CompVolTids>> GetCompVolTids(double blenderId, double blendId);
        Task<string> LogMessage(int msgID, string prgm1, string gnrlText, string prgm2, string prgm3, string prgm4, string prgm5, string prgm6, string prgm7, string res);
    }
}
