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
    public class GetLineAchRateController : ApiController
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
        [Route("api/LineAchRate/{MES_FACTORY}/{FACTORY}/{PROD_AREA}/{LINE_NAME}/{DATE}")]
        ///api/machine/information/{Equipment_Code}
        public IHttpActionResult GetLineAchRate(string MES_FACTORY, string FACTORY, string PROD_AREA, string LINE_NAME, string DATE)
        {
            //matlist.Add(m2);
            if (MES_FACTORY == null || FACTORY == null || PROD_AREA == null || LINE_NAME == null || DATE  == null)
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string factory = FACTORY.Trim().ToUpper();
            string prodarea = PROD_AREA.Trim().ToUpper();
            string date = DATE.Trim();
            string linename = LINE_NAME.Trim().ToUpper();
            string schema = MES_FACTORY.Trim();//getSchema(factory);
            //if (schema == "")
            //    return Content(HttpStatusCode.NotFound, "Please check input data");
            #region getDataMain
            string sql = "select G.* from ( "
                + "( "
                + "select  "
                + " C.FACTORY, "
                + " c.prod_area_code as PROD_AREA, "
                + " A.LINE_NAME, "
                + " A.WORK_DATE, "
                + " A.PROCESS_NAME as MONITOR_SECTION, "
                + " round( (SUM(SUM_REAL)/SUM(SUM_PLAN)*100),2) as ACH_RATE, "
                + " SUM(SUM_PLAN) as PLANNED_OUTPUT, "
                + " SUM(SUM_REAL) as ACTUAL_OUTPUT, "
                + " SUM(SUM_PLAN)-SUM(SUM_REAL) as DIFFERENCE, "
                + " round( (SUM(SUM_PLAN)-SUM(SUM_REAL))/SUM(SUM_PLAN)*100,2) as DELAY_RATE "
                + " from " + schema + ".r_pqm_summary_t A, " + schema + ".C_LINE_DESC_T B , " + schema + ".C_PROD_AREA_T C "
                + " where A.LINE_NAME = B.LINE_NAME "
                + " and B.PROD_AREA_ID = C.PROD_AREA_ID "
                + " and 1=1 "// C.PROD_AREA_CODE = 'DET2_APABU_MAIN' "
                + " and 2=2 "//--and A.LINE_NAME = 'A06' "
                + " and B.VALID_FLAG = 'Y' "
                + " and A.work_date = '" + date + "' "
                + " group by c.FACTORY,c.prod_area_code,A.LINE_NAME,A.WORK_DATE,A.PROCESS_NAME "
                + ") "
                + "union"
                + "( "
                + "select  "
                + " C.FACTORY, "
                + " c.prod_area_code as PROD_AREA, "
                + " A.LINE_NAME, "
                + " A.WORK_DATE, "
                + " A.PROCESS_NAME as MONITOR_SECTION, "
                + " round( (SUM(SUM_REAL)/SUM(SUM_PLAN)*100),2) as ACH_RATE, "
                + " SUM(SUM_PLAN) as PLANNED_OUTPUT, "
                + " SUM(SUM_REAL) as ACTUAL_OUTPUT, "
                + " SUM(SUM_PLAN)-SUM(SUM_REAL) as DIFFERENCE, "
                + " round( (SUM(SUM_PLAN)-SUM(SUM_REAL))/SUM(SUM_PLAN)*100,2) as DELAY_RATE "
                + " from " + schema + ".r_pqm_summary_smt_t A, " + schema + ".C_LINE_DESC_T B , " + schema + ".C_PROD_AREA_T C "
                + " where A.LINE_NAME = B.LINE_NAME "
                + " and B.PROD_AREA_ID = C.PROD_AREA_ID "
                + " and 1=1 "// C.PROD_AREA_CODE = 'DET2_APABU_MAIN' "
                + " and 2=2 "//--and A.LINE_NAME = 'A06' "
                + " and B.VALID_FLAG = 'Y' "
                + " and A.work_date = '" + date + "' "
                + " group by c.FACTORY,c.prod_area_code,A.LINE_NAME,A.WORK_DATE,A.PROCESS_NAME "
                + ") "
                + ") G "
                + " order by FACTORY,prod_area,LINE_NAME,WORK_DATE,MONITOR_SECTION";
            if (prodarea != "ALL")
                sql = sql.Replace("1=1", "C.PROD_AREA_CODE = '" + prodarea + "'");
            else
                sql = sql.Replace("1=1", "C.FACTORY = '"+factory+"'");

            if (linename != "ALL")
                sql = sql.Replace("2=2", "A.LINE_NAME = '"+linename+"'");

            DataTable dt = QueryDataTable(sql, connectionReport);
            if (dt.Rows.Count > 0)
            {
                var _list = (from rw in dt.AsEnumerable()
                             select new
                             {
                                 FACTORY = Convert.ToString(rw["FACTORY"]),
                                 PROD_AREA = Convert.ToString(rw["PROD_AREA"]),
                                 LINE_NAME = Convert.ToString(rw["LINE_NAME"]),
                                 WORK_DATE = Convert.ToString(rw["WORK_DATE"]),
                                 MONITOR_SECTION = Convert.ToString(rw["MONITOR_SECTION"]),
                                 ACH_RATE = Convert.ToDouble(rw["ACH_RATE"]),
                                 PLANNED_OUTPUT = Convert.ToInt32(rw["PLANNED_OUTPUT"]),
                                 ACTUAL_OUTPUT = Convert.ToInt32(rw["ACTUAL_OUTPUT"]),
                                 DIFFERENCE = Convert.ToInt32(rw["DIFFERENCE"]),
                                 DELAY_RATE = Convert.ToDouble(rw["DELAY_RATE"])
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