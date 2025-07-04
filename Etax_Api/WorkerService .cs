using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Etax_Api
{
    public class WorkerService : BackgroundService
    {
        private const int generalDelay = 5000;
        private IConfiguration _config;
        private ApplicationDbContext _context;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //var builder = new ConfigurationBuilder()
            //.AddJsonFile($"appsettings.json");
            //_config = builder.Build();

            //_context = new ApplicationDbContext(_config);
            //_context.Database.SetCommandTimeout(180);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(generalDelay, stoppingToken);
                //await ShareFile();
            }
        }

        private Task ShareFile()
        {
            try
            {
                DateTime dateCheck = DateTime.Now.AddDays(-1);
                List<EtaxFile> listEtaxFile = _context.etax_files
                .Where(x => x.share_path != "" && x.share_path != null && x.create_date > dateCheck)
                .ToList();

                foreach (EtaxFile etax in listEtaxFile)
                {
                    string xmlPath = etax.share_path + "/xml/" + etax.name + ".xml";
                    string pdfPath = etax.share_path + "/pdf/" + etax.name + ".pdf";

                    if (!File.Exists(_config["Path:Share"] + xmlPath))
                    {
                        string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etax.url_path + "/xml/" + etax.name + ".xml", _config["Path:Mode"]);
                        if (fileBase64 != "")
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(_config["Path:Share"] + xmlPath));
                            File.WriteAllBytes(_config["Path:Share"] + xmlPath, Convert.FromBase64String(fileBase64));
                        }
                    }

                    if (!File.Exists(_config["Path:Share"] + pdfPath))
                    {
                        string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etax.url_path + "/pdf/" + etax.name + ".pdf", _config["Path:Mode"]);
                        if (fileBase64 != "")
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(_config["Path:Share"] + pdfPath));
                            File.WriteAllBytes(_config["Path:Share"] + pdfPath, Convert.FromBase64String(fileBase64));
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return Task.FromResult("Done");
        }
    }
}
