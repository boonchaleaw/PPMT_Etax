
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Etax_Api.Controllers
{
    [ApiController]
    public class ReturnEbxmlController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private readonly IExceptionLogger _exceptionLogger;
        public ReturnEbxmlController(ApplicationDbContext context, IConfiguration config, IExceptionLogger exceptionLogger)
        {
            _config = config;
            _context = context;
            _context.Database.SetCommandTimeout(180);
            _exceptionLogger = exceptionLogger;
        }

        [HttpGet]
        [Route("lighthouse2/httpd/ebms/inbound")]

        public async Task<string> Get()
        {
            return "OK";
        }

        [HttpPost]
        [Route("lighthouse2/httpd/ebms/inbound")]
        public async Task<string> Post()
        {
            try
            {
                string content = await new StreamReader(Request.Body).ReadToEndAsync();

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(content);

                XmlNodeList xnList = doc.GetElementsByTagName("DocumentControl");
                foreach (XmlNode xn in xnList)
                {
                    string referenceNo = xn["ReferenceNo"].InnerText;
                    if (referenceNo != "")
                    {
                        var sendEbxml = _context.send_ebxml
                        .Where(x => x.conversation_id == referenceNo)
                        .FirstOrDefault();

                        if (sendEbxml != null)
                        {
                            xnList = doc.GetElementsByTagName("DocumentDetail");
                            foreach (XmlNode detail in xnList)
                            {
                                if (detail.FirstChild.Name == "Accept")
                                {
                                    xnList = doc.GetElementsByTagName("Accept");
                                    foreach (XmlNode accept in xnList)
                                    {
                                        string message = accept["Message"].InnerText;
                                        sendEbxml.etax_status = "success";
                                        sendEbxml.etax_status_finish = DateTime.Now;
                                    }
                                }
                                else
                                {
                                    xnList = doc.GetElementsByTagName("Reject");
                                    foreach (XmlNode reject in xnList)
                                    {
                                        string message = reject["Message"].InnerText;
                                        sendEbxml.etax_status = "fail";
                                        sendEbxml.error = message;
                                        sendEbxml.etax_status_finish = DateTime.Now;
                                    }
                                }
                            }
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                string data = "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\">";
                data += "<SOAP-ENV:Header/>";
                data += "<SOAP-ENV:Body>";
                data += "</SOAP-ENV:Body>";
                data += "</SOAP-ENV:Envelope>";

                return data;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
