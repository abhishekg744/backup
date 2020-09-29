﻿using BlendMonitor.Repository;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static BlendMonitor.Constans;

namespace BlendMonitor
{
    public class Shared
    {
        private static IBlendMonitorRepository _repository;
        private readonly IConfiguration _configuration;
        private static string programName;

        public Shared(IBlendMonitorRepository repository, IConfiguration configuration)
        {
            _repository = repository;
            _configuration = configuration;
            programName = _configuration.GetSection("ProgramName").Value.ToUpper();
        }

        public static async Task<GoodBad> DCSCommStatus()
        {
            GoodBad DCSCommStatus = GoodBad.BAD;
            //' DCS communication watchdog tag toggled between
            //' 0 and 1. We may want to test this toggling
            //' condition in next release. For the time being,
            //' we only check the quality and timestamp. K. Hui
            //'JAIME: Read the flag from projects defaults to determine the comm with the Controller
            string res = await _repository.GetCommWDTag();
            if (res == "YES")
                DCSCommStatus = GoodBad.GOOD;
            return DCSCommStatus;
        }
        public static async Task<GoodBad> ChkDcsComm(double lngBldID, double intBldrID, string strBldrName)
        {
            string strRunState = "", strErrPrefix = "";
            //'strErrPrefix is the starting characters in an error string to be
            //'written to program_error in abc_blenders, indicating which program
            // 'generated the error
            //'e.g., error generated by Blend Monitor will start with "BMON",
            //'Tank Monitor with "TMON", etc.
            switch (programName)
            {
                case "ABC BLEND MONITOR":
                    strErrPrefix = "BMON";
                    break;
                case "ABC OPTIMIZE MONITOR":
                    strErrPrefix = "OPMON";
                    break;
                case "ABC TANK MONITOR":
                    strErrPrefix = "TMON";
                    break;
                case "ABC ANALYZER MONITOR":
                    strErrPrefix = "AMON";
                    break;
            }

            if (await DCSCommStatus() == GoodBad.GOOD)
            {
                //'clear *_NODCS error on blender
                await _repository.SetBlenderErrFlag("", intBldrID, strErrPrefix + "_NODCS");
                return GoodBad.GOOD;
            }
            else
            {
                // 'warning msg "No DCS communication"
                var res = "";
                await _repository.LogMessage(Convert.ToInt32(CommonMsgTmpIDs.COM_W4), programName, "BL-" + lngBldID, strErrPrefix, strBldrName, "", "", "", "", res);
                //'set *_NODCS error on blender
                await _repository.SetBlenderErrFlag(strErrPrefix + "_NODCS", intBldrID, "");
                return GoodBad.BAD;
            }

        }

        // *********** CheckNewIntvRecs ***********
        public static async void CheckNewIntvRecs(double lngBldID, int intIntvNum, DebugLevels enumDebugLevel, DateTime dteCurTime)
        {
            bool blnIntvExists;
            string strSqlIntvProps;
            string strSqlIntvComps;
            string strSqlIntv;
            string strBlendID;
            string strTankType;
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            // ERIK ** Check for intIntvNum existence before creation
            // ******* cmdCheckIntv.Fields("INTVS") returns 1 if intv exists
            // ******* else 0
            blnIntvExists = false;
           int intervalCount =  await _repository.CheckIntv(lngBldID, intIntvNum);
            
            if (intervalCount == 0)
            {
                // create new interval in ABC_BLEND_INTERVALS
                await _repository.AddNewBldIntv(lngBldID, intIntvNum, dteCurTime);
                // create records for new interval in ABC_BLEND_INTERVAL_COMPS
                await _repository.AddNewIntvComps(intIntvNum, lngBldID);
                // create records for new interval in ABC_BLEND_INTERVAL_PROPS
                await _repository.AddNewIntvProps(intIntvNum, lngBldID);
                // JO: Jan. 26, 04: set volume for new interval
                await _repository.SetNewIntv(0, lngBldID, intIntvNum);
                blnIntvExists = true;
            }

            // if interval does not exist yet then update abc_blend_interval_props.BiasCalc_current and .bias/.unfilt_bias from
            // abc_prdgrp_props.biascalc_default
            if (intIntvNum == 0 && blnIntvExists == true)
            {
                await _repository.SetBiasCalcCurrent(lngBldID, intIntvNum);

                // When interval zero is created, if product tank is not a tank, then update intvl_props.bias_calccurrent from
                // prdgrp_props.alt_biascalc_default, instead of prdgrp_props.biascalc_default
                // get the product tank type
                List<string> PrdTankType =  await _repository.GetPrdTankType(lngBldID);
                if (PrdTankType.Count > 0)
                {
                    strTankType = (PrdTankType[0] == null)? PrdTankType[0]: "TANK";
                    if ((strTankType != "TANK"))
                    {
                        // Update the blend_intv_props.biascalc_current field for none tanks types of
                        // storages from the prdgrp_props.alt_biascalc_default
                        await _repository.SetBiasCalcCurrent2(lngBldID, intIntvNum);
                    }
                }
            }

            // JO - Aug, 03: copy BiasCalc_current from "0" interval and initial bias from blend_props to .bias/.unfilt_bias of "1" interval
            if (intIntvNum == 1 && blnIntvExists == true)
            {
                await _repository.SetBiasCalcCurrent3(lngBldID, intIntvNum);
            }            
        }


