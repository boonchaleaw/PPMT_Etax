using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class MemberProblem
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public int member_user_id { get; set; }
        public int type { get; set; }
        public string subject { get; set; }
        public string description { get; set; }
        public int priority { get; set; }
        public int status { get; set; }
        public DateTime update_date { get; set; }
        public DateTime create_date { get; set; }
    }
}
