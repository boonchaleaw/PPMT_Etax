
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
    public class DocumentTypeController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public DocumentTypeController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        [HttpPost]
        [Route("get_document_type")]
        public async Task<IActionResult> GetDocumentType()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

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
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

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
                (x.member_id == jwtStatus.member_id && x.service_type_id == 2)||
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
        public async Task<IActionResult> GetDocumentTypeAdmin()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var document_type = await _context.document_type
                .ToListAsync();

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
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

    }
}
