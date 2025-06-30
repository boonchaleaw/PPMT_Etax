using System.ComponentModel.DataAnnotations;
using System;

namespace Etax_Api.Class.Database
{
    public class JobActivity
    {
        public int id { get; set; }

        public string? activity { get; set; }

        public string? detail { get; set; }

        public string? name { get; set; }

        public int? jobid { get; set; }

        public int? memberid { get; set; }

        public DateTime? date { get; set; }
    }
}
