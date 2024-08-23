using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ViewEtaxCountDay
    {
        [Key]
        public Int64 id { get; set; }
        public int member_id { get; set; }
        public DateTime create_date { get; set; }
        public int file_total { get; set; }
    }
}