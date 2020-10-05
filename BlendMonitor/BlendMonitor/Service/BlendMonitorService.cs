using BlendMonitor.Entities;
using BlendMonitor.Model;
using BlendMonitor.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static BlendMonitor.Constans;

namespace BlendMonitor.Service
{
    public class BlendMonitorService : IBlendMonitorService
    {
        private IBlendMonitorRepository _repository;
        private IConfiguration _configuration;
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
        DcsTag gTagTotFlow;
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
        CurBlendData curblend;
        string[] gArPrevRBCState;
        DateTime gDteCurTime;


        public BlendMonitorService(IBlendMonitorRepository repository, IConfiguration configuration)
        {
            _repository = repository;
            _configuration = configuration;
            programName = _configuration.GetSection("ProgramName").Value.ToUpper();
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
            DateTime? vntTagValTime = null;


            if (vntBldrsData[intBldrIdx].RbcWdogTid == null)
            {
                //'check DCS->ABC communication
                if (await Shared.ChkDcsComm(curblend.lngID, vntBldrsData[intBldrIdx].Id, gstrBldrName) == GoodBad.BAD)
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
                            //'JAIME: Reset the blend state to READY state when the Start_OK="OFF"
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

        private async void ChkIntervals(int intBldrIdx, CurBlendData curblend, DebugLevels enumDebugLevel,bool blnCloseIntv = false)
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
                Shared.CheckNewIntvRecs(curblend.lngID, 0, enumDebugLevel, gDteCurTime);
                Shared.CheckNewIntvRecs(curblend.lngID, 1, enumDebugLevel, gDteCurTime);
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
                // JO - Jan 27, 04: override the start_time for interval 1.  This way interval
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
                        Shared.CheckNewIntvRecs(curblend.lngID, curblend.intCurIntv, enumDebugLevel, gDteCurTime);                       
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
        }
        // *********** ChkDcsRcp ***********

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

        private async void ChkDcsRcp(int intBldrIdx, double lngBldID, string strBldName, DebugLevels enumDebugLevel)
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
                    return;
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
        }

        private async void UpdatePropTable(int intBldrIdx, int intPrdgrpID, double lngBldID, string strBldName, DebugLevels enumDebugLevel)
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
                    gArSrcTkPrpValTime[intBldrIdx].arValueTime = new DateTime[intNCompProps - 1];
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

            intTimeConv = Shared.TimeConvFactor(strFlowDenom);

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
            intTimeConv = Shared.TimeConvFactor(strFlowDenom);
            
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
        // JO - April 04: Get the prev stations vols from blend stations
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
        // *********** ModelLocal ***********


        // call GAMS to do recipe or line property calculations
        // For OPTIMIZE mode, vntData is run ID for the optimizer run
        // For LINEPROP mode, vntData is a 1-D array of interval comp vols
        // JO - intIntNum: holds the StartInterval for composite samples.   A new parameter has been added to
        // intStopInterval (optional): holds the StopInterval for composite/spot samples, which is used to get the range of sample intervals
        public async void ModelLocal(GAMSCalcTypes enumCalcType, double lngBlendId, int intIntvNum, double[] vntData, DebugLevels enumDebugLevel, int? intStopInterval,RetStatus gintOptResult)
        {           
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            if (enumDebugLevel == DebugLevels.High)
            {
                // LOG: msg "**** Entering Model_Local ****"
            }            
            
            string gstrOptEngine = await _repository.GetOptEngine();
            string gstrSBPath = "";
            string gstrModelDir = "";
            string gstrOutputDir = "";
            string gstrInputDir = "";
            string gstrErrorDir = "";

            List<string> Data = await _repository.GetSBPath();
            if (Data.Count() > 0)
            {
                gstrSBPath = (Data[0] ==null)?"C:\\SB35": Data[0];
                gstrModelDir = (gstrSBPath + cstrModelDir);
                gstrInputDir = (gstrSBPath + cstrInputDir);
                gstrOutputDir = (gstrSBPath + cstrOutputDir);
                gstrErrorDir = (gstrSBPath + cstrErrorDir);
            }
            else
            {
                gstrSBPath = "C:\\SB35";
                gstrModelDir = ("C:\\SB35" + cstrModelDir);
                gstrInputDir = ("C:\\SB35" + cstrInputDir);
                gstrOutputDir = ("C:\\SB35" + cstrOutputDir);
                gstrErrorDir = ("C:\\SB35" + cstrErrorDir);
            }            

            //if (enumCalcType == GAMSCalcTypes.LINEPROP && ! vntData.IsArray)
            //{
            //    Err.Description = ("Argument vntData for ModelLocal() must be " + "array for line prop calc");
            //    Err.Raise;
            //    (vbObjectError + 1000);
            //}

            //if ((enumCalcType != GAMSCalcTypes.OPTIMIZE) && (enumCalcType != GAMSCalcTypes.LINEPROP))
            //{
            //    Err.Description = "Unknown GAMS calc type. Must be OPTIMIZE(= 0) or LINEPROP(= 2)";
            //    Err.Raise;
            //    (vbObjectError + 1000);
            //}

            // Partha - create the error dir if it doesn't exist - 11/17/2000
            CreateDir(gstrErrorDir)
            GAMSIntface enumCalcType, lngBlendId,intIntvNum,vntData,enumDebugLevel,intStopInterval;
            //// JO - Jan. 19, 04: opt result is required then pass the FAILURE/SUCCESS flag
            //if (gintOptResult == RetStatus.FAILURE)
            //{
            //    gintOptResult = gArOptSoluStats(enumCalcType);
            //}

            return;        
        }

