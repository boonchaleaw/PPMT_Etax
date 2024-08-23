using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodyUserform
    {
        public string code { get; set; }
        public int id { get; set; }
        public string tax_id { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public string zipcode { get; set; }
        public string email { get; set; }
        public string tel { get; set; }
    }
}
