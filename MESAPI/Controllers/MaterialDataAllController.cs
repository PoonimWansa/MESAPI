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
    public class MaterialDataAllController : ApiController
    {
        private List<Mat2Model> matlist = null;
        string connectionBP = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THPUBMES-SCAN)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = THPUBMES)));Password=Delta12345;User ID=MESAP03";
        string connectionWG = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THWGRMESDB00)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = DETBCWG)));Password=Delta12345;User ID=MESAP03";

        public MaterialDataAllController()
        {
            matlist = new List<Mat2Model>()
            {
                new Mat2Model(){Factory="A",MaterialNo="B",SerialNumber="C" },
                new Mat2Model(){Factory="A2",MaterialNo="B2",SerialNumber="C2" }
            };
        }
        [System.Web.Http.HttpPost()]
        public IHttpActionResult MaterialDataAll(Mat2Model m2)
        {
            //matlist.Add(m2);
            if (m2 == null || m2.Factory == null ||m2.MaterialNo == null || m2.SerialNumber == null)
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string factory = ""+m2.Factory.Trim();
            string material_no = ""+m2.MaterialNo.Trim();
            string serial_no = ""+m2.SerialNumber.Trim();
            if (factory == "" || material_no == "" || serial_no == "")
            {
                return Content(HttpStatusCode.NotFound, "Please check input data");
            }
            else
            {
                string schema = getSchema(factory.ToUpper());
                if (schema != "")
                {
                    #region getMatData
                    string sql = "select G1.*,G2.* from "
                + " ( select DISTINCT "
                + "b.barcode,"
                + "a.mo_number as MO_PARENT,"
                + "a.model_name as MODEL_PARENT,"
                + "b.mo_number,"
                + "t.model_name,"
                + "b.material_no,"
                + "b.qty,"
                + "b.unit,"
                + "b.vendor,"
                + "b.datecode,"
                + "b.serial_no,"
                //+ "b.status,"
                + "(case when b.status = 4 then 'SMT-Kitting' when b.status = 5 then 'Pre-forming Receive' when b.status = 8 then 'IPQC SCAN' "
                + " when b.status = 9 then 'PRE-FORMING RETURN' else to_char(b.status) end) as STATUS,"
                + "to_char(b.create_date, 'mm/dd/yyyy HH24:MI:SS') as CREATE_DATE,"
                + "b.create_emp,"
                + "b.reject_reason,"
                + "t.plantname "
                + "from " + schema + ".R_MATERIAL_OPERATE_DETAIL_T b "
                + " inner join " + schema + ".r_mo_smt_t t "
                + " on t.mo_number = b.mo_number "
                + " inner join " + schema + ".r_mo_base_t a "
                + "on t.m_wono = a.mo_number "
                + "where b.material_no = '" + material_no + "' AND b.serial_no = '" + serial_no + "') G1"
                + ","
                + "(SELECT s.MATNR ,OPCODE, NVL(MENGE, 0) WH_ISSUE_QTY, NVL(p.scanQty,0) SCANNED_QTY, NVL(W.qrty, 0) MRP_QTY "
                + "FROM(SELECT wono, matnr, SUM(rqty) qrty FROM jit.\"stockout_detail\" WHERE "
                + "wono = (select LPAD(mo_number,12,'0') from "+schema+".R_MATERIAL_OPERATE_DETAIL_T where material_no = '" + material_no + "' and serial_no = '" + serial_no + "') "
                + "and matnr = '" + material_no + "' GROUP BY matnr, wono "
                + "union SELECT wono, matnr, SUM(qtya) qrty FROM " + schema + ".fivetransferdetail WHERE "
                + "wono = (select mo_number from "+schema+".R_MATERIAL_OPERATE_DETAIL_T where material_no = '" + material_no + "' and serial_no = '" + serial_no + "') "
                + "and  matnr = '" + material_no + "' GROUP BY matnr, wono) w "
                + "inner join(SELECT RECEIVE.WONO, RECEIVE.MATNR, RECEIVE.OPCODE, (RECEIVE.GETS - NVL(BACK.BACKS, 0)) MENGE "
                + "FROM(SELECT WONO, MATNR, OPCODE, SUM(MENGE) GETS FROM sfcs.stockoutrf_detail WHERE "
                + "WONO = (select mo_number from "+schema+".R_MATERIAL_OPERATE_DETAIL_T where material_no = '" + material_no + "' and serial_no = '" + serial_no + "') "
                + "and  matnr = '" + material_no + "' AND OPFLAG <> 3 AND COMMITFLAG = 1 GROUP BY WONO, MATNR, OPCODE) RECEIVE "
                + "LEFT JOIN(SELECT WONO, MATNR, OPCODE, SUM(MENGE) BACKS FROM sfcs.stockoutrf_detail WHERE "
                + "WONO = (select mo_number from "+schema+".R_MATERIAL_OPERATE_DETAIL_T where material_no = '" + material_no + "' and serial_no = '" + serial_no + "') "
                + "and  matnr = '" + material_no + "' AND OPFLAG = 3 AND COMMITFLAG = 1 GROUP BY WONO, MATNR, OPCODE) BACK "
                + "ON RECEIVE.WONO = BACK.WONO AND RECEIVE.MATNR = BACK.MATNR) S "
                + "on W.matnr = S.MATNR "
                + "left join(SELECT material_no, SUM(qty) scanQty FROM " + schema + ".r_material_operate_detail_t WHERE "
                + "MO_NUMBER = (select mo_number from "+schema+".R_MATERIAL_OPERATE_DETAIL_T where material_no = '" + material_no + "' and serial_no = '" + serial_no + "') "
                + "and material_no = '" + material_no + "' AND status = '4' GROUP BY material_no) p "
                + "ON p.material_no = w.matnr ORDER BY P.SCANQTY) G2 "
                + " where G1.MATERIAL_NO = G2.MATNR ";
                    DataTable dt = QueryDataTable(sql, fGetConnectionBPWG(schema));
                    if (dt.Rows.Count > 0)
                    {
                        //string a = dt.Columns["QTY"].DataType.ToString();
                        //string b = dt.Columns["STATUS"].DataType.ToString();
                        //string c = dt.Columns["WH_ISSUE_QTY"].DataType.ToString();
                        //string d = dt.Columns["SCANNED_QTY"].DataType.ToString();
                        //string e = dt.Columns["MRP_QTY"].DataType.ToString();
                        //ret = DataTableToJSONWithJSONNet(dt);
                        List<MatModel> _MatList = new List<MatModel>();
                        _MatList = DataTableToList<MatModel>(dt);
                        return Ok(_MatList);
                    }
                    else
                        return Content(HttpStatusCode.NotFound, "Please check material_no / serial_no");
                    #endregion
                }
                else
                    return Content(HttpStatusCode.NotFound, "Please check factory");
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