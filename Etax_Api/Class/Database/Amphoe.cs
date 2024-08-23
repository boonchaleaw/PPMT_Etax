using System;
using System.ComponentModel.DataAnnotations;

namespace Etax_Api
{
    public class Amphoe
    {
        [Key]
        public int amphoe_code { get; set; }
        public int province_code { get; set; }
        public string amphoe_th { get; set; }
        public string amphoe_en { get; set; }
        public string amphoe_th_s { get; set; }
        public string amphoe_en_s { get; set; }
    }
}