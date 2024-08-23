using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class MemberToken
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public string library_path { get; set; }
        public int slot { get; set; }
        public string token_label { get; set; }
        public string cert_label { get; set; }
        public string pin { get; set; }
        public DateTime? expire_date { get; set; }
        public DateTime? update_date { get; set; }
        public DateTime? create_date { get; set; }
        public int delete_status { get; set; }

    }
}
