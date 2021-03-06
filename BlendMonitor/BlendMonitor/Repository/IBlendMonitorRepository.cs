﻿using BlendMonitor.Entities;
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
        Task<List<AbcBlendStations>> GetAllBldStations(double blendId);
        Task<List<CompIntVols>> CompIntVols(double blendId, int interval);
        Task<CompBldData> CompBldData(double blendId);
        Task<double?> GetBldLineupId(double blendId, double MatId);
        Task<List<BldrStationsData>> GetBldrStationsData(double? lngCompLineupID, double blenderId);
        Task<AbcBlendStations> GetBldStationsData(double blendId, double MatId, double? stationId);
        Task<string> GetFlowDenom(int intPrdgrpID);
        Task<int> UpdateAbcBlendCompWild(double blendId, double matId, string vntTagVal);
        Task<double?> GetAddStationVol(double blendId, double matId, double? curLineupId);
        Task<int> SetStationCurVol(string TagVal, double blendId, double? stationId, double matId);
        Task<int> SetStationPrevVol(string TagVal, double blendId, double? stationId, double matId);
        Task<int> SetIntRcp(double intRcp, double blendId, double matId, int intCurIntv);
        Task<List<PrdgrpVolFactor>> GetPrdgrpVolFactor(int intPrdgrpID, int intProductId, int intAdditiveId);
        Task<List<BlendStationEqp>> GetBlendStationEqp(double? lineUpId, double blendId, double matid);
        Task<int> SetBldStatPar(double dblStationActRcp, double blendId, double? stationId, double matId);
        Task<double?> GetIntVol(double blendId, int intCurIntv);
        Task<int> SetIntVolCost(double? dblIntVol, double dblIntCost, double gdblBldVol, double blendId, int intCurIntv);
        Task<int> SetBldVolCost(double gdblBldVol, double dblBldCost, string vntTagVal, double blendId);
        Task<List<SelTankProps>> GetSelTankProps(double blendId);
        Task<int> SetFeebackPred(double dblFeedbackPred, double blendId, int intCurIntv, int intCompPropID);
        Task<string> GetOptEngine();
        Task<List<string>> GetSBPath();
        Task<List<double>> GetBlendIntProps(double blendId, double PrdgrpId);
        Task<int> CheckPropertyUsed(double blendId, double propId, string strUsedFlag);
        Task<List<AbcBlendIntervalProps>> GetBiasCalData1(double blendId, double propId, int intStartInterval, int intStopInterval);
        Task<List<AbcBlendIntervalProps>> GetBiasCalData2(double blendId, double propId, int intStartInterval, int intStopInterval);
        Task<List<BldSampleProps>> GetBldSampleProps(double blendId, string sampleName);
        Task<List<SampleIntvProps>> GetSampleIntvProps(double blendId, int intMatchIntv, double propID, double prdgrpId);
        Task<List<PropNameModel>> GetPropName(double propId);
        Task<double> GetConvValue(double sngOrigValue, string strFromUnitName, string strToUnitName);
        Task<int> setUnfiltBias(double dblUnfilBias, double blendId, double propId, int vntIntvNum, int intStopInterval, int intMatchingIntv);
        Task<List<double>> GetPrevIntBias(double blendId, int IntvNum, double propID);
        Task<int> SetModelErrExistsFlag(string txt, double blendId, double propId);
        Task<int> SetModelErrClrdFlag(double blendId, double propId);
        Task<int> setBias(double dblIntBias, double blendId, double propId, int vntIntvNum, int intStopInterval, int intMatchingIntv);
        Task<int> setBiasAndUnfiltBias(double dblIntBias, double dblUnfilBias, double blendId, double propId, int vntIntvNum, int intMatchingIntv);
        Task<int> SetUsedFlag(double blendId, double propId, string strSampleName);
        Task<int> SetIntCalcPropertyFlag(double blendId, double propId, int vntIntvNum);
        Task<List<CheckAnzrMeasure>> CheckAnzrMeasure(double blenderId, double propId);
        Task<double> GetBlendInterval(double blendId, DateTime currentDateTime);
        Task<List<AbcBlendIntervalProps>> GetBlendIntervalPropsData(double blendId, double propId, int intCurIntv, int intBlendIntvSeq, double anzID);
        Task<List<string>> CheckBiasCalcAnzFallback(int prdgrpID, double propId);
        Task<int> SetBiasCalcCurrent(double blendId, double propId, int intCurIntv, string biasCalcCurrent);
        Task<List<string>> GetSampleType(double propId, double blendId);
        Task<List<BiasData>> GetBiasData(double blendId, double blenderId, double prdgrpId);
        Task<int> SetPropAnzOffset(double sngAnzOfst, double blendId, double propId);
        Task<int> SetModelErr(double dblIntBiasNew, double blenderId, double vntPropID, double blendId, int vntIntvNum, double vntPropID2);
        Task<int> SetUnFiltBias(double dblUnfilBias, double blendId, int vntIntvNum, double propId);
        Task<int> SetIntvBias(double dblIntBias, double blendId, int vntIntvNum, double vntPropID);
        Task<int> SetTqi(double blendId);
        Task<List<AbcBlendProps>> GetAllBlendProps(double blendId);
        Task<List<AbcBlendIntervalProps>> GetFdbackPred(double blendId);
        Task<int> SetBlendPropsValue(double? vntFdbkPred, double blendId, double propId);
        Task<int> SetBlendSampleProps(string strSampleField, double dblAvgVarValue, double blendId, string strSampleName, double propId);
        Task<List<BldSampleProps>> CompositeSpotSample(double blendId);
        Task<double> GetMatId(string name);
        Task<int> SetProcessSampleFlag(double blendId);
        Task<double> GetAbcBlendIntervalSequence(string strBlendID, DateTime dteStartStopDate);
        Task<double> GetHighLowSequenceVolRange(string strBlendID, string strMinMaxVol);
        Task<PropNameModel> GetPropertyID(string text);
        Task<List<AbcBlendIntervalProps>> GetEtohAnzIntProp(double blendId, int intStartInterval, int intStopInterval, int intEtohId);
        Task<List<PropCalcId>> GetPropCalcId(int prdgrpID, double propID);
        Task<List<AbcBlendIntervalProps>> GetAnzIntProp(double blendId, int num, double propID);
        Task<List<PropNameModel>> GetAbcBlendIntervalPropsdata(string strVarName, double blendId, int num);
        Task<bool> GetNewUsedSample(double blendId, string strSample);
        Task<int> SetWriteStrTagVal(string prdName, double? BlendDescTid);
        Task<List<BldrSrcSlctfbTids>> GetBldrSrcSlctfbTids(double blenderId, double blendId);
        Task<string> GetTankName(double tankId);
        Task<List<AbcTanks>> GetDataTankID(double tankID);
        Task<AbcCompLineups> GetDCSCompLineupNum(double lngLineupID);
        Task<List<AbcBlenderComps>> GetAllBldrComps(double blenderId);
        Task<List<AbcBlenderDest>> GetBldrDestSelTid(double blenderId, int tankId);
        Task<List<AbcBlenders>> GetBldrLineupTags(double blenderId);
        Task<List<AbcBlendDest>> GetTkDestData(double blendId, int tankId);
        Task<List<AbcStations>> GetStationPaceTids(double blendId);
        Task<List<double>> GetBldStationMatId(double blendId, double stationId);
        Task<int> SetBlendCompPacingFactor(int factor, double blendId, double MatId);
        Task<List<DateTime?>> GetLastOptTime(double blendId);
        Task<List<AbcProdLineups>> GetPrdLnupSlctTid(double blendId, int destTank1ID, int bldrID, int destTank2ID);
        Task<List<CompEqpData>> GetCompEqpData(double blendId, int blenderId);
        Task<List<double?>> GetPumpInuseTids(int lnupID);
        Task<List<double?>> GetAllPumpsForPrdgrp(int bldrID);
        Task<List<AbcBlends>> GetAbcBlendData(double blenderId, int prodId);
        Task<List<BldComps>> GetBldComps(double blendId);
        Task<List<BldProps>> GetBldProps(double blendId);
        Task<double> GetSelTankProp(double tankId, int etohEtohPropId);
        Task<List<DenaturantProps>> GetDenaturantProps();
        Task<int> SetEtohBldgReqd(string text, double blendId);
        Task<List<AbcProdLineups>> GetAbcProLinupData();
        Task<List<AbcCompLineups>> GetAbcCompLinupData();
        Task<string> GetTagName(double? tagId);
        Task<List<AbcTanks>> GetTankNum(int tankid);
        Task<List<BlendSwingsData>> BlendSwingsData(string txt, int tankId, double blendId);
        Task<List<ASTankID>> GetASTankID(int tankId);
        Task<string> GetDestTankData(double blendId, int tankId, double vntHeelVol);
        Task<int> SetReadTagVal(int startOkTid);
        Task<List<AbcBlenderComps>> GetBldrCmpData(double blenderId, double blendId);
        Task<List<CompTanksData>> GetCompTanksData(double blendId);
        Task<List<AbcBlenderSources>> GetBldrSrcPreselTID(double blenderId, double blendId, double matId, string text);
        Task<double?> GetBldrSrcSlctTid(double blenderId, double tankId);
        Task<int> SetStationinuseFlg(string text, double lngCompLineupID);
        Task<int> SetIntvRcpSp(double? curRecipe, double blendId, double matId, int value);
        Task<int> SetAbcBlendCompData(double blendId);
        Task<List<string>> GetTankStName(string text);
        Task<AbcStations> GetStationInuseFlgs(double? lineupId);
        Task<(string, double?, double?, double?, double?, string)> GetTankData(int tankId);
        Task<List<DestProps>> GetDestProps(double blendId, double tankId);
        Task<int> SetHeelVal(double? vntPropVal1, double? vntPropVal2, double blendId, double tankId, double vntPropID);
        Task<List<AbcPrograms>> GetPrgCycleTimes();
        Task<double?> GetDestHeelProp(double lngBlendId, int intDestTankID, int intPropID);
        Task<int> GetCalcID(string text);
        Task<List<DestHeelVals>> GetAllDestHeelValsModified(double blendId, int tankId);
        Task<List<DestHeelVals>> GetAllDestHeelValsModified2(double blendId, int tankId);
        Task<int> SetAbcBlendDestPropData(double heelValue, double currentValue, double blendId, int tankId, string propName);
        Task<double> GetPrevBldToTk(int tankID);
        Task<double> GetBldPropCurVal(double bldId, int tankID, int etohEtohPropId);
        Task<double> GetETOHLabLimit();
        Task<DateTime?> GetBlendEndTime(double blendId);
        Task<double> GetSourceId(string sourceName);
        Task<List<NonLinTkPropValsModified>> GetNonLinTkPropValsModified(double bldId, int tankID, int calcSrceId);
        Task<List<NonLinTkPropValsModified>> GetNonLinTkPropValsModified2(double bldId, int tankID, int calcSrceId);
        Task<List<AbcAnzHdrProps>> GetModelErrThrshVals(string bldrName);
        Task<List<AbcPrdgrpProps>> GetMinMaxBiasVals(int prdgrpID);
        Task<string> GetCalcRoutine(double blend, string text);
        Task<List<double>> GetCalcCoeffs(int prdgrpID, string text1, string text2);
        Task<List<AbcPumps>> GetPumpsData(double lineId);
        Task<List<CompSrceData>> GetCompSrceData(double blendId);
        Task<List<AbcStations>> GetAllBldrStationsData(double blenderId);
        Task<List<BlendSourcesTankData>> GetBlendSourcesTankData(double blendId, int matId);
        Task<AbcMaterials> GetMatName(int matID);
        Task<List<LineGeoId>> GetLineGeoIdProduct(double lineupID);
        Task<List<LineGeoId>> GetLineGeoId(double lineupID);
        Task<List<double?>> GetPumpIdProd(double lineupID);
        Task<List<double?>> GetPumpIdComp(double lineupID);
        Task<List<double?>> GetPumpIdProd(double lineupID, int lineEqpOrder);
        Task<List<double?>> GetPumpIdComp(double lineupID, int lineEqpOrder);
        Task<List<double?>> GetPumpIdProd2(double lineupID, int lineEqpOrder);
        Task<List<double?>> GetPumpIdComp2(double lineupID, int lineEqpOrder);
        Task<List<double?>> GetPumpIdProd3(double lineupID, int lineEqpOrder);
        Task<List<double?>> GetPumpIdComp4(double lineupID, int lineEqpOrder);
        Task<List<double?>> GetPumpIdProd4(double lineupID, int lineEqpOrder);
        Task<List<double?>> GetPumpIdComp5(double lineupID, int lineEqpOrder);
        Task<List<double?>> GetPumpIdProd5(double lineupID, int lineEqpOrder);
        Task<List<double?>> GetPumpIdComp3(double lineupID, int lineEqpOrder);
        Task<AbcPumps> GetPumpCfg(double pumpXId);
        Task<List<AbcPumps>> GetProdPumpsData(double prodLineupId);
        Task<List<AbcBlenderDest>> GetBldrDestPreselTID(double blenderId, double blendId, string text);
        Task<int> SetBlendDestSequenceTime(DateTime startTime, double blendId, double tankId, int sequence);
        Task<AbcBlenders> GetBldrSwingOccurID(double blenderId);
        Task<List<AbcTags>> GetReadWriteVal(double swingTID);
        Task<int> SetBlendSwingData(string state, double blendId, double tankdId, double toTankId);
        Task<List<double>> GetBldDestSwgSeq(double blendId);
        Task<double?> GetBldDestSumVolAdded(double blendId);
        Task<int> SetBlendDestSeqData(double dblSeqVolAdded, double blendId, double tankId, int sequence);
        Task<int> SetBlendState(double blendId, string state);
        Task<List<AbcBlends>> GetReadyPrevBld(double blenderId, double blendId);
        Task<int> SetBlendPendingState(double blendId, string state);
        Task<string> BOCopyPkg(double blendId, string name, string copyOk);
        Task<double> GetBlendId(string name);
        Task<List<double>> GetBlendSwingData(int tankId, string blendName);
        Task<int> SetBlendTargetVol(double vol, double blendId);
        Task<int> SetBlendDesOnSpecVol(double vol, double blendId);
        Task<List<double>> GetBlendOrderTankData(string blenderName, string matName, double tankId);
        Task<int> InsertAbcBlendDest(int posDestTankId, double blendId, double lineUpId);
        Task<int> InsertBlendSwingData(double blendId, double tankId, double destTankId, double? swingCriteriaID);
        Task<int> SetBlendSwingData(double swingCriteriaID, double blendId, double tankId, double destTankId);
        Task<bool> TPCreatePkg(double? vntTransLineId, bool strNoError);
        Task<bool> TPUpdateDefvalPkg(double? vntTransLineId, bool strNoError);
        Task<bool> TPCreateLabPkg(double? vntTransLineId, bool strNoError);
        Task<int> DeleteAbcBlendDestProps(double blendId, double tankId);
        Task<int> DeleteAbcBlendDestSeq(double blendId, double tankId);
        Task<int> DeleteAbcBlendDest(double blendId, double tankId);
        Task<int> DeleteAbcBlendDSwings(double blendId, double tankId);
        Task<int> DeleteAbcBlendDSwings2(double blendId);
        Task<int> DeleteAbcBlendDSwings3(double blendId);
        Task<int> SetAbcBlendDestData(double blendId);
        Task<int> SetAbcBlendDestData2(double blendId, double tankId, double dblHeelVol, double lineupId);
        Task<int> SetAbcBlendDestData3(double blendId, double tankId, double dblHeelVol, double lineupId);
        Task<int> InsertAbcBlendDestProps(double blendId, double tankId);
        Task<int> InsertAbcBlendDestSeq(double blendId, double tankId);
        Task<List<double>> GetDefaultLineupIds(double blendId, double tankId);
        Task<List<double>> GetCriteriaId(string name);
        Task<List<AbcPrdPropSpecs>> GetAbcPrdPropSpecs(string prodName, string gradeName, string blenderName, string blendName);
        Task<List<AbcPrdPropSpecs>> GetAbcPrdPropSpecs2(string prodName, string gradeName, string blenderName, string blendName);
        Task<int> InsertAbcBlendPropsData(string blendName, double propId, double? giveawayCost, double? controlMin, double? controlMax, double? salesMin, double? salesMax);
        Task<List<AbcPrdgrpMatProps>> GetPrdgrpMatPropData(string blenderName, string prodName);
        Task<int> SetAbcBlendPropsData(string blendName, double propId, double? giveawayCost, double? controlMin, double? controlMax, double? salesMin, double? salesMax);
        Task<int> SetAbcBlendPropsValidMin(string blendName, double propId, double min);
        Task<int> SetAbcBlendPropsValidMax(string blendName, double propId, double max);
        Task<int> SetAbcBlendProps(string blendName);
        Task<int> SetAbcBlendProps2(string blendName);
        Task<int> SetAbcBlendPropsResTagId(string blendName);
        Task<int> SetAbcBlendPropsAnzOffset(string blendName);
        Task<int> SetAbcBlendPropsCalcAndCost(string blendName, string blenderName);
        Task<List<double>> GetBlendIntervalSequence(double blendId);
        Task<int> SetAbcBlendPropsIntrvBias(string blendName, double sequence, double blendId);
        Task<int> SetAbcBlendCompTankMin(double oldBlendId, double sequence, string blendName);
        Task<int> SetAbcBlendCompTankMax(double oldBlendId, double sequence, string blendName);
        Task<int> SetAbcBlendCompPlanRecipe(double oldBlendId, double sequence, string blendName);
        Task<int> SetAbcBlendBatchTargetVolume(double oldBlendId, double sequence, string blendName);
        Task<int> SetAbcBlendCompRcpConstraintType(double oldBlendId, double sequence, string blendName);
        Task<List<RecipeHdr>> RecipeHdr(double blendId);
        Task<List<RecipeBlend>> RecipeBlend(double blendId);
        Task<int> SetAbcBlendPrevId(double oldBlendId, double blendId);
        Task<int> SetBlendSwingState(double tankId, double blendId, string state);
        Task<int> SetBlendSwingStateAndDoneAt(double tankId, double toTankId, double blendId);
        Task<string> GetPrgRunState(string app);
        Task<int> SetBlendSwingData2(string state, double blendId, double tankdId, double toTankId);
        Task<int> SetBlendSourceSeqData(double blendId, double matId, double tankId, DateTime actualStart);
        Task<int> SetAbcBlendInUseFlag(double blendId, double matId, double tankId, string flag);
        Task<int> SetAbcBlendInUseFlag2(double blendId, double matId, double tankId, string flag);
        Task<int> SetBlendSwingStateAndDoneAt2(double tankId, double toTankId, double blendId);
        Task<int> SetBlendSourceSeqData2(double blendId, double matId, double tankId, int seq, double dblSeqVolUsed);
        Task<List<double?>> GetCompLineup(double blendId, double matId, double tankId);
        Task<AbcBlenderComps> GetBldrCompsSwingOccurID(double blenderId, double blendId, double tankId, double matId);
        Task<List<double>> GetBldSourceSwgSeq(double blendId, double matId);
        Task<AbcBlendComps> GetBldMatVol(double blendId, double matId);
        Task<double?> GetBldSourceSumVolUsed(double blendId, double matId);
        Task<int> InsetBlendSourceSeqData(double blendId, double matId, double tankId, int seq);
        Task<List<double?>> GetCompLineups(double blendId, double matId, double tankId);
        Task<int> InsertBlendStations(double blendId, double matId, double stationid, double stationMax, double stationMin);
        Task<int> InsertBlendStations(double blendId, double matId, double lineupId);
        Task<int> DeleteBlendStations(double blendId, double matId, double stationId);
        Task<int> DeleteBlendStation2(double blendId, double matId);
        Task<List<AbcBlendStations>> GetBlStations(double blendId, double matId);
        Task<int> SetBlendStationsData(double blendId, double matId, double stationId, double? setpoint);
        Task<double> GetCompEqpOrder(double lineupId, double stationId);
        Task<int> SetBlendSwingState2(double tankId, double toTankId, double blendId, string state);
        Task<int> SetBlendSwingStateAndDoneAt3(double tankId, double toTankId, double blendId);
        Task<string> LogMessage(int msgID, string prgm1, string gnrlText, string prgm2, string prgm3, string prgm4, string prgm5, string prgm6, string prgm7, string res);
    }
}
