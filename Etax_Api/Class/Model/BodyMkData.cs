using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodyMkData
    {
        public string documentType { get; set; }
        public string branchCode { get; set; }
        public string billID { get; set; }
        public string bilIDate { get; set; }
        public string baseAmount { get; set; }
        public string discount { get; set; }
        public string vat { get; set; }
        public string totalAmount { get; set; }
        public string billIDRef { get; set; }
        public string checkSum { get; set; }
    }
}
