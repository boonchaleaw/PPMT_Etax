
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class SendEmailController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public SendEmailController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        [HttpPost]
        [Route("get_email_tabel")]
        public async Task<IActionResult> GetSendEbxmlDataTabel([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var searchBy = bodyDtParameters.searchText;
                var orderCriteria = "id";
                var orderAscendingDirection = true;

                if (bodyDtParameters.Order != null)
                {
                    orderCriteria = bodyDtParameters.Columns[bodyDtParameters.Order[0].Column].Data;
                    orderAscendingDirection = bodyDtParameters.Order[0].Dir.ToString().ToLower() == "asc";
                }

                var result = _context.view_send_email_new.Where(x => x.member_id == jwtStatus.member_id).AsQueryable();

                if (bodyDtParameters.view_self_only)
                {
                    result = result.Where(r => r.member_user_id == jwtStatus.user_id);
                }

                if (bodyDtParameters.view_branch_only)
                {
                    List<int> listBranchs = new List<int>();
                    foreach (PermissionBranch branch in bodyDtParameters.branchs)
                        listBranchs.Add(branch.branch_id);
                    result = result.Where(r => listBranchs.Contains(r.branch_id));
                }

                bodyDtParameters.dateStart = DateTime.Parse(bodyDtParameters.dateStart.ToString()).Date;
                bodyDtParameters.dateEnd = bodyDtParameters.dateEnd.AddDays(+1).AddMilliseconds(-1);

                int document_id = System.Convert.ToInt32(bodyDtParameters.docType);

                if (!string.IsNullOrEmpty(searchBy))
                {
                    if (document_id == 0)
                    {
                        if (bodyDtParameters.dateType == "issue_date")
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.raw_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
                                (r.raw_name.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd)
                            );
                        }
                    }
                    else
                    {
                        if (bodyDtParameters.dateType == "issue_date")
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd)
                            );
                        }
                    }
                }
                else
                {
                    if (document_id == 0)
                    {
                        if (bodyDtParameters.dateType == "issue_date")
                        {
                            result = result.Where(r =>
                                (r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd)
                            );
                        }
                    }
                    else
                    {
                        if (bodyDtParameters.dateType == "issue_date")
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd)
                            );
                        }
                    }
                }

                if (bodyDtParameters.statusType1 != "")
                {
                    result = result.Where(r => r.send_email_status == bodyDtParameters.statusType1);
                }

                if (bodyDtParameters.statusType2 != "")
                {
                    result = result.Where(r => r.email_status == bodyDtParameters.statusType2);
                }

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.CountAsync();
                var totalResultsCount = await _context.view_send_email.Where(x => x.member_id == jwtStatus.member_id).CountAsync();

                if (bodyDtParameters.Length == -1)
                {

                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.etax_file_id,
                        x.etax_id,
                        x.member_id,
                        x.document_type_id,
                        x.create_type,
                        x.raw_name,
                        x.name,
                        x.buyer_email,
                        x.send_email_status,
                        x.send_email_finish,
                        x.send_xml_status,
                        x.email_status,
                        x.error,
                        x.url_path,
                        x.issue_date,

                    })
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        data = data
                    });
                }
                else
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.etax_file_id,
                        x.etax_id,
                        x.member_id,
                        x.document_type_id,
                        x.create_type,
                        x.raw_name,
                        x.name,
                        x.buyer_email,
                        x.send_email_status,
                        x.send_email_finish,
                        x.send_xml_status,
                        x.email_status,
                        x.error,
                        x.url_path,
                        x.issue_date,

                    })
                    .Skip(bodyDtParameters.Start)
                    .Take(bodyDtParameters.Length)
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        data = data
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("get_email_detail/{id}")]
        public async Task<IActionResult> GetEmailDetail(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var sendEmail = await _context.view_send_email
                .Where(x => x.etax_file_id == id && x.member_id == jwtStatus.member_id)
                .Select(x => new
                {
                    x.id,
                    x.etax_file_id,
                    x.etax_id,
                    x.member_id,
                    x.branch_id,
                    x.document_type_id,
                    x.create_type,
                    x.name,
                    x.raw_name,
                    x.send_email_status,
                    x.send_email_finish,
                    x.email_status,
                    x.email_receive,
                    x.email_open,
                    x.error,
                    x.url_path,
                })
                .FirstOrDefaultAsync();

                var member = await _context.members
                .Where(x => x.id == sendEmail.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                })
                .FirstOrDefaultAsync();

                var branch = await _context.branchs
                .Where(x => x.id == sendEmail.branch_id)
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

                var etax = await _context.view_etax_files
                .Where(x => x.id == sendEmail.etax_file_id)
                .Select(x => new
                {
                    x.etax_id,
                    x.issue_date,
                    x.ref_etax_id,
                    x.ref_issue_date,
                    x.buyer_branch_code,
                    x.buyer_id,
                    x.buyer_name,
                    x.buyer_tax_id,
                    x.buyer_address,
                    x.buyer_tel,
                    x.buyer_fax,
                    x.buyer_email,
                    x.original_price,
                    x.price,
                    x.discount,
                    x.tax,
                    x.total,
                })
                .FirstOrDefaultAsync();

                if (sendEmail != null && member != null && branch != null)
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
                            id = sendEmail.id,
                            etax_file_id = sendEmail.etax_file_id,
                            document_type_id = sendEmail.document_type_id,
                            create_type = sendEmail.create_type,
                            name = sendEmail.name,
                            raw_name = sendEmail.raw_name,
                            send_email_status = sendEmail.send_email_status,
                            send_email_finish = sendEmail.send_email_finish,
                            email_status = sendEmail.email_status,
                            email_receive = sendEmail.email_receive,
                            email_open = sendEmail.email_open,
                            error = sendEmail.error,
                            url_path = sendEmail.url_path,
                            etax_id = etax.etax_id,
                            issue_date = etax.issue_date,
                            ref_etax_id = etax.ref_etax_id,
                            ref_issue_date = etax.ref_issue_date,
                            buyer_branch_code = etax.buyer_branch_code,
                            buyer_id = etax.buyer_id,
                            buyer_name = etax.buyer_name,
                            buyer_tax_id = etax.buyer_tax_id,
                            buyer_address = etax.buyer_address,
                            buyer_tel = etax.buyer_tel,
                            buyer_fax = etax.buyer_fax,
                            buyer_email = etax.buyer_email,
                            original_price = etax.original_price,
                            price = etax.price,
                            discount = etax.discount,
                            tax = etax.tax,
                            total = etax.total,
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
        [Route("get_email_status/{id}")]
        public async Task<IActionResult> GetEmailStatus(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var responsEmail = await _context.response_email
                .Where(x => x.send_email_id == id)
                .Select(x => new
                {
                    x.id,
                    x.email,
                    x.email_status,
                })
                .ToListAsync();

                if (responsEmail != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = responsEmail,
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
        [Route("get_email_process/{id}")]
        public async Task<IActionResult> GetEmailProcess(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var sendEmail = _context.send_email
                .Where(x => x.id == id)
                .FirstOrDefault();

                if (sendEmail != null)
                {
                    var etaxFiles = await _context.etax_files
                    .Where(x => x.id == sendEmail.etax_file_id && x.member_id == jwtStatus.member_id)
                    .Select(x => new
                    {
                        x.id,
                        x.send_xml_status,
                        x.send_other_status,
                        x.gen_pdf_status,
                        x.add_email_status,
                        x.add_ebxml_status,
                        x.buyer_email,
                    })
                    .FirstOrDefaultAsync();

                    var sendEmailAttachFiles = await _context.send_email_attach_files
                    .Where(x => x.etax_file_id == sendEmail.etax_file_id)
                    .ToListAsync();


                    List<ReturnSendOtherFile> listSendOtherFile = new List<ReturnSendOtherFile>();
                    foreach (var file in sendEmailAttachFiles)
                    {
                        listSendOtherFile.Add(new ReturnSendOtherFile()
                        {
                            name = file.file_name,
                            size = file.file_size,
                        });
                    }

                    if (etaxFiles != null)
                    {
                        return StatusCode(200, new
                        {
                            message = "เรียกดูข้อมูลสำเร็จ",
                            data = new
                            {
                                etax_file_id = etaxFiles.id,
                                send_xml_status = (etaxFiles.send_xml_status == "N" || etaxFiles.send_xml_status == null) ? false : true,
                                send_other_status = (etaxFiles.send_other_status == "N" || etaxFiles.send_other_status == null) ? false : true,
                                gen_pdf_status = (etaxFiles.gen_pdf_status != "no") ? true : false,
                                send_email_status = (etaxFiles.add_email_status != "no") ? true : false,
                                send_ebxml_status = (etaxFiles.add_ebxml_status != "no") ? true : false,
                                buyer_email = etaxFiles.buyer_email,
                                send_other_file = listSendOtherFile,
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
        [Route("update_email_process")]
        public async Task<IActionResult> UpdateEmailProcess([FromBody] BodyEmail bodyEmail)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                if (String.IsNullOrEmpty(bodyEmail.buyer_email))
                    return StatusCode(400, new { message = "กรุณากำหนดอีเมล", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var etaxfile = _context.etax_files
                        .Where(x => x.id == bodyEmail.etax_file_id)
                        .FirstOrDefault();

                        if (etaxfile != null)
                        {
                            etaxfile.buyer_email = bodyEmail.buyer_email;
                            etaxfile.send_xml_status = (bodyEmail.send_xml_status) ? "Y" : "N";

                            if (bodyEmail.send_other_status)
                            {
                                var sendEmailAttachFiles = await _context.send_email_attach_files
                                .Where(x => x.etax_file_id == bodyEmail.etax_file_id)
                                .ToListAsync();
                                foreach (var f in sendEmailAttachFiles)
                                {
                                    List<BodySendOtherFile> listWhere = bodyEmail.send_other_file.Where(x => x.name == f.file_name).ToList();
                                    if (listWhere.Count() == 0)
                                        _context.send_email_attach_files.Remove(f);
                                }

                                foreach (BodySendOtherFile of in bodyEmail.send_other_file)
                                {
                                    if (of.data != "" && of.data != null)
                                    {
                                        string email_files = "/" + jwtStatus.member_id + "/email_files";
                                        Directory.CreateDirectory(_config["Path:Output"] + email_files);

                                        email_files += "/" + of.name;
                                        byte[] fileBytes = Convert.FromBase64String(of.data.Split(',')[1]);
                                        System.IO.File.WriteAllBytes(_config["Path:Output"] + email_files, fileBytes);

                                        var checkEmailAttachFiles = await _context.send_email_attach_files
                                        .Where(x => x.etax_file_id == bodyEmail.etax_file_id && x.file_name == of.name)
                                        .FirstOrDefaultAsync();
                                        if (checkEmailAttachFiles == null)
                                        {
                                            SendEmailAttachFile sendEmailAttachFile = new SendEmailAttachFile()
                                            {
                                                etax_file_id = bodyEmail.etax_file_id,
                                                file_name = of.name,
                                                file_path = email_files,
                                                file_size = Math.Round(of.size, 2),
                                                create_date = DateTime.Now,
                                            };
                                            _context.Add(sendEmailAttachFile);
                                        }
                                    }
                                }

                                etaxfile.send_other_status = (bodyEmail.send_other_status) ? "Y" : "N";
                            }

                            var sendEmail = _context.send_email
                            .Where(x => x.etax_file_id == bodyEmail.etax_file_id)
                            .FirstOrDefault();

                            if (sendEmail != null)
                            {
                                sendEmail.send_email_status = "pending";
                                sendEmail.email_status = null;
                                sendEmail.error = null;
                            }
                            await _context.SaveChangesAsync();

                            LogSendEmail logSendEmail = new LogSendEmail()
                            {
                                member_id = jwtStatus.member_id,
                                user_modify_id = jwtStatus.user_id,
                                etax_file_id = bodyEmail.etax_file_id,
                                action_type = "update",
                                create_date = DateTime.Now,
                            };
                            _context.Add(logSendEmail);
                            await _context.SaveChangesAsync();
                            transaction.Commit();
                        }
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

        //////////////////////////Admin//////////////////////////

        [HttpPost]
        [Route("admin/get_email_tabel")]
        public async Task<IActionResult> GetSendEbxmlDataTabelAdmin([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

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


                var result = _context.view_send_email.Where(x => listDocumentTypeID.Contains(x.document_type_id)).AsQueryable();

                if (bodyDtParameters.id != 0)
                {
                    result = result.Where(x => x.member_id == bodyDtParameters.id);
                }
                else
                {
                    var membereId = await (from um in _context.user_members
                                           where um.user_id == jwtStatus.user_id
                                           select um.member_id).ToListAsync();

                    result = result.Where(x => membereId.Contains(x.member_id));
                }

                bodyDtParameters.dateStart = DateTime.Parse(bodyDtParameters.dateStart.ToString()).Date;
                bodyDtParameters.dateEnd = bodyDtParameters.dateEnd.AddDays(+1).AddMilliseconds(-1);

                int document_id = System.Convert.ToInt32(bodyDtParameters.docType);

                if (!string.IsNullOrEmpty(searchBy))
                {
                    if (document_id == 0)
                    {
                        if (bodyDtParameters.dateType == "issue_date")
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.raw_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
                                (r.raw_name.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd)
                            );
                        }
                    }
                    else
                    {
                        if (bodyDtParameters.dateType == "issue_date")
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd)
                            );
                        }
                    }
                }
                else
                {
                    if (document_id == 0)
                    {
                        if (bodyDtParameters.dateType == "issue_date")
                        {
                            result = result.Where(r =>
                                (r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd)
                            );
                        }
                    }
                    else
                    {
                        if (bodyDtParameters.dateType == "issue_date")
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd)
                            );
                        }
                    }
                }

                if (bodyDtParameters.statusType1 != "")
                {
                    result = result.Where(r => r.send_email_status == bodyDtParameters.statusType1);
                }

                if (bodyDtParameters.statusType2 != "")
                {
                    result = result.Where(r => r.email_status == bodyDtParameters.statusType2);
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
                        x.etax_file_id,
                        x.etax_id,
                        x.member_id,
                        x.document_type_id,
                        x.create_type,
                        x.raw_name,
                        x.name,
                        x.buyer_email,
                        x.send_email_status,
                        x.send_email_finish,
                        x.send_xml_status,
                        x.email_status,
                        x.error,
                        x.url_path,
                        x.issue_date,

                    })
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        data = data
                    });
                }
                else
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.etax_file_id,
                        x.etax_id,
                        x.member_id,
                        x.document_type_id,
                        x.create_type,
                        x.raw_name,
                        x.name,
                        x.buyer_email,
                        x.send_email_status,
                        x.send_email_finish,
                        x.send_xml_status,
                        x.email_status,
                        x.error,
                        x.url_path,
                        x.issue_date,

                    })
                    .Skip(bodyDtParameters.Start)
                    .Take(bodyDtParameters.Length)
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        data = data
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("admin/get_email_detail/{id}")]
        public async Task<IActionResult> GetEmailDetailAdmin(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var sendEmail = await _context.view_send_email
                .Where(x => x.id == id)
                .Select(x => new
                {
                    x.id,
                    x.etax_file_id,
                    x.etax_id,
                    x.member_id,
                    x.branch_id,
                    x.document_type_id,
                    x.create_type,
                    x.name,
                    x.raw_name,
                    x.send_email_status,
                    x.send_email_finish,
                    x.email_status,
                    x.email_receive,
                    x.email_open,
                    x.error,
                    x.url_path,
                })
                .FirstOrDefaultAsync();

                var member = await _context.members
                .Where(x => x.id == sendEmail.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                })
                .FirstOrDefaultAsync();

                var branch = await _context.branchs
                .Where(x => x.id == sendEmail.branch_id)
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

                var etax = await _context.view_etax_files
                .Where(x => x.id == sendEmail.etax_file_id)
                .Select(x => new
                {
                    x.etax_id,
                    x.issue_date,
                    x.ref_etax_id,
                    x.ref_issue_date,
                    x.buyer_branch_code,
                    x.buyer_id,
                    x.buyer_name,
                    x.buyer_tax_id,
                    x.buyer_address,
                    x.buyer_tel,
                    x.buyer_fax,
                    x.buyer_email,
                    x.original_price,
                    x.price,
                    x.discount,
                    x.tax,
                    x.total,
                })
                .FirstOrDefaultAsync();

                if (sendEmail != null && member != null && branch != null)
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
                            document_type_id = sendEmail.document_type_id,
                            create_type = sendEmail.create_type,
                            name = sendEmail.name,
                            raw_name = sendEmail.raw_name,
                            send_email_status = sendEmail.send_email_status,
                            send_email_finish = sendEmail.send_email_finish,
                            email_status = sendEmail.email_status,
                            email_receive = sendEmail.email_receive,
                            email_open = sendEmail.email_open,
                            error = sendEmail.error,
                            url_path = sendEmail.url_path,
                            etax_id = etax.etax_id,
                            issue_date = etax.issue_date,
                            ref_etax_id = etax.ref_etax_id,
                            ref_issue_date = etax.ref_issue_date,
                            buyer_branch_code = etax.buyer_branch_code,
                            buyer_id = etax.buyer_id,
                            buyer_name = etax.buyer_name,
                            buyer_tax_id = etax.buyer_tax_id,
                            buyer_address = etax.buyer_address,
                            buyer_tel = etax.buyer_tel,
                            buyer_fax = etax.buyer_fax,
                            buyer_email = etax.buyer_email,
                            original_price = etax.original_price,
                            price = etax.price,
                            discount = etax.discount,
                            tax = etax.tax,
                            total = etax.total,
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
        [Route("admin/update_email")]
        public async Task<IActionResult> UpdateEmailAdmin([FromBody] BodyEmail bodyEmail)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                if (String.IsNullOrEmpty(bodyEmail.buyer_email))
                    return StatusCode(400, new { message = "กรุณากำหนดอีเมล", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var etaxfile = _context.etax_files
                        .Where(x => x.id == bodyEmail.etax_file_id)
                        .FirstOrDefault();

                        if (etaxfile != null)
                        {
                            etaxfile.buyer_email = bodyEmail.buyer_email;
                            etaxfile.send_xml_status = (bodyEmail.send_xml_status) ? "Y" : "N";

                            var sendEmail = _context.send_email
                            .Where(x => x.etax_file_id == bodyEmail.etax_file_id)
                            .FirstOrDefault();

                            if (sendEmail != null)
                            {
                                sendEmail.send_email_status = "pending";
                                sendEmail.email_status = null;
                                sendEmail.error = null;
                            }
                            await _context.SaveChangesAsync();

                            LogSendEmail logSendEmail = new LogSendEmail()
                            {
                                member_id = jwtStatus.member_id,
                                user_modify_id = jwtStatus.user_id,
                                etax_file_id = bodyEmail.etax_file_id,
                                action_type = "update",
                                create_date = DateTime.Now,
                            };
                            _context.Add(logSendEmail);
                            await _context.SaveChangesAsync();
                            transaction.Commit();
                        }
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

    }
}
