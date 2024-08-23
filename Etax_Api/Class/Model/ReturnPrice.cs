using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ReturnPrice
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public int count { get; set; }
        public double price { get; set; }
        public int count_use { get; set; }
        public double price_use { get; set; }

    }
}
