using System;
using System.ComponentModel.DataAnnotations;

namespace Etax_Api
{
    public class UserLoginFail
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public int login_count { get; set; }
        public DateTime login_date { get; set; }
        public DateTime create_date { get; set; }
    }
}