using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Etax_Api
{
    public class BodyApiGetEtaxCycleList
    {
        public string cycle { get; set; }
        public string time { get; set; }
        public string APIKey { get; set; }
    }
}
