using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class UserPermission
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public string per_user_menu { get; set; }
        public string per_user_detail { get; set; }
        public string per_member_menu { get; set; }
        public string per_member_detail { get; set; }
        public string per_raw_menu { get; set; }
        public string per_raw_detail { get; set; }
        public string per_xml_menu { get; set; }
        public string per_xml_detail { get; set; }
        public string per_pdf_menu { get; set; }
        public string per_pdf_detail { get; set; }
        public string per_email_menu { get; set; }
        public string per_email_detail { get; set; }
        public string per_sms_menu { get; set; }
        public string per_sms_detail { get; set; }
        public string per_ebxml_menu { get; set; }
        public string per_ebxml_detail { get; set; }
        public string per_report_menu { get; set; }
        public string per_report_detail { get; set; }
        public string per_xml_file { get; set; }
        public string per_pdf_file { get; set; }
    }
}
