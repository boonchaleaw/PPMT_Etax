
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class ContactController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public ContactController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
        }

        [HttpPost]
        [Route("create_contact")]
        public async Task<IActionResult> CreateBranch([FromBody] BodyContact bodyContact)
        {
            try
            {
                if (String.IsNullOrEmpty(bodyContact.subject))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อเรื่อง", });

                if (String.IsNullOrEmpty(bodyContact.name))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อ", });

                if (String.IsNullOrEmpty(bodyContact.tel))
                    return StatusCode(400, new { message = "กรุณากำหนดเบอร์โทรศัพท์", });

                if (String.IsNullOrEmpty(bodyContact.email))
                    return StatusCode(400, new { message = "กรุณากำหนดอีเมล", });

                if (String.IsNullOrEmpty(bodyContact.message))
                    return StatusCode(400, new { message = "กรุณากำหนดรายละเอียด", });

                if (bodyContact.tel.Substring(0, 1) != "0")
                    return StatusCode(400, new { message = "กรุณากำหนดเบอร์โทรศัพท์ให้ถูกต้อง", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        Contact contact = new Contact()
                        {
                            subject = bodyContact.subject,
                            name = bodyContact.name,
                            tel = bodyContact.tel,
                            email = bodyContact.email,
                            message = bodyContact.message,
                            create_date = DateTime.Now,
                        };
                        _context.Add(contact);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        string message = "หัวข้อ : " + bodyContact.subject + "\n";
                        message += "ชื่อ : " + bodyContact.name + "\n";
                        message += "เบอร์โทรศัพท์ : " + bodyContact.tel + "\n";
                        message += "อีเมล : " + bodyContact.email + "\n";
                        message += "ข้อความ : " + bodyContact.message + "\n";
                        message = System.Web.HttpUtility.UrlEncode(message, Encoding.UTF8);


                        var request = (HttpWebRequest)WebRequest.Create("https://notify-api.line.me/api/notify");
                        var postData = string.Format("message={0}", message);
                        var data = Encoding.UTF8.GetBytes(postData);
                        request.Method = "POST";
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.ContentLength = data.Length;
                        request.Headers.Add("Authorization", "Bearer " + "XNS7yfPV9A4RziaXHgQvFdnCdfC4QtW1K0qxYoUflvf");
                        var stream = request.GetRequestStream();
                        stream.Write(data, 0, data.Length);
                        var response = (HttpWebResponse)request.GetResponse();
                        var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                        request = (HttpWebRequest)WebRequest.Create("https://notify-api.line.me/api/notify");
                        request.Method = "POST";
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.ContentLength = data.Length;
                        request.Headers.Add("Authorization", "Bearer " + "0hVWXu2gGb4QBiPBbVtVmr6X5GP6WXjNapxWV2nMs3o");
                        stream = request.GetRequestStream();
                        stream.Write(data, 0, data.Length);
                        response = (HttpWebResponse)request.GetResponse();
                        responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                        return StatusCode(200, new
                        {
                            message = "ส่งข้อมูลสำเร็จ",
                        });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(400, new { message = ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

    }
}
