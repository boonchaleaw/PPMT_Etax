using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Etax_Api
{
    public class BodyApiGetReportEmailTis
    {
        public string date { get; set; }
        public int hour { get; set; }
        public string APIKey { get; set; }
    }
}
