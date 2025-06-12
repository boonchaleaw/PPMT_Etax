
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class OtherReportController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IJwtService _jwtService;
        public OtherReportController(ApplicationDbContext context, IConfiguration config, IExceptionLogger exceptionLogger, IJwtService jwtService)
        {
            _config = config;
            _context = context;
            _context.Database.SetCommandTimeout(180);
            _exceptionLogger = exceptionLogger;
            _jwtService = jwtService;
        }

        [HttpPost]
        [Route("get_other_report_tabel")]
        public async Task<IActionResult> GetOtherReportTabel([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var searchBy = bodyDtParameters.searchText;
                var orderCriteria = "id";
                var orderAscendingDirection = true;

                if (bodyDtParameters.Order != null)
                {
                    orderCriteria = bodyDtParameters.Columns[bodyDtParameters.Order[0].Column].Data;
                    orderAscendingDirection = bodyDtParameters.Order[0].Dir.ToString().ToLower() == "asc";
                }

                var result = _context.other_reports.Where(x => x.member_id == jwtStatus.member_id).AsQueryable();

                bodyDtParameters.dateStart = DateTime.Parse(bodyDtParameters.dateStart.ToString()).Date;
                bodyDtParameters.dateEnd = bodyDtParameters.dateEnd.AddDays(+1).AddMilliseconds(-1);

                if (!string.IsNullOrEmpty(searchBy))
                {
                    result = result.Where(r =>
                        (r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                        (r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                        (r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                    );
                }
                else
                {
                    result = result.Where(r =>
                        (r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                    );
                }

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.CountAsync();
                var totalResultsCount = await _context.other_reports.Where(x => x.member_id == jwtStatus.member_id).CountAsync();

                if (bodyDtParameters.Length == -1)
                {
                    var data = await result
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
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }
    }
}
