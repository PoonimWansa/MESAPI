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
using System.Reflection;

namespace MESAPI.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class GetPlantInfoController : ApiController
    {
        string connectionReport = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
                + "(HOST = THBPODSMRACDB)(PORT = 1521))"
                + "(CONNECT_DATA = (SERVER = DEDICATED)"
                + "(SERVICE_NAME = THSFDB)));Password=tmudk$o0;User ID=PQM_QUERY";

        [System.Web.Http.HttpGet()]
        [Route("api/plant/information/{PROD_AREA}")]
        ///api/plant/information/{Prod_Area_ID}
        public IHttpActionResult GetPlantInfo(string PROD_AREA)
        {
            //matlist.Add(m2);
            if (PROD_AREA == null)
                return Content(HttpStatusCode.NotFound, "Please check input data");
            string prod_area = PROD_AREA.Trim();// "" + m2.PROD_AREA.Trim();
            //DateTime timenow = DateTime.Now;
            //DateTime sectionFrom = new DateTime(timenow.Year, timenow.Month, timenow.Day, 7, 30, 0);
            //DateTime sectionFrom2 = new DateTime(timenow.Year, timenow.Month, timenow.Day, 20, 0, 0);
            //string sSectionFrom = (timenow >= sectionFrom && timenow < sectionFrom2 ? sectionFrom.ToString("yyyyMMddHHmm") : sectionFrom2.ToString("yyyyMMddHHmm"));
            //List<MachinePlantInfoModel> _lDataAll = new List<MachinePlantInfoModel>();
            MachinePlantInfoModel _lDataAll = new MachinePlantInfoModel();
            #region List Line
            string sqlListLine = "select L.LINE_NAME from DET_CNDC.c_line_desc_t L inner join sfcs.c_prod_area_t PA on L.PROD_AREA_ID = PA.PROD_AREA_ID " +
                " where PA.PROD_AREA_CODE = '" + prod_area + "' and L.VALID_FLAG = 'Y' order by L.LINE_NAME";
            DataTable dtListLine = QueryDataTable(sqlListLine, connectionReport);

            #endregion

            #region UTI DATA
            string sqlUti = " select B.PROD_AREA_CODE,t.line_name,bas.equipment_code,CAST(T.EQUIPMENT_ID AS VARCHAR2(2000)) as EQUIPMENT_ID , " +
                " round(nvl(t.uti,0)*100,2) as UTI_TODAY, round((nvl(t2.GREEN_LIGHT_TIME,0) / 1440)*100, 2)as UTI_YESTERDAY " +
                "  from det_cndc.r_equip_active_daily_shift_t t " +
                "  inner join det_cndc.r_equip_active_daily_t t2 " +
                "  on t.equipment_id = t2.equipment_id " +
                "  inner join det_cndc.C_EQUIPMENT_BASIC_EDMS_T bas " +
                "  on bas.equipment_id = t.equipment_id " +
                "  and t.line_name = t2.line_name " +
                "  inner join DET_CNDC.c_line_desc_t A " +
                "  on t.line_name = A.line_name " +
                " inner join sfcs.c_prod_area_t B " +
                " on A.prod_area_id = B.prod_area_id " +
                " where B.PROD_AREA_CODE = '"+prod_area+"' " +
                " and A.VALID_FLAG = 'Y' " +
                " and t.work_date = to_char(sysdate, 'yyyymmdd') " +
                //" and t.shift = (case when substr(" + sSectionFrom + ", 9, 4) < '1200' then 'DAY' else 'NIGHT' end) " +
                " and t2.work_date = to_char(sysdate-1, 'yyyymmdd') order by t.line_name ";
            DataTable dtUti = QueryDataTable(sqlUti, connectionReport);
            //List<MachineSubModel> _lDataAllSub = new List<MachineSubModel>();
            //MachineSubModel _DataAllSub = new MachineSubModel();
            List<MachinePlantInfoUtiDetailModel> _lMachinePlantInfoUtiDetailModel = new List<MachinePlantInfoUtiDetailModel>();
            if (dtUti.Rows.Count > 0)
            {
                try
                {
                    var avgUtiProdArea = (from row in dtUti.AsEnumerable()
                                      group row by new { PROD_AREA_CODE = row["PROD_AREA_CODE"].ToString() } into g
                                      select new
                                      {
                                          PROD_AREA_CODE = g.Key.PROD_AREA_CODE,
                                          UTI_TODAY = Math.Round(g.Average(x => x.Field<decimal>("UTI_TODAY")),2),
                                          UTI_YESTERDAY = Math.Round(g.Average(x => x.Field<decimal>("UTI_YESTERDAY")),2)
                                      }
                                      ).ToList();
                    var avgUtiLine = dtUti.AsEnumerable()
                       .GroupBy(x => new { PROD_AREA_CODE = x.Field<string>("PROD_AREA_CODE"), LINE_NAME = x.Field<string>("LINE_NAME") })
                       .Select(x => new { PROD_AREA_CODE = x.Key.PROD_AREA_CODE, LINE_NAME = x.Key.LINE_NAME, UTI_TODAY = Math.Round(x.Average(y => y.Field<decimal>("UTI_TODAY")),2) })
                       .ToList();
                    DataTable dtUti_Line = LINQToDataTable(avgUtiLine);
                    //dtUti_Line.Columns["UTI_TODAY"].ColumnName = "UTILIZATION";
                    //dtUti_Line.Columns["UTILIZATION"].AllowDBNull = true;
                    dtUti_Line.Columns.Add("UTILIZATION").DataType = typeof(string);
                    dtUti_Line.Columns["UTILIZATION"].AllowDBNull = true;
                    foreach(DataRow r in dtUti_Line.Rows)
                    {
                        r["UTILIZATION"] = r["UTI_TODAY"].ToString();
                    }
                    dtUti_Line.Columns.Remove("UTI_TODAY");

                    var uniqueObjects = dtUti_Line.AsEnumerable().Select(x => x.Field<string>("LINE_NAME")).Distinct().ToList();

                    foreach (DataRow rowListLine in dtListLine.Rows)
                    {
                        string line = rowListLine["LINE_NAME"].ToString();
                        if (!uniqueObjects.Contains(line))
                        {
                            DataRow dr = dtUti_Line.NewRow();
                            dr["PROD_AREA_CODE"] = prod_area;
                            dr["LINE_NAME"] = line;
                            dr["UTILIZATION"] = "N/A";
                            dtUti_Line.Rows.Add(dr);
                        }
                    }
                    dtUti_Line.DefaultView.Sort = "LINE_NAME ASC";
                    dtUti_Line = dtUti_Line.DefaultView.ToTable();
                    _lMachinePlantInfoUtiDetailModel = DataTableToList<MachinePlantInfoUtiDetailModel>(dtUti_Line);
                    _lDataAll.UTILIZATION_RATE_TODAY = avgUtiProdArea[0].UTI_TODAY;
                    _lDataAll.UTILIZATION_RATE_YESTERDAY = avgUtiProdArea[0].UTI_YESTERDAY;
                    _lDataAll.UTILIZATION_LINE = _lMachinePlantInfoUtiDetailModel;
                }
                catch(Exception ex)
                { }
            }
            #endregion
            #region TBS Data
            string sqlTbs = " select P.PROD_AREA_CODE ,L1.LINE_NAME,bas.equipment_code " +
                " ,round(ceil((tbs.begin_point-tbs.last_end_point)*24*60*60)/60,2) TBS_TODAY " +
                " ,round(ceil((tbs2.begin_point-tbs2.last_end_point)*24*60*60)/60,2) TBS_YESTERDAY " +
                "     from det_cndc.r_equip_tbs_t tbs " +
                "     left join det_cndc.r_equip_tbs_t tbs2 " +
                "     on tbs.equipment_id = tbs2.equipment_id " +
                "    inner join det_cndc.C_EQUIPMENT_BASIC_EDMS_T bas " +
                "    on tbs.EQUIPMENT_ID = bas.EQUIPMENT_ID " +
                "    inner join DET_CNDC.C_LINE_STATION_T  L1 " +
                "    on bas.EQUIPMENT_ID = L1.EQUIPMENT_ID " +
                "    inner join DET_CNDC.c_line_desc_t L " +
                "    on L.LINE_NAME = L1.LINE_NAME " +
                "    and L.VALID_FLAG= 'Y' " +
                "    inner join sfcs.c_prod_area_t P " +
                "    on L.PROD_AREA_ID = P.PROD_AREA_ID " +
                " where P.PROD_AREA_CODE = '"+prod_area+"' " +
                " and tbs.END_POINT is not null " +
                " and trunc(tbs.END_POINT) = trunc(sysdate) " +
                " and tbs2.end_point  is not null " +
                " and trunc(tbs2.END_POINT) = trunc(sysdate-1) order by L1.LINE_NAME";
            DataTable dtTbs = QueryDataTable(sqlTbs, connectionReport);
            List<MachinePlantInfoTbsDetailModel> _lMachinePlantInfoTbsDetailModel = new List<MachinePlantInfoTbsDetailModel>();
            if (dtTbs.Rows.Count > 0)
            {
                try
                {
                    var avgTbsProdArea = dtTbs.AsEnumerable()
                       .GroupBy(x => new { PROD_AREA_CODE = x.Field<string>("PROD_AREA_CODE") })
                       .Select(x => new { PROD_AREA_CODE = x.Key.PROD_AREA_CODE, TBS_TODAY = Math.Round(x.Average(y => y.Field<decimal>("TBS_TODAY")), 2), TBS_YESTERDAY = Math.Round(x.Average(y => y.Field<decimal>("TBS_YESTERDAY")), 2) })
                       .ToList();
                    var avgTbsLine = dtTbs.AsEnumerable()
                       .GroupBy(x => new { PROD_AREA_CODE = x.Field<string>("PROD_AREA_CODE"), LINE_NAME = x.Field<string>("LINE_NAME") })
                       .Select(x => new { PROD_AREA_CODE = x.Key.PROD_AREA_CODE, LINE_NAME = x.Key.LINE_NAME, TBS_TODAY = Math.Round(x.Average(y => y.Field<decimal>("TBS_TODAY")), 2) })
                       .ToList();
                    DataTable dtTbs_Line = LINQToDataTable(avgTbsLine);

                    dtTbs_Line.Columns.Add("TBS").DataType = typeof(string);
                    dtTbs_Line.Columns["TBS"].AllowDBNull = true;
                    foreach (DataRow r in dtTbs_Line.Rows)
                    {
                        r["TBS"] = r["TBS_TODAY"].ToString();
                    }
                    dtTbs_Line.Columns.Remove("TBS_TODAY");

                    var uniqueObjects = dtTbs_Line.AsEnumerable().Select(x => x.Field<string>("LINE_NAME")).Distinct().ToList();
                    foreach (DataRow rowListLine in dtListLine.Rows)
                    {
                        string line = rowListLine["LINE_NAME"].ToString();
                        if (!uniqueObjects.Contains(line))
                        {
                            DataRow dr = dtTbs_Line.NewRow();
                            dr["PROD_AREA_CODE"] = prod_area;
                            dr["LINE_NAME"] = line;
                            dr["TBS"] = "N/A";
                            dtTbs_Line.Rows.Add(dr);
                        }
                    }
                    dtTbs_Line.DefaultView.Sort = "LINE_NAME ASC";
                    dtTbs_Line = dtTbs_Line.DefaultView.ToTable();
                    _lMachinePlantInfoTbsDetailModel = DataTableToList<MachinePlantInfoTbsDetailModel>(dtTbs_Line);
                    _lDataAll.AVERAGE_TBS_TODAY = avgTbsProdArea[0].TBS_TODAY;
                    _lDataAll.AVERAGE_TBS_YESTERDAY = avgTbsProdArea[0].TBS_YESTERDAY;
                    _lDataAll.AVERAGE_TBS_LINE = _lMachinePlantInfoTbsDetailModel;
                }
                catch (Exception ex)
                { }
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
        internal static DataTable LINQToDataTable<T>(IEnumerable<T> varlist)
        {
            DataTable dtReturn = new DataTable();

            // column names 
            PropertyInfo[] oProps = null;

            if (varlist == null) return dtReturn;

            foreach (T rec in varlist)
            {
                 if (oProps == null)
                {
                    oProps = ((Type)rec.GetType()).GetProperties();
                    foreach (PropertyInfo pi in oProps)
                    {
                        Type colType = pi.PropertyType;

                        if ((colType.IsGenericType) && (colType.GetGenericTypeDefinition()
                        == typeof(Nullable<>)))
                        {
                            colType = colType.GetGenericArguments()[0];
                        }

                        dtReturn.Columns.Add(new DataColumn(pi.Name, colType));
                    }
                }

                DataRow dr = dtReturn.NewRow();

                foreach (PropertyInfo pi in oProps)
                {
                    dr[pi.Name] = pi.GetValue(rec, null) == null ? DBNull.Value : pi.GetValue
                    (rec, null);
                }

                dtReturn.Rows.Add(dr);
            }
            return dtReturn;
        }
    }
}