using BlendMonitor.Entities;
using BlendMonitor.Model;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static BlendMonitor.Constans;

namespace BlendMonitor.Repository
{
    class BlendMonitorRepository : IBlendMonitorRepository
    {
        private BlendMonitorContext _blendMonitorContext;
        private readonly IConfiguration _configuration;
        private string programName;

        public BlendMonitorRepository(BlendMonitorContext blendMonitorContext, IConfiguration configuration)
        {
            _blendMonitorContext = blendMonitorContext;
            _configuration = configuration;
            programName = _configuration.GetSection("ProgramName").Value.ToUpper();
        }

        public async Task<List<AbcBlenders>> GetBlenders()
        {
            //select id, prdgrp_id, total_flow_tid, rbc_state_tid, rbc_mode_tid, upper(local_global_flag) local_global_flag, blend_id_tid,
            //product_tid, targ_vol_tid, rbc_vol_sp_fb_tid, total_vol_tid, targ_rate_tid, start_tid, stop_tid, pause_tid, restart_tid, 
            //download_ok_tid, downloading_tid, rbc_wdog_tid, in_ser_flag,blend_desc_tid, start_ok_tid, rundn_flag, swing_occurred_tid, 
            //swing_tid,comm_err_flag as comm_flag,on_spec_vol,download_type as download_type,optimize_flag,calcprop_flag, swing_exist_tid,
            //swing_vol_tid,anzr_start_delay, dcs_blname_fb_tid, grade_tid,
            //nvl(stop_opt_vol, 0) stop_opt_vol, ethanol_flag
            // from abc_blenders
            List<AbcBlenders> Data = await _blendMonitorContext.AbcBlenders.ToListAsync<AbcBlenders>();
            return Data;
        }

        public async Task<int> SetStartTime()
        {
            AbcPrograms Data = await _blendMonitorContext.AbcPrograms
                                  .Where<AbcPrograms>(row => row.Name.ToUpper() == programName)
                                  .SingleOrDefaultAsync<AbcPrograms>();
            Data.LastStartTime = new DateTime();
            return await _blendMonitorContext.SaveChangesAsync();
        }

        public async Task<AbcPrograms> ProcessEnabled()
        {
            //select enabled_flag, nvl(debug_flag, 'NO'), nvl(debug_level, 0) into enabled_flag, debug_flag, debug_level 
            //from abc_programs where upper(name) = upper(prg_name);
            AbcPrograms Data = await _blendMonitorContext.AbcPrograms
                                    .Where<AbcPrograms>(row => row.Name == programName)
                                    .SingleOrDefaultAsync<AbcPrograms>();
            Data.DebugFlag = (Data.DebugFlag == null) ? "NO" : Data.DebugFlag;
            Data.DebugLevel = (Data.DebugLevel == null) ? 0 : Data.DebugLevel;
            return Data;
        }

        public double? GetCycleTime(string name)
        {
            //select enabled_flag, nvl(debug_flag, 'NO'), nvl(debug_level, 0) into enabled_flag, debug_flag, debug_level 
            //from abc_programs where upper(name) = upper(prg_name);
            AbcPrograms Data = _blendMonitorContext.AbcPrograms
                                    .Where<AbcPrograms>(row => row.Name == name)
                                    .SingleOrDefault<AbcPrograms>();
            return Data.CycleTime;
        }

        public async Task<(ProjDfData, double)> getProjDefaults(ProjDfData gProjDfs)
        {
            //   SELECT
            //  @CMD_TIMEOUT = isnull(ABC_PROJ_DEFAULTS.START_TIMEOUT, 0),
            //  @ALLOW_START_STOP = isnull(upper(ABC_PROJ_DEFAULTS.ALLOW_START_AND_STOP_FLAG), 'NO'),
            //  @ALLOW_RATE_VOL_UPDS = isnull(upper(ABC_PROJ_DEFAULTS.ALLOW_RATE_AND_VOL_UPDS_FLAG), 'NO'),
            //  @ALLOW_COMP_UPDS = isnull(upper(ABC_PROJ_DEFAULTS.ALLOW_COMP_UPDATES), 'NO'),
            //  @VOL_TOLERANCE = ABC_PROJ_DEFAULTS.VOL_TOLERANCE,
            //  @RCP_TOLERANCE = ABC_PROJ_DEFAULTS.RECIPE_TOLERANCE,
            //  @MAX_INTV_LEN = ABC_PROJ_DEFAULTS.MAX_INTERVAL_LEN,
            //  @MIN_INTV_LEN = ABC_PROJ_DEFAULTS.MIN_INTERVAL_LEN,
            //  @MAX_MAT_COST = ABC_PROJ_DEFAULTS.MAX_MAT_COST,
            //  @MIN_MAT_COST = ABC_PROJ_DEFAULTS.MIN_MAT_COST
            //FROM dbo.ABC_PROJ_DEFAULTS

            AbcProjDefaults Data = await _blendMonitorContext.AbcProjDefaults
                                        .FirstOrDefaultAsync<AbcProjDefaults>();
            gProjDfs.dblCmdTimeout = Data.StartTimeout == null ? 0 : Convert.ToDouble(Data.StartTimeout);
            gProjDfs.strAllowStartStop = Data.AllowStartAndStopFlag == null ? "NO" : Data.AllowStartAndStopFlag;
            gProjDfs.strAllowRateVolUpds = Data.AllowRateAndVolUpdsFlag == null ? "NO" : Data.AllowRateAndVolUpdsFlag;
            gProjDfs.strAllowCompUpds = Data.AllowCompUpdates == null ? "NO" : Data.AllowCompUpdates;
            gProjDfs.vntVolTolr = Data.VolTolerance;
            gProjDfs.vntRcpTolr = Data.RecipeTolerance;
            gProjDfs.vntMaxIntvLen = Data.MaxIntervalLen;
            gProjDfs.vntMinIntvLen = Data.MinIntervalLen;
            gProjDfs.vntMaxMatCost = Data.MaxMatCost;
            gProjDfs.vntMinMatCost = Data.MinMatCost;

            gProjDfs.strLimsSampleStartStopType = Data.LimsCompStartStopType;
            gProjDfs.strAllowSCSampling = Data.LimsAllowCompSampling;
            gProjDfs.dblTotalizerTimestampTolerance = Data.TotalizerTimestampTolerance;
            gProjDfs.dblSleepTime = Data.BmonSleepTime;
            gProjDfs.sngFGEEtoh = Data.FgeEtoh;
            gProjDfs.sngMinEtoh = Data.MinEtoh;
            gProjDfs.strProjName = Data.Name;
            gProjDfs.strLIMSSeparateProps = Data.LimsSeparatePropsFlag;

            double gdblProjCycleTime = Data.CycleTime;

            return (gProjDfs, gdblProjCycleTime);

        }

        public async Task<int> ChkPendingOnBldr(double blenderId)
        {
            //select
            //      count(*) pendblends
            //from
            //      abc_blends b
            //where
            //      b.pending_state is not null
            //  and b.pending_state <> 'OPTIMIZING'
            //  and b.blend_state in ('READY', 'ACTIVE', 'LOADED', 'PAUSED')
            //  and b.blender_id = ?

            List<string> status = new List<string>() { "READY", "ACTIVE", "LOADED", "PAUSED" };
            List<AbcBlends> Data = await _blendMonitorContext.AbcBlends
                .Where<AbcBlends>(row => row.PendingState != null && row.PendingState != "OPTIMIZING"
                && status.Contains<string>(row.BlendState) && row.BlenderId == blenderId)
                .ToListAsync<AbcBlends>();

            return Data.Count;
        }

        public async Task<(DebugLevels, string, string)> GetBlenderDebugs(string strPrgName, string strDebugFlag, DebugLevels enumDebugLevel, int intBlenderID,
                                             string strBlenderDebugFlag, DebugLevels enumBlenderDebugLevel, string strBlenderName)
        {

            AbcBlenders Data = await _blendMonitorContext.AbcBlenders
                                    .Where<AbcBlenders>(row => row.Id == intBlenderID)
                                    .SingleOrDefaultAsync<AbcBlenders>();
            strBlenderDebugFlag = (Data.DebugFlag == null) ? "NO" : Data.DebugFlag;
            enumBlenderDebugLevel = (DebugLevels)((Data.DebugLevel == null) ? 0 : Data.DebugLevel);
            strBlenderName = Data.Name;

            if (strDebugFlag == "NO" && intBlenderID > 0)
            {
                if (enumBlenderDebugLevel == DebugLevels.High)
                {
                    var res = "";
                    await LogMessage(Convert.ToInt32(CommonMsgTmpIDs.COM_D6), strPrgName, cstrDebug, strBlenderDebugFlag,
                        HelperMethods.gArDebugLevelStrs(Convert.ToInt32(enumBlenderDebugLevel)),
                        strBlenderName, "", "", "", res);
                }
            }
            else
            {
                strBlenderDebugFlag = strDebugFlag;
                enumBlenderDebugLevel = enumDebugLevel;
            }

            if (strBlenderDebugFlag == "NO")
            {
                enumBlenderDebugLevel = 0;
            }
            return (enumBlenderDebugLevel, strBlenderDebugFlag, strBlenderName);
        }

        public async Task<List<AbcBlends>> GetActvBldsData(double blenderId)
        {
            //SELECT ID, UPPER(NAME) NAME, TARGET_VOL, TARGET_RATE, GRADE_ID, MIN_VOL, MAX_VOL, MIN_RATE, MAX_RATE,
            //    BLEND_STATE, CORRECTION_FACTOR, UPPER(CONTROL_MODE) CONTROL_MODE, CURRENT_VOL, ACTUAL_START,
            //    PRODUCT_ID, UPPER(PENDING_STATE) PENDING_STATE, DESCRIPTION, PREVIOUS_BLEND_ID, IGNORE_LINE_CONSTRAINTS,RAMPING_ACT_FLAG,
            //BIAS_OVERRIDE_FLAG, ETHANOL_BLDG_REQD_FLAG
            //FROM ABC_BLENDS WHERE BLENDER_ID = ? AND UPPER(BLEND_STATE) IN('LOADED', 'ACTIVE', 'PAUSED')
            List<string> status = new List<string>() { "LOADED", "ACTIVE", "PAUSED" };

            List<AbcBlends> Data = await _blendMonitorContext.AbcBlends
                                    .Where<AbcBlends>(row => row.BlenderId == blenderId && status.Contains(row.BlendState.ToUpper()))
                                    .ToListAsync<AbcBlends>();

            return Data;

        }
        public async Task<List<AbcBlendDest>> GetDestTkFlags(double lngBlendId)
        {
            //select tank_id, heel_volume, fix_heel_flag, in_use_flag, flush_tk_flag, 
            //    end_linefill_tk_flag, lineup_id from abc_blend_dest where blend_id = ?
            List<AbcBlendDest> Data = await _blendMonitorContext.AbcBlendDest
                                        .Where<AbcBlendDest>(row => row.BlendId == lngBlendId)
                                        .ToListAsync<AbcBlendDest>();
            return Data;
        }

        public async Task<List<AbcBlendSwings>> GetBldSwgTransferVol(double lngBlendId, double? lngFlushTankId, double? lngDestTkId)
        {
            //SELECT nvl(criteria_num_lmt,0) as bld_transfer_vol, swing_state
            //FROM abc_blend_swings
            //WHERE blend_id = ? and
            //from_tk_id = ? and to_tk_id = ?
            List<AbcBlendSwings> data = await _blendMonitorContext.AbcBlendSwings
                    .Where<AbcBlendSwings>(row => row.BlendId == lngBlendId && row.FromTkId == lngFlushTankId && row.ToTkId == lngDestTkId)
                    .ToListAsync<AbcBlendSwings>();
            return data;

        }

        public async Task<List<AbcBlends>> GetReadyBlds(double blenderId)
        {
            //SELECT ID, UPPER(NAME) NAME, TARGET_VOL, TARGET_RATE, GRADE_ID, MIN_VOL, MAX_VOL, MIN_RATE, MAX_RATE, BLEND_STATE, 
            //    CORRECTION_FACTOR, UPPER(CONTROL_MODE) CONTROL_MODE, CURRENT_VOL, ACTUAL_START, PRODUCT_ID, 
            //    UPPER(PENDING_STATE) PENDING_STATE,DESCRIPTION,PREVIOUS_BLEND_ID, 
            //    ETHANOL_BLDG_REQD_FLAG
            //     FROM ABC_BLENDS WHERE BLENDER_ID = ? AND UPPER(BLEND_STATE) = 'READY' and upper(pending_state) = 'DOWNLOADING'
            List<AbcBlends> data = await _blendMonitorContext.AbcBlends
                .Where<AbcBlends>(row => row.BlenderId == blenderId && row.BlendState.ToUpper() == "READY" && row.PendingState.ToUpper() == "DOWNLOADING")
                .ToListAsync<AbcBlends>();

            return data;
        }

