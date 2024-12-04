using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodyAdjustAddress
    {
        public string address { get; set; }
        public int province_code { get; set; }
        public int amphoe_code { get; set; }
        public int district_code { get; set; }
        public string zipcode { get; set; }
    }
}
