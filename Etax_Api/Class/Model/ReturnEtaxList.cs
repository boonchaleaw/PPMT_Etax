using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ReturnEtaxList
    {
        public int id { get; set; }
        public string type { get; set; }
        public string file { get; set; }
        public int count_xml_success { get; set; }
        public int count_pdf_success { get; set; }
        public List<ReturnEtaxFile> listXml { get; set; }
        public List<ReturnEtaxFile> listPdf { get; set; }
    }

    public class ReturnEtaxFile
    {
        public string name { get; set; }
        public string path { get; set; }
    }
}
