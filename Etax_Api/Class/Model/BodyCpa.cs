using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodyCpa
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public string cpa_id { get; set; }
        public string rd_uri { get; set; }
        public string conversation_id { get; set; }
    }
}
