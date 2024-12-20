using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ViewTaxCsvReport
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public string name { get; set; }
        public int document_type_id { get; set; }
        public string document_type_name { get; set; }
        public string gen_xml_status { get; set; }
        public DateTime? gen_xml_finish { get; set; }
        public string url_path { get; set; }
        public string etax_id { get; set; }
        public DateTime? issue_date { get; set; }
        public string ref_etax_id { get; set; }
        public DateTime? ref_issue_date { get; set; }
        public string buyer_branch_code { get; set; }
        public string buyer_name { get; set; }
        public string buyer_tax_id { get; set; }
        public string buyer_address { get; set; }
        public double original_price { get; set; }
        public double price { get; set; }
        public double discount { get; set; }
        public string tax_type { get; set; }
        public int tax_rate { get; set; }
        public double tax { get; set; }
        public double total { get; set; }
        public string other { get; set; }
        public string other2 { get; set; }
        public DateTime create_date { get; set; }
        public int delete_status { get; set; }
        public string send_email_status { get; set; }
        public string send_ebxml_status { get; set; }
        public string tax_type_filter { get; set; }
    }
}