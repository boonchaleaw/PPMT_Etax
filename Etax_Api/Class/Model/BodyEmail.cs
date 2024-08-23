using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodyEmail
    {
        public int etax_file_id { get; set; }
        public string buyer_email { get; set; }
        public bool send_xml_status { get; set; }
        public bool send_other_status { get; set; }
        public List<BodySendOtherFile> send_other_file { get; set; }
    }
}
