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
    public class SAPPlantListController : ApiController
    {
        private List<MO2Model> molist = null;
        string connectionBP = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THPUBMES-SCAN)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = THPUBMES)));Password=Delta12345;User ID=MESAP03";

        [System.Web.Http.HttpPost()]
        public IHttpActionResult SAPPlantList()
        {
            #region getData
            string sql = "select SAP_PLANT,FACTORY,SCHEMA as MES_PLANT  from sfcs.c_sap_plant_all_v order by SAP_PLANT";
            DataTable dt = QueryDataTable(sql, connectionBP);
            if (dt.Rows.Count > 0)
            {
                var _Plant = (from rw in dt.AsEnumerable()
                               select new
                               {
                                   SAP_PLANT = Convert.ToString(rw["SAP_PLANT"]),
                                   FACTORY = Convert.ToString(rw["FACTORY"]),
                                   MES_PLANT = Convert.ToString(rw["MES_PLANT"])
                               }).ToList();
                return Ok(_Plant);
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
    }
}