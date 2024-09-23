using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Etax_Api
{
    public class BodyCostReportAdmin
    {
        public int member_id { get; set; }
        public DateTime dateStart { get; set; }
        public DateTime dateEnd { get; set; }
        public List<Member> member { get; set; }
    }
}
