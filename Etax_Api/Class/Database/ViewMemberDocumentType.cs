using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ViewMemberDocumentType
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public int document_type_id { get; set; }
        public string document_type_name { get; set; }
        public int service_type_id { get; set; }
    }
}
