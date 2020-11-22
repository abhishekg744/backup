using BlendMonitor.Entities;
using BlendMonitor.Model;
using BlendMonitor.Repository;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static BlendMonitor.Constans;
using SharedGAMSInterface;
using ABBAdvancedBlendOptimizerSQL.PostProcess.Repository;

namespace BlendMonitor.Service
{
    public class BlendMonitorService : IBlendMonitorService
    {
        private IBlendMonitorRepository _repository;
        private IConfiguration _configuration;
        CommonGAMSOptimizer commonGAMSOptimizer;
        string connectionString;
        private string programName;
        DateTime[] gDteCompSwgCmdTime;
        DateTime[] gDteProdSwgCmdTime;
        double gdblProjCycleTime;
        double gDblCycleTime;
        // cycle time for Blend Monitor
        string gArBldCmds, gArBldStates;
        PrevBlendData[] gArPrevBldData;
        DateTime[] gArBldFinishTime;
        bool[] gArAnzOfstSvd;
        ValTime[] gArCompValTime;
        ValTime[] gArStnValTime;
        ValTime[] gArSrcTkPrpValTime;
        double gdblBldVol;
        RbcWatchDog[] gArRbcWdog;
        ProjDfData gProjDfs;
        string gstrBldrName;
        DcsTag gTagTotFlow = new DcsTag();
        double gTID1LocVar;
        double gTID2LocVar;
        string gstrRundnFlag;
        bool[] gblnCompSwgTimeIn;
        bool[] gblnProdSwgTimeIn;
        bool[] gblnOptimizing;
        bool[] gblnPrevStatePaused;
        int[] gintNameCount;
        bool[] gblnBmonStarted;
        bool gblnAutoStart;
        string gstrDownloadType;
        bool[] gblnNOProcActBlds;
        bool[] gblnSetOptNowFlag;
        int[] gintSkipCycleBmon;
        DateTime[] gArAnzDelay;
        bool[] gblnMsgLogged;
        bool[] gblnSampleMsgLogged;
        //Set the sample log msg only once
        double[,] gArPrevTargetVol;
        double[,] gArPrevTargetRate;
        double[,] gArPrevTransferVol;
        bool gblnIntFeasible, gblnAvgFeasible;
        bool[,] gblnFirstBiasCalc;
        int[] gintStartStopIntv = new int[1];
        bool[] gblnBiasRedimDone;
        bool gblnLoadedResBld;
        bool gPendingBlendsOnly = false;
        string strWinDateFmt;
        string strOraDateFmt;
        const int LOCALE_SSHORTDATE = 31; //&H1F 
        //   Short date format string
        const string WIN_TIME_FMT = "HH:mm:ss";
        const string ORA_TIME_FMT = "HH24:MI:SS";

        bool gblnEthanolBlend; // Blend is Ethanol blend (used in GAMSinterface modules)
        double? gsngFGEEtoh; //Limiting value for Fuel Grade Ethanol (used in GAMSinterface modules)
        double? gsngMinEtoh;
        string gstrProjName;
        string gstrLIMSSeparateProps;
        int gintEtohEtohPropId;
        int gintEtohPropId;
        CurBlendData curblend = new CurBlendData();
        string[] gArPrevRBCState;
        DateTime gDteCurTime;
        Shared _shared;
        public BlendMonitorService(IBlendMonitorRepository repository, IConfiguration configuration, Shared shared)
        {
            _repository = repository;
            _configuration = configuration;
            _shared = shared;
            programName = _configuration.GetSection("ProgramName").Value.ToUpper();
            commonGAMSOptimizer = new CommonGAMSOptimizer();
            connectionString = _configuration.GetConnectionString("ABC_BlendMonitorDB");
        }

        public async Task<int> testc()
        {
            var res = "";
            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, curblend.strName, "CHECK_COMMAND_VALIDITY",
                    "", "", "", "", res);
            return 0;
        }

        private void NextBlend()
        {
            //'Reset the curblend data to nothing
            curblend.intCurIntv = 0;
            curblend.strState = "";
        }

        // optional  - bool blnWaitTillNext,int intBldrIdx ,double sngSwingExistTid,double sngSwingVolTid
        public async Task<double?> CalcTargVol(double lngBlendId, double? sngTargVol,
            bool blnWaitTillNext = false, int intBldrIdx = 0, double sngSwingExistTid = 0, double sngSwingVolTid = 0)
        {
            double? lngDestTkId = null, lngEndLineFillTankId = null, lngFlushTankId = null;
            double lngProdLineupId, lngTransferLineId, sngTransferLineVol = 0;

            List<AbcBlendDest> DestTkFlags = await _repository.GetDestTkFlags(lngBlendId);
            if (DestTkFlags.Count > 0)
            {
                List<AbcBlendDest> Data = DestTkFlags.Where<AbcBlendDest>(row => row.FlushTkFlag == "YES").ToList<AbcBlendDest>();
                if (Data.Count > 0)
                    lngFlushTankId = Data[0].TankId;

                Data = DestTkFlags.Where<AbcBlendDest>(row => row.InUseFlag == "YES").ToList<AbcBlendDest>();
                if (Data.Count > 0)
                    lngDestTkId = Data[0].TankId;

                if (lngDestTkId != null && lngFlushTankId != null)
                {
                    //'get trasfer line vol from flush tank to destination tank
                    List<AbcBlendSwings> DataRes = await _repository.GetBldSwgTransferVol(lngBlendId, lngFlushTankId, lngDestTkId);

                    if (DataRes.Count > 0)
                    {
                        sngTransferLineVol = (DataRes[0].CriteriaNumLmt) == null ? 0 : Convert.ToDouble(DataRes[0].CriteriaNumLmt);
                    }
                    else
                    {
                        sngTransferLineVol = 0;
                    }
                }

                //'Find the END_LINEFILL_TK_FLAG=YES record
                Data = DestTkFlags.Where<AbcBlendDest>(row => row.EndLinefillTkFlag == "YES").ToList<AbcBlendDest>();
                if (Data.Count > 0)
                    lngEndLineFillTankId = Data[0].TankId;

            }

            // 'Note: lngFlushTankId=NULL is considered equal to blend destination tank
            // 'lngEndLineFillTankId=NULL is considered NOT qual to blend destination tank
            if ((lngFlushTankId == lngDestTkId && lngEndLineFillTankId == lngDestTkId) ||
                (lngFlushTankId == null && lngEndLineFillTankId == lngDestTkId))
            {
                //'Recalculate the batch size for this blend
                sngTargVol = sngTargVol - sngTransferLineVol;
            }
            else if ((lngFlushTankId == lngDestTkId && lngEndLineFillTankId != lngDestTkId) ||
                    (lngFlushTankId != lngDestTkId && lngEndLineFillTankId == lngDestTkId) ||
                (lngFlushTankId == null && lngEndLineFillTankId != lngDestTkId) ||
                (lngFlushTankId == null && lngEndLineFillTankId == null) ||
                (lngFlushTankId == lngDestTkId && lngEndLineFillTankId == null))
            {
                // 'Do not change the batch size for this blend
                sngTargVol = sngTargVol;
            }
            else if (lngFlushTankId != lngDestTkId && lngEndLineFillTankId != lngDestTkId ||
                    (lngFlushTankId != lngDestTkId && lngEndLineFillTankId == null))
            {
                // 'recalculate the batch size for this blend
                sngTargVol = sngTargVol + sngTransferLineVol;
            }


            //'Pass back the validated target volume
            return sngTargVol;

            //'if function is call from WaitTillNextCycle then do this
            //   If blnWaitTillNext = True Then
            //      'if this is a new blend in this blender then reinitialize the arrays
            //      If lngBlendId<> gArPrevTransferVol(intBldrIdx, intBldrIdx +1) Then
            //          'reinitialze the blend_id/target rate/target vol for this blend
            //          gArPrevTransferVol(intBldrIdx, intBldrIdx) = 0
            //          gArPrevTransferVol(intBldrIdx, intBldrIdx + 1) = 0
            //      End If


            //      If sngTransferLineVol<> 0 And sngTransferLineVol<> gArPrevTransferVol(intBldrIdx, intBldrIdx) Then
            //        'Turn ON the swing exist flag in DCS
            //        If sngSwingExistTid<> 0 Then
            //            ABCdataEnv.cmdSetWriteTagVal YES, "YES", sngSwingExistTid
            //        End If
            //        If sngSwingVolTid <> 0 Then
            //            'Download the swing BLEND target volume TO DCS
            //            ABCdataEnv.cmdSetWriteTagVal sngTransferLineVol, "YES", sngSwingVolTid


            //            'Save the previous Blend transfer line volume
            //            gArPrevTransferVol(intBldrIdx, intBldrIdx) = sngTransferLineVol
            //            gArPrevTransferVol(intBldrIdx, intBldrIdx + 1) = lngBlendId
            //        End If
            //      End If
            //   End If
        }

        public async Task<int> ChkRbcWatchDog(int intBldrIdx, double lngBldID, List<AbcBlenders> vntBldrsData, CurBlendData curblend)
        {
            //vntTagVal, vntTagValTime, vntTagValQlt, vntDummy, vntScanRateName As Variant
            int intWDLimit, intUpperLimit, intDCSCommWDRate;
            string vntDummy = "", vntTagValQlt = "", vntScanRateName = "";
            double vntTagVal = 0;
            DateTime? vntTagValTime = new DateTime();


            if (vntBldrsData[intBldrIdx].RbcWdogTid == null)
            {
                //'check DCS->ABC communication
                if (await _shared.ChkDcsComm(curblend.lngID, vntBldrsData[intBldrIdx].Id, gstrBldrName) == GoodBad.BAD)
                {
                    // 'Set the blend state to COMM ERROR
                    curblend.strState = "COMM ERR";

                    if (vntBldrsData[intBldrIdx].CommErrFlag == "NO")
                    {
                        //'update blenders table to hold a bad comm flag
                        await _repository.SetBlenderErrorFlag("YES", gstrBldrName);
                        //'warn msg "^1 NOT RESPONDING ON ^1 (MANAGER/BLENDER)
                        var res = "";
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN61), programName, "BL-" + lngBldID, "RBC", "BLENDER " + gstrBldrName, "", "", "", "", res);
                    }
                }
                else
                {
                    // 'update blenders table to hold a good comm flag
                    await _repository.SetBlenderErrorFlag("YES", gstrBldrName);

                    if (curblend.strState.Trim() == "COMM ERR")
                    {
                        if (vntBldrsData[intBldrIdx].CommErrFlag == "YES")
                        {
                            //'Warn msg "Please reset the COMM ERRor state of the blend ^1"                         
                            var res = "";
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN78), programName, "BL-" + lngBldID, curblend.strName, "", "", "", "", "", res);
                        }

                    }
                }
                //Exit Sub
            }

            intUpperLimit = 300;
            //'GETS THE AMOUNT OF RETRIES IF COMMUNICATION IS LOST & TAKE 5 TIMES LESS THAN THE RETRIES FOR SCANNER
            AbcProjDefaults SwgDefTimeOutData = await _repository.SwgDefTimeOut();
            intWDLimit = Convert.ToInt32((SwgDefTimeOutData.WdogLimit == null ? 10 : SwgDefTimeOutData.WdogLimit) / 2);

            // 'get watch dog tag value, time, and quality from database
            //validate - debug
            var Data = await _repository.GetTagValAndFlags(vntBldrsData[intBldrIdx].RbcWdogTid, vntDummy, vntTagVal, vntTagValTime, vntTagValQlt, vntDummy, vntDummy, vntScanRateName);
            vntDummy = Data.Item1;
            vntTagVal = Data.Item2;
            vntTagValTime = Data.Item3;
            vntTagValQlt = Data.Item4;
            vntScanRateName = Data.Item7;

            //'Get the WD's scan rate value
            intDCSCommWDRate = Convert.ToInt32(await _repository.GetScanRate(vntScanRateName));

            //'WAIT one, two or three times the scan rate to process
            if (gArRbcWdog[intBldrIdx].intRandomNum == 1)
            {
                if (DateAndTime.DateDiff("s", gArRbcWdog[intBldrIdx].dteTagTime, DateAndTime.Now) <= 1 * intDCSCommWDRate)
                {
                    return 0;
                }
            }
            else if (gArRbcWdog[intBldrIdx].intRandomNum == 2)
            {
                if (DateAndTime.DateDiff("s", gArRbcWdog[intBldrIdx].dteTagTime, DateAndTime.Now) <= 2 * intDCSCommWDRate)
                {
                    return 0;
                }
            }
            else if (gArRbcWdog[intBldrIdx].intRandomNum == 3)
            {
                if (DateAndTime.DateDiff("s", gArRbcWdog[intBldrIdx].dteTagTime, DateAndTime.Now) <= 1.5 * intDCSCommWDRate)
                {
                    return 0;
                }
            }

            gArRbcWdog[intBldrIdx].dteTagTime = Convert.ToDateTime(vntTagValTime);

            if (vntTagVal != gArRbcWdog[intBldrIdx].intWDValue && vntTagValQlt == "GOOD")
            {
                gArRbcWdog[intBldrIdx].intCnt = 0;
                gArRbcWdog[intBldrIdx].intWDValue = Convert.ToInt32(vntTagVal);

                //'Set the counter in the ABC = to intUpperLimit
                await _repository.SetWriteTagVal(intUpperLimit, "YES", vntBldrsData[intBldrIdx].RbcWdogTid);

                //' update blenders table to hold a good comm flag
                await _repository.SetBlenderErrorFlag("NO", gstrBldrName);

                if (curblend.strState.Trim() == "COMM ERR")
                {
                    if (vntBldrsData[intBldrIdx].CommErrFlag == "YES")
                    {
                        //'Warn msg "Please reset the COMM ERRor state of the blend ^1"                                         
                        var res = "";
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN78), programName, "BL-" + lngBldID, curblend.strName, "", "", "", "", "", res);
                    }
                }
            }
            else
            {
                gArRbcWdog[intBldrIdx].intCnt = gArRbcWdog[intBldrIdx].intCnt + 1;
                if (gArRbcWdog[intBldrIdx].intCnt == intWDLimit)
                {
                    //'warn msg "^1 NOT RESPONDING ON ^1 (MANAGER/BLENDER)
                    var res = "";
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN61), programName, "BL-" + lngBldID, "RBC", "BLENDER " + gstrBldrName, "", "", "", "", res);
                }

                // 'Set the COMM ERR blend state
                if (gArRbcWdog[intBldrIdx].intCnt >= intWDLimit)
                {
                    // ' Set the blend state to COMM ERROR
                    curblend.strState = "COMM ERR";
                    //' update blenders table to hold a bad comm flag
                    await _repository.SetBlenderErrorFlag("Yes", gstrBldrName);
                }
            }

            //'to skip some cycles for reading the WD value from the DCS
            if (gArRbcWdog[intBldrIdx].intRandomNum == 3)
            {
                gArRbcWdog[intBldrIdx].intRandomNum = 0;
            }
            else
            {
                gArRbcWdog[intBldrIdx].intRandomNum = gArRbcWdog[intBldrIdx].intRandomNum + 1;
            }

            return 0;

        }

        public async Task<RetStatus> SetBlendState(int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend, DebugLevels enumDebugLevel)
        {
            DcsTag tagRBCState = new DcsTag();
            string strDCSState;
            DcsTag tagSwingOccurred = new DcsTag();
            DcsTag tagPermissive = new DcsTag();
            DateTime? dteIntvStartTime;
            DateTime dteTmonTime;
            double? dblTmonCycleTime;
            DcsTag tagReadyToStart = new DcsTag();
            int intRbcStTid;
            int intStartOkTid;
            DcsTag tagRbcMode = new DcsTag();
            // Added for checking the RBC Mode
            string vntTagVal;
            string vntTagName;
            double? sngDCSBlNameTid;
            var res = "";

            RetStatus rtrnData = RetStatus.SUCCESS;

            if (enumDebugLevel == DebugLevels.High)
            {
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, curblend.strName, "SET_BLENDSTATE", "", "", "", "", res);
            }

            //Added to replace the intRbcStTid by vntBldrsData array
            intRbcStTid = Convert.ToInt32(vntBldrsData[intBldrIdx].RbcStateTid);
            intStartOkTid = Convert.ToInt32(vntBldrsData[intBldrIdx].StartOkTid);
            sngDCSBlNameTid = vntBldrsData[intBldrIdx].DcsBlnameFbTid;

            if (sngDCSBlNameTid != null)
            {
                AbcTags DataRes1 = await _repository.GetStrTagNameAndVal(vntBldrsData[intBldrIdx].DcsBlnameFbTid);
                vntTagName = DataRes1.Name;
                vntTagVal = DataRes1.ReadString;

                // 'If the current state is loaded, but DCS Blend Name != to ABC Name then
                //'set a counter and after three retries set LOADED bo to COMM ERR, because DCS is already RUNNING a different Blend

                if ((gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED" || gArPrevBldData[intBldrIdx].strState.Trim() == "ACTIVE" ||
                   gArPrevBldData[intBldrIdx].strState.Trim() == "PAUSED") && curblend.strName != vntTagVal &&
                        (vntTagName != null) || (vntTagVal != null))
                {

                    gintNameCount[intBldrIdx] = gintNameCount[intBldrIdx] + 1;


                    if (gintNameCount[intBldrIdx] >= 3)
                    {
                        curblend.strState = "COMM ERR";
                        //'CURRENT DCS NAME ^1 DOES NOT MATCH ABC NAME ^2 IN ^3.  CURRENT ^2 WAS SET COMM ERR"                        
                        res = "";
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN90), programName, "BL-" + curblend.lngID, vntTagVal, curblend.strName, "BLENDER " + gstrBldrName,
                            "BLEND", "", "", res);

                        gintNameCount[intBldrIdx] = 0;
                        return rtrnData;
                    }
                }
            }

            //  'get start time of interval #1
            dteIntvStartTime = await _repository.GetIntvStartTime(curblend.lngID, 1);


            // 'get RBC state alias from DCS tag
            AbcTags DataRes = await _repository.GetStrTagNameAndVal(intRbcStTid);
            tagRBCState.vntTagName = DataRes.Name;
            tagRBCState.vntTagVal = DataRes.ReadString;

            // 'Save RBC state tag value
            gArPrevRBCState[intBldrIdx] = tagRBCState.vntTagVal;

            if (tagRBCState.vntTagVal == null)
            {
                //'warning msg "Bad RBC state tag"
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN6), programName, "BL-" + curblend.lngID, tagRBCState.vntTagName, gstrBldrName, "", "", "", "", res);
                if (dteIntvStartTime == cdteNull)
                {
                    rtrnData = RetStatus.FAILURE;
                    return rtrnData;
                }
                else
                {
                    strDCSState = "COMM ERR";
                }
            }
            else
            {
                //'get RBC state name from ABC_RBC_STATES
                strDCSState = await _repository.GetRbcStName(tagRBCState.vntTagVal.Trim());
            }

            if (enumDebugLevel == DebugLevels.High)
            {
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG25), programName, cstrDebug, curblend.strName, tagRBCState.vntTagName, strDCSState, "", "", "", res);
            }

            //'check the read_to_start_tid tag
            AbcTags data = await _repository.GetTagNameAndVal(intStartOkTid);
            tagReadyToStart.vntTagName = data.Name;
            tagReadyToStart.vntTagVal = data.ReadValue.ToString();

            //Set the DCS state to Comm error if the ABC Blend state is Comm error
            if (curblend.strState.Trim() == "COMM ERR" || gArPrevBldData[intBldrIdx].strState.Trim() == "COMM ERR")
            {
                strDCSState = "COMM ERR";
            }
            var res1 = "";
            switch (strDCSState.Trim())
            {
                case "PRE-START":
                case "STARTING":
                    if (tagReadyToStart.vntTagVal == null)
                    {
                        //'warn msg "Bad or null read_to_start tag for blender ^1"
                        tagReadyToStart.vntTagVal = ((int)YesNo.NO).ToString();
                    }
                    //To update the BO state from Loaded to ready in case a Loaded BO to DCS
                    //has been started in Auto Mode (Manual)    
                    if (strDCSState.Trim() == "STARTING")
                    {
                        if (vntBldrsData[intBldrIdx].RbcModeTid != null)
                        {
                            //'get RBC mode flag value from ABC_TAGS
                            AbcTags DataRes2 = await _repository.GetTagNameAndVal(vntBldrsData[intBldrIdx].RbcModeTid);
                            tagRbcMode.vntTagName = DataRes2.Name;
                            tagRbcMode.vntTagVal = DataRes2.ReadValue.ToString();
                        }
                        else
                        {
                            tagRbcMode.vntTagName = null;
                            tagRbcMode.vntTagVal = null;
                        }

                        if (((tagRbcMode.vntTagVal == null) ? (int)YesNo.NO : Convert.ToInt32(tagRbcMode.vntTagVal)) == (int)YesNo.NO)
                        {
                            //'set a blend to Done state if it was resume in loaded state but the DCS state is already in
                            //'Starting state in auto mode otherwise set it to Ready state
                            if ((gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED" && curblend.dteActualStart != cdteNull) ||
                               (gArPrevBldData[intBldrIdx].strState.Trim() == "ACTIVE" || gArPrevBldData[intBldrIdx].strState.Trim() == "PAUSED"))
                            {
                                //'set current time to ABC_BLENDS.ACTUAL_END
                                await _repository.SetBlendEndTime(curblend.lngID);
                                curblend.strState = "DONE";
                                gArPrevBldData[intBldrIdx].enumCmd = null;
                                return rtrnData;
                            }
                            else
                            {
                                //'warning msg "DCS MODE TAG ^1 IS NOT REMOTE.  ^2 ORDER ^3 WAS RESET TO READY STATE
                                res1 = "";
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN69), programName, "BL-" + curblend.lngID, tagRbcMode.vntTagName, "BLEND", curblend.strName, "", "", "", res1);
                                //'set the Blend state to Ready
                                curblend.strState = "READY";
                                gArPrevBldData[intBldrIdx].enumCmd = null;
                                return rtrnData;
                            }
                        }
                        //' Check start_ok flag:if it's YES, set blend_state to LOADED
                        //' otherwise, set to previous state
                        if (tagReadyToStart.vntTagVal == ((int)YesNo.YES).ToString())
                        {
                            curblend.strState = "LOADED";
                        }
                        else
                        {
                            curblend.strState = gArPrevBldData[intBldrIdx].strState.Trim();
                        }
                        return rtrnData;
                    }

                    if (strDCSState.Trim() == "PRE-START")
                    {
                        if ((gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED" && curblend.dteActualStart != cdteNull) ||
                           (gArPrevBldData[intBldrIdx].strState.Trim() == "ACTIVE" || gArPrevBldData[intBldrIdx].strState.Trim() == "PAUSED"))
                        {
                            //'set current time to ABC_BLENDS.ACTUAL_END
                            await _repository.SetBlendEndTime(curblend.lngID);
                            curblend.strState = "DONE";
                            gArPrevBldData[intBldrIdx].enumCmd = null;
                            return rtrnData;
                        }


                        if (vntBldrsData[intBldrIdx].RbcModeTid != null)
                        {
                            //'get RBC mode flag value from ABC_TAGS
                            AbcTags DataRes2 = await _repository.GetTagNameAndVal(vntBldrsData[intBldrIdx].RbcModeTid);
                            tagRbcMode.vntTagName = DataRes2.Name;
                            tagRbcMode.vntTagVal = DataRes2.ReadValue.ToString();
                        }
                        else
                        {
                            tagRbcMode.vntTagName = null;
                            tagRbcMode.vntTagVal = null;
                        }


                        // ' Check start_ok flag:if it's YES, set blend_state to LOADED
                        // '  otherwise, set to previous state
                        if (((tagRbcMode.vntTagVal == null) ? (int)YesNo.NO : Convert.ToInt32(tagRbcMode.vntTagVal)) == (int)YesNo.YES)
                        {
                            if (tagReadyToStart.vntTagVal == ((int)YesNo.YES).ToString())
                            {
                                curblend.strState = "LOADED";
                            }
                            else if (gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED" && curblend.dteActualStart == cdteNull)
                            {
                                // 'warning msg "DCS MODE TAG ^1 IS NOT REMOTE.  ^2 ORDER ^3 WAS RESET TO READY STATE                               
                                res1 = "";
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN69), programName, "BL-" + curblend.lngID, tagRbcMode.vntTagName, "BLEND", curblend.strName, "", "", "", res1);
                                curblend.strState = "READY";
                                gArPrevBldData[intBldrIdx].enumCmd = null;
                            }
                            else
                            {
                                curblend.strState = gArPrevBldData[intBldrIdx].strState.Trim();
                            }
                        }
                        else
                        {
                            //' Reset the blend state to READY state when the Start_OK="OFF"
                            //'and the RBC Mode is not Remote
                            if (gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED" && curblend.dteActualStart == cdteNull)
                            {
                                //'warning msg "DCS MODE TAG ^1 IS NOT REMOTE.  ^2 ORDER ^3 WAS RESET TO READY STATE                                
                                res1 = "";
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN69), programName, "BL-" + curblend.lngID, tagRbcMode.vntTagName, "BLEND", curblend.strName, "", "", "", res1);
                                curblend.strState = "READY";
                                gArPrevBldData[intBldrIdx].enumCmd = null;
                            }
                            else
                            {
                                curblend.strState = gArPrevBldData[intBldrIdx].strState.Trim();
                            }
                        }
                    }

                    break;
                case "RUNNING":
                case "STOPPING":

                    //set blend state to Paused when prevoius state was paused
                    //' and current DCS state is stopping
                    if (strDCSState.Trim() == "STOPPING" && gArPrevBldData[intBldrIdx].strState.Trim() == "PAUSED")
                    {
                        curblend.strState = "PAUSED";
                        return rtrnData;
                    }

                    //Checking of Swing Ocurred tag in the DCS
                    //'get RBC swing Occurred tag value from ABC_TAGS        
                    if (gstrRundnFlag == "YES")
                    {
                        if (gArPrevBldData[intBldrIdx].strState.Trim() == "READY")
                        {
                            if (((tagReadyToStart.vntTagVal == null) ? (int)YesNo.NO : Convert.ToInt32(tagReadyToStart.vntTagVal)) == (int)YesNo.YES)
                            {
                                curblend.strState = "LOADED";
                                //'Set a global flag to wake up the blend monitor when
                                //'the auto start is going on
                                gblnAutoStart = true;
                                return rtrnData;
                            }
                            else
                            {
                                curblend.strState = gArPrevBldData[intBldrIdx].strState.Trim();
                                return rtrnData;
                            }
                        }
                        else if (gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED")
                        {
                            if (curblend.vntPendSt.Trim() == "DOWNLOADING")
                            {
                                curblend.vntPendSt = null;
                                gArPrevBldData[intBldrIdx].enumCmd = null;
                            }
                            curblend.strState = "ACTIVE";
                            //'Reset the flag of auto start to OFF
                            gblnAutoStart = false;
                            return rtrnData;
                        }
                    }

                    //Check For RBC mode flag value if Optimize_flag is ON
                    if (vntBldrsData[intBldrIdx].OptimizeFlag == "YES")
                    {
                        if (vntBldrsData[intBldrIdx].RbcModeTid != null)
                        {
                            //'get RBC mode flag value from ABC_TAGS
                            AbcTags DataRes3 = await _repository.GetTagNameAndVal(vntBldrsData[intBldrIdx].RbcModeTid);
                            tagRbcMode.vntTagName = DataRes3.Name;
                            tagRbcMode.vntTagVal = DataRes3.ReadValue.ToString();
                        }
                        else
                        {
                            tagRbcMode.vntTagName = null;
                            tagRbcMode.vntTagVal = null;
                        }

                        if (enumDebugLevel == DebugLevels.High)
                        {
                            res1 = "";
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG3), programName, cstrDebug, curblend.strState, tagRbcMode.vntTagVal, "", "", "", "", res1);
                        }

                        AbcTags DataRes2 = await _repository.GetTagNameAndVal(vntBldrsData[intBldrIdx].DownloadOkTid);
                        tagPermissive.vntTagName = DataRes2.Name;
                        tagPermissive.vntTagVal = DataRes2.ReadValue.ToString();

                        if (((tagPermissive.vntTagVal == null) ? (int)OnOff.OFF : Convert.ToInt32(tagPermissive.vntTagVal)) == (int)OnOff.ON_)
                        {
                            if (((tagRbcMode.vntTagVal == null) ? (int)YesNo.NO : Convert.ToInt32(tagRbcMode.vntTagVal)) == (int)YesNo.NO &&
                                gblnMsgLogged[intBldrIdx] == false)
                            {
                                // 'warning msg "ABC -> DCS download not permitted"                               
                                res1 = "";
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN10), programName, "BL-" + curblend.lngID, tagRbcMode.vntTagName, gstrBldrName, "", "", "", "", res1);

                                gblnMsgLogged[intBldrIdx] = true;
                            }
                            else if (((tagRbcMode.vntTagVal == null) ? (int)YesNo.NO : Convert.ToInt32(tagRbcMode.vntTagVal)) == (int)YesNo.YES)
                            {
                                gblnMsgLogged[intBldrIdx] = false;
                            }
                        }
                    }
                    curblend.strState = "ACTIVE";
                    break;
                case "PAUSED":
                case "RESUMING":

                    //set blend state to Paused when prevoius state was paused
                    //' and current DCS state is stopping
                    if (curblend.dteActualStart != cdteNull)
                    {
                        curblend.strState = "PAUSED";
                        return rtrnData;
                    }
                    else if (gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED")
                    {
                        curblend.strState = "LOADED";
                        return rtrnData;
                    }

                    //To update the BO state from Loaded to ready in case a Loaded BO to DCS
                    //has been started in Auto Mode (Manual)

                    if (tagReadyToStart.vntTagVal == null)
                    {
                        //'warn msg "Bad or null read_to_start tag for blender ^1"
                        tagReadyToStart.vntTagVal = ((int)YesNo.NO).ToString();
                    }

                    if (strDCSState.Trim() == "RESUMING")
                    {
                        if (vntBldrsData[intBldrIdx].RbcModeTid != null)
                        {
                            //'get RBC mode flag value from ABC_TAGS
                            AbcTags DataRes3 = await _repository.GetTagNameAndVal(vntBldrsData[intBldrIdx].RbcModeTid);
                            tagRbcMode.vntTagName = DataRes3.Name;
                            tagRbcMode.vntTagVal = DataRes3.ReadValue.ToString();
                        }
                        else
                        {
                            tagRbcMode.vntTagName = null;
                            tagRbcMode.vntTagVal = null;
                        }

                        if (((tagRbcMode.vntTagVal == null) ? (int)YesNo.NO : Convert.ToInt32(tagRbcMode.vntTagVal)) == (int)YesNo.NO)
                        {
                            //  'set a blend to Done state if it was resume in loaded state but the DCS state is already in
                            // 'Starting state in auto mode otherwise set it to Ready state
                            if ((gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED" && curblend.dteActualStart != cdteNull) ||
                              (gArPrevBldData[intBldrIdx].strState.Trim() == "ACTIVE" || gArPrevBldData[intBldrIdx].strState.Trim() == "PAUSED"))
                            {
                                // 'set current time to ABC_BLENDS.ACTUAL_END
                                await _repository.SetBlendEndTime(curblend.lngID);
                                curblend.strState = "DONE";
                                gArPrevBldData[intBldrIdx].enumCmd = null;
                                return rtrnData;
                            }
                            else
                            {
                                //   'warning msg "DCS MODE TAG ^1 IS NOT REMOTE.  ^2 ORDER ^3 WAS RESET TO READY STATE                                
                                res1 = "";
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN69), programName, "BL-" + curblend.lngID, tagRbcMode.vntTagName, "BLEND", curblend.strName, "", "", "", res1);

                                //  'set the Blend state to Ready
                                curblend.strState = "READY";
                                gArPrevBldData[intBldrIdx].enumCmd = null;
                                return rtrnData;
                            }
                        }
                        //' Check start_ok flag:if it's YES, set blend_state to LOADED
                        // ' otherwise, set to previous state

                        if (((tagRbcMode.vntTagVal == null) ? (int)YesNo.NO : Convert.ToInt32(tagRbcMode.vntTagVal)) == (int)YesNo.YES)
                        {
                            if (tagReadyToStart.vntTagVal == ((int)YesNo.YES).ToString())
                            {
                                curblend.strState = "LOADED";
                            }
                            else if (gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED" && curblend.dteActualStart == cdteNull)
                            {
                                //'warning msg "DCS MODE TAG ^1 IS NOT REMOTE.  ^2 ORDER ^3 WAS RESET TO READY STATE                                
                                res1 = "";
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN69), programName, "BL-" + curblend.lngID, tagRbcMode.vntTagName, "BLEND", curblend.strName, "", "", "", res1);

                                curblend.strState = "READY";
                                gArPrevBldData[intBldrIdx].enumCmd = null;
                            }
                            else
                            {
                                curblend.strState = gArPrevBldData[intBldrIdx].strState.Trim();
                            }
                        }
                        else
                        {
                            curblend.strState = gArPrevBldData[intBldrIdx].strState.Trim();
                        }
                        return rtrnData;
                    }

                    if (strDCSState.Trim() == "PAUSED")
                    {
                        if ((gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED" && curblend.dteActualStart != cdteNull) ||
                              (gArPrevBldData[intBldrIdx].strState.Trim() == "ACTIVE" || gArPrevBldData[intBldrIdx].strState.Trim() == "PAUSED"))
                        {
                            // 'set current time to ABC_BLENDS.ACTUAL_END
                            await _repository.SetBlendEndTime(curblend.lngID);
                            curblend.strState = "DONE";
                            gArPrevBldData[intBldrIdx].enumCmd = null;
                            return rtrnData;
                        }

                        if (vntBldrsData[intBldrIdx].RbcModeTid != null)
                        {
                            //'get RBC mode flag value from ABC_TAGS
                            AbcTags DataRes3 = await _repository.GetTagNameAndVal(vntBldrsData[intBldrIdx].RbcModeTid);
                            tagRbcMode.vntTagName = DataRes3.Name;
                            tagRbcMode.vntTagVal = DataRes3.ReadValue.ToString();
                        }
                        else
                        {
                            tagRbcMode.vntTagName = null;
                            tagRbcMode.vntTagVal = null;
                        }

                        if (((tagRbcMode.vntTagVal == null) ? (int)YesNo.NO : Convert.ToInt32(tagRbcMode.vntTagVal)) == (int)YesNo.YES)
                        {
                            if (tagReadyToStart.vntTagVal == ((int)YesNo.YES).ToString())
                            {
                                curblend.strState = "LOADED";
                            }
                            else if (gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED" && curblend.dteActualStart == cdteNull)
                            {
                                //'warning msg "DCS MODE TAG ^1 IS NOT REMOTE.  ^2 ORDER ^3 WAS RESET TO READY STATE                                
                                res1 = "";
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN69), programName, "BL-" + curblend.lngID, tagRbcMode.vntTagName, "BLEND", curblend.strName, "", "", "", res);

                                curblend.strState = "READY";
                                gArPrevBldData[intBldrIdx].enumCmd = null;
                            }
                            else
                            {
                                curblend.strState = gArPrevBldData[intBldrIdx].strState.Trim();
                            }
                        }
                        else
                        {
                            //Reset the blend state to READY state when the Start_OK="OFF"
                            //'and the RBC Mode is not Remote
                            if (gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED" && curblend.dteActualStart == cdteNull)
                            {
                                //'warning msg "DCS MODE TAG ^1 IS NOT REMOTE.  ^2 ORDER ^3 WAS RESET TO READY STATE                                
                                res1 = "";
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN69), programName, "BL-" + curblend.lngID, tagRbcMode.vntTagName, "BLEND", curblend.strName, "", "", "", res);

                                curblend.strState = "READY";
                                gArPrevBldData[intBldrIdx].enumCmd = null;
                            }
                            else
                            {
                                curblend.strState = gArPrevBldData[intBldrIdx].strState.Trim();
                            }
                        }
                    }
                    break;

                case "COMPLETE":

                    if (gArPrevBldData[intBldrIdx].strState.Trim() == "DONE")
                    {
                        curblend.strState = "DONE";
                        return rtrnData;
                    }
                    if (gstrRundnFlag == "YES")
                    {
                        if (gArPrevBldData[intBldrIdx].strState.Trim() == "READY")
                        {
                            if (((tagReadyToStart.vntTagVal == null) ? (int)YesNo.NO : Convert.ToInt32(tagReadyToStart.vntTagVal)) == (int)YesNo.YES)
                            {
                                curblend.strState = "LOADED";
                                return rtrnData;
                            }
                            else
                            {
                                curblend.strState = "READY";
                                return rtrnData;
                            }
                        }
                        else if (gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED")
                        {
                            curblend.strState = gArPrevBldData[intBldrIdx].strState.Trim();
                            return rtrnData;
                        }
                        else if (gArPrevBldData[intBldrIdx].strState.Trim() == "ACTIVE")
                        {
                            //set current time to ABC_BLENDS.ACTUAL_END
                            await _repository.SetBlendEndTime(curblend.lngID);
                            curblend.strState = "DONE";
                            return rtrnData;
                        }
                    }


                    if (gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED")
                    {
                        //set current time to ABC_BLENDS.ACTUAL_END
                        await _repository.SetBlendEndTime(curblend.lngID);
                        curblend.strState = "DONE";
                        return rtrnData;
                    }

                    if (gArBldFinishTime[intBldrIdx] == cdteNull)
                    {
                        curblend.strState = "ACTIVE";
                        gArBldFinishTime[intBldrIdx] = await _repository.GetCurTime();
                    }
                    else
                    {
                        //set current time to ABC_BLENDS.ACTUAL_END
                        await _repository.SetBlendEndTime(curblend.lngID);

                        //Skip the waiting time for the Tmon if the LineProp<>"YES"
                        if (vntBldrsData[intBldrIdx].CalcpropFlag == "YES")
                        {
                            //get last_run_time of Tank Monitor
                            dteTmonTime = await _repository.GetLastRunTime("ABC TANK MONITOR");

                            //if Tank Monitor has finished last cycle, close the blend
                            //else, if the waiting time has exceeded twice the cycle time of
                            //Tank Monitor, then issue warning msg and close the blend
                            //otherwise just exit sub
                            if (gArBldFinishTime[intBldrIdx] > dteTmonTime)
                            {
                                //get current time
                                gDteCurTime = await _repository.GetCurTime();
                                //get cycle time of Tank Monitor
                                dblTmonCycleTime = _repository.GetCycleTime("ABC TANK MONITOR");

                                if (DateAndTime.DateDiff("s", gArBldFinishTime[intBldrIdx], gDteCurTime) < 2 * dblTmonCycleTime * 60)
                                {
                                    curblend.strState = "ACTIVE";
                                    // Set the flag to skip the Process calc of active blends when the
                                    //Tmon is finishing the TQI and the DCS state is Complete
                                    gblnNOProcActBlds[intBldrIdx] = true;
                                    return rtrnData;
                                }


                                //warning msg "Tank Monitor may be inactive"                               
                                res1 = "";
                                await _repository.LogMessage(Convert.ToInt32(CommonMsgTmpIDs.COM_W1), programName, "BL-" + curblend.lngID, "ABC TANK MONITOR", "", "", "", "", "", res);

                            }
                        }
                        //close blend
                        curblend.strState = "DONE";

                    }
                    break;
                case "COMM ERR":
                    curblend.strState = "COMM ERR";
                    break;
                default:
                    curblend.strState = gArPrevBldData[intBldrIdx].strState.Trim();
                    //'warning msg "Bad RBC state tag"        
                    res1 = "";
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN6), programName, "BL-" + curblend.lngID, tagRBCState.vntTagName, gstrBldrName, "", "", "", "", res);
                    rtrnData = RetStatus.FAILURE;
                    break;
            }

            if (strDCSState.Trim() != "COMPLETE")
            {
                await _repository.SetPaceActFlag("NO", curblend.lngID);
            }

            if (enumDebugLevel == DebugLevels.High)
            {
                res1 = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG26), programName, cstrDebug, curblend.strName, curblend.strState, "", "", "", "", res);
            }

            return rtrnData;


        }

        public async Task<ValidInvalid> ChkCmdValidity(int intBldrIdx, CurBlendData curblend, DebugLevels enumDebugLevel)
        {
            bool blnRdndntSt = false;
            ValidInvalid rtrnData = ValidInvalid.invalid;
            blnRdndntSt = false;
            var res = "";
            if (enumDebugLevel == DebugLevels.High) {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, curblend.strName, "CHECK_COMMAND_VALIDITY",
                    "", "", "", "", res);
            }

            switch (curblend.strState)
            {
                case "READY":
                    switch (curblend.vntPendSt)
                    {
                        case "DOWNLOADING":
                        case "OPTIMIZING":
                        case "RETURNING":
                            rtrnData = ValidInvalid.valid;
                            break;
                    }
                    break;
                case "LOADED":
                    switch (curblend.vntPendSt)
                    {
                        case "STARTING":
                        case "STOPPING":
                        case "OPTIMIZING":
                        case "RETURNING":
                            rtrnData = ValidInvalid.valid;
                            break;
                        case "DOWNLOADING":
                            if (gArPrevBldData[intBldrIdx].enumCmd != null)
                            { //NULL_ 
                                if (gArPrevBldData[intBldrIdx].enumCmd == BlendCmds.DOWNLOAD)
                                {
                                    rtrnData = ValidInvalid.valid;
                                }
                                else
                                {
                                    blnRdndntSt = true;
                                }
                            }
                            else
                            {
                                rtrnData = ValidInvalid.valid;
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case "PAUSED":
                    switch (curblend.vntPendSt)
                    {
                        case "STARTING":
                        case "STOPPING":
                        case "OPTIMIZING":
                        case "RETURNING":
                            rtrnData = ValidInvalid.valid;
                            break;
                        case "SWINGING":
                            if (gArPrevBldData[intBldrIdx].enumCmd != null)
                            { //NULL_ 
                                if (gArPrevBldData[intBldrIdx].enumCmd == BlendCmds.PAUSE)
                                {
                                    rtrnData = ValidInvalid.valid;
                                }
                                else
                                {
                                    blnRdndntSt = true;
                                }
                            }
                            else
                            {
                                rtrnData = ValidInvalid.valid;
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case "ACTIVE":
                    switch (curblend.vntPendSt)
                    {
                        case "STOPPING":
                        case "PAUSING":
                        case "OPTIMIZING":
                        case "SWINGING":
                            rtrnData = ValidInvalid.valid;
                            break;
                        case "STARTING":
                            if (gArPrevBldData[intBldrIdx].enumCmd != null)
                            { //NULL_ 
                                if (gArPrevBldData[intBldrIdx].enumCmd == BlendCmds.START)
                                {
                                    rtrnData = ValidInvalid.valid;
                                }
                                else
                                {
                                    blnRdndntSt = true;
                                }
                            }
                            else
                            {
                                rtrnData = ValidInvalid.valid;
                            }
                            break;
                        case "RESTARTING":
                            if (gArPrevBldData[intBldrIdx].enumCmd != null)
                            { //NULL_ 
                                if (gArPrevBldData[intBldrIdx].enumCmd == BlendCmds.RESTART)
                                {
                                    rtrnData = ValidInvalid.valid;
                                }
                                else
                                {
                                    blnRdndntSt = true;
                                }
                            }
                            else
                            {
                                rtrnData = ValidInvalid.valid;
                            }
                            break;
                        //case "SWINGING":
                        //    if (gArPrevBldData[intBldrIdx].enumCmd != null)
                        //    { //NULL_                                
                        //        rtrnData = ValidInvalid.valid;                              
                        //    }
                        //    else
                        //    {
                        //        rtrnData = ValidInvalid.valid;
                        //    }
                        //    break;
                        default:
                            break;
                    }
                    break;
                case "DONE":
                    if (curblend.vntPendSt == "STOPPING")
                    {
                        if (gArPrevBldData[intBldrIdx].enumCmd == BlendCmds.STOP_)
                        {
                            rtrnData = ValidInvalid.valid;
                        }
                        else
                        {
                            blnRdndntSt = true;
                        }
                    }
                    else
                    {
                        rtrnData = ValidInvalid.valid;
                    }
                    break;
                default:
                    break;
            }

            if (blnRdndntSt)
            {
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN8), programName, "BL-" + curblend.lngID, curblend.vntPendSt, curblend.strState,
                    "", "", "", "", res);
            } else if (rtrnData == ValidInvalid.invalid)
            {
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN7), programName, "BL-" + curblend.lngID, curblend.vntPendSt, curblend.strState,
                    "", "", "", "", res);
            }
            return rtrnData;
        }

        public async Task<int> SetSwingTIDOFF(int intPrdgrpID, double? lngBldrSwgTID, int intBlenderID)
        {
            List<MatSwingId> vntBlendersComps = new List<MatSwingId>();
            int intI;
            int intNRecords;
            int count = 0;
            // For COMPONENT swings
            vntBlendersComps = await _repository.GetMatSwingId(intPrdgrpID);
            intNRecords = vntBlendersComps.Count();

            // loop of all blender comps to find if they are being used by an ACTIVE blend
            for (intI = 0; (intI <= (intNRecords - 1)); intI++)
            {
                count = await _repository.CheckBlendsCount(vntBlendersComps[intI].MatId, intBlenderID);
                // If there is no active blend with an active swing using this blender comp then set swing Tid=OFF
                if (count == 0 && vntBlendersComps[intI].SwingTId != null)
                {
                    // reset Write value=0 for the swing tid of this blender comp.  There is no need to send this cmd
                    // to DCS, so the write_now_flag ="NO"
                    await _repository.SetWriteTagVal((int)OnOff.OFF, "NO", vntBlendersComps[intI].SwingTId);
                }

            }

            // The product tanks are not shared between blenders (off course), so set the flag to OFF in any case
            // for the current starting (LOADED) or finishing (DONE) blends (blender).  It is assume in the entire
            // design that only one blend at the time can be in ACTIVE state in a single blender
            // reset Write value=0 for the swing tid of this blender. There is no need to send this cmd
            // to DCS, so the write_now_flag ="NO"
            if ((lngBldrSwgTID != null))
            {
                await _repository.SetWriteTagVal((int)OnOff.OFF, "NO", lngBldrSwgTID);
            }
            return 0;
        }

        // *********** MonitorBlend ***********

        private async Task<int> ChkIntervals(int intBldrIdx, CurBlendData curblend, DebugLevels enumDebugLevel,bool blnCloseIntv = false)
        {
            double vntIntvNum;
            DateTime? vntStartTime;
            double dblPrevIntVol;
            var res = "";
            if(enumDebugLevel == DebugLevels.High)
            {                
               res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, curblend.strName, "CHECK_INTERVALS",
                    "", "", "", "", res);
            }

            gDteCurTime = await _repository.GetCurTime();
            List<AbcBlendIntervals> BlendIntvsList = await _repository.GetBlendIntvs(curblend.lngID);
            if(BlendIntvsList.Count() < 1)
            {
                //check for and create if needed intv 0 and intv 1
                await _shared.CheckNewIntvRecs(curblend.lngID, 0, enumDebugLevel, gDteCurTime);
                await _shared.CheckNewIntvRecs(curblend.lngID, 1, enumDebugLevel, gDteCurTime);
                BlendIntvsList = await _repository.GetBlendIntvs(curblend.lngID);
            }

            vntIntvNum = BlendIntvsList[0].Sequence;
            vntStartTime = BlendIntvsList[0].Starttime;
            if (vntStartTime == null)
            {
                //  set start time and volume for interval #1
                vntIntvNum = await _repository.SetNewIntv(0, curblend.lngID, Convert.ToInt32(vntIntvNum));
                //  save current interval #
                curblend.intCurIntv = Convert.ToInt32(vntIntvNum);
            }
            else
            {
                // Commented to move to the last interval instead of looping through all the records until the last one
                //       Do Until IsNull(vntStopTime.Value)                
                //       Loop
                //  override the start_time for interval 1.  This way interval
                // will not be anymore a dummy interval.  It will have its own volumes
                if (vntIntvNum == 1 && gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED" && curblend.strState.Trim() == "ACTIVE")
                {
                    //  set start time and volume for interval #1
                    vntIntvNum = await _repository.SetNewIntv(0, curblend.lngID, Convert.ToInt32(vntIntvNum));
                    //  save current interval #
                    curblend.intCurIntv = Convert.ToInt32(vntIntvNum);
                }
                else
                {                   
                    if (DateAndTime.DateDiff("s", vntStartTime.Value, gDteCurTime) >= (60 * curblend.vntIntvLen))
                    {
                        // save current time into stop time of last interval
                        await _repository.SetIntvEndTime(gDteCurTime, curblend.lngID, vntIntvNum);
                        curblend.intCurIntv = Convert.ToInt32(vntIntvNum + 1);
                        //LINEPROP at the middle of the interval has been suppressed.
                        // Reset the flag to procces lineprop calc only two times and no more
                        //          gIntProcLineProp[intBldrIdx] = 1
                        // create records for new interval in ABC_BLEND_INTERVALS,
                        // ABC_BLEND_INTERVAL_COMPS and ABC_BLEND_INTERVAL_PROPS
                        // ERIK *** use CheckNewIntvRecs in BLEND_MON
                        // CreateNewIntvRecs curBlend.lngID, curBlend.intCurIntv, enumDebugLevel
                        await _shared.CheckNewIntvRecs(curblend.lngID, curblend.intCurIntv, enumDebugLevel, gDteCurTime);                       
                    }
                    else
                    {
                        curblend.intCurIntv = Convert.ToInt32(vntIntvNum);
                    }

                    gArPrevBldData[intBldrIdx].intCurIntv = Convert.ToInt32(vntIntvNum);
                    if (enumDebugLevel >= DebugLevels.Medium)
                    {                       
                        res = "";
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG17), programName, cstrDebug, curblend.strName, curblend.vntIntvLen.ToString(),
                            curblend.intCurIntv.ToString(), "", "", "", res);
                    }                   
                }
                // if interval is =1
            }
            // if start time is null
            return 0;
        }        

        private async Task<string> GetBldMatUsage(double lngBlendId, double? sngMatId)
        {
            string rtnData = "";
            List<BldCompUsage> BldCompUsageData =  await _repository.GetBldCompUsage(lngBlendId, sngMatId);            
            if (BldCompUsageData.Count() > 0)
            {
                rtnData = BldCompUsageData[0].UsageName;
            }
            else
            {
                rtnData = "NULL";
            }
            return rtnData;           
        }

        private async Task<int> ChkDcsRcp(int intBldrIdx, double lngBldID, string strBldName, DebugLevels enumDebugLevel)
        {
            List<IntComps> vntIntComps;
            int intNIntComps;
            int intI;
            double dblTotVol;
            double dblIntActRcp;
            string strUsageName;
            var res = "";
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            if ((enumDebugLevel == DebugLevels.High))
            {               
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, strBldName, "CHECK_DCS_RECIPE",
                    "", "", "", "", res);
            }

            vntIntComps = await _repository.GetIntComps(lngBldID, gArPrevBldData[intBldrIdx].intCurIntv);
                        
            intNIntComps = vntIntComps.Count();
            if ((intNIntComps > 0))
            {                             
                dblTotVol = 0;
                for (intI = 0; intI <= (intNIntComps - 1); intI++)
                {
                    // get the Usage Name for the given blend Component
                    // RW 21-Jan-15
                    // strUsageName = GetBldMatUsage(lngBldID, vntIntComps(0, intI))
                    strUsageName = await GetBldMatUsage(lngBldID, vntIntComps[intI].Id);
                    // RW 21-Jan-15
                    if ((strUsageName != "ADDITIVE"))
                    {
                        dblTotVol = (dblTotVol + ((vntIntComps[intI].Volume == null)? 0: Convert.ToDouble(vntIntComps[intI].Volume)));
                    }

                }

                if ((dblTotVol < cDblEp))
                {
                    return 0;
                }

                for (intI = 0; intI <= (intNIntComps - 1); intI++)
                {
                    // get the Usage Name for the given blend Component
                    // RW 21-Jan-15
                    // strUsageName = GetBldMatUsage(lngBldID, vntIntComps(0, intI))
                    strUsageName = await GetBldMatUsage(lngBldID, vntIntComps[intI].Id);
                    // RW 21-Jan-15
                    if ((strUsageName != "ADDITIVE"))
                    {   
                        dblIntActRcp = (((vntIntComps[intI].Volume == null)?0: Convert.ToDouble(vntIntComps[intI].Volume))
                                    / (dblTotVol * 100));

                        if ((dblIntActRcp > (vntIntComps[intI].SpRecipe + gProjDfs.vntRcpTolr)) || (dblIntActRcp < (vntIntComps[intI].SpRecipe - gProjDfs.vntRcpTolr)))
                        {
                            // warning msg "Interval actual rcp ^1 for comp ^2 did not match set point ^3"
                            res = "";
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN52), programName, "BL-" + lngBldID, dblIntActRcp.ToString(), vntIntComps[intI].Name,
                                vntIntComps[intI].SpRecipe.ToString(), gArPrevBldData[intBldrIdx].intCurIntv.ToString(), "", "", res);                            
                        }
                    }
                }
            }
            return 0;
        }

        private async Task<int> UpdatePropTable(int intBldrIdx, int intPrdgrpID, double lngBldID, string strBldName, DebugLevels enumDebugLevel)
        {
            List<CompTankProps> vntSrcTkPrps;
            int intNCompProps;
            int intI;
            double dblDfPropVal;
            string strCompName;
            string strPropName;
            var res = "";
            if(enumDebugLevel == DebugLevels.High)
            {
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, strBldName, "UPDATE_PROP_TABLE",
                    "", "", "", "", res);
            }
            //'get all comp tank props for the blend
            vntSrcTkPrps =  await _repository.GetCompTankProps(lngBldID);
            intNCompProps = vntSrcTkPrps.Count();
            if ((intNCompProps > 0))
            {
                if (!gArSrcTkPrpValTime[intBldrIdx].blnArraySet)
                {
                    gArSrcTkPrpValTime[intBldrIdx].arValueTime = new DateTime[intNCompProps];
                }

                // copy comp prop values to ABC_BLEND_COMP_PROPS if new data available
                for (intI = 0; intI <= (intNCompProps - 1); intI++)
                {
                    if ((vntSrcTkPrps[intI].ValueTime > gArSrcTkPrpValTime[intBldrIdx].arValueTime[intI]))
                    {
                        if ((vntSrcTkPrps[intI].GoodFlag == "YES"))
                        {
                            await _repository.SetCompProp(vntSrcTkPrps[intI].SourceName, vntSrcTkPrps[intI].Value, lngBldID,
                            vntSrcTkPrps[intI].MatId, vntSrcTkPrps[intI].TankId, vntSrcTkPrps[intI].PropId);

                            gArSrcTkPrpValTime[intBldrIdx].arValueTime[intI] = Convert.ToDateTime(vntSrcTkPrps[intI].ValueTime);
                        }
                        else if (!gArSrcTkPrpValTime[intBldrIdx].blnArraySet)
                        {
                            // get comp and prop name
                            strCompName = await _repository.GetCompName(vntSrcTkPrps[intI].MatId);
                            strPropName = await _repository.GetPropAlias(Convert.ToInt32(vntSrcTkPrps[intI].PropId));

                            // warn msg "Selected_flag or good_flag is NO for prop ^1"                           
                            res = "";
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN53), programName, "BL-" + lngBldID, strCompName, strPropName,
                                "", "", "", "", res);

                            // copy default value from ABC_PRDGRP_MAT_PROPS
                            dblDfPropVal = await _repository.GetDfPropVal(intPrdgrpID, vntSrcTkPrps[intI].MatId, vntSrcTkPrps[intI].PropId);
                            await _repository.SetCompProp(vntSrcTkPrps[intI].SourceName, dblDfPropVal, lngBldID, vntSrcTkPrps[intI].MatId,
                            vntSrcTkPrps[intI].TankId, vntSrcTkPrps[intI].PropId);
                        }
                    }
                }

                if (!gArSrcTkPrpValTime[intBldrIdx].blnArraySet)
                {
                    gArSrcTkPrpValTime[intBldrIdx].blnArraySet = true;
                }                
            }
            return 0;
        }
        private async Task<double> ChkStnVol(int intBldrIdx, double intPrdgrpID, double? dblCurRcp, double? intStnId, DebugLevels enumDebugLevel)
        {
            string strFlowDenom;
            // BDS 6-Jul-2012 PQ-D0074 Calculate the volume change
            // for a given blend station from its last update time

            double dblDltTime;
            int intTimeConv;
            long intI;
            DateTime dteLastValTime;
            double rtrData = 0;

            // Get flow denominator for the product group
            strFlowDenom = await _repository.GetFlowDenom(Convert.ToInt32(intPrdgrpID));

            intTimeConv = _shared.TimeConvFactor(strFlowDenom);

            // Find database update time stored for the specified blend station
            dteLastValTime = cdteNull;
            //UBound(gArStnValTime[intBldrIdx].arKey
            for (intI = 0; intI <= gArStnValTime[intBldrIdx].arKey.Max(); intI++)
            {
                if (gArStnValTime[intBldrIdx].arKey[intI] == intStnId)
                {
                    dteLastValTime = gArStnValTime[intBldrIdx].arValueTime[intI];
                    break;
                }
            }

            if (dteLastValTime == cdteNull)
                // Assuming Blend Monitor cycle time is in minutes
                dblDltTime = gDblCycleTime / (double)(60 * intTimeConv);
            else
                dblDltTime = Convert.ToDouble(DateTime.Now - dteLastValTime);

            rtrData = Convert.ToDouble(dblCurRcp) * Convert.ToDouble(gTagTotFlow.vntTagVal) * intTimeConv * dblDltTime / (double)100;
            return rtrData;
       
        }
        private async Task<double> ChkCompVol(int intBldrIdx, int intPrdgrpID, double dblCurRcp, DateTime dteLastValTime, DebugLevels enumDebugLevel)
        {
            // BDS 11-May-2012 PQ-D0074 Modified to calculate a volume
            // change using the last update time passed as a parameter
            // Private Function ChkCompVol(ByVal intBldrIdx As Integer, ByVal intPrdgrpID As Integer, _
            // ByVal dblCurRcp As Double, ByVal intCompIdx As Integer, _
            // ByVal enumDebugLevel As DebugLevels) As Double
            string strFlowDenom;
            double dblDltTime;
            int intTimeConv;
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            // get flow denominator for the product group
            strFlowDenom = await _repository.GetFlowDenom(intPrdgrpID);
            intTimeConv = _shared.TimeConvFactor(strFlowDenom);
            
            if ((dteLastValTime == cdteNull))
            {
                //  Assuming the cycle time of BMON is in minutes
                dblDltTime = (gDblCycleTime / (60 * intTimeConv));
            }
            else
            {
                dblDltTime = Convert.ToDouble(DateTime.Now - dteLastValTime);
            }

            return (dblCurRcp * Convert.ToDouble(gTagTotFlow.vntTagVal) * intTimeConv * dblDltTime/100);


        }

        private void LogStnUpdateTim(int intBldrIdx, double?[] arStationsDone)
        {
            // BDS 6-Jul-2012 PQ-D0074 Record time database is updated with station current volumes
            // for stations processed up to this point in the current Blend Monitor program cycle
            int intI;
            int intJ;
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            //UBound(arStationsDone)
            for (intI = 0; (intI <= arStationsDone.Max()); intI++)
            {
                if ((arStationsDone[intI] > 0))
                {
                    //UBound(gArStnValTime[intBldrIdx].arKey)
                    for (intJ = 0; (intJ <= gArStnValTime[intBldrIdx].arKey.Max()); intJ++)
                    {
                        if ((gArStnValTime[intBldrIdx].arKey[intJ] == arStationsDone[intI]))
                        {
                            gArStnValTime[intBldrIdx].arValueTime[intJ] = DateTime.Now;
                            break;
                        }
                    }
                }
            }

            return;
        }
        // Get the prev stations vols from blend stations
        private async Task<double> GetOrgStationVols(double blendId, double matId, double? curLineupId, double dblTotalStationVol)
        {            
            double? dblAddStationVol = 0;           

            // get the Stations that have the sume of act_setpoint<>0 for a single material           
            dblAddStationVol = await _repository.GetAddStationVol(blendId, matId, curLineupId);

            if(dblAddStationVol == null)
            {
                dblAddStationVol = 0;
            } else if(dblAddStationVol > 0)
            {
                dblTotalStationVol = dblTotalStationVol + Convert.ToDouble(dblAddStationVol);
            }
                       
            // Pass the total station vol back to the procedure
            return dblTotalStationVol;           
        }

        // ************* GetVolConvFactor *************
        private async Task<double?> GetVolConvFactor(double lngBlendId, int intPrdgrpID, int intProductId, int intAdditiveId)
        {
            string strPrdgrpInits;
            string strAddInits;
            double? rtnData = 0;
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            List<PrdgrpVolFactor> PrdgrpVolFactorData = await _repository.GetPrdgrpVolFactor(intPrdgrpID, intProductId, intAdditiveId);
            
            if (PrdgrpVolFactorData.Count() > 0)
            {
                rtnData = PrdgrpVolFactorData[0].UnitFactor;
            }
            else
            {
                strPrdgrpInits = PrdgrpVolFactorData[0].PrdgrpVolUnits;
                strAddInits = PrdgrpVolFactorData[0].AddVolUnits;
                // Volume conversion factor does not exist from additive units ^1 to blend volume units ^2
                var res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN91), programName, "BL-" + lngBlendId, strAddInits, strPrdgrpInits,
                    "", "", "", "", res);

                rtnData = 1;
            }
            return rtnData;
        }        
        private async Task<double> ChkIntBiasCalcCurr(string strBiasCalcCurrent, int intStartInterval, double lngBlendId, double lngPropID, int intStopInterval = 0)
        {                       
            string strBiasType;
            // TODO: On Error GoTo Warning!!!: The statement is not translatable            
            string strWhereSeq = "";
            double rtrnData = -1;
            List<AbcBlendIntervalProps> BiasCalData = new List<AbcBlendIntervalProps>();
            if ((intStopInterval != 0))
            {                
                BiasCalData =  await _repository.GetBiasCalData1(lngBlendId, lngPropID, intStartInterval, intStopInterval);
            }
            else
            {
                BiasCalData = await _repository.GetBiasCalData2(lngBlendId, lngPropID, intStartInterval, intStopInterval);
            }

            foreach (AbcBlendIntervalProps BiasCalDataObj in BiasCalData)
            {
                // find the interval bias calc current = SPOT/COMPOsite to return that interval.  If the it is
                // ANALYZER or NOCALC, then return the last found SPOT/COMPOsite
                strBiasType = (BiasCalDataObj.BiascalcCurrent==null)?"": BiasCalDataObj.BiascalcCurrent;
                if ((strBiasCalcCurrent != ""))
                {
                    if ((strBiasType == strBiasCalcCurrent))
                    {
                        // return the last maching interval
                        rtrnData = BiasCalDataObj.Sequence;
                        if ((strBiasCalcCurrent == "SPOT"))
                        {
                            // Pass the first interval found
                            break; //Warning!!! Review that break works as 'Exit Do' as it could be in a nested instruction like switch
                        }

                    }

                    // In the given range at least one interval has to match the specified calc current type.
                    // pass the first found, if not found pass NULL_
                }
                else if ((strBiasType == "COMPOSITE") || (strBiasType == "SPOT"))
                {
                    // return the last maching interval
                    rtrnData = BiasCalDataObj.Sequence;
                }
                else
                {
                    // if at least one interval do not match leave and pass the last
                    // matching interval
                    break; //Warning!!! Review that break works as 'Exit Do' as it could be in a nested instruction like switch
                }               
            }
            return rtrnData;
        }

        //Check if sample property was previuosly used for bias calc
        private async Task<bool> CheckSPUsed(double lngBlendId, double lngPropID, string strUsedFlag, int intStartIntv, bool bln1stBias, string strBiasCalc)
        {           
            int? intMatchingIntv;
            // TODO: On Error GoTo Warning!!!: The statement is not translatable            
            // check if sample property was ever used in this blend
            int count = await _repository.CheckPropertyUsed(lngBlendId, lngPropID, strUsedFlag);
            bool rtrData = false;
            if (count > 0)
            {
                rtrData = true;
            }
            else if (bln1stBias)
            {
                rtrData = bln1stBias;
                if ((strBiasCalc != "REG"))
                {
                    // if property was never used in this blend, then check if biasCalc is SPOT or COMPOSITE
                    // to define how far we should go back for the first bias update
                    intMatchingIntv = null;
                    // find out how many intervals have the same BiasCalc_current (SPOT/COMPOSITE) from the start interval until
                    // the first blend interval (Desc)
                    // strCurBiasType="", means that at this point function will return any intervals where biascalc_type=COMPOSITE or SPOT
                    intMatchingIntv = (int) await ChkIntBiasCalcCurr("", intStartIntv, lngBlendId, lngPropID);
                    if ((intMatchingIntv >= 0))
                    {
                        // Reset 1st bias calc for sample prop that has not been used
                        rtrData = false;
                    }
                }
            }
            else
            {
                rtrData = bln1stBias;
            }

            return rtrData;
        }
        private async Task<string> CalcBiasFallBack(string strBiasCalcCurrent, double lngBlenderID, CurBlendData curblend, double lngPropID, int intPrdgrpID)
        {            
            string strWhere;
            int intBlendIntvSeq;
            int intRetry;
            int intFrozenLimit;
            double lngAnzID;
            double dblAnzCycleTime;
            double dblAnzTranspTime;
            double dblTotalCycleTime;
            bool blnFallback;
            DateTime dteResTime;
            DateTime dteSmplTime;
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            string rtrnData = "";

            // Find out if the current prop is being measured by an anzr
            List<CheckAnzrMeasure> CheckAnzrMeasureData = await _repository.CheckAnzrMeasure(lngBlenderID, lngPropID);
           
            blnFallback = false;
            if (CheckAnzrMeasureData.Count() > 0)
            {
                intRetry = 0;
                // initialize
                lngAnzID = CheckAnzrMeasureData[0].AnzId;
                intFrozenLimit = (CheckAnzrMeasureData[0].FrozenOpLmt == null)?10:Convert.ToInt32(CheckAnzrMeasureData[0].FrozenOpLmt);
                dblAnzCycleTime = (CheckAnzrMeasureData[0].CycleTime == null)?10:Convert.ToDouble(CheckAnzrMeasureData[0].CycleTime);
                // get the anzr transport time per prop
                dblAnzTranspTime = (CheckAnzrMeasureData[0].TransportTime == null)? 5: Convert.ToDouble(CheckAnzrMeasureData[0].TransportTime);
                dteResTime = (CheckAnzrMeasureData[0].ResTime == null)?cdteNull: Convert.ToDateTime(CheckAnzrMeasureData[0].ResTime);
                // calculate total cycle time for this analyzer
                dblTotalCycleTime = (dblAnzCycleTime + dblAnzTranspTime);
            NEXT_RESTIME:
                dteSmplTime = DateAndTime.DateAdd("n", (dblTotalCycleTime * -1), dteResTime);
                if ((dteSmplTime > curblend.dteActualStart))
                {
                    // if abc_blends.bias_overrite_flag="YES", then fallback, if there is at least one composite/spot sample
                    // Do not care about BIASCALC_CURRENT for all intervals
                    
                    //             ' bias override should is used to process several samples at once, but the biascalc_current
                    //             'should match the sample type
                    //             If curblend.strBiasOverrideFlag = "NO" Then
                    // Get interval, where an anz value should exist
                    intBlendIntvSeq = (int)await _repository.GetBlendInterval(curblend.lngID, dteSmplTime);
                    if (intBlendIntvSeq != -1)
                    {

                        // Check if all the range of intervals between current interval and sample (expected)
                        // interval (minus one - to be sure) to see if at least one anzr value has come.
                        // If not then do one more check (configurable limit) in the next sample interval, if abc_blend_interval_count.result_count
                        // is still zero, then claim anzr as bad and fallback
                        // Blend_Interval_props.anz_id can be use to determine if there has been an update, because
                        // Anzr monitor only writes to this field if a new anzr value is available(and good) for a prop
                        List<AbcBlendIntervalProps> BlendIntervalPropsData = await _repository.GetBlendIntervalPropsData(curblend.lngID, lngPropID, curblend.intCurIntv, intBlendIntvSeq, lngAnzID);
                        
                        if (BlendIntervalPropsData.Count == 0)
                        {
                            // if anzr value not found then check more intervals (until limit is reached)
                            // Set the result time to the calc sample time of first interval and
                            // find the prev anzr interval, when result was expected
                            if (intRetry <= intFrozenLimit)
                            {
                                dteResTime = dteSmplTime;
                                intRetry = (intRetry + 1);
                                goto NEXT_RESTIME;
                            }
                            else
                            {
                                blnFallback = true;
                            }

                        }
                        else
                        {
                            blnFallback = false;
                        }

                        // result count = NULL for the expected anzr interval result
                    }

                    //  blend interval found for sample time
                    // if biasoverride_flag is set "NO", then update intervals forward only if needed

                    //----checked------
                    strWhere = (" AND SEQUENCE >="
                                + (curblend.intCurIntv - 1));
                }
                else if ((intRetry > 1))
                {
                    // This means, that at least two checks were done and it did fallback
                    blnFallback = true;
                    
                    //----checked------
                    strWhere = (" AND SEQUENCE >="
                                + (curblend.intCurIntv - 1));
                }

                // sample time < blend start time
            }

            // No anzr id for prop
            if ((blnFallback == true))
            {
                // check if this prop has a fallback value defined in abc_prdgrp_props.bias_calc_anz_fallback
                List<string> CheckBiasCalcAnzFallback = await _repository.CheckBiasCalcAnzFallback(intPrdgrpID, lngPropID);
                                
                if (CheckBiasCalcAnzFallback.Count() > 0)
                {
                    // If the fallback is NOT "NONE" then procceed, otherwise
                    // leave the BIASCALC_CURRENT untouched
                    if (CheckBiasCalcAnzFallback[0] != "NONE")
                    {
                        // update BIASCALC_CURRENT = BIAS_CALC_ANZ_FALLBACK for all intervals forward and log a msg
                        await _repository.SetBiasCalcCurrent(curblend.lngID, lngPropID, (curblend.intCurIntv - 1), CheckBiasCalcAnzFallback[0]);
                        
                        // Pass back the fallback value
                        rtrnData = CheckBiasCalcAnzFallback[0];
                    }

                }
                else
                {
                    // if there is not a fallback defined for this prop, then find out if there is a composite/spot
                    // sample available for this prop and fallback to that prop                    
                    List<string> sampleType = await _repository.GetSampleType(lngPropID, curblend.lngID);

                    if (sampleType.Count() > 0)
                    {
                        // update BIASCALC_CURRENT = BIAS_CALC_ANZ_FALLBACK for all intervals forward and log a msg
                        await _repository.SetBiasCalcCurrent(curblend.lngID, lngPropID, (curblend.intCurIntv - 1), sampleType[0]);
                        
                        // Pass back the fallback value
                        rtrnData = sampleType[0];
                    }
                }
            }
            
            // TODO: Exit Function: Warning!!! Need to return the value
            return rtrnData;
        }
        private async Task<int> CopyLineprop()
        {
            object fs;
            string strLinePropPath;
            string strDebugPath;
            string cstrStarBLendDir;
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            // create the folder Debug if does not exist yet
            // get proj default STARBLEND_INST_PATH
            

            AbcProjDefaults SwgDefTimeOut = await _repository.SwgDefTimeOut();
            cstrStarBLendDir = (SwgDefTimeOut.StarblendInstPath == null)? "C:\\SB35\\": SwgDefTimeOut.StarblendInstPath;
            
            // Add the \ symbol for the folder if it is not in the path already
            if ((cstrStarBLendDir.Substring((cstrStarBLendDir.Length - 1)) != "\\"))
            {
                cstrStarBLendDir = (cstrStarBLendDir + "\\");
            }

            strLinePropPath = (cstrStarBLendDir + ("Input\\"
                        + (gstrBldrName + "\\Lineprop\\*")));
            strDebugPath = (cstrStarBLendDir + ("Debug\\"
                        + (gstrBldrName + ("\\"
                        + (DateTime.Now.ToString("MM/dd/yyyy HH:mm")+ "\\Lineprop\\"))))); //Format(Now, ("mmm dd " + "hh_nn")) 
            //------------ debug ------
            //CreateDir(strDebugPath);
            //// Copy the folder from "C:\SB35\Input\BlenderName\Lineprop\*" to the C:\SB35\Debug\BlenderName\Lineprop\*"

             //PENDING
            //fs = CreateObject("Scripting.FileSystemObject");
            //fs.CopyFile;
            //strLinePropPath;
            //strDebugPath;
            //true;
            return 0;
        
        }      

        private async Task<int> CalcBias(int intBldrIdx,List<AbcBlenders> vntBldrsData, CurBlendData curblend, DebugLevels enumDebugLevel,string strIntBiasType,string strSampleName= "", 
            int intStartInterval = 0,int intStopInterval = 0,int intMatchIntv = 0)
        { 
            int vntIntvNum;
            double vntPropID;
            // , vntIntBiasCur As Variant
            double sngAnzOfst;
            double sngBiasFilt;
            double sngFbPredBias;
            double sngAnzRes;
            double sngFdbkPred;
            double sngBias;
            object vntMinBias;
            object vntMaxBias;
            string strModelErrExists;
            string strModelErrClrd ="";
            double dblIntBiasNew;
            double dblIntBias;
            double dblRateLimit;
            double dblUnfilBias;
            double dblBiasClamp;
            string strPropAlias = "";
            string strPropName = "";
            string strPropUnit= "";
            bool blnCopyLineprop;
            float sngCorrellBias;
            // Sample composite/spot declaration
            int intPropIndex;
            int intMatchingIntv;
            int intNprops;
            string strBiasCalcCurrent = "";
            string strFallbackProps;
            string strSampleType;
            string strUserFallbackType;
            string strUserCalcType;
            string strCalcBiasFallBack;
            double sngSampleRes;
            long lngPropID;
            bool blnFirstBias;
            int intTimeDiff;
            int intNRec;
            var res = "";
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 

            if (enumDebugLevel == DebugLevels.High)
            {
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, curblend.strName, "CALC_BIAS",
                    "", "", "", "", res);                
            }
            List<double> BlendIntPropsList = await _repository.GetBlendIntProps(curblend.lngID, vntBldrsData[intBldrIdx].PrdgrpId);
            intNprops = BlendIntPropsList.Count();
            if (gblnBiasRedimDone[intBldrIdx] == false) {
                if (Information.UBound(gblnFirstBiasCalc, 2) < (intNprops - 1)){
                    //'prop index will be base 0
                    // ReDim Preserve gblnFirstBiasCalc(Information.UBound(vntBldrsData, 2), 0 To(intNprops - 1))

                    //--------------debug--------------------//
                    gblnFirstBiasCalc = HelperMethods.ResizeArray<bool>(gblnFirstBiasCalc, vntBldrsData.Count(), intNprops);
                }
            }

            intPropIndex = -1; //'prop index will be base 0
            strFallbackProps = "";
            //'Loop of all blend interval props
            foreach (var BlendIntProp in BlendIntPropsList)
            {
                intPropIndex = intPropIndex + 1;
                vntPropID = BlendIntProp;
                List<BldSampleProps> BldSamplePropsList = new List<BldSampleProps>();
                if ((gblnBiasRedimDone[intBldrIdx] == false && gProjDfs.strAllowSCSampling == "YES") || (strIntBiasType != "REG" && gProjDfs.strAllowSCSampling == "YES")) {
                    //'if the first bias has already been calculated, then check if the bias calc type
                    //'has changed from Analyzer to COM or SPOT.  If that is the case, then reset this flag
                    //'to allow bias update all the way to first interval
                    //'Check if sample prop bias was calculated in the past
                    gblnFirstBiasCalc[intBldrIdx, intPropIndex] = await CheckSPUsed(curblend.lngID, vntPropID, "YES", intStartInterval, gblnFirstBiasCalc[intBldrIdx, intPropIndex], strIntBiasType);
                }

                if (strIntBiasType == "COM" || strIntBiasType == "SPO")
                {
                    BldSamplePropsList = await _repository.GetBldSampleProps(curblend.lngID, strSampleName);
                    intNRec = BldSamplePropsList.Count();
                    if (intNRec > 0)
                    {
                        List<BldSampleProps> BldSamplePropsListFltrd = BldSamplePropsList.Where<BldSampleProps>(row => row.PropId == vntPropID).ToList();

                        if (BldSamplePropsListFltrd.Count > 0)
                        {
                            // Set vntPropID = ABCdataEnv.rscomGetBldSampleProps.Fields("VALUE")
                            // sngFdbkPred = NVL(ABCdataEnv.rscomGetBldSampleProps.Fields("FEEDBACK_PRED").Value, 0)
                            // Set flag to copy lineprop folder when bias is outside of a valid range
                            blnCopyLineprop = true;
                            vntIntvNum = intStartInterval;
                            // Set vntPropID = ABCdataEnv.rscomGetBldSampleProps.Fields("PROP_ID")
                            // get the interval sample prop data.  The matching interval(where COMPOSITE or SPOT data exist) is used
                            // to get the fb pred, bias and so on.
                            List<SampleIntvProps> SampleIntvPropsList = await _repository.GetSampleIntvProps(curblend.lngID, intMatchIntv, vntPropID, vntBldrsData[intBldrIdx].PrdgrpId);

                            if (SampleIntvPropsList.Count() > 0)
                            {
                                strBiasCalcCurrent = (SampleIntvPropsList[0].BiascalcCurrent == null) ? "ANALYZER" : SampleIntvPropsList[0].BiascalcCurrent;
                                switch (strBiasCalcCurrent)
                                {
                                    case "COMPOSITE":
                                    case "SPOT":
                                        sngFdbkPred = (SampleIntvPropsList[0].FeedbackPred == null) ? -1 : Convert.ToDouble(SampleIntvPropsList[0].FeedbackPred);
                                        sngFbPredBias = (SampleIntvPropsList[0].FbPredBias == null) ? 0 : Convert.ToDouble(SampleIntvPropsList[0].FbPredBias);
                                        sngSampleRes = (BldSamplePropsList[0].Value == null) ? -1 : Convert.ToDouble(BldSamplePropsList[0].Value);
                                        if ((strBiasCalcCurrent == "COMPOSITE"))
                                        {
                                            sngBiasFilt = (SampleIntvPropsList[0].CompositeFilter == null) ? 0 : Convert.ToDouble(SampleIntvPropsList[0].CompositeFilter);
                                            dblBiasClamp = (SampleIntvPropsList[0].CompositeBiasClamp == null) ? -1 : Convert.ToDouble(SampleIntvPropsList[0].CompositeBiasClamp);
                                        }
                                        else
                                        {
                                            // SPOT
                                            sngBiasFilt = (SampleIntvPropsList[0].SpotFilter == null) ? 0 : Convert.ToDouble(SampleIntvPropsList[0].SpotFilter);
                                            dblBiasClamp = (SampleIntvPropsList[0].SpotBiasClamp == null) ? -1 : Convert.ToDouble(SampleIntvPropsList[0].SpotBiasClamp);
                                        }

                                        // Sample_res and the Feedback_pred should be NOT NULL
                                        if (sngFdbkPred == -1 || sngSampleRes == -1)
                                        {
                                            goto NEXTPROP;
                                        }

                                        // Get The property units name for Viscosity
                                        // In the near future this function will be implemented for all props
                                        List<PropNameModel> PropNameData = await _repository.GetPropName(vntPropID);

                                        if (PropNameData.Count() > 0)
                                        {
                                            strPropName = PropNameData[0].PropName;
                                            strPropUnit = PropNameData[0].UnitsName;
                                        }

                                        if (strPropName == "D_VISC" || strPropName == "F_VISC")
                                        {
                                            if ((strPropName == "D_VISC"))
                                            {
                                                // vntPropID   D_VISC
                                                // Convert the Fb predicted and anz result to same CST for viscosity
                                                sngFdbkPred = await _repository.GetConvValue(sngFdbkPred, strPropUnit, "CST@40C");
                                                sngSampleRes = await _repository.GetConvValue(sngSampleRes, strPropUnit, "CST@40C");
                                            }
                                            else if ((strPropName == "F_VISC"))
                                            {
                                                // vntPropID  F_VISC
                                                // Convert the Fb predicted and anz result to same CST for viscosity
                                                sngFdbkPred = await _repository.GetConvValue(sngFdbkPred, strPropUnit, "CST@50C");
                                                sngSampleRes = await _repository.GetConvValue(sngSampleRes, strPropUnit, "CST@50C");
                                            }

                                        }

                                        // calc new pure composite/spot bias
                                        dblIntBiasNew = (sngSampleRes
                                                    - (sngFdbkPred - sngFbPredBias));
                                        dblUnfilBias = dblIntBiasNew;
                                        if (dblBiasClamp == -1)
                                        {
                                            dblBiasClamp = dblUnfilBias;
                                        }

                                        if (Math.Abs(dblUnfilBias) > Math.Abs(dblBiasClamp))
                                        {
                                            // clamp to min bias and set abc_blend_intervals.unfilt_bias
                                            // Ensure that sign of bias clamp has no effect
                                            dblUnfilBias = Math.Abs(dblBiasClamp) * dblUnfilBias / Math.Abs(dblUnfilBias);
                                        }

                                        intMatchingIntv = -1;
                                        // find the interval range where the bias will be updated (interval range between stopinterval and
                                        // current closed interval (currIntvl -1))
                                        //  Comment out BiasOverrideFlag
                                        //                                 If curblend.strBiasOverrideFlag = "NO" Then
                                        // check if all intervals have the same BiasCalc_current. strCurBiasType="", means that at this point
                                        // function will return any intervals where biascalc_type=COMPOSITE or SPOT
                                        intMatchingIntv = (int)await ChkIntBiasCalcCurr("", intStopInterval, curblend.lngID, vntPropID, (curblend.intCurIntv - 1));
                                        // If no records found, then update only the range of composite/spot intervals
                                        // Note: the range of intervals where the composite/spot was allocated will be updated, only
                                        // for those intervals where the CurrentCalc_type is  "COMPOSITE" or "SPOT"
                                        if ((intMatchingIntv == -1))
                                        {
                                            // setting the intMatchingIntv = intStopInterval allows update between start and stop intervals only
                                            intMatchingIntv = intStopInterval;
                                        }

                                        // If intMatchingIntv = Curr intvl - 1, then update bias all the way to last interval (forward)
                                        if ((intMatchingIntv
                                                    >= (curblend.intCurIntv - 1)))
                                        {
                                            intMatchingIntv = curblend.intCurIntv;
                                        }

                                        if (intMatchingIntv != -1)
                                        {
                                            // Save pure (or clamping bias) in DB (abc_blend_intervals.unfilt_bias)
                                            // exclude ANALYZER/SPOT calc_curr_types
                                            await _repository.setUnfiltBias(dblUnfilBias, curblend.lngID, vntPropID, vntIntvNum, intStopInterval, intMatchingIntv);
                                        }

                                        List<double> PrevIntBiasData = await _repository.GetPrevIntBias(curblend.lngID, (vntIntvNum - 1), vntPropID);

                                        sngBias = 0;
                                        if (PrevIntBiasData.Count > 0)
                                        {
                                            sngBias = PrevIntBiasData[0];
                                        }

                                        //  if this is the first time bias is being calc for this prop, then
                                        // skip filtering
                                        blnFirstBias = false;
                                        if ((gblnFirstBiasCalc[intBldrIdx, intPropIndex] == true))
                                        {
                                            // Filter the pure Bias
                                            dblIntBias = ((sngBias * sngBiasFilt)
                                                        + (dblIntBiasNew * (1 - sngBiasFilt)));
                                        }
                                        else
                                        {
                                            // set this flag to true for the rest of the blend for this prop since first time bias was already processed
                                            gblnFirstBiasCalc[intBldrIdx, intPropIndex] = true;
                                            dblIntBias = dblIntBiasNew;
                                            blnFirstBias = true;
                                        }

                                        if (enumDebugLevel >= DebugLevels.Medium)
                                        {
                                            strPropAlias = await _repository.GetPropAlias((int)vntPropID);

                                            if (enumDebugLevel == DebugLevels.Medium)
                                            {
                                                res = "";
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG35), programName, cstrDebug, curblend.strName, strPropAlias,
                                                    dblIntBiasNew.ToString(), dblIntBias.ToString(), "", "", res);
                                            }
                                            else
                                            {
                                                res = "";
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG36), programName, cstrDebug, curblend.strName, strPropAlias,
                                                    sngSampleRes.ToString(), sngFdbkPred.ToString(), dblIntBiasNew.ToString(), sngBias.ToString(), res);

                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG40), programName, cstrDebug, curblend.strName, strPropAlias,
                                                    sngBiasFilt.ToString(), dblIntBias.ToString(), "", "", res);
                                            }

                                        }

                                        if (Math.Abs(dblIntBias) > Math.Abs(dblBiasClamp))
                                        {
                                            strPropAlias = await _repository.GetPropAlias((int)vntPropID);

                                            res = "";
                                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN95), programName, "BL-" + curblend.lngID, curblend.strName, strPropAlias,
                                                Math.Round(Math.Abs(dblIntBias), 3).ToString(), Math.Abs(dblBiasClamp).ToString(), Math.Abs(dblBiasClamp).ToString(), "", res);

                                            // clamp to min bias and set ABC_BLEND_PROPS.MODEL_ERR_EXISTS_FLAG to YES
                                            // Ensure that sign of bias clamp has no effect
                                            dblIntBias = (Math.Abs(dblBiasClamp) * (dblIntBias / Math.Abs(dblIntBias)));
                                            strModelErrExists = "YES";
                                            await _repository.SetModelErrExistsFlag("YES", curblend.lngID, vntPropID);
                                        }
                                        else
                                        {
                                            // set ABC_BLEND_PROPS.MODEL_ERR_CLRD.FLAG to YES if
                                            // MODEL_ERR_EXISTS_FLAG is YES
                                            // also set ABC_BLEND_PROPS.MODEL_ERR_EXISTS_FLAG to NO
                                            strModelErrExists = "NO";
                                            await _repository.SetModelErrClrdFlag(curblend.lngID, vntPropID);
                                        }

                                        if (enumDebugLevel >= DebugLevels.Medium)
                                        {
                                            res = "";
                                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG37), programName, cstrDebug, curblend.strName, strPropAlias,
                                                dblIntBias.ToString(), (0).ToString(), dblBiasClamp.ToString(), "", res);

                                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG38), programName, cstrDebug, curblend.strName, strPropAlias,
                                               strModelErrExists, strModelErrClrd, "", "", res);

                                        }

                                        // save new interval prop bias to this and all sub sebsequent intervals
                                        dblIntBias = Math.Round(dblIntBias, 10);
                                        // if bias_override flag is set to YES, then update bias all the way to first interval
                                        // Comment out BiasOverrideFlag
                                        //                                 If curblend.strBiasOverrideFlag = "NO" Then
                                        // find out how many intervals have the same BiasCalc_current (SPOT/COMPOSITE) between stopInterval and curblend.intCurIntv - 1
                                        // strCurBiasType="", means that at this point function will return any intervals where biascalc_type=COMPOSITE or SPOT
                                        if (intMatchingIntv != -1)
                                        {
                                            await _repository.setBias(dblIntBias, curblend.lngID, vntPropID, vntIntvNum, intStopInterval, intMatchingIntv);

                                            if ((intMatchingIntv >= curblend.intCurIntv))
                                            {
                                                intMatchingIntv = (curblend.intCurIntv - 1);
                                            }

                                            // if interval found matching the curr_bias_type=COMPOSITE/SPOT, then recalc LINEPROP up to that interval
                                            if (((gintStartStopIntv[(int)StartStop.STRT] > vntIntvNum)
                                                        || (gintStartStopIntv[(int)StartStop.STRT] == -1)))
                                            {
                                                gintStartStopIntv[(int)StartStop.STRT] = vntIntvNum;
                                            }

                                            if (((gintStartStopIntv[(int)StartStop.STP] < intMatchingIntv)
                                                        || (gintStartStopIntv[(int)StartStop.STP] == -1)))
                                            {
                                                gintStartStopIntv[(int)StartStop.STP] = intMatchingIntv;
                                            }

                                            //  Make sure that start/stop intervals are valid
                                            if ((gintStartStopIntv[(int)StartStop.STRT] > gintStartStopIntv[(int)StartStop.STP]))
                                            {
                                                gintStartStopIntv[(int)StartStop.STP] = gintStartStopIntv[(int)StartStop.STRT];
                                            }

                                        }

                                        // If we are here, then it is because the sample prop had the USED_flag='NO'.
                                        // If at least one prop satisfy these conditions, then the calc Bias should be updated
                                        // all the way to specified interval along with LINEPROP. Check if biascalc_current for the
                                        // StartInterval is equal to all previous intervals
                                        // A global var containing the start/stop intervals will be passed back to perform (recalc) Lineprop for those intervals
                                        if ((blnFirstBias == true))
                                        {
                                            intMatchingIntv = -1;
                                            // Comment out BiasOverrideFlag
                                            //                                     'if bias_override flag is set to YES, then update bias all the way to first interval
                                            //                                     If curblend.strBiasOverrideFlag = "NO" Then
                                            // find out how many intervals have the same BiasCalc_current (SPOT/COMPOSITE) from the start interval until
                                            // the first blend interval (Desc)
                                            // strCurBiasType="", means that at this point function will return any intervals where biascalc_type=COMPOSITE or SPOT
                                            intMatchingIntv = (int)await ChkIntBiasCalcCurr("", vntIntvNum, curblend.lngID, vntPropID);
                                            if ((intMatchingIntv >= 0))
                                            {
                                                // update BiasCalc_current all the way to the found interval matching biasCalc Current COMPOSITE/SPOT
                                                await _repository.setBiasAndUnfiltBias(dblIntBias, dblUnfilBias, curblend.lngID, vntPropID, vntIntvNum, intMatchingIntv);

                                                // For interval zero, there is not need to recalculate anything
                                                if (intMatchingIntv == 0)
                                                {
                                                    intMatchingIntv = 1;
                                                }

                                                if ((gintStartStopIntv[(int)StartStop.STRT] > intMatchingIntv))
                                                {
                                                    gintStartStopIntv[(int)StartStop.STRT] = intMatchingIntv;
                                                }

                                                if ((gintStartStopIntv[(int)StartStop.STP] < intStopInterval))
                                                {
                                                    gintStartStopIntv[(int)StartStop.STP] = intStopInterval;
                                                }

                                            }
                                        }

                                        // update abc_blend_sample_props.USED_flag to "YES" to avoid further processing of this prop
                                        await _repository.SetUsedFlag(curblend.lngID, vntPropID, strSampleName);

                                        // Only the latest anzr value should be used for bias calc.
                                        // The previous anzr values should be excluded once the current values are used in bias calc
                                        // This query has been modified to update the Calc_Prop_flag to NO for a prop in intvs <= Current intvl
                                        await _repository.SetIntCalcPropertyFlag(curblend.lngID, vntPropID, vntIntvNum);
                                        break;
                                    case "ANALYZER":
                                        if (gProjDfs.strAllowSCSampling == "YES" && curblend.strState.Trim() != "PAUSED")
                                        {
                                            strCalcBiasFallBack = "";
                                            if ((gArAnzDelay[intBldrIdx] != cdteNull))
                                            {
                                                // Compare the anzr_start_delay (optimizer_delay) with the current time
                                                // get current time
                                                gDteCurTime = await _repository.GetCurTime();

                                                // get the time diff between the curr time  and the anz timer for processing (in minutes)
                                                intTimeDiff = (int)DateAndTime.DateDiff("n", gArAnzDelay[intBldrIdx], gDteCurTime);
                                                if (intTimeDiff > (2 * vntBldrsData[intBldrIdx].AnzrStartDelay))
                                                {
                                                    // calc sub CalcBiasFallBack to handle fallback if needed
                                                    strCalcBiasFallBack = await CalcBiasFallBack(strBiasCalcCurrent, vntBldrsData[intBldrIdx].Id, curblend, vntPropID, (int)vntBldrsData[intBldrIdx].PrdgrpId);
                                                }

                                            }
                                            else
                                            {
                                                // calc sub CalcBiasFallBack to handle fallback if needed
                                                strCalcBiasFallBack = await CalcBiasFallBack(strBiasCalcCurrent, vntBldrsData[intBldrIdx].Id, curblend, vntPropID, (int)vntBldrsData[intBldrIdx].PrdgrpId);
                                            }

                                            if ((strCalcBiasFallBack != ""))
                                            {
                                                // if this function returns <>"" value, then log a msg and go to next prop
                                                // This means that the a fallback was requested and it will be processed in the next cycle of BMon
                                                // Create an array of props and log the msg only once for all props
                                                List<PropNameModel> PropNameList = await _repository.GetPropName(vntPropID);

                                                if (PropNameList.Count() > 0)
                                                {
                                                    strPropName = PropNameList[0].PropName;
                                                }

                                                strUserCalcType = "ANALYZER";
                                                List<AbcTranstxt> TranstxtData1 = await _repository.GetTranstxtData("BIASCALCTYPE");
                                                List<AbcTranstxt> TranstxtDataFltrd1 = TranstxtData1.Where<AbcTranstxt>(row => row.Value == strBiasCalcCurrent).ToList();

                                                if (TranstxtDataFltrd1.Count() > 0)
                                                {
                                                    strUserCalcType = TranstxtDataFltrd1[0].UserValue;
                                                }

                                                strUserFallbackType = "NONE";

                                                List<AbcTranstxt> TranstxtData2 = await _repository.GetTranstxtData("ANZFALLBACKTYPE");
                                                List<AbcTranstxt> TranstxtDataFltrd2 = TranstxtData2.Where<AbcTranstxt>(row => row.Value == strCalcBiasFallBack).ToList();

                                                if (TranstxtDataFltrd2.Count() > 0)
                                                {
                                                    strUserFallbackType = TranstxtDataFltrd2[0].UserValue;
                                                }

                                                // save the prop name to display a message after the prop loop
                                                if ((strFallbackProps == ""))
                                                {
                                                    strFallbackProps = strPropName;
                                                    // & ":" & strUserCalcType & "2" & strUserFallbackType
                                                }
                                                else
                                                {
                                                    strFallbackProps = (strFallbackProps + ("," + strPropName));
                                                    // & ":" & strUserCalcType & "2" & strUserFallbackType
                                                }

                                            }

                                        }

                                        // strAllowSCSampling
                                        break;
                                    case "NOCALC":
                                        break;
                                }
                                // Select Bias Calc Type
                            }

                        // Sample Interval prop
                        NEXTPROP: { }
                        }

                    }

                    // if not Blend Sample props
                }
                else
                {
                    List<SampleIntvProps> SampleIntvPropsList = await _repository.GetSampleIntvProps(curblend.lngID, curblend.intCurIntv - 1, vntPropID, vntBldrsData[intBldrIdx].PrdgrpId);
                    strBiasCalcCurrent = "";
                    if (SampleIntvPropsList.Count() > 0)
                    {
                        strBiasCalcCurrent = (SampleIntvPropsList[0].BiascalcCurrent == null) ? "ANALYZER" : SampleIntvPropsList[0].BiascalcCurrent;
                        //' Fallback handling should be located here, because this checking should be done
                        //'only for ANALYZER type of abc_blend_interval_props.calc_current_type
                        //'The interval should be (Current_interval - 1), because we will be doing Bias calc only after a new interval is created
                        if ((strBiasCalcCurrent == "ANALYZER"))
                        {
                            // The fallback logic should apply only when sampling is enabled
                            if ((gProjDfs.strAllowSCSampling == "YES"))
                            {
                                strCalcBiasFallBack = "";
                                if ((gArAnzDelay[intBldrIdx] != cdteNull))
                                {
                                    // Compare the anzr_start_delay (optimizer_delay) with the current time
                                    // get current time
                                    gDteCurTime = await _repository.GetCurTime();

                                    // get the time diff between the curr time  and the anz timer for processing (in minutes)
                                    intTimeDiff = (int)DateAndTime.DateDiff("n", gArAnzDelay[intBldrIdx], gDteCurTime);
                                    if (intTimeDiff > (2 * vntBldrsData[intBldrIdx].AnzrStartDelay))
                                    {
                                        // calc sub CalcBiasFallBack to handle fallback if needed
                                        strCalcBiasFallBack = await CalcBiasFallBack(strBiasCalcCurrent, vntBldrsData[intBldrIdx].Id, curblend, vntPropID, Convert.ToInt32(vntBldrsData[intBldrIdx].PrdgrpId));
                                    }

                                }
                                else
                                {
                                    // calc sub CalcBiasFallBack to handle fallback if needed
                                    strCalcBiasFallBack = await CalcBiasFallBack(strBiasCalcCurrent, vntBldrsData[intBldrIdx].Id, curblend, vntPropID, Convert.ToInt32(vntBldrsData[intBldrIdx].PrdgrpId));
                                }

                                // Calc sub CalcBiasFallBack to handle fallback if needed
                                if ((strCalcBiasFallBack != ""))
                                {
                                    // if fallback was requested then set strBiasCalcCurrent=CalcBiasFallBack.  This
                                    // will skip the current analyzer bias calc for this prop in this cycle.  It will
                                    // be done in the next call of CalcBias according with the new bias calc current type
                                    strBiasCalcCurrent = strCalcBiasFallBack;
                                    // if this function returns <>"" value, then log a msg and go to next prop
                                    // This means that the a fallback was requested and it will be processed in the next cycle of BMon
                                    // Create an array of props and log the msg only once for all props
                                    List<PropNameModel> PropNameData = await _repository.GetPropName(vntPropID);

                                    if (PropNameData.Count() > 0)
                                    {
                                        strPropName = PropNameData[0].PropName;
                                    }

                                    strUserCalcType = "ANALYZER";
                                    List<AbcTranstxt> TranstxtData1 = await _repository.GetTranstxtData("BIASCALCTYPE");
                                    List<AbcTranstxt> TranstxtDataFltrd1 = TranstxtData1.Where<AbcTranstxt>(row => row.Value == strBiasCalcCurrent).ToList();

                                    if (TranstxtDataFltrd1.Count() > 0)
                                    {
                                        strUserCalcType = TranstxtDataFltrd1[0].UserValue;
                                    }

                                    strUserFallbackType = "NONE";

                                    List<AbcTranstxt> TranstxtData2 = await _repository.GetTranstxtData("ANZFALLBACKTYPE");
                                    List<AbcTranstxt> TranstxtDataFltrd2 = TranstxtData2.Where<AbcTranstxt>(row => row.Value == strCalcBiasFallBack).ToList();

                                    if (TranstxtDataFltrd2.Count() > 0)
                                    {
                                        strUserFallbackType = TranstxtDataFltrd2[0].UserValue;
                                    }

                                    // save the prop name to display a message after the prop loop
                                    if ((strFallbackProps == ""))
                                    {
                                        strFallbackProps = strPropName;
                                        // & ":" & strUserCalcType & "2" & strUserFallbackType
                                    }
                                    else
                                    {
                                        strFallbackProps = (strFallbackProps + ("," + strPropName));
                                        // & ":" & strUserCalcType & "2" & strUserFallbackType
                                    }


                                }// calcbiasfallback function

                            }// if proj default allow sampling
                        }//  biascalc Current
                    }// ' not interval prop data


                    //  the BiasCalc_current field will be used to perform bias calc
                    // The default value is "ANALYZER", so if the user wants to skip bias calc
                    // for analyzers, then BiasCalc_type should be set to "NOCALC"
                    if ((strBiasCalcCurrent == "ANALYZER"))
                    {
                        //   Create a loop of props and find the latest good value fo each
                        // prop in order to calculate the BIAS based in that record.
                        //  this cmd was modified to add a "where" clause where the query will
                        // return only NOT NULL records for anzr_res and feedback pred values

                        // This cmd needs to be executed only once, because it queries all props for all intvs
                        List<BiasData> BiasDataList = await _repository.GetBiasData(curblend.lngID, vntBldrsData[intBldrIdx].Id, vntBldrsData[intBldrIdx].PrdgrpId);

                        intNRec = -1;
                        if (BiasDataList.Count() > 0)
                        {
                            intNRec = BiasDataList.Count();

                            // Set flag to copy lineprop folder when bias is outside of a valid range
                            blnCopyLineprop = true;
                            List<BiasData> BiasDataListfltrd = BiasDataList.Where<BiasData>(row => row.PropId == vntPropID).ToList();

                            if (BiasDataListfltrd.Count() > 0)
                            {
                                vntIntvNum = Convert.ToInt32(BiasDataListfltrd[0].Sequence);
                                vntPropID = BiasDataListfltrd[0].PropId;
                                sngFdbkPred = (BiasDataListfltrd[0].FeedbackPred == null) ? 0 : Convert.ToDouble(BiasDataListfltrd[0].FeedbackPred);
                                sngAnzRes = (BiasDataListfltrd[0].AnzRes == null) ? 0 : Convert.ToDouble(BiasDataListfltrd[0].AnzRes);
                                sngFbPredBias = (BiasDataListfltrd[0].FbPredBias == null) ? 0 : Convert.ToDouble(BiasDataListfltrd[0].FbPredBias);
                                sngBiasFilt = (BiasDataListfltrd[0].BiasFilter == null) ? 0 : Convert.ToDouble(BiasDataListfltrd[0].BiasFilter);
                                sngAnzOfst = (BiasDataListfltrd[0].Offset == null) ? 0 : Convert.ToDouble(BiasDataListfltrd[0].Offset);
                                dblBiasClamp = (BiasDataListfltrd[0].ModelErrThrsh == null) ? -1 : Convert.ToDouble(BiasDataListfltrd[0].ModelErrThrsh);

                                // save anz offset to ABC_BLEND_PROPS
                                if (!gArAnzOfstSvd[intBldrIdx])
                                {
                                    sngAnzOfst = Math.Round(sngAnzOfst, 10);
                                    await _repository.SetPropAnzOffset(sngAnzOfst, curblend.lngID, vntPropID);
                                }

                                //Get The property units name for Viscosity
                                // In the near future this function will be implemented for all props
                                List<PropNameModel> PropNameList = await _repository.GetPropName(vntPropID);

                                if (PropNameList.Count() > 0)
                                {
                                    strPropName = PropNameList[0].PropName;
                                    strPropUnit = PropNameList[0].UnitsName;
                                }

                                if ((strPropName == "D_VISC") || (strPropName == "F_VISC"))
                                {
                                    if ((strPropName == "D_VISC"))
                                    {
                                        // vntPropID   D_VISC
                                        // Convert the Fb predicted and anz result to same CST for viscosity
                                        sngFdbkPred = await _repository.GetConvValue(sngFdbkPred, strPropUnit, "CST@40C");
                                        sngAnzRes = await _repository.GetConvValue(sngAnzRes, strPropUnit, "CST@40C");
                                    }
                                    else if ((strPropName == "F_VISC"))
                                    {
                                        // vntPropID  F_VISC
                                        // Convert the Fb predicted and anz result to same CST for viscosity
                                        sngFdbkPred = await _repository.GetConvValue(sngFdbkPred, strPropUnit, "CST@50C");
                                        sngAnzRes = await _repository.GetConvValue(sngAnzRes, strPropUnit, "CST@50C");
                                    }

                                }

                                //Get the correlation bias from abc_prdgrp_mat_props
                                sngCorrellBias = 0;

                                // Get the sum of anz bias + Correlation bias
                                sngFbPredBias = sngFbPredBias + sngCorrellBias;

                                dblIntBiasNew = (sngAnzRes - (sngFdbkPred - sngFbPredBias));
                                dblIntBiasNew = Math.Round(dblIntBiasNew, 10);
                                await _repository.SetModelErr(dblIntBiasNew, vntBldrsData[intBldrIdx].Id, vntPropID, curblend.lngID, vntIntvNum, vntPropID);
                                // **********
                                // Clamp the pure Bias and save it into abc_blend_intervals.unfilt_bias
                                dblUnfilBias = dblIntBiasNew;
                                // After disc. with Krisk: if bias clamp is null then set = unfilt_bias (no limits)
                                if (dblBiasClamp == -1)
                                {
                                    dblBiasClamp = dblUnfilBias;
                                }

                                if (Math.Abs(dblUnfilBias) > Math.Abs(dblBiasClamp))
                                {
                                    // clamp to min bias and set abc_blend_intervals.unfilt_bias
                                    // Ensure that sign of bias clamp has no effect
                                    dblUnfilBias = (Math.Abs(dblBiasClamp) * (dblUnfilBias / Math.Abs(dblUnfilBias)));
                                }

                                // Save pure (or clamping bias) in DB (abc_blend_intervals.unfilt_bias)
                                await _repository.SetUnFiltBias(dblUnfilBias, curblend.lngID, vntIntvNum, vntPropID);

                                //Get the base bias from the current close interval - 1                            

                                List<double> PrevIntBias = await _repository.GetPrevIntBias(curblend.lngID, (vntIntvNum - 1), vntPropID);

                                sngBias = 0;
                                if (PrevIntBias.Count() > 0)
                                {
                                    sngBias = PrevIntBias[0];
                                }

                                //if this is the first time bias is being calc for this prop, then
                                // skip filtering
                                if (gblnFirstBiasCalc[intBldrIdx, intPropIndex] == true)
                                {
                                    // Filter the pure Bias
                                    dblIntBias = ((sngBias * sngBiasFilt) + (dblIntBiasNew * (1 - sngBiasFilt)));
                                }
                                else
                                {
                                    // set this flag to false for the rest of the blend for this prop since first time bias was already processed
                                    gblnFirstBiasCalc[intBldrIdx, intPropIndex] = true;
                                    dblIntBias = dblIntBiasNew;
                                }

                                if (enumDebugLevel == DebugLevels.Medium)
                                {
                                    // June 04/2001: Check the new bias for each property and copy the input files to the debug
                                    //               the debug folder for later analysis.  Could be removed after debugging
                                    // Copy files if bias greater than rate limit from abc_anz_hdr_props.rate_lmt
                                    dblRateLimit = (BiasDataListfltrd[0].RateLmt == null) ? 1 : Convert.ToDouble(BiasDataListfltrd[0].RateLmt);
                                    if (Math.Abs((dblIntBias - sngBias)) > Math.Abs(dblRateLimit))
                                    {
                                        if ((blnCopyLineprop == true))
                                        {
                                            // Copy Lineprop to the debug folder
                                            await CopyLineprop();
                                            blnCopyLineprop = false;
                                        }

                                    }

                                }

                                if (enumDebugLevel >= DebugLevels.Medium)
                                {
                                    strPropAlias = await _repository.GetPropAlias(Convert.ToInt32(vntPropID));
                                    ;
                                    if (enumDebugLevel == DebugLevels.Medium)
                                    {
                                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG35), programName, cstrDebug, curblend.strName, strPropAlias,
                                                   dblIntBiasNew.ToString(), dblIntBias.ToString(), "", "", res);
                                    }
                                    else
                                    {
                                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG36), programName, cstrDebug, curblend.strName, strPropAlias,
                                                   sngAnzRes.ToString(), sngFdbkPred.ToString(), dblIntBiasNew.ToString(), sngBias.ToString(), res);

                                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG40), programName, cstrDebug, curblend.strName, strPropAlias,
                                                   sngBiasFilt.ToString(), dblIntBias.ToString(), "", "", res);
                                    }

                                }

                                if (Math.Abs(dblIntBias) > Math.Abs(dblBiasClamp))
                                {
                                    strPropAlias = await _repository.GetPropAlias(Convert.ToInt32(vntPropID));
                                    //Log a message: BLEND ^1, PROP ^2: INTERVAL BIAS ^3 EXCEEDS THE MODEL ERROR THRESHOLD ^4. CURRENT BIAS CLAMPED TO MIN BIAS ^5
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN95), programName, "BL-" + curblend.lngID, curblend.strName, strPropAlias,
                                                   Math.Round(Math.Abs(dblIntBias), 3).ToString(), Math.Abs(dblBiasClamp).ToString(), Math.Abs(dblBiasClamp).ToString(), "", res);

                                    // clamp to min bias and set ABC_BLEND_PROPS.MODEL_ERR_EXISTS_FLAG to YES
                                    // Ensure that sign of bias clamp has no effect
                                    dblIntBias = (Math.Abs(dblBiasClamp) * (dblIntBias / Math.Abs(dblIntBias)));
                                    strModelErrExists = "YES";
                                    await _repository.SetModelErrExistsFlag("YES", curblend.lngID, vntPropID);

                                }
                                else
                                {
                                    // If dblBiasClamp <> NULL_ Then 'And Not IsNull(vntMaxBias.Value) Then
                                    // set ABC_BLEND_PROPS.MODEL_ERR_CLRD.FLAG to YES if
                                    // MODEL_ERR_EXISTS_FLAG is YES
                                    // also set ABC_BLEND_PROPS.MODEL_ERR_EXISTS_FLAG to NO
                                    strModelErrExists = "NO";
                                    await _repository.SetModelErrClrdFlag(curblend.lngID, vntPropID);
                                }

                                if (enumDebugLevel >= DebugLevels.Medium)
                                {
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG37), programName, cstrDebug, curblend.strName, strPropAlias,
                                                   dblIntBias.ToString(), (0).ToString(), dblBiasClamp.ToString(), "", res);
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG38), programName, cstrDebug, curblend.strName, strPropAlias,
                                                  strModelErrExists.ToString(), strModelErrClrd.ToString(), "", "", res);
                                }

                                // save new interval prop bias to this and all sub sebsequent intervals
                                dblIntBias = Math.Round(dblIntBias, 10);
                                await _repository.SetIntvBias(dblIntBias, curblend.lngID, vntIntvNum, vntPropID);
                                // save new interval prop calc Property Flag to NO for this interval only
                                // Only the latest anzr value should be used for bias calc. The previuos
                                // anzr values should be excluded once the current values are used in bias calc
                                // This query has been modified to update the Calc_Prop_flag to NO for a prop in the previous intvs
                                await _repository.SetIntCalcPropertyFlag(curblend.lngID, vntPropID, vntIntvNum);
                                //  To force the optimization the the first time when Bias is calculated or when
                                //  the Bmon starts after it got killed or terminated
                                if (gblnSetOptNowFlag[intBldrIdx] == false)
                                {
                                    if (((curblend.intCurIntv > gArPrevBldData[intBldrIdx].intCurIntv) && (curblend.intCurIntv > 1))
                                                && ((curblend.vntPendSt == null) && (vntBldrsData[intBldrIdx].OptimizeFlag == "YES")))
                                    {
                                        //  the Bmon now sets the TQI_NOW_FLAG instead of the PENDING_STATE='OPTIMIZING'
                                        await _repository.SetTqi(curblend.lngID);

                                        // Set the flag to false to avoid this routine for the others properties/rest of the blend
                                        gblnSetOptNowFlag[intBldrIdx] = true;
                                    }
                                }
                            }

                        } // if prop no found
                    }//'If biascalc_type is not Analyzer
                }// 'COM, REG or SPO calc types
            }//'Loop of all blend interval props

            if (!gArAnzOfstSvd[intBldrIdx])
            {
                gArAnzOfstSvd[intBldrIdx] = true;
            }

            if ((strFallbackProps != ""))
            {
                // IN BLEND ^1 & INTERVAL ^2, THE FOLLOWING PROPS CHANGED CURR BIAS CALC TYPE TO DEFAULT BIAS TYPE: ^3
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN102), programName, "BL-" + curblend.lngID, curblend.strName, (curblend.intCurIntv - 1).ToString(),
                                                  strFallbackProps.ToString(), "", "", "", res);               
            }

        NEXT_SAMPLE:
            
            // At this point the array constaining the bias of first time calc: true/false has been done
            // for the current blender, so set a boolean for the rest of the blend to avoid further redim
            if ((gblnBiasRedimDone[intBldrIdx] == false))
            {
                gblnBiasRedimDone[intBldrIdx] = true;
            }
            return 0;
        }

        // *********** UpdateBlendPropValue ***********       
        private async Task<int> UpdateBlendProps(int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend, DebugLevels enumDebugLevel)
        {
            double vntPropID1;
            int vntIntvNum;
            double? vntFdbkPred;
            // , vntBiasValue As Variant
            double dblPropValue;
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            
            var res = "";
            // vntBiasValue = Null
            if (enumDebugLevel == DebugLevels.High)
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, curblend.strName, "UPDATE_BLEND_PROPS_VALUE",
                                                 "", "", "", "", res);
            }

            //  Get all properties
            List<AbcBlendProps> AllBlendProps = await _repository.GetAllBlendProps(curblend.lngID);

            foreach (var BlendProp in AllBlendProps)
            {
                vntPropID1 = BlendProp.PropId;

                List<AbcBlendIntervalProps> GetFdbackPredList = await _repository.GetFdbackPred(curblend.lngID);
                
                if (GetFdbackPredList.Count() > 0)
                {
                    vntIntvNum = (int)GetFdbackPredList[0].Sequence;
                    // pick the oldest interval with updated anz results
                    List<AbcBlendIntervalProps> GetFdbackPredListFltrd = GetFdbackPredList.Where< AbcBlendIntervalProps >(row => row.Sequence == vntIntvNum).ToList();
                    List<AbcBlendIntervalProps> GetFdbackPredObj = GetFdbackPredListFltrd.Where<AbcBlendIntervalProps>(row => row.PropId == vntPropID1).ToList();
                    
                    if (GetFdbackPredObj.Count() > 0)
                    {
                        vntFdbkPred = GetFdbackPredObj[0].FeedbackPred;
                        
                        if (vntFdbkPred != null)
                        {                           
                            await _repository.SetBlendPropsValue(vntFdbkPred,curblend.lngID,vntPropID1);                            
                        }

                    }
                }               
            }
            return 0;       
        }

        //  Get the lowest interval that corresponds to a given blend date range
        private async Task<string> GetHighLowSequenceDateRange(string strBlendID, DateTime dteStartStopDate)
        {           
            bool blnRepeat;
            
            string strQuery;

            double rtrnData = await _repository.GetAbcBlendIntervalSequence(strBlendID, dteStartStopDate);
            
            return rtrnData.ToString();
       
        }

        //  This sub calc and updates the Avg Anzr value to be compared with the composite value
        //               in abc_blend_sample_props.anzr_value
        private async Task<int> CalcAvgAnzProp(int intStartInterval, int intStopInterval, double lngBlendId, string strSampleName, int intPrdgrpID)
        {
            int intNum;
            int intLWCalcId;
            int intGRAV;
            int intAPI;
            int intJ;
            double lngPropID;
            double lngMatId;
            double dblAvgAnzrProp;
            double dblCompIntvVol;
            double dblDensity;
            double dblAnzrRes;
            double dblTotalCompIntvVol;
            double dblTotalIntDensity;
            double dblIntVarValue;
            double dblAvgVarValue;
            double dblAvgDensity = 0;
            double dblFBPred;
            double dblSPPred;
            double dblVarRes = 0;
            string strUsageName;
            string strMatName;
            string strCalcName;
            string strPropName = "";
            string strPropUnit = "";
            string strSampleField;
            string strVarName;

            // RW 14-Oct-16 Gasoline Ethanol blending
            int intGRAVETOH = 0;
            // GRAV_ETOH prop id
            string strGRAVUnit = "";
            // GRAV/GRAV_ETOH units name
            string strAPIUnit;
            // API units name
            int intEtohId = 0;
            // ETOH prop id
            double dblIntvEtoh = 0;
            // ETOH property value for interval
            double dblTotIntDensEtohFree = 0;
            // Total ETOH-free density for intvs from start to stop
            double dblTotCompIntVolEtohFree = 0;
            // Total ETOH-free component vols for intvs from start to stop
            // RW 14-Oct-16 Gasoline Ethanol blending
            // TODO: On Error GoTo Warning!!!: The statement is not translatable            

            intGRAV = (int)(await _repository.GetPropertyID("GRAV")).PropId;

            PropNameModel APIData = await _repository.GetPropertyID("API");
            intAPI = Convert.ToInt32(APIData.PropId);

            // RW 14-Oct-16 Gasoline Ethanol blending
            strAPIUnit = APIData.UnitsName;

            if ((gblnEthanolBlend == true))
            {
                PropNameModel GRAVData = await _repository.GetPropertyID("GRAV_ETOH");

                intGRAVETOH = Convert.ToInt32(GRAVData.PropId);
                strGRAVUnit = GRAVData.UnitsName;

                // --- RW 25-Jan-17 Gasoline Ethanol blending remedial ---
                // ABCdataEnv.cmdGetPropertyID "ETOH"
                intEtohId = (int)(await _repository.GetPropertyID("ETOH_ETOH")).PropId;

            }

            // RW 14-Oct-16 Gasoline Ethanol blending
            int EtohAnzIntPropCount = 0;
            List<AbcBlendIntervalProps> EtohAnzIntPropList = new List<AbcBlendIntervalProps>();
            if ((gblnEthanolBlend == true))
            {
                // Get ETOH (ETOH_ETOH RW 25-Jan-17) property ANZ_RES, FEEDBACK_PRED, & SETPOINT_PRED value for intervals from start to stop
                EtohAnzIntPropList = await _repository.GetEtohAnzIntProp(lngBlendId, intStartInterval, intStopInterval, intEtohId);
            }

            // RW 14-Oct-16 Gasoline Ethanol blending
            List<BldSampleProps> BldSamplePropsList = await _repository.GetBldSampleProps(lngBlendId, strSampleName);

            foreach (BldSampleProps BldSamplePropsObj in BldSamplePropsList)
            {
                lngPropID = BldSamplePropsObj.PropId;

                // get the calc routine
                strCalcName = "LINEAR VOL";
                List<PropCalcId> PropCalcIdList = await _repository.GetPropCalcId(intPrdgrpID, lngPropID);

                if (PropCalcIdList.Count() > 0)
                {
                    strCalcName = PropCalcIdList[0].CalcName;
                }

                // Get The property units name for Viscosity
                // In the near future this function will be implemented for all props
                List<PropNameModel> PropNameList = await _repository.GetPropName(lngPropID);

                if (PropNameList.Count() > 0)
                {
                    strPropName = PropNameList[0].PropName;
                    strPropUnit = PropNameList[0].UnitsName;
                }

                // Make a loop of three elements to calc Anzr val=1, FB pred=2, SP pred=3
                for (intJ = 1; (intJ <= 3); intJ++)
                {
                    //  Initialize the density value
                    dblTotalIntDensity = 0;
                    
                    dblTotalCompIntvVol = 0;
                    dblIntVarValue = 0;
                    dblCompIntvVol = 0;
                    strSampleField = "";
                    strVarName = "";
                    if ((gblnEthanolBlend == true))
                    {
                        dblTotIntDensEtohFree = 0;
                        dblTotCompIntVolEtohFree = 0;
                        // Position at start interval
                        // position at start interval
                    }

                    // RW 14-Oct-16 Gasoline Ethanol blending
                    for (intNum = intStartInterval; (intNum <= intStopInterval); intNum++)
                    {
                        // This calc should be done for abc_blend_sample_props.prop_id that has an analyzer value within the same
                        // interval range
                        List<AbcBlendIntervalProps> AnzIntPropData = await _repository.GetAnzIntProp(lngBlendId, intNum, lngPropID);

                        if (AnzIntPropData.Count() > 0)
                        {
                            dblAnzrRes = Convert.ToDouble(AnzIntPropData[0].AnzRes);
                            dblFBPred = Convert.ToDouble(AnzIntPropData[0].FeedbackPred);
                            dblSPPred = Convert.ToDouble(AnzIntPropData[0].SetpointPred);
                            // Calculate the anzr, fb_pred or Sp pred
                            switch (intJ)
                            {
                                case 1:
                                    dblVarRes = dblAnzrRes;
                                    strVarName = "ANZ_RES";
                                    strSampleField = "ANZ_VALUE";
                                    break;
                                case 2:
                                    dblVarRes = dblFBPred;
                                    strVarName = "FEEDBACK_PRED";
                                    strSampleField = "FEEDBACK";
                                    break;
                                case 3:
                                    dblVarRes = dblSPPred;
                                    strVarName = "SETPOINT_PRED";
                                    strSampleField = "SETPOINT_PRED";
                                    break;
                            }
                            if (dblVarRes != -1)
                            {
                                // get density
                                // RW 14-Oct-16 Gasoline Ethanol blending
                                // If strPropName = "API" And dblVarRes <> -131.5 Then
                                if (((strPropName == "API")
                                            && (strPropUnit == "APIGRAV")))
                                {
                                    dblVarRes = await _repository.GetConvValue(dblVarRes, "APIGRAV", "SPECGRAV");
                                    // Make the calc routine to Linear by volume
                                    strCalcName = "LINEAR VOL";
                                }

                                if (((strPropName == "D_VISC")
                                            || (strPropName == "F_VISC")))
                                {
                                    if ((strPropName == "D_VISC"))
                                    {
                                        // vntPropID   D_VISC
                                        // Convert the ANZR RES to same CST for viscosity
                                        dblVarRes = await _repository.GetConvValue(dblVarRes, strPropUnit, "CST@40C");
                                    }
                                    else if ((strPropName == "F_VISC"))
                                    {
                                        // vntPropID  F_VISC
                                        // Convert the ANZR RES to same CST for viscosity
                                        dblVarRes = await _repository.GetConvValue(dblVarRes, strPropUnit, "CST@50C");
                                    }

                                }

                                // Get the Interval volumes for every one of the comps
                                List<CompIntVols> CompIntVolsList = await _repository.CompIntVols(lngBlendId, intNum);

                                dblCompIntvVol = 0;
                                foreach (CompIntVols CompIntVolsObj in CompIntVolsList)
                                {
                                    strMatName = CompIntVolsObj.Name;
                                    lngMatId = await _repository.GetMatId(strMatName);

                                    // get the Usage Name for the given blend Component
                                    strUsageName = await GetBldMatUsage(lngBlendId, lngMatId);
                                    if ((strUsageName != "ADDITIVE"))
                                    {
                                        // get the Sum(Int Comp Vol) in the whole range of intervals to calc the average recipe for composite/spot sample
                                        dblCompIntvVol = (dblCompIntvVol + Convert.ToDouble(CompIntVolsObj.Volume));
                                    }
                                }

                                // RW 14-Oct-16 Gasoline Ethanol blending
                                if ((gblnEthanolBlend == true))
                                {
                                    // Get the ETOH (ETOH_ETOH RW 25-Jan-17) anz_res/feedback_pred/setpoint_pred value for the interval
                                    if (strVarName == "ANZ_RES")
                                    {
                                        dblIntvEtoh = (EtohAnzIntPropList[EtohAnzIntPropCount].AnzRes == null) ? Convert.ToDouble(EtohAnzIntPropList[EtohAnzIntPropCount].FeedbackPred) :
                                            Convert.ToDouble(EtohAnzIntPropList[EtohAnzIntPropCount].AnzRes);
                                    }
                                    else if (strVarName == "SETPOINT_PRED")
                                    {
                                        dblIntvEtoh = (EtohAnzIntPropList[EtohAnzIntPropCount].SetpointPred == null) ? Convert.ToDouble(EtohAnzIntPropList[EtohAnzIntPropCount].FeedbackPred) :
                                            Convert.ToDouble(EtohAnzIntPropList[EtohAnzIntPropCount].SetpointPred);
                                    }
                                    else if (strVarName == "FEEDBACK_PRED")
                                    {
                                        dblIntvEtoh = Convert.ToDouble(EtohAnzIntPropList[EtohAnzIntPropCount].FeedbackPred);
                                    }

                                }

                                // RW 14-Oct-16 Gasoline Ethanol blending
                                if ((strCalcName == "LINEAR WT"))
                                {
                                    // get dest density
                                    dblDensity = 1;

                                    // Get Density from abc_blend_interval_Props as nvl(anz_res, feedback_pred).
                                    // confirm this approach
                                    List<PropNameModel> AbcBlendIntervalPropsdata = await _repository.GetAbcBlendIntervalPropsdata(strVarName, lngBlendId, intNum);

                                    if (AbcBlendIntervalPropsdata.Count > 0)
                                    {
                                        List<PropNameModel> AbcBlendIntervalPropsdataFlt = new List<PropNameModel>();
                                        // RW 14-Oct-16 Gasoline Ethanol blending
                                        if (((gblnEthanolBlend == false) || (strPropName.Substring((strPropName.Length - 4)) != "ETOH")))
                                        {
                                            // Non-Ethanol blend or Ethanol blend and XXX property
                                            // RW 14-Oct-16 Gasoline Ethanol blending
                                            AbcBlendIntervalPropsdataFlt = AbcBlendIntervalPropsdata.Where<PropNameModel>(row => row.PropId == intAPI).ToList();

                                            if (AbcBlendIntervalPropsdataFlt.Count() > 0)
                                            {
                                                // RW 14-Oct-16 Gasoline Ethanol blending
                                                // If .Fields("VALUE").Value <> -131.5 Then _
                                                //dblDensity = await _repository.GetConvValue(AbcBlendIntervalPropsdataFlt[0].Value, "APIGRAV", "SPECGRAV");
                                                if ((strAPIUnit == "APIGRAV"))
                                                {
                                                    dblDensity = await _repository.GetConvValue(Convert.ToDouble(AbcBlendIntervalPropsdataFlt[0].Value), "APIGRAV", "SPECGRAV");
                                                }
                                            }
                                            else
                                            {
                                                AbcBlendIntervalPropsdataFlt = AbcBlendIntervalPropsdata.Where<PropNameModel>(row => row.PropId == intGRAV).ToList();

                                                if (AbcBlendIntervalPropsdataFlt.Count() > 0)
                                                {
                                                    dblDensity = Convert.ToDouble(AbcBlendIntervalPropsdataFlt[0].Value);

                                                    // RW 14-Oct-16 Gasoline Ethanol blending
                                                    if ((strGRAVUnit == "APIGRAV"))
                                                    {
                                                        dblDensity = await _repository.GetConvValue(dblDensity, "APIGRAV", "SPECGRAV");
                                                    }

                                                    // RW 14-Oct-16 Gasoline Ethanol blending
                                                }

                                            }

                                            // RW 14-Oct-16 Gasoline Ethanol blending
                                        }
                                        else
                                        {
                                            // Ethanol blend and property = ETOH or xxx_ETOH
                                            AbcBlendIntervalPropsdataFlt = AbcBlendIntervalPropsdata.Where<PropNameModel>(row => row.PropId == intGRAVETOH).ToList();

                                            if (AbcBlendIntervalPropsdataFlt.Count() > 0)
                                            {
                                                dblDensity = Convert.ToDouble(AbcBlendIntervalPropsdataFlt[0].Value);
                                                if ((strGRAVUnit == "APIGRAV"))
                                                {
                                                    dblDensity = await _repository.GetConvValue(dblDensity, "APIGRAV", "SPECGRAV");
                                                }
                                            }
                                        }
                                        // RW 14-Oct-16 Gasoline Ethanol blending
                                    }

                                    if ((gblnEthanolBlend == false) || (strPropName.Substring((strPropName.Length - 4)) == "ETOH"))
                                    {
                                        //  eg. SULF_ETOH, OXYG_ETOH
                                        dblIntVarValue = (dblIntVarValue + (dblCompIntvVol * (dblVarRes * dblDensity)));
                                        // Calc Avg density for all range of intervals.  Density LINEAR VOL
                                        dblTotalIntDensity = (dblTotalIntDensity + (dblCompIntvVol * dblDensity));
                                        // RW 14-Oct-16 Gasoline Ethanol blending
                                    }
                                    else
                                    {
                                        // XXX property of Ethanol blend eg. SULF, OXYG
                                        // Use Etoh-free component interval volume
                                        dblIntVarValue = (dblIntVarValue + (dblCompIntvVol * (dblVarRes * (dblDensity * ((100 - dblIntvEtoh) / 100)))));
                                        // Calc Avg Etoh-free density for all range of intervals.  Density LINEAR VOL
                                        dblTotIntDensEtohFree = (dblTotIntDensEtohFree + (dblCompIntvVol * (dblDensity * ((100 - dblIntvEtoh) / 100))));
                                    }

                                }
                                else
                                {
                                    // RW 14-Oct-16 Gasoline Ethanol blending
                                    // LINEAR BY VOL addition of prop
                                    // Calc Intvs Vol * Anz res during that interval
                                    // RW 14-Oct-16 Gasoline Ethanol blending
                                    // Line below added
                                    // --- RW 25-Jan-17 Gasoline Ethanol blending remedial ---
                                    // ETOH_ETOH property added to comment below
                                    if ((gblnEthanolBlend == false) || (strPropName.Substring((strPropName.Length - 4)) == "ETOH"))
                                    {
                                        //  eg. AROM_ETOH, RON_ETOH, ETOH, ETOH_ETOH
                                        dblIntVarValue = (dblIntVarValue + (dblCompIntvVol * dblVarRes));
                                        // RW 14-Oct-16 Gasoline Ethanol blending
                                    }
                                    else
                                    {
                                        //  XXX property of Ethanol blend eg. AROM, RON
                                        // Use ETOH-free interval volume
                                        dblIntVarValue = (dblIntVarValue + (dblCompIntvVol * (dblVarRes * ((100 - dblIntvEtoh) / 100))));
                                    }
                                }
                                // RW 14-Oct-16 Gasoline Ethanol blending
                                // Save total comp interval volumes
                                dblTotalCompIntvVol = (dblTotalCompIntvVol + dblCompIntvVol);
                                // RW 14-Oct-16 Gasoline Ethanol blending
                                if ((gblnEthanolBlend == true))
                                {
                                    // Save total component ETOH-free interval volumes
                                    dblTotCompIntVolEtohFree = (dblTotCompIntVolEtohFree + (dblCompIntvVol * ((100 - dblIntvEtoh) / 100)));
                                }

                                // RW 14-Oct-16 Gasoline Ethanol blending
                            }// If anzr res <>NULL_
                        }// Prop

                        // RW 14-Oct-16 Gasoline Ethanol blending
                        if ((gblnEthanolBlend == true))
                        {
                            // Position to next interval for Etoh property value
                            if ((EtohAnzIntPropList.Count() - 1) < EtohAnzIntPropCount)
                                EtohAnzIntPropCount++;
                        }

                        // RW 14-Oct-16 Gasoline Ethanol blending
                    }
                    if (((dblTotalCompIntvVol != 0) && (dblIntVarValue != 0)))
                    {
                        if ((strCalcName == "LINEAR WT"))
                        {
                            // RW 14-Oct-16 Gasoline Ethanol blending
                            // Line below added
                            if (((gblnEthanolBlend == false)
                                        || (strPropName.Substring((strPropName.Length - 4)) == "ETOH")))
                            {
                                //  eg. OXYG_ETOH, SULF_ETOH
                                // calc avg density
                                if ((dblTotalIntDensity != 0))
                                {
                                    dblAvgDensity = (dblTotalIntDensity / dblTotalCompIntvVol);
                                }

                                // LINEAR BY WT addition of prop
                                // Calc the average anzr value in the whole range of intervals
                                dblAvgVarValue = (dblIntVarValue / (dblTotalCompIntvVol * dblAvgDensity));
                            }
                            else
                            {
                                // XXX property of Ethanol blend
                                // calc avg density
                                if ((dblTotIntDensEtohFree != 0))
                                {
                                    dblAvgDensity = (dblTotIntDensEtohFree / dblTotCompIntVolEtohFree);
                                }

                                // LINEAR BY WT addition of prop
                                // Calc the average anzr value in the whole range of intervals
                                dblAvgVarValue = (dblIntVarValue
                                            / (dblTotCompIntVolEtohFree * dblAvgDensity));
                            }

                            // RW 14-Oct-16 Gasoline Ethanol blending
                        }
                        else
                        {
                            // LINEAR BY VOL addition of prop
                            // RW 14-Oct-16 Gasoline Ethanol blending
                            // Line below added
                            // --- RW 25-Jan-17 Gasoline Ethanol blending remedial ---
                            // ETOH_ETOH property added to comment below
                            if (((gblnEthanolBlend == false) || (strPropName.Substring((strPropName.Length - 4)) == "ETOH")))
                            {
                                //  eg. AROM_ETOH, RON_ETOH, ETOH, ETOH_ETOH
                                dblAvgVarValue = (dblIntVarValue / dblTotalCompIntvVol);
                                // RW 14-Oct-16 Gasoline Ethanol blending
                            }
                            else
                            {
                                //  XXX property of Ethanol blend eg. AROM, RON
                                // Use total ETOH-free interval volume
                                dblAvgVarValue = (dblIntVarValue / dblTotCompIntVolEtohFree);
                            }

                            // RW 14-Oct-16 Gasoline Ethanol blending
                        }

                        // RW 14-Oct-16 Gasoline Ethanol blending
                        // If strPropName = "API" Then
                        if ((((strPropName == "API") && (strAPIUnit == "APIGRAV")) || (strPropUnit == "APIGRAV")))
                        {
                            // re-converting SG  to API after volumetric addition
                            dblAvgVarValue = await _repository.GetConvValue(dblAvgVarValue, "SPECGRAV", "APIGRAV");
                        }

                        await _repository.SetBlendSampleProps(strSampleField, dblAvgVarValue, lngBlendId, strSampleName, lngPropID);                        
                    }
                }
            }
            return 0;
        }

        // Process samples at any cycle of BMon in Active or Paused states
        private async Task<RetStatus> ProcessSamples(int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend, DebugLevels enumDebugLevel)
        {                                  
            string strStartInterval;
            string strStopInterval;
            string strMatName;            
            string strSampleName;
            string strSampleType;
            string strProcessSampleFlag;
            string strUsageName;
            double sngStartVolume;
            double sngStopVolume;
            DateTime dteStartTime;
            DateTime dteStopTime;
            bool blnSkipGAMS;
            bool blnMatchBiasCurr;
            bool blnBiasOverrideFlag;
            double lngPropID;
            double lngMatId;
            double?[,] arCompositeIntVol;
            double dblSumVol;
            double[] arCompIntVol = new double[0];
            int intNPeriod;
            int intSampleIndex;
            int intNSamples;
            int intMatchIntv= 0;
            int intSampleInterval;
            int intNComps;
            int intCalcs;
            int intTempStart;
            int intTempStop;
            int intTempNPeriod;
            int intNP;
            int intNC;
            const int intMaxNIntv = 20;
            int intI;
            int intNum;
            RetStatus gintOptResult;
            bool blnUsedSample;
            var res = "";
            
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            RetStatus rtrnData = RetStatus.FAILURE;
            //  If proj default allow sampling flag is NO, then skip sampling processing
            if (gProjDfs.strAllowSCSampling == "YES")
            {
                // Composite sampling.  If there is data in ABC_BLENDS_SAMPLE_PROPS call GAMS to calculate the
                // FB Pred for that composite/spot sample.
                List<BldSampleProps> CompositeSpotSampleList = await _repository.CompositeSpotSample(curblend.lngID);
                List<BldSampleProps> CompositeSpotSampleListFlt = new List<BldSampleProps>();
                intNSamples = 0;
                intSampleIndex = 0;
                blnBiasOverrideFlag = false;
                if (CompositeSpotSampleList.Count() > 0)
                {
                    // save the number of samples
                    intNSamples = CompositeSpotSampleList.Count();
                    
                    // if BiasOverride Flag is NO, then process only the latest sample (spot/Composite)
                    if ((curblend.strBiasOverrideFlag == "NO"))
                    {
                        if ((gProjDfs.strLimsSampleStartStopType == "VOLUME"))
                        {
                            // sort the rs by start_volume DESC
                            CompositeSpotSampleList = CompositeSpotSampleList.OrderByDescending(row => row.StartVolume).ToList();
                        }
                        else
                        {
                            // Sort the rs by START_DATE to process the earliest sample, when several samples exist
                            CompositeSpotSampleList = CompositeSpotSampleList.OrderByDescending(row => row.StartDate).ToList();
                        }

                        // Filter the rs by SAMPLE_NAME to get the latest - the last sample entered in the system
                        CompositeSpotSampleList = CompositeSpotSampleList.Where<BldSampleProps>(row => row.SampleName == CompositeSpotSampleList[0].SampleName)
                                                        .ToList();
                    }
                    else
                    {
                        // if BiasOverride Flag is YES,
                        // then all samples (COM/SPOT) will be processed one by one, starting from
                        // the oldest to latest samples where at least one sample property is NOT USED
                        // Filter the rs by USED_FLAG=NO to include all unused samples
                        if ((gProjDfs.strLimsSampleStartStopType == "VOLUME"))
                        {
                            // sort the rs by start_volume ASC                            
                            CompositeSpotSampleList = CompositeSpotSampleList.OrderBy(row => row.StartVolume).ToList();
                        }
                        else
                        {
                            // Sort the rs by START_DATE to process the earliest sample, when several samples exist
                            CompositeSpotSampleList = CompositeSpotSampleList.OrderBy(row => row.StartDate).ToList();                            
                        }

                    }

                    // BiasOverride Flag
                }

                // Samples EOF
                // check if composition records exist.  If not skip this calc completely
                // If multiple sample exist for a blend then process all of them in one cycle of Bmon
                //----------------bebug----------- CompositeSpotSampleListFlt/CompositeSpotSampleList
                foreach (BldSampleProps CompositeSpotSampleObj in CompositeSpotSampleList)
                {                               
                    // Record the sample index
                    intSampleIndex = (intSampleIndex + 1);
                    dteStartTime = (CompositeSpotSampleObj.StartDate == null)?cdteNull: Convert.ToDateTime(CompositeSpotSampleObj.StartDate);
                    dteStopTime = (CompositeSpotSampleObj.StopDate == null) ? cdteNull : Convert.ToDateTime(CompositeSpotSampleObj.StopDate);
                    sngStartVolume = (CompositeSpotSampleObj.StartVolume == null) ? -1 : Convert.ToDouble(CompositeSpotSampleObj.StartVolume);
                    sngStopVolume = (CompositeSpotSampleObj.StopVolume == null) ? -1 : Convert.ToDouble(CompositeSpotSampleObj.StopVolume);
                    // get the sample type (spot/composite)
                    strSampleType = (CompositeSpotSampleObj.SampleType == null) ? "SPOT" : CompositeSpotSampleObj.SampleType;
                    strSampleName = CompositeSpotSampleObj.SampleName;
                    strProcessSampleFlag = CompositeSpotSampleObj.ProcessSampleFlag;
                    // Find the interval range (for composite) or interval number (for spot) where the sample belong to
                    // strIntervals = GetStartStopInt(curblend.lngID, sngStartVolume, sngStopVolume, dteStartTime, dteStopTime)
                    if ((gProjDfs.strLimsSampleStartStopType == "DATE"))
                    {
                        strStartInterval = await GetHighLowSequenceDateRange(curblend.lngID.ToString(), dteStartTime);
                        strStopInterval = await GetHighLowSequenceDateRange(curblend.lngID.ToString(), dteStopTime);
                    }
                    else
                    {
                        strStartInterval = Convert.ToString(await _repository.GetHighLowSequenceVolRange(curblend.lngID.ToString(), sngStartVolume.ToString()));
                        strStopInterval = Convert.ToString(await _repository.GetHighLowSequenceVolRange(curblend.lngID.ToString(), sngStopVolume.ToString()));
                    }

                    if (((strStartInterval != "")
                                && (strStopInterval != "")))
                    {
                        // Check that at least one abc_blend_interval_props.biascalc_current is "SPOT/COMPOSITE" type for
                        // intervals between stoptime until the starttime of the composite/spot sample
                        // Create a loop of sample props to check the blend interval props.calctype_current
                        // If at least one sample prop satisfies that calctype_current ="SPOT" OR "COMPOSITE", then
                        // proceed and do LINEPROP, but update bias only for props that have the same biascalc_type (composite or spot) 
                        // get the Blend Sample Props for the specified sample name
                        // BiasOverride Flag means that the user
                        // from the GUI (ABC Displays) will set the biascalc_current from ANZR to
                        // COMPOSITE or SPOT samples, therefore BMon will only process COM/SPOT samples types.
                        // There is not automatic biasoverrrite from ANZR to COM/SPOT over the entire blend
                        //                         If curblend.strBiasOverrideFlag = "NO" Then
                        List<BldSampleProps> BldSamplePropsList = await _repository.GetBldSampleProps(curblend.lngID,strSampleName);
                        
                        blnMatchBiasCurr = false;
                        foreach (BldSampleProps BldSamplePropsObj in BldSamplePropsList)
                        {
                            lngPropID = BldSamplePropsObj.PropId;
                            // check if for at least one prop the biascalc_current type =COMPOSITE/SPOT
                            // between starttime until the stoptime of the composite/spot sample
                            // For SPOT check a range of three intervals
                            if ((strSampleType == "SPOT"))
                            {
                                intMatchIntv = Convert.ToInt32(await ChkIntBiasCalcCurr(strSampleType, (Convert.ToInt32(strStartInterval) - 1), curblend.lngID, lngPropID, int.Parse((strStopInterval + 1))));
                            }
                            else
                            {
                                intMatchIntv = Convert.ToInt32(await ChkIntBiasCalcCurr(strSampleType, Convert.ToInt32(strStartInterval), curblend.lngID, lngPropID, int.Parse(strStopInterval)));
                            }

                            if ((intMatchIntv != -1))
                            {
                                // if at least one prop satisfies the condition: in the specified interval range, biascalc_current is = required type
                                // then leave the DO Loop and continue with calcs
                                blnMatchBiasCurr = true;
                                break; //Warning!!! Review that break works as 'Exit Do' as it could be in a nested instruction like switch
                            }
                            
                        }

                        //Update the avg anzr prop
                        await CalcAvgAnzProp(Convert.ToInt32(strStartInterval), Convert.ToInt32(strStopInterval),curblend.lngID,strSampleName,Convert.ToInt32(vntBldrsData[intBldrIdx].PrdgrpId));
                        
                        //Used (true) or new(Used) sample
                        blnUsedSample = await _repository.GetNewUsedSample(curblend.lngID, strSampleName);
                        //Reset the message flag to log messages for new samples
                        if ((gblnSampleMsgLogged[intBldrIdx] && (curblend.intCurIntv > gArPrevBldData[intBldrIdx].intCurIntv)))
                        {
                            gblnSampleMsgLogged[intBldrIdx] = false;
                        }

                        
                        //log a msg when the sample is new and it is located at the same interval
                        if (((gblnSampleMsgLogged[intBldrIdx] == false) && ((int.Parse(strStartInterval) == curblend.intCurIntv) && !blnUsedSample)))
                        {
                            // WARNING IN BLEND ^1 - SAMPLE ^2 TAKEN IN AN OPEN INTERVAL (^2)
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN105), programName, "BL-" + curblend.lngID, curblend.strName, strSampleName,
                                                strStartInterval, "", "", "", res);                            
                            gblnSampleMsgLogged[intBldrIdx] = true;
                        }
                        
                        // if no matching Bias calc_type current in the range of intervals or
                        // if process_sample_flag is set
                        if (((blnMatchBiasCurr == false)
                                    || (strProcessSampleFlag == "NO")))
                        {
                            goto NEXT_SAMPLE;
                        }
                        
                        //Reset the gblnSampleMsgLogged flag for the next sample
                        if ((gblnSampleMsgLogged[intBldrIdx] == true))
                        {
                            gblnSampleMsgLogged[intBldrIdx] = false;
                        }

                        // Get the Interval volumes for every one of the comps.
                        List<CompIntVols> CompIntVolsList = await _repository.CompIntVols(curblend.lngID,Convert.ToInt32(strStartInterval));
                        
                        intI = 0;
                        dblSumVol = 0;
                        foreach (CompIntVols CompIntVolsObj in CompIntVolsList)
                        {                        
                            // get the mat_id of the given mat_name
                            strMatName = CompIntVolsObj.Name;
                            lngMatId = await _repository.GetMatId(strMatName);
                                                        
                            // get the Usage Name for the given blend Component
                            strUsageName = await GetBldMatUsage(curblend.lngID, lngMatId);
                            if ((strUsageName != "ADDITIVE"))
                            {
                                //Exclude additives to pass comps to opt
                                Array.Resize(ref arCompIntVol, intI);
                                
                                // get the Sum(Int Comp Vol) in the SPOT interval to calc recipe
                                arCompIntVol[intI] = Convert.ToDouble(CompIntVolsObj.Volume);
                                dblSumVol = (dblSumVol + ((arCompIntVol[intI] == null)? 0: Convert.ToDouble(arCompIntVol[intI])));
                                intI = (intI + 1);
                            }
                        }

                        //Store the real number of comps, excluding the additives
                        //For composite get the number of comps from the start interval
                        intNComps = intI;
                        // Check for composite or spot sample
                        if ((strSampleType == "SPOT"))
                        {
                            // strStartInterval = strStopInterval
                            if ((intNComps > 1))
                            {
                                //  if dblSumVol=0: BLEND ^1: TOTAL COMPONENT INTERVAL VOLUME IS ZERO FOR INTERVAL ^2.  LINEPROP CALC WILL NOT BE PERFORMED
                                if ((dblSumVol == 0))
                                {
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN94), programName, "BL-" + curblend.lngID, curblend.strName, strStartInterval,
                                               "", "", "", "", res);                                   
                                }

                                // if calcprop_flag="YES" in abc_blenders, then proccess LINEPROP
                                if ((vntBldrsData[intBldrIdx].CalcpropFlag == "YES") && ((curblend.sngCurVol != 0) && (dblSumVol > 0)))
                                {
                                    // call MODEL_LOCAL to do line property calc for SPOT sample interval
                                    gintOptResult = RetStatus.FAILURE;
                                    //PENDING
                                    await commonGAMSOptimizer.Call_Modal_Local("GASOLINE", connectionString, (ABBAdvancedBlendOptimizer.Enum.GAMSCalcTypes)GAMSCalcTypes.LINEPROP,(long)curblend.lngID,Convert.ToInt32(strStartInterval),(long)arCompIntVol[0], 
                                        (ABBAdvancedBlendOptimizer.Enum.DebugLevels)enumDebugLevel, Convert.ToInt32(strStopInterval), (ABBAdvancedBlendOptimizer.Enum.RetStatus)gintOptResult);
                                    

                                    // This array(2) will return start and stop intervals from the bias calc within the CalcBlend Sub
                                    gintStartStopIntv[(int)StartStop.STRT] = -1;
                                    gintStartStopIntv[(int)StartStop.STP] = -1;
                                    //  The matching interval should be used only to process the sample in the range, but the
                                    // bias, fb pred properties should be obtained from the StartInterval
                                    intMatchIntv = Convert.ToInt32(strStartInterval);
                                    // Process the BIAS for SPOT Sample. Pass also the matching interval (intv where biascalc_current is SPOT)
                                    strSampleName = CompositeSpotSampleObj.SampleName;
                                    await CalcBias(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, "SPO", strSampleName, Convert.ToInt32(strStartInterval),
                                        Convert.ToInt32(strStopInterval), intMatchIntv);
                                    // Set a boolean to set the override flag = NO after bias calc of all prop samples
                                    if ((curblend.strBiasOverrideFlag == "YES"))
                                    {
                                        blnBiasOverrideFlag = true;
                                    }

                                    //  This array(2) will return start and stop intervals from the bias calc within the CalcBlend Sub
                                    if (gintStartStopIntv[(int)StartStop.STRT] != -1)
                                    {
                                        strStartInterval = gintStartStopIntv[(int)StartStop.STRT].ToString();
                                    }

                                    if (gintStartStopIntv[(int)StartStop.STP] != -1)
                                    {
                                        strStopInterval = gintStartStopIntv[(int)StartStop.STP].ToString();
                                    }

                                    if ((Convert.ToInt32(strStartInterval) != -1) && Convert.ToInt32(strStopInterval) != -1)
                                    {
                                        // RECALCulate the FB Pred from Spot StartInterval to last Interval
                                        // redim the array to hold period,int vol
                                        // For SPOT sample: get StopInterval
                                        if (((curblend.intCurIntv > gArPrevBldData[intBldrIdx].intCurIntv)
                                                    && (gintStartStopIntv[(int)StartStop.STP] > gArPrevBldData[intBldrIdx].intCurIntv)))
                                        {
                                            strStopInterval = gArPrevBldData[intBldrIdx].intCurIntv.ToString();
                                        }

                                        if (Convert.ToInt32(strStartInterval) <= 0)
                                        {
                                            strStartInterval = (1).ToString();
                                        }

                                        if (Convert.ToInt32(strStopInterval) <= 0)
                                        {
                                            strStopInterval = (1).ToString();
                                        }

                                        intNPeriod = Convert.ToInt32(strStopInterval) - Convert.ToInt32(strStartInterval);
                                        //  If the number of intervals to be included in the recalculation of FB Predicted Value
                                        // is greater than (12-20), then split the recalculation in multiples of 12-20 intervals.
                                        // Gams optimization problem becomes too big and the Solver reaches the maximun number of iterations or it takes
                                        // a long time (up to 5 minutes) to process such a big number of intervals
                                        intCalcs = 0;
                                        intTempStart = Convert.ToInt32(strStartInterval);
                                        intTempStop = Convert.ToInt32(strStopInterval);
                                        intTempNPeriod = intNPeriod;
                                        // find the numbers of times it takes to calc all intervals
                                        if ((intNPeriod > intMaxNIntv))
                                        {
                                            for (intI = 1; (intI <= intNPeriod); intI++)
                                            {
                                                if ((intNPeriod > (intI * intMaxNIntv)))
                                                {
                                                    intCalcs = (intCalcs + 1);
                                                }
                                                else
                                                {
                                                    // if number found, leave the for loop
                                                    break;
                                                }

                                            }

                                        }

                                        for (intNC = 0; (intNC <= intCalcs); intNC++)
                                        {
                                            if ((intCalcs > 0))
                                            {
                                                intTempStart = Convert.ToInt32(strStartInterval)+ (intNC * intMaxNIntv);
                                                intTempStop = (intTempStart + (intMaxNIntv - 1));
                                                if (intTempStop > Convert.ToInt32(strStopInterval))
                                                {
                                                    intTempStop = Convert.ToInt32(strStopInterval);
                                                }

                                                // Calc the new number of intervals to be calculated
                                                intTempNPeriod = (intTempStop - intTempStart);
                                            }

                                            arCompositeIntVol = new double?[intTempNPeriod, intNComps - 1];
                                            blnSkipGAMS = false;
                                            intNP = 0;
                                            //  Number of intervals(periods) base zero (0)
                                            // Calculate the sum of comp (I) volumes in every one of the range of the interval found
                                            for (intNum = intTempStart; (intNum <= intTempStop); intNum++)
                                            {
                                                // Get the Inteval volumes for every one of the comps
                                                List<CompIntVols> CompIntVolsList1 =  await _repository.CompIntVols(curblend.lngID, intNum);
                                                
                                                intI = 0;
                                                dblSumVol = 0;
                                                foreach (CompIntVols CompIntVolsObj in CompIntVolsList1)                                               
                                                {
                                                    // get the mat_id of the given mat_name
                                                    strMatName = CompIntVolsObj.Name;
                                                    lngMatId =  await _repository.GetMatId(strMatName);
                                                                                                        
                                                    // get the Usage Name for the given blend Component
                                                    strUsageName = await GetBldMatUsage(curblend.lngID, lngMatId);
                                                    if ((strUsageName != "ADDITIVE"))
                                                    {
                                                        // get the Sum(Int Comp Vol) in the whole range of intervals to calc the average recipe for composite/spot sample
                                                        // arCompIntVol(intI) = NVL(arCompIntVol(intI), 0) + ABCdataEnv.rscmdCompIntVols.Fields("VOLUME").Value
                                                        // get an array of (Period,comp interval volumes) for composite samples after composite bias has been calculated
                                                        arCompositeIntVol[intNP, intI] = CompIntVolsObj.Volume;
                                                        dblSumVol = (dblSumVol + ((arCompositeIntVol[intNP, intI] == null)?0: Convert.ToDouble(arCompositeIntVol[intNP, intI])));
                                                        intI = (intI + 1);
                                                    }
                                                }
                                                
                                                if ((dblSumVol == 0))
                                                {
                                                    blnSkipGAMS = true;
                                                }

                                                intNP = (intNP + 1);
                                            }

                                            // Log Msg if dblSumVol=0: BLEND ^1: TOTAL COMPONENT INTERVAL VOLUME IS ZERO FOR INTERVAL ^2.  LINEPROP CALC WILL NOT BE PERFORMED
                                            if ((blnSkipGAMS == true))
                                            {
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN94), programName, "BL-" + curblend.lngID, curblend.strName, (intTempStart + ("-" + intTempStop)),
                                               "", "", "", "", res);                                                
                                            }

                                            // if calcprop_flag="YES" in abc_blenders, then proccess LINEPROP
                                            if ((vntBldrsData[intBldrIdx].CalcpropFlag == "YES") && ((curblend.sngCurVol != 0) && (blnSkipGAMS == false)))
                                            {
                                                // Recalculate the feedback pred. for intervals where BIAS has been updated
                                                // to have the correct TQI when TMMON runs.
                                                // Create one single task (multiperiod) for the GAMS Opt., instead of callig it one by one, which will downgrade performance of this program
                                                // call MODEL_GLOBAL to do line property calculation for the SPOT interval sample
                                                gintOptResult = RetStatus.FAILURE;
                                                
                                                await commonGAMSOptimizer.Call_Modal_Global("GASOLINE", connectionString, (ABBAdvancedBlendOptimizer.Enum.GAMSCalcTypes)GAMSCalcTypes.LINEPROP, (long)curblend.lngID, intTempStart, (long)arCompositeIntVol[0,0],
                                                (ABBAdvancedBlendOptimizer.Enum.DebugLevels)enumDebugLevel, intTempStop, (ABBAdvancedBlendOptimizer.Enum.RetStatus)gintOptResult);

                                                if (gintOptResult == RetStatus.SUCCESS)
                                                {
                                                    //Msg "^1 SAMPLE ^2 HAS BEEN USED IN BLEND ^3 FOR INTERVALS ^4 TO ^5"
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN103), programName, "BL-" + curblend.lngID, strSampleType, strSampleName,
                                              curblend.strName, intTempStart.ToString(), intTempStop.ToString(), "", res);
                                                    
                                                    // Set the function back value
                                                    rtrnData = RetStatus.SUCCESS;
                                                    //set the process_sample_flag to OFF after processing the sample
                                                    await _repository.SetProcessSampleFlag(curblend.lngID);                                                   
                                                }

                                            }

                                            // conditions to perform LINEPROP for multiperiod
                                        }

                                        // perform GAMS calc in multiples of MaxNPeriod
                                    }
                                    else
                                    {
                                        // LOG MSG: ^1 SAMPLE ^2 COULD NOT BE ALLOCATED WITHIN THE INTVL RANGE OF BLEND ^3. BLEND SAMPLE DATA IGNORED
                                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN100), programName, "BL-" + curblend.lngID, strSampleType, strSampleName, curblend.strName,
                                               "", "", "", res);
                                    }// START/STOP intervals = NULL_
                                } // check to perform LINEPROP
                            }// Check for N of comps > 1
                        }
                        else if ((strSampleType == "COMPOSITE"))
                        {
                            // strStartInterval < strStopInterval
                            // This is a COMPOSITE sample
                            // redim the array to hold period,int vol
                            // intNPeriod = strStopInterval - strStartInterval
                            // ReDim arCompositeIntVol(0 To intNPeriod, 0 To intNComps - 1)
                            // redim the comp int vols
                            arCompIntVol = new double[intNComps - 1];
                            blnSkipGAMS = false;
                            for (intNum = Convert.ToInt32(strStartInterval); (intNum <= Convert.ToInt32(strStopInterval)); intNum++)
                            {
                                // Get the Inteval volumes for every one of the comps
                                List<CompIntVols> CompIntVolsList1 = await _repository.CompIntVols(curblend.lngID, intNum);
                                
                                intI = 0;
                                dblSumVol = 0;
                                foreach (CompIntVols CompIntVolsObj in CompIntVolsList1)                                
                                {
                                    // get the mat_id of the given mat_name
                                    strMatName = CompIntVolsObj.Name;
                                    lngMatId = await _repository.GetMatId(strMatName);
                                   
                                    // get the Usage Name for the given blend Component
                                    strUsageName = await GetBldMatUsage(curblend.lngID, lngMatId);
                                    if ((strUsageName != "ADDITIVE"))
                                    {
                                        // get the Sum(Int Comp Vol) in the whole range of intervals to calc the average recipe for composite/spot sample
                                        arCompIntVol[intI] = ((arCompIntVol[intI] == null)?0: arCompIntVol[intI]) + ((CompIntVolsObj.Volume ==null)?0: Convert.ToDouble(CompIntVolsObj.Volume));
                                        // get an array of (Period,comp interval volumes) for composite samples after composite bias has been calculated
                                        // arCompositeIntVol(intNPeriod, intI) = ABCdataEnv.rscmdCompIntVols.Fields("VOLUME").Value
                                        // dblSumVol = dblSumVol + NVL(arCompositeIntVol(intNPeriod, intI), 0)
                                        dblSumVol = (dblSumVol + ((CompIntVolsObj.Volume == null) ? 0 : Convert.ToDouble(CompIntVolsObj.Volume)));
                                        intI = (intI + 1);
                                    }                                    
                                }
                                
                                if ((dblSumVol == 0))
                                {
                                    blnSkipGAMS = true;
                                }

                            }

                            if ((intNComps > 1))
                            {
                                // Log Msg if dblSumVol=0: BLEND ^1: TOTAL COMPONENT INTERVAL VOLUME IS ZERO FOR INTERVAL ^2.  LINEPROP CALC WILL NOT BE PERFORMED
                                if ((blnSkipGAMS == true))
                                {
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN94), programName, "BL-" + curblend.lngID, curblend.strName, (strStartInterval + ("-" + strStopInterval)),
                                               "", "", "", "", res);                                   
                                }

                                // if calcprop_flag="YES" in abc_blenders, then proccess LINEPROP
                                if (((vntBldrsData[intBldrIdx].CalcpropFlag == "YES") && ((curblend.sngCurVol != 0) && (blnSkipGAMS == false))))
                                {
                                    // call MODEL_LOCAL to do line property calculation for the COMPOSITE interval sample
                                    // Pass the average volume of comps in all composite interval range
                                    gintOptResult = RetStatus.FAILURE;
                                                                       
                                    await commonGAMSOptimizer.Call_Modal_Local("GASOLINE", connectionString, (ABBAdvancedBlendOptimizer.Enum.GAMSCalcTypes)GAMSCalcTypes.LINEPROP, (long)curblend.lngID, Convert.ToInt32(strStartInterval), (long)arCompIntVol[0],
                                       (ABBAdvancedBlendOptimizer.Enum.DebugLevels)enumDebugLevel, Convert.ToInt32(strStopInterval), (ABBAdvancedBlendOptimizer.Enum.RetStatus)gintOptResult);

                                    // This array(2) will return start and stop intervals from the bias calc within the CalcBlend Sub
                                    gintStartStopIntv[(int)StartStop.STRT] = -1;
                                    gintStartStopIntv[(int)StartStop.STP] = -1;
                                    
                                    // Process the BIAS for COMPOSITE Sample.
                                    // Pass the first matching interval (intv where biascalc_current is COMPOSITE)
                                    await CalcBias(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, "COM", strSampleName, Convert.ToInt32(strStartInterval),
                                        Convert.ToInt32(strStopInterval), intMatchIntv);
                                    // Set a boolean to set the override flag = NO after bias calc of all prop samples
                                    if ((curblend.strBiasOverrideFlag == "YES"))
                                    {
                                        blnBiasOverrideFlag = true;
                                    }

                                    //This array(2) will return start and stop intervals from the bias calc within the CalcBlend Sub
                                    if (gintStartStopIntv[(int)StartStop.STRT] != -1)
                                    {
                                        strStartInterval = gintStartStopIntv[(int)StartStop.STRT].ToString();
                                    }

                                    if (gintStartStopIntv[(int)StartStop.STP] != -1)
                                    {
                                        strStopInterval = gintStartStopIntv[(int)StartStop.STP].ToString();
                                    }                                                                                                         

                                    if ((Convert.ToInt32(strStartInterval) != -1) && (Convert.ToInt32(strStopInterval) != -1))
                                    {
                                        // RECALCulate the FB Pred from Spot StartInterval to last Interval
                                        // redim the array to hold period,int vol
                                        // For SPOT sample: get StopInterval
                                        if (((curblend.intCurIntv > gArPrevBldData[intBldrIdx].intCurIntv)
                                                    && (gintStartStopIntv[(int)StartStop.STP] > gArPrevBldData[intBldrIdx].intCurIntv)))
                                        {
                                            strStopInterval = gArPrevBldData[intBldrIdx].intCurIntv.ToString();
                                        }

                                        if (Convert.ToInt32(strStartInterval) <= 0)
                                        {
                                            strStartInterval = (1).ToString();
                                        }

                                        if (Convert.ToInt32(strStopInterval) <= 0)
                                        {
                                            strStopInterval = (1).ToString();
                                        }

                                        intNPeriod = Convert.ToInt32(strStopInterval) - Convert.ToInt32(strStartInterval);
                                        // If the number of intervals to be included in the recalculation of FB Predicted Value
                                        // is greater than (12-20), then split the recalculation in multiples of 12-20 intervals.
                                        // Gams optimization problem becomes too big and the Solver reaches the maximun number of iterations or it takes
                                        // a long time (up to 5 minutes) to process such a big number of intervals
                                        intCalcs = 0;
                                        intTempStart = Convert.ToInt32(strStartInterval);
                                        intTempStop = Convert.ToInt32(strStopInterval);
                                        intTempNPeriod = intNPeriod;
                                        // find the numbers of times it takes to calc all intervals
                                        if ((intNPeriod > intMaxNIntv))
                                        {
                                            for (intI = 1; (intI <= intNPeriod); intI++)
                                            {
                                                if ((intNPeriod > (intI * intMaxNIntv)))
                                                {
                                                    intCalcs = (intCalcs + 1);
                                                }
                                                else
                                                {
                                                    // if number found, leave the for loop
                                                    break;
                                                }

                                            }

                                        }

                                        for (intNC = 0; (intNC <= intCalcs); intNC++)
                                        {
                                            if ((intCalcs > 0))
                                            {
                                                intTempStart = (Convert.ToInt32(strStartInterval) + (intNC * intMaxNIntv));
                                                intTempStop = (intTempStart + (intMaxNIntv - 1));
                                                if (intTempStop > Convert.ToInt32(strStopInterval))
                                                {
                                                    intTempStop = Convert.ToInt32(strStopInterval);
                                                }

                                                // Calc the new number of intervals to be calculated
                                                intTempNPeriod = (intTempStop - intTempStart);
                                            }

                                            arCompositeIntVol = new double?[intTempNPeriod, intNComps - 1];
                                            blnSkipGAMS = false;
                                            intNP = 0;
                                            //  Number of intervals(periods) base zero (0)
                                            // Calculate the sum of comp (I) volumes in every one of the range of the interval found
                                            for (intNum = intTempStart; (intNum <= intTempStop); intNum++)
                                            {
                                                // Get the Inteval volumes for every one of the comps
                                                List<CompIntVols> CompIntVolsList1 =  await _repository.CompIntVols(curblend.lngID, intNum);
                                                
                                                intI = 0;
                                                dblSumVol = 0;
                                                foreach (CompIntVols CompIntVolsObj in CompIntVolsList1)
                                                { 
                                                    // get the mat_id of the given mat_name
                                                    strMatName = CompIntVolsObj.Name;
                                                    lngMatId = await _repository.GetMatId(strMatName);
                                                    
                                                    // get the Usage Name for the given blend Component
                                                    strUsageName = await GetBldMatUsage(curblend.lngID, lngMatId);
                                                    if ((strUsageName != "ADDITIVE"))
                                                    {
                                                        // get the Sum(Int Comp Vol) in the whole range of intervals to calc the average recipe for composite/spot sample
                                                        // arCompIntVol(intI) = NVL(arCompIntVol(intI), 0) + ABCdataEnv.rscmdCompIntVols.Fields("VOLUME").Value
                                                        // get an array of (Period,comp interval volumes) for composite samples after composite bias has been calculated
                                                        arCompositeIntVol[intNP, intI] = CompIntVolsObj.Volume;
                                                        
                                                        dblSumVol = (dblSumVol + ((arCompositeIntVol[intNP, intI] == null) ? 0 : Convert.ToDouble(arCompositeIntVol[intNP, intI])));
                                                        intI = (intI + 1);
                                                    }
                                                }
                                                
                                                if ((dblSumVol == 0))
                                                {
                                                    blnSkipGAMS = true;
                                                }

                                                intNP = (intNP + 1);
                                            }

                                            // Log Msg if dblSumVol=0: BLEND ^1: TOTAL COMPONENT INTERVAL VOLUME IS ZERO FOR INTERVAL ^2.  LINEPROP CALC WILL NOT BE PERFORMED
                                            if ((blnSkipGAMS == true))
                                            {
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN94), programName, "BL-" + curblend.lngID, curblend.strName, (intTempStart + ("-" + intTempStop)),
                                               "", "", "", "", res);                                                
                                            }

                                            // if calcprop_flag="YES" in abc_blenders, then proccess LINEPROP
                                            if (((vntBldrsData[intBldrIdx].CalcpropFlag == "YES") && ((curblend.sngCurVol != 0) && (blnSkipGAMS == false))))
                                            {
                                                // Recalculate the feedback pred. for intervals where BIAS has been updated
                                                // to have the correct TQI when TMMON runs.
                                                // Create one single task (multiperiod) for the GAMS Opt., instead of callig it one by one, which will downgrade performance of this program
                                                // call MODEL_GLOBAL to do line property calculation for the COMPOSITE interval sample
                                                gintOptResult = RetStatus.FAILURE;
                                                
                                                await commonGAMSOptimizer.Call_Modal_Global("GASOLINE", connectionString, (ABBAdvancedBlendOptimizer.Enum.GAMSCalcTypes)GAMSCalcTypes.LINEPROP, (long)curblend.lngID, intTempStart, (long)arCompositeIntVol[0, 0],
                                               (ABBAdvancedBlendOptimizer.Enum.DebugLevels)enumDebugLevel, intTempStop, (ABBAdvancedBlendOptimizer.Enum.RetStatus)gintOptResult);

                                                if (gintOptResult == RetStatus.SUCCESS)
                                                {
                                                    //Msg "^1 SAMPLE ^2 HAS BEEN USED IN BLEND ^3 FOR INTERVALS ^4 TO ^5"
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN103), programName, "BL-" + curblend.lngID, strSampleType, strSampleName,
                                                curblend.strName, intTempStart.ToString(), intTempStop.ToString(), "", res);
                                                    
                                                    // Set the function back value
                                                    rtrnData = RetStatus.SUCCESS;
                                                    // set the process_sample_flag to OFF after processing the sample
                                                    await _repository.SetProcessSampleFlag(curblend.lngID);                                                   
                                                }
                                            }// conditions to perform LINEPROP for multiperiod
                                        }// perform GAMS calc in multiples of MaxNPeriod
                                    }
                                    else
                                    {
                                        // LOG MSG: ^1 SAMPLE ^2 COULD NOT BE ALLOCATED WITHIN THE INTVL RANGE OF BLEND ^3. BLEND SAMPLE DATA IGNORED
                                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN100), programName, "BL-" + curblend.lngID, strSampleType, strSampleName,
                                               curblend.strName, "", "", "", res);
                                    }// START/STOP intervals = NULL_
                                }// conditions to perform LINEPROP
                            }// Check for N of comps > 1                            
                        }
                        else
                        {
                            // LOG MSG: START INTV>STOP INTV FOR ^1 SAMPLE ^2 IN BLEND ^3. BLEND SAMPLE DATA IGNORED
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN101), programName, "BL-" + curblend.lngID, strSampleType, strSampleName,
                                             curblend.strName, "", "", "", res);
                        }// check of composite/spot sample type
                    }
                    else
                    {
                        // LOG MSG: ^1 SAMPLE ^2 COULD NOT BE ALLOCATED WITHIN THE INTVL RANGE OF BLEND ^3. BLEND SAMPLE DATA IGNORED
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN100), programName, "BL-" + curblend.lngID, strSampleType, strSampleName,
                                             curblend.strName, "", "", "", res);
                    }// Interval Range
                NEXT_SAMPLE:
                    { }
                }

                //   of Composite/Spot samples
                // reset the filter
                //ABCdataEnv.rscomCompositeSpotSample.Filter = adFilterNone;
                
                // if this is the last sample, then set the bias override flag back to NO
                if ((curblend.strBiasOverrideFlag == "YES") && (blnBiasOverrideFlag == true))
                {
                    // reset the bias_override flag to NO if procces LP has been processed for Composite/spot sample
                    await _repository.SetBiasOverrideFlag(curblend.lngID);

                    //IN BLEND ^1 BIAS OVERRIDE FLAG HAS BEEN PROCESSED
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN104), programName, "BL-" + curblend.lngID, curblend.strName, "",
                                             "", "", "", "", res);
                    
                    // According with Discussion with KA on Sep 30, 03, let's process all samples with Bias
                    // override flag =YES
                    curblend.strBiasOverrideFlag = "NO";
                }
            }
            
            return rtrnData;
        }
        private async Task<int> CalcBlend(int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend, DebugLevels enumDebugLevel)
        {
            List<CompVolTids> vntCompsData;
            int intNComps;
            int intI;
            double?[] arStationId = new double?[0];
            int intNCompIndx;
            double? vntCurRcp;
            string vntValQuality = "";
            DcsTag tagTotVol = new DcsTag();
            DcsTag tagWildFlag = new DcsTag();
            string strReadEnabled = "";
            string strScanEnabled = "";
            string strScanGrpName = "";
            string strCompVolTidsOrgQuery;
            string strExecute;
            string strStationName;
            // , strScanGroupName As String
            double dblNewVol;
            double[] arDltVol;
            double[] arDltStatVol;
            double?[] ardblStationCurRcp;
            double[] arCompIntVol;
            double dblTotCompIntVol;
            double[] dblStationNewVol = new double[0];
            double[] arCompBldVol;
            double dblTotCompBldVol;
            double dblIntCost;
            double dblBldCost;
            double dblStationCurVol = 0;
            double? vntCompIntVol;
            double? vntCompBldVol;
            double? vntActRcp;
            double? vntAvgRcp;
            double? vntCompCost;
            double dblIntRcp = 0;
            double? dblIntVol=0;
            double dblTotalVol;
            DateTime? vntValTime = new DateTime();
            double dblFeedbackPred;
            double dblStationActRcp;
            double dblTotStationVol;
            int intCompPropID;
            int intStationNum= 0;
            int intNum;
            int intNStations;
            bool blnRollBack;
            double? lngCompLineupID;
            double? lngTotalStationVolTid;
            double? lngWildStationTid;
            double? lngTotalCompFlowTid;
            double? lngScanGroupId;
            double? sngScanRate;
            double sngDateMinMaxDiff;
            DateTime dteMinValTime;
            DateTime dteMaxValTime;
            int intTotNStations = 0;
            int intStationNumber;
            double[] arAddDltStatVol;
            double[] dblAddStationNewVol = new double[0];
            double[] arAddDltVol;
            double dblAddTotalVol = 0;
            double[] arAddCompIntVol;
            double[] arAddCompBldVol;
            double dblAddNewVol = 0;
            double dblAddTotCompIntVol = 0;
            double dblAddTotCompBldVol = 0;
            double dblAddTotStationVol;
            double dblCompIntCost;
            double dblCompBldCost;
            double dblAddIntCost = 0;
            double dblAddBldCost = 0;
            double? dblVolConvFactor;
            double dblSumVol;
            string strUsageName;
            string strRcpConstraintType;
            RetStatus gintOptResult;
            RetStatus intSampleResult;
            string strMinMaxTimeTag = "";
            string strAggregateQuality;
            List<double> vntStations = new List<double>();
            double?[] arStationsDone;
            int intJ;
            var res = "";

            if (enumDebugLevel == DebugLevels.High)
            {
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, curblend.strName, "CALC_BLEND",
                    "", "", "", "", res);
            }

            //'get total flow on blender   
            AbcTags DataRes = await _repository.GetTagNameAndVal(vntBldrsData[intBldrIdx].TotalFlowTid);
            gTagTotFlow.vntTagName = DataRes.Name;
            gTagTotFlow.vntTagVal = DataRes.ReadValue.ToString();

            if (gTagTotFlow.vntTagVal == null)
            {
                //'warn msg "Null or bad total flow tag on blender ^1"     
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN4), programName, "BL-" + curblend.lngID, gTagTotFlow.vntTagName, gstrBldrName, "BMON",
                     "", "", "", res);
                return 0;
            }
            List<TotalStatVol> GetTotalStatVolData = new List<TotalStatVol>();
            List<TotalCompVol> GetTotalCompVolData = new List<TotalCompVol>();
            if (gstrDownloadType == "STATION" || gstrDownloadType == "LINEUP")
            {
                vntCompsData = await _repository.GetCompStatVolTids(curblend.lngID);

                intNComps = vntCompsData.Count();
                // Get all the total station Vol at once (Batch selection)
                GetTotalStatVolData = await _repository.GetTotalStatVol(curblend.lngID, vntBldrsData[intBldrIdx].Id);
                if (GetTotalStatVolData.Count() > 0)
                {
                    intTotNStations = GetTotalStatVolData.Count();
                    //  RW 28-Mar-2012 for PreemL PQ-19
                    //  Don't check difference between earliest and latest flow totaliser timestamps if ramping up or ramping down at end of blend
                    if (((curblend.strRampingActFlag == "NO") && (curblend.sngCurVol <= (curblend.sngTgtVol - vntBldrsData[intBldrIdx].StopOptVol))))
                    {
                        // RW 28-Mar-2012 for PreemL
                        if ((gintSkipCycleBmon[intBldrIdx] == 0) && (curblend.strState.Trim() != "PAUSED"))
                        {
                            // Get the Scan_Group_Id for one of the stations
                            lngTotalCompFlowTid = GetTotalStatVolData[0].TotalStationVolTid; // NULL_
                            strScanGrpName = GetTotalStatVolData[0].ScanGroupName;
                            lngScanGroupId = GetTotalStatVolData[0].ScanGroupId;// NULL_
                            sngScanRate = GetTotalStatVolData[0].Scanrate; // NULL_);
                            if (lngTotalCompFlowTid != null && lngScanGroupId != null && sngScanRate != null)
                            {
                                // Get the max and min value time
                                dteMinValTime = cdteNull;
                                dteMaxValTime = cdteNull;
                                //                     'get the min and max tag times
                                //                     GetMinMaxTagStationTimes curblend.lngID, vntBldrsData(BLDR_ID, intBldrIdx), dteMinValTime, dteMaxValTime
                                MxMnValTime MxMnValTimeData = await _repository.GetMxMnValTime(curblend.lngID, vntBldrsData[intBldrIdx].Id);

                                if (MxMnValTimeData != null)
                                {
                                    dteMinValTime = (MxMnValTimeData.MinValTime == null) ? cdteNull : Convert.ToDateTime(MxMnValTimeData.MinValTime);
                                    dteMaxValTime = (MxMnValTimeData.MaxValTime == null) ? cdteNull : Convert.ToDateTime(MxMnValTimeData.MaxValTime);
                                }

                                sngDateMinMaxDiff = DateAndTime.DateDiff("s", dteMinValTime, dteMaxValTime);

                                if ((sngDateMinMaxDiff > gProjDfs.dblTotalizerTimestampTolerance))
                                {
                                    //  RW 28-Mar-2012 for PreemL PQ-19                                    
                                    res = "";
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN89), programName, "BL-" + curblend.lngID, strScanGrpName, "", "",
                                         "", "", "", res);
                                    //  Added RW 28-Mar-2012 for PreemL PQ-19
                                    //  Write message to Blend Monitor log containing min and max timestamps and tags
                                    List<TotalizerScanTimes> TotalizerScanTimesData = await _repository.GetTotalizerScanTimes(curblend.lngID, vntBldrsData[intBldrIdx].Id);

                                    if (TotalizerScanTimesData.Count() > 0)
                                    {
                                        strMinMaxTimeTag = ("MIN=" + (TotalizerScanTimesData[0].ScanTime) + " " + TotalizerScanTimesData[0].TagName);
                                        //ABCdataEnv.rscmdGetTotalizerScanTimes.MoveLast;
                                        strMinMaxTimeTag = strMinMaxTimeTag + ", MAX=" + (TotalizerScanTimesData[TotalizerScanTimesData.Count() - 1].ScanTime + " " + TotalizerScanTimesData[TotalizerScanTimesData.Count() - 1].TagName);
                                    }

                                    //  Write message
                                    _shared.ErrorLog("TOTALIZER VOLUME SCAN TIMES ARE NOT SYNCHRONIZED IN SCAN GROUP " + strScanGrpName + ", " + "BL-"
                                                + curblend.lngID + ", " + strMinMaxTimeTag, true);
                                    gintSkipCycleBmon[intBldrIdx] = 1;

                                    return 0;
                                }
                            }

                        }
                        else if ((((gintSkipCycleBmon[intBldrIdx] == 1) || (gintSkipCycleBmon[intBldrIdx] == 2)) &&
                            (curblend.strState.Trim() != "PAUSED")))
                        {
                            // Get the Scan_Group_Id for one of the stations
                            lngTotalCompFlowTid = GetTotalStatVolData[0].TotalStationVolTid;
                            strScanGrpName = GetTotalStatVolData[0].ScanGroupName;
                            lngScanGroupId = GetTotalStatVolData[0].ScanGroupId;
                            sngScanRate = GetTotalStatVolData[0].Scanrate;
                            if (lngTotalCompFlowTid != null && lngScanGroupId != null && sngScanRate != null)
                            {
                                // Get the max and min value time
                                dteMinValTime = cdteNull;
                                dteMaxValTime = cdteNull;
                                //                     'get the min and max tag times
                                //                     GetMinMaxTagStationTimes curblend.lngID, vntBldrsData(BLDR_ID, intBldrIdx), dteMinValTime, dteMaxValTime
                                MxMnValTime MxMnValTimeData = await _repository.GetMxMnValTime(curblend.lngID, vntBldrsData[intBldrIdx].Id);

                                if (MxMnValTimeData != null)
                                {
                                    dteMinValTime = (MxMnValTimeData.MinValTime == null) ? cdteNull : Convert.ToDateTime(MxMnValTimeData.MinValTime);
                                    dteMaxValTime = (MxMnValTimeData.MaxValTime == null) ? cdteNull : Convert.ToDateTime(MxMnValTimeData.MaxValTime);
                                }

                                sngDateMinMaxDiff = DateAndTime.DateDiff("s", dteMinValTime, dteMaxValTime);
                                //  If sngDateMinMaxDiff > 0.8 * sngScanRate Then                       RW 28-Mar-2012 for PreemL PQ-19
                                if ((sngDateMinMaxDiff > gProjDfs.dblTotalizerTimestampTolerance))
                                {
                                    //  RW 28-Mar-2012 for PreemL PQ-19
                                    if ((gintSkipCycleBmon[intBldrIdx] == 1))
                                    {
                                        gintSkipCycleBmon[intBldrIdx] = 2;
                                    }
                                    else if ((gintSkipCycleBmon[intBldrIdx] == 2))
                                    {
                                        // reinitialize the skip cycle
                                        gintSkipCycleBmon[intBldrIdx] = 0;
                                    }

                                }
                                else
                                {
                                    gintSkipCycleBmon[intBldrIdx] = 0;
                                }
                            }
                            else
                            {
                                // Set the flag tp false(0) for the next cycles of Bmon
                                gintSkipCycleBmon[intBldrIdx] = 0;
                            }

                        }
                        else
                        {
                            // Set the flag tp false(0) for the next cycles of Bmon
                            gintSkipCycleBmon[intBldrIdx] = 0;
                        }

                    }

                    //  RW 28-Mar-2012 PreemL PQ-19
                }

                // Redim this variable
                dblStationNewVol = new double[intTotNStations];
                dblAddStationNewVol = new double[intTotNStations];
            }
            else
            {
                vntCompsData = await _repository.GetCompVolTids(vntBldrsData[intBldrIdx].Id, curblend.lngID);
                intNComps = vntCompsData.Count();
                // Get all the total station Vol at once (Batch selection)
                GetTotalCompVolData = await _repository.GetTotalCompVol(curblend.lngID, vntBldrsData[intBldrIdx].Id);
                if (GetTotalCompVolData.Count() > 0)
                {
                    intTotNStations = GetTotalCompVolData.Count();
                    //  RW 28-Mar-2012 for PreemL PQ-19
                    //  Don't check difference between earliest and latest flow totaliser timestamps if ramping up or ramping down at end of blend
                    if (((curblend.strRampingActFlag == "NO") && (curblend.sngCurVol <= (curblend.sngTgtVol - vntBldrsData[intBldrIdx].StopOptVol))))
                    {
                        // RW 28-Mar-2012 for PreemL
                        if ((gintSkipCycleBmon[intBldrIdx] == 0))
                        {
                            // Get the Scan_Group_Id for one of the stations
                            lngTotalCompFlowTid = GetTotalCompVolData[0].TotCompVolTid; // NULL_
                            strScanGrpName = GetTotalCompVolData[0].ScanGroupName;
                            lngScanGroupId = GetTotalCompVolData[0].ScanGroupId;// NULL_
                            sngScanRate = GetTotalCompVolData[0].Scanrate; // NULL_);
                            if (lngTotalCompFlowTid != null && lngScanGroupId != null && sngScanRate != null)
                            {
                                // Get the max and min value time
                                dteMinValTime = cdteNull;
                                dteMaxValTime = cdteNull;
                                //                     'get the min and max tag times
                                //                     GetMinMaxTagStationTimes curblend.lngID, vntBldrsData(BLDR_ID, intBldrIdx), dteMinValTime, dteMaxValTime
                                MxMnValTime MxMnValTimeData = await _repository.GetMxMnValTime(curblend.lngID, vntBldrsData[intBldrIdx].Id);

                                if (MxMnValTimeData != null)
                                {
                                    dteMinValTime = (MxMnValTimeData.MinValTime == null) ? cdteNull : Convert.ToDateTime(MxMnValTimeData.MinValTime);
                                    dteMaxValTime = (MxMnValTimeData.MaxValTime == null) ? cdteNull : Convert.ToDateTime(MxMnValTimeData.MaxValTime);
                                }

                                sngDateMinMaxDiff = DateAndTime.DateDiff("s", dteMinValTime, dteMaxValTime);

                                if ((sngDateMinMaxDiff > gProjDfs.dblTotalizerTimestampTolerance))
                                {
                                    //  RW 28-Mar-2012 for PreemL PQ-19                                    
                                    res = "";
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN89), programName, "BL-" + curblend.lngID, strScanGrpName, "", "",
                                         "", "", "", res);
                                    //  Added RW 28-Mar-2012 for PreemL PQ-19
                                    //  Write message to Blend Monitor log containing min and max timestamps and tags
                                    List<TotalizerScanTimes> TotalizerScanTimesData = await _repository.GetTotalizerScanTimes(curblend.lngID, vntBldrsData[intBldrIdx].Id);

                                    if (TotalizerScanTimesData.Count() > 0)
                                    {
                                        strMinMaxTimeTag = ("MIN=" + (TotalizerScanTimesData[0].ScanTime) + " " + TotalizerScanTimesData[0].TagName);
                                        //ABCdataEnv.rscmdGetTotalizerScanTimes.MoveLast;
                                        strMinMaxTimeTag = strMinMaxTimeTag + ", MAX=" + (TotalizerScanTimesData[TotalizerScanTimesData.Count() - 1].ScanTime + " " + TotalizerScanTimesData[TotalizerScanTimesData.Count() - 1].TagName);
                                    }

                                    //  Write message
                                    _shared.ErrorLog("TOTALIZER VOLUME SCAN TIMES ARE NOT SYNCHRONIZED IN SCAN GROUP " + strScanGrpName + ", " + "BL-"
                                                + curblend.lngID + ", " + strMinMaxTimeTag, true);
                                    gintSkipCycleBmon[intBldrIdx] = 1;

                                    return 0;
                                }
                            }

                        }
                        else if ((((gintSkipCycleBmon[intBldrIdx] == 1) || (gintSkipCycleBmon[intBldrIdx] == 2)) &&
                            (curblend.strState.Trim() != "PAUSED")))
                        {
                            // Get the Scan_Group_Id for one of the stations
                            lngTotalCompFlowTid = GetTotalCompVolData[0].TotCompVolTid;
                            strScanGrpName = GetTotalCompVolData[0].ScanGroupName;
                            lngScanGroupId = GetTotalCompVolData[0].ScanGroupId;
                            sngScanRate = GetTotalCompVolData[0].Scanrate;
                            if (lngTotalCompFlowTid != null && lngScanGroupId != null && sngScanRate != null)
                            {
                                // Get the max and min value time
                                dteMinValTime = cdteNull;
                                dteMaxValTime = cdteNull;
                                //                     'get the min and max tag times
                                //                     GetMinMaxTagStationTimes curblend.lngID, vntBldrsData(BLDR_ID, intBldrIdx), dteMinValTime, dteMaxValTime
                                MxMnValTime MxMnValTimeData = await _repository.GetMxMnValTime(curblend.lngID, vntBldrsData[intBldrIdx].Id);

                                if (MxMnValTimeData != null)
                                {
                                    dteMinValTime = (MxMnValTimeData.MinValTime == null) ? cdteNull : Convert.ToDateTime(MxMnValTimeData.MinValTime);
                                    dteMaxValTime = (MxMnValTimeData.MaxValTime == null) ? cdteNull : Convert.ToDateTime(MxMnValTimeData.MaxValTime);
                                }

                                sngDateMinMaxDiff = DateAndTime.DateDiff("s", dteMinValTime, dteMaxValTime);
                                //  If sngDateMinMaxDiff > 0.8 * sngScanRate Then                       RW 28-Mar-2012 for PreemL PQ-19
                                if ((sngDateMinMaxDiff > gProjDfs.dblTotalizerTimestampTolerance))
                                {
                                    //  RW 28-Mar-2012 for PreemL PQ-19
                                    if ((gintSkipCycleBmon[intBldrIdx] == 1))
                                    {
                                        gintSkipCycleBmon[intBldrIdx] = 2;
                                    }
                                    else if ((gintSkipCycleBmon[intBldrIdx] == 2))
                                    {
                                        // reinitialize the skip cycle
                                        gintSkipCycleBmon[intBldrIdx] = 0;
                                    }

                                }
                                else
                                {
                                    gintSkipCycleBmon[intBldrIdx] = 0;
                                }
                            }
                            else
                            {
                                // Set the flag tp false(0) for the next cycles of Bmon
                                gintSkipCycleBmon[intBldrIdx] = 0;
                            }

                        }
                        else
                        {
                            // Set the flag tp false(0) for the next cycles of Bmon
                            gintSkipCycleBmon[intBldrIdx] = 0;
                        }

                    }

                    //  RW 28-Mar-2012 PreemL PQ-19
                }
            }

            // component interval vols are used for calling GAMS interface to do
            // line prop calculations, the comp names are thus needed
            arCompIntVol = new double[(intNComps)];
            arCompBldVol = new double[(intNComps)];
            arDltVol = new double[(intNComps)];
            arAddDltVol = new double[(intNComps)];
            arAddCompIntVol = new double[(intNComps)];
            arAddCompBldVol = new double[(intNComps)];

            //'initialize value time array for components on the blender, if neccessary
            if (!gArCompValTime[intBldrIdx].blnArraySet)
            {
                gArCompValTime[intBldrIdx].arValueTime = new DateTime[(intNComps)];
                for (intI = 0; (intI
                            <= (intNComps - 1)); intI++)
                {
                    gArCompValTime[intBldrIdx].arValueTime[intI] = cdteNull;
                }

                gArCompValTime[intBldrIdx].blnArraySet = true;
            }

            // Array to record the times when blend station current volumes are updated
            // Initialize value time array for blend stations used by blender, if neccessary
            if (!gArStnValTime[intBldrIdx].blnArraySet)
            {
                gArStnValTime[intBldrIdx].arValueTime = new DateTime[intTotNStations];
                gArStnValTime[intBldrIdx].arKey = new double[intTotNStations];
                // Store all blend station ids for associating times with stations
                // This is necessary since the database is only updated for stations in the currently selected lineup                
                List<AbcBlendStations> AllBldStationsData = await _repository.GetAllBldStations(curblend.lngID);
                vntStations = AllBldStationsData.Select(row => row.StationId).ToList<double>(); // ABCdataEnv.rscomGetAllBldStations.GetRows(adGetRowsRest, adBookmarkFirst, "STATION_ID");
                for (intI = 0; intI <= (intTotNStations - 1); intI++)
                {
                    gArStnValTime[intBldrIdx].arValueTime[intI] = cdteNull;
                    gArStnValTime[intBldrIdx].arKey[intI] = vntStations[intI];
                }

                gArStnValTime[intBldrIdx].blnArraySet = true;
            }

            // calculate new volume, calculate comp interval and blend volumes,
            // update comp interval volumes            
            List<CompIntVols> CompIntVolsData = await _repository.CompIntVols(curblend.lngID, curblend.intCurIntv);
            vntCompIntVol = CompIntVolsData[0].Volume;

            CompBldData CompBld = await _repository.CompBldData(curblend.lngID);
            vntCompBldVol = CompBld.Volume;
            vntCurRcp = CompBld.CurRecipe;

            dblNewVol = 0;
            dblTotCompIntVol = 0;
            dblTotCompBldVol = 0;
            //------------check----------------------
            //intStationNumber = 1;
            intStationNumber = 0;
            // Array for recording blend stations that have been processed
            arStationsDone = new double?[(intTotNStations)];
            blnRollBack = true;
            for (intI = 0; intI <= (intNComps - 1); intI++)
            {
                //'Set the total component vol to zero at the beginning of the processing
                dblTotalVol = 0;
                dblAddTotalVol = 0;
                strRcpConstraintType = "";
                //'BDS 6-Jun-2012 PQ-D0078 Initialize flag for aggregate data
                //'quality of all station volume totalizer tags in the lineup
                strAggregateQuality = "GOOD";
                //'BDS 6-Jun-2012 PQ-D0078

                if (gstrDownloadType == "STATION" || gstrDownloadType == "LINEUP")
                {
                    // Reset the new station vol to zero for new comps
                    dblStationNewVol[intStationNumber] = 0;
                    dblAddStationNewVol[intStationNumber] = 0;
                    // Get the lineup id from abc_blend_sources
                    lngCompLineupID = await _repository.GetBldLineupId(curblend.lngID, vntCompsData[intI].MatId);

                    // get the Usage Name for the given blend Component
                    strUsageName = await GetBldMatUsage(curblend.lngID, vntCompsData[intI].WildFlagTid);
                    //  Get component Rcp Constraint Type
                    List<BldCompUsage> BldCompUsageData = await _repository.GetBldCompUsage(curblend.lngID, vntCompsData[intI].MatId);

                    if (BldCompUsageData.Count() > 0)
                    {
                        strRcpConstraintType = BldCompUsageData[0].RcpConstraintType;
                    }

                    // Get all the stations having this component                   
                    List<BldrStationsData> GetBldrStationsDataList = await _repository.GetBldrStationsData(lngCompLineupID, vntBldrsData[intBldrIdx].Id);

                    intStationNum = 0;
                    intNStations = GetBldrStationsDataList.Count();
                    // redim the arrays
                    arStationId = new double?[intNStations];
                    ardblStationCurRcp = new double?[intNStations];
                    arDltStatVol = new double[intNStations];
                    arAddDltStatVol = new double[intNStations];

                    for (int i = 0; i < intNStations; i++)
                    {
                        //intStationNum = (intStationNum + 1);
                        arStationId[intStationNum] = GetBldrStationsDataList[i].StationId;
                        strStationName = GetBldrStationsDataList[i].StationName;
                        lngWildStationTid = GetBldrStationsDataList[i].WildFlagTid;
                        // Find the total vol station Tid for this station
                        // get volume tag value for the component
                        List<TotalStatVol> TotalStatVolObj = GetTotalStatVolData.Where<TotalStatVol>(row => row.StationId == arStationId[intStationNum]).ToList<TotalStatVol>();

                        if (TotalStatVolObj.Count() > 0)
                        {
                            lngTotalStationVolTid = TotalStatVolObj[0].TotalStationVolTid;
                            tagTotVol.vntTagName = TotalStatVolObj[0].TotalStationTag;
                            tagTotVol.vntTagVal = TotalStatVolObj[0].ReadValue.ToString();
                            vntValTime = TotalStatVolObj[0].ValueTime;
                            vntValQuality = TotalStatVolObj[0].ValueQuality;
                            strReadEnabled = TotalStatVolObj[0].ReadEnabledFlag;
                            strScanEnabled = TotalStatVolObj[0].ScanEnabledFlag;
                            strScanGrpName = TotalStatVolObj[0].ScanGroupName;
                        }
                        else
                        {
                            lngTotalStationVolTid = null;
                        }

                        if ((lngTotalStationVolTid == null))
                        {
                            // set BMON_MISSINGTAG to blender, skip calculation
                            await _repository.SetBlenderErrFlag("BMON_MISSINGTAG", vntBldrsData[intBldrIdx].Id, "");

                            // warn msg "Missing totalizer volume tag"
                            res = "";
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN54), programName, "BL-" + curblend.lngID, vntCompsData[intI].Name, gstrBldrName, "",
                                 "", "", "", res);
                            return 0;
                        }

                        if (tagTotVol.vntTagVal != null)
                        {
                            // BDS 25-May-2012 PQ-D0075 Clamp the totalizer tag read value to zero if it is negative
                            if ((Convert.ToInt32(tagTotVol.vntTagVal) < 0))
                            {
                                tagTotVol.vntTagVal = (0).ToString();
                            }

                            // BDS 25-May-2012 PQ-D0075
                            if ((strUsageName != "ADDITIVE"))
                            {
                                dblTotalVol = (dblTotalVol + Convert.ToInt32(tagTotVol.vntTagVal));
                            }
                            else
                            {
                                dblAddTotalVol = (dblAddTotalVol + Convert.ToInt32(tagTotVol.vntTagVal));
                            }

                        }
                        else if ((strUsageName != "ADDITIVE"))
                        {
                            dblTotalVol = dblTotalVol;
                        }
                        else
                        {
                            dblAddTotalVol = dblAddTotalVol;
                        }

                        if (vntCompBldVol == null)
                        {
                            vntCompBldVol = 0;
                        }

                        if (vntValQuality == "GOOD")
                        {
                            //                  'save current totalizer volume into ABC_BLEND_STATIONS.CUR_VOL
                            //                  ABCdataEnv.cmdSetStationCurVol tagTotVol.vntTagVal, curblend.lngID, _
                            //                      arStationId(intStationNum), vntCompsData(2, intI)
                            if (enumDebugLevel >= DebugLevels.Low)
                            {
                                res = "";
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG41), programName, cstrDebug, curblend.strName, vntCompsData[intI].Name, strStationName, tagTotVol.vntTagVal,
                                     vntValTime.ToString(), "", res);
                                // BDS 6-Jul-2012 PQ-D0074 Record time database is updated with station current volumes
                                LogStnUpdateTim(intBldrIdx, arStationsDone);
                                // BDS 6-Jul-2012 PQ-D0074
                            }

                            if ((strReadEnabled == "NO"))
                            {
                                // warning msg "DCS tag ^1 reading disabled"
                                res = "";
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN41), programName, "BL-" + curblend.lngID, "TOTALIZER STATION_VOL_TID", tagTotVol.vntTagName,
                                    "", "", "", "", res);
                                // BDS 6-Jul-2012 PQ-D0074 Record time database is updated with station current volumes
                                LogStnUpdateTim(intBldrIdx, arStationsDone);
                                // BDS 6-Jul-2012 PQ-D0074
                            }

                            if ((strScanEnabled == "NO"))
                            {
                                // warning msg "Scan group ^1 disabled"
                                res = "";
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN42), programName, "BL-" + curblend.lngID, strScanGrpName, "TOTALIZER STATION_VOL_TID", tagTotVol.vntTagName,
                                    "", "", "", res);
                                // BDS 6-Jul-2012 PQ-D0074 Record time database is updated with station current volumes
                                LogStnUpdateTim(intBldrIdx, arStationsDone);
                                // BDS 6-Jul-2012 PQ-D0074
                            }

                            // get the Current vol stored in the DB of every station with this material
                            AbcBlendStations GetBldStationsDataObj = await _repository.GetBldStationsData(curblend.lngID, vntCompsData[intI].MatId, arStationId[intStationNum]);

                            dblStationCurVol = (GetBldStationsDataObj.CurVol == null) ? 0 : Convert.ToDouble(GetBldStationsDataObj.CurVol);
                            ardblStationCurRcp[intStationNum] = GetBldStationsDataObj.CurSetpoint;

                            if ((Convert.ToDouble(tagTotVol.vntTagVal) > dblStationCurVol) && (vntValQuality == "GOOD"))
                            {
                                if ((strUsageName != "ADDITIVE"))
                                {
                                    arDltStatVol[intStationNum] = (Convert.ToDouble(tagTotVol.vntTagVal) - dblStationCurVol);
                                }
                                else
                                {
                                    arAddDltStatVol[intStationNum] = (Convert.ToDouble(tagTotVol.vntTagVal) - dblStationCurVol);
                                }

                            }
                            else if (Convert.ToDouble(tagTotVol.vntTagVal) == dblStationCurVol)
                            {
                                // warning msg "Totalizer vol unchaged on station ^1"
                                if ((strUsageName != "ADDITIVE"))
                                {
                                    arDltStatVol[intStationNum] = 0;
                                }
                                else
                                {
                                    arAddDltStatVol[intStationNum] = 0;
                                }

                                //  ZERO COMP RCP Handling
                                // If a rcp is meant to be zero in the ABC, then skip msgs
                                if ((strRcpConstraintType != "ZERO_OUT"))
                                {
                                    // Only issue msg on debug
                                    if (enumDebugLevel >= DebugLevels.Low)
                                    {
                                        // warn msg "Totalizer vol unchanged"
                                        // check this message if apply
                                        res = "";
                                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN55), programName, "BL-" + curblend.lngID, tagTotVol.vntTagVal, strStationName, "",
                                            "", "", "", res);
                                        // BDS 6-Jul-2012 PQ-D0074 Record time database is updated with station current volumes
                                        LogStnUpdateTim(intBldrIdx, arStationsDone);
                                        // BDS 6-Jul-2012 PQ-D0074
                                    }

                                }

                                // 1.0001 is a tolerance to avoid false messsages
                                // BDS 11-May-2012 Tolerance increased by a factor of ten
                                // ElseIf tagTotVol.vntTagVal * 1.0001 < dblStationCurVol Then
                            }
                            else if ((Convert.ToDouble(tagTotVol.vntTagVal) * 1.001) < dblStationCurVol)
                            {
                                // BDS 11-May-2012
                                // warn msg "Totalizer vol less than previous vol"
                                // BDS 6-Jun-2012 PQ-D0075 Prevent a message when the totalizer tag value is negative
                                if (Convert.ToDouble(tagTotVol.vntTagVal) > 0)
                                {
                                    res = "";
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN57), programName, "BL-" + curblend.lngID, tagTotVol.vntTagVal, dblStationCurVol.ToString(), strStationName,
                                        gstrBldrName, "", "", res);
                                    // BDS 6-Jul-2012 PQ-D0074 Record time database is updated with station current volumes
                                    LogStnUpdateTim(intBldrIdx, arStationsDone);
                                    // BDS 6-Jul-2012 PQ-D0074
                                }

                                // BDS 6-Jun-2012 PQ-D0074
                                if ((ardblStationCurRcp[intStationNum] == null) && (strUsageName != "ADDITIVE"))
                                {
                                    // set BMON_BADVOLUME error to blender, skip calculation
                                    await _repository.SetBlenderErrFlag("BMON_BADVOLUME", vntBldrsData[intBldrIdx].Id, "");

                                    // BDS 6-Jun-2012 PQ-D0074 Message moved from before rollback to avoid database update
                                    // warn msg "Null current recipe for comp ^1 in station ^2 "
                                    res = "";
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN56), programName, "BL-" + curblend.lngID, strStationName, gstrBldrName, "",
                                        "", "", "", res);
                                    // BDS 6-Jun-2012 PQ-D0074
                                    return 0;
                                }

                                // Call CHECK_STATION_VOLUME
                                // BDS 6-Jul-2012 PQ-D0074 Calculate station volume change based on time of last update
                                if ((strUsageName != "ADDITIVE"))
                                {
                                    arDltStatVol[intStationNum] = await ChkStnVol(intBldrIdx, vntBldrsData[intBldrIdx].PrdgrpId, ardblStationCurRcp[intStationNum], arStationId[intStationNum], enumDebugLevel);
                                }
                                else
                                {
                                    arAddDltStatVol[intStationNum] = await ChkStnVol(intBldrIdx, vntBldrsData[intBldrIdx].PrdgrpId, ardblStationCurRcp[intStationNum], arStationId[intStationNum], enumDebugLevel);
                                }

                                // BDS 6-Jul-2012 PQ-D0074
                            }

                            if ((strUsageName != "ADDITIVE"))
                            {
                                dblStationNewVol[intStationNumber] = (dblStationNewVol[intStationNumber] + arDltStatVol[intStationNum]);
                            }
                            else
                            {
                                dblAddStationNewVol[intStationNumber] = (dblAddStationNewVol[intStationNumber] + arAddDltStatVol[intStationNum]);
                            }

                        }
                        else if ((tagTotVol.vntTagVal == null) || (vntValQuality == "BAD"))
                        {
                            // warning msg "Bad total vol tag ^1"
                            res = "";
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN58), programName, "BL-" + curblend.lngID, tagTotVol.vntTagName, vntCompsData[intI].Name, gstrBldrName,
                                "", "", "", res);
                            // BDS 6-Jul-2012 PQ-D0074 Record time database is updated with station current volumes
                            LogStnUpdateTim(intBldrIdx, arStationsDone);
                            // BDS 6-Jul-2012 PQ-D0074
                        }

                        // Get wild_flag_tid from abc_stations and write to abc_blend_comps.wild
                        if ((lngWildStationTid != null))
                        {
                            // get value of wild_flag_tid                           
                            AbcTags DataRes1 = await _repository.GetTagNameAndVal(lngWildStationTid);
                            tagWildFlag.vntTagName = DataRes1.Name;
                            tagWildFlag.vntTagVal = DataRes1.ReadValue.ToString();

                            if (tagWildFlag.vntTagVal != null)
                            {
                                //  Update abc_blend_comps.wild
                                await _repository.UpdateAbcBlendCompWild(curblend.lngID, vntCompsData[intI].MatId, tagWildFlag.vntTagVal);
                            }

                        }

                        // save current totalizer volume into ABC_BLEND_STATIONS.CUR_VOL
                        await _repository.SetStationCurVol(tagTotVol.vntTagVal, curblend.lngID, arStationId[intStationNum], vntCompsData[intI].MatId);
                        // save the the abc_blend_stations.CUR_VOL in abc_blend_stations.PREV_VOL
                        await _repository.SetStationPrevVol(dblStationCurVol.ToString(), curblend.lngID, arStationId[intStationNum], vntCompsData[intI].MatId);
                        //  ************
                        // BDS 6-Jul-2012 PQ-D0074 Add blend station to list of stations processed
                        for (intJ = 0; intJ <= (intTotNStations - 1); intJ++)
                        {
                            if (arStationsDone[intJ] == 0)
                            {
                                arStationsDone[intJ] = arStationId[intStationNum];
                                break;
                            }

                        }

                        // BDS 6-Jul-2012 PQ-D0074
                        // BDS 6-Jun-2012 PQ-D0078 Record bad data quality of totalizer tag in lineup
                        if (vntValQuality == null || vntValQuality == "BAD")
                        {
                            strAggregateQuality = "BAD";
                        }

                        intStationNumber = (intStationNumber + 1);
                    }

                    // BDS 6-Jun-2012 PQ-D0078 Record aggregate data quality of volume totalizer tags in lineup
                    vntValQuality = strAggregateQuality;
                    // BDS 6-Jun-2012 PQ-D0078
                    //  Handling switching of multiple stations in active/paused mode
                    // check for previous lineup (stations) and add those stored volumes to the current total station vol
                    // call function to get prev vol
                    if ((strUsageName != "ADDITIVE"))
                    {
                        dblTotalVol = await GetOrgStationVols(curblend.lngID, vntCompsData[intI].MatId, lngCompLineupID, dblTotalVol);
                    }
                    else
                    {
                        dblAddTotalVol = await GetOrgStationVols(curblend.lngID, vntCompsData[intI].MatId, lngCompLineupID, dblAddTotalVol);
                    }
                }
                else
                {
                    //  Get component Rcp Constraint Type
                    List<BldCompUsage> BldCompUsageList = await _repository.GetBldCompUsage(curblend.lngID, vntCompsData[intI].MatId);

                    if (BldCompUsageList.Count() > 0)
                    {
                        strRcpConstraintType = BldCompUsageList[0].RcpConstraintType;
                    }

                    // Find the total comp vol Tid for this comp
                    // get volume tag value for the component

                    List<TotalCompVol> GetTotalCompVolDataFltr = GetTotalCompVolData.Where<TotalCompVol>(row => row.MatId == vntCompsData[intI].MatId).ToList<TotalCompVol>();

                    if (GetTotalCompVolDataFltr.Count() > 0)
                    {
                        lngTotalCompFlowTid = GetTotalCompVolDataFltr[0].TotCompVolTid;
                        tagTotVol.vntTagName = GetTotalCompVolDataFltr[0].TotalCompTag;
                        tagTotVol.vntTagVal = GetTotalCompVolDataFltr[0].ReadValue.ToString();
                        vntValTime = GetTotalCompVolDataFltr[0].ValueTime;
                        vntValQuality = GetTotalCompVolDataFltr[0].ValueQuality;
                        strReadEnabled = GetTotalCompVolDataFltr[0].ReadEnabledFlag;
                        strScanEnabled = GetTotalCompVolDataFltr[0].ScanEnabledFlag;
                        strScanGrpName = GetTotalCompVolDataFltr[0].ScanGroupName;
                    }
                    else
                    {
                        lngTotalCompFlowTid = null;
                    }

                    if ((lngTotalCompFlowTid == null))
                    {
                        // set BMON_MISSINGTAG to blender, skip calculation
                        await _repository.SetBlenderErrFlag("BMON_MISSINGTAG", vntBldrsData[intBldrIdx].Id, "");
                        // warn msg "Missing totalizer volume tag"
                        res = "";
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN54), programName, "BL-" + curblend.lngID, vntCompsData[intI].Name, gstrBldrName, "",
                             "", "", "", res);

                        return 0;
                    }

                    // get the Usage Name for the given blend Component
                    strUsageName = await GetBldMatUsage(curblend.lngID, vntCompsData[intI].MatId);

                    if (tagTotVol.vntTagVal != null)
                    {
                        // BDS 25-May-2012 PQ-D0075 Clamp the totalizer tag read value to zero if it is negative
                        if (Convert.ToDouble(tagTotVol.vntTagVal) < 0)
                        {
                            tagTotVol.vntTagVal = (0).ToString();
                        }

                        // BDS 25-May-2012 PQ-D0075
                        if ((strUsageName != "ADDITIVE"))
                        {
                            dblTotalVol = (dblTotalVol + Convert.ToDouble(tagTotVol.vntTagVal));
                        }
                        else
                        {
                            dblAddTotalVol = (dblAddTotalVol + Convert.ToDouble(tagTotVol.vntTagVal));
                        }

                    }
                    else if ((strUsageName != "ADDITIVE"))
                    {
                        dblTotalVol = dblTotalVol;
                    }
                    else
                    {
                        dblAddTotalVol = dblAddTotalVol;
                    }

                    if (vntCompBldVol == null)
                    {
                        vntCompBldVol = 0;
                    }

                    // Get wild_flag_tid from abc_blender_comps and write to abc_blend_comps.wild
                    if (vntCompsData[intI].WildFlagTid != null)
                    {
                        // get value of wild_flag_tid
                        AbcTags DataRes2 = await _repository.GetTagNameAndVal(vntCompsData[intI].WildFlagTid);
                        tagWildFlag.vntTagName = DataRes2.Name;
                        tagWildFlag.vntTagVal = DataRes2.ReadValue.ToString();

                        if (tagWildFlag.vntTagVal != null)
                        {
                            // Update abc_blend_comps.wild
                            await _repository.UpdateAbcBlendCompWild(curblend.lngID, vntCompsData[intI].MatId, tagWildFlag.vntTagVal);
                        }
                    }

                    if ((vntValQuality == "GOOD"))
                    {
                        if ((strReadEnabled == "NO"))
                        {
                            // warning msg "DCS tag ^1 reading disabled"
                            res = "";
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN41), programName, "BL-" + curblend.lngID, "TOTALIZER STATION_VOL_TID", tagTotVol.vntTagName, "",
                                 "", "", "", res);
                        }

                        if ((strScanEnabled == "NO"))
                        {
                            // warning msg "Scan group ^1 disabled"
                            res = "";
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN42), programName, "BL-" + curblend.lngID, strScanGrpName, "TOTALIZER STATION_VOL_TID", tagTotVol.vntTagName,
                                 "", "", "", res);
                        }

                    }
                    else if (tagTotVol.vntTagVal == null || vntValQuality == "BAD")
                    {
                        // warning msg "Bad total vol tag ^1"
                        res = "";
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN58), programName, "BL-" + curblend.lngID, tagTotVol.vntTagName, vntCompsData[intI].Name, gstrBldrName,
                             "", "", "", res);
                    }
                }
                //'End of download type calculation

                //'Calculate delta volume based in one of the previous calculations
                if (strUsageName != "ADDITIVE")
                {
                    if ((dblTotalVol > vntCompBldVol) && (vntValQuality == "GOOD"))
                    {
                        arDltVol[intI] = Convert.ToDouble(dblTotalVol - vntCompBldVol);
                    }
                    else if (dblTotalVol == vntCompBldVol)
                    {
                        //  ZERO COMP RCP Handling.  Skip Msg
                        if ((strRcpConstraintType != "ZERO_OUT"))
                        {
                            // Only issue msg on debug
                            if ((enumDebugLevel >= DebugLevels.Low))
                            {
                                // warn msg "Totalizer vol unchanged"
                                res = "";
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN55), programName, "BL-" + curblend.lngID, dblTotalVol.ToString(), vntCompsData[intI].Name, "",
                                     "", "", "", res);
                                // BDS 6-Jul-2012 PQ-D0074 Record time database is updated with station current volumes
                                if ((gstrDownloadType == "STATION") || (gstrDownloadType == "LINEUP"))
                                {
                                    LogStnUpdateTim(intBldrIdx, arStationsDone);
                                }
                            }

                        }

                        arDltVol[intI] = 0;
                    }
                    else
                    {
                        if ((vntCurRcp == null) && (strUsageName != "ADDITIVE"))
                        {
                            // set BMON_BADVOLUME error to blender, skip calculation
                            await _repository.SetBlenderErrFlag("BMON_BADVOLUME", vntBldrsData[intBldrIdx].Id, "");
                            // warn msg "Null current recipe for comp ^1"
                            res = "";
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN56), programName, "BL-" + curblend.lngID, vntCompsData[intI].Name, gstrBldrName, "",
                                 "", "", "", res);
                            return 0;
                        }

                        // 1.0001 is a tolerance to avoid false messsages
                        // BDS 11-May-2012 Tolerance increased by a factor of ten
                        // If dblTotalVol * 1.0001 < vntCompBldVol.Value Then
                        if ((dblTotalVol * 1.001) < vntCompBldVol.Value)
                        {
                            // BDS 11-May-2012
                            // warn msg "Totalizer vol less than previous vol"
                            // BDS 6-Jun-2012 PQ-D0075 Prevent a message when the total value is negative
                            if ((dblTotalVol > 0))
                            {
                                res = "";
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN57), programName, "BL-" + curblend.lngID, dblTotalVol.ToString(), vntCompBldVol.ToString(), vntCompsData[intI].Name, gstrBldrName, "",
                                     "", res);
                                // BDS 6-Jul-2012 PQ-D0074 Record time database is updated with station current volumes
                                if (((gstrDownloadType == "STATION")
                                            || (gstrDownloadType == "LINEUP")))
                                {
                                    LogStnUpdateTim(intBldrIdx, arStationsDone);
                                }
                            }
                        }

                        // Call CHECK_COMPONENT_VOLUME
                        // BDS 11-May-2012 PQ-D0074 Calculate component volume change based on time of last update
                        // arDltVol(intI) = ChkCompVol(intBldrIdx, vntBldrsData(PRDGRP_ID, intBldrIdx), _
                        //    vntCurRcp.Value, intI, enumDebugLevel)
                        arDltVol[intI] = await ChkCompVol(intBldrIdx, Convert.ToInt32(vntBldrsData[intBldrIdx].PrdgrpId), vntCurRcp.Value, gArCompValTime[intBldrIdx].arValueTime[intI], enumDebugLevel);
                        // BDS 11-May-2012 PQ-D0074
                    }

                    arCompIntVol[intI] = ((vntCompIntVol == null) ? 0 : Convert.ToDouble(vntCompIntVol)) + arDltVol[intI];
                    arCompBldVol[intI] = (vntCompBldVol.Value + arDltVol[intI]);
                    dblNewVol = (dblNewVol + arDltVol[intI]);
                    dblTotCompIntVol = (dblTotCompIntVol + arCompIntVol[intI]);
                    dblTotCompBldVol = (dblTotCompBldVol + arCompBldVol[intI]);
                    vntCompIntVol = arCompIntVol[intI];

                    if (enumDebugLevel >= DebugLevels.Low)
                    {
                        res = "";
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG18), programName, cstrDebug, curblend.strName, vntCompsData[intI].Name, arDltVol[intI].ToString(), dblTotalVol.ToString(),
                            vntCompBldVol.ToString(), "", res);

                        // BDS 6-Jul-2012 PQ-D0074 Record time database is updated with station current volumes
                        if ((gstrDownloadType == "STATION") || (gstrDownloadType == "LINEUP"))
                        {
                            LogStnUpdateTim(intBldrIdx, arStationsDone);
                        }
                    }
                }
                else //'if additive
                {
                    if ((dblAddTotalVol > vntCompBldVol.Value) && (vntValQuality == "GOOD"))
                    {
                        arAddDltVol[intI] = Convert.ToDouble(dblAddTotalVol - vntCompBldVol);
                    }
                    else if ((dblAddTotalVol == vntCompBldVol.Value))
                    {
                        // ZERO COMP RCP Handling. Skip msg
                        if ((strRcpConstraintType != "ZERO_OUT"))
                        {
                            // Only issue msg on debug
                            if ((enumDebugLevel >= DebugLevels.Low))
                            {
                                // warn msg "Totalizer vol unchanged"
                                res = "";
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN55), programName, "BL-" + curblend.lngID, dblAddTotalVol.ToString(), vntCompsData[intI].Name, "",
                                     "", "", "", res);
                                // BDS 6-Jul-2012 PQ-D0074 Record time database is updated with station current volumes
                                if (((gstrDownloadType == "STATION")
                                            || (gstrDownloadType == "LINEUP")))
                                {
                                    LogStnUpdateTim(intBldrIdx, arStationsDone);
                                }
                            }

                        }
                        arAddDltVol[intI] = 0;
                    }
                    else
                    {
                        if ((vntCurRcp == null) && (strUsageName != "ADDITIVE"))
                        {
                            // set BMON_BADVOLUME error to blender, skip calculation
                            await _repository.SetBlenderErrFlag("BMON_BADVOLUME", vntBldrsData[intBldrIdx].Id, "");
                            res = "";
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN56), programName, "BL-" + curblend.lngID, vntCompsData[intI].Name, gstrBldrName, "",
                                 "", "", "", res);
                            return 0;
                        }

                        // 1.0001 is a tolerance to avoid false messsages
                        // BDS 11-May-2012 Tolerance increased by a factor of ten
                        // If dblAddTotalVol * 1.0001 < vntCompBldVol.Value Then
                        if ((dblAddTotalVol * 1.001) < vntCompBldVol.Value)
                        {
                            // BDS 11-May-2012
                            // warn msg "Totalizer vol less than previous vol"
                            // BDS 6-Jun-2012 PQ-D0075 Prevent a message when the total value is negative
                            if ((dblAddTotalVol > 0))
                            {
                                res = "";
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN57), programName, "BL-" + curblend.lngID, dblAddTotalVol.ToString(), vntCompBldVol.ToString(), vntCompsData[intI].Name, gstrBldrName, "",
                                     "", res);
                                // BDS 6-Jul-2012 PQ-D0074 Record time database is updated with station current volumes
                                if ((gstrDownloadType == "STATION") || (gstrDownloadType == "LINEUP"))
                                {
                                    LogStnUpdateTim(intBldrIdx, arStationsDone);
                                }
                            }
                        }

                        // Call CHECK_COMPONENT_VOLUME
                        // BDS 11-May-2012 PQ-D0074 Calculate component volume change based on time of last update
                        // arAddDltVol(intI) = ChkCompVol(intBldrIdx, vntBldrsData(PRDGRP_ID, intBldrIdx), _
                        //     NVL(vntCurRcp.Value, 0), intI, enumDebugLevel)
                        arAddDltVol[intI] = await ChkCompVol(intBldrIdx, Convert.ToInt32(vntBldrsData[intBldrIdx].PrdgrpId), (vntCurRcp == null) ? 0 : Convert.ToDouble(vntCurRcp), gArCompValTime[intBldrIdx].arValueTime[intI], enumDebugLevel);
                    }

                    arAddCompIntVol[intI] = ((vntCompIntVol == null) ? 0 : Convert.ToDouble(vntCompIntVol)) + arAddDltVol[intI];
                    arAddCompBldVol[intI] = (vntCompBldVol.Value + arAddDltVol[intI]);
                    // Get Volume conversion factor
                    // dblVolConvFactor = GetVolConvFactor(curblend.lngID, vntBldrsData(PRDGRP_ID, intBldrIdx), curblend.intProdID, vntCompsData(2, intI))
                    // Convert arAddDltVol[intI],arAddCompIntVol[intI],arAddCompBldVol[intI] to same units of blend volume
                    dblAddNewVol = (dblAddNewVol + arAddDltVol[intI]);
                    // * dblVolConvFactor
                    dblAddTotCompIntVol = (dblAddTotCompIntVol + arAddCompIntVol[intI]);
                    // * dblVolConvFactor
                    dblAddTotCompBldVol = (dblAddTotCompBldVol + arAddCompBldVol[intI]);
                    // * dblVolConvFactor
                    vntCompIntVol = arAddCompIntVol[intI];
                    if (enumDebugLevel >= DebugLevels.Low)
                    {
                        res = "";
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG18), programName, cstrDebug, curblend.strName, vntCompsData[intI].Name, arAddDltVol[intI].ToString(), dblAddTotalVol.ToString(),
                            vntCompBldVol.ToString(), "", res);
                        // BDS 6-Jul-2012 PQ-D0074 Record time database is updated with station current volumes
                        if ((gstrDownloadType == "STATION") || (gstrDownloadType == "LINEUP"))
                        {
                            LogStnUpdateTim(intBldrIdx, arStationsDone);
                        }

                    }
                }


             }
            
            blnRollBack = false;
            if ((gstrDownloadType == "STATION")|| (gstrDownloadType == "LINEUP"))
            {
                LogStnUpdateTim(intBldrIdx,arStationsDone);
                // Record null database update time for any out-of-service blend stations
                for (intI = 0; intI <= (intTotNStations - 1); intI++)
                {
                    for (intJ = 0; intJ <= (intTotNStations - 1); intJ++)
                    {
                        if (gArStnValTime[intBldrIdx].arKey[intI] == arStationsDone[intJ])
                        {
                            break;
                        }

                    }
                    if (intJ > (intTotNStations - 1))
                    {
                        gArStnValTime[intBldrIdx].arValueTime[intI] = cdteNull;
                    }
                }
            }

            //'update comp blend volumes, calculate and update actual and average blend recipe,
            //'calculate interval and blend cost
            dblIntCost = 0;
            dblBldCost = 0;
            //--------------------------checked------------------
            intStationNumber = 0;
            //intStationNumber = 1;
            vntActRcp = CompBld.ActRecipe;
            vntAvgRcp = CompBld.AvgRecipe;
            vntCompCost = CompBld.Cost;
            //ABCdataEnv.rscmdCompBldData.MoveFirst   
            blnRollBack = true;

            for (intI = 0; intI <= (intNComps - 1); intI++)
            {
                // if null values set to zero
                vntActRcp = (vntActRcp == null)?0: vntActRcp;
                vntAvgRcp = (vntAvgRcp == null) ? 0 : vntAvgRcp;
                // get the Usage Name for the given blend Component
                strUsageName = await GetBldMatUsage(curblend.lngID, vntCompsData[intI].MatId);
                if ((strUsageName != "ADDITIVE"))
                {
                    vntCompBldVol = arCompBldVol[intI];
                    // ********* Protect divide by 0
                    if (dblNewVol > 1E-06)
                    {
                        vntActRcp = (arDltVol[intI] / (dblNewVol * 100));
                    }

                    if ((dblTotCompBldVol > 1E-06))
                    {
                        vntAvgRcp = (arCompBldVol[intI]/ (dblTotCompBldVol * 100));
                    }

                    if ((dblTotCompIntVol > 1E-06))
                    {
                        dblIntRcp = (arCompIntVol[intI] / dblTotCompIntVol);
                        //     To set the Int Recipe to display in the BO
                        dblIntRcp = Math.Round(dblIntRcp, 5);
                        await _repository.SetIntRcp((dblIntRcp * 100),curblend.lngID,vntCompsData[intI].MatId,curblend.intCurIntv);
                    }

                }
                else
                {
                    vntCompBldVol = arAddCompBldVol[intI];
                    // Get Volume conversion factor
                    dblVolConvFactor = await GetVolConvFactor(curblend.lngID, Convert.ToInt32(vntBldrsData[intBldrIdx].PrdgrpId), curblend.intProdID, Convert.ToInt32(vntCompsData[intI].MatId));
                    // Actual additive recipe is: delta add vol/(New blend vol*dblVolConvFactor)
                    if ((dblNewVol > 1E-06) && (dblVolConvFactor != 0))
                    {
                        vntActRcp = (arAddDltVol[intI] / (dblNewVol * dblVolConvFactor));
                    }

                    // Average additive recipe is: add vol in blend/(total blend vol*dblVolConvFactor)
                    if ((dblTotCompBldVol > 1E-06) && (dblVolConvFactor != 0))
                    {
                        vntAvgRcp = (arAddCompBldVol[intI] / (dblTotCompBldVol * dblVolConvFactor));
                    }

                    // interval add recipe is: add vol in interval/(delta blend interval vol*dblVolConvFactor)
                    if ((dblTotCompIntVol > 1E-06) && (dblVolConvFactor != 0))
                    {
                        dblIntRcp = (arAddCompIntVol[intI] / (dblTotCompIntVol * Convert.ToDouble(dblVolConvFactor)));
                        dblIntRcp = Math.Round(dblIntRcp, 5);
                        await _repository.SetIntRcp(dblIntRcp,curblend.lngID,vntCompsData[intI].MatId,curblend.intCurIntv);
                    }

                }

                // **********************************
                if ((gstrDownloadType == "STATION") || (gstrDownloadType == "LINEUP"))
                {
                    // Get the lineup id from abc_blend_sources
                    lngCompLineupID = await _repository.GetBldLineupId(curblend.lngID, vntCompsData[intI].MatId);

                    // Get the number of stations per component
                    List<BlendStationEqp> BlendStationEqpList = await _repository.GetBlendStationEqp(lngCompLineupID,curblend.lngID,vntCompsData[intI].MatId);
                    
                    if (BlendStationEqpList.Count() > 0)
                    {
                        intStationNum = BlendStationEqpList.Count();
                        // BDS 11-May-2012 PQ-D0077 Array redimensioned to wrong number
                        // ReDim arStationId(0 To intNStations)
                        arStationId = new double?[intStationNum];
                        //ABCdataEnv.rscmdGetBlendStationEqp.MoveFirst;
                    }

                    dblTotStationVol = 0;
                    dblAddTotStationVol = 0;
                    for (intNum = 0; intNum <= (intStationNum - 1); intNum++)
                    {
                        arStationId[intNum] = BlendStationEqpList[intNum].StationId;
                        if ((strUsageName != "ADDITIVE"))
                        {
                            dblTotStationVol = (dblTotStationVol + dblStationNewVol[(intStationNumber + intNum)]);
                        }
                        else
                        {
                            dblAddTotStationVol = (dblAddTotStationVol + dblAddStationNewVol[(intStationNumber + intNum)]);
                        }
                        //ABCdataEnv.rscmdGetBlendStationEqp.MoveNext;
                    }
                    
                    //Set the actual recipe in abc_blend_stations.act_Setpoint
                    for (intNum = 0; intNum <= (intStationNum - 1); intNum++)
                    {
                        if ((strUsageName != "ADDITIVE"))
                        {
                            // Write zero to actual recipe if comp volume is zero
                            dblStationActRcp = 0;
                            if ((dblTotStationVol > 1E-06))
                            {
                                dblStationActRcp = (dblStationNewVol[(intStationNumber + intNum)] * (vntActRcp.Value / dblTotStationVol));
                            }

                            // save the act recipe in abc_blend_stations.act_setpoint
                            dblStationActRcp = Math.Round(dblStationActRcp, 5);
                            await _repository.SetBldStatPar(dblStationActRcp, curblend.lngID, arStationId[intNum], vntCompsData[intI].MatId);
                        }
                        else
                        {
                            // Write zero to actual recipe if addtive volume is zero
                            dblStationActRcp = 0;
                            if ((dblAddTotStationVol > 1E-06))
                            {
                                // Add Station actual recipe is: Station vol(gr) times Actual recipe (gr/gal) / (total station vol(gr))
                                dblStationActRcp = (dblAddStationNewVol[(intStationNumber + intNum)] * (vntActRcp.Value / dblAddTotStationVol));
                            }

                            // save the act recipe in abc_blend_stations.act_setpoint
                            dblStationActRcp = Math.Round(dblStationActRcp, 5);
                            await _repository.SetBldStatPar(dblStationActRcp,curblend.lngID,arStationId[intNum],vntCompsData[intI].MatId);
                        }
                    }

                    // Move to the next station
                    intStationNumber = (intStationNumber + intStationNum);
                }

                if ((enumDebugLevel == DebugLevels.Low))
                {
                    res = "";
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG19), programName, cstrDebug, curblend.strName, vntCompsData[intI].Name, vntActRcp.ToString(), "",
                    "", "", res);

                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG21), programName, cstrDebug, curblend.strName, vntCompsData[intI].Name, arCompIntVol[intI].ToString(), arCompBldVol[intI].ToString(),
                   "", "", res);
                }

                if (vntCompCost == null)
                {
                    // warn msg "Null material cost for comp ^1"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN59), programName, "BL-" + curblend.lngID, vntCompsData[intI].Name, curblend.strName, "", "",
                  "", "", res);
                }
                else if ((strUsageName != "ADDITIVE"))
                {
                    dblIntCost = (dblIntCost + (dblIntRcp * vntCompCost.Value));
                    dblBldCost = (dblBldCost + (vntAvgRcp.Value * (vntCompCost.Value / 100)));
                    //             dblCompIntCost = dblCompIntCost + arCompIntVol[intI] * vntCompCost.Value
                    //             dblCompBldCost = dblCompBldCost + arCompBldVol[intI] * vntCompCost.Value
                }
                else
                {
                    dblAddIntCost = (dblAddIntCost + (arAddCompIntVol[intI] * vntCompCost.Value));
                    dblAddBldCost = (dblAddBldCost + (arAddCompBldVol[intI] * vntCompCost.Value));
                }

                if ((enumDebugLevel == DebugLevels.High))
                {
                    res = "";
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG23), programName, cstrDebug, curblend.strState, vntCompsData[intI].Name, vntActRcp.ToString(), dblIntRcp.ToString(),
                    "", "", res);
                }                
               // ABCdataEnv.rscmdCompBldData.MoveNext;
            }
            
            blnRollBack = false;           
            // BDS 11-May-2012 PQ-D0074 Record time of component volume database update
            for (intI = 0; intI <= (intNComps - 1); intI++)
            {
                gArCompValTime[intBldrIdx].arValueTime[intI] = DateTime.Now;
            }
            
            //    'Calculate Interval/Blend cost excluding the additives
            //    dblIntCost = (dblCompIntCost + dblAddIntCost) / (dblTotCompIntVol + dblAddTotCompIntVol)
            //    dblBldCost = (dblCompBldCost + dblAddBldCost) / (dblTotCompBldVol + dblAddTotCompBldVol)
            if (enumDebugLevel == DebugLevels.Low)
            {
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG24), programName, cstrDebug, curblend.strName, dblTotCompIntVol.ToString(), dblIntCost.ToString(), dblBldCost.ToString(),
                "", "", res);
            }

            // calculate interval and blend volumes
            dblIntVol = await _repository.GetIntVol(curblend.lngID,curblend.intCurIntv);
            dblIntVol = (dblIntVol + dblNewVol);
            gdblBldVol = Convert.ToDouble(curblend.sngCurVol + dblNewVol);

            if (enumDebugLevel == DebugLevels.Low)
            {
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG20), programName, cstrDebug, curblend.strName, curblend.intCurIntv.ToString(), dblNewVol.ToString(), dblIntVol.ToString(),
                "", "", res);                
            }

            if (enumDebugLevel >= DebugLevels.Low)
            {
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG22), programName, cstrDebug, curblend.strName, gdblBldVol.ToString(), dblTotCompBldVol.ToString(), "",
                "", "", res);               
            }

            // check consistency between total comp volume and blend volume '+ dblAddTotCompBldVol
            if (((Math.Abs((dblTotCompBldVol - gdblBldVol)) > ((gProjDfs.vntVolTolr== null)?(0.002 * curblend.sngTgtVol): gProjDfs.vntVolTolr))
                        && (curblend.intCurIntv > gArPrevBldData[intBldrIdx].intCurIntv)))
            {
                if ((enumDebugLevel == DebugLevels.Low))
                {
                    // warning msg "Total comp vol and blend vol differ more than ^1"
                    res = "";
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN60), programName, "BL-" + curblend.lngID, dblTotCompBldVol.ToString(), gdblBldVol.ToString(), curblend.strName,
                        (gProjDfs.vntVolTolr == null) ? (0.002 * curblend.sngTgtVol).ToString() : gProjDfs.vntVolTolr.ToString(), "", "", res);                  
                }
            }

            // save interval volume,cost
            dblIntVol = Math.Round(Convert.ToDouble(dblIntVol), 5);
            dblIntCost = Math.Round(dblIntCost, 5);
            //update also new field abc_blend_intervals.blend_volume in every cycle
            await _repository.SetIntVolCost(dblIntVol,dblIntCost,gdblBldVol,curblend.lngID,curblend.intCurIntv);
            //initialize the sum of vols for all materials = total  component inteval vol
            dblSumVol = dblTotCompIntVol;
            //Save current volume, cost and current rate to the blend
            dblBldCost = Math.Round(dblBldCost, 5);
            await _repository.SetBldVolCost(gdblBldVol,dblBldCost,gTagTotFlow.vntTagVal,curblend.lngID);
            //Update the local value of current volume for the rest of the calcs during this cycle of Bmon
            curblend.sngCurVol = gdblBldVol;
            gintOptResult = RetStatus.FAILURE;
            if (curblend.strState.Trim() != "PAUSED")
            {
                //Call GAMS two times per interval: One at the middle and another one at the end of the interval
                if ((((curblend.intCurIntv > gArPrevBldData[intBldrIdx].intCurIntv) && (curblend.intCurIntv > 1)) || 
                    ((gArBldFinishTime[intBldrIdx] != cdteNull) && (curblend.intCurIntv > 1))))
                {
                    //            Or (gIntProcLineProp[intBldrIdx] = 2 And curblend.intCurIntv > 1) Then
                    //           Move to the beginning of the program
                    //          Set up global array of prdgrp IDs
                    //          GetPrdgrpIDs
                    // If blend is finished in RBC, then close current interval and open a new one.
                    // The TQI will be set for TMon to perform it and blend will be closed in the next cycle of Bmon
                    // Note that all remaining volume was allocated in the previuos interval to be accounted in the last TQI
                    if ((gArBldFinishTime[intBldrIdx] != cdteNull) && (curblend.intCurIntv > 1))
                    {
                        await ChkIntervals(intBldrIdx,curblend,enumDebugLevel);
                    }

                    
                    // Get the Inteval volumes for the previous interval to pass it to GAMS
                    List<CompIntVols> CompIntVolsList = await _repository.CompIntVols(curblend.lngID,gArPrevBldData[intBldrIdx].intCurIntv);
                    
                    intI = 0;
                    intNCompIndx = 0;
                    dblSumVol = 0;
                    foreach (var CompIntVolsobj in CompIntVolsList)
                    {
                        // get the Usage Name for the given blend Component
                        strUsageName = await GetBldMatUsage(curblend.lngID, vntCompsData[intNCompIndx].MatId);
                        if ((strUsageName != "ADDITIVE"))
                        {
                            //If ABCdataEnv.rscmdCompIntVols.Fields("VOLUME").Value <> 0 Then
                            //Exclude additives to pass comps to opt
                            Array.Resize(ref arCompIntVol, intI + 1);

                            arCompIntVol[intI] = (CompIntVolsobj.Volume == null) ? 0 : Convert.ToDouble(CompIntVolsobj.Volume);
                            dblSumVol = (dblSumVol + arCompIntVol[intI]);
                            intI = (intI + 1);
                        }

                        intNCompIndx = (intNCompIndx + 1);                      
                    }                    
                    
                    //Store the real number of comps, excluding the additives
                    intNComps = intI;
                    //Skip the call of ModelLocal if the Number of components is only one
                    if ((intNComps > 1))
                    {
                        //  if dblSumVol=0: BLEND ^1: TOTAL COMPONENT INTERVAL VOLUME IS ZERO FOR INTERVAL ^2.  LINEPROP CALC WILL NOT BE PERFORMED
                        if ((dblSumVol == 0))
                        {
                            res = "";
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN94), programName, "BL-" + curblend.lngID, curblend.strName, curblend.intCurIntv.ToString(), "",
                                "", "", "", res);
                        }

                        //   if calcprop_flag="YES" in abc_blenders, then proccess LINEPROP
                        if (((vntBldrsData[intBldrIdx].CalcpropFlag) == "YES") && (curblend.sngCurVol != 0) && (dblSumVol > 0))
                        {
                            // call MODEL_LOCAL to do line property calculation for previous interval
                            
                            await commonGAMSOptimizer.Call_Modal_Local("GASOLINE", connectionString, (ABBAdvancedBlendOptimizer.Enum.GAMSCalcTypes)GAMSCalcTypes.LINEPROP, (long)curblend.lngID, gArPrevBldData[intBldrIdx].intCurIntv, (long)arCompIntVol[0],
                                       (ABBAdvancedBlendOptimizer.Enum.DebugLevels)enumDebugLevel, 0, (ABBAdvancedBlendOptimizer.Enum.RetStatus)gintOptResult);
                        }
                    }
                    else
                    {
                        //  Copy the selected Properties from ABC_BLEND_COMP_Props to Feedback_pred in abc_blend_interval_props
                        List<SelTankProps> SelTankPropsList = await _repository.GetSelTankProps(curblend.lngID);
                        foreach (var SelTankPropsObj in SelTankPropsList)
                        {
                            dblFeedbackPred = (SelTankPropsObj.Value == null) ? 0 : Convert.ToInt32(SelTankPropsObj.Value);
                            intCompPropID = Convert.ToInt32(SelTankPropsObj.PropId);
                            await _repository.SetFeebackPred(dblFeedbackPred, curblend.lngID, gArPrevBldData[intBldrIdx].intCurIntv, intCompPropID);
                        }
                    }

                        // if # of components > 1
                        // Added a bias Calc parameter to diff, between calc bias types.  This call is for
                        // the common bias calc done every new interval.  This calls updates the Anzr values only (prev calc bias logic)
                        // Note: That calcbias sub has been moved to this place to calc bias only when a new interval is created, just
                        // after LINEPROP calc
                        // Pass an internal variable to identify the procedence of the bias call: REGular
                        await CalcBias(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, "REG");
                        await UpdateBlendProps(intBldrIdx, vntBldrsData, curblend, enumDebugLevel);
                 }// if new interval was created
             }// if blend state = "ACTIVE"

            // Process samples if needed in ACTIVE or PAUSED states
            intSampleResult = await ProcessSamples(intBldrIdx, vntBldrsData, curblend, enumDebugLevel);
            // Set tqi_now_flag to "YES" right after LINEPROP because of the sampling or regular LINEPROP
            // Set the TQI_NOW_FLAG=YES
            if ((curblend.intCurIntv > gArPrevBldData[intBldrIdx].intCurIntv && vntBldrsData[intBldrIdx].CalcpropFlag == "YES"
                && curblend.intCurIntv > 1 && gintOptResult == RetStatus.SUCCESS && gArBldFinishTime[intBldrIdx] == cdteNull) ||
                (gArBldFinishTime[intBldrIdx] != cdteNull && curblend.intCurIntv > 1 && vntBldrsData[intBldrIdx].CalcpropFlag == "YES"
                && gintOptResult == RetStatus.SUCCESS) ||
                (vntBldrsData[intBldrIdx].CalcpropFlag == "YES" && intSampleResult == RetStatus.SUCCESS))
            {
                await _repository.SetTqi(curblend.lngID);
            }
            return 0;
        }
        
        // *********** ChkDcsFeedback ***********
        private async Task<int> ChkDcsFeedback(int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend, int intDestTankID, DebugLevels enumDebugLevel)
        { 
            DcsTag tagFbVol = new DcsTag();
            DcsTag tagTotVol = new DcsTag();
            // tagTgtVol As DcsTag,
            double vntSrcTankID;
            double? vntBldrSrcSlctFbTid=0;
            DcsTag tagBldrSrcSlctFb = new DcsTag();
            double vntDummy;
            double? vntPrdLnupSlctFbTid;
            DcsTag tagPrdLnupSlctFb = new DcsTag();
            string strTankName = "";
            string strFlushSwgState;
            double lngStationPacingTid;
            double lngMatId;
            double lngStationId;
            double lngPrevMatId = 0;
            double lngFlushTankId = 0;
            DcsTag tagStationPacing = new DcsTag();
            string strPacingFlag;
            double lngProdLineupId;
            double lngTransferLineId;
            // , sngTransferLineVol As Single
            bool blnCompPacing;
            int intTankID = 0;
            // *****
            double lngLineupID;
            double lngTankSelFbTID;
            double lngLineupSelFBTID;
            double lngLineupSelTID;
            // , lngTankSelTID As Long
            int intDCSTankNum;
            int intDCSLineupNum;
            string strLineupName = "";
            string strCompName;
            var res = "";
            // *****
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            // get vol tag values the blender
            AbcTags DataRes = await _repository.GetTagNameAndVal(vntBldrsData[intBldrIdx].RbcVolSpFbTid);
            tagFbVol.vntTagName = DataRes.Name;
            tagFbVol.vntTagVal = DataRes.ReadValue.ToString();

            DataRes = await _repository.GetTagNameAndVal(vntBldrsData[intBldrIdx].TotalVolTid);
            tagTotVol.vntTagName = DataRes.Name;
            tagTotVol.vntTagVal = DataRes.ReadValue.ToString();

           
            if (tagFbVol.vntTagVal != null)
            {
                // And Not IsNull(tagTgtVol.vntTagVal)
                if ((curblend.sngTgtVol == gArPrevBldData[intBldrIdx].sngPrevBldTargVol) && 
                    (Math.Abs(Convert.ToDouble(tagFbVol.vntTagVal) - Convert.ToDouble(curblend.sngTgtVol)) > gProjDfs.vntVolTolr))
                {
                    // warn msg RBC ACTUAL TARGET VOL ^1 AND ABC TARGET VOL ^2 DIFFER MORE THAN DEFAULT VOL TOLERANCE ^3 FOR BLEND ^4
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN62), programName, "BL-" + curblend.lngID, tagFbVol.vntTagVal, curblend.sngTgtVol.ToString(),
                        gProjDfs.vntVolTolr.ToString(),curblend.strName, "", "", res);
                }

            }

            // *********** Following is unnecessarily warning the very first time !!!!!
            if (tagTotVol.vntTagVal!=null)
            {
                if ((Math.Abs(Convert.ToDouble(tagTotVol.vntTagVal) - Convert.ToDouble(gdblBldVol)) > gProjDfs.vntVolTolr))
                {
                    // warn msg "DCS TOTAL VOL ^1 AND ABC TOTAL VOL ^2 DIFFER MORE THAN DEFAULT VOL TOLERANCE ^3 FOR BLEND ^4
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN63), programName, "BL-" + curblend.lngID, tagFbVol.vntTagVal, gdblBldVol.ToString(),
                       gProjDfs.vntVolTolr.ToString(), curblend.strName, "", "", res);
                }

            }

            // compare blender source selection tags downloaded to and actually set in DCS
            List<BldrSrcSlctfbTids> BldrSrcSlctfbTidsList = await _repository.GetBldrSrcSlctfbTids(vntBldrsData[intBldrIdx].Id, curblend.lngID);
            
            vntSrcTankID = BldrSrcSlctfbTidsList[0].TankId;
            vntBldrSrcSlctFbTid = BldrSrcSlctfbTidsList[0].SelectionFbTid;
            foreach (BldrSrcSlctfbTids BldrSrcSlctfbTidsObj in BldrSrcSlctfbTidsList)            
            {
                // Skip if tag is null
                if (vntBldrSrcSlctFbTid != null)
                {
                    
                    DataRes = await _repository.GetTagNameAndVal(vntBldrSrcSlctFbTid);
                    tagBldrSrcSlctFb.vntTagName = DataRes.Name;
                    tagBldrSrcSlctFb.vntTagVal = DataRes.ReadValue.ToString();

                    if (Convert.ToInt32(tagBldrSrcSlctFb.vntTagVal) != (int)OnOff.ON_)
                    {
                        strTankName = await _repository.GetTankName(vntSrcTankID);
                        // warn msg "Comp tank ^1 requested by ABC not the same as used in DCS BY ^2
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN64), programName, "BL-" + curblend.lngID, strTankName, ("BLEND " + curblend.strName),
                      "","", "", "", res);                       
                    }

                }

                // Get all the stations used by this component/lineup
                // $$$$$$$$$$$$$$$
                if (((gstrDownloadType == "STATION") || (gstrDownloadType == "LINEUP")))
                {
                    // Check the comp tank index fb selection
                    lngLineupID = (BldrSrcSlctfbTidsObj.LineUpId == null) ? -1 : Convert.ToDouble(BldrSrcSlctfbTidsObj.LineUpId);
                    lngMatId = (BldrSrcSlctfbTidsObj.MatId == null) ? -1 : Convert.ToDouble(BldrSrcSlctfbTidsObj.MatId);
                    List<AbcTanks> DataTankIDData = await _repository.GetDataTankID(vntSrcTankID);
                    
                    strTankName = DataTankIDData[0].Name;
                    intDCSTankNum = (DataTankIDData[0].DcsTankNum == null)?-1: Convert.ToInt32(DataTankIDData[0].DcsTankNum);

                    List<BldrStationsData> BldrStationsDataList = await _repository.GetBldrStationsData(lngLineupID,vntBldrsData[intBldrIdx].Id);

                    foreach (BldrStationsData BldrStationsDataObj in BldrStationsDataList)                    
                    {
                        lngTankSelFbTID = (BldrStationsDataObj.TankFeedbackTid==null)? -1: Convert.ToDouble(BldrStationsDataObj.TankFeedbackTid);
                        lngLineupSelFBTID = (BldrStationsDataObj.LineupFeedbackTid == null) ? -1 : Convert.ToDouble(BldrStationsDataObj.LineupFeedbackTid);
                        // feedback warnings the Station interface
                        if (lngTankSelFbTID != -1)
                        {
                            // Get ABC Tank Selected DCS Index and compare with the index tank sel feedback
                            DataRes = await _repository.GetTagNameAndVal(lngTankSelFbTID);
                            tagBldrSrcSlctFb.vntTagName = DataRes.Name;
                            tagBldrSrcSlctFb.vntTagVal = DataRes.ReadValue.ToString();
                            
                            if (Convert.ToInt32(tagBldrSrcSlctFb.vntTagVal) != intDCSTankNum)
                            {
                                // warn msg "Comp tank ^1 requested by ABC not the same as used in DCS BY ^2
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN64), programName, "BL-" + curblend.lngID, strTankName, ("BLEND " + curblend.strName),
                                "", "", "", "", res);
                            }

                        }

                        if (lngLineupSelFBTID != -1)
                        {
                            // get DCS Lineup index if selected lineup id is not null
                            intDCSLineupNum = -1;
                            if (lngLineupID != -1)
                            {
                                AbcCompLineups DCSCompLineupNumData = await _repository.GetDCSCompLineupNum(lngLineupID);
                                intDCSLineupNum = Convert.ToInt32(DCSCompLineupNumData.DcsLineupNum);
                                strLineupName = DCSCompLineupNumData.Name;
                            }

                            // Get ABC Tank Selected DCS Index and compare with the index tank sel feedback
                            DataRes = await _repository.GetTagNameAndVal(lngLineupSelFBTID);
                            tagBldrSrcSlctFb.vntTagName = DataRes.Name;
                            tagBldrSrcSlctFb.vntTagVal = DataRes.ReadValue.ToString();
                            
                            if (Convert.ToInt32(tagBldrSrcSlctFb.vntTagVal) != intDCSLineupNum)
                            {
                                // get the comp name
                                strCompName = await _repository.GetCompName(lngMatId);

                                // IN BLEND ^1, COMP ^2, DCS LINEUP NUM IS NULL FOR LINEUP ^3.  CMD SEL/PRESEL IGNORED
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN96), programName, "BL-" + curblend.lngID, curblend.strName, strCompName,
                                strLineupName, "", "", "", res);                                
                            }
                        }
                    }                    

                    // get the blender_comps.lineup_feedback_tid
                    lngLineupSelFBTID = -1;

                    List<AbcBlenderComps> AllBldrCompsData = await _repository.GetAllBldrComps(vntBldrsData[intBldrIdx].Id);
                    List<AbcBlenderComps> AllBldrCompsDataFlt = new List<AbcBlenderComps>();
                    if (AllBldrCompsData.Count()>0)
                    {
                        AllBldrCompsDataFlt = AllBldrCompsData.Where<AbcBlenderComps>(row => row.MatId == lngMatId).ToList();
                        
                        if (AllBldrCompsDataFlt.Count()>0)
                        {
                            lngLineupSelFBTID = (AllBldrCompsDataFlt[0].LineupFeedbackTid == null)?-1: Convert.ToDouble(AllBldrCompsDataFlt[0].LineupFeedbackTid);
                        }

                    }                    

                    // Feb. 03: Download lineup indexes to DCS using the blender comps interface
                    if (lngLineupSelFBTID != -1)
                    {
                        // get DCS Lineup index if selected lineup id is not null
                        intDCSLineupNum = -1;
                        if (lngLineupID != -1)
                        {
                            AbcCompLineups Data = await _repository.GetDCSCompLineupNum(lngLineupID);
                            intDCSLineupNum = Convert.ToInt32(Data.DcsLineupNum);
                            strLineupName = Data.Name;
                        }

                        // Get ABC Tank Selected DCS Index and compare with the index tank sel feedback
                        DataRes = await _repository.GetTagNameAndVal(lngLineupSelFBTID);
                        tagBldrSrcSlctFb.vntTagName = DataRes.Name;
                        tagBldrSrcSlctFb.vntTagVal = DataRes.ReadValue.ToString();
                        
                        if (Convert.ToInt32(tagBldrSrcSlctFb.vntTagVal) != intDCSLineupNum)
                        {
                            // get the comp name
                            strCompName = await _repository.GetCompName(lngMatId);

                            // IN BLEND ^1, COMP ^2, DCS LINEUP NUM IS NULL FOR LINEUP ^3.  CMD SEL/PRESEL IGNORED
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN96), programName, "BL-" + curblend.lngID, curblend.strName, strCompName,
                                strLineupName, "", "", "", res);                           
                        }
                    }
                }// download is station/lineup based
            }
            
            strFlushSwgState = "";

            List<AbcBlendDest> DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);
            List<AbcBlendDest> DestTkFlagsFlt = new List<AbcBlendDest>();

            if (DestTkFlags.Count() > 0)
            {
                // Find the in_use_flag=YES record
                DestTkFlagsFlt = DestTkFlags.Where<AbcBlendDest>(row => row.InUseFlag == "YES").ToList();
                
                if (DestTkFlagsFlt.Count() > 0)
                {
                    intTankID = Convert.ToInt32(DestTkFlagsFlt[0].TankId);
                }

                // Find if flush_tk_flag=YES for at least one of the records
                DestTkFlagsFlt = DestTkFlags.Where<AbcBlendDest>(row => row.FlushTkFlag == "YES").ToList();
                
                if (DestTkFlagsFlt.Count()>0)
                {
                    lngFlushTankId = DestTkFlagsFlt[0].TankId;
                }

                if ((intDestTankID != -1) && (lngFlushTankId != -1))
                {

                    // get trasfer line vol from flush tank to destination tank
                    List<AbcBlendSwings> BldSwgTransferVolData = await _repository.GetBldSwgTransferVol(curblend.lngID,lngFlushTankId,intTankID);
                    
                    if (BldSwgTransferVolData.Count() > 0)
                    {
                        strFlushSwgState = (BldSwgTransferVolData[0].SwingState == null)?"": BldSwgTransferVolData[0].SwingState;
                    }
                    else
                    {
                        strFlushSwgState = "";
                    }
                }
            }           

            // Compare the curr blend vol > tolerance*transfer_vol_line to issue a message
            if ((strFlushSwgState == "") || (strFlushSwgState == "COMPLETE"))
            {
                vntPrdLnupSlctFbTid = -1;
                List<AbcBlenderDest> BldrDestSelTid = await _repository.GetBldrDestSelTid(vntBldrsData[intBldrIdx].Id,intDestTankID);
                if (BldrDestSelTid.Count() > 0)
                {
                    vntPrdLnupSlctFbTid = BldrDestSelTid[0].SelectionFbTid;
                }

                // Skip if tag is null
                if (Convert.ToDouble(vntPrdLnupSlctFbTid) != -1)
                {
                    DataRes = await _repository.GetTagNameAndVal(vntPrdLnupSlctFbTid);
                    tagPrdLnupSlctFb.vntTagName = DataRes.Name;
                    tagPrdLnupSlctFb.vntTagVal = DataRes.ReadValue.ToString();
                   
                    if (Convert.ToInt32(tagPrdLnupSlctFb.vntTagVal) != (int)OnOff.ON_)
                    {
                        strTankName = await _repository.GetTankName(intDestTankID);

                        // warn msg "Dest tank ^1 requested by ABC not the same as used in DCS BY ^2
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN65), programName, "BL-" + curblend.lngID, strTankName, ("BLEND " + curblend.strName),
                               "", "", "", "", res);                        
                    }

                }

                if ((gstrDownloadType == "STATION") || (gstrDownloadType == "LINEUP"))
                {
                    //  Blenders.tank_FEEDBACK_tid and
                    // blenders.lineup_feedback_tid
                    lngTankSelFbTID = -1;
                    lngLineupSelFBTID = -1;

                    List<AbcBlenders> BldrLineupTags =  await _repository.GetBldrLineupTags(vntBldrsData[intBldrIdx].Id);
                    
                    if (BldrLineupTags.Count() > 0)
                    {
                        lngTankSelFbTID = (BldrLineupTags[0].TankFeedbackTid == null)?-1: Convert.ToDouble(BldrLineupTags[0].TankFeedbackTid);
                        lngLineupSelFBTID = (BldrLineupTags[0].LineupFeedbackTid == null) ? -1 : Convert.ToDouble(BldrLineupTags[0].LineupFeedbackTid);
                    }
                    
                    if (lngTankSelFbTID != -1)
                    {
                        // Get dest data
                        List<AbcTanks> DataTankID = await _repository.GetDataTankID(intDestTankID);
                        
                        strTankName = DataTankID[0].Name;
                        intDCSTankNum = (DataTankID[0].DcsTankNum == null) ? -1 : Convert.ToInt32(DataTankID[0].DcsTankNum);

                        // Get ABC Tank Selected DCS Index and compare with the index tank sel feedback
                        DataRes = await _repository.GetTagNameAndVal(lngTankSelFbTID);
                        tagBldrSrcSlctFb.vntTagName = DataRes.Name;
                        tagBldrSrcSlctFb.vntTagVal = DataRes.ReadValue.ToString();
                        
                        if (Convert.ToInt32(tagBldrSrcSlctFb.vntTagVal) != intDCSTankNum)
                        {
                            // warn msg "Dest tank ^1 requested by ABC not the same as used in DCS BY ^2
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN65), programName, "BL-" + curblend.lngID, strTankName, ("BLEND " + curblend.strName),
                              "", "", "", "", res);
                        }

                    }

                    if (lngLineupSelFBTID != -1)
                    {
                        // get Lineup_id for the dest tank                        

                        List<AbcBlendDest> TkDestData = await _repository.GetTkDestData(curblend.lngID,intDestTankID);
                        lngLineupID = (TkDestData[0].LineupId == null)?-1: TkDestData[0].LineupId;
                        
                        // *************
                        // get DCS Lineup index if selected lineup id is not null
                        intDCSLineupNum = -1;
                        if (lngLineupID != -1)
                        {
                            AbcCompLineups DCSCompLineupNum =  await _repository.GetDCSCompLineupNum(lngLineupID);
                            intDCSLineupNum = Convert.ToInt32(DCSCompLineupNum.DcsLineupNum);
                            strLineupName = DCSCompLineupNum.Name;                            
                        }

                        // Get ABC Tank Selected DCS Index and compare with the index tank sel feedback
                        DataRes = await _repository.GetTagNameAndVal(lngLineupSelFBTID);
                        tagBldrSrcSlctFb.vntTagName = DataRes.Name;
                        tagBldrSrcSlctFb.vntTagVal = DataRes.ReadValue.ToString();
                        
                        if (Convert.ToInt32(tagBldrSrcSlctFb.vntTagVal) != intDCSLineupNum)
                        {
                            // IN BLEND ^1, DEST ^2, PROD DCS LINEUP NUM IS NULL FOR LINEUP ^2.  CMD SEL/PRESEL IGNORED
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN98), programName, "BL-" + curblend.lngID, curblend.strName, strTankName,
                              strLineupName, "", "", "", res);
                        }
                    }
                }
                // end station/lineup based download
            }

            // flushing condition
            // set pacing_act_flag in ABC_BLENDS according to station pacing flag tag
            strPacingFlag = "NO";
            blnCompPacing = false;
           List<AbcStations> StationPaceTids = await _repository.GetStationPaceTids(curblend.lngID);

            foreach (AbcStations StationPaceTidObj in StationPaceTids)            
            {
                lngStationId = StationPaceTidObj.Id;
                lngStationPacingTid = (StationPaceTidObj.PaceMeFlagTid == null)?-1: Convert.ToDouble(StationPaceTidObj.PaceMeFlagTid);
                if (lngStationPacingTid != -1)
                {
                    // Get the Material associated with the current station from abc_blend_stations
                    List<double> BldStationMatId = await _repository.GetBldStationMatId(curblend.lngID,lngStationId);
                    
                    if (BldStationMatId.Count()>0)
                    {
                        lngMatId = BldStationMatId[0];
                        // Get the tag value from abc_tags                        

                        DataRes = await _repository.GetTagNameAndVal(lngStationPacingTid);
                        tagStationPacing.vntTagName = DataRes.Name;
                        tagStationPacing.vntTagVal = DataRes.ReadValue.ToString();

                        if (Convert.ToInt32(tagStationPacing.vntTagVal) == (int)YesNo.YES)
                        {
                            //  update abc_blend_comps.pacing_factor according with the pacing factor in the DCS
                            await _repository.SetBlendCompPacingFactor(1, curblend.lngID, lngMatId);                            
                            strPacingFlag = "YES";
                            blnCompPacing = true;
                        }
                        else if (Convert.ToInt32(tagStationPacing.vntTagVal) == (int)YesNo.NO)
                        {
                            if (((blnCompPacing == false) || ((blnCompPacing == true) && (lngPrevMatId != lngMatId))))
                            {
                                // update abc_blend_comps.pacing_factor according with the pacing factor in the DCS
                                await _repository.SetBlendCompPacingFactor(0, curblend.lngID, lngMatId);
                            }

                        }

                        // Save previous mat id
                        lngPrevMatId = lngMatId;
                    }
                }
            }

            // Set the overall pacing flag if at least one component (station) is pacing
            await _repository.SetPaceActFlag(strPacingFlag, curblend.lngID);
            return 0;
        }

        private async Task<string> DefLineupExists(string strBlend, string strTank)
        {
            string rtnData = "";
            List<double> lineupIds = await _repository.GetDefaultLineupIds(Convert.ToDouble(strBlend), Convert.ToDouble(strTank));
                      
            if (lineupIds.Count() == 0)
            {
                rtnData = "NULL";
            }
            else if (lineupIds[0] == null)
            {
                rtnData = "NULL";
            }
            else
            {
                rtnData = lineupIds[0].ToString();
            }

            return rtnData;
        }

        private async Task<int> DestTankChanged(string strTankId, string strNewBlendId, string strOldTankId)
        {
            string strExecute;
            // To hold the SQLEXEC for null testing
            bool strError = false;
            string strLineupID;
            double dblHeelVol;
            double dblDestVolume;
            object vntBookmark;
            bool blnFlushing = false;
            bool blnTransaction;
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            //  insert tank property records where needed */
            strError = await _repository.TPCreatePkg(Convert.ToDouble(strTankId), strError);
            // Set the tank property value and time to the default
            strError = await _repository.TPUpdateDefvalPkg(Convert.ToDouble(strTankId),strError);
            //  insert lab tank props
            strError = await _repository.TPCreateLabPkg(Convert.ToDouble(strTankId),strError);            
            
            blnTransaction = true;
            await _repository.DeleteAbcBlendDestProps(Convert.ToDouble(strNewBlendId), Convert.ToDouble(strOldTankId));

            await _repository.DeleteAbcBlendDestSeq(Convert.ToDouble(strNewBlendId), Convert.ToDouble(strOldTankId));

            //  remove blend dest record if exists and insert blend dest new record
            await _repository.DeleteAbcBlendDest(Convert.ToDouble(strNewBlendId), Convert.ToDouble(strOldTankId));

            await _repository.DeleteAbcBlendDSwings(Convert.ToDouble(strNewBlendId), Convert.ToDouble(strOldTankId));

            await _repository.DeleteAbcBlendDSwings2(Convert.ToDouble(strNewBlendId));

            await _repository.DeleteAbcBlendDSwings3(Convert.ToDouble(strNewBlendId));
            
            strLineupID = await DefLineupExists(strNewBlendId, strTankId);

            if (strLineupID != "NULL" || strLineupID != null)
            {
                
                blnTransaction = false;
                
                //--------------------------- validate---------------------------------

                List<AbcBlendDest> DestTkFlags = await _repository.GetDestTkFlags(Convert.ToDouble(strNewBlendId));
                List<AbcBlendDest> DestTkFlagsFlt = new List<AbcBlendDest>();
                if (DestTkFlags.Count() > 0)
                {
                    blnFlushing = false;
                    DestTkFlagsFlt = DestTkFlags.Where<AbcBlendDest>(row => row.FlushTkFlag == "YES").ToList();
                   
                    if (DestTkFlagsFlt.Count() > 0)
                    {
                        blnFlushing = true;
                    }

                }

                // get the heel volume
                dblHeelVol = Convert.ToDouble(await _repository.GetHeelVol(Convert.ToDouble(strTankId)));

                List<DCSProdLineupNum> DCSProdLineupNumData =  await _repository.GetDCSProdLineupNum(Convert.ToDouble(strLineupID));
                
                dblDestVolume = (DCSProdLineupNumData[0].DestLineVolume == null) ? 0 : Convert.ToDouble(DCSProdLineupNumData[0].DestLineVolume);
                
                dblHeelVol = (dblHeelVol + dblDestVolume);
                await _repository.SetAbcBlendDestData(Convert.ToDouble(strNewBlendId));
               
                if (blnFlushing == true)
                {
                    // update new records into abc_blend_dest and set flush tank ='YES' for the tk In use
                    await _repository.SetAbcBlendDestData2(Convert.ToDouble(strNewBlendId), Convert.ToDouble(strTankId), dblHeelVol, Convert.ToDouble(strLineupID));                   
                }
                else
                {
                    // update new records into abc_blend_dest
                    await _repository.SetAbcBlendDestData3(Convert.ToDouble(strNewBlendId), Convert.ToDouble(strTankId), dblHeelVol, Convert.ToDouble(strLineupID));                    
                }

                // Delete props from blend_Props id they exist
                await _repository.DeleteAbcBlendDestProps(Convert.ToDouble(strNewBlendId), Convert.ToDouble(strTankId));

                //  insert blend dest prop records
                await _repository.InsertAbcBlendDestProps(Convert.ToDouble(strNewBlendId), Convert.ToDouble(strTankId));
                
                await _repository.DeleteAbcBlendDestProps(Convert.ToDouble(strNewBlendId), Convert.ToDouble(strTankId));
                
                await _repository.InsertAbcBlendDestSeq(Convert.ToDouble(strNewBlendId), Convert.ToDouble(strTankId));
            }
            else
            {               
                blnTransaction = false;
            }

            return 0;
        }

        private async Task<string> GetSwgCriteria(string strCriteriaName)
        {
            List<double> Criteriaid = await _repository.GetCriteriaId(strCriteriaName);
            
            if (Criteriaid.Count() > 0)
            {
                return null;
            }
            else
            {
                return Criteriaid[0].ToString();
            }
            
        }
        private async Task<string> UpdateProdTank(string strOldBlendName, string strNewBlendName, string strNewBlendId, int intDestTankID, string strMatName, string strBlenderName, double dblOnSpecVol)
        {
            string strExecute;
            string strPout;
            double strTankId;
            string strTankName;
            double dblMinvol;
            double dblMaxVol;
            double dblAvailVol;
            double dblAvailSpace;
            double dblAvailComp;
            double dblDesVol;
            string strLineupID;
            string strSwingCriteriaID;
            int intRecordCount;
            int intPosDestTankId = 0;
            var res = "";
            string rtnData = "";
            List<double> BlendSwingData = await _repository.GetBlendSwingData(intDestTankID, strOldBlendName);
           
            if (BlendSwingData.Count() > 0)
            {
                intRecordCount = BlendSwingData.Count();
                
                strTankId = BlendSwingData[0];
                strTankName = await _repository.GetTankName(strTankId);
                if ((intRecordCount > 1))
                {
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN73), "DEST_TANK", "BL-" + strNewBlendId, strOldBlendName, strTankName,
                             "", "", "", "", res);
                }

                //   Change the destination tank where it is needed
                await DestTankChanged(strTankId.ToString(),strNewBlendId,intDestTankID.ToString());
                // Update the batch size=Available space in the new dest tank
                List<ASTankID> ASTankIDData = await _repository.GetASTankID((int)strTankId);

                dblMinvol = (ASTankIDData[0].MinVol == null) ? 0 : Convert.ToDouble(ASTankIDData[0].MinVol);
                dblMaxVol = (ASTankIDData[0].MaxVol == null) ? 0 : Convert.ToDouble(ASTankIDData[0].MaxVol);
                dblAvailVol = (ASTankIDData[0].AvailVol == null) ? 0 : Convert.ToDouble(ASTankIDData[0].AvailVol);
                dblAvailSpace = (dblMaxVol - (dblAvailVol - dblMinvol));

                await _repository.SetBlendTargetVol(dblAvailSpace, Convert.ToDouble(strNewBlendId));
                
                // Update the desired Vol of the new blend=abc_blenders.on-spec_vol*target_Vol
                dblDesVol = (dblOnSpecVol * (dblAvailSpace / 100));
                await _repository.SetBlendDesOnSpecVol(dblDesVol, Convert.ToDouble(strNewBlendId));

                //  Post Destination Tank id for the new blend order
                List<double> TankIds = await _repository.GetBlendOrderTankData(strBlenderName, strMatName, strTankId);
                
                // loop all the tanks to find the available space
                dblAvailComp = -1;
                foreach (double id in TankIds)               
                {
                    List<ASTankID> ASTankIDList =  await _repository.GetASTankID((int)id);

                    dblMinvol = (ASTankIDList[0].MinVol == null) ? 0 : Convert.ToDouble(ASTankIDList[0].MinVol);
                    dblMaxVol = (ASTankIDList[0].MaxVol == null) ? 0 : Convert.ToDouble(ASTankIDList[0].MaxVol);
                    dblAvailVol = (ASTankIDList[0].AvailVol == null) ? 0 : Convert.ToDouble(ASTankIDList[0].AvailVol);                    
                    dblAvailSpace = (dblMaxVol - (dblAvailVol - dblMinvol));
                    if ((dblAvailSpace > dblAvailComp))
                    {
                        dblAvailComp = dblAvailSpace;
                        intPosDestTankId = (int)id;
                    }
                }

                //  Create the additional product tank
                strLineupID = await DefLineupExists(strNewBlendId, intPosDestTankId.ToString());
                if (strLineupID != "NULL")
                {
                    //  insert new tank
                    // Get the abc_dest_tanks.flush_tk_flag                   
                    List<AbcBlendDest> DestTkFlags = await _repository.GetDestTkFlags(Convert.ToDouble(strNewBlendId));
                    List<AbcBlendDest> DestTkFlagsFlt = new List<AbcBlendDest>();

                    if (DestTkFlags.Count() > 0)
                    {
                        // Find if TANK_ID=ADDITIONAL TANK ID
                        DestTkFlagsFlt = DestTkFlags.Where<AbcBlendDest>(row => row.TankId == intPosDestTankId).ToList();
                        
                        if (DestTkFlagsFlt.Count() == 0)
                        {
                            await _repository.InsertAbcBlendDest(intPosDestTankId, Convert.ToDouble(strNewBlendId), Convert.ToDouble(strLineupID));
                        }
                    }

                    List<BlendSwingsData> BlendSwingsDataList = await _repository.BlendSwingsData("PRODUCT",Convert.ToInt32(strTankId),Convert.ToDouble(strNewBlendId));
                    List<BlendSwingsData> BlendSwingsDataListFlt = new List<BlendSwingsData>();

                    if (BlendSwingsDataList.Count() == 0)
                    {
                        // Get the criteria Id from the given name
                        strSwingCriteriaID = await GetSwgCriteria("HIGH LIMIT");
                        await _repository.InsertBlendSwingData(Convert.ToDouble(strNewBlendId), Convert.ToDouble(strTankId), Convert.ToDouble(intPosDestTankId), Convert.ToDouble(strSwingCriteriaID));
                    }
                    else
                    {
                        BlendSwingsDataListFlt = BlendSwingsDataList.Where<BlendSwingsData>(row => row.ToTkId == intPosDestTankId).ToList();
                        
                        if (BlendSwingsDataListFlt.Count() == 0)
                        {
                            // Get the criteria Id from the given name
                            strSwingCriteriaID = await GetSwgCriteria("HIGH LIMIT");
                            await _repository.InsertBlendSwingData(Convert.ToDouble(strNewBlendId), Convert.ToDouble(strTankId), Convert.ToDouble(intPosDestTankId), Convert.ToDouble(strSwingCriteriaID));                            
                        }
                        else
                        {
                            // Get the criteria Id from the given name
                            strSwingCriteriaID = await GetSwgCriteria("HIGH LIMIT");
                            await _repository.SetBlendSwingData(strSwingCriteriaID, Convert.ToDouble(strNewBlendId), Convert.ToDouble(strTankId), Convert.ToDouble(intPosDestTankId));                            
                        }
                    }
                }
                else
                {
                    //  Issue a message that Post Blend dest Tank was not created
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN77), "POST_DEST_TANK", "BL-" + strNewBlendId, strNewBlendName, "",
                             "", "", "", "", res);
                }

                rtnData = strTankId.ToString();
            }
           
            return rtnData;
        }
        private async Task<int> UpdateBOProps(string strBlend, string strBlender, string strProduct, string strGrade)
        {
            string strExecute;
            string strPropID;
            string strControlMin;
            string strControlMax;
            string strSalesMin;
            string strSalesMax;
            string strCost;
            List <AbcPrdPropSpecs> AbcPrdPropSpecsData = await _repository.GetAbcPrdPropSpecs(strProduct, strGrade, strBlender, strBlend);
           
            if (AbcPrdPropSpecsData.Count() > 0)
            {
                foreach (AbcPrdPropSpecs AbcPrdPropSpecsObj in AbcPrdPropSpecsData)
                {
                    strPropID = AbcPrdPropSpecsObj.PropId.ToString();
                    if (AbcPrdPropSpecsObj.Giveawaycost != null)
                    {
                        strCost = AbcPrdPropSpecsObj.Giveawaycost.ToString();
                    }
                    else
                    {
                        strCost = null;
                    }

                    if (AbcPrdPropSpecsObj.ControlMin != null)
                    {
                        strControlMin = AbcPrdPropSpecsObj.ControlMin.ToString();
                    }
                    else
                    {
                        strControlMin = null;
                    }

                    if (AbcPrdPropSpecsObj.ControlMax != null)
                    {
                        strControlMax = AbcPrdPropSpecsObj.ControlMax.ToString();
                    }
                    else
                    {
                        strControlMax = null;
                    }

                    if (AbcPrdPropSpecsObj.SalesMin != null)
                    {
                        strSalesMin = AbcPrdPropSpecsObj.SalesMin.ToString();
                    }
                    else
                    {
                        strSalesMin = null;
                    }

                    if (AbcPrdPropSpecsObj.SalesMax != null)
                    {
                        strSalesMax = AbcPrdPropSpecsObj.SalesMax.ToString();
                    }
                    else
                    {
                        strSalesMax = null;
                    }

                    await _repository.SetAbcBlendPropsData(strBlend, Convert.ToDouble(strPropID), Convert.ToDouble(strCost), Convert.ToDouble(strControlMin),
                        Convert.ToDouble(strControlMax), Convert.ToDouble(strSalesMin), Convert.ToDouble(strSalesMax));
                }

            }
          
            return 0;
        }
        private async Task<int> SetExtraBOProps(string strBlend, string strBlender, string strProduct, string strGrade)
        {
            string strExecute;
            string strPropID;
            double? controlMin;
            double? controlMax;
            double? salesMin;
            double? salesMax;
            double? cost;
            List <AbcPrdPropSpecs> AbcPrdPropSpecs2  = await _repository.GetAbcPrdPropSpecs2(strProduct, strGrade, strBlender, strBlend);
            
            if (AbcPrdPropSpecs2.Count() > 0)
            {
                foreach (AbcPrdPropSpecs AbcPrdPropSpecsObj in AbcPrdPropSpecs2)                
                {
                    strPropID = AbcPrdPropSpecsObj.PropId.ToString();
                    if (AbcPrdPropSpecsObj.Giveawaycost != null)
                    {
                        cost = AbcPrdPropSpecsObj.Giveawaycost;
                    }
                    else
                    {
                        cost = null;
                    }

                    if (AbcPrdPropSpecsObj.ControlMin != null)
                    {
                        controlMin = AbcPrdPropSpecsObj.ControlMin;
                    }
                    else
                    {
                        controlMin = null;
                    }

                    if (AbcPrdPropSpecsObj.ControlMax != null)
                    {
                        controlMax = AbcPrdPropSpecsObj.ControlMax;
                    }
                    else
                    {
                        controlMax = null;
                    }

                    if (AbcPrdPropSpecsObj.SalesMin != null)
                    {
                        salesMin = AbcPrdPropSpecsObj.SalesMin;
                    }
                    else
                    {
                        salesMin = null;
                    }

                    if (AbcPrdPropSpecsObj.SalesMax != null)
                    {
                        salesMax = AbcPrdPropSpecsObj.SalesMax;
                    }
                    else
                    {
                        salesMax = null;
                    }

                    await _repository.InsertAbcBlendPropsData(strBlend, Convert.ToDouble(strPropID), cost, controlMin, controlMax, salesMin, salesMax);                    
                }                
            }

            return 0;
        }
        private async Task<int> SetMinMax(string strBlend, string strBlender, string strProduct)
        {
            string strExecute;
            double Min;
            double Max;
            double PropID;
            List<AbcPrdgrpMatProps> PrdgrpMatPropData =  await _repository.GetPrdgrpMatPropData(strBlender, strProduct);
            
            if (PrdgrpMatPropData.Count() > 0)
            {
                //         abcdataenv.oracleconn.BeginTrans
                foreach (AbcPrdgrpMatProps PrdgrpMatPropDataObj in PrdgrpMatPropData)                
                {
                    PropID = PrdgrpMatPropDataObj.PropId;

                    if (PrdgrpMatPropDataObj.ValidMin != null)
                    {
                        Min = PrdgrpMatPropDataObj.ValidMin;
                        await _repository.SetAbcBlendPropsValidMin(strBlend, PropID, Min);                        
                    }

                    if (PrdgrpMatPropDataObj.ValidMax != null)
                    {
                        Max = PrdgrpMatPropDataObj.ValidMax;
                        await _repository.SetAbcBlendPropsValidMin(strBlend, PropID, Max);
                    }                    
                }                
            }
        
            return 0;
        }
        private async Task<int> SetControlled(string blendName)
        {
            await _repository.SetAbcBlendProps(blendName);
            await _repository.SetAbcBlendProps2(blendName);
            return 0;
        }
        private async Task<int> SetBlendPropAnz(string blendName)
        {
            await _repository.SetAbcBlendPropsResTagId(blendName);
            await _repository.SetAbcBlendPropsAnzOffset(blendName);
            return 0;
        }
        private async Task<int> UpdateInitRcpBias(string strOldBlendId, string strNewBlendName)
        {
            // Update the Bias from the last Interval of the previous Blend into
            // the Initial Bias of the new Blend
            List<double> sequence = await _repository.GetBlendIntervalSequence(Convert.ToDouble(strOldBlendId));
            double? Sequence = 0;
            if (sequence.Count() > 0)
            {
                
                if (sequence[0] == null)
                {
                    Sequence = null;
                }
                else
                {
                    Sequence = sequence[0];
                }
            }
            else
            {
                Sequence = null;
            }
           
            if (Sequence != null)
            {
                await _repository.SetAbcBlendPropsIntrvBias(strNewBlendName, (double)Sequence, Convert.ToDouble(strOldBlendId));

                // Update Initial Rcp from the last interval of the previous blend into
                // The Plan Recipe of the New Blend
                // Update the Tank Min Constraints if needed
                await _repository.SetAbcBlendCompTankMin(Convert.ToDouble(strOldBlendId), (double)Sequence, strNewBlendName);

                // Update the Tank Max Constraints if needed
                await _repository.SetAbcBlendCompTankMax(Convert.ToDouble(strOldBlendId), (double)Sequence, strNewBlendName);

                // update the Int_Recipe
                await _repository.SetAbcBlendCompPlanRecipe(Convert.ToDouble(strOldBlendId), (double)Sequence, strNewBlendName);                
            }

            // Update the bacth target vol from the previuos volume
            await _repository.SetAbcBlendBatchTargetVolume(Convert.ToDouble(strOldBlendId), (double)Sequence, strNewBlendName);

            // update the Rcp Constraint Type
            await _repository.SetAbcBlendCompRcpConstraintType(Convert.ToDouble(strOldBlendId), (double)Sequence, strNewBlendName);
            
            return 0;
        }
        private async Task<int> ActionsApprove(CurBlendData curblend, int intTankID, string strNewBlendId, string strNewBlendName)
        {
            string strSQL;
            string strTotRec;
            double lngHITot = 0;
            double lngLOTot = 0;
            bool blnNoLineup = false;
            bool blnOk;
            string strBlendID;
            string strUsageName;
            bool blnNotApproved;
            var res = "";
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            blnNotApproved = true;
            if ((intTankID == 0))
            {
                blnNotApproved = false;
                goto NOT_APPROVED;
            }

            //     approve total of Low_cons and High_cons
            List<RecipeHdr> RecipeHdrData =  await _repository.RecipeHdr(Convert.ToDouble(strNewBlendId));
            if (RecipeHdrData.Count() > 0)
            {
                
                lngHITot = 0;
                lngLOTot = 0;
                foreach (RecipeHdr RecipeHdrObj in RecipeHdrData)                
                {
                    strUsageName = await GetBldMatUsage(Convert.ToDouble(strNewBlendId), RecipeHdrObj.MatId);
                    if (strUsageName != "ADDITIVE")
                    {
                        if (RecipeHdrObj.Maximum != null)
                        {
                            lngHITot = lngHITot + Convert.ToDouble(RecipeHdrObj.Maximum);
                        }

                        if (RecipeHdrObj.Minimum != null)
                        {
                            lngLOTot = lngLOTot + Convert.ToDouble(RecipeHdrObj.Minimum);
                        }
                    }
                }
            }
            else
            {                
                blnNotApproved = false;
                goto NOT_APPROVED;
            }

            if (lngHITot < 100)
            {               
                blnNotApproved = false;
                goto NOT_APPROVED;
            }

            if ((lngLOTot > 100))
            {
                
                blnNotApproved = false;
                goto NOT_APPROVED;
            }

            //  approve total of tank min and max
            List<RecipeBlend> RecipeBlendData = await _repository.RecipeBlend(Convert.ToDouble(strNewBlendId));
            if (RecipeBlendData.Count() > 0)
            {                
                lngHITot = 0;
                lngLOTot = 0;
                foreach (RecipeBlend RecipeBlendObj in RecipeBlendData)
                {
                    strUsageName = await GetBldMatUsage(Convert.ToDouble(strNewBlendId), RecipeBlendObj.MatId);
                    if ((strUsageName != "ADDITIVE"))
                    {
                        if (RecipeBlendObj.TankMax != null)
                        {
                            lngHITot = lngHITot + Convert.ToDouble(RecipeBlendObj.TankMax);
                        }

                        if (RecipeBlendObj.TankMin != null)
                        {
                            lngLOTot = lngLOTot + Convert.ToDouble(RecipeBlendObj.TankMin);
                        }
                    }                    
                }                
            }
            else
            {
                blnNotApproved = false;
                goto NOT_APPROVED;
            }

            if ((lngHITot < 100))
            {               
                blnNotApproved = false;
                goto NOT_APPROVED;
            }

            if ((lngLOTot > 100))
            {              
                blnNotApproved = false;
                goto NOT_APPROVED;
            }

            //  Check if every component is associated with a lineup
            if (RecipeHdrData.Count() > 0)
            {                
                blnNoLineup = false;
                foreach (RecipeHdr RecipeHdrObj in RecipeHdrData)                
                {
                    if (RecipeHdrObj.LineupId == null)
                    {
                        blnNoLineup = true;
                        break;
                    }
                }
            }

            if (blnNoLineup)
            {
                //             MsgBox "All lineups must be selected! Blend Order is not Approved.", _
                //                 vbOKOnly + vbExclamation, "Approve Error"
                blnNotApproved = false;
                goto NOT_APPROVED;
            }
            
            if (blnNotApproved == false)
            {           
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN74), "NOT_APPROVED_BO", strNewBlendName, "", "",
                              "", "", "", "", res);
                return 0;
            }
       
            // update the Prev_blend_id field
            await _repository.SetAbcBlendPrevId(curblend.lngID, Convert.ToDouble(strNewBlendId));

            // approve blend order in partial mode by setting state to ready
            await _repository.SetBlendState(Convert.ToDouble(strNewBlendId), "READY");

        NOT_APPROVED:           
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN74), "NOT_APPROVED_BO", strNewBlendName, "", "",
                              "", "", "", "", res);
                return 0;               
        }
        private async Task<string> DuplicateBO(CurBlendData curblend, double dblOnSpecVol)
        {
            string rtnData = "";
            string strID;
            string strBlendID;
            string strName;
            string strCopyName;
            string strUser;
            string strCopyOK = "";
            string strPout;
            string strBlendName;
            string strBlender;
            string strProduct;
            string strGrade;
            int intCounter;
            int intItemsCount;
            int intResponse;
            int intIndex;
            int intDestTankID;
            int intNewDestTankID;
            double dblAvailVol;
            DateTime datCurDate;
            string strBldDateTime;
            var res = "";
            strID = curblend.lngID.ToString();
            strCopyName = curblend.strName;
            strName = strCopyName.Substring(0, 16);
            // Create the new blend order name
            datCurDate = await _repository.GetCurTime();
            
            // RW 05-Dec-14 PQ-E0014
            // strBldDateTime = Format(datCurDate, "mmmdd,yy HhNnSsAM/PM")
            strBldDateTime = datCurDate.ToString();
            // RW 05-Dec-14 PQ-E0014
            strBlendName = ("BL-" + strBldDateTime.ToUpper());
            if ((strBlendName != ""))
            {
                //      DoEvents
                await _repository.BOCopyPkg(curblend.lngID, strBlendName,strCopyOK);
                if ((strCopyOK == "TRUE"))
                {
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN72), "COPY_BLEND_ORDER", "BL-" + strID, strBlendName, strName,
                              "BLMON PROGRAM", "", "", "", res);

                    //  get blend, blender,product and grade names
                    //         strBlendName = strName
                    strBlender = gstrBldrName;
                    AbcMaterials MatName = await _repository.GetMatName(curblend.intProdID);
                    strProduct = MatName.Name;

                    strGrade = await _repository.GetGradeName(curblend.intGrdID);
                    // get the new blend id
                    strBlendID = (await _repository.GetBlendId(strBlendName)).ToString();
                     
                    // Pass the new blend Id to the return value of the function
                    rtnData = strBlendID;
                    // get dest tank ID
                    intDestTankID = (int)await _repository.GetDestTankId(curblend.lngID);
                    // Get the new destination tank for the new blend
                    intNewDestTankID = Convert.ToInt32(await UpdateProdTank(strCopyName, strBlendName, strBlendID, intDestTankID, strProduct, strBlender, dblOnSpecVol));
                    //  Update blend property values - control_min,control_max,
                    //  calc_id, sales-min, sales_max, valid_min, valid-max, controlled,analyzer ID
                    await UpdateBOProps(strBlendName,strBlender,strProduct,strGrade);
                    //  Create new blend prop records if grade is not BASE and there are
                    //  extra properties defined for the selected grade
                    await SetExtraBOProps(strBlendName, strBlender, strProduct, strGrade);
                    //  Update the created prop records with their appropriate Valid Min and Max
                    await SetMinMax(strBlendName,strBlender,strProduct);
                    //  Update Controlled flag if Control Min and Max are Null
                    await SetControlled(strBlendName);
                    //  Update Blend Prop Anz ID from Anz Hdr Props
                    await SetBlendPropAnz(strBlendName);
                    //  Update Blend Prop Calc ID and GiveawayCost from Prdgrp Props
                    await _repository.SetAbcBlendPropsCalcAndCost(strBlendName,strBlender);
                    // Update Inital Recipe and Bias from the previous blend (last Interval)
                    await UpdateInitRcpBias(curblend.lngID.ToString(),strBlendName);

                    if ((gstrRundnFlag == "YES"))
                    {
                        // approval action from partial to ready
                        await ActionsApprove(curblend,intNewDestTankID,strBlendID,strBlendName);
                    }

                    // TODO: Exit Function: Warning!!! Need to return the value
                    return rtnData;
                }
                else
                {
                    return "";

                }

            }
            else
            {
                rtnData = "";
            }

            // TODO: Exit Function: Warning!!! Need to return the value
            return rtnData;
        }
        private async Task<int> SwingCompTank(int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend, DebugLevels enumDebugLevel)
        {
            string strSwingState;
            string strAutoSwingFlag;
            string strAbcServFlag;
            string strTankName;
            string strSrceTankName;
            string strToTankName;
            string strCriteriaName;
            double lngSwingTID;
            double lngSwingOccurredTID;
            double lngMatId;
            double lngToTankID;
            double lngPreselTID;
            double lngPreselOFFTID;
            double lngSelTID;
            double lngSelOFFTID;
            double lngTankSelectNumTid;
            double lngTankPreSelectNumTid;
            DcsTag tagSwingOccurred = new DcsTag();
            DcsTag tagPrdLnupSlctFb = new DcsTag(); ;
            DcsTag tagSwing = new DcsTag(); ;
            double dblSeqVolUsed;
            double dblMatVolUsed;
            double dblSumVolUsed;
            double dblCriteriaNumberLmt;
            double dblMinvol;
            double dblAvailVol;
            double dblMaxVol;
            double dblSwgTimeOut;
            int intSwingSeq;
            double lngSrcTankID = 0;
            double? vntDcsServTid;
            double sngCompVolume;
            DateTime dteCriteriaTimeLmt = new DateTime();
            bool blnRollBack;
            DcsTag tagPermissive = new DcsTag();
            double lngTankPreselTID;
            double lngLineupPreselTID;
            double lngTOLineupID = 0;
            double lngFromLineupID;
            double lngLineupSelTID;
            int intDCSTankNum = 0;
            int intDCSLineupNum;
            string strLineupName = "";
            string strCompName;
            // *****
            string res = "";
            //' COMPONENT SWING: Get blender source selection tags (Comp Current tanks in use)
            List<BldrSrcSlctfbTids> BldrSrcSlctfbTidsData = await _repository.GetBldrSrcSlctfbTids(vntBldrsData[intBldrIdx].Id, curblend.lngID);


            //get download OK tag (permissive tag) value from ABC_TAGS
            AbcTags DataRes = await _repository.GetTagNameAndVal(vntBldrsData[intBldrIdx].DownloadOkTid);
            tagPermissive.vntTagName = DataRes.Name;
            tagPermissive.vntTagVal = (DataRes.ReadValue.ToString() == null) ? ((int)OnOff.OFF).ToString() : DataRes.ReadValue.ToString();

            foreach (BldrSrcSlctfbTids BldrSrcSlctfbTidsObj in BldrSrcSlctfbTidsData)
            {
                lngSrcTankID = BldrSrcSlctfbTidsObj.TankId;
                lngFromLineupID = (BldrSrcSlctfbTidsObj.LineUpId == null) ? -1 : (double)BldrSrcSlctfbTidsObj.LineUpId;
                lngMatId = (BldrSrcSlctfbTidsObj.MatId == null) ? -1 : (double)BldrSrcSlctfbTidsObj.MatId;

                //'Get the blend swing data for a specific from tank
                List<BlendSwingsData> BlendSwingsDataList = await _repository.BlendSwingsData("COMPONENT", (int)lngSrcTankID, curblend.lngID);
                // Setting the preselection of all the tanks to OFF if there is no swing record configured for the tank in use
                if (BlendSwingsDataList.Count() > 0)
                {
                    // Get the abc_blender_sources.preselection_tid for all tanks with this material
                    List<AbcBlenderSources> BldrSrcPreselTIDData = await _repository.GetBldrSrcPreselTID(vntBldrsData[intBldrIdx].Id, curblend.lngID, lngMatId, "%");
                    foreach (AbcBlenderSources BldrSrcPreselTIDObj in BldrSrcPreselTIDData)
                    {
                        lngPreselTID = (BldrSrcPreselTIDObj.PreselectionTid == null) ? -1 : (double)BldrSrcPreselTIDObj.PreselectionTid;
                        //  Reset Write value=0 for the Preselection tid
                        if (lngPreselTID != -1 && Convert.ToInt32(tagPermissive.vntTagVal) == (int)OnOff.ON_)
                        {
                            await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngPreselTID);
                        }
                    }

                    // Get all the stations used by this component/lineup
                    List<BldrStationsData> BldrStationsDataList = await _repository.GetBldrStationsData(lngFromLineupID, vntBldrsData[intBldrIdx].Id);
                    foreach (BldrStationsData BldrStationsDataObj in BldrStationsDataList)
                    {
                        lngTankPreselTID = (BldrStationsDataObj.TankPreSelectNumTid == null) ? -1 : (double)BldrStationsDataObj.TankPreSelectNumTid;
                        lngLineupPreselTID = (BldrStationsDataObj.LineupPreSelTid == null) ? -1 : (double)BldrStationsDataObj.LineupPreSelTid;

                        //Set to OFF ("0") to tank presel/lineup presel indexes to DCS using the Station interface
                        if (Convert.ToInt32(tagPermissive.vntTagVal) == (int)OnOff.ON_)
                        {
                            if (lngTankPreselTID != -1)
                            {
                                await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngTankPreselTID);
                            }

                            if (lngLineupPreselTID != -1)
                            {
                                await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngLineupPreselTID);
                            }

                        }
                    }
                    //get the blender_comps.lineup_presel_tid
                    lngLineupPreselTID = -1;

                    List<AbcBlenderComps> AllBldrCompsList = await _repository.GetAllBldrComps(vntBldrsData[intBldrIdx].Id);
                    List<AbcBlenderComps> AllBldrCompsListFlt = new List<AbcBlenderComps>();

                    if (AllBldrCompsList.Count() > 0)
                    {
                        AllBldrCompsListFlt = AllBldrCompsList.Where<AbcBlenderComps>(row => row.MatId == lngMatId).ToList();
                        if (AllBldrCompsListFlt.Count() > 0)
                        {
                            lngLineupPreselTID = (AllBldrCompsListFlt[0].LineupPreselTid == null) ? -1 : (double)AllBldrCompsListFlt[0].LineupPreselTid;
                        }
                    }

                    // Feb. 03: Download lineup indexes to DCS using the blender comps interface
                    if (lngLineupPreselTID != -1 && Convert.ToInt32(tagPermissive.vntTagVal) == (int)OnOff.ON_)
                    {
                        // set to OFF the preslected lineup tag
                        await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngLineupPreselTID);
                    }


                }
                foreach (BlendSwingsData BlendSwingsDataObj in BlendSwingsDataList)
                {
                    strSwingState = (BlendSwingsDataObj.SwingState == null) ? "" : BlendSwingsDataObj.SwingState;
                    lngMatId = BlendSwingsDataObj.FromTkMatId;
                    lngToTankID = BlendSwingsDataObj.ToTkId;
                    dblCriteriaNumberLmt = (BlendSwingsDataObj.CriteriaNumLmt == null) ? 0 : (double)BlendSwingsDataObj.CriteriaNumLmt;
                    dteCriteriaTimeLmt = (BlendSwingsDataObj.CriteriaTimLmt == null) ? dteCriteriaTimeLmt : Convert.ToDateTime(BlendSwingsDataObj.CriteriaTimLmt);
                    strAutoSwingFlag = BlendSwingsDataObj.AutoSwingFlag;
                    // get the comp name
                    strCompName = await _repository.GetCompName(lngMatId);

                    List<double?> CompLineupData = await _repository.GetCompLineup(curblend.lngID, lngMatId, lngToTankID);

                    if (CompLineupData.Count() > 0)
                    {
                        lngTOLineupID = (double)CompLineupData[0];
                    }

                    //  Update blend source sequences table to hold the new time in
                    if (gblnCompSwgTimeIn[intBldrIdx] == false)
                    {
                        await _repository.SetBlendSourceSeqData(curblend.lngID, lngMatId, lngSrcTankID, curblend.dteActualStart);
                        gblnCompSwgTimeIn[intBldrIdx] = true;
                    }

                    AbcBlenderComps BldrCompsSwingOccurID = await _repository.GetBldrCompsSwingOccurID(vntBldrsData[intBldrIdx].Id, curblend.lngID, lngSrcTankID, lngMatId);

                    lngSwingOccurredTID = (BldrCompsSwingOccurID.SwingOccurredTid == null) ? -1 : (double)BldrCompsSwingOccurID.SwingOccurredTid;
                    lngSwingTID = (BldrCompsSwingOccurID.SwingTid == null) ? -1 : (double)BldrCompsSwingOccurID.SwingTid;

                    if (lngSwingOccurredTID != -1)
                    {
                        DataRes = await _repository.GetTagNameAndVal(lngSwingOccurredTID);
                        tagSwingOccurred.vntTagName = DataRes.Name;
                        tagSwingOccurred.vntTagVal = DataRes.ReadValue.ToString();
                    }
                    else
                    {
                        tagSwingOccurred.vntTagName = null;
                        tagSwingOccurred.vntTagVal = ((int)OnOff.OFF).ToString();
                    }

                    if (tagSwingOccurred.vntTagVal == null)
                    {
                        tagSwingOccurred.vntTagVal = ((int)OnOff.OFF).ToString();
                    }

                    if (lngSwingTID != -1)
                    {
                        List<AbcTags> ReadWriteVal = await _repository.GetReadWriteVal(lngSwingTID);
                        if (ReadWriteVal.Count() > 0)
                        {
                            tagSwing.vntTagName = (ReadWriteVal[0].Name == null) ? null : ReadWriteVal[0].Name;
                            tagSwing.vntTagVal = (ReadWriteVal[0].WriteValue == null) ? ((int)OnOff.OFF).ToString() : ReadWriteVal[0].WriteValue.ToString();
                            // June 04, 03: Force to check for time out for active swings, even if the write values of
                            // this tag is OFF
                            if ((strSwingState == "ACTIVE"))
                            {
                                tagSwing.vntTagVal = ((int)OnOff.ON_).ToString();
                            }

                        }
                        else
                        {
                            tagSwing.vntTagName = null;
                            tagSwing.vntTagVal = ((int)OnOff.OFF).ToString();
                        }
                    }
                    else
                    {
                        tagSwing.vntTagName = null;
                        tagSwing.vntTagVal = ((int)OnOff.OFF).ToString();
                    }

                    if (strSwingState == "ACTIVE" || (strSwingState == "READY" && tagSwing.vntTagVal == ((int)OnOff.OFF).ToString() && tagSwingOccurred.vntTagVal == ((int)OnOff.ON_).ToString()))
                    {
                        if (tagSwingOccurred.vntTagVal == ((int)OnOff.ON_).ToString())
                        {
                            //'Update the abc_blend_sources=NO for the from_tk_id and to YES for the to_tk_id
                            await _repository.SetAbcBlendInUseFlag(curblend.lngID, lngMatId, lngToTankID, "NO");
                            await _repository.SetAbcBlendInUseFlag2(curblend.lngID, lngMatId, lngToTankID, "NO");

                            //' update blend swings table
                            await _repository.SetBlendSwingStateAndDoneAt2(lngSrcTankID, lngToTankID, curblend.lngID);

                            //' Get tank Names
                            strSrceTankName = await _repository.GetTankName(lngSrcTankID);
                            strToTankName = await _repository.GetTankName(lngToTankID);


                            // ' Log a message that swing is complete
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN79), programName, "BL-" + curblend.lngID, strSrceTankName, strToTankName,
                                        curblend.strName, "", "", "", res);

                            // Set to false thi flag to redim the comp number of props for
                            // the new to tank when a swing, in case the number of props is different
                            gArSrcTkPrpValTime[intBldrIdx].blnArraySet = false;
                            List<double> BldSourceSwgSeqData = await _repository.GetBldSourceSwgSeq(curblend.lngID, lngMatId);
                            if (BldSourceSwgSeqData.Count() > 0)
                            {
                                intSwingSeq = (BldSourceSwgSeqData[0] == null) ? 0 : (int)BldSourceSwgSeqData[0];
                            }
                            else
                            {
                                intSwingSeq = 0;
                            }

                            //  Get the Volume used of this component in the blend
                            //retun 0 if null
                            AbcBlendComps BldMatVol = await _repository.GetBldMatVol(curblend.lngID, lngMatId);
                            dblMatVolUsed = (double)BldMatVol.Volume;
                            //  Get the sum of vol used of this component for all the sequences
                            dblSumVolUsed = (double) await _repository.GetBldSourceSumVolUsed(curblend.lngID, lngMatId);

                            //  Get the Vol used for updating the abc_blend_source_seq
                            dblSeqVolUsed = (dblMatVolUsed - dblSumVolUsed);

                            // Update blend source sequences table
                            await _repository.SetBlendSourceSeqData2(curblend.lngID, lngMatId, lngSrcTankID, intSwingSeq, dblSeqVolUsed);

                            //  create new record in blend source sequences table with the to tank id
                            await _repository.InsetBlendSourceSeqData(curblend.lngID, lngMatId, lngToTankID, intSwingSeq + 1);
                            
                            lngSelTID = -1;
                            // Get the abc_blender_sources.selection_tid for the to tank id
                            List<AbcBlenderSources> BldrSrcPreselTID = await _repository.GetBldrSrcPreselTID(vntBldrsData[intBldrIdx].Id,curblend.lngID,lngMatId,lngToTankID.ToString());
                            lngSelTID = (BldrSrcPreselTID[0].SelectionTid == null) ? -1 : (double)BldrSrcPreselTID[0].SelectionTid;
                            lngPreselTID = (BldrSrcPreselTID[0].PreselectionTid == null) ? -1 : (double)BldrSrcPreselTID[0].PreselectionTid;
                            
                            if (lngSelTID != -1)
                            {
                                // Set Write value=1 for the selection tid
                                await _repository.SetWriteTagVal((int)OnOff.ON_, "YES", lngSelTID);
                            }

                            // Set selection tid for sources
                            // Get the abc_blender_sources.preselection_tid/selection_tid for all tanks with this material
                            lngPreselOFFTID = -1;
                            lngSelOFFTID = -1;
                            BldrSrcPreselTID = await _repository.GetBldrSrcPreselTID(vntBldrsData[intBldrIdx].Id,curblend.lngID,lngMatId,"%");

                            foreach (AbcBlenderSources BldrSrcPreselTIDObj in BldrSrcPreselTID)                            
                            {
                                lngPreselOFFTID = (BldrSrcPreselTIDObj.PreselectionTid == null) ? -1 : (double)BldrSrcPreselTIDObj.PreselectionTid;
                                lngSelOFFTID = (BldrSrcPreselTIDObj.SelectionTid == null) ? -1 : (double)BldrSrcPreselTIDObj.SelectionTid;
                                if (lngSelOFFTID != lngSelTID)
                                {
                                    //  reset Write value=0 for the selection  tid
                                    await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngSelOFFTID);
                                }

                                if (lngPreselOFFTID != lngPreselTID)
                                {
                                    //  reset Write value=0 for the Preselection tid
                                    await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngPreselOFFTID);
                                }
                            }

                            List<AbcTanks> DataTankIDData = await _repository.GetDataTankID(lngToTankID);

                            strTankName = DataTankIDData[0].Name;
                            strAbcServFlag = DataTankIDData[0].AbcServiceFlag;
                            vntDcsServTid = DataTankIDData[0].DcsServiceTid;
                            if (gstrDownloadType == "STATION" || gstrDownloadType == "LINEUP")
                            {
                                intDCSTankNum = (DataTankIDData[0].DcsTankNum == null) ? -1 : (int)DataTankIDData[0].DcsTankNum;
                            }

                            //  Get all the stations having this component
                            if (gstrDownloadType == "STATION" || gstrDownloadType == "LINEUP")
                            {
                                List<BldrStationsData> BldrStationsDataList = await _repository.GetBldrStationsData(lngTOLineupID, vntBldrsData[intBldrIdx].Id);

                                foreach (BldrStationsData BldrStationsDataObj in BldrStationsDataList)                                
                                {
                                    lngTankSelectNumTid = (BldrStationsDataObj.TankSelectNumTid == null) ? -1 : (double)BldrStationsDataObj.TankSelectNumTid;
                                    lngLineupSelTID = (BldrStationsDataObj.LineupSelTid == null) ? -1 : (double)BldrStationsDataObj.LineupSelTid;
                                    if (lngTankSelectNumTid != -1 && intDCSTankNum != -1)
                                    {
                                        // Write the DCS Select Tank number to the DCS
                                        await _repository.SetWriteTagVal(intDCSTankNum, "YES", lngTankSelectNumTid);
                                    }

                                    // Feb. 17, 03: Download lineup indexes to DCS using the Station interface
                                    if (lngLineupSelTID != -1)
                                    {
                                        // get DCS Lineup index if selected lineup id is not null
                                        if (lngTOLineupID != -1)
                                        {
                                            AbcCompLineups DCSCompLineupNum = await _repository.GetDCSCompLineupNum(lngTOLineupID);
                                            intDCSLineupNum = (int)DCSCompLineupNum.DcsLineupNum;
                                            strLineupName = DCSCompLineupNum.Name;                                            
                                        }
                                        else
                                        {
                                            intDCSLineupNum = -1;
                                        }

                                        if (intDCSLineupNum != -1)
                                        {
                                            // Write the Selected DCS LINEUP number to the DCS
                                            await _repository.SetWriteTagVal(intDCSLineupNum, "YES", lngLineupSelTID);
                                        }
                                        else
                                        {
                                            // IN BLEND ^1, COMP ^2, DCS LINEUP NUM IS NULL FOR LINEUP ^3.  CMD SEL/PRESEL IGNORED
                                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN96), programName, "BL-" + curblend.lngID, curblend.strName, strCompName,
                                        strLineupName, "", "", "", res);
                                        }
                                    }
                                }
                               
                                // select the blender_comps.lineup_sel_tid
                                lngLineupSelTID = -1;

                                List<AbcBlenderComps> AllBldrComps = await _repository.GetAllBldrComps(vntBldrsData[intBldrIdx].Id);
                                List<AbcBlenderComps> AllBldrCompsFlt = new List<AbcBlenderComps>();
                                if (AllBldrComps.Count() > 0)
                                {
                                    AllBldrCompsFlt = AllBldrComps.Where<AbcBlenderComps>(row => row.MatId == lngMatId).ToList();
                                    
                                    if (AllBldrCompsFlt.Count() > 0)
                                    {
                                        lngLineupSelTID = (AllBldrCompsFlt[0].LineupSelTid == null) ? -1 : (double)AllBldrCompsFlt[0].LineupSelTid;
                                    }

                                }
                                
                                // Download lineup indexes to DCS using the blender comps interface
                                if (lngLineupSelTID != -1)
                                {
                                    // get DCS Lineup index if selected lineup id is not null
                                    if (lngTOLineupID != -1)
                                    {
                                        AbcCompLineups DCSCompLineupNum = await _repository.GetDCSCompLineupNum(lngTOLineupID);
                                        intDCSLineupNum = (int)DCSCompLineupNum.DcsLineupNum;
                                        strLineupName = DCSCompLineupNum.Name;
                                        
                                    }
                                    else
                                    {
                                        intDCSLineupNum = -1;
                                    }

                                    if (intDCSLineupNum != -1)
                                    {
                                        // Write the Selected DCS LINEUP number to the DCS
                                        await _repository.SetWriteTagVal(intDCSLineupNum, "YES", lngLineupSelTID);
                                    }
                                    else
                                    {
                                        // IN BLEND ^1, COMP ^2, DCS LINEUP NUM IS NULL FOR LINEUP ^3.  CMD SEL/PRESEL IGNORED
                                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN96), programName, "BL-" + curblend.lngID, curblend.strName, strCompName,
                                        strLineupName, "", "", "", res);
                                    }
                                }                               
                            }

                            //'Insert new Stations into abc_blend_stations for this material according
                            //'with the new lineup_id
                            await CreateCompStations(lngTOLineupID, lngSrcTankID, lngToTankID, curblend, lngMatId, intBldrIdx, vntBldrsData, enumDebugLevel);

                            //' Check for in service state of the tank
                            await ChkTankServ(curblend.lngID, lngToTankID, strTankName, vntDcsServTid, strAbcServFlag, enumDebugLevel);

                            // Reset Write value=0 for the swing tid
                            await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngSwingTID);

                            // Reset Write value=OFF for the swing occurred tid
                            await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngSwingOccurredTID);
                            
                            // Reset pending state to NULL
                            curblend.vntPendSt = null;
                            // set ABC_BLENDS.PENDING_STATE to null
                            await _repository.SetPendingState(null,curblend.lngID);
                        }
                        else if (tagSwingOccurred.vntTagVal == ((int)OnOff.OFF).ToString() && tagSwing.vntTagVal == ((int)OnOff.ON_).ToString())
                        {
                            // get current time
                            gDteCurTime = await _repository.GetCurTime();

                            // get proj default swing time out
                            AbcProjDefaults SwgDefTimeOutData = await _repository.SwgDefTimeOut();
                            dblSwgTimeOut = (SwgDefTimeOutData.SwingTimeOut == null) ? 10 : (double)SwgDefTimeOutData.SwingTimeOut;
                            
                            if ((DateAndTime.DateDiff("n", gDteCompSwgCmdTime[intBldrIdx], gDteCurTime) > dblSwgTimeOut))
                            {
                                // Issue a message - SWING TIME OUT. SWING WAS NOT PERFORMED IN (BLEND)^1
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN75), programName, "SWING_TIME_OUT", "BLEND " + curblend.strName, "",
                                        "", "", "", "", res);
                                
                                // Reset pending state to NULL
                                curblend.vntPendSt = null;
                                // set ABC_BLENDS.PENDING_STATE to null
                                await _repository.SetPendingState(null,curblend.lngID);
                                //  update blend swings table to hold the incomplete swing state
                                await _repository.SetBlendSwingState2(lngSrcTankID, lngToTankID, curblend.lngID, "INCOMPLETE");

                                await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngSwingTID);
                            }
                        }
                    }
                    else if (strSwingState == "READY" && tagSwing.vntTagVal == ((int)OnOff.OFF).ToString())// Issue a Swing Cmd
                    {
                        if (curblend.strState.Trim() == "LOADED" || curblend.strState.Trim() == "ACTIVE" || curblend.strState.Trim() == "PAUSED")
                        {
                            lngPreselOFFTID = -1;
                            // Get the abc_blender_sources.preselection_tid for the to tank id
                            List<AbcBlenderSources> BldrSrcPreselTID = await _repository.GetBldrSrcPreselTID(vntBldrsData[intBldrIdx].Id,curblend.lngID,lngMatId,lngToTankID.ToString());
                            lngPreselTID = (BldrSrcPreselTID[0].PreselectionTid == null) ? -1 : (double)BldrSrcPreselTID[0].PreselectionTid;

                            // Get the abc_blender_sources.preselection_tid for all tanks with this material
                            BldrSrcPreselTID = await _repository.GetBldrSrcPreselTID(vntBldrsData[intBldrIdx].Id, curblend.lngID, lngMatId, "%");
                            
                            blnRollBack = true;
                            foreach (AbcBlenderSources BldrSrcPreselTIDObj in BldrSrcPreselTID)                            
                            {
                                lngPreselOFFTID = (BldrSrcPreselTIDObj.PreselectionTid == null) ? -1 : (double)BldrSrcPreselTIDObj.PreselectionTid;
                                if ((lngPreselTID != lngPreselOFFTID))
                                {
                                    //  Reset Write value=0 for the Preselection tid
                                    await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngPreselOFFTID);
                                }                                
                            }

                            List<AbcTanks> DataTankIDData = await _repository.GetDataTankID(lngToTankID);

                            strTankName = DataTankIDData[0].Name;
                            strAbcServFlag = DataTankIDData[0].AbcServiceFlag;
                            vntDcsServTid = DataTankIDData[0].DcsServiceTid;
                            if (gstrDownloadType == "STATION" || gstrDownloadType == "LINEUP")
                            {
                                intDCSTankNum = (DataTankIDData[0].DcsTankNum == null) ? -1 : (int)DataTankIDData[0].DcsTankNum;
                            }

                            // Check for in service state of the tank
                            await ChkTankServ(curblend.lngID, lngToTankID, strTankName, vntDcsServTid, strAbcServFlag, enumDebugLevel);
                           
                            //  set Write value=1 for the Preselection tid
                            if (lngPreselTID != -1)
                            {
                                await _repository.SetWriteTagVal((int)OnOff.ON_, "YES", lngPreselTID);
                            }

                            // Get the abc_stations.tank_preselect_num_tid for downloadtype=STATION
                            if (gstrDownloadType == "STATION" || gstrDownloadType == "LINEUP")
                            {
                                //  Get all the stations having this component

                                List<BldrStationsData> BldrStationsData = await _repository.GetBldrStationsData(lngTOLineupID, vntBldrsData[intBldrIdx].Id);

                                foreach (BldrStationsData BldrStationsDataObj in BldrStationsData)                               
                                {
                                    lngTankPreSelectNumTid = (BldrStationsDataObj.TankPreSelectNumTid == null) ? -1 : (double)BldrStationsDataObj.TankPreSelectNumTid;
                                    lngLineupPreselTID = (BldrStationsDataObj.LineupPreSelTid == null) ? -1 : (double)BldrStationsDataObj.LineupPreSelTid; 
                                    
                                    if (lngTankPreSelectNumTid != -1 && intDCSTankNum != -1)
                                    {
                                        // Write the DCS Preselect Tank number to the DCS
                                        await _repository.SetWriteTagVal(intDCSTankNum, "YES", lngTankPreSelectNumTid);
                                    }

                                    // Download lineup indexes to DCS using the Station interface
                                    if (lngLineupPreselTID != -1)
                                    {
                                        // get DCS Lineup index if selected lineup id is not null
                                        if (lngTOLineupID != -1)
                                        {
                                            AbcCompLineups DCSCompLineupNum = await _repository.GetDCSCompLineupNum(lngTOLineupID);
                                            intDCSLineupNum = (int)DCSCompLineupNum.DcsLineupNum;
                                            strLineupName = DCSCompLineupNum.Name;
                                        }
                                        else
                                        {
                                            intDCSLineupNum = -1;
                                        }

                                        if (intDCSLineupNum != -1)
                                        {
                                            // Write the Selected DCS LINEUP number to the DCS
                                            await _repository.SetWriteTagVal(intDCSLineupNum, "YES", lngLineupPreselTID);
                                        }
                                        else
                                        {
                                            // IN BLEND ^1, COMP ^2, DCS LINEUP NUM IS NULL FOR LINEUP ^3.  CMD SEL/PRESEL IGNORED
                                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN96), programName, "BL-" + curblend.lngID, curblend.strName, strCompName,
                                            strLineupName, "", "", "", res);
                                        }
                                    }
                                }

                                // presele the lineup using blender comps.lineup_Presel_tid
                                lngLineupPreselTID = -1;

                                List<AbcBlenderComps> AllBldrComps = await _repository.GetAllBldrComps(vntBldrsData[intBldrIdx].Id);
                                List<AbcBlenderComps> AllBldrCompsFlt = new List<AbcBlenderComps>();
                                if (AllBldrComps.Count() > 0)
                                {
                                    AllBldrCompsFlt = AllBldrComps.Where<AbcBlenderComps>(row => row.MatId == lngMatId).ToList();

                                    if (AllBldrCompsFlt.Count() > 0)
                                    {
                                        lngLineupPreselTID = (AllBldrCompsFlt[0].LineupPreselTid == null) ? -1 : (double)AllBldrCompsFlt[0].LineupPreselTid;
                                    }

                                }
                                                               
                                if (lngLineupPreselTID != -1)
                                {
                                    // get DCS Lineup index if presel lineup id is not null
                                    if (lngTOLineupID != -1)
                                    {
                                        AbcCompLineups DCSCompLineupNum = await _repository.GetDCSCompLineupNum(lngTOLineupID);
                                        intDCSLineupNum = (int)DCSCompLineupNum.DcsLineupNum;
                                        strLineupName = DCSCompLineupNum.Name;
                                    }
                                    else
                                    {
                                        intDCSLineupNum = -1;
                                    }                                    

                                    if (intDCSLineupNum != -1)
                                    {
                                        // Write the Selected DCS LINEUP number to the DCS
                                        await _repository.SetWriteTagVal(intDCSLineupNum, "YES", lngLineupPreselTID);
                                    }
                                    else
                                    {
                                        // IN BLEND ^1, COMP ^2, DCS LINEUP NUM IS NULL FOR LINEUP ^3.  CMD SEL/PRESEL IGNORED
                                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN96), programName, "BL-" + curblend.lngID, curblend.strName, strCompName,
                                            strLineupName, "", "", "", res);
                                    }
                                }
                            }// end Station Download
                        }// not active, paused, loaded
                         
                        // For active/paused blends
                        if (curblend.strState.Trim() == "ACTIVE" || curblend.strState.Trim() == "PAUSED")
                        {
                            // 'Get the swing Criteria in abc_blend_swings
                            strCriteriaName = (BlendSwingsDataObj.CriteriaName == null) ? "" : BlendSwingsDataObj.CriteriaName;
                            
                            switch (strCriteriaName)
                            {
                                case "NOW":
                                    await IssueSwingCommand(strAutoSwingFlag, lngSwingTID, lngSrcTankID, lngToTankID, intBldrIdx, vntBldrsData, enumDebugLevel);
                                    break;
                                case "TANK VOLUME":
                                    // Get the data from the from tank id
                                    List<ASTankID> ASTankIDList = await _repository.GetASTankID((int)lngSrcTankID);

                                    dblMinvol = (ASTankIDList[0].MinVol == null) ? 0 : Convert.ToDouble(ASTankIDList[0].MinVol);
                                    dblAvailVol = (ASTankIDList[0].AvailVol == null) ? 0 : Convert.ToDouble(ASTankIDList[0].AvailVol);
                                    
                                    // Compare the criteria_num_lmt with current vol in the tank
                                    if (dblCriteriaNumberLmt >= (dblAvailVol + dblMinvol))
                                    {
                                        await IssueSwingCommand(strAutoSwingFlag, lngSwingTID, lngSrcTankID, lngToTankID, intBldrIdx, vntBldrsData, enumDebugLevel);
                                    }
                                    break;
                                case "BLEND VOLUME":
                                    //' Get the Volume of this component in the blend
                                    AbcBlendComps BldMatVol = await _repository.GetBldMatVol(curblend.lngID, lngMatId);
                                    sngCompVolume = (BldMatVol.Volume == null) ? 0 : (double)BldMatVol.Volume;
                                    //'Compare the criteria_num_lmt with current vol in the tank
                                    if(sngCompVolume >= dblCriteriaNumberLmt)
                                    {
                                        await IssueSwingCommand(strAutoSwingFlag, lngSwingTID, lngSrcTankID, lngToTankID, intBldrIdx, vntBldrsData, enumDebugLevel);
                                    }
                                    break;
                                case "SWING TIME":
                                    // 'get current time
                                    gDteCurTime = await _repository.GetCurTime();
                                    //'Compare the criteria_TIM_lmt with current time
                                    if (gDteCurTime >= dteCriteriaTimeLmt){
                                        await IssueSwingCommand(strAutoSwingFlag, lngSwingTID, lngSrcTankID, lngToTankID, intBldrIdx, vntBldrsData, enumDebugLevel);
                                    }
                                    break;
                                case "HIGH LIMIT":
                                    // 'Get the data from the from tank id
                                    List<ASTankID> ASTankIDList2 = await _repository.GetASTankID((int)lngSrcTankID);

                                    dblMinvol = (ASTankIDList2[0].MinVol == null) ? 0 : Convert.ToDouble(ASTankIDList2[0].MinVol);
                                    dblMaxVol = (ASTankIDList2[0].MaxVol == null) ? 0 : Convert.ToDouble(ASTankIDList2[0].MaxVol);
                                    dblAvailVol = (ASTankIDList2[0].AvailVol == null) ? 0 : Convert.ToDouble(ASTankIDList2[0].AvailVol);

                                    if ((dblAvailVol + dblMinvol) >= dblMaxVol)
                                    {
                                        await IssueSwingCommand(strAutoSwingFlag, lngSwingTID, lngSrcTankID, lngToTankID, intBldrIdx, vntBldrsData, enumDebugLevel);
                                    }
                                    break;
                                case "LOW LIMIT":
                                    // 'Get the data from the from tank id
                                    List<ASTankID> ASTankIDList3 = await _repository.GetASTankID((int)lngSrcTankID);

                                    dblMinvol = (ASTankIDList3[0].MinVol == null) ? 0 : Convert.ToDouble(ASTankIDList3[0].MinVol);
                                    dblMaxVol = (ASTankIDList3[0].MaxVol == null) ? 0 : Convert.ToDouble(ASTankIDList3[0].MaxVol);
                                    dblAvailVol = (ASTankIDList3[0].AvailVol == null) ? 0 : Convert.ToDouble(ASTankIDList3[0].AvailVol);

                                    if ((dblAvailVol + dblMinvol) <= dblMinvol)
                                    {
                                        await IssueSwingCommand(strAutoSwingFlag, lngSwingTID, lngSrcTankID, lngToTankID, intBldrIdx, vntBldrsData, enumDebugLevel);
                                    }
                                    break;
                            }//switch
                        }
                    }
                }//loop
            }

            return 0;

        }
        private async Task<int> IssueSwingCommand(string strAutoSwingFlag, double lngSwingTID, double lngSrcTankID, double lngToTankID, int intBldrIdx, List<AbcBlenders> vntBldrsData, DebugLevels enumDebugLevel)
        {
            string res = "";
            if ((strAutoSwingFlag == "YES"))
            {
                //  Issue the swing command
                if ((gProjDfs.strAllowStartStop == "YES"))
                {
                    //  set Write value=1 for the swing tid
                    await _repository.SetWriteTagVal((int)OnOff.ON_, "YES", lngSwingTID);

                    // set pending state to SWINGING
                    curblend.vntPendSt = "SWINGING";
                    await _repository.SetPendingState(curblend.vntPendSt, curblend.lngID);

                    //  update blend swings table to hold the active swing state
                    await _repository.SetBlendSwingStateAndDoneAt3(lngSrcTankID, lngToTankID, curblend.lngID);

                    gDteCurTime = await _repository.GetCurTime();

                    //  set the Swing Command Time to a global variable
                    gDteCompSwgCmdTime[intBldrIdx] = gDteCurTime;
                }
                else
                {
                    // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "SWING", gstrBldrName,
                    "", "", "", "", res);

                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "",
                    "", "", "", "", res);
                }

            }
            else if (gstrRundnFlag != "YES")
            {
                //  Issue the Pause command
                if (gProjDfs.strAllowStartStop == "YES")
                {
                    if (curblend.strState.Trim() != "PAUSED")
                    {
                        // set pending state to PAUSING
                        curblend.vntPendSt = "PAUSING";
                        await _repository.SetPendingState(curblend.vntPendSt, curblend.lngID);
                        // call PAUSE_BLEND function
                        await ProcessBldCmd(BlendCmds.PAUSE, intBldrIdx, vntBldrsData, curblend, enumDebugLevel);
                    }

                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "",
                    "", "", "", "", res);
                }
                else
                {
                    // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "PAUSE", gstrBldrName,
                    "", "", "", "", res);

                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "",
                    "", "", "", "", res);
                }
            }
            return 0;
        }
        private async Task<int> CreateCompStations(double lngTOLineupID, double lngFromTkId, double lngToTkId, CurBlendData curblend, double lngMatId,int intBldrIdx,
                               List<AbcBlenders> vntBldrsData, DebugLevels enumDebugLevel)
        {
            double sngStnId;
            double sngCurSP;
            double sngCurOrigSP;
            double dblMinFlow;
            double dblMaxFlow;
            double dblStnMinSum;
            double dblSumRcp;
            double dblStnMinSuma;
            double dblStnMaxSum;
            double dblRemFlow = 0;
            bool blnLessStns;
            bool blnRollBack;
            int intJ;
            int intI;
            int intNStations;
            int intStnNo = 0;
            int intNToStations = 0;
            string strCompName;
            string strExecute;
            string strStationName;
            double[] arStationRcps;
            double sngStationMin = 0;
            double sngStationMax = 0;
            double[] arStationsIds;
            double sngStationCurRecipe;
            double[] arToStatLineups = new double[0];
            double vntSelStationTIDAll;
            string vntTagName;
            double vntPumpsData;
            double lngTankSelectNumTid;
            double lngMatNumTid;
            double lngStationId;
            double lngFromStationId;
            double lngRcpSpTid;
            double lngCompTankId;
            int intSelStation = 0;
            int intNS;
            int intDestTankID;
            int intNRecords;
            int intNFromStations;
            int intMatNum;
            int intTankNum;
            DcsTag tagSelStation = new DcsTag();
            DcsTag PumpTag = new DcsTag();
            bool blnDelete;
            bool blnSelect;
            string strUsageName;
            DcsTag tag = new DcsTag();
            double lngLineupPreselID;
            double lngCompLineupID = 0;
            double lngLineupSelTID;
            double lngLineupPreselTID;
            double lngPumpASelTID;
            double lngPumpBSelTID;
            double lngPumpCSelTID;
            double lngPumpDSelTID;
            double lngPumpXSelTID = 0;
            int intDCSLineupNum;
            int intPmpIndex;
            int intDcsPumpID;
            string strLineupName = "";
            string strPumpName;
            string strModeTag;
            string strInServFlag;
            string strRcpConstraintType = "";
            int intLineEqpOrder;
            int intNumPumps = 0;
            double lngPumpAId = 0;
            double lngPumpBId = 0;
            double lngPumpCId = 0;
            double lngPumpDId = 0;
            double lngPumpXId = 0; 
            bool blnZeroOut = false;
            string res = "";
            double[] vntSelStation = new double[0]; 
            // Get the stations for the to tank
            List<double?> CompLineups =  await _repository.GetCompLineups(curblend.lngID,lngMatId,lngToTkId);
            List<double?> CompLineupsFlt = new List<double?>();
            if (CompLineups.Count() > 0)
            {
                intNRecords = CompLineups.Count();
                arToStatLineups = new double[intNRecords];
            }

            foreach (double? CompLineup in CompLineups)            
            {

                arToStatLineups[intNToStations] = (CompLineup == null) ? -1 : (double)CompLineup;
                intNToStations = (intNToStations + 1);
            }

            // Get the stations for the from tank
            CompLineups = await _repository.GetCompLineups(curblend.lngID, lngMatId, lngFromTkId);
            
            if (CompLineups.Count() > 0)
            {
                intNFromStations = CompLineups.Count();
                if ((intNFromStations <= intNToStations))
                {
                    for (intI = 1; (intI <= intNToStations); intI++)
                    {
                        CompLineupsFlt = CompLineups.Where<double?>(row => row == arToStatLineups[intI]).ToList();
                        
                        if (CompLineupsFlt.Count() > 0)
                        {
                            lngFromStationId = (double)CompLineupsFlt[0];
                        }
                        else
                        {
                            lngFromStationId = -1;
                        }

                        if (CompLineupsFlt.Count() > 0)
                        {
                            goto NextStation;
                        }
                        else
                        {
                            //  Get all the stations tag selection from abc_stations
                            List<AbcStations> AllBldrStationsData2 = await _repository.GetAllBldrStationsData(vntBldrsData[intBldrIdx].Id);
                            List<AbcStations> AllBldrStationsDataFlt = AllBldrStationsData2.Where<AbcStations>(row => row.Id == arToStatLineups[intI]).ToList();
                           
                            if (AllBldrStationsDataFlt.Count() > 0)
                            {
                                sngStationMin = (AllBldrStationsDataFlt[0].Min == null) ? 0 : (double)AllBldrStationsDataFlt[0].Min;
                                sngStationMax = (AllBldrStationsDataFlt[0].Max == null) ? 0 : (double)AllBldrStationsDataFlt[0].Max;

                                // insert blend_stations records with the this station_id
                                await _repository.InsertBlendStations(curblend.lngID, lngMatId, arToStatLineups[intI], sngStationMax, sngStationMin);                             
                            }
                        }

                        NextStation: { }
                    }

                    // delete the from stations of they are not included in the collection of to stations                   
                    blnDelete = true;
                    foreach (double? CompLineup in CompLineups)
                    {
                        lngFromStationId = (CompLineup == null)? 0:(double)CompLineup;
                        for (intI = 1; (intI <= intNToStations); intI++)
                        {
                            if ((lngFromStationId == arToStatLineups[intI]))
                            {
                                blnDelete = false;
                                goto NextFromStation;
                            }

                        }

                        // Delete this station record since it was not found in the collection of stations for the
                        // new lineup id (to stations)
                        if (blnDelete == true && lngFromStationId != 0)
                        {
                            // delete from blend comp stations this station
                            await _repository.DeleteBlendStations(curblend.lngID, lngMatId, lngFromStationId);
                        }

                        NextFromStation: { }
                        blnDelete = true;
                    }

                }
                else
                {
                    //  if From stations > to stations                    
                    blnDelete = true;
                    foreach (double? CompLineup in CompLineups)
                    {
                        lngFromStationId = (CompLineup == null)? 0:(double)CompLineup;
                        for (intI = 1; (intI <= intNToStations); intI++)
                        {
                            if (lngFromStationId == arToStatLineups[intI])
                            {
                                blnDelete = false;
                                goto NextFromStat;
                            }

                        }

                        // Delete this station record since it was not found in the collection of stations for the
                        // new lineup id (to stations)
                        if (blnDelete == true && lngFromStationId != 0)
                        {
                            // delete from blend comp stations this station
                            await _repository.DeleteBlendStations(curblend.lngID, lngMatId, lngFromStationId);                           
                        }

                        NextFromStat: { }
                        blnDelete = true;
                    }
                }
            }
            else
            {
                // If not stations for the from tank create the stations for the to tank
                // Create blend station records
                if (lngTOLineupID != -1)
                {
                    // delete all blend comp stations records
                    await _repository.DeleteBlendStation2(curblend.lngID, lngMatId);

                    // insert blend_stations records with the new lineup_id (tank_id)
                    await _repository.InsertBlendStations(curblend.lngID, lngMatId, lngTOLineupID);                    
                }
            }

            // Check if all stations fo the new lineup id are created in abc_blend_stations
            // Get all stations from abc_blend_stations for this mat_id
            List <AbcBlendStations> GetBlStationsData = await _repository.GetBlStations(curblend.lngID,lngMatId);
            List<AbcBlendStations> GetBlStationsDataFlt = new List<AbcBlendStations>();


            if (GetBlStationsData.Count() > 0)
            {
                for (intI = 1; intI <= intNToStations; intI++)
                {
                    GetBlStationsDataFlt = GetBlStationsData.Where<AbcBlendStations>(row => row.StationId == arToStatLineups[intI]).ToList();
                   
                    if (GetBlStationsDataFlt.Count() == 0 && arToStatLineups[intI] != -1)
                    {
                        // insert blend_stations records with this station_id
                        await _repository.InsertBlendStations(curblend.lngID, lngMatId, arToStatLineups[intI], sngStationMax, sngStationMin);
                    }
                }
            }
            else
            {
                // Create blend station records
                if (lngTOLineupID != -1)
                {
                    // delete all blend comp stations records
                    await _repository.DeleteBlendStation2(curblend.lngID, lngMatId);

                    // insert blend_stations records with the new lineup_id (tank_id)
                    await _repository.InsertBlendStations(curblend.lngID, lngMatId, lngTOLineupID);
                }
            }

            if (gstrDownloadType == "STATION" || gstrDownloadType == "LINEUP")
            {
                // get the Usage Name for the given blend Component
                strUsageName = await GetBldMatUsage(curblend.lngID, lngMatId);
                // Get the Current SP for this component in abc_blend_comps
                AbcBlendComps BldMatVol = await _repository.GetBldMatVol(curblend.lngID,lngMatId);

                sngCurSP = (BldMatVol.CurRecipe == null) ? 0 : (double)BldMatVol.CurRecipe;
                // Store the original Curr SP from abc_blend_comps
                sngCurOrigSP = sngCurSP;

                // handle rcp_constraint Type
                List<BldCompUsage> BldCompUsageData = await _repository.GetBldCompUsage(curblend.lngID,lngMatId);
                
                if (BldCompUsageData.Count() > 0)
                {
                    strRcpConstraintType = BldCompUsageData[0].RcpConstraintType;
                }
                
                if (sngCurSP <= 0.01 && (strRcpConstraintType == "ZERO_MIN_MAX" || strRcpConstraintType == "ZERO_OUT"))
                {
                    blnZeroOut = true;
                }

                // Need to split the recipes to station recipes
                // get stations for this material
                List<AbcBlendStations> BlStationsData = await _repository.GetBlStations(curblend.lngID,lngMatId);

                strCompName = await _repository.GetCompName(lngMatId);
                
                if (BlStationsData.Count() > 0)
                {
                    intNStations = BlStationsData.Count();

                    arStationRcps = new double[intNStations];
                    arStationsIds = new double[intNStations];
                    dblStnMinSum = 0;
                    dblStnMaxSum = 0;
                    intJ = 0;
                    // With...
                    if ((intNStations == 1))
                    {
                        sngStnId = BlStationsData[0].StationId;
                        
                        arStationRcps[intJ] = sngCurSP;
                        arStationsIds[intJ] = sngStnId;
                        intJ = (intJ + 1);                       
                    }
                    else
                    {
                        // if IntNStations > 1
                        blnLessStns = false;
                        for (int i = 0; i < BlStationsData.Count(); i++)
                        {
                            AbcBlendStations BlStationsObj = BlStationsData[i];                        
                            // get sum of station min flow and max flows
                            dblMinFlow = (BlStationsObj.MinFlow == null) ? 0 : (double)BlStationsObj.MinFlow;
                            dblMaxFlow = (BlStationsObj.MaxFlow == null) ? dblMinFlow : (double)BlStationsObj.MaxFlow;
                            dblStnMinSum = (dblStnMinSum + dblMinFlow);
                            dblStnMaxSum = (dblStnMaxSum + dblMaxFlow);
                            dblRemFlow = ((sngCurSP * (0.01 * Convert.ToDouble(curblend.sngTgtRate))) - dblStnMinSum);
                            // checking to detect if flow <min flow (allowing 1% error)
                            if ((dblRemFlow < ((0.01 * dblStnMinSum) * -1)))
                            {
                                // Determine the number of stations to be used if the flow < dblStnMinSum
                                intStnNo = i;//ABCdataEnv.rscmdGetBlStations.AbsolutePosition;
                                // Revert back the total station min and max by excluding this station
                                dblStnMinSum = (dblStnMinSum - dblMinFlow);
                                dblStnMaxSum = (dblStnMaxSum - dblMaxFlow);
                                dblRemFlow = (dblRemFlow + dblMinFlow);
                                blnLessStns = true;
                                break; //Warning!!! Review that break works as 'Exit Do' as it could be in a nested instruction like switch
                            }
                            
                        }

                        // add a tolerance on the max of (0.1)
                        if ((sngCurSP * 0.01 * curblend.sngTgtRate - dblStnMaxSum) > 0.1 && (blnZeroOut == false))
                        {
                            // Issue an error message " Incompatible equipment Min/Max configuration for blender ^1.
                            // Check Lineup data"
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN29), programName, "BL-" + curblend.lngID, curblend.strName, gstrBldrName,
                                        "", "", "", "", res);
                            return 0;
                        }

                        if (((dblStnMaxSum <= cDblEp) && (blnZeroOut == false)))
                        {
                            // Issue an error message " Incompatible equipment Min/Max configuration for blender ^1.
                            // Check Lineup data"
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN29), programName, "BL-" + curblend.lngID, curblend.strName, gstrBldrName,
                                        "", "", "", "", res);
                            return 0;
                        }

                        if ((curblend.sngTgtRate <= cDblEp))
                        {
                            // Issue a message "BAD HEADER FLOW RATE. CHECK TOTAL FLOW TAG OR TARGET RATE FOR BLENDER ^1"
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN31), programName, "BL-" + curblend.lngID, gstrBldrName,"",
                                        "", "", "", "", res);
                            return 0;
                        }

                        // Loop of stations
                        for (int i = 0; i < BlStationsData.Count(); i++)
                        {
                            AbcBlendStations BlStationsObj = BlStationsData[i];
                            sngStnId = BlStationsObj.StationId;
                            
                            if ((i >= intStnNo) && (blnLessStns == true))
                            {
                                if ((intStnNo == 1) && (blnZeroOut == false))
                                {
                                    // issue a message that "station rcp could not be downloaded for comp^1
                                    //  in blend ^2 since flow < min flow of stations"
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN30), programName, "BL-" + curblend.lngID, strCompName, curblend.strName,
                                        "", "", "", "", res);
                                }

                                // set station rcp =0 for stations whose min flow>rem flow
                                sngCurSP = 0;                               
                            }
                            else
                            {
                                dblMinFlow = (BlStationsObj.MinFlow == null) ? 0 : (double)BlStationsObj.MinFlow;
                                dblMaxFlow = (BlStationsObj.MaxFlow == null) ? 0 : (double)BlStationsObj.MaxFlow;
                                sngCurSP = (100 * (dblMinFlow + (dblRemFlow * dblMaxFlow / dblStnMaxSum)) / (double)curblend.sngTgtRate);
                                sngCurSP = Math.Round(sngCurSP, 3);
                            }

                            arStationRcps[intJ] = sngCurSP;
                            arStationsIds[intJ] = sngStnId;                        
                            NEXT_STATION: { }
                            intJ = (intJ + 1);
                        }
                    }
                    
                    // Normalize stations recipe and update cur_SP in abc_blend_stations
                    dblSumRcp = 0;
                    for (intI = 0; intI <= (intNStations - 1); intI++)
                    {
                        // need this for renormalizing recipes after stn. rcp calcs
                        dblSumRcp = (dblSumRcp + arStationRcps[intI]);
                    }

                    for (intI = 0; intI <= (intNStations - 1); intI++)
                    {
                        if (strUsageName != "ADDITIVE")
                        {
                            // Check for zero recipes
                            if (dblSumRcp != 0 && sngCurOrigSP != 0)
                            {
                                arStationRcps[intI] = Math.Round((arStationRcps[intI]/ (dblSumRcp * sngCurOrigSP)), 3);
                            }
                            else
                            {
                                arStationRcps[intI] = arStationRcps[intI];
                            }

                        }

                        // update abc_blend_stations with comp current_recipe
                        await _repository.SetBlendStationsData(curblend.lngID, lngMatId, arStationsIds[intI], arStationRcps[intI]);
                    }
                }
                else
                {
                    // Issue a message "RECIPE NOT DOWNLOADED. NO STATIONS ARE IN USE FOR COMP^1 IN BLEND^2"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN32), programName, "BL-" + curblend.lngID, strCompName, curblend.strName,
                                       "", "", "", "", res);
                }               
            }

            // Download the new equipment (pumps) for the new lineup
            // get pump data, including inuse_tag_id for this comp
            // Download pumps based on lineup ID
            if (lngTOLineupID != -1)
            {
                await DownloadLineupCompPmps(intBldrIdx, vntBldrsData, curblend, lngTOLineupID, enumDebugLevel, true);
            }

            // Nov. 08/2001: Leave the sub if the download type is component
            if (gstrDownloadType == "COMPONENT")
            {
                return 0;
            }

            // Get all stations from abc_blend_stations  
            List<AbcBlendStations> AllBldStationsData = await _repository.GetAllBldStations(curblend.lngID);
            intNStations = AllBldStationsData.Count();
            vntSelStation = new double[intNStations];

            foreach (AbcBlendStations AllBldStationsObj in AllBldStationsData)            
            {

                vntSelStation[intSelStation] = (AllBldStationsObj.StationId == null) ? -1 : (double)AllBldStationsObj.StationId;
                intSelStation = (intSelStation + 1);
            }

            //  July 13/2001: Deselect others stations of the blender at the beginning of downloading
            //  Get all the stations tag selection from abc_stations
            List<AbcStations> AllBldrStationsData = await _repository.GetAllBldrStationsData(vntBldrsData[intBldrIdx].Id);

            // get dest tank ID
            intDestTankID = (int)await _repository.GetDestTankId(curblend.lngID);

            // Get the component source data            
            List<CompSrceData> CompSrceDataList = await _repository.GetCompSrceData(curblend.lngID);

            foreach (AbcStations BldrStationsDataObj in AllBldrStationsData)
            {
                lngStationId = BldrStationsDataObj.Id;
                vntSelStationTIDAll = (BldrStationsDataObj.SelectStationTid == null) ? -1 : (double)BldrStationsDataObj.SelectStationTid;
                lngTankSelectNumTid = (BldrStationsDataObj.TankSelectNumTid == null) ? -1 : (double)BldrStationsDataObj.TankSelectNumTid;                 
                lngMatNumTid = (BldrStationsDataObj.MatNumTid == null) ? -1 : (double)BldrStationsDataObj.MatNumTid;
                lngRcpSpTid = (BldrStationsDataObj.RcpSpTagId == null) ? -1 : (double)BldrStationsDataObj.RcpSpTagId;                 
                lngLineupSelTID = (BldrStationsDataObj.LineupSelTid == null) ? -1 : (double)BldrStationsDataObj.LineupSelTid; 
                
                lngPumpASelTID = (BldrStationsDataObj.PumpaSelTid == null) ? -1 : (double)BldrStationsDataObj.PumpaSelTid;
                lngPumpBSelTID = (BldrStationsDataObj.PumpbSelTid == null) ? -1 : (double)BldrStationsDataObj.PumpbSelTid;
                lngPumpCSelTID = (BldrStationsDataObj.PumpcSelTid == null) ? -1 : (double)BldrStationsDataObj.PumpcSelTid;
                lngPumpDSelTID = (BldrStationsDataObj.PumpdSelTid == null) ? -1 : (double)BldrStationsDataObj.PumpdSelTid;

                List<AbcBlendStations> AllBldStationsDataFlt = new List<AbcBlendStations>();
                List<CompSrceData> CompSrceDataListFlt = new List<CompSrceData>();
                for (intNS = 0; intNS < AllBldStationsData.Count(); intNS++)
                {
                    if (vntSelStation[intNS] != lngStationId)
                    {
                        goto lblNextTID;
                    }
                    else
                    {
                        AllBldStationsDataFlt = AllBldStationsData.Where<AbcBlendStations>(row => row.StationId == lngStationId).ToList();
                        
                        if (AllBldStationsDataFlt.Count() > 0)
                        {
                            lngMatId = (AllBldStationsDataFlt[0].MatId == null) ? -1 : (double)AllBldStationsDataFlt[0].MatId;
                        }

                        if (lngMatId != -1)
                        {
                            CompSrceDataListFlt = CompSrceDataList.Where<CompSrceData>(row => row.MatId == lngMatId).ToList();
                            
                            if (CompSrceDataListFlt.Count() > 0)
                            {
                                lngCompLineupID = (CompSrceDataListFlt[0].LineupId == null) ? -1 : (double)CompSrceDataListFlt[0].LineupId;
                            }
                            else
                            {
                                lngCompLineupID = -1;
                            }
                        }

                        // Download the selection of this station to DCS
                        strStationName = BldrStationsDataObj.Name;

                        // get the comp name
                        strCompName = await _repository.GetCompName(lngMatId);
                        // set recipe tags
                        if (lngRcpSpTid != -1)
                        {
                            AbcBlendStations BldStationsData = await _repository.GetBldStationsData(curblend.lngID,lngMatId,lngStationId);
                            if (BldStationsData != null)
                            {
                                sngStationCurRecipe = (BldStationsData.CurSetpoint == null) ? -1 : (double)BldStationsData.CurSetpoint;
                            }
                            else
                            {
                                sngStationCurRecipe = -1;
                            }
                            
                            if (sngStationCurRecipe != -1)
                            {
                                await _repository.SetWriteTagVal((int)sngStationCurRecipe, "YES", lngRcpSpTid);

                                // get RECIPE_SP_TID tag name
                                vntTagName = await _repository.GetTagName(lngRcpSpTid);
                                ;
                                if (enumDebugLevel >= DebugLevels.Medium)
                                {
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG12), programName, cstrDebug, curblend.strName, sngStationCurRecipe.ToString(),
                                        strCompName, vntTagName, "", "", res);
                                }                               
                            }
                            else
                            {
                                //  Null cur_recipe in the station ^1 used for component ^2. Download canceled
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN80), programName, "BL-" + curblend.lngID, strStationName, strCompName,
                                        "", "", "", "", res);

                                await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, intDestTankID, enumDebugLevel);                                
                                return 0;
                            }

                        }
                        else
                        {
                            // warn msg "Recipe_sp_tid tag missing"
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN18), programName, "BL-" + curblend.lngID, "RECIPE_SP_TID", strCompName,
                                       gstrBldrName, "", "", "", res);

                            await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, intDestTankID, enumDebugLevel);
                            return 0;
                        }

                        // set select_station_tid to ON in the DCS
                        if (vntSelStationTIDAll != -1)
                        {
                            // set select_station_tid to ON in the DCS
                            await _repository.SetWriteTagVal((int)OnOff.ON_, "YES", vntSelStationTIDAll);
                            
                        }
                        else if ((gstrDownloadType == "STATION"))
                        {
                            // warn msg "Selection_Station_tid tag missing"
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN18), programName, "BL-" + curblend.lngID, "SELECT_STATION_TID", strStationName,
                                       gstrBldrName, "", "", "", res);

                            await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, intDestTankID, enumDebugLevel);
                            return 0;
                        }

                        // Download the DCS_Mat_Num to the DCS through DCS_Mat_Num_tid
                        if (lngMatNumTid != -1)
                        {
                            if (lngMatId != -1)
                            {
                                AbcMaterials MatNameData =  await _repository.GetMatName((int)lngMatId);
                                intMatNum = (MatNameData.DcsMatNum == null) ? -1 : (int)MatNameData.DcsMatNum;                                
                            }
                            else
                            {
                                intMatNum = -1;
                            }

                            if (intMatNum != -1)
                            {
                                // Write the DCS Mat number to the DCS
                                await _repository.SetWriteTagVal(intMatNum, "YES", lngMatNumTid);
                            }
                            else
                            {
                                // ^1 IS NULL IN ^2 FOR COMP ^3.  ^4 NOT SELECTED FOR STATION ^5
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN81), programName, "BL-" + curblend.lngID, "MAT_NUM", "ABC_MATERIALS",
                                       strCompName, "MATERIAL", strStationName, "", res);
                            }

                        }
                        else if ((gstrDownloadType == "STATION"))
                        {
                            // warn msg "MAT_NUM_TID tag missing"
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN18), programName, "BL-" + curblend.lngID, "MAT_NUM_TID", strStationName,
                                      gstrBldrName, "", "", "", res);

                            await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, intDestTankID, enumDebugLevel);
                            return 0;                            
                        }

                        // Download the DCS_Tank_Num to the DCS through Tank_Select_Num_tid
                        if (lngTankSelectNumTid != -1)
                        {
                            CompSrceDataListFlt = CompSrceDataList.Where<CompSrceData>(row => row.MatId == lngMatId).ToList();

                            if (CompSrceDataListFlt.Count() > 0)
                            {
                                lngCompTankId = (CompSrceDataListFlt[0].TankId == null) ? -1 : (double)CompSrceDataListFlt[0].TankId;
                            }
                            else
                            {
                                lngCompTankId = -1;
                            }                            

                            if (lngCompTankId != -1)
                            {
                                List<AbcTanks> DataTankID = await _repository.GetDataTankID(lngCompTankId);
                                intTankNum = (DataTankID[0].DcsTankNum == null) ? -1 : (int)DataTankID[0].DcsTankNum;
                            }
                            else
                            {
                                intTankNum = -1;
                            }

                            if (intTankNum != -1)
                            {
                                // Write the DCS Tank number to the DCS
                                await _repository.SetWriteTagVal(intTankNum, "YES", lngTankSelectNumTid);
                            }
                            else
                            {
                                // ^1 IS NULL IN ^2 FOR COMP ^3.  ^4 NOT SELECTED FOR STATION ^5
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN81), programName, "BL-" + curblend.lngID, "TANK_NUM", "ABC_TANKS",
                                      strCompName, "TANK", strStationName, "", res);
                            }

                        }
                        else if ((gstrDownloadType == "STATION"))
                        {
                            // warn msg "TANK_SELECT_NUM_TID tag missing"
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN18), programName, "BL-" + curblend.lngID, "TANK_SELECT_NUM_TID", strStationName,
                                      gstrBldrName, "", "", "", res);

                            await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, intDestTankID, enumDebugLevel);
                            return 0;
                        }

                        // Feb. 17, 03: Download lineup indexes to DCS using the Station interface
                        if (lngLineupSelTID != -1)
                        {
                            // get DCS Lineup index if selected lineup id is not null
                            if (lngCompLineupID != -1)
                            {
                                AbcCompLineups DCSCompLineupNum = await _repository.GetDCSCompLineupNum(lngCompLineupID);
                                intDCSLineupNum = (int)DCSCompLineupNum.DcsLineupNum;
                                strLineupName = DCSCompLineupNum.Name;
                            }
                            else
                            {
                                intDCSLineupNum = -1;
                            }

                            if (intDCSLineupNum != -1)
                            {
                                // Write the Selected DCS LINEUP number to the DCS
                                await _repository.SetWriteTagVal(intDCSLineupNum, "YES", lngLineupSelTID);
                            }
                            else
                            {
                                // IN BLEND ^1, COMP ^2, DCS LINEUP NUM IS NULL FOR LINEUP ^3.  CMD SEL/PRESEL IGNORED
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN96), programName, "BL-" + curblend.lngID, curblend.strName, strCompName,
                                     strLineupName, "", "", "", res);
                            }
                        }

                            if ((gstrDownloadType == "STATION"))
                            {
                                // get the eqp_order to donwload pumps corresponding stations
                                intLineEqpOrder = (int)await _repository.GetCompEqpOrder(lngCompLineupID,lngStationId);
                                                                
                                // Get the component lineup pumps per station
                                (lngPumpAId, lngPumpBId, lngPumpCId, lngPumpDId, intNumPumps)  = await GetStationPumps(lngCompLineupID,"COMPONENT",intLineEqpOrder,lngPumpAId,lngPumpBId,lngPumpCId,lngPumpDId,intNumPumps);
                                intPmpIndex = 0;

                                for (intPmpIndex = 0; intPmpIndex <= (intNumPumps - 1); intPmpIndex++)
                                {
                                    switch (intPmpIndex)
                                    {
                                        case 0:
                                            lngPumpXSelTID = lngPumpASelTID;
                                            lngPumpXId = lngPumpAId;
                                            break;
                                        case 1:
                                            lngPumpXSelTID = lngPumpBSelTID;
                                            lngPumpXId = lngPumpBId;
                                            break;
                                        case 2:
                                            lngPumpXSelTID = lngPumpCSelTID;
                                            lngPumpXId = lngPumpCId;
                                            break;
                                        case 3:
                                            lngPumpXSelTID = lngPumpDSelTID;
                                            lngPumpXId = lngPumpDId;
                                            break;
                                    }


                                    AbcPumps PumpCfg = await _repository.GetPumpCfg(lngPumpXId);

                                    strPumpName = string.IsNullOrEmpty(PumpCfg.Name) ? "-1" : PumpCfg.Name;
                                    strModeTag = (PumpCfg.ModeTid == null) ? "-1" : PumpCfg.ModeTid.ToString();
                                    strInServFlag = PumpCfg.InSerFlag;
                                    intDcsPumpID = (PumpCfg.DcsPumpId == null) ? -1 : (int)PumpCfg.DcsPumpId;
                                    blnSelect = true;
                                    if ((strInServFlag != "YES"))
                                    {
                                        // warn msg "PUMP ^1 NOT IN ABC SERVICE OR NOT AUTO MODE.  COMMAND SELECTION IGNORED
                                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN23), programName, "BL-" + curblend.lngID, strPumpName, "",
                                        "", "\\", "", "", res);
                                        blnSelect = false;
                                    }

                                    if (strModeTag != "-1")
                                    {
                                        AbcTags DataRes2 = await _repository.GetTagNameAndVal(Convert.ToDouble(strModeTag));
                                        tag.vntTagName = DataRes2.Name;
                                        tag.vntTagVal = DataRes2.ReadValue.ToString();
                                        
                                        if (tag.vntTagVal != ((int)OnOff.ON_).ToString())
                                        {
                                            // warn msg "PUMP ^1 NOT IN ABC SERVICE OR NOT AUTO MODE.  COMMAND SELECTION IGNORED
                                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN23), programName, "BL-" + curblend.lngID, strPumpName, "",
                                            "", "\\", "", "", res);
                                            blnSelect = false;
                                        }

                                    }

                                    if ((blnSelect == true))
                                    {
                                        if (lngPumpXSelTID != -1 && intDcsPumpID != -1)
                                        {
                                            await _repository.SetWriteTagVal(intDcsPumpID, "YES", lngPumpXSelTID);                                            
                                        }

                                    }

                                }

                            }
                        goto lblNextStation;
                        
                    }
                    lblNextTID: { }
                }

                //  get the tag value for this station
                AbcTags DataRes = await _repository.GetTagNameAndVal(vntSelStationTIDAll);
                tagSelStation.vntTagName = DataRes.Name;
                tagSelStation.vntTagVal = DataRes.ReadValue.ToString();
               
                //  Reset this tag value if it is not reset yet
                if (tagSelStation.vntTagVal != null && tagSelStation.vntTagVal != ((int)OnOff.OFF).ToString())
                {
                    await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", vntSelStationTIDAll);
                }

                if ((lngTankSelectNumTid != -1))
                {
                    await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngTankSelectNumTid);
                }

                if ((lngMatNumTid != -1))
                {
                    await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngMatNumTid);
                }

                if ((lngRcpSpTid != -1))
                {
                    await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngRcpSpTid);
                }

                if ((lngLineupSelTID != -1))
                {
                    await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngLineupSelTID);
                }

                if ((lngPumpASelTID != -1))
                {
                    await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngPumpASelTID);
                }

                if ((lngPumpBSelTID != -1))
                {
                    await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngPumpBSelTID);
                }

                if ((lngPumpCSelTID != -1))
                {
                    await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngPumpCSelTID);
                }

                if ((lngPumpDSelTID != -1))
                {
                    await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngPumpDSelTID);
                }

                lblNextStation: { }
            }

            // get the blender_comps.lineup_sel_tid, blender_comps.lineup_presel_tid
            lngLineupSelTID = -1;

            List<AbcBlenderComps> AllBldrComps = await _repository.GetAllBldrComps(vntBldrsData[intBldrIdx].Id);
            List<AbcBlenderComps> AllBldrCompsFlt = new List<AbcBlenderComps>();
            if (AllBldrComps.Count() > 0)
            {
                AllBldrCompsFlt = AllBldrComps.Where<AbcBlenderComps>(row => row.MatId == lngMatId).ToList();
                
                if (AllBldrCompsFlt.Count() > 0)
                {
                    lngLineupSelTID = (AllBldrCompsFlt[0].LineupSelTid == null) ? -1 : (double)AllBldrCompsFlt[0].LineupSelTid;
                }

            }
            
            // Download lineup indexes to DCS using the blender comps interface
            if (lngLineupSelTID != -1)
            {
                // get DCS Lineup index if selected lineup id is not null
                intDCSLineupNum = -1;
                if (lngCompLineupID != -1)
                {
                    AbcCompLineups DCSCompLineupNum = await _repository.GetDCSCompLineupNum(lngCompLineupID);
                    intDCSLineupNum = (int)DCSCompLineupNum.DcsLineupNum;
                    strLineupName = DCSCompLineupNum.Name;
                }

                if (intDCSLineupNum != -1)
                {
                    // Write the Selected DCS LINEUP number to the DCS
                    await _repository.SetWriteTagVal(intDCSLineupNum, "YES", lngLineupSelTID);
                }
                else
                {
                    // get the comp name
                    strCompName = await _repository.GetCompName(lngMatId);

                    // IN BLEND ^1, COMP ^2, DCS LINEUP NUM IS NULL FOR LINEUP ^3.  CMD SEL/PRESEL IGNORED
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN96), programName, "BL-" + curblend.lngID, curblend.strName, strCompName,
                                            strLineupName, "", "", "", res);
                }
            }

            return 0;
        }
        private async Task<int> SwingProdTank(int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend, int intDestTankID, DebugLevels enumDebugLevel)
        {
            string strSwingState;
            string strAutoSwingFlag;
            string strAbcServFlag;
            string strTankName ="";
            string strDestTankName;
            string strToTankName;
            string strRundnBldName;
            string strNewBlendId;
            string strCriteriaName;
            string strFlushTkFlag;
            string strTkInUseFlag;
            string strSrceDestType;
            string strDestSelectName;
            string strFlushSwgState;
            double lngSwingTID;
            double? lngSwingOccurredTID;
            double lngToTankID;
            double lngRundnBldID;
            double lngPreselTID;
            double lngPreselOFFTID;
            double lngSelTID;
            double lngSelOFFTID;
            double lngDestTkId;
            double lngProdLineupId;
            double lngTransferLineId;
            double lngDestSelectNameTid;
            double lngFlushTankId;
            double sngTransferLineVol = 0;
            DcsTag tagSwingOccurred = new DcsTag();
            DcsTag tagSwing = new DcsTag();
            double dblSeqVolAdded;
            double dblBldVolAdded;
            double dblSumVolAdded;
            double dblCriteriaNumberLmt;
            double dblMinvol;
            double dblAvailVol;
            double dblMaxVol;
            double dblSwgTimeOut;
            double dblDestVolume;
            double dblPrdHeelVol;
            int intSwingSeq;
            int intReadyRundnBlds;
            int intNDestTks =0;
            DateTime dteCriteriaTimeLmt = new DateTime();
            bool blnRollBack = false;
            bool blnFlushing = false;
            double vntDcsServTid;

            double lngTankPreselTID;
            double lngLineupPreselTID;
            double lngToTankLineupID;
            double lngFromTankLineupId;
            double lngTankSelTID;
            double lngLineupSelTID;
            int intDCSTankNum;
            int intDCSLineupNum;
            string strLineupName = "";
            AbcTags DataRes = new AbcTags();
            var res = "";

            //'Skip this calc if the pending state is not null
            if (curblend.vntPendSt == null)
            {
                // Get the abc_dest_tanks.flush_tk_flag
                List<AbcBlendDest> DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);
                List<AbcBlendDest> DestTkFlagsFlt = new List<AbcBlendDest>();

                if (DestTkFlags.Count() > 0)
                {
                    blnFlushing = false;
                    strFlushSwgState = "";
                    DestTkFlagsFlt = DestTkFlags.Where<AbcBlendDest>(row => row.InUseFlag == "YES").ToList();

                    if (DestTkFlagsFlt.Count() > 0)
                    {
                        intDestTankID = (int)DestTkFlagsFlt[0].TankId;
                    }

                    // Find if flush_tk_flag=YES for at least one of the records
                    DestTkFlagsFlt = DestTkFlags.Where<AbcBlendDest>(row => row.FlushTkFlag == "YES").ToList();

                    if (DestTkFlagsFlt.Count() > 0)
                    {
                        lngFlushTankId = DestTkFlagsFlt[0].TankId;

                        // get swing flush state from flush tank to destination tank
                        List<AbcBlendSwings> BldSwgTransferVol = await _repository.GetBldSwgTransferVol(curblend.lngID, lngFlushTankId, intDestTankID);

                        if (BldSwgTransferVol.Count() > 0)
                        {
                            strFlushSwgState = (BldSwgTransferVol[0].SwingState == null) ? "" : BldSwgTransferVol[0].SwingState;
                        }
                        else
                        {
                            strFlushSwgState = "";
                        }

                        // If flush swing state is not READY or ACTIVE then flushing is done
                        if (((strFlushSwgState == "READY") || (strFlushSwgState == "ACTIVE")))
                        {
                            blnFlushing = true;
                        }

                    }

                    intNDestTks = DestTkFlags.Count();
                }
                foreach (AbcBlendDest DestTkFlagsObj in DestTkFlags)
                    {
                        lngDestTkId = DestTkFlagsObj.TankId;
                        strFlushTkFlag = DestTkFlagsObj.FlushTkFlag;
                        strTkInUseFlag = DestTkFlagsObj.InUseFlag;
                        // PRODUCT SWING:For PRODUCT tanks obtain the records from abc blend swings
                        // Get the blend swing data for a specific from product tank

                        List<BlendSwingsData> BlendSwingsDataList = await _repository.BlendSwingsData("PRODUCT", (int)lngDestTkId, curblend.lngID);
                        // Set the preselection tid to "0" of all tanks if there are not blend swings
                        // records for the product tank in use
                        if ((strTkInUseFlag == "YES"))
                        {
                            if (BlendSwingsDataList.Count() == 0)
                            {
                                // Get the abc_blender_dest.preselection_tid for all tanks with this material
                                lngPreselTID = -1;
                                List<AbcBlenderDest> BldrDestPreselTID = await _repository.GetBldrDestPreselTID(vntBldrsData[intBldrIdx].Id, curblend.lngID, "%");
                                foreach (AbcBlenderDest BldrDestPreselTIDObj in BldrDestPreselTID)
                                {
                                    lngPreselTID = (BldrDestPreselTIDObj.PreselectionTid == null) ? -1 : Convert.ToDouble(BldrDestPreselTIDObj.PreselectionTid);
                                    //  reset Write value=0 for the Preselection tid
                                    if (lngPreselTID != -1)
                                    {
                                        await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngPreselTID);
                                    }
                                }

                                // Set Preselection to OFF ("0") using tag Blenders.tank_presel_tid and
                                // blenders.lineup_presel_tid
                                lngTankPreselTID = -1;
                                lngLineupPreselTID = -1;

                                List<AbcBlenders> BldrLineupTags = await _repository.GetBldrLineupTags(vntBldrsData[intBldrIdx].Id);

                                if (BldrLineupTags.Count() > 0)
                                {
                                    lngTankPreselTID = (BldrLineupTags[0].TankPreselTid == null) ? -1 : Convert.ToDouble(BldrLineupTags[0].TankPreselTid);
                                    lngLineupPreselTID = (BldrLineupTags[0].LineupPreselTid == null) ? -1 : Convert.ToDouble(BldrLineupTags[0].LineupPreselTid);
                                }

                                if ((lngTankPreselTID != -1))
                                {
                                    await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngTankPreselTID);
                                }

                                if ((lngLineupPreselTID != -1))
                                {
                                    await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngLineupPreselTID);
                                }

                            }

                        }

                        if (BlendSwingsDataList.Count() > 0)
                        {
                            if (gblnProdSwgTimeIn[intBldrIdx] == false)
                            {
                                //  Update blend dest sequences table to hold the new time in
                                await _repository.SetBlendDestSequenceTime(curblend.dteActualStart, curblend.lngID, lngDestTkId, 1);
                                gblnProdSwgTimeIn[intBldrIdx] = true;
                            }
                        }
                        foreach (BlendSwingsData BlendSwingsDataObj in BlendSwingsDataList)
                        {
                            strSwingState = string.IsNullOrEmpty(BlendSwingsDataObj.SwingState) ? "" : BlendSwingsDataObj.SwingState;
                            lngToTankID = BlendSwingsDataObj.ToTkId;
                            dblCriteriaNumberLmt = (BlendSwingsDataObj.CriteriaNumLmt == null) ? 0 : Convert.ToDouble(BlendSwingsDataObj.CriteriaNumLmt);
                            dteCriteriaTimeLmt = (BlendSwingsDataObj.CriteriaTimLmt == null) ? dteCriteriaTimeLmt : Convert.ToDateTime(BlendSwingsDataObj.CriteriaTimLmt);
                            strAutoSwingFlag = BlendSwingsDataObj.AutoSwingFlag;
                            // get Lineup_id for the from tank and to tank

                            List<AbcBlendDest> TkDestData = await _repository.GetTkDestData(curblend.lngID, (int)lngToTankID);
                            lngToTankLineupID = TkDestData[0].LineupId;

                            TkDestData = await _repository.GetTkDestData(curblend.lngID, (int)lngDestTkId);

                            lngFromTankLineupId = TkDestData[0].LineupId;

                            // Get the swing tags from abc_blenders for products
                            AbcBlenders BldrSwingOccurID = await _repository.GetBldrSwingOccurID(vntBldrsData[intBldrIdx].Id);
                            lngSwingOccurredTID = (BldrSwingOccurID.SwingOccurredTid == null) ? -1 : Convert.ToDouble(BldrSwingOccurID.SwingOccurredTid);

                            lngSwingTID = (BldrSwingOccurID.SwingTid == null) ? -1 : Convert.ToDouble(BldrSwingOccurID.SwingTid);

                            // Get The values of the swing ocurred tid/swing tid
                            if (lngSwingOccurredTID != -1)
                            {
                                DataRes = await _repository.GetTagNameAndVal(lngSwingOccurredTID);
                                tagSwingOccurred.vntTagName = DataRes.Name;
                                tagSwingOccurred.vntTagVal = DataRes.ReadValue.ToString();
                            }
                            else
                            {
                                tagSwingOccurred.vntTagName = null;
                                tagSwingOccurred.vntTagVal = "-1";
                            }

                            if (lngSwingTID != -1)
                            {
                                List<AbcTags> ReadWriteVal = await _repository.GetReadWriteVal(lngSwingTID);
                                if (ReadWriteVal.Count() > 0)
                                {
                                    tagSwing.vntTagName = string.IsNullOrEmpty(ReadWriteVal[0].Name) ? null : ReadWriteVal[0].Name;
                                    tagSwing.vntTagVal = (ReadWriteVal[0].WriteValue == null) ? ((int)OnOff.OFF).ToString() : ReadWriteVal[0].WriteValue.ToString();
                                    // Force to check for time out for active swings, even if the write values of
                                    // this tag is OFF. Make sure that the flushing is done
                                    if (((blnFlushing == false) && (strSwingState == "ACTIVE")))
                                    {
                                        tagSwing.vntTagVal = ((int)OnOff.ON_).ToString();
                                    }

                                }
                                else
                                {
                                    tagSwing.vntTagName = null;
                                    tagSwing.vntTagVal = ((int)OnOff.OFF).ToString();
                                }
                            }
                            else
                            {
                                tagSwing.vntTagName = null;
                                tagSwing.vntTagVal = ((int)OnOff.OFF).ToString();
                            }

                            // Get line transfer vol from the flushing tank to destination tank
                            if ((strFlushTkFlag == "YES"))
                            {
                                sngTransferLineVol = dblCriteriaNumberLmt;
                            }

                            if (((strSwingState == "ACTIVE") || ((strSwingState == "READY") && ((tagSwing.vntTagVal == ((int)OnOff.OFF).ToString()) && (tagSwingOccurred.vntTagVal == ((int)OnOff.ON_).ToString())))))
                            {
                                // If swing Ocurred is still ON and this is a new starting blend in the DCS
                                // then skip the swing_ocurred signal for this cycle.  It will be processed only
                                // if the previous state was ACTIVE
                                if (((tagSwingOccurred.vntTagVal == ((int)OnOff.ON_).ToString()) && ((gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED")
                                            && (curblend.strState.Trim() == "ACTIVE"))))
                                {
                                    goto END_SUB;
                                }

                                // Skip the monitor of selected product tank if flushing is true AND the line fill is not
                                // flushed yet
                                if ((((blnFlushing == true) && ((intNDestTks > 1) && (strTkInUseFlag == "YES"))) || ((strTkInUseFlag == "NO") && (strFlushTkFlag == "NO"))))
                                {
                                    goto NEXT_SWING;
                                }

                                if (tagSwingOccurred.vntTagVal == ((int)OnOff.ON_).ToString())
                                {
                                    /// update blend swings table
                                    await _repository.SetBlendSwingData("COMPLETE", curblend.lngID, lngDestTkId, lngToTankID);

                                    //' Get tank Names
                                    strDestTankName = await _repository.GetTankName(lngDestTkId);
                                    strToTankName = await _repository.GetTankName(lngToTankID);

                                    //  Log a message that swing is complete
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN79), programName, "BL-" + curblend.lngID, strDestTankName, strToTankName,
                                    curblend.strName, "", "", "", res);

                                    //            Get the highest Swing sequence for this blend
                                    List<double> BldDestSwgSeq = await _repository.GetBldDestSwgSeq(curblend.lngID);
                                    if (BldDestSwgSeq.Count() > 0)
                                    {
                                        intSwingSeq = (BldDestSwgSeq[0] == null) ? 0 : Convert.ToInt32(BldDestSwgSeq[0]);
                                    }
                                    else
                                    {
                                        intSwingSeq = 0;
                                    }

                                    //  Get the CURRENT Volume of the blend
                                    dblBldVolAdded = (double)curblend.sngCurVol;
                                    //  Get the sum of vol added of the product for all the sequences
                                    dblSumVolAdded = (double) await _repository.GetBldDestSumVolAdded(curblend.lngID);

                                    // Get the Vol added for updating the abc_blend_dest_seq
                                    dblSeqVolAdded = (dblBldVolAdded - dblSumVolAdded);
                                    
                                    // update blend source sequences table
                                    await _repository.SetBlendDestSeqData(dblSeqVolAdded, curblend.lngID, lngDestTkId, intSwingSeq);

                                    // If flush_tk_flag=In_use_Tk_flag then create a new BO
                                    if ((strTkInUseFlag == "YES"))
                                    {
                                        // Download the new BO for rundown case
                                        // Set blend state of the active blend to complete
                                        // set current time to ABC_BLENDS.ACTUAL_END
                                        curblend.strState = "DONE";
                                        await SetStopTimeInt(curblend.lngID);
                                        await _repository.SetBlendEndTime(curblend.lngID);
                                        await _repository.SetBlendState(curblend.lngID, curblend.strState);
                                        // Get blend in READY state AND PREVIOUS_BLEND_ID=curblend.lngId
                                        List<AbcBlends> ReadyPrevBld = await _repository.GetReadyPrevBld(vntBldrsData[intBldrIdx].Id, curblend.lngID);
                                        
                                        if (ReadyPrevBld.Count() > 0)
                                        {
                                            //                          If gstrRundnFlag = "YES" Then
                                            // download any blend that has a previous blend preconfigured
                                            intReadyRundnBlds = ReadyPrevBld.Count();

                                            // Get the data for this blend
                                            lngRundnBldID = ReadyPrevBld[0].Id;
                                            strRundnBldName = ReadyPrevBld[0].Name;
                                            if ((intReadyRundnBlds > 1))
                                            {
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN71), programName, "RUNDOWN BLEND", curblend.strName, curblend.strState,
                                                strRundnBldName, "", "", "", res);
                                            }

                                            // Update the abc_blends.pending_state for this blend
                                            await _repository.SetBlendPendingState(lngRundnBldID, "DOWNLOADING");
                                            
                                            // Swing has happenned on blender ^1. Blend ^2 has been set to DONE. Download a new blend order to DCS
                                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN71), programName, "DOWNLOAD", gstrBldrName, curblend.strName,
                                               "", "", "", "", res);                                            
                                        }
                                        else
                                        {
                                            //  Copy the current blend
                                            strNewBlendId = await DuplicateBO(curblend, (double)vntBldrsData[intBldrIdx].OnSpecVol);
                                            if ((strNewBlendId == ""))
                                            {
                                                // Issue a message that the blend cannot be duplicated
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN82), programName, "DUPLICATE_ERROR", curblend.strName,"",
                                               "", "", "", "", res);

                                                return 0;
                                            }

                                            if ((gstrRundnFlag == "YES"))
                                            {
                                                // get the blend state to define if it was approved or not
                                                List<AbcBlends> BlendStateData = await _repository.GetBlendState(Convert.ToDouble(strNewBlendId));
                                                if (BlendStateData[0].BlendState == "READY")
                                                {
                                                    // Update the abc_blends.pending state                                                                               ending_state for this blend
                                                    await _repository.SetBlendPendingState(Convert.ToDouble(strNewBlendId), "DOWNLOADING");                                                   
                                                }
                                            }
                                            else
                                            {
                                                // Issue a msG: Swing has happenned on blender ^1. Blend ^2 has been set to DONE. Download a new blend order to DCS
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN86), programName, "DOWNLOAD", gstrBldrName, curblend.strName,
                                               "", "", "", "", res);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //  skip until this point is swing is related to line_flush_tk
                                        //                    If curblend.strState = "PAUSED" Then .  Commented out 
                                        // Get the abc_blender_dest.selection_tid for the to_tank_id
                                        List<AbcBlenderDest> BldrDestPreselTID = await _repository.GetBldrDestPreselTID(vntBldrsData[intBldrIdx].Id,curblend.lngID,lngToTankID.ToString());
                                        lngSelTID = (BldrDestPreselTID[0].SelectionTid == null)? -1 : Convert.ToDouble(BldrDestPreselTID[0].SelectionTid);
                                        
                                        if (lngSelTID != -1)
                                        {
                                            // Set Write value=1 for the Selection tid for the To Tk Id when Flushing is done
                                            await _repository.SetWriteTagVal((int)OnOff.ON_,"YES",lngSelTID);
                                        }

                                        //Set the blenders.tank_sel_tid, blenders.lineup_sel_tid in DCS
                                        // %%%%%%%%%%%%%%%%%%%%%%%
                                        lngTankSelTID = -1;
                                        lngLineupSelTID = -1;

                                        List<AbcBlenders> BldrLineupTagsData = await _repository.GetBldrLineupTags(vntBldrsData[intBldrIdx].Id);
                                        
                                        if (BldrLineupTagsData.Count() > 0)
                                        {
                                            lngTankSelTID = (BldrLineupTagsData[0].TankSelTid == null)?-1: Convert.ToDouble(BldrLineupTagsData[0].TankSelTid);
                                            lngLineupSelTID = (BldrLineupTagsData[0].LineupSelTid == null) ? -1 : Convert.ToDouble(BldrLineupTagsData[0].LineupSelTid);
                                        }
                                        
                                        // selection Tank index, lineup index to DCS
                                        if (lngTankSelTID != -1)
                                        {                                            
                                            // Get DCS Tank Num for this tank
                                            intDCSTankNum = -1;
                                            List<AbcTanks> TankNum = await _repository.GetTankNum((int)lngToTankID);
                                            
                                            if (TankNum.Count() > 0)
                                            {
                                                intDCSTankNum = (TankNum[0].DcsTankNum == null) ? -1 : Convert.ToInt32(TankNum[0].DcsTankNum);
                                                strTankName = TankNum[0].Name;
                                            }

                                            if (intDCSTankNum != -1)
                                            {
                                                await _repository.SetWriteTagVal(intDCSTankNum, "YES", lngTankSelTID);
                                            }
                                            else
                                            {
                                                // TANK INDEX IS NULL IN ^1 TABLE. TANK ^2 WILL NOT BE SEL/PRESEL IN DCS
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN97), programName, "BL-" + curblend.lngID, "ABC_TANKS", strTankName,
                                                "", "", "", "", res);
                                            }

                                        }

                                        // Download Lineup sel indexes to DCS
                                        if (lngLineupSelTID != -1)
                                        {
                                            // get DCS Lineup index if selected lineup id is not null
                                            if (lngToTankLineupID != -1)
                                            {
                                                List<DCSProdLineupNum> DCSProdLineupNumData = await _repository.GetDCSProdLineupNum(lngToTankLineupID);

                                                intDCSLineupNum = (int)DCSProdLineupNumData[0].DCSLineUpNum;
                                                strLineupName = DCSProdLineupNumData[0].LineUpName;
                                            }
                                            else
                                            {
                                                intDCSLineupNum = -1;
                                            }

                                            if (intDCSLineupNum != -1)
                                            {
                                                // Write the Selected DCS LINEUP number to the DCS
                                                await _repository.SetWriteTagVal(intDCSLineupNum, "YES", lngLineupSelTID);                                                
                                            }
                                            else
                                            {
                                                // IN BLEND ^1, DEST ^2, PROD DCS LINEUP NUM IS NULL FOR LINEUP ^2.  CMD SEL/PRESEL IGNORED
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN98), programName, "BL-" + curblend.lngID, curblend.strName, strTankName,
                                               strLineupName, "", "", "", res);
                                            }

                                        }

                                        // %%%%%%%%%%%%%%%%%%%%%%%
                                        // if source_destn_type <> "TANK" Then write the ship name to DCS

                                        List<AbcTanks> DataTankID = await _repository.GetDataTankID(lngToTankID);
                                        
                                        strSrceDestType = (DataTankID[0].SourceDestnType == null)? "": DataTankID[0].SourceDestnType;
                                        
                                        if (strSrceDestType != "TANK")
                                        {
                                            // get and set the abc_blender_dest.dest_select_name_tid
                                            List<AbcBlenderDest> BldrDestSelTid = await _repository.GetBldrDestSelTid(vntBldrsData[intBldrIdx].Id,(int)lngToTankID);
                                            lngDestSelectNameTid = (BldrDestSelTid[0].DestSelectNameTid == null) ? -1 : Convert.ToDouble(BldrDestSelTid[0].DestSelectNameTid);


                                            TkDestData = await _repository.GetTkDestData(curblend.lngID, (int)lngToTankID);
                                            
                                            strDestSelectName = (TkDestData[0].DestSelectName == null)? strSrceDestType: TkDestData[0].DestSelectName;
                                           
                                            if (lngDestSelectNameTid != -1)
                                            {
                                                // write the string name to the DCS tag Id
                                                await _repository.SetWriteStrTagVal(strDestSelectName, lngDestSelectNameTid);                                                
                                            }
                                        }

                                        // if the previuos state is active and current state is active update the heel vol of the
                                        // real product tank
                                        if (gArPrevBldData[intBldrIdx].strState.Trim() == "ACTIVE" && curblend.strState.Trim() == "ACTIVE" && blnFlushing == true)
                                        {
                                            if (lngToTankLineupID != -1)
                                            {
                                                // Jget the update_heel_flag from abc_blends to decide whether or not
                                                // the heel_vol should be updated                                                

                                                List<AbcBlends> BlendState = await _repository.GetBlendState(curblend.lngID);
                                                if (BlendState.Count() > 0)
                                                {
                                                    List<DCSProdLineupNum> DCSProdLineupNumData = await _repository.GetDCSProdLineupNum(lngToTankLineupID);
                                                    
                                                    dblDestVolume = (DCSProdLineupNumData[0].DestLineVolume == null)?0: Convert.ToDouble(DCSProdLineupNumData[0].DestLineVolume);
                                                    
                                                    dblPrdHeelVol = (double) await _repository.GetHeelVol(lngToTankID + dblDestVolume);
                                                    // set heel volume in dest tank
                                                    await _repository.SetHeelVol(dblPrdHeelVol,curblend.lngID,lngToTankID);
                                                }
                                            }
                                            // product lineup exists
                                        }
                                        //  active and prev state=Active
                                    }
                                    // flush or real product swing occurred

                                    //  Reset Write value=0 for the swing tid
                                    await _repository.SetWriteTagVal((int)OnOff.OFF,"YES",lngSwingTID);
                                    // Reset pending state to NULL
                                    curblend.vntPendSt = null;
                                    // set ABC_BLENDS.PENDING_STATE to null
                                    await _repository.SetPendingState(null,curblend.lngID);
                                    // Leave the sub, because the blend is in DONE state
                                    if (curblend.strState.Trim() == "DONE")
                                    {
                                        goto END_SUB;
                                    }
                                }
                                else if(Convert.ToInt32(tagSwingOccurred.vntTagVal) == (int)OnOff.OFF && Convert.ToInt32(tagSwing.vntTagVal) == (int)OnOff.ON_)
                                {
                                    // get current time
                                    gDteCurTime = await _repository.GetCurTime();

                                    // get proj default swing time out
                                   AbcProjDefaults SwgDefTimeOut = await _repository.SwgDefTimeOut();
                                    dblSwgTimeOut = (double)SwgDefTimeOut.SwingTimeOut;
                                    
                                    if ((DateAndTime.DateDiff("n", gDteProdSwgCmdTime[intBldrIdx], gDteCurTime) > dblSwgTimeOut))
                                    {
                                        // Issue a message - SWING TIME OUT. SWING WAS NOT PERFORMED IN (BLEND)^1
                                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN75), programName, "SWING_TIME_OUT", "BLEND " + curblend.strName, "",
                                        "", "", "", "", res);
                                        
                                        // Reset pending state to NULL
                                        curblend.vntPendSt = null;
                                        // set ABC_BLENDS.PENDING_STATE to null
                                        await _repository.SetPendingState(null,curblend.lngID);

                                        //  update blend swings table to hold the incomplete swing state
                                        await _repository.SetBlendSwingState(lngDestTkId, curblend.lngID, "INCOMPLETE");

                                        await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngSwingTID);
                                        
                                        // if the previuos state is active and current state is active update the heel vol of the
                                        // real product tank
                                        if (gArPrevBldData[intBldrIdx].strState.Trim() == "ACTIVE" && curblend.strState.Trim() == "ACTIVE" && blnFlushing == true
                                                    && strFlushTkFlag == "YES")
                                        {
                                            if (lngToTankLineupID != -1)
                                            {
                                                List<DCSProdLineupNum> DCSProdLineupNumData = await _repository.GetDCSProdLineupNum(lngToTankLineupID);

                                                dblDestVolume = (DCSProdLineupNumData[0].DestLineVolume == null) ? 0 : Convert.ToDouble(DCSProdLineupNumData[0].DestLineVolume);

                                                // get the update_heel_flag from abc_blends to decide whether or not
                                                // the heel_vol should be updated                                                

                                               List<AbcBlends> BlendState = await _repository.GetBlendState(curblend.lngID);
                                                if (BlendState.Count() > 0)
                                                {
                                                    dblPrdHeelVol = (double)await _repository.GetHeelVol(lngToTankID + dblDestVolume);
                                                    // set heel volume in dest tank
                                                    await _repository.SetHeelVol(dblPrdHeelVol,curblend.lngID,lngToTankID);
                                                }
                                            }
                                            // product lineup exists
                                        }
                                        //  active and prev state=Active
                                    }
                                }
                                else if (blnFlushing == true && intNDestTks > 1 && strFlushTkFlag == "YES" && strTkInUseFlag == "NO" && curblend.sngCurVol > (1.1 * sngTransferLineVol))
                                {
                                    //  Get tank Names
                                    strDestTankName = await _repository.GetTankName(lngDestTkId);
                                    strToTankName = await _repository.GetTankName(lngToTankID);

                                    // update blend swings table
                                    await _repository.SetBlendSwingStateAndDoneAt(lngDestTkId, lngToTankID, curblend.lngID);

                                    // Log a message that swing is incomplete
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN79), programName, "BL-" + curblend.lngID, strDestTankName , strToTankName, curblend.strName,
                                         "", "", "", res);
                                    
                                    //if the previuos state is active and current state is active update the heel vol of the
                                    // real product tank
                                    if (gArPrevBldData[intBldrIdx].strState.Trim() == "ACTIVE" && curblend.strState.Trim() == "ACTIVE" && blnFlushing == true && strFlushTkFlag == "YES")
                                    {
                                        if (lngToTankLineupID != -1)
                                        {
                                           List<DCSProdLineupNum> DCSProdLineupNumData =  await _repository.GetDCSProdLineupNum(lngToTankLineupID);

                                            dblDestVolume = (DCSProdLineupNumData[0].DestLineVolume == null) ? 0 : Convert.ToDouble(DCSProdLineupNumData[0].DestLineVolume);

                                            // get the update_heel_flag from abc_blends to decide whether or not
                                            // the heel_vol should be updated                                            
                                            List<AbcBlends> BlendState = await _repository.GetBlendState(curblend.lngID);
                                            if (BlendState.Count() > 0)
                                            {
                                                dblPrdHeelVol = (double)await _repository.GetHeelVol(lngToTankID + dblDestVolume);
                                                // set heel volume in dest tank
                                                await _repository.SetHeelVol(dblPrdHeelVol,curblend.lngID,lngToTankID);
                                            }
                                        }
                                        // product lineup exists
                                    }
                                    //  active and prev state=Active
                                }
                            }
                            else if (strSwingState.Trim() == "READY" && Convert.ToInt32(tagSwing.vntTagVal) == (int)OnOff.OFF) // Check for Swing Conditions
                            {
                                if (curblend.strState.Trim() == "LOADED" || curblend.strState.Trim() == "ACTIVE" || curblend.strState.Trim() == "PAUSED")
                                {
                                    if (intNDestTks == 1 && strTkInUseFlag == "YES" || intNDestTks > 1 && strTkInUseFlag == "YES" && blnFlushing == false)
                                    {
                                        // (blnFlushing = True And intNDestTks > 1 And _
                                        // strTkInUseFlag = "YES" And curblend.sngCurVol > sngTransferLineVol) Or _
                                        // Nov 05/2001: If flushing =false then do not select product tank in every cycle
                                        if ((blnFlushing == true))
                                        {
                                            lngSelTID = -1;
                                            // Get the abc_blender_dest.selection_tid for the from tank id
                                            List<AbcBlenderDest> BldrDestPreselTIDData = await _repository.GetBldrDestPreselTID(vntBldrsData[intBldrIdx].Id,curblend.lngID,lngDestTkId.ToString());
                                            lngSelTID = (BldrDestPreselTIDData[0].SelectionTid==null)?-1:Convert.ToDouble(BldrDestPreselTIDData[0].SelectionTid);
                                            
                                            if (lngSelTID != -1)
                                            {
                                                // Set Write value=1 for the Selection tid
                                                await _repository.SetWriteTagVal((int)OnOff.ON_, "YES", lngSelTID);                                                
                                            }

                                            //Set the blenders.tank_sel_tid, blenders.lineup_sel_tid in DCS
                                            // %%%%%%%%%%%%%%%%%%%%%%%
                                            lngTankSelTID = -1;
                                            lngLineupSelTID = -1;

                                            List<AbcBlenders> BldrLineupTags1 = await _repository.GetBldrLineupTags(vntBldrsData[intBldrIdx].Id);
                                            
                                            if (BldrLineupTags1.Count() > 0)
                                            {
                                                lngTankSelTID = (BldrLineupTags1[0].TankSelTid == null) ? -1 : (double)BldrLineupTags1[0].TankSelTid;
                                                lngLineupSelTID = (BldrLineupTags1[0].LineupSelTid == null) ? -1 : (double)BldrLineupTags1[0].LineupSelTid;                                                
                                            }
                                            
                                            //selection Tank index, lineup index to DCS
                                            if (lngTankSelTID != -1)
                                            {                                                
                                                // Get DCS Tank Num for this tank
                                                intDCSTankNum = -1;
                                                List<AbcTanks> TankNum = await _repository.GetTankNum((int)lngDestTkId);
                                                
                                                if (TankNum.Count() > 0)
                                                {
                                                    intDCSTankNum = (int)TankNum[0].DcsTankNum;
                                                    strTankName = TankNum[0].Name;
                                                }

                                                if ((intDCSTankNum != -1))
                                                {
                                                    await _repository.SetWriteTagVal(intDCSTankNum, "YES", lngTankSelTID);
                                                }
                                                else
                                                {
                                                    // TANK INDEX IS NULL IN ^1 TABLE. TANK ^2 WILL NOT BE SEL/PRESEL IN DCS
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN97), programName, "BL-" + curblend.lngID, "ABC_TANKS", strTankName, "",
                                                    "", "", "", res);
                                                }

                                            }

                                            // Download Lineup sel indexes to DCS
                                            if ((lngLineupSelTID != -1))
                                            {
                                                // get DCS Lineup index if selected lineup id is not null
                                                if ((lngFromTankLineupId != -1))
                                                {
                                                    List<DCSProdLineupNum> DCSProdLineupNumData = await _repository.GetDCSProdLineupNum(lngFromTankLineupId);

                                                    intDCSLineupNum = (int)DCSProdLineupNumData[0].DCSLineUpNum;
                                                    strLineupName = DCSProdLineupNumData[0].LineUpName;
                                                }
                                                else
                                                {
                                                    intDCSLineupNum = -1;
                                                }

                                                if (intDCSLineupNum != -1)
                                                {
                                                    // Write the Selected DCS LINEUP number to the DCS
                                                    await _repository.SetWriteTagVal(intDCSLineupNum, "YES", lngLineupSelTID);
                                                }
                                                else
                                                {
                                                    // IN BLEND ^1, DEST ^2, PROD DCS LINEUP NUM IS NULL FOR LINEUP ^2.  CMD SEL/PRESEL IGNORED
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN98), programName, "BL-" + curblend.lngID, curblend.strName, strTankName, strLineupName,
                                                    "", "", "", res);
                                                }
                                            }
                                            // %%%%%%%%%%%%%%%%%%%%%%%
                                        }

                                        lngPreselTID = -1;
                                        // Get the abc_blender_dest.preselection_tid for the to tank id
                                        List<AbcBlenderDest> BldrDestPreselTID = await _repository.GetBldrDestPreselTID(vntBldrsData[intBldrIdx].Id,curblend.lngID,lngToTankID.ToString());
                                        lngPreselTID = (BldrDestPreselTID[0].PreselectionTid == null) ? -1 : Convert.ToDouble(BldrDestPreselTID[0].PreselectionTid);
                                        
                                        // Get the abc_blender_dest.preselection_tid for all tanks with this material
                                        lngPreselOFFTID = -1;
                                        BldrDestPreselTID = await _repository.GetBldrDestPreselTID(vntBldrsData[intBldrIdx].Id, curblend.lngID,"%");
                                        
                                        blnRollBack = true;
                                        foreach (AbcBlenderDest BldrDestPreselTIDObj in BldrDestPreselTID)                                        
                                        {
                                            lngPreselOFFTID = (BldrDestPreselTIDObj.PreselectionTid == null) ? -1 : Convert.ToDouble(BldrDestPreselTIDObj.PreselectionTid);
                                            if ((lngPreselOFFTID != lngPreselTID))
                                            {
                                                //  reset Write value=0 for the Preselection tid
                                                await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngPreselOFFTID);
                                            }
                                        }
                                        
                                        blnRollBack = false;
                                        await _repository.SetWriteTagVal((int)OnOff.ON_, "YES", lngPreselTID);
                                        
                                        // Set Preselection to DCS Indexes using tag Blenders.tank_presel_tid and
                                        // blenders.lineup_presel_tid
                                        // %%%%%%%%%%%%%%
                                        lngTankPreselTID = -1;
                                        lngLineupPreselTID = -1;

                                        List<AbcBlenders> BldrLineupTags = await _repository.GetBldrLineupTags(vntBldrsData[intBldrIdx].Id);
                                        
                                        if (BldrLineupTags.Count() > 0)
                                        {
                                            lngTankPreselTID = (BldrLineupTags[0].TankPreselTid == null) ? -1 : Convert.ToDouble(BldrLineupTags[0].TankPreselTid);
                                            lngLineupPreselTID = (BldrLineupTags[0].LineupPreselTid == null) ? -1 : Convert.ToDouble(BldrLineupTags[0].LineupPreselTid);                                             
                                        }
                                        
                                        // Preselect a tank in the DCS
                                        if ((lngTankPreselTID != -1))
                                        {                                            
                                            // Get DCS Tank Num for the to tank
                                            intDCSTankNum = -1;
                                            List<AbcTanks> GetTankNum = await _repository.GetTankNum((int)lngToTankID);
                                            
                                            if (GetTankNum.Count() > 0)
                                            {
                                                intDCSTankNum = (GetTankNum[0].DcsTankNum == null) ? -1 : (int)GetTankNum[0].DcsTankNum;
                                                strTankName = GetTankNum[0].Name;
                                            }

                                            if ((intDCSTankNum != -1))
                                            {
                                                await _repository.SetWriteTagVal(intDCSTankNum, "YES", lngTankPreselTID);
                                            }
                                            else
                                            {
                                                // TANK INDEX IS NULL IN ^1 TABLE. TANK ^2 WILL NOT BE SEL/PRESEL IN DCS
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN97), programName, "BL-" + curblend.lngID, "ABC_TANKS", strTankName, "",
                                                    "", "", "", res);
                                            }

                                        }

                                        if ((lngLineupPreselTID != -1))
                                        {
                                            // get DCS Lineup index if presel lineup id is not null
                                            if ((lngToTankLineupID != -1))
                                            {
                                                List<DCSProdLineupNum> DCSProdLineupNumData = await _repository.GetDCSProdLineupNum(lngToTankLineupID);
                                                intDCSLineupNum = (int)DCSProdLineupNumData[0].DCSLineUpNum;
                                                strLineupName = DCSProdLineupNumData[0].LineUpName;
                                            }
                                            else
                                            {
                                                intDCSLineupNum = -1;
                                            }

                                            if ((intDCSLineupNum != -1))
                                            {
                                                // Write the Preselected DCS LINEUP number to the DCS
                                                await _repository.SetWriteTagVal(intDCSLineupNum, "YES", lngLineupPreselTID);                                                
                                            }
                                            else
                                            {
                                                // IN BLEND ^1, DEST ^2, PROD DCS LINEUP NUM IS NULL FOR LINEUP ^2.  CMD SEL/PRESEL IGNORED
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN98), programName, "BL-" + curblend.lngID, curblend.strName, strTankName, strLineupName,
                                                    "", "", "", res);
                                            }
                                        }

                                        // %%%%%%%%%%%%%%
                                        // if source_destn_type <> "TANK" Then write the ship name to DCS                                        
                                        List<AbcTanks> DataTankID = await _repository.GetDataTankID(lngDestTkId);

                                        strSrceDestType = (DataTankID[0].SourceDestnType == null) ? "" : DataTankID[0].SourceDestnType;
                                        
                                        if (strSrceDestType != "TANK")
                                        {
                                            // get and set the abc_blender_dest.dest_select_name_tid
                                            List<AbcBlenderDest> BldrDestSelTid = await _repository.GetBldrDestSelTid(vntBldrsData[intBldrIdx].Id, (int)lngDestTkId);
                                            lngDestSelectNameTid = (BldrDestSelTid[0].DestSelectNameTid == null) ? -1 : (double)BldrDestSelTid[0].DestSelectNameTid;

                                            List<AbcBlendDest> TkDestData1 = await _repository.GetTkDestData(curblend.lngID, (int)lngDestTkId);
                                            
                                            strDestSelectName = (TkDestData1[0].DestSelectName == null)?strSrceDestType: TkDestData1[0].DestSelectName;
                                            
                                            if (intNDestTks == 1 || (strFlushTkFlag == "YES" && strTkInUseFlag == "YES") || (intNDestTks > 1 && strTkInUseFlag == "YES")
                                                || blnFlushing == false)
                                            {
                                                if ((lngDestSelectNameTid != -1))
                                                {
                                                    // write the string name to the DCS tag Id
                                                    await _repository.SetWriteStrTagVal(strDestSelectName, lngDestSelectNameTid);                                                    
                                                }

                                            }

                                        }

                                        // @@@@@@@@@@@@@@
                                    }
                                    else if (blnFlushing == true && strFlushTkFlag == "YES" && strTkInUseFlag == "NO")
                                    {
                                        // And curblend.sngCurVol < sngTransferLineVol Then
                                        lngPreselTID = -1;
                                        // Get the abc_blender_dest.preselection_tid for the to tank id
                                        List<AbcBlenderDest> BldrDestPreselTID = await _repository.GetBldrDestPreselTID(vntBldrsData[intBldrIdx].Id,curblend.lngID,lngToTankID.ToString());
                                        lngPreselTID = (BldrDestPreselTID[0].PreselectionTid == null) ? -1 : (double)BldrDestPreselTID[0].PreselectionTid;
                                        
                                        // Get the abc_blender_dest.preselection_tid for all tanks with this material
                                        lngPreselOFFTID = -1;
                                        BldrDestPreselTID = await _repository.GetBldrDestPreselTID(vntBldrsData[intBldrIdx].Id,curblend.lngID,"%");
                                        
                                        blnRollBack = true;
                                        foreach (AbcBlenderDest BldrDestPreselTIDObj in BldrDestPreselTID)                                        
                                        {
                                            lngPreselOFFTID = (BldrDestPreselTIDObj.PreselectionTid == null) ? -1 : (double)BldrDestPreselTIDObj.PreselectionTid;
                                            if ((lngPreselOFFTID != lngPreselTID))
                                            {
                                                //  reset Write value=0 for the Preselection tid
                                                await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngPreselOFFTID);                                                
                                            }                                            
                                        }
                                        
                                        blnRollBack = false;
                                        await _repository.SetWriteTagVal((int)OnOff.ON_, "YES", lngPreselTID);
                                        
                                        // Set Preselection to DCS Indexes using tag Blenders.tank_presel_tid and
                                        // blenders.lineup_presel_tid
                                        // %%%%%%%%%%%%%%
                                        lngTankPreselTID = -1;
                                        lngLineupPreselTID = -1;

                                        List<AbcBlenders> BldrLineupTags = await _repository.GetBldrLineupTags(vntBldrsData[intBldrIdx].Id);
                                        
                                        if (BldrLineupTags.Count() > 0)
                                        {
                                            lngTankPreselTID = (BldrLineupTags[0].TankPreselTid == null)?-1: (double)BldrLineupTags[0].TankPreselTid;
                                            lngLineupPreselTID = (BldrLineupTags[0].LineupPreselTid == null)?-1:(double)BldrLineupTags[0].LineupPreselTid;
                                        }
                                        
                                        //Preselect a tank in the DCS
                                        if (lngTankPreselTID != -1)
                                        {                                            
                                            // Get DCS Tank Num for the to tank
                                            intDCSTankNum = -1;
                                            List<AbcTanks> GetTankNum = await _repository.GetTankNum((int)lngToTankID);

                                            if (GetTankNum.Count() > 0)
                                            {
                                                intDCSTankNum = (GetTankNum[0].DcsTankNum == null) ? -1 : (int)GetTankNum[0].DcsTankNum;
                                                strTankName = GetTankNum[0].Name;
                                            }

                                            if ((intDCSTankNum != -1))
                                            {
                                                await _repository.SetWriteTagVal(intDCSTankNum, "YES", lngTankPreselTID);
                                            }
                                            else
                                            {
                                                // TANK INDEX IS NULL IN ^1 TABLE. TANK ^2 WILL NOT BE SEL/PRESEL IN DCS
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN97), programName, "BL-" + curblend.lngID, "ABC_TANKS", strTankName, "",
                                                    "", "", "", res);
                                            }
                                        }

                                        if ((lngLineupPreselTID != -1))
                                        {
                                            // get DCS Lineup index if presel lineup id is not null
                                            if ((lngToTankLineupID != -1))
                                            {
                                                List<DCSProdLineupNum> DCSProdLineupNumData = await _repository.GetDCSProdLineupNum(lngToTankLineupID);
                                                intDCSLineupNum = (int)DCSProdLineupNumData[0].DCSLineUpNum;
                                                strLineupName = DCSProdLineupNumData[0].LineUpName;
                                            }
                                            else
                                            {
                                                intDCSLineupNum = -1;
                                            }

                                            if ((intDCSLineupNum != -1))
                                            {
                                                // Write the Preselected DCS LINEUP number to the DCS
                                                await _repository.SetWriteTagVal(intDCSLineupNum, "YES", lngLineupPreselTID);
                                            }
                                            else
                                            {
                                                // IN BLEND ^1, DEST ^2, PROD DCS LINEUP NUM IS NULL FOR LINEUP ^2.  CMD SEL/PRESEL IGNORED
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN98), programName, "BL-" + curblend.lngID, curblend.strName, strTankName, strLineupName,
                                                    "", "", "", res);
                                            }
                                        }

                                        // %%%%%%%%%%%%%%
                                        // if source_destn_type <> "TANK" Then write the ship name to DCS

                                        List<AbcTanks> DataTankID = await _repository.GetDataTankID(lngDestTkId);
                                        strSrceDestType = (DataTankID[0].SourceDestnType == null) ? "" : DataTankID[0].SourceDestnType;

                                        if (strSrceDestType != "TANK")
                                        {
                                            // get and set the abc_blender_dest.dest_select_name_tid
                                            List<AbcBlenderDest> BldrDestSelTid = await _repository.GetBldrDestSelTid(vntBldrsData[intBldrIdx].Id, (int)lngDestTkId);
                                            lngDestSelectNameTid = (BldrDestSelTid[0].DestSelectNameTid == null) ? -1 : (double)BldrDestSelTid[0].DestSelectNameTid;

                                            List<AbcBlendDest> TkDestData1 = await _repository.GetTkDestData(curblend.lngID, (int)lngDestTkId);

                                            strDestSelectName = (TkDestData1[0].DestSelectName == null) ? strSrceDestType : TkDestData1[0].DestSelectName;

                                            if ((lngDestSelectNameTid != -1))
                                            {
                                                // write the string name to the DCS tag Id
                                                await _repository.SetWriteStrTagVal(strDestSelectName, lngDestSelectNameTid);
                                            }
                                        }
                                        // @@@@@@@@@@@@@@
                                    }

                                    // Check the in service of the tank in the DCS and ABC
                                    List<AbcTanks> DataTankID1 = await _repository.GetDataTankID(lngToTankID);

                                    strTankName = DataTankID1[0].Name;
                                    strAbcServFlag = DataTankID1[0].AbcServiceFlag;
                                    vntDcsServTid = (double)DataTankID1[0].DcsServiceTid;
                                    
                                    await ChkTankServ(curblend.lngID,lngToTankID,strTankName,vntDcsServTid,strAbcServFlag,enumDebugLevel);
                                    //  = OUT_SERV
                                }

                            // For active/paused blends
                            if (curblend.strState.Trim() == "ACTIVE" || curblend.strState.Trim() == "PAUSED")
                            {
                                //' Get the swing Criteria in abc_blend_swings
                                strCriteriaName = (BlendSwingsDataObj.CriteriaName == null) ? "" : BlendSwingsDataObj.CriteriaName;
                                switch (strCriteriaName)
                                {
                                    case "NOW":
                                        if ((strAutoSwingFlag == "YES"))
                                        {
                                            // Issue the swing command
                                            if ((gProjDfs.strAllowStartStop == "YES"))
                                            {
                                                //  Set Write value=1 for the swing tid
                                                await _repository.SetWriteTagVal((int)OnOff.ON_, "YES", lngSwingTID);
                                                
                                                // set pending state to SWINGING
                                                curblend.vntPendSt = "SWINGING";
                                                await _repository.SetPendingState(curblend.vntPendSt,curblend.lngID);
                                                //  update blend swings table to hold the active swing state
                                                await _repository.SetBlendSwingData2("ACTIVE", curblend.lngID, lngDestTkId, lngToTankID);

                                                gDteCurTime = await _repository.GetCurTime();
                                                
                                                //  set the Swing Command Time to a global variable
                                                gDteProdSwgCmdTime[intBldrIdx] = gDteCurTime;
                                            }
                                            else
                                            {
                                                // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "SWING", gstrBldrName, "",
                                                    "", "", "", res);
                                                // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                    "", "", "", res);
                                            }

                                        }
                                        else if ((gstrRundnFlag != "YES"))
                                        {
                                            //  Issue the Pause command
                                            if ((gProjDfs.strAllowStartStop == "YES"))
                                            {
                                                if ((curblend.strState.Trim() != "PAUSED"))
                                                {
                                                    // set pending state to PAUSING
                                                    curblend.vntPendSt = "PAUSING";

                                                    //  update blend swings table to hold the active swing state
                                                    await _repository.SetPendingState(curblend.vntPendSt, curblend.lngID);

                                                    // save the bookmark to recover the rs position after command download
                                                    //List<AbcBlendDest> DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);
                                                    //List<AbcBlendDest> DestTkFlagsFlt = new List<AbcBlendDest>();

                                                    // call PAUSE_BLEND function
                                                    await ProcessBldCmd(BlendCmds.PAUSE, intBldrIdx, vntBldrsData, curblend, enumDebugLevel);
                                                    
                                                    DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);

                                                }

                                                // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                   "", "", "", res);
                                            }
                                            else
                                            {
                                                // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "SWING", gstrBldrName, "",
                                                   "", "", "", res);
                                                // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                  "", "", "", res);
                                            }

                                        }
                                        break;
                                    case "TANK VOLUME":
                                        // Get the data from the from tank id
                                        List<ASTankID> ASTankIDdata = await _repository.GetASTankID((int)lngDestTkId);
                                        
                                        dblMinvol = (ASTankIDdata[0].MinVol == null)?0: (double)ASTankIDdata[0].MinVol;
                                        dblAvailVol = (ASTankIDdata[0].AvailVol == null)?0:(double)ASTankIDdata[0].AvailVol;
                                        
                                        // Compare the criteria_num_lmt with current vol in the tank
                                        if ((dblCriteriaNumberLmt <= (dblAvailVol + dblMinvol)))
                                        {
                                            if ((strAutoSwingFlag == "YES"))
                                            {
                                                //  Issue the swing command
                                                if ((gProjDfs.strAllowStartStop == "YES"))
                                                {
                                                    await _repository.SetWriteTagVal((int)OnOff.ON_, "YES", lngSwingTID);

                                                    // set pending state to SWINGING
                                                    curblend.vntPendSt = "SWINGING";
                                                    await _repository.SetPendingState(curblend.vntPendSt, curblend.lngID);
                                                    //  update blend swings table to hold the active swing state
                                                    await _repository.SetBlendSwingData2("ACTIVE", curblend.lngID, lngDestTkId, lngToTankID);

                                                    gDteCurTime = await _repository.GetCurTime();

                                                    //  set the Swing Command Time to a global variable
                                                    gDteProdSwgCmdTime[intBldrIdx] = gDteCurTime;
                                                }
                                                else
                                                {
                                                    // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "SWING", gstrBldrName, "",
                                                    "", "", "", res);
                                                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                        "", "", "", res);
                                                }

                                            }
                                            else if ((gstrRundnFlag != "YES"))
                                            {
                                                //  Issue the Paused command
                                                if ((gProjDfs.strAllowStartStop == "YES"))
                                                {
                                                    if ((curblend.strState.Trim() != "PAUSED"))
                                                    {
                                                        curblend.vntPendSt = "PAUSING";

                                                        //  update blend swings table to hold the active swing state
                                                        await _repository.SetPendingState(curblend.vntPendSt, curblend.lngID);

                                                        // save the bookmark to recover the rs position after command download
                                                        //List<AbcBlendDest> DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);
                                                        //List<AbcBlendDest> DestTkFlagsFlt = new List<AbcBlendDest>();

                                                        // call PAUSE_BLEND function
                                                        await ProcessBldCmd(BlendCmds.PAUSE, intBldrIdx, vntBldrsData, curblend, enumDebugLevel);

                                                        DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);

                                                    }

                                                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                    "", "", "", res);                                                    
                                                }
                                                else
                                                {
                                                    // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "SWING", gstrBldrName, "",
                                                    "", "", "", res);
                                                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                      "", "", "", res);
                                                }

                                            }

                                        }
                                        break;
                                    case "BLEND VOLUME":
                                        // Download swing_target_vol and swing_exist tags values to DCS
                                        if (((strFlushTkFlag == "YES") && (blnFlushing == true)))
                                        {                                            
                                            if (vntBldrsData[intBldrIdx].SwingVolTid != null)
                                            {
                                                // Turn ON the swing exist flag in DCS
                                                if (vntBldrsData[intBldrIdx].SwingExistTid != null)
                                                {
                                                    await _repository.SetWriteTagVal((int)YesNo.YES, "YES", vntBldrsData[intBldrIdx].SwingExistTid);
                                                }

                                                // Download the swing target volume in DCS
                                                await _repository.SetWriteTagVal((int)dblCriteriaNumberLmt, "YES", vntBldrsData[intBldrIdx].SwingVolTid);                                                
                                            }

                                        }
                                        else if (((strTkInUseFlag == "YES") && (blnFlushing == false)))
                                        {                                            
                                            if (vntBldrsData[intBldrIdx].SwingVolTid != null)
                                            {
                                                // Turn ON the swing exist flag in DCS
                                                if (vntBldrsData[intBldrIdx].SwingExistTid != null)
                                                {
                                                    await _repository.SetWriteTagVal((int)YesNo.YES, "YES", vntBldrsData[intBldrIdx].SwingExistTid);
                                                }

                                                // Download the swing target volume in DCS
                                                await _repository.SetWriteTagVal((int)dblCriteriaNumberLmt, "YES", vntBldrsData[intBldrIdx].SwingVolTid);
                                            }
                                        }

                                        // Compare the criteria_num_lmt with current vol in the tank
                                        if (curblend.sngCurVol >= dblCriteriaNumberLmt)
                                        {
                                            if ((strAutoSwingFlag == "YES"))
                                            {
                                                //  Issue the swing command
                                                if ((gProjDfs.strAllowStartStop == "YES"))
                                                {
                                                    await _repository.SetWriteTagVal((int)OnOff.ON_, "YES", lngSwingTID);

                                                    // set pending state to SWINGING
                                                    curblend.vntPendSt = "SWINGING";
                                                    await _repository.SetPendingState(curblend.vntPendSt, curblend.lngID);
                                                    //  update blend swings table to hold the active swing state
                                                    await _repository.SetBlendSwingData2("ACTIVE", curblend.lngID, lngDestTkId, lngToTankID);

                                                    gDteCurTime = await _repository.GetCurTime();

                                                    //  set the Swing Command Time to a global variable
                                                    gDteProdSwgCmdTime[intBldrIdx] = gDteCurTime;
                                                }
                                                else
                                                {
                                                    // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "SWING", gstrBldrName, "",
                                                    "", "", "", res);
                                                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                        "", "", "", res);
                                                }

                                            }
                                            else if ((gstrRundnFlag != "YES"))
                                            {
                                                //  Issue the Paused command
                                                if ((gProjDfs.strAllowStartStop == "YES"))
                                                {
                                                    if ((curblend.strState.Trim() != "PAUSED"))
                                                    {
                                                        curblend.vntPendSt = "PAUSING";

                                                        //  update blend swings table to hold the active swing state
                                                        await _repository.SetPendingState(curblend.vntPendSt, curblend.lngID);

                                                        // save the bookmark to recover the rs position after command download
                                                        //List<AbcBlendDest> DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);
                                                        //List<AbcBlendDest> DestTkFlagsFlt = new List<AbcBlendDest>();

                                                        // call PAUSE_BLEND function
                                                        await ProcessBldCmd(BlendCmds.PAUSE, intBldrIdx, vntBldrsData, curblend, enumDebugLevel);

                                                        DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);

                                                    }

                                                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                    "", "", "", res);
                                                }
                                                else
                                                {
                                                    // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "SWING", gstrBldrName, "",
                                                    "", "", "", res);
                                                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                      "", "", "", res);
                                                }

                                            }

                                        }
                                        break;
                                    case "SWING TIME":
                                        gDteCurTime = await _repository.GetCurTime();
                                        // Compare the criteria_num_lmt with current vol in the tank
                                        if (gDteCurTime >= dteCriteriaTimeLmt)
                                        {
                                            if ((strAutoSwingFlag == "YES"))
                                            {
                                                //  Issue the swing command
                                                if ((gProjDfs.strAllowStartStop == "YES"))
                                                {
                                                    await _repository.SetWriteTagVal((int)OnOff.ON_, "YES", lngSwingTID);

                                                    // set pending state to SWINGING
                                                    curblend.vntPendSt = "SWINGING";
                                                    await _repository.SetPendingState(curblend.vntPendSt, curblend.lngID);
                                                    //  update blend swings table to hold the active swing state
                                                    await _repository.SetBlendSwingData2("ACTIVE", curblend.lngID, lngDestTkId, lngToTankID);

                                                    gDteCurTime = await _repository.GetCurTime();

                                                    //  set the Swing Command Time to a global variable
                                                    gDteProdSwgCmdTime[intBldrIdx] = gDteCurTime;
                                                }
                                                else
                                                {
                                                    // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "SWING", gstrBldrName, "",
                                                    "", "", "", res);
                                                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                        "", "", "", res);
                                                }

                                            }
                                            else if ((gstrRundnFlag != "YES"))
                                            {
                                                //  Issue the Paused command
                                                if ((gProjDfs.strAllowStartStop == "YES"))
                                                {
                                                    if ((curblend.strState.Trim() != "PAUSED"))
                                                    {
                                                        curblend.vntPendSt = "PAUSING";

                                                        //  update blend swings table to hold the active swing state
                                                        await _repository.SetPendingState(curblend.vntPendSt, curblend.lngID);

                                                        // save the bookmark to recover the rs position after command download
                                                        //List<AbcBlendDest> DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);
                                                        //List<AbcBlendDest> DestTkFlagsFlt = new List<AbcBlendDest>();

                                                        // call PAUSE_BLEND function
                                                        await ProcessBldCmd(BlendCmds.PAUSE, intBldrIdx, vntBldrsData, curblend, enumDebugLevel);

                                                        DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);

                                                    }

                                                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                    "", "", "", res);
                                                }
                                                else
                                                {
                                                    // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "SWING", gstrBldrName, "",
                                                    "", "", "", res);
                                                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                      "", "", "", res);
                                                }

                                            }

                                        }
                                        break;
                                    case "HIGH LIMIT":
                                        //'Get the data from the from tank id
                                        List<ASTankID> ASTankIDData = await _repository.GetASTankID((int)lngDestTkId);

                                        dblMinvol = (ASTankIDData[0].MinVol == null) ? 0 : (double)ASTankIDData[0].MinVol;
                                        dblMaxVol = (ASTankIDData[0].MaxVol == null) ? 0 : (double)ASTankIDData[0].MaxVol;
                                        dblAvailVol = (ASTankIDData[0].AvailVol == null) ? 0 : (double)ASTankIDData[0].AvailVol;
                                        
                                        // Compare the Max vol with current vol in the tank
                                        if ((dblAvailVol + dblMinvol) >= dblMaxVol)
                                        {
                                            if ((strAutoSwingFlag == "YES"))
                                            {
                                                //  Issue the swing command
                                                if ((gProjDfs.strAllowStartStop == "YES"))
                                                {
                                                    await _repository.SetWriteTagVal((int)OnOff.ON_, "YES", lngSwingTID);

                                                    // set pending state to SWINGING
                                                    curblend.vntPendSt = "SWINGING";
                                                    await _repository.SetPendingState(curblend.vntPendSt, curblend.lngID);
                                                    //  update blend swings table to hold the active swing state
                                                    await _repository.SetBlendSwingData2("ACTIVE", curblend.lngID, lngDestTkId, lngToTankID);

                                                    gDteCurTime = await _repository.GetCurTime();

                                                    //  set the Swing Command Time to a global variable
                                                    gDteProdSwgCmdTime[intBldrIdx] = gDteCurTime;
                                                }
                                                else
                                                {
                                                    // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "SWING", gstrBldrName, "",
                                                    "", "", "", res);
                                                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                        "", "", "", res);
                                                }

                                            }
                                            else if ((gstrRundnFlag != "YES"))
                                            {
                                                //  Issue the Paused command
                                                if ((gProjDfs.strAllowStartStop == "YES"))
                                                {
                                                    if ((curblend.strState.Trim() != "PAUSED"))
                                                    {
                                                        curblend.vntPendSt = "PAUSING";

                                                        //  update blend swings table to hold the active swing state
                                                        await _repository.SetPendingState(curblend.vntPendSt, curblend.lngID);

                                                        // save the bookmark to recover the rs position after command download
                                                        //List<AbcBlendDest> DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);
                                                        //List<AbcBlendDest> DestTkFlagsFlt = new List<AbcBlendDest>();

                                                        // call PAUSE_BLEND function
                                                        await ProcessBldCmd(BlendCmds.PAUSE, intBldrIdx, vntBldrsData, curblend, enumDebugLevel);

                                                        DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);

                                                    }

                                                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                    "", "", "", res);
                                                }
                                                else
                                                {
                                                    // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "SWING", gstrBldrName, "",
                                                    "", "", "", res);
                                                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                      "", "", "", res);
                                                }

                                            }

                                        }
                                        break;
                                    case "LOW LIMIT":
                                        //'Get the data from the from tank id
                                        List<ASTankID> ASTankIDData2 = await _repository.GetASTankID((int)lngDestTkId);

                                        dblMinvol = (ASTankIDData2[0].MinVol == null) ? 0 : (double)ASTankIDData2[0].MinVol;
                                        dblMaxVol = (ASTankIDData2[0].MaxVol == null) ? 0 : (double)ASTankIDData2[0].MaxVol;
                                        dblAvailVol = (ASTankIDData2[0].AvailVol == null) ? 0 : (double)ASTankIDData2[0].AvailVol;

                                        // Compare the Max vol with current vol in the tank
                                        if ((dblAvailVol + dblMinvol) <= dblMinvol)
                                        {
                                            if ((strAutoSwingFlag == "YES"))
                                            {
                                                //  Issue the swing command
                                                if ((gProjDfs.strAllowStartStop == "YES"))
                                                {
                                                    await _repository.SetWriteTagVal((int)OnOff.ON_, "YES", lngSwingTID);

                                                    // set pending state to SWINGING
                                                    curblend.vntPendSt = "SWINGING";
                                                    await _repository.SetPendingState(curblend.vntPendSt, curblend.lngID);
                                                    //  update blend swings table to hold the active swing state
                                                    await _repository.SetBlendSwingData2("ACTIVE", curblend.lngID, lngDestTkId, lngToTankID);

                                                    gDteCurTime = await _repository.GetCurTime();

                                                    //  set the Swing Command Time to a global variable
                                                    gDteProdSwgCmdTime[intBldrIdx] = gDteCurTime;
                                                }
                                                else
                                                {
                                                    // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "SWING", gstrBldrName, "",
                                                    "", "", "", res);
                                                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                        "", "", "", res);
                                                }

                                            }
                                            else if ((gstrRundnFlag != "YES"))
                                            {
                                                //  Issue the Paused command
                                                if ((gProjDfs.strAllowStartStop == "YES"))
                                                {
                                                    if ((curblend.strState.Trim() != "PAUSED"))
                                                    {
                                                        curblend.vntPendSt = "PAUSING";

                                                        //  update blend swings table to hold the active swing state
                                                        await _repository.SetPendingState(curblend.vntPendSt, curblend.lngID);

                                                        // save the bookmark to recover the rs position after command download
                                                        //List<AbcBlendDest> DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);
                                                        //List<AbcBlendDest> DestTkFlagsFlt = new List<AbcBlendDest>();

                                                        // call PAUSE_BLEND function
                                                        await ProcessBldCmd(BlendCmds.PAUSE, intBldrIdx, vntBldrsData, curblend, enumDebugLevel);

                                                        DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);

                                                    }

                                                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                    "", "", "", res);
                                                }
                                                else
                                                {
                                                    // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "SWING", gstrBldrName, "",
                                                    "", "", "", res);
                                                    // SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN76), programName, "BL-" + curblend.lngID, curblend.strName, "", "",
                                                      "", "", "", res);
                                                }

                                            }

                                        }
                                        break;
                                }
                            }

                        }
                        NEXT_SWING: { }
                        }
                    }
                END_SUB: { }
                
            }
            return 0;
        }
        private async Task<int> MonitorBlend(int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend, int intDestTankID, DebugLevels enumDebugLevel)
        {
            string strPrdName;
            // , strCalcPropFlag As String
            string strGrdName;
            DateTime dteOpmonTime;
            object varPropID;
            double dblInitialBias;
            DcsTag tagPermissive = new DcsTag();
            var res = "";
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            if ((enumDebugLevel == DebugLevels.High))
            {
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, curblend.strName, "MONITOR_BLEND",
                    "", "", "", "", res);
            }


            //get current time
            gDteCurTime = await _repository.GetCurTime();

            //to set the actual start
            if (gArPrevBldData[intBldrIdx].strState.Trim() != "ACTIVE" && curblend.dteActualStart == cdteNull)
            {
                // if ABC_BLENDS.ACTUAL_START is null, then this is the start of a new
                // blend, hence set ABC_BLENDS.ACTUAL_START to current time
                await _repository.SetBlendStartTime(curblend.lngID);
            }
            else if (curblend.strState.Trim() == "ACTIVE" && curblend.dteActualStart == cdteNull)
            {
                await _repository.SetBlendStartTime(curblend.lngID);
            }

            // check DCS->ABC communication
            if (await _shared.ChkDcsComm(curblend.lngID, vntBldrsData[intBldrIdx].Id, gstrBldrName) == GoodBad.BAD)
            {
                return 0;
            }

            //get download OK tag (permissive tag) value from ABC_TAGS            
            AbcTags DataRes = await _repository.GetTagNameAndVal(vntBldrsData[intBldrIdx].DownloadOkTid);
            tagPermissive.vntTagName = DataRes.Name;
            tagPermissive.vntTagVal = DataRes.ReadValue.ToString();

            if (((tagPermissive.vntTagVal == null) ? (int)OnOff.OFF : Convert.ToInt32(tagPermissive.vntTagVal)) == (int)OnOff.ON_)
            {
                if ((gProjDfs.strAllowRateVolUpds == "YES"))
                {
                    if ((curblend.sngTgtVol != gArPrevBldData[intBldrIdx].sngPrevBldTargVol))
                    {
                        // send new target vol to DCS
                        await _repository.SetWriteTagVal(Convert.ToInt32(curblend.sngTgtVol), "YES", vntBldrsData[intBldrIdx].TargVolTid);
                        // Save the previous Blend Target volume
                        gArPrevBldData[intBldrIdx].sngPrevBldTargVol = curblend.sngTgtVol;
                    }

                    if (curblend.sngTgtRate != gArPrevBldData[intBldrIdx].sngPrevBldTargRate)
                    {
                        // send new target rate to DCS
                        await _repository.SetWriteTagVal(Convert.ToInt32(curblend.sngTgtRate), "YES", vntBldrsData[intBldrIdx].TargRateTid);
                        // Save the previous Blend Target Rate
                        gArPrevBldData[intBldrIdx].sngPrevBldTargRate = curblend.sngTgtRate;
                    }

                }

            }

            // *****************************
            // March 12/2001: Get the RBC Rate FB TID from DCS and update abc_blends.rate_sp_fb
            //    ABCdataEnv.cmdGetTagNameAndVal vntBldrsData(RBC_RATE_SP_FB_TID, intBldrIdx)
            //    ABCdataEnv.cmdSetBldRateSPFB vntBldrsData(RBC_RATE_SP_FB_TID, intBldrIdx), curBlend.lngID
            if (enumDebugLevel == DebugLevels.High)
            {
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG15), programName, cstrDebug, curblend.strName, curblend.sngTgtRate.ToString(),
                    curblend.sngTgtVol.ToString(), gProjDfs.strAllowRateVolUpds, "", "", res);
            }


            // get the last interval if the blend monitor just started
            if ((gArPrevBldData[intBldrIdx].intCurIntv == 0))
            {
                List<AbcBlendIntervals> BlendIntvs = await _repository.GetBlendIntvs(curblend.lngID);
                if (BlendIntvs.Count() > 0)
                {
                    //ABCdataEnv.rscmdGetBlendIntvs.MoveLast;
                    gArPrevBldData[intBldrIdx].intCurIntv = Convert.ToInt32(BlendIntvs[BlendIntvs.Count() - 1].Sequence);
                }
            }

            if ((gArBldFinishTime[intBldrIdx] != cdteNull))
            {
                // if the blend is done, then close open interval and process lineprop
                // save current time into stop time of last interval
                // ABCdataEnv.cmdSetIntvEndTime gDteCurTime, curblend.lngID, gArPrevBldData[intBldrIdx].intCurIntv
                // set the current interval = Prev interval and process Lineprop
                curblend.intCurIntv = gArPrevBldData[intBldrIdx].intCurIntv;
            }
            else if (gArPrevBldData[intBldrIdx].strState.Trim() == "PAUSED" && curblend.strState.Trim() == "ACTIVE")
            {
                // wait for one more cycle to create a new interval after a Paused state
                curblend.intCurIntv = gArPrevBldData[intBldrIdx].intCurIntv;
                gblnPrevStatePaused[intBldrIdx] = true;
            }
            else if ((gblnPrevStatePaused[intBldrIdx] == true))
            {
                // second cycle after a paused state
                curblend.intCurIntv = gArPrevBldData[intBldrIdx].intCurIntv;
                gblnPrevStatePaused[intBldrIdx] = false;
            }
            else
            {
                await ChkIntervals(intBldrIdx, curblend, enumDebugLevel);
            }

            //This is for copying the previous BIAS to the current interval BIAS if the interval is new
            //In addition to the bias, copy also the BiasCalc_current to the new interval
            if (curblend.intCurIntv > gArPrevBldData[intBldrIdx].intCurIntv && curblend.intCurIntv > 1)
            {
                // Populate bias field for interval 1 from abc_blend_props.initial_bias as soon as it is created
                // THIS CODE WAS MOVED TO shared (ChkNewIntvlCreation)
                // The cmd update was improve to update all props at once, intead
                // of looping one by one
                // modified this update statement to update the biascalc_current field along with
                // the interval bias.  Dec. 03: update unfiltered_bias=Bias from prev interval
                await _repository.CopyPrevBias(gArPrevBldData[intBldrIdx].intCurIntv, gArPrevBldData[intBldrIdx].intCurIntv, gArPrevBldData[intBldrIdx].intCurIntv,
                curblend.lngID, curblend.intCurIntv);
            }

            //    ********************
            // ERIK ChkDcsRcp only if debug enabled
            if (enumDebugLevel >= DebugLevels.Low)
            {
                // if new interval, call CHECK_DCS_RECIPE
                if (curblend.intCurIntv > gArPrevBldData[intBldrIdx].intCurIntv && curblend.intCurIntv > 1)
                {
                    await ChkDcsRcp(intBldrIdx, curblend.lngID, curblend.strName, enumDebugLevel);
                }
            }

            // ***********************************
            // if ALLOW_COMP_UPDATES is YES then call UPDATE_PROP_TABLE function
            if ((gProjDfs.strAllowCompUpds == "YES"))
            {
                await UpdatePropTable(intBldrIdx, Convert.ToInt32(vntBldrsData[intBldrIdx].PrdgrpId), curblend.lngID, curblend.strName, enumDebugLevel);
            }

            // call CALC_BLEND function
            await CalcBlend(intBldrIdx, vntBldrsData, curblend, enumDebugLevel);

            // issue warning msg if current vol exceeds target vol * 1.01 for the blend
            if ((curblend.sngCurVol
                        > (curblend.sngTgtVol * 1.01)))
            {
                // warning msg "Total volume in blend ^1 exceeding target volume"                
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN51), programName, "BL-" + curblend.lngID, curblend.sngCurVol.ToString(), curblend.strName.ToString(),
                    curblend.sngTgtVol.ToString(), "", "", "", res);
            }

            //   Download the comment in case it has been changed
            // Concatenate the Product name and Grade and download comment

            if (((tagPermissive.vntTagVal == null) ? (int)OnOff.OFF : Convert.ToInt32(tagPermissive.vntTagVal)) == (int)OnOff.ON_)
            {
                if (vntBldrsData[intBldrIdx].BlendDescTid != null && curblend.strBldDesc != gArPrevBldData[intBldrIdx].strPrevBldDescr)
                {
                    strPrdName = await _repository.GetCompName(curblend.intProdID);

                    strGrdName = await _repository.GetGradeName(curblend.intGrdID);

                    if (vntBldrsData[intBldrIdx].GradeTid == null)
                    {
                        await _repository.SetWriteStrTagVal((strPrdName + ("//"
                                    + (strGrdName + ("//" + curblend.strBldDesc)))), vntBldrsData[intBldrIdx].BlendDescTid);
                    }
                    else
                    {
                        // If grade_tid is not null, then download the description alone
                        await _repository.SetWriteStrTagVal(curblend.strBldDesc, vntBldrsData[intBldrIdx].BlendDescTid);
                    }

                    // Save the previous Blend description
                    gArPrevBldData[intBldrIdx].strPrevBldDescr = curblend.strBldDesc;
                }

            }

            //   ******
            // Nov.07/2001: Skip one cycle if the calcblend procedure was aborted because of
            //  the difference between scan times in the totalyzers tags
            if (((gintSkipCycleBmon[intBldrIdx] == 0) || (gintSkipCycleBmon[intBldrIdx] == 2)))
            {
                //  call Prod TANK_SWING function                
                await SwingProdTank(intBldrIdx, vntBldrsData, curblend, intDestTankID, enumDebugLevel);
                // Skip comp Tank swing if the pending state is not null and blend state is DONE
                // RW 31-Jul-14 PreemL PQL-79
                // If IsNull(curblend.vntPendSt) And Trim(curblend.strState) <> "DONE" Then
                if ((((curblend.vntPendSt == null) || (curblend.vntPendSt == "SWINGING")) && (curblend.strState.Trim() != "DONE")))
                {
                   await SwingCompTank(intBldrIdx, vntBldrsData, curblend, enumDebugLevel);
                }

                // Nov 07/2001: Relocated this function to be after Prod Swing checking
                // to avoid unnecessary messages when a product swing happens
                // RW 17-Feb-16 PreemL PQL-130
                // If Trim(curblend.strState) <> "DONE" Or gArBldFinishTime[intBldrIdx] = cdteNull Then
                if (((curblend.strState.Trim() != "DONE") && (gArBldFinishTime[intBldrIdx] == cdteNull)))
                {
                    // call CHECK_DCS_FEEDBACK function
                   await ChkDcsFeedback(intBldrIdx, vntBldrsData, curblend, intDestTankID, enumDebugLevel);
                }

            }

            // *******
            if (((curblend.vntPendSt == "OPTIMIZING") && (curblend.strState.Trim() != "DONE")))
            {
                // get last_run_time of OPTIM MONITOR
                //     ABCdataEnv.cmdGetLastRunTime "ABC OPTIMIZE MONITOR", dteOpmonTime
                List<DateTime?> LastOptTime = await _repository.GetLastOptTime(curblend.lngID);
                
                if (LastOptTime.Count() > 0)
                {
                    dteOpmonTime = (LastOptTime[0] == null)?cdteNull: Convert.ToDateTime(LastOptTime[0]);
                    if ((DateAndTime.DateDiff("s", dteOpmonTime, gDteCurTime) > (3 * (curblend.vntIntvLen * 60))))
                    {
                        // warning msg "Optimizer Monitor may be inactive"
                        await _repository.LogMessage(Convert.ToInt32(CommonMsgTmpIDs.COM_W1), programName, "BL-" + curblend.lngID, "ABC OPTIMIZER MONITOR", "",
                    "", "", "", "", res);

                        // set ABC_BLENDS.PENDING_STATE to null
                        await _repository.SetPendingState(null, curblend.lngID);
                        curblend.vntPendSt = "";
                    }
                }
            }

            return 0;
        }
        private async Task<int> SetStopTimeInt(double blendId)
        {
            DateTime dteGetCurTime;
            double IntvNum;
            DateTime vntStartTime;
            List<AbcBlendIntervals> BlendIntvs = await _repository.GetBlendIntvs(blendId);
            if(BlendIntvs.Count() > 0)
            {
                IntvNum = (BlendIntvs[BlendIntvs.Count() - 1].Sequence == null) ? -1 : BlendIntvs[BlendIntvs.Count() - 1].Sequence;
                vntStartTime = Convert.ToDateTime(BlendIntvs[BlendIntvs.Count() - 1].Starttime);

                if (vntStartTime != null) {
                    dteGetCurTime = await _repository.GetCurTime();
                    //'save current time into stop time of last interval
                    await _repository.SetIntvEndTime(dteGetCurTime, blendId, IntvNum);
                }
            }
            return 0;
        }

        // *********** FinishBlend ***********        
        private async Task<int> FinishBlend(int intBldrID, CurBlendData curblend, int intDestTankID, DebugLevels enumDebugLevel, bool blnGenReport = false)
        {
            object vntLnupSlctTid;
            // Warning!!! Optional parameters not supported
            double? vntBldrSrcTid;
            double? vntCompSlctTid;
            double lngPrdLnupSlctTid;
            double lngDummy;
            int intLnupID;
            double lngLineupSelTID;
            //  RW 22/10/2010
            double lngPumpInUseTid;
            //  RW 22/10/2010
            //    Dim intMatID As Integer, intSrcTankID As Integer
            int intMatID;
            //  RW 22/10/2010
            int intCounter = 0;
            //  RW 22/10/2010
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            var res = "";
            if (enumDebugLevel == DebugLevels.High)
            {                
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, curblend.strName, "FINISH_BLEND",
                   "", "", "", "", res);
            }

            List<AbcProdLineups> PrdLnupSlctTid = await _repository.GetPrdLnupSlctTid(curblend.lngID,intDestTankID,intBldrID,intDestTankID);
            if (PrdLnupSlctTid.Count() > 0)
            {
                lngPrdLnupSlctTid = (PrdLnupSlctTid[0].SelectionTid == null)?-1: Convert.ToDouble(PrdLnupSlctTid[0].SelectionTid);
                lngDummy = (PrdLnupSlctTid[0].SelectionFbTid == null) ? -1 : Convert.ToDouble(PrdLnupSlctTid[0].SelectionFbTid);
                await _repository.SetWriteTagVal((int)YesNo.NO,"NO",lngPrdLnupSlctTid);
            }
            else
            {
                lngPrdLnupSlctTid = -1;
                lngDummy = -1;
            }

            List<CompEqpData> CompEqpData = await _repository.GetCompEqpData(curblend.lngID,intBldrID);

            foreach (CompEqpData CompEqpDataObj in CompEqpData)           
            {
                //  Moved from 8 lines below to fix bug where lineup id
                //  was read from first record only and therefore only the pumps
                //  used by the first line up had their in use tags reset RW 22/10/2010
                //    intSrcTankID = NVL(ABCdataEnv.rscmdGetCompEqpData.Fields("TANK_ID"), NULL_)
                intMatID = (CompEqpDataObj.MatId == null)?-1: Convert.ToInt32(CompEqpDataObj.MatId);
                //  Uncommented out RW 22/10/2010
                intLnupID = (CompEqpDataObj.LineupId == null) ? -1 : Convert.ToInt32(CompEqpDataObj.LineupId);                
                
                vntBldrSrcTid = CompEqpDataObj.Bldrsrctid;
                vntCompSlctTid = CompEqpDataObj.Selectcomptid;
                intCounter = (intCounter + 1);
                //  RW 22/10/2010
                // Do Until ABCdataEnv.rscmdGetCompEqpData.EOF    ' Commented out RW 22/10/2010
                // set blend source in_use_flag to NO
                //  Partha - commenting the foll. line - else blend order form won't show any material/tanks
                //  after the blend is complete - 7/27/2000
                //  ABCdataEnv.cmdSetSrcTankFlag "NO", curBlend.lngID, intMatID, intSrcTankID.Value
                // set comp lineup selection tag to NO
                //  the foll. line is not needed since the linup is not downloaded to DCS (only individual eqp. ids are downloaded)
                //  ABCdataEnv.cmdSetWriteTagVal NO,"NO", vntLnupSlctTid.Value
                // set blender source selection tag to NO
                await _repository.SetWriteTagVal((int)YesNo.NO,"NO",vntBldrSrcTid);
                // set blender comp selection tag to NO
                await _repository.SetWriteTagVal((int)YesNo.NO,"NO",vntCompSlctTid);
                // set pump selection tags to NO


                List<double?> PumpInuseTids = await _repository.GetPumpInuseTids(intLnupID);

                foreach (double? inuseTid in PumpInuseTids)                
                {
                    await _repository.SetWriteTagVal((int)YesNo.NO, "YES", inuseTid);
                                       
                }

                // set IN_USE_FLAG to NO for all stations used by the comp lineup
                //  there is not need to set station_in_used flag to NO.
                //       ABCdataEnv.cmdsetStationinuseFlg "NO", intLnupID
                // -------------------- Code below added for SRTF RW 22/10/2010 --------------------------'
                //  Get the stations used by this lineup

                List<BldrStationsData> BldrStationsDataList =  await _repository.GetBldrStationsData(intLnupID, intBldrID);
                foreach (BldrStationsData BldrStationsDataObj in BldrStationsDataList )
                { 
                    lngLineupSelTID = (BldrStationsDataObj.LineupSelTid == null) ? -1 : Convert.ToDouble(BldrStationsDataObj.LineupSelTid);
                    if ((lngLineupSelTID != -1))
                    {
                        await _repository.SetWriteTagVal((int)YesNo.NO, "YES", lngLineupSelTID);                       
                    }                  
                }                             
            } 

            lngLineupSelTID = -1;

             List<AbcBlenders> BldrLineupTags =  await _repository.GetBldrLineupTags(intBldrID);
            
            if (BldrLineupTags.Count() > 0)
            {
                lngLineupSelTID = (BldrLineupTags[0].LineupSelTid == null)?-1: Convert.ToDouble(BldrLineupTags[0].LineupSelTid);
            }
            
            if (lngLineupSelTID != -1)
            {
                await _repository.SetWriteTagVal((int)YesNo.NO, "YES", lngLineupSelTID);                
            }

            List<double?> AllPumpsForPrdgrp = await _repository.GetAllPumpsForPrdgrp(intBldrID);
            foreach (double? PumpsForPrdgrp in AllPumpsForPrdgrp)
            {
                lngPumpInUseTid = (PumpsForPrdgrp == null)?-1: Convert.ToDouble(PumpsForPrdgrp);
                if (lngPumpInUseTid != -1)
                {
                    await _repository.SetWriteTagVal((int)YesNo.NO, "YES", lngPumpInUseTid);                    
                }

            }

            // -------------------- End of code added RW 22/10/2010 --------------------------'
            //   call to set the stoptime fo the last interval
            await SetStopTimeInt(curblend.lngID);
            // generate blend report
            // temporary setting to prevent generation of blend report - Partha 7/24/00
            blnGenReport = false;
            if (blnGenReport)
            {
                //GenBlendRpt(curblend.lngID); -- entire code inside is commented in VB
            }

            return 0;
        }

        //Equipment validation at downloading
        private async Task<ValidInvalid> ChkBlendEquip(int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend)
        {
            
            double lngPrevBlendId;
            double lngMatId;
            double lngCurMatId;
            double lngCurTankId;
            double lngCurLineupId;
            double lngPrevMatId;
            double lngPrevTankId;
            double lngPrevLineupId;
            double lngPrdLnupSlctFbTid;
            // , lngToTankID As double
            int intCurrDestTankID;
            int intPrevDestTankID;
            string strPrevBldName = "";
            string strTankName;
            string strUsageName;
            DcsTag tagPrdLnupSlctFb = new DcsTag();
            var res = "";
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            ValidInvalid rtnData = ValidInvalid.invalid;

            List<AbcBlends> AbcBlendData = await _repository.GetAbcBlendData(vntBldrsData[intBldrIdx].Id, curblend.intProdID);
           
            if (AbcBlendData.Count() > 0)
            {
                lngPrevBlendId = AbcBlendData[0].Id;
                strPrevBldName = AbcBlendData[0].Name;
                // get all blender comps
               List<AbcBlenderComps> AllBldrComps = await _repository.GetAllBldrComps(vntBldrsData[intBldrIdx].Id);

                foreach (AbcBlenderComps BldrCompsObj in AllBldrComps)               
                {
                    lngMatId = BldrCompsObj.MatId;
                    // Get the comp Data for the downloading blend
                    List<CompTanksData> CompTanksData = await _repository.GetCompTanksData(curblend.lngID);

                    List<CompTanksData> CompTanksDataFlt = CompTanksData.Where<CompTanksData>(row => row.MatId == lngMatId).ToList();
                   
                    if (CompTanksDataFlt.Count() > 0)
                    {
                        lngCurMatId = CompTanksDataFlt[0].MatId;
                        lngCurTankId = CompTanksDataFlt[0].TankId;
                        lngCurLineupId = (CompTanksDataFlt[0].LineupId == null)?-1: Convert.ToDouble(CompTanksDataFlt[0].LineupId);
                    }
                    else
                    {
                        lngCurMatId = -1;
                        lngCurTankId = -1;
                        lngCurLineupId = -1;
                    }

                  
                    // get the Usage Name for the given blend Component
                    strUsageName = await GetBldMatUsage(curblend.lngID, lngMatId);
                    if ((strUsageName != "ADDITIVE"))
                    {
                        // Get the comp Data for the previous blend                        
                        CompTanksData = await _repository.GetCompTanksData(lngPrevBlendId);

                        CompTanksDataFlt = CompTanksData.Where<CompTanksData>(row => row.MatId == lngMatId).ToList();

                        if (CompTanksDataFlt.Count() > 0)
                        {
                            lngPrevMatId = CompTanksDataFlt[0].MatId;
                            lngPrevTankId = CompTanksDataFlt[0].TankId;
                            lngPrevLineupId = (CompTanksDataFlt[0].LineupId == null) ? -1 : Convert.ToDouble(CompTanksDataFlt[0].LineupId);
                        }
                        else
                        {
                            lngPrevMatId = -1;
                            lngPrevTankId = -1;
                            lngPrevLineupId = -1;
                        }
                        
                        if ((lngCurMatId != lngPrevMatId) || ((lngCurTankId != lngPrevTankId) || (lngCurLineupId != lngPrevLineupId)))
                        {
                            // Issue a msg: Blender on DCS is in PAUSED state. Equip in blend ^1 is not equal to Equip in previous Blend ^2.  Downloading Calceled.
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN87), programName, "BL-" + curblend.lngID, curblend.strName,
                                strPrevBldName,"", "", "", "", res);
                           
                            // Either reset the Blender in the DCS or duplicate a blend with similar equipment.
                            rtnData = ValidInvalid.invalid;
                            // Leave the event and cancel the downloading
                            // set ABC_BLENDS.PENDING_STATE to null and set blend_state to READY
                            await _repository.SetPendingState(null,curblend.lngID);
                            curblend.strState = "READY";
                            
                            return rtnData;
                        }
                    }
                }

                // If it finish the loop it is because all comp data matched all right
                // Check that the Selected Prod tank in the current blend is = to the To_tk_id of the
                // Previous blend
                // get dest tank ID of current blend
                intCurrDestTankID = Convert.ToInt32(await _repository.GetDestTankId(curblend.lngID));

                List<AbcBlenderDest> BldrDestSelTid =  await _repository.GetBldrDestSelTid(vntBldrsData[intBldrIdx].Id, intCurrDestTankID);
                lngPrdLnupSlctFbTid = (BldrDestSelTid[0].SelectionFbTid == null)?-1: Convert.ToDouble(BldrDestSelTid[0].SelectionFbTid);

                AbcTags DataRes = await _repository.GetTagNameAndVal(lngPrdLnupSlctFbTid);
                tagPrdLnupSlctFb.vntTagName = DataRes.Name;
                tagPrdLnupSlctFb.vntTagVal = DataRes.ReadValue.ToString();
                
                if (Convert.ToInt32(tagPrdLnupSlctFb.vntTagVal) == (int)OnOff.ON_)
                {
                    // The swing happened from the prev blend and it is Ok for downloading
                    rtnData = ValidInvalid.valid;                    
                }
                else
                {
                    strTankName = await _repository.GetTankName(intCurrDestTankID);
                    // warn msg "Dest tank ^1 requested by ABC not the same as used in DCS"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN65), programName, "BL-" + curblend.lngID, strTankName,
                               "", "", "", "", "", res);                    
                }

                //           ABCdataEnv.rscomBlendSwingsData.MoveNext
                //         Loop 'loop of swing records in the prev blend
                //         ABCdataEnv.rscomBlendSwingsData.Close
            }

            // No blends Matching on this blender on DONE or SEALED to compare with
            if ((rtnData == ValidInvalid.invalid))
            {
                // Issue a msg: Blender on DCS is in PAUSED state. Equip in blend ^1 is not equal to Equip in previous Blend ^2.  Downloading Calceled.
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN87), programName, "BL-" + curblend.lngID, curblend.strName,
                              strPrevBldName, "", "", "", "", res);
                // Leave the event and cancel the downloading
                // set ABC_BLENDS.PENDING_STATE to null and set blend_state to READY
                await _repository.SetPendingState(null,curblend.lngID);
                curblend.strState = "READY";
            }
            
            return rtnData;
        }
        public async Task<int> NullCmdAction(int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend, DebugLevels enumDebugLevel, bool blnSkipMonitor = false)
        {
            int intDestTankID, intNDestTks = 0, intTimeDiff;
            int intIntvNum = 0;
            double? lngProdLineupId, lngTransferLineId, lngDestTkId = 0;
            double lngFlushTankId = 0;
            double? dblDestVolume, dblPrdHeelVol;
            string strFlushSwgState;
            string strTkInUseFlag, strABCService, strDCSState;
            string strAnzName, strFlushTkFlag, strHeelUpdOccurredFlag;
            DcsTag tagTotVol = new DcsTag();
            DcsTag tagPermissive = new DcsTag();
            bool blnFlushing = false;
            RetStatus intSampleResult;
            var res = "";

            if(enumDebugLevel == DebugLevels.High)
            {
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, curblend.strName, "NULL_COMMAND_ACTION",
                    "", "", "", "", res);
            }

            if (gblnOptimizing[intBldrIdx] != true && ((curblend.vntPendSt == null) || curblend.vntPendSt != "SWINGING"))
            {
                //'set ABC_BLENDS.PENDING_STATE to null
                await _repository.SetPendingState(null, curblend.lngID);
            }

            if(blnSkipMonitor == false )
            {
                if (((gblnBmonStarted[intBldrIdx] == true) && 
                    ((curblend.strState.Trim() == "ACTIVE") || (curblend.strState.Trim() == "PAUSED"))))
                {
                    // Set swing tags write tags values to OFF
                    // Set the product swing cmds (blender.swing_tid) to OFF (Write Value)
                    //  - Aug, 03: Replace this update by the new created sub SetSwingTIDOFF
                    await SetSwingTIDOFF(Convert.ToInt32(vntBldrsData[intBldrIdx].PrdgrpId), vntBldrsData[intBldrIdx].SwingTid, Convert.ToInt32(vntBldrsData[intBldrIdx].Id));
                    gblnBmonStarted[intBldrIdx] = false;
                }

                //Get the Prod Tank when flushing is going on
                //'get dest tank ID
                intDestTankID = Convert.ToInt32(await _repository.GetDestTankId(curblend.lngID));

                if (curblend.strState.Trim() == "ACTIVE" || curblend.strState.Trim() == "LOADED" ||
                  (curblend.strState.Trim() == "PAUSED" && curblend.dteActualStart != cdteNull))
                {
                    // 'Get the abc_dest_tanks.flush_tk_flag to loop through all dest tanks for this blend
                    List<AbcBlendDest> GetDestTkFlagsData = await _repository.GetDestTkFlags(curblend.lngID);
                    List<AbcBlendDest> GetDestTkFlagsDataFiltered = new List<AbcBlendDest>();
                    if (GetDestTkFlagsData.Count() > 0)
                    {
                        blnFlushing = false;
                        //'Find if flush_tk_flag=YES for at least one of the records
                        GetDestTkFlagsDataFiltered = GetDestTkFlagsData.Where<AbcBlendDest>(row => row.FlushTkFlag == "YES").ToList<AbcBlendDest>();
                        if (GetDestTkFlagsDataFiltered.Count() > 0)
                        {
                            lngFlushTankId = GetDestTkFlagsDataFiltered[0].TankId;
                            blnFlushing = true;
                        }
                        intNDestTks = GetDestTkFlagsData.Count();
                    }

                    if(intNDestTks > 1)
                    {
                        //'get trasfer line vol from flush tank to destination tank
                        List<AbcBlendSwings> DataRes = await _repository.GetBldSwgTransferVol(curblend.lngID, lngFlushTankId, intDestTankID);
                        if (DataRes.Count() != 0)
                        {
                            strFlushSwgState = (DataRes[0].SwingState == null) ? "" : DataRes[0].SwingState;
                        }
                        else
                        {
                            strFlushSwgState = "";
                        }

                        if ((strFlushSwgState.Trim() == "READY" || strFlushSwgState.Trim() == "ACTIVE") && blnFlushing == true)
                        {

                            //check debug - GetDestTkFlagsDataFiltered or GetDestTkFlagsData
                            for (int i = 0; i < GetDestTkFlagsDataFiltered.Count(); i++)
                            {
                                lngDestTkId = GetDestTkFlagsDataFiltered[i].TankId;
                                strFlushTkFlag = GetDestTkFlagsDataFiltered[i].FlushTkFlag;
                                strTkInUseFlag = GetDestTkFlagsDataFiltered[i].InUseFlag;
                                if (strTkInUseFlag == "NO" && strFlushTkFlag == "YES")
                                {
                                    intDestTankID = Convert.ToInt32(lngDestTkId);
                                    //Warning!!! Review that break works as 'Exit Do' as it could be in a nested instruction like switch
                                    break;
                                }
                            }
                        }
                        else if(blnFlushing == true && (strFlushSwgState.Trim() == "COMPLETE" || strFlushSwgState.Trim() == "INCOMPLETE"))
                        {
                            if (gArPrevBldData[intBldrIdx].strState.Trim() == "PAUSED" && curblend.strState.Trim() == "ACTIVE")
                            {
                                lngProdLineupId = null; // NULL_
                                //check debug - GetDestTkFlagsDataFiltered or GetDestTkFlagsData
                                List<AbcBlendDest> DataResult = GetDestTkFlagsDataFiltered.Where<AbcBlendDest>(row =>row.InUseFlag == "YES").ToList<AbcBlendDest>();                                
                                if (DataResult.Count() > 0)
                                {
                                    lngProdLineupId = DataResult[0].LineupId;
                                    lngDestTkId = DataResult[0].TankId;
                                }
                                if(lngProdLineupId != null) //NULL_
                                {
                                    List<AbcBlends> GetBlendStateList = await _repository.GetBlendState(curblend.lngID);
                                    if(GetBlendStateList.Count() > 0)
                                    {
                                        strHeelUpdOccurredFlag = GetBlendStateList[0].HeelUpdOccurredFlag;
                                        if (strHeelUpdOccurredFlag == "NO")
                                        {
                                            List<DCSProdLineupNum> GetDCSProdLineupNumData = await _repository.GetDCSProdLineupNum(lngProdLineupId);
                                                
                                            dblDestVolume = (GetDCSProdLineupNumData[0].DestLineVolume == null )?0: Convert.ToDouble(GetDCSProdLineupNumData[0].DestLineVolume);
                                            
                                            dblPrdHeelVol = await _repository.GetHeelVol(lngDestTkId) + dblDestVolume;
                                            // set heel volume in dest tank
                                            await _repository.SetHeelVol(dblPrdHeelVol,curblend.lngID,lngDestTkId);
                                            // Update heel_updated_occurred_flag in abc_blends for the rest for the blend
                                            await _repository.SetHeelUpdated(curblend.lngID);
                                        }
                                    }
                                }

                            }
                        }
                    }
                }

                if (curblend.strState.Trim() == "ACTIVE" && gblnNOProcActBlds[intBldrIdx] == false)
                {
                    // Update ignore_line_constraints in abc_blends
                    if ((curblend.strIgnLineConstr == "YES"))
                    {
                        await _repository.SetIgnoreLineCOnstraint(curblend.lngID);                        
                    }

                    // If Blend state changes from LOADED to ACTIVE and the analyzers is
                    // in DCS service but not in ABC Service, then issue a warning msg
                    // also check for the anzr_start_delay (optimizer_delay) to update the ramping_act_flag
                    if ((((gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED")|| (gArPrevBldData[intBldrIdx].strState.Trim() == "PAUSED"))
                                && curblend.strState.Trim() == "ACTIVE"))
                    {
                        //if state just become active, then update bias_override_flag "NO"
                        if (gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED" && curblend.strBiasOverrideFlag == "YES")
                        {
                            // UPDATE abc_blends.bias_override_flag TO NO
                            await _repository.SetBiasOverrideFlag(curblend.lngID);                            
                            // Update ramping on flag from now on
                            curblend.strBiasOverrideFlag = "NO";
                        }

                        if (gArBldFinishTime[intBldrIdx] == cdteNull)
                        {
                            //BLEND ^1 CHANGED STATE FROM ^2 TO ^3                     
                            res = "";
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN93), programName, "BL-" + curblend.lngID, curblend.strName,
                                gArPrevBldData[intBldrIdx].strState, curblend.strState, "", "", "", res);

                        }

                        //           '***********
                        //'Set the anzr_start_delay (optimizer_delay) TO the current time
                        //'get current time
                        gDteCurTime = await _repository.GetCurTime();
                        //'Set the start timer for this blender
                        gArAnzDelay[intBldrIdx] = gDteCurTime;
                        //'***********

                        //'get the anz_id,abc_service, dcs_state_tag_id and read_value and its corresponding state
                        //'in abc_anz_states
                        List<HdrAnzrsData> GetHdrAnzrsData = await _repository.GetHdrAnzrsData(vntBldrsData[intBldrIdx].Id);
                        for (int i = 0; i < GetHdrAnzrsData.Count(); i++)
                        {
                            strAnzName = GetHdrAnzrsData[i].AnzName;
                            strDCSState = GetHdrAnzrsData[i].AnzState;
                            strABCService = GetHdrAnzrsData[i].AbcServiceFlag;

                            if (strDCSState.Trim() == "IN SERVICE" && strABCService.Trim() == "NO") {
                              //  'msg: Analyzer ^1 on blender ^2 is in DCS service but not in ABC Service.  Analyzer Properties values will not be used!                                
                                res = "";
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN88), programName, "BL-" + curblend.lngID, strAnzName,
                                    gstrBldrName, "", "", "", "", res);
                            }                            
                        }
                    }

                    if (gArAnzDelay[intBldrIdx] != cdteNull)
                    {
                        // Compare the anzr_start_delay (optimizer_delay) with the current time
                        // get current time
                        gDteCurTime = await _repository.GetCurTime();
                        // get the time diff between the curr time  and the anz timer for processing (in minutes)
                        intTimeDiff = Convert.ToInt32(DateAndTime.DateDiff("n", gArAnzDelay[intBldrIdx], gDteCurTime));
                        if (intTimeDiff <= vntBldrsData[intBldrIdx].AnzrStartDelay && curblend.strRampingActFlag == "NO")
                        {
                            // UPDATE abc_blends.ramping_act_flag TO YES
                            await _repository.SetRampingActFlag(curblend.lngID, "YES");
                            // Update ramping on flag from now on
                            curblend.strRampingActFlag = "YES";
                        }
                        else if (intTimeDiff > vntBldrsData[intBldrIdx].AnzrStartDelay && curblend.strRampingActFlag == "YES")
                        {
                            // UPDATE abc_blends.ramping_act_flag to NO
                            await _repository.SetRampingActFlag(curblend.lngID, "NO");
                            // Update ramping on flag from now on
                            curblend.strRampingActFlag = "NO";
                        }

                    }
                    else if ((curblend.strRampingActFlag == "YES"))
                    {
                        // UPDATE abc_blends.ramping_act_flag to NO
                        await _repository.SetRampingActFlag(curblend.lngID, "NO");
                        // Update ramping on flag from now on
                        curblend.strRampingActFlag = "NO";
                    }

                    //'Main sub for monitoring of ACTIVE blends
                    await MonitorBlend(intBldrIdx, vntBldrsData, curblend, intDestTankID, enumDebugLevel);
                }
                else if (curblend.strState.Trim() == "DONE" && gArPrevBldData[intBldrIdx].strState.Trim() != "DONE")
                {
                    gArBldFinishTime[intBldrIdx] = cdteNull;
                    //To pass to the function the whole curBlend array instead of single parameters
                    await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, intDestTankID, enumDebugLevel, true);
                }
                else if (curblend.strState.Trim() == "PAUSED")
                {
                    //Log a msg on change of state
                    if (curblend.strState.Trim() == "PAUSED" && (gArPrevBldData[intBldrIdx].strState.Trim() == "ACTIVE" || gArPrevBldData[intBldrIdx].strState.Trim() == "LOADED")) {
                        //'BLEND ^1 CHANGED STATE FROM ^2 TO ^3
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN93), programName, "BL-" + curblend.lngID, curblend.strName,
                               gArPrevBldData[intBldrIdx].strState, curblend.strState, "", "", "", res);                        
                    }

                    if (curblend.dteActualStart != cdteNull)
                    { //' Skip this calc if this blend has never started

                        //'Update the blend intervals if needed in Paused State
                        List<AbcBlendIntervals> BlendIntvs = await _repository.GetBlendIntvs(curblend.lngID);
                        if (BlendIntvs.Count() < 1)
                        {
                            gDteCurTime = await _repository.GetCurTime();

                            await _shared.CheckNewIntvRecs(curblend.lngID, 0, enumDebugLevel, gDteCurTime);
                            await _shared.CheckNewIntvRecs(curblend.lngID, 1, enumDebugLevel, gDteCurTime);
                            BlendIntvs = await _repository.GetBlendIntvs(curblend.lngID);
                        }
                        if (BlendIntvs[0].Starttime == null)
                        {
                            //'set start time and volume for interval #1
                            await _repository.SetNewIntv(0, curblend.lngID, intIntvNum);
                            //'save current interval #
                            curblend.intCurIntv = intIntvNum;
                        }
                        else
                        {
                            //ABCdataEnv.rscmdGetBlendIntvs.MoveLast
                            intIntvNum = Convert.ToInt32(BlendIntvs[BlendIntvs.Count() - 1].Sequence);
                            curblend.intCurIntv = intIntvNum;
                        }

                        if (vntBldrsData[intBldrIdx].TotalVolTid != null)
                        {
                            //'get the total blend integrated volume
                            AbcTags DataRes = await _repository.GetTagNameAndVal(vntBldrsData[intBldrIdx].TotalVolTid);
                            tagTotVol.vntTagName = DataRes.Name;
                            tagTotVol.vntTagVal = DataRes.ReadValue.ToString();
                        }
                        else
                        {
                            tagTotVol.vntTagName = null;
                            tagTotVol.vntTagVal = null;
                        }

                        //'Update the total volume in ABC
                        if(tagTotVol.vntTagVal != null)
                        {
                            if (Math.Abs((Convert.ToInt32(tagTotVol.vntTagVal) - gdblBldVol)) > 1)
                            {
                                // Check DCS->ABC communication
                                if (await _shared.ChkDcsComm(curblend.lngID, vntBldrsData[intBldrIdx].Id, gstrBldrName) == GoodBad.BAD)
                                {
                                    return 0;
                                }

                                //  *** added only write on change code  ***
                                if ((gProjDfs.strAllowRateVolUpds == "YES"))
                                {
                                    // get download OK tag (permissive tag) value from ABC_TAGS
                                    AbcTags DataRes = await _repository.GetTagNameAndVal(vntBldrsData[intBldrIdx].DownloadOkTid);
                                    tagPermissive.vntTagName = DataRes.Name;
                                    tagPermissive.vntTagVal = DataRes.ReadValue.ToString();
                                    
                                    if (((Convert.ToInt32(tagPermissive.vntTagVal) == null) ? (int)OnOff.OFF : Convert.ToInt32(tagPermissive.vntTagVal)) == (int)OnOff.ON_)
                                    {
                                        if ((curblend.sngTgtVol != gArPrevBldData[intBldrIdx].sngPrevBldTargVol))
                                        {
                                            // send new target vol to DCS
                                            await _repository.SetWriteTagVal(Convert.ToInt32(curblend.sngTgtVol),"YES",vntBldrsData[intBldrIdx].TargVolTid);
                                            // Save the previous Blend Target volume
                                            gArPrevBldData[intBldrIdx].sngPrevBldTargVol = curblend.sngTgtVol;
                                        }

                                        if ((curblend.sngTgtRate != gArPrevBldData[intBldrIdx].sngPrevBldTargRate))
                                        {
                                            // send new target rate to DCS
                                            await _repository.SetWriteTagVal(Convert.ToInt32(curblend.sngTgtRate), "YES", vntBldrsData[intBldrIdx].TargRateTid);
                                            
                                            // Save the previous Blend Target Rate
                                            gArPrevBldData[intBldrIdx].sngPrevBldTargRate = curblend.sngTgtRate;
                                        }
                                    }
                                }

                                // *****************************
                                //update also previuos  interval
                                gArPrevBldData[intBldrIdx].intCurIntv = curblend.intCurIntv;
                                // ************************************
                                // call PausedCalcBlendVol subroutine to update comp vols and Current Flow Rate
                                // PausedCalcBlendVol intBldrIdx, vntBldrsData, curblend, enumDebugLevel
                                await CalcBlend(intBldrIdx, vntBldrsData, curblend, enumDebugLevel);
                            }
                            else
                            {
                                // process samples if needed in PAUSED state
                                // update also current interval
                                gArPrevBldData[intBldrIdx].intCurIntv = curblend.intCurIntv;
                                if ((gProjDfs.strAllowSCSampling == "YES"))
                                {
                                    // Process samples if needed in ACTIVE or PAUSED states
                                    intSampleResult = await ProcessSamples(intBldrIdx, vntBldrsData, curblend, enumDebugLevel);
                                    //  Set tqi_now_flag to "YES" right after LINEPROP because of the sampling or regular LINEPROP
                                    // Jan. 03,03: Set the TQI_NOW_FLAG=YES
                                    if ((vntBldrsData[intBldrIdx].CalcpropFlag == "YES") && (intSampleResult == RetStatus.SUCCESS))
                                    {
                                        await _repository.SetTqi(curblend.lngID);
                                    }

                                }

                            }
                        }

                        //call Prod TANK_SWING function                        
                        await SwingProdTank(intBldrIdx, vntBldrsData, curblend, intDestTankID, enumDebugLevel);

                        //'Skip comp Tank swing if the pending state is not null and blend state is DONE
                        if (curblend.vntPendSt == null && curblend.strState.Trim() != "DONE") {
                            
                            await SwingCompTank(intBldrIdx, vntBldrsData, curblend, enumDebugLevel);         
                        }
                    } // 'Skip the previous calc if this blend has never started
                }
            }

            return 0;
        }
        
        private dynamic GetAbcBlenderParam(AbcBlenders obj, int number)
        {
            //id, prdgrp_id, total_flow_tid, 
            //rbc_state_tid, 
            //rbc_mode_tid, 
            //upper(local_global_flag) local_global_flag, 
            //blend_id_tid, 
            //product_tid, 
            //targ_vol_tid, 
            //rbc_vol_sp_fb_tid, 
            //total_vol_tid,
            // targ_rate_tid, 
            // start_tid, 
            // stop_tid, 
            // pause_tid, 
            // restart_tid, 
            // download_ok_tid, 
            // downloading_tid, 
            // rbc_wdog_tid, 
            // in_ser_flag, -19
            // blend_desc_tid, start_ok_tid, rundn_flag, swing_occurred_tid, 
            // swing_tid, comm_err_flag as comm_flag, on_spec_vol, download_type as download_type,
            // optimize_flag,calcprop_flag, swing_exist_tid, 
            // swing_vol_tid,anzr_start_delay, dcs_blname_fb_tid, grade_tid,
            //nvl(stop_opt_vol, 0) stop_opt_vol, ethanol_flag

            if(number == 0)
            {
                return obj.Id;
            } else if (number == 1)
            {
                return obj.PrdgrpId;
            }
            else if (number == 2)
            {
                return obj.TotalFlowId;
            }
            else if (number == 3)
            {
                return obj.RbcStateTid;
            }
            else if (number == 4)
            {
                return obj.RbcModeTid;
            }
            else if (number == 5)
            {
                return obj.LocalGlobalFlag;
            }
            else if (number == 6)
            {
                return obj.BlendIdTid;
            }
            else if (number == 7)
            {
                return obj.ProductTid;
            }
            else if (number == 8)
            {
                return obj.TargVolTid;
            }
            else if (number == 9)
            {
                return obj.RbcVolSpFbTid;
            }
            else if (number == 10)
            {
                return obj.TotalVolTid;
            }
            else if (number == 11)
            {
                return obj.TargRateTid;
            }
            else if (number == 12)
            {
                return obj.StartTid;
            }
            else if (number == 13)
            {
                return obj.StopTid;
            }
            else if (number == 14)
            {
                return obj.PauseTid;
            }
            else if (number == 15)
            {
                return obj.RestartTid;
            }
            else if (number == 16)
            {
                return obj.DownloadOkTid;
            }
            else if (number == 17)
            {
                return obj.DownloadingTid;
            }
            else if (number == 18)
            {
                return obj.RbcWdogTid;
            }
            else if (number == 19)
            {
                return obj.InSerFlag;
            }
            else if (number == 20)
            {
                return obj.BlendDescTid;
            }
            else if (number == 21)
            {
                return obj.StartOkTid;
            }
            else if (number == 22)
            {
                return obj.RundnFlag;
            }
            else if (number == 23)
            {
                return obj.SwingOccurredTid;
            }
            else if (number ==24)
            {
                return obj.SwingTid;
            }
            else if (number == 25)
            {
                return obj.CommErrFlag;
            }
            else if (number == 26)
            {
                return obj.OnSpecVol;
            }
            else if (number == 27)
            {
                return obj.DownloadType;
            }            
            else if (number == 28)
            {
                return obj.OptimizeFlag;
            }
            else if (number == 29)
            {
                return obj.CalcpropFlag;
            }
            else if (number == 30)
            {
                return obj.SwingExistTid;
            }
            else if (number == 31)
            {
                return obj.SwingVolTid;
            }
            else if (number == 32)
            {
                return obj.AnzrStartDelay;
            }
            else if (number == 33)
            {
                return obj.DcsBlnameFbTid;
            }
            else if (number == 34)
            {
                return obj.GradeTid;
            }
            else if (number == 35)
            {
                return (obj.StopOptVol == null)?0:Convert.ToDouble(obj.StopOptVol);
            }
            else if (number == 36)
            {
                return obj.EthanolFlag;
            }
            return "";            
        }
        // *********** ProcessBldCmd ***********
        private async Task<RetStatus> ProcessBldCmd(BlendCmds enumBldCmd, int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend, DebugLevels enumDebugLevel)
        {            
            DcsTag tagRbcMode = new DcsTag();
            int intDestTankID;
            double lngFlushTankId;
            string strFlushSwgState;
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            RetStatus rtnData = RetStatus.FAILURE;
            var res = "";
            if ((enumDebugLevel == DebugLevels.High) && (enumBldCmd != BlendCmds.DOWNLOAD))
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, curblend.strName, (gArBldCmds[(int)enumBldCmd] + "_BLEND"),
                   "", "", "", "", res);
            }
            
            if (enumDebugLevel >= DebugLevels.Medium)
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG5), programName, cstrDebug, curblend.strName, Convert.ToString(gArBldCmds[(int)gArPrevBldData[intBldrIdx].enumCmd]),
                 "", "", "", "", res);
            }
           
            if (gArPrevBldData[intBldrIdx].enumCmd == enumBldCmd)
            {
                if (curblend.strState.Trim() == Convert.ToString(gArBldStates[(int)enumBldCmd]))
                {
                    // Dec 12, 02: Set the downloading flag="NO"
                    await _repository.SetWriteTagVal((int)YesNo.NO,"YES",vntBldrsData[intBldrIdx].DownloadingTid);
                    // June 25,02: call NULL_COMMAND_ACTION function.  Process blend once a movement is in the expecting state
                    await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel);
                    gArPrevBldData[intBldrIdx].enumCmd = null;
                    gArPrevBldData[intBldrIdx].arCmdTime[(int)enumBldCmd] = cdteNull;
                }
                else
                {
                    if (enumDebugLevel == DebugLevels.High)
                    {
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG6), programName, cstrDebug, curblend.strName, Convert.ToString(gArPrevBldData[intBldrIdx].arCmdTime[(int)enumBldCmd]),
                gProjDfs.dblCmdTimeout.ToString(), "", "", "", res);                        
                    }
                   
                    if (DateAndTime.DateDiff("s", gArPrevBldData[intBldrIdx].arCmdTime[(int)enumBldCmd], DateTime.Now) > (60 * gProjDfs.dblCmdTimeout))
                    {
                        // warning msg "^1 command has timed out"
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN9), programName, "BL-" + curblend.lngID, gArBldCmds[(int)enumBldCmd].ToString(),
                       gProjDfs.dblCmdTimeout.ToString(),"", "", "", "", res);

                        // Dec 12, 02: Set the downloading flag="NO"
                        await _repository.SetWriteTagVal((int)YesNo.NO,"YES",vntBldrsData[intBldrIdx].DownloadingTid);
                        // call NULL_COMMAND_ACTION function
                        await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel);                        
                        gArPrevBldData[intBldrIdx].enumCmd = null;
                        gArPrevBldData[intBldrIdx].arCmdTime[(int)enumBldCmd] = cdteNull;
                    }
                }
            }
            else
            {
                // this is a new cmd
                if (vntBldrsData[intBldrIdx].RbcModeTid != null)
                {
                    // get RBC mode flag value from ABC_TAGS
                    AbcTags DataRes = await _repository.GetTagNameAndVal(vntBldrsData[intBldrIdx].RbcModeTid);
                    tagRbcMode.vntTagName = DataRes.Name;
                    tagRbcMode.vntTagVal = DataRes.ReadValue.ToString();                   
                }
                else
                {
                    tagRbcMode.vntTagName = null;
                    tagRbcMode.vntTagVal = null;
                }

                if (enumDebugLevel == DebugLevels.High)
                {
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG3), programName, cstrDebug, curblend.strState,
                       tagRbcMode.vntTagVal, "", "", "", "", res);
                }
               
                if (((tagRbcMode.vntTagVal == null)?(int)YesNo.NO: Convert.ToInt32(tagRbcMode.vntTagVal)) == (int)YesNo.NO)
                {
                    // warning msg "ABC -> DCS download not permitted"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN10), programName, "BL-" + curblend.lngID, tagRbcMode.vntTagName,
                      gstrBldrName, "", "", "", "", res);

                    // call NULL_COMMAND_ACTION function
                    await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);
                    
                }
                else
                {
                    if (enumDebugLevel == DebugLevels.High)
                    {
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG7), programName, cstrDebug, curblend.strState,
                       gProjDfs.strAllowStartStop.ToString(), "", "", "", "", res);
                    }
                   
                    if (((enumBldCmd != BlendCmds.DOWNLOAD)
                                && (gProjDfs.strAllowStartStop == "NO")))
                    {
                        // warning msg ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "DOWNLOAD",
                        gstrBldrName, "", "", "", "", res);
                        // call NULL_COMMAND_ACTION function
                        await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);                        
                    }
                    else if (await _shared.ChkDcsComm(curblend.lngID, vntBldrsData[intBldrIdx].Id, gstrBldrName) == GoodBad.GOOD)
                    {
                        
                        if ((GetAbcBlenderParam(vntBldrsData[intBldrIdx], ((int)enumBldCmd + (int)BldrsDataFieldIdices.START_TID))  == null ))  //(enumBldCmd + START_TID) - check the obj param
                        {
                            // warn msg "Cmd tag missing in table"
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN13), programName, "BL-" + curblend.lngID, Convert.ToString(gArBldCmds[(int)enumBldCmd]),
                        gstrBldrName, "", "", "", "", res);

                            await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);
                        }
                        else if (enumBldCmd != BlendCmds.DOWNLOAD)
                        {
                            // send command to DCS tag
                            double? data = (GetAbcBlenderParam(vntBldrsData[intBldrIdx], ((int)enumBldCmd + (int)BldrsDataFieldIdices.START_TID)));
                            await _repository.SetWriteTagVal((int)YesNo.YES,"YES",data);
                            if ((enumBldCmd == BlendCmds.PAUSE) || (enumBldCmd == BlendCmds.STOP_))
                            {
                                // If STOP/PAUSE cmds are issued then check if flusinh is done
                                lngFlushTankId = -1;
                                // Get the abc_dest_tanks.flush_tk_flag to loop through all dest tanks for this blend
                                List<AbcBlendDest> DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);
                                 List<AbcBlendDest> DestTkFlagsFlt = new List<AbcBlendDest>();
                                if (DestTkFlags.Count() > 0)
                                {
                                    // Find if flush_tk_flag=YES for at least one of the records
                                    DestTkFlagsFlt = DestTkFlags.Where<AbcBlendDest>(row => row.FlushTkFlag == "YES").ToList();
                                    
                                    if (DestTkFlagsFlt.Count() > 0)
                                    {
                                        lngFlushTankId = DestTkFlagsFlt[0].TankId;
                                    }
                                }
                                
                                if (lngFlushTankId != -1)
                                {

                                    // get destination tank
                                    intDestTankID = Convert.ToInt32(await _repository.GetDestTankId(curblend.lngID));
                                    // get trasfer line vol from flush tank to destination tank
                                    List<AbcBlendSwings> BldSwgTransferVol =  await _repository.GetBldSwgTransferVol(curblend.lngID,lngFlushTankId,intDestTankID);
                                    
                                    if (BldSwgTransferVol.Count() > 0)
                                    {
                                        strFlushSwgState = (BldSwgTransferVol[0].SwingState == null)? "": BldSwgTransferVol[0].SwingState;
                                    }
                                    else
                                    {
                                        strFlushSwgState = "";
                                    }
                                    
                                    // If flushing is not done yet. Swing state="READY"
                                    if ((strFlushSwgState.Trim() == "READY"))
                                    {
                                        // warning msg BLEND ^1 IN PAUSED/STOPPED STATE BEFORE LINEFILL WAS FLUSHED. TQI CALCULATIONS COULD BE AFFECTED
                                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN85), programName, "BL-" + curblend.lngID, curblend.strName,
                                        "", "", "", "", "", res);                                        
                                    }

                                }

                                // No flushing tank
                            }

                            // Cmd is not PAUSE or STOP
                            // update prev_blend_cmd and Cmd_time
                            // Download excluded.  See Downloading Function
                            gArPrevBldData[intBldrIdx].enumCmd = enumBldCmd;
                            gArPrevBldData[intBldrIdx].arCmdTime[(int)enumBldCmd] = DateTime.Now;
                        }                     
                        // signal to continue downloading process
                        rtnData = RetStatus.SUCCESS;
                    }
                }
            }

            // TODO: Exit Function: Warning!!! Need to return the value
            return rtnData;
        }
        private async Task<RetStatus> CheckNonEtohProps(List<BldProps> vntPropData, List<BldProps> vntPropDataFlt, double lngBlendId, string strBlendName)
        {

            //  Added RW 14-Oct-16 for Gasoline Ethanol blending
            //  If _ETOH property is included in blend, ensure non _ETOH property is also included (ie. must be in Product Specs)
            int intI;
            string strPropName;
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            RetStatus rtnData = RetStatus.SUCCESS;
            for (intI = 0; intI <= vntPropDataFlt.Count() -1 ; intI++)
            {
                //  vntPropData = rsPropData.rows
                if ((vntPropData[intI].Name.Substring((vntPropData[intI].Name.Length - 5)) == "_ETOH"))
                {
                    switch (vntPropData[intI].Name)
                    {
                        case "AKI_ETOH":
                            strPropName = "RDOI";
                            break;
                        case "BENZ_ETOH":
                            strPropName = "BENZENE";
                            break;
                        case "DI_ETOH":
                            strPropName = "DRVIDX";
                            break;
                        case "E70C_ETOH":
                        case "E100C_ETOH":
                        case "E150C_ETOH":
                        case "E180C_ETOH":
                        case "E200F_ETOH":
                        case "E300F_ETOH":
                            strPropName = ("E_V" + vntPropData[intI].Name.Substring(1, (vntPropData[intI].Name.Length - 6)));
                            // E_V70C, E_V100C, E_V200F etc
                            break;
                        case "VLI_ETOH":
                            strPropName = "VLI_UK";
                            break;
                        case "WATER_ETOH":
                            strPropName = "WATERSED";
                            break;
                        default:
                            strPropName = vntPropData[intI].Name.Substring(0, (vntPropData[intI].Name.Length - 5));
                            // eg. RON
                            break;
                    }
                    
                    if ((gProjDfs.strProjName == "PKN - POLAND"))
                    {
                        if ((vntPropData[intI].Name == "E150C_ETOH"))
                        {
                            strPropName = "E_V180C";
                        }
                        else if ((vntPropData[intI].Name == "MTBE_ETOH"))
                        {
                            strPropName = "E_V200F";
                        }
                        else if ((vntPropData[intI].Name == "ETBE_ETOH"))
                        {
                            strPropName = "E_V300F";
                        }
                    }

                    vntPropDataFlt = vntPropData.Where<BldProps>(row => row.Name == strPropName).ToList();                    
                    if (vntPropDataFlt.Count() == 0)
                    {
                        // Issue error msg 'BLEND ^1 PROP ^2 CANNOT BE CALCULATED AS PROP ^3 IS NOT IN PRODUCT SPECS. DOWNLOAD CANCELLED'
                        var res = "";
                              await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN106), programName, "BL-" + lngBlendId, strBlendName,
                                        vntPropData[intI].Name, strPropName, "DOWNLOAD CANCELLED", "", "", res);                       
                        rtnData = RetStatus.FAILURE;
                    }
                }
            }           
            return rtnData;
        }
        private async Task<RetStatus> CheckEthanol(List<AbcBlenders> vntBlenders, int intBldrIdx, CurBlendData actvblend, List<BldComps> vntCompData, List<BldProps> vntPropData)
        {
            // ((ADODB.Recordset)(rsPropData));

            // Added RW 14-Oct-16 for Gasoline Ethanol blending
            // Check whether blend has FGE component or component containing ETOH or whether tank heel contains ETOH
            int intI;
            int intDestTankID;
            //  Destination tank id
            // --- RW 20-Feb-17 Gasoline Ethanol blending remedial ---
            // Dim dblEtohHeelValue As Double      ' ETOH heel value
            double vntCompETOH = 0;
            //  Component ETOH (or ETOH_ETOH RW 20-Feb-17) property value
            string strFGECompName = "";
            //  Name of FGE component
            string strEtohCompName = "";
            //  Name of component w/ ETOH (or ETOH_ETOH RW 20-Feb-17) > MIN_ETOH
            bool blnNotFound = false;
            // --- RW 20-Feb-17 Gasoline Ethanol blending remedial ---
            bool blnNoETOH_ETOH = false;
            bool blnNoETOH = false;
            double sngEtohHeelValue;
            //  ETOH_ETOH heel value
            double? vntEtohHeelValue = 0;
            //  ETOH_ETOH heel value
            double vntCompETOHETOH = 0;
            //  Component ETOH_ETOH property value
            // --- RW 20-Feb-17 Gasoline Ethanol blending remedial ---
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            RetStatus rtnData = RetStatus.SUCCESS;
            List<BldProps> vntPropDataFlt = new List<BldProps>();
            var res = "";
            // Check ETOH_ETOH and ETOH properties have been configured
            if ((gintEtohEtohPropId == 0))
            {
                // Output warning msg 'BLEND ^1 PROPERTY ^2 NOT CONFIGURED, BYPASSING ETHANOL BLENDING'
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN111), programName, "BL-" + actvblend.lngID, actvblend.strName,
                                        "ETOH_ETOH", "", "", "", "", res);
            }

            if ((gintEtohPropId == 0))
            {
                // Output warning msg 'BLEND ^1 PROPERTY ^2 NOT CONFIGURED, BYPASSING ETHANOL BLENDING'
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN111), programName, "BL-" + actvblend.lngID, actvblend.strName,
                                       "ETOH", "", "", "", "", res);
            }

            if ((gintEtohEtohPropId == 0) || (gintEtohPropId == 0))
            {
                // TODO: Exit Function: Warning!!! Need to return the value
                return rtnData;
            }

            // Check whether blend has FGE component or component with ETOH_ETOH or ETOH > MIN_ETOH
            for (intI = 0; intI <= vntCompData.Count(); intI++)
            {
                //  Get component tank ETOH property value
                vntCompETOH = await _repository.GetSelTankProp(vntCompData[intI].TankId, gintEtohPropId);
                // If gstrLIMSSeparateProps = NO, ETOH value from LIMS will be stored in both ETOH & ETOH_ETOH properties
                if ((gstrLIMSSeparateProps == "YES"))
                {
                    // Get component ETOH_ETOH property value
                    vntCompETOHETOH = await _repository.GetSelTankProp(vntCompData[intI].TankId, gintEtohEtohPropId);
                    // Set vntCompETOH = largest of the two property values
                    // vntCompETOH = IIf(vntCompETOHETOH > vntCompETOH, vntCompETOHETOH, vntCompETOH)
                }

                // If Not IsNull(vntCompETOH) Then
                if (((vntCompETOH >= gProjDfs.sngFGEEtoh) || (vntCompETOHETOH >= gProjDfs.sngFGEEtoh)))
                {
                    // FGE component detected, save name
                    strFGECompName = vntCompData[intI].MatName;
                    //  FGE
                }
                else if (((vntCompETOH >= gProjDfs.sngMinEtoh)
                            || (vntCompETOHETOH >= gProjDfs.sngMinEtoh)))
                {
                    strEtohCompName = vntCompData[intI].MatName;
                    //  component w/ ETOH > MIN_ETOH
                }

                // End If
            }

            if (strFGECompName != "" || strEtohCompName != "")
            {
                // Either FGE component has been identified or component with ETOH or ETOH_ETOH >= MIN_ETOH
                // Check whether blend has ETOH_ETOH and ETOH properties (ie. will have the properties if Product Specs has the properties)
                //rsPropData.MoveFirst;
                vntPropDataFlt = vntPropData.Where<BldProps>(row => row.Name == "ETOH_ETOH").ToList();
                if (vntPropDataFlt.Count() == 0)
                {
                    blnNoETOH_ETOH = true;
                    if (strFGECompName != "")
                    {
                        // Output warning msg 'Blend ^1 has FGE Component but Product Spec has no ETOH_ETOH property, bypassing ethanol blending'
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN108), programName, "BL-" + actvblend.lngID, actvblend.strName,
                                      "ETOH_ETOH", "", "", "", "", res);
                    }

                    if ((strEtohCompName != ""))
                    {
                        if (vntCompETOH >= gProjDfs.sngMinEtoh)
                        {
                            // Output warning msg 'Blend ^1 has component with ETOH property >= MIN_ETOH but Product Spec has no ETOH_ETOH property, bypassing ethanol blendin
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN109), programName, "BL-" + actvblend.lngID, actvblend.strName,
                                     "COMPONENT", "ETOH", "ETOH_ETOH", "", "", res);
                        }
                        else if ((vntCompETOHETOH >= gProjDfs.sngMinEtoh))
                        {
                            // Output warning msg 'Blend ^1 has component with ETOH_ETOH property >= MIN_ETOH but Product Spec has no ETOH_ETOH property, bypassing ethanol blending'
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN109), programName, "BL-" + actvblend.lngID, actvblend.strName,
                                    "COMPONENT", "ETOH_ETOH", "ETOH_ETOH", "", "", res);
                        }

                    }

                }

                // Check whether blend has ETOH property (ie. will have ETOH property if Product specs has ETOH property)
                vntPropDataFlt = vntPropData.Where<BldProps>(row => row.Name == "ETOH").ToList();

                if (vntPropDataFlt.Count() == 0)
                {
                    blnNoETOH = true;
                    if ((strFGECompName != ""))
                    {
                        // Output warning msg 'Blend ^1 has FGE Component but Product Spec has no ETOH property, bypassing ethanol blending'
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN108), programName, "BL-" + actvblend.lngID, actvblend.strName,
                                      "ETOH", "", "", "", "", res);
                    }

                    if ((strEtohCompName != ""))
                    {
                        if ((vntCompETOH >= gProjDfs.sngMinEtoh))
                        {
                            // Output warning msg 'Blend ^1 has component with ETOH property >= MIN_ETOH but Product Spec has no ETOH property, bypassing ethanol blending'
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN109), programName, "BL-" + actvblend.lngID, actvblend.strName,
                                   "COMPONENT", "ETOH", "ETOH", "", "", res);
                        }
                        else if ((vntCompETOHETOH >= gProjDfs.sngMinEtoh))
                        {
                            // Output warning msg 'Blend ^1 has component with ETOH_ETOH property >= MIN_ETOH but Product Spec has no ETOH property, bypassing ethanol blending'
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN109), programName, "BL-" + actvblend.lngID, actvblend.strName,
                                  "COMPONENT", "ETOH_ETOH", "ETOH", "", "", res);
                        }
                    }
                }

                if (blnNoETOH_ETOH == true || blnNoETOH == true)
                {
                    return rtnData;
                }
            }
            else
            {
                //  No FGE component or component with ETOH >= MIN_ETOH

                // Check whether tank heel contains ETOH >= MIN_ETOH
                // Blend will not contain ETOH property if product spec does not contain ETOH property
                //  Get dest tank id
                intDestTankID = Convert.ToInt32(await _repository.GetDestTankId(actvblend.lngID));

                //  Get ETOH_ETOH of destination tank
                vntEtohHeelValue = await _repository.GetSelTankProp(intDestTankID, gintEtohEtohPropId);

                if (vntEtohHeelValue == null)
                {
                    sngEtohHeelValue = 0;
                }
                else
                {
                    sngEtohHeelValue = Convert.ToDouble(vntEtohHeelValue);
                }

                if ((sngEtohHeelValue < gProjDfs.sngMinEtoh))
                {
                    // Output warning msg 'Blend ^1 no components or tank heel have ETOH_ETOH >= MIN_ETOH, bypassing ethanol blending'
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN110), programName, "BL-" + actvblend.lngID, actvblend.strName,
                                 "", "", "", "", "", res);
                    // TODO: Exit Function: Warning!!! Need to return the value
                    return rtnData;
                    //  Optimization will still take place, but ethanol blending algorithms will not be used
                }
                else
                {
                    // Check whether blend has ETOH_ETOH and ETOH properties (ie. will have the properties if Product Specs has the properties)
                    vntPropDataFlt = vntPropData.Where<BldProps>(row => row.Name == "ETOH_ETOH").ToList();

                    if (vntPropDataFlt.Count() == 0)
                    {
                        blnNoETOH_ETOH = true;
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN109), programName, "BL-" + actvblend.lngID, actvblend.strName,
                                  "TANK HEEL", "ETOH_ETOH", "ETOH_ETOH", "", "", res);
                    }

                    // Check whether blend has ETOH property (ie. will have ETOH property if Product specs has ETOH property)
                    vntPropDataFlt = vntPropData.Where<BldProps>(row => row.Name == "ETOH").ToList();

                    if (vntPropDataFlt.Count() == 0)
                    {
                        blnNoETOH = true;
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN109), programName, "BL-" + actvblend.lngID, actvblend.strName,
                                 "TANK HEEL", "ETOH_ETOH", "ETOH", "", "", res);
                    }

                    if (blnNoETOH_ETOH == true || blnNoETOH == true)
                    {
                        // TODO: Exit Function: Warning!!! Need to return the value
                        return rtnData;
                    }
                }
            }
            // --- RW 20-Feb-17 Gasoline Ethanol blending remedial ---
            // If blend contains FGE component, ensure denaturant properties are configured
            // (ie. get list of FGE component properties excluding _etoh props and make sure denaturant has these properties configured)
            if ((strFGECompName != ""))
            {
                List<DenaturantProps> DenaturantProps = await _repository.GetDenaturantProps();
                List<DenaturantProps> DenaturantPropsFlt = new List<DenaturantProps>();
                // With...
                if (DenaturantProps.Count() > 0)
                {
                    for (intI = 0; intI <= vntPropData.Count(); intI++)
                    {
                        if ((vntPropData[intI].Name.Substring((vntPropData[intI].Name.Length - 4)) != "ETOH"))
                        {
                            DenaturantPropsFlt = DenaturantProps.Where<DenaturantProps>(row => row.Name == vntPropData[intI].Name).ToList();
                            if (DenaturantPropsFlt.Count() > 0)
                            {
                                blnNotFound = true;
                                break;
                            }
                        }
                    }
                }

                if ((blnNotFound == true))
                {
                    // Output warning msg 'Blend ^1 has FGE component but required denaturant properties are not configured, download cancelled'
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN107), programName, "BL-" + actvblend.lngID, actvblend.strName,
                             "DOWNLOAD CANCELLED", "", "", "", "", res);

                    return RetStatus.FAILURE;

                    //  Optimization will not be done
                }
            }

            // Check required non _ETOH properties are included in blend (ie. must be in Product Specs)
            if ((await CheckNonEtohProps(vntPropData, vntPropData, actvblend.lngID, actvblend.strName)) == RetStatus.FAILURE)
            {
                rtnData = RetStatus.FAILURE;
                //  No optimization will be done
            }
            else
            {
                gblnEthanolBlend = true;
            }

            // TODO: Exit Function: Warning!!! Need to return the value
            return rtnData;

        }        
        private async Task<InservOutserv> ChkTankServ(double lngBldID, double intTankID, string strTankName, double? vntDcsServTid, string strAbcServFlag, DebugLevels enumDebugLevel)
        {
            DcsTag tagDcsServ = new DcsTag();
            string strTankState ="";
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            InservOutserv rtnData = InservOutserv.OUT_SERV;
            //   To skip DCS in service if source_destn_type<>"TANK"
            if (vntDcsServTid != null)
            {
                // get DCS_service_tid tag value
                AbcTags DataRes = await _repository.GetStrTagNameAndVal(vntDcsServTid);
                tagDcsServ.vntTagName = DataRes.Name;
                tagDcsServ.vntTagVal = DataRes.ReadString;
                var res = "";

                if (tagDcsServ.vntTagVal != null)
                {
                    strTankState = "";
                    List<string> TankStName =  await _repository.GetTankStName(tagDcsServ.vntTagVal.Trim());
                    
                    if (TankStName.Count() > 0)
                    {
                        strTankState = TankStName[0];
                    }

                    if ((strTankState == "OUT OF SERV"))
                    {
                        // warning msg "Tank ^1 not in service in DCS"
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN34), programName, "BL-" + lngBldID, strTankName, "DCS",
                        gstrBldrName, "", "", "", res);
                        
                        // TODO: Exit Function: Warning!!! Need to return the value
                        return rtnData;
                    }
                    else if ((strTankState == ""))
                    {
                        // warn msg "Bad dcs_service_tid. Check in-service status of tank ^1 in DCS"
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN33), programName, "BL-" + lngBldID, tagDcsServ.vntTagName, strTankName,
                         "", "", "", "", res);
                    }

                }
                else
                {
                    // warn msg "Bad dcs_service_tid. Check in-service status of tank ^1 in DCS"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN33), programName, "BL-" + lngBldID, tagDcsServ.vntTagName, strTankName,
                        "", "", "", "", res);
                }

                if ((strAbcServFlag == "NO"))
                {
                    // warning msg "Tank ^1 not in service in ABC"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN34), programName, "BL-" + lngBldID, strTankName, "ABC",
                        gstrBldrName, "", "", "", res);                    
                    // TODO: Exit Function: Warning!!! Need to return the value
                    return rtnData;
                }

            }

            return InservOutserv.IN_SERV;        
        }
        private async Task<int> CalcHeelProps(double lngBlendId, int intTankID, string strBlendName, string strBldrName, int intPrdgrpID)
        {
            // 
            //  Calculate heel properties
            // 
            double lngPrevBldId;
            //  Previous blend to product tank
            int intPropId;
            //  Prop id
            int intCalcIdLV;
            //  LINEAR VOL calc id
            int intCalcIdLW;
            //  LINEAR WT calc id
            int intPropCalcId;
            //  Property calc id
            int intLabSrceId;
            //  LAB property source id
            int intCalcSrceId;
            //  CALC property source id
            double? dblEtohHeelValue;
            //  Heel ETOH_ETOH property value
            double dblCalcdValue = 0;
            //  Calculated value
            double dblEtohPropHeelValue;
            //  _ETOH property heel value
            double dblDensEtohHeelValue = 0;
            //  GRAV_ETOH property value
            double dblCalcdDensValue = 0;
            //  Calculated density value
            double dblEtohPropValue;
            //  ETOH property value
            double dblPropErr;
            //  Calculated property error
            double dblCorrectedValue = 0;
            //  Corrected property value
            double sngLabLimit;
            //  ETOH_PROPS_LAB_LIMIT from abc_proj_defaults
            string strEtohPropName;
            //  _ETOH property name
            string strXXXPropName;
            //  non _ETOH property name
            string strOrigQuery;
            //  original SQL command text
            string strCalcRtnName;
            //  Calc routine name
            string strPropValue;
            //  Property value as string
            string strPout;
            //  Return from comMessagePkg
            string strPropAlias;
            //  property alias
            string strDensityUOM ="";
            //  Density UOM
            bool blnNoUpdate = false;
            bool blnMETFound;
            bool blnNoTVL20Coeffs = false;
            DateTime? dteEndTime;
            //  Previous blend order end time
            List<double> vntCoeffs = new List<double>();

            List<NonLinTkPropValsModified> rsPrevNonLinNonEtohPropsCALC;
            //  Calculated non-linear non _ETOH properties for tank
            List<NonLinTkPropValsModified> rsPrevNonLinEtohPropsCALC;
            //  Calculated non-linear _ETOH properties for tank
            List<NonLinTkPropValsModified> rsPrevNonLinEtohPropsLAB;
            //  Non-linear _ETOH properties lab values for tank
            Dictionary<string, string> colPropNames = new Dictionary<string, string>();
            //  Non _ETOH property names
            Dictionary<string, string> colEtohPropNames = new Dictionary<string, string>();
            //  _ETOH property names
            Dictionary<string, double> colPropErrors = new Dictionary<string, double>();
            //  _ETOH property errors
            Dictionary<string, double> colCorrectedPropVals = new Dictionary<string, double>();
            //  Corrected non _ETOH properties
            var res = "";
            // hold error handler return flag
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            vntCoeffs = null;
            //  abc_blend_dest_props contains the set of properties that exist in abc_blend_props, with heel and current values set to the good and selected property values in the destination tank
            //  abc_blend_props contains the properties that are configured in product specs
            // Get ETOH_ETOH heel value
            dblEtohHeelValue = await _repository.GetDestHeelProp(lngBlendId, intTankID, gintEtohEtohPropId);
            //  will be set = 0 if null or prop not found

            if(dblEtohHeelValue >= gsngMinEtoh)
            {
                //'Get calc ids for LINEAR VOL & LINEAR WT
                intCalcIdLV = await _repository.GetCalcID("LINEAR VOL");
                intCalcIdLW = await _repository.GetCalcID("LINEAR WT");

                List<DestHeelVals> AllDestHeelVals =  await _repository.GetAllDestHeelValsModified(lngBlendId, intTankID);

                //'Calculate density first
                //Find GRAV_ETOH and calculate GRAV (or API_ETOH and calculate API)
                List<DestHeelVals> AllDestHeelValsFlt = AllDestHeelVals.Where<DestHeelVals>(row => row.Name == "GRAV_ETOH").ToList();
                if(AllDestHeelValsFlt.Count == 0)
                {
                    AllDestHeelValsFlt = AllDestHeelVals.Where<DestHeelVals>(row => row.Name == "API_ETOH").ToList();
                }

                if(AllDestHeelValsFlt.Count > 0)
                {
                    dblDensEtohHeelValue = AllDestHeelValsFlt[0].Value;
                    strDensityUOM = AllDestHeelValsFlt[0].UnitsName;
                    if ((strDensityUOM == "KGM3"))
                    {
                        // Calculate GRAV/API as = ((XXX_ETOH) - (793.9 * ETOH /100)) /  ((100 - ETOH)/100)   Values from D4184 Table X4.1 (ABB FDS)
                        dblCalcdDensValue = (dblDensEtohHeelValue - (793.9 * Convert.ToDouble(dblEtohHeelValue) / 100)) / ((100 - Convert.ToDouble(dblEtohHeelValue)) / 100);
                    }
                    else if ((strDensityUOM == "APIGRAV"))
                    {
                        // Calculate GRAV/API as = ((XXX_ETOH) - (46.73 * ETOH /100)) /  ((100 - ETOH)/100)
                        dblCalcdDensValue = (dblDensEtohHeelValue - (46.73* Convert.ToDouble(dblEtohHeelValue) / 100))/ ((100 - Convert.ToDouble(dblEtohHeelValue)) / 100);
                    }
                    else
                    {
                        //  units = SPECGRAV, KGLITER or GM/CC
                        // Calculate GRAV/API as = ((XXX_ETOH) - (0.7939 * ETOH /100)) /  ((100 - ETOH)/100)
                        dblCalcdDensValue = (dblDensEtohHeelValue - (0.7939 * Convert.ToDouble(dblEtohHeelValue) / 100)) / ((100 - Convert.ToDouble(dblEtohHeelValue)) / 100);
                    }
                }

                foreach (DestHeelVals DestHeelValsObj in AllDestHeelVals)
                {
                    intPropId = Convert.ToInt32(DestHeelValsObj.PropId);
                    strEtohPropName = DestHeelValsObj.Name;
                    dblEtohPropHeelValue = DestHeelValsObj.Value;
                    intPropCalcId = Convert.ToInt32(DestHeelValsObj.CalcId);
                    switch (strEtohPropName)
                    {
                        case "AKI_ETOH":
                            strXXXPropName = "RDOI";
                            break;
                        case "BENZ_ETOH":
                            strXXXPropName = "BENZENE";
                            break;
                        case "DI_ETOH":
                            strXXXPropName = "DRVIDX";
                            break;
                        case "E70C_ETOH":
                        case "E100C_ETOH":
                        case "E180C_ETOH":
                        case "E200F_ETOH":
                        case "E300F_ETOH":
                            strXXXPropName = (strEtohPropName.Substring(0, 1) + ("_V" + strEtohPropName.Substring(1, ((strEtohPropName.IndexOf("_", 0) + 1) - 2))));
                            break;
                        case "E150C_ETOH":
                            if ((gstrProjName == "PKN - POLAND"))
                            {
                                // For PKN ORLEN, E_V150C is alias of E_V180C
                                strXXXPropName = "E_V180C";
                            }
                            else
                            {
                                strXXXPropName = "E_V150C";
                            }

                            break;
                        case "ETBE_ETOH":
                            if ((gstrProjName == "PKN - POLAND"))
                            {
                                // For PKN ORLEN, ETBE is alias of E_V300F
                                strXXXPropName = "E_V300F";
                            }
                            else
                            {
                                strXXXPropName = "ETBE";
                            }

                            break;
                        case "MTBE_ETOH":
                            if ((gstrProjName == "PKN - POLAND"))
                            {
                                // For PKN ORLEN, MTBE is alias of E_V200F
                                strXXXPropName = "E_V200F";
                            }
                            else
                            {
                                strXXXPropName = "MTBE";
                            }

                            break;
                        case "WATER_ETOH":
                            strXXXPropName = "WATERSED";
                            break;
                        case "VLI_ETOH":
                            strXXXPropName = "VLI_UK";
                            break;
                        default:
                            strXXXPropName = strEtohPropName.Substring(0, (strEtohPropName.Length - 5));
                            //  eg. RON / RON_ETOH
                            break;
                    }

                    // Calculate xxx Properties where possible, eg AROM, BENZENE, OLEF, GRAV, SULF etc from xxx_ETOH and ETOH
                    // Use TQI calculated values from previous blend order to this Tank where can not directly calculate xxx Properties, eg RON MON AKI RVP E70 etc.
                    // 
                    if ((intPropCalcId == intCalcIdLV))
                    {
                        //  Linear vol eg. AROM_ETOH, BENZ_ETOH etc.
                        if ((strXXXPropName == "ETOH"))
                        {
                            dblCalcdValue = Convert.ToDouble(dblEtohHeelValue);
                        }
                        else if ((strXXXPropName != "GRAV"))
                        {
                            // eg AROM = AROM_ETOH * 100 / (100 - ETOH)
                            dblCalcdValue = (Convert.ToDouble(dblEtohPropHeelValue) * (100 / (100 - Convert.ToDouble(dblEtohHeelValue))));
                        }
                        else
                        {
                            dblCalcdValue = dblCalcdDensValue;
                            //  previously calc'd density value
                        }

                    }
                    else if ((intPropCalcId == intCalcIdLW))
                    {
                        //  Linear wt ie. SULF_ETOH, OXYG_ETOH
                        if ((strXXXPropName == "OXYG"))
                        {
                            // Values from D4184 Table X4.1 (ABB FDS)
                            // = ((GRAV_ETOH * OXYG_ETOH) - (793.9 * 0.3473 * ETOH)) / (GRAV * (100 - ETOH)/100)
                            if (strDensityUOM == "KGM3")
                            {
                                dblCalcdValue = (((dblDensEtohHeelValue * dblEtohPropHeelValue) - (793.9 * (0.3473 * Convert.ToDouble(dblEtohHeelValue))))
                                            / (dblCalcdDensValue
                                            * ((100 - Convert.ToDouble(dblEtohHeelValue))
                                            / 100)));
                            }
                            else if ((strDensityUOM == "APIGRAV"))
                            {
                                dblCalcdValue = (((dblDensEtohHeelValue * dblEtohPropHeelValue) - (46.73 * (0.3473 * Convert.ToDouble(dblEtohHeelValue))))
                                            / (dblCalcdDensValue
                                            * ((100 - Convert.ToDouble(dblEtohHeelValue))
                                            / 100)));
                            }
                            else
                            {
                                //  units = SPECGRAV, KGLITER or GM/CC
                                dblCalcdValue = (((dblDensEtohHeelValue * dblEtohPropHeelValue) - (0.7939 * (0.3473 * Convert.ToDouble(dblEtohHeelValue))))
                                            / (dblCalcdDensValue
                                            * ((100 - Convert.ToDouble(dblEtohHeelValue))
                                            / 100)));
                            }

                        }
                        else
                        {
                            //  SULF and any other properties using wt% or ppmwt
                            // = GRAV_ETOH * SULF_ETOH / ( GRAV_ETOH - (ETOH*793.9/100))
                            if ((strDensityUOM == "KGM3"))
                            {
                                dblCalcdValue = ((dblDensEtohHeelValue * dblEtohPropHeelValue)
                                            / (dblDensEtohHeelValue
                                            - (Convert.ToDouble(dblEtohHeelValue) * (793.9 / 100))));
                            }
                            else if ((strDensityUOM == "APIGRAV"))
                            {
                                dblCalcdValue = ((dblDensEtohHeelValue * dblEtohPropHeelValue)
                                            / (dblDensEtohHeelValue
                                            - (Convert.ToDouble(dblEtohHeelValue) * (46.73 / 100))));
                            }
                            else
                            {
                                //  units = SPECGRAV, KGLITER or GM/CC
                                dblCalcdValue = ((dblDensEtohHeelValue * dblEtohPropHeelValue)
                                            / (dblDensEtohHeelValue
                                            - (Convert.ToDouble(dblEtohHeelValue) * (0.7939 / 100))));
                            }
                        }
                    }
                    else
                    {
                        // store (non linear) non _ETOH prop name with _ETOH prop name as key
                        colPropNames.Add(strXXXPropName,strEtohPropName);
                        // store (non-linear) ETOH prop name with non_ETOH prop name as key
                        colEtohPropNames.Add(strEtohPropName,strXXXPropName);
                        blnNoUpdate = true;
                    }

                    if ((blnNoUpdate == false))
                    {
                        dblCalcdValue = Math.Round(dblCalcdValue, 5);
                        // Store calculated value at heel_value and current_value
                        await _repository.SetAbcBlendDestPropData(dblCalcdValue, dblCalcdValue, lngBlendId, intTankID, strXXXPropName);
                    }
                    else
                    {
                        blnNoUpdate = false;
                    }
                }

                //'Use TQI calculated values from previous blend order to this Tank where cannot directly calculate xxx properties, eg RON MON AKI RVP E70 etc.
                //'Identify previous blend order routed to this tank with ETOH >= MIN_ETOH
                lngPrevBldId = await _repository.GetPrevBldToTk(intTankID);
                
                if(lngPrevBldId != 0)
                {
                    // Get previous blend ETOH_ETOH property value
                    dblEtohPropValue = await _repository.GetBldPropCurVal(lngPrevBldId,intTankID,gintEtohEtohPropId);
                   
                    if(dblEtohPropValue >= gsngMinEtoh)
                    {
                        // Get etoh_props_lab_limit from Proj Defaults
                        sngLabLimit = await _repository.GetETOHLabLimit();

                        // Get blend end time

                        dteEndTime = await _repository.GetBlendEndTime(lngPrevBldId);

                        // Identify non-linear xxx calculated property values for the tank for previous blend order
                        // RON MON AKI RVP E70 E100 E150 T10 T30 T50 T70 T90 IBP FBP etc.
                        // 
                        intCalcSrceId = (int)await _repository.GetSourceId("CALC");

                        rsPrevNonLinNonEtohPropsCALC  = await _repository.GetNonLinTkPropValsModified(lngPrevBldId,intTankID,intCalcSrceId);
                                                
                        // Identify non-linear xxx_ETOH calculated property values for the tank for previous blend order
                        rsPrevNonLinEtohPropsCALC = await _repository.GetNonLinTkPropValsModified2(lngPrevBldId, intTankID, intCalcSrceId);

                        // Identify non-linear xxx_ETOH Lab property values for the tank for previous blend order
                        intLabSrceId = (int)await _repository.GetSourceId("LAB");
                        rsPrevNonLinEtohPropsLAB = await _repository.GetNonLinTkPropValsModified2(lngPrevBldId, intTankID, intLabSrceId);
                                               
                        // --- RW 20-Jan-17 Gasoline Ethanol blending remedial ---
                        // Line below added
                        if(rsPrevNonLinEtohPropsCALC.Count() > 0 && rsPrevNonLinEtohPropsLAB.Count() > 0)
                        {
                            // Calculate xxx_ETOH property error as = xxx_ETOH Lab value - xxx_ETOH Calc value for that blend order.
                            // Can be +ve or -ve.  Limit ABS corrections to same value as Header Analysers > Model Error Threshold, or Properties > Properties > Max Bias and Min Bias.
                            // Warning Message "Abs Correction higher than limit??"
                            // 
                            // Get model err threshold values
                            List<AbcAnzHdrProps> ModelErrThrshVals = await _repository.GetModelErrThrshVals(strBldrName);
                            // Get min/max bias values
                            List<AbcPrdgrpProps> MinMaxBiasVals =  await _repository.GetMinMaxBiasVals(intPrdgrpID);
                            List<NonLinTkPropValsModified> rsPrevNonLinEtohPropsLABFlt = new List<NonLinTkPropValsModified>();
                            foreach (NonLinTkPropValsModified rsPrevNonLinEtohPropsCALCObj in rsPrevNonLinEtohPropsCALC)
                            {
                                rsPrevNonLinEtohPropsLABFlt = rsPrevNonLinEtohPropsLAB.Where<NonLinTkPropValsModified>(row => row.Name == rsPrevNonLinEtohPropsCALCObj.Name).ToList();
                                if (rsPrevNonLinEtohPropsLABFlt.Count() > 0)
                                {
                                    //'Check lab value is dated after blend end time + lab limit
                                    // ---------validate
                                    if (Convert.ToDateTime(rsPrevNonLinEtohPropsLABFlt[0].ValueTime) >= (Convert.ToDateTime(dteEndTime).AddHours(sngLabLimit / 24)))
                                    {
                                        //  'xxx_ETOH property error = xxx_ETOH Lab value - xxx_ETOH CalcError
                                        dblPropErr = Convert.ToDouble(rsPrevNonLinEtohPropsLABFlt[0].Value) - Convert.ToDouble(rsPrevNonLinEtohPropsCALCObj.Value);
                                    }
                                    else
                                    {
                                        dblPropErr = 0;
                                    }
                                }
                                else
                                {
                                    dblPropErr = 0;
                                }

                                blnMETFound = false;

                                List<AbcAnzHdrProps> ModelErrThrshValsFlt = new List<AbcAnzHdrProps>();
                                // Find model error threshold for property
                                if (ModelErrThrshVals.Count() > 0)
                                {
                                    ModelErrThrshValsFlt = ModelErrThrshVals.Where<AbcAnzHdrProps>(row => row.PropId == rsPrevNonLinEtohPropsCALCObj.Id).ToList();
                                    
                                    if (ModelErrThrshValsFlt.Count() > 0)
                                    {
                                        if (ModelErrThrshValsFlt[0].ModelErrThrsh != null)
                                        {
                                            blnMETFound = true;
                                            if (Math.Abs(dblPropErr) > Convert.ToDouble(ModelErrThrshValsFlt[0].ModelErrThrsh))
                                            {
                                                // Output warning msg 'BLEND ^1, PROP ^2: ABS CORRECTION ^3 EXCEEDS MODEL ERROR THRESHOLD ^4, CORRECTION CLAMPED TO ^5'
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN112), programName, "BL-" + lngBlendId, strBlendName, rsPrevNonLinEtohPropsCALCObj.Alias,
                                                Math.Abs(dblPropErr).ToString(), ModelErrThrshValsFlt[0].ModelErrThrsh.ToString(), ModelErrThrshValsFlt[0].ModelErrThrsh.ToString(), "", res);
                                               
                                                dblPropErr = Convert.ToDouble(ModelErrThrshValsFlt[0].ModelErrThrsh);
                                            }
                                        }
                                    }                                    
                                }

                                List<AbcPrdgrpProps> MinMaxBiasValsFlt = new List<AbcPrdgrpProps>();
                                // If property has no model error threshold configured, find its min/max bias
                                if (blnMETFound == false)
                                {
                                    if (MinMaxBiasVals.Count() > 0 )
                                    {
                                        MinMaxBiasValsFlt = MinMaxBiasVals.Where<AbcPrdgrpProps>(row => row.PropId == rsPrevNonLinEtohPropsCALCObj.Id).ToList();
                                        
                                        if (MinMaxBiasValsFlt.Count() > 0)
                                        {
                                            if (MinMaxBiasValsFlt[0].MinBias != null)
                                            {
                                                if (dblPropErr < Convert.ToDouble(MinMaxBiasValsFlt[0].MinBias))
                                                {
                                                    // Output warning msg 'BLEND ^1 PROP ^2: CORRECTION ^3 ^4 BIAS ^5, CORRECTION CLAMPED TO ^6'
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN113), programName, "BL-" + lngBlendId, strBlendName, rsPrevNonLinEtohPropsCALCObj.Alias,
                                                    (dblPropErr).ToString(),"< MIN" , MinMaxBiasVals[0].MinBias.ToString(), MinMaxBiasVals[0].MinBias.ToString(), res);
                                                   
                                                    dblPropErr = Convert.ToDouble(MinMaxBiasVals[0].MinBias);
                                                }

                                            }

                                            if (MinMaxBiasValsFlt[0].MaxBias != null)
                                            {
                                                if (dblPropErr > Convert.ToDouble(MinMaxBiasValsFlt[0].MaxBias))
                                                {
                                                    // Output warning msg 'BLEND ^1 PROP ^2: CORRECTION ^3 ^4 BIAS ^5, CORRECTION CLAMPED TO ^6'
                                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN113), programName, "BL-" + lngBlendId, strBlendName, rsPrevNonLinEtohPropsCALCObj.Alias,
                                                    (dblPropErr).ToString(), "> MAX", MinMaxBiasVals[0].MaxBias.ToString(), MinMaxBiasVals[0].MaxBias.ToString(), res);

                                                    dblPropErr = Convert.ToDouble(MinMaxBiasVals[0].MaxBias);
                                                }
                                            }                                            
                                        }

                                    }

                                }

                                //'store property error with non-linear _etoh property name as key
                                colPropErrors.Add(rsPrevNonLinEtohPropsCALCObj.Name, dblPropErr);
                                //rsPrevNonLinEtohPropsCALC.MoveNext
                            }

                            //'For each non-linear xxx calculated value eg.RON MON etc
                            foreach (NonLinTkPropValsModified rsPrevNonLinNonEtohPropsCALCObj in rsPrevNonLinNonEtohPropsCALC)
                            {
                                if (rsPrevNonLinNonEtohPropsCALCObj.Name != "DRVIDX" && rsPrevNonLinNonEtohPropsCALCObj.Name != "TVL20" 
                                    && rsPrevNonLinNonEtohPropsCALCObj.Name != "VABP" && rsPrevNonLinNonEtohPropsCALCObj.Name != "VLI_UK"
                                    && ((rsPrevNonLinNonEtohPropsCALCObj.Name != "E_V200F" || rsPrevNonLinNonEtohPropsCALCObj.Name != "E_V300F")
                                    && gstrProjName == "PKN - POLAND"))
                                {
                                    // --- RW 02-Mar-17 Gasoline Ethanol blending remedial ---
                                    // Handle case where prev blend order may be different grade and may have properties (specified in prod specs) that aren't in current blend
                                    try
                                    {
                                        dblCorrectedValue = Convert.ToDouble(rsPrevNonLinNonEtohPropsCALCObj.Value) + colPropErrors[colEtohPropNames[rsPrevNonLinNonEtohPropsCALCObj.Name]];
                                        // Store corrected value of properties E_P10, E_P30, E_P50, E_P70, E_P90, RVP, E_V70C,
                                        // for later calculation of DRVIDX, TVL20, VABP, VLI_UK if necessary
                                        switch (rsPrevNonLinNonEtohPropsCALCObj.Name)
                                        {
                                            case "E_P10":
                                            case "E_P30":
                                            case "E_P50":
                                            case "E_P70":
                                            case "E_P90":
                                            case "RVP":
                                            case "E_V70C":
                                                colCorrectedPropVals.Add(rsPrevNonLinNonEtohPropsCALCObj.Name, dblCorrectedValue);                                                
                                                break;
                                        }
                                        await _repository.SetAbcBlendDestPropData(dblCorrectedValue, dblCorrectedValue, lngBlendId, intTankID, rsPrevNonLinNonEtohPropsCALCObj.Name);                                        
                                    }
                                    catch (Exception ex)
                                    {

                                    }                                   
                                }
                            }

                            //' Calcs below are dependent on corrected values calculated above

                            if(colCorrectedPropVals.Count > 0)
                            {
                                 //'Check if DRVIDX is property of blend
                                strCalcRtnName = await _repository.GetCalcRoutine(lngBlendId, "DRVIDX");
                                if (strCalcRtnName == "DRIVABILITY")
                                {   // 'Is property of blend & calc routine = DRIVABILITY
                                    dblCorrectedValue = 1.5 * colCorrectedPropVals["E_P10"] + 3 * colCorrectedPropVals["E_P50"] + colCorrectedPropVals["E_P90"];
                                    try
                                    {

                                        dblCorrectedValue = Math.Round(dblCorrectedValue, 5);
                                        //'Update existing heel and current values with calculated value (based on corrected values)
                                        await _repository.SetAbcBlendDestPropData(dblCorrectedValue, dblCorrectedValue, lngBlendId, intTankID, "DRVIDX");

                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                }

                                strCalcRtnName = await _repository.GetCalcRoutine(lngBlendId, "TVL20");
                                if (strCalcRtnName == "TVL20")
                                {
                                    vntCoeffs.Add(114.6);
                                    vntCoeffs.Add(-4.1);
                                    vntCoeffs.Add(0.2);
                                    vntCoeffs.Add(0.17);

                                    dblCorrectedValue = vntCoeffs[0] - vntCoeffs[1] * colCorrectedPropVals["RVP"] + vntCoeffs[2] * colCorrectedPropVals["E_P10"] +
                                                        vntCoeffs[3] * colCorrectedPropVals["E_P50"];
                                } 
                                else if(strCalcRtnName == "TVL201")
                                {
                                    vntCoeffs = await _repository.GetCalcCoeffs(intPrdgrpID, "TVL20", "TVL201");
                                    if (vntCoeffs.Count() < 4) {

                                        blnNoTVL20Coeffs = true;
                                        //'Use default values
                                        vntCoeffs = new List<double>();
                                        vntCoeffs.Add(107.3372);
                                        vntCoeffs.Add(-3.65024);
                                        vntCoeffs.Add(0.417052);
                                        vntCoeffs.Add(0.132032);
                                        vntCoeffs.Add(-0.03274);
                                     }

                                    dblCorrectedValue = vntCoeffs[0] - vntCoeffs[1] * colCorrectedPropVals["RVP"] + vntCoeffs[2] * colCorrectedPropVals["E_P10"] +
                                                        vntCoeffs[3] * colCorrectedPropVals["E_P50"] + vntCoeffs[4] * colCorrectedPropVals["E_P90"];                                    
                                }

                                if(strCalcRtnName == "TVL20" || strCalcRtnName == "TVL201")
                                {
                                    dblCorrectedValue = Math.Round(dblCorrectedValue, 5);
                                    // 'Update existing heel and current values with calculated value (based on corrected values)
                                    await _repository.SetAbcBlendDestPropData(dblCorrectedValue, dblCorrectedValue, lngBlendId, intTankID, "TVL20");

                                    //'If default coefficients were used
                                    if (blnNoTVL20Coeffs == true)
                                    {
                                        //' Get property alias
                                        strPropAlias = "TVL20";//await _repository.GetPropAlias("TVL20");                                    

                                        //'Output warning msg ^1 COEFFICIENTS FOR PROP ^2 IN BLEND ^3 NOT FOUND, TAKEN AS DEFAULT
                                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN114), programName, "BL-" + lngBlendId, strCalcRtnName, strPropAlias,
                                        strBlendName, "", "", "", res);
                                    }
                                }

                                // Check if E_P10 is property of current blend
                                strCalcRtnName = await _repository.GetCalcRoutine(lngBlendId,"E_P10");
                                if (strCalcRtnName != null && strCalcRtnName != "NULL")
                                {
                                    // E_P10 is property of blend
                                    // Check if E_P50 is property of current blend
                                    strCalcRtnName = await _repository.GetCalcRoutine(lngBlendId,"E_P50");
                                    if (strCalcRtnName != null && strCalcRtnName != "NULL")
                                    {
                                        // E_P50 is property of blend
                                        // Check if E_P90 is property of current blend
                                        strCalcRtnName = await _repository.GetCalcRoutine(lngBlendId, "E_P90");

                                        if (strCalcRtnName != null && strCalcRtnName != "NULL")
                                        {
                                            // E_P90 is property of blend
                                            blnNoUpdate = true;
                                        }
                                    }
                                    else
                                    {
                                        blnNoUpdate = true;
                                    }
                                }
                                else
                                {
                                    blnNoUpdate = true;
                                }

                                if(blnNoUpdate == false)
                                {
                                    // 'Check if VABP is property of current blend and get calc routine
                                    strCalcRtnName = await _repository.GetCalcRoutine(lngBlendId, "VABP");
                                    
                                    if(strCalcRtnName == "VABPASTM" || strCalcRtnName == "VABPTEX") //'Is property of blend
                                    {
                                        switch (strCalcRtnName)
                                        {
                                            case "VABPASTM":
                                                strCalcRtnName = await _repository.GetCalcRoutine(lngBlendId, "E_P30");

                                                if (strCalcRtnName != null && strCalcRtnName != "NULL")
                                                {
                                                    //  E_P30 is not property of blend
                                                    //  Use average of E_P10 & E_P50
                                                    colCorrectedPropVals.Add("E_P30",((colCorrectedPropVals["E_P10"] + colCorrectedPropVals["E_P50"]) / 2));
                                                    
                                                }

                                                // Check if E_P70 is property of blend and get calc routine
                                                strCalcRtnName = await _repository.GetCalcRoutine(lngBlendId, "E_P70");

                                                if (strCalcRtnName != null && strCalcRtnName != "NULL")
                                                {
                                                    //  E_P70 is not property of blend
                                                    //  Use average of E_P50 & E_P90
                                                    colCorrectedPropVals.Add("E_P70", ((colCorrectedPropVals["E_P50"] + colCorrectedPropVals["E_P90"]) / 2));                                                    
                                                }

                                                dblCorrectedValue = (colCorrectedPropVals["E_P10"] + colCorrectedPropVals["E_P30"] + colCorrectedPropVals["E_P50"]
                                                            + colCorrectedPropVals["E_P70"] + colCorrectedPropVals["E_P90"]) / 5;
                                                break;
                                            case "VABPTEX":
                                                dblCorrectedValue = (colCorrectedPropVals["E_P10"] + colCorrectedPropVals["E_P50"] + colCorrectedPropVals["E_P50"]
                                                            + colCorrectedPropVals["E_P90"]) / 4;                                                  
                                                break;
                                        }

                                        dblCorrectedValue = Math.Round(dblCorrectedValue, 5);
                                        //'Update existing heel and current values with calculated value (based on corrected values)
                                        await _repository.SetAbcBlendDestPropData(dblCorrectedValue, dblCorrectedValue, lngBlendId, intTankID, "VABP");
                                        
                                    }
                                }
                                else
                                {
                                    blnNoUpdate = false;
                                }

                                // Check if VLI_UK is property of current blend and get calc routine
                                strCalcRtnName = await _repository.GetCalcRoutine(lngBlendId, "VLI_UK");
                                
                                if (strCalcRtnName == "VLI")
                                {
                                    // Is property of blend & calc routine = VLI
                                    // --- RW 02-Mar-17 Gasoline Ethanol blending remedial ---
                                    // 'Check if E_V70C is property of blend and get calc routine
                                    // ABCdataEnv.cmdGetCalcRoutine lngBlendId, "E_V70C", strCalcRtnName
                                    // Check if RVP is property of blend and get calc routine
                                    strCalcRtnName = await _repository.GetCalcRoutine(lngBlendId, "RVP");
                                    
                                    // --- RW 02-Mar-17 Gasoline Ethanol blending remedial ---
                                    if ((strCalcRtnName == "RVPINDEX"))
                                    {
                                        // TODO: On Error Resume Next Warning!!!: The statement is not translatable 
                                        try
                                        {
                                            dblCorrectedValue = Math.Round((10 * colCorrectedPropVals["RVP"] + 7 * colCorrectedPropVals["E_V70C"]), 5);

                                            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
                                            // Update existing heel and current values with calculated value (based on corrected values)
                                            await _repository.SetAbcBlendDestPropData(dblCorrectedValue, dblCorrectedValue, lngBlendId, intTankID, "VLI_UK");                                            
                                        }
                                        catch(Exception ex)
                                        {

                                        }
                                    }
                                }
                            }//If colCorrectedPropVals.Count > 0
                        }



                    }
                }
            }
            else
            {
                List<DestHeelVals> AllDestHeelVals = await _repository.GetAllDestHeelValsModified2(lngBlendId, intTankID);
                foreach (DestHeelVals DestHeelValsObj in AllDestHeelVals)
                {
                    strXXXPropName = DestHeelValsObj.Name;
                    if (DestHeelValsObj.Value != null)
                    {
                        strPropValue = DestHeelValsObj.Value.ToString();
                    }
                    else
                    {
                        strPropValue = "0";
                    }

                    switch (strXXXPropName)
                    {
                        case "BENZENE":
                            strEtohPropName = "BENZ_ETOH";
                            break;
                        case "DRVIDX":
                            strEtohPropName = "DI_ETOH";
                            break;
                        case "E_V70C":
                        case "E_V100C":
                        case "E_V150C":
                            strEtohPropName = (strXXXPropName.Substring(0, 1) + (strXXXPropName.Substring(3, (strXXXPropName.Length - 3)) + "_ETOH"));
                            break;
                        case "E_V180C":
                            if ((gstrProjName == "PKN - POLAND"))
                            {
                                // For PKN ORLEN, E_V150C is alias of E_V180C
                                strEtohPropName = "E150C_ETOH";
                            }
                            else
                            {
                                strEtohPropName = (strXXXPropName.Substring(0, 1) + (strXXXPropName.Substring(3, (strXXXPropName.Length - 3)) + "_ETOH"));
                            }
                            break;
                        case "E_V200F":
                            if ((gstrProjName == "PKN - POLAND"))
                            {
                                // For PKN ORLEN, MTBE is alias of E_V200F
                                strEtohPropName = "MTBE_ETOH";
                            }
                            else
                            {
                                strEtohPropName = (strXXXPropName.Substring(0, 1) + (strXXXPropName.Substring(3, (strXXXPropName.Length - 3)) + "_ETOH"));
                            }
                            break;
                        case "E_V300F":
                            if ((gstrProjName == "PKN - POLAND"))
                            {
                                // For PKN ORLEN, ETBE is alias of E_V300F
                                strEtohPropName = "ETBE_ETOH";
                            }
                            else
                            {
                                strEtohPropName = (strXXXPropName.Substring(0, 1) + (strXXXPropName.Substring(3, (strXXXPropName.Length - 3)) + "_ETOH"));
                            }
                            break;
                        case "RDOI":
                            strEtohPropName = "AKI_ETOH";
                            break;
                        case "WATERSED":
                            strEtohPropName = "WATER_ETOH";
                            break;
                        case "VLI_UK":
                            strEtohPropName = "VLI_ETOH";
                            break;
                        default:
                            strEtohPropName = (strXXXPropName + "_ETOH");
                            break;
                    }

                    await _repository.SetAbcBlendDestPropData(Convert.ToDouble(strPropValue), Convert.ToDouble(strPropValue), lngBlendId, intTankID, strEtohPropName);
                }
            }
            return 0;
        }
        private async Task<ValidInvalid> ChkBlendData(CurBlendData curblend, List<CompTanksData> vntSrcTksData, List<DcsTag> arSrcTksVolTags, DestTankData DestTank,int intBldrIdx,
                         List<AbcBlenders> vntBldrsData, DebugLevels enumDebugLevel)
        {
            object vntStationName;
            double? vntAvlVolTid = 0;
            double? vntMaxVolTid = 0;
            double? vntMinVolTid = 0;

            string[] arReadEnabled = new string[0];
            string[] arScanEnabled = new string[0];
            string[] arScanGrpName = new string[0];
            string strExecute;
            double? vntDcsServTid = 0;
            string strAbcServFlag = "";
            List<AbcPrograms> vntPrgCycleTimes;
            DcsTag tagRundnRate = new DcsTag();
            double dblAvailVol = 0;
            double dblReqvol;
            double sngMaxPrdVol;
            double vntPropID;
            int intNprops;
            int intPropId;
            int intI;
            string vntDummy = "";
            double dblBldTimeRem;
            object vntPumpInUseTIDs;
            string strSrceDestType;
            string strFlushTkFlag;
            string strTkFixHeelFlag;
            string strTkInUseFlag;
            string strTkEndLineFillFlag;
            double sngTkHeelVol;
            double lngDestTkId;
            double lngTkLineupId;
            double lngTransferLineId;
            double lngInUseTankId;
            double lngFlushTankId;
            double sngTransferLineVol = 0;
            string vntInuseFlg = "";
            double? vntPropVal = 0;
            string vntCtrld;
            double vntAbsMin;
            double vntAbsMax;
            string vntPropAlias;

            ValidInvalid rtnData = ValidInvalid.invalid;
            var res = "";
            if (enumDebugLevel == DebugLevels.High)
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, curblend.strName, "CHECK_BLEND_DATA",
                    "", "", "", "", res);
            }

            if(gProjDfs.vntMinIntvLen == null)
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN26), programName, cstrGen, "MINIMUM", "1",
                    "", "", "", "", res);
                gProjDfs.vntMinIntvLen = 1;
            }

            if (gProjDfs.vntMaxIntvLen == null)
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN26), programName, cstrGen, "MAXIMUM", "60",
                    "", "", "", "", res);
                gProjDfs.vntMaxIntvLen = 60;
            }

            if ((curblend.vntIntvLen < gProjDfs.vntMinIntvLen))
            {
                // warning msg "Interval length < minimum default"
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN27), programName, "BL-" + curblend.lngID, curblend.vntIntvLen.ToString(), curblend.strName,
                     gProjDfs.vntMinIntvLen.ToString(), "", "", "", res);
                curblend.vntIntvLen = gProjDfs.vntMinIntvLen;
            }
            else if ((curblend.vntIntvLen > gProjDfs.vntMaxIntvLen))
            {
                // warning msg "Interval length > maximum default"
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN28), programName, "BL-" + curblend.lngID, curblend.vntIntvLen.ToString(), curblend.strName,
                    gProjDfs.vntMaxIntvLen.ToString(), "", "", "", res);                
                curblend.vntIntvLen = gProjDfs.vntMaxIntvLen;
            }

            if (enumDebugLevel == DebugLevels.High)
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG27), programName, cstrDebug, curblend.strName,
                    curblend.intCurIntv.ToString(), gProjDfs.vntMinIntvLen.ToString(), gProjDfs.vntMaxIntvLen.ToString(), "", "", res);
            }

            // check blend target rate
            if (curblend.vntMinRate != null)
            {
                if (curblend.sngTgtRate < curblend.vntMinRate)
                {
                    // warning msg "Target rate < minimum rate"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN29), programName, "BL-" + curblend.lngID, curblend.sngTgtRate.ToString(), curblend.strName,
                   curblend.vntMinRate.ToString(), "", "", "", res);
                }
            }

            if (curblend.vntMaxRate != null)
            {
                if ((curblend.sngTgtRate > curblend.vntMaxRate))
                {
                    // warning msg "Target rate > maximum rate"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN30), programName, "BL-" + curblend.lngID, curblend.sngTgtRate.ToString(), curblend.strName,
                  curblend.vntMaxRate.ToString(), "", "", "", res);                    
                }
            }

            if (enumDebugLevel == DebugLevels.High)
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG28), programName, cstrDebug, curblend.strName,
                    curblend.sngTgtRate.ToString(), curblend.vntMinRate.ToString(), curblend.vntMaxRate.ToString(), "", "", res);
            }

            // check blend target volume
            if (curblend.vntMinVol != null)
            {
                if (curblend.sngTgtVol < curblend.vntMinVol)
                {
                    // warning msg "Target rate < minimum rate"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN29), programName, "BL-" + curblend.lngID, curblend.sngTgtVol.ToString(), curblend.strName,
                   curblend.vntMinVol.ToString(), "", "", "", res);
                }
            }

            if (curblend.vntMaxVol != null)
            {
                if ((curblend.sngTgtVol > curblend.vntMaxVol))
                {
                    // warning msg "Target rate > maximum rate"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN30), programName, "BL-" + curblend.lngID, curblend.sngTgtVol.ToString(), curblend.strName,
                  curblend.vntMaxVol.ToString(), "", "", "", res);
                }
            }

            if (enumDebugLevel == DebugLevels.High)
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG28), programName, cstrDebug, curblend.strName,
                    curblend.sngTgtVol.ToString(), curblend.vntMinVol.ToString(), curblend.vntMaxVol.ToString(), "", "", res);
            }

            await _repository.SetAbcBlendCompData(curblend.lngID);

            // calculate blend time remaining
            dblBldTimeRem = (((double)curblend.sngTgtVol - (double)curblend.sngCurVol) / (double)curblend.sngTgtRate);
            // check if all comp tanks are in service in both DCS and ABC
            // check if each comp tank is already in use by another blender
            // check if each comp tank has enough comp volume for the blend
            vntSrcTksData =  await _repository.GetCompTanksData(curblend.lngID);                        
           
            arSrcTksVolTags = new List<DcsTag>();//Information.UBound(vntSrcTksData, 2)

            for (intI = 0; intI < vntSrcTksData.Count; intI++)
            {
               
                if (await ChkTankServ(curblend.lngID, vntSrcTksData[intI].TankId, vntSrcTksData[intI].TankName,
                vntSrcTksData[intI].DcsServiceTid, vntSrcTksData[intI].AbcServiceFlag, enumDebugLevel) == InservOutserv.OUT_SERV)
                {
                    return rtnData;
                }

                //check if source tank is already in use on other blenders

                /* not usung as inside code is commented
                 * ABCdataEnv.cmdGetSrcInUseFlag curblend.lngID, vntSrcTksData(TANK_ID, intI)
                  Set ABCdataEnv.rscmdGetSrcInUseFlag.ActiveConnection = Nothing
                  If ABCdataEnv.rscmdGetSrcInUseFlag.RecordCount > 0 Then
                     'Krish: inconsistent with use by Forms
                     'warning msg "Comp tank ^1 already in use on other blend(s)"
            '         ABCdataEnv.cmdLogMessage WARN35, App.Title, "BL-" & Format(curBlend.lngID, _
            '            cstrIDFmt), vntSrcTksData(TANK_NAME, intI), "", "", "", "", "", gStrRetOK
                  End If
                  ABCdataEnv.rscmdGetSrcInUseFlag.Close
                 */

                // check in_use_flag for stations
                // ERIK ** Skip if lineup id is null *****
                if (vntSrcTksData[intI].LineupId != null)
                {
                    AbcStations StationInuseFlgs = await _repository.GetStationInuseFlgs(vntSrcTksData[intI].LineupId);

                    vntStationName = StationInuseFlgs.Name;
                    vntInuseFlg = StationInuseFlgs.InUseFlag;

                    /*
                    do
                        {
                            if ((vntInuseFlg.Value == "YES"))
                            {
                                // warn msg "Station ^1 already in use"
                                //  Krish :For future use
                                //             ABCdataEnv.cmdLogMessage WARN36, App.Title, "BL-" & Format(curBlend.lngID, _
                                //                cstrIDFmt), vntStationName.Value, "", "", "", "", "", gStrRetOK
                            }

                            ABCdataEnv.rscmdGetStationInuseFlgs.MoveNext;
                        } while (ABCdataEnv.rscmdGetStationInuseFlgs.EOF);

                        ABCdataEnv.rscmdGetStationInuseFlgs.Close;
                    */
                }

                AbcTags DataRes = await _repository.GetTagNameAndVal(vntSrcTksData[intI].AvailVolId);
                DcsTag data = new DcsTag();
                data.vntTagName = DataRes.Name;
                data.vntTagVal = DataRes.ReadValue.ToString();
                arSrcTksVolTags.Add(data);

                DataRes = await _repository.GetTagNameAndVal(vntSrcTksData[intI].MinVolTid);
                data = new DcsTag();
                data.vntTagName = DataRes.Name;
                data.vntTagVal = DataRes.ReadValue.ToString();
                arSrcTksVolTags.Add(data);

                DataRes = await _repository.GetTagNameAndVal(vntSrcTksData[intI].RundnId);
                data = new DcsTag();
                data.vntTagName = DataRes.Name;
                data.vntTagVal = DataRes.ReadValue.ToString();
                arSrcTksVolTags.Add(data);

                // To skip the checking of avail vol if source_destn_type<>"TANK"
                if ((vntSrcTksData[intI].SourceDestnType == "TANK"))
                {
                    if (arSrcTksVolTags[intI].vntTagVal != null)
                    {
                        if (tagRundnRate.vntTagVal != null)
                        {
                            dblAvailVol = (Convert.ToInt32(arSrcTksVolTags[intI].vntTagVal) + (Convert.ToInt32(tagRundnRate.vntTagVal) * dblBldTimeRem));
                        }
                        else
                        {
                            dblAvailVol = Convert.ToDouble(arSrcTksVolTags[intI].vntTagVal);
                        }
                    }
                    else
                    {
                        // warn msg "Bad or null avail_vol_tid tag ^1 for comp tank ^2"
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN67), programName, "BL-" + curblend.lngID, arSrcTksVolTags[intI].vntTagName,
                        vntSrcTksData[intI].TankName, vntSrcTksData[intI].CompName, "", "", "", res);                        
                    }

                }

                if (vntSrcTksData[intI].CurRecipe == null)
                {
                    // warn msg "Null cur recipe for comp ^1"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN37), programName, "BL-" + curblend.lngID, vntSrcTksData[intI].CompName,
                        "", "", "", "", "", res);                    
                    // TODO: Exit Function: Warning!!! Need to return the value
                    return rtnData;
                }

                dblReqvol = (Convert.ToDouble(curblend.sngTgtVol) * (Convert.ToDouble(vntSrcTksData[intI].CurRecipe) / 100));
                // July 30, 2002: To skip the checking of req. vol > avail vol if source_destn_type <> "TANK"
                if (((vntSrcTksData[intI].SourceDestnType == "TANK") && (arSrcTksVolTags[intI].vntTagVal != null)))
                {
                    if ((dblReqvol > dblAvailVol))
                    {
                        // warning msg REQUIRED VOL ^1 > AVAIL VOL ^2 FOR ^3(SOURCE/COMP) TANK ^4
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN38), programName, "BL-" + curblend.lngID, dblReqvol.ToString(),
                        dblAvailVol.ToString(), "COMP", vntSrcTksData[intI].TankName, "", "", res);                        
                    }

                }

                if (enumDebugLevel >= DebugLevels.Medium)
                {
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG33), programName, cstrDebug, curblend.strName,
                        vntSrcTksData[intI].CurRecipe.ToString(), dblReqvol.ToString(), vntSrcTksData[intI].CompName, curblend.sngTgtVol.ToString(), "", res);
                }
            }

            // check if dest tank is in service in both DCS and ABC
            DestTank.strFixHeelFlg =  await _repository.GetDestTankData(curblend.lngID,DestTank.intID,DestTank.vntHeelVol);
            // If there is a flush tank then change the dest tank in use for the
            // tank with abc_dest_tanks.flush_tk_flag="YES"
            // If the in_use_flag=Flush_tk_flag then no change is needed
            List<AbcBlendDest> DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);
            List<AbcBlendDest> DestTkFlagsFlt = new List<AbcBlendDest>();
            if (DestTkFlags.Count > 0)
            {
                lngInUseTankId = -1;
                lngFlushTankId = -1;
                sngTransferLineVol = 0;
                // Find if flush_tk_flag=YES for at least one of the records
                DestTkFlagsFlt = DestTkFlags.Where<AbcBlendDest>(row => row.FlushTkFlag == "YES").ToList();                
                if (DestTkFlagsFlt.Count() >  0)
                {
                    lngFlushTankId = DestTkFlagsFlt[0].TankId;
                }

                // Find the in_use_flag=YES record
                DestTkFlagsFlt = DestTkFlags.Where<AbcBlendDest>(row => row.InUseFlag == "YES").ToList();
                if (DestTkFlagsFlt.Count() > 0)
                {
                    lngInUseTankId = DestTkFlagsFlt[0].TankId;
                }

                if (lngInUseTankId != -1 && lngFlushTankId != -1)
                {

                    // get trasfer line vol from flush tank to destination tank
                    List<AbcBlendSwings> BldSwgTransferVol = await _repository.GetBldSwgTransferVol(curblend.lngID, lngFlushTankId, lngInUseTankId);
                    
                    if (BldSwgTransferVol.Count > 0)
                    {
                        sngTransferLineVol = (BldSwgTransferVol[0].CriteriaNumLmt == null)? 0 : Convert.ToDouble(BldSwgTransferVol[0].CriteriaNumLmt);
                    }
                    else
                    {
                        sngTransferLineVol = 0;
                    }
                    
                }
            }

            foreach (AbcBlendDest DestTkFlagsObj in DestTkFlags)
            {
                lngDestTkId = DestTkFlagsObj.TankId;
                strFlushTkFlag = DestTkFlagsObj.FlushTkFlag;
                strTkInUseFlag = DestTkFlagsObj.InUseFlag;
                sngTkHeelVol = (DestTkFlagsObj.HeelVolume == null)?-1:Convert.ToDouble(DestTkFlagsObj.HeelVolume);
                strTkFixHeelFlag = DestTkFlagsObj.FixHeelFlag;
                if (((strTkInUseFlag == "NO") && (strFlushTkFlag == "YES")))
                {
                    DestTank.intID = Convert.ToInt32(lngDestTkId);
                    DestTank.vntHeelVol = sngTkHeelVol;
                    DestTank.strFixHeelFlg = strTkFixHeelFlag;
                }

                (DestTank.strName, vntAvlVolTid, vntMaxVolTid, vntMinVolTid, vntDcsServTid, strAbcServFlag) = await _repository.GetTankData(DestTank.intID);
                
                if (await ChkTankServ(curblend.lngID, DestTank.intID, DestTank.strName, vntDcsServTid, strAbcServFlag, enumDebugLevel) == InservOutserv.OUT_SERV)
                {
                    // TODO: Exit Function: Warning!!! Need to return the value
                    return rtnData;
                }

                // To skip the checking of dest tank data if source_destn_type <> "TANK"
                List<AbcTanks> DataTankID = await _repository.GetDataTankID(DestTank.intID);
                
                strSrceDestType = DataTankID[0].SourceDestnType;
                
                if (strSrceDestType == "TANK")
                {
                    // check heel volume in dest tank
                    double value1 = 0;
                    DateTime value2;
                    (DestTank.tagMinVol.vntTagName, value1, value2, vntDummy, arReadEnabled[0], arScanEnabled[0], arScanGrpName[0]) = 
                        await _repository.GetTagValAndFlags(vntMinVolTid, DestTank.tagMinVol.vntTagName, Convert.ToDouble(DestTank.tagMinVol.vntTagVal), Convert.ToDateTime(vntDummy), vntDummy,
                        arReadEnabled[0], arScanEnabled[0], arScanGrpName[0]);
                    DestTank.tagMinVol.vntTagVal = value1.ToString();
                    vntDummy = value2.ToString();
                                         
                    (DestTank.tagMinVol.vntTagName, value1, value2, vntDummy, arReadEnabled[0], arScanEnabled[0], arScanGrpName[0]) =
                        await _repository.GetTagValAndFlags(vntAvlVolTid, DestTank.tagAvlVol.vntTagName, Convert.ToDouble(DestTank.tagAvlVol.vntTagVal), Convert.ToDateTime(vntDummy), vntDummy,
                        arReadEnabled[1], arScanEnabled[1], arScanGrpName[1]);
                    DestTank.tagMinVol.vntTagVal = value1.ToString();
                    vntDummy = value2.ToString();

                    (DestTank.tagAvlVol.vntTagName, value1, value2, vntDummy, arReadEnabled[1], arScanEnabled[1], arScanGrpName[1]) =
                       await _repository.GetTagValAndFlags(vntAvlVolTid, DestTank.tagAvlVol.vntTagName, Convert.ToDouble(DestTank.tagAvlVol.vntTagVal), Convert.ToDateTime(vntDummy), vntDummy,
                       arReadEnabled[1], arScanEnabled[1], arScanGrpName[1]);
                    DestTank.tagAvlVol.vntTagVal = value1.ToString();
                    vntDummy = value2.ToString();

                    (DestTank.tagMaxVol.vntTagName, value1, value2, vntDummy, arReadEnabled[2], arScanEnabled[2], arScanGrpName[2]) =
                      await _repository.GetTagValAndFlags(vntMaxVolTid, DestTank.tagMaxVol.vntTagName, Convert.ToDouble(DestTank.tagMaxVol.vntTagVal), Convert.ToDateTime(vntDummy), vntDummy,
                      arReadEnabled[2], arScanEnabled[2], arScanGrpName[2]);
                    DestTank.tagMaxVol.vntTagVal = value1.ToString();
                    vntDummy = value2.ToString();

                    if (DestTank.tagMinVol.vntTagVal != null)
                    {
                        if (DestTank.tagMaxVol.vntTagVal == null)
                        {
                            //         warn msg "Max vol tag bad or not existing"
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN40), programName, "BL-" + curblend.lngID, "MAX_VOL_TID",
                            DestTank.tagMaxVol.vntTagName, DestTank.strName, "", "", "", res);
                        }
                        else
                        {
                            //  Checking for reading
                            if (arReadEnabled[2] == "NO")
                            {
                                //  Warning msg "MAX vol tag reading disabled"
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN41), programName, "BL-" + curblend.lngID, "MAX_VOL_TID",
                                    DestTank.tagMaxVol.vntTagName,"", "", "", "", res);
                            }

                            if (arScanEnabled[2] == "NO")
                            {
                                // warning msg "Max Vol tag's scan group disabled"
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN42), programName, "BL-" + curblend.lngID, arScanGrpName[2], "MAX_VOL_TID",
                                   DestTank.tagMaxVol.vntTagName, "", "", "", res);                               
                            }

                        }

                        if ((arReadEnabled[0] == "NO"))
                        {
                            //  Warning msg "Min vol tag reading disabled"
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN41), programName, "BL-" + curblend.lngID, "MIN_VOL_TID",
                                   DestTank.tagMinVol.vntTagName, "","", "", "", res);                          
                        }

                        if (arScanEnabled[0] == "NO")
                        {
                            // warning msg "Min Vol tag's scan group disabled"
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN42), programName, "BL-" + curblend.lngID, arScanGrpName[2], "MIN_VOL_TID",
                                  DestTank.tagMinVol.vntTagName, "", "", "", res);
                        }

                        if (DestTank.tagAvlVol.vntTagVal != null)
                        {
                            if (arReadEnabled[1] == "NO")
                            {
                                // warning msg "Avl vol tag reading disabled"
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN41), programName, "BL-" + curblend.lngID, "AVAIL_VOL_TID",
                                  DestTank.tagAvlVol.vntTagName, "","", "", "", res);                               
                            }

                            if (arScanEnabled[1] == "NO")
                            {
                                //  Warning msg "Avl vol tag's scan group disabled"
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN42), programName, "BL-" + curblend.lngID, arScanGrpName[1], "AVAIL_VOL_TID",
                                 DestTank.tagAvlVol.vntTagName, "", "", "", res);
                                
                            }

                            DestTank.vntHeelVol = Convert.ToDouble(DestTank.tagMinVol.vntTagVal) + Convert.ToDouble(DestTank.tagAvlVol.vntTagVal);
                            if (enumDebugLevel >= DebugLevels.Medium)
                            {
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG13), programName, cstrDebug, curblend.strName, DestTank.strName,
                                DestTank.vntHeelVol.ToString(),DestTank.tagAvlVol.vntTagVal,DestTank.tagMinVol.vntTagVal,"",res);
                            }
                        }
                        else
                        {
                            if (DestTank.vntHeelVol == -1)
                            {
                                DestTank.vntHeelVol = Convert.ToDouble(DestTank.tagMinVol.vntTagVal);
                            }

                            // warn msg "Bad Avail vol tag, heel vol set is ^1"
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN43), programName, "BL-" + curblend.lngID, arScanGrpName[1], "AVAIL",
                                DestTank.tagAvlVol.vntTagName, DestTank.strName, DestTank.vntHeelVol.ToString(), "", res);
                        }
                    }
                    else
                    {
                        if (DestTank.vntHeelVol > Convert.ToDouble(DestTank.tagMaxVol.vntTagVal)) {
                            // 'warning msg "HEEL VOL ^1 FOR DEST TANK ^2 OUTSIDE VALID LIMITS OF TANK MIN VOL ^3 AND MAX VOL ^4. DOWNLOADING ON ^5(MANAGER/BLENDER) ^6 CANCELED
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN39), programName, "BL-" + curblend.lngID, DestTank.vntHeelVol.ToString(), DestTank.strName,
                                DestTank.tagMinVol.vntTagVal, DestTank.tagMaxVol.vntTagVal, "BLENDER", gstrBldrName, res);

                            return rtnData;
                        }

                        if (DestTank.vntHeelVol == null) {
                            DestTank.vntHeelVol = 0;
                        }

                        // 'warn msg "Bad min vol tag, heel vol set as ^1"
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN43), programName, "BL-" + curblend.lngID, "MIN", DestTank.tagMinVol.vntTagName,
                                 DestTank.strName, DestTank.vntHeelVol.ToString(), "", "", res);
                    }
                }

                //check dest tank properties
               List<DestProps> DestPropsList = await _repository.GetDestProps(curblend.lngID,DestTank.intID);
                
                intNprops = DestPropsList.Count();
                if (intNprops == 0)
                {
                    // warn msg "No good props found for dest tank"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN68), programName, "BL-" + curblend.lngID, DestTank.strName,
                        gstrBldrName, "", "", "", "", res);
                   
                    // TODO: Exit Function: Warning!!! Need to return the value
                    return rtnData;
                }
                
                vntPropID = DestPropsList[0].PropId;
                vntPropVal = DestPropsList[0].Value;
                vntCtrld = DestPropsList[0].Controlled;
                vntAbsMin = DestPropsList[0].AbsMin;
                vntAbsMax = DestPropsList[0].AbsMax;
                vntPropAlias = DestPropsList[0].Alias;

                for (intI = 0; intI < intNprops; intI++)
                {
                    if (vntPropVal == null)
                    {
                        // warn msg "NULL prop value for dest tank ^1 prop ^2"
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN25), programName, "BL-" + curblend.lngID, vntPropAlias, DestTank.strName,
                        gstrBldrName, "", "", "", res);

                        // TODO: Exit Function: Warning!!! Need to return the value
                        return rtnData;
                    }
                    else
                    {
                        if(vntAbsMin != null)
                        {
                            if (vntPropVal < vntAbsMin)
                            {
                                if (vntCtrld == "YES")
                                {
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN48), programName, "BL-" + curblend.lngID, vntPropAlias,
                                    gstrBldrName, "","", "", "", res);                                   

                                    // TODO: Exit Function: Warning!!! Need to return the value
                                    return rtnData;
                                }
                                else if (vntCtrld == null)
                                {
                                    // warn msg "Null controlled flag in abc_blend_props for prop ^1"
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN49), programName, "BL-" + curblend.lngID, vntPropAlias,
                                    DestTank.strName, "", "", "", "", res);
                                }

                                // warning msg "Current prop ^1 in dest tank < min"
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN45), programName, "BL-" + curblend.lngID, vntPropAlias, vntPropVal.ToString(),
                                    DestTank.strName, vntAbsMin.ToString(), "", "", res);

                                vntPropVal = vntAbsMin;
                            }
                        }
                        else
                        {
                            // warn msg "Null abs_min for prop ^1"
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN47), programName, "BL-" + curblend.lngID, "ABS_MIN", vntPropAlias,
                                   DestTank.strName, "", "", "", res);
                            
                            if(vntAbsMax != null)
                            {
                                if (vntPropVal.Value > vntAbsMax)
                                {
                                    if ((vntCtrld == "YES"))
                                    {
                                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN48), programName, "BL-" + curblend.lngID, vntPropAlias,
                                         gstrBldrName,"", "", "", "", res);
                                        // TODO: Exit Function: Warning!!! Need to return the value
                                        return rtnData;
                                    }
                                    else if (vntCtrld == null)
                                    {
                                        // warn msg "Null controlled flag in abc_blend_props for prop ^1"
                                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN49), programName, "BL-" + curblend.lngID, vntPropAlias,
                                        DestTank.strName, "", "", "", "", res);
                                    }

                                    // warning msg "Current prop ^1 in dest tank > max"
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN46), programName, "BL-" + curblend.lngID, vntPropAlias,
                                        vntPropVal.ToString(), DestTank.strName, vntAbsMin.ToString(), "", "", res);
                                   
                                    vntPropVal = vntAbsMax;
                                }

                            }
                            else
                            {
                                // warn msg "Null abs_max for prop ^1"
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN47), programName, "BL-" + curblend.lngID, "ABS_MAX",
                                        vntPropAlias, DestTank.strName, "", "", "", res);
                            }
                        }

                        // set heel value for this prop in ABC_DEST_PROPS
                        await _repository.SetHeelVal(vntPropVal,vntPropVal,curblend.lngID,DestTank.intID,vntPropID);
                        if (enumDebugLevel >= DebugLevels.Medium)
                        {
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG14), programName, cstrDebug, curblend.strName, DestTank.strName,
                            vntPropAlias, vntPropVal.ToString(), "", "", res);
                        }
                    }

                    if (enumDebugLevel == DebugLevels.High)
                    {
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG32), programName, cstrDebug, curblend.strName,
                        DestTank.strName, vntPropAlias, vntPropVal.ToString(), vntAbsMin.ToString(), vntAbsMax.ToString(), res);
                    }
                }

                //To skip the checking of dest tank data if source_destn_type <> "TANK"
                if(strSrceDestType == "TANK")
                {
                    // --- RW 02-Mar-17 Gasoline Ethanol blending remedial ---
                    //  If Ethanol blend, re-calculate heel properties as blend property values
                    //  will have been replaced with latest selected and good properties from S&LP
                    // 
                    if (vntBldrsData[intBldrIdx].EthanolFlag == "YES" && curblend.vntEtohBldgReqd == "YES")
                    {
                        await CalcHeelProps(curblend.lngID,DestTank.intID,curblend.strName,gstrBldrName,Convert.ToInt32(vntBldrsData[intBldrIdx].PrdgrpId));
                    }

                    // --- RW 02-Mar-17 Gasoline Ethanol blending remedial ---
                    // check maximum product vol in dest tank
                    if (DestTank.tagMaxVol.vntTagVal!= null && DestTank.tagAvlVol.vntTagVal != null)
                    {
                        sngMaxPrdVol = Convert.ToDouble(DestTank.tagMaxVol.vntTagVal) - Convert.ToDouble(DestTank.tagAvlVol.vntTagVal);
                        if ((strTkInUseFlag == "YES"))
                        {
                            if ((curblend.sngTgtVol > sngMaxPrdVol))
                            {
                                // warning msg "Target vol > max product vol"
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN50), programName, "BL-" + curblend.lngID, curblend.sngTgtVol.ToString(),
                                curblend.strName, sngMaxPrdVol.ToString(), DestTank.strName, "", "", res);
                            }

                            if (enumDebugLevel >= DebugLevels.Medium)
                            {
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG34), programName, cstrDebug, curblend.strName,
                               curblend.strName, DestTank.tagAvlVol.vntTagVal.ToString(), DestTank.tagMaxVol.vntTagVal.ToString(), sngMaxPrdVol.ToString(), "", res);
                            }                          
                        }
                        else if (strFlushTkFlag == "YES")
                        {
                            if (sngTransferLineVol > sngMaxPrdVol)
                            {
                                // warning msg TRANSFER LINE VOL ^1 FOR BLEND ^2 > MAX AVAILABLE SPACE ^3 FOR DEST TANK ^4
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN84), programName, "BL-" + curblend.lngID, sngTransferLineVol.ToString(),
                                curblend.strName, sngMaxPrdVol.ToString(), DestTank.strName, "", "", res);                                
                            }
                        }
                    }
                }
            }

            // check the order of cycle times for Blend Monitor, Optimization Monitor,
            // and Tank Monitor
            vntPrgCycleTimes = await _repository.GetPrgCycleTimes();
            
            if (vntPrgCycleTimes[2].CycleTime < vntPrgCycleTimes[0].CycleTime)
            {
                // warning msg "Tank Monitor cycle time is smaller than Blend Monitor"
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN44), programName, "BL-" + curblend.lngID, vntPrgCycleTimes[2].CycleTime.ToString(), vntPrgCycleTimes[2].Name,
                        vntPrgCycleTimes[0].CycleTime.ToString(), vntPrgCycleTimes[0].Name, "", "", res);
            }

            if (vntPrgCycleTimes[1].CycleTime < vntPrgCycleTimes[2].CycleTime)
            {
                // warning msg "Optimization Monitor cycle time is smaller than Tank Monitor"
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN44), programName, "BL-" + curblend.lngID, vntPrgCycleTimes[1].CycleTime.ToString(), vntPrgCycleTimes[1].Name,
                        vntPrgCycleTimes[2].CycleTime.ToString(), vntPrgCycleTimes[2].Name, "", "", res);               
            }

            if (enumDebugLevel == DebugLevels.High)
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG31), programName, cstrDebug, curblend.strName, vntPrgCycleTimes[2].CycleTime.ToString(),
                        vntPrgCycleTimes[1].CycleTime.ToString(), vntPrgCycleTimes[0].CycleTime.ToString(), "", "", res);
            }

            // Pass the real (not flushing) dest tank back to the function downloading for further calcs
            DestTank.strFixHeelFlg = await _repository.GetDestTankData(curblend.lngID,DestTank.intID,DestTank.vntHeelVol);
            return ValidInvalid.valid;
        }
        private async Task<RetStatus> DownloadLineupCompPmps(int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend, double lngLineupID, DebugLevels enumDebugLevel, bool blnFromSwing = false)
        {
            List<AbcPumps> vntPumpsData;
            string strInUsePmpId;
            string strPumpName;
            string strModePmpTID;
            string strStatusPmpId;
            string strInServFlag;
            DcsTag tag = new DcsTag();
            int intJ;
            bool blnSelect;
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            RetStatus rtnData = RetStatus.FAILURE;
            var res = "";
            // get pump data based on the lineup ID (stations)
            vntPumpsData = await _repository.GetPumpsData(lngLineupID);
            
            if (vntPumpsData.Count() > 0)
            {                
                for (intJ = 0; intJ < vntPumpsData.Count(); intJ++)
                {
                    blnSelect = true;
                    if (vntPumpsData[intJ].InuseTagId != null)
                    {
                        // checks and warnings
                        if (vntPumpsData[intJ].StatusTagId != null)
                        {
                            AbcTags DataRes = await _repository.GetTagNameAndVal(vntPumpsData[intJ].StatusTagId);
                            tag.vntTagName = DataRes.Name;
                            tag.vntTagVal = DataRes.ReadValue.ToString();
                            
                            if (Convert.ToInt32(tag.vntTagVal) == (int)OnOff.ON_)
                            {
                                // warn msg "Pump ^1 is currently running"
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN21), programName, "BL-" + curblend.lngID, vntPumpsData[intJ].Name, "",
                                "", "\\", "", "", res);
                                // July 15, 2002: Download the pump selection anyway if pump is running
                            }

                        }

                        //   Skip this calculation if there are not pumps preconfigured for the lineup id
                        if (vntPumpsData[intJ].InSerFlag != null)
                        {
                            if (vntPumpsData[intJ].InSerFlag != "YES")
                            {
                                if ((blnFromSwing == false))
                                {
                                    // warn msg "PUMP ^1 NOT LISTED IN SERVICE IN ABC OR NOT IN AUTO MODE IN DCS.  DOWNLOADING CANCELED
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN22), programName, "BL-" + curblend.lngID, vntPumpsData[intJ].Name, "",
                                    "", "\\", "", "", res);
                                    
                                    // July 23,2002: abort downloading when pump is not in Auto mode                                    

                                    await NullCmdAction(intBldrIdx,vntBldrsData,curblend,enumDebugLevel,true);
                                    gArPrevBldData[intBldrIdx].enumCmd = null;
                                    gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                                    // TODO: Exit Function: Warning!!! Need to return the value
                                    return rtnData;
                                }
                                else
                                {
                                    // warn msg "PUMP ^1 NOT IN ABC SERVICE OR NOT AUTO MODE.  COMMAND SELECTION IGNORED
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN23), programName, "BL-" + curblend.lngID, vntPumpsData[intJ].Name, "",
                                    "", "\\", "", "", res);
                                    blnSelect = false;
                                }

                            }

                        }

                        if (vntPumpsData[intJ].ModeTid != null)
                        {
                            AbcTags DataRes = await _repository.GetTagNameAndVal(vntPumpsData[intJ].ModeTid);
                            tag.vntTagName = DataRes.Name;
                            tag.vntTagVal = DataRes.ReadValue.ToString();
                            
                            if (Convert.ToInt32(tag.vntTagVal) != (int)OnOff.ON_)
                            {
                                if ((blnFromSwing == false))
                                {
                                    // warn msg "PUMP ^1 NOT LISTED IN SERVICE IN ABC OR NOT IN AUTO MODE IN DCS.  DOWNLOADING CANCELED
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN22), programName, "BL-" + curblend.lngID, vntPumpsData[intJ].Name, "",
                                    "", "\\", "", "", res);

                                    // set ABC_BLENDS.PENDING_STATE to null
                                    await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);
                                    gArPrevBldData[intBldrIdx].enumCmd = null;
                                    gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                                    // TODO: Exit Function: Warning!!! Need to return the value
                                    return rtnData;                                    
                                }
                                else
                                {
                                    // warn msg "PUMP ^1 NOT IN ABC SERVICE OR NOT AUTO MODE.  COMMAND SELECTION IGNORED
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN23), programName, "BL-" + curblend.lngID, vntPumpsData[intJ].Name, "",
                                    "", "\\", "", "", res);
                                    blnSelect = false;
                                }
                            }
                        }

                        if ((blnSelect == true))
                        {
                            if (vntPumpsData[intJ].InuseTagId != null)
                            {
                                await _repository.SetWriteTagVal((int)YesNo.YES, "YES", vntPumpsData[intJ].InuseTagId);
                            }
                        }
                    }                 
                }
            }
            return RetStatus.SUCCESS;
        }
        private async Task<RetStatus> DownloadBlendStation(int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend, DebugLevels enumDebugLevel)
        {
            List<CompTanksData> vntSrcTksData = new List<CompTanksData>();
            DcsTag tagSelSrce =  new DcsTag();
            DcsTag tagSelStation = new DcsTag();
            DcsTag tag = new DcsTag();
            List<DcsTag> arSrcTksVolTags = new List<DcsTag>();
            double? vntRcpSpTid;
            double? vntSlctStationTid;
            DestTankData DestTank = new DestTankData();
            int intMatID;
            int intTankID;
            int intPreSelectTankId;
            int intStationId;
            int intNS;
            int intNStations;
            int intI;
            int intMatNum;
            int intDCSLineupNum;
            int intNComps;
            int intSelStation;
            int intTankNum;
            int intPmpIndex;
            int intDcsPumpID;
            double dblMaxVol;
            double dblAvailComp;
            double dblStationCurRecipe;
            double dblMinvol;
            double dblAvailVol;
            double dblAvailSpace;
            string strTankName;
            string strCompName;
            string strStationName;
            string strModeTag;
            string strInServFlag;
            string strPumpName;
            string strLineupName = "";
            string vntTagName;
            double lngSelTID;
            double lngMatNumTid;
            double lngTankPreSelectNumTid;
            double lngPreselecTankTagId;
            double lngPreselTID;
            double? vntTagID = 0;
            double lngTankSelectNumTid;
            double? vntSelStationTIDAll = 0;
            double[] vntSelStation = new double[0];
            double lngTankIdAS;
            double lngCompLineupID;
            double lngLineupPreselID;
            double lngLineupSelTID;
            double lngLineupPreselTID;
            double lngPumpASelTID;
            double lngPumpBSelTID;
            double lngPumpCSelTID;
            double lngPumpDSelTID;
            double lngPumpXSelTID = 0;
            double lngRcpSpTid;
            double? vntLineupSelTIDAll;
            int intSelComp;
            int intLineEqpOrder;
            int intNumPumps = 0;
            double lngPumpAId = 0;
            double lngPumpBId = 0;
            double lngPumpCId = 0;
            double lngPumpDId = 0;
            double lngPumpXId = 0;
            bool blnSelect;
            var res = "";
            RetStatus rtnData = RetStatus.FAILURE;
            if (enumDebugLevel == DebugLevels.High)
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, curblend.strName, "DOWNLOAD_STATION",
                    "", "", "", "", res);
            }

            if (await ChkBlendData(curblend, vntSrcTksData, arSrcTksVolTags, DestTank, intBldrIdx, vntBldrsData, enumDebugLevel) == ValidInvalid.invalid)
            {
                if (enumDebugLevel == DebugLevels.High)
                {
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG30), programName, cstrDebug, curblend.strName, "INVALID",
                   "", "", "", "", res);
                }

                // warn msg "Blend order cannot be downloaded due to invalid data
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN16), programName, "BL-" + curblend.lngID, gstrBldrName, "",
                  "", "", "", "", res);

                // call NULL_COMMAND_ACTION function
                await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);
                gArPrevBldData[intBldrIdx].enumCmd = null;
                gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                // TODO: Exit Function: Warning!!! Need to return the value
                return rtnData;               
            }
            else if (enumDebugLevel == DebugLevels.High)
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG30), programName, cstrDebug, curblend.strName, "VALID",
                 "", "", "", "", res);
            }


            List<CompSrceData> CompSrceDataList = await _repository.GetCompSrceData(curblend.lngID);

            intNComps = CompSrceDataList.Count();
            //  redimendion the array containing the comps IDs
            double[] vntSelComp = new double[intNComps];
             
            intSelComp = 0;
            // Save the total number of stations per blender                
            List<AbcStations> AllBldrStationsData = await _repository.GetAllBldrStationsData(vntBldrsData[intBldrIdx].Id);
            if (AllBldrStationsData.Count() > 0)
            {
                intNStations = AllBldrStationsData.Count();
                vntSelStation = new double[intNStations];
            }

            // Reset the number of stations to zero
            intSelStation = 0;

            for (intI = 0; intI < intNComps; intI++)
            {
                intMatID = (int)CompSrceDataList[intI].MatId;
                intTankID = (int)CompSrceDataList[intI].TankId;
                lngCompLineupID = (CompSrceDataList[intI].LineupId == 0) ? -1 : Convert.ToDouble(CompSrceDataList[intI].LineupId);
                strCompName = (CompSrceDataList[intI].CompName == null) ? "" : CompSrceDataList[intI].CompName;
                strTankName = (CompSrceDataList[intI].TankName == null) ? "" : CompSrceDataList[intI].TankName;
                if (lngCompLineupID == -1)
                {
                    // warn msg "Lineup_id missing for comp"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN20), programName, "BL-" + curblend.lngID, "BLEND SOURCE LINEUP_ID", strCompName,
                    strTankName, gstrBldrName, "", "", res);

                    await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, DestTank.intID, enumDebugLevel);

                    //  Call NULL_COMMAND_ACTION function
                    await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);
                    gArPrevBldData[intBldrIdx].enumCmd = null;
                    gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                    // TODO: Exit Function: Warning!!! Need to return the value
                    return rtnData;
                }

                List<BlendSourcesTankData> BlendSourcesTankDataList = await _repository.GetBlendSourcesTankData(curblend.lngID, intMatID);
                intPreSelectTankId = -1;
                lngLineupPreselID = -1;
                if (BlendSourcesTankDataList.Count() > 0)
                {
                    intPreSelectTankId = (int)BlendSourcesTankDataList[0].ToTkId;
                    lngLineupPreselID = (double)BlendSourcesTankDataList[0].LineupId;
                }

                List<AbcBlenderSources> BldrSrcPreselTID = new List<AbcBlenderSources>();
                // get the preselected_tid for the preselected tank
                if ((intPreSelectTankId != -1))
                {
                     BldrSrcPreselTID = await _repository.GetBldrSrcPreselTID(vntBldrsData[intBldrIdx].Id, curblend.lngID, intMatID, intPreSelectTankId.ToString());
                    if (BldrSrcPreselTID.Count() > 0)
                    {
                        lngPreselecTankTagId = (BldrSrcPreselTID[0].PreselectionTid == null) ? -1 : Convert.ToDouble(BldrSrcPreselTID[0].PreselectionTid);
                    }
                    else
                    {
                        lngPreselecTankTagId = -1;
                    }
                    
                }
                else
                {
                    lngPreselecTankTagId = -1;
                }

                if ((lngPreselecTankTagId != -1))
                {
                    // Set Write value=1 for the PreSelection tid
                    await _repository.SetWriteTagVal((int)OnOff.ON_, "YES", lngPreselecTankTagId);                    
                }

                // *************
                // Set selection tid for all sources to OFF except fo the current selected
                // Get the abc_blender_sources.selection_tid for all tanks with this material
                lngPreselTID = -1;
                BldrSrcPreselTID = await _repository.GetBldrSrcPreselTID(vntBldrsData[intBldrIdx].Id,curblend.lngID,intMatID,"%");
                vntTagID = await _repository.GetBldrSrcSlctTid(vntBldrsData[intBldrIdx].Id, intTankID);
                if (vntTagID != null)
                {
                    // Set Write value=1 for the Selection tid
                    await _repository.SetWriteTagVal((int)YesNo.YES, "YES", vntTagID);                    
                }

                foreach (AbcBlenderSources BldrSrcPreselTIDObj in BldrSrcPreselTID)                
                {
                    lngSelTID = (BldrSrcPreselTIDObj.SelectionTid == null) ? -1 : Convert.ToDouble(BldrSrcPreselTIDObj.SelectionTid);
                    lngPreselTID = (BldrSrcPreselTIDObj.PreselectionTid == null) ? -1 : Convert.ToDouble(BldrSrcPreselTIDObj.PreselectionTid);
                    if ((vntTagID != lngSelTID))
                    {
                        AbcTags DataRes = await _repository.GetTagNameAndVal(lngSelTID);
                        tagSelSrce.vntTagName = DataRes.Name;
                        tagSelSrce.vntTagVal = DataRes.ReadValue.ToString();
                        
                        if (tagSelSrce.vntTagVal != null && Convert.ToInt32(tagSelSrce.vntTagVal) ==(int) OnOff.ON_)
                        {
                            // reset Write value=0 for the selection tid
                            await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngSelTID);
                        }

                    }

                    if ((lngPreselecTankTagId != lngPreselTID))
                    {
                        AbcTags DataRes = await _repository.GetTagNameAndVal(lngPreselTID);
                        tagSelSrce.vntTagName = DataRes.Name;
                        tagSelSrce.vntTagVal = DataRes.ReadValue.ToString();

                        if (tagSelSrce.vntTagVal != null && Convert.ToInt32(tagSelSrce.vntTagVal) == (int)OnOff.ON_)
                        {
                            // reset Write value=0 for the selection tid
                            await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngPreselTID);
                        }                        
                    }                    
                }

                //' Get all the stations used by this component               
                List<BldrStationsData> BldrStationsDataList = await _repository.GetBldrStationsData(lngCompLineupID, vntBldrsData[intBldrIdx].Id);
                foreach (BldrStationsData BldrStationsDataObj in BldrStationsDataList)
                {
                    vntRcpSpTid = BldrStationsDataObj.RcpSpTagId;
                    vntSlctStationTid = BldrStationsDataObj.SelectStationTid;
                    lngMatNumTid = (BldrStationsDataObj.MatNumTid == null) ? -1 : Convert.ToDouble(BldrStationsDataObj.MatNumTid);
                    strStationName = BldrStationsDataObj.StationName;
                    lngTankSelectNumTid = (BldrStationsDataObj.TankSelectNumTid == null) ? -1 : Convert.ToDouble(BldrStationsDataObj.TankSelectNumTid);
                    lngTankPreSelectNumTid = (BldrStationsDataObj.TankPreSelectNumTid == null) ? -1 : Convert.ToDouble(BldrStationsDataObj.TankPreSelectNumTid);
                    lngLineupSelTID = (BldrStationsDataObj.LineupSelTid == null) ? -1 : Convert.ToDouble(BldrStationsDataObj.LineupSelTid);
                    lngLineupPreselTID = (BldrStationsDataObj.LineupPreSelTid == null) ? -1 : Convert.ToDouble(BldrStationsDataObj.LineupPreSelTid);
                    intStationId = (int)BldrStationsDataObj.StationId;
                    // set recipe tags
                    if (vntRcpSpTid != null)
                    {
                        AbcBlendStations BldStationsData = await _repository.GetBldStationsData(curblend.lngID,intMatID,intStationId);

                        dblStationCurRecipe = (BldStationsData.CurSetpoint == null) ? -1 : Convert.ToDouble(BldStationsData.CurSetpoint);
                                                
                        if ((dblStationCurRecipe != -1))
                        {
                            await _repository.SetWriteTagVal((int)dblStationCurRecipe, "YES", vntRcpSpTid);

                            // get RECIPE_SP_TID tag name
                            vntTagName = await _repository.GetTagName(vntRcpSpTid);
                            
                            if (enumDebugLevel >= DebugLevels.Medium)
                            {
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG12), programName, cstrDebug, curblend.strName, dblStationCurRecipe.ToString(),
                                strCompName, vntTagName, "", "", res);
                            }                            
                        }
                        else
                        {
                            //  Null cur_recipe in the station ^1 used for component ^2. Download canceled
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN80), programName, "BL-" + curblend.lngID, strStationName, strCompName,
                                "", "", "", "", res);

                            await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, DestTank.intID, enumDebugLevel);

                            //  Call NULL_COMMAND_ACTION function
                            await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);
                            gArPrevBldData[intBldrIdx].enumCmd = null;
                            gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                            // TODO: Exit Function: Warning!!! Need to return the value
                            return rtnData;                           
                        }

                    }
                    else
                    {
                        // warn msg "Recipe_sp_tid tag missing"
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN18), programName, "BL-" + curblend.lngID, "RECIPE_SP_TID", strCompName,
                                gstrBldrName, "", "", "", res);

                        await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, DestTank.intID, enumDebugLevel);

                        //  Call NULL_COMMAND_ACTION function
                        await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);
                        gArPrevBldData[intBldrIdx].enumCmd = null;
                        gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                        // TODO: Exit Function: Warning!!! Need to return the value
                        return rtnData;                        
                    }

                    // set select_station_tid to ON in the DCS
                    if (vntSlctStationTid != null)
                    {
                        //  Get all the stations tag selection from abc_stations
                        List<AbcStations> AllBldrStationsDataList =  await _repository.GetAllBldrStationsData(vntBldrsData[intBldrIdx].Id);
                        List<AbcStations> AllBldrStationsDataListFlt = new List<AbcStations>();
                        if (AllBldrStationsDataList.Count() > 0)
                        {
                            AllBldrStationsDataListFlt = AllBldrStationsDataList.Where<AbcStations>(row => row.SelectStationTid == vntSlctStationTid).ToList();
                            
                            if (AllBldrStationsDataListFlt.Count() > 0)
                            {
                                // Store the selected STATION in an array
                                intSelStation = (intSelStation + 1);
                                vntSelStation[intSelStation] = Convert.ToDouble(vntSlctStationTid);
                            }

                        }

                        // set select_station_tid to ON in the DCS
                        await _repository.SetWriteTagVal((int)OnOff.ON_, "YES", vntSlctStationTid);
                    }
                    else if ((gstrDownloadType == "STATION"))
                    {
                        // warn msg "Selection_Station_tid tag missing"
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN18), programName, "BL-" + curblend.lngID, "SELECT_STATION_TID", strStationName,
                               gstrBldrName, "", "", "", res);

                        await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, DestTank.intID, enumDebugLevel);

                        //  Call NULL_COMMAND_ACTION function
                        await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);
                        gArPrevBldData[intBldrIdx].enumCmd = null;
                        gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                        // TODO: Exit Function: Warning!!! Need to return the value
                        return rtnData;
                    }

                    // Download the DCS_Mat_Num to the DCS through DCS_Mat_Num_tid
                    if (lngMatNumTid != -1)
                    {
                        if (intMatID != -1)
                        {
                            AbcMaterials MatNameData =  await _repository.GetMatName(intMatID);
                            intMatNum = (MatNameData.DcsMatNum == null)?-1: Convert.ToInt32(MatNameData.DcsMatNum);                            
                        }
                        else
                        {
                            intMatNum = -1;
                        }

                        if (intMatNum != -1)
                        {
                            // Write the DCS Mat number to the DCS
                            await _repository.SetWriteTagVal(intMatNum, "YES", lngMatNumTid);                           
                        }
                        else
                        {
                            // ^1 IS NULL IN ^2 FOR COMP ^3.  ^4 NOT SELECTED FOR STATION ^5
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN81), programName, "BL-" + curblend.lngID, "MAT_NUM", "ABC_MATERIALS",
                               strCompName, "MATERIAL", strStationName, "", res);
                        }

                    }
                    else if ((gstrDownloadType == "STATION"))
                    {
                        // warn msg "MAT_NUM_TID tag missing"
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN18), programName, "BL-" + curblend.lngID, "MAT_NUM_TID", strStationName,
                               gstrBldrName, "", "", "", res);

                        await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, DestTank.intID, enumDebugLevel);

                        //  Call NULL_COMMAND_ACTION function
                        await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);
                        gArPrevBldData[intBldrIdx].enumCmd = null;
                        gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                        // TODO: Exit Function: Warning!!! Need to return the value
                        return rtnData;
                    }

                    // Download the DCS_Tank_Num to the DCS through Tank_Select_Num_tid
                    if ((lngTankSelectNumTid != -1))
                    {
                        if ((intTankID != -1))
                        {
                            List<AbcTanks> DataTankID = await _repository.GetDataTankID(intTankID);
                            intTankNum = (DataTankID[0].DcsTankNum == null)?-1:Convert.ToInt32(DataTankID[0].DcsTankNum);                            
                        }
                        else
                        {
                            intTankNum = -1;
                        }

                        if ((intTankNum != -1))
                        {
                            // Write the DCS Tank number to the DCS
                            await _repository.SetWriteTagVal(intTankNum, "YES", lngTankSelectNumTid);
                        }
                        else
                        {
                            // ^1 IS NULL IN ^2 FOR COMP ^3.  ^4 NOT SELECTED FOR STATION ^5
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN81), programName, "BL-" + curblend.lngID, "TANK_NUM", "ABC_TANKS",
                              strCompName, "TANK", strStationName, "", res);
                        }

                    }
                    else if ((gstrDownloadType == "STATION"))
                    {
                        // warn msg "TANK_SELECT_NUM_TID tag missing"
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN18), programName, "BL-" + curblend.lngID, "TANK_SELECT_NUM_TID", strStationName,
                               gstrBldrName, "", "", "", res);

                        await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, DestTank.intID, enumDebugLevel);

                        //  Call NULL_COMMAND_ACTION function
                        await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);
                        gArPrevBldData[intBldrIdx].enumCmd = null;
                        gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                        // TODO: Exit Function: Warning!!! Need to return the value
                        return rtnData;
                    }

                    // Download the DCS_Tank_Num to the DCS through Tank_PreSelect_Num_tid
                    if ((lngTankPreSelectNumTid != -1))
                    {
                        if ((intPreSelectTankId != -1))
                        {
                            List<AbcTanks> DataTankIDData = await _repository.GetDataTankID(intPreSelectTankId);
                            intTankNum = (DataTankIDData[0].DcsTankNum== null)?-1:Convert.ToInt32(DataTankIDData[0].DcsTankNum);                            
                        }
                        else
                        {
                            intTankNum = -1;
                        }

                        if ((intTankNum != -1))
                        {
                            // Write the DCS Tank number to the DCS
                            await _repository.SetWriteTagVal(intTankNum, "YES", lngTankPreSelectNumTid);                            
                        }

                    }

                    // Feb. 17, 03: Download lineup indexes to DCS using the Station interface
                    if ((lngLineupSelTID != -1))
                    {
                        // get DCS Lineup index if selected lineup id is not null
                        if ((lngCompLineupID != -1))
                        {
                            AbcCompLineups DCSCompLineupNum = await _repository.GetDCSCompLineupNum(lngCompLineupID);
                            intDCSLineupNum = (int)DCSCompLineupNum.DcsLineupNum;
                            strLineupName = DCSCompLineupNum.Name;                            
                        }
                        else
                        {
                            intDCSLineupNum = -1;
                        }

                        if ((intDCSLineupNum != -1))
                        {
                            // Write the Selected DCS LINEUP number to the DCS
                            await _repository.SetWriteTagVal(intDCSLineupNum, "YES", lngLineupSelTID);                            
                        }
                        else
                        {
                            // IN BLEND ^1, COMP ^2, DCS LINEUP NUM IS NULL FOR LINEUP ^3.  CMD SEL/PRESEL IGNORED
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN96), programName, "BL-" + curblend.lngID, curblend.strName, strCompName,
                               strLineupName, "", "", "", res);
                        }

                    }

                    if ((lngLineupPreselTID != -1))
                    {
                        // get DCS Lineup index if presel lineup id is not null
                        if ((lngLineupPreselID != -1))
                        {
                            AbcCompLineups DCSCompLineupNum = await _repository.GetDCSCompLineupNum(lngLineupPreselID);
                            intDCSLineupNum = Convert.ToInt32(DCSCompLineupNum.DcsLineupNum);
                            strLineupName = DCSCompLineupNum.Name;
                        }
                        else
                        {
                            intDCSLineupNum = -1;
                        }

                        if ((intDCSLineupNum != -1))
                        {
                            // Write the Preselected DCS LINEUP number to the DCS
                            await _repository.SetWriteTagVal(intDCSLineupNum, "YES", lngLineupPreselTID);                            
                        }

                    }

                    // get the eqp_order to donwload pumps corresponding stations
                    intLineEqpOrder = (BldrStationsDataObj.LineEqpOrder == null)?-1:Convert.ToInt32(BldrStationsDataObj.LineEqpOrder);
                    if(gstrDownloadType == "STATION")
                    {
                        //Download pumps based on stations
                        lngPumpASelTID = (BldrStationsDataObj.PumpASelTid == null) ? -1 : Convert.ToDouble(BldrStationsDataObj.PumpASelTid);
                        lngPumpBSelTID = (BldrStationsDataObj.PumpBSelTid == null) ? -1 : Convert.ToDouble(BldrStationsDataObj.PumpBSelTid);
                        lngPumpCSelTID = (BldrStationsDataObj.PumpCSelTid == null) ? -1 : Convert.ToDouble(BldrStationsDataObj.PumpCSelTid);
                        lngPumpDSelTID = (BldrStationsDataObj.PumpDSelTid == null) ? -1 : Convert.ToDouble(BldrStationsDataObj.PumpDSelTid);


                        //Get the component lineup pumps per station
                        (lngPumpAId, lngPumpBId, lngPumpCId, lngPumpDId, intNumPumps) = await GetStationPumps(lngCompLineupID, "COMPONENT", intLineEqpOrder, lngPumpAId, lngPumpBId, lngPumpCId, lngPumpDId, intNumPumps);

                        intPmpIndex = 0;
                        for (intPmpIndex = 0; intPmpIndex < intNumPumps; intPmpIndex++)
                        {
                            switch (intPmpIndex)
                            {
                                case 0:
                                    lngPumpXSelTID = lngPumpASelTID;
                                    lngPumpXId = lngPumpAId;
                                    break;
                                case 1:
                                    lngPumpXSelTID = lngPumpBSelTID;
                                    lngPumpXId = lngPumpBId;
                                    break;
                                case 2:
                                    lngPumpXSelTID = lngPumpCSelTID;
                                    lngPumpXId = lngPumpCId;
                                    break;
                                case 3:
                                    lngPumpXSelTID = lngPumpDSelTID;
                                    lngPumpXId = lngPumpDId;
                                    break;
                            }
                            AbcPumps PumpCfg  = await _repository.GetPumpCfg(lngPumpXId);
                            
                            strPumpName = string.IsNullOrEmpty(PumpCfg.Name)?"-1": PumpCfg.Name;
                            strModeTag = (PumpCfg.ModeTid == null) ? "-1" : PumpCfg.ModeTid.ToString();
                            strInServFlag = PumpCfg.InSerFlag;
                            intDcsPumpID = (PumpCfg.DcsPumpId == null) ? -1 : Convert.ToInt32(PumpCfg.DcsPumpId);
                            blnSelect = true;
                            if ((strInServFlag != "YES"))
                            {
                                // warn msg "PUMP ^1 NOT IN ABC SERVICE OR NOT AUTO MODE.  COMMAND SELECTION IGNORED
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN23), programName, "BL-" + curblend.lngID, strPumpName, "",
                               "", "\\", "", "", res);
                                blnSelect = false;
                            }

                            if (strModeTag != "-1")
                            {
                                AbcTags DataRes = await _repository.GetTagNameAndVal(Convert.ToDouble(strModeTag));
                                tag.vntTagName = DataRes.Name;
                                tag.vntTagVal = DataRes.ReadValue.ToString();
                               
                                if (Convert.ToInt32(tag.vntTagVal) != (int)OnOff.ON_)
                                {
                                    // warn msg "PUMP ^1 NOT IN ABC SERVICE OR NOT AUTO MODE.  COMMAND SELECTION IGNORED
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN23), programName, "BL-" + curblend.lngID, strPumpName, "",
                                    "", "\\", "", "", res);                                   
                                    blnSelect = false;
                                }

                            }

                            if ((blnSelect == true))
                            {
                                if (lngPumpXSelTID != -1 && intDcsPumpID != -1)
                                {
                                    await _repository.SetWriteTagVal(intDcsPumpID, "YES", lngPumpXSelTID);
                                }
                            }
                        }
                    }

                }
                //get the blender_comps.lineup_sel_tid, blender_comps.lineup_presel_tid
                lngLineupSelTID = -1;
                lngLineupPreselTID = -1;
                
                List<AbcBlenderComps> AllBldrComps1 = await _repository.GetAllBldrComps(vntBldrsData[intBldrIdx].Id);
                List<AbcBlenderComps> AllBldrCompsFlt = new List<AbcBlenderComps>();
                if (AllBldrComps1.Count() > 0)
                {
                    AllBldrCompsFlt = AllBldrComps1.Where<AbcBlenderComps>(row => row.MatId == intMatID).ToList();
                    if (AllBldrCompsFlt.Count() > 0)
                    {
                        lngLineupSelTID = (AllBldrCompsFlt[0].LineupSelTid == null)?-1:Convert.ToDouble(AllBldrCompsFlt[0].LineupSelTid);
                        lngLineupPreselTID = (AllBldrCompsFlt[0].LineupPreselTid == null) ? -1 : Convert.ToDouble(AllBldrCompsFlt[0].LineupPreselTid);
                    }
                }

                // Get the lineup selected tid's from blender comps
                if (AllBldrComps1.Count() > 0)
                {
                    AllBldrCompsFlt = AllBldrComps1.Where<AbcBlenderComps>(row => row.SelectCompTid == lngLineupSelTID).ToList();
                    if (AllBldrCompsFlt.Count() > 0)
                    {
                        // Store the selected comps in an array
                        intSelComp = (intSelComp + 1);
                        vntSelComp[intSelComp] = lngLineupSelTID;
                    }

                }
                
                //Download lineup indexes to DCS using the blender comps interface
                if (lngLineupSelTID != -1)
                {
                    // get DCS Lineup index if selected lineup id is not null
                    if (lngCompLineupID != -1)
                    {
                        AbcCompLineups DCSCompLineupNum = await _repository.GetDCSCompLineupNum(lngCompLineupID);
                        intDCSLineupNum = (int)DCSCompLineupNum.DcsLineupNum;
                        strLineupName = DCSCompLineupNum.Name;                        
                    }
                    else
                    {
                        intDCSLineupNum = -1;
                    }

                    if ((intDCSLineupNum != -1))
                    {
                        // Write the Selected DCS LINEUP number to the DCS
                        await _repository.SetWriteTagVal(intDCSLineupNum, "YES", lngLineupSelTID);                        
                    }
                    else
                    {
                        // IN BLEND ^1, COMP ^2, DCS LINEUP NUM IS NULL FOR LINEUP ^3.  CMD SEL/PRESEL IGNORED
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN96), programName, "BL-" + curblend.lngID, curblend.strName, strCompName,
                                    strLineupName, "", "", "", res);
                    }
                }
                if ((lngLineupPreselTID != -1))
                {
                    // get DCS Lineup index if presel lineup id is not null
                    if ((lngLineupPreselID != -1))
                    {
                        AbcCompLineups DCSCompLineupNum = await _repository.GetDCSCompLineupNum(lngLineupPreselID);
                        
                        intDCSLineupNum = (int)DCSCompLineupNum.DcsLineupNum;
                        strLineupName = DCSCompLineupNum.Name;
                    }
                    else
                    {
                        intDCSLineupNum = -1;
                    }

                    if ((intDCSLineupNum != -1))
                    {
                        // Write the Preselected DCS LINEUP number to the DCS
                        await _repository.SetWriteTagVal(intDCSLineupNum, "YES", lngLineupPreselTID);
                    }
                    else
                    {
                        // IN BLEND ^1, COMP ^2, DCS LINEUP NUM IS NULL FOR LINEUP ^3.  CMD SEL/PRESEL IGNORED
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN96), programName, "BL-" + curblend.lngID, curblend.strName, strCompName,
                                    strLineupName, "", "", "", res);                       
                    }

                }

                //  Download pumps based on lineup ID
                if ((gstrDownloadType == "STATION"))
                {
                    if ((lngCompLineupID != -1))
                    {
                       await DownloadLineupCompPmps(intBldrIdx,vntBldrsData,curblend,lngCompLineupID,enumDebugLevel);
                    }

                }

                // set IN_USE_FLAG to YES for all stations used by the comp lineup
                await _repository.SetStationinuseFlg("YES", lngCompLineupID);                
            }

            //' Deselect others stations of the blender at the beginning of downloading
            //' Get all the stations tag selection from abc_stations
            AllBldrStationsData = await _repository.GetAllBldrStationsData(vntBldrsData[intBldrIdx].Id);
            foreach (AbcStations AllBldrStationsDataObj in AllBldrStationsData)
            {
                vntSelStationTIDAll = AllBldrStationsDataObj.SelectStationTid;
                lngTankSelectNumTid = (AllBldrStationsDataObj.TankSelectNumTid == null) ? -1:Convert.ToDouble(AllBldrStationsDataObj.TankSelectNumTid);
                lngMatNumTid = (AllBldrStationsDataObj.MatNumTid == null) ? -1 : Convert.ToDouble(AllBldrStationsDataObj.MatNumTid);
                lngLineupSelTID = (AllBldrStationsDataObj.LineupSelTid == null) ? -1 : Convert.ToDouble(AllBldrStationsDataObj.LineupSelTid);                
                lngRcpSpTid = (AllBldrStationsDataObj.RcpSpTagId == null) ? -1 : Convert.ToDouble(AllBldrStationsDataObj.RcpSpTagId);                 
                lngLineupSelTID = (AllBldrStationsDataObj.LineupSelTid == null) ? -1 : Convert.ToDouble(AllBldrStationsDataObj.LineupSelTid);                 
                lngPumpASelTID = (AllBldrStationsDataObj.PumpaSelTid == null) ? -1 : Convert.ToDouble(AllBldrStationsDataObj.PumpaSelTid);                 
                lngPumpBSelTID = (AllBldrStationsDataObj.PumpbSelTid == null) ? -1 : Convert.ToDouble(AllBldrStationsDataObj.PumpbSelTid);                 
                lngPumpCSelTID = (AllBldrStationsDataObj.PumpcSelTid == null) ? -1 : Convert.ToDouble(AllBldrStationsDataObj.PumpcSelTid);                 
                lngPumpDSelTID = (AllBldrStationsDataObj.PumpdSelTid == null) ? -1 : Convert.ToDouble(AllBldrStationsDataObj.PumpdSelTid);                 
                //           lngDcsStationNumTid = NVL(ABCdataEnv.rscomGetAllBldrStationsData.Fields("DCS_STATION_NUM").Value, NULL_)
                for (intNS = 1; (intNS <= intSelStation); intNS++)
                {
                    if ((vntSelStation[intNS] != vntSelStationTIDAll))
                    {
                        goto lblNextTID;
                    }
                    else
                    {
                        goto lblNextStation;
                    }

                lblNextTID: { }
                }
                // get the tag value for this station
                AbcTags DataRes = await _repository.GetTagNameAndVal(Convert.ToDouble(vntSelStationTIDAll));
                tagSelStation.vntTagName = DataRes.Name;
                tagSelStation.vntTagVal = DataRes.ReadValue.ToString();
                
                //  Reset this tag value if it is not reset yet
                if ((tagSelStation.vntTagVal != null) && (Convert.ToInt32(tagSelStation.vntTagVal) != (int)OnOff.OFF))
                {
                    await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", vntSelStationTIDAll);
                    
                    if ((lngTankSelectNumTid != -1))
                    {
                        await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngTankSelectNumTid);                       
                    }

                    if ((lngMatNumTid != -1))
                    {
                        await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngMatNumTid);                       
                    }

                    if ((lngRcpSpTid != -1))
                    {
                        await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngRcpSpTid);                      
                    }

                    if ((lngLineupSelTID != -1))
                    {
                        await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngLineupSelTID);                        
                    }

                    if ((lngPumpASelTID != -1))
                    {
                        await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngPumpASelTID);                        
                    }

                    if ((lngPumpBSelTID != -1))
                    {
                        await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngPumpBSelTID);                        
                    }

                    if ((lngPumpCSelTID != -1))
                    {
                        await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngPumpCSelTID);                        
                    }

                    if ((lngPumpDSelTID != -1))
                    {
                        await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngPumpDSelTID);                        
                    }                    
                }

            lblNextStation: { }
            }

            // Deselect others LINEUP of the blender at the beginning of downloading
            //  Get all the tag selection comps from abc_blender_comps
            List<AbcBlenderComps> AllBldrComps = await _repository.GetAllBldrComps(vntBldrsData[intBldrIdx].Id);

            foreach (AbcBlenderComps AllBldrCompsObj in AllBldrComps)            
            {
                vntLineupSelTIDAll = AllBldrCompsObj.LineupSelTid;
                for (intNS = 1; (intNS <= intSelComp); intNS++)
                {
                    if ((vntSelComp[intNS] != vntLineupSelTIDAll))
                    {
                        goto lblNextTAGID;
                    }
                    else
                    {
                        goto lblNextComp;
                    }

                    lblNextTAGID: { }
                }

                //  get the tag value for this component
                AbcTags DataRes = await _repository.GetTagNameAndVal(Convert.ToDouble(vntLineupSelTIDAll));
                tag.vntTagName = DataRes.Name;
                tag.vntTagVal = DataRes.ReadValue.ToString();                
                //  Reset this tag value if it is not reset yet
                if ((tag.vntTagVal != null) && Convert.ToInt32(tag.vntTagVal) != (int)OnOff.OFF)
                {
                    await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", vntLineupSelTIDAll);
                }

                lblNextComp: { }                
            }
            
            // save current recipe (Total if multiple stations) to sp_recipe in ABC_BLEND_INTERVAL_COMPS
            for (intI = 0; (intI <= (intNComps - 1)); intI++)
            {
                await _repository.SetIntvRcpSp(vntSrcTksData[intI].CurRecipe,curblend.lngID,vntSrcTksData[intI].MatId ,1);
            }

            // Set Result Download value
            return RetStatus.SUCCESS;
        }

        private async Task<(double,double,double,double,int)> GetStationPumps(double lngLineupID, string strLineupType, int intLineEqpOrder, double lngPumpAId, double lngPumpBId, double lngPumpCId, double lngPumpDId, int intNumPumps)
        {
            int intIndex;
            int intGeoID;
            int intNumStations;
            string strOrder;            
            
            // Initialize Number of pumps
            intNumPumps = 0;
            lngPumpAId = 0;
            lngPumpBId = 0;
            lngPumpCId = 0;
            lngPumpDId = 0;
            List<LineGeoId> LineGeoIdData = new List<LineGeoId>();
            if ((strLineupType == "PRODUCT"))
            {
                // Get the line_geo_id from the abc_product_lineups
                LineGeoIdData = await _repository.GetLineGeoIdProduct(lngLineupID);
            }
            else
            {
                // Get the line_geo_id from the abc_comp_lineups
                // Get the line_geo_id from the abc_product_lineups
                LineGeoIdData = await _repository.GetLineGeoId(lngLineupID);
            }

            if (LineGeoIdData.Count() > 0)
            {
                intGeoID = Convert.ToInt32(LineGeoIdData[0].LineGeoID);
                // Determine the number of pumps per station knowing the geometry (fix geometry from ABC_LINEUP_GEO)
                switch (intGeoID)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 6:
                    case 8:
                    case 9:
                    case 11:
                    case 15:
                    case 16:
                    case 19:
                    case 22:
                    case 24:
                    case 25:
                    case 26:
                    case 27:
                    case 28:
                        // 1P1S/1P2S/1P3S/2P1S/2P2S/2P3S/3P1S/3P2S/3P3S/4P1S/4P2S/4P3S/1P0S/0P0S/2P0S/3P0S/4P0S
                        // get the appropiate pumps from abc_comp_lineup_eqp
                        List<double?> pumpId = new List<double?>();
                        if ((strLineupType == "PRODUCT"))
                        {
                            // Get the pump from from the abc_prod_lineup_eqp                            
                            pumpId = await _repository.GetPumpIdProd(lngLineupID);
                            
                        }
                        else
                        {
                            // Get the pump from from the abc_prod_lineup_eqp
                            pumpId = await _repository.GetPumpIdComp(lngLineupID);
                        }

                        intIndex = 1;
                        foreach (double? id in pumpId)                        
                        {
                            if ((intIndex == 1))
                            {
                                lngPumpAId = Convert.ToDouble(id);
                            }
                            else if ((intIndex == 2))
                            {
                                lngPumpBId = Convert.ToDouble(id);
                            }
                            else if ((intIndex == 3))
                            {
                                lngPumpCId = Convert.ToDouble(id);
                            }
                            else if ((intIndex == 4))
                            {
                                lngPumpDId = Convert.ToDouble(id);
                            }

                            // save totatl number of pumps
                            intNumPumps = intIndex;
                            intIndex = (intIndex + 1);
                        }
                        break;
                    case 5:
                    case 12:
                        // 1P1S_1P1S/1P1S_1P1S_1P1S
                        // get the appropiate pumps from abc_comp_lineup_eqp
                        List<double?> pumpids = new List<double?>();
                        if ((strLineupType == "PRODUCT"))
                        {
                            // Get the pump from from the abc_prod_lineup_eqp
                            pumpids = await _repository.GetPumpIdProd(lngLineupID, intLineEqpOrder);
                        }
                        else
                        {
                            // Get the pump from from the abc_prod_lineup_eqp
                            pumpids = await _repository.GetPumpIdComp(lngLineupID, intLineEqpOrder);                            
                        }
                        
                        intIndex = 1;
                        if (pumpids.Count() > 0)
                        {
                            if ((intIndex == 1))
                            {
                                lngPumpAId = Convert.ToDouble(pumpids[0]);
                            }

                            // save total number of pumps
                            intNumPumps = intIndex;
                        }
                        break;
                    case 7:
                    case 10:
                    case 13:
                    case 18:
                    case 21:
                        // 1P1S_1P2S/1P1S_2P1S/1P1S_2P2S/1P1S_3P1S/1P1S_3P2S
                        // get the appropiate pumps from abc_comp_lineup_eqp                        

                        List<double?> pumpiDs = new List<double?>();
                        if ((strLineupType == "PRODUCT"))
                        {
                            // Get the pump from from the abc_prod_lineup_eqp
                            if ((intLineEqpOrder == 1))
                            {
                                pumpids = await _repository.GetPumpIdProd2(lngLineupID, 1);                                
                            }
                            else if (((intLineEqpOrder == 2)
                                        || (intLineEqpOrder == 3)))
                            {
                                pumpids = await _repository.GetPumpIdProd3(lngLineupID, 2);
                            }
                            
                        }
                        else
                        {
                            // Get the pump from from the abc_prod_lineup_eqp
                            if ((intLineEqpOrder == 1))
                            {
                                pumpids = await _repository.GetPumpIdComp2(lngLineupID, 1);
                            }
                            else if (((intLineEqpOrder == 2)
                                        || (intLineEqpOrder == 3)))
                            {
                                pumpids = await _repository.GetPumpIdComp3(lngLineupID, 2);
                            }
                            
                        }
                        
                        intIndex = 1;
                        foreach (double? id in pumpiDs)                        
                        {
                            if ((intIndex == 1))
                            {
                                lngPumpAId = Convert.ToDouble(id);
                            }
                            else if ((intIndex == 2))
                            {
                                lngPumpBId = Convert.ToDouble(id);
                            }
                            else if ((intIndex == 3))
                            {
                                lngPumpCId = Convert.ToDouble(id);
                            }
                            else if ((intIndex == 4))
                            {
                                lngPumpDId = Convert.ToDouble(id);
                            }

                            // save totatl number of pumps
                            intNumPumps = intIndex;
                            intIndex = (intIndex + 1);
                        }
                        break;
                    case 14:
                    case 23:
                        // 1P2S_2P1S/1P2S_3P1S
                        // get the appropiate pumps from abc_comp_lineup_eqp                       
                        List<double?> pumpIDs = new List<double?>();
                        if ((strLineupType == "PRODUCT"))
                        {
                            // Get the pump from from the abc_prod_lineup_eqp
                            if (((intLineEqpOrder == 1) || (intLineEqpOrder == 2)))
                            {
                                pumpIDs = await _repository.GetPumpIdProd2(lngLineupID, 1);
                            }
                            else if ((intLineEqpOrder == 3))
                            {
                                pumpIDs = await _repository.GetPumpIdProd3(lngLineupID, 2);
                            }
                        }
                        else
                        {
                            // Get the pump from from the abc_prod_lineup_eqp
                            if (((intLineEqpOrder == 1) || (intLineEqpOrder == 2)))
                            {
                                pumpIDs = await _repository.GetPumpIdComp2(lngLineupID, 1);
                            }
                            else if ((intLineEqpOrder == 3))
                            {
                                pumpIDs = await _repository.GetPumpIdComp3(lngLineupID, 2);
                            }
                        }
                        
                        intIndex = 1;
                        foreach (double? id in pumpIDs)                        
                        {
                            if ((intIndex == 1))
                            {
                                lngPumpAId = Convert.ToDouble(id);
                            }
                            else if ((intIndex == 2))
                            {
                                lngPumpBId = Convert.ToDouble(id);
                            }
                            else if ((intIndex == 3))
                            {
                                lngPumpCId = Convert.ToDouble(id);
                            }
                            else if ((intIndex == 4))
                            {
                                lngPumpDId = Convert.ToDouble(id);
                            }

                            // save totatl number of pumps
                            intNumPumps = intIndex;
                            intIndex = (intIndex + 1);
                        }
                        break;
                    case 17:
                    case 20:
                        // 2P1S_2P1S/2P1S_2P2S
                        // get the appropiate pumps from abc_comp_lineup_eqp                        

                        List<double?> IDs = new List<double?>();
                        if ((strLineupType == "PRODUCT"))
                        {
                            // Get the pump from from the abc_prod_lineup_eqp
                            if ((intLineEqpOrder == 1))
                            {
                                IDs = await _repository.GetPumpIdProd4(lngLineupID, 1);
                            }
                            else if (((intLineEqpOrder == 2)
                                     || (intLineEqpOrder == 3)))
                            {
                                IDs = await _repository.GetPumpIdProd4(lngLineupID, 2);
                            }
                        }
                        else
                        {
                            // Get the pump from from the abc_prod_lineup_eqp
                            if ((intLineEqpOrder == 1))
                            {
                                IDs = await _repository.GetPumpIdComp5(lngLineupID, 1);
                            }
                            else if (((intLineEqpOrder == 2)
                                    || (intLineEqpOrder == 3)))
                            {
                                IDs = await _repository.GetPumpIdComp5(lngLineupID, 2);
                            }
                        }
                      
                        intIndex = 1;
                        foreach (double? id in IDs)                        
                        {
                            if ((intIndex == 1))
                            {
                                lngPumpAId = Convert.ToDouble(id);
                            }
                            else if ((intIndex == 2))
                            {
                                lngPumpBId = Convert.ToDouble(id);
                            }
                            else if ((intIndex == 3))
                            {
                                lngPumpCId = Convert.ToDouble(id);
                            }
                            else if ((intIndex == 4))
                            {
                                lngPumpDId = Convert.ToDouble(id);
                            }

                            // save totatl number of pumps
                            intNumPumps = intIndex;
                            intIndex = (intIndex + 1);
                        }
                        break;
                    case 29:
                    case 30:
                    case 31:
                        // 0P1S/0P2S/0P3S/0P4S
                        // no pumps to select.  Return empty pump id variables
                        break;
                }
            }            
            return (lngPumpAId, lngPumpBId, lngPumpCId,lngPumpDId, intNumPumps);
        }
        private async Task<RetStatus> DownloadBlendComp(int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend, DebugLevels enumDebugLevel)
        {
            List<CompTanksData> vntSrcTksData = new List<CompTanksData>();
            DcsTag tagSelComp = new DcsTag();
            DcsTag tagSelSrce = new DcsTag();
            List<DcsTag> arSrcTksVolTags = new List<DcsTag>();
            double? vntRcpSpTid;
            double? vntTagID;
            DestTankData DestTank = new DestTankData();
            int intCursor = 0;
            int intNS = 0;
            int intI = 0;
            int intNSrcTanks;
            int intNComps;
            int intSelComp;
            string vntTagName;
            double lngSelTID;
            double lngCompLineupID;
            double? vntSlctCompTid;
            double? vntSelCompTIDAll;
            double?[] vntSelComp;
            var res = "";
            RetStatus rtnData = RetStatus.FAILURE;
            // TODO: On Error GoTo Warning!!!: The statement is not translatable             
            if (enumDebugLevel == DebugLevels.High)
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, curblend.strName, "DOWNLOAD_COMP_BASED",
                    "", "", "", "", res);
            }

            if (await ChkBlendData(curblend, vntSrcTksData, arSrcTksVolTags, DestTank, intBldrIdx, vntBldrsData, enumDebugLevel) == ValidInvalid.invalid)
            {
                if (enumDebugLevel == DebugLevels.High)
                {
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG30), programName, cstrDebug, curblend.strName, "INVALID",
                   "", "", "", "", res);
                }

                // warn msg "Blend order cannot be downloaded due to invalid data
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN16), programName, "BL-" + curblend.lngID, gstrBldrName, "",
                   "", "", "", "", res);
                
                // call NULL_COMMAND_ACTION function
                await NullCmdAction(intBldrIdx,vntBldrsData,curblend,enumDebugLevel,true);
                gArPrevBldData[intBldrIdx].enumCmd = null;
                gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                // TODO: Exit Function: Warning!!! Need to return the value
                return rtnData;
            }
            else if (enumDebugLevel == DebugLevels.High)
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG30), programName, cstrDebug, curblend.strName, "VALID",
                   "", "", "", "", res);
            }
            List<AbcBlenderComps> BldrCmpData = await _repository.GetBldrCmpData(vntBldrsData[intBldrIdx].Id, curblend.lngID);

            vntRcpSpTid = BldrCmpData[0].RecipeSpTid;
            vntSlctCompTid = BldrCmpData[0].SelectCompTid;
            intNSrcTanks = vntSrcTksData.Count; //Information.UBound(vntSrcTksData, 2);
            intNComps = BldrCmpData.Count();
            //  redimendion the array containing the comps IDs
            vntSelComp = new double?[intNComps];
            intSelComp = 0;
            intCursor = 0;
            for (intI = 0; intI < intNComps; intI++)
            {
                if (vntRcpSpTid != null)
                {
                    await _repository.SetWriteTagVal(Convert.ToInt32(vntSrcTksData[intI].CurRecipe), "YES", vntRcpSpTid);
                    // get RECIPE_SP_TID tag name
                    vntTagName = await _repository.GetTagName(vntRcpSpTid);
                    if (enumDebugLevel >= DebugLevels.Medium)
                    {
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG12), programName, cstrDebug, curblend.strName, vntSrcTksData[intI].CurRecipe.ToString(),
                         vntSrcTksData[intI].CompName, vntTagName, "", "", res);
                    }
                }
                else
                {
                    // warn msg "Recipe_sp_tid tag missing"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN18), programName, "BL-" + curblend.lngID, "RECIPE_SP_TID",
                         vntSrcTksData[intI].CompName, gstrBldrName, "", "", "", res);

                    await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, DestTank.intID, enumDebugLevel);

                    //  Call NULL_COMMAND_ACTION function
                    await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);
                    gArPrevBldData[intBldrIdx].enumCmd = null;
                    gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                    // TODO: Exit Function: Warning!!! Need to return the value
                    return rtnData;
                }

                lngSelTID = 0;
                List<AbcBlenderSources> BldrSrcPreselTID = await _repository.GetBldrSrcPreselTID(vntBldrsData[intBldrIdx].Id, curblend.lngID, vntSrcTksData[intI].MatId, "%");
                vntTagID = await _repository.GetBldrSrcSlctTid(vntBldrsData[intBldrIdx].Id, vntSrcTksData[intI].TankId);
                foreach (AbcBlenderSources BldrSrcPreselTIDObj in BldrSrcPreselTID)
                {
                    lngSelTID = (BldrSrcPreselTIDObj.SelectionTid == null) ? -1 : Convert.ToDouble(BldrSrcPreselTIDObj.SelectionTid);
                    if ((vntTagID != lngSelTID))
                    {
                        AbcTags DataRes = await _repository.GetTagNameAndVal(lngSelTID);
                        tagSelSrce.vntTagName = DataRes.Name;
                        tagSelSrce.vntTagVal = DataRes.ReadValue.ToString();

                        if (tagSelSrce.vntTagVal != null && Convert.ToInt32(tagSelSrce.vntTagVal) == (int)OnOff.ON_)
                        {
                            // reset Write value=0 for the selection tid
                            await _repository.SetWriteTagVal((int)OnOff.OFF, "YES", lngSelTID);
                        }
                    }
                }

                //If the selection_tid field is not a tank then it could be null and
                //skip the downloading of this source
                if (vntSrcTksData[intI].SourceDestnType == "TANK")
                {
                    if (vntTagID == null)
                    {
                        // warn msg "Selection_tid tag missing"
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN19), programName, "BL-" + curblend.lngID, "BLENDER SOURCE SELECTION_TID",
                        vntSrcTksData[intI].TankName, gstrBldrName, "", "", "", res);
                        await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, DestTank.intID, enumDebugLevel);

                        //  Call NULL_COMMAND_ACTION function
                        await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);
                        gArPrevBldData[intBldrIdx].enumCmd = null;
                        gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                        // TODO: Exit Function: Warning!!! Need to return the value
                        return rtnData;
                    }
                    await _repository.SetWriteTagVal((int)YesNo.YES, "YES", vntTagID);
                }
                
                //get Lineup Id
                lngCompLineupID = (vntSrcTksData[intI].LineupId == null)?-1: Convert.ToDouble(vntSrcTksData[intI].LineupId);
                if (lngCompLineupID == -1)
                {
                    // warn msg "Lineup_id missing for comp"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN20), programName, "BL-" + curblend.lngID, "BLEND SOURCE LINEUP_ID",
                        vntSrcTksData[intI].CompName, vntSrcTksData[intI].TankName, gstrBldrName, "", "", res);

                    await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, DestTank.intID, enumDebugLevel);

                    //  Call NULL_COMMAND_ACTION function
                    await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);
                    gArPrevBldData[intBldrIdx].enumCmd = null;
                    gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                    // TODO: Exit Function: Warning!!! Need to return the value
                    return rtnData;
                }
                else
                {
                    //  Download pumps based on com lineup id

                    // --- validate parama
                    if (await DownloadLineupCompPmps(intI, vntBldrsData, curblend, lngCompLineupID, enumDebugLevel) == RetStatus.FAILURE)
                    {
                        // close rs before exiting the function
                        await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, DestTank.intID, enumDebugLevel);                       
                        return rtnData;
                    }
                }

                // set IN_USE_FLAG to YES for all stations used by the comp lineup
                await _repository.SetStationinuseFlg("YES",lngCompLineupID);

                // set blender select_comp_tid
                if (vntSlctCompTid != null)
                {
                    //  Get all the tag selection comps from abc_blender_comps
                    List<AbcBlenderComps> AllBldrComps = await _repository.GetAllBldrComps(vntBldrsData[intBldrIdx].Id);
                    List<AbcBlenderComps> AllBldrCompsFlt = new List<AbcBlenderComps>();
                    if (AllBldrComps.Count() > 0)
                    {
                        AllBldrCompsFlt = AllBldrComps.Where<AbcBlenderComps>(row => row.SelectCompTid == vntSlctCompTid).ToList();
                        
                        if (AllBldrCompsFlt.Count()>0)
                        {
                            // Store the selected comps in an array
                            intSelComp = (intSelComp + 1);
                            vntSelComp[intSelComp] = vntSlctCompTid;
                        }
                    }

                    //  Set the blender_comp_tid
                    await _repository.SetWriteTagVal((int)YesNo.YES, "YES", vntSlctCompTid);                    
                }
                else
                {
                    // warn msg "Blender select_comp_tid tag missing"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN18), programName, "BL-" + curblend.lngID, "SELECT_COMP_TID",
                        vntSrcTksData[intI].CompName, gstrBldrName,"", "", "", res);

                    await FinishBlend(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend, DestTank.intID, enumDebugLevel);

                    //  Call NULL_COMMAND_ACTION function
                    await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);
                    gArPrevBldData[intBldrIdx].enumCmd = null;
                    gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                    // TODO: Exit Function: Warning!!! Need to return the value
                    return rtnData;
                }
            }

            //Deselect others components of the blender at the beginning of downloading
            //  Get all the tag selection comps from abc_blender_comps
            List<AbcBlenderComps> AllBldrComps2 = await _repository.GetAllBldrComps(vntBldrsData[intBldrIdx].Id);

            foreach (AbcBlenderComps BldrCompsObj in AllBldrComps2)            
            {
                vntSelCompTIDAll = BldrCompsObj.SelectCompTid;
                bool skip = false;
                for (intNS = 1; (intNS <= intSelComp); intNS++)
                {
                    if (vntSelComp[intNS] != vntSelCompTIDAll)
                    {
                        continue;
                    }
                    else
                    {
                        skip = true;
                        break ;
                    }
                }

                if (!skip)
                {
                    //  get the tag value for this component
                    AbcTags DataRes = await _repository.GetTagNameAndVal(vntSelCompTIDAll);
                    tagSelComp.vntTagName = DataRes.Name;
                    tagSelComp.vntTagVal = DataRes.ReadValue.ToString();

                    //  Reset this tag value if it is not reset yet
                    if (tagSelComp.vntTagVal != null && (Convert.ToInt32(tagSelComp.vntTagVal) != (int)OnOff.OFF))
                    {
                        await _repository.SetWriteTagVal((int)YesNo.NO, "YES", vntSelCompTIDAll);
                    }
                }
            }

            // save current recipe (Total if multiple stations) to sp_recipe in ABC_BLEND_INTERVAL_COMPS
            for (intI = 0; intI <= (intNComps - 1); intI++)
            {
                await _repository.SetIntvRcpSp(vntSrcTksData[intI].CurRecipe, curblend.lngID, vntSrcTksData[intI].MatId, 1);
            }

            // Set Result Download value
            rtnData = RetStatus.SUCCESS;
            return rtnData;
        }
        private async Task<int> Downloading(int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend, DebugLevels enumDebugLevel)
        {
            DcsTag tagDwnlding = new DcsTag();
            DcsTag tagDwnldOK = new DcsTag();
            object vntSrcTksData;
            DcsTag[] arSrcTksVolTags = new DcsTag[0];
            DcsTag tag = new DcsTag();
            double? vntTagID;
            DestTankData DestTank = new DestTankData();
            int intNDestTks = 0;
            int intPmpIndex;
            int intDcsPumpID;
            int intStartOkTid;
            int intDCSTankNum;
            int intDCSLineupNum;
            double dblMaxVol;
            double dblDestVolume;
            string strPrdName;
            string strGrdName;
            string strSrceDestType;
            string strTankName = "";
            string strPumpName;
            string strModeTag;
            string strInServFlag;
            string strInUsePmpId;
            string strStatusPmpId;
            string strFlushTkFlag;
            string strSwgCriteria;
            string strUpdateHeelFlag;
            string strTkInUseFlag;
            string strTkEndLineFillFlag;
            string strExecute;
            string strDestSelectName;
            string strLineupName ="";
            string vntTagName;
            bool blnFlushing = false;
            object vntSelStation;
            double lngDestSelectNameTid;
            double lngTankSelTID;
            double lngTankPreselTID;
            double lngLineupSelTID;
            double lngLineupPreselTID;
            double lngPumpASelTID;
            double lngPumpBSelTID;
            double lngPumpCSelTID;
            double lngPumpDSelTID;
            double lngProdLineupId;
            double lngBldrDestPreselTkTid;
            double lngPumpXSelTID = 0;
            double lngDestTkId;
            double sngTargetVolume;
            int intLnupID;
            //  RW 23/10/2010
            int intMatID;
            double lngPumpInUseTid;
            //  RW 23/10/2010
            // RW 14-Oct-16 Gasoline Ethanol blending
            List<BldComps> vntCompData;
            List<BldProps> vntPropData;
            var res = "";

            if(enumDebugLevel == DebugLevels.High)
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, curblend.strName, "DOWNLOADING_BLEND",
                    "", "''", "", "", res);
            }

            //   Get and set downloading_tid tag
            if (vntBldrsData[intBldrIdx].DownloadingTid == null)
            {
                // warn msg "downloading_tid missing"
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN17), programName, "BL-" + curblend.lngID, "DOWNLOADING_TID", gstrBldrName,
                    "", "''", "", "", res);
                await NullCmdAction(intBldrIdx,vntBldrsData,curblend,enumDebugLevel,true);
                return 0;
            }

            AbcTags DataRes = await _repository.GetTagNameAndVal(vntBldrsData[intBldrIdx].DownloadingTid);
            tagDwnlding.vntTagName = DataRes.Name;
            tagDwnlding.vntTagVal = DataRes.ReadValue.ToString();
           
            if ((Convert.ToInt32(tagDwnlding.vntTagVal) == (int)YesNo.YES) && (gArPrevBldData[intBldrIdx].enumCmd == BlendCmds.DOWNLOAD))
            {
                await _repository.SetWriteTagVal((int)YesNo.NO,"YES", vntBldrsData[intBldrIdx].DownloadingTid);
            }

           
            // if the download command is invalid for the current blend state, or ABC->DCS
            // download is not permitted, exit sub
            if (await ProcessBldCmd(BlendCmds.DOWNLOAD, intBldrIdx, vntBldrsData, curblend, enumDebugLevel) == RetStatus.FAILURE)
            {
                return 0;
            }

            if (vntBldrsData[intBldrIdx].DownloadOkTid == null)
            {
                //  Warn msg "donwload_ok_tid missing"
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN17), programName, "BL-" + curblend.lngID, "DOWNLOAD_OK_TID", gstrBldrName,
                   "", "''", "", "", res);

                //  Call NULL_COMMAND_ACTION function
                await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);               
                gArPrevBldData[intBldrIdx].enumCmd = null;
                gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                return 0;
            }
            else
            {
                // get download OK tag value from ABC_TAGS
                DataRes = await _repository.GetTagNameAndVal(vntBldrsData[intBldrIdx].DownloadOkTid);
                tagDwnldOK.vntTagName = DataRes.Name;
                tagDwnldOK.vntTagVal = DataRes.ReadValue.ToString();
                
                if (((tagDwnldOK.vntTagVal == null)?(int)YesNo.NO:Convert.ToInt32(tagDwnldOK.vntTagVal)) == (int)YesNo.NO)
                {
                    // warning msg "Download of blend order not permitted by DCS"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN14), programName, "BL-" + curblend.lngID, tagDwnldOK.vntTagName, gstrBldrName,
                  "", "''", "", "", res);
                    // call NULL_COMMAND_ACTION function
                    await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);                   
                    gArPrevBldData[intBldrIdx].enumCmd = null;
                    gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                    return 0;                    
                }
            }

            // if blend_state is READY and there are other blends on the blender with blend_state
            // value of LOADED, ACTIVE or PAUSED, then exit sub
            if ((curblend.strState.Trim() == "READY"))
            {
                List<AbcBlends> ActvBldsData = await _repository.GetActvBldsData(vntBldrsData[intBldrIdx].Id);
                
                if (ActvBldsData .Count() > 0)
                {
                    // warning msg "New blend order downloaded not permitted.  Active blend on blender"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN15), programName, "BL-" + curblend.lngID, gstrBldrName,"",
                 "", "", "", "", res);

                    // call NULL_COMMAND_ACTION function
                    await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);                   
                    gArPrevBldData[intBldrIdx].enumCmd = null;
                    gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                    return 0;
                }
                
            }

            // check if any DCS tags for downloading are null
            if (vntBldrsData[intBldrIdx].BlendIdTid == null)
            {
                // warn msg "Blend_id_tid missing"
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN17), programName, "BL-" + curblend.lngID, "BLEND_ID_TID", gstrBldrName,
                "", "", "", "", res);                
                return 0;
            }
            else if (vntBldrsData[intBldrIdx].TargVolTid == null)
            {
                // warn msg "Targ_vol_tid missing"
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN17), programName, "BL-" + curblend.lngID, "TARG_VOL_TID", gstrBldrName,
                "", "", "", "", res);
                return 0;                
            }
            else if (vntBldrsData[intBldrIdx].TargRateTid == null)
            {
                if ((gstrRundnFlag != "YES"))
                {
                    // warn msg "Targ_rate_tid missing"
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN17), programName, "BL-" + curblend.lngID, "TARG_RATE_TID", gstrBldrName,
                    "", "", "", "", res);
                    return 0;                   
                }
            }

            if (vntBldrsData[intBldrIdx].ProductTid == null)
            {
                // warn msg "Product_tid missing"
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN17), programName, "BL-" + curblend.lngID, "PRODUCT_TID", gstrBldrName,
                    "", "", "", "", res);
                return 0;                
            }

            if (vntBldrsData[intBldrIdx].EthanolFlag == "YES")
            {
                if (curblend.vntEtohBldgReqd == null) // NULL_
                {
                    vntCompData =  await _repository.GetBldComps(curblend.lngID);
                    vntPropData = await _repository.GetBldProps(curblend.lngID);
                   
                    if (await CheckEthanol(vntBldrsData, intBldrIdx, curblend, vntCompData, vntPropData) == RetStatus.FAILURE)
                    {
                        // Ethanol blend identified but missing data
                        // Download will be cancelled                        

                        await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);
                        gArPrevBldData[intBldrIdx].enumCmd = null;
                        gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                        return 0;
                    }
                    else if ((gblnEthanolBlend == true))
                    {
                        // Set blend's ethanol blending required flag = YES
                        await _repository.SetEtohBldgReqd("YES",curblend.lngID);
                        curblend.vntEtohBldgReqd = "YES";
                    }
                    else
                    {
                        // Set blend's ethanol blending required flag = NO
                        await _repository.SetEtohBldgReqd("NO", curblend.lngID);                        
                        curblend.vntEtohBldgReqd = "NO";
                    }
                }
            }

            //'---------------- Code added for SRTF RW 23/10/2010 --------------------------'
            //'Reset all lineup sel tags
            List<AbcProdLineups> AbcProLinupData = await _repository.GetAbcProLinupData();
            //'loop all records
            lngLineupSelTID = -1;
            foreach (AbcProdLineups LineupObj in AbcProLinupData)
            {
                intLnupID = (LineupObj.Id == null)?-1: Convert.ToInt32(LineupObj.Id);

                lngLineupSelTID = (LineupObj.SelectionTid == null)?-1: Convert.ToDouble(LineupObj.SelectionTid);
                if (lngLineupSelTID != -1)
                {
                    //  Reset lineup sel tag
                    await _repository.SetWriteTagVal((int)YesNo.NO, "YES", lngLineupSelTID);
                }

                //  Get the stations used by this lineup               
                List<BldrStationsData> BldrStationsList = await _repository.GetBldrStationsData(intLnupID,vntBldrsData[intBldrIdx].Id);
                foreach (BldrStationsData BldrStationsObj in BldrStationsList)
                {
                    lngLineupSelTID = (BldrStationsObj.LineupSelTid == null)?-1: Convert.ToDouble(BldrStationsObj.LineupSelTid);
                    if (lngLineupSelTID != -1)
                    {
                        await _repository.SetWriteTagVal((int)YesNo.NO, "YES", lngLineupSelTID);                        
                    }
                }
            }

            // 'get lineup id and selection_tid from component lineups
            //'loop all records
            List<AbcCompLineups> AbcCompLinupData = await _repository.GetAbcCompLinupData();
            lngLineupSelTID = -1;
            foreach (AbcCompLineups LineupObj in AbcCompLinupData)
            {
                intLnupID = (LineupObj.Id == null) ? -1 : Convert.ToInt32(LineupObj.Id);

                lngLineupSelTID = (LineupObj.SelectionTid == null) ? -1 : Convert.ToDouble(LineupObj.SelectionTid);
                if (lngLineupSelTID != -1)
                {
                    //  Reset lineup sel tag
                    await _repository.SetWriteTagVal((int)YesNo.NO, "YES", lngLineupSelTID);
                }

                //  Get the stations used by this lineup               
                List<BldrStationsData> BldrStationsList = await _repository.GetBldrStationsData(intLnupID, vntBldrsData[intBldrIdx].Id);
                foreach (BldrStationsData BldrStationsObj in BldrStationsList)
                {
                    lngLineupSelTID = (BldrStationsObj.LineupSelTid == null) ? -1 : Convert.ToDouble(BldrStationsObj.LineupSelTid);
                    if (lngLineupSelTID != -1)
                    {
                        await _repository.SetWriteTagVal((int)YesNo.NO, "YES", lngLineupSelTID);
                    }
                }
            }

            //  Reset Blenders > Select lineup tag
            lngLineupSelTID = -1;
            List<AbcBlenders> BldrLineupTags = await _repository.GetBldrLineupTags(vntBldrsData[intBldrIdx].Id);
            
            if (BldrLineupTags.Count() > 0)
            {
                lngLineupSelTID = (BldrLineupTags[0].LineupSelTid == null)?-1: Convert.ToDouble(BldrLineupTags[0].LineupSelTid);
            }

            
            if (lngLineupSelTID != -1)
            {
                await _repository.SetWriteTagVal((int)YesNo.NO, "YES", lngLineupSelTID);               
            }

            //  Set reset all pumps in this product group to not in use           
            List<double?> AllPumpsForPrdgrp = await _repository.GetAllPumpsForPrdgrp(Convert.ToInt32(vntBldrsData[intBldrIdx].Id));
            foreach (double? PumpsForPrdgrp in AllPumpsForPrdgrp)
            {
                lngPumpInUseTid = (PumpsForPrdgrp == null)?-1: Convert.ToDouble(PumpsForPrdgrp);
                if (lngPumpInUseTid != -1)
                {
                    await _repository.SetWriteTagVal((int)YesNo.NO, "YES", lngPumpInUseTid);                   
                }              
            }


            //Download based type
            switch (gstrDownloadType)
            {
                case "COMPONENT":
                    if (await DownloadBlendComp(intBldrIdx, vntBldrsData, curblend, enumDebugLevel) == RetStatus.FAILURE)
                    {
                        return 0;
                    }
                    break;
                case "STATION":
                case "LINEUP":
                    if (await DownloadBlendStation(intBldrIdx, vntBldrsData, curblend, enumDebugLevel) == RetStatus.FAILURE)
                    {
                        return 0;
                    }
                    break;
                default:
                    if (await DownloadBlendStation(intBldrIdx, vntBldrsData, curblend, enumDebugLevel) == RetStatus.FAILURE)
                    {
                        return 0;
                    }
                    break;
            }

            //   Download blend order by copying data to DCS tags
            await _repository.SetWriteStrTagVal(curblend.strName, vntBldrsData[intBldrIdx].BlendIdTid);

            await _repository.SetWriteTagVal(Convert.ToInt32(curblend.sngTgtRate),"YES", vntBldrsData[intBldrIdx].TargRateTid);
            strPrdName = await _repository.GetCompName(curblend.intProdID);
            strGrdName = await _repository.GetGradeName(curblend.intGrdID);
            await _repository.SetWriteTagVal(Convert.ToInt32(curblend.sngTgtVol),"YES", vntBldrsData[intBldrIdx].TargVolTid);
            //Added to concatenate the Product name and Grade and download descr
            await _repository.SetWriteStrTagVal(strPrdName, vntBldrsData[intBldrIdx].ProductTid);
            //  write the grade_tid is not null, then download the description alone
            if (vntBldrsData[intBldrIdx].GradeTid != null)
            {
                await _repository.SetWriteStrTagVal(strGrdName, vntBldrsData[intBldrIdx].GradeTid);
            }

            if (vntBldrsData[intBldrIdx].BlendDescTid != null)
            {
                if (vntBldrsData[intBldrIdx].GradeTid == null)
                {
                    await _repository.SetWriteStrTagVal((strPrdName + ("//" + (strGrdName + ("//" + curblend.strBldDesc)))), vntBldrsData[intBldrIdx].BlendDescTid);
                }
                else
                {
                    // If grade_tid is not null, then download the description alone
                    await _repository.SetWriteStrTagVal(curblend.strBldDesc, vntBldrsData[intBldrIdx].BlendDescTid);
                }

            }
            
            if (enumDebugLevel >= DebugLevels.Medium)
            {
                // get BLEND_ID_TID tag name
                vntTagName = await _repository.GetTagName(vntBldrsData[intBldrIdx].BlendIdTid);
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG8), programName, cstrDebug, curblend.strName, curblend.strName,
                   vntTagName, "", "", "", res);

                // get TARG_VOL_TID tag name
                vntTagName = await _repository.GetTagName(vntBldrsData[intBldrIdx].TargVolTid);
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG10), programName, cstrDebug, curblend.strName, curblend.sngTgtVol.ToString(),
                  vntTagName, "", "", "", res);

                // get TARG_RATE_TID tag name
                vntTagName = await _repository.GetTagName(vntBldrsData[intBldrIdx].TargRateTid);
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG10), programName, cstrDebug, curblend.strName, curblend.sngTgtRate.ToString(),
                  vntTagName, "", "", "", res);

                // get PRODUCT_TID tag name
                vntTagName = await _repository.GetTagName(vntBldrsData[intBldrIdx].BlendIdTid);
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG9), programName, cstrDebug, curblend.strName, strPrdName,
                  vntTagName, "", "", "", res);               
            }

            //Get Lineup tags from blenders
            lngTankSelTID = -1;
            lngTankPreselTID = -1;
            lngLineupSelTID = -1;
            lngLineupPreselTID = -1;
            lngPumpASelTID = -1;
            lngPumpBSelTID = -1;
            lngPumpCSelTID = -1;
            lngPumpDSelTID = -1;
            
            BldrLineupTags = await _repository.GetBldrLineupTags(vntBldrsData[intBldrIdx].Id);
            if (BldrLineupTags.Count() > 0)
            {
                lngTankSelTID = (BldrLineupTags[0].TankSelTid == null) ? -1 : Convert.ToDouble(BldrLineupTags[0].TankSelTid);
                lngTankPreselTID = (BldrLineupTags[0].TankPreselTid == null) ? -1 : Convert.ToDouble(BldrLineupTags[0].TankPreselTid);
                lngLineupSelTID = (BldrLineupTags[0].LineupSelTid == null) ? -1 : Convert.ToDouble(BldrLineupTags[0].LineupSelTid);
                lngLineupPreselTID = (BldrLineupTags[0].LineupPreselTid == null) ? -1 : Convert.ToDouble(BldrLineupTags[0].LineupPreselTid);
                lngPumpASelTID = (BldrLineupTags[0].PumpaSelTid == null) ? -1 : Convert.ToDouble(BldrLineupTags[0].PumpaSelTid);
                lngPumpBSelTID = (BldrLineupTags[0].PumpbSelTid == null) ? -1 : Convert.ToDouble(BldrLineupTags[0].PumpbSelTid);
                lngPumpCSelTID = (BldrLineupTags[0].PumpcSelTid == null) ? -1 : Convert.ToDouble(BldrLineupTags[0].PumpcSelTid);
                lngPumpDSelTID = (BldrLineupTags[0].PumpdSelTid == null) ? -1 : Convert.ToDouble(BldrLineupTags[0].PumpdSelTid);
            }

            //Get the abc_dest_tanks.flush_tk_flag to loop through all dest tanks for this blend
            List<AbcBlendDest> DestTkFlags = await _repository.GetDestTkFlags(curblend.lngID);
            List<AbcBlendDest> DestTkFlagsFlt = new List<AbcBlendDest>();
            if (DestTkFlags.Count() > 0)
            {
                blnFlushing = false;
                //'Find if flush_tk_flag=YES for at least one of the records
                DestTkFlagsFlt = DestTkFlags.Where<AbcBlendDest>(row => row.FlushTkFlag == "YES").ToList();
               if(DestTkFlagsFlt.Count() > 0)
                {
                    blnFlushing = true;
                }

                intNDestTks = DestTkFlags.Count();               
            }
            List<AbcTanks> DataTankID = new List<AbcTanks>();
            foreach (AbcBlendDest DestTkFlagObj in DestTkFlags)
            {
                lngDestTkId = DestTkFlagObj.TankId;
                strFlushTkFlag = DestTkFlagObj.FlushTkFlag;
                strTkInUseFlag = DestTkFlagObj.InUseFlag;
                strTkEndLineFillFlag = DestTkFlagObj.EndLinefillTkFlag;
                lngProdLineupId = DestTkFlagObj.LineupId;


                DestTank.intID = Convert.ToInt32(lngDestTkId);

                //Update heel volume of in used dest tank
                if (strTkInUseFlag == "YES")
                {
                    // get the update_heel_flag from abc_blends to decide whether or not
                    //'the heel_vol should be updated

                    List<AbcBlends> BlendState = await _repository.GetBlendState(curblend.lngID);
                    if (BlendState.Count() > 0)
                    {
                        //'Update the abc_blends.pending state ending_state for this blend
                        strUpdateHeelFlag = (BlendState[0].UpdateHeelFlag == null) ? "YES" : BlendState[0].UpdateHeelFlag;
                        if (strUpdateHeelFlag == "YES")
                        {
                            List<DCSProdLineupNum> DCSProdLineupNumData = await _repository.GetDCSProdLineupNum(lngProdLineupId);

                            dblDestVolume = (DCSProdLineupNumData[0].DestLineVolume == null) ? 0 : Convert.ToDouble(DCSProdLineupNumData[0].DestLineVolume);

                            DestTank.vntHeelVol = Convert.ToDouble(await _repository.GetHeelVol(DestTank.intID)) + dblDestVolume;
                            //'set heel volume in dest tank
                            await _repository.SetHeelVol(DestTank.vntHeelVol, curblend.lngID, DestTank.intID);
                        }
                    }
                }

                if ((intNDestTks == 1) || (strFlushTkFlag == "YES" && strTkInUseFlag == "YES") || (intNDestTks > 1 && strTkInUseFlag == "NO" && strFlushTkFlag == "YES") ||
                    (blnFlushing == false && strTkInUseFlag == "YES"))
                {
                    //'get and set prod lineup selection_tid AND get the abc_blender_dest.dest_select_name_tid
                    List<AbcBlenderDest> BldrDestSelTid = await _repository.GetBldrDestSelTid(vntBldrsData[intBldrIdx].Id, DestTank.intID);
                    vntTagID = BldrDestSelTid[0].SelectionTid;

                    if (vntTagID != null)
                    {
                        await _repository.SetWriteTagVal((int)YesNo.YES, "YES", vntTagID);
                    }
                    // selection Tank index, lineup index to DCS
                    if (lngTankSelTID != -1)
                    {
                        // Get DCS Tank Num for this tank
                        intDCSTankNum = -1;
                        List<AbcTanks> TankNum = await _repository.GetTankNum(DestTank.intID);

                        if (TankNum.Count() > 0)
                        {
                            intDCSTankNum = (TankNum[0].DcsTankNum == null) ? -1 : Convert.ToInt32(TankNum[0].DcsTankNum);
                            strTankName = TankNum[0].Name;
                        }

                        if (intDCSTankNum != -1)
                        {
                            await _repository.SetWriteTagVal(intDCSTankNum, "YES", lngTankSelTID);
                        }
                        else
                        {
                            // TANK INDEX IS NULL IN ^1 TABLE. TANK ^2 WILL NOT BE SELECTED IN DCS
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN97), programName, "BL-" + curblend.lngID, "ABC_TANKS", strTankName,
                            "", "", "", "", res);
                        }

                    }

                    //Download Lineup sel/presel indexes to DCS
                    if (lngLineupSelTID != -1)
                    {
                        // get DCS Lineup index if selected lineup id is not null
                        if (lngProdLineupId != -1)
                        {
                            List<DCSProdLineupNum> DCSProdLineupNumData = await _repository.GetDCSProdLineupNum(lngProdLineupId);

                            intDCSLineupNum = Convert.ToInt32(DCSProdLineupNumData[0].DCSLineUpNum);
                            strLineupName = DCSProdLineupNumData[0].LineUpName;
                        }
                        else
                        {
                            intDCSLineupNum = -1;
                        }

                        if (intDCSLineupNum != -1)
                        {
                            // Write the Selected DCS LINEUP number to the DCS
                            await _repository.SetWriteTagVal(intDCSLineupNum, "YES", lngLineupSelTID);
                        }
                        else
                        {
                            // IN BLEND ^1, DEST ^2, PROD DCS LINEUP NUM IS NULL FOR LINEUP ^2.  CMD SEL/PRESEL IGNORED
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN98), programName, "BL-" + curblend.lngID, curblend.strName, strTankName,
                            strLineupName, "", "", "", res);

                        }

                    }

                    //Download swing_target_vol and swing_exist tags values to DCS
                    if (((intNDestTks > 1) && ((strTkInUseFlag == "NO") && (strFlushTkFlag == "YES"))))
                    {
                        if (vntBldrsData[intBldrIdx].SwingVolTid != null)
                        {
                            // For PRODUCT tanks obtain the records from abc blend swings
                            // Get the blend swing data for a specific from product tank

                            List<BlendSwingsData> BlendSwingsDataList = await _repository.BlendSwingsData("PRODUCT", DestTank.intID, curblend.lngID);
                            if (BlendSwingsDataList.Count() == 0)
                            {
                                // Turn Off the swing exist flag in DCS
                                if (vntBldrsData[intBldrIdx].SwingExistTid != null)
                                {
                                    await _repository.SetWriteTagVal((int)YesNo.NO, "YES", vntBldrsData[intBldrIdx].SwingExistTid);
                                }
                            }

                            foreach (BlendSwingsData BlendSwingsDataObj in BlendSwingsDataList)
                            {
                                strSwgCriteria = (BlendSwingsDataObj.CriteriaName == null) ? "" : BlendSwingsDataObj.CriteriaName;
                                if ((strSwgCriteria == "BLEND VOLUME"))
                                {
                                    // Turn ON the swing exist flag in DCS
                                    if (vntBldrsData[intBldrIdx].SwingExistTid != null)
                                    {
                                        await _repository.SetWriteTagVal((int)YesNo.YES, "YES", vntBldrsData[intBldrIdx].SwingExistTid);
                                    }

                                    // Get the data from the from tank id
                                    List<ASTankID> ASTankIDData = await _repository.GetASTankID(DestTank.intID);

                                    dblMaxVol = (ASTankIDData[0].MaxVol == null) ? 0 : Convert.ToDouble(ASTankIDData[0].MaxVol);

                                    sngTargetVolume = (BlendSwingsDataObj.CriteriaNumLmt == null) ? dblMaxVol : Convert.ToDouble(BlendSwingsDataObj.CriteriaNumLmt);
                                    // Download the swing target volume in DCS
                                    await _repository.SetWriteTagVal(Convert.ToInt32(sngTargetVolume), "YES", vntBldrsData[intBldrIdx].SwingVolTid);
                                }
                                else
                                {
                                    // Turn Off the swing exist flag in DCS
                                    if (vntBldrsData[intBldrIdx].SwingExitTid != null)
                                    {
                                        await _repository.SetWriteTagVal((int)YesNo.NO, "YES", vntBldrsData[intBldrIdx].SwingExitTid);
                                    }
                                }
                            }
                        }
                    }
                    // ************
                }
                else if ((intNDestTks > 1 && strTkInUseFlag == "YES") || ((intNDestTks > 1) && ((strTkInUseFlag == "NO") && (blnFlushing == false))))
                {
                    // Get and set prod lineup Preselection_tid
                    strTankName = await _repository.GetTankName(DestTank.intID);
                    List<AbcBlenderDest> BldrDestSelTid =  await _repository.GetBldrDestSelTid(vntBldrsData[intBldrIdx].Id,DestTank.intID);
                    lngBldrDestPreselTkTid = (BldrDestSelTid[0].PreselectionTid == null)?-1:Convert.ToDouble(BldrDestSelTid[0].PreselectionTid);                   

                    if (lngBldrDestPreselTkTid != -1)
                    {
                        await _repository.SetWriteTagVal((int)YesNo.YES, "YES", lngBldrDestPreselTkTid);                        
                    }

                    // Preselect a tank in the DCS
                    if (lngTankPreselTID != -1)
                    {                       
                        // Get DCS Tank Num for this tank
                        intDCSTankNum = -1;
                        List<AbcTanks> TankNum = await _repository.GetTankNum(DestTank.intID);
                        
                        if (TankNum.Count() > 0)
                        {
                            intDCSTankNum = (TankNum[0].DcsTankNum == null)?-1:Convert.ToInt32(TankNum[0].DcsTankNum);
                            strTankName = TankNum[0].Name;
                        }

                        if (intDCSTankNum != -1)
                        {
                            await _repository.SetWriteTagVal(intDCSTankNum, "YES", lngTankPreselTID);                            
                        }
                        else
                        {
                            // TANK INDEX IS NULL IN ^1 TABLE. TANK ^2 WILL NOT BE SEL/PRESEL IN DCS
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN97), programName, "BL-" + curblend.lngID, "ABC_TANKS", strTankName,
                            "", "", "", "", res);
                        }

                    }

                    if (lngLineupPreselTID != -1)
                    {
                        // get DCS Lineup index if presel lineup id is not null
                        if (lngProdLineupId != -1)
                        {
                            List<DCSProdLineupNum> DCSProdLineupNumData = await _repository.GetDCSProdLineupNum(lngProdLineupId);
                            intDCSLineupNum = Convert.ToInt32(DCSProdLineupNumData[0].DCSLineUpNum);
                            strLineupName = DCSProdLineupNumData[0].LineUpName;                            
                        }
                        else
                        {
                            intDCSLineupNum = -1;
                        }

                        if ((intDCSLineupNum != -1))
                        {
                            // Write the Preselected DCS LINEUP number to the DCS
                            await _repository.SetWriteTagVal(intDCSLineupNum, "YES", lngLineupPreselTID);
                        }
                        else
                        {
                            // IN BLEND ^1, DEST ^2, PROD DCS LINEUP NUM IS NULL FOR LINEUP ^2.  CMD SEL/PRESEL IGNORED
                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN98), programName, "BL-" + curblend.lngID, curblend.strName, strTankName,
                            strLineupName, "", "", "", res);
                        }
                    }
                }
               
                // Download the Ship/Tank Name of the selected tank in the ABC
                if (blnFlushing == true || (blnFlushing == false && strTkInUseFlag == "YES"))
                {
                    // To skip the checking of dest tank data if source_destn_type <> "TANK"
                    DataTankID = await _repository.GetDataTankID(DestTank.intID);
                    
                    strSrceDestType = (DataTankID[0].SourceDestnType == null)?"": DataTankID[0].SourceDestnType;                    

                    if (strSrceDestType != "TANK")
                    {
                        // get and set the abc_blender_dest.dest_select_name_tid
                       List<AbcBlenderDest> BldrDestSelTid = await _repository.GetBldrDestSelTid(vntBldrsData[intBldrIdx].Id, DestTank.intID);
                        lngDestSelectNameTid = (BldrDestSelTid[0].DestSelectNameTid == null)?-1:Convert.ToDouble(BldrDestSelTid[0].DestSelectNameTid);

                        List<AbcBlendDest> TkDestData = await _repository.GetTkDestData(curblend.lngID,DestTank.intID);
                        
                        strDestSelectName = (TkDestData[0].DestSelectName == null)?strSrceDestType: TkDestData[0].DestSelectName;                        
                        
                        if ((intNDestTks == 1) || (strFlushTkFlag == "YES" && strTkInUseFlag == "YES") ||
                            (blnFlushing == true && intNDestTks > 1 && strFlushTkFlag == "YES" && strTkInUseFlag == "NO")
                                    || (blnFlushing == false))
                        {
                            if (lngDestSelectNameTid != -1)
                            {
                                // write the string name to the DCS tag Id
                                await _repository.SetWriteStrTagVal(strDestSelectName,lngDestSelectNameTid);
                            }

                        }

                    }

                    if ((gstrDownloadType != "LINEUP"))
                    {
                        //  Get and set the product lineups pumps
                        // get pump data, including inuse_tag_id for the product
                        if ((intNDestTks == 1) || (strFlushTkFlag == "YES" && strTkInUseFlag == "YES") ||
                            (intNDestTks > 1 && strTkInUseFlag == "NO" && strFlushTkFlag == "YES") || 
                            (intNDestTks > 1 && strTkInUseFlag == "YES" && blnFlushing == false))
                        {
                            //Download product lineup pumps
                            if (lngProdLineupId != -1)
                            {
                                //Download pumps based on blenders                              
                                List<AbcPumps> ProdPumpsData = await _repository.GetProdPumpsData(lngProdLineupId);                                
                                intPmpIndex = 0;
                                foreach (AbcPumps ProdPumpObj in ProdPumpsData)                                
                                {
                                    switch (intPmpIndex)
                                    {
                                        case 0:
                                            lngPumpXSelTID = lngPumpASelTID;
                                            break;
                                        case 1:
                                            lngPumpXSelTID = lngPumpBSelTID;
                                            break;
                                        case 2:
                                            lngPumpXSelTID = lngPumpCSelTID;
                                            break;
                                        case 3:
                                            lngPumpXSelTID = lngPumpDSelTID;
                                            break;
                                    }
                                    strPumpName = (ProdPumpObj.Name == null)?"-1": ProdPumpObj.Name;
                                    strModeTag = (ProdPumpObj.ModeTid == null) ? "-1" : ProdPumpObj.ModeTid.ToString();
                                    strInServFlag = ProdPumpObj.InSerFlag;
                                    intDcsPumpID = (ProdPumpObj.DcsPumpId == null) ? -1 : Convert.ToInt32(ProdPumpObj.DcsPumpId);
                                    strInUsePmpId = (ProdPumpObj.InuseTagId == null) ? "-1" : ProdPumpObj.InuseTagId.ToString();
                                    // Skip this calculation if there are not pumps preconfigured for the lineup id
                                    if ((strInServFlag != "YES"))
                                    {
                                        // warn msg "PUMP ^1 NOT LISTED IN SERVICE IN ABC OR NOT IN AUTO MODE IN DCS.  DOWNLOADING CANCELED
                                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN22), programName, "BL-" + curblend.lngID, strPumpName, "",
                                        "", "\\", "", "", res);
                                        
                                        // set ABC_BLENDS.PENDING_STATE to null
                                        await NullCmdAction(intBldrIdx,vntBldrsData,curblend,enumDebugLevel,true);
                                        gArPrevBldData[intBldrIdx].enumCmd = null;
                                        gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;
                                        return 0;
                                    }

                                    if (strModeTag != "-1")
                                    {
                                        DataRes = await _repository.GetTagNameAndVal(Convert.ToDouble(strModeTag));
                                        tag.vntTagName = DataRes.Name;
                                        tag.vntTagVal = DataRes.ReadValue.ToString();
                                       
                                        if (Convert.ToInt32(tag.vntTagVal) != (int)OnOff.ON_)
                                        {
                                            // warn msg "PUMP ^1 NOT LISTED IN SERVICE IN ABC OR NOT IN AUTO MODE IN DCS.  DOWNLOADING CANCELED
                                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN22), programName, "BL-" + curblend.lngID, strPumpName, "",
                                            "", "\\", "", "", res);

                                            // set ABC_BLENDS.PENDING_STATE to null
                                            await NullCmdAction(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, true);
                                            gArPrevBldData[intBldrIdx].enumCmd = null;
                                            gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = cdteNull;                                            
                                            return 0;
                                        }

                                    }

                                    if (strInUsePmpId != "-1")
                                    {
                                        // checks and warnings
                                        strStatusPmpId = (ProdPumpObj.StatusTagId == null)?"-1": ProdPumpObj.StatusTagId.ToString();
                                        if (strStatusPmpId != "-1")
                                        {
                                            DataRes = await _repository.GetTagNameAndVal(Convert.ToDouble(strStatusPmpId));
                                            tag.vntTagName = DataRes.Name;
                                            tag.vntTagVal = DataRes.ReadValue.ToString();
                                            
                                            if (Convert.ToInt32(tag.vntTagVal) == (int)OnOff.ON_)
                                            {
                                                // warn msg "Pump ^1 is currently running"
                                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN21), programName, "BL-" + curblend.lngID, strPumpName, "",
                                                "", "\\", "", "", res);                                                                                                
                                            }

                                        }

                                        // Set the selection in DCS
                                        await _repository.SetWriteTagVal((int)YesNo.YES, "YES", Convert.ToDouble(strInUsePmpId));                                        
                                    }

                                    if (lngPumpXSelTID != -1)
                                    {
                                        if (intDcsPumpID != -1)
                                        {
                                            await _repository.SetWriteTagVal(intDcsPumpID, "YES", lngPumpXSelTID);                                            
                                        }
                                        else
                                        {
                                            // Warn msg "DCS PUMP ID FOR PUMP ^1 not configured.  Command selection Ignored"
                                            await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN99), programName, "BL-" + curblend.lngID, strPumpName, "",
                                               "", "\\", "", "", res);                                            
                                        }
                                    }

                                    intPmpIndex = (intPmpIndex + 1);
                                    
                                }

                            }// Lineup is null
                        }
                    }// lineup DOWNLOAD  BASED
                }

            }

            await _repository.SetWriteTagVal((int)YesNo.YES, "YES", vntBldrsData[intBldrIdx].DownloadingTid);
            await InitDatabase(Convert.ToInt32(vntBldrsData[intBldrIdx].Id), curblend.lngID, curblend.strName, enumDebugLevel);

            //   Issue a warning message if the Optimize_flag is ON and the Autodownload_flag is NO or
            //    the if the dest_type is TANK or SHIP and the tank Control_flag is OFF
            DestTank.strFixHeelFlg = await _repository.GetDestTankData(curblend.lngID,DestTank.intID,DestTank.vntHeelVol);

            DataTankID = await _repository.GetDataTankID(DestTank.intID);
            
            strSrceDestType = (DataTankID[0].SourceDestnType == null)?"": DataTankID[0].SourceDestnType;
            
            if ((vntBldrsData[intBldrIdx].OptimizeFlag == "YES") && (curblend.strCtlMode != "AUTO"))
            {
                // Log message: ^1 FLAG IS NOT ON FOR BLEND ^2 ON BLENDER ^3. OPTIMIZER WILL ^4
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN83), programName, cstrDebug, "AUTO-DOWNLOAD", curblend.strName,
                 gstrBldrName, "NOT DOWNLOAD SP TO DCS", "", "", res);
            }
            else if (((DestTank.strFixHeelFlg != "YES") && ((strSrceDestType == "TANK") || (strSrceDestType == "SHIP"))))
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN83), programName, cstrDebug, "TANK CONTROL", curblend.strName,
                 gstrBldrName, "NOT CORRECT THE TANK HEEL", "", "", res);
            }

            // There is no DOWNLOAD command in DCS. It is only a dummy command created
            // just for ABC program logic, and is not downloaded to DCS
            // update Prev_blend_cmd and DCS_cmd_time
            gArPrevBldData[intBldrIdx].enumCmd = BlendCmds.DOWNLOAD;
            gArPrevBldData[intBldrIdx].arCmdTime[(int)BlendCmds.DOWNLOAD] = DateTime.Now;
            // Dec. 12, 02:Update the read value from abc_tags
            intStartOkTid = (vntBldrsData[intBldrIdx].StartOkTid == null)?-1: Convert.ToInt32(vntBldrsData[intBldrIdx].StartOkTid);
            if (intStartOkTid != -1)
            {
                // Update abc_Tags.read_value of download OK tag
                await _repository.SetReadTagVal(intStartOkTid);
            }

            return 0;
        }
        private async Task<int> InitDatabase(int intBldrID, double lngBldID, string strBldName, DebugLevels enumDebugLevel)
        {
            var res = "";
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            if (enumDebugLevel == DebugLevels.High)
            {
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, strBldName, "INITIALIZE_DATABASE",
                 "", "", "", "", res);
            }


            // clear ABC_BLENDERS.PROGRAM_ERROR for the blender
            await _repository.SetBlenderErrFlag("",intBldrID,"");
            gDteCurTime = await _repository.GetCurTime();

            // create new records for interval #0 if not already exist
            // use CheckNewIntvRecs in case 0 exists
            //    CreateNewIntvRecs lngBldID, 0, enumDebugLevel
            await _shared.CheckNewIntvRecs(lngBldID,0,enumDebugLevel,gDteCurTime);
            // create new records for interval #1
            //    CreateNewIntvRecs lngBldID, 1, enumDebugLevel
            await _shared.CheckNewIntvRecs(lngBldID, 1, enumDebugLevel, gDteCurTime);
            
            return 0;
        
        }
        public async Task<int> ProcessBlenders()
        {
            string strDebugFlag; //'debug flag string ("YES" or "NO")
            bool blnArraysSet = false;
            double lngBldActiveID = 0;
            DebugLevels enumDebugLevel = 0, enumBldrDbgLevel = 0;//'integer value of debug level for low, medium and high
            string strBldrDbgFlag = "";
            string strBlendState = "";
            DcsTag tagRbcMode = new DcsTag();
            List<AbcBlenders> vntBldrsData = new List<AbcBlenders>();
            int intNBldrs = 0, intNBlds = 0, intNActiveBlds = 0, intI = 0, intJ = 0;
            Console.WriteLine("getting program data to check process enabled for blend monitor");
            AbcPrograms Data = await _repository.ProcessEnabled();
            strDebugFlag = Data.DebugFlag;
            enumDebugLevel = (DebugLevels)Convert.ToInt32(Data.DebugLevel);
            DateTime gDteCurTime = DateTime.Now;
            string vntOptRunState = "";
            string res = "";
            if (Data.EnabledFlag.ToUpper() == "YES")
            {
                Console.WriteLine("enabled");
                Console.WriteLine("log start time");
                await _repository.SetStartTime();
                if (enumDebugLevel == DebugLevels.High)
                {
                   
                    //await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, "", "PROCESS_BLENDERS", "", "", "", "", res);
                    //  intNBldrs = ABCdataEnv.rscmdGetBlendersData.RecordCount
                }

                vntBldrsData = await _repository.GetBlenders();
                intNBldrs = vntBldrsData.Count;
                Console.WriteLine("getting all blenders Data - count - " + intNBldrs);
                // ---- MAIN code------start-----
                if (intNBldrs > 0)
                {
                    //LINEPROP at the middle of the interval has been suppressed.                       
                    gintSkipCycleBmon = new int[intNBldrs];
                    gDteCompSwgCmdTime = new DateTime[intNBldrs];
                    gDteProdSwgCmdTime = new DateTime[intNBldrs];
                    gblnMsgLogged = new bool[intNBldrs];
                    gblnSampleMsgLogged = new bool[intNBldrs];
                    gblnProdSwgTimeIn = new bool[intNBldrs];
                    gblnCompSwgTimeIn = new bool[intNBldrs];
                    gblnBmonStarted = new bool[intNBldrs];
                    // '       ' This intVar allows recalc of LINEPROP after CalcBias all the way to first interval
                    //'       gint1stSampleBias = new int[intNBldrs - 1];
                    //       'First member is for blend id, second is for target vol/target vol/Transfer Vol

                    gArPrevTargetVol = new double[intNBldrs, intNBldrs + 1];
                    gArPrevTargetRate = new double[intNBldrs, intNBldrs + 1];
                    gArPrevTransferVol = new double[intNBldrs, intNBldrs + 1];

                    // initialize variables per blender
                    for (intI = 0; (intI < intNBldrs); intI++)
                    {
                        //  LINEPROP at the middle of the interval has been suppressed.
                        // Variable to calc the component volumes in the middle of an interval
                        // gIntProcLineProp(intI) = 1
                        // Initializing flag to skip a cycle in the Bmon if Vol Tags
                        // have different value time
                        gintSkipCycleBmon[intI] = 0;
                        gblnProdSwgTimeIn[intI] = false;
                        gblnCompSwgTimeIn[intI] = false;
                        gblnBmonStarted[intI] = true;
                        gDteCompSwgCmdTime[intI] = gDteCurTime;
                        gDteProdSwgCmdTime[intI] = gDteCurTime;
                        // Set the flag to issue the message about ABC-DCS comm
                        gblnMsgLogged[intI] = false;
                        gblnSampleMsgLogged[intI] = false;
                    }
                }
                // ---- MAIN code------end-----

                if (!blnArraysSet)
                {
                    gArPrevBldData = new PrevBlendData[intNBldrs];
                    gArBldFinishTime = new DateTime[intNBldrs];
                    gArAnzOfstSvd = new bool[intNBldrs];
                    gArCompValTime = new ValTime[intNBldrs];
                    // 'BDS 11-May-2012 PQ-D0074 Array to record times when station current volumes are updated
                    gArStnValTime = new ValTime[intNBldrs];
                    // 'BDS 11-May-2012 PQ-D0074
                    gArSrcTkPrpValTime = new ValTime[intNBldrs];
                    // '     gArBldEqpTags[intNBldrs - 1]
                    gArRbcWdog = new RbcWatchDog[intNBldrs];
                    gArAnzDelay = new DateTime[intNBldrs];//the anz_start_delay of the blenders
                    gblnNOProcActBlds = new bool[intNBldrs];//the anz_start_delay of the blenders
                    gblnOptimizing = new bool[intNBldrs];//Optimizing flag
                    gintNameCount = new int[intNBldrs]; //counter of retries when ABC<>RBC name
                    gblnSetOptNowFlag = new bool[intNBldrs]; //This flag sets the TQI_NOW_FLAG='YES' when the BMON starts (1st Time]
                    gblnPrevStatePaused = new bool[intNBldrs];//create new intervals only two cycles of Bmon after paused state
                                                              // '      gint1stSampleBias[intNBldrs - 1]  This intVar allows recalc of LINEPROP after CalcBias all the way to first interval

                    for (int i = 0; i < intNBldrs; i++)
                    {
                        gArPrevBldData[i] = new PrevBlendData();
                        gArCompValTime[i] = new ValTime();
                        gArStnValTime[i] = new ValTime();
                        gArSrcTkPrpValTime[i] = new ValTime();
                        gArRbcWdog[i] = new RbcWatchDog();                        
                    }
                    gblnFirstBiasCalc = new bool[intNBldrs, 1]; //'initialize
                    gblnBiasRedimDone = new bool[intNBldrs];
                    gArPrevRBCState = new string[intNBldrs];

                    blnArraysSet = true;
                }

                Console.WriteLine("get project default data");
                var resData = await _repository.getProjDefaults(gProjDfs);
                gProjDfs = resData.Item1;
                gdblProjCycleTime = resData.Item2;

                //'   Following code moved up from below in order to make changes for PQ-D0070 as efficient as possible RW 19-Apr-2012
                if (gProjDfs.vntRcpTolr == null)
                {
                    Console.WriteLine("recipe tolerance is null, taken as 1.0");
                    //'warn msg "Null default recipe tolerance, taken as 1.0"                    
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN1), programName, cstrGen, "", "", "", "''", "", "", res);
                    gProjDfs.vntRcpTolr = 1;
                }
                gstrProjName = gProjDfs.strProjName;
                gsngFGEEtoh = gProjDfs.sngFGEEtoh;
                gsngMinEtoh = gProjDfs.sngMinEtoh;
                gstrLIMSSeparateProps = gProjDfs.strLIMSSeparateProps;
                Console.WriteLine("Iterating through all blenders");
                for (intI = 0; intI < intNBldrs; intI++)
                {
                    //Flag to skip the Proccess calc of active blends when the
                    //Tmon is finishing the TQI and the DCS state is Complete
                    gblnNOProcActBlds[intI] = false;

                    gblnEthanolBlend = false;
                    //'*** added blender in service check ***
                    //'Ignore blenders with IN_SER_FLAG <> 'YES'
                    if (vntBldrsData[intI].InSerFlag != "YES")
                    {
                        Console.WriteLine("if inserFlag for blend is Yes move to next blend");
                        NextBlend();
                        continue;
                    }


                    // ' If woken up to process pending blends only, ensure only pending blends are processed
                    // ' ie. check if this blender has any blends with pending state <> NULL and <> OPTIMIZING
                    if (gPendingBlendsOnly == true)
                    {
                        int count = await _repository.ChkPendingOnBldr(vntBldrsData[intI].Id);
                        Console.WriteLine("check if this blender has any blends with pending state <> NULL and <> OPTIMIZING - count - " + count);
                        Console.WriteLine("if count 0 move to next blend");
                        if (count == 0)
                        {
                            Console.WriteLine("move to next blend");
                            NextBlend();
                            continue;
                        }
                    }

                    // 'log start time of the first blender header
                    //ABCdataEnv.cmdGetCurTime gDteCurTime
                    gDteCurTime = DateTime.Now;
                    Console.WriteLine("log start time of the first blender header to messages");
                    if (enumDebugLevel == DebugLevels.High)
                    {                        
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG1), programName, cstrDebug, gDteCurTime.ToString(), "", "", "", "", "", res);
                    }

                    Console.WriteLine("get debug flag and level for the blender - id - " + vntBldrsData[intI].Id);
                    var dataRes = await _repository.GetBlenderDebugs(programName, strDebugFlag, enumDebugLevel,
                        Convert.ToInt32(vntBldrsData[intI].Id), strBldrDbgFlag, enumBldrDbgLevel, gstrBldrName);
                    enumBldrDbgLevel = dataRes.Item1;
                    strBldrDbgFlag = dataRes.Item2;
                    gstrBldrName = dataRes.Item3;
                    Console.WriteLine("debugLevel - " + enumBldrDbgLevel + "debugFlag - " + strBldrDbgFlag + "blender name - " + gstrBldrName);

                    Console.WriteLine("getting data for the blend on this blender, with blend_state LOADED,'ACTIVE, or PAUSED");
                    List<AbcBlends> ActvBldsData = await _repository.GetActvBldsData(vntBldrsData[intI].Id);
                    intNBlds = ActvBldsData.Count;
                    Console.WriteLine("if more than 1 blends found for blender then move to next blend - blends found - count - " + intNBlds);
                    if (intNBlds > 1)
                    {
                        // 'warning msg "More than one ^1 blends found on blender ^1. ^2 Action has been Canceled"                                   
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN2), programName, cstrGen, "ACTIVE", gstrBldrName, "PROCESSING", "", "", "", res);
                        NextBlend();
                        Console.WriteLine("Move to next blend");
                        continue;
                    }
                    else if (intNBlds == 1)
                    {
                        Console.WriteLine("if there only 1 blend then copy data to current blend(curblend)");

                        curblend.lngID = ActvBldsData[0].Id;
                        curblend.strName = ActvBldsData[0].Name;
                        curblend.sngTgtVol = ActvBldsData[0].TargetVol;

                        Console.WriteLine("Recalculate Target Volume if needed for flushing - volume - " + curblend.sngTgtVol);
                        curblend.sngTgtVol = await CalcTargVol(curblend.lngID, curblend.sngTgtVol);
                        Console.WriteLine("claculated value - " + curblend.sngTgtVol);

                        curblend.sngTgtRate = ActvBldsData[0].TargetRate;
                        curblend.intGrdID = Convert.ToInt32(ActvBldsData[0].GradeId);
                        curblend.vntMinVol = ActvBldsData[0].MinVol;
                        curblend.vntMaxVol = ActvBldsData[0].MaxVol;
                        curblend.vntMinRate = ActvBldsData[0].MinRate;
                        curblend.vntMaxRate = ActvBldsData[0].MaxRate;
                        curblend.dblCorrFac = ActvBldsData[0].CorrectionFactor;
                        curblend.strCtlMode = ActvBldsData[0].ControlMode;
                        curblend.sngCurVol = (ActvBldsData[0].CurrentVol == null) ? 0 : ActvBldsData[0].CurrentVol;
                        curblend.dteActualStart = (ActvBldsData[0].ActualStart == null) ? cdteNull : Convert.ToDateTime(ActvBldsData[0].ActualStart);
                        curblend.intProdID = Convert.ToInt32(ActvBldsData[0].ProductId);
                        curblend.vntPendSt = ActvBldsData[0].PendingState;
                        //Added to handle the description download to the RBC
                        curblend.strBldDesc = (ActvBldsData[0].Description == null) ? "" : ActvBldsData[0].Description;

                        gArPrevBldData[intI].strState = ActvBldsData[0].BlendState.Trim();
                        curblend.lngPrevBldId = (ActvBldsData[0].PreviousBlendId == null) ? 0 : ActvBldsData[0].PreviousBlendId;
                        curblend.strIgnLineConstr = ActvBldsData[0].IgnoreLineConstraints;
                        curblend.strRampingActFlag = ActvBldsData[0].RampingActFlag;
                        curblend.strBiasOverrideFlag = ActvBldsData[0].BiasOverrideFlag;
                        //'Flag would be either YES or NO for an active/loaded/paused blend
                        curblend.vntEtohBldgReqd = ActvBldsData[0].EthanolBldgReqdFlag;
                    }
                    else
                    {
                        List<AbcBlends> ReadyBlds = await _repository.GetReadyBlds(vntBldrsData[intI].Id);
                        intNBlds = ReadyBlds.Count;
                        Console.WriteLine("Get blends in READY state and DOWNLOADING pending state - count - " + intNBlds);
                        if (intNBlds != 1)
                        {
                            if (intNBlds != 0)
                            {
                                curblend.lngID = ReadyBlds[0].Id;
                                curblend.strName = ReadyBlds[0].Name;
                                curblend.strState = ReadyBlds[0].BlendState.Trim();
                            }

                            if (intNBlds > 1)
                            {
                                Console.WriteLine("More than one ^1 blends found on blender - log to message");
                                //'warning msg "More than one ^1 blends found on blender ^2. ^3 action has been Canceled"                                                                
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN2), programName, cstrGen, "READY", gstrBldrName, "DOWNLOADING", "", "", "", res);

                                Console.WriteLine("set ABC_BLENDS.PENDING_STATE to null for all");
                                foreach (AbcBlends blends in ReadyBlds)
                                {
                                    curblend.lngID = blends.Id;
                                    // 'set ABC_BLENDS.PENDING_STATE to null
                                    await _repository.SetPendingState(null, curblend.lngID);
                                }
                            }

                            //'if there are only ready blends in this blender then initialize Watch Dog variables to defaults
                            gArRbcWdog[intI].dteTagTime = cdteNull;
                            gArRbcWdog[intI].intCnt = 0;
                            gArRbcWdog[intI].intWDValue = 300;
                            gArRbcWdog[intI].intRandomNum = 0;

                            //update blenders table to hold a good comm flag for ready blenders                            
                            if (vntBldrsData[intI].RbcWdogTid != null)
                            {
                                //'monitor and set RBC watch dog flag
                               await ChkRbcWatchDog(intI, curblend.lngID, vntBldrsData, curblend);
                            }
                            else if (vntBldrsData[intI].CommErrFlag == "YES")
                            {
                                // 'monitor and set RBC watch dog flag
                               await ChkRbcWatchDog(intI, curblend.lngID, vntBldrsData, curblend);
                            }

                            if (intNBlds >= 1)
                            {
                                //check for active, loaded or paused blends at the time of downloading
                                List<AbcBlends> CheckBldsData = await _repository.CheckBlds(vntBldrsData[intI].Id);
                                intNActiveBlds = CheckBldsData.Count;

                                if (intNActiveBlds >= 1)
                                {
                                    //'warning msg "More than one ^1 blends found on blender ^2. ^3 action has been Canceled"                                    
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN2), programName, cstrGen, "ACTIVE", gstrBldrName, "DOWNLOADING", "", "", "", res);

                                    foreach (AbcBlends item in CheckBldsData)
                                    {
                                        lngBldActiveID = item.Id;
                                        //'set ABC_BLENDS.PENDING_STATE to null
                                        await _repository.SetPendingState(null, lngBldActiveID);
                                    }
                                }
                            }

                            //Check for ready blends in RETURNING pending state
                            //'send cmd STOP to DCS and clear pending state
                            List<AbcBlends> GetBlendReturningData = await _repository.GetBlendReturning(vntBldrsData[intI].Id);
                            if (GetBlendReturningData.Count > 0)
                            {
                                lngBldActiveID = GetBlendReturningData[0].Id;
                                //Send cmd stop to DCS to indicate DCS that ABC is not longer monitoring 'this blend.
                                if (gProjDfs.strAllowStartStop == "YES" && (vntBldrsData[intI].StopTid != null))
                                {

                                    if (vntBldrsData[intI].RbcModeTid != null)
                                    {
                                        //'get RBC mode flag value from ABC_TAGS
                                        AbcTags DataRes = await _repository.GetTagNameAndVal(vntBldrsData[intI].RbcModeTid);
                                        tagRbcMode.vntTagName = DataRes.Name;
                                        tagRbcMode.vntTagVal = DataRes.ReadValue.ToString();
                                    }
                                    else
                                    {
                                        tagRbcMode.vntTagName = null;
                                        tagRbcMode.vntTagVal = null;
                                    }

                                    if (((tagRbcMode.vntTagVal == null) ? (int)YesNo.YES : Convert.ToInt32(tagRbcMode.vntTagVal)) == (int)YesNo.YES)
                                    {
                                        // 'send stop command to DCS tag
                                        await _repository.SetWriteTagVal((int)YesNo.YES, "YES", vntBldrsData[intI].StopTid);
                                    }
                                    //   'set ABC_BLENDS.PENDING_STATE to null
                                    await _repository.SetPendingState(null, lngBldActiveID);
                                }
                            }

                            //'Reset the previous blend cmd to default
                            gArPrevBldData[intI].enumCmd = null; //NULL_

                            //'Reset the anz start delay to NULL
                            gArAnzDelay[intI] = cdteNull;
                            NextBlend();
                            continue;
                        }

                        // 'copy blend data values from database query
                        curblend.lngID = ReadyBlds[0].Id;
                        curblend.strName = ReadyBlds[0].Name;
                        curblend.sngTgtVol = ReadyBlds[0].TargetVol;
                        //'Recalculate Target Volume if needed for flushing
                        curblend.sngTgtVol = await CalcTargVol(curblend.lngID, curblend.sngTgtVol);
                        curblend.sngTgtRate = ReadyBlds[0].TargetRate;
                        curblend.intGrdID = Convert.ToInt32(ReadyBlds[0].GradeId);
                        curblend.vntMinVol = ReadyBlds[0].MinVol;
                        curblend.vntMaxVol = ReadyBlds[0].MaxVol;
                        curblend.vntMinRate = ReadyBlds[0].MinRate;
                        curblend.vntMaxRate = ReadyBlds[0].MaxRate;
                        curblend.dblCorrFac = ReadyBlds[0].CorrectionFactor;
                        curblend.strCtlMode = ReadyBlds[0].ControlMode;
                        curblend.sngCurVol = (ReadyBlds[0].CurrentVol == null) ? 0 : ReadyBlds[0].CurrentVol;
                        curblend.dteActualStart = (ReadyBlds[0].ActualStart == null) ? cdteNull : Convert.ToDateTime(ReadyBlds[0].ActualStart);
                        curblend.intProdID = Convert.ToInt32(ReadyBlds[0].ProductId);
                        curblend.vntPendSt = ReadyBlds[0].PendingState;
                        //Added to handle the description download to the RBC
                        curblend.strBldDesc = (ReadyBlds[0].Description == null) ? "" : ReadyBlds[0].Description;
                        gArPrevBldData[intI].strState = ReadyBlds[0].BlendState.Trim();
                        curblend.lngPrevBldId = (ReadyBlds[0].PreviousBlendId == null) ? 0 : ReadyBlds[0].PreviousBlendId;
                        //'RW 14-Oct-16 Gasoline Ethanol blending
                        //'Flag could be null for a ready blend
                        curblend.vntEtohBldgReqd = ReadyBlds[0].EthanolBldgReqdFlag; // NULL_
                        curblend.strState = (curblend.strState == null) ? "" : curblend.strState;


                    }

                    //'get cycle time for the product group - interval length
                    curblend.vntIntvLen = await _repository.GetPrdgrpCycleTime(vntBldrsData[intI].PrdgrpId);
                    if (curblend.vntIntvLen == null)
                    {
                        //'Statement setting the interval equal to the Project Cycle Time was commented out so un-commented it
                        curblend.vntIntvLen = gdblProjCycleTime;                        
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN3), programName, "BL-" + curblend.lngID, curblend.strName, curblend.vntIntvLen.ToString(), "", "", "", "", res);
                    }

                    //'if this is a new blend on the blender then reinitialize global variables
                    if (curblend.lngID != gArPrevBldData[intI].lngID)
                    {
                        gArPrevBldData[intI].lngID = curblend.lngID;
                        gArPrevBldData[intI].enumCmd = null; //NULL_
                        gArPrevBldData[intI].intCurIntv = 0;
                        gArPrevBldData[intI].strPrevBldDescr = "";
                        gArPrevBldData[intI].sngPrevBldTargVol = 0;
                        gArPrevBldData[intI].sngPrevBldTargRate = 0;
                        //'Initializing flag to skip a cycle in the Bmon if Vol Tags
                        //'have different value time
                        gintSkipCycleBmon[intI] = 0;
                        gblnOptimizing[intI] = false;
                        gintNameCount[intI] = 0;
                        gblnSetOptNowFlag[intI] = false;
                        //This intVar allows recalc of LINEPROP after CalcBias all the way to first interval
                        //gint1stSampleBias(intI) = 0
                        //'Set swing commands to OFF (Write Values of swing tags)
                        gblnBmonStarted[intI] = true;
                        gblnPrevStatePaused[intI] = false;

                        //'LINEPROP at the middle of the interval has been suppressed.
                        //'Variable to calc the component volumes in the middle of an interval                         

                        gblnProdSwgTimeIn[intI] = false;
                        gblnCompSwgTimeIn[intI] = false;
                        //'initialize the swing time out time =current time
                        gDteCompSwgCmdTime[intI] = gDteCurTime;
                        gDteProdSwgCmdTime[intI] = gDteCurTime;
                        //'Set the flag to issue the message about ABC-DCS comm
                        gblnMsgLogged[intI] = false;
                        gblnSampleMsgLogged[intI] = false;

                        for (intJ = 0; intJ < Enum.GetNames(typeof(BlendCmds)).Length - 1; intJ++)
                        {
                            gArPrevBldData[intI].arCmdTime[intJ] = cdteNull;
                        }

                        //'initialize the array of first time calc for a new blend
                        gblnBiasRedimDone[intI] = false;

                        // -------------------convert - debug ---------------

                        //For intJ = 0 To(UBound(gblnFirstBiasCalc, 2) - 1)
                        //   gblnFirstBiasCalc(intI, intJ) = False
                        //Next intJ
                        for ( intJ = 0; intJ < gblnFirstBiasCalc.Length; intJ++)
                        {
                            gblnFirstBiasCalc[intI, intJ] = false;
                        }

                        // ------------------

                        gArBldFinishTime[intI] = cdteNull;
                        //'Reset the anz start delay to NULL
                        gArAnzDelay[intI] = cdteNull;


                        gArAnzOfstSvd[intI] = false;

                        gArCompValTime[intI].blnArraySet = false;
                        // Reinitialize array for recording
                        // 'the times when blend station volumes are updated
                        gArStnValTime[intI].blnArraySet = false;
                        gArSrcTkPrpValTime[intI].blnArraySet = false;

                        gArRbcWdog[intI].dteTagTime = cdteNull;
                        gArRbcWdog[intI].intCnt = 0;
                        gArRbcWdog[intI].intWDValue = 300;
                        gArRbcWdog[intI].intRandomNum = 0;

                    }

                    //'RW 14-Oct-16 Gasoline Ethanol blending
                    if (vntBldrsData[intI].EthanolFlag == "YES")
                    {
                        if (curblend.vntEtohBldgReqd == "YES")
                        {
                            gblnEthanolBlend = true;
                        }

                        //'--- RW 25-Jan-17 Gasoline Ethanol blending remedial ---
                        if (gintEtohEtohPropId == 0 || gintEtohPropId == 0)
                        {
                            //'Get ETOH and ETOH_ETOH prop id
                            List<AbcProperties> GetEtohPropIdsData = await _repository.GetEtohPropIds();
                            foreach (AbcProperties item in GetEtohPropIdsData)
                            {
                                if (item.Name == "ETOH")
                                {
                                    gintEtohPropId = Convert.ToInt32(item.Id);
                                }
                                else
                                {
                                    gintEtohEtohPropId = Convert.ToInt32(item.Id);
                                }
                            }
                        }
                    }

                    //'monitor and set RBC watch dog flag
                    await ChkRbcWatchDog(intI, curblend.lngID, vntBldrsData, curblend);

                    //'Set the Download type string to a global variable - April 12/2001
                    gstrDownloadType = (vntBldrsData[intI].DownloadType == null) ? "" : vntBldrsData[intI].DownloadType;
                    //'get the rundown (perpetual flag) of teh blender
                    gstrRundnFlag = vntBldrsData[intI].RundnFlag;

                    //'Get the donwload type of the blender
                    if (gstrDownloadType != "")
                    {
                        //' Get the value from abc_transtxt based in the user value of downloadtype
                        List<AbcTranstxt> GetTranstxtData = await _repository.GetTranstxtData("DOWNLOADTYPE");
                        List<AbcTranstxt> GetTranstxtDataRes = GetTranstxtData.Where<AbcTranstxt>(row => row.Value == gstrDownloadType)
                                                                .ToList<AbcTranstxt>();
                        if (GetTranstxtDataRes.Count > 0)
                        {
                            gstrDownloadType = GetTranstxtDataRes[0].Value;
                        }
                        else
                        {
                            gstrDownloadType = "STATION";
                        }
                    }
                    else
                    {
                        //'get proj default download type
                        AbcProjDefaults SwgDefTimeOutData = await _repository.SwgDefTimeOut();
                        gstrDownloadType = SwgDefTimeOutData.DownloadType;
                        //'if download type="" then pick station
                        if (gstrDownloadType == "")
                        {
                            gstrDownloadType = "STATION";
                        }
                    }
                    
                    //if blens state=COMM ERR, skip next function
                    if (curblend.strState.Trim() != "COMM ERR")
                    {
                        // 'call SET_BLENDSTATE function to get current blend state in RBC
                        //'Pass the whole array instead of vntBldrsData(RBC_STATE, IntI)
                        if (await SetBlendState(intI, vntBldrsData, curblend, enumBldrDbgLevel) == RetStatus.FAILURE)
                        {
                            NextBlend();
                            continue;
                        }
                    }
                    else if (curblend.vntPendSt.Trim() == "DOWNLOADING" && gArPrevBldData[intI].strState.Trim() == "READY" &&
                        gstrRundnFlag == "NO")
                    {
                        //'if blend state is COMM ERR and pending state is Downloading, prev_state=READY and rundown=NO, then
                        //'set current state = READY and clear pending state
                        gArPrevBldData[intI].enumCmd = null; //NULL_
                        curblend.strState = "READY";
                        curblend.vntPendSt = null;
                        //'set ABC_BLENDS.PENDING_STATE to null
                        await _repository.SetPendingState(null, curblend.lngID);
                    }

                    if (enumDebugLevel >= DebugLevels.Medium) {
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG39), programName, cstrDebug, curblend.strName, curblend.strName, (curblend.vntPendSt == null)?"": curblend.vntPendSt, curblend.strState, "", "", res);
                    }
                    if (curblend.vntPendSt != null) {
                        //'call CHECK_COMMAND_VALIDITY function, if INVALID, set pending state to null
                        if(await ChkCmdValidity(intI, curblend, enumBldrDbgLevel) == ValidInvalid.invalid) {
                            curblend.vntPendSt = null;
                        }
                    }
                    if (curblend.vntPendSt == null)
                    {
                        //'call NULL_COMMAND_ACTION function
                        if ((curblend.strState).Trim() == "COMM ERR")
                        {
                            //'Skip Monitoring function
                            await NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel, true);
                        }
                        else
                        {
                            await NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel);
                        }
                    }
                    else
                    {
                        string OutRes = "";
                        switch (curblend.vntPendSt)
                        {
                            case "STARTING":
                                if (gProjDfs.strAllowStartStop == "YES")
                                {
                                    //'call START_BLEND function
                                    await ProcessBldCmd(BlendCmds.START, intI, vntBldrsData, curblend, enumBldrDbgLevel);

                                    if (curblend.vntPendSt == null)
                                    {
                                        //'call NULL_COMMAND_ACTION function
                                        await NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel);
                                    }
                                }
                                else
                                {
                                    //  'ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                                    OutRes = "";
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "START", gstrBldrName,
                                        "", "", "", "", OutRes);

                                    curblend.vntPendSt = null;
                                    //'call NULL_COMMAND_ACTION function
                                    await NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel);
                                }
                                break;                            
                            case "RESTARTING":
                                if ((gProjDfs.strAllowStartStop == "YES"))
                                {
                                    // call RESTART_BLEND function                                    
                                    await ProcessBldCmd(BlendCmds.RESTART, intI, vntBldrsData, curblend, enumBldrDbgLevel);
                                    if (curblend.vntPendSt == null)
                                    {
                                        //'call NULL_COMMAND_ACTION function
                                        await NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel);
                                    }
                                }
                                else
                                {
                                    // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                                    OutRes = "";
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "RESTART", gstrBldrName,
                                        "", "", "", "", OutRes);

                                    curblend.vntPendSt = null;
                                    //'call NULL_COMMAND_ACTION function
                                    await NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel);
                                }

                                break;
                            case "PAUSING":
                                if ((gProjDfs.strAllowStartStop == "YES"))
                                {
                                    // call PAUSE_BLEND function
                                    await ProcessBldCmd(BlendCmds.PAUSE, intI, vntBldrsData, curblend, enumBldrDbgLevel);
                                    if (curblend.vntPendSt == null)
                                    {
                                        //'call NULL_COMMAND_ACTION function
                                        await NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel);
                                    }
                                }
                                else
                                {
                                    // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                                   
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "PAUSE", gstrBldrName,
                                        "", "", "", "", OutRes);

                                    curblend.vntPendSt = null;
                                    //'call NULL_COMMAND_ACTION function
                                    await NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel);
                                }

                                break;
                            case "DOWNLOADING":
                                if ((curblend.strState == "PAUSED"))
                                {
                                    if ((await ChkBlendEquip(intI, vntBldrsData, curblend) == ValidInvalid.invalid))
                                    {
                                        goto UPDATE_BLD_DATA;
                                    }

                                }

                                // call DOWNLOAD_BLEND function
                                await Downloading(intI,vntBldrsData,curblend,enumBldrDbgLevel);
                                if (curblend.vntPendSt == null)
                                {
                                    //'call NULL_COMMAND_ACTION function
                                    await NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel);
                                }
                                break;
                            case "STOPPING":
                                if ((gProjDfs.strAllowStartStop == "YES"))
                                {
                                    // call STOP_BLEND function
                                    await ProcessBldCmd(BlendCmds.STOP_, intI, vntBldrsData, curblend, enumBldrDbgLevel);
                                    if (curblend.vntPendSt == null)
                                    {
                                        //'call NULL_COMMAND_ACTION function
                                        await NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel);
                                    }
                                }
                                else
                                {
                                    // ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN11), programName, "BL-" + curblend.lngID, "STOP", gstrBldrName,
                                        "", "", "", "", OutRes);

                                    curblend.vntPendSt = null;
                                    //'call NULL_COMMAND_ACTION function
                                    await NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel);
                                }

                                break;
                            case "OPTIMIZING":
                                vntOptRunState = await _repository.GetPrgRunState("ABC OPTIMIZE MONITOR");
                                if (vntOptRunState == null || vntOptRunState != "RUNNING")
                                {
                                    curblend.vntPendSt = null;
                                    // call NULL_COMMAND_ACTION function
                                    await NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel, true);
                                }

                                // Monitor the active blend even if the pending state is optimizing
                                // set the optimization flag to monitor blends during pending state="OPTIMIZING"
                                gblnOptimizing[intI] = true;                                
                                if (enumDebugLevel == DebugLevels.High)
                                {
                                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG4), programName, cstrDebug, curblend.strName, "NULL_COMMAND_ACTION",
                                       "", "", "", "", OutRes);
                                }
                                // call NULL_COMMAND_ACTION function
                                await NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel);
                                gblnOptimizing[intI] = false;
                                break;
                            case "SWINGING":
                                await NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel);
                                break;
                            case "RETURNING":
                                if ((curblend.strState.Trim() == "LOADED"))
                                {
                                    curblend.strState = "READY";
                                }

                                if ((gProjDfs.strAllowStartStop == "YES")&& (vntBldrsData[intI].StopTid != null))
                                {
                                    if (vntBldrsData[intI].RbcModeTid != null)
                                    {
                                        // get RBC mode flag value from ABC_TAGS
                                        AbcTags data = await _repository.GetTagNameAndVal(vntBldrsData[intI].RbcModeTid);
                                        tagRbcMode.vntTagName = data.Name;
                                        tagRbcMode.vntTagVal = data.ReadValue.ToString();
                                    }
                                    else
                                    {
                                        tagRbcMode.vntTagName = null;
                                        tagRbcMode.vntTagVal = null;
                                    }

                                    if (((tagRbcMode.vntTagVal == null) ? (int)YesNo.YES : Convert.ToInt32(tagRbcMode.vntTagVal)) == (int)YesNo.YES)
                                    {
                                        // 'send stop command to DCS tag
                                        await _repository.SetWriteTagVal((int)YesNo.YES, "YES", vntBldrsData[intI].StopTid);
                                    }
                                    curblend.vntPendSt = null;
                                    //   'set ABC_BLENDS.PENDING_STATE to null
                                    await _repository.SetPendingState(null, curblend.lngID);
                                }

                                gArPrevBldData[intI].enumCmd = null;
                                break;
                            default:                                
                                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN5), programName, "BL-" + curblend.lngID, curblend.vntPendSt, curblend.strName,
                                    "", "", "", "", OutRes);
                                await NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel, true);
                                break;
                        }                        
                    }

                UPDATE_BLD_DATA:
                    List<AbcBlends> BlendState = await _repository.GetBlendState(curblend.lngID);
                    strBlendState = BlendState[0].BlendState;
                    if (strBlendState.Trim() == "PARTIAL" || strBlendState.Trim() == "READY" && curblend.vntPendSt.Trim() != "DOWNLOADING")
                    {
                        gArPrevBldData[intI].enumCmd = null;
                    }
                    else
                    {
                        // update ABC_BLENDS.BLEND_STATE with current blend state
                        await _repository.SetBlendState(curblend.lngID, curblend.strState);
                        // Clear swing flags for all comps in this blender.  Check that
                        // no other blender is using the comps before clearing up the flags. Do this checking
                        // for Loaded and Done blends (one time)
                        if (((curblend.strState.Trim() == "LOADED") || (curblend.strState.Trim() == "DONE")))
                        {
                            await SetSwingTIDOFF((int)vntBldrsData[intI].PrdgrpId, ((vntBldrsData[intI].SwingTid == null) ? -1 : Convert.ToDouble(vntBldrsData[intI].SwingTid)), (int)vntBldrsData[intI].Id);
                        }

                    }
                    
                    // update interval # in previous blend data
                    if ((gArPrevBldData[intI].intCurIntv < curblend.intCurIntv))
                    {
                        gArPrevBldData[intI].intCurIntv = curblend.intCurIntv;
                    }

                NEXT_BLENDER:
                    curblend.intCurIntv = 0;
                    curblend.strState = "";

                }
            }
            return 0;
        }
    }
}