        private async Task<double> ChkIntBiasCalcCurr(string strBiasCalcCurrent, int intStartInterval, double lngBlendId, double lngPropID, int intStopInterval = 0)
        {           
            string strWhereSeq;
            string strBiasType;
            // TODO: On Error GoTo Warning!!!: The statement is not translatable            
            strWhereSeq = "";
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

        private async void CalcBias(int intBldrIdx,List<AbcBlenders> vntBldrsData, CurBlendData curblend, DebugLevels enumDebugLevel,string strIntBiasType,string strSampleName= "", 
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
            // JO - Aug, 03: Sample composite/spot declaration
            int intPropIndex;
            int intMatchingIntv;
            int intNprops;
            string strBiasCalcCurrent;
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
                   // Array.Resize(ref gblnFirstBiasCalc[Information.UBound(vntBldrsData, 2)], intNprops - 1);
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
                                        // JO/KA/NL - Jan. 20, 04: Comment out BiasOverrideFlag
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

                                        // JO - Aug, 03: if this is the first time bias is being calc for this prop, then
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
                                        // JO/KA/NL - Jan. 20, 04: Comment out BiasOverrideFlag
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

                                            // JO - July , 2005: Make sure that start/stop intervals are valid
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
                                            // JO/KA/NL - Jan. 20, 04: Comment out BiasOverrideFlag
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
                                                    strCalcBiasFallBack = CalcBiasFallBack(strBiasCalcCurrent, vntBldrsData[intBldrIdx].Id, curblend, vntPropID, vntBldrsData[intBldrIdx].PrdgrpId);
                                                }

                                            }
                                            else
                                            {
                                                // calc sub CalcBiasFallBack to handle fallback if needed
                                                strCalcBiasFallBack = CalcBiasFallBack(strBiasCalcCurrent, vntBldrsData[intBldrIdx].Id, curblend, vntPropID, vntBldrsData[intBldrIdx].PrdgrpId);
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


                        //ABCdataEnv.rscomGetBldSampleProps.MoveFirst;
                    }

