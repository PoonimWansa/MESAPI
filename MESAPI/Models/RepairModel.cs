using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MESAPI.Models
{
    public class RepairModel
    {
        public string SERIAL_NUMBER { get; set; }
        public string MO_NUMBER { get; set; }
        public string MODEL_NAME { get; set; }
        public string LINE_NAME { get; set; }
        public string SECTION_NAME { get; set; }
        public string GROUP_NAME { get; set; }
        public string RESULT { get; set; }
        public string TEST_DATE { get; set; }
    }
}