
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
using System.Threading.Tasks;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class CerController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IJwtService _jwtService;
        public CerController(ApplicationDbContext context, IConfiguration config, IExceptionLogger exceptionLogger, IJwtService jwtService)
        {
            _config = config;
            _context = context;
            _context.Database.SetCommandTimeout(180);
            _exceptionLogger = exceptionLogger;
            _jwtService = jwtService;
        }

        //////////////////////////Admin//////////////////////////

        [HttpPost]
        [Route("admin/get_cer_tabel")]
        public async Task<IActionResult> GetCerTabelAdmin([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_user_detail).FirstOrDefaultAsync();

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

                var table = _context.member_token.Where(x => x.delete_status == 0).AsQueryable();

                if (bodyDtParameters.id > 0)
                    table = table.Where(x => x.member_id == bodyDtParameters.id);

                if (!string.IsNullOrEmpty(searchBy))
                {
                    table = table.Where(r =>
                        r.id.ToString().ToUpper().Contains(searchBy.ToUpper()) ||
                        r.token_label != null && r.token_label.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.cert_label != null && r.cert_label.ToUpper().Contains(searchBy.ToUpper())
                    );
                }

                table = orderAscendingDirection ? table.OrderByProperty(orderCriteria) : table.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await table.CountAsync();

                var tableCount = _context.member_token.Where(x => x.delete_status == 0).AsQueryable();

                if (bodyDtParameters.id > 0)
                    tableCount = tableCount.Where(x => x.member_id == bodyDtParameters.id);

                var totalResultsCount = await tableCount.CountAsync();

                if (bodyDtParameters.Length == -1)
                {
                    var data = await table
                    .Select(x => new
                    {
                        x.id,
                        x.library_path,
                        x.slot,
                        x.token_label,
                        x.cert_label,
                        expire_date = (x.expire_date == null) ? "" : ((DateTime)x.expire_date).ToString("dd/MM/yyyy"),
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
                        x.library_path,
                        x.slot,
                        x.token_label,
                        x.cert_label,
                        expire_date = (x.expire_date == null) ? "" : ((DateTime)x.expire_date).ToString("dd/MM/yyyy"),
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
        [Route("admin/get_cer_detail/{id}")]
        public async Task<IActionResult> GetCerDetail(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_user_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                var user = await _context.member_token
                .Where(x => x.id == id)
                .Select(x => new
                {
                    id = x.id,
                    library_path = x.library_path,
                    slot = x.slot,
                    token_label = x.token_label,
                    cert_label = x.cert_label,
                    pin = "******",
                })
                .FirstOrDefaultAsync();

                if (user != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = user,
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
        [Route("admin/add_cer")]
        public async Task<IActionResult> AddCer([FromBody] BodyCer bodyCer)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_user_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                if (String.IsNullOrEmpty(bodyCer.library_path))
                    return StatusCode(400, new { message = "กรุณากำหนด Library Path", });

                if (String.IsNullOrEmpty(bodyCer.token_label))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อ Token", });

                if (String.IsNullOrEmpty(bodyCer.cert_label))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อใบรับรอง", });

                if (String.IsNullOrEmpty(bodyCer.pin))
                    return StatusCode(400, new { message = "กรุณากำหนด Pin", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        DateTime now = DateTime.Now;
                        MemberToken member_token = new MemberToken()
                        {
                            member_id = bodyCer.member_id,
                            library_path = bodyCer.library_path,
                            slot = bodyCer.slot,
                            token_label = bodyCer.token_label,
                            cert_label = bodyCer.cert_label,
                            pin = Encryption.Decrypt_AES256(bodyCer.pin),
                            update_date = now,
                            create_date = now,
                        };
                        _context.Add(member_token);
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
        [Route("admin/update_cer")]
        public async Task<IActionResult> UpdateCer([FromBody] BodyCer bodyCer)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_user_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });



                if (String.IsNullOrEmpty(bodyCer.library_path))
                    return StatusCode(400, new { message = "กรุณากำหนด Library Path", });

                if (String.IsNullOrEmpty(bodyCer.token_label))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อ Token", });

                if (String.IsNullOrEmpty(bodyCer.cert_label))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อใบรับรอง", });

                if (String.IsNullOrEmpty(bodyCer.pin))
                    return StatusCode(400, new { message = "กรุณากำหนด Pin", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var member_token = await _context.member_token
                        .Where(x => x.id == bodyCer.id)
                        .FirstOrDefaultAsync();

                        if (member_token == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        DateTime now = DateTime.Now;
                        member_token.library_path = bodyCer.library_path;
                        member_token.slot = bodyCer.slot;
                        member_token.token_label = bodyCer.token_label;
                        member_token.cert_label = bodyCer.cert_label;

                        if(bodyCer.pin != "" && bodyCer.pin != "******")
                        member_token.pin = Encryption.Encrypt_AES256(bodyCer.pin);

                        member_token.update_date = now;

                        _context.Update(member_token);
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
        [Route("admin/delete_cer/{id}")]
        public async Task<IActionResult> DeleteCer(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_user_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });



                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var member_token = await _context.member_token
                        .Where(x => x.id == id)
                        .FirstOrDefaultAsync();

                        if (member_token == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        DateTime now = DateTime.Now;
                        member_token.delete_status = 1;
                        member_token.update_date = now;

                        _context.Update(member_token);
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
