using System;
using System.Collections.Generic;

namespace Etax_Api
{
    public class Seller
    {
        public string tax_id { get; set; }
        public string branch_code { get; set; }
        public string branch_name_th { get; set; }
        public string branch_name_en { get; set; }
        public string building_number { get; set; }
        public string building_name_th { get; set; }
        public string building_name_en { get; set; }
        public string street_name_th { get; set; }
        public string street_name_en { get; set; }
        public string district_code { get; set; }
        public string district_name_th { get; set; }
        public string district_name_en { get; set; }
        public string amphoe_code { get; set; }
        public string amphoe_name_th { get; set; }
        public string amphoe_name_en { get; set; }
        public string province_code { get; set; }
        public string province_name_th { get; set; }
        public string province_name_en { get; set; }
        public string zipcode { get; set; }
    }
    public class Buyer
    {
        public string branch_code { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string tax_id { get; set; }
        public string address { get; set; }
        public string zipcode { get; set; }
        public string tel { get; set; }
        public string fax { get; set; }
        public string country_code { get; set; }
        public string email { get; set; }
    }
    public class ItemEtax
    {
        public string code { get; set; }
        public string name { get; set; }
        public double qty { get; set; }
        public string unit { get; set; }
        public double price { get; set; }
        public double discount { get; set; }
        public double tax { get; set; }
        public double total { get; set; }
        public string tax_type { get; set; }
        public double tax_rate { get; set; }
        public string other { get; set; }
    }

    public class BodyApiCreateEtax
    {
        public string pdf_service { get; set; }
        public string email_service { get; set; }
        public string sms_service { get; set; }
        public string rd_service { get; set; }
        public string document_type_code { get; set; }
        public Seller seller { get; set; }
        public Buyer buyer { get; set; }
        public bool gen_form_status { get; set; }
        public bool gen_pdf_status { get; set; }
        public bool send_email_status { get; set; }
        public bool send_sms_status { get; set; }
        public bool send_ebxml_status { get; set; }
        public string template_pdf { get; set; }
        public string template_email { get; set; }
        public string etax_id { get; set; }
        public string issue_date { get; set; }
        public string ref_etax_id { get; set; }
        public string ref_issue_date { get; set; }
        public double original_price { get; set; }
        public double price { get; set; }
        public double discount { get; set; }
        public double tax { get; set; }
        public double total { get; set; }
        public string remark { get; set; }
        public string other { get; set; }
        public List<ItemEtax> items { get; set; }
        public string pdf_base64 { get; set; }
    }
}
