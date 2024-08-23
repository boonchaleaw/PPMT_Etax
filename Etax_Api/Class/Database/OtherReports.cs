using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class OtherReports
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public string file_name { get; set; }
        public DateTime? create_date { get; set; }
    }
}
