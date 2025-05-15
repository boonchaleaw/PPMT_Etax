using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ReturnCostReport
    {
        public double total_xml_price { get; set; }
        public double total_pdf_price { get; set; }
        public double total_email_price { get; set; }
        public double total_ebxml_price { get; set; }
        public double total_sms_price { get; set; }
        public int total_xml_count { get; set; }
        public int total_pdf_count { get; set; }
        public int total_email_count { get; set; }
        public int total_ebxml_count { get; set; }
        public int total_sms_count { get; set; }

        public List<ReturnCostReportData> listReturnCostReportData { get; set; }
    }
    public class ReturnCostReportData
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public string create_type { get; set; }
        public string row_name { get; set; }
        public int xml_count { get; set; }
        public int pdf_count { get; set; }
        public int email_count { get; set; }
        public int ebxml_count { get; set; }
        public int sms_count { get; set; }
    }
}
