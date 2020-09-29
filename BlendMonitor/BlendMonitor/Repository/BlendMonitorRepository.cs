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
                              TotalStationTag = tag.Name,
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
                                             bs.BlendId == blendId && (bc.ActRecipe > RecipeViolationTolerance || bc.RcpConstraintType != "ZERO_OUT"
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
                                             bs.BlendId == blendId && (bc.ActRecipe > RecipeViolationTolerance || bc.RcpConstraintType != "ZERO_OUT"
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
