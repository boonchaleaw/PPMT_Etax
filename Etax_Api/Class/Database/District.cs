using System;
using System.ComponentModel.DataAnnotations;

namespace Etax_Api
{
    public class District
    {
        [Key]
        public int district_code { get; set; }
        public int amphoe_code { get; set; }
        public string district_th { get; set; }
        public string district_en { get; set; }
        public string district_th_s { get; set; }
        public string district_en_s { get; set; }
        public string zipcode { get; set; }
    }
}