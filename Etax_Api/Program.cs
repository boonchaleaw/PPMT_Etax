using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Etax_Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Serilog.Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Debug()
           .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
           .CreateLogger();

            try
            {
                Serilog.Log.Information("Starting host...");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Serilog.Log.Fatal(ex, "Host terminated unexpectedly.");
            }
            finally
            {
                Serilog.Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog() // Use Serilog for logging
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<WorkerService>();
                });
    }
}
