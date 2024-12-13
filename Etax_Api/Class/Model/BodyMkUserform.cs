using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodyMkUserform
    {
        public string lang { get; set; }
        public int type { get; set; }
        public int member_id { get; set; }
        public int branch_id { get; set; }
        public string tax_id { get; set; }
        public string branch_code { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public int province_code { get; set; }
        public string province { get; set; }
        public int amphoe_code { get; set; }
        public string amphoe { get; set; }
        public int district_code { get; set; }
        public string district { get; set; }
        public string zipcode { get; set; }
        public string email { get; set; }
        public string tel { get; set; }
        public BodyMkData dataQr { get; set; }
    }
}
