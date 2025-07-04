
using Etax_Api.Class.Database;
using Etax_Api.Class.Model;
using Etax_Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class MemberUserController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IJwtService _jwtService;
        public MemberUserController(ApplicationDbContext context, IConfiguration config, IExceptionLogger exceptionLogger, IJwtService jwtService)
        {
            _config = config;
            _context = context;
            _context.Database.SetCommandTimeout(180);
            _exceptionLogger = exceptionLogger;
            _jwtService = jwtService;
        }

        [HttpPost]
        [Route("get_member_user_tabel")]
        public async Task<IActionResult> GetRawFilesDataTabel([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_user_view).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                var searchBy = bodyDtParameters.Search?.Value;
                var orderCriteria = "id";
                var orderAscendingDirection = true;

                if (bodyDtParameters.Order != null)
                {
                    orderCriteria = bodyDtParameters.Columns[bodyDtParameters.Order[0].Column].Data;
                    orderAscendingDirection = bodyDtParameters.Order[0].Dir.ToString().ToLower() == "asc";
                }

                var result = _context.view_member_users.Where(x => x.member_id == jwtStatus.member_id && x.type != "admin" && x.delete_status == 0).AsQueryable();

                if (!string.IsNullOrEmpty(searchBy))
                {
                    result = result.Where(r =>
                        r.id.ToString().ToUpper().Contains(searchBy.ToUpper()) ||
                        r.username != null && r.username.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.first_name != null && r.first_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.last_name != null && r.last_name.ToUpper().Contains(searchBy.ToUpper())
                    );
                }

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.CountAsync();
                var totalResultsCount = await _context.view_member_users.Where(x => x.member_id == jwtStatus.member_id && x.type != "admin").CountAsync();

                var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.username,
                        x.first_name,
                        x.last_name,
                    })
                    .Skip(bodyDtParameters.Start)
                    .Take(bodyDtParameters.Length)
                    .ToListAsync();


                return StatusCode(200, new
                {
                    draw = bodyDtParameters.Draw,
                    recordsTotal = totalResultsCount,
                    recordsFiltered = filteredResultsCount,
                    data = data,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("create_member_user")]
        public async Task<IActionResult> CreateMemberUser([FromBody] BodyMemberUser bodyMemberUser)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_user_manage).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                if (string.IsNullOrEmpty(bodyMemberUser.first_name))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อผู้ใช้งาน", });

                if (string.IsNullOrEmpty(bodyMemberUser.last_name))
                    return StatusCode(400, new { message = "กรุณากำหนดนามสกุลผู้ใช้งาน", });

                if (string.IsNullOrEmpty(bodyMemberUser.username))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อเข้าใช้งานมากกว่า 4 ตัวอักษร", });
                if (bodyMemberUser.username.Length < 4)
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อเข้าใช้งานมากกว่า 4 ตัวอักษร", });

                if (string.IsNullOrEmpty(bodyMemberUser.password))
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสผ่านมากกว่า 8 ตัวอักษร", });
                if (bodyMemberUser.password.Length < 8)
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสผ่านมากกว่า 8 ตัวอักษร", });


                var hasNumber = new Regex(@"[0-9]+");
                var hasUpperChar = new Regex(@"[A-Z]+");
                var hasMiniMaxChars = new Regex(@".{8,64}");
                var hasLowerChar = new Regex(@"[a-z]+");
                var hasSymbols = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");

                if (!hasLowerChar.IsMatch(bodyMemberUser.password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีภาษาอังกฤษตัวเล็กอย่างน้อย 1 ตัวอักษร", });
                }
                else if (!hasUpperChar.IsMatch(bodyMemberUser.password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีภาษาอังกฤษตัวใหญ่อย่างน้อย 1 ตัวอักษร", });
                }
                else if (!hasMiniMaxChars.IsMatch(bodyMemberUser.password))
                {
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสผ่านใหม่มากกว่า 8 ตัวอักษร", });
                }
                else if (!hasNumber.IsMatch(bodyMemberUser.password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีภาษาอังกฤษตัวเลขอย่างน้อย 1 ตัวอักษร", });
                }
                else if (!hasSymbols.IsMatch(bodyMemberUser.password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีสัญลักษณ์พิเศษอย่างน้อย 1 ตัวอักษร", });
                }

                var checkMemberUser = await _context.member_users
                    .Where(x => x.username == bodyMemberUser.username)
                    .FirstOrDefaultAsync();

                if (checkMemberUser != null)
                    return StatusCode(400, new { message = "ชื่อเข้าใช้งานนี้มีในระบบแล้ว", });

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        MemberUser memberUser = new MemberUser()
                        {
                            member_id = jwtStatus.member_id,
                            first_name = bodyMemberUser.first_name,
                            last_name = bodyMemberUser.last_name,
                            department = bodyMemberUser.department,
                            email = bodyMemberUser.email,
                            tel = bodyMemberUser.tel,
                            username = bodyMemberUser.username,
                            password = Encryption.SHA256(bodyMemberUser.password),
                            type = "user",
                            create_date = DateTime.Now,
                        };
                        _context.Add(memberUser);
                        await _context.SaveChangesAsync();

                        MemberUserPermission memberUserPermission = new MemberUserPermission()
                        {
                            member_user_id = memberUser.id,
                            per_branch_view = (bodyMemberUser.permission.per_branch_view == true) ? "Y" : "N",
                            per_branch_manage = (bodyMemberUser.permission.per_branch_manage == true) ? "Y" : "N",
                            per_user_view = (bodyMemberUser.permission.per_user_view == true) ? "Y" : "N",
                            per_user_manage = (bodyMemberUser.permission.per_user_manage == true) ? "Y" : "N",
                            per_raw_view = (bodyMemberUser.permission.per_raw_view == true) ? "Y" : "N",
                            per_raw_manage = (bodyMemberUser.permission.per_raw_manage == true) ? "Y" : "N",
                            per_xml_view = (bodyMemberUser.permission.per_xml_view == true) ? "Y" : "N",
                            per_xml_manage = (bodyMemberUser.permission.per_xml_manage == true) ? "Y" : "N",
                            per_pdf_view = (bodyMemberUser.permission.per_pdf_view == true) ? "Y" : "N",
                            per_email_view = (bodyMemberUser.permission.per_email_view == true) ? "Y" : "N",
                            per_email_manage = (bodyMemberUser.permission.per_email_manage == true) ? "Y" : "N",
                            per_sms_view = (bodyMemberUser.permission.per_sms_view == true) ? "Y" : "N",
                            per_sms_manage = (bodyMemberUser.permission.per_sms_manage == true) ? "Y" : "N",
                            per_ebxml_view = (bodyMemberUser.permission.per_ebxml_view == true) ? "Y" : "N",
                            per_report_view = (bodyMemberUser.permission.per_report_view == true) ? "Y" : "N",
                            per_etax_delete = "N",
                            per_email_import = "N",
                            view_self_only = (bodyMemberUser.permission.view_self_only == true) ? "Y" : "N",
                            view_branch_only = (bodyMemberUser.permission.view_branch_only == true) ? "Y" : "N",
                        };
                        _context.Add(memberUserPermission);
                        await _context.SaveChangesAsync();


                        foreach (BodyPermissionBranch branch in bodyMemberUser.permission.branchs)
                        {
                            if (branch.select)
                            {
                                MemberUserBranch memberUserBranch = new MemberUserBranch()
                                {
                                    member_id = branch.member_id,
                                    member_user_id = memberUser.id,
                                    branch_id = branch.id,
                                };
                                _context.Add(memberUserBranch);
                                await _context.SaveChangesAsync();
                            }
                        }


                        LogMemberUser logMemberUser = new LogMemberUser()
                        {
                            member_id = jwtStatus.member_id,
                            user_modify_id = jwtStatus.user_id,
                            user_id = memberUser.id,
                            action_type = "create",
                            create_date = DateTime.Now,
                        };
                        _context.Add(logMemberUser);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return StatusCode(400, new { message = ex.Message });
                    }
                }

                return StatusCode(200, new
                {
                    message = "เพิ่มผู้ใช้งานสำเร็จ",
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("delete_mamber_user/{id}")]
        public async Task<IActionResult> DeleteMemberUser(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_user_manage).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var member_user = await _context.member_users
                        .Where(x => x.id == id)
                        .FirstOrDefaultAsync();

                        if (member_user == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        DateTime now = DateTime.Now;
                        member_user.delete_status = 1;
                        member_user.update_date = now;

                        _context.Update(member_user);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return StatusCode(200, new
                        {
                            message = "ลบข้อมูลสำเร็จ",
                        });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return StatusCode(400, new { message = ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("get_member_user_detail/{id}")]
        public async Task<IActionResult> GetMemberUserDetail(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_user_view).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                var memberUser = await _context.member_users
                .Where(x => x.id == id && x.member_id == jwtStatus.member_id)
                .Select(x => new
                {
                    x.id,
                    x.username,
                    x.first_name,
                    x.last_name,
                    x.department,
                    x.email,
                    x.tel,
                })
                .FirstOrDefaultAsync();

                var memberUserPermission = await _context.member_user_permission
                .Where(x => x.member_user_id == id)
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
                    x.per_setting_manage,
                    x.per_report_view,
                    x.view_self_only,
                    x.view_branch_only,
                    x.job_order,
                    x.job_order_name,
                })
                .FirstOrDefaultAsync();



                var memberUserBranch = await _context.member_user_branch
                .Where(x => x.member_user_id == id)
                .ToListAsync();

                var jobNamesSelect = await _context.job_permission
.Where(p => p.UserMemberId == jwtStatus.user_id && p.JobName != null)
.Include(j => j.JobName)
.Select(j => new Dictionary<string, object>
{
              { "Id",j.JobId},
        { "ThaiName", j.JobName.ThaiName },
        { "EngName", j.JobName.EngName },
        { "JobCode", j.JobName.JobCode }
})
.ToListAsync();

                if (memberUser != null && memberUserPermission != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            memberUser.id,
                            memberUser.username,
                            memberUser.first_name,
                            memberUser.last_name,
                            memberUser.department,
                            memberUser.email,
                            memberUser.tel,
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
                                view_self_only = (memberUserPermission.view_self_only == "Y") ? true : false,
                                view_branch_only = (memberUserPermission.view_branch_only == "Y") ? true : false,
                                branchs = memberUserBranch,
                                job_order = (memberUserPermission.job_order == "Y") ? true : false,
                                job_order_name = (memberUserPermission.job_order_name == "Y") ? true : false,
                                jobNamesSelect = jobNamesSelect,
                            }
                        },
                    });
                }
                else
                {
                    return StatusCode(404, new
                    {
                        message = "ไม่พบข้อมูลที่ต้องการ",
                    });
                }

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("update_member_user")]
        public async Task<IActionResult> UpdateMemberUser([FromBody] BodyMemberUser bodyMemberUser)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_user_manage).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });



                if (String.IsNullOrEmpty(bodyMemberUser.first_name))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อผู้ใช้งาน", });

                if (String.IsNullOrEmpty(bodyMemberUser.last_name))
                    return StatusCode(400, new { message = "กรุณากำหนดนามสกุลผู้ใช้งาน", });

                var memberUser = await _context.member_users
                    .Where(x => x.id == bodyMemberUser.id)
                    .FirstOrDefaultAsync();

                var memberUserPermission = await _context.member_user_permission
                   .Where(x => x.member_user_id == bodyMemberUser.id)
                   .FirstOrDefaultAsync();

                if (memberUser == null || memberUserPermission == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });


                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        memberUser.first_name = bodyMemberUser.first_name;
                        memberUser.last_name = bodyMemberUser.last_name;
                        memberUser.email = bodyMemberUser.email;
                        memberUser.tel = bodyMemberUser.tel;
                        await _context.SaveChangesAsync();

                        memberUserPermission.per_branch_view = (bodyMemberUser.permission.per_branch_view == true) ? "Y" : "N";
                        memberUserPermission.per_branch_manage = (bodyMemberUser.permission.per_branch_manage == true) ? "Y" : "N";
                        memberUserPermission.per_user_view = (bodyMemberUser.permission.per_user_view == true) ? "Y" : "N";
                        memberUserPermission.per_user_manage = (bodyMemberUser.permission.per_user_manage == true) ? "Y" : "N";
                        memberUserPermission.per_raw_view = (bodyMemberUser.permission.per_raw_view == true) ? "Y" : "N";
                        memberUserPermission.per_raw_manage = (bodyMemberUser.permission.per_raw_manage == true) ? "Y" : "N";
                        memberUserPermission.per_xml_view = (bodyMemberUser.permission.per_xml_view == true) ? "Y" : "N";
                        memberUserPermission.per_xml_manage = (bodyMemberUser.permission.per_xml_manage == true) ? "Y" : "N";
                        memberUserPermission.per_pdf_view = (bodyMemberUser.permission.per_pdf_view == true) ? "Y" : "N";
                        memberUserPermission.per_email_view = (bodyMemberUser.permission.per_email_view == true) ? "Y" : "N";
                        memberUserPermission.per_email_manage = (bodyMemberUser.permission.per_email_manage == true) ? "Y" : "N";
                        memberUserPermission.per_sms_view = (bodyMemberUser.permission.per_sms_view == true) ? "Y" : "N";
                        memberUserPermission.per_sms_manage = (bodyMemberUser.permission.per_sms_manage == true) ? "Y" : "N";
                        memberUserPermission.per_ebxml_view = (bodyMemberUser.permission.per_ebxml_view == true) ? "Y" : "N";
                        memberUserPermission.per_setting_manage = (bodyMemberUser.permission.per_setting_manage == true) ? "Y" : "N";
                        memberUserPermission.per_report_view = (bodyMemberUser.permission.per_report_view == true) ? "Y" : "N";
                        memberUserPermission.view_self_only = (bodyMemberUser.permission.view_self_only == true) ? "Y" : "N";
                        memberUserPermission.view_branch_only = (bodyMemberUser.permission.view_branch_only == true) ? "Y" : "N";
                        await _context.SaveChangesAsync();

                        foreach (BodyPermissionBranch branch in bodyMemberUser.permission.branchs)
                        {
                            if (branch.select)
                            {
                                var memberUserBranchCheck = await _context.member_user_branch
                                .Where(x => x.member_id == branch.member_id && x.member_user_id == memberUser.id && x.branch_id == branch.id)
                                .FirstOrDefaultAsync();
                                if (memberUserBranchCheck == null)
                                {
                                    MemberUserBranch memberUserBranch = new MemberUserBranch()
                                    {
                                        member_id = branch.member_id,
                                        member_user_id = memberUser.id,
                                        branch_id = branch.id,
                                    };
                                    _context.Add(memberUserBranch);
                                }
                            }
                            else
                            {
                                var memberUserBranchCheck = await _context.member_user_branch
                                .Where(x => x.member_id == branch.member_id && x.member_user_id == memberUser.id && x.branch_id == branch.id)
                                .FirstOrDefaultAsync();

                                if (memberUserBranchCheck != null)
                                {
                                    _context.Remove(memberUserBranchCheck);
                                }
                            }
                        }


                        var jobPermissionCheck = await _context.job_permission
                              .Where(x => x.UserMemberId == memberUser.id)
                              .ToListAsync();

                        if (jobPermissionCheck != null)
                        {
                            _context.RemoveRange(jobPermissionCheck);
                        }

                        foreach (BodyPermissionJobName jobname in bodyMemberUser.permission.jobNamesSelect)
                        {

                            JobPermission jobPermission = new JobPermission()
                            {
                                UserMemberId = memberUser.id,
                                JobId = jobname.Id,
                                JobName = await _context.job_name.FindAsync(jobname.Id),
                            };
                            _context.Add(jobPermission);

                        }


                        LogMemberUser logMemberUser = new LogMemberUser()
                        {
                            member_id = jwtStatus.member_id,
                            user_modify_id = jwtStatus.user_id,
                            user_id = memberUser.id,
                            action_type = "update",
                            create_date = DateTime.Now,
                        };
                        _context.Add(logMemberUser);
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();

                        return StatusCode(200, new
                        {
                            message = "แก้ไขข้อมูลสำเร็จ",
                        });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return StatusCode(400, new { message = ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("reset_password")]
        public async Task<IActionResult> ResetPasswordMemberUser([FromBody] BodyResetPassword bodyResetPassword)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                if (String.IsNullOrEmpty(bodyResetPassword.old_password))
                    return StatusCode(400, new { message = "กรุณากำหนดข้อมูลให้ครบถ้วน", });

                if (String.IsNullOrEmpty(bodyResetPassword.new_password))
                    return StatusCode(400, new { message = "กรุณากำหนดข้อมูลให้ครบถ้วน", });

                if (String.IsNullOrEmpty(bodyResetPassword.confirm_password))
                    return StatusCode(400, new { message = "กรุณากำหนดข้อมูลให้ครบถ้วน", });

                if (bodyResetPassword.new_password.Length < 8)
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสผ่านใหม่มากกว่า 8 ตัวอักษร", });

                if (bodyResetPassword.new_password != bodyResetPassword.confirm_password)
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสผ่านใหม่และยืนยันรหัสผ่านใหม่ให้ตรงกัน", });

                var hasNumber = new Regex(@"[0-9]+");
                var hasUpperChar = new Regex(@"[A-Z]+");
                var hasMiniMaxChars = new Regex(@".{8,64}");
                var hasLowerChar = new Regex(@"[a-z]+");
                var hasSymbols = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");

                if (!hasLowerChar.IsMatch(bodyResetPassword.new_password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีภาษาอังกฤษตัวเล็กอย่างน้อย 1 ตัวอักษร", });
                }
                else if (!hasUpperChar.IsMatch(bodyResetPassword.new_password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีภาษาอังกฤษตัวใหญ่อย่างน้อย 1 ตัวอักษร", });
                }
                else if (!hasMiniMaxChars.IsMatch(bodyResetPassword.new_password))
                {
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสผ่านใหม่มากกว่า 8 ตัวอักษร", });
                }
                else if (!hasNumber.IsMatch(bodyResetPassword.new_password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีภาษาอังกฤษตัวเลขอย่างน้อย 1 ตัวอักษร", });
                }
                else if (!hasSymbols.IsMatch(bodyResetPassword.new_password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีสัญลักษณ์พิเศษอย่างน้อย 1 ตัวอักษร", });
                }


                var memberUser = _context.member_users
                    .Where(x => x.id == jwtStatus.user_id)
                    .FirstOrDefault();

                if (memberUser == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                if (Encryption.SHA256(bodyResetPassword.old_password) != memberUser.password)
                    return StatusCode(400, new { message = "รหัสผ่านเก่าไม่ถูกต้อง", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        memberUser.password = Encryption.SHA256(bodyResetPassword.new_password);
                        await _context.SaveChangesAsync();

                        LogMemberUser logMemberUser = new LogMemberUser()
                        {
                            member_id = jwtStatus.member_id,
                            user_modify_id = jwtStatus.user_id,
                            user_id = memberUser.id,
                            action_type = "reset_password",
                            create_date = DateTime.Now,
                        };
                        _context.Add(logMemberUser);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "แก้ไขข้อมูลสำเร็จ",
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

        [HttpPost]
        [Route("change_password")]
        public async Task<IActionResult> ChangePasswordMemberUser([FromBody] BodyChangePassword bodyChangePassword)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_user_manage).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                if (String.IsNullOrEmpty(bodyChangePassword.new_password))
                    return StatusCode(400, new { message = "กรุณากำหนดข้อมูลให้ครบถ้วน", });

                if (String.IsNullOrEmpty(bodyChangePassword.confirm_password))
                    return StatusCode(400, new { message = "กรุณากำหนดข้อมูลให้ครบถ้วน", });

                if (bodyChangePassword.new_password.Length < 8)
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสผ่านใหม่มากกว่า 8 ตัวอักษร", });

                if (bodyChangePassword.new_password != bodyChangePassword.confirm_password)
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสผ่านใหม่และยืนยันรหัสผ่านใหม่ให้ตรงกัน", });

                var hasNumber = new Regex(@"[0-9]+");
                var hasUpperChar = new Regex(@"[A-Z]+");
                var hasMiniMaxChars = new Regex(@".{8,64}");
                var hasLowerChar = new Regex(@"[a-z]+");
                var hasSymbols = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");

                if (!hasLowerChar.IsMatch(bodyChangePassword.new_password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีภาษาอังกฤษตัวเล็กอย่างน้อย 1 ตัวอักษร", });
                }
                else if (!hasUpperChar.IsMatch(bodyChangePassword.new_password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีภาษาอังกฤษตัวใหญ่อย่างน้อย 1 ตัวอักษร", });
                }
                else if (!hasMiniMaxChars.IsMatch(bodyChangePassword.new_password))
                {
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสผ่านใหม่มากกว่า 8 ตัวอักษร", });
                }
                else if (!hasNumber.IsMatch(bodyChangePassword.new_password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีภาษาอังกฤษตัวเลขอย่างน้อย 1 ตัวอักษร", });
                }
                else if (!hasSymbols.IsMatch(bodyChangePassword.new_password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีสัญลักษณ์พิเศษอย่างน้อย 1 ตัวอักษร", });
                }


                var memberUser = _context.member_users
                    .Where(x => x.id == bodyChangePassword.id)
                    .FirstOrDefault();

                if (memberUser == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        memberUser.password = Encryption.SHA256(bodyChangePassword.new_password);
                        await _context.SaveChangesAsync();

                        LogMemberUser logMemberUser = new LogMemberUser()
                        {
                            member_id = jwtStatus.member_id,
                            user_modify_id = jwtStatus.user_id,
                            user_id = memberUser.id,
                            action_type = "change_password",
                            create_date = DateTime.Now,
                        };
                        _context.Add(logMemberUser);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "แก้ไขข้อมูลสำเร็จ",
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

        //////////////////////////Admin//////////////////////////

        [HttpPost]
        [Route("admin/get_mamber_user_tabel")]
        public async Task<IActionResult> GetMemberUserTabelAdmin([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                var searchBy = bodyDtParameters.Search?.Value;
                var orderCriteria = "id";
                var orderAscendingDirection = true;

                if (bodyDtParameters.Order != null)
                {
                    orderCriteria = bodyDtParameters.Columns[bodyDtParameters.Order[0].Column].Data;
                    orderAscendingDirection = bodyDtParameters.Order[0].Dir.ToString().ToLower() == "asc";
                }

                var table = _context.member_users.Where(x => x.delete_status == 0).AsQueryable();

                if (bodyDtParameters.id > 0)
                    table = table.Where(x => x.member_id == bodyDtParameters.id);

                if (!string.IsNullOrEmpty(searchBy))
                {
                    table = table.Where(r =>
                        r.id.ToString().ToUpper().Contains(searchBy.ToUpper()) ||
                        r.username != null && r.username.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.first_name != null && r.first_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.last_name != null && r.last_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.email != null && r.email.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.tel != null && r.tel.ToUpper().Contains(searchBy.ToUpper())
                    );
                }

                table = orderAscendingDirection ? table.OrderByProperty(orderCriteria) : table.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await table.CountAsync();

                var tableCount = _context.member_users.Where(x => x.delete_status == 0).AsQueryable();

                if (bodyDtParameters.id > 0)
                    tableCount = tableCount.Where(x => x.member_id == bodyDtParameters.id);

                var totalResultsCount = await tableCount.CountAsync();


                if (bodyDtParameters.Length == -1)
                {
                    var data = await table
                    .Select(x => new
                    {
                        x.id,
                        x.username,
                        x.first_name,
                        x.last_name,
                        x.email,
                        x.tel,
                        x.type,
                        create_date = (x.create_date == null) ? "" : ((DateTime)x.create_date).ToString("dd/MM/yyyy HH:mm:ss"),
                    })
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        data = data,
                    });
                }
                else
                {
                    var data = await table
                    .Select(x => new
                    {
                        x.id,
                        x.username,
                        x.first_name,
                        x.last_name,
                        x.email,
                        x.tel,
                        x.type,
                        create_date = (x.create_date == null) ? "" : ((DateTime)x.create_date).ToString("dd/MM/yyyy HH:mm:ss"),
                    })
                    .Skip(bodyDtParameters.Start)
                    .Take(bodyDtParameters.Length)
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        data = data,
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("admin/get_mamber_user_detail/{id}")]
        public async Task<IActionResult> GetMemberUserDetailAdmin(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                var member_users = await _context.member_users
                .Where(x => x.id == id)
                .Select(x => new
                {
                    id = x.id,
                    username = x.username,
                    first_name = x.first_name,
                    last_name = x.last_name,
                    email = x.email,
                    tel = x.tel,
                    create_date = (x.create_date == null) ? "" : ((DateTime)x.create_date).ToString("dd/MM/yyyy HH:mm:ss"),
                })
                .FirstOrDefaultAsync();

                if (member_users != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = member_users,
                    });
                }
                else
                {
                    return StatusCode(404, new
                    {
                        message = "ไม่พบข้อมูลที่ต้องการ",
                    });
                }

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("admin/get_permission_detail/{id}")]
        public async Task<IActionResult> GetPermissionDetailAdmin(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                var member_user_permission = await _context.member_user_permission
                .Where(x => x.id == id)
                .Select(x => new
                {
                    id = x.id,
                    per_branch_view = (x.per_branch_view == "Y") ? true : false,
                    per_branch_manage = (x.per_branch_manage == "Y") ? true : false,
                    per_user_view = (x.per_user_view == "Y") ? true : false,
                    per_user_manage = (x.per_user_manage == "Y") ? true : false,
                    per_raw_view = (x.per_raw_view == "Y") ? true : false,
                    per_raw_manage = (x.per_raw_manage == "Y") ? true : false,
                    per_xml_view = (x.per_xml_view == "Y") ? true : false,
                    per_pdf_view = (x.per_pdf_view == "Y") ? true : false,
                    per_email_view = (x.per_email_view == "Y") ? true : false,
                    per_ebxml_view = (x.per_ebxml_view == "Y") ? true : false,
                    per_report_view = (x.per_raw_view == "Y") ? true : false,
                })
                .FirstOrDefaultAsync();

                if (member_user_permission != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = member_user_permission,
                    });
                }
                else
                {
                    return StatusCode(404, new
                    {
                        message = "ไม่พบข้อมูลที่ต้องการ",
                    });
                }

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("admin/add_mamber_user")]
        public async Task<IActionResult> AddMemberUserAdmin([FromBody] BodyMemberUserAdmin bodyMemberUserAdmin)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                if (String.IsNullOrEmpty(bodyMemberUserAdmin.first_name))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อลูกค้า", });

                if (String.IsNullOrEmpty(bodyMemberUserAdmin.last_name))
                    return StatusCode(400, new { message = "กรุณากำหนดรามสกุลลูกค้า", });

                if (String.IsNullOrEmpty(bodyMemberUserAdmin.email))
                    return StatusCode(400, new { message = "กรุณากำหนดอีเมลลูกค้า", });

                if (String.IsNullOrEmpty(bodyMemberUserAdmin.tel))
                    return StatusCode(400, new { message = "กรุณากำหนดเบอร์โทรศัพท์ลูกค้า", });

                if (bodyMemberUserAdmin.password.Length < 8)
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสผ่านมากกว่า 8 ตัวอักษร", });

                if (bodyMemberUserAdmin.password != bodyMemberUserAdmin.confirm_password)
                    return StatusCode(400, new { message = "รหัสผ่านและยืนยันรหัสผ่านไม่ถูกต้อง", });

                var hasNumber = new Regex(@"[0-9]+");
                var hasUpperChar = new Regex(@"[A-Z]+");
                var hasMiniMaxChars = new Regex(@".{8,64}");
                var hasLowerChar = new Regex(@"[a-z]+");
                var hasSymbols = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");

                if (!hasLowerChar.IsMatch(bodyMemberUserAdmin.password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีภาษาอังกฤษตัวเล็กอย่างน้อย 1 ตัวอักษร", });
                }
                else if (!hasUpperChar.IsMatch(bodyMemberUserAdmin.password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีภาษาอังกฤษตัวใหญ่อย่างน้อย 1 ตัวอักษร", });
                }
                else if (!hasMiniMaxChars.IsMatch(bodyMemberUserAdmin.password))
                {
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสผ่านใหม่มากกว่า 8 ตัวอักษร", });
                }
                else if (!hasNumber.IsMatch(bodyMemberUserAdmin.password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีภาษาอังกฤษตัวเลขอย่างน้อย 1 ตัวอักษร", });
                }
                else if (!hasSymbols.IsMatch(bodyMemberUserAdmin.password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีสัญลักษณ์พิเศษอย่างน้อย 1 ตัวอักษร", });
                }

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        DateTime now = DateTime.Now;
                        MemberUser memberuser = new MemberUser()
                        {
                            member_id = bodyMemberUserAdmin.member_id,
                            username = bodyMemberUserAdmin.username,
                            password = Encryption.SHA256(bodyMemberUserAdmin.password),
                            first_name = bodyMemberUserAdmin.first_name,
                            last_name = bodyMemberUserAdmin.last_name,
                            email = bodyMemberUserAdmin.email,
                            tel = bodyMemberUserAdmin.tel,
                            type = "user",
                            update_date = now,
                            create_date = now,
                        };
                        _context.Add(memberuser);
                        await _context.SaveChangesAsync();

                        MemberUserPermission member_user_permission = new MemberUserPermission()
                        {
                            member_user_id = memberuser.id,
                            per_branch_view = "Y",
                            per_branch_manage = "Y",
                            per_user_view = "Y",
                            per_user_manage = "Y",
                            per_raw_view = "Y",
                            per_raw_manage = "Y",
                            per_xml_view = "Y",
                            per_xml_manage = "Y",
                            per_pdf_view = "Y",
                            per_email_view = "Y",
                            per_email_manage = "Y",
                            per_ebxml_view = "Y",
                            per_report_view = "Y",
                            update_date = now,
                            create_date = now,
                        };
                        _context.Add(member_user_permission);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "เพิ่มข้อมูลสำเร็จ",
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

        [HttpPost]
        [Route("admin/update_mamber_user")]
        public async Task<IActionResult> UpdateMemberUserAdmin([FromBody] BodyMemberUserAdmin bodyMemberUserAdmin)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                if (String.IsNullOrEmpty(bodyMemberUserAdmin.first_name))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อลูกค้า", });

                if (String.IsNullOrEmpty(bodyMemberUserAdmin.last_name))
                    return StatusCode(400, new { message = "กรุณากำหนดรามสกุลลูกค้า", });

                if (String.IsNullOrEmpty(bodyMemberUserAdmin.email))
                    return StatusCode(400, new { message = "กรุณากำหนดอีเมลลูกค้า", });

                if (String.IsNullOrEmpty(bodyMemberUserAdmin.tel))
                    return StatusCode(400, new { message = "กรุณากำหนดเบอร์โทรศัพท์ลูกค้า", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        DateTime now = DateTime.Now;
                        var member_user = await _context.member_users
                        .Where(x => x.id == bodyMemberUserAdmin.id)
                        .FirstOrDefaultAsync();

                        if (member_user == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        member_user.first_name = bodyMemberUserAdmin.first_name;
                        member_user.last_name = bodyMemberUserAdmin.last_name;
                        member_user.email = bodyMemberUserAdmin.email;
                        member_user.tel = bodyMemberUserAdmin.tel;
                        member_user.update_date = now;
                        _context.Update(member_user);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "แก้ไขข้อมูลสำเร็จ",
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

        [HttpPost]
        [Route("admin/update_permission")]
        public async Task<IActionResult> UpdatePermissionAdmin([FromBody] BodyMemberUserPermissionAdmin bodyMemberUserPermissionAdmin)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var member_user_permission = await _context.member_user_permission
                        .Where(x => x.id == bodyMemberUserPermissionAdmin.id)
                        .FirstOrDefaultAsync();

                        if (member_user_permission == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        DateTime now = DateTime.Now;
                        member_user_permission.per_branch_view = (bodyMemberUserPermissionAdmin.per_branch_view) ? "Y" : "N";
                        member_user_permission.per_branch_manage = (bodyMemberUserPermissionAdmin.per_branch_manage) ? "Y" : "N";
                        member_user_permission.per_user_view = (bodyMemberUserPermissionAdmin.per_user_view) ? "Y" : "N";
                        member_user_permission.per_user_manage = (bodyMemberUserPermissionAdmin.per_user_manage) ? "Y" : "N";
                        member_user_permission.per_raw_view = (bodyMemberUserPermissionAdmin.per_raw_view) ? "Y" : "N";
                        member_user_permission.per_raw_manage = (bodyMemberUserPermissionAdmin.per_raw_manage) ? "Y" : "N";
                        member_user_permission.per_xml_view = (bodyMemberUserPermissionAdmin.per_xml_view) ? "Y" : "N";
                        member_user_permission.per_pdf_view = (bodyMemberUserPermissionAdmin.per_pdf_view) ? "Y" : "N";
                        member_user_permission.per_email_view = (bodyMemberUserPermissionAdmin.per_email_view) ? "Y" : "N";
                        member_user_permission.per_ebxml_view = (bodyMemberUserPermissionAdmin.per_ebxml_view) ? "Y" : "N";
                        member_user_permission.per_report_view = (bodyMemberUserPermissionAdmin.per_report_view) ? "Y" : "N";
                        member_user_permission.update_date = now;

                        _context.Update(member_user_permission);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "แก้ไขข้อมูลสำเร็จ",
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


        [HttpPost]
        [Route("admin/reset_password_mamber_user")]
        public async Task<IActionResult> ResetPasswordMemberUserAdmin([FromBody] BodyMemberUserAdmin bodyMemberUserAdmin)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                if (bodyMemberUserAdmin.password.Length < 8)
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสผ่านมากกว่า 8 ตัวอักษร", });

                if (bodyMemberUserAdmin.password != bodyMemberUserAdmin.confirm_password)
                    return StatusCode(400, new { message = "รหัสผ่านและยืนยันรหัสผ่านไม่ถูกต้อง", });

                var hasNumber = new Regex(@"[0-9]+");
                var hasUpperChar = new Regex(@"[A-Z]+");
                var hasMiniMaxChars = new Regex(@".{8,64}");
                var hasLowerChar = new Regex(@"[a-z]+");
                var hasSymbols = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");

                if (!hasLowerChar.IsMatch(bodyMemberUserAdmin.password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีภาษาอังกฤษตัวเล็กอย่างน้อย 1 ตัวอักษร", });
                }
                else if (!hasUpperChar.IsMatch(bodyMemberUserAdmin.password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีภาษาอังกฤษตัวใหญ่อย่างน้อย 1 ตัวอักษร", });
                }
                else if (!hasMiniMaxChars.IsMatch(bodyMemberUserAdmin.password))
                {
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสผ่านใหม่มากกว่า 8 ตัวอักษร", });
                }
                else if (!hasNumber.IsMatch(bodyMemberUserAdmin.password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีภาษาอังกฤษตัวเลขอย่างน้อย 1 ตัวอักษร", });
                }
                else if (!hasSymbols.IsMatch(bodyMemberUserAdmin.password))
                {
                    return StatusCode(400, new { message = "กรุณาตั้งรหัสผ่านให้มีสัญลักษณ์พิเศษอย่างน้อย 1 ตัวอักษร", });
                }

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        DateTime now = DateTime.Now;
                        var member_user = await _context.member_users
                        .Where(x => x.id == bodyMemberUserAdmin.id)
                        .FirstOrDefaultAsync();

                        if (member_user == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        member_user.password = Encryption.SHA256(bodyMemberUserAdmin.password);
                        member_user.update_date = now;
                        _context.Update(member_user);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "แก้ไขข้อมูลสำเร็จ",
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

        [HttpPost]
        [Route("admin/delete_mamber_user/{id}")]
        public async Task<IActionResult> DeleteMemberUserAdmin(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var member_user = await _context.member_users
                        .Where(x => x.id == id)
                        .FirstOrDefaultAsync();

                        if (member_user == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        DateTime now = DateTime.Now;
                        member_user.delete_status = 1;
                        member_user.update_date = now;

                        _context.Update(member_user);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "ลบข้อมูลสำเร็จ",
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
