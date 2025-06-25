using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class MemberUserPermission
    {
        public int id { get; set; }
        public int member_user_id { get; set; }
        public string per_branch_view { get; set; }
        public string per_branch_manage { get; set; }
        public string per_user_view { get; set; }
        public string per_user_manage { get; set; }
        public string per_raw_view { get; set; }
        public string per_raw_manage { get; set; }
        public string per_xml_view { get; set; }
        public string per_xml_manage { get; set; }
        public string per_pdf_view { get; set; }
        public string per_email_view { get; set; }
        public string per_email_manage { get; set; }
        public string per_sms_view { get; set; }
        public string per_sms_manage { get; set; }
        public string per_ebxml_view { get; set; }
        public string per_setting_manage { get; set; }
        public string per_report_view { get; set; }
        public string per_etax_delete { get; set; }
        public string per_email_import { get; set; }
        public string view_self_only { get; set; }
        public string view_branch_only { get; set; }
        public DateTime? update_date { get; set; }
        public DateTime? create_date { get; set; }

        public string job_order { get; set; }

        public string job_order_name { get; set; }
    }
}
