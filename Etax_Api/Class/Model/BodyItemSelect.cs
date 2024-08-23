using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodyItemSelect
    {
        public int id { get; set; }
        public bool select { get; set; }
    }
}
