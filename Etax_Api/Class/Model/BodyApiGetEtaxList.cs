using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Etax_Api
{
    public class BodyApiGetEtaxList
    {
        public string cycle { get; set; }
        public string template { get; set; }
        public int hour { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public string APIKey { get; set; }
    }
}
