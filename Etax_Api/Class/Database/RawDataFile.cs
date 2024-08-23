using System;


namespace Etax_Api
{
    public class RawDataFile
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public int branch_id { get; set; }
        public int member_user_id { get; set; }
        public int document_type_id { get; set; }
        public string file_name { get; set; }
        public string cycle { get; set; }
        public string load_file_status { get; set; }
        public string input_path { get; set; }
        public string output_path { get; set; }
        public string url_path { get; set; }
        public string share_path { get; set; }
        public string gen_pdf_status { get; set; }
        public int gen_xml_success { get; set; }
        public string send_email_status { get; set; }
        public string? send_sms_status { get; set; }
        public string send_ebxml_status { get; set; }
        public string comment { get; set; }
        public string template_pdf { get; set; }
        public string template_email { get; set; }
        public string mode { get; set; }
        public DateTime create_date { get; set; }
    }
}