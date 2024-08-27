using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class User
    {
        public int id { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string type { get; set; }
        public DateTime? update_date { get; set; }
        public DateTime? create_date { get; set; }
        public int delete_status { get; set; }

    }
}
