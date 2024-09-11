using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class EtaxFile
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public int branch_id { get; set; }
        public int member_user_id { get; set; }
        public int document_type_id { get; set; }
        public int rawdata_file_id { get; set; }
        public string create_type { get; set; }
        public string cycle { get; set; }
        public string name { get; set; }
        public string gen_xml_status { get; set; }
        public DateTime? gen_xml_finish { get; set; }
        public string gen_pdf_status { get; set; }
        public string add_email_status { get; set; }
        public string add_sms_status { get; set; }
        public string add_ebxml_status { get; set; }
        public string send_xml_status { get; set; }
        public string send_other_status { get; set; }
        public string error { get; set; }
        public string output_path { get; set; }
        public string url_path { get; set; }
        public string share_path { get; set; }
        public string etax_id { get; set; }
        public string buyer_branch_code { get; set; }
        public DateTime issue_date { get; set; }
        public string? ref_etax_id { get; set; }
        public DateTime? ref_issue_date { get; set; }
        public string ref_document_type { get; set; }
        public string buyer_id { get; set; }
        public string buyer_name { get; set; }
        public string buyer_tax_id { get; set; }
        public string buyer_tax_type { get; set; }
        public string buyer_address { get; set; }
        public string buyer_zipcode { get; set; }
        public string buyer_tel { get; set; }
        public string buyer_fax { get; set; }
        public string buyer_country_code { get; set; }
        public string buyer_email { get; set; }
        public double original_price { get; set; }
        public double new_price { get; set; }
        public double price { get; set; }
        public double discount { get; set; }
        public int tax_rate { get; set; }
        public double tax { get; set; }
        public double total{ get; set; }
        public string remark { get; set; }
        public string other { get; set; }
        public string group_name { get; set; }
        public string template_pdf { get; set; }
        public string template_email { get; set; }
        public string mode { get; set; }
        public DateTime? update_date { get; set; }
        public DateTime? create_date { get; set; }
        public string xml_payment_status { get; set; }
        public string pdf_payment_status { get; set; }
        public int delete_status { get; set; }
        public string password { get; set; }
        public string form_code { get; set; }
        public int paper { get; set; }
        public int webhook { get; set; }
    }
}
