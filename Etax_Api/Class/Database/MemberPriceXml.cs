using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class MemberPriceXml
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public int count { get; set; }
        public double price { get; set; }

    }
}
