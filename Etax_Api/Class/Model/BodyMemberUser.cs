using System;
using System.Collections.Generic;


namespace Etax_Api
{
    public class BodyMemberUser
    {
        public int id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string department { get; set; }
        public string email { get; set; }
        public string tel { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public BodyMemberUserPermission permission { get; set; }
    }
}
