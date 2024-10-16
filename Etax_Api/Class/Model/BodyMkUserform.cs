using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodyMkUserform
    {
        public int member_id { get; set; }
        public int branch_id { get; set; }
        public string tax_id { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public int province_code { get; set; }
        public string province { get; set; }
        public int amphoe_code { get; set; }
        public string amphoe { get; set; }
        public int district_code { get; set; }
        public string district { get; set; }
        public string zipcode { get; set; }
        public string email { get; set; }
        public string tel { get; set; }
        public BodyMkUserformDataQr dataQr { get; set; }
    }

    public class BodyMkUserformDataQr
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
