using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class SendSms
    {
        public int id { get; set; }
        public int etax_file_id { get; set; }
        public string send_sms_status { get; set; }
        public DateTime? send_sms_finish { get; set; }
        public string open_sms_status { get; set; }
        public DateTime? open_sms_finish { get; set; }
        public string error { get; set; }
        public string payment_status { get; set; }
        public int send_count { get; set;}
        public int message_count { get; set; }
    }
}
