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
    public class GetMachineListByLineNameController : ApiController
    {
        string connectionReport = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THBPODSMRACDB)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = THSFDB)));Password=tmudk$o0;User ID=PQM_QUERY";

        [System.Web.Http.HttpGet()]
        [Route("api/machine/whole_line/{LINE_NAME}")]
        ///api/machine/whole_line/{line_name}
        public IHttpActionResult GetMachineListByLineName(string LINE_NAME)
        {
            //matlist.Add(m2);
            if (LINE_NAME == null )
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string line_name = LINE_NAME.Trim();// "" + m2.PROD_AREA.Trim();
            #region getDataMain
            string sqlm = " select  " +
                " E1.DESCRIPTION as MACHINE_NAME,CAST(E1.EQUIPMENT_ID AS VARCHAR2(2000)) as EQUIPMENT_ID ,E1.EQUIPMENT_CODE,T.TYPE_CODE as EQUIPMENT_TYPE, " +
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
                " where L1.VALID_FLAG = '1' " +
                " and L1.LINE_NAME = '"+line_name+"' " +
                " order by T.TYPE_CODE,E1.EQUIPMENT_ID ";
            DataTable dtm = QueryDataTable(sqlm, connectionReport);
            List<MachineListbyLineNameModel> _lDataAll = new List<MachineListbyLineNameModel>();
            List<MachineDetailModel> _lMachineDetailModel = new List<MachineDetailModel>();
            List<string> slist = new List<string>();
            if (dtm.Rows.Count > 0)
            {
                _lMachineDetailModel = DataTableToList<MachineDetailModel>(dtm);
                //_lDataAllSub.Clear();
                //_lDataAllSub.Add(_DataAllSub);

                MachineListbyLineNameModel _DataAll = new MachineListbyLineNameModel();
                _DataAll.MACHINE_LIST = _lMachineDetailModel;
                _lDataAll.Add(_DataAll);
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
    }
}