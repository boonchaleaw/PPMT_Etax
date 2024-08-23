
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class ApiSftpController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public ApiSftpController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        [HttpPost]
        [Route("ppmt_create_etax_file_sftp_benz")]
        public async Task<IActionResult> ApiCreateEtaxFileSftpBenz([FromForm] BodyApiCreateEtaxFileMwa bodyApiCreateEtaxFile)
        {
            try
            {

                if (bodyApiCreateEtaxFile.APIKey != "l60leBwJDveYgG5Ar67HXLgUhJL6ANS9G756Cccz0c0fvIAWmNRSgQyb4o4NjhBl640ivaONHQGHr8Ad5p4eiyE0Xob6gp5LHVvm2OVdUpIlYHV5hKqpJSeen6Bg4ARA")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E001",
                        errorMessage = "APIKey ไม่ถูกต้อง",
                    });

                if (bodyApiCreateEtaxFile.UserCode != "benz" || bodyApiCreateEtaxFile.AccessKey != "P@ssw0rd")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E002",
                        errorMessage = "ชื่อผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง",
                    });

                if (bodyApiCreateEtaxFile.SellerTaxId != "0105539109901")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E003",
                        errorMessage = "ไม่พบ Tax ID ในระบบ",
                    });

                if (bodyApiCreateEtaxFile.SellerBranchId != "00000")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E004",
                        errorMessage = "ไม่พบสาขาที่ต้องการ",
                    });

                if (bodyApiCreateEtaxFile.TextContent == null)
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E005",
                        errorMessage = "ไม่พบไฟล์ข้อมูล",
                    });


                DateTime now = DateTime.Now;
                int member_id = 1013;
                int user_id = 1039;

                string raw_file_name = bodyApiCreateEtaxFile.TextContent.FileName;
                string input = _config["Path:Input"] + "/" + member_id + "/" + now.ToString("yyyyMM") + "/" + raw_file_name;
                string url = "/" + member_id + "/" + now.ToString("yyyyMM") + "/" + raw_file_name;
                string output = _config["Path:Output"];

                var checkRawdataFiles = _context.rawdata_files
                .Where(x => x.file_name == raw_file_name)
                .FirstOrDefault();

                if (checkRawdataFiles != null)
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E006",
                        errorMessage = "ชื่อไฟล์ซ้ำในระบบ",
                    });


                var branch = _context.branchs
                .Where(x => x.member_id == member_id && x.branch_code == bodyApiCreateEtaxFile.SellerBranchId)
                .Select(x => new
                {
                    x.id,
                    x.branch_code,
                })
                .FirstOrDefault();

                try
                {
                    int document_type_id = 1;

                    if (bodyApiCreateEtaxFile.PdfTemplateId == "Bill-Payment")
                        document_type_id = 101;
                    else if (bodyApiCreateEtaxFile.PdfTemplateId == "Temp-Receipt")
                        document_type_id = 102;
                    else
                    {
                        return StatusCode(400, new
                        {
                            status = "ER",
                            errorCode = "E008",
                            errorMessage = "ไม่พบรูปแบบเอกสารที่ต้องการ",
                        });
                    }

                    string load_file_status = "no";
                    string gen_pdf_status = "no";
                    string send_email_status = "no";
                    string send_sms_status = "no";
                    string send_ebxml_status = "no";

                    if (bodyApiCreateEtaxFile.ServiceCode == "S01")
                    {
                        load_file_status = "pending";
                        gen_pdf_status = "no";
                        send_email_status = "no";
                        send_sms_status = "no";
                        send_ebxml_status = "no";
                    }
                    else if (bodyApiCreateEtaxFile.ServiceCode == "S02")
                    {
                        load_file_status = "pending";
                        gen_pdf_status = "pending";
                        send_email_status = "no";
                        send_sms_status = "no";
                        send_ebxml_status = "no";
                    }
                    else if (bodyApiCreateEtaxFile.ServiceCode == "S03")
                    {
                        load_file_status = "pending";
                        gen_pdf_status = "pending";
                        send_email_status = "pending";
                        send_sms_status = "no";
                        send_ebxml_status = "no";
                    }
                    else if (bodyApiCreateEtaxFile.ServiceCode == "S04")
                    {
                        load_file_status = "pending";
                        gen_pdf_status = "pending";
                        send_email_status = "no";
                        send_sms_status = "pending";
                        send_ebxml_status = "no";
                    }
                    else if (bodyApiCreateEtaxFile.ServiceCode == "S05")
                    {
                        load_file_status = "pending";
                        gen_pdf_status = "pending";
                        send_email_status = "pending";
                        send_sms_status = "pending";
                        send_ebxml_status = "no";
                    }
                    else
                    {
                        load_file_status = "pending";
                        gen_pdf_status = "pending";
                        send_email_status = "no";
                        send_sms_status = "no";
                        send_ebxml_status = "no";
                    }

                    string share_path = Encryption.SHA512("path_" + now.ToString("dd-MM-yyyy"));

                    byte[] bytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        bodyApiCreateEtaxFile.TextContent.OpenReadStream().CopyTo(memoryStream);
                        bytes = memoryStream.ToArray();
                    }
                    string base64 = Convert.ToBase64String(bytes);

                    if (ApiFileTransfer.UploadFile(_config["Path:FileTransfer"], url, base64, _config["Path:Mode"]))
                    {
                        RawDataFile rawDataFile = new RawDataFile()
                        {
                            member_id = member_id,
                            branch_id = branch.id,
                            member_user_id = user_id,
                            document_type_id = document_type_id,
                            file_name = raw_file_name,
                            input_path = input,
                            output_path = output,
                            load_file_status = load_file_status,
                            gen_pdf_status = gen_pdf_status,
                            send_email_status = send_email_status,
                            send_sms_status= send_sms_status,
                            send_ebxml_status = send_ebxml_status,
                            comment = "",
                            template_pdf = bodyApiCreateEtaxFile.PdfTemplateId,
                            template_email = bodyApiCreateEtaxFile.PdfTemplateId,
                            mode = "normal",
                            create_date = DateTime.Now,
                        };
                        _context.Add(rawDataFile);
                        _context.SaveChanges();

                        return StatusCode(200, new
                        {
                            status = "OK",
                        });
                    }
                    else return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "",
                        errorMessage = "อัพโหลดไฟล์ข้อมูลไม่สำเร็จ",
                    });
                }
                catch (Exception ex)
                {
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E099",
                        errorMessage = "เกิดความผิดพลาดบ้างอย่างในระบบ กรุณาติดต่อเจ้าหน้าที่",
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new
                {
                    status = "ER",
                    errorCode = "E099",
                    errorMessage = "เกิดความผิดพลาดบ้างอย่างในระบบ กรุณาติดต่อเจ้าหน้าที่",
                });
            }
        }

    }
}
