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
    public class GetMachinePostDataController : ApiController
    {
        string connectionReport = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THBPODSMRACDB)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = THSFDB)));Password=tmudk$o0;User ID=PQM_QUERY";

        [System.Web.Http.HttpGet()]
        [Route("api/{FACTORY}/machine/post/{EQUIPMENT_CODE}")]
        ///api/machine/tbs/{Equipment_Code}/period/{Period}
        public IHttpActionResult GetMachinePostData(string FACTORY,string EQUIPMENT_CODE)
        {
            //matlist.Add(m2);
            if (FACTORY == null || EQUIPMENT_CODE == null)
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string equip = EQUIPMENT_CODE.Trim().ToUpper();
            #region getDataMain
            string sqlm = " select CAST(A.EQUIPMENT_ID AS VARCHAR2(2000)) as EQUIPMENT_ID, " +
                " M.EQUIPMENT_CODE as INTERFACE_ID, " +
                " A.STATUS, " +
                " A.STATUS_CODE, " +
                " A.PASS_QTY, " +
                " A.FAIL_QTY, " +
                " A.WARNING_CNT as ERROR_CNT, " +
                " A.WARNING_TIME as ERROR_TIMES, " +
                " A.CYCLE_TIME, " +
                " A.RUNNING_TIME, " +
                " A.WAITING_TIME, " +
                " A.SELF_CHECK, " +
                " A.INPUT_QTY, " +
                " A.BARCODE, " +
                " A.MODEL_NAME as MODEL, " +
                " to_char(A.COLLECT_DATE,'YYYY-MM-DD HH24:MI:SS') as COLLECT_DATE, " +
                " B.PARAM_CODE, " +
                " B.PARAM_VALUE, " +
                " '0' as LOCATION " +
                " from " +
                " "+FACTORY+".R_EQUIPMENT_BASIC_DATA_T A " +
                " LEFT JOIN "+ FACTORY + ".c_equipment_basic_t M " +
                " ON A.EQUIPMENT_ID = M.EQUIPMENT_ID " +
                " LEFT JOIN "+ FACTORY + ".R_EQUIPMENT_PARAM_RT_T B " +
                " ON B.EQUIPMENT_ID = M.EQUIPMENT_ID " +
                " WHERE  M.EQUIPMENT_CODE = '"+equip+"' ";
            DataTable dtm = QueryDataTable(sqlm, connectionReport);
            MachinePostDataModel _lDataAll = new MachinePostDataModel();
            List<MachinePostDataDetailModel> _lMachinePostDataDetailModel = new List<MachinePostDataDetailModel>();
            if (dtm.Rows.Count > 0)
            {
                try
                {

                    DataTable _dt2 = dtm.Select().CopyToDataTable().DefaultView.ToTable(false, "PARAM_CODE", "PARAM_VALUE", "LOCATION");
                    _lMachinePostDataDetailModel = DataTableToList<MachinePostDataDetailModel>(_dt2);
                    _lDataAll.EQUIPMENT_ID = dtm.Rows[0]["EQUIPMENT_ID"].ToString();
                    _lDataAll.INTERFACE_ID = dtm.Rows[0]["INTERFACE_ID"].ToString();
                    _lDataAll.STATUS = dtm.Rows[0]["STATUS"].ToString();
                    _lDataAll.STATUS_CODE = dtm.Rows[0]["STATUS_CODE"].ToString();
                    _lDataAll.PASS_QTY = dtm.Rows[0]["PASS_QTY"].ToString();
                    _lDataAll.FAIL_QTY = dtm.Rows[0]["FAIL_QTY"].ToString();
                    _lDataAll.ERROR_CNT = dtm.Rows[0]["ERROR_CNT"].ToString();
                    _lDataAll.ERROR_TIMES = dtm.Rows[0]["ERROR_TIMES"].ToString();
                    _lDataAll.CYCLE_TIME = dtm.Rows[0]["CYCLE_TIME"].ToString();
                    _lDataAll.RUNNING_TIME = dtm.Rows[0]["RUNNING_TIME"].ToString();
                    _lDataAll.WAITING_TIME = dtm.Rows[0]["WAITING_TIME"].ToString();
                    _lDataAll.SELF_CHECK = dtm.Rows[0]["SELF_CHECK"].ToString();
                    _lDataAll.INPUT_QTY = dtm.Rows[0]["INPUT_QTY"].ToString();
                    _lDataAll.BARCODE = dtm.Rows[0]["BARCODE"].ToString();
                    _lDataAll.MODEL = dtm.Rows[0]["MODEL"].ToString();
                    _lDataAll.COLLECT_DATE = dtm.Rows[0]["COLLECT_DATE"].ToString();
                    _lDataAll.PARAM_LIST = _lMachinePostDataDetailModel;
                    return Ok(_lDataAll);

                }

                catch (Exception ex)
                { 
                    return Content(HttpStatusCode.NotFound, ex.Message); 
                }
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
    }
}