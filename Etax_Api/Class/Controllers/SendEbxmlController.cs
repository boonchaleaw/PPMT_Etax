
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class SendEbxmlController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public SendEbxmlController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        [HttpPost]
        [Route("get_ebxml_tabel")]
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

                _context.Database.SetCommandTimeout(180);
                var result = _context.view_send_ebxml.Where(x => x.member_id == jwtStatus.member_id).AsQueryable();

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
                                (r.etax_id.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
                                (r.raw_name.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd)
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
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd)
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
                                (r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd)
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
                                (r.document_type_id == document_id && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd)
                            );
                        }
                    }
                }


                if (bodyDtParameters.statusType1 != "")
                {
                    result = result.Where(r => r.send_ebxml_status == bodyDtParameters.statusType1);
                }

                if (bodyDtParameters.statusType2 != "")
                {
                    result = result.Where(r => r.etax_status == bodyDtParameters.statusType2);
                }

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.CountAsync();
                var totalResultsCount = await _context.view_send_ebxml.Where(x => x.member_id == jwtStatus.member_id).CountAsync();

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
                        x.send_ebxml_status,
                        x.send_ebxml_finish,
                        x.etax_status,
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
                        x.send_ebxml_status,
                        x.send_ebxml_finish,
                        x.etax_status,
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
        [Route("get_ebxml_detail/{id}")]
        public async Task<IActionResult> GetPdfDataDetail(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var sendEbxml = await _context.view_send_ebxml
                .Where(x => x.id == id && x.member_id == jwtStatus.member_id)
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
                    x.send_ebxml_status,
                    x.send_ebxml_finish,
                    x.etax_status,
                    x.etax_status_finish,
                    x.error,
                    x.url_path,
                })
                .FirstOrDefaultAsync();

                var member = await _context.members
                .Where(x => x.id == sendEbxml.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                })
                .FirstOrDefaultAsync();

                var branch = await _context.branchs
                .Where(x => x.id == sendEbxml.branch_id)
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
                .Where(x => x.id == sendEbxml.etax_file_id)
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

                if (sendEbxml != null && member != null && branch != null)
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
                            id = sendEbxml.id,
                            etax_file_id = sendEbxml.etax_file_id,
                            document_type_id = sendEbxml.document_type_id,
                            document_type_name = sendEbxml.document_type_name,
                            create_type = sendEbxml.create_type,
                            name = sendEbxml.name,
                            raw_name = sendEbxml.raw_name,
                            send_ebxml_status = sendEbxml.send_ebxml_status,
                            send_ebxml_finish = sendEbxml.send_ebxml_finish,
                            etax_status = sendEbxml.etax_status,
                            etax_status_finish = sendEbxml.etax_status_finish,
                            error = sendEbxml.error,
                            url_path = sendEbxml.url_path,
                            etax_id = etax.etax_id,
                            issue_date = etax.issue_date,
                            ref_etax_id = etax.ref_etax_id,
                            ref_issue_date = etax.ref_issue_date,
                            buyer_id = etax.buyer_id,
                            buyer_branch_code = etax.buyer_branch_code,
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

        //////////////////////////Admin//////////////////////////

        [HttpPost]
        [Route("admin/get_ebxml_tabel")]
        public async Task<IActionResult> GetSendEbxmlDataTabelAdmin([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var searchBy = bodyDtParameters.Search?.Value;
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

                var result = _context.view_send_ebxml.Where(x => listDocumentTypeID.Contains(x.document_type_id)).AsQueryable();

                if (bodyDtParameters.member.Count() > 0)
                {
                    var memberIds = bodyDtParameters.member.Select(m => m.id).ToList();
                    result = result.Where(x => memberIds.Contains(x.member_id));
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
                                (r.etax_id.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
                                (r.raw_name.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd)
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
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd)
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
                                (r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd)
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
                                (r.document_type_id == document_id && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd)
                            );
                        }
                    }
                }


                if (bodyDtParameters.statusType1 != "")
                {
                    result = result.Where(r => r.send_ebxml_status == bodyDtParameters.statusType1);
                }

                if (bodyDtParameters.statusType2 != "")
                {
                    result = result.Where(r => r.etax_status == bodyDtParameters.statusType2);
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
                        x.send_ebxml_status,
                        x.send_ebxml_finish,
                        x.etax_status,
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
                        x.send_ebxml_status,
                        x.send_ebxml_finish,
                        x.etax_status,
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
        [Route("admin/get_ebxml_detail/{id}")]
        public async Task<IActionResult> GetPdfDataDetailAdmin(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var sendEbxml = await _context.view_send_ebxml
                .Where(x => x.id == id)
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
                    x.send_ebxml_status,
                    x.send_ebxml_finish,
                    x.etax_status,
                    x.etax_status_finish,
                    x.error,
                    x.url_path,
                })
                .FirstOrDefaultAsync();

                var member = await _context.members
                .Where(x => x.id == sendEbxml.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                })
                .FirstOrDefaultAsync();

                var branch = await _context.branchs
                .Where(x => x.id == sendEbxml.branch_id)
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
                .Where(x => x.id == sendEbxml.etax_file_id)
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

                if (sendEbxml != null && member != null && branch != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            id = sendEbxml.id,
                            etax_file_id = sendEbxml.etax_file_id,
                            document_type_id = sendEbxml.document_type_id,
                            document_type_name = sendEbxml.document_type_name,
                            create_type = sendEbxml.create_type,
                            name = sendEbxml.name,
                            raw_name = sendEbxml.raw_name,
                            send_ebxml_status = sendEbxml.send_ebxml_status,
                            send_ebxml_finish = sendEbxml.send_ebxml_finish,
                            etax_status = sendEbxml.etax_status,
                            etax_status_finish = sendEbxml.etax_status_finish,
                            error = sendEbxml.error,
                            url_path = sendEbxml.url_path,
                            etax_id = etax.etax_id,
                            issue_date = etax.issue_date,
                            ref_etax_id = etax.ref_etax_id,
                            ref_issue_date = etax.ref_issue_date,
                            buyer_id = etax.buyer_id,
                            buyer_branch_code = etax.buyer_branch_code,
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

    }
}
