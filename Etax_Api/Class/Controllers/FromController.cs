
using ExcelDataReader;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class FromController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public FromController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        [HttpPost]
        [Route("get_form_tabel")]
        public async Task<IActionResult> GetFormFilesDataTabel([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

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
        [Route("get_data_form_mk")]
        public async Task<IActionResult> GetSataForm_MK([FromBody] BodyMkData bodyMkData)
        {
            try
            {
                var branch = await (from b in _context.branchs
                                    where b.name.Contains(bodyMkData.branchCode)
                                    select new
                                    {
                                        b.id,
                                        b.member_id,
                                        b.name,
                                        b.branch_code,
                                        b.building_number,
                                        b.building_name,
                                        b.street_name,
                                        b.district_code,
                                        b.district_name,
                                        b.amphoe_code,
                                        b.amphoe_name,
                                        b.province_code,
                                        b.province_name,
                                        b.zipcode,
                                    }).FirstOrDefaultAsync();


                var member = await (from m in _context.members
                                    where m.group_name == "Mk" && m.id == branch.member_id
                                    select new
                                    {
                                        m.id,
                                        m.name,
                                        m.tax_id,
                                    }).FirstOrDefaultAsync();

                var etaxFile = await (from ef in _context.etax_files
                                      where ef.member_id == branch.member_id && ef.etax_id == bodyMkData.billID
                                      select new
                                      {
                                          ef.buyer_name,
                                          ef.buyer_tax_id,
                                          ef.buyer_address,
                                          ef.buyer_zipcode,
                                          ef.buyer_tel,
                                          ef.buyer_email,
                                          ef.create_date,
                                          ef.delete_status,
                                      }).FirstOrDefaultAsync();

                bool already = false;
                bool expiry = false;
                bool cancel = false;

                if (etaxFile != null)
                {
                    already = true;

                    DateTime expiryDate = DateTime.Now.AddDays(-7);
                    if (expiryDate > etaxFile.create_date)
                        expiry = true;

                    if (etaxFile.delete_status == 1)
                        cancel = true;
                }

                if (member != null && branch != null)
                {

                    string branch_name = "";
                    string[] branchNameArray = branch.name.Split('|');
                    foreach (string branchName in branchNameArray)
                    {
                        if (branchName.Contains(bodyMkData.branchCode))
                        {
                            branch_name = branchName;
                            break;
                        }
                    }

                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            member = member,
                            branch = new
                            {
                                branch.id,
                                branch.member_id,
                                name = branch_name,
                                branch.branch_code,
                                branch.building_number,
                                branch.building_name,
                                branch.street_name,
                                branch.district_code,
                                branch.district_name,
                                branch.amphoe_code,
                                branch.amphoe_name,
                                branch.province_code,
                                branch.province_name,
                                branch.zipcode,
                            },
                            etaxFile = etaxFile,
                            already = already,
                            expiry = expiry,
                            cancel = cancel,
                            totalText = Function.ThBahtText(bodyMkData.totalAmount),
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
        [Route("save_form_mk")]
        public async Task<IActionResult> SaveForm_MK([FromBody] BodyMkUserform bodyUserform)
        {
            try
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        DateTime now = DateTime.Now;

                        EtaxFile etaxFile = await _context.etax_files
                        .Where(x => x.member_id == bodyUserform.member_id && x.etax_id == bodyUserform.dataQr.billID)
                        .FirstOrDefaultAsync();

                        if (etaxFile == null)
                        {
                            EtaxFile newEtaxFile = new EtaxFile()
                            {
                                member_id = bodyUserform.member_id,
                                branch_id = bodyUserform.branch_id,
                                member_user_id = 0,
                                document_type_id = 7,
                                create_type = "api",
                                rawdata_file_id = 0,
                                gen_xml_status = "pending",
                                gen_pdf_status = "pending",
                                add_email_status = "pending",
                                add_sms_status = "no",
                                add_ebxml_status = "no",
                                send_xml_status = "N",
                                send_other_status = "N",
                                error = "",
                                output_path = _config["Path:Output"],
                                etax_id = bodyUserform.dataQr.billID,
                                buyer_branch_code = "00000",
                                issue_date = DateTime.ParseExact(bodyUserform.dataQr.bilIDate, "yyyyMMdd", CultureInfo.InvariantCulture),
                                buyer_id = "",
                                buyer_name = bodyUserform.name,
                                buyer_tax_id = bodyUserform.tax_id,
                                buyer_tax_type = "",
                                buyer_address = bodyUserform.address + " " + bodyUserform.district + " " + bodyUserform.amphoe + " " + bodyUserform.province,
                                buyer_zipcode = bodyUserform.zipcode,
                                buyer_tel = bodyUserform.tel,
                                buyer_fax = "",
                                buyer_country_code = "TH",
                                buyer_email = bodyUserform.email,
                                price = double.Parse(bodyUserform.dataQr.baseAmount),
                                discount = double.Parse(bodyUserform.dataQr.discount),
                                tax_rate = 7,
                                tax = double.Parse(bodyUserform.dataQr.vat),
                                total = double.Parse(bodyUserform.dataQr.totalAmount),
                                remark = "",
                                other = bodyUserform.dataQr.branchCode,
                                group_name = "",
                                template_pdf = "",
                                template_email = "",
                                xml_payment_status = "pending",
                                pdf_payment_status = "pending",
                                mode = "normal",
                                update_date = now,
                                create_date = now,
                            };
                            _context.Add(newEtaxFile);
                            await _context.SaveChangesAsync();

                            EtaxFileItem newEtaxFileItem = new EtaxFileItem()
                            {
                                etax_file_id = newEtaxFile.id,
                                code = "",
                                name = "อาหารและเครื่องดื่ม",
                                qty = 1,
                                unit = "",
                                price = newEtaxFile.price,
                                discount = newEtaxFile.discount,
                                tax = newEtaxFile.tax,
                                tax_rate = 7,
                                total = newEtaxFile.total,
                            };
                            _context.Add(newEtaxFileItem);
                        }
                        else
                        {
                            etaxFile.buyer_name = bodyUserform.name;
                            etaxFile.buyer_tax_id = bodyUserform.tax_id;
                            etaxFile.buyer_address = bodyUserform.address + " " + bodyUserform.district + " " + bodyUserform.amphoe + " " + bodyUserform.province;
                            etaxFile.buyer_zipcode = bodyUserform.zipcode;
                            etaxFile.buyer_tel = bodyUserform.tel;
                            etaxFile.buyer_email = bodyUserform.email;

                            etaxFile.gen_xml_status = "pending";
                            etaxFile.gen_pdf_status = "pending";
                            etaxFile.add_email_status = "pending";
                        }

                        EtaxFile etaxFileRef = await _context.etax_files
                        .Where(x => x.member_id == bodyUserform.member_id && x.etax_id == bodyUserform.dataQr.billIDRef)
                        .FirstOrDefaultAsync();

                        if (etaxFileRef != null)
                            etaxFileRef.delete_status = 1;


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
        [Route("cancel_form_mk")]
        public async Task<IActionResult> CancelForm_MK([FromBody] BodyMkCancel bodyCancel)
        {
            try
            {
                if (bodyCancel.cancelPassword != "12345")
                    return StatusCode(401, new { message = "รหัสลับในการลบใบกำกับภาษีไม่ถูกต้อง", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        EtaxFile etaxFile = await _context.etax_files
                        .Where(x => x.member_id == bodyCancel.member_id && x.etax_id == bodyCancel.billID)
                        .FirstOrDefaultAsync();


                        if (etaxFile != null)
                            etaxFile.delete_status = 1;


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

        //////////////////////////Admin//////////////////////////

    }
}
