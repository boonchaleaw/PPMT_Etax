using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ViewTotalReport
    {
        public int id { get; set; }
        public string member_name { get; set; }
        public int xml_count { get; set; }
        public int pdf_count { get; set; }
        public int email_count { get; set; }
        public int sms_count { get; set; }
        public int sms_message_count { get; set; }
        public int ebxml_count { get; set; }
    }
}