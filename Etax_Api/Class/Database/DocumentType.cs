using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class DocumentType
    {
        public int id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
    }
}
