using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class Member
    {
        public int id { get; set; }
        public string name { get; set; }
        public string tax_id { get; set; }
        public string group_name { get; set; }
        public DateTime? update_date { get; set; }
        public DateTime? create_date { get; set; }
        public int delete_status { get; set; }
    }
}
