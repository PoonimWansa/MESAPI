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

namespace MESAPI.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class GetLossInfoProductLossController : ApiController
    {
        string connectionReport = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THBPODSMRACDB)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = THSFDB)));Password=tmudk$o0;User ID=PQM_QUERY";
        string connectionBP = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THPUBMES-SCAN)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = THPUBMES)));Password=Delta12345;User ID=MESAP03";

        [System.Web.Http.HttpGet()]
        [Route("api/LossInformation/ProductLoss/{MES_FACTORY}/{FACTORY}/{PROD_AREA}/{DATE}")]
        ///api/machine/information/{Equipment_Code}
        public IHttpActionResult GetLossInfoProductLoss(string MES_FACTORY, string FACTORY, string PROD_AREA, string DATE)
        {
            //matlist.Add(m2);
            if (MES_FACTORY == null || FACTORY == null || PROD_AREA == null || DATE  == null)
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string factory = FACTORY.Trim().ToUpper();
            string prodarea = PROD_AREA.Trim().ToUpper();
            string date = DATE.Trim();
            string schema = MES_FACTORY.Trim();//getSchema(factory);
            //if (schema == "")
            //    return Content(HttpStatusCode.NotFound, "Please check input data");
            #region getDataMain
            string sql = " select G1.* "
                + " ,NVL(sum( round(  ( to_date(G2.END_TIME,'YYYY/MM/DD HH24:MI') - to_date(G2.BEGIN_TIME,'YYYY/MM/DD HH24:MI') )*24*60  ) ),0) as ACTUAL_LOSS "
                + " from "
                + " ( "
                + " select  "
                + " C.PROD_AREA_CODE as PROD_AREA, "
                + " A.* "
                + " from "
                + " ( "
                + " SELECT  "
                + " A.LINE_NAME, "
                + " A.WORK_DATE , "
                + " A.SECTION_RANGE AS TIME_RANGE, "
                + " A.SHIFT, "
                + " A.WORK_TIME, "
                + " A.PROCESS_NAME, "
                + " MIN(A.MODEL_NAME) AS MODEL_NAME, "
                + " SUM(A.REAL_QTY) AS REAL_QTY, "
                + " case when count(*) > 1 then case when SUM(A.SUM_PRODUCE_OI) = 0 then FLOOR((AVG(A.WORK_TIME) * 60) /MIN(A.LINE_RATE)) "
                + " else floor((AVG(A.WORK_TIME) * 60) /SUM(A.SUM_PRODUCE_OI)*SUM(A.REAL_QTY)) end "
                + " else AVG(A.PLAN_QTY) end as PLAN_QTY, "
                + " case when count(*) > 1 then case when SUM(A.SUM_PRODUCE_OI) = 0 then FLOOR((AVG(A.WORK_TIME) * 60) /MIN(A.LINE_RATE)) "
                + " else floor((AVG(A.WORK_TIME) * 60) /SUM(A.SUM_PRODUCE_OI)*SUM(A.REAL_QTY)) end "
                + " else AVG(A.PLAN_QTY) end "
                + " - SUM(A.REAL_QTY) AS DIFFERENCE "
                + " ,case when count(*)> 1 then floor( (1- SUM(A.SUM_PRODUCE_OI) / (AVG(A.WORK_TIME) * 60))*AVG(A.WORK_TIME) )  "
                + " else "
                + " FLOOR( (1-(SUM(A.REAL_QTY)/ "
                + " case when count(*) > 1 then case when SUM(A.SUM_PRODUCE_OI) = 0 then FLOOR((AVG(A.WORK_TIME) * 60) /MIN(A.LINE_RATE)) "
                + " else floor((AVG(A.WORK_TIME) * 60) /SUM(A.SUM_PRODUCE_OI)*SUM(A.REAL_QTY)) end "
                + " else AVG(A.PLAN_QTY) end  "
                + " ))*MIN(A.WORK_TIME) ) "
                + " end  "
                + " as LOSS_MIN "
                + " ,ROUND(SUM(A.REAL_QTY)/ "
                + " case when count(*) > 1 then case when SUM(A.SUM_PRODUCE_OI) = 0 then FLOOR((AVG(A.WORK_TIME) * 60) /MIN(A.LINE_RATE)) "
                + " else floor((AVG(A.WORK_TIME) * 60) /SUM(A.SUM_PRODUCE_OI)*SUM(A.REAL_QTY)) end "
                + " else AVG(A.PLAN_QTY) end  "
                + " ,2) AS CAPACITY "
                + " FROM "
                + " ( "
                + " SELECT T.LINE_RATE*T.REAL_QTY AS SUM_PRODUCE_OI ,T.WORK_DATE,T.ECHELON_NAME,T.SHIFT,T.LINE_NAME,T.MODEL_NAME,T.PROCESS_NAME,T.OUT_GROUP,T.SECTION_RANGE,"
                + " T.WORK_TIME,T.LINE_RATE,T.PLAN_QTY,T.REAL_QTY,T.DIF,T.CAPACITY,T.FAIL_QTY,T.LOSSMIN,T.SAFE_RATE,T.ALERT_RATE,T.STATE,T.SHOW_FLAG "
                + " FROM "+ schema + ".R_PQM_DAILY_T T "
                + " WHERE T.WORK_DATE = '" + date + "' "
                + " AND T.SHOW_FLAG = 'Y' "
                + " union"
                + " SELECT T.LINE_RATE*T.REAL_QTY AS SUM_PRODUCE_OI ,T.WORK_DATE,T.ECHELON_NAME,T.SHIFT,T.LINE_NAME,T.MODEL_NAME,T.PROCESS_NAME,T.OUT_GROUP,T.SECTION_RANGE,"
                + " 60 as WORK_TIME,T.LINE_RATE,T.PLAN_QTY,T.REAL_QTY,T.DIF,T.CAPACITY,T.FAIL_QTY,T.LOSSMIN,T.SAFE_RATE,T.ALERT_RATE,T.STATE,T.SHOW_FLAG "
                + " FROM " + schema + ".R_PQM_DAILY_SMT_T T "
                + " WHERE T.WORK_DATE = '" + date + "' "
                //+ " AND T.SHOW_FLAG = 'Y' "
                + " ) A "
                + " GROUP BY A.WORK_DATE,A.SECTION_RANGE,A.SHIFT,A.WORK_TIME,A.PROCESS_NAME,A.LINE_NAME "
                + " ) "
                + " A , "+ schema + ".C_LINE_DESC_T B , "+ schema + ".C_PROD_AREA_T C "
                + " where A.LINE_NAME = B.LINE_NAME "
                + " and B.PROD_AREA_ID = C.PROD_AREA_ID "
                + " and 1=1 "
                + " and B.VALID_FLAG = 'Y' "
                + " ) G1 "
                + " LEFT JOIN "+ schema + ".p_pqm_line_loss_t G2 "
                + " on G1.WORK_DATE=G2.WORK_DATE "
                + " and G1.LINE_NAME = G2.LINE_NAME "
                + " and G1.PROCESS_NAME = G2.PROCESS_NAME "
                + " and to_number ( replace(substr(G2.BEGIN_TIME,12,5),':','')) >= to_number(substr(G1.TIME_RANGE,0,4)) "
                + " and to_number ( replace(substr(G2.BEGIN_TIME,12,5),':','')) < to_number(substr(G1.TIME_RANGE,6,4)) "
                + " and G2.STAY_FLAG = 0 "
                + " where G1.CAPACITY < 0.95 "
                + " group by G1.PROD_AREA,G1.WORK_DATE,G1.TIME_RANGE,G1.SHIFT,G1.WORK_TIME,G1.PROCESS_NAME,G1.LINE_NAME,G1.MODEL_NAME,G1.REAL_QTY,G1.PLAN_QTY,G1.DIFFERENCE,G1.LOSS_MIN,G1.CAPACITY "
                + " order by G1.PROD_AREA,G1.WORK_DATE,G1.LINE_NAME,G1.TIME_RANGE,G1.SHIFT,G1.WORK_TIME,G1.PROCESS_NAME ";
            if (prodarea != "ALL")
                sql = sql.Replace("1=1", "C.PROD_AREA_CODE = '" + prodarea + "'");
            else
                sql = sql.Replace("1=1", "C.FACTORY = '"+factory+"'");
            DataTable dt = QueryDataTable(sql, connectionReport);
            if (dt.Rows.Count > 0)
            {
                var _list = (from rw in dt.AsEnumerable()
                             select new
                             {
                                 PROD_AREA = Convert.ToString(rw["PROD_AREA"]),
                                 LINE_NAME = Convert.ToString(rw["LINE_NAME"]),
                                 WORK_DATE = Convert.ToString(rw["WORK_DATE"]),
                                 TIME_RANGE = Convert.ToString(rw["TIME_RANGE"]),
                                 SHIFT = Convert.ToString(rw["SHIFT"]),
                                 WORK_TIME = Convert.ToString(rw["WORK_TIME"]),
                                 PROCESS_NAME = Convert.ToString(rw["PROCESS_NAME"]),
                                 MODEL_NAME = Convert.ToString(rw["MODEL_NAME"]),
                                 REAL_QTY = Convert.ToInt32(rw["REAL_QTY"]),
                                 PLAN_QTY = Convert.ToInt32(rw["PLAN_QTY"]),
                                 DIFFERENCE = Convert.ToInt32(rw["DIFFERENCE"]),
                                 LOSS_MIN = Convert.ToInt32(rw["LOSS_MIN"]),
                                 ACTUAL_LOSS = Convert.ToInt32(rw["ACTUAL_LOSS"])
                             }).ToList();
                return Ok(_list);
            }
            else
                return Content(HttpStatusCode.NotFound, "");
            #endregion
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
        internal string getSchema(string factory)
        {
            string ret = "";
            string sqlSchema = "select a.factory,a.schema from SFCS.c_factory_area_t a where a.factory_area = 'Thailand' and 1=1";
            sqlSchema = sqlSchema.Replace("1=1", "upper(a.factory) = '" + factory + "'");
            string connection = connectionBP;
            DataTable dtSchema = QueryDataTable(sqlSchema, connection);
            if (dtSchema.Rows.Count > 0)
                ret = "" + dtSchema.Rows[0]["SCHEMA"].ToString().Trim();
            else
                ret = "";

            return ret;
        }
    }
}