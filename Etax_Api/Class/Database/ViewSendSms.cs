using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ViewSendSms
    {
        public int id { get; set; }
        public int etax_file_id { get; set; }
        public string etax_id { get; set; }
        public int member_id { get; set; }
        public string member_name { get; set; }
        public int member_user_id { get; set; }
        public int branch_id { get; set; }
        public int rawdata_file_id { get; set; }
        public int document_type_id { get; set; }
        public string document_type_name { get; set; }
        public string create_type { get; set; }
        public string name { get; set; }
        public string buyer_tel{ get; set; }
        public string raw_name { get; set; }
        public string send_sms_status { get; set; }
        public DateTime? send_sms_finish { get; set; }
        public string open_sms_status { get; set; }
        public DateTime? open_sms_finish { get; set; }
        public string error { get; set; }
        public string url_path { get; set; }
        public string payment_status { get; set; }
        public DateTime? issue_date { get; set; }
        public DateTime? create_date { get; set; }
        public string ref_etax_id { get; set; }
        public int send_count { get; set; }
        public int message_count { get; set; }
        public string tax_type_filter { get; set; }
        public string group_name { get; set; }
    }
}