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
using MESAPI.ComFunc;


namespace MESAPI.Controllers
{
    public class FPYRDataController : ApiController
    {

        [System.Web.Http.HttpPost()]
        public IHttpActionResult FPYRDataAll(POModel Mfpyr)
        {
            List<FPYR> _MatList = new List<FPYR>();
            //matlist.Add(m2);
            if (Mfpyr == null || Mfpyr.Factory == null || Mfpyr.Prod_Area == null)
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string factory = "" + Mfpyr.Factory.Trim();
            string prod_area = "" + Mfpyr.Prod_Area.Trim();


            if (factory == "" || prod_area == "")
            {
                return Content(HttpStatusCode.NotFound, "Please check input data");
            }
            else
            {

                DataTable line = getLine(prod_area.ToUpper(), factory);

                foreach (DataRow roopline in line.Rows)
                {
                    #region getMatData
                    string sql = @"with DimLine as (
select  a.LINE_NAME,a.PROD_AREA_ID,b.Factory,b.prod_area_desc,
              NVL(a.LINE_STYLE,'ASSEMBLY') LINE_STYLE,a.automation_flag,CASE wHEN a.automation_flag=0 then  '人工線' else '自動化線' end as Line_Category,d.ARCHIVER_LOWWARNING,d.ARCHIVER_LOWLIMIT,LINE_STYLE as ORI_LINE_STYLE,
              e.group_name,e.group_index,case when b.AREA_FLAG='0' then 0 else 1 end as SMT_FLAG
     from (select * from C_LINE_DESC_T where valid_flag='Y') a
            inner join (
                       select * from SFCS.C_PROD_AREA_T
                       where delete_flag='0'
                       and  pqm_flag='Y'
                       --and AREA_FLAG = '0'
                       --and FACTORY = 'DG3'
                       ) b
            on a.PROD_AREA_ID=b.PROD_AREA_ID
        left join C_LINE_CT_RUNR_PASSR_ALERT_T d on a.PROD_AREA_ID=d.PROD_AREA_ID and a.line_name=d.line_name
          inner join (
            select line_name,group_name,MAX(NVL(station_idx,0)) as group_index
            from ( select * from C_STATION_CONFIG_T  where valid_flag='Y')
            where line_name in '" + roopline[0] + "' " +
            @" group by line_name,group_name
            order by group_index
          ) e on a.line_name = e.line_name
     WHERE 1 = 1 and a.line_name in '" + roopline[0] + "' " +
@")
,DimOriCanlendar as (
select a.*,
       min(SN_BY_ECHELON) over(partition by a.work_date, a.line_name, a.SHIFT, a.night_flag) as min_sn_by_night_flag,
       max(SN_BY_ECHELON) over(partition by a.work_date, a.line_name, a.SHIFT, a.night_flag) as max_sn_by_night_flag
  from(
         select
                 e.Line_name, e.ECHELON_NAME, E.SHIFT, E.SECTION_FROM, E.SECTION_TO, E.duration, E.work_time as ori_work_time,
                 case when TO_CHAR(SYSDATE, 'yyyyMMdd') <> TO_CHAR(SYSDATE, 'yyyyMMdd') then E.work_time
                       when(E.SECTION_FROM <= TO_CHAR(SYSDATE, 'HH24MI') AND TO_CHAR(SYSDATE, 'HH24MI') <= E.SECTION_TO) AND REST_FROM is not null and REST_TO is not null
                             then Case when TO_CHAR(SYSDATE, 'HH24MI') <= REST_FROM then   round((SYSDATE - TO_DATE(TO_CHAR(SYSDATE, 'yyyy-MM-dd') || ' ' || Substr(SECTION_FROM, 1, 2) || ':' || substr(SECTION_FROM, 3, 2), 'YYYY-MM-DD hh24:mi')) * 1440, 0)
                                        when REST_FROM < TO_CHAR(SYSDATE, 'HH24MI') and TO_CHAR(SYSDATE, 'HH24MI') <= REST_TO then round((SYSDATE - TO_DATE(TO_CHAR(SYSDATE, 'yyyy-MM-dd') || ' ' || Substr(SECTION_FROM, 1, 2) || ':' || substr(SECTION_FROM, 3, 2), 'YYYY-MM-DD hh24:mi')) * 1440 - (SYSDATE - TO_DATE(TO_CHAR(SYSDATE, 'yyyy-MM-dd') || ' ' || Substr(REST_FROM, 1, 2) || ':' || substr(REST_FROM, 3, 2), 'YYYY-MM-DD hh24:mi')) * 1440, 0)
                                        when REST_TO < TO_CHAR(SYSDATE, 'HH24MI') then round((SYSDATE - TO_DATE(TO_CHAR(SYSDATE, 'yyyy-MM-dd') || ' ' || Substr(SECTION_FROM, 1, 2) || ':' || substr(SECTION_FROM, 3, 2), 'YYYY-MM-DD hh24:mi')) * 1440 - (TO_DATE(TO_CHAR(SYSDATE, 'yyyy-MM-dd') || ' ' || Substr(REST_TO, 1, 2) || ':' || substr(REST_TO, 3, 2), 'YYYY-MM-DD hh24:mi') - TO_DATE(TO_CHAR(SYSDATE, 'yyyy-MM-dd') || ' ' || Substr(REST_FROM, 1, 2) || ':' || substr(REST_FROM, 3, 2), 'YYYY-MM-DD hh24:mi')) * 1440, 0)
                                        else E.work_time end
                       when(E.SECTION_FROM <= TO_CHAR(SYSDATE, 'HH24MI') AND TO_CHAR(SYSDATE, 'HH24MI') <= E.SECTION_TO) and REST_FROM  is null then round((SYSDATE-TO_DATE(TO_CHAR(SYSDATE, 'yyyy-MM-dd') || ' ' || Substr(SECTION_FROM, 1, 2) || ':' || substr(SECTION_FROM, 3, 2), 'YYYY-MM-DD hh24:mi'))*1440,0)
                       else E.work_time end as work_time,
                 TO_CHAR(SYSDATE, 'HH24MI') as CUR_TIME,
                 max(case when to_date(E.WORK_DATE || E.SECTION_FROM, 'YYYYMMDDHH24MI') <= (SYSDATE)AND(SYSDATE) <= to_date(E.WORK_DATE || decode(E.SECTION_TO, '2400', '2359', E.SECTION_TO), 'YYYYMMDDHH24MI') then SHIFT else '' end ) over(partition by E.Line_name) as CUR_SHIFT,
                 max(case when E.SECTION_FROM <= TO_CHAR(((SYSDATE)), 'HH24MI') AND TO_CHAR(((SYSDATE)), 'HH24MI') <= E.SECTION_TO then SECTION_FROM else '' end ) over(partition by E.Line_name) as CUR_SECTION_FROM,
                 WORK_DATE,
                 (CASE when NIGHT_FLAG = 'B' then to_char(to_date(WORK_DATE,'yyyymmdd')-1,'yyyymmdd') else WORK_DATE end ) as Factory_DATE,
                 night_flag,e.REST_FROM,e.REST_TO,
                 case when REST_FROM  is null  then '1' else '0' end test,
                       row_number() over(partition by E.LINE_NAME order by WORK_DATE, section_from) as SN_BY_ECHELON,
                 min(SECTION_FROM) over(partition by E.line_name, E.SHIFT, nvl(E.night_flag, '')) as min_from,
                 max(SECTION_TO) over(partition by E.line_name, E.SHIFT, nvl(E.night_flag, '')) as max_to,
                 max(case when work_Date || section_from <= to_char((SYSDATE), 'YYYYMMDDHH24MI') and to_char((SYSDATE), 'YYYYMMDDHH24MI') <= work_Date || section_to then night_flag else '' end )   over(partition by E.LINE_NAME) as cur_night_flag
                 ,F.Factory,F.SMT_FLAG
           from C_LINE_ECHELON_DAY_T E,(select distinct LINE_NAME, FACTORY, SMT_FLAG from DimLine) F
         where 1 = 1
               and E.LINE_NAME = F.LINE_NAME
               and WORK_DATE in (TO_CHAR(SYSDATE, 'yyyyMMdd'), to_char(to_date(TO_CHAR(SYSDATE, 'yyyyMMdd'), 'YYYYMMDD') - 1, 'YYYYMMDD'))
       )a
where work_date || SECTION_FROM <= to_char((SYSDATE), 'YYYYMMDD') || CUR_TIME
        Order by WORK_DATE,SECTION_FROM
)
,DimCanlendar as (
select FACTORY, SMT_FLAG, LINE_NAME, ECHELON_NAME, SHIFT, SECTION_FROM, SECTION_TO, DURATION, ORI_WORK_TIME, WORK_TIME, CUR_TIME, CUR_SHIFT, CUR_SECTION_FROM, WORK_DATE, FACTORY_DATE, NIGHT_FLAG, REST_FROM, REST_TO, TEST, SN_BY_ECHELON, MIN_SN_BY_NIGHT_FLAG, MAX_SN_BY_NIGHT_FLAG, MIN_FROM, MAX_TO, CUR_NIGHT_FLAG
  from DimOriCanlendar
          WHERE(
FACTORY_DATE = (CASE WHEN  night_flag = 'B' THEN TO_CHAR(SYSDATE - 1, 'YYYYMMDD') ELSE TO_CHAR(SYSDATE, 'YYYYMMDD') END)
                 )
            AND LINE_NAME  in '" + roopline[0] + "'" +
            @" order by LINE_NAME,WORK_DATE,SECTION_FROM
),
raw_c_route_element_t as (
    select PROD_AREA_CODE, MODEL_NAME, LINE_NAME, PPVERSION, GROUP_SEQUENCE, PROCESS_GROUP, PROCESS_GROUP_DESC, PROCESS_TYPE, ROUTING_CHECK, DEFAULT_ROUTE_NAME, INSERT_TIME, SET_EMP, MES_GROUP, PPNO, PROCESS_UNIT_CODE, ROUTE_CODE, FPYR_FLAG, CURRENT_FLAG,
    row_number() over(partition by MODEL_NAME, route_code, line_name order by  group_sequence) as sn_by_route
     from(
       SELECT  c.*, e.MODEL_NAME, e.line_name
        FROM(select * from c_pp_route_element_t where upper(fpyr_flag) = 'TRUE') c
       inner join(select * from c_pp_route_t  where line_name in '" + roopline[0] + "' " +
       @") e
         on c.ppno = e.ppno
         and c.ppversion = e.ppversion
         --where route_code = '1000003329'
       ) a
    order by MODEL_NAME, route_code, GROUP_SEQUENCE
 ),
    Proces_c_route_element_t(PROD_AREA_CODE, MODEL_NAME, LINE_NAME, PPVERSION, GROUP_INTER_SEQUENCE, PROCESS_GROUP, PROCESS_GROUP_DESC, PROCESS_TYPE, ROUTING_CHECK, DEFAULT_ROUTE_NAME, INSERT_TIME, SET_EMP, MES_GROUP, PPNO, PROCESS_UNIT_CODE, ROUTE_CODE, FPYR_FLAG, CURRENT_FLAG,
      sn_by_route, GROUP_SEQUENCE)
    as (
    select PROD_AREA_CODE, MODEL_NAME, LINE_NAME, PPVERSION, GROUP_SEQUENCE  as GROUP_INTER_SEQUENCE,PROCESS_GROUP,PROCESS_GROUP_DESC,PROCESS_TYPE,ROUTING_CHECK,DEFAULT_ROUTE_NAME,INSERT_TIME,SET_EMP,MES_GROUP,PPNO,PROCESS_UNIT_CODE,ROUTE_CODE,FPYR_FLAG,CURRENT_FLAG,
      sn_by_route,GROUP_SEQUENCE
    from raw_c_route_element_t
    where sn_by_route = 1
    union all
    select a.PROD_AREA_CODE,a.MODEL_NAME,a.LINE_NAME,b.PPVERSION,
    case when a.PROCESS_UNIT_CODE = b.PROCESS_UNIT_CODE then a.GROUP_INTER_SEQUENCE else b.GROUP_SEQUENCE end as GROUP_INTER_SEQUENCE,b.PROCESS_GROUP,b.PROCESS_GROUP_DESC,b.PROCESS_TYPE,b.ROUTING_CHECK,b.DEFAULT_ROUTE_NAME,b.INSERT_TIME,b.SET_EMP,b.MES_GROUP,b.PPNO,b.PROCESS_UNIT_CODE,a.ROUTE_CODE,b.FPYR_FLAG,b.CURRENT_FLAG,
    b.sn_by_route,b.GROUP_SEQUENCE
    from Proces_c_route_element_t a, raw_c_route_element_t b
   where a.MODEL_NAME = b.MODEL_NAME
    and a.route_code = b.route_code
    and a.line_name = b.line_name
    and a.sn_by_route + 1 = b.sn_by_route
    )
    ,dimOriRoute as (
    select c.factory,a.PROD_AREA_CODE,case when d.smt_flag = 1 then a.model_name || 'SMT' else a.MODEL_NAME end as MODEL_NAME,a.LINE_NAME,a.ROUTE_CODE,a.PROCESS_UNIT_CODE,a.GROUP_INTER_SEQUENCE,
    case when row_number() over(partition by  a.prod_area_code, a.model_name, a.line_name, a.route_code, a.default_route_name, a.process_unit_code, a.GROUP_INTER_SEQUENCE order by  a.GROUP_SEQUENCE) = 1 then 1 else 0 end as firstGroupInprocess,
    row_number() over(partition by  a.prod_area_code, a.model_name, a.line_name, a.route_code, a.default_route_name, a.process_unit_code, a.GROUP_INTER_SEQUENCE order by  a.GROUP_SEQUENCE) as SN_BY_PROCES,
    a.PPVERSION, a.GROUP_SEQUENCE,a.PROCESS_GROUP,a.PROCESS_GROUP_DESC,a.PROCESS_TYPE,a.ROUTING_CHECK,a.DEFAULT_ROUTE_NAME,a.INSERT_TIME,a.SET_EMP,a.MES_GROUP,a.PPNO,a.FPYR_FLAG,a.CURRENT_FLAG,
    a.sn_by_route
    from Proces_c_route_element_t a
      inner join c_line_desc_t b on a.line_name = b.line_name
     inner join(select* from sfcs.c_prod_area_t where  delete_flag= '0' and pqm_flag = 'Y') c on b.prod_area_id = c.prod_area_id
     --SMT
     inner join(select distinct line_name, smt_flag from DimLine)  d on a.line_name = d.line_name
     order by c.factory,a.PROD_AREA_CODE,a.MODEL_NAME,a.LINE_NAME,a.ROUTE_CODE,a.PROCESS_UNIT_CODE,a.GROUP_INTER_SEQUENCE, a.GROUP_SEQUENCE
    ),
    dimRoute as (
    select a.FACTORY,a.PROD_AREA_CODE,a.MODEL_NAME,a.LINE_NAME,a.ROUTE_CODE,a.PROCESS_UNIT_CODE,a.GROUP_INTER_SEQUENCE,a.FIRSTGROUPINPROCESS,a.SN_BY_PROCES,a.PPVERSION,a.GROUP_SEQUENCE,a.PROCESS_GROUP,a.PROCESS_GROUP_DESC,a.PROCESS_TYPE,a.ROUTING_CHECK,a.DEFAULT_ROUTE_NAME,a.INSERT_TIME,a.SET_EMP,a.MES_GROUP,a.PPNO,a.FPYR_FLAG,a.CURRENT_FLAG,a.SN_BY_ROUTE
    from dimOriRoute a
    union all
    select a.FACTORY,a.PROD_AREA_CODE,rtrim(a.MODEL_NAME, 'SMT') as MODEL_NAME,a.LINE_NAME,a.ROUTE_CODE,a.PROCESS_UNIT_CODE,a.GROUP_INTER_SEQUENCE,a.FIRSTGROUPINPROCESS,a.SN_BY_PROCES,a.PPVERSION,a.GROUP_SEQUENCE,a.PROCESS_GROUP,a.PROCESS_GROUP_DESC,a.PROCESS_TYPE,a.ROUTING_CHECK,a.DEFAULT_ROUTE_NAME,a.INSERT_TIME,a.SET_EMP,a.MES_GROUP,a.PPNO,a.FPYR_FLAG,a.CURRENT_FLAG,a.SN_BY_ROUTE
     from dimOriRoute a
    where instr(a.MODEL_NAME,'SMT')> 0
  )
    ,
factGroupYield as (
            select d.FACTORY_DATE as WORK_DAY,c.work_date,c.PROD_PLANT,c.MODEL_NAME,c.LINE_NAME,c.ROUTE_CODE,c.GROUP_NAME,c.PASS_QTY,c.FAIL_QTY,c.SECTION_RANGE,c.SHIFT,c.NIGHT_FLAG
                  from(Select * from R_PROC_FPYR_GROUP_RANGE_T where line_name in '" + roopline[0] + "' " +
                  @" and WORK_DATE in (TO_CHAR(SYSDATE, 'yyyyMMdd'), to_char(to_date(TO_CHAR(SYSDATE, 'yyyyMMdd'), 'YYYYMMDD') - 1, 'YYYYMMDD'))) c inner join
                         (
                          select *
                          from DimCanlendar
     
                          --where smt_flag = 0
     
                          ) d
                              on c.WORK_DATE = d.WORK_DATE
                          and c.LINE_NAME = d.LINE_NAME
                          and c.SECTION_RANGE = d.SECTION_FROM || '-' || d.SECTION_TO
                          and c.SHIFT = d.SHIFT
                          and NVL(c.NIGHT_FLAG,' ') = NVL(d.NIGHT_FLAG, ' ')
                      inner join(select cc.line_name, cc.prod_area_id, aa.FACTORY, bb.FPYR_FLAG, aa.prod_area_code
                            from (select* from sfcs.c_prod_area_t where  delete_flag= '0' and pqm_flag = 'Y')  aa join sfcs.c_prod_area_config_t bb on aa.prod_area_id = bb.prod_area_id
                                                    join(select * from C_LINE_DESC_T
                                                             where valid_flag = 'Y'
                                                           ) cc on aa.prod_area_id = cc.prod_area_id
                                ) e
                        on c.line_name = e.line_name
)
,
dimMix as (
select b.FACTORY_DATE as WORK_DAY,b.work_Date,b.SHIFT,B.Night_FLAG,SECTION_FROM,SECTION_TO, SECTION_FROM || '-' || SECTION_TO as SECTION_RANGE,a.factory,a.PROD_AREA_CODE,a.MODEL_NAME,a.LINE_NAME,a.ROUTE_CODE,a.PROCESS_UNIT_CODE,a.GROUP_INTER_SEQUENCE,a.FIRSTGROUPINPROCESS,a.SN_BY_PROCES,a.FPYR_FLAG,a.MES_GROUP as GROUP_NAME,a.GROUP_SEQUENCE
from(select * from dimRoute
       where model_name || route_code in (select model_name || route_Code
                                          from factGroupYield
                                        )
       )   a inner join(
                          select *
                          from DimCanlendar
                          --where smt_flag = 0
                          )   b
on a.line_name = b.line_name
)
,
Gen_R_ROUTE_YIELD_GROUP_SHIFT_SUM as (
select WORK_DAY, factory, PROD_AREA_CODE, ROUTE_CODE, LINE_NAME, SHIFT, MODEL_NAME, GROUP_INTER_SEQUENCE, PROCESS_UNIT_CODE, GROUP_NAME, SECTION_RANGE, PASS_QTY, FAIL_QTY, YIELD, PROCESS_GROUP_OUTPUT_QTY, PROCESS_GROUP_FAIL_QTY, PROCESS_OUTPUT_QTY, SN_BY_PROCES, involved,null as SUM_WEIGHT
from(
select
    a.WORK_DAY, a.factory, a.PROD_AREA_CODE, a.ROUTE_CODE, a.LINE_NAME, a.SHIFT, a.MODEL_NAME, A.GROUP_INTER_SEQUENCE, a.PROCESS_UNIT_CODE, a.GROUP_NAME,
    a.SECTION_RANGE,
    sum(b.pass_qty) as pass_qty,
        case    when a.group_name is not null then  sum(b.fail_qty) else
                                          case when sum(b.pass_qty) is null and sum(nvl(b.fail_qty, 0)) = 0 then null else sum(nvl(b.fail_qty, 0))  end end as fail_qty,
    round(((sum(b.pass_qty) / sum(b.pass_qty + b.fail_qty))*100), 4) as yield,
    sum(case when SN_BY_PROCES = 1 then b.PASS_QTY else null end)+sum(b.FAIL_QTy) as process_group_output_qty,
    sum(b.FAIL_QTy) as process_group_fail_qty,
    sum(case when SN_BY_PROCES = 1 then b.PASS_QTY else null end)  process_output_qty,a.SN_BY_PROCES,
    case when a.GROUP_NAME is null and a.PROCESS_UNIT_CODE is not null then case when sum(b.pass_qty+b.fail_qty)= 0 or sum(b.pass_qty+b.fail_qty) is null then 'N' else 'Y' End else null end as involved

from
dimMix a left
join
(
                select *from factGroupYield

        )  b
        on a.line_name = b.line_name
        and a.work_date = b.work_Date
        and a.SECTION_FROM || '-' || a.SECTION_TO = b.SECTION_RANGE
        and a.SHIFT = b.SHIFT
        and nvl(a.NIGHT_FLAG,' ') = nvl(b.NIGHT_FLAG, ' ')
        and a.route_code = b.route_Code
        and a.model_name = b.model_name
        and a.group_name = b.group_name
        group by grouping sets
      (
      --(b.FACTORY_DATE, c.FACTORY, a.prod_plant, a.line_name, a.MODEL_NAME, a.ROUTE_CODE, a.group_name, a.shift, a.section_range)
         (a.WORK_DAY, a.factory, a.PROD_AREA_CODE, a.ROUTE_CODE, a.LINE_NAME, a.MODEL_NAME, A.GROUP_INTER_SEQUENCE, a.PROCESS_UNIT_CODE, a.GROUP_NAME, a.SHIFT, a.SECTION_RANGE, a.SN_BY_PROCES)
         ,(a.WORK_DAY, a.factory, a.PROD_AREA_CODE, a.ROUTE_CODE, a.LINE_NAME, a.MODEL_NAME, A.GROUP_INTER_SEQUENCE, a.PROCESS_UNIT_CODE, a.SHIFT, a.SECTION_RANGE)
        ,(a.WORK_DAY, a.factory, a.PROD_AREA_CODE, a.ROUTE_CODE, a.LINE_NAME, a.MODEL_NAME, A.GROUP_INTER_SEQUENCE, a.PROCESS_UNIT_CODE, a.SHIFT)
         ,(a.WORK_DAY, a.factory, a.PROD_AREA_CODE, a.ROUTE_CODE, a.LINE_NAME, a.MODEL_NAME, A.GROUP_INTER_SEQUENCE, a.PROCESS_UNIT_CODE)

       )
     ) t
      order by PROD_AREA_CODE, FACTORY, MODEL_NAME, PROCESS_UNIT_CODE, WORK_DAY, LINE_NAME,case when group_name is null then ' ' else group_name end, SECTION_RANGE, SHIFT desc,SN_BY_PROCES
)
SELECT a.MODEL_NAME,a.PROCESS_UNIT_CODE,coalesce(a.YIELD, 0)as YIELD ,a.line_name,a.SHIFT
from
(
  select WORK_DAY, SHIFT, LINE_NAME, MODEL_NAME ||case when ROUTE_CODE is not null then '_' || to_char(ROUTE_CODE) else '' end as MODEL_NAME, PROCESS_UNIT_CODE, YIELD,GROUP_INTER_SEQUENCE
       FROM Gen_R_ROUTE_YIELD_GROUP_SHIFT_SUM T
  where SHIFT is not null and SECTION_RANGE is null and Group_name is null and PROCESS_UNIT_CODE is not null
) a
order by
    MODEL_NAME asc,
     SHIFT asc

                    ";

                    //var conDB = "";
                    //if (factory == "det_am")
                    //{
                    //    conDB = connectionRE;
                    //}
                    //else
                    //{
                    //    conDB = connectionRE;
                    //}
                    DataTable dt = QueryDataTable(sql, conDatabase(factory));
                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow ds in dt.Rows)
                        {
                            FPYR item = new FPYR();
                            item.LINE_NAME = ds["LINE_NAME"].ToString();
                            item.MODEL_NAME = ds["MODEL_NAME"].ToString();
                            item.PROCESS_UNIT_CODE = ds["PROCESS_UNIT_CODE"].ToString();
                            item.YIELD = ds["YIELD"].ToString();
                            item.SHIFT = ds["SHIFT"].ToString();

                            _MatList.Add(item);
                            //List<FPYR> _itemList = new List<FPYR>();
                            //_itemList = DataTableToList<FPYR>(dt);
                        }

                        //string a = dt.Columns["QTY"].DataType.ToString();
                        //string b = dt.Columns["STATUS"].DataType.ToString();
                        //string c = dt.Columns["WH_ISSUE_QTY"].DataType.ToString();
                        //string d = dt.Columns["SCANNED_QTY"].DataType.ToString();
                        //string e = dt.Columns["MRP_QTY"].DataType.ToString();
                        //ret = DataTableToJSONWithJSONNet(dt);
                        //List<FPYR> _MatList = new List<FPYR>();
                        // _MatList = DataTableToList<FPYR>(dt);

                    }


                    #endregion
                }
                return Ok(_MatList);
            }
            //return Ok(_MatList);
        }

        internal string conDatabase(string factory)
        {
            var conDB = "";
            if (factory == "DET_AM")
            {
                conDB = Comfunction.connection1();
            }
            else if (factory == "DET_DNI")
            {
                conDB = Comfunction.connection3();
            }
            else if (factory == "DET_MP")
            {
                conDB = Comfunction.connection4();
            }
            else if (factory == "DET_HP")
            {
                conDB = Comfunction.connection5();
            }
            else if (factory == "DET_CNDC")
            {
                conDB = Comfunction.connection7();
            }
            else if (factory == "DET_FM")
            {
                conDB = Comfunction.connection6();
            }
            else if (factory == "DET_MS")
            {
                conDB = Comfunction.connectionMS();
            }
            else
            {
                return ("Plese check your factory if it is correct");
            }
            return conDB;
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
        internal DataTable getLine(string prod_area, string factory)
        {

            string sqlLine = "select line_name from c_line_desc_t l"
                                + " inner join sfcs.c_prod_area_t a "
                                + " on a.prod_area_id = l.prod_area_id"
                                + " where a.prod_area_code = '" + prod_area + "'";
            //string connection = connection3;
            DataTable dtLine = QueryDataTable(sqlLine, conDatabase(factory));

            return dtLine;
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
    }
}