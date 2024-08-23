using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class MemberUserBranch
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public int member_user_id { get; set; }
        public int branch_id { get; set; }
    }
}
