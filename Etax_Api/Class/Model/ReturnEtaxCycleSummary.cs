using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ReturnEtaxCycleSummary
    {
        public string cycle { get; set; }
        public String template_pdf { get; set; }
        public int total { get; set; }
        public int count_xml_fail { get; set; }
        public int count_pdf_fail { get; set; }
        public int count_email_fail { get; set; }
        public List<ReturnEtaxSummaryFail> listXmlFail { get; set; }
        public List<ReturnEtaxSummaryFail> listPdfFail { get; set; }
        public List<ReturnEtaxSummaryFail> listEmailFail { get; set; }
    }

    public class ReturnEtaxCycleSummaryFail
    {
        public string name { get; set; }
        public string error { get; set; }
    }
}
