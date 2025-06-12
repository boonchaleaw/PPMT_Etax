
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class RawFilesController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IJwtService _jwtService;

        public RawFilesController(ApplicationDbContext context, IConfiguration config, IExceptionLogger exceptionLogger, IJwtService jwtService)
        {
            _config = config;
            _context = context;
            _context.Database.SetCommandTimeout(180);
            _exceptionLogger = exceptionLogger;
            _jwtService = jwtService;
        }
        [HttpPost]
        [Route("get_rawdata_tabel")]
        public async Task<IActionResult> GetRawFilesDataTabel([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                    JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select new
                                        {
                                            mup.per_raw_view,
                                            mup.view_self_only,
                                            mup.view_branch_only,
                                        }).FirstOrDefaultAsync();

                if (permission.per_raw_view != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                var searchBy = bodyDtParameters.Search?.Value;
                var orderCriteria = "id";
                var orderAscendingDirection = true;

                if (bodyDtParameters.Order != null)
                {
                    orderCriteria = bodyDtParameters.Columns[bodyDtParameters.Order[0].Column].Data;
                    orderAscendingDirection = bodyDtParameters.Order[0].Dir.ToString().ToLower() == "asc";
                }

                var result = _context.view_rawdata_files.Where(x => x.member_id == jwtStatus.member_id).AsQueryable();

                if (permission.view_self_only == "Y")
                {
                    result = result.Where(r => r.member_user_id == jwtStatus.user_id);
                }

                if (permission.view_branch_only == "Y")
                {
                    List<int> branchId = await (from mub in _context.member_user_branch
                                                where mub.member_user_id == jwtStatus.user_id
                                                select mub.branch_id).ToListAsync();

                    result = result.Where(r => branchId.Contains(r.branch_id));
                }

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.CountAsync();
                var totalResultsCount = await _context.view_rawdata_files.Where(x => x.member_id == jwtStatus.member_id).CountAsync();

                var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.member_id,
                        x.document_type_name,
                        x.file_name,
                        x.url_path,
                        x.load_file_status,
                        x.create_date,
                        x.load_file_error,
                        x.gen_pdf_status,
                        x.send_email_status,
                        x.send_ebxml_status,
                    })
                    .Skip(bodyDtParameters.Start)
                    .Take(bodyDtParameters.Length)
                    .ToListAsync();


                return StatusCode(200, new
                {
                    draw = bodyDtParameters.Draw,
                    recordsTotal = totalResultsCount,
                    recordsFiltered = filteredResultsCount,
                    data = data,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("get_add_raw_detail")]
        public async Task<IActionResult> GetAddRawDetail()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_raw_manage).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                var member = await _context.members
                .Where(x => x.id == jwtStatus.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                })
                .FirstOrDefaultAsync();

                List<Branch> liasBranch = await _context.branchs
               .Where(x => x.member_id == jwtStatus.member_id)
               .ToListAsync();

                var documentTypes = await _context.view_member_document_type
                .Select(x => new
                {
                    x.member_id,
                    x.document_type_id,
                    x.document_type_name,
                    x.service_type_id,
                })
                .Where(x =>
                (x.member_id == jwtStatus.member_id && x.service_type_id == 1) ||
                (x.member_id == jwtStatus.member_id && x.service_type_id == 3))
                .ToListAsync();


                if (member != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            member = member,
                            branchs = liasBranch,
                            document_types = documentTypes,
                        },
                    });
                }
                else
                {
                    return StatusCode(404, new
                    {
                        message = "ไม่พบข้อมูลที่ต้องการ",
                    });
                }

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("create_rawdata")]
        public async Task<IActionResult> GetCreateRawFile([FromBody] BodyRawData bodyRawData)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_raw_manage).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                if (bodyRawData.branch == null)
                    return StatusCode(400, new { message = "กรุณากำหนดสาขา", });

                if (bodyRawData.document_type == null)
                    return StatusCode(400, new { message = "กรุณากำหนดประเภทเอกสาร", });

                if (String.IsNullOrEmpty(bodyRawData.raw_file_data))
                    return StatusCode(400, new { message = "กรุณากำหนดไฟล์ข้อมูล", });

                DateTime now = DateTime.Now;

                //string folder_name = RandomString(50);
                string input = _config["Path:Input"] + "/" + jwtStatus.member_id + "/" + now.ToString("yyyyMM") + "/" + bodyRawData.raw_file_name;
                string url = "/" + jwtStatus.member_id + "/" + now.ToString("yyyyMM") + "/" + bodyRawData.raw_file_name;
                string output = _config["Path:Output"];

                string extension = Path.GetExtension(bodyRawData.raw_file_name).ToLower();
                if (extension != ".csv" && extension != ".txt" && extension != ".xlsx" && extension != ".xls")
                {
                    return StatusCode(400, new { message = "สามารถ upload ไฟล์ .txt .csv .xls .xlsx ได้เท่านั้น", });
                }

                string raw_file_name = bodyRawData.raw_file_name.Split('.')[0];

                var checkRawdataFiles = _context.rawdata_files
                .Where(x => x.file_name == raw_file_name)
                .FirstOrDefault();

                if (checkRawdataFiles != null)
                    return StatusCode(400, new { message = "ชื่อไฟล์ซ้ำในระบบ", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        byte[] bytes = Convert.FromBase64String(bodyRawData.raw_file_data.Split(',')[1]);
                        string base64 = Convert.ToBase64String(bytes);

                        if (ApiFileTransfer.UploadFile(_config["Path:FileTransfer"], url, base64, _config["Path:Mode"]))
                        {
                            RawDataFile rawDataFile = new RawDataFile()
                            {
                                member_id = jwtStatus.member_id,
                                branch_id = bodyRawData.branch.id,
                                member_user_id = jwtStatus.user_id,
                                document_type_id = bodyRawData.document_type.document_type_id,
                                file_name = raw_file_name,
                                input_path = input,
                                output_path = output,
                                load_file_status = "pending",
                                gen_pdf_status = (bodyRawData.gen_pdf_status) ? "pending" : "no",
                                send_email_status = (bodyRawData.send_email_status) ? "pending" : "no",
                                send_sms_status = "no",
                                send_ebxml_status = (bodyRawData.send_ebxml_status) ? "pending" : "no",
                                comment = (bodyRawData.comment != null) ? bodyRawData.comment : "",
                                template_pdf = "",
                                template_email = "",
                                mode = "normal",
                                create_date = DateTime.Now,
                            };
                            _context.Add(rawDataFile);
                            await _context.SaveChangesAsync();

                            LogRawFile logRawFile = new LogRawFile()
                            {
                                member_id = jwtStatus.member_id,
                                user_modify_id = jwtStatus.user_id,
                                rawdata_file_id = rawDataFile.id,
                                action_type = "create",
                                create_date = now,
                            };
                            _context.Add(logRawFile);
                            await _context.SaveChangesAsync();
                            transaction.Commit();


                            //if (bodyRawData.raw_file_data != "")
                            //{
                            //    Directory.CreateDirectory(Path.GetDirectoryName(input));
                            //    byte[] fileBytes = Convert.FromBase64String(bodyRawData.raw_file_data.Split(',')[1]);
                            //    System.IO.File.WriteAllBytes(input, fileBytes);
                            //}

                            try
                            {
                                Member member = await _context.members.Where(x => x.id == jwtStatus.member_id).FirstOrDefaultAsync();
                                if (member != null)
                                {
                                    string message = "หัวข้อ : อัพโหลด " + rawDataFile.file_name + "\n";
                                    message += "ลูกค้า : " + member.name + "\n";
                                    message = System.Web.HttpUtility.UrlEncode(message, Encoding.UTF8);

                                    var request = (HttpWebRequest)WebRequest.Create("https://notify-api.line.me/api/notify");
                                    var postData = string.Format("message={0}", message);
                                    var data = Encoding.UTF8.GetBytes(postData);
                                    request.Method = "POST";
                                    request.ContentType = "application/x-www-form-urlencoded";
                                    request.ContentLength = data.Length;
                                    request.Headers.Add("Authorization", "Bearer " + "iNmWWvIz3r3lsB9gG2I7NfVeU9dbj4bwSwkBEqZagl4");
                                    var stream = request.GetRequestStream();
                                    stream.Write(data, 0, data.Length);
                                    var response = (HttpWebResponse)request.GetResponse();
                                    var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                                }
                            }


                            catch (Exception ex)
                            {

                            }

                            return StatusCode(200, new
                            {
                                message = "อัพโหลดไฟล์ข้อมูลสำเร็จ",
                            });
                        }
                        else return StatusCode(400, new { message = "อัพโหลดไฟล์ข้อมูลไม่สำเร็จ", });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(400, new { message = ex.Message });
                    }
                }
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
        [Route("get_rawdata_detail/{id}")]
        public async Task<IActionResult> GetRawDataDetail(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_raw_view).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                var rawDataFile = await _context.view_rawdata_files
                .Where(x => x.id == id && x.member_id == jwtStatus.member_id)
                .Select(x => new
                {
                    x.id,
                    x.member_id,
                    x.member_user_id,
                    x.branch_id,
                    x.document_type_id,
                    x.document_type_name,
                    x.file_name,
                    x.url_path,
                    x.load_file_status,
                    x.load_file_processing,
                    x.load_file_finish,
                    x.load_file_error,
                    x.gen_pdf_status,
                    x.send_email_status,
                    x.send_ebxml_status,
                    x.file_total,
                    x.gen_xml_success,
                    x.gen_xml_fail,
                    x.create_date,
                })
                .FirstOrDefaultAsync();

                var member = await _context.members
                .Where(x => x.id == jwtStatus.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                })
                .FirstOrDefaultAsync();

                var branch = await _context.branchs
                .Where(x => x.id == rawDataFile.branch_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.building_number,
                    x.building_name,
                    x.street_name,
                    x.district_code,
                    x.district_name,
                    x.amphoe_code,
                    x.amphoe_name,
                    x.province_code,
                    x.province_name,
                    x.zipcode,
                })
                .FirstOrDefaultAsync();

                var member_user = await _context.member_users
                .Where(x => x.id == rawDataFile.member_user_id)
                .FirstOrDefaultAsync();

                if (rawDataFile != null && member != null && branch != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            member = new
                            {
                                id = member.id,
                                name = member.name,
                                tax_id = member.tax_id,
                            },
                            branch = new
                            {
                                id = branch.id,
                                name = branch.name,
                                building_number = branch.building_number,
                                building_name = branch.building_name,
                                street_name = branch.street_name,
                                district_code = branch.district_code,
                                district_name = branch.district_name,
                                amphoe_code = branch.amphoe_code,
                                amphoe_name = branch.amphoe_name,
                                province_code = branch.province_code,
                                province_name = branch.province_name,
                                zipcode = branch.zipcode,
                            },
                            document_type_id = rawDataFile.document_type_id,
                            document_type_name = rawDataFile.document_type_name,
                            file_name = rawDataFile.file_name,
                            url_path = rawDataFile.url_path,
                            load_file_status = rawDataFile.load_file_status,
                            load_file_processing = rawDataFile.load_file_processing,
                            load_file_finish = rawDataFile.load_file_finish,
                            load_file_error = rawDataFile.load_file_error,
                            gen_pdf_status = (rawDataFile.gen_pdf_status != "no") ? true : false,
                            send_email_status = (rawDataFile.send_email_status != "no") ? true : false,
                            send_ebxml_status = (rawDataFile.send_ebxml_status != "no") ? true : false,
                            file_total = rawDataFile.file_total,
                            gen_xml_success = rawDataFile.gen_xml_success,
                            gen_xml_fail = rawDataFile.gen_xml_fail,
                            member_user_name = member_user.first_name,
                            create_date = rawDataFile.create_date,
                        },
                    });
                }
                else
                {
                    return StatusCode(404, new
                    {
                        message = "ไม่พบข้อมูลที่ต้องการ",
                    });
                }

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("get_raw_process_detail/{id}")]
        public async Task<IActionResult> GetProcessDetail(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_raw_view).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                var rawdataFiles = await _context.rawdata_files
                .Where(x => x.id == id && x.member_id == jwtStatus.member_id)
                .Select(x => new
                {
                    x.id,
                    x.gen_pdf_status,
                    x.send_email_status,
                    x.send_ebxml_status,
                })
                .FirstOrDefaultAsync();

                if (rawdataFiles != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            id = rawdataFiles.id,
                            gen_pdf_status = (rawdataFiles.gen_pdf_status != "no") ? true : false,
                            send_email_status = (rawdataFiles.send_email_status != "no") ? true : false,
                            send_ebxml_status = (rawdataFiles.send_ebxml_status != "no") ? true : false,
                        },
                    });
                }
                else
                {
                    return StatusCode(404, new
                    {
                        message = "ไม่พบข้อมูลที่ต้องการ",
                    });
                }

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("update_raw_process")]
        public async Task<IActionResult> UpdateProcess([FromBody] BodyProcess bodyProcess)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_raw_manage).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var rawdataFiles = _context.rawdata_files
                        .Where(x => x.id == bodyProcess.id)
                        .FirstOrDefault();

                        if (rawdataFiles == null)
                            return StatusCode(401, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        if (rawdataFiles.gen_pdf_status == "no")
                            rawdataFiles.gen_pdf_status = (bodyProcess.gen_pdf_status) ? "pending" : "no";
                        if (rawdataFiles.send_email_status == "no")
                            rawdataFiles.send_email_status = (bodyProcess.send_email_status) ? "pending" : "no";
                        if (rawdataFiles.send_ebxml_status == "no")
                            rawdataFiles.send_ebxml_status = (bodyProcess.send_ebxml_status) ? "pending" : "no";


                        List<EtaxFile> listEtaxFile = await _context.etax_files
                        .Where(x => x.rawdata_file_id == rawdataFiles.id)
                        .ToListAsync();

                        foreach (EtaxFile etaxFile in listEtaxFile)
                        {
                            if (etaxFile.gen_pdf_status == "no")
                                etaxFile.gen_pdf_status = (bodyProcess.gen_pdf_status) ? "pending" : "no";
                            if (etaxFile.add_email_status == "no")
                                etaxFile.add_email_status = (bodyProcess.send_email_status) ? "pending" : "no";
                            if (etaxFile.add_ebxml_status == "no")
                                etaxFile.add_ebxml_status = (bodyProcess.send_ebxml_status) ? "pending" : "no";
                        }
                        await _context.SaveChangesAsync();

                        LogRawFile logRawFile = new LogRawFile()
                        {
                            member_id = jwtStatus.member_id,
                            user_modify_id = jwtStatus.user_id,
                            rawdata_file_id = rawdataFiles.id,
                            action_type = "update",
                            create_date = DateTime.Now,
                        };
                        _context.Add(logRawFile);
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
                    message = "แก้ไขข้อมูลสำเร็จ",
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("raw_file_zip_xml/{id}")]
        public async Task<IActionResult> RawFileZipXml(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_raw_view).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                List<ViewEtaxFile> etaxFiles = await _context.view_etax_files
                .Where(x => x.rawdata_file_id == id && x.member_id == jwtStatus.member_id && x.gen_xml_status == "success" && x.delete_status == 0)
                .ToListAsync();

                List<ReturnFile> listFile = new List<ReturnFile>();
                foreach (ViewEtaxFile etax in etaxFiles)
                {
                    string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etax.url_path + "/xml/" + etax.name + ".xml", _config["Path:Mode"]);
                    //Byte[] bytes = System.IO.File.ReadAllBytes(etax.output_path + etax.url_path + "\\xml\\" + etax.name + ".xml");
                    //String file = Convert.ToBase64String(bytes);
                    if (fileBase64 != "")
                    {
                        listFile.Add(new ReturnFile()
                        {
                            name = etax.name + ".xml",
                            data = fileBase64,
                        });
                    }
                    else
                        return StatusCode(400, new { message = "ไม่พบไฟล์ " + etax.name, });
                }

                return StatusCode(200, new
                {
                    message = "โหลดข้อมูลสำเร็จ",
                    data = listFile
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("raw_file_zip_pdf/{id}")]
        public async Task<IActionResult> RawFileZipPdf(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_raw_view).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                List<ViewEtaxFile> etaxFiles = await _context.view_etax_files
                .Where(x => x.rawdata_file_id == id && x.member_id == jwtStatus.member_id && x.gen_pdf_status == "success" && x.delete_status == 0)
                .ToListAsync();

                List<ReturnFile> listFile = new List<ReturnFile>();
                foreach (ViewEtaxFile etax in etaxFiles)
                {
                    string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etax.url_path + "/pdf/" + etax.name + ".pdf", _config["Path:Mode"]);
                    //Byte[] bytes = System.IO.File.ReadAllBytes(etax.output_path + etax.url_path + "\\pdf\\" + etax.name + ".pdf");
                    //String file = Convert.ToBase64String(bytes);
                    if (fileBase64 != "")
                    {
                        listFile.Add(new ReturnFile()
                        {
                            name = etax.name + ".pdf",
                            data = fileBase64,
                        });
                    }
                    else
                        return StatusCode(400, new { message = "ไม่พบไฟล์ " + etax.name, });
                }

                return StatusCode(200, new
                {
                    message = "โหลดข้อมูลสำเร็จ",
                    data = listFile
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("raw_file_zip_select")]
        public async Task<IActionResult> RawFileZipSelect([FromBody] List<BodyItemSelect> listBodyItemSelect)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_raw_view).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                List<ReturnRawFile> listRawFile = new List<ReturnRawFile>();
                foreach (BodyItemSelect bodyItemSelect in listBodyItemSelect)
                {
                    if (bodyItemSelect.select)
                    {
                        List<ViewEtaxFile> etaxFiles = await _context.view_etax_files
                        .Where(x => x.rawdata_file_id == bodyItemSelect.id && x.member_id == jwtStatus.member_id && x.delete_status == 0)
                        .ToListAsync();

                        if (etaxFiles.Count > 0)
                        {
                            listRawFile.Add(new ReturnRawFile()
                            {
                                name = etaxFiles[0].raw_name,
                                listFileXml = new List<ReturnFile>(),
                                listFilePdf = new List<ReturnFile>(),
                            });

                            foreach (ViewEtaxFile etax in etaxFiles)
                            {
                                if (etax.gen_xml_status == "success")
                                {
                                    string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etax.url_path + "/xml/" + etax.name + ".xml", _config["Path:Mode"]);
                                    //Byte[] bytes = System.IO.File.ReadAllBytes(etax.output_path + etax.url_path + "\\xml\\" + etax.name + ".xml");
                                    //String file = Convert.ToBase64String(bytes);
                                    if (fileBase64 != "")
                                    {
                                        listRawFile.Last().listFileXml.Add(new ReturnFile()
                                        {
                                            name = etax.name + ".xml",
                                            data = fileBase64,
                                        });
                                    }
                                    else
                                        return StatusCode(400, new { message = "ไม่พบไฟล์ " + etax.name, });
                                }

                                if (etax.gen_pdf_status == "success")
                                {
                                    string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etax.url_path + "/pdf/" + etax.name + ".pdf", _config["Path:Mode"]);
                                    //Byte[] bytes = System.IO.File.ReadAllBytes(etax.output_path + etax.url_path + "\\pdf\\" + etax.name + ".pdf");
                                    //String file = Convert.ToBase64String(bytes);
                                    if (fileBase64 != "")
                                    {
                                        listRawFile.Last().listFilePdf.Add(new ReturnFile()
                                        {
                                            name = etax.name + ".pdf",
                                            data = fileBase64,
                                        });
                                    }
                                    else
                                        return StatusCode(400, new { message = "ไม่พบไฟล์ " + etax.name, });
                                }
                            }
                        }
                    }
                }

                return StatusCode(200, new
                {
                    message = "โหลดข้อมูลสำเร็จ",
                    data = listRawFile
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        //////////////////////////Admin//////////////////////////

        [HttpPost]
        [Route("admin/get_rawdata_tabel")]
        public async Task<IActionResult> GetRawdataTabelAdmin([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_raw_menu).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                var searchBy = bodyDtParameters.searchText;
                var orderCriteria = "id";
                var orderAscendingDirection = true;

                if (bodyDtParameters.Order != null)
                {
                    orderCriteria = bodyDtParameters.Columns[bodyDtParameters.Order[0].Column].Data;
                    orderAscendingDirection = bodyDtParameters.Order[0].Dir.ToString().ToLower() == "asc";
                }

                List<int> listDocumentTypeID = await (from td in _context.document_type
                                                      where td.type == "etax"
                                                      select td.id).ToListAsync();

                var result = _context.view_rawdata_files.Where(x => listDocumentTypeID.Contains(x.document_type_id)).AsQueryable();

                if (bodyDtParameters.id != 0)
                {
                    result = result.Where(x => x.member_id == bodyDtParameters.id);
                }
                else
                {
                    var user_members = await _context.user_members
                    .Where(x => x.user_id == jwtStatus.user_id)
                    .ToListAsync();

                    List<int> membereId = new List<int>();
                    foreach (var member in user_members)
                        membereId.Add(member.member_id);

                    result = result.Where(x => membereId.Contains(x.member_id));
                }

                if (!string.IsNullOrEmpty(searchBy))
                {
                    result = result.Where(r =>
                        r.id.ToString().ToUpper().Contains(searchBy.ToUpper()) ||
                        r.member_name != null && r.member_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.document_type_name != null && r.document_type_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.file_name != null && r.file_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.load_file_status != null && r.load_file_status.ToUpper().Contains(searchBy.ToUpper())
                    );
                }

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.CountAsync();
                var totalResultsCount = 0;

                if (bodyDtParameters.Length == -1)
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.member_name,
                        x.document_type_name,
                        x.file_name,
                        x.load_file_status,
                        load_file_finish = (x.load_file_finish == null) ? "" : ((DateTime)x.load_file_finish).ToString("dd/MM/yyyy HH:mm:ss"),
                    })
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        data = data,
                    });
                }
                else
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.member_name,
                        x.document_type_name,
                        x.file_name,
                        x.load_file_status,
                        load_file_finish = (x.load_file_finish == null) ? "" : ((DateTime)x.load_file_finish).ToString("dd/MM/yyyy HH:mm:ss"),
                    })
                    .Skip(bodyDtParameters.Start)
                    .Take(bodyDtParameters.Length)
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        data = data,
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("admin/get_rawdata_detail/{id}")]
        public async Task<IActionResult> GetRawDataDetailAdmin(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_raw_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                var rawDataFile = await _context.view_rawdata_files
                .Where(x => x.id == id)
                .Select(x => new
                {
                    x.id,
                    x.member_id,
                    x.branch_id,
                    x.document_type_id,
                    x.document_type_name,
                    x.file_name,
                    x.url_path,
                    x.load_file_status,
                    load_file_processing = (x.load_file_processing == null) ? "" : ((DateTime)x.load_file_processing).ToString("dd/MM/yyyy HH:mm:ss"),
                    load_file_finish = (x.load_file_finish == null) ? "" : ((DateTime)x.load_file_finish).ToString("dd/MM/yyyy HH:mm:ss"),
                    x.load_file_error,
                    x.gen_pdf_status,
                    x.send_email_status,
                    x.send_ebxml_status,
                    x.file_total,
                    x.gen_xml_success,
                    x.gen_xml_fail,
                    x.comment,
                    create_date = x.create_date.ToString("dd/MM/yyyy HH:mm:ss"),
                })
                .FirstOrDefaultAsync();

                var membere = await (from um in _context.user_members
                                     where um.user_id == jwtStatus.user_id && um.member_id == rawDataFile.member_id
                                     select um).FirstOrDefaultAsync();

                if (membere == null)
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                var member = await _context.members
                .Where(x => x.id == jwtStatus.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                })
                .FirstOrDefaultAsync();

                var branch = await _context.branchs
                .Where(x => x.id == rawDataFile.branch_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.building_number,
                    x.building_name,
                    x.street_name,
                    x.district_code,
                    x.district_name,
                    x.amphoe_code,
                    x.amphoe_name,
                    x.province_code,
                    x.province_name,
                    x.zipcode,
                })
                .FirstOrDefaultAsync();

                if (rawDataFile != null && member != null && branch != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            member = new
                            {
                                id = member.id,
                                name = member.name,
                                tax_id = member.tax_id,
                            },
                            branch = new
                            {
                                id = branch.id,
                                name = branch.name,
                                building_number = branch.building_number,
                                building_name = branch.building_name,
                                street_name = branch.street_name,
                                district_code = branch.district_code,
                                district_name = branch.district_name,
                                amphoe_code = branch.amphoe_code,
                                amphoe_name = branch.amphoe_name,
                                province_code = branch.province_code,
                                province_name = branch.province_name,
                                zipcode = branch.zipcode,
                            },
                            document_type_id = rawDataFile.document_type_id,
                            document_type_name = rawDataFile.document_type_name,
                            file_name = rawDataFile.file_name,
                            url_path = rawDataFile.url_path,
                            load_file_status = rawDataFile.load_file_status,
                            load_file_processing = rawDataFile.load_file_processing,
                            load_file_finish = rawDataFile.load_file_finish,
                            load_file_error = rawDataFile.load_file_error,
                            gen_pdf_status = (rawDataFile.gen_pdf_status != "no") ? true : false,
                            send_email_status = (rawDataFile.send_email_status != "no") ? true : false,
                            send_ebxml_status = (rawDataFile.send_ebxml_status != "no") ? true : false,
                            file_total = rawDataFile.file_total,
                            gen_xml_success = rawDataFile.gen_xml_success,
                            gen_xml_fail = rawDataFile.gen_xml_fail,
                            comment = rawDataFile.comment,
                            create_date = rawDataFile.create_date,
                        },
                    });
                }
                else
                {
                    return StatusCode(404, new
                    {
                        message = "ไม่พบข้อมูลที่ต้องการ",
                    });
                }

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

    }
}
