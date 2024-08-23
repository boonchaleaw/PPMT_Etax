using System;
using System.ComponentModel.DataAnnotations;

namespace Etax_Api
{
    public class ResponsEmail
    {
        public int id { get; set; }
        public int send_email_id { get; set; }
        public int message_id { get; set; }
        public string email { get; set; }
        public string email_status { get; set; }
        public DateTime update_date { get; set; }
        public DateTime create_date { get; set; }
    }
}