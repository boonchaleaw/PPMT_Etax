using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class SendEmail
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public int member_user_id { get; set; }
        public int etax_file_id { get; set; }
        public string send_email_status { get; set; }
        public DateTime? send_email_finish { get; set; }
        public string email_status { get; set; }
        public int send_count { get; set; }
        public string error { get; set; }
        public string payment_status { get; set; }
    }
}
