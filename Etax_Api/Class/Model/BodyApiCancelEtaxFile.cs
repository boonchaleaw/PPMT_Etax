using Microsoft.AspNetCore.Http;

namespace Etax_Api
{
    public class BodyApiCancelEtaxFile
    {
        public string SellerTaxId { get; set; }
        public string SellerBranchId { get; set; }
        public string UserCode { get; set; }
        public string AccessKey { get; set; }
        public string APIKey { get; set; }
        public string ServiceCode { get; set; }
        public string FileName { get; set; }
        public string SourceSystem { get; set; }
    }
}
