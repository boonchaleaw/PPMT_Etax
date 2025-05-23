using System;

namespace Etax_Api.Class.Model
{
    public class BodyError_log
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public string error { get; set; }

        public DateTime error_time { get; set; }
        public string method_name { get; set; }

        public string input_data { get; set; }
        public string response_data { get; set; }
        public int etax_file_id { get; set; }
        public string class_name { get; set; }
        public string service { get; set; }

        public string admin_email_status { get; set; }

        public string error_id { get; set; }
        public string etax_id { get; set; }
    }
}
