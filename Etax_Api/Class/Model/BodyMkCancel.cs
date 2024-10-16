using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodyMkCancel
    {
        public int member_id { get; set; }
        public string billID { get; set; }
        public string cancelPassword { get; set; }
    }
}
