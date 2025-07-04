
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System.DirectoryServices.AccountManagement;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Etax_Api.Middleware;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class LoadFileController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private readonly IExceptionLogger _exceptionLogger;
        public LoadFileController(ApplicationDbContext context, IConfiguration config, IExceptionLogger exceptionLogger)
        {
            _config = config;
            _context = context;
            _context.Database.SetCommandTimeout(180);
            _exceptionLogger = exceptionLogger;
        }
        [HttpPost]
        [Route("loadfile_link/{no}")]
        public async Task<IActionResult> LoadfileLink(string no)
        {
            try
            {
                int id = System.Convert.ToInt32(Encryption.Decrypt_AES256(no));

                EtaxFile etaxFile = await _context.etax_files
                .Where(x => x.id == id)
                .FirstOrDefaultAsync();

                if (etaxFile == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                DateTime now = DateTime.Now;
                string sharePath = "/" + etaxFile.member_id + "/" + Encryption.SHA256("path_" + now.ToString("dd-MM-yyyy")) + "/pdf/" + etaxFile.name + ".pdf";

                Function.DeleteFile(_config["Path:Share"]);
                Function.DeleteDirectory(_config["Path:Share"]);

                string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etaxFile.url_path + "/pdf/" + etaxFile.name + ".pdf", _config["Path:Mode"]);
                if (fileBase64 != "")
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_config["Path:Share"] + sharePath));
                    System.IO.File.WriteAllBytes(_config["Path:Share"] + sharePath, Convert.FromBase64String(fileBase64));
                }
                else
                    return StatusCode(400, new { message = "ไม่พบไฟล์ที่ต้องการ", });



                var send_sms = await (from ss in _context.send_sms
                                      where ss.etax_file_id == id
                                      select ss).FirstOrDefaultAsync();

                if (send_sms != null)
                {
                    send_sms.open_sms_status = "success";
                    send_sms.open_sms_finish = now;
                    _context.SaveChanges();
                }

                return StatusCode(200, new
                {
                    message = "โหลดข้อมูลสำเร็จ",
                    data = new
                    {
                        url = _config["Path:Url"] + sharePath,
                    },
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }
    }
}