        public async Task<int> SetPendingState(string state, double blendId)
        {
            //"update abc_blends set pending_state = ? where id = ?"
            AbcBlends Data = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Id == blendId)
                                .SingleAsync<AbcBlends>();
            Data.PendingState = state;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetBlenderErrorFlag(string flag, string name)
        {
            //"UPDATE abc_blenders SET comm_err_flag='YES' " & _
            //"WHERE name='" & gstrBldrName & "'"
            AbcBlenders Data = await _blendMonitorContext.AbcBlenders
                                        .Where<AbcBlenders>(row => row.Name == name)
                                        .FirstOrDefaultAsync<AbcBlenders>();
            Data.CommErrFlag = flag;
            return await _blendMonitorContext.SaveChangesAsync();
        }

        public async Task<int> SetBlenderErrFlag(string prgmError, double belnderId, string text)
        {
            //update abc_blenders set program_error = ? where id = ? and upper(program_error) like ? || '%'
            List<AbcBlenders> DataList = await _blendMonitorContext.AbcBlenders
                   .Where<AbcBlenders>(row => row.Id == belnderId)
                   .ToListAsync<AbcBlenders>();
            if (DataList[0].ProgramError != null)
            {
                AbcBlenders Data = DataList.Where<AbcBlenders>(row => (row.ProgramError.Contains(text) || row.ProgramError.Contains("%"))).FirstOrDefault<AbcBlenders>();
                Data.ProgramError = prgmError;
                return await _blendMonitorContext.SaveChangesAsync();
            }
            return 0;
        }

        public async Task<string> GetCommWDTag()
        {
            //SELECT DCS_COMM_FLAG FROM ABC_PROJ_DEFAULTS
            return await _blendMonitorContext.AbcProjDefaults.Select(row => row.DcsCommFlag).FirstOrDefaultAsync<string>();
        }

        public async Task<AbcProjDefaults> SwgDefTimeOut()
        {
            //select swing_time_out, download_type, wdog_limit, STARBLEND_INST_PATH 
            //from abc_proj_defaults
            return await _blendMonitorContext.AbcProjDefaults.FirstOrDefaultAsync<AbcProjDefaults>();

        }

