using System;
using System.ComponentModel.DataAnnotations;

namespace Etax_Api
{
    public class RequestEtax
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public int member_user_id { get; set; }
        public int etax_file_id { get; set; }
        public string xml_path { get; set; }
        public string pdf_path { get; set; }
        public string status { get; set; }
        public DateTime create_date { get; set; }
    }
}