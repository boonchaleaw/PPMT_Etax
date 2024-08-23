using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class EtaxFileItem
    {
        public int id { get; set; }
        public int etax_file_id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public double qty { get; set; }
        public string unit { get; set; }
        public double price { get; set; }
        public double discount { get; set; }
        public double tax { get; set; }
        public double total { get; set; }
        public string tax_type { get; set; }
        public int tax_rate { get; set; }
        public string other { get; set; }
    }
}
