using System;


namespace Etax_Api
{
    public class BodyRawData
    {
        public BodyMember member { get; set; }
        public Branch branch { get; set; }
        public ViewMemberDocumentType document_type { get; set; }
        public bool gen_pdf_status { get; set; }
        public bool send_email_status { get; set; }
        public bool send_ebxml_status { get; set; }
        public string raw_file_name { get; set; }
        public string raw_file_data { get; set; }
        public string comment { get; set; }

    }
}
