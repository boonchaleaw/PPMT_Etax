using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ReturnMemberCostReport
    {
        public int id { get; set; }
        public string name { get; set; }
        public ReturnCostReport listCostReport { get; set; }
    }
}
