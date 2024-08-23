using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodyApiProcess
    {
        public string etax_id { get; set; }
        public bool gen_pdf_status { get; set; }
        public bool send_email_status { get; set; }
        public bool send_xml_status { get; set; }
        public string buyer_email { get; set; }
        public bool send_ebxml_status { get; set; }
    }
}
