using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodySms
    {
        public int etax_file_id { get; set; }
        public string buyer_tel{ get; set; }
        public bool send_sms_status { get; set; }
    }
}
