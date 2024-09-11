using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class viewFileGroup
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public string group_name { get; set; }
    }
}