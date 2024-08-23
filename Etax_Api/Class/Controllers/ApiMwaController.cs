
using Microsoft.AspNetCore.Http;
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
using System.Threading;
using System.Threading.Tasks;

namespace Etax_Api.Controllersท
{
    [Produces("application/json")]
    public class ApiMwaController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public ApiMwaController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        [HttpPost]
        [Route("ppmt_create_etax_file")]
        public async Task<IActionResult> ApiCreateEtaxFile([FromForm] BodyApiCreateEtaxFileMwa bodyApiCreateEtaxFile)
        {
            try
            {

                if (bodyApiCreateEtaxFile.APIKey != "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoiMTAxMSIsIm1lbWJlcl9pZCI6IjEyIiwiZXhwIjoxNzQ5Nzg3ODMyLCJpc3MiOiJwYXBlcm1hdGVfZXRheCIsImF1ZCI6IjEwMTEifQ.Tj1mbOH4Cf_E3F_1AqndDEFtGtMIVQ-pvw8EqlKIyBEzGbZG1qQx9cfdxhn45AZFCOmrXtNT1HJjeWJQxe_d_A")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E001",
                        errorMessage = "APIKey ไม่ถูกต้อง",
                    });

                if (bodyApiCreateEtaxFile.UserCode != "mwatest" || bodyApiCreateEtaxFile.AccessKey != "P@ssw0rd")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E002",
                        errorMessage = "ชื่อผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง",
                    });

                if (bodyApiCreateEtaxFile.SellerTaxId != "0994000165463")
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
                int member_id = 12;
                int user_id = 1035;

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
                    if (bodyApiCreateEtaxFile.PdfTemplateId == "TIV-MWA01" ||
                        bodyApiCreateEtaxFile.PdfTemplateId == "TIV-MWA02" ||
                        bodyApiCreateEtaxFile.PdfTemplateId == "TIV-MWA03" ||
                        bodyApiCreateEtaxFile.PdfTemplateId == "TIV-WTGL01" ||
                        bodyApiCreateEtaxFile.PdfTemplateId == "TIV-MWA0102")
                        document_type_id = 1;
                    else if (bodyApiCreateEtaxFile.PdfTemplateId == "RCT-MWA02" ||
                        bodyApiCreateEtaxFile.PdfTemplateId == "RCT-OTGL01")
                        document_type_id = 4;
                    else if (bodyApiCreateEtaxFile.PdfTemplateId == "CDN-MWA01" ||
                        bodyApiCreateEtaxFile.PdfTemplateId == "CDN-MWA02")
                        document_type_id = 3;
                    else if (bodyApiCreateEtaxFile.PdfTemplateId == "EIV-MWA-WT01" ||
                        bodyApiCreateEtaxFile.PdfTemplateId == "EIV-MWA01" ||
                        bodyApiCreateEtaxFile.PdfTemplateId == "EIV-MWA02" ||
                        bodyApiCreateEtaxFile.PdfTemplateId == "EIV-MWA03" ||
                        bodyApiCreateEtaxFile.PdfTemplateId == "EIV-MWA-DOC01")
                        document_type_id = 5;
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
                        send_ebxml_status = "no";
                    }
                    else if (bodyApiCreateEtaxFile.ServiceCode == "S02")
                    {
                        load_file_status = "pending";
                        gen_pdf_status = "pending";
                        send_email_status = "no";
                        send_ebxml_status = "no";
                    }
                    else if (bodyApiCreateEtaxFile.ServiceCode == "S03")
                    {
                        load_file_status = "pending";
                        gen_pdf_status = "pending";
                        send_email_status = "pending";
                        send_ebxml_status = "no";
                    }
                    else
                    {
                        load_file_status = "pending";
                        gen_pdf_status = "pending";
                        send_email_status = "pending";
                        send_ebxml_status = "no";
                    }


                    if (bodyApiCreateEtaxFile.TextContent.FileName.Contains("SFTP"))
                    {
                        string[] nameArray = raw_file_name.Split('_');
                        string cycle = nameArray[2];
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
                                cycle = cycle,
                                input_path = input,
                                output_path = output,
                                load_file_status = load_file_status,
                                gen_pdf_status = gen_pdf_status,
                                send_email_status = send_email_status,
                                send_sms_status = send_sms_status,
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
                    else
                    {
                        List<string> listData = new List<string>();
                        using (var reader = new StreamReader(bodyApiCreateEtaxFile.TextContent.OpenReadStream()))
                        {
                            while (reader.Peek() >= 0)
                                listData.Add(reader.ReadLine());
                        }

                        int docCount = 0;
                        foreach (string line in listData)
                        {
                            string[] lineArray = line.Split(',');
                            if (lineArray.First().Replace("\"", "") == "C")
                            {
                                docCount++;
                            }
                        }

                        if (docCount > 1)
                        {
                            return StatusCode(400, new
                            {
                                status = "ER",
                                errorCode = "E007",
                                errorMessage = "พบเอกสารมากกว่า 1 รายการในไฟล์ข้อมูล",
                            });
                        }

                        string file_name = Path.GetFileNameWithoutExtension(bodyApiCreateEtaxFile.TextContent.FileName);
                        string sharePath = "/" + member_id + "/" + Encryption.SHA256("path_" + now.ToString("dd-MM-yyyy HH:mm:ss"));
                        string base64 = "";

                        try
                        {
                            byte[] bytes;
                            using (var memoryStream = new MemoryStream())
                            {
                                bodyApiCreateEtaxFile.TextContent.OpenReadStream().CopyTo(memoryStream);
                                bytes = memoryStream.ToArray();
                            }
                            base64 = Convert.ToBase64String(bytes);
                        }
                        catch (Exception ex)
                        {
                            return StatusCode(400, new
                            {
                                status = "ER",
                                errorCode = "E009",
                                errorMessage = "ไม่สามารถอ่านไฟล์ข้อมูลได้",
                            });
                        }

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
                                send_sms_status = send_sms_status,
                                send_ebxml_status = send_ebxml_status,
                                comment = "",
                                share_path = sharePath,
                                template_pdf = bodyApiCreateEtaxFile.PdfTemplateId,
                                template_email = bodyApiCreateEtaxFile.PdfTemplateId,
                                mode = "normal",
                                create_date = DateTime.Now,
                            };

                            _context.Add(rawDataFile);
                            _context.SaveChanges();

                            //    Thread.Sleep(500);

                            //    int countCheck = 0;
                            //CheckShare:

                            //    List<EtaxFile> listEtaxFile = _context.etax_files
                            //    .Where(x => x.rawdata_file_id == rawDataFile.id)
                            //    .ToList();

                            //    if (countCheck > 50)
                            //    {
                            //        return StatusCode(400, new
                            //        {
                            //            status = "ER",
                            //            errorCode = "E011",
                            //            errorMessage = "ระบบไม่สามารถแชร์ไฟล์ได้",
                            //        });
                            //    }
                            //    else if (listEtaxFile.Count == 0)
                            //    {
                            //        Thread.Sleep(500);
                            //        countCheck++;
                            //        goto CheckShare;
                            //    }


                            //    foreach (EtaxFile etax in listEtaxFile)
                            //    {
                            //        string xmlPath = etax.share_path + "/xml/" + etax.name + ".xml";
                            //        string pdfPath = etax.share_path + "/pdf/" + etax.name + ".pdf";

                            //        if (!System.IO.File.Exists(_config["Path:Share"] + xmlPath))
                            //        {
                            //            string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etax.url_path + "/xml/" + etax.name + ".xml", _config["Path:Mode"]);
                            //            if (fileBase64 != "")
                            //            {
                            //                Directory.CreateDirectory(Path.GetDirectoryName(_config["Path:Share"] + xmlPath));
                            //                System.IO.File.WriteAllBytes(_config["Path:Share"] + xmlPath, Convert.FromBase64String(fileBase64));
                            //            }
                            //        }

                            //        if (!System.IO.File.Exists(_config["Path:Share"] + pdfPath))
                            //        {
                            //            string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etax.url_path + "/pdf/" + etax.name + ".pdf", _config["Path:Mode"]);
                            //            if (fileBase64 != "")
                            //            {
                            //                Directory.CreateDirectory(Path.GetDirectoryName(_config["Path:Share"] + pdfPath));
                            //                System.IO.File.WriteAllBytes(_config["Path:Share"] + pdfPath, Convert.FromBase64String(fileBase64));
                            //            }
                            //        }

                            //        Thread.Sleep(500);

                            //    ReCheckShare:
                            //        if (!System.IO.File.Exists(_config["Path:Share"] + xmlPath) || !System.IO.File.Exists(_config["Path:Share"] + pdfPath))
                            //        {
                            //            Thread.Sleep(500);
                            //            goto ReCheckShare;
                            //        }
                            //    }

                            if (rawDataFile.gen_pdf_status == "pending")
                            {
                                return StatusCode(200, new
                                {
                                    status = "OK",
                                    //xmlURL = _config["Path:Url"] + sharePath + "/xml/" + file_name + ".xml",
                                    //pdfURL = _config["Path:Url"] + sharePath + "/pdf/" + file_name + ".pdf",
                                });
                            }
                            else
                            {
                                return StatusCode(200, new
                                {
                                    status = "OK",
                                    //xmlURL = _config["Path:Url"] + sharePath + "/xml/" + file_name + ".xml",
                                });
                            }
                        }
                        else return StatusCode(400, new
                        {
                            status = "ER",
                            errorCode = "E010",
                            errorMessage = "อัพโหลดไฟล์ข้อมูลไม่สำเร็จ",
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

        [HttpPost]
        [Route("ppmt_cancel_etax")]
        public async Task<IActionResult> ApiCancelEtax([FromForm] BodyApiCancelEtaxFile bodyApiCancelEtaxFile)
        {
            try
            {
                if (bodyApiCancelEtaxFile.APIKey != "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoiMTAxMSIsIm1lbWJlcl9pZCI6IjEyIiwiZXhwIjoxNzQ5Nzg3ODMyLCJpc3MiOiJwYXBlcm1hdGVfZXRheCIsImF1ZCI6IjEwMTEifQ.Tj1mbOH4Cf_E3F_1AqndDEFtGtMIVQ-pvw8EqlKIyBEzGbZG1qQx9cfdxhn45AZFCOmrXtNT1HJjeWJQxe_d_A")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E001",
                        errorMessage = "APIKey ไม่ถูกต้อง",
                    });

                if (bodyApiCancelEtaxFile.UserCode != "mwatest" || bodyApiCancelEtaxFile.AccessKey != "P@ssw0rd")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E002",
                        errorMessage = "ชื่อผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง",
                    });

                if (bodyApiCancelEtaxFile.SellerTaxId != "0994000165463")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E003",
                        errorMessage = "ไม่พบ Tax ID ในระบบ",
                    });

                if (bodyApiCancelEtaxFile.SellerBranchId != "00000")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E004",
                        errorMessage = "ไม่พบสาขาที่ต้องการ",
                    });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        List<EtaxFile> listEtaxFile = await _context.etax_files
                        .Where(x => x.name == bodyApiCancelEtaxFile.FileName && x.member_id == 12)
                        .ToListAsync();

                        if (listEtaxFile.Count == 0)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลในระบบ", });

                        foreach (EtaxFile etax in listEtaxFile)
                        {
                            if (etax.add_ebxml_status == "success")
                                return StatusCode(400, new { message = "ข้อมูลถูกส่งสรรพากรแล้ว ไม่สามารถลบได้", });

                            if (bodyApiCancelEtaxFile.ServiceCode == "S02")
                                etax.delete_status = 2;
                            else
                                etax.delete_status = 1;
                        }
                        await _context.SaveChangesAsync();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(400, new { message = ex.Message });
                    }
                }

                return StatusCode(200, new
                {
                    message = "ลบข้อมูลสำเร็จ",
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("ppmt_get_etax_list")]
        public async Task<IActionResult> ApiGetEtaxList([FromForm] BodyApiGetEtaxList bodyApiGetEtaxList)
        {
            try
            {

                if (bodyApiGetEtaxList.APIKey != "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoiMTAxMSIsIm1lbWJlcl9pZCI6IjEyIiwiZXhwIjoxNzQ5Nzg3ODMyLCJpc3MiOiJwYXBlcm1hdGVfZXRheCIsImF1ZCI6IjEwMTEifQ.Tj1mbOH4Cf_E3F_1AqndDEFtGtMIVQ-pvw8EqlKIyBEzGbZG1qQx9cfdxhn45AZFCOmrXtNT1HJjeWJQxe_d_A")
                    return StatusCode(401, new { message = "token ไม่ถูกต้อง", });

                string fileName = "_" + bodyApiGetEtaxList.cycle + "_" + bodyApiGetEtaxList.hour.ToString().PadLeft(2, '0') + "_";

                List<ReturnEtaxList> listfile = _context.rawdata_files
                .Where(x => x.cycle == bodyApiGetEtaxList.cycle && x.member_id == 12 && x.template_pdf == bodyApiGetEtaxList.template && x.file_name.Contains(fileName))
                .Select(x => new ReturnEtaxList
                {
                    id = x.id,
                    type = x.template_pdf,
                    file = x.output_path + x.url_path,
                })
                .ToList();

                foreach (var data in listfile)
                {
                    data.listXml = _context.etax_files
                        .Where(x => x.rawdata_file_id == data.id && x.gen_xml_status == "success" && x.delete_status == 0)
                        .Select(x => new ReturnEtaxFile
                        {
                            name = x.name,
                            path = x.url_path,
                        }).ToList();

                    data.listPdf = _context.etax_files
                    .Where(x => x.rawdata_file_id == data.id && x.gen_pdf_status == "success" && x.delete_status == 0)
                    .Select(x => new ReturnEtaxFile
                    {
                        name = x.name,
                        path = x.url_path,
                    }).ToList();

                    data.count_xml_success = data.listXml.Count();
                    data.count_pdf_success = data.listPdf.Count();
                }


                DateTime dateStart = DateTime.ParseExact(bodyApiGetEtaxList.cycle, "yyyy-MM-dd", new CultureInfo("th-TH"));
                DateTime dateEnd = dateStart.AddDays(1);

                List<ReturnEtaxList> listfileAdhoc = _context.rawdata_files
                .Where(x => x.cycle == null && x.member_id == 12 && x.template_pdf == bodyApiGetEtaxList.template && x.create_date >= dateStart && x.create_date < dateEnd)
                .Select(x => new ReturnEtaxList
                {
                    id = x.id,
                    type = x.template_pdf,
                    file = x.output_path + x.url_path,
                })
                .ToList();

                foreach (var data in listfileAdhoc)
                {
                    data.listXml = _context.etax_files
                        .Where(x => x.rawdata_file_id == data.id && x.gen_xml_status == "success" && x.delete_status == 0)
                        .Select(x => new ReturnEtaxFile
                        {
                            name = x.name,
                            path = x.url_path,
                        }).ToList();

                    data.listPdf = _context.etax_files
                    .Where(x => x.rawdata_file_id == data.id && x.gen_pdf_status == "success" && x.delete_status == 0)
                    .Select(x => new ReturnEtaxFile
                    {
                        name = x.name,
                        path = x.url_path,
                    }).ToList();

                    data.count_xml_success = data.listXml.Count();
                    data.count_pdf_success = data.listPdf.Count();
                }

                return StatusCode(200, new
                {
                    data = new
                    {
                        list_file = listfile,
                        list_file_adhoc = listfileAdhoc,
                    },
                    message = "เรียกดูข้อมูลสำเร็จ",
                });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        [Route("ppmt_get_etax_summary")]
        public async Task<IActionResult> ApiGetEtaxSummary([FromForm] BodyApiGetEtaxList bodyApiGetEtaxList)
        {
            try
            {

                if (bodyApiGetEtaxList.APIKey != "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoiMTAxMSIsIm1lbWJlcl9pZCI6IjEyIiwiZXhwIjoxNzQ5Nzg3ODMyLCJpc3MiOiJwYXBlcm1hdGVfZXRheCIsImF1ZCI6IjEwMTEifQ.Tj1mbOH4Cf_E3F_1AqndDEFtGtMIVQ-pvw8EqlKIyBEzGbZG1qQx9cfdxhn45AZFCOmrXtNT1HJjeWJQxe_d_A")
                    return StatusCode(401, new { message = "token ไม่ถูกต้อง", });

                List<ReturnEtaxSummary> listData = _context.etax_files
                .Where(x => x.cycle == bodyApiGetEtaxList.cycle && x.member_id == 12 && x.create_date >= bodyApiGetEtaxList.start && x.create_date <= bodyApiGetEtaxList.end)
                .GroupBy(x => new
                {
                    x.cycle,
                    x.template_pdf
                })
                .Select(x => new ReturnEtaxSummary
                {
                    cycle = x.Key.cycle,
                    template_pdf = x.Key.template_pdf,
                    total = x.Count(),
                    count_xml_fail = x.Count(x => x.gen_xml_status == "fail"),
                    count_pdf_fail = x.Count(x => x.gen_pdf_status == "fail"),
                })
                .ToList();

                foreach (ReturnEtaxSummary data in listData)
                {
                    data.listXmlFail = new List<ReturnEtaxSummaryFail>();
                    if (data.count_xml_fail > 0)
                    {
                        data.listXmlFail = _context.etax_files
                            .Where(x => x.cycle == bodyApiGetEtaxList.cycle && x.member_id == 12 && x.gen_xml_status == "fail" && x.create_date >= bodyApiGetEtaxList.start && x.create_date <= bodyApiGetEtaxList.end)
                            .Select(x => new ReturnEtaxSummaryFail
                            {
                                name = x.name,
                                error = x.error,
                            })
                            .ToList();
                    }

                    data.listPdfFail = new List<ReturnEtaxSummaryFail>();
                    if (data.count_pdf_fail > 0)
                    {
                        data.listXmlFail = _context.etax_files
                            .Where(x => x.cycle == bodyApiGetEtaxList.cycle && x.member_id == 12 && x.gen_pdf_status == "fail" && x.create_date >= bodyApiGetEtaxList.start && x.create_date <= bodyApiGetEtaxList.end)
                            .Select(x => new ReturnEtaxSummaryFail
                            {
                                name = x.name,
                                error = x.error,
                            })
                            .ToList();
                    }

                    data.listEmailFail = new List<ReturnEtaxSummaryFail>();
                }

                List<EtaxFile> listEtax = _context.etax_files.Where(x => x.cycle == bodyApiGetEtaxList.cycle && x.member_id == 12 && x.create_date >= bodyApiGetEtaxList.start && x.create_date <= bodyApiGetEtaxList.end).ToList();
                foreach (EtaxFile etax in listEtax)
                {
                    ReturnEtaxSummary etaxSummaryWhere = listData.Where(x => x.template_pdf == etax.template_pdf).FirstOrDefault();
                    if (etaxSummaryWhere != null)
                    {
                        List<SendEmail> listSendEmail = _context.send_email.Where(x => x.etax_file_id == etax.id && (x.send_email_status == "fail" || x.email_status == "fail")).ToList();
                        foreach (SendEmail sendEmail in listSendEmail)
                        {
                            if (sendEmail.error != "Email address not found.")
                            {
                                etaxSummaryWhere.count_email_fail++;
                                etaxSummaryWhere.listEmailFail.Add(new ReturnEtaxSummaryFail()
                                {
                                    name = etax.name,
                                    error = sendEmail.error.Replace("[Taximail] : ", ""),
                                });
                            }
                        }
                    }
                }

                return StatusCode(200, new
                {
                    data = listData,
                    message = "เรียกดูข้อมูลสำเร็จ",
                });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        [Route("ppmt_get_etax_cycle_summary")]
        public async Task<IActionResult> ApiGetEtaxCycleSummary([FromForm] BodyApiGetEtaxCycleList bodyApiGetEtaxCycleLis)
        {
            try
            {

                if (bodyApiGetEtaxCycleLis.APIKey != "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoiMTAxMSIsIm1lbWJlcl9pZCI6IjEyIiwiZXhwIjoxNzQ5Nzg3ODMyLCJpc3MiOiJwYXBlcm1hdGVfZXRheCIsImF1ZCI6IjEwMTEifQ.Tj1mbOH4Cf_E3F_1AqndDEFtGtMIVQ-pvw8EqlKIyBEzGbZG1qQx9cfdxhn45AZFCOmrXtNT1HJjeWJQxe_d_A")
                    return StatusCode(401, new { message = "token ไม่ถูกต้อง", });

                DateTime dateStart = DateTime.ParseExact(bodyApiGetEtaxCycleLis.cycle + " " + bodyApiGetEtaxCycleLis.time + ":00:00", "yyyy-MM-dd HH:mm:ss", new CultureInfo("th-TH"));
                DateTime dateEnd = dateStart.AddHours(1).AddMinutes(-1);

                string file_name = bodyApiGetEtaxCycleLis.cycle + "_" + bodyApiGetEtaxCycleLis.time;

                List<RawDataFile> listRowFile = _context.rawdata_files
                .Where(x => x.cycle == bodyApiGetEtaxCycleLis.cycle && x.member_id == 12 && x.file_name.Contains(file_name))
                .ToList();

                List<ReturnEtaxSummary> listData = new List<ReturnEtaxSummary>();
                foreach (RawDataFile rowFile in listRowFile)
                {
                    listData.AddRange(_context.etax_files
                    .Where(x => x.rawdata_file_id == rowFile.id)
                    .GroupBy(x => new
                    {
                        x.cycle,
                        x.template_pdf,
                    })
                    .Select(x => new ReturnEtaxSummary
                    {
                        cycle = x.Key.cycle,
                        template_pdf = x.Key.template_pdf,
                        total = x.Count(),
                        count_xml_fail = x.Count(x => x.gen_xml_status == "fail"),
                        count_pdf_fail = x.Count(x => x.gen_pdf_status == "fail"),
                    })
                    .ToList());

                    foreach (ReturnEtaxSummary data in listData)
                    {
                        data.listXmlFail = new List<ReturnEtaxSummaryFail>();
                        if (data.count_xml_fail > 0)
                        {
                            data.listXmlFail = _context.etax_files
                                .Where(x => x.rawdata_file_id == rowFile.id && x.gen_xml_status == "fail")
                                .Select(x => new ReturnEtaxSummaryFail
                                {
                                    name = x.name,
                                    error = x.error,
                                })
                                .ToList();
                        }

                        data.listPdfFail = new List<ReturnEtaxSummaryFail>();
                        if (data.count_pdf_fail > 0)
                        {
                            data.listXmlFail = _context.etax_files
                                .Where(x => x.rawdata_file_id == rowFile.id && x.gen_pdf_status == "fail")
                                .Select(x => new ReturnEtaxSummaryFail
                                {
                                    name = x.name,
                                    error = x.error,
                                })
                                .ToList();
                        }

                        data.listEmailFail = new List<ReturnEtaxSummaryFail>();
                    }

                    List<EtaxFile> listEtax = _context.etax_files.Where(x => x.rawdata_file_id == rowFile.id).ToList();
                    foreach (EtaxFile etax in listEtax)
                    {
                        ReturnEtaxSummary etaxSummaryWhere = listData.Where(x => x.template_pdf == etax.template_pdf).FirstOrDefault();
                        if (etaxSummaryWhere != null)
                        {
                            List<SendEmail> listSendEmail = _context.send_email.Where(x => x.etax_file_id == etax.id && (x.send_email_status == "fail" || x.email_status == "fail")).ToList();
                            foreach (SendEmail sendEmail in listSendEmail)
                            {
                                if (sendEmail.error != "Email address not found.")
                                {
                                    etaxSummaryWhere.count_email_fail++;
                                    etaxSummaryWhere.listEmailFail.Add(new ReturnEtaxSummaryFail()
                                    {
                                        name = etax.name,
                                        error = sendEmail.error.Replace("[Taximail] : ", ""),
                                    });
                                }
                            }
                        }
                    }

                };

                return StatusCode(200, new
                {
                    data = listData,
                    message = "เรียกดูข้อมูลสำเร็จ",
                });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new
                {
                    message = ex.Message
                });
            }
        }
    }
}