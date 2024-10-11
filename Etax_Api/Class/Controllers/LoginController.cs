
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
                    x.delete_status,
                })
                .Where(x => x.username == bodyLogin.username && x.password == Encryption.SHA256(bodyLogin.password) && x.delete_status == 0)
                .FirstOrDefault();

            if (user != null)
            {
                var session = await (from ms in _context.member_session
                                     where ms.member_id == user.member_id && ms.member_user_id == user.id
                                     select ms).FirstOrDefaultAsync();

                if (session != null)
                    _context.Remove(session);

                string session_key = Encryption.GetUniqueKey(50);
                _context.member_session.Add(new MemberSession()
                {
                    member_id = user.member_id,
                    member_user_id = user.id,
                    session_key = session_key,
                    create_date = DateTime.Now,
                });
                _context.SaveChanges();


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
                    token = Jwt.GenerateJwtToken(user.id, user.member_id, session_key),
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
        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            string token = Request.Headers[HeaderNames.Authorization].ToString();
            JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

            if (!jwtStatus.status)
                return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

            var session = (from ms in _context.member_session
                           where ms.member_id == jwtStatus.member_id &&
                           ms.member_user_id == jwtStatus.user_id &&
                           ms.session_key == jwtStatus.session_key
                           select ms).FirstOrDefault();

            if (session != null)
            {
                _context.Remove(session);
                _context.SaveChanges();
            }

            return StatusCode(200, new
            {
                message = "ออกจากระบบสำเร็จ",
            });
        }

        [HttpPost]
        [Route("get_user_permission")]
        public async Task<IActionResult> GetMemberUserPermission()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

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
                    x.per_etax_delete,
                    x.per_ebxml_view,
                    x.per_setting_manage,
                    x.per_report_view,
                    x.per_email_import,
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
                            per_setting_manage = (memberUserPermission.per_setting_manage == "Y") ? true : false,
                            per_report_view = (memberUserPermission.per_report_view == "Y") ? true : false,
                            per_etax_delete = (memberUserPermission.per_etax_delete == "Y") ? true : false,
                            per_email_import = (memberUserPermission.per_email_import == "Y") ? true : false,
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
                .Where(x => x.username == bodyLogin.username && x.delete_status == 0)
                .FirstOrDefault();

                if (user == null)
                    return StatusCode(404, new { message = "ไม่พบข้อมูลผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง", });

                if (user.type == "AD")
                {
                    PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, _config["AD:host"]);
                    bool userValid = principalContext.ValidateCredentials(bodyLogin.username, bodyLogin.password);

                    if (!userValid)
                        return StatusCode(404, new { message = "ไม่พบข้อมูลผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง", });

                    string session_key = Encryption.GetUniqueKey(50);
                    _context.user_session.Add(new UserSession()
                    {
                        user_id = user.id,
                        session_key = session_key,
                        create_date = DateTime.Now,
                    });
                    _context.SaveChanges();

                    return StatusCode(200, new
                    {
                        message = "เข้าสู่ระบบสำเร็จ",
                        data = new
                        {
                            id = user.id,
                            username = user.username,
                        },
                        token = Jwt.GenerateJwtToken(user.id, user.id, session_key),
                    });
                }
                else
                {
                    var checkUser = _context.users
                    .Where(x => x.username == bodyLogin.username && x.password == Encryption.SHA256(bodyLogin.password) && x.delete_status == 0)
                    .FirstOrDefault();


                    if (checkUser == null)
                        return StatusCode(404, new { message = "ไม่พบข้อมูลผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง", });

                    string session_key = Encryption.GetUniqueKey(50);
                    _context.user_session.Add(new UserSession()
                    {
                        user_id = user.id,
                        session_key = session_key,
                        create_date = DateTime.Now,
                    });
                    _context.SaveChanges();

                    return StatusCode(200, new
                    {
                        message = "เข้าสู่ระบบสำเร็จ",
                        data = new
                        {
                            id = user.id,
                            username = user.username,
                        },
                        token = Jwt.GenerateJwtToken(user.id, user.id, session_key),
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("admin/get_user_permission")]
        public async Task<IActionResult> GetUserPermission()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var userPermission = await (from up in _context.user_permission
                                            where up.user_id == jwtStatus.user_id
                                            select new
                                            {
                                                per_user_menu = (up.per_user_menu == "Y") ? true : false,
                                                per_user_detail = (up.per_user_detail == "Y") ? true : false,
                                                per_member_menu = (up.per_member_menu == "Y") ? true : false,
                                                per_member_detail = (up.per_member_detail == "Y") ? true : false,
                                                per_raw_menu = (up.per_raw_menu == "Y") ? true : false,
                                                per_raw_detail = (up.per_raw_detail == "Y") ? true : false,
                                                per_xml_menu = (up.per_xml_menu == "Y") ? true : false,
                                                per_xml_detail = (up.per_xml_detail == "Y") ? true : false,
                                                per_pdf_menu = (up.per_pdf_menu == "Y") ? true : false,
                                                per_pdf_detail = (up.per_pdf_detail == "Y") ? true : false,
                                                per_email_menu = (up.per_email_menu == "Y") ? true : false,
                                                per_email_detail = (up.per_email_detail == "Y") ? true : false,
                                                per_sms_menu = (up.per_sms_menu == "Y") ? true : false,
                                                per_sms_detail = (up.per_sms_detail == "Y") ? true : false,
                                                per_ebxml_menu = (up.per_ebxml_menu == "Y") ? true : false,
                                                per_ebxml_detail = (up.per_ebxml_detail == "Y") ? true : false,
                                                per_report_menu = (up.per_report_menu == "Y") ? true : false,
                                                per_report_detail = (up.per_report_detail == "Y") ? true : false,
                                                per_xml_file = (up.per_xml_file == "Y") ? true : false,
                                                per_pdf_file = (up.per_pdf_file == "Y") ? true : false,
                                            }).ToListAsync();


                if (userPermission.Count() == 0)
                    return StatusCode(404, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                return StatusCode(200, new
                {
                    message = "เรียกดูข้อมูลสำเร็จ",
                    data = new
                    {
                        permission = userPermission.First(),
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
