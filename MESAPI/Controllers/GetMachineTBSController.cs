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
    public class GetMachineTBSController : ApiController
    {
        string connectionReport = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THBPODSMRACDB)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = THSFDB)));Password=tmudk$o0;User ID=PQM_QUERY";

        [System.Web.Http.HttpGet()]
        [Route("api/machine/tbs/{EQUIPMENT_CODE}/period/{PERIOD}")]
        ///api/machine/tbs/{Equipment_Code}/period/{Period}
        public IHttpActionResult GetMachineTBS(string EQUIPMENT_CODE,string PERIOD)
        {
            //matlist.Add(m2);
            if (EQUIPMENT_CODE == null || PERIOD == null)
                return Content(HttpStatusCode.NotFound, "Please check input data");
            if(PERIOD.ToUpper().Trim() != "DAY" && PERIOD.ToUpper().Trim() != "WEEK" && PERIOD.ToUpper().Trim() != "MONTH")
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string equip = EQUIPMENT_CODE.Trim();
            string period = PERIOD.Trim().ToUpper();
            string speriod = period == "DAY" ? "1" : (period == "WEEK" ? "7" : "30");
            #region getDataMain
            string sqlm = " select to_char(trunc(begindate),'YYYYMMDD') as DATETIME,round(avg(tbs),2) as TBS from " +
                " ( " +
                " select tbs.last_end_point lastend, " +
                "          tbs.begin_point begindate, " +
                "          tbs.end_point enddate, " +
                "          round(ceil((tbs.begin_point-tbs.last_end_point)*24*60*60)/60,2) TBS, " +
                "          round(ceil((tbs.end_point-tbs.begin_point)*24*60*60)/60,2) TBS2, " +
                "          tbs.equipment_id " +
                "     from det_cndc.r_equip_tbs_t tbs " +
                "    inner join det_cndc.C_EQUIPMENT_BASIC_EDMS_T bas " +
                "    on tbs.EQUIPMENT_ID = bas.EQUIPMENT_ID " +
                " where bas.equipment_code = '"+equip+"' " +
                " and tbs.END_POINT is not null " +
                " and trunc(tbs.END_POINT)  > trunc(sysdate - "+ speriod + ") " +
                " ) " +
                " group by trunc(begindate) " +
                " order by trunc(begindate) ";
            DataTable dtm = QueryDataTable(sqlm, connectionReport);
            List<MachineTBSModel> _lMachineTBSModel = new List<MachineTBSModel>();
            if (dtm.Rows.Count > 0)
            {
                _lMachineTBSModel = DataTableToList<MachineTBSModel>(dtm);
            }
            #endregion
            return Ok(_lMachineTBSModel);
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
    }
}