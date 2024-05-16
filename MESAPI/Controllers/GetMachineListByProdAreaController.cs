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
    public class GetMachineListByProdAreaController : ApiController
    {
        string connectionReport = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THBPODSMRACDB)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = THSFDB)));Password=tmudk$o0;User ID=PQM_QUERY";

        [System.Web.Http.HttpGet()]
        [Route("api/machine/whole_plant/{PROD_AREA}")]
        ///api/machine/whole_plant/{Prod_Area_ID}
        public IHttpActionResult GetMachineListByProdArea(string PROD_AREA)
        {
            //matlist.Add(m2);
            if (PROD_AREA == null)
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string prod_area = PROD_AREA.Trim();// "" + m2.PROD_AREA.Trim();
            #region List Line
            string sqlListLine = "select L.LINE_NAME from DET_CNDC.c_line_desc_t L inner join sfcs.c_prod_area_t PA on L.PROD_AREA_ID = PA.PROD_AREA_ID " +
                " where PA.PROD_AREA_CODE = '"+prod_area+"' and L.VALID_FLAG = 'Y' order by L.LINE_NAME";
            DataTable dtListLine = QueryDataTable(sqlListLine, connectionReport);

            #endregion
            #region getDataMain
            string sqlm = " select " +
                " L1.LINE_NAME,E1.DESCRIPTION as MACHINE_NAME,CAST(E1.EQUIPMENT_ID AS VARCHAR2(2000)) as EQUIPMENT_ID ,E1.EQUIPMENT_CODE,T.TYPE_CODE as EQUIPMENT_TYPE, " +
                " (case ST.STATUS when 1 then 'Error' when 0 then 'Normal' when -1 then 'offline' else 'Warning' end) EQUIPMENT_STATUS, " +
                " ST.STATUS_CODE as EQUIPMENT_STATUS_CODE " +
                " from DET_CNDC.C_EQUIPMENT_BASIC_EDMS_T E1 " +
                " inner join sfcs.C_EQUIP_TYPE_EDMS_T T " +
                " on E1.TYPE_ID = T.TYPE_ID " +
                " inner join DET_CNDC.C_LINE_STATION_T L1 " +
                " on E1.EQUIPMENT_ID = L1.EQUIPMENT_ID " +
                " inner join DET_CNDC.c_line_desc_t L2  " +
                " on L1.LINE_NAME = L2.LINE_NAME " +
                " inner join sfcs.c_prod_area_t PA " +
                " on L2.PROD_AREA_ID = PA.PROD_AREA_ID " +
                " inner join det_cndc.R_EQUIP_STATUS_T ST " +
                " on E1.EQUIPMENT_ID = ST.EQUIPMENT_ID " +
                " and ST.END_POINT is null " +
                " where PA.PROD_AREA_CODE = '" + prod_area + "' " +
                " and L1.VALID_FLAG = '1' order by L1.LINE_NAME,E1.EQUIPMENT_CODE";
            DataTable dtm = QueryDataTable(sqlm, connectionReport);
            List<MachineListbyProdAreaModel> _lDataAll = new List<MachineListbyProdAreaModel>();
            //List<MachineSubModel> _lDataAllSub = new List<MachineSubModel>();
            //MachineSubModel _DataAllSub = new MachineSubModel();
            List<MachineDetailModel> _lMachineDetailModel = new List<MachineDetailModel>();
            if (dtm.Rows.Count > 0)
            {
                var uniqueObjects = dtm.AsEnumerable().Select(x => x.Field<string>("LINE_NAME")).Distinct().ToList();
                foreach (DataRow rowListLine in dtListLine.Rows)
                {
                    string line = rowListLine["LINE_NAME"].ToString();
                    if (uniqueObjects.Contains(line))
                    {
                        DataTable dtn = dtm.AsEnumerable()
                       .Where(x => x.Field<string>("LINE_NAME") == line)
                       .CopyToDataTable();
                        dtn.Columns.Remove("LINE_NAME");
                        _lMachineDetailModel = DataTableToList<MachineDetailModel>(dtn);

                        MachineListbyProdAreaModel _DataAll = new MachineListbyProdAreaModel();
                        _DataAll.LINE_NAME = line;
                        _DataAll.MACHINE_LIST = _lMachineDetailModel;
                        _lDataAll.Add(_DataAll);
                    }
                    else
                    {
                        DataTable dtnew = new DataTable();

                        _lMachineDetailModel = DataTableToList<MachineDetailModel>(dtnew);
                        MachineListbyProdAreaModel _DataAll = new MachineListbyProdAreaModel();
                        _DataAll.LINE_NAME = line;
                        _DataAll.MACHINE_LIST = _lMachineDetailModel; 
                        _lDataAll.Add(_DataAll);
                    }
                    //foreach (var linename in uniqueObjects)
                    //{
                    //    DataTable dtn = dtm.AsEnumerable()
                    //   .Where(x => x.Field<string>("LINE_NAME") == linename.ToString())
                    //   .CopyToDataTable();
                    //    dtn.Columns.Remove("LINE_NAME");
                    //    //_DataAllSub.LINE_NAME = linename.ToString();
                    //    _lMachineDetailModel = DataTableToList<MachineDetailModel>(dtn);
                    //    //_DataAllSub.MACHINE_LIST = _lMachineDetailModel;
                    //    //_lDataAllSub.Clear();
                    //    //_lDataAllSub.Add(_DataAllSub);

                    //    MachineListbyProdAreaModel _DataAll = new MachineListbyProdAreaModel();
                    //    _DataAll.LINE_NAME = linename.ToString();
                    //    _DataAll.MACHINE_LIST = _lMachineDetailModel;
                    //    _lDataAll.Add(_DataAll);

                    //}
                }
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