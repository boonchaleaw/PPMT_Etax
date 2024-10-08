using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class MemberPriceType
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public string xml_price_type { get; set; }
        public string pdf_price_type { get; set; }
        public string email_price_type { get; set; }
        public string sms_price_type { get; set; }
        public string ebxml_price_type { get; set; }

    }
}
