using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Internal;

namespace Etax_Api
{
    public interface IExceptionLogger
    {
        Task LogErrorAsync(ErrorLog errorLog, Exception exception);
    }

    public class ExceptionLogger : IExceptionLogger
    {
        private readonly IDbContextFactory<ApplicationDbContext> _factory;
        private readonly ILogger<ExceptionLogger> _logger;
        public ExceptionLogger(IDbContextFactory<ApplicationDbContext> factory, ILogger<ExceptionLogger> logger)
        {
              _factory = factory;
            _logger = logger;
        }
        public async Task LogErrorAsync(ErrorLog errorLog, Exception exception)
        {
            DateTime now = DateTime.Now;
            string etax_id = errorLog.etax_id ?? "N/A"; // ใช้ค่า N/A ถ้า EtaxId เป็น null   

            string errorMessage = exception.InnerException != null ? exception.InnerException.Message : exception.Message;

            errorLog.error = errorMessage;
            errorLog.error_time = now;

            string errorId = $"<Msg-{Guid.NewGuid}-{now.ToString("yyyyMMddHHmmssffff")}>";
            if (string.IsNullOrEmpty(errorLog.error_id))
                errorLog.error_id = errorId; // ถ้าไม่มี ErrorId ให้ใช้ ErrorId ใหม่
           
            string message = $"[Class: {errorLog.class_name}] {errorLog.method_name} - Exception: {errorMessage} : {etax_id}";



            // 1. เขียนลงไฟล์ผ่าน Serilog
            Serilog.Log.Error(exception, message);



            // 3. บันทึกลงฐานข้อมูลเอง (ไม่ผ่าน Serilog sink ก็ได้)
            try
            {
                using var context = _factory.CreateDbContext();
                await context.error_log.AddAsync(errorLog);
                await context.SaveChangesAsync();
            }
            catch (Exception innerEx)
            {
                Serilog.Log.Fatal(innerEx, "ExceptionMiddleware บันทึกลงฐานข้อมูลไม่สำเร็จ");
            }
        }
       
    }
}
