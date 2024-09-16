using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ReturnReportByEmail
    {
        public string group_name { get; set; }
        public int total { get; set; }
        public int xml_count { get; set; }
        public int pdf_count { get; set; }
        public int send_to_cus_count { get; set; }
        public int not_send_to_cus_count { get; set; }
        public int send_ebxml_status { get; set; }
        public int etax_status { get; set; }

        public List<ReturnReportByEmailError> listError { get; set; }

    }

    public class ReturnReportByEmailError
    {
        public string type { get; set; }
        public string group_name { get; set; }
        public string etax_id { get; set; }
        public string error { get; set; }
    }
}
