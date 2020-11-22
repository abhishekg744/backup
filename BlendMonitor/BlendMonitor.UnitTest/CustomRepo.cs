
using BlendMonitor.Entities;
using BlendMonitor.Model;
using BlendMonitor.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BlendMonitor.Constans;

namespace BlendMonitor.UnitTest
{
    public class CustomRepo : IBlendMonitorRepository
    {
        private BlendMonitorContext _blendMonitorContext;
        private readonly IConfiguration _configuration;
        private string programName;

        public CustomRepo(BlendMonitorContext blendMonitorContext, IConfiguration configuration)
        {
            _blendMonitorContext = blendMonitorContext;
            _configuration = configuration;
            programName = _configuration.GetSection("ProgramName").Value.ToUpper();
        }
        //public readonly BlendMonitorContext _cont;
        //public readonly IConfiguration _config;
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
            gProjDfs = new ProjDfData();
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
            if (DataList.Count > 0 && DataList[0].ProgramError != null)
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
            return (Data == null || Data.Starttime == null) ? DateTime.Parse("1/1/1900") : DateTime.Parse(Data.Starttime.ToString());

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
                    .Select(row => new DCSProdLineupNum
                    {
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

        public async Task<string> GetGradeName(double gradeId)
        {
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
            return await _blendMonitorContext.SaveChangesAsync();
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

            return await _blendMonitorContext.SaveChangesAsync();
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

            return await _blendMonitorContext.SaveChangesAsync();
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
                          select new TotalizerScanTimes
                          {
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
                          select new CompIntVols
                          {
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

        public async Task<List<PrdgrpVolFactor>> GetPrdgrpVolFactor(int intPrdgrpID, int intProductId, int intAdditiveId)
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
                           bs.BlendId == blendId && bs.MatId == matid && bs.InUseFlag == "YES"
                          select new BlendStationEqp
                          {
                              StationId = bs.StationId,
                              MinFlow = bs.MinFlow,
                              MaxFlow = bs.MaxFlow,
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
        public async Task<int> SetIntVolCost(double? dblIntVol, double dblIntCost, double gdblBldVol, double blendId, int intCurIntv)
        {
            //update abc_blend_intervals set volume = ?, cost = ?, blend_volume = ? where blend_id = ? and sequence = ?
            AbcBlendIntervals Data = await _blendMonitorContext.AbcBlendIntervals
                                        .Where<AbcBlendIntervals>(row => row.BlendId == blendId && row.Sequence == intCurIntv)
                                        .FirstOrDefaultAsync<AbcBlendIntervals>();
            Data.Volume = dblIntVol;
            Data.Cost = dblIntCost;
            Data.BlendVolume = gdblBldVol;
            return await _blendMonitorContext.SaveChangesAsync();
        }

        public async Task<int> SetBldVolCost(double gdblBldVol, double dblBldCost, string vntTagVal, double blendId)
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
        public async Task<double?> GetIntVol(double blendId, int intCurIntv)
        {
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
        public async Task<int> SetFeebackPred(double dblFeedbackPred, double blendId, int intCurIntv, int intCompPropID)
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
        public async Task<List<BldSampleProps>> GetBldSampleProps(double blendId, string sampleName)
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
        public async Task<List<SampleIntvProps>> GetSampleIntvProps(double blendId, int intMatchIntv, double propID, double prdgrpId)
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
                          bip.Sequence == intMatchIntv && bip.PropId == propID &&
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
        public async Task<int> setBiasAndUnfiltBias(double dblIntBias, double dblUnfilBias, double blendId, double propId, int vntIntvNum, int intMatchingIntv)
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
        public async Task<List<double>> GetPrevIntBias(double blendId, int IntvNum, double propID)
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
        public async Task<int> SetModelErrClrdFlag(double blendId, double propId)
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
        public async Task<List<CheckAnzrMeasure>> CheckAnzrMeasure(double blenderId, double propId)
        {
            //"SELECT AHP.ANZ_ID, ANZR.CYCLE_TIME, " & _
            //"AHP.RES_TIME, AHP.TRANSPORT_TIME,AHP.FROZEN_OP_LMT " & _

            //"FROM ABC_ANZ_HDR_PROPS AHP, ABC_ANZS ANZR " & _
            //"WHERE AHP.ANZ_ID=ANZR.ID(+) AND " & _
            //"AHP.BLENDER_ID=" & lngBlenderID & " AND " & _
            //"AHP.PROP_ID= " & lngPropID & " AND AHP.IN_USE_FLAG='YES' AND " & _
            //"ANZR.ABC_SERVICE_FLAG='YES'"

            return await (from AHP in _blendMonitorContext.AbcAnzHdrProps
                          from ANZR in _blendMonitorContext.AbcAnzs
                          where AHP.AnzId == ANZR.Id && AHP.BlenderId == blenderId &&
                          AHP.PropId == propId && AHP.InUseFlag == "YES" && ANZR.AbcServiceFlag == "YES"
                          select new CheckAnzrMeasure
                          {
                              AnzId = AHP.AnzId,
                              CycleTime = ANZR.CycleTime,
                              ResTime = AHP.ResTime,
                              TransportTime = AHP.TransportTime,
                              FrozenOpLmt = AHP.FrozenOpLmt
                          }).ToListAsync<CheckAnzrMeasure>();

        }       
        public async Task<List<AbcBlendIntervalProps>> GetBlendIntervalPropsData(double blendId, double propId, int intCurIntv, int intBlendIntvSeq, double anzID)
        {
            //"SELECT BIP.BIASCALC_CURRENT, " & _
            //"BIP.RESULT_CNT, BIP.ANZ_GOOD_FLAG " & _
            //"FROM ABC_BLEND_INTERVAL_PROPS BIP " & _
            //"WHERE BIP.BLEND_ID=" & curblend.lngID & " AND " & _
            //"BIP.PROP_ID= " & lngPropID & " AND " & _
            //"BIP.SEQUENCE <= " & (curblend.intCurIntv - 1) & " AND BIP.SEQUENCE > = " & _
            //(intBlendIntvSeq - 1) & " AND BIP.ANZ_ID = " & lngAnzID

            return await _blendMonitorContext.AbcBlendIntervalProps
                        .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId && row.PropId == propId
                        && row.Sequence <= (intCurIntv - 1) && row.Sequence >= (intBlendIntvSeq - 1) && row.AnzId == anzID)
                        .ToListAsync<AbcBlendIntervalProps>();
        }
        public async Task<List<string>> CheckBiasCalcAnzFallback(int prdgrpID, double propId)
        {
            //"SELECT PP.BIASCALC_ANZ_FALLBACK " & _
            //"FROM ABC_PRDGRP_PROPS PP " & _
            //"WHERE PP.PRDGRP_ID=" & intPrdgrpID & " AND " & _
            //"PP.PROP_ID= " & lngPropID
            return await _blendMonitorContext.AbcPrdgrpProps
                        .Where<AbcPrdgrpProps>(row => row.PrdgrpId == prdgrpID && row.PropId == propId)
                        .Select(row => row.BiascalcAnzFallback)
                        .ToListAsync<string>();
        }
        public async Task<int> SetBiasCalcCurrent(double blendId, double propId, int intCurIntv, string biasCalcCurrent)
        {
            //"UPDATE ABC_BLEND_INTERVAL_PROPS SET BIASCALC_CURRENT= '" & _
            //objRst.Fields("BIASCALC_ANZ_FALLBACK").Value & "' WHERE BLEND_ID = " & curblend.lngID & _
            //" AND PROP_ID= " & lngPropID & (" AND SEQUENCE >="
            //+(curblend.intCurIntv - 1));

            AbcBlendIntervalProps Data = await _blendMonitorContext.AbcBlendIntervalProps
                                            .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId && row.PropId == propId && row.Sequence >= intCurIntv)
                                            .FirstOrDefaultAsync<AbcBlendIntervalProps>();
            Data.BiascalcCurrent = biasCalcCurrent;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<string>> GetSampleType(double propId, double blendId)
        {
            //"SELECT BSP.PROP_ID, BS.TYPE AS SAMPLE_TYPE " & _
            //"FROM ABC_BLEND_SAMPLES_PROPS BSP, ABC_BLEND_SAMPLES BS " & _
            //"WHERE BSP.BLEND_ID=BS.BLEND_ID AND BSP.SAMPLE_NAME=BS.NAME(+) AND " & _
            //"BSP.PROP_ID=" & lngPropID & " AND BSP.BLEND_ID=" & curblend.lngID & " AND " & _
            //"BSP.USED_FLAG='NO'"
            return await (from BSP in _blendMonitorContext.AbcBlendSampleProps
                          from BS in _blendMonitorContext.AbcBlendSamples
                          where BSP.BlendId == BS.BlendId && BSP.SampleName == BS.Name &&
                          BSP.PropId == propId && BSP.BlendId == blendId && BSP.UsedFlag == "NO"
                          select BS.Type).ToListAsync<string>();

        }
        public async Task<List<BiasData>> GetBiasData(double blendId, double blenderId, double prdgrpId)
        {
            //select bip.sequence, bip.prop_id, bip.feedback_pred, bip.anz_res, 
            //bip.fb_pred_bias, bip.bias, ahp.bias_filter, ahp.offset, pp.max_bias, 
            //pp.min_bias, bi.starttime, bi.stoptime, ahp.model_err_thrsh,ahp.rate_lmt

            //from abc_blend_interval_props bip, abc_anz_hdr_props ahp, abc_prdgrp_props pp,
            //abc_blend_intervals bi

            //where bip.prop_id = ahp.prop_id and
            //bip.prop_id = pp.prop_id and
            //bip.blend_id = bi.blend_id and
            //bip.sequence = bi.sequence and
            //bip.anz_id(+) = ahp.anz_id and
            //upper(bip.anz_good_flag) = 'YES' and
            //upper(bip.calc_property_flag) = 'YES' and
            //bi.stoptime IS NOT NULL and
            //bip.feedback_pred IS NOT NULL and
            //bip.anz_res IS NOT NULL and
            //bip.blend_id = ? and
            //ahp.blender_id = ? and
            //pp.prdgrp_id = ?
            //order by bip.sequence desc

            return await (from bip in _blendMonitorContext.AbcBlendIntervalProps
                          from ahp in _blendMonitorContext.AbcAnzHdrProps
                          from pp in _blendMonitorContext.AbcPrdgrpProps
                          from bi in _blendMonitorContext.AbcBlendIntervals
                          where bip.PropId == ahp.PropId &&
                            bip.PropId == pp.PropId &&
                            bip.BlendId == bi.BlendId &&
                            bip.Sequence == bi.Sequence &&
                            bip.AnzId == ahp.AnzId &&
                            bip.AnzGoodFlag.ToUpper() == "YES" &&
                            bip.CalcPropertyFlag.ToUpper() == "YES" &&
                            bi.Stoptime != null &&
                            bip.FeedbackPred != null &&
                            bip.AnzRes != null &&
                            bip.BlendId == blendId &&
                            ahp.BlenderId == blenderId &&
                            pp.PrdgrpId == prdgrpId
                          select new BiasData
                          {
                              Sequence = bip.Sequence,
                              PropId = bip.PropId,
                              FeedbackPred = bip.FeedbackPred,
                              AnzRes = bip.AnzRes,
                              FbPredBias = bip.FbPredBias,
                              Bias = bip.Bias,
                              BiasFilter = ahp.BiasFilter,
                              Offset = ahp.Offset,
                              MaxBias = pp.MaxBias,
                              MinBias = pp.MinBias,
                              StartTime = bi.Starttime,
                              StopTime = bi.Stoptime,
                              ModelErrThrsh = ahp.ModelErrThrsh,
                              RateLmt = ahp.RateLmt
                          }).OrderBy(row => row.Sequence)
                          .ToListAsync<BiasData>();
        }
        public async Task<int> SetPropAnzOffset(double sngAnzOfst, double blendId, double propId)
        {
            //update abc_blend_props set anz_offset = ? where blend_id = ? and prop_id = ?
            AbcBlendProps Data = await _blendMonitorContext.AbcBlendProps
                                    .Where<AbcBlendProps>(row => row.BlendId == blendId && row.PropId == propId)
                                    .FirstOrDefaultAsync<AbcBlendProps>();
            Data.AnzOffset = sngAnzOfst;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetModelErr(double dblIntBiasNew, double blenderId, double vntPropID, double blendId, int vntIntvNum, double vntPropID2)
        {
            //update abc_anz_hdr_props set model_err = ?
            //where blender_id = ? and prop_id =? and 
            //anz_id = (select distinct abc_blend_interval_props.anz_id 
            //          from abc_blend_interval_props, abc_anz_hdr_props 
            //          where abc_blend_interval_props.blend_id =? and  abc_blend_interval_props.sequence =? and
            //          abc_blend_interval_props.prop_id = abc_anz_hdr_props.prop_id  and abc_blend_interval_props.anz_id = abc_anz_hdr_props.anz_id 
            //          and abc_blend_interval_props.prop_id =?) 

            double? anzId = await (from abip in _blendMonitorContext.AbcBlendIntervalProps
                                   from aahp in _blendMonitorContext.AbcAnzHdrProps
                                   where abip.BlendId == blendId && abip.Sequence == vntIntvNum &&
                                   abip.PropId == aahp.PropId && abip.AnzId == aahp.AnzId
                                   && abip.PropId == vntPropID2
                                   select abip.AnzId)
                                  .FirstOrDefaultAsync<double?>();
            AbcAnzHdrProps Data = await _blendMonitorContext.AbcAnzHdrProps
                                        .Where<AbcAnzHdrProps>(row => row.BlenderId == blenderId && row.PropId == vntPropID)
                                        .FirstOrDefaultAsync<AbcAnzHdrProps>();
            Data.ModelErr = dblIntBiasNew;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetUnFiltBias(double dblUnfilBias, double blendId, int vntIntvNum, double propId)
        {
            //"UPDATE ABC_BLEND_INTERVAL_PROPS SET UNFILT_BIAS=" & dblUnfilBias &
            //" WHERE BLEND_ID = " & curblend.lngID & " AND SEQUENCE >=" & vntIntvNum.Value & " AND" &
            //" PROP_ID=" & vntPropID.Value           

            AbcBlendIntervalProps Data = await _blendMonitorContext.AbcBlendIntervalProps
                                        .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId && row.PropId == propId && row.Sequence >= vntIntvNum)
                                        .FirstOrDefaultAsync<AbcBlendIntervalProps>();
            Data.UnfiltBias = dblUnfilBias;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetIntvBias(double dblIntBias, double blendId, int vntIntvNum, double vntPropID)
        {
            //update abc_blend_interval_props set bias = ? where blend_id = ? and sequence >= ? and prop_id = ?
            AbcBlendIntervalProps Data = await _blendMonitorContext.AbcBlendIntervalProps
                                                .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId && row.Sequence >= vntIntvNum && row.PropId == vntPropID)
                                                .FirstOrDefaultAsync<AbcBlendIntervalProps>();
            Data.Bias = dblIntBias;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetTqi(double blendId)
        {
            //UPDATE ABC_BLENDS SET TQI_NOW_FLAG=\'YES\' WHERE " + ("ID = " + curblend.lngID));
            AbcBlends Data = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Id == blendId)
                                .FirstOrDefaultAsync<AbcBlends>();
            Data.TqiNowFlag = "YES";
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<AbcBlendProps>> GetAllBlendProps(double blendId)
        {
            //SELECT BLEND_ID, PROP_ID, ANZ_RES_TAG_ID, VALUE, INITIAL_BIAS FROM ABC_BLEND_PROPS WHERE BLEND_ID =?
            return await _blendMonitorContext.AbcBlendProps
                    .Where<AbcBlendProps>(row => row.BlendId == blendId)
                    .ToListAsync<AbcBlendProps>();
        }
        public async Task<List<AbcBlendIntervalProps>> GetFdbackPred(double blendId)
        {
            //SELECT abc_blend_interval_props.sequence, 
            //    abc_blend_interval_props.prop_id, 
            //    abc_blend_interval_props.feedback_pred, 
            //    abc_blend_interval_props.bias
            //FROM abc_blend_interval_props, abc_blend_props, 
            //    abc_blend_intervals
            //WHERE abc_blend_interval_props.blend_id = abc_blend_props.blend_id
            //     AND
            //    abc_blend_interval_props.prop_id = abc_blend_props.prop_id AND
            //     abc_blend_intervals.blend_id = abc_blend_interval_props.blend_id
            //     AND abc_blend_interval_props.blend_id = ? AND
            //    abc_blend_intervals.sequence = abc_blend_interval_props.sequence
            //     AND abc_blend_interval_props.sequence <> 0 AND
            //    abc_blend_intervals.stoptime IS NOT NULL
            //ORDER BY abc_blend_interval_props.sequence DESC

            return await (from abip in _blendMonitorContext.AbcBlendIntervalProps
                          from abp in _blendMonitorContext.AbcBlendProps
                          from abi in _blendMonitorContext.AbcBlendIntervals
                          where abip.BlendId == abp.BlendId &&
                        abip.PropId == abp.PropId && abi.BlendId == abip.BlendId
                         && abip.BlendId == blendId && abi.Sequence == abip.Sequence
                         && abip.Sequence != 0 && abi.Stoptime != null
                          select abip).OrderByDescending(row => row.Sequence).ToListAsync<AbcBlendIntervalProps>();
        }
        public async Task<int> SetBlendPropsValue(double? vntFdbkPred, double blendId, double propId)
        {
            //update abc_blend_props set value = ? where blend_id = ? and prop_id = ?
            AbcBlendProps Data = await _blendMonitorContext.AbcBlendProps
                                        .Where<AbcBlendProps>(row => row.BlendId == blendId && row.PropId == propId)
                                        .FirstOrDefaultAsync<AbcBlendProps>();
            Data.Value = vntFdbkPred;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<BldSampleProps>> CompositeSpotSample(double blendId)
        {
            //SELECT DISTINCT BS.NAME AS SAMPLE_NAME,
            // bs.type as sample_type,
            //bs.start_date, bs.stop_date,
            //nvl(bs.start_volume, -1) as start_volume,
            //nvl(bs.stop_volume, -1) as stop_volume,
            //BS.PROCESS_SAMPLE_FLAG
            //FROM ABC_BLEND_SAMPLE_PROPS SP, ABC_BLEND_SAMPLES BS
            //WHERE BS.BLEND_ID = SP.BLEND_ID AND SP.SAMPLE_NAME = BS.NAME(+)  AND(SP.BLEND_ID =?)
            //AND USED_FLAG IN('NO', 'YES')
            //ORDER BY start_DATE, start_volume
            List<string> Flags = new List<string>() { "NO", "YES" };
            return await (from bsp in _blendMonitorContext.AbcBlendSampleProps
                          from bs in _blendMonitorContext.AbcBlendSamples
                          where bsp.BlendId == bs.BlendId && bsp.SampleName == bs.Name &&
                          Flags.Contains(bsp.UsedFlag) && bsp.BlendId == blendId
                          select new BldSampleProps
                          {
                              SampleName = bs.Name,
                              SampleType = bs.Type,
                              StartDate = bs.StartDate,
                              StopDate = bs.StopDate,
                              StartVolume = (bs.StartVolume == null) ? -1 : bs.StartVolume,
                              StopVolume = (bs.StopVolume == null) ? -1 : bs.StopVolume,
                              ProcessSampleFlag = bs.ProcessSampleFlag
                          }).OrderBy(row => row.StartDate).ThenBy(row => row.StartVolume)
                         .ToListAsync<BldSampleProps>();

        }
        public async Task<double> GetMatId(string name)
        {
            //select ID as mat_ID from abc_materials where NAME=?
            return await _blendMonitorContext.AbcMaterials
                        .Where<AbcMaterials>(row => row.Name == name)
                        .Select(row => row.Id)
                        .FirstOrDefaultAsync<double>();
        }
        public async Task<int> SetProcessSampleFlag(double blendId)
        {
            //UPDATE ABC_BLEND_SAMPLES SET PROCESS_SAMPLE_FLAG=\'NO\' WHERE " + ("BLEND_ID = " + curblend.lngID)
            AbcBlendSamples Data = await _blendMonitorContext.AbcBlendSamples
                        .Where<AbcBlendSamples>(row => row.BlendId == blendId)
                        .FirstOrDefaultAsync<AbcBlendSamples>();
            Data.ProcessSampleFlag = "NO";
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<double> GetAbcBlendIntervalSequence(string strBlendID, DateTime dteStartStopDate)
        {
            //"SELECT min(SEQUENCE) AS SEQ  " & _
            //"FROM ABC_BLEND_INTERVALS " & _
            //"WHERE BLEND_ID = " & strBlendID & " AND " & _
            //"STOPTIME >= TO_DATE('" & Format(dteStartStopDate, strWinDateFmt & " " & WIN_TIME_FMT) & "','" & strOraDateFmt & " " & ORA_TIME_FMT & "')"

            return await _blendMonitorContext.AbcBlendIntervals
                            .Where<AbcBlendIntervals>(row => row.BlendId == Convert.ToDouble(strBlendID) && row.Stoptime >= dteStartStopDate)
                            .Select(row => row.Sequence)
                            .MinAsync<double>();
        }
        public async Task<double> GetHighLowSequenceVolRange(string strBlendID, string strMinMaxVol)
        {
            //"SELECT min(SEQUENCE) AS SEQ " & _
            //"FROM ABC_BLEND_INTERVALS " & _
            //"WHERE BLEND_ID = " & strBlendID & " AND " & _
            //"BLEND_VOLUME >= " & strMinMaxVol

            return await _blendMonitorContext.AbcBlendIntervals
                            .Where<AbcBlendIntervals>(row => row.BlendId == Convert.ToDouble(strBlendID) && row.BlendVolume >= Convert.ToDouble(strMinMaxVol))
                            .Select(row => row.Sequence)
                            .MinAsync<double>();
        }
        public async Task<PropNameModel> GetPropertyID(string text)
        {
            //select p.id as prop_id, u.units_name
            //from abc_properties p, abc_uom u
            //where p.uom_id = u.id and
            //p.name =?

            return await (from p in _blendMonitorContext.AbcProperties
                          from u in _blendMonitorContext.AbcUom
                          where p.UomId == u.Id && p.Name == text
                          select new PropNameModel
                          {
                              PropId = p.Id,
                              UnitsName = u.UnitsName
                          }).FirstOrDefaultAsync<PropNameModel>();
        }
        public async Task<List<AbcBlendIntervalProps>> GetEtohAnzIntProp(double blendId, int intStartInterval, int intStopInterval, int intEtohId)
        {
            //Select nvl(bip.anz_res, -1) as anz_res, nvl(bip.setpoint_pred, -1) as setpoint_pred, nvl(bip.feedback_pred, -1) as feedback_pred
            //from abc_blend_interval_props bip
            //where bip.blend_id =? and
            //bip.sequence >=? and
            //bip.sequence <=? and
            //bip.prop_id =?
            //order by bip.sequence
            return await _blendMonitorContext.AbcBlendIntervalProps
                                            .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId && row.Sequence >= intStartInterval
                                            && row.Sequence <= intStopInterval && row.PropId == intEtohId)
                                            .Select(row => new AbcBlendIntervalProps
                                            {
                                                AnzRes = (row.AnzRes == null) ? -1 : row.AnzRes,
                                                SetpointPred = (row.SetpointPred == null) ? -1 : row.SetpointPred,
                                                FeedbackPred = (row.FeedbackPred == null) ? -1 : row.FeedbackPred
                                            }).ToListAsync<AbcBlendIntervalProps>();
        }
        public async Task<List<PropCalcId>> GetPropCalcId(int prdgrpID, double propID)
        {
            //select pp.calc_id, cr.name as calc_name
            //from abc_prdgrp_props pp, abc_calc_routines cr
            //where pp.calc_id = cr.id(+) and pp.prdgrp_id =? and
            //  pp.prop_id =?
            return await (from pp in _blendMonitorContext.AbcPrdgrpProps
                          from cr in _blendMonitorContext.AbcCalcRoutines
                          where pp.CalcId == cr.Id && pp.PrdgrpId == prdgrpID && pp.PropId == propID
                          select new PropCalcId
                          {
                              CalcId = pp.CalcId,
                              CalcName = cr.Name
                          }).ToListAsync<PropCalcId>();

        }
        public async Task<List<AbcBlendIntervalProps>> GetAnzIntProp(double blendId, int num, double propID)
        {
            //Select nvl(bip.anz_res, -1) as anz_res, nvl(bip.setpoint_pred, -1) as setpoint_pred, nvl(bip.feedback_pred, -1) as feedback_pred
            //from abc_blend_interval_props bip, abc_blend_sample_props bsp
            //where bip.blend_id = bsp.blend_id and bip.prop_id = bsp.prop_id and
            //bsp.used_flag = 'NO' and bip.blend_id =? and bip.sequence =? and
            //  bip.prop_id =?
            return await (from bip in _blendMonitorContext.AbcBlendIntervalProps
                          from bsp in _blendMonitorContext.AbcBlendSampleProps
                          where bip.BlendId == bsp.BlendId && bip.PropId == bsp.PropId &&
                           bsp.UsedFlag == "NO" && bip.BlendId == blendId && bip.Sequence == num &&
                           bip.PropId == propID
                          select new AbcBlendIntervalProps
                          {
                              AnzRes = (bip.AnzRes == null) ? -1 : bip.AnzRes,
                              SetpointPred = (bip.SetpointPred == null) ? -1 : bip.SetpointPred,
                              FeedbackPred = (bip.FeedbackPred == null) ? -1 : bip.FeedbackPred,
                          }).ToListAsync<AbcBlendIntervalProps>();
        }
        public async Task<List<PropNameModel>> GetAbcBlendIntervalPropsdata(string strVarName, double blendId, int num)
        {
            //"select prop_id, nvl(" & strVarName & ",feedback_pred) as value " & _
            //"from abc_blend_interval_props where blend_id = " & lngBlendId & " and " & _
            //"sequence= " & intNum

            return await _blendMonitorContext.AbcBlendIntervalProps
                                                    .Where<AbcBlendIntervalProps>(row => row.BlendId == blendId && row.Sequence == num)
                                                    .Select(row => new PropNameModel
                                                    {
                                                        PropId = row.PropId,
                                                        Value = (strVarName == "ANZ_RES") ? ((row.AnzRes == null) ? row.FeedbackPred : row.AnzRes) :
                                                            ((strVarName == "SETPOINT_PRED") ? ((row.SetpointPred == null) ? row.FeedbackPred : row.SetpointPred) : row.FeedbackPred)
                                                    }).ToListAsync<PropNameModel>();
        }
        public async Task<int> SetBlendSampleProps(string strSampleField, double dblAvgVarValue, double blendId, string strSampleName, double propId)
        {
            //"UPDATE ABC_BLEND_SAMPLE_PROPS SET " & strSampleField & "=" & dblAvgVarValue & _
            //" WHERE BLEND_ID = " & lngBlendId & " AND SAMPLE_NAME='" & strSampleName & _
            //"' AND PROP_ID=" & lngPropID
            AbcBlendSampleProps Data = await _blendMonitorContext.AbcBlendSampleProps
                        .Where<AbcBlendSampleProps>(row => row.BlendId == blendId && row.SampleName == strSampleName && row.PropId == propId)
                        .FirstOrDefaultAsync<AbcBlendSampleProps>();
            if (strSampleField == "ANZ_VALUE")
            {
                Data.AnzValue = dblAvgVarValue;
            }
            else if (strSampleField == "FEEDBACK")
            {
                Data.Feedback = dblAvgVarValue;
            }
            else if (strSampleField == "SETPOINT_PRED")
            {
                Data.SetpointPred = dblAvgVarValue;
            }

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<bool> GetNewUsedSample(double blendId, string strSample)
        {
            //"SELECT USED_FLAG " & _
            //"FROM ABC_BLEND_SAMPLE_PROPS " & _
            //"WHERE BLEND_ID = " & lngBlendId & " AND " & _
            //"SAMPLE_NAME = '" & strSample & "' AND " & _
            //"USED_FLAG = 'YES'"
            var data = await _blendMonitorContext.AbcBlendSampleProps
                            .Where<AbcBlendSampleProps>(row => row.BlendId == blendId && row.SampleName == strSample && row.UsedFlag == "YES")
                            .Select(row => row.UsedFlag)
                            .ToListAsync<string>();
            if (data.Count() > 0)
                return true;
            else
                return false;
        }
        public async Task<int> SetWriteStrTagVal(string prdName, double? BlendDescTid)
        {
            //update abc_tags set write_string = ?, write_now_flag = 'YES' where id = ?
            AbcTags data = await _blendMonitorContext.AbcTags
                                .Where<AbcTags>(row => row.Id == BlendDescTid)
                                .FirstOrDefaultAsync<AbcTags>();
            data.WriteString = prdName;
            data.WriteNowFlag = "YES";
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<BldrSrcSlctfbTids>> GetBldrSrcSlctfbTids(double blenderId, double blendId)
        {
            //select abc_blender_sources.tank_id, 
            //       abc_blend_sources.mat_id,
            //       abc_blender_sources.selection_fb_tid,
            //       abc_blend_sources.lineup_id
            //from abc_blender_sources,
            //     abc_blend_sources
            //where abc_blender_sources.blender_id = ?
            //  and abc_blend_sources.blend_id = ?
            //  and abc_blend_sources.in_use_flag = 'YES'
            //  and abc_blend_sources.tank_id = abc_blender_sources.tank_id
            //order by abc_blend_sources.mat_id
            return await (from abrs in _blendMonitorContext.AbcBlenderSources
                          from abs in _blendMonitorContext.AbcBlendSources
                          where abrs.BlenderId == blenderId
                          && abs.BlendId == blendId
                          && abs.InUseFlag == "YES"
                          && abs.TankId == abrs.TankId
                          select new BldrSrcSlctfbTids
                          {
                              TankId = abrs.TankId,
                              MatId = abs.MatId,
                              SelectionFbTid = abrs.SelectionFbTid,
                              LineUpId = abs.LineupId
                          }).OrderBy(row => row.MatId)
                          .ToListAsync<BldrSrcSlctfbTids>();
        }
        public async Task<string> GetTankName(double tankId)
        {
            //SELECT @TANK_NAME = ABC_TANKS.NAME
            // FROM dbo.ABC_TANKS
            // WHERE ABC_TANKS.ID = @TANK_ID
            return await _blendMonitorContext.AbcTanks
                        .Where(row => row.Id == tankId)
                        .Select(row => row.Name)
                        .FirstOrDefaultAsync<string>();
        }
        public async Task<List<AbcTanks>> GetDataTankID(double tankID)
        {
            //select Name, Mat_id, abc_service_flag, dcs_service_tid, source_destn_type, shared_name, dcs_tank_num, volume as transfer_volume
            //from abc_tanks where id =?
            return await _blendMonitorContext.AbcTanks
                    .Where(row => row.Id == tankID)
                    .ToListAsync<AbcTanks>();
        }
        public async Task<AbcCompLineups> GetDCSCompLineupNum(double lngLineupID)
        {
            //select name as lineup_name, nvl(dcs_lineup_num,-1) as dcs_lineup_num  from abc_comp_lineups where id=?
            return await _blendMonitorContext.AbcCompLineups
                        .Where<AbcCompLineups>(row => row.Id == lngLineupID)
                        .FirstOrDefaultAsync<AbcCompLineups>();
        }
        public async Task<List<AbcBlenderComps>> GetAllBldrComps(double blenderId)
        {
            //SELECT MAT_ID, SELECT_COMP_TID, TOT_COMP_VOL_TID, LINEUP_SEL_TID, LINEUP_PRESEL_TID, LINEUP_FEEDBACK_TID
            //FROM ABC_BLENDER_COMPS WHERE BLENDER_ID =?
            return await _blendMonitorContext.AbcBlenderComps
                        .Where<AbcBlenderComps>(row => row.BlenderId == blenderId)
                        .ToListAsync<AbcBlenderComps>();
        }
        public async Task<List<AbcBlenderDest>> GetBldrDestSelTid(double blenderId, int tankId)
        {
            // select selection_tid, selection_fb_tid, dest_select_name_tid, preselection_tid from abc_blender_dest where blender_id = ? and tank_id = ?
            return await _blendMonitorContext.AbcBlenderDest
                         .Where<AbcBlenderDest>(row => row.BlenderId == blenderId && row.TankId == tankId)
                         .ToListAsync<AbcBlenderDest>();

        }
        public async Task<List<AbcBlenders>> GetBldrLineupTags(double blenderId)
        {
            //select name as blender_name, tank_sel_tid, tank_presel_tid, tank_feedback_tid, lineup_sel_tid, lineup_presel_tid,
            //lineup_feedback_tid, pumpa_sel_tid, pumpb_sel_tid, pumpc_sel_tid, pumpd_sel_tid 
            //from abc_blenders where id=?
            return await _blendMonitorContext.AbcBlenders
                        .Where<AbcBlenders>(row => row.Id == blenderId)
                        .ToListAsync<AbcBlenders>();
        }
        public async Task<List<AbcBlendDest>> GetTkDestData(double blendId, int tankId)
        {
            //select dest_select_name,lineup_id from abc_blend_dest where blend_id= ? and tank_id = ?
            return await _blendMonitorContext.AbcBlendDest
                        .Where<AbcBlendDest>(row => row.BlendId == blendId && row.TankId == tankId)
                        .ToListAsync<AbcBlendDest>();
        }
        public async Task<List<AbcStations>> GetStationPaceTids(double blendId)
        {
            //select id as station_id, pace_me_flag_tid 
            //from abc_stations 
            //where upper(in_use_flag) = 'YES' and id in 
            //(select station_id from abc_comp_lineup_eqp 
            //where line_id in (select lineup_id from abc_blend_sources where blend_id =?))

            List<double?> LineupIds = await _blendMonitorContext.AbcBlendSources
                                            .Where<AbcBlendSources>(row => row.BlendId == blendId)
                                            .Select(row => row.LineupId)
                                            .ToListAsync<double?>();
            List<double?> StationIds = await _blendMonitorContext.AbcCompLineupEqp
                                            .Where<AbcCompLineupEqp>(row => LineupIds.Contains(row.LineId))
                                            .Select(row => row.StationId)
                                            .ToListAsync<double?>();
            return await _blendMonitorContext.AbcStations
                            .Where<AbcStations>(row => row.InUseFlag.ToUpper() == "YES" && StationIds.Contains(row.Id))
                            .ToListAsync<AbcStations>();
        }
        public async Task<List<double>> GetBldStationMatId(double blendId, double stationId)
        {
            //select mat_id from abc_blend_stations where blend_id=? and station_id=? order by mat_id asc
            return await _blendMonitorContext.AbcBlendStations
                            .Where<AbcBlendStations>(row => row.BlendId == blendId && row.StationId == stationId)
                            .Select(row => row.MatId)
                            .OrderBy(row => row)
                            .ToListAsync<double>();

        }
        public async Task<int> SetBlendCompPacingFactor(int factor, double blendId, double MatId)
        {
            //"UPDATE abc_blend_comps SET pacing_factor = 1" & _
            //"WHERE blend_id=" & curblend.lngID & " AND mat_id=" & lngMatId

            AbcBlendComps Data = await _blendMonitorContext.AbcBlendComps
                                    .Where<AbcBlendComps>(row => row.BlendId == blendId && row.MatId == MatId)
                                    .FirstOrDefaultAsync<AbcBlendComps>();
            Data.PacingFactor = factor;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<DateTime?>> GetLastOptTime(double blendId)
        {
            //select LAST_OPTIMIZED_TIME from abc_blends where id=?
            return await _blendMonitorContext.AbcBlends
                        .Where<AbcBlends>(row => row.Id == blendId)
                        .Select(row => row.LastOptimizedTime)
                        .ToListAsync<DateTime?>();
        }
        public async Task<List<AbcProdLineups>> GetPrdLnupSlctTid(double blendId, int destTank1ID, int bldrID, int destTank2ID)
        {
            //select selection_tid, selection_fb_tid from
            //abc_prod_lineups where id =
            //(select lineup_id from abc_blend_dest where blend_id =? and tank_id =?) and
            //source_id = ? and destination_id = ?
            double lineupId = await _blendMonitorContext.AbcBlendDest
                                .Where<AbcBlendDest>(row => row.BlendId == blendId && row.TankId == destTank1ID)
                                .Select(row => row.LineupId)
                                .FirstOrDefaultAsync<double>();
            return await _blendMonitorContext.AbcProdLineups
                        .Where<AbcProdLineups>(row => row.Id == lineupId && row.SourceId == bldrID && row.DestinationId == destTank2ID)
                        .ToListAsync<AbcProdLineups>();
        }
        public async Task<List<CompEqpData>> GetCompEqpData(double blendId, int blenderId)
        {
            //select abc_blend_sources.tank_id, abc_blend_sources.mat_id,
            //abc_blend_sources.lineup_id, abc_comp_lineups.selection_tid lnup_slct_tid,
            //abc_blender_sources.selection_tid bldr_src_tid, abc_blender_comps.select_comp_tid

            //from abc_blend_sources, abc_comp_lineups, abc_blender_sources, abc_blender_comps

            //where abc_blend_sources.blend_id = ? and abc_blender_sources.blender_id = ? and 
            //abc_blend_sources.lineup_id = abc_comp_lineups.id and abc_blend_sources.tank_id = abc_blender_sources.tank_id 
            //and abc_blend_sources.mat_id = abc_blender_comps.mat_id 
            //and abc_blender_sources.blender_id = abc_blender_comps.blender_id

            return await (from abs in _blendMonitorContext.AbcBlendSources
                          from acl in _blendMonitorContext.AbcCompLineups
                          from abrs in _blendMonitorContext.AbcBlenderSources
                          from abc in _blendMonitorContext.AbcBlenderComps
                          where abs.BlendId == blendId && abrs.BlenderId == blenderId &&
                          abs.LineupId == acl.Id && abs.TankId == abrs.TankId
                          && abs.MatId == abc.MatId && abrs.BlenderId == abc.BlenderId
                          select new CompEqpData
                          {
                              TankId = abs.TankId,
                              MatId = abs.MatId,
                              LineupId = abs.LineupId,
                              LineupSelectTid = acl.SelectionTid,
                              Bldrsrctid = abrs.SelectionTid,
                              Selectcomptid = abc.SelectCompTid
                          }).ToListAsync<CompEqpData>();

        }
        public async Task<List<double?>> GetPumpInuseTids(int lnupID)
        {
            //select inuse_tag_id from abc_pumps where id in (select pump_id from abc_comp_lineup_eqp where line_id = ?)
            List<double?> pumpIds = await _blendMonitorContext.AbcCompLineupEqp
                                        .Where<AbcCompLineupEqp>(row => row.LineId == lnupID)
                                        .Select(row => row.PumpId)
                                        .ToListAsync<double?>();
            return await _blendMonitorContext.AbcPumps
                        .Where<AbcPumps>(row => pumpIds.Contains(row.Id))
                        .Select(row => row.InuseTagId)
                        .ToListAsync<double?>();
        }
        public async Task<List<double?>> GetAllPumpsForPrdgrp(int bldrID)
        {
            //select inuse_tag_id from abc_pumps where prdgrp_id = (select prdgrp_id from abc_blenders where id = ?)
            List<double> prdgrpIds = await _blendMonitorContext.AbcBlenders
                                       .Where<AbcBlenders>(row => row.Id == bldrID)
                                       .Select(row => row.PrdgrpId)
                                       .ToListAsync<double>();
            return await _blendMonitorContext.AbcPumps
                        .Where<AbcPumps>(row => prdgrpIds.Contains(Convert.ToDouble(row.PrdgrpId)))
                        .Select(row => row.InuseTagId)
                        .ToListAsync<double?>();
        }
        public async Task<List<AbcBlends>> GetAbcBlendData(double blenderId, int prodId)
        {
            //"SELECT ID, NAME,ACTUAL_START " & _
            //"FROM ABC_BLENDS " & _
            //"WHERE BLENDER_ID= " & vntBldrsData(BLDR_ID, intBldrIdx) & " AND " & _
            //"BLEND_STATE IN ('DONE','SEALED','COMM ERR','CANCELED') AND PRODUCT_ID =" & curblend.intProdID & _
            //" AND ACTUAL_START IS NOT NULL ORDER BY ACTUAL_START DESC"
            List<string> states = new List<string>() { "DONE", "SEALED", "COMM ERR", "CANCELED" };

            return await _blendMonitorContext.AbcBlends
                .Where<AbcBlends>(row => row.BlenderId == blenderId && states.Contains(row.BlendState) &&
                row.ProductId == prodId && row.ActualStart != null)
                .OrderByDescending(row => row.ActualStart)
                .Select(row => new AbcBlends
                {
                    Id = row.Id,
                    Name = row.Name,
                    ActualStart = row.ActualStart
                })
                .ToListAsync<AbcBlends>();

        }
        public async Task<List<BldComps>> GetBldComps(double blendId)
        {
            //select bs.mat_id, m.name as mat_name, bs.tank_id
            //from abc_blend_sources bs, abc_materials m
            //where bs.mat_id = m.id and
            //bs.blend_id = ? and
            //bs.in_use_flag = 'YES'
            //order by m.id
            return await (from bs in _blendMonitorContext.AbcBlendSources
                          from m in _blendMonitorContext.AbcMaterials
                          where bs.MatId == m.Id && bs.BlendId == blendId && bs.InUseFlag == "YES"
                          select new BldComps
                          {
                              MatId = bs.MatId,
                              MatName = m.Name,
                              TankId = bs.TankId
                          }).OrderBy(row => row.MatId)
                          .ToListAsync<BldComps>();
        }
        public async Task<List<BldProps>> GetBldProps(double blendId)
        {
            //select p.id, p.name, p.alias
            //from abc_properties p, abc_blend_props bp
            //where p.id = bp.prop_id and
            //bp.blend_id = ?
            //order by p.id
            return await (from bp in _blendMonitorContext.AbcBlendProps
                          from p in _blendMonitorContext.AbcProperties
                          where p.Id == bp.PropId && bp.BlendId == blendId
                          select new BldProps
                          {
                              Id = p.Id,
                              Name = p.Name,
                              Alias = p.Alias
                          }).OrderBy(row => row.Id)
                          .ToListAsync<BldProps>();
        }
     
        public async Task<List<DenaturantProps>> GetDenaturantProps()
        {
            //select p.name, pmp.def_val, uom.units_name, uom.dimension, c.name as calc_rtn

            //from abc_prdgrp_mat_props pmp, abc_properties p, abc_uom uom, 
            //abc_materials m, abc_prdgrp_props pp, abc_calc_routines c

            //where pmp.prop_id = p.id and pmp.mat_id = m.id and
            //pmp.usage_id = (select id from abc_usages where name = 'COMPONENT') and
            //pmp.prdgrp_id = 103 and m.name = 'DENATURANT' and
            //pmp.prdgrp_id = pp.prdgrp_id and
            //pp.prop_id = p.id and
            //pp.calc_id = c.id and
            //p.uom_id = uom.id
            //order by p.id
            double usageId = await _blendMonitorContext.AbcUsages
                                .Where<AbcUsages>(row => row.Name == "COMPONENT")
                                .Select(row => row.Id).FirstOrDefaultAsync<double>();
            return await (from pmp in _blendMonitorContext.AbcPrdgrpMatProps
                          from p in _blendMonitorContext.AbcProperties
                          from uom in _blendMonitorContext.AbcUom
                          from m in _blendMonitorContext.AbcMaterials
                          from pp in _blendMonitorContext.AbcPrdgrpProps
                          from c in _blendMonitorContext.AbcCalcRoutines
                          where pmp.PropId == p.Id && pmp.MatId == m.Id &&
                           pmp.UsageId == usageId && pmp.PrdgrpId == 103 && m.Name == "DENATURANT" &&
                           pmp.PrdgrpId == pp.PrdgrpId && pp.PropId == p.Id && pp.CalcId == c.Id &&
                           p.UomId == uom.Id
                          select new DenaturantProps
                          {
                              Id = p.Id,
                              Name = p.Name,
                              DefVal = pmp.DefVal,
                              UnitsName = uom.UnitsName,
                              Dimension = uom.Dimension,
                              CalcRtn = c.Name
                          }).OrderBy(row => row.Id)
                     .ToListAsync<DenaturantProps>();
        }
        public async Task<int> SetEtohBldgReqd(string text, double blendId)
        {
            //update abc_blends 
            //set ethanol_bldg_reqd_flag = ?
            //where id = ?
            AbcBlends Data = await _blendMonitorContext.AbcBlends.Where<AbcBlends>(row => row.Id == blendId).FirstOrDefaultAsync<AbcBlends>();
            Data.EthanolBldgReqdFlag = text;

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<AbcProdLineups>> GetAbcProLinupData()
        {
            //SELECT ID, SELECTION_TID FROM ABC_PROD_LINEUPS
            return await _blendMonitorContext.AbcProdLineups.Select(row => new AbcProdLineups
            {
                Id = row.Id,
                SelectionTid = row.SelectionTid
            }).ToListAsync<AbcProdLineups>();
        }
        public async Task<List<AbcCompLineups>> GetAbcCompLinupData()
        {
            //SELECT ID, SELECTION_TID FROM ABC_COMP_LINEUPS
            return await _blendMonitorContext.AbcCompLineups.Select(row => new AbcCompLineups
            {
                Id = row.Id,
                SelectionTid = row.SelectionTid
            }).ToListAsync<AbcCompLineups>();
        }
        public async Task<string> GetTagName(double? tagId)
        {
            //select upper(name) into tag_name from abc_tags where id = tag_id;
            return await _blendMonitorContext.AbcTags
                        .Where<AbcTags>(row => row.Id == tagId)
                        .Select(row => row.Name.ToUpper())
                        .FirstOrDefaultAsync<string>();
        }
        public async Task<List<AbcTanks>> GetTankNum(int tankid)
        {
            //select name as tank_name, mat_id, dcs_tank_num from abc_tanks where id=?
            return await _blendMonitorContext.AbcTanks
                    .Where<AbcTanks>(row => row.Id == tankid)
                    .ToListAsync<AbcTanks>();
        }
        public async Task<List<BlendSwingsData>> BlendSwingsData(string txt, int tankId, double blendId)
        {
            //SELECT bldswg.blend_id, bldswg.from_tk_id, bldswg.to_tk_id, 
            //  bldswg.done_at,mat1.id as from_tk_mat_id, bldswg.swing_type,  bldswg.criteria_id, swgcrit.name as criteria_name,
            //    bldswg.criteria_num_lmt, 
            //    bldswg.criteria_tim_lmt,  bldswg.swing_state, bldswg.auto_swing_flag
            //FROM abc_blend_swings bldswg, abc_blends bld, abc_swing_criteria swgcrit,
            //    abc_transtxt txt1, abc_transtxt txt2,
            //    abc_tanks tank1, abc_tanks tank2, abc_materials mat1,
            //    abc_materials mat2
            //WHERE bldswg.blend_id = bld.id(+) AND
            //    bldswg.from_tk_id = tank1.id AND
            //    bldswg.to_tk_id = tank2.id AND
            //    tank1.mat_id = mat1.id(+) AND
            //    tank2.mat_id = mat2.id(+) AND
            //    bldswg.criteria_id = swgcrit.id(+) AND
            //    bldswg.swing_type = txt1.value(+) AND
            //    txt1.value = ? AND
            //    txt1.word_set(+) = 'SWINGTYPE' AND
            //    bldswg.auto_swing_flag = txt2.value(+) AND
            //    txt2.word_set(+) = 'YESNO' AND
            //    bldswg.from_tk_id =? AND bldswg.blend_id = ? AND
            //    (bldswg.swing_state <> 'COMPLETE' AND bldswg.swing_state <> 'INCOMPLETE' AND bldswg.swing_state IS NOT NULL)

            return await (from bldswg in _blendMonitorContext.AbcBlendSwings
                          from bld in _blendMonitorContext.AbcBlends
                          from swgcrit in _blendMonitorContext.AbcSwingCriteria
                          from txt1 in _blendMonitorContext.AbcTranstxt
                          from txt2 in _blendMonitorContext.AbcTranstxt
                          from tank1 in _blendMonitorContext.AbcTanks
                          from tank2 in _blendMonitorContext.AbcTanks
                          from mat1 in _blendMonitorContext.AbcMaterials
                          from mat2 in _blendMonitorContext.AbcMaterials
                          where bldswg.BlendId == bld.Id &&
                        bldswg.FromTkId == tank1.Id &&
                        bldswg.ToTkId == tank2.Id &&
                        tank1.MatId == mat1.Id &&
                        tank2.MatId == mat2.Id &&
                        bldswg.CriteriaId == swgcrit.Id &&
                        bldswg.SwingType == txt1.Value &&
                        txt1.Value == txt &&
                        txt1.WordSet == "SWINGTYPE" &&
                        bldswg.AutoSwingFlag == txt2.Value &&
                        txt2.WordSet == "YESNO" &&
                        bldswg.FromTkId == tankId && bldswg.BlendId == blendId &&
                        (bldswg.SwingState != "COMPLETE" && bldswg.SwingState != "INCOMPLETE" && bldswg.SwingState != null)
                          select new BlendSwingsData
                          {
                              BlendId = bldswg.BlendId,
                              FromTkId = bldswg.FromTkId,
                              ToTkId = bldswg.ToTkId,
                              DoneAt = bldswg.DoneAt,
                              FromTkMatId = mat1.Id,
                              SwingType = bldswg.SwingType,
                              CriteriaId = bldswg.CriteriaId,
                              CriteriaName = swgcrit.Name,
                              CriteriaNumLmt = bldswg.CriteriaNumLmt,
                              CriteriaTimLmt = bldswg.CriteriaTimLmt,
                              SwingState = bldswg.SwingState,
                              AutoSwingFlag = bldswg.AutoSwingFlag
                          }).ToListAsync<BlendSwingsData>();
        }
        public async Task<List<ASTankID>> GetASTankID(int tankId)
        {
            //SELECT TANK.MAX_VOL_TID, TAG1.READ_VALUE AS MAX_VOL,
            //TANK.MIN_VOL_TID, TAG2.READ_VALUE AS MIN_VOL,TANK.AVAIL_VOL_ID, 
            //TAG3.READ_VALUE AS AVAIL_VOL 

            //FROM ABC_TANKS TANK, ABC_TAGS TAG1,
            //ABC_TAGS TAG2, ABC_TAGS TAG3 

            //WHERE TANK.MAX_VOL_TID = TAG1.ID AND TANK.MIN_VOL_TID = TAG2.ID
            //AND TANK.AVAIL_VOL_ID = TAG3.ID AND TANK.ID =?

            return await (from TANK in _blendMonitorContext.AbcTanks
                          from TAG1 in _blendMonitorContext.AbcTags
                          from TAG2 in _blendMonitorContext.AbcTags
                          from TAG3 in _blendMonitorContext.AbcTags
                          where TANK.MaxVolTid == TAG1.Id && TANK.MinVolTid == TAG2.Id
                            && TANK.AvailVolId == TAG3.Id && TANK.Id == tankId
                          select new ASTankID
                          {
                              MaxVolTid = TANK.MaxVolTid,
                              MaxVol = TAG1.ReadValue,
                              MinVolTid = TANK.MinVolTid,
                              MinVol = TAG2.ReadValue,
                              AvailVolId = TANK.AvailVolId,
                              AvailVol = TAG3.ReadValue
                          }).ToListAsync<ASTankID>();
        }
        
        public async Task<int> SetReadTagVal(int startOkTid)
        {
            //UPDATE ABC_TAGS SET READ_VALUE= 0 " + ("WHERE ID = " + intStartOkTid)
            AbcTags Data = await _blendMonitorContext.AbcTags
                            .Where<AbcTags>(row => row.Id == startOkTid)
                            .SingleOrDefaultAsync<AbcTags>();
            Data.ReadValue = 0;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<AbcBlenderComps>> GetBldrCmpData(double blenderId, double blendId)
        {
            //select abc_blender_comps.recipe_sp_tid,
            //       abc_blender_comps.select_comp_tid
            //from   abc_blender_comps,
            //       abc_blend_sources
            //where
            //       abc_blender_comps.blender_id =?
            //  and  abc_blend_sources.blend_id =?
            //  and  abc_blend_sources.mat_id = abc_blender_comps.mat_id
            //  and abc_blend_sources.in_use_flag = 'YES'
            //order by
            //       abc_blend_sources.mat_id

            return await (from abc in _blendMonitorContext.AbcBlenderComps
                          from abs in _blendMonitorContext.AbcBlendSources
                          where abc.BlenderId == blenderId && abs.BlendId == blendId
                          && abs.MatId == abc.MatId && abs.InUseFlag == "YES"
                          select new AbcBlenderComps
                          {
                              RecipeSpTid = abc.RecipeSpTid,
                              SelectCompTid = abc.SelectCompTid,
                              MatId = abs.MatId
                          }).OrderBy(row => row.MatId)
                          .ToListAsync<AbcBlenderComps>();
        }
        public async Task<List<CompTanksData>> GetCompTanksData(double blendId)
        {
            //select abc_blend_sources.mat_id,
            //    upper(abc_materials.name) comp_name,
            //    abc_blend_sources.tank_id,
            //    upper(abc_tanks.name) tank_name,
            //    abc_tanks.rundn_id,
            //    abc_tanks.avail_vol_id,
            //    abc_tanks.min_vol_tid,
            //    abc_tanks.dcs_service_tid,
            //    upper(abc_tanks.abc_service_flag) abc_service_flag,
            //    abc_blend_sources.lineup_id,
            //    abc_blend_comps.cur_recipe,
            //    abc_tanks.max_vol_tid,
            //    abc_tanks.source_destn_type,
            //    abc_blend_comps.usage_id
            //from abc_blend_sources,
            //    abc_materials,
            //    abc_tanks,
            //    abc_blend_comps
            //where abc_blend_sources.blend_id = ?
            //    and abc_blend_sources.tank_id = abc_tanks.id
            //    and abc_blend_sources.blend_id = abc_blend_comps.blend_id
            //    and abc_blend_sources.mat_id = abc_materials.id
            //    and abc_blend_sources.mat_id = abc_blend_comps.mat_id
            //    and abc_blend_sources.in_use_flag = 'YES'
            //order by abc_blend_sources.mat_id

            return await (from abs in _blendMonitorContext.AbcBlendSources
                          from am in _blendMonitorContext.AbcMaterials
                          from abc in _blendMonitorContext.AbcBlendComps
                          from at in _blendMonitorContext.AbcTanks
                          where abs.BlendId == blendId
                            && abs.TankId == at.Id
                            && abs.BlendId == abc.BlendId
                            && abs.MatId == am.Id
                            && abs.MatId == abc.MatId
                            && abs.InUseFlag == "YES"
                          select new CompTanksData
                          {
                              MatId = abs.MatId,
                              CompName = am.Name.ToUpper(),
                              TankId = abs.TankId,
                              TankName = at.Name,
                              RundnId = at.RundnId,
                              AvailVolId = at.AvailVolId,
                              MinVolTid = at.MinVolTid,
                              DcsServiceTid = at.DcsServiceTid,
                              AbcServiceFlag = at.AbcServiceFlag.ToUpper(),
                              LineupId = abs.LineupId,
                              CurRecipe = abc.CurRecipe,
                              MaxVolTid = at.MaxVolTid,
                              SourceDestnType = at.SourceDestnType,
                              UsageId = abc.UsageId
                          }).OrderBy(row => row.MatId)
                          .ToListAsync<CompTanksData>();

        }
        public async Task<List<AbcBlenderSources>> GetBldrSrcPreselTID(double blenderId, double blendId, double matId, string text)
        {
            //select abc_blender_sources.preselection_tid,
            //abc_blender_sources.selection_tid
            //from abc_blender_sources,
            //     abc_blend_sources
            //where abc_blender_sources.blender_id = ?
            //  and abc_blend_sources.blend_id = ?
            //  and abc_blend_sources.tank_id = abc_blender_sources.tank_id
            //  and abc_blend_sources.mat_id =?
            //  and to_char(abc_blender_sources.tank_id) like ?

            return await (from abrs in _blendMonitorContext.AbcBlenderSources
                          from abs in _blendMonitorContext.AbcBlendSources
                          where abrs.BlenderId == blenderId
                          && abs.BlendId == blendId
                          && abs.TankId == abrs.TankId
                          && abs.MatId == matId
                          && abrs.TankId.ToString().Contains(text)
                          select new AbcBlenderSources
                          {
                              PreselectionTid = abrs.PreselectionTid,
                              SelectionTid = abrs.SelectionTid,
                          }).ToListAsync<AbcBlenderSources>();
        }
        public async Task<double?> GetBldrSrcSlctTid(double blenderId, double tankId)
        {
            //select selection_tid into selection_tid from abc_blender_sources where blender_id = blender_id1
            //and tank_id = tank_id1;

            return await _blendMonitorContext.AbcBlenderSources
                    .Where(row => row.BlenderId == blenderId && row.TankId == tankId)
                    .Select(row => row.SelectionTid)
                    .FirstOrDefaultAsync<double?>();
        }
        public async Task<int> SetStationinuseFlg(string text, double lngCompLineupID)
        {
            //update abc_stations set in_use_flag = ? where id in (select station_id from abc_comp_lineup_eqp where line_id = ?)
            List<double?> stationIds = await _blendMonitorContext.AbcCompLineupEqp
                                        .Where<AbcCompLineupEqp>(row => row.LineId == lngCompLineupID)
                                        .Select(row => row.StationId)
                                        .ToListAsync<double?>();
            List<AbcStations> Data = await _blendMonitorContext.AbcStations
                        .Where<AbcStations>(row => stationIds.Contains(row.Id))
                        .ToListAsync<AbcStations>();
            foreach (AbcStations item in Data)
            {
                item.InUseFlag = text;
            }

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetIntvRcpSp(double? curRecipe, double blendId, double matId, int value)
        {
            //update abc_blend_interval_comps set sp_recipe = ? where blend_id = ? and mat_id = ? and sequence = ?

            AbcBlendIntervalComps Data = await _blendMonitorContext.AbcBlendIntervalComps
                                            .Where<AbcBlendIntervalComps>(row => row.BlendId == blendId && row.MatId == matId && row.Sequence == value)
                                            .FirstOrDefaultAsync<AbcBlendIntervalComps>();
            Data.SpRecipe = curRecipe;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetAbcBlendCompData(double blendId)
        {
            //"UPDATE ABC_BLEND_COMPS BC1 SET BC1.CUR_RECIPE = " & _
            //"(SELECT BC2.PLAN_RECIPE FROM ABC_BLEND_COMPS BC2 WHERE " & _
            //"BC2.BLEND_ID = " & curblend.lngID & " AND BC2.MAT_ID=BC1.MAT_ID) " & _
            //"WHERE BC1.BLEND_ID = " & curblend.lngID

            double? recipie = await (from BC1 in _blendMonitorContext.AbcBlendComps
                                     from BC2 in _blendMonitorContext.AbcBlendComps
                                     where BC2.BlendId == blendId && BC2.MatId == BC1.MatId
                                     select BC2.PlanRecipe)
                                        .FirstOrDefaultAsync<double?>();
            AbcBlendComps Data = await _blendMonitorContext.AbcBlendComps
                                    .Where<AbcBlendComps>(row => row.BlendId == blendId)
                                    .FirstOrDefaultAsync<AbcBlendComps>();
            Data.CurRecipe = recipie;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<string>> GetTankStName(string text)
        {
            //"select upper(name) as st_name from abc_tank_states where upper(alias) =?
            return await _blendMonitorContext.AbcTankStates
                        .Where<AbcTankStates>(row => row.Alias == text)
                        .Select(row => row.Name.ToUpper())
                        .ToListAsync<string>();
        }
        public async Task<AbcStations> GetStationInuseFlgs(double? lineupId)
        {
            //select upper(name) name, upper(in_use_flag) in_use_flag 
            //from abc_stations 
            //where id in (select station_id from abc_comp_lineup_eqp where line_id = ?)

            List<double?> stationids = await _blendMonitorContext.AbcCompLineupEqp
                                           .Where<AbcCompLineupEqp>(row => row.LineId == lineupId)
                                           .Select(row => row.StationId)
                                           .ToListAsync<double?>();

            return await _blendMonitorContext.AbcStations
                        .Where<AbcStations>(row => stationids.Contains(row.Id))
                        .Select(row => new AbcStations
                        {
                            Name = row.Name.ToUpper(),
                            InUseFlag = row.InUseFlag.ToUpper()
                        }).FirstOrDefaultAsync<AbcStations>();
        }
     
        public async Task<List<DestProps>> GetDestProps(double blendId, double tankId)
        {
            //SELECT abc_tank_props.prop_id, abc_tank_props.value, UPPER(abc_blend_props.controlled) controlled, 
            //abc_properties.abs_min, abc_properties.abs_max, UPPER(abc_properties.alias) alias 

            //FROM abc_tank_props, abc_blend_props, abc_properties 

            //WHERE abc_blend_props.blend_id = ? AND abc_tank_props.tank_id = ? and
            //upper(abc_tank_props.good_flag) = 'YES' and upper(abc_tank_props.selected_flag) = 'YES'
            //AND abc_tank_props.prop_id = abc_blend_props.prop_id AND abc_blend_props.prop_id = abc_properties.id

            return await (from atp in _blendMonitorContext.AbcTankProps
                          from abp in _blendMonitorContext.AbcBlendProps
                          from ap in _blendMonitorContext.AbcProperties
                          where abp.BlendId == blendId && atp.TankId == tankId &&
                            atp.GoodFlag.ToUpper() == "YES" && atp.SelectedFlag == "YES"
                        && atp.PropId == abp.PropId && abp.PropId == ap.Id
                          select new DestProps
                          {
                              PropId = atp.PropId,
                              Value = atp.Value,
                              Controlled = abp.Controlled.ToUpper(),
                              AbsMin = ap.AbsMin,
                              AbsMax = ap.AbsMax,
                              Alias = ap.Alias.ToUpper()
                          }).ToListAsync<DestProps>();
        }
        public async Task<int> SetHeelVal(double? vntPropVal1, double? vntPropVal2, double blendId, double tankId, double vntPropID)
        {
            //update abc_blend_dest_props set heel_value = ?, current_value = ? where blend_id = ? and tank_id = ? and prop_id = ?
            AbcBlendDestProps Data = await _blendMonitorContext.AbcBlendDestProps
                                        .Where<AbcBlendDestProps>(row => row.BlendId == blendId && row.TankId == tankId && row.PropId == vntPropID)
                                        .FirstOrDefaultAsync<AbcBlendDestProps>();
            Data.HeelValue = vntPropVal1;
            Data.CurrentValue = vntPropVal2;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<AbcPrograms>> GetPrgCycleTimes()
        {
            //select upper(name) name, cycle_time 
            //from abc_programs 
            //where upper(name) in ('ABC BLEND MONITOR', 'ABC OPTIMIZE MONITOR', 'ABC TANK MONITOR') order by upper(name)

            List<string> prgms = new List<string>() { "ABC BLEND MONITOR", "ABC OPTIMIZE MONITOR", "ABC TANK MONITOR" };
            return await _blendMonitorContext.AbcPrograms
                        .Where<AbcPrograms>(row => prgms.Contains(row.Name.ToUpper()))
                        .Select(row => new AbcPrograms
                        {
                            Name = row.Name.ToUpper(),
                            CycleTime = row.CycleTime
                        }).ToListAsync<AbcPrograms>();
        }
        public async Task<double?> GetDestHeelProp(double lngBlendId, int intDestTankID, int intPropID)
        {
            //select nvl(heel_value,0) into heel_prop 
            //from abc_blend_dest_props 
            //where blend_id = blend_id1 and tank_id = dest_tank_id and prop_id = prop_id1;
            double? destHellvlm = 0;
            try
            {
                destHellvlm = (await _blendMonitorContext.AbcBlendDestProps
                                .Where<AbcBlendDestProps>(row => row.BlendId == lngBlendId && row.TankId == intDestTankID
                                && row.PropId == intPropID)
                                .FirstAsync<AbcBlendDestProps>()).HeelValue;
                if (destHellvlm == null)
                {
                    destHellvlm = 0;
                }
                return destHellvlm;
            }
            catch (Exception)
            {
                return destHellvlm;
            }

        }
        public async Task<int> GetCalcID(string text)
        {
            AbcCalcRoutines obj = await _blendMonitorContext.AbcCalcRoutines
                         .Where<AbcCalcRoutines>(row => row.Name.ToUpper() == text.ToUpper())
                         .FirstOrDefaultAsync();
            return Convert.ToInt32(obj.Id);
        }
        public async Task<List<DestHeelVals>> GetAllDestHeelValsModified(double blendId, int tankId)
        {
            //select bdp.prop_id, p.name, nvl(bdp.heel_value, 0) as value, bp.calc_id, u.units_name

            // from abc_blend_dest_props bdp, abc_properties p, abc_blend_props bp, abc_uom u

            // where bdp.prop_id = bp.prop_id and
            //bdp.blend_id = bp.blend_id and
            //bdp.prop_id = p.id and
            //p.uom_id = u.id and
            //bdp.blend_id = ? and
            //bdp.tank_id = ?
            return await (from bdp in _blendMonitorContext.AbcBlendDestProps
                          from p in _blendMonitorContext.AbcProperties
                          from bp in _blendMonitorContext.AbcBlendProps
                          from u in _blendMonitorContext.AbcUom
                          where bdp.PropId == bp.PropId &&
                        bdp.BlendId == bp.BlendId &&
                        bdp.PropId == p.Id &&
                        p.UomId == u.Id &&
                        bdp.BlendId == blendId &&
                        bdp.TankId == tankId && (p.Name.Substring((p.Name.Length - 5), 5) == "_ETOH")
                          select new DestHeelVals
                          {
                              PropId = bdp.PropId,
                              Name = p.Name,
                              Value = (bdp.HeelValue == null) ? 0 : Convert.ToDouble(bdp.HeelValue),
                              CalcId = bp.CalcId,
                              UnitsName = u.UnitsName
                          }).ToListAsync<DestHeelVals>();

        }
        public async Task<List<DestHeelVals>> GetAllDestHeelValsModified2(double blendId, int tankId)
        {
            //select bdp.prop_id, p.name, nvl(bdp.heel_value, 0) as value, bp.calc_id, u.units_name

            // from abc_blend_dest_props bdp, abc_properties p, abc_blend_props bp, abc_uom u

            // where bdp.prop_id = bp.prop_id and
            //bdp.blend_id = bp.blend_id and
            //bdp.prop_id = p.id and
            //p.uom_id = u.id and
            //bdp.blend_id = ? and
            //bdp.tank_id = ?
            return await (from bdp in _blendMonitorContext.AbcBlendDestProps
                          from p in _blendMonitorContext.AbcProperties
                          from bp in _blendMonitorContext.AbcBlendProps
                          from u in _blendMonitorContext.AbcUom
                          where bdp.PropId == bp.PropId &&
                        bdp.BlendId == bp.BlendId &&
                        bdp.PropId == p.Id &&
                        p.UomId == u.Id &&
                        bdp.BlendId == blendId &&
                        bdp.TankId == tankId && (p.Name.Substring((p.Name.Length - 5), 5) != "_ETOH")
                          select new DestHeelVals
                          {
                              PropId = bdp.PropId,
                              Name = p.Name,
                              Value = (bdp.HeelValue == null) ? 0 : Convert.ToDouble(bdp.HeelValue),
                              CalcId = bp.CalcId,
                              UnitsName = u.UnitsName
                          }).ToListAsync<DestHeelVals>();

        }
        public async Task<int> SetAbcBlendDestPropData(double heelValue, double currentValue, double blendId, int tankId, string propName)
        {
            //"UPDATE ABC_BLEND_DEST_PROPS " & _
            //"SET HEEL_VALUE=" & dblCalcdValue & ",CURRENT_VALUE=" & dblCalcdValue & " " & _
            //"WHERE BLEND_ID=" & lngBlendId & " AND TANK_ID=" & intTankID & " AND " & _
            //"PROP_ID=(SELECT ID FROM ABC_PROPERTIES WHERE NAME = '" & strXXXPropName & "')"            

            double propId = await _blendMonitorContext.AbcProperties
                                .Where<AbcProperties>(row => row.Name == propName)
                                .Select(row => row.Id)
                                .FirstOrDefaultAsync<double>();
            AbcBlendDestProps Data = await _blendMonitorContext.AbcBlendDestProps
                                        .Where<AbcBlendDestProps>(row => row.BlendId == blendId && row.TankId == tankId && row.PropId == propId)
                                        .FirstOrDefaultAsync<AbcBlendDestProps>();
            Data.HeelValue = heelValue;
            Data.CurrentValue = currentValue;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<double> GetPrevBldToTk(int tankID)
        {
            //select order_id
            //from abc_tanks
            //where order_source = 'BLEND' and
            //id = ?
            double? data = 0;
            try
            {
                data = await _blendMonitorContext.AbcTanks
                            .Where<AbcTanks>(row => row.OrderSource == "BLEND" && row.Id == tankID)
                            .Select(row => (row.OrderId == null) ? 0 : row.OrderId)
                            .FirstOrDefaultAsync<double?>();
                return (double)data;
            }
            catch (Exception ex)
            {
                return (double)data;
            }
        }
        public async Task<double> GetBldPropCurVal(double bldId, int tankID, int etohEtohPropId)
        {
            //select nvl(current_value,0) as current_value
            //from abc_blend_dest_props
            //where blend_id = ? and tank_id = ? and prop_id = ?

            return await _blendMonitorContext.AbcBlendDestProps
                        .Where<AbcBlendDestProps>(row => row.BlendId == bldId && row.TankId == tankID && row.PropId == etohEtohPropId)
                        .Select(row => (row.CurrentValue == null) ? 0 : Convert.ToDouble(row.CurrentValue))
                        .FirstOrDefaultAsync<double>();
        }
        public async Task<double> GetETOHLabLimit()
        {
            //select nvl(etoh_props_lab_limit,0) as etoh_props_lab_limit
            //from abc_proj_defaults

            return await _blendMonitorContext.AbcProjDefaults
                            .Select(row => (row.EtohPropsLabLimit == null) ? 0 : Convert.ToDouble(row.EtohPropsLabLimit))
                            .FirstOrDefaultAsync<double>();
        }
        public async Task<DateTime?> GetBlendEndTime(double blendId)
        {
            //select actual_end
            //from abc_blends
            //where id = ?

            return await _blendMonitorContext.AbcBlends
                        .Where<AbcBlends>(row => row.Id == blendId)
                        .Select(row => row.ActualEnd)
                        .FirstOrDefaultAsync<DateTime?>();
        }
        public async Task<double> GetSourceId(string sourceName)
        {
            var sourceIdResult = await _blendMonitorContext.AbcPropSources.Where(prop => prop.Name.Equals(sourceName)).FirstOrDefaultAsync();

            return sourceIdResult.Id;
        }
        public async Task<List<NonLinTkPropValsModified>> GetNonLinTkPropValsModified(double bldId, int tankID, int calcSrceId)
        {
            //select p.id,  p.name, p.alias, t.value, t.value_time, t.good_flag, t.selected_flag, c.name as calc_rtn

            //from abc_tank_props t, abc_properties p, abc_blend_props bp,  abc_calc_routines c

            //where t.prop_id = p.id and p.id = bp.prop_id and
            //bp.calc_id = c.id and
            //bp.blend_id = ? and
            //t.tank_id = ? and
            //t.source_id = ? and
            //t.good_flag = 'YES' and
            //(c.name not like 'LINEAR%' or p.name like 'E_V%')

            return await (from t in _blendMonitorContext.AbcTankProps
                          from p in _blendMonitorContext.AbcProperties
                          from bp in _blendMonitorContext.AbcBlendProps
                          from c in _blendMonitorContext.AbcCalcRoutines
                          where t.PropId == p.Id && p.Id == bp.PropId &&
                            bp.CalcId == c.Id && bp.BlendId == bldId &&
                            t.TankId == tankID && t.SourceId == calcSrceId &&
                            t.GoodFlag == "YES" && (c.Name.Substring(0, 6) != "LINEAR" || p.Name.Substring(0, 2) == "EV")
                            && (p.Name.Substring((p.Name.Length - 4), 4) != "ETOH")
                          select new NonLinTkPropValsModified
                          {
                              Id = p.Id,
                              Name = p.Name,
                              Alias = p.Alias,
                              Value = t.Value,
                              ValueTime = t.ValueTime,
                              GoodFlag = t.GoodFlag,
                              SelectedFlag = t.SelectedFlag,
                              Calcrtn = c.Name
                          }).ToListAsync<NonLinTkPropValsModified>();
        }
        public async Task<List<NonLinTkPropValsModified>> GetNonLinTkPropValsModified2(double bldId, int tankID, int calcSrceId)
        {
            //select p.id,  p.name, p.alias, t.value, t.value_time, t.good_flag, t.selected_flag, c.name as calc_rtn

            //from abc_tank_props t, abc_properties p, abc_blend_props bp,  abc_calc_routines c

            //where t.prop_id = p.id and p.id = bp.prop_id and
            //bp.calc_id = c.id and
            //bp.blend_id = ? and
            //t.tank_id = ? and
            //t.source_id = ? and
            //t.good_flag = 'YES' and
            //(c.name not like 'LINEAR%' or p.name like 'E_V%')

            return await (from t in _blendMonitorContext.AbcTankProps
                          from p in _blendMonitorContext.AbcProperties
                          from bp in _blendMonitorContext.AbcBlendProps
                          from c in _blendMonitorContext.AbcCalcRoutines
                          where t.PropId == p.Id && p.Id == bp.PropId &&
                            bp.CalcId == c.Id && bp.BlendId == bldId &&
                            t.TankId == tankID && t.SourceId == calcSrceId &&
                            t.GoodFlag == "YES" && (c.Name.Substring(0, 6) != "LINEAR" || p.Name.Substring(0, 2) == "EV")
                            && (p.Name.Substring((p.Name.Length - 5), 5) == "_ETOH")
                          select new NonLinTkPropValsModified
                          {
                              Id = p.Id,
                              Name = p.Name,
                              Alias = p.Alias,
                              Value = t.Value,
                              ValueTime = t.ValueTime,
                              GoodFlag = t.GoodFlag,
                              SelectedFlag = t.SelectedFlag,
                              Calcrtn = c.Name
                          }).ToListAsync<NonLinTkPropValsModified>();
        }
        public async Task<List<AbcAnzHdrProps>> GetModelErrThrshVals(string bldrName)
        {
            //select prop_id, model_err_thrsh
            //from abc_anz_hdr_props
            //where blender_id = (select id from abc_blenders where name = ?) 
            //order by prop_id, model_err_thrsh desc

            double blenderId = await _blendMonitorContext.AbcBlenders
                                    .Where<AbcBlenders>(row => row.Name == bldrName)
                                    .Select(row => row.Id)
                                    .FirstOrDefaultAsync<double>();
            return await _blendMonitorContext.AbcAnzHdrProps
                            .Where<AbcAnzHdrProps>(row => row.BlenderId == blenderId)
                            .OrderBy(row => row.PropId).ThenByDescending(row => row.ModelErrThrsh)
                            .Select(row => new AbcAnzHdrProps
                            {
                                PropId = row.PropId,
                                ModelErrThrsh = row.ModelErrThrsh
                            }).ToListAsync<AbcAnzHdrProps>();
        }
        public async Task<List<AbcPrdgrpProps>> GetMinMaxBiasVals(int prdgrpID)
        {
            //select prop_id, min_bias, max_bias
            //from abc_prdgrp_props
            //where prdgrp_id = ?
            //order by prop_id

            return await _blendMonitorContext.AbcPrdgrpProps
                        .Where<AbcPrdgrpProps>(row => row.PrdgrpId == prdgrpID)
                        .OrderBy(row => row.PropId)
                        .Select(row => new AbcPrdgrpProps
                        {
                            PropId = row.PropId,
                            MinBias = row.MinBias,
                            MaxBias = row.MaxBias
                        }).ToListAsync<AbcPrdgrpProps>();
        }       
        public async Task<List<double>> GetCalcCoeffs(int prdgrpID, string text1, string text2)
        {
            //select coef_order, coef
            //from abc_calc_coefficients
            //where prdgrp_id = ? and
            //prop_id = (select id from abc_properties where name = ? ) and
            //calc_id = (select id from abc_calc_routines where name = ? )

            double propId = await _blendMonitorContext.AbcProperties
                                .Where<AbcProperties>(row => row.Name == text1)
                                .Select(row => row.Id)
                                .FirstOrDefaultAsync<double>();
            double calcId = await _blendMonitorContext.AbcCalcRoutines
                                .Where<AbcCalcRoutines>(row => row.Name == text2)
                                .Select(row => row.Id)
                                .FirstOrDefaultAsync<double>();
            return await _blendMonitorContext.AbcCalcCoefficients
                        .Where<AbcCalcCoefficients>(row => row.PrdgrpId == prdgrpID && row.PropId == propId && row.CalcId == calcId)
                        .OrderBy(row => row.CoefOrder)
                        .Select(row => Convert.ToDouble(row.Coef))
                        .ToListAsync<double>();
        }
        public async Task<List<AbcPumps>> GetPumpsData(double lineId)
        {
            //select upper(name) name, inuse_tag_id, status_tag_id, upper(in_ser_flag) in_ser_flag, mode_tid, dcs_pump_id 
            //from abc_pumps 
            //where id in (select pump_id from abc_comp_lineup_eqp where line_id = ?)
            List<double> pumpIds = await _blendMonitorContext.AbcCompLineupEqp
                                .Where<AbcCompLineupEqp>(row => row.LineId == lineId)
                                .Select(row => Convert.ToDouble(row.PumpId))
                                .ToListAsync<double>();

            return await _blendMonitorContext.AbcPumps
                            .Where<AbcPumps>(row => pumpIds.Contains(row.Id))
                            .ToListAsync<AbcPumps>();
        }
        public async Task<List<CompSrceData>> GetCompSrceData(double blendId)
        {
            //select abc_blend_sources.mat_id,
            // upper(abc_materials.name) comp_name,
            // abc_blend_sources.tank_id,
            // upper(abc_tanks.name) tank_name,
            // abc_blend_sources.lineup_id

            //from abc_blend_sources,
            // abc_materials,
            // abc_tanks

            //where abc_blend_sources.blend_id = ?
            // and abc_blend_sources.tank_id = abc_tanks.id
            // and abc_blend_sources.mat_id = abc_materials.id
            // and abc_blend_sources.in_use_flag = 'YES'
            //order by abc_blend_sources.mat_id

            return await (from abs in _blendMonitorContext.AbcBlendSources
                          from am in _blendMonitorContext.AbcMaterials
                          from at in _blendMonitorContext.AbcTanks
                          where abs.BlendId == blendId
                            && abs.TankId == at.Id
                            && abs.MatId == am.Id
                            && abs.InUseFlag == "YES"
                          select new CompSrceData
                          {
                              MatId = abs.MatId,
                              CompName = am.Name.ToUpper(),
                              TankId = abs.TankId,
                              TankName = at.Name.ToUpper(),
                              LineupId = abs.LineupId
                          }).ToListAsync<CompSrceData>();
        }
        public async Task<List<AbcStations>> GetAllBldrStationsData(double blenderId)
        {
            //Select id as station_id,blender_id,name as station_name, in_use_flag,select_station_tid,tank_select_num_tid, 
            //tank_preselect_num_tid, mat_num_tid, dcs_station_num, total_station_vol_tid,min, max, RCP_SP_TAG_ID,
            //lineup_sel_tid, pumpa_sel_tid, pumpb_sel_tid, pumpc_sel_tid,
            //pumpd_sel_tid
            //from abc_stations where blender_id =?

            return await _blendMonitorContext.AbcStations
                        .Where<AbcStations>(row => row.BlenderId == blenderId)
                        .ToListAsync<AbcStations>();
        }
        public async Task<List<BlendSourcesTankData>> GetBlendSourcesTankData(double blendId, int matId)
        {
            //"SELECT SWG.TO_TK_ID, BS.LINEUP_ID FROM ABC_BLEND_SOURCES BS, ABC_BLEND_SWINGS SWG " & +
            //" WHERE SWG.BLEND_ID=BS.BLEND_ID AND SWG.BLEND_ID=" & curblend.lngID & " AND SWG.FROM_TK_ID=" +
            //"(SELECT TANK_ID FROM " & +
            //" ABC_BLEND_SOURCES WHERE BLEND_ID=" & curblend.lngID & " AND MAT_ID=" & intMatID & " AND IN_USE_FLAG='YES') AND " & +
            //" BS.MAT_ID=" & intMatID & " AND SWG.SWING_TYPE='COMPONENT' AND SWG.SWING_STATE='READY' AND " & +
            //" BS.IN_USE_FLAG<>'YES'"

            double tankId = await _blendMonitorContext.AbcBlendSources
                                .Where<AbcBlendSources>(row => row.BlendId == blendId && row.MatId == matId && row.InUseFlag == "YES")
                                .Select(row => row.TankId)
                                .FirstOrDefaultAsync<double>();
            return await (from BS in _blendMonitorContext.AbcBlendSources
                          from SWG in _blendMonitorContext.AbcBlendSwings
                          where SWG.BlendId == BS.BlendId && SWG.BlendId == blendId && SWG.FromTkId == tankId
                          && BS.MatId == matId && SWG.SwingType == "COMPONENT" && SWG.SwingState == "READY" && BS.InUseFlag != "YES"
                          select new BlendSourcesTankData
                          {
                              ToTkId = SWG.ToTkId,
                              LineupId = BS.LineupId
                          }).ToListAsync<BlendSourcesTankData>();

        }
        public async Task<AbcMaterials> GetMatName(int matID)
        {
            //SELECT NAME,DCS_MAT_NUM FROM ABC_MATERIALS WHERE ID=?
            return await _blendMonitorContext.AbcMaterials
                        .Where<AbcMaterials>(row => row.Id == matID)
                        .Select(row => new AbcMaterials
                        {
                            Name = row.Name,
                            DcsMatNum = row.DcsMatNum
                        }).FirstOrDefaultAsync<AbcMaterials>();
        }
        public async Task<List<LineGeoId>> GetLineGeoIdProduct(double lineupID)
        {
            //"select pl.line_geo_id,geo.num_of_pumps, " & _
            //"geo.num_of_stations from abc_prod_lineups pl, abc_lineup_geo geo " & _
            //"where pl.line_geo_id=geo.id(+) and pl.id=" & lngLineupID
            return await (from pl in _blendMonitorContext.AbcProdLineups
                          from geo in _blendMonitorContext.AbcLineupGeo
                          where pl.LineGeoId == geo.Id && pl.Id == lineupID
                          select new LineGeoId
                          {
                              LineGeoID = pl.LineGeoId,
                              NumOfPumps = geo.NumOfPumps
                          }).ToListAsync<LineGeoId>();
        }
        public async Task<List<LineGeoId>> GetLineGeoId(double lineupID)
        {
            //"select cl.line_geo_id, geo.num_of_pumps, " & _
            //"geo.num_of_stations from abc_comp_lineups cl, abc_lineup_geo geo " & _
            //"where cl.line_geo_id=geo.id(+) and cl.id=" & lngLineupID
            return await (from cl in _blendMonitorContext.AbcCompLineups
                          from geo in _blendMonitorContext.AbcLineupGeo
                          where cl.LineGeoId == geo.Id && cl.Id == lineupID
                          select new LineGeoId
                          {
                              LineGeoID = cl.LineGeoId,
                              NumOfPumps = geo.NumOfPumps
                          }).ToListAsync<LineGeoId>();
        }
        public async Task<List<double?>> GetPumpIdProd(double lineupID)
        {
            //"select pump_id from abc_prod_lineup_eqp " & _
            //"where line_id =" & lngLineupID & " And pump_id IS NOT NULL"
            return await _blendMonitorContext.AbcProdLineupEqp
                            .Where<AbcProdLineupEqp>(row => row.LineId == lineupID && row.PumpId != null)
                            .Select(row => row.PumpId)
                            .ToListAsync<double?>();

        }
        public async Task<List<double?>> GetPumpIdComp(double lineupID)
        {
            //"select pump_id from abc_comp_lineup_eqp " & _
            //"where line_id =" & lngLineupID & " And pump_id IS NOT NULL"
            return await _blendMonitorContext.AbcCompLineupEqp
                            .Where<AbcCompLineupEqp>(row => row.LineId == lineupID && row.PumpId != null)
                            .Select(row => row.PumpId)
                            .ToListAsync<double?>();

        }
        public async Task<List<double?>> GetPumpIdProd(double lineupID, int lineEqpOrder)
        {
            //"select pump_id from abc_prod_lineup_eqp " & _
            //"where line_id =" & lngLineupID & " and line_eqp_order=" & intLineEqpOrder
            return await _blendMonitorContext.AbcProdLineupEqp
                            .Where<AbcProdLineupEqp>(row => row.LineId == lineupID && row.LineEqpOrder == lineEqpOrder)
                            .Select(row => row.PumpId)
                            .ToListAsync<double?>();

        }
        public async Task<List<double?>> GetPumpIdComp(double lineupID, int lineEqpOrder)
        {
            //"select pump_id from abc_prod_lineup_eqp " & _
            //"where line_id =" & lngLineupID & " and line_eqp_order=" & intLineEqpOrder
            return await _blendMonitorContext.AbcCompLineupEqp
                            .Where<AbcCompLineupEqp>(row => row.LineId == lineupID && row.LineEqpOrder == lineEqpOrder)
                            .Select(row => row.PumpId)
                            .ToListAsync<double?>();

        }
        public async Task<List<double?>> GetPumpIdProd2(double lineupID, int lineEqpOrder)
        {
            //"select pump_id from abc_prod_lineup_eqp " & _
            //"where line_id =" & lngLineupID & " and line_eqp_order" & strOrder & " And pump_id IS NOT NULL"
            return await _blendMonitorContext.AbcProdLineupEqp
                            .Where<AbcProdLineupEqp>(row => row.LineId == lineupID && row.LineEqpOrder == lineEqpOrder && row.PumpId != null)
                            .Select(row => row.PumpId)
                            .ToListAsync<double?>();

        }
        public async Task<List<double?>> GetPumpIdComp2(double lineupID, int lineEqpOrder)
        {
            //"select pump_id from abc_prod_lineup_eqp " & _
            //"where line_id =" & lngLineupID & " and line_eqp_order=" & intLineEqpOrder
            return await _blendMonitorContext.AbcCompLineupEqp
                            .Where<AbcCompLineupEqp>(row => row.LineId == lineupID && row.LineEqpOrder == lineEqpOrder && row.PumpId != null)
                            .Select(row => row.PumpId)
                            .ToListAsync<double?>();

        }
        public async Task<List<double?>> GetPumpIdProd3(double lineupID, int lineEqpOrder)
        {
            //"select pump_id from abc_prod_lineup_eqp " & _
            //"where line_id =" & lngLineupID & " and line_eqp_order" & strOrder & " And pump_id IS NOT NULL"
            return await _blendMonitorContext.AbcProdLineupEqp
                            .Where<AbcProdLineupEqp>(row => row.LineId == lineupID && row.LineEqpOrder >= lineEqpOrder && row.PumpId != null)
                            .Select(row => row.PumpId)
                            .ToListAsync<double?>();

        }
        public async Task<List<double?>> GetPumpIdComp3(double lineupID, int lineEqpOrder)
        {
            //"select pump_id from abc_prod_lineup_eqp " & _
            //"where line_id =" & lngLineupID & " and line_eqp_order=" & intLineEqpOrder
            return await _blendMonitorContext.AbcCompLineupEqp
                            .Where<AbcCompLineupEqp>(row => row.LineId == lineupID && row.LineEqpOrder >= lineEqpOrder && row.PumpId != null)
                            .Select(row => row.PumpId)
                            .ToListAsync<double?>();

        }

        public async Task<List<double?>> GetPumpIdProd4(double lineupID, int lineEqpOrder)
        {
            //"select pump_id from abc_prod_lineup_eqp " & _
            //"where line_id =" & lngLineupID & " and line_eqp_order" & strOrder & " And pump_id IS NOT NULL"
            return await _blendMonitorContext.AbcProdLineupEqp
                            .Where<AbcProdLineupEqp>(row => row.LineId == lineupID && row.LineEqpOrder <= lineEqpOrder)
                            .Select(row => row.PumpId)
                            .ToListAsync<double?>();

        }
        public async Task<List<double?>> GetPumpIdComp4(double lineupID, int lineEqpOrder)
        {
            //"select pump_id from abc_prod_lineup_eqp " & _
            //"where line_id =" & lngLineupID & " and line_eqp_order=" & intLineEqpOrder
            return await _blendMonitorContext.AbcCompLineupEqp
                            .Where<AbcCompLineupEqp>(row => row.LineId == lineupID && row.LineEqpOrder <= lineEqpOrder)
                            .Select(row => row.PumpId)
                            .ToListAsync<double?>();

        }

        public async Task<List<double?>> GetPumpIdProd5(double lineupID, int lineEqpOrder)
        {
            //"select pump_id from abc_prod_lineup_eqp " & _
            //"where line_id =" & lngLineupID & " and line_eqp_order" & strOrder & " And pump_id IS NOT NULL"
            return await _blendMonitorContext.AbcProdLineupEqp
                            .Where<AbcProdLineupEqp>(row => row.LineId == lineupID && row.LineEqpOrder >= lineEqpOrder)
                            .Select(row => row.PumpId)
                            .ToListAsync<double?>();

        }
        public async Task<List<double?>> GetPumpIdComp5(double lineupID, int lineEqpOrder)
        {
            //"select pump_id from abc_prod_lineup_eqp " & _
            //"where line_id =" & lngLineupID & " and line_eqp_order=" & intLineEqpOrder
            return await _blendMonitorContext.AbcCompLineupEqp
                            .Where<AbcCompLineupEqp>(row => row.LineId == lineupID && row.LineEqpOrder >= lineEqpOrder)
                            .Select(row => row.PumpId)
                            .ToListAsync<double?>();

        }
        public async Task<AbcPumps> GetPumpCfg(double pumpXId)
        {
            //select upper(name) name, inuse_tag_id, status_tag_id, upper(in_ser_flag) in_ser_flag, mode_tid, dcs_pump_id
            //from abc_pumps where id =?
            return await _blendMonitorContext.AbcPumps
                        .Where<AbcPumps>(row => row.Id == pumpXId)
                        .FirstOrDefaultAsync<AbcPumps>();
        }
        public async Task<List<AbcPumps>> GetProdPumpsData(double prodLineupId)
        {
            //select upper(name) name, inuse_tag_id, status_tag_id, upper(in_ser_flag) in_ser_flag, mode_tid, dcs_pump_id
            //from abc_pumps
            //where id in (select pump_id from abc_prod_lineup_eqp where line_id = ?)
            List<double> pumpIds = await _blendMonitorContext.AbcProdLineupEqp
                                        .Where<AbcProdLineupEqp>(row => row.LineId == prodLineupId)
                                        .Select(row => Convert.ToDouble(row.PumpId))
                                        .ToListAsync<double>();

            return await _blendMonitorContext.AbcPumps
                    .Where<AbcPumps>(row => pumpIds.Contains(row.Id))
                    .ToListAsync<AbcPumps>();
        }
        public async Task<List<AbcBlenderDest>> GetBldrDestPreselTID(double blenderId, double blendId, string text)
        {
            //select abc_blender_dest.preselection_tid,
            //abc_blender_dest.selection_tid
            //from abc_blender_dest,
            //     abc_blend_dest
            //where abc_blender_dest.blender_id = ?
            //  and abc_blend_dest.blend_id = ?
            //  and abc_blend_dest.tank_id = abc_blender_dest.tank_id
            //  and to_char(abc_blender_dest.tank_id) like ?
            return await (from abrd in _blendMonitorContext.AbcBlenderDest
                          from abd in _blendMonitorContext.AbcBlendDest
                          where abrd.BlenderId == blenderId && abd.BlendId == blendId
                          && abd.TankId == abrd.TankId && abrd.TankId.ToString().Contains(text)
                          select new AbcBlenderDest
                          {
                              PreselectionTid = abrd.PreselectionTid,
                              SelectionTid = abrd.SelectionTid
                          }).ToListAsync<AbcBlenderDest>();
        }
        public async Task<int> SetBlendDestSequenceTime(DateTime startTime, double blendId, double tankId, int sequence)
        {
            //"UPDATE abc_blend_dest_seq SET time_in=" & 
            //"TO_DATE('" & Format(curblend.dteActualStart, strWinDateFmt & " " & WIN_TIME_FMT) & "','" & strOraDateFmt & " " & ORA_TIME_FMT & "') 
            //WHERE blend_id=" & curblend.lngID & _
            //" AND tank_id=" & lngDestTkId & " AND swing_sequence=1"
            AbcBlendDestSeq Data = await _blendMonitorContext.AbcBlendDestSeq
                                    .Where<AbcBlendDestSeq>(row => row.BlendId == blendId && row.TankId == tankId && row.SwingSequence == sequence)
                                    .FirstOrDefaultAsync<AbcBlendDestSeq>();
            Data.TimeIn = startTime;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<AbcBlenders> GetBldrSwingOccurID(double blenderId)
        {
            //select abc_blenders.swing_occurred_tid,
            //abc_blenders.swing_tid
            //from abc_blenders
            //where abc_blenders.id = ?
            return await _blendMonitorContext.AbcBlenders
                        .Where<AbcBlenders>(row => row.Id == blenderId)
                        .Select(row => new AbcBlenders
                        {
                            SwingOccurredTid = row.SwingOccurredTid,
                            SwingTid = row.SwingTid
                        }).FirstOrDefaultAsync<AbcBlenders>();
        }
        public async Task<List<AbcTags>> GetReadWriteVal(double swingTID)
        {
            // select name, read_value, write_value from abc_tags where id = ? and  upper(value_quality) = 'GOOD'
            return await _blendMonitorContext.AbcTags
                        .Where<AbcTags>(row => row.Id == swingTID && row.ValueQuality.ToUpper() == "GOOD")
                        .Select(row => new AbcTags
                        {
                            Name = row.Name,
                            ReadValue = row.ReadValue,
                        }).ToListAsync<AbcTags>();
        }
        public async Task<int> SetBlendSwingData(string state, double blendId, double tankdId, double toTankId)
        {
            //"UPDATE abc_blend_swings SET (swing_state,done_at)=(" & _
            //"SELECT 'COMPLETE',SYSDATE FROM DUAL) WHERE blend_id=" & curblend.lngID & _
            //" AND from_tk_id=" & lngDestTkId & " AND to_tk_id=" & lngToTankID & " AND swing_state<>'COMPLETE'"

            AbcBlendSwings Data = await _blendMonitorContext.AbcBlendSwings
                                    .Where<AbcBlendSwings>(row => row.BlendId == blendId && row.FromTkId == tankdId
                                    && row.ToTkId == toTankId && row.SwingState != "COMPLETE")
                                    .FirstOrDefaultAsync<AbcBlendSwings>();
            Data.SwingState = state;
            Data.DoneAt = DateTime.Now;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetBlendSwingData2(string state, double blendId, double tankdId, double toTankId)
        {

            //"UPDATE abc_blend_swings SET (swing_state,done_at)=(" & _
            //"SELECT 'ACTIVE',SYSDATE FROM DUAL) WHERE blend_id=" & curblend.lngID & _
            //" AND from_tk_id=" & lngDestTkId & " AND to_tk_id=" & lngToTankID & " AND swing_state='READY'"

            AbcBlendSwings Data = await _blendMonitorContext.AbcBlendSwings
                                    .Where<AbcBlendSwings>(row => row.BlendId == blendId && row.FromTkId == tankdId
                                    && row.ToTkId == toTankId && row.SwingState == "READY")
                                    .FirstOrDefaultAsync<AbcBlendSwings>();
            Data.SwingState = state;
            Data.DoneAt = DateTime.Now;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<double>> GetBldDestSwgSeq(double blendId)
        {
            //select swing_sequence from abc_blend_dest_seq where blend_id=? order by swing_sequence desc
            return await _blendMonitorContext.AbcBlendDestSeq
                    .Where<AbcBlendDestSeq>(row => row.BlendId == blendId)
                    .OrderByDescending(row => row.SwingSequence)
                    .Select(row => row.SwingSequence)
                    .ToListAsync<double>();
        }
        public async Task<double?> GetBldDestSumVolAdded(double blendId)
        {
            //select sum(vol_added) as Sum_VolAdded from abc_blend_dest_seq where blend_id=?
            return await _blendMonitorContext.AbcBlendDestSeq
                        .Where<AbcBlendDestSeq>(row => row.BlendId == blendId)
                        .SumAsync(row => row.VolAdded);
        }
        public async Task<int> SetBlendDestSeqData(double dblSeqVolAdded, double blendId, double tankId, int sequence)
        {
            //"UPDATE abc_blend_dest_seq SET (vol_added,time_out)=(" & _
            //"SELECT " & dblSeqVolAdded & ",SYSDATE FROM DUAL) WHERE blend_id=" & curblend.lngID & _
            //" AND tank_id=" & lngDestTkId & " AND swing_sequence=" & intSwingSeq
            AbcBlendDestSeq Data = await _blendMonitorContext.AbcBlendDestSeq
                                        .Where<AbcBlendDestSeq>(row => row.BlendId == blendId && row.TankId == tankId
                                        && row.SwingSequence == sequence)
                                        .FirstOrDefaultAsync<AbcBlendDestSeq>();
            Data.VolAdded = dblSeqVolAdded;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetBlendState(double blendId, string state)
        {
            //update abc_blends set blend_state = ? where id = ?
            AbcBlends Data = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Id == blendId)
                                .FirstOrDefaultAsync<AbcBlends>();
            Data.BlendState = state;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetBlendPendingState(double blendId, string state)
        {
            //"UPDATE ABC_BLENDS SET PENDING_STATE=\'DOWNLOADING\' WHERE " + ("ID = " + lngRundnBldID)
            AbcBlends Data = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Id == blendId)
                                .FirstOrDefaultAsync<AbcBlends>();
            Data.PendingState = state;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<AbcBlends>> GetReadyPrevBld(double blenderId, double blendId)
        {
            //SELECT ID, UPPER(NAME) NAME, PREVIOUS_BLEND_ID 
            //FROM ABC_BLENDS 
            //WHERE BLENDER_ID = ? AND UPPER(BLEND_STATE) = 'READY' AND PREVIOUS_BLEND_ID =?
            return await _blendMonitorContext.AbcBlends
                        .Where<AbcBlends>(row => row.BlenderId == blenderId && row.BlendState.ToUpper() == "READY" && row.PreviousBlendId == blendId)
                        .Select(row => new AbcBlends
                        {
                            Id = row.Id,
                            Name = row.Name.ToUpper(),
                            PreviousBlendId = row.PreviousBlendId
                        }).ToListAsync<AbcBlends>();
        }     
        public async Task<double> GetBlendId(string name)
        {
            //SELECT ID FROM ABC_BLENDS WHERE NAME=?
            return await _blendMonitorContext.AbcBlends
                        .Where<AbcBlends>(row => row.Name == name)
                        .Select(row => row.Id)
                        .FirstOrDefaultAsync<double>();
        }
        public async Task<List<double>> GetBlendSwingData(int tankId, string blendName)
        {
            //"SELECT BSWG.TO_TK_ID AS TANK_ID " &
            //"FROM ABC_BLEND_SWINGS BSWG " &
            //"WHERE BSWG.FROM_TK_ID = " & intDestTankID & " AND " &
            //"BSWG.BLEND_ID = (SELECT ID FROM ABC_BLENDS WHERE NAME = '" &
            //strOldBlendName & "')"
            double blendId = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Name == blendName)
                                .Select(row => row.Id)
                                .FirstOrDefaultAsync<double>();
            return await _blendMonitorContext.AbcBlendSwings
                        .Where<AbcBlendSwings>(row => row.FromTkId == tankId && row.BlendId == blendId)
                        .Select(row => row.ToTkId)
                        .ToListAsync<double>();
        }
        public async Task<int> SetBlendTargetVol(double vol, double blendId)
        {
            //"UPDATE abc_blends SET TARGET_VOL=" + dblAvailSpace);
            //" WHERE Id=" + strNewBlendId));
            AbcBlends Data = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Id == blendId)
                                .FirstOrDefaultAsync<AbcBlends>();
            Data.TargetVol = vol;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetBlendDesOnSpecVol(double vol, double blendId)
        {
            //"UPDATE abc_blends SET DES_ONSPEC_VOL=" + dblDesVol);
            //" WHERE Id=" + strNewBlendId));
            AbcBlends Data = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Id == blendId)
                                .FirstOrDefaultAsync<AbcBlends>();
            Data.DesOnspecVol = vol;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<double>> GetBlendOrderTankData(string blenderName, string matName, double tankId)
        {
            // "SELECT T.ID " +
            //  "FROM ABC_TANKS T, ABC_MATERIALS M, ABC_PRDGRP_USAGES PGU, " & _
            //  "ABC_USAGES U, ABC_BLENDERS BLDR " +

            // "WHERE T.MAT_ID=PGU.MAT_ID AND PGU.MAT_ID=M.ID AND T.PRDGRP_ID=PGU.PRDGRP_ID AND " & _
            //"PGU.PRDGRP_ID=BLDR.PRDGRP_ID AND BLDR.NAME='" & strBlenderName & "' AND " & _
            //"PGU.USAGE_ID=U.ID AND U.NAME='PRODUCT' AND M.NAME='" & strMatName & "' AND " & _
            //"T.ID <> " & strTankId & " AND T.ABC_SERVICE_FLAG = 'YES' ORDER BY T.NAME ASC"

            return await (from T in _blendMonitorContext.AbcTanks
                          from M in _blendMonitorContext.AbcMaterials
                          from PGU in _blendMonitorContext.AbcPrdgrpUsages
                          from U in _blendMonitorContext.AbcUsages
                          from BLDR in _blendMonitorContext.AbcBlenders
                          where T.MatId == PGU.MatId && PGU.MatId == M.Id && T.PrdgrpId == PGU.PrdgrpId
                          && PGU.PrdgrpId == BLDR.PrdgrpId && BLDR.Name == blenderName &&
                          PGU.UsageId == U.Id && U.Name == "PRODUCT" && M.Name == matName &&
                          T.Id != tankId && T.AbcServiceFlag == "YES"
                          select T.Id).ToListAsync<double>();
        }
        public async Task<int> InsertAbcBlendDest(int posDestTankId, double blendId, double lineUpId)
        {
            //"INSERT INTO abc_blend_dest (BLEND_ID,TANK_ID,IN_USE_FLAG,FIX_HEEL_FLAG,HEEL_VOLUME,LINEUP_ID) "
            //" (SELECT " & strNewBlendId & ", ABC_TANKS.ID, 'NO', 'YES', (T1.READ_VALUE + T2.READ_VALUE)," & _
            //strLineupID & 
            //" FROM ABC_TANKS,ABC_TAGS T1,ABC_TAGS T2 " +
            //"WHERE ABC_TANKS.ID = " & intPosDestTankId & " AND ABC_TANKS.AVAIL_VOL_ID=T1.ID(+) " & _
            //" AND ABC_TANKS.MIN_VOL_TID=T2.ID(+))"
            var Data = await (from AT in _blendMonitorContext.AbcTanks
                              from T1 in _blendMonitorContext.AbcTags
                              from T2 in _blendMonitorContext.AbcTags
                              where AT.Id == posDestTankId && AT.AvailVolId == T1.Id && AT.MinVolTid == T2.Id
                              select new Tuple<double, double?>(AT.Id, T1.ReadValue + T2.ReadValue))
                              .FirstOrDefaultAsync<Tuple<double, double?>>();
            AbcBlendDest obj = new AbcBlendDest();
            obj.BlendId = blendId;
            obj.TankId = Data.Item1;
            obj.InUseFlag = "NO";
            obj.FixHeelFlag = "YES";
            obj.HeelVolume = Data.Item2;
            obj.LineupId = lineUpId;
            var data = await _blendMonitorContext.AbcBlendDest
                        .AddAsync(obj);
            return await _blendMonitorContext.SaveChangesAsync();

        }
        public async Task<int> InsertBlendSwingData(double blendId, double tankId, double destTankId, double? swingCriteriaID)
        {
            //"INSERT INTO ABC_BLEND_SWINGS " & _
            //"(BLEND_ID,FROM_TK_ID,TO_TK_ID,DONE_AT,SWING_TYPE,CRITERIA_ID,SWING_STATE, " & _
            //"AUTO_SWING_FLAG, CRITERIA_NUM_LMT,CRITERIA_TIM_LMT) " & _
            //"VALUES (" & strNewBlendId & "," & strTankId & "," & intPosDestTankId & "," & _
            //"SYSDATE,'PRODUCT'," & strSwingCriteriaID & ",'READY','YES',NULL,NULL)"            

            AbcBlendSwings obj = new AbcBlendSwings();
            obj.BlendId = blendId;
            obj.FromTkId = tankId;
            obj.ToTkId = destTankId;
            obj.DoneAt = DateTime.Now;
            obj.SwingType = "PRODUCT";
            obj.CriteriaId = swingCriteriaID;
            obj.SwingState = "READY";
            obj.AutoSwingFlag = "YES";
            obj.CriteriaNumLmt = null;
            obj.CriteriaTimLmt = null;

            var data = await _blendMonitorContext.AbcBlendSwings
                .AddAsync(obj);
            return await _blendMonitorContext.SaveChangesAsync();

        }
        public async Task<int> SetBlendSwingData(double swingCriteriaID, double blendId, double tankId, double destTankId)
        {
            //"UPDATE ABC_BLEND_SWINGS SET CRITERIA_ID=" & _
            //strSwingCriteriaID & " WHERE BLEND_ID=" & strNewBlendId & _
            //" AND FROM_TK_ID=" & strTankId & " AND TO_TK_ID=" & intPosDestTankId
            AbcBlendSwings Data = await _blendMonitorContext.AbcBlendSwings
                                    .Where<AbcBlendSwings>(row => row.BlendId == blendId && row.FromTkId == tankId
                                    && row.ToTkId == destTankId).FirstOrDefaultAsync<AbcBlendSwings>();
            Data.CriteriaId = swingCriteriaID;
            return await _blendMonitorContext.SaveChangesAsync();
        }      
        public async Task<int> DeleteAbcBlendDestProps(double blendId, double tankId)
        {
            //"DELETE FROM abc_blend_dest_props "
            //"WHERE blend_id=" & strNewBlendId & " AND " & _
            //" tank_id = " & strOldTankId

            List<AbcBlendDestProps> Data = await _blendMonitorContext.AbcBlendDestProps
                                            .Where<AbcBlendDestProps>(row => row.BlendId == blendId && row.TankId == tankId)
                                            .ToListAsync<AbcBlendDestProps>();
            if (Data.Count() > 0)
                _blendMonitorContext.AbcBlendDestProps.RemoveRange(Data);
            return 0;
        }
        public async Task<int> DeleteAbcBlendDestSeq(double blendId, double tankId)
        {
            //"DELETE FROM abc_blend_dest_seq  "
            //" WHERE BLEND_ID = " & strNewBlendId & " AND " & _
            //" TANK_ID = " & strOldTankId

            List<AbcBlendDestSeq> Data = await _blendMonitorContext.AbcBlendDestSeq
                                            .Where<AbcBlendDestSeq>(row => row.BlendId == blendId && row.TankId == tankId)
                                            .ToListAsync<AbcBlendDestSeq>();
            if (Data.Count() > 0)
                _blendMonitorContext.AbcBlendDestSeq.RemoveRange(Data);
            return 0;
        }
        public async Task<int> DeleteAbcBlendDest(double blendId, double tankId)
        {
            //"DELETE FROM abc_blend_dest "
            //"WHERE blend_id=" & strNewBlendId & " AND " & _
            //" tank_id = " & strOldTankId

            List<AbcBlendDest> Data = await _blendMonitorContext.AbcBlendDest
                                            .Where<AbcBlendDest>(row => row.BlendId == blendId && row.TankId == tankId)
                                            .ToListAsync<AbcBlendDest>();
            if (Data.Count() > 0)
                _blendMonitorContext.AbcBlendDest.RemoveRange(Data);
            return 0;
        }
        public async Task<int> DeleteAbcBlendDSwings(double blendId, double tankId)
        {
            //"DELETE FROM ABC_BLEND_SWINGS " & _
            //" WHERE BLEND_ID = " & strNewBlendId & _
            //" AND SWING_STATE = 'READY' " & _
            //" AND (FROM_TK_ID = " & strOldTankId & _
            //" OR TO_TK_ID = " & strOldTankId & ")"

            List<AbcBlendSwings> Data = await _blendMonitorContext.AbcBlendSwings
                                            .Where<AbcBlendSwings>(row => row.BlendId == blendId && row.SwingState == "READY"
                                            && (row.FromTkId == tankId || row.ToTkId == tankId))
                                            .ToListAsync<AbcBlendSwings>();
            if (Data.Count() > 0)
                _blendMonitorContext.AbcBlendSwings.RemoveRange(Data);
            return 0;
        }
        public async Task<int> DeleteAbcBlendDSwings2(double blendId)
        {
            //"DELETE FROM ABC_BLEND_SWINGS " & _
            //" WHERE BLEND_ID = " & strNewBlendId & _
            //" AND SWING_STATE = 'READY' " & _
            //" AND FROM_TK_ID = (" & _
            //" SELECT TANK_ID FROM ABC_BLEND_DEST WHERE " & _
            //" BLEND_ID = " & strNewBlendId & _
            //" AND IN_USE_FLAG = 'YES')"

            double tankId = await _blendMonitorContext.AbcBlendDest
                                .Where<AbcBlendDest>(row => row.BlendId == blendId && row.InUseFlag == "YES")
                                .Select(row => row.TankId)
                                .FirstOrDefaultAsync<double>();

            List<AbcBlendSwings> Data = await _blendMonitorContext.AbcBlendSwings
                                            .Where<AbcBlendSwings>(row => row.BlendId == blendId && row.SwingState == "READY"
                                            && row.FromTkId == tankId)
                                            .ToListAsync<AbcBlendSwings>();
            if (Data.Count() > 0)
                _blendMonitorContext.AbcBlendSwings.RemoveRange(Data);
            return 0;
        }
        public async Task<int> DeleteAbcBlendDSwings3(double blendId)
        {
            //"DELETE FROM ABC_BLEND_SWINGS " & _
            //" WHERE BLEND_ID = " & strNewBlendId & _
            //" AND SWING_STATE = 'READY' " & _
            //" AND TO_TK_ID = (" & _
            //" SELECT TANK_ID FROM ABC_BLEND_DEST WHERE " & _
            //" BLEND_ID = " & strNewBlendId & _
            //" AND IN_USE_FLAG = 'YES')"

            double tankId = await _blendMonitorContext.AbcBlendDest
                                .Where<AbcBlendDest>(row => row.BlendId == blendId && row.InUseFlag == "YES")
                                .Select(row => row.TankId)
                                .FirstOrDefaultAsync<double>();

            List<AbcBlendSwings> Data = await _blendMonitorContext.AbcBlendSwings
                                            .Where<AbcBlendSwings>(row => row.BlendId == blendId && row.SwingState == "READY"
                                            && row.ToTkId == tankId)
                                            .ToListAsync<AbcBlendSwings>();
            if (Data.Count() > 0)
                _blendMonitorContext.AbcBlendSwings.RemoveRange(Data);
            return 0;
        }
        public async Task<int> SetAbcBlendDestData(double blendId)
        {
            // UPDATE ABC_BLEND_DEST SET (IN_USE_FLAG,FLUSH_TK_FLAG,END_LINEFILL_TK_FLAG) =
            //" + (" (SELECT \'NO\',\'NO\',\'NO\' FROM DUAL) WHERE BLEND_ID = " + strNewBlendId));
            AbcBlendDest Data = await _blendMonitorContext.AbcBlendDest
                                    .Where<AbcBlendDest>(row => row.BlendId == blendId)
                                    .FirstOrDefaultAsync<AbcBlendDest>();
            Data.InUseFlag = "NO";
            Data.FlushTkFlag = "NO";
            Data.EndLinefillTkFlag = "NO";
            return await _blendMonitorContext.SaveChangesAsync();

        }
        public async Task<int> SetAbcBlendDestData2(double blendId, double tankId, double dblHeelVol, double lineupId)
        {
            //"UPDATE abc_blend_dest SET (IN_USE_FLAG,FIX_HEEL_FLAG,HEEL_VOLUME,LINEUP_ID,FLUSH_TK_FLAG)= "
            //"(SELECT 'YES','YES'," & dblHeelVol & "," & strLineupID & ",'YES'" & _
            //" FROM DUAL) WHERE BLEND_ID=" & strNewBlendId & " AND TANK_ID=" & strTankId

            AbcBlendDest Data = await _blendMonitorContext.AbcBlendDest
                                    .Where<AbcBlendDest>(row => row.BlendId == blendId && row.TankId == tankId)
                                    .FirstOrDefaultAsync<AbcBlendDest>();
            Data.InUseFlag = "YES";
            Data.FixHeelFlag = "YES";
            Data.HeelVolume = dblHeelVol;
            Data.LineupId = lineupId;
            Data.FlushTkFlag = "YES";

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetAbcBlendDestData3(double blendId, double tankId, double dblHeelVol, double lineupId)
        {
            //"UPDATE abc_blend_dest SET (IN_USE_FLAG,FIX_HEEL_FLAG,HEEL_VOLUME,LINEUP_ID)= "
            //(SELECT 'YES','YES'," & dblHeelVol & "," & strLineupID & _
            //FROM DUAL) WHERE BLEND_ID=" & strNewBlendId & " AND TANK_ID=" & strTankId

            AbcBlendDest Data = await _blendMonitorContext.AbcBlendDest
                                    .Where<AbcBlendDest>(row => row.BlendId == blendId && row.TankId == tankId)
                                    .FirstOrDefaultAsync<AbcBlendDest>();
            Data.InUseFlag = "YES";
            Data.FixHeelFlag = "YES";
            Data.HeelVolume = dblHeelVol;
            Data.LineupId = lineupId;

            return await _blendMonitorContext.SaveChangesAsync();

        }
        public async Task<int> InsertAbcBlendDestProps(double blendId, double tankId)
        {
            //"INSERT INTO abc_blend_dest_props (BLEND_ID,TANK_ID,PROP_ID," & _
            //        " HEEL_VALUE,CURRENT_VALUE,ON_SPEC_FLAG) "

            // (SELECT " & strNewBlendId & ", ABC_TANK_PROPS.TANK_ID, " & _
            //" ABC_TANK_PROPS.PROP_ID,ABC_TANK_PROPS.VALUE, ABC_TANK_PROPS.VALUE, 'NO' " & _

            //" FROM ABC_TANK_PROPS, ABC_TANKS " & _

            //" WHERE ABC_TANK_PROPS.TANK_ID = ABC_TANKS.ID AND " & _
            //" ABC_TANKS.ID = " & strTankId & " AND " & _
            //" ABC_TANK_PROPS.SELECTED_FLAG = 'YES' AND " & _
            //" ABC_TANK_PROPS.GOOD_FLAG = 'YES' AND " & _
            //" ABC_TANK_PROPS.PROP_ID IN " & _

            //" (SELECT PROP_ID FROM ABC_BLEND_PROPS WHERE BLEND_ID = " & strNewBlendId & "))"

            List<double> propIds = await _blendMonitorContext.AbcBlendProps
                                .Where<AbcBlendProps>(row => row.BlendId == blendId)
                                .Select(row => row.PropId)
                                .ToListAsync<double>();

            List<AbcBlendDestProps> Data = await (from ATP in _blendMonitorContext.AbcTankProps
                                                  from AT in _blendMonitorContext.AbcTanks
                                                  where ATP.TankId == AT.Id && AT.Id == tankId && ATP.SelectedFlag == "YES"
                                                  && ATP.GoodFlag == "YES" && propIds.Contains(ATP.PropId)
                                                  select new AbcBlendDestProps
                                                  {
                                                      BlendId = blendId,
                                                      TankId = ATP.TankId,
                                                      PropId = ATP.PropId,
                                                      HeelValue = ATP.Value,
                                                      CurrentValue = ATP.Value,
                                                      OnSpecFlag = "NO"
                                                  }).ToListAsync<AbcBlendDestProps>();

            await _blendMonitorContext.AbcBlendDestProps.AddRangeAsync(Data);

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> InsertAbcBlendDestSeq(double blendId, double tankId)
        {
            //"INSERT INTO abc_blend_dest_seq (BLEND_ID,TANK_ID,SWING_SEQUENCE,TIME_IN)

            //(SELECT " & strNewBlendId & "," & strTankId & ", 1, nvl(ABC_BLENDS.PLANNED_START,sysdate) " & _
            //FROM ABC_BLENDS WHERE ABC_BLENDS.ID = " & strNewBlendId & ")"           

            List<AbcBlendDestSeq> Data = await _blendMonitorContext.AbcBlends
                                            .Where<AbcBlends>(row => row.Id == blendId)
                                            .Select(row => new AbcBlendDestSeq
                                            {
                                                BlendId = blendId,
                                                TankId = tankId,
                                                SwingSequence = 1,
                                                TimeIn = (row.PlannedStart == null) ? DateTime.Now : Convert.ToDateTime(row.PlannedStart)
                                            })
                                            .ToListAsync<AbcBlendDestSeq>();

            await _blendMonitorContext.AbcBlendDestSeq.AddRangeAsync(Data);

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<double>> GetDefaultLineupIds(double blendId, double tankId)
        {
            //"SELECT DEFAULT_LINEUP_ID " & _
            //"FROM ABC_BLENDER_DEST " & _
            //"WHERE TANK_ID = " & _
            //strTank & " AND " & _
            //"BLENDER_ID = " & _
            //"(SELECT BLENDER_ID FROM ABC_BLENDS WHERE ID = " & _
            //strBlend & ")"

            double blenderId = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Id == blendId)
                                .Select(row => row.BlenderId)
                                .FirstOrDefaultAsync<double>();

            return await _blendMonitorContext.AbcBlenderDest
                        .Where<AbcBlenderDest>(row => row.TankId == tankId && row.BlenderId == blenderId)
                        .Select(row => row.DefaultLineupId)
                        .ToListAsync<double>();
        }
        public async Task<List<double>> GetCriteriaId(string name)
        {
            //"SELECT ID AS CRITERIA_ID " & _
            //"FROM ABC_SWING_CRITERIA " & _
            //"WHERE NAME='" & strCriteriaName & "'"

            return await _blendMonitorContext.AbcSwingCriteria
                        .Where<AbcSwingCriteria>(row => row.Name == name)
                        .Select(row => row.Id)
                        .ToListAsync<double>();
        }
        public async Task<List<AbcPrdPropSpecs>> GetAbcPrdPropSpecs(string prodName, string gradeName, string blenderName, string blendName)
        {
            //"SELECT S.PROP_ID,S.GIVEAWAYCOST, " & _
            //"S.CONTROL_MIN,S.CONTROL_MAX, " & _
            //"S.SALES_MIN,S.SALES_MAX " & _

            //"FROM ABC_PRD_PROP_SPECS S,ABC_BLEND_PROPS P," & _
            //"ABC_MATERIALS M,ABC_GRADES G " & _

            //"WHERE S.PROP_ID = P.PROP_ID AND " & _
            //"S.MAT_ID = M.ID AND " & _
            //"M.NAME = '" & strProduct & "' AND " & _
            //"S.GRADE_ID = G.ID AND " & _
            //"G.NAME = '" & strGrade & "' AND " & _
            //"S.PRDGRP_ID = " & _
            //"(SELECT PRDGRP_ID FROM ABC_BLENDERS WHERE NAME = '" & strBlender & _
            //"') AND P.BLEND_ID = " & _
            //"(SELECT ID FROM ABC_BLENDS WHERE NAME = '" & strBlend & "')"

            double prdgrpId = await _blendMonitorContext.AbcBlenders
                                .Where<AbcBlenders>(row => row.Name == blenderName)
                                .Select(row => row.PrdgrpId)
                                .FirstOrDefaultAsync<double>();

            double blendId = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Name == blendName)
                                .Select(row => row.Id)
                                .FirstOrDefaultAsync<double>();

            return await (from S in _blendMonitorContext.AbcPrdPropSpecs
                          from P in _blendMonitorContext.AbcBlendProps
                          from M in _blendMonitorContext.AbcMaterials
                          from G in _blendMonitorContext.AbcGrades
                          where S.PropId == P.PropId && S.MatId == M.Id &&
                          M.Name == prodName && S.GradeId == G.Id &&
                          G.Name == gradeName && S.PrdgrpId == prdgrpId && P.BlendId == blendId
                          select new AbcPrdPropSpecs
                          {
                              PropId = S.PropId,
                              Giveawaycost = S.Giveawaycost,
                              ControlMin = S.ControlMin,
                              ControlMax = S.ControlMax,
                              SalesMin = S.SalesMin,
                              SalesMax = S.SalesMax
                          }).ToListAsync<AbcPrdPropSpecs>();

        }
        public async Task<List<AbcPrdPropSpecs>> GetAbcPrdPropSpecs2(string prodName, string gradeName, string blenderName, string blendName)
        {
            //"SELECT S.PROP_ID,S.GIVEAWAYCOST, " & _
            //"S.CONTROL_MIN,S.CONTROL_MAX, " & _
            //"S.SALES_MIN,S.SALES_MAX " & _

            //"FROM ABC_PRD_PROP_SPECS S," & _
            //"ABC_MATERIALS M,ABC_GRADES G " & _

            //"WHERE S.PROP_ID NOT IN " & _
            //"(SELECT PROP_ID FROM ABC_BLEND_PROPS WHERE BLEND_ID = " & _
            //"(SELECT ID FROM ABC_BLENDS WHERE NAME = '" & strBlend & "')) 
            //AND " & _
            //"S.MAT_ID = M.ID AND " & _
            //"M.NAME = '" & strProduct & "' AND " & _
            //"S.GRADE_ID = G.ID AND " & _
            //"G.NAME = '" & strGrade & "' AND " & _
            //"S.PRDGRP_ID = " & _
            //"(SELECT PRDGRP_ID FROM ABC_BLENDERS WHERE NAME = '" & _
            //strBlender & "')"

            double prdgrpId = await _blendMonitorContext.AbcBlenders
                                .Where<AbcBlenders>(row => row.Name == blenderName)
                                .Select(row => row.PrdgrpId)
                                .FirstOrDefaultAsync<double>();

            double blendId = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Name == blendName)
                                .Select(row => row.Id)
                                .FirstOrDefaultAsync<double>();

            List<double> propIds = await _blendMonitorContext.AbcBlendProps
                                .Where<AbcBlendProps>(row => row.PropId == blendId)
                                .Select(row => row.PropId)
                                .ToListAsync<double>();

            return await (from S in _blendMonitorContext.AbcPrdPropSpecs
                          from M in _blendMonitorContext.AbcMaterials
                          from G in _blendMonitorContext.AbcGrades
                          where !propIds.Contains(S.PropId) && S.MatId == M.Id &&
                          M.Name == prodName && S.GradeId == G.Id &&
                          G.Name == gradeName && S.PrdgrpId == prdgrpId
                          select new AbcPrdPropSpecs
                          {
                              PropId = S.PropId,
                              Giveawaycost = S.Giveawaycost,
                              ControlMin = S.ControlMin,
                              ControlMax = S.ControlMax,
                              SalesMin = S.SalesMin,
                              SalesMax = S.SalesMax
                          }).ToListAsync<AbcPrdPropSpecs>();
        }
        public async Task<int> SetAbcBlendPropsData(string blendName, double propId, double? giveawayCost, double? controlMin, double? controlMax, double? salesMin, double? salesMax)
        {
            //"UPDATE ABC_BLEND_PROPS SET GIVEAWAYCOST = " & strCost & _
            //" WHERE BLEND_ID = (SELECT ID FROM ABC_BLENDS " & _
            //" WHERE NAME = '" & strBlend & "') AND " & _
            //" PROP_ID = " & strPropID

            //"UPDATE ABC_BLEND_PROPS SET CONTROL_MIN = " & strControlMin & _
            //" WHERE BLEND_ID = (SELECT ID FROM ABC_BLENDS " & _
            //" WHERE NAME = '" & strBlend & "') AND " & _
            //" PROP_ID = " & strPropID

            //"UPDATE ABC_BLEND_PROPS SET CONTROL_MAX = " & strControlMax & _
            //" WHERE BLEND_ID = (SELECT ID FROM ABC_BLENDS " & _
            //" WHERE NAME = '" & strBlend & "') AND " & _
            //" PROP_ID = " & strPropID

            //"UPDATE ABC_BLEND_PROPS SET SALES_MIN = " & strSalesMin & _
            //" WHERE BLEND_ID = (SELECT ID FROM ABC_BLENDS " & _
            //" WHERE NAME = '" & strBlend & "') AND " & _
            //" PROP_ID = " & strPropID

            //"UPDATE ABC_BLEND_PROPS SET SALES_MAX = " & strSalesMax & _
            //" WHERE BLEND_ID = (SELECT ID FROM ABC_BLENDS " & _
            //" WHERE NAME = '" & strBlend & "') AND " & _
            //" PROP_ID = " & strPropID

            double blendId = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Name == blendName)
                                .Select(row => row.Id)
                                .FirstOrDefaultAsync<double>();

            AbcBlendProps Data = await _blendMonitorContext.AbcBlendProps
                                    .Where<AbcBlendProps>(row => row.BlendId == blendId && row.PropId == propId)
                                    .FirstOrDefaultAsync<AbcBlendProps>();
            Data.Giveawaycost = giveawayCost;
            Data.ControlMin = controlMin;
            Data.ControlMax = controlMax;
            Data.SalesMin = salesMin;
            Data.SalesMax = salesMax;

            return await _blendMonitorContext.SaveChangesAsync();

        }
        public async Task<int> InsertAbcBlendPropsData(string blendName, double propId, double? giveawayCost, double? controlMin, double? controlMax, double? salesMin, double? salesMax)
        {
            //"INSERT INTO ABC_BLEND_PROPS " & _
            //"(BLEND_ID,PROP_ID,GIVEAWAYCOST," & _
            //"SALES_MIN,SALES_MAX,CONTROL_MIN,CONTROL_MAX," & _
            //"MODEL_ERR_EXISTS_FLAG,MODEL_ERR_CLRD_FLAG)

            //"(SELECT ABC_BLENDS.ID bid,'" & strPropID & "','" & _
            //strCost & "','" & strSalesMin & "','" & strSalesMax & "','" & _
            //strControlMin & "','" & strControlMax & _
            //"','NO','NO' FROM DUAL,ABC_BLENDS " & _
            //" WHERE ABC_BLENDS.NAME = '" & strBlend & "')"

            AbcBlends Data = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Name == blendName)
                                .FirstOrDefaultAsync<AbcBlends>();

            AbcBlendProps DataObj = new AbcBlendProps();
            DataObj.BlendId = Data.Id;
            DataObj.PropId = propId;
            DataObj.Giveawaycost = giveawayCost;
            DataObj.SalesMin = salesMin;
            DataObj.SalesMax = salesMax;
            DataObj.ControlMin = controlMin;
            DataObj.ControlMax = controlMax;
            DataObj.ModelErrExistsFlag = "NO";
            DataObj.ModelErrClrdFlag = "NO";

            await _blendMonitorContext.AbcBlendProps.AddAsync(DataObj);
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<AbcPrdgrpMatProps>> GetPrdgrpMatPropData(string blenderName, string prodName)
        {
            //"SELECT MATPROPS.PROP_ID AS PROP_ID, " & _
            //"MATPROPS.VALID_MIN AS MIN, " & _
            //"MATPROPS.VALID_MAX AS MAX " & _

            //"FROM ABC_PRDGRP_MAT_PROPS MATPROPS, " & _
            //"ABC_USAGES USAGE " & _

            //"WHERE MATPROPS.USAGE_ID = USAGE.ID AND " & _
            //"USAGE.NAME = 'PRODUCT'  AND " & _
            //"MATPROPS.PRDGRP_ID = " & _
            //"(SELECT PRDGRP_ID FROM ABC_BLENDERS WHERE NAME = '" & strBlender & _
            //"') AND MATPROPS.MAT_ID = " & _
            //"(SELECT ID FROM ABC_MATERIALS WHERE NAME = '" & strProduct & "')"

            double prdgrpId = await _blendMonitorContext.AbcBlenders
                               .Where<AbcBlenders>(row => row.Name == blenderName)
                               .Select(row => row.PrdgrpId)
                               .FirstOrDefaultAsync<double>();

            double matId = await _blendMonitorContext.AbcMaterials
                                .Where<AbcMaterials>(row => row.Name == prodName)
                                .Select(row => row.Id)
                                .FirstOrDefaultAsync<double>();

            return await (from MATPROPS in _blendMonitorContext.AbcPrdgrpMatProps
                          from USAGE in _blendMonitorContext.AbcUsages
                          where MATPROPS.UsageId == USAGE.Id && USAGE.Name == "PRODUCT"
                          && MATPROPS.PrdgrpId == prdgrpId && MATPROPS.MatId == matId
                          select new AbcPrdgrpMatProps
                          {
                              PropId = MATPROPS.PropId,
                              ValidMin = MATPROPS.ValidMin,
                              ValidMax = MATPROPS.ValidMax
                          }).ToListAsync<AbcPrdgrpMatProps>();
        }
        public async Task<int> SetAbcBlendPropsValidMin(string blendName, double propId, double min)
        {
            //"UPDATE ABC_BLEND_PROPS SET VALID_MIN = " & strMin & _
            //" WHERE BLEND_ID = (SELECT ID FROM ABC_BLENDS " & _
            //" WHERE NAME = '" & strBlend & "') AND " & _
            //" PROP_ID = " & strPropID

            double blendId = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Name == blendName)
                                 .Select(row => row.Id)
                                .FirstOrDefaultAsync<double>();

            AbcBlendProps Data = await _blendMonitorContext.AbcBlendProps
                                    .Where<AbcBlendProps>(row => row.BlendId == blendId && row.PropId == propId)
                                    .FirstOrDefaultAsync<AbcBlendProps>();
            Data.ValidMin = min;
            return await _blendMonitorContext.SaveChangesAsync();

        }

        public async Task<int> SetAbcBlendPropsValidMax(string blendName, double propId, double max)
        {
            //"UPDATE ABC_BLEND_PROPS SET VALID_MAX = " & strMax & _
            //" WHERE BLEND_ID = (SELECT ID FROM ABC_BLENDS " & _
            //" WHERE NAME = '" & strBlend & "') AND " & _
            //" PROP_ID = " & strPropID

            double blendId = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Name == blendName)
                                 .Select(row => row.Id)
                                .FirstOrDefaultAsync<double>();

            AbcBlendProps Data = await _blendMonitorContext.AbcBlendProps
                                    .Where<AbcBlendProps>(row => row.BlendId == blendId && row.PropId == propId)
                                    .FirstOrDefaultAsync<AbcBlendProps>();
            Data.ValidMax = max;
            return await _blendMonitorContext.SaveChangesAsync();

        }
        public async Task<int> SetAbcBlendProps(string blendName)
        {
            //"UPDATE ABC_BLEND_PROPS SET CONTROLLED = 'NO' " & _
            //"WHERE BLEND_ID = (SELECT ID FROM ABC_BLENDS " & _
            //" WHERE NAME = '" & strBlend & "') AND " & _
            //" CONTROL_MIN IS NULL AND CONTROL_MAX IS NULL"

            double blendId = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Name == blendName)
                                 .Select(row => row.Id)
                                .FirstOrDefaultAsync<double>();

            AbcBlendProps Data = await _blendMonitorContext.AbcBlendProps
                                    .Where<AbcBlendProps>(row => row.BlendId == blendId && row.ControlMin == null && row.ControlMax == null)
                                    .FirstOrDefaultAsync<AbcBlendProps>();
            Data.Controlled = "NO";
            return await _blendMonitorContext.SaveChangesAsync();

        }

        public async Task<int> SetAbcBlendProps2(string blendName)
        {
            //"UPDATE ABC_BLEND_PROPS SET CONTROLLED = 'YES' " & _
            //"WHERE BLEND_ID = (SELECT ID FROM ABC_BLENDS " & _
            //" WHERE NAME = '" & strBlend & "') AND " & _
            //" ((CONTROL_MIN IS NOT NULL) OR (CONTROL_MAX IS NOT NULL))"

            double blendId = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Name == blendName)
                                 .Select(row => row.Id)
                                .FirstOrDefaultAsync<double>();

            AbcBlendProps Data = await _blendMonitorContext.AbcBlendProps
                                    .Where<AbcBlendProps>(row => row.BlendId == blendId && row.ControlMin != null && row.ControlMax != null)
                                    .FirstOrDefaultAsync<AbcBlendProps>();
            Data.Controlled = "YES";
            return await _blendMonitorContext.SaveChangesAsync();

        }
        public async Task<int> SetAbcBlendPropsResTagId(string blendName)
        {
            //"UPDATE ABC_BLEND_PROPS P SET ANZ_RES_TAG_ID = " & _
            //"(SELECT MAX(RES_TAG_ID) " & _
            //"FROM ABC_BLEND_PROPS B, ABC_ANZ_HDR_PROPS A " & _
            //"WHERE B.PROP_ID = A.PROP_ID(+) AND " & _
            //"B.BLEND_ID = (SELECT ID FROM ABC_BLENDS WHERE NAME = '" & strBlend & "') AND " & _
            //"A.BLENDER_ID = (SELECT BLENDER_ID FROM ABC_BLENDS WHERE NAME = '" & strBlend & "') AND " & _
            //"B.PROP_ID = P.PROP_ID) " & _
            //"WHERE P.BLEND_ID = (SELECT ID FROM ABC_BLENDS WHERE NAME = '" & strBlend & "')"

            double blendId = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Name == blendName)
                                 .Select(row => row.Id)
                                .FirstOrDefaultAsync<double>();

            double blenderId = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Name == blendName)
                                 .Select(row => row.BlenderId)
                                .FirstOrDefaultAsync<double>();

            double? ResTagId = await (from B in _blendMonitorContext.AbcBlendProps
                                      from A in _blendMonitorContext.AbcAnzHdrProps
                                      from P in _blendMonitorContext.AbcBlendProps
                                      where B.PropId == A.PropId && B.BlendId == blendId && A.BlenderId == blenderId
                                      && B.PropId == P.PropId
                                      select B.AnzResTagId)
                                        .MaxAsync<double?>();
            AbcBlendProps Data = await _blendMonitorContext.AbcBlendProps
                                    .Where<AbcBlendProps>(row => row.BlendId == blendId)
                                    .FirstOrDefaultAsync<AbcBlendProps>();
            Data.AnzResTagId = ResTagId;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetAbcBlendPropsAnzOffset(string blendName)
        {
            //"UPDATE ABC_BLEND_PROPS P SET ANZ_OFFSET = " & _
            //"(SELECT MAX(OFFSET) " & _

            //"FROM ABC_BLEND_PROPS B, ABC_ANZ_HDR_PROPS A " & _

            //"WHERE B.PROP_ID = A.PROP_ID(+) AND " & _
            //"B.BLEND_ID = (SELECT ID FROM ABC_BLENDS WHERE NAME = '" & strBlend & "') AND " & _
            //"A.BLENDER_ID = (SELECT BLENDER_ID FROM ABC_BLENDS WHERE NAME = '" & strBlend & "') AND " & _
            //"B.PROP_ID = P.PROP_ID) " & _
            //"WHERE P.BLEND_ID = (SELECT ID FROM ABC_BLENDS WHERE NAME = '" & strBlend & "')"

            double blendId = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Name == blendName)
                                 .Select(row => row.Id)
                                .FirstOrDefaultAsync<double>();

            double blenderId = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Name == blendName)
                                 .Select(row => row.BlenderId)
                                .FirstOrDefaultAsync<double>();

            double? offset = await (from B in _blendMonitorContext.AbcBlendProps
                                    from A in _blendMonitorContext.AbcAnzHdrProps
                                    from P in _blendMonitorContext.AbcBlendProps
                                    where B.PropId == A.PropId && B.BlendId == blendId && A.BlenderId == blenderId
                                    && B.PropId == P.PropId
                                    select A.Offset)
                                        .MaxAsync<double?>();
            AbcBlendProps Data = await _blendMonitorContext.AbcBlendProps
                                    .Where<AbcBlendProps>(row => row.BlendId == blendId)
                                    .FirstOrDefaultAsync<AbcBlendProps>();
            Data.AnzOffset = offset;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetAbcBlendPropsIntrvBias(string blendName, double sequence, double blendId)
        {
            //"UPDATE ABC_BLEND_PROPS PROP SET PROP.INITIAL_BIAS =  " & _

            //"(SELECT INT.BIAS " & _            
            //" FROM ABC_BLEND_INTERVAL_PROPS INT " & _            
            //" WHERE INT.SEQUENCE = " & strSequence & _
            //"   AND INT.BLEND_ID = " & strOldBlendId & _
            //"   AND INT.PROP_ID = PROP.PROP_ID) " & _
            //" WHERE PROP.BLEND_ID = (SELECT ID FROM ABC_BLENDS " & _
            //" WHERE NAME = '" & strNewBlendName & "')"

            double? bias = await (from INT in _blendMonitorContext.AbcBlendIntervalProps
                                  from P in _blendMonitorContext.AbcBlendProps
                                  where INT.Sequence == sequence && INT.BlendId == blendId && INT.PropId == P.PropId
                                  select INT.Bias)
                                  .FirstOrDefaultAsync<double?>();

            AbcBlendProps Data = await _blendMonitorContext.AbcBlendProps
                                    .Where<AbcBlendProps>(row => row.BlendId == blendId)
                                    .FirstOrDefaultAsync<AbcBlendProps>();
            Data.InitialBias = bias;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetAbcBlendPropsCalcAndCost(string blendName, string blenderName)
        {
            //"UPDATE ABC_BLEND_PROPS P SET (CALC_ID,GIVEAWAYCOST) = " & _
            //"(SELECT PRD.CALC_ID, PRD.GIVEAWAYCOST " & _

            //"FROM ABC_BLEND_PROPS B, ABC_PRDGRP_PROPS PRD " & _

            //"WHERE B.PROP_ID = PRD.PROP_ID(+) AND " & _
            //"B.BLEND_ID = (SELECT ID FROM ABC_BLENDS WHERE NAME = '" & strBlend & "') AND " & _
            //"PRD.PRDGRP_ID = (SELECT PRDGRP_ID FROM ABC_BLENDERS WHERE NAME = '" & strBlender & "') AND " & _
            //"B.PROP_ID = P.PROP_ID) " & _
            //"WHERE P.BLEND_ID = (SELECT ID FROM ABC_BLENDS WHERE NAME = '" & strBlend & "')"

            double blendId = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Name == blendName)
                                 .Select(row => row.Id)
                                .FirstOrDefaultAsync<double>();

            double prdgrpId = await _blendMonitorContext.AbcBlenders
                                .Where<AbcBlenders>(row => row.Name == blenderName)
                                 .Select(row => row.PrdgrpId)
                                .FirstOrDefaultAsync<double>();

            var res = await (from B in _blendMonitorContext.AbcBlendProps
                             from PRD in _blendMonitorContext.AbcPrdgrpProps
                             from P in _blendMonitorContext.AbcBlendProps
                             where B.PropId == PRD.PropId && B.BlendId == blendId && PRD.PrdgrpId == prdgrpId
                             && B.PropId == P.PropId
                             select new Tuple<double?, double?>(PRD.CalcId, PRD.Giveawaycost))
                                      .FirstOrDefaultAsync<Tuple<double?, double?>>();

            AbcBlendProps Data = await _blendMonitorContext.AbcBlendProps
                                    .Where<AbcBlendProps>(row => row.BlendId == blendId)
                                    .FirstOrDefaultAsync<AbcBlendProps>();
            Data.CalcId = res.Item1;
            Data.Giveawaycost = res.Item2;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<double>> GetBlendIntervalSequence(double blendId)
        {
            //"SELECT BI.SEQUENCE,B1.ACTUAL_END 
            //FROM ABC_BLEND_INTERVALS BI,ABC_BLENDS B1 
            //WHERE " & _
            //" B1.ID = " & strOldBlendId & " AND BI.STOPTIME = " & _

            //"(SELECT MAX(INT.STOPTIME) " & _
            //"FROM ABC_BLEND_INTERVALS INT " & _
            //"WHERE INT.BLEND_ID = " & strOldBlendId & ")"

            DateTime? time = await _blendMonitorContext.AbcBlendIntervals
                            .Where<AbcBlendIntervals>(row => row.BlendId == blendId)
                            .Select(row => row.Stoptime)
                            .MaxAsync<DateTime?>();

            return await (from BI in _blendMonitorContext.AbcBlendIntervals
                          from B1 in _blendMonitorContext.AbcBlends
                          where B1.Id == blendId && BI.Stoptime == time
                          select BI.Sequence).ToListAsync<double>();

        }
        public async Task<int> SetAbcBlendCompTankMin(double oldBlendId, double sequence, string blendName)
        {
            //"UPDATE ABC_BLEND_COMPS COMP SET COMP.TANK_MIN=" &

            //"(SELECT DECODE(GREATEST(INT.INT_RECIPE, COMP.TANK_MIN),INT.INT_RECIPE,COMP.TANK_MIN,COMP.TANK_MIN,INT.INT_RECIPE,0) " &
            //"FROM ABC_BLEND_INTERVAL_COMPS INT 
            // WHERE INT.BLEND_ID=" & strOldBlendId &
            //" AND INT.SEQUENCE= " & strSequence & " AND INT.MAT_ID=COMP.MAT_ID) " &
            //"WHERE COMP.BLEND_ID = (SELECT ID FROM ABC_BLENDS " &
            //" WHERE NAME = '" & strNewBlendName & "')"

            double blendId = await _blendMonitorContext.AbcBlends
                               .Where<AbcBlends>(row => row.Name == blendName)
                                .Select(row => row.Id)
                               .FirstOrDefaultAsync<double>();

            var res = await (from COMP in _blendMonitorContext.AbcBlendComps
                             from INT in _blendMonitorContext.AbcBlendIntervalComps
                             where INT.BlendId == oldBlendId && INT.Sequence == sequence && INT.MatId == COMP.MatId
                             select new Tuple<double?, double?>(INT.IntRecipe, COMP.TankMin))
                          .FirstOrDefaultAsync<Tuple<double?, double?>>();

            double? tankMin = 0;
            double? greatest = (res.Item1 > res.Item2) ? res.Item1 : res.Item2;
            if (greatest == res.Item1)
            {
                tankMin = res.Item2;
            }
            else if (greatest == res.Item2)
            {
                tankMin = res.Item1;
            }

            AbcBlendComps Data = await _blendMonitorContext.AbcBlendComps
                                    .Where<AbcBlendComps>(row => row.BlendId == blendId)
                                    .FirstOrDefaultAsync<AbcBlendComps>();

            Data.TankMin = tankMin;

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetAbcBlendCompTankMax(double oldBlendId, double sequence, string blendName)
        {
            //"UPDATE ABC_BLEND_COMPS COMP SET COMP.TANK_MAX=" & _

            //"(SELECT DECODE(GREATEST(INT.INT_RECIPE,COMP.TANK_MAX),COMP.TANK_MAX,COMP.TANK_MAX,INT.INT_RECIPE,INT.INT_RECIPE,100) " & _
            //"FROM ABC_BLEND_INTERVAL_COMPS INT WHERE INT.BLEND_ID=" & strOldBlendId & _
            //" AND INT.SEQUENCE= " & strSequence & " AND INT.MAT_ID=COMP.MAT_ID)  " & _
            //"WHERE COMP.BLEND_ID = (SELECT ID FROM ABC_BLENDS " & _
            //" WHERE NAME = '" & strNewBlendName & "')"

            double blendId = await _blendMonitorContext.AbcBlends
                               .Where<AbcBlends>(row => row.Name == blendName)
                                .Select(row => row.Id)
                               .FirstOrDefaultAsync<double>();

            var res = await (from COMP in _blendMonitorContext.AbcBlendComps
                             from INT in _blendMonitorContext.AbcBlendIntervalComps
                             where INT.BlendId == oldBlendId && INT.Sequence == sequence && INT.MatId == COMP.MatId
                             select new Tuple<double?, double?>(INT.IntRecipe, COMP.TankMax))
                          .FirstOrDefaultAsync<Tuple<double?, double?>>();

            double? tankMin = 100;
            double? greatest = (res.Item1 > res.Item2) ? res.Item1 : res.Item2;
            if (greatest == res.Item1)
            {
                tankMin = res.Item1;
            }
            else if (greatest == res.Item2)
            {
                tankMin = res.Item2;
            }

            AbcBlendComps Data = await _blendMonitorContext.AbcBlendComps
                                    .Where<AbcBlendComps>(row => row.BlendId == blendId)
                                    .FirstOrDefaultAsync<AbcBlendComps>();

            Data.TankMin = tankMin;

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetAbcBlendCompPlanRecipe(double oldBlendId, double sequence, string blendName)
        {
            //"UPDATE ABC_BLEND_COMPS COMP SET COMP.PLAN_RECIPE =  " & _
            //"(SELECT INT.INT_RECIPE " & _
            //" FROM ABC_BLEND_INTERVAL_COMPS INT " & _
            //" WHERE INT.SEQUENCE = " & strSequence & _
            //"   AND INT.BLEND_ID = " & strOldBlendId & _
            //"   AND INT.MAT_ID = COMP.MAT_ID)" & _
            //" WHERE COMP.BLEND_ID = (SELECT ID FROM ABC_BLENDS " & _
            //" WHERE NAME = '" & strNewBlendName & "')"

            double blendId = await _blendMonitorContext.AbcBlends
                               .Where<AbcBlends>(row => row.Name == blendName)
                                .Select(row => row.Id)
                               .FirstOrDefaultAsync<double>();

            double? intRecipe = await (from COMP in _blendMonitorContext.AbcBlendComps
                                       from INT in _blendMonitorContext.AbcBlendIntervalComps
                                       where INT.BlendId == oldBlendId && INT.Sequence == sequence && INT.MatId == COMP.MatId
                                       select INT.IntRecipe)
                          .FirstOrDefaultAsync<double?>();

            AbcBlendComps Data = await _blendMonitorContext.AbcBlendComps
                                    .Where<AbcBlendComps>(row => row.BlendId == blendId)
                                    .FirstOrDefaultAsync<AbcBlendComps>();

            Data.PlanRecipe = intRecipe;

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetAbcBlendBatchTargetVolume(double oldBlendId, double sequence, string blendName)
        {
            //"UPDATE ABC_BLENDS BLEND SET BLEND.BATCH_TARGET_VOL =  " & _
            //"(SELECT OLDBLEND.BATCH_TARGET_VOL " & _
            //" FROM ABC_BLENDS OLDBLEND " & _
            //" WHERE OLDBLEND.ID = " & strOldBlendId & ")" & _
            //" WHERE BLEND.ID = (SELECT ID FROM ABC_BLENDS " & _
            //" WHERE NAME = '" & strNewBlendName & "')"

            double blendId = await _blendMonitorContext.AbcBlends
                               .Where<AbcBlends>(row => row.Name == blendName)
                                .Select(row => row.Id)
                               .FirstOrDefaultAsync<double>();

            double? batchTargetVol = await (from OLDBLEND in _blendMonitorContext.AbcBlends
                                            where OLDBLEND.Id == oldBlendId
                                            select OLDBLEND.BatchTargetVol)
                                        .FirstOrDefaultAsync<double?>();

            AbcBlends Data = await _blendMonitorContext.AbcBlends
                                    .Where<AbcBlends>(row => row.Id == blendId)
                                    .FirstOrDefaultAsync<AbcBlends>();

            Data.BatchTargetVol = batchTargetVol;

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetAbcBlendCompRcpConstraintType(double oldBlendId, double sequence, string blendName)
        {
            //"UPDATE ABC_BLEND_COMPS COMP SET COMP.RCP_CONSTRAINT_TYPE =  " & _
            //"(SELECT OLDCOMP.RCP_CONSTRAINT_TYPE " & _
            //" FROM ABC_BLEND_COMPS OLDCOMP " & _
            //" WHERE OLDCOMP.BLEND_ID = " & strOldBlendId & _
            //"  AND OLDCOMP.MAT_ID = COMP.MAT_ID)" & _
            //" WHERE COMP.BLEND_ID = (SELECT ID FROM ABC_BLENDS " & _
            //" WHERE NAME = '" & strNewBlendName & "')"

            double blendId = await _blendMonitorContext.AbcBlends
                               .Where<AbcBlends>(row => row.Name == blendName)
                                .Select(row => row.Id)
                               .FirstOrDefaultAsync<double>();

            string rcpConstraintType = await (from COMP in _blendMonitorContext.AbcBlendComps
                                              from OLDCOMP in _blendMonitorContext.AbcBlendComps
                                              where OLDCOMP.BlendId == oldBlendId && OLDCOMP.MatId == COMP.MatId
                                              select OLDCOMP.RcpConstraintType)
                                        .FirstOrDefaultAsync<string>();

            AbcBlendComps Data = await _blendMonitorContext.AbcBlendComps
                                    .Where<AbcBlendComps>(row => row.BlendId == blendId)
                                    .FirstOrDefaultAsync<AbcBlendComps>();

            Data.RcpConstraintType = rcpConstraintType;

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<RecipeHdr>> RecipeHdr(double blendId)
        {
            //SELECT ABC_BLEND_COMPS.BLEND_ID,
            // ABC_BLEND_COMPS.MAT_ID,
            // ABC_MATERIALS.NAME AS COMPONENT,
            // ABC_BLEND_SOURCES.TANK_ID,
            // ABC_TANKS.NAME AS TANK, 
            // ABC_BLEND_COMPS.TANK_MIN AS MINIMUM,
            // ABC_BLEND_COMPS.CUR_RECIPE, 
            // ABC_BLEND_COMPS.ACT_RECIPE AS ACTUAL,
            // ABC_BLEND_COMPS.TANK_MAX AS MAXIMUM,
            // ABC_BLEND_COMPS.PACING_FACTOR AS PACING,
            // ABC_BLEND_COMPS.COST AS COST,
            // ABC_BLEND_SOURCES.LINEUP_ID,
            // ABC_BLEND_COMPS.USAGE_ID

            // FROM ABC_BLEND_COMPS,
            //      ABC_BLEND_SOURCES,
            //      ABC_MATERIALS,
            //      ABC_TANKS

            // WHERE ABC_BLEND_COMPS.BLEND_ID = ABC_BLEND_SOURCES.BLEND_ID
            //   AND ABC_BLEND_COMPS.MAT_ID = ABC_BLEND_SOURCES.MAT_ID
            //   AND ABC_BLEND_COMPS.MAT_ID = ABC_MATERIALS.ID
            //   AND ABC_BLEND_SOURCES.TANK_ID = ABC_TANKS.ID
            //   AND ABC_BLEND_SOURCES.IN_USE_FLAG = 'YES' AND ABC_BLEND_SOURCES.BLEND_ID = ?

            return await (from ABC in _blendMonitorContext.AbcBlendComps
                          from ABS in _blendMonitorContext.AbcBlendSources
                          from AM in _blendMonitorContext.AbcMaterials
                          from AT in _blendMonitorContext.AbcTanks
                          where ABC.BlendId == ABS.BlendId
                           && ABC.MatId == ABS.MatId
                           && ABC.MatId == AM.Id
                           && ABS.TankId == AT.Id
                           && ABS.InUseFlag == "YES" && ABS.BlendId == blendId
                          select new RecipeHdr
                          {
                              BlendId = ABC.BlendId,
                              MatId = ABC.MatId,
                              Component = AM.Name,
                              TankId = ABS.TankId,
                              Tank = AT.Name,
                              Minimum = ABC.TankMin,
                              CurRecipe = ABC.CurRecipe,
                              Actual = ABC.ActRecipe,
                              Maximum = ABC.TankMax,
                              Pacing = ABC.PacingFactor,
                              Cost = ABC.Cost,
                              LineupId = ABS.LineupId,
                              UsageId = ABC.UsageId
                          }).ToListAsync<RecipeHdr>();
        }
        public async Task<List<RecipeBlend>> RecipeBlend(double blendId)
        {
            //SELECT ABC_BLEND_COMPS.BLEND_ID, 
            //ABC_BLEND_COMPS.MAT_ID, 
            //ABC_MATERIALS.NAME AS COMPONENT, 
            //ABC_BLEND_SOURCES.TANK_ID, 
            //ABC_TANKS.NAME AS TANK,
            //ABC_BLEND_COMPS.TANK_MIN, 
            //ABC_BLEND_COMPS.PREF_RECIPE AS PREFERRED, 
            //ABC_BLEND_COMPS.PLAN_RECIPE AS PLANNED, 
            //ABC_BLEND_COMPS.AVG_RECIPE AS AVERAGE, 
            //ABC_BLEND_COMPS.TANK_MAX, 
            //ABC_BLEND_COMPS.VOLUME,
            //ABC_BLEND_SOURCES.LINEUP_ID

            //FROM ABC_BLEND_COMPS, 
            //ABC_MATERIALS, 
            //ABC_BLEND_SOURCES, 
            //ABC_TANKS

            //WHERE ABC_BLEND_COMPS.MAT_ID = ABC_MATERIALS.ID
            //  AND ABC_BLEND_COMPS.BLEND_ID = ABC_BLEND_SOURCES.BLEND_ID
            //  AND ABC_BLEND_COMPS.MAT_ID = ABC_BLEND_SOURCES.MAT_ID
            //  AND ABC_BLEND_SOURCES.TANK_ID = ABC_TANKS.ID
            //  AND ABC_BLEND_SOURCES.IN_USE_FLAG = 'YES'
            //  AND ABC_BLEND_SOURCES.BLEND_ID = ?

            return await (from ABC in _blendMonitorContext.AbcBlendComps
                          from ABS in _blendMonitorContext.AbcBlendSources
                          from AM in _blendMonitorContext.AbcMaterials
                          from AT in _blendMonitorContext.AbcTanks
                          where ABC.BlendId == ABS.BlendId
                           && ABC.MatId == ABS.MatId
                           && ABC.MatId == AM.Id
                           && ABS.TankId == AT.Id
                           && ABS.InUseFlag == "YES" && ABS.BlendId == blendId
                          select new RecipeBlend
                          {
                              BlendId = ABC.BlendId,
                              MatId = ABC.MatId,
                              Component = AM.Name,
                              TankId = ABS.TankId,
                              Tank = AT.Name,
                              TankMin = ABC.TankMin,
                              Preferred = ABC.PrefRecipe,
                              Planned = ABC.PlanRecipe,
                              Average = ABC.AvgRecipe,
                              TankMax = ABC.TankMax,
                              Volume = ABC.Volume,
                              LineupId = ABS.LineupId
                          }).ToListAsync<RecipeBlend>();
        }
        public async Task<int> SetAbcBlendPrevId(double oldBlendId, double blendId)
        {
            //"UPDATE ABC_BLENDS SET previous_blend_id =" & curblend.lngID & " WHERE id=" & CLng(strNewBlendId)

            AbcBlends Data = await _blendMonitorContext.AbcBlends
                                .Where<AbcBlends>(row => row.Id == blendId)
                                .FirstOrDefaultAsync<AbcBlends>();
            Data.PreviousBlendId = oldBlendId;

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetBlendSwingState(double tankId, double blendId, string state)
        {
            //"UPDATE abc_blend_swings SET swing_state=" & _
            //"'INCOMPLETE' WHERE blend_id=" & curblend.lngID & _
            //" AND from_tk_id=" & lngDestTkId & " AND swing_state='ACTIVE'"

            AbcBlendSwings Data = await _blendMonitorContext.AbcBlendSwings
                               .Where<AbcBlendSwings>(row => row.BlendId == blendId && row.FromTkId == tankId && row.SwingState == "ACTIVE")
                               .FirstOrDefaultAsync<AbcBlendSwings>();
            Data.SwingState = state;

            return await _blendMonitorContext.SaveChangesAsync();

        }
        public async Task<int> SetBlendSwingState2(double tankId, double toTankId, double blendId, string state)
        {

            AbcBlendSwings Data = await _blendMonitorContext.AbcBlendSwings
                               .Where<AbcBlendSwings>(row => row.BlendId == blendId && row.FromTkId == tankId
                                && row.ToTkId == toTankId && row.SwingState == "ACTIVE")
                               .FirstOrDefaultAsync<AbcBlendSwings>();
            Data.SwingState = state;

            return await _blendMonitorContext.SaveChangesAsync();

        }
        public async Task<int> SetBlendSwingStateAndDoneAt(double tankId, double toTankId, double blendId)
        {
            //"UPDATE abc_blend_swings SET (swing_state,done_at)=(" & _
            //"SELECT 'INCOMPLETE',SYSDATE FROM DUAL) WHERE blend_id=" & curblend.lngID & _
            //" AND from_tk_id=" & lngDestTkId & " AND to_tk_id=" & lngToTankID

            AbcBlendSwings Data = await _blendMonitorContext.AbcBlendSwings
                               .Where<AbcBlendSwings>(row => row.BlendId == blendId && row.FromTkId == tankId && row.ToTkId == toTankId)
                               .FirstOrDefaultAsync<AbcBlendSwings>();

            Data.SwingState = "INCOMPLETE";
            Data.DoneAt = DateTime.Now;

            return await _blendMonitorContext.SaveChangesAsync();

        }
        public async Task<int> SetBlendSwingStateAndDoneAt2(double tankId, double toTankId, double blendId)
        {
            //"UPDATE abc_blend_swings SET (swing_state,done_at)=(" & _
            //"SELECT 'COMPLETE',SYSDATE FROM DUAL) WHERE blend_id=" & curblend.lngID & _
            //" AND from_tk_id=" & lngSrcTankID & " AND to_tk_id=" & lngToTankID & " AND swing_state NOT IN ('INCOMPLETE','COMPLETE')"            

            List<string> SwingStates = new List<string>() { "INCOMPLETE", "COMPLETE" };
            AbcBlendSwings Data = await _blendMonitorContext.AbcBlendSwings
                               .Where<AbcBlendSwings>(row => row.BlendId == blendId && row.FromTkId == tankId && row.ToTkId == toTankId
                               && !SwingStates.Contains(row.SwingState))
                               .FirstOrDefaultAsync<AbcBlendSwings>();

            Data.SwingState = "COMPLETE";
            Data.DoneAt = DateTime.Now;

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetBlendSwingStateAndDoneAt3(double tankId, double toTankId, double blendId)
        {
            //"UPDATE abc_blend_swings SET (swing_state,done_at)=(" & _
            //"SELECT 'ACTIVE',SYSDATE FROM DUAL) WHERE blend_id=" & curblend.lngID & _
            //" AND from_tk_id=" & lngSrcTankID & " AND to_tk_id=" & lngToTankID & " AND swing_state='READY'"

            AbcBlendSwings Data = await _blendMonitorContext.AbcBlendSwings
                               .Where<AbcBlendSwings>(row => row.BlendId == blendId && row.FromTkId == tankId && row.ToTkId == toTankId
                               && row.SwingState == "READY")
                               .FirstOrDefaultAsync<AbcBlendSwings>();

            Data.SwingState = "ACTIVE";
            Data.DoneAt = DateTime.Now;

            return await _blendMonitorContext.SaveChangesAsync();
        }     
        public async Task<int> SetBlendSourceSeqData(double blendId, double matId, double tankId, DateTime actualStart)
        {
            //"UPDATE abc_blend_source_seq SET time_in=" & _
            //"TO_DATE('" & Format(curblend.dteActualStart, strWinDateFmt & " " & WIN_TIME_FMT) & "','" & strOraDateFmt & " " & ORA_TIME_FMT & "') WHERE blend_id=" & curblend.lngID & _
            //" AND mat_id=" & lngMatId & " AND tank_id=" & lngSrcTankID & _
            //" AND swing_sequence=1"
            AbcBlendSourceSeq Data = await _blendMonitorContext.AbcBlendSourceSeq
                                        .Where<AbcBlendSourceSeq>(row => row.BlendId == blendId && row.MatId == matId
                                        && row.TankId == tankId && row.SwingSequence == 1)
                                        .FirstOrDefaultAsync();
            Data.TimeIn = actualStart;
            return await _blendMonitorContext.SaveChangesAsync();

        }
        public async Task<int> SetBlendSourceSeqData2(double blendId, double matId, double tankId, int seq, double dblSeqVolUsed)
        {
            //"UPDATE abc_blend_source_seq SET (vol_used,time_out)=(" & _
            //"SELECT " & dblSeqVolUsed & ",SYSDATE FROM DUAL) WHERE blend_id=" & curblend.lngID & _
            //" AND mat_id=" & lngMatId & " AND tank_id=" & lngSrcTankID & _
            //" AND swing_sequence=" & intSwingSeq
            AbcBlendSourceSeq Data = await _blendMonitorContext.AbcBlendSourceSeq
                                        .Where<AbcBlendSourceSeq>(row => row.BlendId == blendId && row.MatId == matId
                                        && row.TankId == tankId && row.SwingSequence == seq)
                                        .FirstOrDefaultAsync();
            Data.VolUsed = dblSeqVolUsed;
            Data.TimeOut = DateTime.Now;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetAbcBlendInUseFlag(double blendId, double matId, double tankId, string flag)
        {
            //"UPDATE abc_blend_sources SET in_use_flag='NO' " & _
            //"WHERE blend_id=" & curblend.lngID & " AND mat_id=" & lngMatId & " AND " & _
            //"tank_id<>" & lngToTankID

            AbcBlendSources Data = await _blendMonitorContext.AbcBlendSources
                                    .Where<AbcBlendSources>(row => row.BlendId == blendId && row.MatId == matId
                                    && row.TankId != tankId).FirstOrDefaultAsync<AbcBlendSources>();
            Data.InUseFlag = flag;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> SetAbcBlendInUseFlag2(double blendId, double matId, double tankId, string flag)
        {
            //"UPDATE abc_blend_sources SET in_use_flag='NO' " & _
            //"WHERE blend_id=" & curblend.lngID & " AND mat_id=" & lngMatId & " AND " & _
            //"tank_id<>" & lngToTankID

            AbcBlendSources Data = await _blendMonitorContext.AbcBlendSources
                                    .Where<AbcBlendSources>(row => row.BlendId == blendId && row.MatId == matId
                                    && row.TankId == tankId).FirstOrDefaultAsync<AbcBlendSources>();
            Data.InUseFlag = flag;
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<double?>> GetCompLineup(double blendId, double matId, double tankId)
        {
            //select lineup_id from abc_blend_sources where blend_id =? and
            //mat_id =? and tank_id =?
            return await _blendMonitorContext.AbcBlendSources
                                        .Where<AbcBlendSources>(row => row.BlendId == blendId
                                        && row.MatId == matId && row.TankId == tankId)
                                        .Select(row => row.LineupId)
                                        .ToListAsync<double?>();
        }
        public async Task<AbcBlenderComps> GetBldrCompsSwingOccurID(double blenderId, double blendId, double tankId, double matId)
        {
            //select abc_blender_comps.swing_occurred_tid,
            //abc_blender_comps.swing_tid
            //from abc_blender_comps,
            //     abc_blend_sources
            //where abc_blender_comps.blender_id = ?
            //  and abc_blend_sources.blend_id = ?
            //  and abc_blend_sources.in_use_flag = 'YES'
            //  and abc_blend_sources.mat_id = abc_blender_comps.mat_id
            //  and abc_blend_sources.tank_id = ? and abc_blender_comps.mat_id =?

            return await (from abc in _blendMonitorContext.AbcBlenderComps
                          from abs in _blendMonitorContext.AbcBlendSources
                          where abc.BlenderId == blenderId
                           && abs.BlendId == blendId
                           && abs.InUseFlag == "YES"
                           && abs.MatId == abc.MatId
                           && abs.TankId == tankId && abc.MatId == matId
                          select new AbcBlenderComps
                          {
                              SwingOccurredTid = abc.SwingOccurredTid,
                              SwingTid = abc.SwingTid
                          }).FirstOrDefaultAsync<AbcBlenderComps>();
        }
        public async Task<List<double>> GetBldSourceSwgSeq(double blendId, double matId)
        {
            //select swing_sequence 
            //from abc_blend_source_seq 
            //where blend_id =? and mat_id =? order by swing_sequence desc

            return await _blendMonitorContext.AbcBlendSourceSeq
                                        .Where<AbcBlendSourceSeq>(row => row.BlendId == blendId && row.MatId == matId)
                                        .OrderByDescending(row => row.SwingSequence)
                                        .Select(row => row.SwingSequence)
                                        .ToListAsync<double>();
        }
        public async Task<double?> GetBldSourceSumVolUsed(double blendId, double matId)
        {
            //select sum(vol_used) as Sum_VolUsed from abc_blend_source_seq where blend_id =? and mat_id =?

            return await _blendMonitorContext.AbcBlendSourceSeq
                                        .Where<AbcBlendSourceSeq>(row => row.BlendId == blendId && row.MatId == matId)
                                        .SumAsync(row => row.VolUsed);
        }
        public async Task<AbcBlendComps> GetBldMatVol(double blendId, double matId)
        {
            //select volume, cur_recipe 
            //from abc_blend_comps
            //where blend_id = ? and mat_id =?


            return await _blendMonitorContext.AbcBlendComps
                                    .Where<AbcBlendComps>(row => row.BlendId == blendId && row.MatId == matId)
                                    .Select(row => new AbcBlendComps
                                    {
                                        Volume = (row.Volume == null) ? 0 : row.Volume,
                                        CurRecipe = row.CurRecipe
                                    })
                                    .FirstOrDefaultAsync<AbcBlendComps>();
        }
        public async Task<int> InsetBlendSourceSeqData(double blendId, double matId, double tankId, int seq)
        {
            //"INSERT INTO abc_blend_source_seq (blend_id,mat_id,tank_id," & _
            //"swing_sequence,time_in) VALUES (" & curblend.lngID & "," & lngMatId & "," & _
            //lngToTankID & "," & intSwingSeq + 1 & ",SYSDATE)"

            AbcBlendSourceSeq Data = new AbcBlendSourceSeq();
            Data.BlendId = blendId;
            Data.MatId = matId;
            Data.TankId = tankId;
            Data.SwingSequence = seq;
            Data.TimeIn = DateTime.Now;
            await _blendMonitorContext.AbcBlendSourceSeq.AddAsync(Data);
            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<List<double?>> GetCompLineups(double blendId, double matId, double tankId)
        {
            //select line.station_id 
            //from abc_blend_sources srce, abc_comp_lineup_eqp line 
            //where srce.lineup_id = line.line_id and srce.blend_id =? and srce.mat_id =? and srce.tank_id = ?

            return await (from srce in _blendMonitorContext.AbcBlendSources
                          from line in _blendMonitorContext.AbcCompLineupEqp
                          where srce.LineupId == line.LineId && srce.BlendId == blendId
                          && srce.MatId == matId && srce.TankId == tankId
                          select line.StationId).ToListAsync<double?>();
        }
        public async Task<int> InsertBlendStations(double blendId, double matId, double stationid, double stationMax, double stationMin)
        {
            //"INSERT INTO abc_blend_stations " & _
            //" (BLEND_ID,MAT_ID,STATION_ID,IN_USE_FLAG,MAX_FLOW,MIN_FLOW) " & _
            //" VALUES (" & curblend.lngID & "," & lngMatId & "," & arToStatLineups(intI) & " ,'YES'," & _
            //sngStationMax & "," & sngStationMin & ")"

            AbcBlendStations Data = new AbcBlendStations();
            Data.BlendId = blendId;
            Data.MatId = matId;
            Data.StationId = stationid;
            Data.InUseFlag = "YES";
            Data.MaxFlow = stationMax;
            Data.MinFlow = stationMin;

            await _blendMonitorContext.AbcBlendStations.AddAsync(Data);

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> InsertBlendStations(double blendId, double matId, double lineupId)
        {
            //"INSERT INTO abc_blend_stations " & _
            //" (BLEND_ID,MAT_ID,STATION_ID,IN_USE_FLAG,MAX_FLOW,MIN_FLOW) " & _
            //" (SELECT " & curblend.lngID & "," & lngMatId & ",EQP.STATION_ID,'YES'," & _
            //" ST.MAX,ST.MIN FROM ABC_COMP_LINEUP_EQP EQP, " & _
            //" ABC_STATIONS ST WHERE EQP.STATION_ID = ST.ID(+) AND " & _
            //" EQP.LINE_ID = " & lngTOLineupID & " AND EQP.STATION_ID IS NOT NULL)"

            var res = await (from EQP in _blendMonitorContext.AbcCompLineupEqp
                             from ST in _blendMonitorContext.AbcStations
                             where EQP.StationId == ST.Id && EQP.LineId == lineupId && EQP.StationId != null
                             select new Tuple<double?, double?, double?>(EQP.StationId, ST.Max, ST.Min))
                   .FirstOrDefaultAsync<Tuple<double?, double?, double?>>();

            AbcBlendStations Data = new AbcBlendStations();
            Data.BlendId = blendId;
            Data.MatId = matId;
            Data.StationId = (double)res.Item1;
            Data.InUseFlag = "YES";
            Data.MaxFlow = res.Item2;
            Data.MinFlow = res.Item3;

            await _blendMonitorContext.AbcBlendStations.AddAsync(Data);

            return await _blendMonitorContext.SaveChangesAsync();
        }
        public async Task<int> DeleteBlendStations(double blendId, double matId, double stationId)
        {
            //"DELETE ABC_BLEND_STATIONS WHERE BLEND_ID = "
            //+ (curblend.lngID + (" AND MAT_ID = "
            //+ (lngMatId + (" AND STATION_ID=" + lngFromStationId)))));
            AbcBlendStations Data = await _blendMonitorContext.AbcBlendStations
                                        .Where<AbcBlendStations>(row => row.BlendId == blendId
                                        && row.MatId == matId && row.StationId == stationId)
                                        .FirstOrDefaultAsync<AbcBlendStations>();
            if (Data != null)
            {
                _blendMonitorContext.AbcBlendStations.Remove(Data);
                return await _blendMonitorContext.SaveChangesAsync();
            }
            return 0;
        }
        public async Task<int> DeleteBlendStation2(double blendId, double matId)
        {
            //"DELETE ABC_BLEND_STATIONS WHERE BLEND_ID = "
            //(curblend.lngID + (" AND MAT_ID = " + lngMatId)));

            AbcBlendStations Data = await _blendMonitorContext.AbcBlendStations
                                        .Where<AbcBlendStations>(row => row.BlendId == blendId
                                        && row.MatId == matId)
                                        .FirstOrDefaultAsync<AbcBlendStations>();
            if (Data != null)
            {
                _blendMonitorContext.AbcBlendStations.Remove(Data);
                return await _blendMonitorContext.SaveChangesAsync();
            }
            return 0;
        }
        public async Task<List<AbcBlendStations>> GetBlStations(double blendId, double matId)
        {
            //select station_id, min_flow, max_flow 
            //from abc_blend_stations 
            //where blend_id =? and mat_id =? and in_use_flag = 'YES' order by min_flow asc

            return await _blendMonitorContext.AbcBlendStations
                        .Where<AbcBlendStations>(row => row.BlendId == blendId && row.MatId == matId && row.InUseFlag == "YES")
                        .Select(row => new AbcBlendStations
                        {
                            StationId = row.StationId,
                            MinFlow = row.MinFlow,
                            MaxFlow = row.MaxFlow
                        }).ToListAsync<AbcBlendStations>();
        }

        public async Task<int> SetBlendStationsData(double blendId, double matId, double stationId, double? setpoint)
        {
            //"UPDATE abc_blend_stations SET cur_setpoint=" & _
            //arStationRcps(intI) & " WHERE blend_id=" & curblend.lngID & _
            //" AND mat_id=" & lngMatId & " AND station_id= " & arStationsIds(intI)

            AbcBlendStations Data = await _blendMonitorContext.AbcBlendStations
                                        .Where<AbcBlendStations>(row => row.BlendId == blendId && row.MatId == matId && row.StationId == stationId)
                                        .FirstOrDefaultAsync<AbcBlendStations>();
            Data.CurSetpoint = setpoint;
            return await _blendMonitorContext.SaveChangesAsync();

        }
        public async Task<double> GetCompEqpOrder(double lineupId, double stationId)
        {
            //select line_eqp_order from abc_comp_lineup_eqp
            //where line_id =? and station_id =?
            double? data = await _blendMonitorContext.AbcCompLineupEqp
                        .Where<AbcCompLineupEqp>(row => row.LineId == lineupId && row.StationId == stationId)
                        .Select(row => row.LineEqpOrder)
                        .FirstOrDefaultAsync<double>();

            if (data == null)
            {
                return 1;
            }
            return (double)data;
        }        

        public async Task<string> GetPrgRunState(string app)
        {
            string outp = "";
            return await Task.FromResult("RUNNING");
        }

        public async Task<bool> TPCreateLabPkg(double? vntTransLineId, bool strNoError)
        {
            return await Task.FromResult(true);
        }

        public async Task<bool> TPUpdateDefvalPkg(double? vntTransLineId, bool strNoError)
        {
            return await Task.FromResult(true);
        }

        public async Task<bool> TPCreatePkg(double? vntTransLineId, bool strNoError)
        {
            return await Task.FromResult(true);
        }

        public async Task<string> BOCopyPkg(double blendId, string name, string copyOk)
        {
            return await Task.FromResult("YES");
        }

        public async Task<string> GetCalcRoutine(double blend, string text)
        {
            return await Task.FromResult("YES");
        }

        public  async Task<(string, double?, double?, double?, double?, string)> GetTankData(int tankId)
        {
            return await Task.FromResult(("Name",2,2,2,2,"YES"));
        }

        public  async Task<string> GetDestTankData(double blendId, int tankId, double vntHeelVol)
        {
            return await Task.FromResult("YES");
        }

        public  async Task<double> GetSelTankProp(double tankId, int etohEtohPropId)
        {
            return await Task.FromResult(0);
        }

        public  async Task<double> GetBlendInterval(double blendId, DateTime currentDateTime)
        {
            return await Task.FromResult(0);
        }

        public  async Task<DateTime> GetLastRunTime(string programeName)
        {
            return await Task.FromResult(DateTime.Now);
        }

        public  async Task<DateTime> GetCurTime()
        {
            return await Task.FromResult(DateTime.Now);
        }

        public  async Task<(string, double, DateTime, string, string, string, string)> GetTagValAndFlags(double? RBC_WDOG_TID, string vntDummy,
            double vntTagVal, DateTime? vntTagValTime, string vntTagValQlt, string readEnabled, string scanEnabled, string vntScanRateName)
        {
            return await Task.FromResult(("name",0,DateTime.Now,"GOOD","YES","YES","name"));
        }

        public async Task<string> LogMessage(int msgID, string prgm1, string gnrlText, string prgm2, string prgm3, string prgm4, string prgm5,
           string prgm6, string prgm7, string res)
        {
            return await Task.FromResult("");
        }
    }
}
