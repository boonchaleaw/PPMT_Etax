using System;


namespace Etax_Api
{
    public class DataCostReport
    {
        public int id { get; set; }
        public string name { get; set; }
        public DateTime create_date { get; set; }
        public XmlData xml { get; set; }
        public PdfData pdf { get; set; }
        public EmailData email { get; set; }
        public EbxmlData ebxml { get; set; }
    }

    public class XmlData
    {
        public int count { get; set; }
        public double price { get; set; }
    }

    public class PdfData
    {
        public int count { get; set; }
        public double price { get; set; }
    }

    public class EmailData
    {
        public int count { get; set; }
        public double price { get; set; }
    }

    public class EbxmlData
    {
        public int count { get; set; }
        public double price { get; set; }
    }
}