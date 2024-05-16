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
    public class GetMachineInfoController : ApiController
    {
        string connectionReport = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THBPODSMRACDB)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = THSFDB)));Password=tmudk$o0;User ID=PQM_QUERY";

        [System.Web.Http.HttpGet()]
        [Route("api/machine/information/{EQUIPMENT_CODE}")]
        ///api/machine/information/{Equipment_Code}
        public IHttpActionResult GetMachineInfo(string EQUIPMENT_CODE)
        {
            //matlist.Add(m2);
            if (EQUIPMENT_CODE == null)
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string equip = EQUIPMENT_CODE.Trim();// "" + m2.PROD_AREA.Trim();
            #region getDataMain
            string sqlm = " select " +
                " CAST(E1.EQUIPMENT_ID AS VARCHAR2(2000)) as EQUIPMENT_ID ,T.TYPE_CODE as EQUIPMENT_TYPE,E1.DESCRIPTION as MACHINE_NAME, " +
                " L1.STATION_NAME,S.GROUP_NAME, " +
                " ('http://10.150.192.16/images/'||T.TYPE_CODE||'.jpg') as IMAGE_URL, " +
                " L1.LINE_NAME, " +
                " (case ST.STATUS when 1 then 'Error' when 0 then 'Normal' when -1 then 'offline' else 'Warning' end) EQUIPMENT_STATUS, " +
                " ST.STATUS_CODE as EQUIPMENT_STATUS_CODE " +
                " from DET_CNDC.C_EQUIPMENT_BASIC_EDMS_T E1 " +
                " inner join sfcs.C_EQUIP_TYPE_EDMS_T T " +
                " on E1.TYPE_ID = T.TYPE_ID " +
                " inner join DET_CNDC.C_LINE_STATION_T L1 " +
                " on E1.EQUIPMENT_ID = L1.EQUIPMENT_ID " +
                " inner join DET_CNDC.c_line_desc_t L2  " +
                " on L1.LINE_NAME = L2.LINE_NAME " +
                " inner join det_cndc.R_EQUIP_STATUS_T ST " +
                " on E1.EQUIPMENT_ID = ST.EQUIPMENT_ID " +
                " and ST.END_POINT is null " +
                " inner join DET_CNDC.C_STATION_CONFIG_T S " +
                " on L1.LINE_NAME = S.LINE_NAME " +
                " and L1.EQUIPMENT_ID = S.EQUIPMENT_ID " +
                " and L1.STATION_NAME = S.STATION_NAME " +
                " where L1.VALID_FLAG = '1' " +
                " and E1.EQUIPMENT_CODE = '" + equip + "' " +
                //" and S.PQM_NAME_EN is not null ";
                "";
            DataTable dtm = QueryDataTable(sqlm, connectionReport);
            List<MachineInfoModel> _lMachineInfoModel = new List<MachineInfoModel>();
            if (dtm.Rows.Count > 0)
            {
                _lMachineInfoModel = DataTableToList<MachineInfoModel>(dtm);
            }
            #endregion
            return Ok(_lMachineInfoModel);
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