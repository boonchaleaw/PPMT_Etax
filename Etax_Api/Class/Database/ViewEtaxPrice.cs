using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ViewEtaxPrice
    {
        [Key]
        public Int64 id { get; set; }
        public int member_id { get; set; }
        public int date_month { get; set; }
        public int date_year { get; set; }
        public DateTime create_date_max { get; set; }
        public DateTime create_date_min { get; set; }
        public double price { get; set; }
        public double tax { get; set; }
        public double total { get; set; }
    }
}