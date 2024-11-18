
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
    public class SendSmsController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public SendSmsController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        [HttpPost]
        [Route("get_sms_tabel")]
        public async Task<IActionResult> GetSendSmsDataTabel([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select new
                                        {
                                            mup.per_sms_view,
                                            mup.view_self_only,
                                            mup.view_branch_only,
                                        }).FirstOrDefaultAsync();

                if (permission.per_sms_view != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                var searchBy = bodyDtParameters.searchText.Trim();
                var orderCriteria = "id";
                var orderAscendingDirection = true;

                if (bodyDtParameters.Order != null)
                {
                    orderCriteria = bodyDtParameters.Columns[bodyDtParameters.Order[0].Column].Data;
                    orderAscendingDirection = bodyDtParameters.Order[0].Dir.ToString().ToLower() == "asc";
                }

                var result = _context.view_send_sms.Where(x => x.member_id == jwtStatus.member_id).AsQueryable();

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
                                (r.etax_id.Contains(searchBy) && r.send_sms_finish >= bodyDtParameters.dateStart && r.send_sms_finish <= bodyDtParameters.dateEnd) ||
                                (r.raw_name.Contains(searchBy) && r.send_sms_finish >= bodyDtParameters.dateStart && r.send_sms_finish <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.send_sms_finish >= bodyDtParameters.dateStart && r.send_sms_finish <= bodyDtParameters.dateEnd)
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
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.send_sms_finish >= bodyDtParameters.dateStart && r.send_sms_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.send_sms_finish >= bodyDtParameters.dateStart && r.send_sms_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.send_sms_finish >= bodyDtParameters.dateStart && r.send_sms_finish <= bodyDtParameters.dateEnd)
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
                                (r.send_sms_finish >= bodyDtParameters.dateStart && r.send_sms_finish <= bodyDtParameters.dateEnd)
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
                                (r.document_type_id == document_id && r.send_sms_finish >= bodyDtParameters.dateStart && r.send_sms_finish <= bodyDtParameters.dateEnd)
                            );
                        }
                    }
                }

                if (bodyDtParameters.statusType1 != "")
                {
                    result = result.Where(r => r.send_sms_status == bodyDtParameters.statusType1);
                }

                if (bodyDtParameters.statusType2 != "")
                {
                    result = result.Where(r => r.open_sms_status == bodyDtParameters.statusType2);
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
                        x.document_type_name,
                        x.create_type,
                        x.raw_name,
                        x.name,
                        x.buyer_tel,
                        x.send_sms_status,
                        x.send_sms_finish,
                        x.open_sms_status,
                        x.open_sms_finish,
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
                        x.document_type_name,
                        x.create_type,
                        x.raw_name,
                        x.name,
                        x.buyer_tel,
                        x.send_sms_status,
                        x.send_sms_finish,
                        x.open_sms_status,
                        x.open_sms_finish,
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
        [Route("get_sms_detail/{id}")]
        public async Task<IActionResult> GetSmsDetail(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_sms_view).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                var sendSms = await _context.view_send_sms
                .Where(x => x.etax_file_id == id && x.member_id == jwtStatus.member_id)
                .Select(x => new
                {
                    x.id,
                    x.etax_file_id,
                    x.etax_id,
                    x.member_id,
                    x.branch_id,
                    x.document_type_id,
                    x.document_type_name,
                    x.create_type,
                    x.name,
                    x.raw_name,
                    x.send_sms_status,
                    x.send_sms_finish,
                    x.error,
                    x.url_path,
                })
                .FirstOrDefaultAsync();

                var member = await _context.members
                .Where(x => x.id == sendSms.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                })
                .FirstOrDefaultAsync();

                var branch = await _context.branchs
                .Where(x => x.id == sendSms.branch_id)
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
                .Where(x => x.id == sendSms.etax_file_id)
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

                if (sendSms != null && member != null && branch != null)
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
                            id = sendSms.id,
                            etax_file_id = sendSms.etax_file_id,
                            document_type_id = sendSms.document_type_id,
                            document_type_name = sendSms.document_type_name,
                            create_type = sendSms.create_type,
                            name = sendSms.name,
                            raw_name = sendSms.raw_name,
                            send_sms_status = sendSms.send_sms_status,
                            send_sms_finish = sendSms.send_sms_finish,
                            error = sendSms.error,
                            url_path = sendSms.url_path,
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
        [Route("get_sms_status/{id}")]
        public async Task<IActionResult> GetSmsStatus(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_sms_view).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


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
        [Route("get_sms_process/{id}")]
        public async Task<IActionResult> GetSmsProcess(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_sms_view).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                var sendSms = _context.send_sms
                .Where(x => x.id == id)
                .FirstOrDefault();

                if (sendSms != null)
                {
                    var etaxFiles = await _context.etax_files
                    .Where(x => x.id == sendSms.etax_file_id && x.member_id == jwtStatus.member_id)
                    .Select(x => new
                    {
                        x.id,
                        x.add_sms_status,
                        x.buyer_tel,
                    })
                    .FirstOrDefaultAsync();

                    var sendEmailAttachFiles = await _context.send_email_attach_files
                    .Where(x => x.etax_file_id == sendSms.etax_file_id)
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
                                send_sms_status = (etaxFiles.add_sms_status == "N" || etaxFiles.add_sms_status == null) ? false : true,
                                buyer_tel = etaxFiles.buyer_tel,
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
        [Route("update_sms_process")]
        public async Task<IActionResult> UpdateSmsProcess([FromBody] BodySms bodySms)
        {
            try
            {
                DateTime now = DateTime.Now;
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                try
                {
                    if (!Log.CheckLogSendSms(bodySms.etax_file_id.ToString(), now))
                    {
                        return StatusCode(400, new { message = "มีการส่งข้อมูลรายการเดิมซ้ำในเวลาเดียวกัน", });
                    }
                }
                catch { }

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_sms_manage).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                if (String.IsNullOrEmpty(bodySms.buyer_tel))
                    return StatusCode(400, new { message = "กรุณากำหนดเบอโทรศัพ", });

                if (bodySms.buyer_tel.Split(',').Length >= 10)
                    return StatusCode(400, new { message = "สามารถส่ง sms ได้สูงสุด 10 รายการ" });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var etaxfile = _context.etax_files
                        .Where(x => x.id == bodySms.etax_file_id)
                        .FirstOrDefault();

                        if (etaxfile != null)
                        {
                            etaxfile.buyer_tel = bodySms.buyer_tel;

                            var sendSms = _context.send_sms
                            .Where(x => x.etax_file_id == bodySms.etax_file_id)
                            .FirstOrDefault();

                            if (sendSms != null)
                            {
                                sendSms.send_sms_status = "pending";
                                sendSms.error = "";
                            }
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
        [Route("admin/get_sms_tabel")]
        public async Task<IActionResult> GetSendSmsDataTabelAdmin([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_sms_menu).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                var searchBy = bodyDtParameters.searchText.Trim();
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

                var result = _context.view_send_sms.Where(x => listDocumentTypeID.Contains(x.document_type_id)).AsQueryable();

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


                if (bodyDtParameters.fileGroup != null && bodyDtParameters.fileGroup != "")
                {
                    result = result.Where(x => x.group_name == bodyDtParameters.fileGroup);
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
                                (r.etax_id.Contains(searchBy) && r.send_sms_finish >= bodyDtParameters.dateStart && r.send_sms_finish <= bodyDtParameters.dateEnd) ||
                                (r.raw_name.Contains(searchBy) && r.send_sms_finish >= bodyDtParameters.dateStart && r.send_sms_finish <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.send_sms_finish >= bodyDtParameters.dateStart && r.send_sms_finish <= bodyDtParameters.dateEnd)
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
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.send_sms_finish >= bodyDtParameters.dateStart && r.send_sms_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.send_sms_finish >= bodyDtParameters.dateStart && r.send_sms_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.send_sms_finish >= bodyDtParameters.dateStart && r.send_sms_finish <= bodyDtParameters.dateEnd)
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
                                (r.send_sms_finish >= bodyDtParameters.dateStart && r.send_sms_finish <= bodyDtParameters.dateEnd)
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
                                (r.document_type_id == document_id && r.send_sms_finish >= bodyDtParameters.dateStart && r.send_sms_finish <= bodyDtParameters.dateEnd)
                            );
                        }
                    }
                }

                if (bodyDtParameters.statusType1 != "")
                {
                    result = result.Where(r => r.send_sms_status == bodyDtParameters.statusType1);
                }

                if (bodyDtParameters.statusType2 != "")
                {
                    result = result.Where(r => r.open_sms_status == bodyDtParameters.statusType2);
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
                        x.member_name,
                        x.document_type_id,
                        x.document_type_name,
                        x.create_type,
                        x.raw_name,
                        x.name,
                        x.buyer_tel,
                        x.send_sms_status,
                        x.send_sms_finish,
                        x.open_sms_status,
                        x.open_sms_finish,
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
                        x.member_name,
                        x.document_type_id,
                        x.document_type_name,
                        x.create_type,
                        x.raw_name,
                        x.name,
                        x.buyer_tel,
                        x.send_sms_status,
                        x.send_sms_finish,
                        x.open_sms_status,
                        x.open_sms_finish,
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
        [Route("admin/get_sms_detail/{id}")]
        public async Task<IActionResult> GetSmsDetailAdmin(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_sms_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                var sendSms = await _context.view_send_sms
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
                    x.send_sms_status,
                    x.send_sms_finish,
                    x.error,
                    x.url_path,
                })
                .FirstOrDefaultAsync();

                var membere = await (from um in _context.user_members
                                     where um.user_id == jwtStatus.user_id && um.member_id == sendSms.member_id
                                     select um).FirstOrDefaultAsync();

                if (membere == null)
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                var member = await _context.members
                .Where(x => x.id == sendSms.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                })
                .FirstOrDefaultAsync();

                var branch = await _context.branchs
                .Where(x => x.id == sendSms.branch_id)
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
                .Where(x => x.id == sendSms.etax_file_id)
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

                if (sendSms != null && member != null && branch != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            id = sendSms.id,
                            etax_file_id = sendSms.etax_file_id,
                            document_type_id = sendSms.document_type_id,
                            create_type = sendSms.create_type,
                            name = sendSms.name,
                            raw_name = sendSms.raw_name,
                            send_sms_status = sendSms.send_sms_status,
                            send_sms_finish = sendSms.send_sms_finish,
                            error = sendSms.error,
                            url_path = sendSms.url_path,
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
        [Route("admin/update_sms")]
        public async Task<IActionResult> UpdateSmsAdmin([FromBody] BodySms bodySms)
        {
            try
            {
                DateTime now = DateTime.Now;
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                try
                {
                    if (!Log.CheckLogSendSms(bodySms.etax_file_id.ToString(), now))
                    {
                        return StatusCode(400, new { message = "มีการส่งข้อมูลรายการเดิมซ้ำในเวลาเดียวกัน", });
                    }
                }
                catch { }

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_sms_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                if (String.IsNullOrEmpty(bodySms.buyer_tel))
                    return StatusCode(400, new { message = "กรุณากำหนดเบอร์โทรศัพท์", });

                if (bodySms.buyer_tel.Split(',').Length >= 10)
                    return StatusCode(400, new { message = "สามารถส่ง sms ได้สูงสุด 10 รายการ" });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var etaxfile = _context.etax_files
                        .Where(x => x.id == bodySms.etax_file_id)
                        .FirstOrDefault();

                        if (etaxfile != null)
                        {
                            var membere = await (from um in _context.user_members
                                                 where um.user_id == jwtStatus.user_id && um.member_id == etaxfile.member_id
                                                 select um).FirstOrDefaultAsync();

                            if (membere == null)
                                return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                            etaxfile.buyer_tel = bodySms.buyer_tel;

                            var sendSms = _context.send_sms
                            .Where(x => x.etax_file_id == bodySms.etax_file_id)
                            .FirstOrDefault();

                            if (sendSms != null)
                            {
                                sendSms.send_sms_status = "pending";
                                sendSms.error = null;
                            }
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
