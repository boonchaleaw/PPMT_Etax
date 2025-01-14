using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ViewSendEmailList
    {
        public int id { get; set; }
        public int etax_file_id { get; set; }
        public int member_id { get; set; }
        public int document_type_id { get; set; }
        public string document_type_name { get; set; }
        public string name { get; set; }
        public string etax_id { get; set; }
        public DateTime? issue_date { get; set; }
        public string ref_etax_id { get; set; }
        public DateTime? ref_issue_date { get; set; }
        public string buyer_tax_id { get; set; }
        public string buyer_name { get; set; }
        public string buyer_email { get; set; }
        public string email { get; set; }
        public string send_email_status { get; set; }
        public string email_status { get; set; }
        public string url_path { get; set; }
        public DateTime? send_email_finish { get; set; }
        public DateTime? create_date { get; set; }
        public string tax_type_filter { get; set; }
        public string payment_status { get;set; }
    }
}