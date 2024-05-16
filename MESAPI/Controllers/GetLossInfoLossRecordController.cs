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
    public class GetLossInfoLossRecordController : ApiController
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
        [Route("api/LossInformation/LossRecord/{MES_FACTORY}/{FACTORY}/{PROD_AREA}/{DATE}")]
        ///api/machine/information/{Equipment_Code}
        public IHttpActionResult GetLossInfoLossRecord(string MES_FACTORY, string FACTORY, string PROD_AREA, string DATE)
        {
            //matlist.Add(m2);
            if (MES_FACTORY == null || FACTORY == null || PROD_AREA == null || DATE  == null)
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string factory = FACTORY.Trim().ToUpper();
            string prodarea = PROD_AREA.Trim().ToUpper();
            string date = DATE.Trim();
            string schema = MES_FACTORY.Trim(); //getSchema(factory);
            //if (schema == "")
            //    return Content(HttpStatusCode.NotFound, "Please check input data");
            #region getDataMain
            string sql = "select  "
                + " G1.PROD_AREA_CODE as PROD_AREA, "
                + " G1.WORK_DATE, "
                + " G1.LINE_NAME, "
                + " G2.SECTION_RANGE as TIME_RANGE, "
                + " G1.MO_NUMBER, "
                + " G1.MODEL_NAME, "
                + " G1.LOSS_CODE, "
                + " G1.LOSS_DESC, "
                + " G1.BEGIN_TIME, "
                + " G1.END_TIME, "
                + " round( (to_date(G1.END_TIME,'YYYY-MM-DD HH24:MI')-to_date(G1.BEGIN_TIME,'YYYY-MM-DD HH24:MI'))*24*60 ) as LOSS_MIN, "
                + " G1.CAUSE_CODE as REASON_CODE, "
                + " G1.CAUSE_DESC as REASON_DESC, "
                + " G1.DEAL_WAY as COUNTERMEASURE_CODE, "
                + " G1.SOLUTION_DESC as COUNTERMEASURE_DESC, "
                + " G1.DEAL_MAN as COUNTERMEASURE_MAINTAINER, "
                + " G1.PROCESS_NAME, "
                + " G1.REL_DESC as DESCRIPTION, "
                + " G1.SET_EMP, "
                + " to_char(G1.SET_TIME,'YYYY-MM-DD HH24:MI:SS') as SET_TIME "
                + " from "
                + " ( "
                + " select  "
                + " A.* "
                + " ,C.PROD_AREA_CODE "
                + " from "+schema+".p_pqm_line_loss_t A , "+schema+".C_LINE_DESC_T B , "+schema+".C_PROD_AREA_T C "
                + " where A.LINE_NAME = B.LINE_NAME "
                + " and B.PROD_AREA_ID = C.PROD_AREA_ID "
                + " and 1=1 "//C.PROD_AREA_CODE = 'DET2_APABU_MAIN'
                             //+ " --and A.LINE_NAME = 'A06' "
                + " and B.VALID_FLAG = 'Y' "
                + " and A.work_date = '"+date+"' "
                + " and A.STAY_FLAG = 0 "
                + " order by A.begin_time "
                + " ) G1 "
                + " left join "
                + " ( select WORK_DATE,LINE_NAME,PROCESS_NAME,MODEL_NAME,SECTION_RANGE,SHOW_FLAG from "+schema+".R_PQM_DAILY_T A "
                + " union select WORK_DATE,LINE_NAME,PROCESS_NAME,MODEL_NAME,SECTION_RANGE,'Y' as SHOW_FLAG from "+schema+".R_PQM_DAILY_SMT_T B )G2 "
                + " on G1.WORK_DATE=G2.WORK_DATE "
                + " and G1.LINE_NAME = G2.LINE_NAME "
                + " and G1.PROCESS_NAME = G2.PROCESS_NAME "
                + " and G1.MODEL_NAME = G2.MODEL_NAME "
                + " and to_number ( replace(substr(G1.BEGIN_TIME,12,5),':','')) >= to_number(substr(G2.SECTION_RANGE,0,4)) "
                + " and to_number ( replace(substr(G1.BEGIN_TIME,12,5),':','')) < to_number(substr(G2.SECTION_RANGE,6,4)) "
                + " and G2.SHOW_FLAG = 'Y' "
                + " order by G1.LINE_NAME,G2.SECTION_RANGE,G1.PROCESS_NAME ";
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
                                 WORK_DATE = Convert.ToString(rw["WORK_DATE"]),
                                 LINE_NAME = Convert.ToString(rw["LINE_NAME"]),
                                 TIME_RANGE = Convert.ToString(rw["TIME_RANGE"]),
                                 MO_NUMBER = Convert.ToString(rw["MO_NUMBER"]),
                                 MODEL_NAME = Convert.ToString(rw["MODEL_NAME"]),
                                 LOSS_CODE = Convert.ToString(rw["LOSS_CODE"]),
                                 LOSS_DESC = Convert.ToString(rw["LOSS_DESC"]),
                                 BEGIN_TIME = Convert.ToString(rw["BEGIN_TIME"]),
                                 END_TIME = Convert.ToString(rw["END_TIME"]),
                                 LOSS_MIN = Convert.ToInt32(rw["LOSS_MIN"]),
                                 REASON_CODE = Convert.ToString(rw["REASON_CODE"]),
                                 REASON_DESC = Convert.ToString(rw["REASON_DESC"]),
                                 COUNTERMEASURE_CODE = Convert.ToString(rw["COUNTERMEASURE_CODE"]),
                                 COUNTERMEASURE_DESC = Convert.ToString(rw["COUNTERMEASURE_DESC"]),
                                 COUNTERMEASURE_MAINTAINER = Convert.ToString(rw["COUNTERMEASURE_MAINTAINER"]),
                                 PROCESS_NAME = Convert.ToString(rw["PROCESS_NAME"]),
                                 DESCRIPTION = Convert.ToString(rw["DESCRIPTION"]),
                                 SET_EMP = Convert.ToString(rw["SET_EMP"]),
                                 SET_TIME = Convert.ToString(rw["SET_TIME"])
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