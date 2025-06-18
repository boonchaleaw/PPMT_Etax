using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System.Threading.Tasks;
using System;
using Etax_Api.Class.Database;
using System.Linq;
using Etax_Api.Class.Model.JobOrderRequest;
using System.Collections.Generic;
using System.IO;

namespace Etax_Api.Class.Controllers.JobOrder
{
    public class JobOrderController : Controller
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IJwtService _jwtService;
        public JobOrderController(ApplicationDbContext context, IConfiguration config, IExceptionLogger exceptionLogger, IJwtService jwtService)
        {
            _config = config;
            _context = context;
            _context.Database.SetCommandTimeout(180);
            _exceptionLogger = exceptionLogger;
            _jwtService = jwtService;
        }
        [HttpGet("prepare_joborder")]
        public async Task<IActionResult> PrepareJobNo()
        {
            try
            {

                // ✅ ตรวจสอบ JWT
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                if (string.IsNullOrEmpty(token))
                    return Unauthorized("Missing token.");

                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = token.Substring("Bearer ".Length).Trim();

                var jwtStatus = _jwtService.ValidateJwtTokenMember(token);
                if (!jwtStatus.status)
                    return Unauthorized(jwtStatus.message);

                var jobNo = await GenerateJobNoAtomicAsync(); // ใช้เมธอดเดิมที่ปลอดภัย
                return StatusCode(200, new
                {
                    message = "Prepare successfully",
                    jobNo
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        private async Task<string> GenerateJobNoAtomicAsync()
        {
            string prefix = "JO";
            string yearMonth = DateTime.Now.ToString("yyyyMM");
            int nextNumber;

            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var running = await _context.job_no_running
                    .Where(r => r.YearMonth == yearMonth)
                    .FirstOrDefaultAsync();

                if (running == null)
                {
                    running = new JobNoRunning
                    {
                        YearMonth = yearMonth,
                        LastNumber = 1
                    };
                    _context.job_no_running.Add(running);
                    nextNumber = 1;
                }
                else
                {
                    running.LastNumber += 1;
                    nextNumber = running.LastNumber;
                    _context.job_no_running.Update(running);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                string runningText = nextNumber.ToString().PadLeft(6, '0');
                return $"{prefix}{yearMonth}{runningText}";
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        [HttpPost("add_joborder")]
        public async Task<IActionResult> AddJobOrder([FromBody] JobOrderRequest request)
        {
            try
            {
                // ✅ ตรวจสอบ JWT
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                if (string.IsNullOrEmpty(token))
                    return Unauthorized("Missing token.");

                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = token.Substring("Bearer ".Length).Trim();

                var jwtStatus = _jwtService.ValidateJwtTokenMember(token);
                if (!jwtStatus.status)
                    return Unauthorized(jwtStatus.message);


                string folderPath = Path.Combine(_config["Path:FilesJob"], request.JobNo);


                if (request.FilesToDelete != null && request.FilesToDelete.Any())
                {
                    foreach (var fileName in request.FilesToDelete)
                    {
                        var fullPath = Path.Combine(folderPath, fileName);
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                        }
                    }
                }


                using var transaction = await _context.Database.BeginTransactionAsync();


                MemberUser user = await _context.member_users
                    .Where(u => u.id == jwtStatus.user_id)
                    .FirstOrDefaultAsync();

                string policy = "Customer"; // กำหนด policy เป็น Customer ตามที่ระบุในคำถาม

                if (user.member_id.Equals(1))
                    policy = "PPMT";

                try
                {
                    if (string.IsNullOrEmpty(request.JobNo))
                        return BadRequest("JobNo is required.");

                    folderPath = Path.Combine(_config["Path:FilesJob"], request.JobNo);

                    var fileInfos = Directory.Exists(folderPath)
                        ? Directory.GetFiles(folderPath).Select(path => new FileInfo(path)).ToList()
                        : new List<FileInfo>();

                    // ✅ เพิ่ม job_order
                    var order = new Database.JobOrder
                    {
                        JobNo = request.JobNo,
                        JobName = request.JobName,
                        WorkOrder = request.WorkOrder,
                        Cycle = request.Cycle,
                        Total = request.Total,
                        TotaPost = request.TotaPost,
                        TotalEmail = request.TotalEmail,
                        TotalByHand = request.TotalByHand,
                        DateCreate = DateTime.Now,
                        PathReportLocal = Directory.Exists(folderPath) ? folderPath : null
                    };


                    _context.job_order.Add(order);
                    await _context.SaveChangesAsync(); // เพื่อให้ order.Id พร้อมใช้

                    // ✅ เพิ่ม job_files (หลายรายการ)
                    foreach (var file in fileInfos)
                    {
                        var jobFile = new JobFile
                        {
                            FileName = file.Name,
                            JobId = order.Id,
                            UploadName = user.first_name + " " + user.last_name, // หรือใช้ชื่อ user ปัจจุบัน
                            Status = "Active",
                            Policy = policy,
                            DateModify = file.LastWriteTime,
                            Size = file.Length,
                            LocalPath = folderPath
                        };

                        _context.job_files.Add(jobFile);
                    }

                    await _context.SaveChangesAsync();


                    var activity = new JobActivity
                    {
                        activity = "CREATE",
                        detail = $"สร้าง Job ใหม่: {order.JobNo} โดยผู้ใช้ {user.username}",
                        name = user.username,
                        jobid = order.Id,
                        memberid = user.id,
                        date = DateTime.Now
                    };

                    _context.job_activities.Add(activity);
                    await _context.SaveChangesAsync();

                    // ✅ commit ทั้งชุด
                    await transaction.CommitAsync();

                    return Ok("Save Success");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _exceptionLogger.LogErrorAsync(new ErrorLog
                    {
                        class_name = nameof(JobOrderController),
                        method_name = nameof(AddJobOrder),
                        etax_id = request.JobNo,
                        error_id = $"<Msg-{Guid.NewGuid()}-{DateTime.Now:yyyyMMddHHmmssffff}>"
                    }, ex);
                    return StatusCode(500, $"Error while adding job order: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                await _exceptionLogger.LogErrorAsync(new ErrorLog
                {
                    class_name = nameof(JobOrderController),
                    method_name = nameof(AddJobOrder),
                    etax_id = request.JobNo,
                    error_id = $"<Msg-{Guid.NewGuid()}-{DateTime.Now:yyyyMMddHHmmssffff}>"
                }, ex);
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }
        [HttpPut("update_joborder")]
        public async Task<IActionResult> UpdateJobOrder([FromBody] JobOrderRequest request)
        {

            // ✅ ตรวจสอบ JWT
            string token = Request.Headers[HeaderNames.Authorization].ToString();
            if (string.IsNullOrEmpty(token))
                return Unauthorized("Missing token.");

            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token.Substring("Bearer ".Length).Trim();

            var jwtStatus = _jwtService.ValidateJwtTokenMember(token);
            if (!jwtStatus.status)
                return Unauthorized(jwtStatus.message);

            if (string.IsNullOrWhiteSpace(request.RevisedReason))
            {
                return BadRequest("RevisedReason is required when updating an existing job.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (string.IsNullOrEmpty(request.JobNo))
                    return BadRequest("JobNo is required.");

                var job = await _context.job_order.FirstOrDefaultAsync(j => j.JobNo == request.JobNo);
                if (job == null)
                    return NotFound("Job not found.");


                MemberUser user = await _context.member_users
               .Where(u => u.id == jwtStatus.user_id)
               .FirstOrDefaultAsync();

                string policy = "Customer";
                string staus = "Send"; // ค่าเริ่มต้น

                if (user.member_id.Equals(1))
                {
                    policy = "PPMT";
                    staus = "Waiting for approve";
                }

                // ✅ อัปเดต field
                job.JobName = request.JobName;
                job.WorkOrder = request.WorkOrder;
                job.Cycle = request.Cycle;
                job.Total = request.Total;
                job.TotaPost = request.TotaPost;
                job.TotalEmail = request.TotalEmail;
                job.TotalByHand = request.TotalByHand;
                job.RevisedReason = request.RevisedReason;
                job.Status = staus;

                _context.job_order.Update(job);
                await _context.SaveChangesAsync();


                // ✅ อ่านไฟล์ใหม่จากโฟลเดอร์
                string folderPath = job.PathReportLocal ?? Path.Combine(_config["Path:FilesJob"], job.JobNo);

                // ✅ ลบไฟล์ที่ถูกเลือกลบจากโฟลเดอร์และฐานข้อมูล
                if (request.FilesToDelete != null && request.FilesToDelete.Any())
                {
                    foreach (var fileName in request.FilesToDelete)
                    {
                        var fullPath = Path.Combine(folderPath, fileName);
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                        }
                    }
                }

                var dbFilesToDelete = await _context.job_files
    .Where(f => f.JobId == job.Id && request.FilesToDelete.Contains(f.FileName))
    .ToListAsync();

                _context.job_files.RemoveRange(dbFilesToDelete);
                await _context.SaveChangesAsync();





                // ✅ เพิ่มไฟล์ใหม่  

                var existingFiles = await _context.job_files
                                           .Where(f => f.JobId == job.Id)
                                           .Select(f => f.FileName)
                                           .ToListAsync();




                var fileInfos = Directory.Exists(folderPath)
                    ? Directory.GetFiles(folderPath).Select(p => new FileInfo(p)).ToList()
                    : new List<FileInfo>();

                foreach (var file in fileInfos)
                {
                    if (!existingFiles.Contains(file.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        _context.job_files.Add(new JobFile
                        {
                            FileName = file.Name,
                            JobId = job.Id,
                            UploadName = user.first_name + " " + user.last_name,
                            Status = "Active",
                            Policy = policy,
                            DateModify = file.LastWriteTime,
                            Size = file.Length,
                            LocalPath = folderPath
                        });
                    }
                }

                await _context.SaveChangesAsync();

                // ✅ เพิ่ม activity log
                _context.job_activities.Add(new JobActivity
                {
                    activity = "UPDATE",
                    detail = $"แก้ไข Job {job.JobNo}",
                    name = user.first_name + " " + user.last_name,
                    jobid = job.Id,
                    memberid = user.id,
                    date = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, jobNo = job.JobNo, id = job.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Update failed: {ex.Message}");
            }
        }
        [HttpPut("approve_joborder")]
        public async Task<IActionResult> ApproveJob([FromQuery] string jobNo)
        {
            try
            {

                string token = Request.Headers[HeaderNames.Authorization].ToString();
                if (string.IsNullOrEmpty(token))
                    return Unauthorized("Missing token.");

                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = token.Substring("Bearer ".Length).Trim();

                var jwtStatus = _jwtService.ValidateJwtTokenMember(token);
                if (!jwtStatus.status)
                    return Unauthorized(jwtStatus.message);

                var job = await _context.job_order.FirstOrDefaultAsync(j => j.JobNo == jobNo);
                if (job == null)
                    return NotFound("Job not found.");


                MemberUser user = await _context.member_users
               .Where(u => u.id == jwtStatus.user_id)
               .FirstOrDefaultAsync();


                job.Status = "Approved";

                _context.job_activities.Add(new JobActivity
                {
                    activity = "APPROVE",
                    detail = $"Job {job.JobNo} approved",
                    name = user.first_name + " " + user.last_name,
                    jobid = job.Id,
                    memberid = user.id,
                    date = DateTime.Now
                });

                await _context.SaveChangesAsync();
                return Ok("Job approved.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error approving job " + ex.Message });
            }
        }


    }
}
