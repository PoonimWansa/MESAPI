using System;
using System.Data.Entity;
using System.Linq;

namespace MESAPI.Models
{
    public class MatModel
    {
        public string BARCODE { get; set; }
        public string MO_PARENT { get; set; }
        public string MODEL_PARENT { get; set; }
        public string MO_NUMBER { get; set; }
        public string MODEL_NAME { get; set; }
        public string MATERIAL_NO { get; set; }
        public decimal QTY { get; set; }
        public string UNIT { get; set; }
        public string VENDOR { get; set; }
        public string DATECODE { get; set; }
        public string SERIAL_NO { get; set; }
        public string STATUS { get; set; }
        public string CREATE_DATE { get; set; }
        public string CREATE_EMP { get; set; }
        public string REJECT_REASON { get; set; }
        public string PLANTNAME { get; set; }
        public string MATNR { get; set; }
        public string OPCODE { get; set; }
        public decimal WH_ISSUE_QTY { get; set; }
        public decimal SCANNED_QTY { get; set; }
        public decimal MRP_QTY { get; set; }
    }
    public class MoModel
    {
        public string MO_NUMBER { get; set; }
        public string MODEL_NAME { get; set; }
        public string LINE_NAME { get; set; }
        public decimal TARGET { get; set; }
        public decimal STATUS { get; set; }
    }
    public class CoverModel
    {
        public string Factory { get; set; }
        public string Cover_SN { get; set; }
    }
}