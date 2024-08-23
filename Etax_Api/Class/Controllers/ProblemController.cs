
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
    public class ProblemController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public ProblemController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        [HttpPost]
        [Route("get_member_problem_tabel")]
        public async Task<IActionResult> GetMemberProblemTabel([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

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

                var result = _context.view_member_problem.Where(x => x.member_id == jwtStatus.member_id).AsQueryable();

                if (!string.IsNullOrEmpty(searchBy))
                {
                    result = result.Where(r =>
                        r.id.ToString().ToUpper().Contains(searchBy.ToUpper()) ||
                        r.subject != null && r.subject.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.member_user_first_name != null && r.member_user_first_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.member_user_lase_name != null && r.member_user_lase_name.ToUpper().Contains(searchBy.ToUpper())
                    );
                }

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.CountAsync();
                var totalResultsCount = await _context.view_member_problem.Where(x => x.member_id == jwtStatus.member_id).CountAsync();

                var data = await result
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
        [Route("create_member_problem")]
        public async Task<IActionResult> CreateMemberProblem([FromBody] BodyMemberProblem bodyMemberProblem)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                if (string.IsNullOrEmpty(bodyMemberProblem.subject))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อปัญหา", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        MemberProblem memberProblem = new MemberProblem()
                        {
                            member_id = jwtStatus.member_id,
                            member_user_id = jwtStatus.user_id,
                            type = bodyMemberProblem.type,
                            subject = bodyMemberProblem.subject,
                            description = bodyMemberProblem.description,
                            priority = bodyMemberProblem.priority,
                            status = 1,
                            update_date = DateTime.Now,
                            create_date = DateTime.Now,
                        };
                        _context.Add(memberProblem);
                        await _context.SaveChangesAsync();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(400, new { message = ex.Message });
                    }
                }

                return StatusCode(200, new
                {
                    message = "เพิ่มการแจ้งปัญหาสำเร็จ",
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }


        //////////////////////////Admin//////////////////////////



    }
}
