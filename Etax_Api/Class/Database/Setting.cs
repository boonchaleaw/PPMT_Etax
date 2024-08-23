using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class Setting
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public string sendemail { get; set; }
        public string sendemail_day { get; set; }
        public string sendemail_dayweek { get; set; }
        public string sendemail_time_hh { get; set; }
        public string sendemail_time_mm { get; set; }
        public string sendebxml { get; set; }
        public string sendebxml_day { get; set; }
        public string sendebxml_dayweek { get; set; }
        public string sendebxml_time_hh { get; set; }
        public string sendebxml_time_mm { get; set; }

    }
}
