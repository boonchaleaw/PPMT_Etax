
using Etax_Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class DocumentTypeController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IJwtService _jwtService;
        public DocumentTypeController(ApplicationDbContext context, IConfiguration config, IExceptionLogger exceptionLogger, IJwtService jwtService)
        {
            _config = config;
            _context = context;
            _context.Database.SetCommandTimeout(180);
            _exceptionLogger = exceptionLogger;
            _jwtService = jwtService;
        }

        [HttpPost]
        [Route("get_document_type")]
        public async Task<IActionResult> GetDocumentType()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                object document_types = await _context.view_member_document_type
                .Select(x => new
                {
                    x.member_id,
                    x.document_type_id,
                    x.document_type_name,
                    x.service_type_id,
                })
                .Where(x =>
                (x.member_id == jwtStatus.member_id && x.service_type_id == 1) ||
                (x.member_id == jwtStatus.member_id && x.service_type_id == 3))
                .ToListAsync();


                if (document_types != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            document_types = document_types,
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
        [Route("get_document_type_outsource")]
        public async Task<IActionResult> GetDocumentType_Outsource()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                object document_types = await _context.view_member_document_type
                .Select(x => new
                {
                    x.member_id,
                    x.document_type_id,
                    x.document_type_name,
                    x.service_type_id,
                })
                .Where(x =>
                (x.member_id == jwtStatus.member_id && x.service_type_id == 2) ||
                (x.member_id == jwtStatus.member_id && x.service_type_id == 3))
                .ToListAsync();


                if (document_types != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            document_types = document_types,
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


        //////////////////////////Admin//////////////////////////

        [HttpPost]
        [Route("admin/get_document_type")]
        public async Task<IActionResult> GetDocumentTypeAdmin([FromBody] BodyDocumentType bodyDocumentType)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                if (bodyDocumentType.listMemberId == null)
                {
                    var document_type = await (from dt in _context.document_type
                                               select dt).ToListAsync();

                    if (document_type != null)
                    {
                        return StatusCode(200, new
                        {
                            message = "เรียกดูข้อมูลสำเร็จ",
                            data = document_type,
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
                else
                {

                    List<int> listDocumentId = await (from mdt in _context.member_document_type
                                                      where bodyDocumentType.listMemberId.Contains(mdt.member_id)
                                                      group new { mdt } by mdt.document_type_id into mdt_g
                                                      select mdt_g.Key).ToListAsync();

                    var document_type = await (from dt in _context.document_type
                                               where listDocumentId.Contains(dt.id)
                                               select dt).ToListAsync();

                    if (document_type != null)
                    {
                        return StatusCode(200, new
                        {
                            message = "เรียกดูข้อมูลสำเร็จ",
                            data = document_type,
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
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("admin/get_group_name")]
        public async Task<IActionResult> GetGroupNameAdmin([FromBody] BodyDocumentType bodyDocumentType)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                if (bodyDocumentType.listMemberId == null)
                {
                    var file_group = await (from vtp in _context.view_file_group
                                            group new { vtp } by vtp.group_name into g
                                            select new
                                            {
                                                group_name = g.Key,
                                            }).ToListAsync();

                    if (file_group != null)
                    {
                        return StatusCode(200, new
                        {
                            message = "เรียกดูข้อมูลสำเร็จ",
                            data = file_group,
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
                else
                {

                    var file_group = await (from vtp in _context.view_file_group
                                            where bodyDocumentType.listMemberId.Contains(vtp.member_id)
                                            group new { vtp } by vtp.group_name into g
                                            select new
                                            {
                                                group_name = g.Key,
                                            }).ToListAsync();

                    if (file_group != null)
                    {
                        return StatusCode(200, new
                        {
                            message = "เรียกดูข้อมูลสำเร็จ",
                            data = file_group,
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
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

    }
}
