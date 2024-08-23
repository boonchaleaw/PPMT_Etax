using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ViewMemberUser
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string tel { get; set; }
        public string type { get; set; }
        public string member_name { get; set; }
    }
}