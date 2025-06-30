using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;
using Microsoft.Net.Http.Headers;
using System.Linq;

namespace Etax_Api.Class.Controllers.JobOrder
{
    public class JobPermissionController : Controller
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IJwtService _jwtService;
        public JobPermissionController(ApplicationDbContext context, IConfiguration config, IExceptionLogger exceptionLogger, IJwtService jwtService)
        {
            _config = config;
            _context = context;
            _context.Database.SetCommandTimeout(180);
            _exceptionLogger = exceptionLogger;
            _jwtService = jwtService;
        }
        [HttpGet("joborder/permissions")]
        public async Task<IActionResult> GetJobPermissions()
        {
            try
            {


                string token = Request.Headers[HeaderNames.Authorization].ToString();

                if (string.IsNullOrEmpty(token))
                    return Unauthorized("Missing token");

                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = token.Substring("Bearer ".Length).Trim();

                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                var result = _context.job_permission.Where(p => p.UserMemberId.Equals(jwtStatus.user_id))


                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error loading job permissions", error = ex.Message });
            }
        }

    }



}
