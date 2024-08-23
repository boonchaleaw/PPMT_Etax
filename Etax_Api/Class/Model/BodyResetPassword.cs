using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodyResetPassword
    {
        public string old_password { get; set; }
        public string new_password { get; set; }
        public string confirm_password { get; set; }
    }
}
