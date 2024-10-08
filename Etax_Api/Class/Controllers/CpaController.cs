
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
    public class CpaController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public CpaController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        //////////////////////////Admin//////////////////////////

        [HttpPost]
        [Route("admin/get_cpa_tabel")]
        public async Task<IActionResult> GetCpaTabelAdmin([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var searchBy = bodyDtParameters.Search?.Value;
                var orderCriteria = "id";
                var orderAscendingDirection = true;

                if (bodyDtParameters.Order != null)
                {
                    orderCriteria = bodyDtParameters.Columns[bodyDtParameters.Order[0].Column].Data;
                    orderAscendingDirection = bodyDtParameters.Order[0].Dir.ToString().ToLower() == "asc";
                }

                var table = _context.cpa.Where(x => x.delete_status == 0).AsQueryable();

                if (bodyDtParameters.id > 0)
                    table = table.Where(x => x.member_id == bodyDtParameters.id);

                if (!string.IsNullOrEmpty(searchBy))
                {
                    table = table.Where(r =>
                        r.id.ToString().ToUpper().Contains(searchBy.ToUpper()) ||
                        r.cpa_id != null && r.cpa_id.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.rd_uri != null && r.rd_uri.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.conversation_id != null && r.conversation_id.ToUpper().Contains(searchBy.ToUpper())
                    );
                }

                table = orderAscendingDirection ? table.OrderByProperty(orderCriteria) : table.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await table.CountAsync();

                var tableCount = _context.cpa.Where(x => x.delete_status == 0).AsQueryable();

                if (bodyDtParameters.id > 0)
                    tableCount = tableCount.Where(x => x.member_id == bodyDtParameters.id);

                var totalResultsCount = await tableCount.CountAsync();

                if (bodyDtParameters.Length == -1)
                {
                    var data = await table
                    .Select(x => new
                    {
                        x.id,
                        x.cpa_id,
                        x.rd_uri,
                        x.conversation_id,
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
                        x.cpa_id,
                        x.rd_uri,
                        x.conversation_id,
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
        [Route("admin/get_cpa_detail/{id}")]
        public async Task<IActionResult> GetCpaDetail(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var cpa = await _context.cpa
                .Where(x => x.id == id)
                .Select(x => new
                {
                    id = x.id,
                    cpa_id = x.cpa_id,
                    rd_uri = x.rd_uri,
                    conversation_id = x.conversation_id,
                })
                .FirstOrDefaultAsync();

                if (cpa != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = cpa,
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
        [Route("admin/add_cpa")]
        public async Task<IActionResult> AddCpa([FromBody] BodyCpa bodyCpa)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                if (String.IsNullOrEmpty(bodyCpa.cpa_id))
                    return StatusCode(400, new { message = "กรุณากำหนด CPA ID", });

                if (String.IsNullOrEmpty(bodyCpa.rd_uri))
                    return StatusCode(400, new { message = "กรุณากำหนด RD URI", });

                if (String.IsNullOrEmpty(bodyCpa.conversation_id))
                    return StatusCode(400, new { message = "กรุณากำหนด Conversation ID", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        DateTime now = DateTime.Now;
                        Cpa cpa = new Cpa()
                        {
                            member_id = bodyCpa.member_id,
                            cpa_id = bodyCpa.cpa_id,
                            rd_uri = bodyCpa.rd_uri,
                            conversation_id = bodyCpa.conversation_id,
                            update_date = now,
                            create_date = now,
                        };
                        _context.Add(cpa);
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
        [Route("admin/update_cpa")]
        public async Task<IActionResult> UpdateCpa([FromBody] BodyCpa bodyCpa)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                if (String.IsNullOrEmpty(bodyCpa.cpa_id))
                    return StatusCode(400, new { message = "กรุณากำหนด CPA ID", });

                if (String.IsNullOrEmpty(bodyCpa.rd_uri))
                    return StatusCode(400, new { message = "กรุณากำหนด RD URI", });

                if (String.IsNullOrEmpty(bodyCpa.conversation_id))
                    return StatusCode(400, new { message = "กรุณากำหนด Conversation ID", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var cpa = await _context.cpa
                        .Where(x => x.id == bodyCpa.id)
                        .FirstOrDefaultAsync();

                        if (cpa == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        DateTime now = DateTime.Now;
                        cpa.cpa_id = bodyCpa.cpa_id;
                        cpa.rd_uri = bodyCpa.rd_uri;
                        cpa.conversation_id = bodyCpa.conversation_id;
                        cpa.update_date = now;

                        _context.Update(cpa);
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
        [Route("admin/delete_cpa/{id}")]
        public async Task<IActionResult> DeleteCpa(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var cpa = await _context.cpa
                        .Where(x => x.id == id)
                        .FirstOrDefaultAsync();

                        if (cpa == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        DateTime now = DateTime.Now;
                        cpa.delete_status = 1;
                        cpa.update_date = now;

                        _context.Update(cpa);
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
