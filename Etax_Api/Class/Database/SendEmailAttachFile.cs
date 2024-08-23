using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class SendEmailAttachFile
    {
        public int id { get; set; }
        public int etax_file_id { get; set; }
        public string file_name { get; set; }
        public string file_path { get; set; }
        public double file_size { get; set; }
        public DateTime create_date { get; set; }
    }
}
