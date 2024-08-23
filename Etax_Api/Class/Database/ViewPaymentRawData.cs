using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ViewPaymentRawData
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public string create_type { get; set; }
        public string row_name { get; set; }
        public DateTime create_date { get; set; }
        public int xml_count { get; set; }
        public int pdf_count { get; set; }
        public int email_count { get; set; }
        public int ebxml_count { get; set; }
        public int sms_count { get; set; }
        public int sms_message_count { get; set; }
    }
}