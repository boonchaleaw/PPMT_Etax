using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class EtaxOutsourceFile
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public int branch_id { get; set; }
        public int member_user_id { get; set; }
        public int document_type_id { get; set; }
        public string file_name { get; set; }
        public string cycle { get; set; }
        public string etax_id { get; set; }
        public DateTime issue_date { get; set; }
        public string? ref_etax_id { get; set; }
        public DateTime? ref_issue_date { get; set; }
        public string buyer_branch_code { get; set; }
        public string buyer_id { get; set; }
        public string buyer_name { get; set; }
        public string buyer_tax_id { get; set; }
        public string buyer_address { get; set; }
        public string buyer_tel { get; set; }
        public string buyer_fax { get; set; }
        public string buyer_country_code { get; set; }
        public string buyer_email { get; set; }
        public double original_price { get; set; }
        public double price { get; set; }
        public double diff_price { get; set; }
        public double discount { get; set; }
        public string tax_type { get; set; }
        public int tax_rate { get; set; }
        public double tax { get; set; }
        public double total { get; set; }
        public string remark { get; set; }
        public string other { get; set; }
        public string template_pdf { get; set; }
        public double template_email { get; set; }
        public double xml_status { get; set; }
        public double pdf_status { get; set; }
        public double error { get; set; }
        public DateTime? create_date { get; set; }
    }
}