                    // if not Blend Sample props
                }
                else
                {

                }


            }
        }
        private async void CalcBlend(int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend, DebugLevels enumDebugLevel)
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
                return;
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
                                    Shared.ErrorLog("TOTALIZER VOLUME SCAN TIMES ARE NOT SYNCHRONIZED IN SCAN GROUP " + strScanGrpName + ", " + "BL-"
                                                + curblend.lngID + ", " + strMinMaxTimeTag, true);
                                    gintSkipCycleBmon[intBldrIdx] = 1;

                                    return;
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
                                    Shared.ErrorLog("TOTALIZER VOLUME SCAN TIMES ARE NOT SYNCHRONIZED IN SCAN GROUP " + strScanGrpName + ", " + "BL-"
                                                + curblend.lngID + ", " + strMinMaxTimeTag, true);
                                    gintSkipCycleBmon[intBldrIdx] = 1;

                                    return;
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
            arCompIntVol = new double[(intNComps - 1)];
            arCompBldVol = new double[(intNComps - 1)];
            arDltVol = new double[(intNComps - 1)];
            arAddDltVol = new double[(intNComps - 1)];
            arAddCompIntVol = new double[(intNComps - 1)];
            arAddCompBldVol = new double[(intNComps - 1)];

            //'initialize value time array for components on the blender, if neccessary
            if (!gArCompValTime[intBldrIdx].blnArraySet)
            {
                gArCompValTime[intBldrIdx].arValueTime = new DateTime[(intNComps - 1)];
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
            intStationNumber = 1;
            // Array for recording blend stations that have been processed
            arStationsDone = new double?[(intTotNStations - 1)];
            blnRollBack = true;
            for (intI = 0; intI < (intNComps - 1); intI++)
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
                    // JO - Dec. 03: Get component Rcp Constraint Type
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
                        intStationNum = (intStationNum + 1);
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
                            return;
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

                                // JO - Sep, 03: ZERO COMP RCP Handling
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
                                    return;
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
                    // JO - April 04: Handling switching of multiple stations in active/paused mode
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
                    // JO - Dec. 03: Get component Rcp Constraint Type
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

                        return;
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
                        // JO - Sep, 03: ZERO COMP RCP Handling.  Skip Msg
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
                            return;
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
                        // JO - Sep, 03: ZERO COMP RCP Handling. Skip msg
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
                            return;
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
            intStationNumber = 1;
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
                        //    JAIME: To set the Int Recipe to display in the BO
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

            // BDS 11-May-2012 PQ-D0074
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
                    //          'JO - Aug, 03: Move to the beginning of the program
                    //          Set up global array of prdgrp IDs
                    //          GetPrdgrpIDs
                    // JO: Nov. 14, 2006: If blend is finished in RBC, then close current interval and open a new one.
                    // The TQI will be set for TMon to perform it and blend will be closed in the next cycle of Bmon
                    // Note that all remaining volume was allocated in the previuos interval to be accounted in the last TQI
                    if ((gArBldFinishTime[intBldrIdx] != cdteNull) && (curblend.intCurIntv > 1))
                    {
                        ChkIntervals(intBldrIdx,curblend,enumDebugLevel);
                    }

                    // JOJOJOJOJOJOJOJ
                    //         If curblend.intCurIntv > gArPrevBldData[intBldrIdx].intCurIntv And curblend.intCurIntv > 1 Then
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
                            Array.Resize(ref arCompIntVol, intI);

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

                        //  Jaime: if calcprop_flag="YES" in abc_blenders, then proccess LINEPROP
                        if (((vntBldrsData[intBldrIdx].CalcpropFlag) == "YES") && (curblend.sngCurVol != 0) && (dblSumVol > 0))
                        {
                            // call MODEL_LOCAL to do line property calculation for previous interval
                            await ModelLocal(GAMSCalcTypes.LINEPROP, curblend.lngID, gArPrevBldData[intBldrIdx].intCurIntv, arCompIntVol, enumDebugLevel, , gintOptResult);
                        }
                    }
                    else
                    {
                        //  Copy the selected Properties from ABC_BLEND_COMP_Props to Feedback_pred in abc_blend_interval_props
                        List<SelTankProps> SelTankPropsList = await _repository.GetSelTankProps(curblend.lngID);
                        foreach (var SelTankPropsObj in SelTankPropsList)
                        {
                            dblFeedbackPred = (SelTankPropsObj.Value == null)? 0: Convert.ToInt32(SelTankPropsObj.Value);
                            intCompPropID = Convert.ToInt32(SelTankPropsObj.PropId);
                            await _repository.SetFeebackPred(dblFeedbackPred,curblend.lngID,gArPrevBldData[intBldrIdx].intCurIntv,intCompPropID);   
                        }                        

                        // if # of components > 1
                        // JO - Aug, 03: Added a bias Calc parameter to diff, between calc bias types.  This call is for
                        // the common bias calc done every new interval.  This calls updates the Anzr values only (prev calc bias logic)
                        // Note: That calcbias sub has been moved to this place to calc bias only when a new interval is created, just
                        // after LINEPROP calc
                        // Pass an internal variable to identify the procedence of the bias call: REGular
                        CalcBias(intBldrIdx, vntBldrsData, curblend, enumDebugLevel, "REG");
                        UpdateBlendProps(intBldrIdx, vntBldrsData, curblend, enumDebugLevel);
                    }

                    // if new interval was created
                }

                // if blend state = "ACTIVE"
            }




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
            if (await Shared.ChkDcsComm(curblend.lngID, vntBldrsData[intBldrIdx].Id, gstrBldrName) == GoodBad.BAD)
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


            // JO - Oct. 07, 2004: get the last interval if the blend monitor just started
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
                ChkIntervals(intBldrIdx, curblend, enumDebugLevel);
            }

            //This is for copying the previous BIAS to the current interval BIAS if the interval is new
            //In addition to the bias, copy also the BiasCalc_current to the new interval
            if (curblend.intCurIntv > gArPrevBldData[intBldrIdx].intCurIntv && curblend.intCurIntv > 1)
            {
                // Populate bias field for interval 1 from abc_blend_props.initial_bias as soon as it is created
                // THIS CODE WAS MOVED TO shared (ChkNewIntvlCreation)
                // The cmd update was improve to update all props at once, intead
                // of looping one by one
                // JO - Aug, 03: modified this update statement to update the biascalc_current field along with
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
                    ChkDcsRcp(intBldrIdx, curblend.lngID, curblend.strName, enumDebugLevel);
                }
            }

            // ***********************************
            // if ALLOW_COMP_UPDATES is YES then call UPDATE_PROP_TABLE function
            if ((gProjDfs.strAllowCompUpds == "YES"))
            {
                UpdatePropTable(intBldrIdx, Convert.ToInt32(vntBldrsData[intBldrIdx].PrdgrpId), curblend.lngID, curblend.strName, enumDebugLevel);
            }

            // call CALC_BLEND function
            CalcBlend(intBldrIdx, vntBldrsData, curblend, enumDebugLevel);

            // issue warning msg if current vol exceeds target vol * 1.01 for the blend
            if ((curblend.sngCurVol
                        > (curblend.sngTgtVol * 1.01)))
            {
                // warning msg "Total volume in blend ^1 exceeding target volume"                
                res = "";
                await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN51), programName, "BL-" + curblend.lngID, curblend.sngCurVol.ToString(), curblend.strName.ToString(),
                    curblend.sngTgtVol.ToString(), "", "", "", res);
            }

            // JO - Mar. 17, 04:  Download the comment in case it has been changed
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
            if (((gintSkipCycleBmon[intBldrIdx] == 0)
                        || (gintSkipCycleBmon[intBldrIdx] == 2)))
            {
                // JAIME: call Prod TANK_SWING function
                SwingProdTank;
                intBldrIdx;
                vntBldrsData;
                curblend;
                intDestTankID;
                enumDebugLevel;
                // Skip comp Tank swing if the pending state is not null and blend state is DONE
                // RW 31-Jul-14 PreemL PQL-79
                // If IsNull(curblend.vntPendSt) And Trim(curblend.strState) <> "DONE" Then
                if (((IsNull(curblend.vntPendSt)
                            || (curblend.vntPendSt == "SWINGING"))
                            && (curblend.strState.Trim() != "DONE")))
                {
                    // RW 31-Jul-14 PreemL PQL-79
                    SwingCompTank;
                    intBldrIdx;
                    vntBldrsData;
                    curblend;
                    enumDebugLevel;
                }

                // Nov 07/2001: Relocated this function to be after Prod Swing checking
                // to avoid unnecessary messages when a product swing happens
                // RW 17-Feb-16 PreemL PQL-130
                // If Trim(curblend.strState) <> "DONE" Or gArBldFinishTime[intBldrIdx] = cdteNull Then
                if (((curblend.strState.Trim() != "DONE")
                            && (gArBldFinishTime[intBldrIdx] == cdteNull)))
                {
                    // call CHECK_DCS_FEEDBACK function
                    ChkDcsFeedback;
                    intBldrIdx;
                    vntBldrsData;
                    curblend;
                    intDestTankID;
                    enumDebugLevel;
                }

            }

            // *******
            if (((curblend.vntPendSt.Trim() == "OPTIMIZING")
                        && (curblend.strState.Trim() != "DONE")))
            {
                // get last_run_time of OPTIM MONITOR
                //     ABCdataEnv.cmdGetLastRunTime "ABC OPTIMIZE MONITOR", dteOpmonTime
                ABCdataEnv.comGetLastOptTime;
                curblend.lngID;
                ABCdataEnv.rscomGetLastOptTime.ActiveConnection = null;
                if (!ABCdataEnv.rscomGetLastOptTime.EOF)
                {
                    dteOpmonTime = NVL(ABCdataEnv.rscomGetLastOptTime.Fields("LAST_OPTIMIZED_TIME").Value, cdteNull);
                    if ((DateDiff("s", dteOpmonTime, gDteCurTime) > (3
                                * (curblend.vntIntvLen * 60))))
                    {
                        // warning msg "Optimizer Monitor may be inactive"
                        ABCdataEnv.cmdLogMessage;
                        COM_W1;
                        App.Title;
                        ("BL-" + Format(curblend.lngID, cstrIDFmt));
                        "ABC OPTIMIZER MONITOR";
                        "";
                        "";
                        "";
                        "";
                        "";
                        gStrRetOK;
                        // set ABC_BLENDS.PENDING_STATE to null
                        ABCdataEnv.cmdSetPendingState;
                        Null;
                        curblend.lngID;
                        curblend.vntPendSt = "";
                    }

                }

            }

            return;
        }

        public async void NullCmdAction(int intBldrIdx, List<AbcBlenders> vntBldrsData, CurBlendData curblend, DebugLevels enumDebugLevel, bool blnSkipMonitor = false)
        {
            int intDestTankID, intNDestTks = 0, intTimeDiff;
            int intIntvNum;
            double? lngProdLineupId, lngTransferLineId, lngDestTkId = 0;
            double lngFlushTankId = 0;
            double? dblDestVolume, dblPrdHeelVol;
            string strFlushSwgState;
            string strTkInUseFlag, strABCService, strDCSState;
            string strAnzName, strFlushTkFlag, strHeelUpdOccurredFlag;
            DcsTag tagTotVol;
            DcsTag tagPermissive;
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
                    // JO - Aug, 03: Replace this update by the new created sub SetSwingTIDOFF
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
                        intNDestTks = GetDestTkFlagsDataFiltered.Count();
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
                    await MonitorBlend(intBldrIdx, vntBldrsData, curblend, intDestTankID, enumDebugLevel)



                }





            }


        }
        public async void ProcessBlenders()
        {
            string strDebugFlag; //'debug flag string ("YES" or "NO")
            bool blnArraysSet = false;
            double lngBldActiveID = 0;
            DebugLevels enumDebugLevel = 0, enumBldrDbgLevel = 0;//'integer value of debug level for low, medium and high
            string strBldrDbgFlag = "";
            DcsTag tagRbcMode = new DcsTag();
            List<AbcBlenders> vntBldrsData = new List<AbcBlenders>();
            int intNBldrs = 0, intNBlds = 0, intNActiveBlds = 0, intI = 0, intJ = 0;
            Console.WriteLine("getting program data to check process enabled for blend monitor");
            AbcPrograms Data = await _repository.ProcessEnabled();
            strDebugFlag = Data.DebugFlag;
            enumDebugLevel = (DebugLevels)Convert.ToInt32(Data.DebugLevel);
            DateTime gDteCurTime = DateTime.Now;
            if (Data.EnabledFlag.ToUpper() == "YES")
            {
                Console.WriteLine("enabled");
                Console.WriteLine("log start time");
                await _repository.SetStartTime();
                if (enumDebugLevel == DebugLevels.High)
                {
                    var res = "";
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
                    gintSkipCycleBmon = new int[intNBldrs - 1];
                    gDteCompSwgCmdTime = new DateTime[intNBldrs - 1];
                    gDteProdSwgCmdTime = new DateTime[intNBldrs - 1];
                    gblnMsgLogged = new bool[intNBldrs - 1];
                    gblnSampleMsgLogged = new bool[intNBldrs - 1];
                    gblnProdSwgTimeIn = new bool[intNBldrs - 1];
                    gblnCompSwgTimeIn = new bool[intNBldrs - 1];
                    gblnBmonStarted = new bool[intNBldrs - 1];
                    // '       'JO - Aug, 03: This intVar allows recalc of LINEPROP after CalcBias all the way to first interval
                    //'       gint1stSampleBias = new int[intNBldrs - 1];
                    //       'First member is for blend id, second is for target vol/target vol/Transfer Vol

                    gArPrevTargetVol = new double[intNBldrs, intNBldrs + 1];
                    gArPrevTargetRate = new double[intNBldrs, intNBldrs + 1];
                    gArPrevTransferVol = new double[intNBldrs, intNBldrs + 1];

                    // initialize variables per blender
                    for (intI = 0; (intI <= (intNBldrs - 1)); intI++)
                    {
                        // JO - Sep, 03: LINEPROP at the middle of the interval has been suppressed.
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
                    gArPrevBldData = new PrevBlendData[intNBldrs - 1];
                    gArBldFinishTime = new DateTime[intNBldrs - 1];
                    gArAnzOfstSvd = new bool[intNBldrs - 1];
                    gArCompValTime = new ValTime[intNBldrs - 1];
                    // 'BDS 11-May-2012 PQ-D0074 Array to record times when station current volumes are updated
                    gArStnValTime = new ValTime[intNBldrs - 1];
                    // 'BDS 11-May-2012 PQ-D0074
                    gArSrcTkPrpValTime = new ValTime[intNBldrs - 1];
                    // '     gArBldEqpTags[intNBldrs - 1]
                    gArRbcWdog = new RbcWatchDog[intNBldrs - 1];
                    gArAnzDelay = new DateTime[intNBldrs - 1];//the anz_start_delay of the blenders
                    gblnNOProcActBlds = new bool[intNBldrs - 1];//the anz_start_delay of the blenders
                    gblnOptimizing = new bool[intNBldrs - 1];//Optimizing flag
                    gintNameCount = new int[intNBldrs - 1]; //counter of retries when ABC<>RBC name
                    gblnSetOptNowFlag = new bool[intNBldrs - 1]; //This flag sets the TQI_NOW_FLAG='YES' when the BMON starts (1st Time]
                    gblnPrevStatePaused = new bool[intNBldrs - 1];//create new intervals only two cycles of Bmon after paused state
                                                                  // '      gint1stSampleBias[intNBldrs - 1] 'JO - Aug, 03: This intVar allows recalc of LINEPROP after CalcBias all the way to first interval


                    // gblnFirstBiasCalc = new bool[intNBldrs - 1, 0 To 1]; //'initialize
                    gblnBiasRedimDone = new bool[intNBldrs - 1];
                    gArPrevRBCState = new string[intNBldrs - 1];

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
                    var res = "";
                    await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.WARN1), programName, cstrGen, "", "", "", "''", "", "", res);
                    gProjDfs.vntRcpTolr = 1;
                }
                gstrProjName = gProjDfs.strProjName;
                gsngFGEEtoh = gProjDfs.sngFGEEtoh;
                gsngMinEtoh = gProjDfs.sngMinEtoh;
                gstrLIMSSeparateProps = gProjDfs.strLIMSSeparateProps;
                Console.WriteLine("Iterating through all blenders");
                for (int i = 0; i < intNBldrs - 1; i++)
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
                        var res = "";
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
                        var res = "";
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
                                var res = "";
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
                                    var res = "";
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


                    }

                    //'get cycle time for the product group - interval length
                    curblend.vntIntvLen = await _repository.GetPrdgrpCycleTime(vntBldrsData[intI].PrdgrpId);
                    if (curblend.vntIntvLen == null)
                    {
                        //'Statement setting the interval equal to the Project Cycle Time was commented out so un-commented it
                        curblend.vntIntvLen = gdblProjCycleTime;
                        var res = "";
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
                        var res = "";
                        await _repository.LogMessage(Convert.ToInt32(msgTmpltIDs.DBUG39), programName, cstrDebug, curblend.strName, curblend.strName, curblend.vntPendSt.ToString(), curblend.strState, "", "", res);
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
                            NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel, true);
                        }
                        else
                        {
                            NullCmdAction(intI, vntBldrsData, curblend, enumBldrDbgLevel);
                        }
                    }
                    else
                    {

                    }


                }
            }
        }
    }
}
