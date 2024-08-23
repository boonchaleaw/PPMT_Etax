using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodyCer
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public string library_path { get; set; }
        public int slot { get; set; }
        public string token_label { get; set; }
        public string cert_label { get; set; }
        public string pin { get; set; }
    }
}
