using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class UserMember
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public int member_id { get; set; }

    }
}
