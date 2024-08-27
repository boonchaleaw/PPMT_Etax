
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System.DirectoryServices.AccountManagement;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class LoginController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public LoginController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        [HttpGet]
        [Route("login")]
        public async Task<IActionResult> Login()
        {
            return StatusCode(200, new
            {
                message = "เข้าสู่ระบบสำเร็จ",
            });
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] BodyLogin bodyLogin)
        {
            if (bodyLogin.username == null || bodyLogin.username == "")
                return StatusCode(400, new { message = "กรุณากรอกผู้ใช้งานให้ถูกต้อง", });

            if (bodyLogin.password == null || bodyLogin.password == "")
                return StatusCode(400, new { message = "กรุณากรอกรหัสผ่านให้ถูกต้อง", });

            var user = _context.view_member_users
                .Select(x => new
                {
                    x.id,
                    x.member_id,
                    x.username,
                    x.password,
                    x.first_name,
                    x.member_name,
                })
                .Where(x => x.username == bodyLogin.username && x.password == Encryption.SHA256(bodyLogin.password))
                .FirstOrDefault();

            if (user != null)
            {
                return StatusCode(200, new
                {
                    message = "เข้าสู่ระบบสำเร็จ",
                    data = new
                    {
                        id = user.id,
                        member_id = user.member_id,
                        user_name = user.first_name,
                        member_name = user.member_name,
                    },
                    token = Jwt.GenerateJwtToken(user.id, user.member_id),
                });
            }
            else
            {
                return StatusCode(404, new
                {
                    message = "ไม่พบข้อมูลผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง",
                });
            }
        }

        [HttpPost]
        [Route("get_user_permission")]
        public async Task<IActionResult> GetAddRawDetail()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var memberUserPermission = await _context.member_user_permission
                .Where(x => x.member_user_id == jwtStatus.user_id)
                .Select(x => new
                {
                    x.per_branch_view,
                    x.per_branch_manage,
                    x.per_user_view,
                    x.per_user_manage,
                    x.per_raw_view,
                    x.per_raw_manage,
                    x.per_xml_view,
                    x.per_xml_manage,
                    x.per_pdf_view,
                    x.per_email_view,
                    x.per_email_manage,
                    x.per_sms_view,
                    x.per_sms_manage,
                    x.per_ebxml_view,
                    x.per_report_view,
                    x.view_self_only,
                    x.view_branch_only,
                })
                .FirstOrDefaultAsync();

                if (memberUserPermission == null)
                    return StatusCode(404, new { message = "ไม่พบข้อมูลที่ต้องการ", });


                var memberDocumentType = await _context.member_document_type
                .Where(x =>
                (x.member_id == jwtStatus.member_id && x.service_type_id == 2) ||
                (x.member_id == jwtStatus.member_id && x.service_type_id == 3))
                .FirstOrDefaultAsync();


                var memberUserBranch = await _context.member_user_branch
                .Where(x => x.member_user_id == jwtStatus.user_id)
                .ToListAsync();

                bool outsource = false;
                if (memberDocumentType != null)
                    outsource = true;

                return StatusCode(200, new
                {
                    message = "เรียกดูข้อมูลสำเร็จ",
                    data = new
                    {
                        permission = new
                        {
                            per_branch_view = (memberUserPermission.per_branch_view == "Y") ? true : false,
                            per_branch_manage = (memberUserPermission.per_branch_manage == "Y") ? true : false,
                            per_user_view = (memberUserPermission.per_user_view == "Y") ? true : false,
                            per_user_manage = (memberUserPermission.per_user_manage == "Y") ? true : false,
                            per_raw_view = (memberUserPermission.per_raw_view == "Y") ? true : false,
                            per_raw_manage = (memberUserPermission.per_raw_manage == "Y") ? true : false,
                            per_xml_view = (memberUserPermission.per_xml_view == "Y") ? true : false,
                            per_xml_manage = (memberUserPermission.per_xml_manage == "Y") ? true : false,
                            per_pdf_view = (memberUserPermission.per_pdf_view == "Y") ? true : false,
                            per_email_view = (memberUserPermission.per_email_view == "Y") ? true : false,
                            per_email_manage = (memberUserPermission.per_email_manage == "Y") ? true : false,
                            per_sms_view = (memberUserPermission.per_sms_view == "Y") ? true : false,
                            per_sms_manage = (memberUserPermission.per_sms_manage == "Y") ? true : false,
                            per_ebxml_view = (memberUserPermission.per_ebxml_view == "Y") ? true : false,
                            per_report_view = (memberUserPermission.per_report_view == "Y") ? true : false,
                            view_self_only = (memberUserPermission.view_self_only == "Y") ? true : false,
                            view_branch_only = (memberUserPermission.view_branch_only == "Y") ? true : false,
                            branchs = memberUserBranch,
                            per_outsource_view = outsource,
                        }
                    },
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        //////////////////////////Admin//////////////////////////

        [HttpPost]
        [Route("admin/login_user")]
        public async Task<IActionResult> LoginUser([FromBody] BodyLogin bodyLogin)
        {
            try
            {
                if (bodyLogin.username == null || bodyLogin.username == "")
                    return StatusCode(400, new { message = "กรุณากรอกผู้ใช้งานให้ถูกต้อง", });

                if (bodyLogin.password == null || bodyLogin.password == "")
                    return StatusCode(400, new { message = "กรุณากรอกรหัสผ่านให้ถูกต้อง", });

                var user = _context.users
                .Where(x => x.username == bodyLogin.username)
                .FirstOrDefault();

                if (user == null)
                    return StatusCode(404, new { message = "ไม่พบข้อมูลผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง", });

                if (user.type == "AD")
                {
                    PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, _config["AD:host"]);
                    bool userValid = principalContext.ValidateCredentials(bodyLogin.username, bodyLogin.password);

                    if (!userValid)
                        return StatusCode(404, new { message = "ไม่พบข้อมูลผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง", });


                    return StatusCode(200, new
                    {
                        message = "เข้าสู่ระบบสำเร็จ",
                        data = new
                        {
                            id = user.id,
                            username = user.username,
                        },
                        token = Jwt.GenerateJwtToken(user.id, user.id),
                    });
                }
                else
                {
                    var checkUser = _context.users
                    .Where(x => x.username == bodyLogin.username && x.password == Encryption.SHA256(bodyLogin.password))
                    .FirstOrDefault();


                    if (checkUser == null)
                        return StatusCode(404, new { message = "ไม่พบข้อมูลผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง", });

                    return StatusCode(200, new
                    {
                        message = "เข้าสู่ระบบสำเร็จ",
                        data = new
                        {
                            id = user.id,
                            username = user.username,
                        },
                        token = Jwt.GenerateJwtToken(user.id, user.id),
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }
    }
}
