using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class LogBranch
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public int user_modify_id { get; set; }
        public int branch_id { get; set; }
        public string action_type { get; set; }
        public DateTime? create_date { get; set; }

    }
}