        // log run time error messages into a text log
        //  Public Sub ErrorLog(ByVal strErr As String)                                           RW 28-Mar-2012 for PreemL PQ-19
        public static void ErrorLog(string strErr, bool blnLogFileOnly)
        {
            ////  RW 28-Mar-2012 for PreemL PQ-19
            //// Warning!!! Optional parameters not supported
            //string strMessage;
            //int intFileNum;
            //int intI;
            //string strFileName;
            //object arStrs;
            //int intNumLines;
            //string strLine;
            //// TODO: On Error GoTo Warning!!!: The statement is not translatable 
            //// Save error and time to Errors.log in current directory
            //strMessage = DateTime.Now.ToString("yyyy’-‘MM’-‘dd’T’HH’:’mm’:’ss"); //Format(Now, "yyyy-mm-dd Hh:Nn:Ss");
            //strMessage = (strMessage + " -- ");
            //strMessage = (strMessage + strErr);
            //// Check if Errors.log exists
            //if ((App.Path.Substring((App.Path.Length - 1)) != "\\"))
            //{
            //    strFileName = (App.Path + ("\\"
            //                + (App.Title + ".log")));
            //}
            //else
            //{
            //    strFileName = (App.Path
            //                + (App.Title + ".log"));
            //}

            //if ((Dir(strFileName) != ""))
            //{
            //    intFileNum = FreeFile;
            //    Open;
            //    strFileName;
            //    for (object Input; ; Input++)
            //    {
            //        // TODO: # ... Warning!!! not translated
            //        intFileNum;
            //        intNumLines = 0;
            //        do
            //        {
            //            Line;
            //            Input;
            //            // TODO: # ... Warning!!! not translated
            //            intFileNum;
            //            strLine;
            //            object Preserve;
            //            arStrs[0, To, intNumLines];
            //            arStrs[intNumLines] = strLine;
            //            intNumLines = (intNumLines + 1);
            //        } while (EOF(intFileNum));

            //        Close;
            //        // TODO: # ... Warning!!! not translated
            //        intFileNum;
            //        intFileNum = FreeFile;
            //        Open;
            //        strFileName;
            //        for (object Output; ; Output++)
            //        {
            //            // TODO: # ... Warning!!! not translated
            //            intFileNum;
            //            Print;
            //            // TODO: # ... Warning!!! not translated
            //            intFileNum;
            //            strMessage;
            //            if ((UBound(arStrs) < 500))
            //            {
            //                for (intI = 0; (intI <= UBound(arStrs)); intI++)
            //                {
            //                    Print;
            //                    // TODO: # ... Warning!!! not translated
            //                    intFileNum;
            //                    arStrs[intI];
            //                }

            //            }
            //            else
            //            {
            //                for (intI = 0; (intI <= 499); intI++)
            //                {
            //                    Print;
            //                    // TODO: # ... Warning!!! not translated
            //                    intFileNum;
            //                    arStrs[intI];
            //                }

            //            }

            //            Close;
            //            // TODO: # ... Warning!!! not translated
            //            intFileNum;
            //            intFileNum = FreeFile;
            //            Open;
            //            strFileName;
            //            for (object Output; ; Output++)
            //            {
            //                // TODO: # ... Warning!!! not translated
            //                intFileNum;
            //                Print;
            //                // TODO: # ... Warning!!! not translated
            //                intFileNum;
            //                strMessage;
            //                Close;
            //                // TODO: # ... Warning!!! not translated
            //                intFileNum;
            //                blnLogFileOnly = false;
            //                //  RW 28-Mar-2012 for PreemL PQ-19
            //                // log error message into ABC_LOGGED_MESSAGES table
            //                ABCdataEnv.cmdLogMessage;
            //                1161;
            //                App.Title;
            //                cstrSys;
            //                strErr;
            //                "";
            //                "";
            //                "";
            //                "";
            //                "";
            //                gStrRetOK;
            //            LOCAL_ERROR:
            //        }

            //            // *********** ErrorLog ***********
            //        }

            //    }

            //}

        }
    }
}