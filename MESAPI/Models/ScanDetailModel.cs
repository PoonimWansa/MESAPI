using System;
using System.Data.Entity;
using System.Linq;
using System.Collections.Generic;

namespace MESAPI.Models
{
    public class ScanResultModel
    {
        public string MATERIAL_NO { get; set; }
        public string OPCODE { get; set; }
        public decimal WH_Issue_Qty { get; set; }
        public decimal Scanned_Qty { get; set; }
        public decimal MRP_Qty { get; set; }
    }
    public class ScanDetailModel
    {
        public string MATERIAL_NO { get; set; }
        public string STATUS { get; set; }
        public decimal QTY { get; set; }
        public string DATECODE { get; set; }
        public string VENDOR { get; set; }
        public string SERIAL_NO { get; set; }
    }
    public class DataAllModel
    {
        public string MO_PARENT { get; set; }
        public string MODEL_PARENT { get; set; }
        public string MO_NUMBER { get; set; }
        public string MODEL_NAME { get; set; }
        public int TARGET_QTY { get; set; }
        public List<ScanResultModel> RESULT { get; set; }
        public List<ScanDetailModel> SCANDETAIL { get; set; }
    }
}