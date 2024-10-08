using System;
using System.ComponentModel.DataAnnotations;

namespace Etax_Api
{
    public class MemberSession
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public int member_user_id { get; set; }
        public string session_key { get; set; }
        public DateTime create_date { get; set; }
    }
}