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

namespace MESAPI.Controllers
{
    public class RepairController : ApiController
    {
        [System.Web.Http.HttpPost()]
        public IHttpActionResult RepairData(ReWModel MoRework)
        {
            List<RepairModel> _MatList = new List<RepairModel>();
            //matlist.Add(m2MoRework
            if (MoRework == null || MoRework.Factory == null || MoRework.Mo_number == null)
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string factory = "" + MoRework.Factory.Trim();
            string mo_number = "" + MoRework.Mo_number.Trim();


            if (factory == "" || mo_number == "")
            {
                return Content(HttpStatusCode.NotFound, "Please check input data");
            }
            else
            {

                //DataTable line = getLine(mo_number.ToUpper(), factory);

                //foreach (DataRow roopline in line.Rows)
                //{
                #region getRepairData
                string sql = @"select SERIAL_NUMBER,MO_NUMBER,MODEL_NAME,LINE_NAME,SECTION_NAME,GROUP_NAME,CASE to_number(ERROR_FLAG) WHEN 1 THEN 'FAIL' else 'PASS' end as RESULT,IN_STATION_TIME from r_sn_detail_t where serial_number in (select SERIAL_NUMBER from r_sn_detail_t where mo_number ='" + mo_number + "' and section_name = 'REPAIR')";


                DataTable dt = QueryDataTable(sql, conDatabase(factory));
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow ds in dt.Rows)
                    {


                        RepairModel item = new RepairModel();
                        item.SERIAL_NUMBER = ds["SERIAL_NUMBER"].ToString();
                        item.MO_NUMBER = ds["MO_NUMBER"].ToString();
                        item.MODEL_NAME = ds["MODEL_NAME"].ToString();
                        item.LINE_NAME = ds["LINE_NAME"].ToString();
                        item.SECTION_NAME = ds["SECTION_NAME"].ToString();
                        item.GROUP_NAME = ds["GROUP_NAME"].ToString();
                        item.RESULT = ds["RESULT"].ToString();
                        item.TEST_DATE = ds["IN_STATION_TIME"].ToString();

                        _MatList.Add(item);

                    }


                    //}


                    #endregion
                }
                return Ok(_MatList);
            }
            //return Ok(_MatList);
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
                conDB = Comfunction.connection7();
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