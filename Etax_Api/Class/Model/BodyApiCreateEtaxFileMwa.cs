using Microsoft.AspNetCore.Http;

namespace Etax_Api
{
    public class BodyApiCreateEtaxFileMwa
    {
        public string SellerTaxId { get; set; }
        public string SellerBranchId { get; set; }
        public string UserCode { get; set; }
        public string AccessKey { get; set; }
        public string APIKey { get; set; }
        public string ServiceCode { get; set; }
        public IFormFile TextContent { get; set; }
        public string PdfTemplateId { get; set; }
        public string SourceSystem { get; set; }
    }
}
