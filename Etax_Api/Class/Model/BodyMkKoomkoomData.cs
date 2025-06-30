using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodyMkKoomkoomData
    {
        public string documentType { get; set; }
        public string branchID { get; set; }
        public string billID { get; set; }
        public string billDate { get; set; }

        public string amount { get; set; }
        public string noDiscount { get; set; }
        public string fAndB { get; set; }
        public string service { get; set; }


        public string baseAmount { get; set; }
        public string discount { get; set; }
        public string vat { get; set; }
        public string totalAmount { get; set; }
        public string totalBeforeRounding { get; set; }
        public string rounding { get; set; }

        public string billIDRef { get; set; }
        public string checkSum { get; set; }
        public string url { get; set; }
        public string option { get; set; }
        public string item { get; set; }

    }
}
