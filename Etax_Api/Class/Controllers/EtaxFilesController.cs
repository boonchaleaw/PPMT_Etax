
using ExcelDataReader;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class EtaxFilesController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public EtaxFilesController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        [HttpPost]
        [Route("get_xml_tabel")]
        public async Task<IActionResult> GetXmlFilesDataTabel([FromBody] BodyDtParameters bodyDtParameters)
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

                List<int> listDocumentTypeID = _context.document_type
                    .Where(x => x.type == "etax")
                    .Select(x => x.id)
                    .ToList();

                var result = _context.view_etax_files_new.Where(x => x.member_id == jwtStatus.member_id && listDocumentTypeID.Contains(x.document_type_id) && x.delete_status == 0).AsQueryable();

                result = result.Where(r => r.mode != "form");

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
                        if (bodyDtParameters.statusType1 == "")
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
                                    (r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                        else
                        {
                            if (bodyDtParameters.dateType == "issue_date")
                            {
                                result = result.Where(r =>
                                    (r.gen_xml_status == bodyDtParameters.statusType1 && r.etax_id.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_xml_status == bodyDtParameters.statusType1 && r.raw_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_xml_status == bodyDtParameters.statusType1 && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                                );
                            }
                            else
                            {
                                result = result.Where(r =>
                                    (r.gen_xml_status == bodyDtParameters.statusType1 && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_xml_status == bodyDtParameters.statusType1 && r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_xml_status == bodyDtParameters.statusType1 && r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                    }
                    else
                    {
                        if (bodyDtParameters.statusType1 == "")
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
                                    (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                        else
                        {
                            if (bodyDtParameters.dateType == "issue_date")
                            {
                                result = result.Where(r =>
                                    (r.document_type_id == document_id && r.gen_xml_status == bodyDtParameters.statusType1 && r.etax_id.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.gen_xml_status == bodyDtParameters.statusType1 && r.raw_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.gen_xml_status == bodyDtParameters.statusType1 && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                                );
                            }
                            else
                            {
                                result = result.Where(r =>
                                    (r.document_type_id == document_id && r.gen_xml_status == bodyDtParameters.statusType1 && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.gen_xml_status == bodyDtParameters.statusType1 && r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.gen_xml_status == bodyDtParameters.statusType1 && r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                    }
                }
                else
                {
                    if (document_id == 0)
                    {
                        if (bodyDtParameters.statusType1 == "")
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
                                    (r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                        else
                        {
                            if (bodyDtParameters.dateType == "issue_date")
                            {
                                result = result.Where(r =>
                                    (r.gen_xml_status == bodyDtParameters.statusType1 && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                                );
                            }
                            else
                            {
                                result = result.Where(r =>
                                    (r.gen_xml_status == bodyDtParameters.statusType1 && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                    }
                    else
                    {
                        if (bodyDtParameters.statusType1 == "")
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
                                    (r.document_type_id == document_id && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                        else
                        {
                            if (bodyDtParameters.dateType == "issue_date")
                            {
                                result = result.Where(r =>
                                    (r.document_type_id == document_id && r.gen_xml_status == bodyDtParameters.statusType1 && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                                );
                            }
                            else
                            {
                                result = result.Where(r =>
                                    (r.document_type_id == document_id && r.gen_xml_status == bodyDtParameters.statusType1 && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                    }
                }

                foreach (ProcessType processType in bodyDtParameters.processType)
                {
                    if (processType.id == "pdf")
                        result = result.Where(r => r.gen_pdf_status == "success");
                    else if (processType.id == "email")
                        result = result.Where(r => r.add_email_status == "success");
                    else if (processType.id == "sms")
                        result = result.Where(r => r.add_sms_status == "success");
                    else if (processType.id == "rd")
                        result = result.Where(r => r.add_ebxml_status == "success");
                }


                List<string> listType = new List<string>();
                foreach (TaxType tax in bodyDtParameters.taxType)
                {
                    if (tax.id == "7")
                    {
                        listType.Add("VAT7");
                    }
                    else if (tax.id == "0")
                    {
                        listType.Add("VAT0");
                    }
                    else if (tax.id == "free")
                    {
                        listType.Add("FRE");
                    }
                    else if (tax.id == "no")
                    {
                        listType.Add("NO");
                    }
                }

                if (listType.Count > 0)
                {
                    result = result.Where(r => listType.Contains(r.tax_type_filter));
                }

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.Where(x => x.delete_status == 0).CountAsync();
                var totalResultsCount = await _context.view_etax_files_new.Where(x => x.member_id == jwtStatus.member_id && x.document_type_id <= 100 && x.delete_status == 0).CountAsync();

                if (bodyDtParameters.Length == -1)
                {
                    var data = await result
                  .Select(x => new
                  {
                      x.id,
                      x.member_id,
                      x.document_type_id,
                      x.create_type,
                      x.raw_name,
                      x.name,
                      x.etax_id,
                      x.gen_xml_status,
                      x.gen_xml_finish,
                      x.error,
                      x.original_price,
                      x.price,
                      x.tax,
                      x.total,
                      x.url_path,
                      x.gen_pdf_status,
                      x.add_email_status,
                      x.add_sms_status,
                      x.add_ebxml_status,
                      x.issue_date,
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
                       x.member_id,
                       x.document_type_id,
                       x.create_type,
                       x.raw_name,
                       x.name,
                       x.etax_id,
                       x.gen_xml_status,
                       x.gen_xml_finish,
                       x.error,
                       x.price,
                       x.tax,
                       x.total,
                       x.url_path,
                       x.gen_pdf_status,
                       x.add_email_status,
                       x.add_sms_status,
                       x.add_ebxml_status,
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
        [Route("get_add_xml_detail")]
        public async Task<IActionResult> GetAddXmlDetail()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var member = await _context.members
                .Where(x => x.id == jwtStatus.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                })
                .FirstOrDefaultAsync();

                var branchs = await _context.branchs
                .Where(x => x.member_id == jwtStatus.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.branch_code,
                    x.building_number,
                    x.building_name,
                    x.street_name,
                    x.district_code,
                    x.district_name,
                    x.amphoe_code,
                    x.amphoe_name,
                    x.province_code,
                    x.province_name,
                    x.zipcode
                })
                .ToListAsync();

                object document_types = await _context.view_member_document_type
                .Select(x => new
                {
                    x.member_id,
                    x.document_type_id,
                    x.document_type_name,
                })
                .Where(x => x.member_id == jwtStatus.member_id)
                .ToListAsync();


                if (member != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            member = member,
                            branchs = branchs,
                            document_types = document_types,
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
        [Route("get_pdf_tabel")]
        public async Task<IActionResult> GetPdfFilesDataTabel([FromBody] BodyDtParameters bodyDtParameters)
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

                var result = _context.view_etax_files_new.Where(x => x.member_id == jwtStatus.member_id && x.gen_pdf_status != "no" && x.delete_status == 0).AsQueryable();

                result = result.Where(r => r.mode != "form");

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
                        if (bodyDtParameters.statusType1 == "")
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
                                    (r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                        else
                        {
                            if (bodyDtParameters.dateType == "issue_date")
                            {
                                result = result.Where(r =>
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.etax_id.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.raw_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                                );
                            }
                            else
                            {
                                result = result.Where(r =>
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                    }
                    else
                    {
                        if (bodyDtParameters.statusType1 == "")
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
                                    (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                        else
                        {
                            if (bodyDtParameters.dateType == "issue_date")
                            {
                                result = result.Where(r =>
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.document_type_id == document_id && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                                );
                            }
                            else
                            {
                                result = result.Where(r =>
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.document_type_id == document_id && r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                    }
                }
                else
                {
                    if (document_id == 0)
                    {
                        if (bodyDtParameters.statusType1 == "")
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
                                    (r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                        else
                        {
                            if (bodyDtParameters.dateType == "issue_date")
                            {
                                result = result.Where(r =>
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                                );
                            }
                            else
                            {
                                result = result.Where(r =>
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                    }
                    else
                    {
                        if (bodyDtParameters.statusType1 == "")
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
                                    (r.document_type_id == document_id && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                        else
                        {
                            if (bodyDtParameters.dateType == "issue_date")
                            {
                                result = result.Where(r =>
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.document_type_id == document_id && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                                );
                            }
                            else
                            {
                                result = result.Where(r =>
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.document_type_id == document_id && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                    }
                }

                foreach (ProcessType processType in bodyDtParameters.processType)
                {
                    if (processType.id == "pdf")
                        result = result.Where(r => r.gen_pdf_status == "success");
                    else if (processType.id == "email")
                        result = result.Where(r => r.add_email_status == "success");
                    else if (processType.id == "sms")
                        result = result.Where(r => r.add_sms_status == "success");
                    else if (processType.id == "rd")
                        result = result.Where(r => r.add_ebxml_status == "success");
                }

                List<string> listType = new List<string>();
                foreach (TaxType tax in bodyDtParameters.taxType)
                {
                    if (tax.id == "7")
                    {
                        listType.Add("VAT7");
                    }
                    else if (tax.id == "0")
                    {
                        listType.Add("VAT0");
                    }
                    else if (tax.id == "free")
                    {
                        listType.Add("FRE");
                    }
                    else if (tax.id == "no")
                    {
                        listType.Add("NO");
                    }
                }

                if (listType.Count > 0)
                {
                    result = result.Where(r => listType.Contains(r.tax_type_filter));
                }

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.Where(x => x.delete_status == 0).CountAsync();
                var totalResultsCount = await _context.view_etax_files_new.Where(x => x.member_id == jwtStatus.member_id && x.gen_pdf_status != "no" && x.delete_status == 0).CountAsync();

                if (bodyDtParameters.Length == -1)
                {

                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.member_id,
                        x.document_type_id,
                        x.create_type,
                        x.raw_name,
                        x.name,
                        x.etax_id,
                        x.gen_pdf_status,
                        x.gen_pdf_finish,
                        x.error,
                        x.original_price,
                        x.price,
                        x.tax,
                        x.total,
                        x.url_path,
                        x.gen_xml_status,
                        x.add_email_status,
                        x.add_sms_status,
                        x.add_ebxml_status,
                        x.issue_date,
                    })
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        data = data
                    }); ;
                }
                else
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.member_id,
                        x.document_type_id,
                        x.create_type,
                        x.raw_name,
                        x.name,
                        x.etax_id,
                        x.gen_pdf_status,
                        x.gen_pdf_finish,
                        x.error,
                        x.price,
                        x.tax,
                        x.total,
                        x.url_path,
                        x.gen_xml_status,
                        x.add_email_status,
                        x.add_sms_status,
                        x.add_ebxml_status,
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
                    }); ;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("get_xml_detail/{id}")]
        public async Task<IActionResult> GetXmlDataDetail(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var xmlFile = await _context.view_etax_files
                .Where(x => x.id == id && x.member_id == jwtStatus.member_id)
                .Select(x => new
                {
                    x.id,
                    x.member_id,
                    x.member_user_id,
                    x.branch_id,
                    x.create_type,
                    x.document_type_id,
                    x.name,
                    x.raw_name,
                    x.gen_xml_status,
                    x.gen_xml_finish,
                    x.gen_pdf_status,
                    x.gen_pdf_finish,
                    x.error,
                    x.url_path,
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

                var member = await _context.members
                .Where(x => x.id == xmlFile.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                })
                .FirstOrDefaultAsync();

                var branch = await _context.branchs
                .Where(x => x.id == xmlFile.branch_id)
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
                .Where(x => x.id == xmlFile.member_user_id)
                .FirstOrDefaultAsync();

                if (xmlFile != null && member != null && branch != null && member_user != null)
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
                            id = xmlFile.id,
                            create_type = xmlFile.create_type,
                            document_type_id = xmlFile.document_type_id,
                            name = xmlFile.name,
                            raw_name = xmlFile.raw_name,
                            gen_xml_status = xmlFile.gen_xml_status,
                            gen_xml_finish = xmlFile.gen_xml_finish,
                            gen_pdf_status = xmlFile.gen_pdf_status,
                            gen_pdf_finish = xmlFile.gen_pdf_finish,
                            error = xmlFile.error,
                            url_path = xmlFile.url_path,
                            etax_id = xmlFile.etax_id,
                            issue_date = xmlFile.issue_date,
                            ref_etax_id = xmlFile.ref_etax_id,
                            ref_issue_date = xmlFile.ref_issue_date,
                            buyer_branch_code = xmlFile.buyer_branch_code,
                            buyer_id = xmlFile.buyer_id,
                            buyer_name = xmlFile.buyer_name,
                            buyer_tax_id = xmlFile.buyer_tax_id,
                            buyer_address = xmlFile.buyer_address,
                            buyer_tel = xmlFile.buyer_tel,
                            buyer_fax = xmlFile.buyer_fax,
                            buyer_email = xmlFile.buyer_email,
                            original_price = xmlFile.original_price,
                            price = xmlFile.price,
                            discount = xmlFile.discount,
                            tax = xmlFile.tax,
                            total = xmlFile.total,
                            member_user_name = member_user.first_name,
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
        [Route("get_pdf_detail/{id}")]
        public async Task<IActionResult> GetPdfDataDetail(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var pdfFile = await _context.view_etax_files
                .Where(x => x.id == id && x.member_id == jwtStatus.member_id)
                .Select(x => new
                {
                    x.id,
                    x.member_id,
                    x.member_user_id,
                    x.branch_id,
                    x.create_type,
                    x.document_type_id,
                    x.name,
                    x.raw_name,
                    x.gen_xml_status,
                    x.gen_xml_finish,
                    x.gen_pdf_status,
                    x.gen_pdf_finish,
                    x.error,
                    x.url_path,
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

                var member = await _context.members
                .Where(x => x.id == pdfFile.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                })
                .FirstOrDefaultAsync();

                var branch = await _context.branchs
                .Where(x => x.id == pdfFile.branch_id)
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
                .Where(x => x.id == pdfFile.member_user_id)
                .FirstOrDefaultAsync();

                if (pdfFile != null && member != null && branch != null && member_user != null)
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
                            id = pdfFile.id,
                            create_type = pdfFile.create_type,
                            document_type_id = pdfFile.document_type_id,
                            name = pdfFile.name,
                            raw_name = pdfFile.raw_name,
                            gen_xml_status = pdfFile.gen_xml_status,
                            gen_xml_finish = pdfFile.gen_xml_finish,
                            gen_pdf_status = pdfFile.gen_pdf_status,
                            gen_pdf_finish = pdfFile.gen_pdf_finish,
                            error = pdfFile.error,
                            url_path = pdfFile.url_path,
                            etax_id = pdfFile.etax_id,
                            issue_date = pdfFile.issue_date,
                            ref_etax_id = pdfFile.ref_etax_id,
                            ref_issue_date = pdfFile.ref_issue_date,
                            buyer_branch_code = pdfFile.buyer_branch_code,
                            buyer_id = pdfFile.buyer_id,
                            buyer_name = pdfFile.buyer_name,
                            buyer_tax_id = pdfFile.buyer_tax_id,
                            buye_address = pdfFile.buyer_address,
                            buyer_tel = pdfFile.buyer_tel,
                            buyer_fax = pdfFile.buyer_fax,
                            buyer_email = pdfFile.buyer_email,
                            original_price = pdfFile.original_price,
                            price = pdfFile.price,
                            discount = pdfFile.discount,
                            tax = pdfFile.tax,
                            total = pdfFile.total,
                            member_user_name = member_user.first_name,
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
        [Route("create_xml")]
        public async Task<IActionResult> CreateXml([FromBody] BodyCreateXml bodyCreateXml)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                if (bodyCreateXml.document_type == null)
                    return StatusCode(400, new { message = "กรุณากำหนดประเภทเอกสาร", });

                if (String.IsNullOrEmpty(bodyCreateXml.etax_id))
                    return StatusCode(400, new { message = "กรุณากำหนดหมายเลขเอกสาร", });

                if (bodyCreateXml.buyer_branch_code.Length != 5)
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสสาขาให้ถูกต้อง", });

                if (bodyCreateXml.issue_date == DateTime.MinValue)
                    return StatusCode(400, new { message = "กรุณากำหนดวันที่สร้างเอกสาร", });

                if (String.IsNullOrEmpty(bodyCreateXml.buyer_id))
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสผู้ซื้อ", });

                if (String.IsNullOrEmpty(bodyCreateXml.buyer_name))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อผู้ซื้อ", });

                if (String.IsNullOrEmpty(bodyCreateXml.buyer_tax_id))
                    return StatusCode(400, new { message = "กรุณากำหนดเลขประจําตัวผู้เสียภาษี", });

                if (String.IsNullOrEmpty(bodyCreateXml.buyer_building_number))
                    return StatusCode(400, new { message = "กรุณากำหนดเลขที่", });

                if (String.IsNullOrEmpty(bodyCreateXml.buyer_province_name))
                    return StatusCode(400, new { message = "กรุณากำหนดจังหวัด", });

                if (String.IsNullOrEmpty(bodyCreateXml.buyer_amphoe_name))
                    return StatusCode(400, new { message = "กรุณากำหนดแขวง/อำเภอ", });

                if (String.IsNullOrEmpty(bodyCreateXml.buyer_district_name))
                    return StatusCode(400, new { message = "กรุณากำหนดเขต/ตำบล", });

                if (String.IsNullOrEmpty(bodyCreateXml.buyer_zipcode))
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสไปรษณีย์", });

                int itemLine = 1;
                foreach (Item item in bodyCreateXml.items)
                {
                    if (String.IsNullOrEmpty(item.code))
                        return StatusCode(400, new { message = "กรุณากำหนดรหัสสินค้า รายการสินค้าที่ " + itemLine, });

                    if (String.IsNullOrEmpty(item.name))
                        return StatusCode(400, new { message = "กรุณากำหนดชื่อสินค้า รายการสินค้าที่ " + itemLine, });

                    if (item.qty <= 0)
                        return StatusCode(400, new { message = "กรุณากำหนดจำนวนสินค้า รายการสินค้าที่ " + itemLine, });

                    if (item.price <= 0)
                        return StatusCode(400, new { message = "กรุณากำหนดจำนวนเงิน รายการสินค้าที่ " + itemLine, });

                    if (item.tax <= 0)
                        return StatusCode(400, new { message = "กรุณากำหนดภาษี รายการสินค้าที่ " + itemLine, });

                    itemLine++;
                }

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        EtaxFile etaxFile = new EtaxFile()
                        {
                            member_id = jwtStatus.member_id,
                            branch_id = bodyCreateXml.branch.id,
                            member_user_id = jwtStatus.user_id,
                            document_type_id = bodyCreateXml.document_type.document_type_id,
                            create_type = "create",
                            gen_xml_status = "pending",
                            gen_pdf_status = (bodyCreateXml.gen_pdf_status == true) ? "pending" : "no",
                            add_email_status = (bodyCreateXml.send_email_status == true) ? "pending" : "no",
                            add_ebxml_status = (bodyCreateXml.send_ebxml_status == true) ? "pending" : "no",
                            output_path = _config["Path:Output"],
                            etax_id = bodyCreateXml.etax_id,
                            buyer_branch_code = bodyCreateXml.buyer_branch_code,
                            issue_date = bodyCreateXml.issue_date,
                            buyer_id = bodyCreateXml.buyer_id,
                            buyer_name = bodyCreateXml.buyer_name,
                            buyer_tax_id = bodyCreateXml.buyer_tax_id,
                            buyer_address = bodyCreateXml.buyer_building_number + " " + bodyCreateXml.buyer_building_name + " " + bodyCreateXml.buyer_street_name + " " + bodyCreateXml.buyer_district_name + " " + bodyCreateXml.buyer_amphoe_name + " " + bodyCreateXml.buyer_province_name + " " + bodyCreateXml.buyer_zipcode,
                            buyer_tel = bodyCreateXml.buyer_tel,
                            buyer_fax = bodyCreateXml.buyer_fax,
                            buyer_email = bodyCreateXml.buyer_email,
                            price = bodyCreateXml.net_price,
                            discount = bodyCreateXml.net_discount,
                            tax = bodyCreateXml.net_tax,
                            total = bodyCreateXml.net_total,
                            xml_payment_status = "pending",
                            pdf_payment_status = "pending",
                            other = "",
                            create_date = DateTime.Now,
                        };
                        _context.Add(etaxFile);
                        await _context.SaveChangesAsync();

                        foreach (Item item in bodyCreateXml.items)
                        {
                            _context.Add(new EtaxFileItem()
                            {
                                etax_file_id = etaxFile.id,
                                code = item.code,
                                name = item.name,
                                price = (double)item.price,
                                unit = (item.unit == null) ? "-" : item.unit,
                                discount = (item.discount == null) ? 0 : (double)item.discount,
                                tax = (double)item.tax,
                                total = (double)item.total,
                            });
                        }
                        await _context.SaveChangesAsync();

                        LogEtaxFile logEtaxFile = new LogEtaxFile()
                        {
                            member_id = jwtStatus.member_id,
                            user_modify_id = jwtStatus.user_id,
                            etax_id = etaxFile.id,
                            create_type = "web",
                            action_type = "create",
                            create_date = DateTime.Now,
                        };
                        _context.Add(logEtaxFile);
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
                    message = "อัพโหลดไฟล์ข้อมูลสำเร็จ",
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("get_etax_process_detail/{id}")]
        public async Task<IActionResult> GetProcessDetail(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var etaxFiles = await _context.etax_files
                .Where(x => x.id == id && x.member_id == jwtStatus.member_id)
                .Select(x => new
                {
                    x.id,
                    x.send_xml_status,
                    x.send_other_status,
                    x.gen_pdf_status,
                    x.add_email_status,
                    x.add_sms_status,
                    x.add_ebxml_status,
                    x.buyer_email,
                    x.buyer_tel,
                })
                .FirstOrDefaultAsync();

                var sendEmailAttachFiles = await _context.send_email_attach_files
                .Where(x => x.etax_file_id == id)
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
                            id = etaxFiles.id,
                            send_xml_status = (etaxFiles.send_xml_status == "N" || etaxFiles.send_xml_status == null) ? false : true,
                            send_other_status = (etaxFiles.send_other_status == "N" || etaxFiles.send_other_status == null) ? false : true,
                            gen_pdf_status = (etaxFiles.gen_pdf_status != "no") ? true : false,
                            send_email_status = (etaxFiles.add_email_status != "no") ? true : false,
                            send_sms_status = (etaxFiles.add_sms_status != "no") ? true : false,
                            send_ebxml_status = (etaxFiles.add_ebxml_status != "no") ? true : false,
                            buyer_email = etaxFiles.buyer_email,
                            buyer_tel = etaxFiles.buyer_tel,
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
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("update_etax_process")]
        public async Task<IActionResult> UpdateProcess([FromBody] BodyProcess bodyProcess)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                double size = 0;
                foreach (BodySendOtherFile of in bodyProcess.send_other_file)
                {
                    size += of.size;
                    if (of.name.Length > 100)
                        return StatusCode(400, new { message = "ชื่อไฟล์แนบมีความยาวเกิน 100 ตัวอักษร" });
                }

                if (size > 4.8)
                    return StatusCode(400, new { message = "ขนวดไฟล์แนบเกิน 5 MB" });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        EtaxFile etaxFile = await _context.etax_files
                        .Where(x => x.id == bodyProcess.id)
                        .FirstOrDefaultAsync();

                        if (etaxFile.gen_pdf_status == "no")
                            etaxFile.gen_pdf_status = (bodyProcess.gen_pdf_status) ? "pending" : "no";

                        if (etaxFile.add_email_status == "no")
                            etaxFile.add_email_status = (bodyProcess.send_email_status) ? "pending" : "no";

                        if (etaxFile.add_sms_status == "no")
                            etaxFile.add_sms_status = (bodyProcess.send_sms_status) ? "pending" : "no";

                        if (etaxFile.add_ebxml_status == "no")
                            etaxFile.add_ebxml_status = (bodyProcess.send_ebxml_status) ? "pending" : "no";

                        if (bodyProcess.send_other_status)
                        {

                            var sendEmailAttachFiles = await _context.send_email_attach_files
                            .Where(x => x.etax_file_id == bodyProcess.id)
                            .ToListAsync();
                            foreach (var f in sendEmailAttachFiles)
                            {
                                List<BodySendOtherFile> listWhere = bodyProcess.send_other_file.Where(x => x.name == f.file_name).ToList();
                                if (listWhere.Count() == 0)
                                    _context.send_email_attach_files.Remove(f);
                            }


                            foreach (BodySendOtherFile of in bodyProcess.send_other_file)
                            {
                                if (of.data != "" && of.data != null)
                                {
                                    string email_files = "/" + jwtStatus.member_id + "/email_files";
                                    Directory.CreateDirectory(_config["Path:Output"] + email_files);

                                    email_files += "/" + of.name;
                                    byte[] fileBytes = Convert.FromBase64String(of.data.Split(',')[1]);
                                    System.IO.File.WriteAllBytes(_config["Path:Output"] + email_files, fileBytes);


                                    var checkEmailAttachFiles = await _context.send_email_attach_files
                                    .Where(x => x.etax_file_id == bodyProcess.id && x.file_name == of.name)
                                    .FirstOrDefaultAsync();
                                    if (checkEmailAttachFiles == null)
                                    {
                                        SendEmailAttachFile sendEmailAttachFile = new SendEmailAttachFile()
                                        {
                                            etax_file_id = bodyProcess.id,
                                            file_name = of.name,
                                            file_path = email_files,
                                            file_size = Math.Round(of.size, 2),
                                            create_date = DateTime.Now,
                                        };
                                        _context.Add(sendEmailAttachFile);
                                    }
                                }
                            }

                            etaxFile.send_other_status = (bodyProcess.send_other_status) ? "Y" : "N";
                        }


                        etaxFile.send_xml_status = (bodyProcess.send_xml_status) ? "Y" : "N";
                        etaxFile.buyer_email = bodyProcess.buyer_email;
                        etaxFile.buyer_tel = bodyProcess.buyer_tel;
                        await _context.SaveChangesAsync();

                        if (etaxFile.create_type == "rawdata")
                        {
                            var rawdataFiles = _context.rawdata_files
                            .Where(x => x.id == etaxFile.rawdata_file_id)
                            .FirstOrDefault();

                            if (rawdataFiles == null)
                                return StatusCode(401, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                            if (rawdataFiles.gen_pdf_status == "no" || rawdataFiles.gen_pdf_status == "success")
                                rawdataFiles.gen_pdf_status = (bodyProcess.gen_pdf_status) ? "pending" : "no";
                            if (rawdataFiles.send_email_status == "no" || rawdataFiles.send_email_status == "success")
                                rawdataFiles.send_email_status = (bodyProcess.send_email_status) ? "pending" : "no";
                            if (rawdataFiles.send_sms_status == "no" || rawdataFiles.send_sms_status == "success")
                                rawdataFiles.send_sms_status = (bodyProcess.send_sms_status) ? "pending" : "no";
                            if (rawdataFiles.send_ebxml_status == "no" || rawdataFiles.send_ebxml_status == "success")
                                rawdataFiles.send_ebxml_status = (bodyProcess.send_ebxml_status) ? "pending" : "no";
                            await _context.SaveChangesAsync();
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
                    message = "แก้ไขข้อมูลสำเร็จ",
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("update_etax_process_all")]
        public async Task<IActionResult> UpdateProcessAll([FromBody] BodyProcessAll bodyProcessAll)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                foreach (BodyItemSelect select in bodyProcessAll.listSelect)
                {
                    if (select.select)
                    {
                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            try
                            {
                                EtaxFile etaxFile = await _context.etax_files
                                .Where(x => x.id == select.id)
                                .FirstOrDefaultAsync();

                                etaxFile.send_xml_status = (bodyProcessAll.send_xml_status) ? "Y" : "N";

                                if (etaxFile.gen_pdf_status == "no")
                                    etaxFile.gen_pdf_status = (bodyProcessAll.gen_pdf_status) ? "pending" : "no";

                                if (etaxFile.add_email_status == "no")
                                    etaxFile.add_email_status = (bodyProcessAll.send_email_status) ? "pending" : "no";

                                if (etaxFile.add_sms_status == "no")
                                    etaxFile.add_sms_status = (bodyProcessAll.send_sms_status) ? "pending" : "no";

                                if (etaxFile.add_ebxml_status == "no")
                                    etaxFile.add_ebxml_status = (bodyProcessAll.send_ebxml_status) ? "pending" : "no";

                                if (etaxFile.create_type == "rawdata")
                                {
                                    var rawdataFiles = _context.rawdata_files
                                    .Where(x => x.id == etaxFile.rawdata_file_id)
                                    .FirstOrDefault();

                                    if (rawdataFiles == null)
                                        return StatusCode(401, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                                    if (rawdataFiles.gen_pdf_status == "no")
                                        rawdataFiles.gen_pdf_status = (bodyProcessAll.gen_pdf_status) ? "pending" : "no";
                                    else if (rawdataFiles.gen_pdf_status == "success")
                                        rawdataFiles.gen_pdf_status = (bodyProcessAll.gen_pdf_status) ? "pending" : "success";

                                    if (rawdataFiles.send_email_status == "no")
                                        rawdataFiles.send_email_status = (bodyProcessAll.send_email_status) ? "pending" : "no";
                                    else if (rawdataFiles.send_email_status == "success")
                                        rawdataFiles.send_email_status = (bodyProcessAll.send_email_status) ? "pending" : "success";

                                    if (rawdataFiles.send_sms_status == "no")
                                        rawdataFiles.send_sms_status = (bodyProcessAll.send_sms_status) ? "pending" : "no";
                                    else if (rawdataFiles.send_sms_status == "success")
                                        rawdataFiles.send_sms_status = (bodyProcessAll.send_sms_status) ? "pending" : "success";

                                    if (rawdataFiles.send_ebxml_status == "no")
                                        rawdataFiles.send_ebxml_status = (bodyProcessAll.send_ebxml_status) ? "pending" : "no";
                                    else if (rawdataFiles.send_ebxml_status == "success")
                                        rawdataFiles.send_ebxml_status = (bodyProcessAll.send_ebxml_status) ? "pending" : "success";
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
        [Route("delete_etax/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        EtaxFile etaxFile = await _context.etax_files
                        .Where(x => x.id == id && x.member_id == jwtStatus.member_id)
                        .FirstOrDefaultAsync();

                        etaxFile.delete_status = 1;
                        await _context.SaveChangesAsync();

                        LogEtaxFile logEtaxFile = new LogEtaxFile()
                        {
                            member_id = jwtStatus.member_id,
                            user_modify_id = jwtStatus.user_id,
                            etax_id = etaxFile.id,
                            create_type = "web",
                            action_type = "delete",
                            create_date = DateTime.Now,
                        };
                        _context.Add(logEtaxFile);
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
        [Route("delete_etax_all")]
        public async Task<IActionResult> DeleteAll([FromBody] List<BodyItemSelect> listBodyItemSelect)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        foreach (BodyItemSelect select in listBodyItemSelect)
                        {
                            if (select.select)
                            {
                                EtaxFile etaxFile = await _context.etax_files
                            .Where(x => x.id == select.id)
                            .FirstOrDefaultAsync();

                                etaxFile.delete_status = 1;
                                await _context.SaveChangesAsync();

                                LogEtaxFile logEtaxFile = new LogEtaxFile()
                                {
                                    member_id = jwtStatus.member_id,
                                    user_modify_id = jwtStatus.user_id,
                                    etax_id = etaxFile.id,
                                    create_type = "web",
                                    action_type = "delete",
                                    create_date = DateTime.Now,
                                };
                                _context.Add(logEtaxFile);
                            }
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
        [Route("download_file_xml/{id}")]
        public async Task<IActionResult> DownloadFileXml(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                EtaxFile etaxFile = await _context.etax_files
                .Where(x => x.id == id && x.member_id == jwtStatus.member_id)
                .FirstOrDefaultAsync();

                if (etaxFile == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                DateTime now = DateTime.Now;
                string sharePath = "/" + etaxFile.member_id + "/" + Encryption.SHA256("path_" + now.ToString("dd-MM-yyyy")) + "/xml/" + etaxFile.name + ".xml";

                Function.DeleteFile(_config["Path:Share"]);
                Function.DeleteDirectory(_config["Path:Share"]);


                string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etaxFile.url_path + "/xml/" + etaxFile.name + ".xml", _config["Path:Mode"]);
                if (fileBase64 != "")
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_config["Path:Share"] + sharePath));
                    System.IO.File.WriteAllBytes(_config["Path:Share"] + sharePath, Convert.FromBase64String(fileBase64));
                }
                else
                    return StatusCode(400, new { message = "ไม่พบไฟล์ที่ต้องการ", });

                return StatusCode(200, new
                {
                    message = "โหลดข้อมูลสำเร็จ",
                    data = new
                    {
                        url = _config["Path:Url"] + sharePath,
                    },
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("download_file_pdf/{id}")]
        public async Task<IActionResult> DownloadFilePdf(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                EtaxFile etaxFile = await _context.etax_files
                .Where(x => x.id == id && x.member_id == jwtStatus.member_id)
                .FirstOrDefaultAsync();

                if (etaxFile == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                DateTime now = DateTime.Now;
                string sharePath = "/" + etaxFile.member_id + "/" + Encryption.SHA256("path_" + now.ToString("dd-MM-yyyy")) + "/pdf/" + etaxFile.name + ".pdf";

                Function.DeleteFile(_config["Path:Share"]);
                Function.DeleteDirectory(_config["Path:Share"]);

                string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etaxFile.url_path + "/pdf/" + etaxFile.name + ".pdf", _config["Path:Mode"]);
                if (fileBase64 != "")
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_config["Path:Share"] + sharePath));
                    System.IO.File.WriteAllBytes(_config["Path:Share"] + sharePath, Convert.FromBase64String(fileBase64));
                }
                else
                    return StatusCode(400, new { message = "ไม่พบไฟล์ที่ต้องการ", });

                //DateTime now = DateTime.Now;
                //string filePath = etaxFile.output_path + etaxFile.url_path + "/pdf/" + etaxFile.name + ".pdf";
                //string sharePath = "/" + etaxFile.member_id + "/" + Encryption.MD5("path_" + now.ToString("dd-MM-yyyy")) + "/pdf/" + etaxFile.name + ".pdf";

                //string[] files = Directory.GetFiles(_config["Path:Share"], "*", SearchOption.AllDirectories);
                //foreach (string file in files)
                //{
                //    FileInfo fi = new FileInfo(file);
                //    if (fi.CreationTime.AddDays(1) < now)
                //        System.IO.File.Delete(file);
                //}

                //if (System.IO.File.Exists(filePath))
                //{
                //    Directory.CreateDirectory(Path.GetDirectoryName(_config["Path:Share"] + sharePath));
                //    System.IO.File.Copy(filePath, _config["Path:Share"] + sharePath, true);
                //}
                //else
                //    return StatusCode(400, new { message = "ไม่พบไฟล์ที่ต้องการ", });

                return StatusCode(200, new
                {
                    message = "โหลดข้อมูลสำเร็จ",
                    data = new
                    {
                        url = _config["Path:Url"] + sharePath,
                    },
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("xml_file_zip_select")]
        public async Task<IActionResult> XmlFileZipSelect([FromBody] List<BodyItemSelect> listBodyItemSelect)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                List<ReturnFile> listFile = new List<ReturnFile>();

                foreach (BodyItemSelect bodyItemSelect in listBodyItemSelect)
                {
                    if (bodyItemSelect.select)
                    {
                        List<ViewEtaxFile> etaxFiles = await _context.view_etax_files
                        .Where(x => x.id == bodyItemSelect.id && x.member_id == jwtStatus.member_id && x.gen_xml_status == "success" && x.delete_status == 0)
                        .ToListAsync();

                        if (etaxFiles.Count > 0)
                        {
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
                        }
                    }
                }

                return StatusCode(200, new
                {
                    message = "โหลดข้อมูลสำเร็จ",
                    data = listFile,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("pdf_file_zip_select")]
        public async Task<IActionResult> PdfFileZipSelect([FromBody] List<BodyItemSelect> listBodyItemSelect)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                List<ReturnFile> listFile = new List<ReturnFile>();

                foreach (BodyItemSelect bodyItemSelect in listBodyItemSelect)
                {
                    if (bodyItemSelect.select)
                    {
                        List<ViewEtaxFile> etaxFiles = await _context.view_etax_files
                        .Where(x => x.id == bodyItemSelect.id && x.member_id == jwtStatus.member_id && x.gen_pdf_status == "success" && x.delete_status == 0)
                        .ToListAsync();

                        if (etaxFiles.Count > 0)
                        {
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
                        }
                    }
                }

                return StatusCode(200, new
                {
                    message = "โหลดข้อมูลสำเร็จ",
                    data = listFile,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("get_form_tabel")]
        public async Task<IActionResult> GetFormFilesDataTabel([FromBody] BodyDtParameters bodyDtParameters)
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

                var result = _context.view_etax_files.Where(x => x.member_id == jwtStatus.member_id && x.document_type_id <= 100 && x.delete_status == 0).AsQueryable();

                result = result.Where(r => r.mode == "form");

                if (bodyDtParameters.view_self_only)
                {
                    result = result.Where(r => r.member_user_id == jwtStatus.user_id);
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
                                (r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
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
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
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
                                (r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
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
                                (r.document_type_id == document_id && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                            );
                        }
                    }
                }

                List<string> listType = new List<string>();
                foreach (TaxType tax in bodyDtParameters.taxType)
                {
                    if (tax.id == "7")
                    {
                        listType.Add("VAT7");
                    }
                    else if (tax.id == "0")
                    {
                        listType.Add("VAT0");
                    }
                    else if (tax.id == "free")
                    {
                        listType.Add("FRE");
                    }
                    else if (tax.id == "no")
                    {
                        listType.Add("NO");
                    }
                }

                if (listType.Count > 0)
                {
                    result = result.Where(r => listType.Contains(r.tax_type_filter));
                }

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.Where(x => x.delete_status == 0).CountAsync();
                var totalResultsCount = await _context.view_etax_files.Where(x => x.member_id == jwtStatus.member_id && x.document_type_id <= 100 && x.delete_status == 0).CountAsync();

                if (bodyDtParameters.Length == -1)
                {
                    var data = await result
                  .Select(x => new
                  {
                      x.id,
                      x.member_id,
                      x.document_type_id,
                      x.create_type,
                      x.raw_name,
                      x.name,
                      x.etax_id,
                      x.gen_xml_status,
                      x.gen_xml_finish,
                      x.error,
                      x.original_price,
                      x.price,
                      x.tax,
                      x.total,
                      x.url_path,
                      x.gen_pdf_status,
                      x.add_email_status,
                      x.add_sms_status,
                      x.add_ebxml_status,
                      x.issue_date,
                      x.create_date,
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
                       x.member_id,
                       x.document_type_id,
                       x.create_type,
                       x.raw_name,
                       x.name,
                       x.etax_id,
                       x.gen_xml_status,
                       x.gen_xml_finish,
                       x.error,
                       x.price,
                       x.tax,
                       x.total,
                       x.url_path,
                       x.gen_pdf_status,
                       x.add_email_status,
                       x.add_sms_status,
                       x.add_ebxml_status,
                       x.issue_date,
                       x.create_date,
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
        [Route("get_etax_form/{code}")]
        public async Task<IActionResult> GetEtaxForm(string code)
        {
            try
            {

                var data = await _context.view_etax_files
                .Where(x => x.form_code == code)
                .Select(x => new
                {
                    x.id,
                    x.member_id,
                    x.member_user_id,
                    x.branch_id,
                    x.create_type,
                    x.document_type_id,
                    x.name,
                    x.raw_name,
                    x.gen_xml_status,
                    x.gen_xml_finish,
                    x.gen_pdf_status,
                    x.gen_pdf_finish,
                    x.error,
                    x.url_path,
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
                    x.mode,
                })
                .FirstOrDefaultAsync();

                if (data == null)
                {
                    return StatusCode(404, new
                    {
                        message = "ไม่พบข้อมูลที่ต้องการในระบบ",
                    });
                }

                var member = await _context.members
                .Where(x => x.id == data.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                })
                .FirstOrDefaultAsync();

                var branch = await _context.branchs
                .Where(x => x.id == data.branch_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.branch_code,
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


                var items = await _context.etax_file_items
                .Where(x => x.etax_file_id == data.id)
                .Select(x => new
                {
                    x.code,
                    x.name,
                    x.qty,
                    x.unit,
                    x.price,
                    x.discount,
                    x.tax,
                    x.total,
                })
                .ToListAsync();


                if (data != null && member != null && branch != null)
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
                            items = items,
                            id = data.id,
                            create_type = data.create_type,
                            document_type_id = data.document_type_id,
                            name = data.name,
                            raw_name = data.raw_name,
                            gen_xml_status = data.gen_xml_status,
                            gen_xml_finish = data.gen_xml_finish,
                            gen_pdf_status = data.gen_pdf_status,
                            gen_pdf_finish = data.gen_pdf_finish,
                            error = data.error,
                            url_path = data.url_path,
                            etax_id = data.etax_id,
                            issue_date = data.issue_date,
                            ref_etax_id = data.ref_etax_id,
                            ref_issue_date = data.ref_issue_date,
                            buyer_branch_code = data.buyer_branch_code,
                            buyer_id = data.buyer_id,
                            buyer_name = data.buyer_name,
                            buyer_tax_id = data.buyer_tax_id,
                            buye_address = data.buyer_address,
                            buyer_tel = data.buyer_tel,
                            buyer_fax = data.buyer_fax,
                            buyer_email = data.buyer_email,
                            original_price = data.original_price,
                            price = data.price,
                            discount = data.discount,
                            tax = data.tax,
                            total = data.total,
                            mode = data.mode,
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
        [Route("save_form")]
        public async Task<IActionResult> SaveForm([FromBody] BodyUserform bodyUserform)
        {
            try
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        EtaxFile etaxFile = await _context.etax_files
                        .Where(x => x.id == bodyUserform.id && x.form_code == bodyUserform.code)
                        .FirstOrDefaultAsync();

                        etaxFile.buyer_tax_id = bodyUserform.tax_id;
                        etaxFile.buyer_name = bodyUserform.name;
                        etaxFile.buyer_address = bodyUserform.address + " " + bodyUserform.zipcode;
                        etaxFile.buyer_email = bodyUserform.email;
                        etaxFile.buyer_tel = bodyUserform.tel;
                        etaxFile.mode = "normal";

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
        [Route("import_email")]
        public async Task<IActionResult> ImportEmail([FromBody] BodyImportEmail bodyImportEmail)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                byte[] bytes = Convert.FromBase64String(bodyImportEmail.email_file_data.Split(',')[1]);
                using (Stream stream = new MemoryStream(bytes))
                {
                    var encoding = Encoding.GetEncoding("UTF-8");
                    using (var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration() { FallbackEncoding = encoding }))
                    {
                        var conf = new ExcelDataSetConfiguration
                        {
                            ConfigureDataTable = _ => new ExcelDataTableConfiguration
                            {
                                UseHeaderRow = true
                            }
                        };

                        var dataSet = reader.AsDataSet(conf);
                        var dataTable = dataSet.Tables[0];

                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            try
                            {
                                foreach (DataRow row in dataTable.Rows)
                                {
                                    string tax_id = row.ItemArray[1].ToString();
                                    string email = row.ItemArray[2].ToString();

                                    var etax_files = await _context.etax_files
                                    .Where(x => x.buyer_tax_id == tax_id)
                                    .ToListAsync();

                                    foreach (EtaxFile etax_file in etax_files)
                                    {
                                        if (etax_file.buyer_email == "")
                                            etax_file.buyer_email = email;
                                    }
                                }

                                _context.SaveChanges();
                                transaction.Commit();

                                return StatusCode(200, new
                                {
                                    message = "นำเข้าข้อมูลสำเร็จ",
                                });
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                return StatusCode(400, new { message = ex.Message });
                            }
                        }
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

        //////////////////////////Admin//////////////////////////

        [HttpPost]
        [Route("admin/get_xml_tabel")]
        public async Task<IActionResult> GetXmlFilesDataTabelAdmin([FromBody] BodyDtParameters bodyDtParameters)
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

                List<int> listDocumentTypeID = _context.document_type
                 .Where(x => x.type == "etax")
                 .Select(x => x.id)
                 .ToList();

                var result = _context.view_etax_files.Where(x => listDocumentTypeID.Contains(x.document_type_id) && x.delete_status == 0).AsQueryable();

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

                bodyDtParameters.dateStart = DateTime.Parse(bodyDtParameters.dateStart.ToString()).Date;
                bodyDtParameters.dateEnd = bodyDtParameters.dateEnd.AddDays(+1).AddMilliseconds(-1);

                int document_id = System.Convert.ToInt32(bodyDtParameters.docType);

                if (!string.IsNullOrEmpty(searchBy))
                {
                    if (document_id == 0)
                    {
                        if (bodyDtParameters.statusType1 == "")
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
                                    (r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                        else
                        {
                            if (bodyDtParameters.dateType == "issue_date")
                            {
                                result = result.Where(r =>
                                    (r.gen_xml_status == bodyDtParameters.statusType1 && r.etax_id.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_xml_status == bodyDtParameters.statusType1 && r.raw_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_xml_status == bodyDtParameters.statusType1 && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                                );
                            }
                            else
                            {
                                result = result.Where(r =>
                                    (r.gen_xml_status == bodyDtParameters.statusType1 && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_xml_status == bodyDtParameters.statusType1 && r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_xml_status == bodyDtParameters.statusType1 && r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                    }
                    else
                    {
                        if (bodyDtParameters.statusType1 == "")
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
                                    (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                        else
                        {
                            if (bodyDtParameters.dateType == "issue_date")
                            {
                                result = result.Where(r =>
                                    (r.document_type_id == document_id && r.gen_xml_status == bodyDtParameters.statusType1 && r.etax_id.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.gen_xml_status == bodyDtParameters.statusType1 && r.raw_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.gen_xml_status == bodyDtParameters.statusType1 && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                                );
                            }
                            else
                            {
                                result = result.Where(r =>
                                    (r.document_type_id == document_id && r.gen_xml_status == bodyDtParameters.statusType1 && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.gen_xml_status == bodyDtParameters.statusType1 && r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.gen_xml_status == bodyDtParameters.statusType1 && r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                    }
                }
                else
                {
                    if (document_id == 0)
                    {
                        if (bodyDtParameters.statusType1 == "")
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
                                    (r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                        else
                        {
                            if (bodyDtParameters.dateType == "issue_date")
                            {
                                result = result.Where(r =>
                                    (r.gen_xml_status == bodyDtParameters.statusType1 && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                                );
                            }
                            else
                            {
                                result = result.Where(r =>
                                    (r.gen_xml_status == bodyDtParameters.statusType1 && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                    }
                    else
                    {
                        if (bodyDtParameters.statusType1 == "")
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
                                    (r.document_type_id == document_id && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                        else
                        {
                            if (bodyDtParameters.dateType == "issue_date")
                            {
                                result = result.Where(r =>
                                    (r.document_type_id == document_id && r.gen_xml_status == bodyDtParameters.statusType1 && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                                );
                            }
                            else
                            {
                                result = result.Where(r =>
                                    (r.document_type_id == document_id && r.gen_xml_status == bodyDtParameters.statusType1 && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                    }
                }

                foreach (ProcessType processType in bodyDtParameters.processType)
                {
                    if (processType.id == "pdf")
                        result = result.Where(r => r.gen_pdf_status == "success");
                    else if (processType.id == "email")
                        result = result.Where(r => r.add_email_status == "success");
                    else if (processType.id == "sms")
                        result = result.Where(r => r.add_sms_status == "success");
                    else if (processType.id == "rd")
                        result = result.Where(r => r.add_ebxml_status == "success");
                }

                List<string> listType = new List<string>();
                foreach (TaxType tax in bodyDtParameters.taxType)
                {
                    if (tax.id == "7")
                    {
                        listType.Add("VAT7");
                    }
                    else if (tax.id == "0")
                    {
                        listType.Add("VAT0");
                    }
                    else if (tax.id == "free")
                    {
                        listType.Add("FRE");
                    }
                    else if (tax.id == "no")
                    {
                        listType.Add("NO");
                    }
                }

                if (listType.Count > 0)
                {
                    result = result.Where(r => listType.Contains(r.tax_type_filter));
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
                      x.member_id,
                      x.document_type_id,
                      x.create_type,
                      x.raw_name,
                      x.name,
                      x.etax_id,
                      x.gen_xml_status,
                      x.gen_xml_finish,
                      x.error,
                      x.original_price,
                      x.price,
                      x.tax,
                      x.total,
                      x.url_path,
                      x.gen_pdf_status,
                      x.add_email_status,
                      x.add_sms_status,
                      x.add_ebxml_status,
                      x.issue_date,
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
                       x.member_id,
                       x.document_type_id,
                       x.create_type,
                       x.raw_name,
                       x.name,
                       x.etax_id,
                       x.gen_xml_status,
                       x.gen_xml_finish,
                       x.error,
                       x.price,
                       x.tax,
                       x.total,
                       x.url_path,
                       x.gen_pdf_status,
                       x.add_email_status,
                       x.add_sms_status,
                       x.add_ebxml_status,
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
        [Route("admin/get_xml_detail/{id}")]
        public async Task<IActionResult> GetXmlDataDetailAdmin(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var xmlFile = await _context.view_etax_files
                .Where(x => x.id == id)
                .Select(x => new
                {
                    x.id,
                    x.member_id,
                    x.branch_id,
                    x.create_type,
                    x.document_type_id,
                    x.name,
                    x.raw_name,
                    x.gen_xml_status,
                    x.gen_xml_finish,
                    x.gen_pdf_status,
                    x.gen_pdf_finish,
                    x.error,
                    x.url_path,
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

                var member = await _context.members
                .Where(x => x.id == xmlFile.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                })
                .FirstOrDefaultAsync();

                var branch = await _context.branchs
                .Where(x => x.id == xmlFile.branch_id)
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

                if (xmlFile != null && member != null && branch != null)
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
                            create_type = xmlFile.create_type,
                            document_type_id = xmlFile.document_type_id,
                            name = xmlFile.name,
                            raw_name = xmlFile.raw_name,
                            gen_xml_status = xmlFile.gen_xml_status,
                            gen_xml_finish = xmlFile.gen_xml_finish,
                            gen_pdf_status = xmlFile.gen_pdf_status,
                            gen_pdf_finish = xmlFile.gen_pdf_finish,
                            error = xmlFile.error,
                            url_path = xmlFile.url_path,
                            etax_id = xmlFile.etax_id,
                            issue_date = xmlFile.issue_date,
                            ref_etax_id = xmlFile.ref_etax_id,
                            ref_issue_date = xmlFile.ref_issue_date,
                            buyer_branch_code = xmlFile.buyer_branch_code,
                            buyer_id = xmlFile.buyer_id,
                            buyer_name = xmlFile.buyer_name,
                            buyer_tax_id = xmlFile.buyer_tax_id,
                            buyer_address = xmlFile.buyer_address,
                            buyer_tel = xmlFile.buyer_tel,
                            buyer_fax = xmlFile.buyer_fax,
                            buyer_email = xmlFile.buyer_email,
                            original_price = xmlFile.original_price,
                            price = xmlFile.price,
                            discount = xmlFile.discount,
                            tax = xmlFile.tax,
                            total = xmlFile.total,
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
        [Route("admin/get_pdf_tabel")]
        public async Task<IActionResult> GetPdfFilesDataTabelAdmin([FromBody] BodyDtParameters bodyDtParameters)
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
                                                      select td.id
                                                      ).ToListAsync();

                var result = _context.view_etax_files.Where(x => listDocumentTypeID.Contains(x.document_type_id) && x.delete_status == 0).AsQueryable();

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


                bodyDtParameters.dateStart = DateTime.Parse(bodyDtParameters.dateStart.ToString()).Date;
                bodyDtParameters.dateEnd = bodyDtParameters.dateEnd.AddDays(+1).AddMilliseconds(-1);


                int document_id = System.Convert.ToInt32(bodyDtParameters.docType);

                if (!string.IsNullOrEmpty(searchBy))
                {
                    if (document_id == 0)
                    {
                        if (bodyDtParameters.statusType1 == "")
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
                                    (r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                        else
                        {
                            if (bodyDtParameters.dateType == "issue_date")
                            {
                                result = result.Where(r =>
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.etax_id.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.raw_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                                );
                            }
                            else
                            {
                                result = result.Where(r =>
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                    }
                    else
                    {
                        if (bodyDtParameters.statusType1 == "")
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
                                    (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                        else
                        {
                            if (bodyDtParameters.dateType == "issue_date")
                            {
                                result = result.Where(r =>
                                    (r.document_type_id == document_id && r.gen_pdf_status == bodyDtParameters.statusType1 && r.etax_id.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.gen_pdf_status == bodyDtParameters.statusType1 && r.raw_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.gen_pdf_status == bodyDtParameters.statusType1 && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                                );
                            }
                            else
                            {
                                result = result.Where(r =>
                                    (r.document_type_id == document_id && r.gen_pdf_status == bodyDtParameters.statusType1 && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.gen_pdf_status == bodyDtParameters.statusType1 && r.raw_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                    (r.document_type_id == document_id && r.gen_pdf_status == bodyDtParameters.statusType1 && r.name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                    }
                }
                else
                {
                    if (document_id == 0)
                    {
                        if (bodyDtParameters.statusType1 == "")
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
                                    (r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                        else
                        {
                            if (bodyDtParameters.dateType == "issue_date")
                            {
                                result = result.Where(r =>
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                                );
                            }
                            else
                            {
                                result = result.Where(r =>
                                    (r.gen_pdf_status == bodyDtParameters.statusType1 && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                    }
                    else
                    {
                        if (bodyDtParameters.statusType1 == "")
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
                                    (r.document_type_id == document_id && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                        else
                        {
                            if (bodyDtParameters.dateType == "issue_date")
                            {
                                result = result.Where(r =>
                                    (r.document_type_id == document_id && r.gen_pdf_status == bodyDtParameters.statusType1 && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                                );
                            }
                            else
                            {
                                result = result.Where(r =>
                                    (r.document_type_id == document_id && r.gen_pdf_status == bodyDtParameters.statusType1 && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                                );
                            }
                        }
                    }
                }

                foreach (ProcessType processType in bodyDtParameters.processType)
                {
                    if (processType.id == "pdf")
                        result = result.Where(r => r.gen_pdf_status == "success");
                    else if (processType.id == "email")
                        result = result.Where(r => r.add_email_status == "success");
                    else if (processType.id == "sms")
                        result = result.Where(r => r.add_sms_status == "success");
                    else if (processType.id == "rd")
                        result = result.Where(r => r.add_ebxml_status == "success");
                }

                List<string> listType = new List<string>();
                foreach (TaxType tax in bodyDtParameters.taxType)
                {
                    if (tax.id == "7")
                    {
                        listType.Add("VAT7");
                    }
                    else if (tax.id == "0")
                    {
                        listType.Add("VAT0");
                    }
                    else if (tax.id == "free")
                    {
                        listType.Add("FRE");
                    }
                    else if (tax.id == "no")
                    {
                        listType.Add("NO");
                    }
                }

                if (listType.Count > 0)
                {
                    result = result.Where(r => listType.Contains(r.tax_type_filter));
                }

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.Where(x => x.delete_status == 0).CountAsync();
                var totalResultsCount = 0;

                if (bodyDtParameters.Length == -1)
                {

                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.member_id,
                        x.document_type_id,
                        x.create_type,
                        x.raw_name,
                        x.name,
                        x.etax_id,
                        x.gen_pdf_status,
                        x.gen_pdf_finish,
                        x.error,
                        x.original_price,
                        x.price,
                        x.tax,
                        x.total,
                        x.url_path,
                        x.gen_xml_status,
                        x.add_email_status,
                        x.add_sms_status,
                        x.add_ebxml_status,
                        x.issue_date,
                    })
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        data = data
                    }); ;
                }
                else
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.member_id,
                        x.document_type_id,
                        x.create_type,
                        x.raw_name,
                        x.name,
                        x.etax_id,
                        x.gen_pdf_status,
                        x.gen_pdf_finish,
                        x.error,
                        x.price,
                        x.tax,
                        x.total,
                        x.url_path,
                        x.gen_xml_status,
                        x.add_email_status,
                        x.add_sms_status,
                        x.add_ebxml_status,
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
                    }); ;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("admin/get_pdf_detail/{id}")]
        public async Task<IActionResult> GetPdfDataDetailAdmin(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var pdfFile = await _context.view_etax_files
                .Where(x => x.id == id)
                .Select(x => new
                {
                    x.id,
                    x.member_id,
                    x.branch_id,
                    x.create_type,
                    x.document_type_id,
                    x.name,
                    x.raw_name,
                    x.gen_xml_status,
                    x.gen_xml_finish,
                    x.gen_pdf_status,
                    x.gen_pdf_finish,
                    x.error,
                    x.url_path,
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

                var member = await _context.members
                .Where(x => x.id == pdfFile.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                })
                .FirstOrDefaultAsync();

                var branch = await _context.branchs
                .Where(x => x.id == pdfFile.branch_id)
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

                if (pdfFile != null && member != null && branch != null)
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
                            create_type = pdfFile.create_type,
                            document_type_id = pdfFile.document_type_id,
                            name = pdfFile.name,
                            raw_name = pdfFile.raw_name,
                            gen_xml_status = pdfFile.gen_xml_status,
                            gen_xml_finish = pdfFile.gen_xml_finish,
                            gen_pdf_status = pdfFile.gen_pdf_status,
                            gen_pdf_finish = pdfFile.gen_pdf_finish,
                            error = pdfFile.error,
                            url_path = pdfFile.url_path,
                            etax_id = pdfFile.etax_id,
                            issue_date = pdfFile.issue_date,
                            ref_etax_id = pdfFile.ref_etax_id,
                            ref_issue_date = pdfFile.ref_issue_date,
                            buyer_branch_code = pdfFile.buyer_branch_code,
                            buyer_id = pdfFile.buyer_id,
                            buyer_name = pdfFile.buyer_name,
                            buyer_tax_id = pdfFile.buyer_tax_id,
                            buyer_address = pdfFile.buyer_address,
                            buyer_tel = pdfFile.buyer_tel,
                            buyer_fax = pdfFile.buyer_fax,
                            buyer_email = pdfFile.buyer_email,
                            original_price = pdfFile.original_price,
                            price = pdfFile.price,
                            discount = pdfFile.discount,
                            tax = pdfFile.tax,
                            total = pdfFile.total,
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
        [Route("admin/delete_etaxfile/{id}")]
        public async Task<IActionResult> DeleteEtaxFileAdmin(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var etax_file = await _context.etax_files
                        .Where(x => x.id == id)
                        .FirstOrDefaultAsync();

                        if (etax_file == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        DateTime now = DateTime.Now;
                        etax_file.delete_status = 1;
                        etax_file.update_date = now;

                        _context.Update(etax_file);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "ลบข้อมูลสำเร็จ",
                        });
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
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("admin/renew_pdffile/{id}")]
        public async Task<IActionResult> RenewPdfFileAdmin(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var etax_file = await _context.etax_files
                        .Where(x => x.id == id)
                        .FirstOrDefaultAsync();

                        if (etax_file == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        DateTime now = DateTime.Now;

                        etax_file.gen_pdf_status = "pending";
                        etax_file.update_date = now;
                        _context.Update(etax_file);

                        if (etax_file.create_type == "rawdata")
                        {
                            var rawdata_files = await _context.rawdata_files
                           .Where(x => x.id == etax_file.rawdata_file_id)
                           .FirstOrDefaultAsync();

                            rawdata_files.gen_pdf_status = "pending";
                            _context.Update(rawdata_files);
                        }
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "สร้างใหม่สำเร็จ",
                        });
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
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("admin/get_etax_process_detail/{id}")]
        public async Task<IActionResult> GetProcessDetailAdmin(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var etaxFiles = await _context.etax_files
                .Where(x => x.id == id)
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

                if (etaxFiles != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            id = etaxFiles.id,
                            send_xml_status = (etaxFiles.send_xml_status == "N" || etaxFiles.send_xml_status == null) ? false : true,
                            send_other_status = (etaxFiles.send_other_status == "N" || etaxFiles.send_other_status == null) ? false : true,
                            gen_pdf_status = (etaxFiles.gen_pdf_status != "no") ? true : false,
                            send_email_status = (etaxFiles.add_email_status != "no") ? true : false,
                            send_ebxml_status = (etaxFiles.add_ebxml_status != "no") ? true : false,
                            buyer_email = etaxFiles.buyer_email,
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
        [Route("admin/update_etax_process")]
        public async Task<IActionResult> UpdateProcessAdmin([FromBody] BodyProcess bodyProcess)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        EtaxFile etaxFile = await _context.etax_files
                        .Where(x => x.id == bodyProcess.id)
                        .FirstOrDefaultAsync();

                        etaxFile.send_xml_status = (bodyProcess.send_xml_status) ? "Y" : "N";

                        if (etaxFile.gen_pdf_status == "no")
                            etaxFile.gen_pdf_status = (bodyProcess.gen_pdf_status) ? "pending" : "no";

                        if (etaxFile.add_email_status == "no")
                            etaxFile.add_email_status = (bodyProcess.send_email_status) ? "pending" : "no";

                        if (etaxFile.add_ebxml_status == "no")
                            etaxFile.add_ebxml_status = (bodyProcess.send_ebxml_status) ? "pending" : "no";

                        etaxFile.buyer_email = bodyProcess.buyer_email;
                        await _context.SaveChangesAsync();

                        if (etaxFile.create_type == "rawdata")
                        {
                            var rawdataFiles = _context.rawdata_files
                            .Where(x => x.id == etaxFile.rawdata_file_id)
                            .FirstOrDefault();

                            if (rawdataFiles == null)
                                return StatusCode(401, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                            if (rawdataFiles.gen_pdf_status == "no" || rawdataFiles.gen_pdf_status == "success")
                                rawdataFiles.gen_pdf_status = (bodyProcess.gen_pdf_status) ? "pending" : "no";
                            if (rawdataFiles.send_email_status == "no" || rawdataFiles.send_email_status == "success")
                                rawdataFiles.send_email_status = (bodyProcess.send_email_status) ? "pending" : "no";
                            if (rawdataFiles.send_ebxml_status == "no" || rawdataFiles.send_ebxml_status == "success")
                                rawdataFiles.send_ebxml_status = (bodyProcess.send_ebxml_status) ? "pending" : "no";
                            await _context.SaveChangesAsync();
                        }

                        LogEtaxFile logEtaxFile = new LogEtaxFile()
                        {
                            member_id = jwtStatus.member_id,
                            user_modify_id = jwtStatus.user_id,
                            etax_id = etaxFile.id,
                            create_type = "web",
                            action_type = "update",
                            create_date = DateTime.Now,
                        };
                        _context.Add(logEtaxFile);
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

    }
}
