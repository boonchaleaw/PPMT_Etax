using System;


namespace Etax_Api
{
    public class ViewRawFile
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public int branch_id { get; set; }
        public int member_user_id { get; set; }
        public string member_name { get; set; }
        public string tax_id { get; set; }
        public string building_number { get; set; }
        public string building_name { get; set; }
        public string street_name { get; set; }
        public int district_code { get; set; }
        public string district_name { get; set; }
        public int amphoe_code { get; set; }
        public string amphoe_name { get; set; }
        public int province_code { get; set; }
        public string province_name { get; set; }
        public string zipcode { get; set; }
        public int document_type_id { get; set; }
        public string document_type_name { get; set; }
        public string cycle { get; set; }
        public string file_name { get; set; }
        public string url_path { get; set; }
        public string load_file_status { get; set; }
        public DateTime? load_file_processing { get; set; }
        public DateTime? load_file_finish { get; set; }
        public string load_file_error{ get; set; }
        public string gen_pdf_status { get; set; }
        public string send_email_status { get; set; }
        public string send_ebxml_status { get; set; }
        public int? file_total { get; set; }
        public int? gen_xml_success { get; set; }
        public int? gen_xml_fail { get; set; }
        public string comment { get; set; }
        public DateTime create_date { get; set; }

    }
}