        public async Task<(string, double, DateTime, string, string, string, string)> GetTagValAndFlags(double? RBC_WDOG_TID, string vntDummy,
            double vntTagVal, DateTime? vntTagValTime, string vntTagValQlt, string readEnabled, string scanEnabled, string vntScanRateName)
        {
            // [dbo].[ABC_SINGLETONS$GET_TAG_VAL_AND_FLAGS]
            //@TAG_ID float(53),
            //@TAG_NAME varchar(max)  OUTPUT,
            //@TAG_VAL float(53)  OUTPUT,
            //@VAL_TIME datetime2(0)  OUTPUT,
            //@VAL_QUALITY varchar(max)  OUTPUT,
            //@READ_ENABLED varchar(max)  OUTPUT,
            //@SCAN_ENABLED varchar(max)  OUTPUT,
            //@SCAN_GRP_NAME varchar(max)  OUTPUT
            try
            {
                SqlParameter tagId = new SqlParameter("@TAG_ID", RBC_WDOG_TID);

                // declaring output param
                SqlParameter tag_name = new SqlParameter();
                tag_name.ParameterName = "@TAG_NAME";
                tag_name.Value = vntDummy;
                tag_name.Direction = ParameterDirection.Output;

                SqlParameter tag_val = new SqlParameter();
                tag_val.ParameterName = "@TAG_VAL";
                tag_val.Value = vntTagVal;
                tag_val.Direction = ParameterDirection.Output;

                SqlParameter val_time = new SqlParameter();
                val_time.ParameterName = "@VAL_TIME";
                val_time.Value = vntTagValTime;
                val_time.Direction = ParameterDirection.Output;

                SqlParameter val_Quality = new SqlParameter();
                val_Quality.ParameterName = "@VAL_QUALITY";
                val_Quality.Value = vntTagValQlt;
                val_Quality.Direction = ParameterDirection.Output;

                SqlParameter read_Enabled = new SqlParameter();
                read_Enabled.ParameterName = "@READ_ENABLED";
                read_Enabled.Value = readEnabled;
                read_Enabled.Direction = ParameterDirection.Output;

                SqlParameter scan_enabled = new SqlParameter();
                scan_enabled.ParameterName = "@SCAN_ENABLED";
                scan_enabled.Value = scanEnabled;
                scan_enabled.Direction = ParameterDirection.Output;

                SqlParameter scan_grp_name = new SqlParameter();
                scan_grp_name.ParameterName = "@SCAN_GRP_NAME";
                scan_grp_name.Value = vntScanRateName;
                scan_grp_name.Direction = ParameterDirection.Output;

                // Processing.  
                string sqlQuery = "[dbo].[ABC_SINGLETONS$GET_TAG_VAL_AND_FLAGS]" +
                                    "@TAG_ID," +
                                    "@TAG_NAME OUT," +
                                    "@TAG_VAL OUT," +
                                    "@VAL_TIME OUT," +
                                    "@VAL_QUALITY OUT," +
                                    "@READ_ENABLED OUT," +
                                    "@SCAN_ENABLED OUT," +
                                    "@SCAN_GRP_NAME OUT";

                await _blendMonitorContext.Database.ExecuteSqlRawAsync(sqlQuery, tagId, tag_name, tag_val, val_time, val_Quality, read_Enabled, scan_enabled, scan_grp_name);
                return (tag_name.Value.ToString(), Convert.ToDouble(tag_val.Value), Convert.ToDateTime(val_time.Value), val_Quality.Value.ToString(),
                    read_Enabled.Value.ToString(), scan_enabled.Value.ToString(), scan_grp_name.Value.ToString());
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<double?> GetScanRate(string name)
        {
            //select scanrate from abc_scan_groups where name =?
            return await _blendMonitorContext.AbcScanGroups
                    .Where<AbcScanGroups>(row => row.Name == name)
                    .Select(row => row.Scanrate)
                    .FirstOrDefaultAsync<double?>();
        }

        public async Task<int> SetWriteTagVal(int intUpperLimit, string flag, double? RBC_WDOG_TID)
        {
            //update abc_tags set write_value = ?, write_now_flag = ? where id = ?
            AbcTags Data = await _blendMonitorContext.AbcTags
                            .Where<AbcTags>(row => row.Id == RBC_WDOG_TID)
                            .SingleOrDefaultAsync<AbcTags>();
            Data.WriteValue = intUpperLimit;
            Data.WriteNowFlag = flag;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<AbcBlends>> CheckBlds(double blenderId)
        {
            //SELECT ID, UPPER(NAME) NAME, UPPER(PENDING_STATE) PENDING_STATE 
            //FROM ABC_BLENDS
            //WHERE BLENDER_ID = ? AND UPPER(BLEND_STATE) IN('LOADED', 'ACTIVE', 'PAUSED')
            List<string> Status = new List<string>() { "LOADED", "ACTIVE", "PAUSED" };
            List<AbcBlends> Data = await _blendMonitorContext.AbcBlends
                                    .Where<AbcBlends>(row => row.BlenderId == blenderId && Status.Contains(row.BlendState.ToUpper()))
                                    .ToListAsync<AbcBlends>();
            return Data;
        }

        public async Task<List<AbcBlends>> GetBlendReturning(double blenderId)
        {
            //SELECT ID, UPPER(NAME) NAME, UPPER(BLEND_STATE)  BLEND_STATE FROM ABC_BLENDS 
            //WHERE BLENDER_ID = ? AND UPPER(PENDING_STATE) ='RETURNING'
            List<AbcBlends> Data = await _blendMonitorContext.AbcBlends
                                    .Where<AbcBlends>(row => row.BlenderId == blenderId && row.PendingState == "RETURNING")
                                    .ToListAsync<AbcBlends>();
            return Data;
        }

        public async Task<AbcTags> GetTagNameAndVal(double? tagId)
        {
            //select upper(name), read_value into tag_name, tag_val from abc_tags 
            //where id = tag_id and upper(value_quality) = 'GOOD';
            AbcTags Data = null;
            if (tagId != null)
            {
                Data = await _blendMonitorContext.AbcTags
                        .Where<AbcTags>(row => row.Id == tagId && row.ValueQuality.ToUpper() == "GOOD")
                        .SingleOrDefaultAsync<AbcTags>();
                return Data;
            }
            Data.Name = null;
            Data.ReadValue = null;
            return Data;

        }

        public async Task<double?> GetPrdgrpCycleTime(double prdgrpId)
        {
            //select cycle_time into cycle_time from abc_prdgrps where id = prdgrp_id;
            return await _blendMonitorContext.AbcPrdgrps.Where<AbcPrdgrps>(row => row.Id == prdgrpId)
                        .Select(row => row.CycleTime).FirstOrDefaultAsync<double?>();
        }
        public async Task<List<AbcProperties>> GetEtohPropIds()
        {
            //select id, name from abc_properties
            //where name = 'ETOH' or name = 'ETOH_ETOH'
            List<AbcProperties> Data = await _blendMonitorContext.AbcProperties
                                        .Where<AbcProperties>(row => row.Name == "ETOH" || row.Name == "ETOH_ETOH")
                                        .ToListAsync<AbcProperties>();
            return Data;
        }
        public async Task<List<AbcTranstxt>> GetTranstxtData(string text)
        {
            //SELECT VALUE,USER_VALUE FROM  ABC_TRANSTXT WHERE WORD_SET=?
            return await _blendMonitorContext.AbcTranstxt
                    .Where<AbcTranstxt>(row => row.WordSet == text)
                    .ToListAsync<AbcTranstxt>();
        }
        public async Task<AbcTags> GetStrTagNameAndVal(double? tagId)
        {
            //select upper(name), upper(read_string) into tag_name, tag_val from abc_tags where id =
            //tag_id and upper(value_quality) = 'GOOD';
            AbcTags Data = null;
            if (tagId != null)
            {
                Data = await _blendMonitorContext.AbcTags
                        .Where<AbcTags>(row => row.Id == tagId && row.ValueQuality.ToUpper() == "GOOD")
                        .SingleOrDefaultAsync<AbcTags>();
                return Data;
            }
            Data.Name = null;
            Data.ReadString = null;
            return Data;
        }

        public async Task<DateTime?> GetIntvStartTime(double blendId, int sequence)
        {
            //select nvl(starttime, to_date('1/1/1900','mm/dd/yy')) into start_time from
            //abc_blend_intervals where blend_id = blend_id1 and sequence = sequence1;
            AbcBlendIntervals Data = await _blendMonitorContext.AbcBlendIntervals
                                        .Where<AbcBlendIntervals>(row => row.BlendId == blendId && row.Sequence == sequence)
                                        .FirstOrDefaultAsync<AbcBlendIntervals>();
            return Data.Starttime = (Data.Starttime == null) ? DateTime.ParseExact("1/1/1900", "MM/dd/yy", CultureInfo.InvariantCulture) :
                                  DateTime.ParseExact(Data.Starttime.ToString(), "MM/dd/yy", CultureInfo.InvariantCulture);

        }
        public async Task<string> GetRbcStName(string val)
        {
            //select upper(name) as rbc_st_name from abc_rbc_states where upper(alias) = ?
            return await _blendMonitorContext.AbcRbcStates
                        .Where<AbcRbcStates>(row => row.Alias.ToUpper() == val.ToUpper())
                        .Select(row => row.Name.ToUpper())
                        .FirstOrDefaultAsync<string>();
        }
        public async Task<int> SetBlendEndTime(double id)
        {
            //"update abc_blends set actual_end = sysdate where id = ?"
            AbcBlends Data = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Id == id)
                                .SingleOrDefaultAsync<AbcBlends>();
            Data.ActualEnd = DateTime.Now;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<DateTime> GetCurTime()
        {
            try
            {
                DateTime? value = null;
                // declaring output param
                SqlParameter p_out = new SqlParameter();
                p_out.ParameterName = "@CUR_TIME";
                p_out.Value = value;
                p_out.Direction = ParameterDirection.Output;

                // Processing.  
                string sqlQuery = "[dbo].[ABC_SINGLETONS$GET_CURRENT_TIME]" +
                                    " @CUR_TIME OUT";

                int Data = await _blendMonitorContext.Database.ExecuteSqlRawAsync(sqlQuery, p_out);
                return Convert.ToDateTime(p_out.Value);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<DateTime> GetLastRunTime(string programeName)
        {
            try
            {
                SqlParameter source = new SqlParameter("@PRG_NAME", programeName);
                DateTime? lastRunTime = null;

                // declaring output param
                SqlParameter p_out = new SqlParameter();
                p_out.ParameterName = "@LAST_RUN_TIME";
                p_out.Value = lastRunTime;
                p_out.Direction = ParameterDirection.Output;

                // Processing.  
                string sqlQuery = "[dbo].[ABC_SINGLETONS$GET_LAST_RUN_TIME]" +
                                    "@PRG_NAME" +
                                    "@LAST_RUN_TIME OUT";

                int Data = await _blendMonitorContext.Database.ExecuteSqlRawAsync(sqlQuery, source, p_out);
                return Convert.ToDateTime(p_out.Value);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<int> SetPaceActFlag(string flag, double blendId)
        {
            //update abc_blends set pacing_act_flag = ? where id = ?
            AbcBlends Data = await _blendMonitorContext.AbcBlends
                               .Where<AbcBlends>(row => row.Id == blendId)
                               .SingleOrDefaultAsync<AbcBlends>();
            Data.PacingActFlag = flag;
            return await _blendMonitorContext.SaveChangesAsync();
        }

        public async Task<double> GetDestTankId(double blendId)
        {
            var tankId = await _blendMonitorContext.AbcBlendDest.Where(b => b.BlendId.Equals(blendId) && b.InUseFlag.Equals("YES")).FirstOrDefaultAsync();//.TankId;
            return tankId.TankId;
        }

        public async Task<List<MatSwingId>> GetMatSwingId(int intPrdgrpID)
        {
            //select DISTINCT bc.mat_id, bc.swing_tid 
            //from abc_blender_comps bc, abc_blenders b 
            //where bc.blender_id=b.id and b.prdgrp_id=" + intPrdgrpID
            List<MatSwingId> Data = await (from abc in _blendMonitorContext.AbcBlenderComps
                                           from ab in _blendMonitorContext.AbcBlenders
                                           where abc.BlenderId == ab.Id && ab.PrdgrpId == intPrdgrpID
                                           select new MatSwingId
                                           {
                                               MatId = abc.MatId,
                                               SwingTId = abc.SwingTid
                                           }).Distinct<MatSwingId>().ToListAsync<MatSwingId>();
            return Data;
        }

        public async Task<int> CheckBlendsCount(double MatId, double BlenderId)
        {
            List<string> states = new List<string>() { "ACTIVE", "PAUSED" };
            List<string> swgstates = new List<string>() { "ACTIVE" };
            // select bs.blend_id, bs.from_tk_id  
            // from abc_blend_swings bs, abc_tanks tk, abc_blends bld, abc_blend_sources srce 
            // where bs.blend_id=bld.id and bs.from_tk_id=tk.id and bs.blend_id=srce.blend_id and
            // bs.from_tk_id=srce.tank_id and srce.mat_id=tk.mat_id and srce.in_use_flag=\'YES\' and
            // srce.mat_id=" +(vntBlendersComps[0, intI] and bld.blender_id <> intBlenderID  and 
            // bld.blend_state IN (\'ACTIVE\',\'PAUSED\') and bs.swing_type = \'COMPONENT\' and bs.swing_state IN (\'ACTIVE\')
            List<double> Data = await (from bs in _blendMonitorContext.AbcBlendSwings
                                       from tk in _blendMonitorContext.AbcTanks
                                       from bld in _blendMonitorContext.AbcBlends
                                       from srce in _blendMonitorContext.AbcBlendSources
                                       where bs.BlendId == bld.Id && bs.FromTkId == tk.Id && bs.BlendId == srce.BlendId &&
                                       bs.FromTkId == srce.TankId && srce.MatId == tk.MatId && srce.InUseFlag == "YES" &&
                                       srce.MatId == MatId && bld.BlenderId != BlenderId &&
                                        states.Contains<string>(bld.BlendState) && bs.SwingType == "COMPONENT" && swgstates.Contains<string>(bs.SwingState)
                                       select bs.BlendId).ToListAsync<double>();
            return Data.Count();
        }

        public async Task<List<AbcBlends>> GetBlendState(double blendId)
        {
            //Select blend_state,update_heel_flag, heel_upd_occurred_flag  from abc_blends where id=?
            return await _blendMonitorContext.AbcBlends
                    .Where<AbcBlends>(row => row.Id == blendId)
                    .ToListAsync<AbcBlends>();

        }
        public async Task<List<DCSProdLineupNum>> GetDCSProdLineupNum(double? lineUpId)
        {
            //select name as lineup_name, nvl(dcs_lineup_num, -1) as dcs_lineup_num, volume as dest_line_volume
            //from abc_prod_lineups where id =?
            return await _blendMonitorContext.AbcProdLineups
                    .Where<AbcProdLineups>(row => row.Id == lineUpId)
                    .Select(row => new DCSProdLineupNum {
                        DCSLineUpNum = row.DcsLineupNum,
                        DestLineVolume = row.Volume,
                        LineUpName = row.Name
                    })
                    .ToListAsync<DCSProdLineupNum>();
        }

        public async Task<double?> GetHeelVol(double? tankId)
        {
            //SELECT(T1.READ_VALUE + T2.READ_VALUE) AS HEEL_VOLUME
            //FROM ABC_TANKS, ABC_TAGS T1,ABC_TAGS T2
            //WHERE ABC_TANKS.ID = 1 AND
            //ABC_TANKS.AVAIL_VOL_ID = T1.ID AND
            //ABC_TANKS.MIN_VOL_TID = T2.ID
            return await (from AbT in _blendMonitorContext.AbcTanks
                          from T1 in _blendMonitorContext.AbcTags
                          from T2 in _blendMonitorContext.AbcTags
                          where AbT.Id == tankId && AbT.AvailVolId == T1.Id
                          && AbT.MinVolTid == T2.Id
                          select T1.ReadValue + T2.ReadValue)
                   .FirstOrDefaultAsync<double?>();
        }

        public async Task<int> SetHeelVol(double? volume, double blendId, double? tankId)
        {
            //update abc_blend_dest set heel_volume = ? where blend_id = ? and tank_id = ?
            AbcBlendDest Data = await _blendMonitorContext.AbcBlendDest
                .Where<AbcBlendDest>(row => row.BlendId == blendId && row.TankId == tankId)
                .FirstOrDefaultAsync<AbcBlendDest>();
            Data.HeelVolume = volume;
            return await _blendMonitorContext.SaveChangesAsync();
        }

        public async Task<int> SetHeelUpdated(double blendId)
        {
            //UPDATE abc_blends SET HEEL_UPD_OCCURRED_FLAG='YES' " & _
            //"WHERE id=" & curblend.lngID
            AbcBlends Data = await _blendMonitorContext.AbcBlends
                .Where<AbcBlends>(row => row.Id == blendId)
                .FirstOrDefaultAsync<AbcBlends>();
            Data.HeelUpdOccurredFlag = "YES";
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetIgnoreLineCOnstraint(double blendId)
        {
            //("UPDATE abc_blends SET ignore_line_constraints=\'NO\' " + ("WHERE id=" + curblend.lngID));
            AbcBlends Data = await _blendMonitorContext.AbcBlends
                .Where<AbcBlends>(row => row.Id == blendId)
                .FirstOrDefaultAsync<AbcBlends>();
            Data.IgnoreLineConstraints = "NO";
            return await _blendMonitorContext.SaveChangesAsync();
        }

        public async Task<int> SetBiasOverrideFlag(double blendId)
        {
            //("UPDATE abc_blends SET bias_override_flag=\'NO\' " + ("WHERE id=" + curblend.lngID));
            AbcBlends Data = await _blendMonitorContext.AbcBlends
                .Where<AbcBlends>(row => row.Id == blendId)
                .FirstOrDefaultAsync<AbcBlends>();
            Data.BiasOverrideFlag = "NO";
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetRampingActFlag(double blendId, string value)
        {
            //"UPDATE abc_blends SET ramping_act_flag=\'YES\' " + ("WHERE id=" + curblend.lngID)
            AbcBlends Data = await _blendMonitorContext.AbcBlends
                .Where<AbcBlends>(row => row.Id == blendId)
                .FirstOrDefaultAsync<AbcBlends>();
            Data.RampingActFlag = value;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetBlendStartTime(double blendId)
        {
            //update abc_blends set actual_start = sysdate where id = ?
            AbcBlends Data = await _blendMonitorContext.AbcBlends
                .Where<AbcBlends>(row => row.Id == blendId)
                .FirstOrDefaultAsync<AbcBlends>();
            Data.ActualStart = DateTime.Now;
            return await _blendMonitorContext.SaveChangesAsync();
        }

        public async Task<List<HdrAnzrsData>> GetHdrAnzrsData(double blenderId)
        {
            //SELECT DISTINCT HDR.ANZ_ID, ANZ.NAME AS ANZ_NAME,
            //ANZ.STATE_TAG_ID, TAG.READ_VALUE AS DCS_ANZ_VALUE, 
            //ANZRST.STATE AS ANZ_STATE, ANZ.ABC_SERVICE_FLAG

            //FROM ABC_ANZS ANZ, ABC_ANZ_HDR_PROPS HDR, ABC_ANZS_STATES ANZRST,
            //ABC_TAGS TAG

            //WHERE HDR.ANZ_ID = ANZ.ID(+) AND
            //ANZ.STATE_TAG_ID = TAG.ID(+) AND
            //ANZRST.VALUE = TAG.READ_VALUE AND
            //HDR.BLENDER_ID =?

            return await (from ANZ in _blendMonitorContext.AbcAnzs
                          from HDR in _blendMonitorContext.AbcAnzHdrProps
                          from ANZRST in _blendMonitorContext.AbcAnzsStates
                          from TAG in _blendMonitorContext.AbcTags
                          where HDR.AnzId == ANZ.Id &&
                          ANZ.StateTagId == TAG.Id &&
                          ANZRST.Value == TAG.ReadValue &&
                          HDR.BlenderId == blenderId
                          select new HdrAnzrsData
                          {
                              AnzId = HDR.AnzId,
                              AnzName = ANZ.Name,
                              StateTagId = ANZ.StateTagId,
                              DcsAnzValue = TAG.ReadValue,
                              AnzState = ANZRST.State,
                              AbcServiceFlag = ANZ.AbcServiceFlag
                          }).Distinct<HdrAnzrsData>().ToListAsync<HdrAnzrsData>();
        }
        public async Task<List<AbcBlendIntervals>> GetBlendIntvs(double blendId)
        {
            //select sequence, starttime, stoptime from abc_blend_intervals where blend_id = ? and sequence > 0 order by sequence
            return await _blendMonitorContext.AbcBlendIntervals
                .Where<AbcBlendIntervals>(row => row.BlendId == blendId && row.Sequence > 0)
                .OrderBy(row => row.Sequence)
                .ToListAsync<AbcBlendIntervals>();
        }

        private async Task<AbcBlendIntervalProps> GetBias(double blendId, int sequence)
        {
            // select bip2.bias from abc_blend_interval_props bip2
            //where bip2.blend_id = bip1.blend_id and bip2.sequence = ? and
            //bip2.prop_id = bip1.prop_id

            return await _blendMonitorContext.AbcBlendIntervalProps
                .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId && row.Sequence == sequence)
                .FirstOrDefaultAsync<AbcBlendIntervalProps>();
        }
        public async Task<int> CopyPrevBias(int sequence1, int sequence2, int sequence3, double blendId, int sequence4)
        {
            //update abc_blend_interval_props bip1 set
            //bip1.bias = (select bip2.bias from abc_blend_interval_props bip2
            //where bip2.blend_id = bip1.blend_id and bip2.sequence = ? and
            //bip2.prop_id = bip1.prop_id),
            //bip1.unfilt_bias = (select bip2.bias from abc_blend_interval_props bip2
            //where bip2.blend_id = bip1.blend_id and bip2.sequence = ? and
            //bip2.prop_id = bip1.prop_id), 
            //bip1.biascalc_current = (select bip2.biascalc_current
            //  from abc_blend_interval_props bip2
            //where bip2.blend_id = bip1.blend_id and bip2.sequence = ? and
            //bip2.prop_id = bip1.prop_id) 
            //where bip1.blend_id = ? and
            //bip1.sequence = ?

            AbcBlendIntervalProps Data = await _blendMonitorContext.AbcBlendIntervalProps
                   .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId && row.Sequence == sequence4)
                   .FirstOrDefaultAsync<AbcBlendIntervalProps>();
            Data.Bias = (await GetBias(Data.BlendId, sequence1)).Bias;
            Data.UnfiltBias = (await GetBias(Data.BlendId, sequence2)).Bias;
            Data.BiascalcCurrent = (await GetBias(Data.BlendId, sequence3)).BiascalcCurrent;

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<string> GetCompName(double MatId)
        {
            //select name into material_name from abc_materials where id = material_id;

            AbcMaterials Data = await _blendMonitorContext.AbcMaterials.Where(row => row.Id == MatId).SingleOrDefaultAsync<AbcMaterials>();
            return Data.Name;
        }

        public async Task<string> GetGradeName(double gradeId) {
            //select name into grade_name from abc_grades where id = grade_id;
            return await _blendMonitorContext.AbcGrades
                        .Where<AbcGrades>(row => row.Id == gradeId)
                        .Select(row => row.Name)
                        .FirstOrDefaultAsync<string>();
        }

        public async Task<int> CheckIntv(double blendId, int intervalNum)
        {
            //SELECT count(*) as intvs
            //FROM abc_blend_intervals
            //WHERE blend_id = ?
            //AND sequence = ?
            return await _blendMonitorContext.AbcBlendIntervals
                    .Where<AbcBlendIntervals>(row => row.BlendId == blendId && row.Sequence == intervalNum)
                    .CountAsync<AbcBlendIntervals>();

        }

        public async Task<int> AddNewBldIntv(double lngBldID, int intIntvNum, DateTime dteCurTime)
        {
            //insert into abc_blend_intervals(blend_id, sequence, starttime) values(?, ?,?)
            AbcBlendIntervals Data = new AbcBlendIntervals();
            Data.BlendId = lngBldID;
            Data.Sequence = intIntvNum;
            Data.Starttime = dteCurTime;
            await _blendMonitorContext.AbcBlendIntervals
                .AddAsync(Data);
            return 0;
        }

        public async Task<int> AddNewIntvComps(int intIntvNum, double lngBldID)
        {
            //insert into abc_blend_interval_comps (blend_id, sequence, mat_id, volume) 
            //select blend_id, ?, mat_id, 0 from abc_blend_comps where blend_id = ?

            AbcBlendIntervalComps Data = new AbcBlendIntervalComps();

            AbcBlendComps AbcBlendCompsData = await _blendMonitorContext.AbcBlendComps
                                    .Where<AbcBlendComps>(row => row.BlendId == lngBldID)
                                    .FirstOrDefaultAsync<AbcBlendComps>();

            Data.BlendId = AbcBlendCompsData.BlendId;
            Data.Sequence = intIntvNum;
            Data.MatId = AbcBlendCompsData.MatId;
            Data.Volume = 0;

            await _blendMonitorContext.AbcBlendIntervalComps.AddAsync(Data);

            return 0;
        }

        public async Task<int> AddNewIntvProps(int intIntvNum, double lngBldID)
        {
            //insert into abc_blend_interval_props (blend_id, sequence, prop_id, anz_good_flag, calc_property_flag) 
            //select blend_id, ?, prop_id, 'NO', 'NO' from abc_blend_props where blend_id = ?
            AbcBlendIntervalProps Data = new AbcBlendIntervalProps();

            AbcBlendProps AbcBlendPropsData = await _blendMonitorContext.AbcBlendProps
                                    .Where<AbcBlendProps>(row => row.BlendId == lngBldID)
                                    .FirstOrDefaultAsync<AbcBlendProps>();

            Data.BlendId = AbcBlendPropsData.BlendId;
            Data.Sequence = intIntvNum;
            Data.PropId = AbcBlendPropsData.PropId;
            Data.AnzGoodFlag = "NO";
            Data.CalcPropertyFlag = "NO";

            await _blendMonitorContext.AbcBlendIntervalProps.AddAsync(Data);

            return 0;
        }
        public async Task<int> SetNewIntv(int volume, double lngBldID, int intIntvNum)
        {
            //update abc_blend_intervals set volume = ? where blend_id = ? and sequence = ?            
            AbcBlendIntervals Data = await _blendMonitorContext.AbcBlendIntervals
                       .Where<AbcBlendIntervals>(row => row.BlendId == lngBldID && row.Sequence == intIntvNum)
                       .FirstOrDefaultAsync<AbcBlendIntervals>();
            Data.Volume = volume;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetBiasCalcCurrent(double lngBldID, int intIntvNum)
        {
            string sql = "   UPDATE bip1   " +
                         "   set bip1.bias = (select bp.initial_bias from abc_blend_props bp   " +
                         "   				where bp.blend_id = bip1.blend_id and bp.prop_id = bip1.prop_id)  " +
                         "   ,bip1.Unfilt_bias = (select bp.initial_bias from abc_blend_props bp where bp.blend_id = bip1.blend_id and bp.prop_id = bip1.prop_id)  " +
                         "   ,bip1.biascalc_current = (select pp.biascalc_default from abc_prdgrp_props pp   " +
                         "   							where pp.prdgrp_id = (select bldr.prdgrp_id from abc_blenders bldr, abc_blends bld   " +
                         "   												   where bld.blender_id=bldr.id and bld.id = " + lngBldID + ") and  " +
                         "   								  pp.prop_id = bip1.prop_id)  " +
                         "   from abc_blend_interval_props bip1  " +
                         "  where bip1.blend_id = " + lngBldID + " and bip1.sequence = " + intIntvNum;

            return await _blendMonitorContext.Database.ExecuteSqlRawAsync(sql);
        }

        public async Task<int> SetBiasCalcCurrent2(double lngBldID, int intIntvNum)
        {
            string sql = "   UPDATE bip1   " +
                         "   set bip1.biascalc_current = (select pp.alt_biascalc_default from abc_prdgrp_props pp   " +
                         "   							 where pp.prdgrp_id = (select bldr.prdgrp_id from abc_blenders bldr, abc_blends bld   " +
                         "   												   where bld.blender_id = bldr.id and bld.id = " + lngBldID + ")  " +
                         "   							 and pp.prop_id = bip1.prop_id)  " +
                         "   from abc_blend_interval_props bip1  " +
                         "   where bip1.blend_id = " + lngBldID + "   " +
                         "  and bip1.sequence = " + intIntvNum;
            return await _blendMonitorContext.Database.ExecuteSqlRawAsync(sql);
        }

        public async Task<int> SetBiasCalcCurrent3(double lngBldID, int intIntvNum)
        {
            string sql = "   UPDATE bip1   " +
                         "   set bip1.bias = (select bp.initial_bias from abc_blend_props bp   " +
                         "   				 where bp.blend_id = bip1.blend_id and bp.prop_id = bip1.prop_id),  " +
                         "   bip1.Unfilt_bias = (select bp.initial_bias from abc_blend_props bp  " +
                         "   					where bp.blend_id = bip1.blend_id and bp.prop_id = bip1.prop_id),  " +
                         "   bip1.biascalc_current = (select bip2.biascalc_current from abc_blend_interval_props bip2   " +
                         "   						 where bip2.blend_id = bip1.blend_id and bip2.sequence = " + (intIntvNum - 1) + "" +
                         "   						 and bip2.prop_id = bip1.prop_id)  " +
                         "   from abc_blend_interval_props bip1  " +
                         "   where bip1.blend_id = " + lngBldID + "  and bip1.sequence = " + intIntvNum;
            return await _blendMonitorContext.Database.ExecuteSqlRawAsync(sql);
        }

        public async Task<List<string>> GetPrdTankType(double blendId)
        {
            //select t.SOURCE_DESTN_TYPE 
            //from abc_tanks t, abc_blend_dest bd, abc_blends b 
            //where t.id = bd.tank_id and bd.blend_id = b.id and bd.in_use_flag = 'YES' and bd.blend_id =?
            return await (from t in _blendMonitorContext.AbcTanks
                          from bd in _blendMonitorContext.AbcBlendDest
                          from b in _blendMonitorContext.AbcBlends
                          where t.Id == bd.TankId && bd.BlendId == b.Id && bd.InUseFlag == "YES" && bd.BlendId == blendId
                          select t.SourceDestnType).ToListAsync<String>();
        }

        public async Task<int> SetIntvEndTime(DateTime gDteCurTime, double blendId, double vntIntvNum)
        {
            //update abc_blend_intervals set stoptime = ? where blend_id = ? and sequence = ?
            AbcBlendIntervals Data = await _blendMonitorContext.AbcBlendIntervals
                        .Where<AbcBlendIntervals>(row => row.BlendId == blendId && row.Sequence == vntIntvNum)
                        .FirstOrDefaultAsync<AbcBlendIntervals>();
            Data.Stoptime = gDteCurTime;
            return await _blendMonitorContext.SaveChangesAsync();

        }

        public async Task<List<IntComps>> GetIntComps(double blendId, int curIntrvl)
        {
            //select upper(abc_materials.name) name, abc_blend_interval_comps.volume, abc_blend_interval_comps.sp_recipe,abc_materials.id
            //from abc_materials, abc_blend_interval_comps 
            //where abc_blend_interval_comps.blend_id = ? and abc_blend_interval_comps.sequence = ? and 
            //abc_materials.id = abc_blend_interval_comps.mat_id

            return await (from am in _blendMonitorContext.AbcMaterials
                          from abic in _blendMonitorContext.AbcBlendIntervalComps
                          where abic.BlendId == blendId && abic.Sequence == curIntrvl && am.Id == abic.MatId
                          select new IntComps
                          {
                              Name = am.Name.ToUpper(),
                              Volume = abic.Volume,
                              SpRecipe = abic.SpRecipe,
                              Id = am.Id
                          }).ToListAsync<IntComps>();
        }
        public async Task<List<BldCompUsage>> GetBldCompUsage(double lngBlendId, double? sngMatId)
        {
            //SELECT BLDMAT.USAGE_ID,USAGE.NAME AS USAGE_NAME, USAGE.ALIAS AS USAGE_ALIAS,  BLDMAT.RCP_CONSTRAINT_TYPE FROM
            //ABC_BLEND_COMPS BLDMAT, ABC_USAGES USAGE WHERE
            //BLDMAT.BLEND_ID = ? AND BLDMAT.MAT_ID = ?
            //AND BLDMAT.USAGE_ID = USAGE.ID(+)

            return await (from BLDMAT in _blendMonitorContext.AbcBlendComps
                          from USAGE in _blendMonitorContext.AbcUsages
                          where BLDMAT.BlendId == lngBlendId && BLDMAT.MatId == sngMatId
                           && BLDMAT.UsageId == USAGE.Id
                          select new BldCompUsage
                          {
                              UsageId = BLDMAT.UsageId,
                              UsageName = USAGE.Name,
                              UsageAlias = USAGE.Alias,
                              RcpConstraintType = BLDMAT.RcpConstraintType
                          }).ToListAsync<BldCompUsage>();

        }
        public async Task<List<CompTankProps>> GetCompTankProps(double lngBlendId)
        {
            //select abc_blend_sources.mat_id, abc_blend_sources.tank_id, abc_tank_props.prop_id,
            //upper(abc_prop_sources.name) src_name, abc_tank_props.value, abc_tank_props.value_time, 
            //upper(abc_tank_props.good_flag) good_flag 

            //from abc_blend_sources, abc_tank_props, abc_prop_sources 

            //where abc_blend_sources.blend_id = ? and upper(abc_blend_sources.in_use_flag) = 'YES' 
            //and abc_blend_sources.tank_id = abc_tank_props.tank_id and abc_tank_props.source_id = abc_prop_sources.id 
            //and upper(abc_tank_props.selected_flag) = 'YES'
            return await (from source in _blendMonitorContext.AbcBlendSources
                          from tankProps in _blendMonitorContext.AbcTankProps
                          from propSource in _blendMonitorContext.AbcPropSources
                          where source.BlendId == lngBlendId && source.InUseFlag.ToUpper() == "YES"
                          && source.TankId == tankProps.TankId && tankProps.SourceId == propSource.Id
                          && tankProps.SelectedFlag.ToUpper() == "YES"
                          select new CompTankProps
                          {
                              MatId = source.MatId,
                              TankId = source.TankId,
                              PropId = tankProps.PropId,
                              SourceName = propSource.Name,
                              Value = tankProps.Value,
                              ValueTime = tankProps.ValueTime,
                              GoodFlag = tankProps.GoodFlag
                          }).ToListAsync<CompTankProps>();
        }
        public async Task<int> SetCompProp(string sourceName, double? value, double blendId, double matId, double tankId, double propId)
        {
            //update abc_blend_comp_props 
            //set source = ?, value = ?, good_flag = 'YES', value_time = sysdate 
            //where blend_id = ? and mat_id = ? and tank_id = ? and prop_id = ?
            AbcBlendCompProps Data = await _blendMonitorContext.AbcBlendCompProps
                                        .Where<AbcBlendCompProps>(row => row.BlendId == blendId && row.MatId == matId && row.TankId == tankId
                                        && row.PropId == propId).FirstOrDefaultAsync<AbcBlendCompProps>();
            Data.Source = sourceName;
            Data.Value = value;
            Data.GoodFlag = "YES";
            Data.ValueTime = DateTime.Now;

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<string> GetPropAlias(int intPropID)
        {
            //select alias into property_alias from abc_properties where id = property_id;
            AbcProperties Data = await _blendMonitorContext.AbcProperties
                                .Where<AbcProperties>(row => row.Id == intPropID).SingleOrDefaultAsync<AbcProperties>();
            return Data.Alias;
        }
        public async Task<double> GetDfPropVal(int intPrdgrpID, double matId, double propId)
        {
            //select def_val into prop_val from abc_prdgrp_mat_props where prdgrp_id = prdgrp_id1 and
            //mat_id = mat_id1 and prop_id = prop_id1
            //and usage_id = (select id from abc_usages where name = 'COMPONENT');
            AbcUsages usage = await _blendMonitorContext.AbcUsages
                            .Where<AbcUsages>(row => row.Name == "COMPONENT")
                            .SingleOrDefaultAsync<AbcUsages>();
            return await _blendMonitorContext.AbcPrdgrpMatProps
                                        .Where<AbcPrdgrpMatProps>(row => row.PrdgrpId == intPrdgrpID && row.MatId == matId && row.PropId == propId
                                        && row.UsageId == usage.Id).Select(row => row.DefVal)
                                        .FirstOrDefaultAsync<double>();
        }
        public async Task<List<CompVolTids>> GetCompStatVolTids(double blendId)
        {
            //select upper(abc_materials.name) name,
            //       abc_blend_sources.tank_id,
            //       abc_blend_sources.mat_id
            //from abc_materials, 
            //     abc_blend_sources
            //where abc_blend_sources.blend_id = ?
            //  and abc_blend_sources.mat_id = abc_materials.id and
            //  abc_blend_sources.in_use_flag = 'YES'
            //order by abc_blend_sources.mat_id
           return await (from am in _blendMonitorContext.AbcMaterials
                   from abs in _blendMonitorContext.AbcBlendSources
                   where abs.BlendId == blendId && abs.MatId == am.Id && abs.InUseFlag == "YES"
                   select new CompVolTids
                   {
                       Name = am.Name.ToUpper(),
                       TankId = abs.TankId,
                       MatId = abs.MatId
                   }).OrderBy(row => row.MatId).ToListAsync<CompVolTids>();
        }
        public async Task<List<CompVolTids>> GetCompVolTids(double blenderId, double blendId)
        {
            //select upper(abc_materials.name) name,
            //       abc_blender_comps.tot_comp_vol_tid,
            //       abc_blender_comps.mat_id,
            //       abc_blender_comps.wild_flag_tid
            //from abc_materials, 
            //     abc_blender_comps,
            //     abc_blend_sources
            //where abc_blender_comps.blender_id = ?
            //  and abc_blend_sources.blend_id = ?
            //  and abc_blend_sources.mat_id = abc_blender_comps.mat_id
            //  and abc_materials.id = abc_blender_comps.mat_id and
            //  abc_blend_sources.in_use_flag = 'YES'
            //order by abc_blender_comps.mat_id

            return await (from am in _blendMonitorContext.AbcMaterials
                          from abc in _blendMonitorContext.AbcBlenderComps
                          from abs in _blendMonitorContext.AbcBlendSources
                          where abc.BlenderId == blenderId && abs.BlendId == blendId && abs.MatId == abc.MatId
                          && am.Id == abc.MatId && abs.InUseFlag == "YES"
                          select new CompVolTids
                          {
                              Name = am.Name.ToUpper(),
                              TotCompVolTid = abc.TotCompVolTid,
                              WildFlagTid = abc.WildFlagTid,
                              MatId = abc.MatId
                          }).OrderBy(row => row.MatId).ToListAsync<CompVolTids>();
        }
        public async Task<List<TotalStatVol>> GetTotalStatVol(double blendId, double blenderId)
        {
            //select station.total_station_vol_tid, station.id as station_id, tag.name as total_station_tag, 
            //tag.read_value, tag.value_time, tag.value_quality, tag.read_enabled_flag,
            //scan.scan_enabled_flag,
            //scan.name as scan_group_name,
            //scan.ID as SCAN_GROUP_ID,
            //scan.scanrate

            //from abc_tags tag, abc_stations station, abc_scan_groups scan

            //where station.total_station_vol_tid = tag.id(+) and
            //tag.scan_group_id = scan.id(+) and station.id in 
            //(select station_id from abc_blend_stations where blend_id =?) and station.blender_id =? and station.in_use_flag = 'YES'
            //order by station.id
            List<double> stationIds = await _blendMonitorContext.AbcBlendStations
                                        .Where<AbcBlendStations>(row => row.BlendId == blendId)
                                        .Select(row => row.StationId)
                                        .ToListAsync<double>();

            return await (from tag in _blendMonitorContext.AbcTags
                          from station in _blendMonitorContext.AbcStations
                          from scan in _blendMonitorContext.AbcScanGroups
                          where station.TotalStationVolTid == tag.Id &&
                          tag.ScanGroupId == scan.Id && stationIds.Contains(station.Id) && station.BlenderId == blenderId 
                          && station.InUseFlag == "YES"
                          select new TotalStatVol
                          {
                              TotalStationVolTid = station.TotalStationVolTid,
                              StationId = station.Id,
                              TotalStationTag = tag.Name,
                              ReadValue = tag.ReadValue,
                              ValueTime = tag.ValueTime,
                              ValueQuality = tag.ValueQuality,
                              ReadEnabledFlag = tag.ReadEnabledFlag,
                              ScanEnabledFlag = scan.ScanEnabledFlag,
                              ScanGroupName = scan.Name,
                              ScanGroupId = scan.Id,
                              Scanrate = scan.Scanrate
                          }).OrderBy(row => row.StationId).ToListAsync<TotalStatVol>();

        }
        public async Task<List<TotalCompVol>> GetTotalCompVol(double blendId, double blenderId)
        {
            //select comp.tot_comp_vol_tid, comp.mat_id, tag.name as total_comp_tag, 
            //tag.read_value, tag.value_time, tag.value_quality, tag.read_enabled_flag,
            //scan.scan_enabled_flag,
            //scan.name as scan_group_name,
            //scan.id as scan_group_id,
            //scan.scanrate as scanrate

            //from abc_tags tag, abc_blender_comps comp, abc_scan_groups scan

            //where comp.tot_comp_vol_tid = tag.id(+) and
            //tag.scan_group_id = scan.id(+) and comp.mat_id in 
            //(select mat_id from abc_blend_comps where blend_id =?) and comp.blender_id =?
            // order by comp.mat_id
            List<double> matIds = await _blendMonitorContext.AbcBlendComps
                                        .Where<AbcBlendComps>(row => row.BlendId == blendId)
                                        .Select(row => row.MatId)
                                        .ToListAsync<double>();

            return await (from tag in _blendMonitorContext.AbcTags
                          from comp in _blendMonitorContext.AbcBlenderComps
                          from scan in _blendMonitorContext.AbcScanGroups
                          where comp.TotCompVolTid == tag.Id &&
                          tag.ScanGroupId == scan.Id && matIds.Contains(comp.MatId) && comp.BlenderId == blenderId
                          select new TotalCompVol
                          {
                              TotCompVolTid = comp.TotCompVolTid,
                              MatId = comp.MatId,
                              TotalCompTag = tag.Name,
                              ReadValue = tag.ReadValue,
                              ValueTime = tag.ValueTime,
                              ValueQuality = tag.ValueQuality,
                              ReadEnabledFlag = tag.ReadEnabledFlag,
                              ScanEnabledFlag = scan.ScanEnabledFlag,
                              ScanGroupName = scan.Name,
                              ScanGroupId = scan.Id,
                              Scanrate = scan.Scanrate
                          }).OrderBy(row => row.MatId).ToListAsync<TotalCompVol>();

        }
        public async Task<MxMnValTime> GetMxMnValTime(double blendId, double blenderId)
        {
            // select min(tag.value_time) as min_val_time, max(tag.value_time) as max_val_time
            // from abc_tags tag, abc_stations station, abc_scan_groups scan
            // where station.total_station_vol_tid = tag.id and
            // tag.scan_group_id = scan.id and station.id in
            // (select bs.station_id from abc_blend_stations bs, abc_blend_comps bc
            // where bs.blend_id = bc.blend_id and bs.mat_id = bc.mat_id and bs.in_use_flag = 'YES' and
            // bs.blend_id =? and(bc.act_recipe > (select recipe_violation_tolerance from abc_proj_defaults) or 
            // bc.rcp_constraint_type <> 'ZERO_OUT') and bc.usage_id not in 
            //(select id from abc_usages where name = 'ADDITIVE') and bs.act_setpoint > (select recipe_violation_tolerance from abc_proj_defaults))
            // and station.blender_id =? and station.in_use_flag = 'YES'


            //(select id from abc_usages where name = 'ADDITIVE')
            List<double> usageIds = await _blendMonitorContext.AbcUsages
                                    .Where<AbcUsages>(row => row.Name == "ADDITIVE")
                                    .Select(row => row.Id)
                                    .ToListAsync<double>();

            double? RecipeViolationTolerance = await _blendMonitorContext.AbcProjDefaults.Select(row => row.RecipeViolationTolerance).SingleOrDefaultAsync<double?>();

            List<double> stationIds = await (from bs in _blendMonitorContext.AbcBlendStations
                                             from bc in _blendMonitorContext.AbcBlendComps
                                             where bs.BlendId == bc.BlendId && bs.MatId == bc.MatId && bs.InUseFlag == "YES" &&
                                             bs.BlendId == blendId && bc.ActRecipe > RecipeViolationTolerance || bc.RcpConstraintType != "ZERO_OUT"
                                             && !usageIds.Contains(Convert.ToDouble(bc.UsageId)) && bs.ActSetpoint > RecipeViolationTolerance
                                             select bs.StationId).ToListAsync<double>();

            List<DateTime?> Data = await (from tag in _blendMonitorContext.AbcTags
                                          from station in _blendMonitorContext.AbcStations
                                          from scan in _blendMonitorContext.AbcScanGroups
                                          where station.TotalStationVolTid == tag.Id && tag.ScanGroupId == scan.Id
                                          && stationIds.Contains(station.Id)
                                          && station.BlenderId == blenderId && station.InUseFlag == "YES"
                                          select tag.ValueTime)
                                           .ToListAsync<DateTime?>();
            if (Data.Count() > 0)
                return new MxMnValTime { MinValTime = Data.Min(), MaxValTime = Data.Max() };

            return null;
        }
        public async Task<List<TotalizerScanTimes>> GetTotalizerScanTimes(double blendId, double blenderId)
        {
            //select tag.value_time as scantime, tag.name as tagname
            //from abc_tags tag, abc_stations station, abc_scan_groups scan
            //where station.total_station_vol_tid = tag.id and
            //tag.scan_group_id = scan.id and station.id in
            //(select bs.station_id from abc_blend_stations bs, abc_blend_comps bc
            //where bs.blend_id = bc.blend_id and bs.mat_id = bc.mat_id and bs.in_use_flag = 'YES' and
            //bs.blend_id =? and(bc.act_recipe > (select recipe_violation_tolerance from abc_proj_defaults) or 
            //    bc.rcp_constraint_type <> 'ZERO_OUT') and
            //bc.usage_id not in (select id from abc_usages where name = 'ADDITIVE') and
            //bs.act_setpoint > (select recipe_violation_tolerance from abc_proj_defaults))
            //and station.blender_id =? and
            //station.in_use_flag = 'YES'
            //order by value_time asc


            //(select id from abc_usages where name = 'ADDITIVE')
            List<double> usageIds = await _blendMonitorContext.AbcUsages
                                    .Where<AbcUsages>(row => row.Name == "ADDITIVE")
                                    .Select(row => row.Id)
                                    .ToListAsync<double>();

            double? RecipeViolationTolerance = await _blendMonitorContext.AbcProjDefaults.Select(row => row.RecipeViolationTolerance).SingleOrDefaultAsync<double?>();

            List<double> stationIds = await (from bs in _blendMonitorContext.AbcBlendStations
                                             from bc in _blendMonitorContext.AbcBlendComps
                                             where bs.BlendId == bc.BlendId && bs.MatId == bc.MatId && bs.InUseFlag == "YES" &&
                                             bs.BlendId == blendId && bc.ActRecipe > RecipeViolationTolerance || bc.RcpConstraintType != "ZERO_OUT"
                                             && !usageIds.Contains(Convert.ToDouble(bc.UsageId)) && bs.ActSetpoint > RecipeViolationTolerance
                                             select bs.StationId).ToListAsync<double>();

            return await (from tag in _blendMonitorContext.AbcTags
                                          from station in _blendMonitorContext.AbcStations
                                          from scan in _blendMonitorContext.AbcScanGroups
                                          where station.TotalStationVolTid == tag.Id && tag.ScanGroupId == scan.Id
                                          && stationIds.Contains(station.Id)
                                          && station.BlenderId == blenderId && station.InUseFlag == "YES"
                                          select new TotalizerScanTimes { 
                                          ScanTime = tag.ValueTime,
                                          TagName = tag.Name
                                          }).OrderBy(row => row.ScanTime)
                                           .ToListAsync<TotalizerScanTimes>();
           
        }
        public async Task<List<AbcBlendStations>> GetAllBldStations(double blendId)
        {
            //select station_id, mat_id from abc_blend_stations where blend_id =?
            return await _blendMonitorContext.AbcBlendStations
                        .Where<AbcBlendStations>(row => row.BlendId == blendId)
                        .ToListAsync<AbcBlendStations>();
        }
        public async Task<List<CompIntVols>> CompIntVols(double blendId, int interval)
        {
            //select upper(abc_materials.name) name, abc_blend_interval_comps.volume 
            //from abc_materials, abc_blend_interval_comps 
            //where abc_materials.id = abc_blend_interval_comps.mat_id and abc_blend_interval_comps.blend_id = ? 
            //and abc_blend_interval_comps.sequence = ? order by abc_blend_interval_comps.mat_id
            return await (from am in _blendMonitorContext.AbcMaterials
                          from abic in _blendMonitorContext.AbcBlendIntervalComps
                          where am.Id == abic.MatId && abic.BlendId == blendId
                          && abic.Sequence == interval
                          select new CompIntVols {
                              Volume = abic.Volume,
                              Name = am.Name.ToUpper()
                          }).ToListAsync<CompIntVols>();
            // returning only volume as name is not used
        }

        public async Task<CompBldData> CompBldData(double blendId)
        {
            //select bldcomp.volume, bldcomp.cur_recipe, bldcomp.act_recipe, bldcomp.avg_recipe, bldcomp.cost, usage_id, usage.name as usage_name 
            //from abc_blend_comps bldcomp, abc_usages usage 
            //where bldcomp.blend_id = ? and bldcomp.usage_id = usage.id order by bldcomp.mat_id
            return await (from bldcomp in _blendMonitorContext.AbcBlendComps
                          from usage in _blendMonitorContext.AbcUsages
                          where bldcomp.BlendId == blendId && bldcomp.UsageId == usage.Id
                          select new CompBldData
                          {
                              Volume = bldcomp.Volume,
                              CurRecipe = bldcomp.CurRecipe,
                              ActRecipe = bldcomp.ActRecipe,
                              AvgRecipe = bldcomp.AvgRecipe,
                              Cost = bldcomp.Cost,
                              UsageId = usage.Id,
                              UsageName = usage.Name
                          }).FirstOrDefaultAsync<CompBldData>();
        }
        public async Task<double?> GetBldLineupId(double blendId, double MatId)
        {
            //select lineup_id from abc_blend_sources where blend_id =? and mat_id =? and in_use_flag = 'YES'
            return await _blendMonitorContext.AbcBlendSources
                        .Where<AbcBlendSources>(row => row.BlendId == blendId && row.MatId == MatId && row.InUseFlag == "YES")
                        .Select(row => row.LineupId)
                        .FirstOrDefaultAsync<double?>();
        }
        public async Task<List<BldrStationsData>> GetBldrStationsData(double? lngCompLineupID, double blenderId)
        {
            //select eqp.station_id, st.name as station_name, st.min,st.max,
            //st.in_use_flag,st.rcp_sp_tag_id,st.rcp_meas_tag_id,
            //st.mat_num_tid,st.tank_select_num_tid,st.tank_preselect_num_tid,
            //st.dcs_station_num,st.select_station_tid,st.total_station_vol_tid,
            //st.wild_flag_tid,st.total_flow_control_tid, st.lineup_sel_tid, st.lineup_presel_tid,st.pumpA_sel_tid, st.pumpb_sel_tid,
            //st.pumpc_sel_tid, st.pumpd_sel_tid, st.lineup_feedback_tid,st.tank_feedback_tid, eqp.line_eqp_order

            //from abc_comp_lineup_eqp eqp, abc_stations st

            // where eqp.station_id = st.id(+) and eqp.line_id =? and
            //   st.blender_id =?
            //   order by eqp.line_eqp_order

            return await (from eqp in _blendMonitorContext.AbcCompLineupEqp
                          from st in _blendMonitorContext.AbcStations
                          where eqp.StationId == st.Id && eqp.LineId == lngCompLineupID && st.BlenderId == blenderId
                          select new BldrStationsData
                          {
                              StationId = eqp.StationId,
                              StationName = st.Name,
                              Min = st.Min,
                              Max = st.Max,
                              InUseFlag = st.InUseFlag,
                              RcpSpTagId = st.RcpSpTagId,
                              RcpMeasTagId = st.RcpMeasTagId,
                              MatNumTid = st.MatNumTid,
                              TankSelectNumTid = st.TankSelectNumTid,
                              TankPreSelectNumTid = st.TankPreselectNumTid,
                              DcsStationNum = st.DcsStationNum,
                              SelectStationTid = st.SelectStationTid,
                              TotalStationVolTid = st.TotalStationVolTid,
                              WildFlagTid = st.WildFlagTid,
                              TotalFlowControlTid = st.TotalFlowControlTid,
                              LineupSelTid = st.LineupSelTid,
                              LineupPreSelTid = st.LineupPreselTid,
                              PumpASelTid = st.PumpaSelTid,
                              PumpBSelTid = st.PumpbSelTid,
                              PumpCSelTid = st.PumpcSelTid,
                              PumpDSelTid = st.PumpdSelTid,
                              LineupFeedbackTid = st.LineupFeedbackTid,
                              TankFeedbackTid = st.TankFeedbackTid,
                              LineEqpOrder = eqp.LineEqpOrder
                          }).ToListAsync<BldrStationsData>();
        }
        public async Task<AbcBlendStations> GetBldStationsData(double blendId, double MatId, double? stationId)
        {
            //SELECT MIN_FLOW, MAX_FLOW, PREV_VOL, CUR_VOL, CUR_SETPOINT AS CUR_RECIPE, IN_USE_FLAG 
            //FROM ABC_BLEND_STATIONS 
            //WHERE BLEND_ID =? AND MAT_ID =? AND STATION_ID =? AND IN_USE_FLAG = 'YES'
            return await _blendMonitorContext.AbcBlendStations
                        .Where<AbcBlendStations>(row => row.BlendId == blendId && row.MatId == MatId && row.StationId == stationId
                        && row.InUseFlag == "YES").FirstOrDefaultAsync<AbcBlendStations>();
        }
        public async Task<string> GetFlowDenom(int intPrdgrpID)
        {
            //select upper(flow_denominator) into flow_denom from abc_prdgrps where id = prdgrp_id;
            return await _blendMonitorContext.AbcPrdgrps
                    .Where<AbcPrdgrps>(row => row.Id == intPrdgrpID)
                    .Select(row => row.FlowDenominator.ToUpper())
                    .FirstOrDefaultAsync<string>();
        }

        public async Task<int> UpdateAbcBlendCompWild(double blendId, double matId, string vntTagVal)
        {
            //("UPDATE abc_blend_comps SET wild="
            //                               + (tagWildFlag.vntTagVal + (" WHERE blend_id="
            //                               + (curblend.lngID + (" AND mat_id=" + vntCompsData(2, intI))))));            

            AbcBlendComps Data = await _blendMonitorContext.AbcBlendComps
                                    .Where<AbcBlendComps>(row => row.BlendId == blendId && row.MatId == matId)
                                    .FirstOrDefaultAsync<AbcBlendComps>();
            Data.Wild = Convert.ToDouble(vntTagVal);
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<double?> GetAddStationVol(double blendId, double matId, double? curLineupId)
        {
            // select sum(bs.cur_vol) as Add_Station_Vol
            // from abc_blend_stations bs
            // where bs.blend_id=" & lngBlendId & " and bs.mat_id= " & lngMatId & " and bs.in_use_flag='NO' and "
            // bs.station_id in ((select station_id from abc_comp_lineup_eqp where line_id= " 
            // (select MASTER_LINEUP_ID from abc_blend_sources where blend_id= " 
            // lngBlendId & " and mat_id= " & lngMatId & " and "
            // in_use_flag='YES')) minus (select station_id from abc_comp_lineup_eqp where line_id=" & lngCurLineupId & "))"

            List<double?> StationIds2 = await _blendMonitorContext.AbcCompLineupEqp
                                    .Where<AbcCompLineupEqp>(row => row.LineId == curLineupId)
                                    .Select(row => row.StationId)
                                    .ToListAsync<double?>();
            double? masterLineupId = await _blendMonitorContext.AbcBlendSources
                                        .Where<AbcBlendSources>(row => row.BlendId == blendId && row.MatId == matId && row.InUseFlag == "YES")
                                        .Select(row => row.MasterLineupId).FirstOrDefaultAsync<double?>();

            List<double?> StationIds1 = await _blendMonitorContext.AbcCompLineupEqp
                                   .Where<AbcCompLineupEqp>(row => row.LineId == masterLineupId)
                                   .Select(row => row.StationId)
                                   .ToListAsync<double?>();
            List<double?> minusedStations = new List<double?>();

            foreach (double? item in StationIds1)
            {
                if (!StationIds2.Contains(item))
                {
                    minusedStations.Add(item);
                }
            }

            return await _blendMonitorContext.AbcBlendStations
                            .Where<AbcBlendStations>(row => row.BlendId == blendId && row.MatId == matId && row.InUseFlag == "NO"
                            && minusedStations.Contains(row.StationId))
                            .SumAsync(row => row.CurVol);
        }
        public async Task<int> SetStationCurVol(string TagVal, double blendId, double? stationId, double matId)
        {
            //update abc_blend_stations set cur_vol = ? where blend_id = ? and station_id = ? and mat_id = ?
            AbcBlendStations Data = await _blendMonitorContext.AbcBlendStations
                                        .Where<AbcBlendStations>(row => row.BlendId == blendId && row.StationId == stationId && row.MatId == matId)
                                        .FirstOrDefaultAsync<AbcBlendStations>();
            Data.CurVol = Convert.ToDouble(TagVal);
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetStationPrevVol(string TagVal, double blendId, double? stationId, double matId)
        {
            //update abc_blend_stations set prev_vol = ? where blend_id = ? and station_id = ? and mat_id = ?
            AbcBlendStations Data = await _blendMonitorContext.AbcBlendStations
                                        .Where<AbcBlendStations>(row => row.BlendId == blendId && row.StationId == stationId && row.MatId == matId)
                                        .FirstOrDefaultAsync<AbcBlendStations>();
            Data.PrevVol = Convert.ToDouble(TagVal);
            return await _blendMonitorContext.SaveChangesAsync();
        }

        public async Task<int> SetIntRcp(double intRcp, double blendId, double matId, int intCurIntv)
        {
            //update abc_blend_interval_comps set int_recipe = ? where blend_id = ? and mat_id = ? and sequence = ?
            AbcBlendIntervalComps Data = await _blendMonitorContext.AbcBlendIntervalComps
                                        .Where<AbcBlendIntervalComps>(row => row.BlendId == blendId && row.Sequence == intCurIntv && row.MatId == matId)
                                        .FirstOrDefaultAsync<AbcBlendIntervalComps>();
            Data.IntRecipe = intRcp;
            return await _blendMonitorContext.SaveChangesAsync();
        }

        public async Task<List<PrdgrpVolFactor>> GetPrdgrpVolFactor(int intPrdgrpID,int intProductId,int intAdditiveId)
        {
            //SELECT PRDGRP.VOLUME_UOM_ID, UOM1.UNITS_NAME AS PRDGRP_VOL_UNITS, ADDT.UOM_ID, UOM2.UNITS_NAME AS ADD_VOL_UNITS, ADDT.UNIT_FACTOR
            //FROM ABC_PRDGRPS PRDGRP, ABC_PRD_ADDITIVES ADDT,ABC_UOM UOM1, ABC_UOM UOM2
            //WHERE PRDGRP.ID = ADDT.PRDGRP_ID AND PRDGRP.VOLUME_UOM_ID = UOM1.ID(+) AND
            //ADDT.UOM_ID = UOM2.ID(+) AND PRDGRP.ID =? AND
            //  ADDT.PRODUCT_ID =? AND ADDT.ADDITIVE_ID =?

            return await (from PRDGRP in _blendMonitorContext.AbcPrdgrps
                          from ADDT in _blendMonitorContext.AbcPrdAdditives
                          from UOM1 in _blendMonitorContext.AbcUom
                          from UOM2 in _blendMonitorContext.AbcUom
                          where PRDGRP.Id == ADDT.PrdgrpId && PRDGRP.VolumeUomId == UOM1.Id && ADDT.UomId == UOM2.Id
                          && PRDGRP.Id == intPrdgrpID && ADDT.ProductId == intProductId && ADDT.AdditiveId == intAdditiveId
                          select new PrdgrpVolFactor
                          {
                              VolumeUomId = PRDGRP.VolumeUomId,
                              PrdgrpVolUnits = UOM1.UnitsName,
                              UomId = ADDT.UomId,
                              AddVolUnits = UOM2.UnitsName,
                              UnitFactor = ADDT.UnitFactor
                          }).ToListAsync<PrdgrpVolFactor>();

        }

        public async Task<List<BlendStationEqp>> GetBlendStationEqp(double? lineUpId, double blendId, double matid)
        {
            //select bs.station_id, bs.min_flow, bs.max_flow, eqp.line_eqp_order
            //from abc_blend_stations bs, abc_comp_lineup_eqp eqp
            // where bs.station_id = eqp.station_id and eqp.line_id =? and
            //  bs.blend_id =? and bs.mat_id =? and in_use_flag = 'YES'
            //order by eqp.line_eqp_order

            return await (from bs in _blendMonitorContext.AbcBlendStations
                          from eqp in _blendMonitorContext.AbcCompLineupEqp                          
                          where bs.StationId == eqp.StationId && eqp.LineId == lineUpId &&
                           bs.BlendId == blendId && bs.MatId ==matid  && bs.InUseFlag == "YES"
                          select new BlendStationEqp
                          {
                             StationId = bs.StationId,
                              MinFlow =  bs.MinFlow,
                              MaxFlow =  bs.MaxFlow,
                              LineEqpOrder = eqp.LineEqpOrder
                          }).ToListAsync<BlendStationEqp>();

        }
        public async Task<int> SetBldStatPar(double dblStationActRcp, double blendId, double? stationId, double matId)
        {
            //update abc_blend_stations set act_setpoint=? where blend_id=? and station_id=? and mat_id=?
            AbcBlendStations Data = await _blendMonitorContext.AbcBlendStations
                                        .Where<AbcBlendStations>(row => row.BlendId == blendId && row.StationId == stationId && row.MatId == matId)
                                        .FirstOrDefaultAsync<AbcBlendStations>();
            Data.ActSetpoint = dblStationActRcp;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetIntVolCost(double? dblIntVol,double dblIntCost,double gdblBldVol, double blendId, int intCurIntv)
        {
            //update abc_blend_intervals set volume = ?, cost = ?, blend_volume = ? where blend_id = ? and sequence = ?
            AbcBlendIntervals Data = await _blendMonitorContext.AbcBlendIntervals
                                        .Where<AbcBlendIntervals>(row => row.BlendId == blendId && row.Sequence== intCurIntv)
                                        .FirstOrDefaultAsync<AbcBlendIntervals>();
            Data.Volume = dblIntVol;
            Data.Cost = dblIntCost;
            Data.BlendVolume = gdblBldVol;
            return await _blendMonitorContext.SaveChangesAsync();
        }

        public async Task<int> SetBldVolCost(double gdblBldVol, double dblBldCost,string vntTagVal,double blendId)
        {
            //update abc_blends set current_vol = ?, cost = ?, rate_sp_fb = ?  where id = ?
            AbcBlends Data = await _blendMonitorContext.AbcBlends
                                        .Where<AbcBlends>(row => row.Id == blendId)
                                        .FirstOrDefaultAsync<AbcBlends>();
            Data.CurrentVol = gdblBldVol;
            Data.Cost = dblBldCost;
            Data.RateSpFb = Convert.ToDouble(vntTagVal);
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<double?> GetIntVol(double blendId, int intCurIntv) {
            //select volume into intv_vol from abc_blend_intervals where blend_id = blend_id1 and
            //sequence = sequence1;

            return await _blendMonitorContext.AbcBlendIntervals
                    .Where<AbcBlendIntervals>(row => row.BlendId == blendId && row.Sequence == intCurIntv)
                    .Select(row => row.Volume)
                    .FirstOrDefaultAsync<double?>();

        }
        public async Task<List<SelTankProps>> GetSelTankProps(double blendId)
        {
            //Select abc_blend_comp_props.prop_id, 
            //abc_blend_comp_props.value

            //from abc_blend_comp_props, abc_blend_sources

            //where abc_blend_sources.mat_id = abc_blend_comp_props.mat_id AND
            //abc_blend_sources.blend_id = abc_blend_comp_props.blend_id AND
            //abc_blend_sources.tank_id = abc_blend_comp_props.tank_id AND
            //abc_blend_sources.blend_id = ? AND
            //upper(abc_blend_sources.in_use_flag) = 'YES' AND
            //upper(abc_blend_comp_props.good_flag) = 'YES'
            return await (from abcp in _blendMonitorContext.AbcBlendCompProps
                   from abs in _blendMonitorContext.AbcBlendSources
                   where abs.MatId == abcp.MatId &&
                    abs.BlendId == abcp.BlendId &&
                    abs.TankId == abcp.TankId &&
                    abs.BlendId == blendId &&
                    abs.InUseFlag.ToUpper() == "YES" &&
                    abcp.GoodFlag == "YES"
                   select new SelTankProps
                   {
                       PropId = abcp.PropId,
                       Value = abcp.Value
                   }).ToListAsync<SelTankProps>();
        }
        public async Task<int> SetFeebackPred(double dblFeedbackPred, double blendId,int intCurIntv,int intCompPropID)
        {
            //UPDATE ABC_BLEND_INTERVAL_PROPS SET FEEDBACK_PRED=? WHERE BLEND_ID=? AND SEQUENCE=? AND PROP_ID=?
            AbcBlendIntervalProps Data = await _blendMonitorContext.AbcBlendIntervalProps
                                                .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId && row.Sequence == intCurIntv
                                                && row.PropId == intCompPropID).FirstOrDefaultAsync<AbcBlendIntervalProps>();
            Data.FeedbackPred = dblFeedbackPred;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<string> GetOptEngine()
        {
            //select opn_engine from abc_proj_defaults
            return await _blendMonitorContext.AbcProjDefaults
                            .Select(row => row.OpnEngine)
                            .FirstOrDefaultAsync<string>();
        }
        public async Task<List<string>> GetSBPath()
        {
            //select STARBLEND_INST_PATH SB_PATH from abc_proj_defaults
            return await _blendMonitorContext.AbcProjDefaults
                            .Select(row => row.StarblendInstPath)
                            .ToListAsync<string>();
        }
        public async Task<List<double>> GetBlendIntProps(double blendId, double PrdgrpId)
        {
            //select distinct bip.prop_id
            //from abc_blend_interval_props bip, abc_prdgrp_props pp
            //where bip.prop_id = pp.prop_id and bip.blend_id = ? and
            //pp.prdgrp_id = ?
            return await (from bip in _blendMonitorContext.AbcBlendIntervalProps
                          from pp in _blendMonitorContext.AbcPrdgrpProps
                          where bip.PropId == pp.PropId && bip.BlendId == blendId &&
                          pp.PrdgrpId == PrdgrpId
                          select bip.PropId).Distinct().ToListAsync<double>();
        }
        public async Task<int> CheckPropertyUsed(double blendId, double propId, string strUsedFlag)
        {
            //"select bsp.prop_id, bsp.sample_name " & _
            //"from abc_blend_sample_props bsp, abc_blends b " & _
            //"where bsp.blend_id=b.id(+) and bsp.blend_id=" & lngBlendId & _
            //" and bsp.used_flag='" & strUsedFlag & "' and bsp.prop_id=" & _
            //lngPropID
            var Data = await (from bsp in _blendMonitorContext.AbcBlendSampleProps
                              from b in _blendMonitorContext.AbcBlends
                              where bsp.BlendId == b.Id && bsp.BlendId == blendId && bsp.UsedFlag == strUsedFlag && bsp.PropId == propId
                              select bsp).ToListAsync<AbcBlendSampleProps>();
            return Data.Count();
        }

        public async Task<List<AbcBlendIntervalProps>> GetBiasCalData1(double blendId, double propId, int intStartInterval, int intStopInterval)
        {
            //"SELECT BIP.SEQUENCE, BIP.BIASCALC_CURRENT " & _
            //"FROM ABC_BLEND_INTERVAL_PROPS BIP " & _
            //"WHERE BIP.BLEND_ID=" & lngBlendId & " AND " & _
            //"BIP.PROP_ID= " & lngPropID & " AND " & _
            //"BIP.SEQUENCE " " >= " & intStartInterval & " AND BIP.SEQUENCE <= " & intStopInterval & " ORDER BY BIP.SEQUENCE ASC"
            return await _blendMonitorContext.AbcBlendIntervalProps
                    .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId && row.PropId == propId
                    && row.Sequence >= intStartInterval && row.Sequence <= intStopInterval)
                    .OrderBy(row => row.Sequence)
                    .ToListAsync<AbcBlendIntervalProps>();
        }

        public async Task<List<AbcBlendIntervalProps>> GetBiasCalData2(double blendId, double propId, int intStartInterval, int intStopInterval)
        {
            //"SELECT BIP.SEQUENCE, BIP.BIASCALC_CURRENT " & _
            //"FROM ABC_BLEND_INTERVAL_PROPS BIP " & _
            //"WHERE BIP.BLEND_ID=" & lngBlendId & " AND " & _
            //"BIP.PROP_ID= " & lngPropID & " AND " & _
            //"BIP.SEQUENCE " "<= " & intStartInterval & " ORDER BY BIP.SEQUENCE DESC"
            return await _blendMonitorContext.AbcBlendIntervalProps
                    .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId && row.PropId == propId
                    && row.Sequence <= intStopInterval)
                    .OrderByDescending(row => row.Sequence)
                    .ToListAsync<AbcBlendIntervalProps>();
        }
        public async Task<List<BldSampleProps>> GetBldSampleProps(double blendId,string sampleName)
        {
            //select bsp.blend_id, bsp.sample_name,
            //bsp.prop_id, bsp.value, bsp.used_flag, bs.type as sample_type,
            //bs.start_date, bs.stop_date,
            //nvl(bs.start_volume, -1) as start_volume,
            //nvl(bs.stop_volume, -1) as stop_volume
            //from abc_blend_sample_props bsp, abc_blend_samples bs
            //where bsp.blend_id = bs.blend_id and bsp.sample_name = bs.name(+) and
            //bsp.used_flag = 'NO' and bsp.blend_id =? and
            //  bsp.sample_name =?
            //  order by start_date,start_volume

            return await (from bsp in _blendMonitorContext.AbcBlendSampleProps
                          from bs in _blendMonitorContext.AbcBlendSamples
                          where bsp.BlendId == bs.BlendId && bsp.SampleName == bs.Name &&
                          bsp.UsedFlag == "NO" && bsp.BlendId == blendId && bsp.SampleName == sampleName
                          select new BldSampleProps
                          {

                              BlendId = bsp.BlendId,
                              SampleName = bsp.SampleName,
                              PropId = bsp.PropId,
                              Value = bsp.Value,
                              UsedFlag = bsp.UsedFlag,
                              SampleType = bs.Type,
                              StartDate = bs.StartDate,
                              StopDate = bs.StopDate,
                              StartVolume = (bs.StartVolume == null) ? -1 : bs.StartVolume,
                              StopVolume = (bs.StopVolume == null) ? -1 : bs.StopVolume
                          }).OrderBy(row => row.StartDate).ThenBy(row => row.StartVolume)
                          .ToListAsync<BldSampleProps>();
        }
        public async Task<List<SampleIntvProps>> GetSampleIntvProps(double blendId, int intMatchIntv,double propID, double prdgrpId)
        {
            //select bip.feedback_pred, bip.bias, bip.fb_pred_bias,
            //bip.biascalc_current, pp.biascalc_default, pp.biascalc_anz_fallback, pp.spot_filter,
            //pp.composite_filter, pp.spot_bias_clamp, pp.composite_bias_clamp

            //from abc_blend_interval_props bip, abc_prdgrp_props pp

            //where bip.prop_id = pp.prop_id and
            //bip.blend_id =? and bip.sequence =? and bip.prop_id =? and
            //pp.prdgrp_id =?

            return await (from bip in _blendMonitorContext.AbcBlendIntervalProps
                          from pp in _blendMonitorContext.AbcPrdgrpProps
                          where bip.PropId == pp.PropId && bip.BlendId == blendId && 
                          bip.Sequence  == intMatchIntv && bip.PropId == propID &&
                            pp.PrdgrpId == prdgrpId
                          select new SampleIntvProps
                          {
                             FeedbackPred = bip.FeedbackPred,
                             Bias = bip.Bias,
                              FbPredBias = bip.FbPredBias,
                              BiascalcCurrent = bip.BiascalcCurrent,
                              BiascalcDefault = pp.BiascalcDefault,
                              BiascalcAnzFallback = pp.BiascalcAnzFallback,
                              SpotFilter = pp.SpotFilter,
                              CompositeFilter = pp.CompositeFilter,
                              SpotBiasClamp = pp.SpotBiasClamp,
                              CompositeBiasClamp = pp.CompositeBiasClamp
                          }).ToListAsync<SampleIntvProps>();
        }
        public async Task<List<PropNameModel>> GetPropName(double propId)
        {
            //select prop.name as prop_name, prop.uom_id, uom.units_name, uom.alias as units_alias 
            //from abc_properties prop, abc_uom uom 
            //where prop.uom_id = uom.id(+) and prop.id =?

            return await (from prop in _blendMonitorContext.AbcProperties
                          from uom in _blendMonitorContext.AbcUom
                          where prop.UomId == uom.Id && prop.Id == propId
                          select new PropNameModel
                          {
                              PropName = prop.Name,
                              UomId = prop.UomId,
                              UnitsName = uom.UnitsName,
                              UnitsAlias = uom.Alias
                          }).ToListAsync<PropNameModel>();
        }
        private double GetUnitNameID(string unitName)
        {
            //SELECT ID FROM ABC_UOM WHERE UNITS_NAME = '" & strFromUnitName & "')
            return (_blendMonitorContext.AbcUom
                        .Where<AbcUom>(row => row.UnitsName == unitName)
                        .SingleOrDefault<AbcUom>()).Id;
        }
        public async Task<double> GetConvValue(double sngOrigValue, string strFromUnitName, string strToUnitName)
        {
            //"SELECT FACTOR, FUNCTION_NAME " & _
            //"FROM ABC_UNIT_CONVERSION " & _
            // WHERE FROM_UNIT = (SELECT ID FROM ABC_UOM WHERE UNITS_NAME ='" & strFromUnitName & "') AND " & _
            //TO_UNIT = (SELECT ID FROM ABC_UOM WHERE UNITS_NAME ='" & strToUnitName & "')"
            try
            {
                AbcUnitConversion Data = await _blendMonitorContext.AbcUnitConversion
                                            .Where<AbcUnitConversion>(row => row.FromUnit == (GetUnitNameID(strFromUnitName))
                                            && row.ToUnit == (GetUnitNameID(strToUnitName)))
                                            .FirstAsync<AbcUnitConversion>();
                double sngFactor = Convert.ToDouble(Data.Factor);
                string strFunctionName = Data.FunctionName;
                double value;
                switch (strFunctionName)
                {
                    case ("CST2SSF"):
                        value = HelperMethods.CST2SSF(sngOrigValue);
                        break;
                    case ("SSF2CST"):
                        value = HelperMethods.SSF2CST(sngOrigValue);
                        break;
                    case ("API2SG"):
                        value = HelperMethods.API2SG(sngOrigValue);
                        break;
                    case ("SG2API"):
                        value = HelperMethods.SG2API(sngOrigValue);
                        break;
                    case ("DEGC2DEGF"):
                        value = HelperMethods.DEGC2DEGF(sngOrigValue);
                        break;
                    case ("DEGF2DEGC"):
                        value = HelperMethods.DEGF2DEGC(sngOrigValue);
                        break;
                    default:
                        //'if no function exist then apply factor conversion
                        value = Convert.ToDouble(sngOrigValue) * sngFactor;
                        break;
                }

                return value;
            }
            catch (Exception ex)
            {
                if (strFromUnitName != strToUnitName)
                {
                    //code
                    //        'The conversion from ^1 unit to ^2 unit is not found in unit conversion table. Original value was not modified.
                    //deABC.cmdLogMessage 6860, App.Title, "UNIT_CONVERSION", _
                    //    strFromUnitName, strToUnitName, "", "", "", "", strMsgOK
                }
                return sngOrigValue;
            }
        }
        public async Task<int> setUnfiltBias(double dblUnfilBias, double blendId, double propId, int vntIntvNum, int intStopInterval, int intMatchingIntv)
        {
            //"UPDATE ABC_BLEND_INTERVAL_PROPS SET UNFILT_BIAS=" & dblUnfilBias &
            //" WHERE BLEND_ID = " & curblend.lngID & "  AND PROP_ID=" & vntPropID.Value & " AND " &
            //" (SEQUENCE IN (SELECT BIP.SEQUENCE FROM ABC_BLEND_INTERVAL_PROPS BIP WHERE BIP.BLEND_ID = " &
            //curblend.lngID & " AND BIP.PROP_ID=" & vntPropID.Value & " AND BIP.SEQUENCE BETWEEN " & vntIntvNum &
            //" AND " & intStopInterval & " AND BIP.BIASCALC_CURRENT NOT IN ('ANALYZER','NOCALC')) OR (SEQUENCE > " & intStopInterval & " AND " &
            //"SEQUENCE <=" & intMatchingIntv & "))";

            List<string> BiasList = new List<string>() { "ANALYZER", "NOCALC" };
            List<double> Sequences = await _blendMonitorContext.AbcBlendIntervalProps
                                    .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId && row.PropId == propId
                                    && row.Sequence <= vntIntvNum && row.Sequence >= intStopInterval && !BiasList.Contains(row.BiascalcCurrent))
                                    .Select(row => row.Sequence)
                                    .ToListAsync<double>();
            AbcBlendIntervalProps Data = await _blendMonitorContext.AbcBlendIntervalProps
                                            .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId && row.PropId == propId &&
                                            (Sequences.Contains(row.Sequence) || (row.Sequence > intStopInterval && row.Sequence <= intMatchingIntv)))
                                            .FirstOrDefaultAsync<AbcBlendIntervalProps>();
            Data.UnfiltBias = dblUnfilBias;

            return await _blendMonitorContext.SaveChangesAsync();
        }

        public async Task<int> setBias(double dblIntBias, double blendId, double propId, int vntIntvNum, int intStopInterval, int intMatchingIntv)
        {
            //"UPDATE ABC_BLEND_INTERVAL_PROPS SET BIAS= " & dblIntBias &
            //" WHERE BLEND_ID = " & curblend.lngID & "  AND PROP_ID=" & vntPropID.Value & " AND " &
            //" (SEQUENCE IN (SELECT BIP.SEQUENCE FROM ABC_BLEND_INTERVAL_PROPS BIP WHERE BIP.BLEND_ID = " &
            //curblend.lngID & " AND BIP.PROP_ID=" & vntPropID.Value & " AND BIP.SEQUENCE BETWEEN " & vntIntvNum &
            //" AND " & intStopInterval & " AND BIP.BIASCALC_CURRENT NOT IN ('ANALYZER','NOCALC')) OR (SEQUENCE > " & intStopInterval & " AND " &
            //"SEQUENCE <=" & intMatchingIntv & "))"

            List<string> BiasList = new List<string>() { "ANALYZER", "NOCALC" };
            List<double> Sequences = await _blendMonitorContext.AbcBlendIntervalProps
                                    .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId && row.PropId == propId
                                    && row.Sequence <= vntIntvNum && row.Sequence >= intStopInterval && !BiasList.Contains(row.BiascalcCurrent))
                                    .Select(row => row.Sequence)
                                    .ToListAsync<double>();
            AbcBlendIntervalProps Data = await _blendMonitorContext.AbcBlendIntervalProps
                                            .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId && row.PropId == propId &&
                                            (Sequences.Contains(row.Sequence) || (row.Sequence > intStopInterval && row.Sequence <= intMatchingIntv)))
                                            .FirstOrDefaultAsync<AbcBlendIntervalProps>();
            Data.Bias = dblIntBias;

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> setBiasAndUnfiltBias(double dblIntBias,double dblUnfilBias, double blendId, double propId, int vntIntvNum, int intMatchingIntv)
        {
            //"UPDATE ABC_BLEND_INTERVAL_PROPS SET BIAS= " & dblIntBias & "," &
            //"UNFILT_BIAS=" & dblUnfilBias & " WHERE BLEND_ID = " & curblend.lngID & " AND SEQUENCE < " & vntIntvNum & " AND " &
            //"SEQUENCE >=" & intMatchingIntv & " AND PROP_ID=" & vntPropID.Value
           
            List<AbcBlendIntervalProps> Data = await _blendMonitorContext.AbcBlendIntervalProps
                                            .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId &&
                                             row.Sequence < vntIntvNum && row.Sequence >= intMatchingIntv && row.PropId == propId)
                                            .ToListAsync<AbcBlendIntervalProps>();
            foreach (AbcBlendIntervalProps obj in Data)
            {
                obj.Bias = dblIntBias;
                obj.UnfiltBias = dblUnfilBias;
            }            
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<double>> GetPrevIntBias(double blendId, int IntvNum,double propID)
        {
            //select NVL(bias,0) AS bias
            //from abc_blend_interval_props
            //where blend_id = ? and sequence =? and prop_id =?
            return await _blendMonitorContext.AbcBlendIntervalProps
                            .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId && row.Sequence == IntvNum && row.PropId == propID)
                            .Select(row => (row.Bias == null) ? 0 : Convert.ToDouble(row.Bias))
                            .ToListAsync<double>();
        }
        public async Task<int> SetModelErrExistsFlag(string txt, double blendId, double propId)
        {
            //update abc_blend_props set model_err_exists_flag = ? where blend_id = ? and prop_id = ?
            AbcBlendProps Data = await _blendMonitorContext.AbcBlendProps
                                    .Where<AbcBlendProps>(row => row.BlendId == blendId && row.PropId == propId)
                                    .FirstOrDefaultAsync<AbcBlendProps>();
            Data.ModelErrExistsFlag = txt;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetModelErrClrdFlag( double blendId, double propId)
        {
            //update abc_blend_props set model_err_exists_flag = 'NO', model_err_clrd_flag = 'YES' 
            //where blend_id = ? and prop_id = ? and upper(model_err_exists_flag) = 'YES' and upper(model_err_clrd_flag) = 'NO'
            AbcBlendProps Data = await _blendMonitorContext.AbcBlendProps
                                    .Where<AbcBlendProps>(row => row.BlendId == blendId && row.PropId == propId
                                    && row.ModelErrExistsFlag.ToUpper() == "YES" && row.ModelErrClrdFlag.ToUpper() == "NO")
                                    .FirstOrDefaultAsync<AbcBlendProps>();
            Data.ModelErrExistsFlag = "NO";
            Data.ModelErrClrdFlag = "YES";
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetUsedFlag(double blendId, double propId, string strSampleName)
        {
            //"UPDATE ABC_BLEND_SAMPLE_PROPS SET USED_FLAG='YES' " &
            //"WHERE BLEND_ID = " & curblend.lngID & " AND SAMPLE_NAME='" & strSampleName & "' AND " &
            //"PROP_ID=" & vntPropID.Value
            AbcBlendSampleProps Data = await _blendMonitorContext.AbcBlendSampleProps
                                            .Where<AbcBlendSampleProps>(row => row.BlendId == blendId && row.SampleName == strSampleName 
                                            && row.PropId == propId).FirstOrDefaultAsync<AbcBlendSampleProps>();
            Data.UsedFlag = "YES";
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetIntCalcPropertyFlag(double blendId, double propId, int vntIntvNum)
        {
            //update abc_blend_interval_props set calc_property_flag = 'NO'
            //where blend_id = ? and prop_id = ? and sequence <= ? and
            //calc_property_flag = 'YES'

            AbcBlendIntervalProps Data = await _blendMonitorContext.AbcBlendIntervalProps
                                            .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId &&
                                             row.Sequence <= vntIntvNum && row.PropId == propId && row.CalcPropertyFlag == "YES")
                                            .FirstOrDefaultAsync<AbcBlendIntervalProps>();
            Data.CalcPropertyFlag = "NO";
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<string> LogMessage(int msgID, string prgm1, string gnrlText, string prgm2, string prgm3, string prgm4, string prgm5,
            string prgm6, string prgm7, string res)
        {
            //[dbo].[ABC_MESSAGES_PKG$LOG_MESSAGE]
            //@M_ID = 1,
            //@M_SOURCE = N'tank',
            //@M_REF = N'ssd',
            //@P1 = N'sad',
            //@P2 = N'asd',
            //@P3 = N'sad',
            //@P4 = N'sd',
            //@P5 = N'asd',
            //@P6 = N'asd',
            //@P_OUT = @P_OUT OUTPUT
            try
            {
                SqlParameter mId = new SqlParameter("@M_ID", msgID);
                SqlParameter source = new SqlParameter("@M_SOURCE", prgm1);
                SqlParameter m_ref = new SqlParameter("@M_REF", gnrlText);
                SqlParameter p1 = new SqlParameter("@P1", prgm2);
                SqlParameter p2 = new SqlParameter("@P2", prgm3);
                SqlParameter p3 = new SqlParameter("@P3", prgm4);
                SqlParameter p4 = new SqlParameter("@P4", prgm5);
                SqlParameter p5 = new SqlParameter("@P5", prgm6);
                SqlParameter p6 = new SqlParameter("@P6", prgm7);

                // declaring output param
                SqlParameter p_out = new SqlParameter();
                p_out.ParameterName = "@P_OUT";
                p_out.Value = res;
                p_out.Direction = ParameterDirection.Output;

                // Processing.  
                string sqlQuery = "[dbo].[ABC_MESSAGES_PKG$LOG_MESSAGE]" +
                                    " @M_ID," +
                                    " @M_SOURCE," +
                                    " @M_REF," +
                                    " @P1," +
                                    " @P2," +
                                    " @P3," +
                                    " @P4," +
                                    " @P5," +
                                    " @P6," +
                                    " @P_OUT OUT";

                int Data = await _blendMonitorContext.Database.ExecuteSqlRawAsync(sqlQuery, mId, source, m_ref, p1, p2, p3, p4, p5, p6, p_out);
                return Convert.ToString(p_out.Value);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


    }
}
