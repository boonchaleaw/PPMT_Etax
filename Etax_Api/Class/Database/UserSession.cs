using System;
using System.ComponentModel.DataAnnotations;

namespace Etax_Api
{
    public class UserSession
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public string session_key { get; set; }
        public DateTime create_date { get; set; }
    }
}