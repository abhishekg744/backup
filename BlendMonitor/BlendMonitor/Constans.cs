using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor
{
    public class Constans
    {
        public const string cstrDblFmt = "0.#####";       // string format for double values
        public const string cstrPctFmt = "#0.0#####";    // string format for percentage
        public const string cstrIDFmt = "00000";        // string format for ID inserted in logged messages
        public const string cstrDebug = "DEBUG";        // string used for message reference
        public const string cstrGen = "GENERAL";        // string used for message reference
        public const string cstrSys = "SYSTEM";         // string used for message reference
        public const double cDblEp = 0.0000000001d;      // 1E-10, small number used to test the "= 0" condition
        public static DateTime cdteNull = DateTime.Parse("1900-01-01");
        public const string cstrModelDir = "\\Model\\";
        public const string cstrInputDir = "\\Input\\";
        public const string cstrOutputDir = "\\Output\\";
        public const string cstrErrorDir = "\\Error";

        public enum DebugLevels
        {
            Low = 1,
            Medium,
            High
        }
        public enum StartStop
        {

            STRT,

            STP,
        }

        public enum msgTmpltIDs
        {
            // Symbol  ID    SV Message template text
            // ------  --    -- ---------------------
            DBUG1 = 1400,  // 1 FIRST BLENDER HEADER STARTED AT ^1
            DBUG2 = 1401,  // 1 PROGRAM_ERROR ^1 SET ON BLENDER ^2
            DBUG3 = 1402,  // 1 PROCESSING BLEND ^1: RBC_MODE_FLAG = ^2
            DBUG4 = 1403,  // 1 PROCESSING BLEND ^1: **** ENTERING ^2 ****
            DBUG5 = 1404,  // 1 PROCESSING BLEND ^1: PREV_BLEND_CMD = ^2
            DBUG6 = 1405,  // 1 PROCESSING BLEND ^1: DCS_CMD_TIME = ^2, START_TIMEOUT = ^3
            DBUG7 = 1406,  // 1 PROCESSING BLEND ^1: ALLOW_START_AND_STOP_FLAG = ^2
            DBUG8 = 1407,  // 1 PROCESSING BLEND ^1: BLEND NAME ^2 DOWNLOADED TO BLEND_ID_TID TAG ^3
            DBUG9 = 1408,  // 1 PROCESSING BLEND ^1: PRODUCT GRADE "^2" DOWNLOADED TO BLEND_ID_TID TAG ^3
            DBUG10 = 1409, // 1 PROCESSING BLEND ^1: TARGET_VOL ^2 DOWNLOADED TO TARGET_VOL_TID TAG ^3
            DBUG11 = 1410, // 1 PROCESSING BLEND ^1: TARGET_RATE ^2 DOWNLOADED TO TARGET_RATE_TID TAG ^3
            DBUG12 = 1411, // 1 PROCESSING BLEND ^1: CUR_RECIPE ^2 FOR COMP ^3 DOWNLOADED TO RECIPE_SP_TID TAG ^4
            DBUG13 = 1412, // 1 PROCESSING BLEND ^1: DEST TANK ^2, CUR VOL = ^3, AVAIL VOL = ^4, MIN VOL = ^5
            DBUG14 = 1413, // 1 PROCESSING BLEND ^1: DEST TANK ^2, PROP ^3, HEEL_VALUE = ^4
            DBUG15 = 1414, // 1 PROCESSING BLEND ^1: TARGET_RATE = ^2, TARGET_VOL = ^3, ALLOW_RATE_AND_VOL_UPDS_FLAG = ^4
            DBUG16 = 1415, // 1 PROCESSING BLEND ^1: LOCAL_GLOBAL_FLAG = ^2
            DBUG17 = 1416, // 1 PROCESSING BLEND ^1: INTV LEN = ^2 MIN, CUR INTV NUM = ^3
            DBUG18 = 1417, // 1 PROCESSING BLEND ^1: COMP ^2, COMP_DELTA_VOL = ^3, CUR_VOL = ^4, PREV_VOL = ^5
            DBUG19 = 1418, // 1 PROCESSING BLEND ^1: COMP ^2, ACTUAL RECIPE = ^3
            DBUG20 = 1419, // 1 PROCESSING BLEND ^1: CUR INTV NUM ^2, NEW_VOL = ^3, NEW INTV_VOL = ^4
            DBUG21 = 1420, // 1 PROCESSING BLEND ^1: COMP ^2, NEW COMP_INTV_VOL = ^3, NEW COMP_BLEND_VOL = ^4
            DBUG22 = 1421, // 1 PROCESSING BLEND ^1: CUR_BLEND_VOL = ^2, COMP_TOT_VOL = ^3
            DBUG23 = 1422, // 1 PROCESSING BLEND ^1: COMP ^2, AVG_BLEND_RECIPE = ^3, INTV_ACTUAL_RECIPE = ^4
            DBUG24 = 1423, // 1 PROCESSING BLEND ^1: TOT_COMP_INTV_VOL = ^2, INTV_COST = ^3, BLEND_COST = ^4
            DBUG25 = 1424, // 1 PROCESSING BLEND ^1: RBC_STATE_TID = ^2, DCS_STATE = ^3
            DBUG26 = 1425, // 1 PROCESSING BLEND ^1: BLEND_STATE SET = ^2
            DBUG27 = 1426, // 1 PROCESSING BLEND ^1: INTV_LEN = ^2, MIN_INTV_LEN = ^3, MAX_INTV_LEN = ^4 MIN
            DBUG28 = 1427, // 1 PROCESSING BLEND ^1: TARGET_RATE = ^2, MIN_RATE = ^3, MAX_RATE = ^4
            DBUG29 = 1428, // 1 PROCESSING BLEND ^1: TARGET_VOL = ^2, MIN_VOL = ^3, MAX_VOL = ^4
            DBUG30 = 1429, // 1 PROCESSING BLEND ^1: CHECK_BLEND_DATA ERROR_FLAG = ^2
            DBUG31 = 1430, // 1 PROCESSING BLEND ^1: CYCLE_TIME FOR ABC TANK MONITOR = ^2 MIN, OPTIMIZATION_MONITOR = ^3 MIN, ABC BLEND MONITOR = ^4 MIN
            DBUG32 = 1431, // 1 PROCESSING BLEND ^1: DEST TANK ^2, PROP ^3, VALUE = ^4, ABS_MIN = ^5, ABS_MAX = ^6
            DBUG33 = 1432, // 1 PROCESSING BLEND ^1: CUR_RECIPE = ^2, REQ_VOL = ^3 FOR COMP ^4, TARGET_VOL = ^5
            DBUG34 = 1433, // 1 PROCESSING BLEND ^1: DEST TANK ^2, AVAIL VOL = ^3, MAX_TANK_VOL = ^4, MAX_PROD_VOL = ^5
            DBUG35 = 1434, // 1 PROCESSING BLEND ^1: PROP ^2, INTV_PROP_BIAS_NEW = ^3, INTV_PROP_BIAS  = ^4
            DBUG36 = 1435, // 1 PROCESSING BLEND ^1: PROP ^2, ANZ_RES = ^3, INTV_UNBIASED_PROP = ^4, INTV_PROP_BIAS_NEW = ^5, INTV_PROP_BIAS_CUR = ^6
            DBUG37 = 1436, // 1 PROCESSING BLEND ^1: PROP ^2, INTV_PROP_BIAS = ^3, MIN_BIAS = ^4, MAX_BIAS = ^5
            DBUG38 = 1437, // 1 PROCESSING BLEND ^1: PROP ^2, MODEL_ERR_EXIST_FLAG = ^3, MODEL_ERR_CLRD_FLAG = ^4
            DBUG39 = 1504, // 1 PROCESSING BLEND ^1: BLEND = ^2, PENDING_STATE = ^3, BLEND_STATE = ^4
            DBUG40 = 1505, // 1 PROCESSING BLEND ^1: PROP ^2, BIAS_FILTER = ^3, INTV_PROP_BIAS = ^4
            DBUG41 = 6164, // 1 PROC. BLEND ^1: COMP ^2, STAT ^3, STAT VOL = ^4, VAL TIME = ^5
            WARN1 = 1438,  // 1 NULL DEFAULT RECIPE TOLERANCE, TAKEN AS 1.0
            WARN2 = 1439,  // 2 More than one ^1 blends found on blender ^2. ^3 has been Canceled
            WARN3 = 1440,  // 2 NULL INTV LEN FOR BLEND ^1, TAKEN AS ^2 MIN
            WARN4 = 1441,  // 3 BAD OR NULL TOTAL FLOW TAG ^1, PROCESSING ON BLENDER ^2 SKIPPED, ^3_BADFLOWTAG ERROR SET
            WARN5 = 1442,  // 2 UNEXPECTED PENDING STATE ^1 FOR BLEND ^2
            WARN6 = 1443,  // 2 NULL OR BAD RBC STATE TAG ^1 FOR BLENDER ^2
            WARN7 = 1444,  // 2 CMD ^1 NOT VALID FOR BLEND STATE ^2
            WARN8 = 1445,  // 2 CMD ^1 IGNORED, BLEND ALREADY IN REQUESTED STATE ^2
            WARN9 = 1446,  // 2 CMD ^1 HAS TIMED OUT, START_TIMEOUT = ^2 MIN
            WARN10 = 1447, // 2 RBC MODE TAG ^1 IS NO, ABC->DCS DOWNLOAD NOT PERMITTED ON BLENDER ^2
            WARN11 = 1448, // 1 ALLOW_START_AND_STOP_FLAG IS NO, CMD ^1 TO DCS NOT ALLOWED ON BLENDER ^1

            // this message is moved to the shared ErrorLog.bas because the ChkDcsComm()
            // function is moved to Shared.bas so it can be shared among background programs
            // WARN12 = 1449 '3 NO DCS COMMUNICATION, ^1_NODCS ERROR SET ON BLENDER ^2

            WARN13 = 1450, // 2 ^1_TID TAG MISSING FOR BLENDER ^2
            WARN14 = 1451, // 2 DOWNLOAD_OK TAG ^1 IS NO, BLEND ORDER DOWNLOAD ON BLENDER ^2 NOT PERMITTED BY DCS
            WARN15 = 1452, // 2 ACTIVE BLEND FOUND ON BLENDER ^1, NEW BLEND ORDER CAN NOT BE DOWNLOADED
            WARN16 = 1453, // 2 BLEND ORDER ON BLENDER ^1 CAN NOT BE DOWNLOADED DUE TO INVALID DATA
            WARN17 = 1454, // 3 ^1 TAG MISSING FOR BLENDER ^2, BLEND ORDER DOWNLOADING CANCELED
            WARN18 = 1455, // 3 ^1 TAG MISSING FOR COMP ^2, BLEND ORDER DOWNLOADING ON BLENDER ^3 CANCELED
            WARN19 = 1456, // 3 ^1 TAG MISSING FOR COMP TANK ^2, BLEND ORDER DOWNLOADING ON BLENDER ^3 CANCELED
            WARN20 = 1457, // 3 ^1 MISSING FOR COMP ^2, TANK ^3, BLEND ORDER DOWNLOADING ON BLENDER ^4 CANCELED
            WARN21 = 1458, // 1 PUMP ^1 ALREADY RUNNING
            WARN22 = 1459, // 1 PUMP ^1 NOT LISTED IN SERVICE IN ABC OR NOT IN AUTO MODE IN DCS.  DOWNLOADING CANCELED
            WARN23 = 1460, // 1 PUMP ^1 NOT IN ABC SERVICE OR NOT AUTO MODE.  COMMAND SELECTION IGNORED
            WARN24 = 1461, // 3 PROD LINEUP PRE/SELECTION_TID MISSING FOR DEST TANK ^1, BLEND ORDER DOWNLOADING ON BLENDER ^2 CANCELED
            WARN25 = 1462, // 2 NULL VALUE FOR PROP ^1, DEST TANK ^2, BLEND ORDER DOWNLOADING ON BLENDER ^3 CANCELED
            WARN26 = 1463, // 1 DEFAULT ^1 INTV LEN IS NULL, TAKEN AS ^2 MIN
            WARN27 = 1464, // 2 INTV LEN ^1 FOR BLEND ^2 < ALLOWED MIN, CLAMPED TO DEFAUT MIN ^3 MIN
            WARN28 = 1465, // 2 INTV LEN ^1 FOR BLEND ^2 > ALLOWED MAX, CLAMPED TO DEFAUT MAX ^3 MIN
            WARN29 = 1466, // 2 TARGET RATE ^1 FOR BLEND ^2 < ALLOWED MIN_RATE ^3
            WARN30 = 1467, // 2 TARGET RATE ^1 FOR BLEND ^2 > ALLOWED MAX_RATE ^3
            WARN31 = 1468, // 2 TARGET VOL ^1 FOR BLEND ^2 < ALLOWED MIN_VOL ^3
            WARN32 = 1469, // 2 TARGET VOL ^1 FOR BLEND ^2 > ALLOWED MAX_VOL ^3
            WARN33 = 1470, // 1 BAD OR NULL DCS_SERVICE TAG ^1 FOR TANK ^2.  CHECK DCS SERVICE STATUS OF THE TANK
            WARN34 = 1471, // 2 TANK ^1 OUT OF SERVICE IN ^2, BLEND ORDER DOWNLOADING ON BLENDER ^3 CANCELED
            WARN35 = 1472, // 2 COMP TANK ^1 ALREADY IN USE BY ANOTHER BLENDER
            WARN36 = 1473, // 2 STATION ^1 ALREADY IN USE BY ANOTHER BLENDER
            WARN37 = 1474, // 2 NULL CUR_RECIP FOR COMP ^1, BLEND ORDER DOWNLOADING ON BLENDER ^2 CANCELED
            WARN38 = 1475, // 2 REQUIRED VOL ^1 > AVAIL VOL ^2 FOR ^3(SOURCE/COMP) TANK ^4
            WARN39 = 1476, // 2 HEEL VOL ^1 FOR DEST TANK ^2 OUTSIDE VALID LIMITS OF TANK MIN VOL ^3 AND MAX VOL ^4. DOWNLOADING ON ^5(MANAGER/BLENDER) ^6 CANCELED
            WARN40 = 1477, // 1 NULL OR BAD ^1 TAG ^2 FOR DEST TANK ^3
            WARN41 = 1478, // 1 ^1 TAG ^2 READING DISABLED
            WARN42 = 1479, // 1 SCAN GROUP ^1 FOR ^2 TAG ^3 DISABLED
            WARN43 = 1480, // 3 NULL OR BAD ^1_VOL_TID TAG ^2 FOR DEST TANK ^3, HEEL VOL TAKEN AS ^4
            WARN44 = 1481, // 2 CYCLE TIME ^1 MIN FOR ^2 < ^3 MIN FOR ^4
            WARN45 = 1482, // 2 PROP ^1 VALUE ^2 FOR DEST TANK ^3 < ABS_MIN, CLAMPED TO ABC_MIN = ^4
            WARN46 = 1483, // 2 PROP ^1 VALUE ^2 FOR DEST TANK ^3 > ABS_MAX, CLAMPED TO ABC_MAX = ^4
            WARN47 = 1484, // 2 NULL ^1 FOR PROP ^2, DEST TANK ^3, PROP VALUE CHECK SKIPPED
            WARN48 = 1485, // 2 CONTROLLED PROP ^1 OUTSIDE VALID LIMITS, BLEND ORDER DOWNLOADING ON BLENDER ^2 CANCELED
            WARN49 = 1486, // 2 NULL CONTROLLED FLAG FOR PROP ^1, DEST TANK ^2, ASSUMED TO BE NO
            WARN50 = 1487, // 2 TARGET VOL ^1 FOR BLEND ^2 > MAX PROD VOL ^3 FOR DEST TANK ^4
            WARN51 = 1488, // 2 TOTAL VOL ^1 FOR BLEND ^2 > TARGET VOL ^3
            WARN52 = 1489, // 1 INTV ACTUAL RECIPE ^1 FOR COMP ^2 NOT MATCHING SETPOINT ^3, IN INTV # ^4
            WARN53 = 1490, // 2 NO GOOD AND SELECTED PROP DATA FOUND FOR COMP ^1, PROP ^2, DEFAULT PROP VALUE USED
            WARN54 = 1491, // 2 TOTALIZER VOL TAG MISSING FOR COMP ^1, BLEND CALC ON BLENDER ^2 SKIPPED, BMON_MISSINGTAG ERROR SET
            WARN55 = 1492, // 1 TOTALIZER VOL ^1 FOR COMP ^2 UNCHANGED ON BLENDER ^3
            WARN56 = 1493, // 2 NULL CUR_RECIPE FOR COMP ^1, BLEND CALC ON BLENDER ^2 SKIPPED, BMON_BADVOLUME ERROR SET
            WARN57 = 1494, // 2 TOTALIZER VOL ^1 < PREV VOL ^2 FOR COMP ^3 ON BLENDER ^4, RECALCULATED WITH APPROXIMATION
            WARN58 = 1495, // 2 BAD TOTALIZER VOL TAG ^1 FOR COMP ^2 ON BLENDER ^3, RECALCULATED WITH APPROXIMATION
            WARN59 = 1496, // 2 NULL MATERIAL COST FOR COMP ^1 FOR BLEND ^2, ASSUMED TO BE 0
            WARN60 = 1497, // 2 COMP TOT VOL ^1 AND CUR BLEND VOL ^2 FOR BLEND ^3 DIFFER MORE THAN DEFAULT VOL TOLERANCE ^4
            WARN61 = 1498, // 3 ^1 NOT RESPONDING ON ^1 (MANAGER/BLENDER)
            WARN62 = 1499, // 3 RBC ACTUAL TARGET VOL ^1 AND ABC TARGET VOL ^2 DIFFER MORE THAN DEFAULT VOL TOLERANCE ^3 FOR BLEND ^4
            WARN63 = 1500, // 1 DCS TOTAL VOL ^1 AND ABC TOTAL VOL ^2 DIFFER MORE THAN DEFAULT VOL TOLERANCE ^3 FOR BLEND ^4
            WARN64 = 1501, // 3 COMP TANK ^1 REQUESTED BY ABC NOT THE SAME AS USED IN DCS
            WARN65 = 1502, // 3 DEST TANK ^1 REQUESTED BY ABC NOT THE SAME AS USED IN DCS
            WARN66 = 1503, // 2 DEST TANK ^1 ALREADY IN USE BY ANOTHER BLENDER
            WARN67 = 1506, // 1 BAD OR NULL AVAIL_VOL_TID TAG ^1 FOR COMP TANK ^2, COMP ^3, VOL CHECK FOR THIS TANK SKIPPED
            WARN68 = 1507, // 1 NO GOOD PROPS FOUND FOR DEST TANK ^1, BLEND ORDER DOWNLOADING ON BLENDER ^2 CANCELED
            WARN69 = 6140, // 1 DCS MODE TAG ^1 IS NOT REMOTE.  ^2 ORDER ^3 WAS RESET TO READY STATE
            WARN70 = 6200, // 1 A SWICTH FLAG HAS JUST OCURRED IN THE DCS
            WARN71 = 6320, // 1 RUNDOWN BLEND ^1 SET TO ^2.  MORE THAN ONE READY BLEND FOUND FOR RUNDOWN. BLEND ^3 IS BEING DOWNLOADED.
            WARN72 = 1380, // 1 BLEND ^1 DUPLICATED FROM ^2 BY ^3
            WARN73 = 6321, // 1 MORE THAN ONE TANK FOUND IN ABC_BLEND_SWINGS AT THE END OF BLEND ^1. TANK ^2 TAKEN AS NEW DESTINATION TANK
            WARN74 = 6340, // 2 BLEND ^1 IS NOT APPROVED AND IS SET BACK TO PARTIAL. DOWNLOAD MANUALLY
            WARN75 = 6380, // 2 SWING TIME OUT. SWING WAS NOT PERFORMED IN (BLEND)^1
            WARN76 = 6400, // 2 SWING CRITERIA IS MET ON BLEND ^1. AUTO SWING FLAG OR ALLOW_START_STOP FLAG ARE OFF. PERFORM SWING IN DCS
            WARN77 = 6480, // 3 Post Blend Destination tank could not be created for blend ^1.  Create it manually as soon as possible!
            WARN78 = 6500, // 3 Please reset the COMM ERRor state of the blend ^1
            WARN79 = 6540, // 1 Swing Ocurred from tank ^1 to tank ^2 for blend ^3
            WARN80 = 6580, // 2 Null cur_recipe in the station ^1 used for component ^2. Download canceled
            WARN81 = 6581, // 2 ^1 IS NULL IN ^2 FOR COMP ^3 .  ^4 NOT SELECTED FOR STATION ^5
            WARN82 = 6680, // 2 BLEND ^1 COULD NOT BE AUTOMATICALLY DUPLICATED.
            WARN83 = 6720, // 2 ^1 FLAG IS NOT ON FOR BLEND ^2 ON BLENDER ^3. OPTIMIZER WILL ^4
            WARN84 = 6721, // 2 TRANSFER LINE VOL ^1 FOR BLEND ^2 > MAX AVAILABLE SPACE ^3 FOR DEST TANK ^4
            WARN85 = 6722, // 2 BLEND ^1 IN PAUSED/STOPPED STATE BEFORE LINEFILL WAS FLUSHED. TQI CALCULATIONS COULD BE AFFECTED
            WARN86 = 6724, // 1 Product Swing has happened on blender ^1. Blend ^2 has been set to DONE. Download a new blend order to DCS
            WARN87 = 6725, // 2 Blender on DCS is in PAUSED state. Equip in blend ^1 is not equal to Equip in previous Blend ^2.  Downloading Calceled.
            WARN88 = 6740, // 2 Analyzer ^1 on blender ^2 is in DCS service but not in ABC Service.  Analyzer Properties values will not be used!
            WARN89 = 6141, // 2 Totalizer Volume scan times are not synchronized in scan group ^1.
            WARN90 = 6851, // 1 ^1 ^2 NAME TAG VALUE ^3 DOES NOT MATCH THE ABC NAME ^4.  CURRENT ^2 WAS SET COMM ERR
            WARN91 = 6852, // 1 Volume conversion factor does not exist from additive units ^1 to blend volume units ^2
            WARN92 = 6182, // 1 CMD ^1 ISSUED IN BLEND ^2 BY ^3
            WARN93 = 6184, // 1 BLEND ^1 CHANGED STATE FROM ^2 TO ^3
            WARN94 = 6881, // 1 BLEND ^1: TOTAL COMPONENT INTERVAL VOLUME IS ZERO FOR INTERVAL ^2.  LINEPROP CALC SKIPPED
            WARN95 = 6905, // 1 BLEND ^1, PROP ^2: INTERVAL BIAS ^2 EXCEEDS THE MODEL ERROR THRESHOLD ^3. CURRENT BIAS CLAMPED TO MIN BIAS ^4
            WARN96 = 6906, // 2 IN BLEND ^1, COMP ^2, DCS LINEUP NUM IS NULL FOR LINEUP ^3.  CMD SEL/PRESEL IGNORED
            WARN97 = 2048, // 2 TANK INDEX IS NULL IN ^1 TABLE. TANK ^2 WILL NOT BE SELECTED IN DCS
            WARN98 = 6907, // 2 IN BLEND ^1, DEST ^2, PROD DCS LINEUP NUM IS NULL FOR LINEUP ^2.  CMD SEL/PRESEL IGNORED
            WARN99 = 2054, // 2 DCS PUMP ID FOR PUMP ^1 NOT CONFIGURED.  COMMAND SELECTION IGNORED
            WARN100 = 6912, // 2 ^1 SAMPLE ^2 COULD NOT BE ALLOCATED WITHIN THE INTVL RANGE OF BLEND ^3. BLEND SAMPLE DATA IGNORED
            WARN101 = 6913, // 2 START INTV>STOP INTV FOR ^1 SAMPLE ^2 IN BLEND ^3. BLEND SAMPLE DATA IGNORED
            WARN102 = 6915, // 2 IN BLEND ^1 & INTERVAL ^2, THE FOLLOWING PROPS CHANGED CURR BIAS CALC TYPE TO DEFAULT BIAS TYPE: ^3
            WARN103 = 6920, // 1 ^1 SAMPLE ^2 HAS BEEN USED IN BLEND ^3 FOR INTERVALS ^4 TO ^5
            WARN104 = 6925, // 1 IN BLEND ^1, BIAS OVERRIDE FLAG HAS BEEN PROCESSED
            WARN105 = 6950, // 1 WARNING: IN BLEND ^1 - SAMPLE ^2 TAKEN IN OPEN INTERVAL (^3)
                            // RW 14-Oct-16 Gasoline Ethanol blending
            WARN106 = 7001, // 3 BLEND ^1 PROP ^2 CANNOT BE CALCULATED AS PROP ^3 IS NOT IN PRODUCT SPECS. ^4'
            WARN107 = 7010, // 3 BLEND ^1 HAS FGE COMPONENT BUT REQUIRED DENATURANT PROPS ARE NOT CONFIGURED. ^4'
                            // --- RW 25-Jan-17 Gasoline Ethanol blending remedial ---
                            // WARN108 = 7005 '2 BLEND ^1 HAS FGE COMPONENT BUT PRODUCT SPEC HAS NO ETOH PROPERTY, BYPASSING ETHANOL BLENDING'
                            // WARN109 = 7006 '2 BLEND ^1 HAS COMPONENT WITH ETOH PROPERTY > MIN_ETOH BUT PRODUCT SPEC HAS NO ETOH PROPERTY, BYPASSING ETHANOL BLENDING'
                            // WARN110 = 7000 '2 BLEND ^1 NO COMPONENTS OR TANK HEEL HAVE ETOH >= MIN_ETOH, BYPASSING ETHANOL BLENDING
            WARN108 = 7005, // 2 BLEND ^1 HAS FGE COMPONENT BUT PRODUCT SPEC HAS NO ^2 PROPERTY, BYPASSING ETHANOL BLENDING'
            WARN109 = 7006, // 2 BLEND ^1 HAS ^2 WITH ^3 PROPERTY > MIN_ETOH BUT PRODUCT SPEC HAS NO ^4 PROPERTY, BYPASSING ETHANOL BLENDING'
            WARN110 = 7000, // 2 BLEND ^1 NO COMPONENTS OR TANK HEEL HAVE ETOH_ETOH >= MIN_ETOH, BYPASSING ETHANOL BLENDING
            WARN111 = 7014, // 2 BLEND ^1 PROPERTY ^2 NOT CONFIGURED, BYPASSING ETHANOL BLENDING
                            // --- RW 25-Jan-17 Gasoline Ethanol blending remedial ---
                            // --- RW 02-Mar-17 Gasoline Ethanol blending remedial ---
            WARN112 = 7007, // 2 BLEND ^1 PROP ^2: ABS CORRECTION ^3 EXCEEDS MODEL ERROR THRESHOLD ^4, CORRECTION CLAMPED TO ^5
            WARN113 = 7008, // 2 BLEND ^1 PROP ^2: CORRECTION ^3 ^4 BIAS ^5, CORRECTION CLAMPED TO ^6
                            // --- RW 02-Mar-17 Gasoline Ethanol blending remedial ---
                            // RW 14-Oct-16 Gasoline Ethanol blending
            WARN114 = 7013, // 2 ^1 COEFFICIENTS FOR PROP ^2 IN BLEND ^3 NOT FOUND/INCOMPLETE, TAKEN AS DEFAULT
        }

        public enum CommonMsgTmpIDs
        {

            // Symbol   ID     Message Template
            COM_D1 = 1144,

            // ******* THIS IS THE END OF PROCESSING CYCLE AT ^1 *******
            COM_D2 = 1145,

            // ******* THIS IS THE END OF PROCESSING CYCLE AT ^1, CYCLE TIME IS ^2 M, SLEEP TIME IS ^3 M *******
            COM_D3 = 1163,

            // COMPAR LAST_RUN_TIME: ^1 - ^2, ^3 - ^4
            COM_D4 = 1170,

            // ******* THIS IS THE START OF PROCESSING CYCLE AT ^1 *******
            COM_D5 = 1171,

            // PROGRAM DEBUG FLAG IS ^1, DEBUG LEVEL IS ^2
            COM_D6 = 1172,

            // DEBUG FLAG IS ^1, DEBUG LEVEL IS ^2 FOR BLENDER ^3
            COM_W1 = 1174,

            // ^1 MAY BE NOT RUNNING
            COM_W2 = 1179,

            // NO NEW RESULTS FROM ^1
            COM_W3 = 1288,

            // NO SOURCE ID FOUND FOR "^1", "^2" SET FOR ALL CURRENTLY ACTIVE BLENDERS
            COM_W4 = 1449,

            // NO DCS COMMUNICATION, ^1_NODCS ERROR SET ON BLENDER ^2
            SYSERR = 1161,
        }

        public enum GoodBad
        {
            BAD,
            GOOD
        }

        public enum RetStatus
        {
            SUCCESS = 0,
            FAILURE = -1
        }

        public enum YesNo
        {
            NO,
            YES
        }

        public enum OnOff
        {
            OFF,
            ON_
        }

        public enum BlendCmds
        {
            START,
            STOP_,
            PAUSE,
            RESTART,
            DOWNLOAD,
            SWING  
        }

        public enum ValidInvalid {
            invalid,
            valid,
        }
        public enum GAMSCalcTypes
        {

            INTERVL,

            // Intervl
            AVERAGE,

            LINEPROP,

            OPTIMIZE,
        }
    }
}
