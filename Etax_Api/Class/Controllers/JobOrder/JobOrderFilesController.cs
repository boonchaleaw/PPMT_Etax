using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.Net.Http.Headers;

namespace Etax_Api.Class.Controllers.JobOrder
{
    public class JobOrderFilesController : Controller
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IJwtService _jwtService;
        public JobOrderFilesController(ApplicationDbContext context, IConfiguration config, IExceptionLogger exceptionLogger, IJwtService jwtService)
        {
            _config = config;
            _context = context;
            _context.Database.SetCommandTimeout(180);
            _exceptionLogger = exceptionLogger;
            _jwtService = jwtService;

        }
        [HttpPost("joborder/upload_temp_file")]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> UploadTempFile([FromForm] string jobNo, [FromForm] List<IFormFile> files)
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

                if (string.IsNullOrWhiteSpace(jobNo))
                    return BadRequest("Missing jobNo.");

                if (files == null || files.Count == 0)
                    return BadRequest("No files uploaded.");

                // ✅ สร้างโฟลเดอร์ถ้ายังไม่มี
                string baseFolder = Path.Combine(_config["Path:FilesJob"], jobNo);
                Directory.CreateDirectory(baseFolder);

                var uploaded = new List<string>();

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        string fileName = Path.GetFileName(file.FileName);
                        string filePath = Path.Combine(baseFolder, fileName);

                        // ✅ บันทึกไฟล์
                        using var stream = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        uploaded.Add(fileName);
                    }
                }


                return Ok(new { message = "Upload file successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Upload error: {ex.Message}");
            }
        }

        [HttpDelete("joborder/delete_file")]
        public IActionResult DeleteJobFile([FromQuery] string jobNo, [FromQuery] string fileName)
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


                if (string.IsNullOrEmpty(jobNo) || string.IsNullOrEmpty(fileName))
                    return BadRequest("Missing jobNo or fileName");

                string jobFolder = Path.Combine(_config["Path:FilesJob"], jobNo);
                string filePath = Path.Combine(jobFolder, fileName);

                if (!System.IO.File.Exists(filePath))
                    return NotFound("File not found");

                System.IO.File.Delete(filePath);

                return StatusCode(200, new { message = "Deleted file successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpGet("joborder/files/{jobNo}")]
        public IActionResult GetUploadedFiles(string jobNo)
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

                if (string.IsNullOrWhiteSpace(jobNo))
                    return BadRequest("Missing jobNo");

                string folderPath = Path.Combine(_config["Path:FilesJob"], jobNo);

                if (!Directory.Exists(folderPath))
                    return NotFound("Folder not found");

                var files = Directory.GetFiles(folderPath)
     .Select(filePath => new {
         FileName = Path.GetFileName(filePath),
         Size = new FileInfo(filePath).Length,
         Modified = System.IO.File.GetLastWriteTime(filePath)
     })
     .ToList();

                return Ok(files);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
            
        }

    }
}
