using System;
using System.ComponentModel.DataAnnotations;

namespace Etax_Api
{
    public class Province
    {
        [Key]
        public int province_code { get; set; }
        public string province_th { get; set; }
        public string province_en { get; set; }
    }
}