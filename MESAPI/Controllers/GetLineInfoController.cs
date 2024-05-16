using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MESAPI.Models;
using System.Web.Http.Cors;
using System.Reflection;

namespace MESAPI.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class GetLineInfoController : ApiController
    {
        string connectionReport = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THBPODSMRACDB)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = THSFDB)));Password=tmudk$o0;User ID=PQM_QUERY";

        [System.Web.Http.HttpGet()]
        [Route("api/line/information/{LINE_NAME}")]
        ///api/line/information/{Line_Name}
        public IHttpActionResult GetLineInfo(string LINE_NAME)
        {
            //matlist.Add(m2);
            if (LINE_NAME == null)
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string line = LINE_NAME.Trim();
            MachineLineInfoModel _lDataAll = new MachineLineInfoModel();
            #region Current MO & MODEL
            string sqlMoModel = "select A.SFCS_MO as CURRENT_MO,A.SFCS_MODEL as CURRENT_MODEL from det_cndc.R_SCHEDULE_SAP_T A " +
                " INNER JOIN DET_CNDC.R_SCHEDULE_SFCS_T B ON A.SCHL_NO = B.SCHL_NO " +
                " WHERE NVL(B.START_DATE,TO_DATE(TO_CHAR(A.PLN_STAT_DATE, 'YYYY/MM/DD') || ' ' ||A.PLN_STAT_TIME,'YYYY/MM/DD HH24:MI:SS')) BETWEEN SYSDATE -30 AND SYSDATE + 7 " +
                " AND B.FLAG = '1' AND B.LINE_NAME = '"+ line + "'";
            DataTable dtMoModel = QueryDataTable(sqlMoModel, connectionReport);
            if(dtMoModel.Rows.Count>0)
            {
                _lDataAll.CURRENT_MO = dtMoModel.Rows[0]["CURRENT_MO"].ToString();
                _lDataAll.CURRENT_MODEL = dtMoModel.Rows[0]["CURRENT_MODEL"].ToString();
            }
            #endregion
            #region UTI DATA
            //string sqlUti = " select t.line_name,bas.equipment_code,CAST(T.EQUIPMENT_ID AS VARCHAR2(2000)) as EQUIPMENT_ID , " +
            //    " round(nvl(t.uti,0)*100,2) as UTI_TODAY, round((nvl(t2.GREEN_LIGHT_TIME,0) / 1440)*100, 2)as UTI_YESTERDAY " +
            //    "  from det_cndc.r_equip_active_daily_shift_t t " +
            //    "  inner join det_cndc.r_equip_active_daily_t t2 " +
            //    "  on t.equipment_id = t2.equipment_id " +
            //    "  inner join det_cndc.C_EQUIPMENT_BASIC_EDMS_T bas " +
            //    "  on bas.equipment_id = t.equipment_id " +
            //    "  and t.line_name = t2.line_name " +
            //    "  inner join DET_CNDC.c_line_desc_t A " +
            //    "  on t.line_name = A.line_name " +
            //    " where A.line_name = '"+line+"' " +
            //    " and A.VALID_FLAG = 'Y' " +
            //    " and t.work_date = to_char(sysdate, 'yyyymmdd') " +
            //    " and t2.work_date = to_char(sysdate-1, 'yyyymmdd') order by t.line_name ";
            string sqlUti = " with DimRoute as (  " +
                    "  SELECT DISTINCT S.LINE_NAME,C.EQUIPMENT_ID,S.PQM_NAME_EN as STATION_NAME_EN, " +
                    "  S.PQM_NAME_CN as STATION_NAME_CN,S.GROUP_NAME,C.STATION_NAME,C.STATION_NUMBER, " +
                    "  S.PROCESS_TYPE,S.STATION_SEQUENCE STATION_IDX  FROM  " +
                    "  (       " +
                    "  SELECT B.MO_NUMBER, B.MODEL_NAME, B.ROUTE_CODE, 1 AS LIST_SEQ   " +
                    "  FROM DET_CNDC.R_MO_OVERALL_V B      " +
                    "  UNION     " +
                    "  SELECT L.MO_NUMBER, L.MODEL_NAME, L.ROUTE_CODE, L.LIST_SEQ    " +
                    "  FROM DET_CNDC.R_MO_ROUTE_LIST_T L   " +
                    "  ) M    " +
                    "  INNER JOIN DET_CNDC.C_ROUTE_NAME_V2_T N   " +
                    "  ON M.ROUTE_CODE = N.ROUTE_CODE  " +
                    "  INNER JOIN DET_CNDC.C_PP_ROUTE_ELEMENT_T E   " +
                    "  ON N.ROUTE_NAME = E.DEFAULT_ROUTE_NAME  " +
                    "  AND N.ROUTE_VERSION = E.PPVERSION  " +
                    "  INNER JOIN  DET_CNDC.R_PP_STATION_T S  " +
                    "  ON E.DEFAULT_ROUTE_NAME = S.ROUTE_NAME  " +
                    "  AND E.PPNO = S.PPNO  AND E.PPVERSION = S.PPVERSION   " +
                    "  INNER JOIN " +
                    "  ( select * from DET_CNDC.C_STATION_CONFIG_T  where valid_flag='Y') C   " +
                    "  ON S.STATION_NUMBER = C.STATION_NUMBER   " +
                    "  INNER JOIN DET_CNDC.R_SCHEDULE_SFCS_T B   " +
                    "  on S.LINE_NAME = B.LINE_NAME   " +
                    "  INNER JOIN DET_CNDC.R_SCHEDULE_SAP_T A   " +
                    "  ON A.SCHL_NO = B.SCHL_NO  " +
                    "  WHERE NVL(B.START_DATE, TO_DATE(TO_CHAR(A.PLN_STAT_DATE, 'YYYY/MM/DD') || ' ' ||A.PLN_STAT_TIME,'YYYY/MM/DD HH24:MI:SS'))  " +
                    "  BETWEEN SYSDATE - 30  " +
                    "  AND SYSDATE + 7   " +
                    "  AND B.FLAG = '1'   " +
                    "  AND B.LINE_NAME = '"+line+"'  " +
                    "  AND M.MO_NUMBER =  A.SFCS_MO   " +
                    "  AND M.MODEL_NAME = A.SFCS_MODEL   " +
                    "  )  " +
                    "   select t.line_name,bas.equipment_code,CAST(T.EQUIPMENT_ID AS VARCHAR2(2000)) as EQUIPMENT_ID ,   " +
                    "   round(nvl(t.uti,0)*100,2) as UTI_TODAY, round((nvl(t2.GREEN_LIGHT_TIME,0) / 1440)*100, 2)as UTI_YESTERDAY   " +
                    "   , case when R.STATION_NAME_EN is null then 'N/A' else R.STATION_NAME_EN || '(' || R.GROUP_NAME || ')' end as MACHINE_NAME " +
                    "   from det_cndc.r_equip_active_daily_shift_t t   " +
                    "   inner join det_cndc.r_equip_active_daily_t t2   " +
                    "   on t.equipment_id = t2.equipment_id    " +
                    "   inner join det_cndc.C_EQUIPMENT_BASIC_EDMS_T bas  " +
                    "   on bas.equipment_id = t.equipment_id    " +
                    "   and t.line_name = t2.line_name    " +
                    "   inner join DET_CNDC.c_line_desc_t A    " +
                    "   on t.line_name = A.line_name " +
                    "   left join DimRoute R " +
                    "   on R.line_name = t.line_name " +
                    "   and R.equipment_id = bas.equipment_id " +
                    "   where A.line_name = '"+line+"'  " +
                    "   and A.VALID_FLAG = 'Y'  " +
                    "   and t.work_date = to_char(sysdate, 'yyyymmdd')  " +
                    "   and t2.work_date = to_char(sysdate-1, 'yyyymmdd')  " +
                    "   order by t.line_name ,bas.equipment_code ";
            DataTable dtUti = QueryDataTable(sqlUti, connectionReport);
            //List<MachineSubModel> _lDataAllSub = new List<MachineSubModel>();
            //MachineSubModel _DataAllSub = new MachineSubModel();
            List<MachineLineInfoUtiDetailModel> _lMachineLineInfoUtiDetailModel = new List<MachineLineInfoUtiDetailModel>();
            if (dtUti.Rows.Count > 0)
            {
                try
                {
                    var avgUtiLineName = (from row in dtUti.AsEnumerable()
                                      group row by new { LINE_NAME = row["LINE_NAME"].ToString() } into g
                                      select new
                                      {
                                          LINE_NAME = g.Key.LINE_NAME,
                                          UTI_TODAY = Math.Round(g.Average(x => x.Field<decimal>("UTI_TODAY")),2),
                                          UTI_YESTERDAY = Math.Round(g.Average(x => x.Field<decimal>("UTI_YESTERDAY")),2)
                                      }
                                      ).ToList();
                    var avgUtiEquip = dtUti.AsEnumerable()
                       .GroupBy(x => new { EQUIPMENT_CODE = x.Field<string>("EQUIPMENT_CODE"), MACHINE_NAME = x.Field<string>("MACHINE_NAME") })
                       .Select(x => new { EQUIPMENT_CODE = x.Key.EQUIPMENT_CODE, MACHINE_NAME = x.Key.MACHINE_NAME, UTI_TODAY = Math.Round(x.Average(y => y.Field<decimal>("UTI_TODAY")),2) })
                       .OrderBy(x => x.EQUIPMENT_CODE)
                       .ToList();
                    DataTable dtUti_Equip = LINQToDataTable(avgUtiEquip);
                    dtUti_Equip.Columns["UTI_TODAY"].ColumnName = "UTILIZATION";
                    _lMachineLineInfoUtiDetailModel = DataTableToList<MachineLineInfoUtiDetailModel>(dtUti_Equip);
                    _lDataAll.UTILIZATION_RATE_TODAY = avgUtiLineName[0].UTI_TODAY;
                    _lDataAll.UTILIZATION_RATE_YESTERDAY = avgUtiLineName[0].UTI_YESTERDAY;
                    _lDataAll.UTILIZATION_MACHINE = _lMachineLineInfoUtiDetailModel;
                }
                catch(Exception ex)
                { }
            }
            #endregion
            #region TBS Data
            //string sqlTbs = " select L1.LINE_NAME,bas.equipment_code " +
            //    " ,round(ceil((tbs.begin_point-tbs.last_end_point)*24*60*60)/60,2) TBS_TODAY " +
            //    "     from det_cndc.r_equip_tbs_t tbs " +
            //    "    inner join det_cndc.C_EQUIPMENT_BASIC_EDMS_T bas " +
            //    "    on tbs.EQUIPMENT_ID = bas.EQUIPMENT_ID " +
            //    "    inner join DET_CNDC.C_LINE_STATION_T  L1 " +
            //    "    on bas.EQUIPMENT_ID = L1.EQUIPMENT_ID " +
            //    "    inner join DET_CNDC.c_line_desc_t L " +
            //    "    on L.LINE_NAME = L1.LINE_NAME " +
            //    "    and L.VALID_FLAG= 'Y' " +
            //    " where L.LINE_NAME = '" + line + "' " +
            //    " and tbs.END_POINT is not null " +
            //    " and trunc(tbs.END_POINT) = trunc(sysdate) ";
            string sqlTbs = " with DimRoute as (  " +
                    "  SELECT DISTINCT S.LINE_NAME,C.EQUIPMENT_ID,S.PQM_NAME_EN as STATION_NAME_EN, " +
                    "  S.PQM_NAME_CN as STATION_NAME_CN,S.GROUP_NAME,C.STATION_NAME,C.STATION_NUMBER, " +
                    "  S.PROCESS_TYPE,S.STATION_SEQUENCE STATION_IDX  FROM  " +
                    "  (       " +
                    "  SELECT B.MO_NUMBER, B.MODEL_NAME, B.ROUTE_CODE, 1 AS LIST_SEQ   " +
                    "  FROM DET_CNDC.R_MO_OVERALL_V B      " +
                    "  UNION     " +
                    "  SELECT L.MO_NUMBER, L.MODEL_NAME, L.ROUTE_CODE, L.LIST_SEQ    " +
                    "  FROM DET_CNDC.R_MO_ROUTE_LIST_T L   " +
                    "  ) M    " +
                    "  INNER JOIN DET_CNDC.C_ROUTE_NAME_V2_T N   " +
                    "  ON M.ROUTE_CODE = N.ROUTE_CODE  " +
                    "  INNER JOIN DET_CNDC.C_PP_ROUTE_ELEMENT_T E   " +
                    "  ON N.ROUTE_NAME = E.DEFAULT_ROUTE_NAME  " +
                    "  AND N.ROUTE_VERSION = E.PPVERSION  " +
                    "  INNER JOIN  DET_CNDC.R_PP_STATION_T S  " +
                    "  ON E.DEFAULT_ROUTE_NAME = S.ROUTE_NAME  " +
                    "  AND E.PPNO = S.PPNO  AND E.PPVERSION = S.PPVERSION   " +
                    "  INNER JOIN " +
                    "  ( select * from DET_CNDC.C_STATION_CONFIG_T  where valid_flag='Y') C   " +
                    "  ON S.STATION_NUMBER = C.STATION_NUMBER   " +
                    "  INNER JOIN DET_CNDC.R_SCHEDULE_SFCS_T B   " +
                    "  on S.LINE_NAME = B.LINE_NAME   " +
                    "  INNER JOIN DET_CNDC.R_SCHEDULE_SAP_T A   " +
                    "  ON A.SCHL_NO = B.SCHL_NO  " +
                    "  WHERE NVL(B.START_DATE, TO_DATE(TO_CHAR(A.PLN_STAT_DATE, 'YYYY/MM/DD') || ' ' ||A.PLN_STAT_TIME,'YYYY/MM/DD HH24:MI:SS'))  " +
                    "  BETWEEN SYSDATE - 30  " +
                    "  AND SYSDATE + 7   " +
                    "  AND B.FLAG = '1'   " +
                    "  AND B.LINE_NAME = '" + line + "'  " +
                    "  AND M.MO_NUMBER =  A.SFCS_MO   " +
                    "  AND M.MODEL_NAME = A.SFCS_MODEL   " +
                    "  )  " +
                    " select L1.LINE_NAME,bas.equipment_code " +
                    " ,round(ceil((tbs.begin_point-tbs.last_end_point)*24*60*60)/60,2) TBS_TODAY " +
                    " , case when R.STATION_NAME_EN is null then 'N/A' else R.STATION_NAME_EN || '(' || R.GROUP_NAME || ')' end as MACHINE_NAME " +
                    " from det_cndc.r_equip_tbs_t tbs " +
                    " inner join det_cndc.C_EQUIPMENT_BASIC_EDMS_T bas " +
                    " on tbs.EQUIPMENT_ID = bas.EQUIPMENT_ID " +
                    " inner join DET_CNDC.C_LINE_STATION_T  L1 " +
                    " on bas.EQUIPMENT_ID = L1.EQUIPMENT_ID " +
                    " inner join DET_CNDC.c_line_desc_t L " +
                    " on L.LINE_NAME = L1.LINE_NAME " +
                    " and L.VALID_FLAG= 'Y' " +
                    " left join DimRoute R " +
                    " on R.line_name = L.line_name " +
                    " and R.equipment_id = bas.equipment_id " +
                    " where L.LINE_NAME = '" + line + "' " +
                    " and tbs.END_POINT is not null " +
                    " and trunc(tbs.END_POINT) = trunc(sysdate) ";
            DataTable dtTbs = QueryDataTable(sqlTbs, connectionReport);
            List<MachineLineInfoTbsDetailModel> _lMachineLineInfoTbsDetailModel = new List<MachineLineInfoTbsDetailModel>();
            if (dtTbs.Rows.Count > 0)
            {
                try
                {
                    var avgTbsLineName = dtTbs.AsEnumerable()
                       .GroupBy(x => new { LINE_NAME = x.Field<string>("LINE_NAME") })
                       .Select(x => new { LINE_NAME = x.Key.LINE_NAME, TBS_TODAY = Math.Round(x.Average(y => y.Field<decimal>("TBS_TODAY")), 2) })
                       .ToList();
                    var avgTbsEquip = dtTbs.AsEnumerable()
                       .GroupBy(x => new { EQUIPMENT_CODE = x.Field<string>("EQUIPMENT_CODE"), MACHINE_NAME = x.Field<string>("MACHINE_NAME") })
                       .Select(x => new { EQUIPMENT_CODE = x.Key.EQUIPMENT_CODE, MACHINE_NAME = x.Key.MACHINE_NAME, TBS_TODAY = Math.Round(x.Average(y => y.Field<decimal>("TBS_TODAY")), 2) })
                       .OrderBy(x => x.EQUIPMENT_CODE)
                       .ToList();
                    DataTable dtTbs_Equip = LINQToDataTable(avgTbsEquip);
                    dtTbs_Equip.Columns["TBS_TODAY"].ColumnName = "TBS";
                    _lMachineLineInfoTbsDetailModel = DataTableToList<MachineLineInfoTbsDetailModel>(dtTbs_Equip);
                    _lDataAll.AVERAGE_TBS = avgTbsLineName[0].TBS_TODAY;
                    _lDataAll.AVERAGE_TBS_MACHINE = _lMachineLineInfoTbsDetailModel;
                }
                catch (Exception ex)
                { }
            }
            #endregion
            #region Automation Degree
            string sqlAutomation = "select A.LINE_NAME,A.AUTOMATION_LEVEL,A.EQUIP_COUNT,A.DL_ID_COUNT ,B.CALCULATEDAY " +
                " from DET_CNDC.R_AUTOMATION_LEVEL_REPORT_T A " +
                " inner join (select FACTORY, WORK_MONTH, count(*) as CALCULATEDAY from DET_CNDC.R_AUTOMATION_FACTORY_LEVEL_T where flag = 0 group by FACTORY,WORK_MONTH) B " +
                " on A.FACTORY = B.FACTORY and A.WORK_MONTH = B.WORK_MONTH where A.LINE_NAME = '" + line + "' " +
                " and A.WORK_DATE = (select MAX(WORK_DATE) from DET_CNDC.R_AUTOMATION_LEVEL_REPORT_T where LINE_NAME = '"+line+"')";
            DataTable dtAutomation = QueryDataTable(sqlAutomation, connectionReport);
            if(dtAutomation.Rows.Count>0)
            {
                _lDataAll.AUTOMATION_DEGREE = dtAutomation.Rows[0]["AUTOMATION_LEVEL"].ToString();
                _lDataAll.EQUIPMENT_COUNT = Convert.ToInt32(dtAutomation.Rows[0]["EQUIP_COUNT"].ToString());
                _lDataAll.DL_COUNT = Convert.ToInt32(dtAutomation.Rows[0]["DL_ID_COUNT"].ToString());
                _lDataAll.CALCULATE_DAY = Convert.ToInt32(dtAutomation.Rows[0]["CALCULATEDAY"].ToString());
            }
            #endregion
            #region STD_ACH, FPYR --, SAFE_RATE, ALERT_RATE, UTILIZATION
            //string sql1 = " with DimLine as ( " +
            //    "  select a.line_name, " +
            //    "            a.prod_area_id, " +
            //    "            a.line_order as  man_sequence, " +
            //    "            nvl(d.archiver_lowwarning,95) archiver_lowwarning, " +
            //    "            nvl(d.archiver_lowlimit,90) archiver_lowlimit, " +
            //    "            nvl(d.yieldr_lowwarning, 95) yieldr_lowwarning, " +
            //    "            nvl(d.yieldr_lowlimit, 90) yieldr_lowlimit, " +
            //    "            nvl(d.show_fpyr_in_pqoverview,'Y') show_fpyr_in_pqoverview, " +
            //    "            e.work_date, " +
            //    "            e.shift, " +
            //    "            e.section_from||'-'||e.section_to section_range, " +
            //    "            e.night_flag, " +
            //    " 		   b.area_flag " +
            //    "       from det_cndc.c_line_desc_t a " +
            //    "      inner join sfcs.c_prod_area_t b " +
            //    "         on a.prod_area_id = b.prod_area_id " +
            //    "        and a.valid_flag = 'Y' " +
            //    "        and b.delete_flag = '0'  " +
            //    "        and b.pqm_flag = 'Y' " +
            //    "        and b.auto_flag = '0' " +
            //    "      inner join det_cndc.c_line_echelon_day_t e " +
            //    "         on a.line_name = e.line_name " +
            //    "       left join det_cndc.c_line_ct_runr_passr_alert_t d " +
            //    "         on a.prod_area_id = d.prod_area_id " +
            //    "        and a.line_name = d.line_name " +
            //    "      where 1 = 1 " +
            //    "        and a.line_name = '" + line + "' " +
            //    "        and e.work_date = to_char(sysdate,'yyyymmdd') " +
            //    "        and (e.section_from <= to_char(sysdate,'hh24mi') and to_char(sysdate,'hh24mi') <= e.section_to)  " +
            //    "    ) " +
            //    "     " +
            //    "  ,LineCap as ( " +
            //    "    SELECT prod_area_id, man_sequence, line_name, work_date, shift, process_name, archiver_lowwarning, archiver_lowlimit, " +
            //    "        yieldr_lowwarning,yieldr_lowlimit,show_fpyr_in_pqoverview,capacity " +
            //    "  from ( " +
            //    " SELECT prod_area_id, man_sequence, line_name, work_date, shift, process_name, archiver_lowwarning, archiver_lowlimit, " +
            //    "        yieldr_lowwarning,yieldr_lowlimit,show_fpyr_in_pqoverview,capacity,--runr_lowlimit, runr_lowwarning, " +
            //    "        row_number() over (partition by line_name order by case when instr(process_name,'後段產出')>0  then -1  when process_name='ALL' then 1 else 2 end,process_name) as process_order " +
            //    " from ( " +
            //    "   SELECT prod_area_id, man_sequence, line_name, work_date, shift, process_name, archiver_lowwarning, archiver_lowlimit, " +
            //    "          yieldr_lowwarning,yieldr_lowlimit,show_fpyr_in_pqoverview,round(avg(capacity), 4) capacity --,runr_lowlimit, runr_lowwarning  " +
            //    "   FROM (             " +
            //    "         SELECT l.prod_area_id,l.man_sequence, l.line_name, p1.work_date, l.shift, decode(p1.process_name,'後段產出','後段產出','ALL') process_name, " +
            //    "                p1.capacity, archiver_lowwarning,archiver_lowlimit,yieldr_lowwarning,yieldr_lowlimit,show_fpyr_in_pqoverview--,l.runr_lowlimit, l.runr_lowwarning  " +
            //    "           FROM DimLine l  " +
            //    "          left join det_cndc.r_pqm_summary_t p1 --主線 " +
            //    "             on p1.line_name = l.line_name " +
            //    "            and p1.work_date = l.work_date " +
            //    "            and p1.shift = l.shift " +
            //    "            where l.area_flag<>1 and nvl(l.night_flag, 'F') = 'F' " +
            //    "         union all " +
            //    "         SELECT l.prod_area_id,l.man_sequence, l.line_name, p1.work_date, l.shift, decode(p1.process_name,'後段產出','後段產出','ALL') process_name, " +
            //    "                p1.capacity, archiver_lowwarning,archiver_lowlimit,yieldr_lowwarning,yieldr_lowlimit,show_fpyr_in_pqoverview--,l.runr_lowlimit, l.runr_lowwarning " +
            //    "           FROM DimLine l  " +
            //    "          left join det_cndc.r_pqm_summary_t p1 " +
            //    "             on p1.line_name = l.line_name " +
            //    "            and p1.work_date = to_char(to_date(l.work_date,'yyyymmdd')-1,'yyyymmdd') " +
            //    "            and p1.shift = l.shift " +
            //    "            where l.area_flag<>1 and l.night_flag = 'B' " +
            //    "         union all " +
            //    "         SELECT l.prod_area_id,l.man_sequence, l.line_name, p1.work_date, l.shift, decode(p1.process_name,'後段產出','後段產出','ALL') process_name, " +
            //    "                p1.capacity, archiver_lowwarning,archiver_lowlimit,yieldr_lowwarning,yieldr_lowlimit,show_fpyr_in_pqoverview--,l.runr_lowlimit, l.runr_lowwarning  " +
            //    "           FROM DimLine l " +
            //    "          left join det_cndc.r_pqm_summary_smt_t p1 --SMT " +
            //    "             on p1.line_name = l.line_name " +
            //    "            and p1.work_date = l.work_date " +
            //    "            and p1.shift = l.shift " +
            //    "            where l.area_flag=1 and nvl(l.night_flag, 'F') = 'F' " +
            //    "         union all " +
            //    "         SELECT l.prod_area_id,l.man_sequence, l.line_name, p1.work_date, l.shift, decode(p1.process_name,'後段產出','後段產出','ALL') process_name, " +
            //    "                p1.capacity, archiver_lowwarning,archiver_lowlimit,yieldr_lowwarning,yieldr_lowlimit,show_fpyr_in_pqoverview--,l.runr_lowlimit, l.runr_lowwarning " +
            //    "           FROM DimLine l " +
            //    "          left join det_cndc.r_pqm_summary_smt_t p1 --SMT " +
            //    "             on p1.line_name = l.line_name " +
            //    "            and p1.work_date = to_char(to_date(l.work_date,'yyyymmdd')-1,'yyyymmdd') " +
            //    "            and p1.shift = l.shift " +
            //    "            where l.area_flag=1 and l.night_flag = 'B') " +
            //    "    GROUP BY prod_area_id, man_sequence, line_name, work_date, shift, process_name, archiver_lowwarning,archiver_lowlimit, " +
            //    "             yieldr_lowwarning,yieldr_lowlimit,show_fpyr_in_pqoverview--,runr_lowlimit, runr_lowwarning  " +
            //    "    ) " +
            //    " )where process_order = 1      " +
            //    " )  " +
            //    "  " +
            //    " select line_name,process_name,capacity as STD_ACH ,yield as FPYR,archiver_lowwarning as SAFE_RATE,archiver_lowlimit as ALERT_RATE,uti as UTILIZATION " +
            //    " from ( " +
            //    "   SELECT " +
            //    "          a.line_name, " +
            //    "          a.process_name, " +
            //    "          a.archiver_lowwarning, " +
            //    "          a.archiver_lowlimit, " +
            //    "          round(a.capacity * 100, 2) as capacity, " +
            //    "          round(a.fpyr * 100, 2)  as yield, " +
            //    "          round(uti * 100, 2) as uti " +
            //    "     FROM (select l.line_name, l.process_name, l.capacity, min(y1.Line_Yield) fpyr, " +
            //    "                  round(avg(e.uti), 4) as uti,  " +
            //    "                  l.archiver_lowwarning, l.archiver_lowlimit " +
            //    "             from linecap l " +
            //    "             left join det_cndc.r_route_yield_line_shift_t y1 " +
            //    "               on l.line_name = y1.line_name " +
            //    "              and l.work_date = y1.work_day " +
            //    "              and l.shift = y1.shift " +
            //    "             left join det_cndc.r_equip_active_daily_shift_t e " +
            //    "               on l.line_name = e.line_name " +
            //    "              and l.work_date = e.work_date " +
            //    "              and l.shift = e.shift " +
            //    "            group by l.line_name, l.process_name, l.capacity, l.archiver_lowwarning, l.archiver_lowlimit) a " +
            //    "   )order by line_name,process_name ";
            //DataTable dt1 = QueryDataTable(sql1, connectionReport);
            //if(dt1.Rows.Count>0)
            //{
            //    _lDataAll.STD_ARCHIVE_PERCENTAGE = Convert.ToDecimal(dt1.Rows[0]["STD_ACH"].ToString());
            //    _lDataAll.FPYR = Convert.ToInt32(dt1.Rows[0]["FPYR"].ToString());
            //}
            #endregion
            
            #region FPYR
            string sqlFPYR = "SELECT round(MIN(t.line_yield)*100,2) as FPYR from det_cndc.r_route_yield_line_shift_t t "
                + " WHERE t.line_name = '" + line + "' AND T.WORK_DAY = TO_CHAR(SYSDATE, 'YYYYMMDD') GROUP BY LINE_NAME ";
            DataTable dtFPYR = QueryDataTable(sqlFPYR, connectionReport);
            if(dtFPYR.Rows.Count>0)
            {
                _lDataAll.FPYR = Convert.ToDecimal(dtFPYR.Rows[0]["FPYR"].ToString());
            }
            #endregion
            #region UPH
            string sqlUPH = " with mes as ( " +
                " select b.seq seq,a.in_station_time in_station_time,round((b.in_station_time-a.in_station_time)*24*60*60) ATT, " +
                " b.model_name model_name1,b.mo_number mo_number,b.line_name line_name,b.group_name group_name,b.serial_number serial_number, " +
                " a.serial_number last_serial_number ,b.in_station_time last_in_station_time,A.model_name model_name,C.OI_RATE OI_RATE,'dataFromSerialNumber' dataFrom ,C.VORNR " +
                "  from (select aa.* ,rownum seq from (select c1.* from  det_cndc.r_sn_ATT_t c1 " +
                "  join det_cndc.c_sn_ATT_t D on c1.group_name = D.GROUP_NAME and D.LINE_NAME = c1.line_name and D.VALID_FLAG = 0 " +
                "  where c1.line_name = '"+line+"' " +
                "  and c1.in_station_time  between sysdate - 1 and sysdate " +
                "  order by c1.in_station_time ) aa) a " +
                " left join (select bb.* ,rownum seq from (select c1.* from  det_cndc.r_sn_ATT_t c1 " +
                " join det_cndc.c_sn_ATT_t D on c1.group_name = D.GROUP_NAME and D.LINE_NAME = c1.line_name and D.VALID_FLAG = 0 " +
                " where c1.line_name = '"+line+"' " +
                " and c1.in_station_time  between sysdate - 1 and sysdate " +
                " order by c1.in_station_time ) bb) b " +
                " on a.seq=b.seq-1 and a.line_name=b.line_name AND a.group_name=b.group_name " +
                " left join (select * from det_cndc.r_schedule_sap_t where OUTPUT_STATION is not null) C " +
                " ON B.line_name=c.line and B.mo_number=c.sfcs_mo and B.model_name=c.sfcs_model and C.OUTPUT_STATION is not null " +
                " ) " +
                " select round(AVG(ATT)) as UPH from mes ";
            DataTable dtUPH = QueryDataTable(sqlUPH, connectionReport);
            if(dtUPH.Rows.Count>0)
            {
                _lDataAll.UPH = Convert.ToInt32(dtUPH.Rows[0]["UPH"].ToString());
            }
            #endregion
            #region OI_RATE
            string sqlOI = "select distinct line_rate as OI_RATE from DET_CNDC.C_MODEL_COLL_INFO_T where line_name = '" + line + "' and model_name = ( "
                + " select A.SFCS_MODEL from det_cndc.R_SCHEDULE_SAP_T A INNER JOIN DET_CNDC.R_SCHEDULE_SFCS_T B ON A.SCHL_NO = B.SCHL_NO "
                + " WHERE NVL(B.START_DATE,TO_DATE(TO_CHAR(A.PLN_STAT_DATE, 'YYYY/MM/DD') || ' ' || A.PLN_STAT_TIME,'YYYY/MM/DD HH24:MI:SS')) BETWEEN SYSDATE -30 AND SYSDATE + 7 "
                + " AND B.FLAG = '1' AND B.LINE_NAME = '"+line+"')";
            DataTable dtOI = QueryDataTable(sqlOI, connectionReport);
            if (dtOI.Rows.Count > 0)
            {
                _lDataAll.OI_RATE = dtOI.Rows[0]["OI_RATE"].ToString();
            }
            else
                _lDataAll.OI_RATE = "N/A";
            #endregion
            #region Line Balnace
            string sqlLineBalnce = " with DimRoute as ( " +
                " SELECT DISTINCT S.LINE_NAME,C.EQUIPMENT_ID,S.PQM_NAME_EN as STATION_NAME_EN, S.PQM_NAME_CN as STATION_NAME_CN,S.GROUP_NAME,C.STATION_NAME,C.STATION_NUMBER,S.PROCESS_TYPE,S.STATION_SEQUENCE STATION_IDX " +
                " FROM ( " +
                "     SELECT B.MO_NUMBER, B.MODEL_NAME, B.ROUTE_CODE, 1 AS LIST_SEQ " +
                "     FROM DET_CNDC.R_MO_OVERALL_V B " +
                "      UNION " +
                "     SELECT L.MO_NUMBER, L.MODEL_NAME, L.ROUTE_CODE, L.LIST_SEQ " +
                "     FROM DET_CNDC.R_MO_ROUTE_LIST_T L " +
                " ) M  " +
                " INNER JOIN DET_CNDC.C_ROUTE_NAME_V2_T N  " +
                " ON M.ROUTE_CODE = N.ROUTE_CODE " +
                " INNER JOIN DET_CNDC.C_PP_ROUTE_ELEMENT_T E  " +
                " ON N.ROUTE_NAME = E.DEFAULT_ROUTE_NAME " +
                " AND N.ROUTE_VERSION = E.PPVERSION " +
                " INNER JOIN  DET_CNDC.R_PP_STATION_T S " +
                " ON E.DEFAULT_ROUTE_NAME = S.ROUTE_NAME " +
                " AND E.PPNO = S.PPNO " +
                " AND E.PPVERSION = S.PPVERSION " +
                " INNER JOIN ( select * from DET_CNDC.C_STATION_CONFIG_T  where valid_flag='Y') C " +
                " ON S.STATION_NUMBER = C.STATION_NUMBER " +
                " INNER JOIN DET_CNDC.R_SCHEDULE_SFCS_T B " +
                " on S.LINE_NAME = B.LINE_NAME " +
                " INNER JOIN DET_CNDC.R_SCHEDULE_SAP_T A " +
                " ON A.SCHL_NO = B.SCHL_NO " +
                " WHERE NVL(B.START_DATE, TO_DATE(TO_CHAR(A.PLN_STAT_DATE, 'YYYY/MM/DD') || ' ' ||A.PLN_STAT_TIME,'YYYY/MM/DD HH24:MI:SS')) BETWEEN SYSDATE - 30 AND SYSDATE + 7 " +
                " AND B.FLAG = '1' " +
                " AND B.LINE_NAME = '"+line+"' " +
                " AND M.MO_NUMBER =  A.SFCS_MO " +
                " AND M.MODEL_NAME = A.SFCS_MODEL " +
                " ) " +
                " select CON.STATION_NAME_EN || '(' || CON.GROUP_NAME || ')' as MACHINE_NAME, round(ba.line_balance,2) as CYCLE_TIME " +
                " from " +
                " (select * from DET_CNDC.R_Line_balance_T where LINE_NAME='"+line+"' )  BA " +
                " RIGHT JOIN (select * from DimRoute where EQUIPMENT_ID is not null) CON " +
                " ON BA.LINE_NAME=CON.LINE_NAME " +
                " AND BA.EQUIPMENT_ID=CON.EQUIPMENT_ID " +
                " ORDER BY CON.STATION_IDX ASC ";
            DataTable dtLineBalnce = QueryDataTable(sqlLineBalnce, connectionReport);
            List<MachineLineInfoLineBalanceDetailModel> _lMachineLineInfoLineBalanceDetailModel = new List<MachineLineInfoLineBalanceDetailModel>();
            if (dtLineBalnce.Rows.Count>0)
            {
                _lMachineLineInfoLineBalanceDetailModel = DataTableToList<MachineLineInfoLineBalanceDetailModel>(dtLineBalnce);
                _lDataAll.LINE_BALANCE = _lMachineLineInfoLineBalanceDetailModel;

            }
            #endregion
            #region STD ACH MAIN LINE
            string sqlSTDACH = " select WORK_DATE,LINE_NAME,PROCESS_NAME,round((capacity)*100,2) as STD_ACH from DET_CNDC.r_pqm_summary_t "
                + "where work_date = to_char(sysdate, 'yyyyMMdd') and line_name = '"+line+"' and PROCESS_NAME = 'MAIN_LINE'";
            DataTable dtSTDACH = QueryDataTable(sqlSTDACH, connectionReport);
            if (dtSTDACH.Rows.Count > 0)
            {
                _lDataAll.STD_ARCHIVE_PERCENTAGE_MAIN_LINE = dtSTDACH.Rows[0]["STD_ACH"].ToString();
            }
            #endregion
            #region STD ACH FINAL LINE
            string sqlSTDACH_F = " select WORK_DATE,LINE_NAME,PROCESS_NAME,round((capacity)*100,2) as STD_ACH from DET_CNDC.r_pqm_summary_t "
                + "where work_date = to_char(sysdate, 'yyyyMMdd') and line_name = '" + line + "' and PROCESS_NAME = 'FINAL_LINE'";
            DataTable dtSTDACH_F = QueryDataTable(sqlSTDACH_F, connectionReport);
            if (dtSTDACH_F.Rows.Count > 0)
            {
                _lDataAll.STD_ARCHIVE_PERCENTAGE_FINAL_LINE = dtSTDACH_F.Rows[0]["STD_ACH"].ToString();
            }
            #endregion
            #region STD ACH Process
            string sqlSTDProcess = " SELECT * " +
                " FROM  " +
                " ( " +
                "     select process_name,round(alert_rate*100) as alert_rate,round(safe_rate*100) as safe_rate,'T'||replace(section_range,'-','_') as section_range,'PLAN_QTY'as QTY_TYPE,plan_qty FROM DET_CNDC.R_PQM_DAILY_SUM_T T " +
                " where WORK_DATE = to_char(sysdate,'yyyyMMdd') " +
                " and LINE_NAME ='"+line+"' " +
                " ) " +
                " PIVOT " +
                " ( " +
                "     SUM (plan_qty) " +
                "     FOR section_range " +
                "     IN  ('T0000_0100', " +
                " 'T0130_0200', " +
                " 'T0200_0300', " +
                " 'T0300_0400', " +
                " 'T0400_0500', " +
                " 'T0730_0830', " +
                " 'T0830_0930', " +
                " 'T0930_1030', " +
                " 'T1030_1130', " +
                " 'T1130_1230', " +
                " 'T1230_1330', " +
                " 'T1330_1430', " +
                " 'T1430_1530', " +
                " 'T1530_1700', " +
                " 'T2000_2100', " +
                " 'T2100_2200', " +
                " 'T2200_2300', " +
                " 'T2300_2400') " +
                " ) " +
                " union " +
                " SELECT * " +
                " FROM  " +
                " ( " +
                "     select process_name,round(alert_rate*100) as alert_rate,round(safe_rate*100) as safe_rate,'T'||replace(section_range,'-','_') as section_range,'REAL_QTY'as QTY_TYPE,real_qty FROM DET_CNDC.R_PQM_DAILY_SUM_T T " +
                " where WORK_DATE = to_char(sysdate,'yyyyMMdd') " +
                " and LINE_NAME ='"+line+"' " +
                " ) " +
                " PIVOT " +
                " ( " +
                "     SUM (real_qty) " +
                "     FOR section_range " +
                "     IN  ('T0000_0100', " +
                " 'T0130_0200', " +
                " 'T0200_0300', " +
                " 'T0300_0400', " +
                " 'T0400_0500', " +
                " 'T0730_0830', " +
                " 'T0830_0930', " +
                " 'T0930_1030', " +
                " 'T1030_1130', " +
                " 'T1130_1230', " +
                " 'T1230_1330', " +
                " 'T1330_1430', " +
                " 'T1430_1530', " +
                " 'T1530_1700', " +
                " 'T2000_2100', " +
                " 'T2100_2200', " +
                " 'T2200_2300', " +
                " 'T2300_2400') " +
                " ) " ;
            DataTable dtSTDProcess = QueryDataTable(sqlSTDProcess, connectionReport);
                List<MachineLineInfoSTDACHDetailModel> _lMachineLineInfoSTDACHDetailModel = new List<MachineLineInfoSTDACHDetailModel>();
            if (dtSTDProcess.Rows.Count > 0)
            {
                dtSTDProcess.Columns["'T0000_0100'"].ColumnName = "'T0000_0100'".Replace("'", "");
                dtSTDProcess.Columns["'T0130_0200'"].ColumnName = "'T0130_0200'".Replace("'", "");
                dtSTDProcess.Columns["'T0200_0300'"].ColumnName = "'T0200_0300'".Replace("'", "");
                dtSTDProcess.Columns["'T0300_0400'"].ColumnName = "'T0300_0400'".Replace("'", "");
                dtSTDProcess.Columns["'T0400_0500'"].ColumnName = "'T0400_0500'".Replace("'", "");
                dtSTDProcess.Columns["'T0730_0830'"].ColumnName = "'T0730_0830'".Replace("'", "");
                dtSTDProcess.Columns["'T0830_0930'"].ColumnName = "'T0830_0930'".Replace("'", "");
                dtSTDProcess.Columns["'T0930_1030'"].ColumnName = "'T0930_1030'".Replace("'", "");
                dtSTDProcess.Columns["'T1030_1130'"].ColumnName = "'T1030_1130'".Replace("'", "");
                dtSTDProcess.Columns["'T1130_1230'"].ColumnName = "'T1130_1230'".Replace("'", "");
                dtSTDProcess.Columns["'T1230_1330'"].ColumnName = "'T1230_1330'".Replace("'", "");
                dtSTDProcess.Columns["'T1330_1430'"].ColumnName = "'T1330_1430'".Replace("'", "");
                dtSTDProcess.Columns["'T1430_1530'"].ColumnName = "'T1430_1530'".Replace("'", "");
                dtSTDProcess.Columns["'T1530_1700'"].ColumnName = "'T1530_1700'".Replace("'", "");
                dtSTDProcess.Columns["'T2000_2100'"].ColumnName = "'T2000_2100'".Replace("'", "");
                dtSTDProcess.Columns["'T2100_2200'"].ColumnName = "'T2100_2200'".Replace("'", "");
                dtSTDProcess.Columns["'T2200_2300'"].ColumnName = "'T2200_2300'".Replace("'", "");
                dtSTDProcess.Columns["'T2300_2400'"].ColumnName = "'T2300_2400'".Replace("'", "");
                _lMachineLineInfoSTDACHDetailModel = DataTableToList<MachineLineInfoSTDACHDetailModel>(dtSTDProcess);
                _lDataAll.STD_ARCHIVE_DATA = _lMachineLineInfoSTDACHDetailModel;
            }
            #endregion
            return Ok(_lDataAll);
        }
        internal DataTable QueryDataTable(string sSql, string sConnect)
        {
            DataTable dt = new DataTable();
            using (OracleConnection connection = new OracleConnection(sConnect))
            {
                OracleCommand command = new OracleCommand(sSql);
                try
                {
                    command.Connection = connection;
                    connection.Open();
                    OracleDataAdapter da = new OracleDataAdapter(command);
                    da.Fill(dt);

                }
                catch (Exception ex)
                {
                }
                finally
                {
                    connection.Close();
                }
            }

            return dt;
        }
        internal static List<T> DataTableToList<T>(DataTable dataTable)
        {
            //Get all the data table column names
            List<string> _ColemnNames = dataTable.Columns.Cast<DataColumn>()
                .Select(c => c.ColumnName.ToLower())
                .ToList();

            //Get all the model properties
            var _ModelProperties = typeof(T).GetProperties();

            //Loop through the datatable rows.
            return dataTable.AsEnumerable().Select(row =>
            {
                //Create an instance of the model
                var _ModelInstatnce = Activator.CreateInstance<T>();
                //Loop through the model properties name
                foreach (var pro in _ModelProperties)
                {
                    // If datatable column name match with model property name
                    if (_ColemnNames.Contains(pro.Name.ToLower()))
                    {
                        //set the datatable column value to model property
                        try
                        {
                            pro.SetValue(_ModelInstatnce, row[pro.Name]);
                        }
                        catch { }
                    }
                }
                return _ModelInstatnce;
            }).ToList();
        }
        internal static DataTable LINQToDataTable<T>(IEnumerable<T> varlist)
        {
            DataTable dtReturn = new DataTable();

            // column names 
            PropertyInfo[] oProps = null;

            if (varlist == null) return dtReturn;

            foreach (T rec in varlist)
            {
                 if (oProps == null)
                {
                    oProps = ((Type)rec.GetType()).GetProperties();
                    foreach (PropertyInfo pi in oProps)
                    {
                        Type colType = pi.PropertyType;

                        if ((colType.IsGenericType) && (colType.GetGenericTypeDefinition()
                        == typeof(Nullable<>)))
                        {
                            colType = colType.GetGenericArguments()[0];
                        }

                        dtReturn.Columns.Add(new DataColumn(pi.Name, colType));
                    }
                }

                DataRow dr = dtReturn.NewRow();

                foreach (PropertyInfo pi in oProps)
                {
                    dr[pi.Name] = pi.GetValue(rec, null) == null ? DBNull.Value : pi.GetValue
                    (rec, null);
                }

                dtReturn.Rows.Add(dr);
            }
            return dtReturn;
        }
    }
}