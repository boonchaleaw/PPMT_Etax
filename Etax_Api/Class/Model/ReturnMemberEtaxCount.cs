using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ReturnMemberEtaxCount
    {
        public int id { get; set; }
        public string name { get; set; }

        public ReturnCount total_count { get; set; }
        public ReturnCount pending_count { get; set; }
        public ReturnCount success_count { get; set; }
        public ReturnCount error_count { get; set; }
    }
}
