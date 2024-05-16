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
    public class MOListByPlantController : ApiController
    {
        private List<MO2Model> molist = null;
        string connectionBP = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THPUBMES-SCAN)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = THPUBMES)));Password=Delta12345;User ID=MESAP03";
        string connectionWG = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THWGRMESDB00)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = DETBCWG)));Password=Delta12345;User ID=MESAP03";

        [System.Web.Http.HttpPost()]
        public IHttpActionResult MOListByPlant(MO2Model m2)
        {
            //matlist.Add(m2);
            if (m2 == null || m2.Plant == null ||m2.SMTFlag.ToString() == "" )
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string plant = ""+m2.Plant.Trim().ToUpper();
            bool smtflag = m2.SMTFlag;  //False = mainline   True = smt
            if (plant == "" || smtflag.ToString() == "")
            {
                return Content(HttpStatusCode.NotFound, "Please check input data");
            }
            else
            {
                string schema = getSchemabySAPPlant(plant);
                if (schema != "")
                {
                    #region getData
                    string sTable = (smtflag== false) ? "r_mo_base_t" : "r_mo_smt_t";
                        string sql = "select MO_NUMBER,MODEL_NAME,DEFAULT_LINE as LINE_NAME,TARGET_QTY as TARGET,CLOSE_FLAG as STATUS from " + schema+"."+ sTable +
                        " where mo_create_date > sysdate - (6 * 30) and close_flag in (2,3,4) and upper(plantname) = '" + plant + "' order by mo_number asc";
                    DataTable dt = QueryDataTable(sql, fGetConnectionBPWG(schema));
                    if (dt.Rows.Count > 0)
                    {
                        //List<MoModel> _molist = new List<MoModel>();
                        //_molist = DataTableToList<MoModel>(dt);
                        var _molist = (from rw in dt.AsEnumerable()
                                       select new
                                       {
                                           MO_NUMBER = Convert.ToString(rw["MO_NUMBER"]),
                                           MODEL_NAME = Convert.ToString(rw["MODEL_NAME"]),
                                           LINE_NAME = Convert.ToString(rw["LINE_NAME"]),
                                           TARGET = Convert.ToInt32(rw["TARGET"]),
                                           STATUS = Convert.ToInt32(rw["STATUS"])
                                       }).ToList();
                        return Ok(_molist);
                    }
                    else
                        return Content(HttpStatusCode.NotFound, "");
                    #endregion
                }
                else
                    return Content(HttpStatusCode.NotFound, "Please check sap-plant");
            }
            //return Ok(new { factory = m2.Factory, materialNo = m2.MaterialNo, serialNumber = m2.SerialNumber });
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
        internal string getSchemabySAPPlant(string plant)
        {
            string ret = "";
            string sqlSchema = "select SCHEMA from sfcs.c_sap_plant_all_v where 1=1";
            sqlSchema = sqlSchema.Replace("1=1", "upper(SAP_PLANT) = '" + plant + "'");
            string connection = connectionBP;
            DataTable dtSchema = QueryDataTable(sqlSchema, connection);
            if (dtSchema.Rows.Count > 0)
                ret = "" + dtSchema.Rows[0]["SCHEMA"].ToString().Trim();
            else
                ret = "";

            return ret;
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
        internal string fGetConnectionBPWG(string schema)
        {
            string ret = "";
            List<string> sSchemaBP = new List<string>()
                { "DET_AM", "DET_CN" ,"DET_DC","SFISM4","DET_MP","DET_PS","DET_HP","DET_MS" };
            List<string> sSchemaWG = new List<string>()
                {"DET_FM","DET_CNDC"};
            if (sSchemaBP.Contains(schema))
                ret = connectionBP;
            else
                ret = connectionWG;

            return ret;
        }
        internal string DataTableToJSONWithJSONNet(DataTable table)
        {
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(table);
            return JSONString;
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