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
    public class GetMachineUtiController : ApiController
    {
        string connectionReport = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THBPODSMRACDB)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = THSFDB)));Password=tmudk$o0;User ID=PQM_QUERY";

        [System.Web.Http.HttpGet()]
        [Route("api/machine/utilization/{EQUIPMENT_CODE}/period/{PERIOD}")]
        ///api/machine/utilization/{Equipment_Code}/period/{Period}
        public IHttpActionResult GetMachineUti(string EQUIPMENT_CODE,string PERIOD)
        {
            //matlist.Add(m2);
            if (EQUIPMENT_CODE == null || PERIOD == null)
                return Content(HttpStatusCode.NotFound, "Please check input data");
            if (PERIOD.ToUpper().Trim() != "DAY" && PERIOD.ToUpper().Trim() != "WEEK" && PERIOD.ToUpper().Trim() != "MONTH")
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string equip = EQUIPMENT_CODE.Trim();
            string period = PERIOD.Trim().ToUpper();
            string speriod = period == "DAY" ? "1" : (period == "WEEK" ? "7" : "30");
            //DateTime timenow = DateTime.Now;
            //DateTime sectionFrom = new DateTime(timenow.Year, timenow.Month, timenow.Day, 7, 30, 0);
            //DateTime sectionFrom2 = new DateTime(timenow.Year, timenow.Month, timenow.Day, 20, 0, 0);
            //string sSectionFrom = (timenow >= sectionFrom && timenow < sectionFrom2 ? sectionFrom.ToString("yyyyMMddHHmm") : sectionFrom2.ToString("yyyyMMddHHmm"));
            #region getDataMain
            string sqlm = " select pub.work_date as DATETIME,pub.uti as UTILIZATION_RATE " +
                " from (SELECT work_date,equipment_id, round(nvl(green_light_time,0) / 1440*100, 2) uti ,'1' as type " +
                " FROM det_cndc.r_equip_active_daily_t " +
                " where equipment_id =(select equipment_id from DET_CNDC.c_equipment_basic_edms_t where equipment_code ='" + equip + "') " +
                " and work_date > to_char(sysdate - " + speriod + ", 'yyyymmdd') " +
                " and work_date <> to_char(sysdate, 'yyyymmdd') " +
                " and " + speriod + " <> 1 " +
                " union all " +
                " SELECT work_date,equipment_id, round(nvl(day_uti, 0)*100,2) uti ,'2' as type " +
                " FROM det_cndc.r_equip_active_daily_shift_t " +
                " where equipment_id = (select equipment_id from DET_CNDC.c_equipment_basic_edms_t where equipment_code ='" + equip + "') " +
                " and work_date = to_char(sysdate, 'yyyymmdd') " +
                " and update_date = work_date " +
                " and " + speriod + " <> 1  " +
                " union all " +
                " SELECT work_date,equipment_id, round(nvl(uti, 0)*100,2) uti ,'4' as type " +
                " FROM det_cndc.r_equip_active_daily_shift_t " +
                " where equipment_id = (select equipment_id from DET_CNDC.c_equipment_basic_edms_t where equipment_code ='" + equip + "') " +
                " and work_date = to_char(sysdate, 'yyyymmdd') " +
                " and " + speriod + " = 1 " +
                " ) pub " +
                " order by pub.work_date ";
            DataTable dtm = QueryDataTable(sqlm, connectionReport);
            List<MachineUtiModel> _lMachineUtiModel = new List<MachineUtiModel>();
            if (dtm.Rows.Count > 0)
            {
                _lMachineUtiModel = DataTableToList<MachineUtiModel>(dtm);
            }
            #endregion
            return Ok(_lMachineUtiModel);
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