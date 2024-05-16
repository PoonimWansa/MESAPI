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

namespace MESAPI.Controllers
{
    public class FlexibleLinkController : ApiController
    {
        string connectionBP = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THPUBMES-SCAN)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = THPUBMES)));Password=Delta12345;User ID=MESAP03";

        // GET api/values
        public string Get(string MO_Number, string Group_Name)
        {
            string ret = "OKKKK";
            if (MO_Number == "" || Group_Name == "")
                ret = "ERROR : Please input Data";
            else
            {
                ret = getDataLink(MO_Number, Group_Name);
            }
            return ret;
        }
        internal string getDataLink(string MO_Number, string Group_Name)
        {
            string ret = "";
            string sql = "select s.link_name, x.model_name, x.group_name, x.sn_qty, x.parent_flag from DET_AM.c_model_link_setup_t x " +
                "inner join DET_AM.c_model_link_name_t s on x.id_linkname = s.id where x.id_linkname in (select t.id_linkname " +
                "from DET_AM.c_model_link_setup_t t inner join det_am.r_mo_base_t a on a.model_name = t.model_name " +
                "where t.group_name = '" + Group_Name + "' and a.mo_number = '" + MO_Number + "') and x.visible = 0";
            string connection = connectionBP;
            DataTable dt = QueryDataTable(sql, connection);
            if (dt.Rows.Count > 0)
                ret = "" + DataTableToJSONWithJSONNet(dt);
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
        internal string DataTableToJSONWithJSONNet(DataTable table)
        {
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(table);
            return JSONString;
        }
    }
}