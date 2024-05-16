using System;
using System.Data.Entity;
using System.Linq;

namespace MESAPI.Models
{
    public class Mat2Model
    {
        public string Factory { get; set; }
        public string MaterialNo { get; set; }
        public string SerialNumber { get; set; }
    }
    public class POModel
    {
        public string Factory { get; set; }
        public string Prod_Area { get; set; }


    }
    public class MO2Model
    {
        public string Plant { get; set; }
        public bool SMTFlag { get; set; }
    }
    public class ReWModel
    {
        public string Factory { get; set; }
        public string Mo_number { get; set; }


    }
    public class RePModel
    {
        public string Factory { get; set; }
        public string Mo_number { get; set; }


    }
}