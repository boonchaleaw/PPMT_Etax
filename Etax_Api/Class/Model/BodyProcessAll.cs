using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodyProcessAll
    {
        public int id { get; set; }
        public bool send_xml_status { get; set; }
        public bool gen_pdf_status { get; set; }
        public bool send_email_status { get; set; }
        public bool send_sms_status { get; set; }
        public bool send_ebxml_status { get; set; }
        public List<BodyItemSelect> listSelect { get; set; }
    }
}
