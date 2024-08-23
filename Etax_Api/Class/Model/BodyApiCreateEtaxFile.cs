using Microsoft.AspNetCore.Http;

namespace Etax_Api
{
    public class BodyApiCreateEtaxFile
    {
        public IFormFile FileData { get; set; }
        public IFormFile FilePdf { get; set; }
    }
}
