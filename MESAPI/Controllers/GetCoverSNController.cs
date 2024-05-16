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
using MESAPI.ComFunc;
using System.Web;

namespace MESAPI.Controllers
{
    public class GetCoverSNController : ApiController
    {
        [System.Web.Http.HttpPost()]
        public IHttpActionResult FPYRDataAll(CoverModel CoverModel)
        {

            List<SerailDetailModel> _MatList = new List<SerailDetailModel>();
            //matlist.Add(m2MoRework
            if (CoverModel == null || CoverModel.Factory == null || CoverModel.Cover_SN == null)
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string factory = "" + CoverModel.Factory.Trim();
            string Cover_SN = "" + CoverModel.Cover_SN.Trim();


            if (factory == "" || Cover_SN == "")
            {
                return Content(HttpStatusCode.NotFound, "Please check input data");
            }
            else
            {

                #region getReworkData
                string sql = @"SELECT * FROM R_SN_LINK_COVER_T where cover_sn ='" + Cover_SN + "'";


                DataTable dt = QueryDataTable(sql, conDatabase(factory));
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow ds in dt.Rows)
                    {


                        SerailDetailModel item = new SerailDetailModel();
                        item.COVER_SN = ds["COVER_SN"].ToString();
                        item.SERIAL_NUMBER = ds["SERIAL_NUMBER"].ToString();
                        item.LINK_TIME = ds["LINK_TIME"].ToString();
                        item.MODEL_NAME = ds["MODEL_NAME"].ToString();
                        item.MO_NUMBER = ds["MO_NUMBER"].ToString();

                        _MatList.Add(item);

                    }


                    //}


                    #endregion
                }
                return Ok(_MatList);
            }
            //return Ok(_MatList);
        }

        public static string connection7()
        {
            string connection7 = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
       + "(HOST = thwgrmesdb00.delta.corp)(PORT = 1521))"
       + "(CONNECT_DATA = (SERVER = DEDICATED)"
       + "(SERVICE_NAME = DETBCWG)));Password=detcndc2007man;User ID=DET_CNDC";
            return connection7;
        }
        internal string conDatabase(string factory)
        {
            var conDB = "";
            if (factory == "DET_AM")
            {
                conDB = Comfunction.connection1();
            }
            else if (factory == "DET_DNI")
            {
                conDB = Comfunction.connection3();
            }
            else if (factory == "DET_MP")
            {
                conDB = Comfunction.connection4();
            }
            else if (factory == "DET_HP")
            {
                conDB = Comfunction.connection5();
            }
            else if (factory == "DET_CNDC")
            {
                conDB = connection7();
            }
            else if (factory == "DET_FM")
            {
                conDB = Comfunction.connection6();
            }
            else if (factory == "DET_MS")
            {
                conDB = Comfunction.connectionMS();
            }
            else
            {
                return ("Plese check your factory if it is correct");
            }
            return conDB;
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
        internal DataTable getLine(string prod_area, string factory)
        {

            string sqlLine = "select line_name from c_line_desc_t l"
                                + " inner join sfcs.c_prod_area_t a "
                                + " on a.prod_area_id = l.prod_area_id"
                                + " where a.prod_area_code = '" + prod_area + "'";
            //string connection = connection3;
            DataTable dtLine = QueryDataTable(sqlLine, conDatabase(factory));

            return dtLine;
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