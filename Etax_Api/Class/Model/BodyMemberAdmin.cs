using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodyMemberAdmin
    {
        public int id { get; set; }
        public string name { get; set; }
        public string tax_id { get; set; }
        public string building_number { get; set; }
        public string building_name { get; set; }
        public string street_name { get; set; }
        public District district { get; set; }
        public Amphoe amphoe { get; set; }
        public Province province { get; set; }
        public List<DocumentType> listDocumentType { get; set; }
    }
}
