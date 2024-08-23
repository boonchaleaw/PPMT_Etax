using System;
using System.Collections.Generic;

namespace Etax_Api
{
    public class Item
    {
        public string code { get; set; }
        public string name { get; set; }
        public int qty { get; set; }
        public string unit { get; set; }
        public double? price { get; set; }
        public double? discount { get; set; }
        public double? tax { get; set; }
        public double? total { get; set; }

    }
    public class BodyCreateXml
    {
        public ViewMemberDocumentType document_type { get; set; }
        public Branch branch { get; set; }
        public bool gen_pdf_status { get; set; }
        public bool send_email_status { get; set; }
        public bool send_ebxml_status { get; set; }
        public string etax_id { get; set; }
        public DateTime issue_date { get; set; }
        public string buyer_branch_code { get; set; }
        public string buyer_id { get; set; }
        public string buyer_name { get; set; }
        public string buyer_tax_id { get; set; }
        public string buyer_district_name { get; set; }
        public string buyer_building_number { get; set; }
        public string buyer_building_name { get; set; }
        public string buyer_street_name { get; set; }
        public string buyer_amphoe_name { get; set; }
        public string buyer_province_name { get; set; }
        public string buyer_zipcode { get; set; }
        public string buyer_tel { get; set; }
        public string buyer_fax { get; set; }
        public string buyer_email { get; set; }
        public string buyer_password { get; set; }
        public List<Item> items { get; set; }
        public double net_price { get; set; }
        public double net_discount { get; set; }
        public double net_tax { get; set; }
        public double net_total { get; set; }

    }
}
