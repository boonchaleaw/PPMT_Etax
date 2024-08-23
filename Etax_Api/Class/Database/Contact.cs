using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class Contact
    {
        public int id { get; set; }
        public string subject { get; set; }
        public string name { get; set; }
        public string tel { get; set; }
        public string email { get; set; }
        public string message { get; set; }
        public DateTime create_date { get; set; }
    }
}
