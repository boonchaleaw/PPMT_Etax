using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodyUser
    {
        public int id { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string username { get; set; }
    }
}
