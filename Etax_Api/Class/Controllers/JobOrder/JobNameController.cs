using Etax_Api.Class.Database;
using Etax_Api.Class.Model.JobOrderRequest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Etax_Api.Class.Controllers.JobOrder
{
    public class JobNameController : Controller
    {

        private IConfiguration _config;
        private ApplicationDbContext _context;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IJwtService _jwtService;
        public JobNameController(ApplicationDbContext context, IConfiguration config, IExceptionLogger exceptionLogger, IJwtService jwtService)
        {
            _config = config;
            _context = context;
            _context.Database.SetCommandTimeout(180);
            _exceptionLogger = exceptionLogger;
            _jwtService = jwtService;
        }
        [HttpPost("joborder/addjobname")]
        public async Task<IActionResult> AddJobName([FromBody] JobNameRequest request)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();

                if (string.IsNullOrEmpty(token))
                    return Unauthorized("Missing token");

                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = token.Substring("Bearer ".Length).Trim();

                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return Unauthorized(jwtStatus.message);

                var job = new JobName
                {
                    JobCode = request.JobCode,
                    ThaiName = request.ThaiName,
                    EngName = request.EngName,
                    MemberId = jwtStatus.member_id,
                };

                _context.job_name.Add(job);
                await _context.SaveChangesAsync();

                return StatusCode(200, new
                {
                    message = "Saved successfully",
                });


            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPost("joborder/jobnames")]
        public async Task<IActionResult> GetJobNames([FromBody] BodyDtParameters request)
        {
            try
            {
                var query = _context.job_name.AsQueryable(); // ใช้ DbSet จาก EF Core

                if (!string.IsNullOrEmpty(request.Search?.Value))
                {
                    string keyword = request.Search?.Value.ToLower();
                    query = query.Where(j =>
                        j.ThaiName.ToLower().Contains(keyword) ||
                        j.EngName.ToLower().Contains(keyword) ||
                        j.JobCode.ToLower().Contains(keyword));
                }

                var totalRecords = await _context.job_name.CountAsync();
                var filteredRecords = await query.CountAsync();

                var data = await query
                    .Skip(request.Start)
                    .Take(request.Length)
                     .Select(j => new Dictionary<string, object>
                {
                    { "Id",j.Id},
                    { "ThaiName", j.ThaiName },
                    { "EngName", j.EngName },
                    { "JobCode", j.JobCode }
                })
                                .ToListAsync();

                return Ok(new
                {
                    draw = request.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredRecords,
                    data = data
                });


            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load job names " + ex.Message });
            }
        }

        [HttpGet("joborder/jobnames")]
        public async Task<IActionResult> GetJobNames()
        {
            try
            {

                string token = Request.Headers[HeaderNames.Authorization].ToString();

                if (string.IsNullOrEmpty(token))
                    return Unauthorized("Missing token");

                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = token.Substring("Bearer ".Length).Trim();

                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return Unauthorized(jwtStatus.message);


                var jobNames = await _context.job_name.Where(p => p.MemberId == jwtStatus.member_id)
                .Select(j => new Dictionary<string, object>
             {
                    { "Id",j.Id},
                    { "ThaiName", j.ThaiName },
                    { "EngName", j.EngName },
                    { "JobCode", j.JobCode }
             })
                             .ToListAsync();



                return Ok(new
                {
                    jobNames = jobNames,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load job names " + ex.Message });
            }
        }

        [HttpGet("joborder/jobnames_permission")]
        public async Task<IActionResult> GetJobNamesByPermission()
        {
            try
            {

                string token = Request.Headers[HeaderNames.Authorization].ToString();

                if (string.IsNullOrEmpty(token))
                    return Unauthorized("Missing token");

                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = token.Substring("Bearer ".Length).Trim();

                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return Unauthorized(jwtStatus.message);




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

                return Ok(jobNamesSelect);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load job names " + ex.Message });
            }
        }
    }
}
