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
    public class ScanDataDetailController : ApiController
    {
        private List<ScanDetail2Model> scanDetail2Model = null;
        string connectionBP = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THPUBMES-SCAN)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = THPUBMES)));Password=Delta12345;User ID=MESAP03";
        string connectionWG = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THWGRMESDB00)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = DETBCWG)));Password=Delta12345;User ID=MESAP03";

        public ScanDataDetailController()
        {
            scanDetail2Model = new List<ScanDetail2Model>()
            {
                new ScanDetail2Model(){Factory="A",MoNumber="B" },
                new ScanDetail2Model(){Factory="A2",MoNumber="B2" }
            };
        }
        [System.Web.Http.HttpPost()]
        public IHttpActionResult ScanDataDetail(ScanDetail2Model m2)
        {
            //matlist.Add(m2);
            if (m2 == null || m2.Factory == null ||m2.MoNumber == null)
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string factory = ""+m2.Factory.Trim();
            string MoNumber = "" + m2.MoNumber.Trim();
            if (factory == "" || MoNumber == "")
            {
                return Content(HttpStatusCode.NotFound, "Please check input data");
            }
            else
            {
                string schema = getSchema(factory.ToUpper());
                if (schema != "")
                {
                    #region getDataMain
                    string sqlm = "select a.mo_number as MO_PARENT,a.model_name as MODEL_PARENT,"
                        + "'" + MoNumber + "' as MO_NUMBER,t.model_name,a.target_qty "
                        + "from " + schema + ".r_mo_smt_t t inner join " + schema + ".r_mo_base_t a "
                        + "on t.m_wono = a.mo_number where t.mo_number = '" + MoNumber + "'";
                    DataTable dtm = QueryDataTable(sqlm, fGetConnectionBPWG(schema));
                    DataAllModel _DataAll = new DataAllModel();
                    if (dtm.Rows.Count > 0)
                    {
                        _DataAll.MO_PARENT = dtm.Rows[0]["MO_PARENT"].ToString();
                        _DataAll.MODEL_PARENT = dtm.Rows[0]["MODEL_PARENT"].ToString();
                        _DataAll.MO_NUMBER = dtm.Rows[0]["MO_NUMBER"].ToString();
                        _DataAll.MODEL_NAME = dtm.Rows[0]["MODEL_NAME"].ToString();
                        _DataAll.TARGET_QTY = int.Parse(dtm.Rows[0]["TARGET_QTY"].ToString());
                    }
                    else
                        return Content(HttpStatusCode.NotFound, "Please check mo_number");


                    #endregion
                    #region getResultData
                    string sqlr = "SELECT s.MATNR material_no,OPCODE, NVL(MENGE, 0) WH_Issue_Qty,   NVL(p.scanQty,0)   Scanned_Qty,   NVL(W.qrty, 0) MRP_Qty "
                        + "FROM(SELECT wono, matnr, SUM(rqty) qrty FROM jit.\"stockout_detail\" "
                        + "WHERE wono = LPAD('" + MoNumber + "', 12, '0') GROUP BY matnr, wono "
                        + "union SELECT wono, matnr, SUM(qtya) qrty FROM " + schema + ".fivetransferdetail WHERE wono = '" + MoNumber + "' GROUP BY matnr, wono) w "
                        + "inner join(SELECT RECEIVE.WONO, RECEIVE.MATNR, RECEIVE.OPCODE, (RECEIVE.GETS - NVL(BACK.BACKS, 0)) MENGE FROM(SELECT WONO, MATNR, OPCODE, SUM(MENGE) GETS "
                        + " FROM sfcs.stockoutrf_detail WHERE WONO = '" + MoNumber + "' AND OPFLAG <> 3 AND COMMITFLAG = 1 GROUP BY WONO, MATNR, OPCODE) RECEIVE "
                        + " LEFT JOIN(SELECT WONO, MATNR, OPCODE, SUM(MENGE) BACKS FROM sfcs.stockoutrf_detail WHERE WONO = '" + MoNumber + "' AND OPFLAG = 3 AND COMMITFLAG = 1 GROUP BY WONO, MATNR, OPCODE) BACK "
                        + " ON RECEIVE.WONO = BACK.WONO AND RECEIVE.MATNR = BACK.MATNR) S "
                        + " on W.matnr = S.MATNR left join(SELECT material_no, SUM(qty) scanQty FROM " + schema + ".r_material_operate_detail_t WHERE MO_NUMBER = '" + MoNumber + "' AND status = '4' GROUP BY material_no) p "
                        + " ON p.material_no = w.matnr ORDER BY P.SCANQTY";
                    DataTable dtr = QueryDataTable(sqlr, fGetConnectionBPWG(schema));
                    List<ScanResultModel> _ScanResultList = new List<ScanResultModel>();
                    if (dtr.Rows.Count > 0)
                    {
                        _ScanResultList = DataTableToList<ScanResultModel>(dtr);
                    }
                    else
                        return Content(HttpStatusCode.NotFound, "Please check mo_number");
                    #endregion

                    #region getScanDetailData
                    string sql = "select t.material_no,'SMT-Kitting' as status,t.qty,t.datecode,t.vendor,t.serial_no "
                        + " from " + schema + ".R_MATERIAL_OPERATE_DETAIL_T t where t.mo_number = '" + MoNumber + "' and t.status = '4'";
                    //status = 4 -> SMT-Kitting
                    DataTable dt = QueryDataTable(sql, fGetConnectionBPWG(schema));
                    List<ScanDetailModel> _ScanDetailList = new List<ScanDetailModel>();
                    if (dt.Rows.Count > 0)
                    {
                        //ret = DataTableToJSONWithJSONNet(dt);
                        _ScanDetailList = DataTableToList<ScanDetailModel>(dt);
                    }
                    else
                        return Content(HttpStatusCode.NotFound, "Please check mo_number");
                    #endregion
                    _DataAll.RESULT = _ScanResultList;
                    _DataAll.SCANDETAIL = _ScanDetailList;
                    return Ok(_DataAll);
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