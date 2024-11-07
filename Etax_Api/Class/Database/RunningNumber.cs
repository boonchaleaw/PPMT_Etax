using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class RunningNumber
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public string type { get; set; }
        public int number { get; set; }
    }
}
