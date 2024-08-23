using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class SendEbxml
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public int member_user_id { get; set; }
        public int etax_file_id { get; set; }
        public string conversation_id { get; set; }
        public string message_id { get; set; }
        public string send_ebxml_status { get; set; }
        public DateTime send_ebxml_finish { get; set; }
        public string etax_status { get; set; }
        public DateTime? etax_status_finish { get; set; }
        public string error { get; set; }
        public string payment_status { get; set; }
    }
}
