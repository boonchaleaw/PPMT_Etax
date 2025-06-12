using Serilog;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;

namespace Etax_Api
{
    public static class ExceptionMiddleware
    {
        private static IConfiguration _config;
        public static void Init(IConfiguration configuration)
        {
            _config = configuration;
        }

        public static async Task LogError(ErrorLog errorLog, Exception exception)
        {
            DateTime now = DateTime.Now;
            string etax_id = errorLog.etax_id ?? "N/A"; // ใช้ค่า N/A ถ้า EtaxId เป็น null   

            string errorMessage = exception.InnerException != null ? exception.InnerException.Message : exception.Message;

            errorLog.error = errorMessage;
            errorLog.error_time = now;

            string errorId = $"<Msg-{Guid.NewGuid}-{now.ToString("yyyyMMddHHmmssffff")}>";
            errorLog.error_id = errorId; // สร้าง ErrorId ใหม่ทุกครั้งที่บันทึก

            string message = $"[Class: {errorLog.class_name}] {errorLog.method_name} - Exception: {errorMessage} : {etax_id}";



            // 1. เขียนลงไฟล์ผ่าน Serilog
           Serilog. Log.Error(exception, message);

            

            // 3. บันทึกลงฐานข้อมูลเอง (ไม่ผ่าน Serilog sink ก็ได้)
            await SaveErrorToDatabase(errorLog);
        }
        private static async Task SaveErrorToDatabase(ErrorLog errorLog)
        {
            try
            {
                //using (var context =  new ApplicationDbContext(_config)) // EF Core context
                //{
                //    await context.error_log.AddAsync(errorLog);
                //    await context.SaveChangesAsync();
                //}
            }
            catch (Exception innerEx)
            {
               Serilog.Log.Fatal(innerEx, "ExceptionMiddleware บันทึกลงฐานข้อมูลไม่สำเร็จ");
            }
        }
    }
}
