using System;
using System.Collections.Generic;

namespace Etax_Api
{
    public class BodyApiTripetchCreateEtax
    {
        public string pdf_service { get; set; }
        public string email_service { get; set; }
        public string sms_service { get; set; }
        public string rd_service { get; set; }
        public string myisuzu_service { get; set; }
        public string etax_id { get; set; }
        public string issue_date { get; set; }
        public string ref_etax_id { get; set; }
        public string ref_issue_date { get; set; }
        public int ref_document_type_code { get; set; }
        public string document_type_code { get; set; }
        public Seller seller { get; set; }
        public Buyer buyer { get; set; }
        public double original_price { get; set; }
        public double new_price { get; set; }
        public double price { get; set; }
        public double correct_tax_invoice_amount { get; set; }
        public double discount { get; set; }
        public double tax_rate { get; set; }
        public double tax { get; set; }
        public double total { get; set; }
        public string remark { get; set; }
        public string other { get; set; }
        public List<ItemEtax> items { get; set; }
        public string source_type { get; set; }
        public string template_pdf { get; set; }
        public string template_email { get; set; }
        public string pdf_base64 { get; set; }
    }
}
