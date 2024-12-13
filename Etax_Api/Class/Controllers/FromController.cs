
using Etax_Api.Class.MocApi;
using ExcelDataReader;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class FromController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private Regex rg = new Regex(@"[0-9]");
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
                DateTime now = DateTime.Now;
                DateTime expiryDate = now.AddDays(-7).Date;
                DateTime bilIDate = DateTime.ParseExact(bodyMkData.bilIDate, "yyyyMMdd", CultureInfo.InvariantCulture);

                if (expiryDate >= bilIDate)
                    return StatusCode(404, new { message = "รายการซื้อขายเกิน 7 วัน ไม่สามารถออกใบกำกับภาษีอิเล็กทรอนิคได้", });



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

                if (branch == null)
                    return StatusCode(404, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                var member = await (from m in _context.members
                                    where m.group_name == "Mk" && m.id == branch.member_id
                                    select new
                                    {
                                        m.id,
                                        m.name,
                                        m.tax_id,
                                    }).FirstOrDefaultAsync();



                if (member.tax_id == "0105562032146")
                {
                    string billID = bodyMkData.billID.Substring(3);
                    string billIDRef = "";
                    if (bodyMkData.billIDRef.Length > 1)
                        billIDRef = bodyMkData.billIDRef.Substring(3);

                    string TextChecksum_X = bodyMkData.branchCode + billID + bodyMkData.amount + bodyMkData.noDiscount + bodyMkData.fAndB + bodyMkData.service;
                    string TextChecksum_Y = bodyMkData.bilIDate + billID + bodyMkData.discount + bodyMkData.totalAmount + bodyMkData.vat + bodyMkData.baseAmount + billIDRef;

                    string dataX = "";
                    string dataY = "";
                    MatchCollection matchX = rg.Matches(TextChecksum_X);
                    if (matchX.Count > 0)
                    {
                        List<int> listData = new List<int>();
                        foreach (Match match in matchX)
                        {
                            dataX += match.Value;
                        }
                    }

                    MatchCollection matchY = rg.Matches(TextChecksum_Y);
                    if (matchX.Count > 0)
                    {
                        List<int> listData = new List<int>();
                        foreach (Match match in matchY)
                        {
                            dataY += match.Value;
                        }

                    }

                    string checksum = Verhoeff_checksum.generateVerhoeff(dataX) + Verhoeff_checksum.generateVerhoeff(dataY);
                    if (checksum != bodyMkData.checkSum)
                        return StatusCode(404, new
                        {
                            message = "รูปแบบบข้อมูลไม่ถูกต้อง",
                        });
                }
                else
                {
                    string TextChecksum_X = bodyMkData.branchCode + bodyMkData.billID.Split('-').Last() + bodyMkData.amount + bodyMkData.noDiscount + bodyMkData.fAndB + bodyMkData.service;
                    string TextChecksum_Y = bodyMkData.bilIDate + bodyMkData.billID.Split('-').Last() + bodyMkData.discount + bodyMkData.totalAmount + bodyMkData.vat + bodyMkData.baseAmount + bodyMkData.billIDRef.Split('-').Last();

                    string dataX = "";
                    string dataY = "";
                    MatchCollection matchX = rg.Matches(TextChecksum_X);
                    if (matchX.Count > 0)
                    {
                        List<int> listData = new List<int>();
                        foreach (Match match in matchX)
                        {
                            dataX += match.Value;
                        }
                    }

                    MatchCollection matchY = rg.Matches(TextChecksum_Y);
                    if (matchX.Count > 0)
                    {
                        List<int> listData = new List<int>();
                        foreach (Match match in matchY)
                        {
                            dataY += match.Value;
                        }

                    }

                    string checksum = Verhoeff_checksum.generateVerhoeff(dataX) + Verhoeff_checksum.generateVerhoeff(dataY);
                    if (checksum != bodyMkData.checkSum)
                        return StatusCode(404, new
                        {
                            message = "รูปแบบบข้อมูลไม่ถูกต้อง",
                        });

                }


                var etaxFile = await
                    (from ef in _context.etax_files
                     where ef.member_id == branch.member_id && ef.ref_etax_id == bodyMkData.billID
                     select new
                     {
                         ef.etax_id,
                         ef.buyer_branch_code,
                         ef.buyer_name,
                         ef.buyer_tax_id,
                         ef.buyer_address,
                         ef.buyer_zipcode,
                         ef.buyer_tel,
                         ef.buyer_email,
                         ef.issue_date,
                         ef.ref_issue_date,
                         ef.create_date,
                         ef.delete_status,
                         ef.update_count,
                     }).FirstOrDefaultAsync();

                bool already = false;
                bool expire_edit = false;
                bool expire_cancel = false;
                bool cancel = false;


                if (etaxFile != null)
                {
                    already = true;

                    expiryDate = now.AddDays(-7).Date;
                    DateTime ref_issue_date = (DateTime)etaxFile.ref_issue_date;

                    if (expiryDate >= ref_issue_date)
                        expire_edit = true;

                    //if (etaxFile.update_count >= 2)
                    //    expire_edit = true;

                    if (bodyMkData.branchCode == "D201")
                    {
                        expiryDate = DateTime.ParseExact(now.ToString("yyyy-MM") + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        ref_issue_date = DateTime.ParseExact(((DateTime)etaxFile.ref_issue_date).ToString("yyyy-MM") + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);

                        if (expiryDate > ref_issue_date)
                            expire_cancel = true;
                    }
                    else
                    {
                        expiryDate = now.AddDays(-1).Date;
                        ref_issue_date = (DateTime)etaxFile.ref_issue_date;

                        if (expiryDate >= ref_issue_date)
                            expire_cancel = true;
                    }

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
                            expire_edit = expire_edit,
                            expire_cancel = expire_cancel,
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
                DateTime now = DateTime.Now;
                EtaxFile etaxFile = await _context.etax_files
                .Where(x => x.member_id == bodyUserform.member_id && x.ref_etax_id == bodyUserform.dataQr.billID)
                .FirstOrDefaultAsync();

                if (bodyUserform.lang == "EN")
                {
                    if (etaxFile == null)
                    {

                        DateTime ref_issue_date = DateTime.ParseExact(bodyUserform.dataQr.bilIDate, "yyyyMMdd", CultureInfo.InvariantCulture);
                        string running_name = "etax_id_" + ref_issue_date.ToString("MM_yyyy");

                        if (running_name == "etax_id_12_2024")
                            running_name = "etax_id";

                        RunningNumber runningNumber = await _context.running_number
                        .Where(x => x.member_id == bodyUserform.member_id && x.type == running_name)
                        .FirstOrDefaultAsync();

                        int running_number = 0;

                        if (runningNumber == null)
                        {
                            RunningNumber newRunningNumber = new RunningNumber()
                            {
                                member_id = bodyUserform.member_id,
                                type = running_name,
                                number = 1,
                                update_date = now,
                                create_date = now,
                            };
                            _context.running_number.Add(newRunningNumber);
                            _context.SaveChanges();

                            running_number = 1;
                        }
                        else
                        {
                            running_number = runningNumber.number + 1;
                            runningNumber.number = running_number;
                            runningNumber.update_date = now;
                            _context.SaveChanges();
                        }


                        double amountNoVat = double.Parse(bodyUserform.dataQr.amount) * 100 / 107;
                        double discountNoVat = double.Parse(bodyUserform.dataQr.discount) * 100 / 107;

                        string etax_id = bodyUserform.dataQr.branchCode +
                            ref_issue_date.ToString("yyyyMMdd", new CultureInfo("th-TH")) + "-" +
                            running_number.ToString().PadLeft(5, '0');

                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            try
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
                                    etax_id = etax_id,
                                    issue_date = ref_issue_date,
                                    ref_etax_id = bodyUserform.dataQr.billID,
                                    ref_issue_date = ref_issue_date,
                                    buyer_branch_code = "00000",
                                    buyer_id = "",
                                    buyer_name = bodyUserform.name,
                                    buyer_tax_id = bodyUserform.tax_id,
                                    buyer_tax_type = "",
                                    buyer_address = bodyUserform.address,
                                    buyer_zipcode = "00000",
                                    buyer_tel = bodyUserform.tel,
                                    buyer_fax = "",
                                    buyer_country_code = "TH",
                                    buyer_email = bodyUserform.email,
                                    price = double.Parse(amountNoVat.ToString("0.00")),
                                    discount = double.Parse(discountNoVat.ToString("0.00")),
                                    tax_rate = 7,
                                    tax = double.Parse(bodyUserform.dataQr.vat),
                                    total = double.Parse(bodyUserform.dataQr.totalAmount),
                                    remark = "",
                                    other = bodyUserform.dataQr.branchCode + "|EN|" + bodyUserform.dataQr.url,
                                    other2 = bodyUserform.dataQr.baseAmount + "|" + bodyUserform.dataQr.noDiscount + "|" + bodyUserform.dataQr.fAndB + "|" + bodyUserform.dataQr.service,
                                    group_name = "",
                                    template_pdf = "",
                                    template_email = "",
                                    xml_payment_status = "pending",
                                    pdf_payment_status = "pending",
                                    mode = "normal",
                                    update_count = 0,
                                    update_date = now,
                                    create_date = now,
                                };

                                if (bodyUserform.type == 1)
                                    newEtaxFile.buyer_branch_code = bodyUserform.branch_code;

                                _context.Add(newEtaxFile);
                                await _context.SaveChangesAsync();

                                double itemPrice1 = double.Parse(bodyUserform.dataQr.fAndB);
                                double itemTax1 = itemPrice1 * newEtaxFile.tax_rate / 100;

                                string itemName = "อาหารและเครื่องดื่ม";
                                if (bodyUserform.dataQr.branchCode == "D201")
                                    itemName = "บัตรสมาชิก";

                                EtaxFileItem newEtaxFileItem = new EtaxFileItem()
                                {
                                    etax_file_id = newEtaxFile.id,
                                    code = "",
                                    name = itemName,
                                    qty = 1,
                                    unit = "",
                                    price = itemPrice1,
                                    discount = 0,
                                    tax = itemTax1,
                                    tax_rate = newEtaxFile.tax_rate,
                                    total = (itemPrice1 + itemTax1),
                                };
                                _context.Add(newEtaxFileItem);

                                if (bodyUserform.dataQr.service != "0")
                                {
                                    double itemPrice2 = double.Parse(bodyUserform.dataQr.service);
                                    double itemTax2 = itemPrice2 * newEtaxFile.tax_rate / 100;
                                    EtaxFileItem newEtaxFileItem2 = new EtaxFileItem()
                                    {
                                        etax_file_id = newEtaxFile.id,
                                        code = "",
                                        name = "ค่าบริการ",
                                        qty = 1,
                                        unit = "",
                                        price = itemPrice2,
                                        discount = 0,
                                        tax = itemTax2,
                                        tax_rate = newEtaxFile.tax_rate,
                                        total = (itemPrice2 + itemTax2),
                                    };
                                    _context.Add(newEtaxFileItem2);
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
                    }
                    else
                    {
                        //if (etaxFile.update_count >= 2)
                        //    return StatusCode(404, new { message = "ไม่สามารถแก้ไขข้อมูลได้", });


                        if (bodyUserform.type == 1)
                            etaxFile.buyer_branch_code = bodyUserform.branch_code;
                        etaxFile.buyer_name = bodyUserform.name;
                        etaxFile.buyer_tax_id = bodyUserform.tax_id;
                        etaxFile.buyer_address = bodyUserform.address;
                        etaxFile.buyer_zipcode = "00000";
                        etaxFile.buyer_tel = bodyUserform.tel;
                        etaxFile.buyer_email = bodyUserform.email;

                        etaxFile.gen_xml_status = "pending";
                        etaxFile.gen_pdf_status = "pending";
                        etaxFile.add_email_status = "pending";

                        etaxFile.update_count = etaxFile.update_count + 1;


                        EtaxFile etaxFileRef = await _context.etax_files
                        .Where(x => x.member_id == bodyUserform.member_id && x.etax_id == bodyUserform.dataQr.billIDRef)
                        .FirstOrDefaultAsync();

                        if (etaxFileRef != null)
                            etaxFileRef.delete_status = 1;

                        await _context.SaveChangesAsync();
                    }

                    return StatusCode(200, new
                    {
                        message = "แก้ไขข้อมูลสำเร็จ",
                    });
                }
                else
                {
                    if (bodyUserform.province == "กรุงเทพมหานคร")
                    {
                        bodyUserform.district = "แขวง" + bodyUserform.district;
                        bodyUserform.amphoe = "เขต" + bodyUserform.amphoe;
                        bodyUserform.province = "จังหวัด" + bodyUserform.province;
                    }
                    else
                    {
                        bodyUserform.district = "ตำบล" + bodyUserform.district;
                        bodyUserform.amphoe = "อำเภอ" + bodyUserform.amphoe;
                        bodyUserform.province = "จังหวัด" + bodyUserform.province;
                    }


                    if (etaxFile == null)
                    {

                        DateTime ref_issue_date = DateTime.ParseExact(bodyUserform.dataQr.bilIDate, "yyyyMMdd", CultureInfo.InvariantCulture);
                        string running_name = "etax_id_" + ref_issue_date.ToString("MM_yyyy");

                        if (running_name == "etax_id_12_2024")
                            running_name = "etax_id";

                        RunningNumber runningNumber = await _context.running_number
                        .Where(x => x.member_id == bodyUserform.member_id && x.type == running_name)
                        .FirstOrDefaultAsync();

                        int running_number = 0;

                        if (runningNumber == null)
                        {
                            RunningNumber newRunningNumber = new RunningNumber()
                            {
                                member_id = bodyUserform.member_id,
                                type = running_name,
                                number = 1,
                                update_date = now,
                                create_date = now,
                            };
                            _context.running_number.Add(newRunningNumber);
                            _context.SaveChanges();

                            running_number = 1;
                        }
                        else
                        {
                            running_number = runningNumber.number + 1;
                            runningNumber.number = running_number;
                            runningNumber.update_date = now;
                            _context.SaveChanges();
                        }


                        double amountNoVat = double.Parse(bodyUserform.dataQr.amount) * 100 / 107;
                        double discountNoVat = double.Parse(bodyUserform.dataQr.discount) * 100 / 107;

                        string etax_id = bodyUserform.dataQr.branchCode +
                            ref_issue_date.ToString("yyyyMMdd", new CultureInfo("th-TH")) + "-" +
                            running_number.ToString().PadLeft(5, '0');

                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            try
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
                                    etax_id = etax_id,
                                    issue_date = ref_issue_date,
                                    ref_etax_id = bodyUserform.dataQr.billID,
                                    ref_issue_date = ref_issue_date,
                                    buyer_branch_code = "00000",
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
                                    price = double.Parse(amountNoVat.ToString("0.00")),
                                    discount = double.Parse(discountNoVat.ToString("0.00")),
                                    tax_rate = 7,
                                    tax = double.Parse(bodyUserform.dataQr.vat),
                                    total = double.Parse(bodyUserform.dataQr.totalAmount),
                                    remark = "",
                                    other = bodyUserform.dataQr.branchCode + "|TH|" + bodyUserform.dataQr.url,
                                    other2 = bodyUserform.dataQr.baseAmount + "|" + bodyUserform.dataQr.noDiscount + "|" + bodyUserform.dataQr.fAndB + "|" + bodyUserform.dataQr.service,
                                    group_name = "",
                                    template_pdf = "",
                                    template_email = "",
                                    xml_payment_status = "pending",
                                    pdf_payment_status = "pending",
                                    mode = "normal",
                                    update_count = 0,
                                    update_date = now,
                                    create_date = now,
                                };

                                if (bodyUserform.type == 1)
                                    newEtaxFile.buyer_branch_code = bodyUserform.branch_code;

                                _context.Add(newEtaxFile);
                                await _context.SaveChangesAsync();

                                double itemPrice1 = double.Parse(bodyUserform.dataQr.fAndB);
                                double itemTax1 = itemPrice1 * newEtaxFile.tax_rate / 100;

                                string itemName = "อาหารและเครื่องดื่ม";
                                if (bodyUserform.dataQr.branchCode == "D201")
                                    itemName = "บัตรสมาชิก";

                                EtaxFileItem newEtaxFileItem = new EtaxFileItem()
                                {
                                    etax_file_id = newEtaxFile.id,
                                    code = "",
                                    name = itemName,
                                    qty = 1,
                                    unit = "",
                                    price = itemPrice1,
                                    discount = 0,
                                    tax = itemTax1,
                                    tax_rate = newEtaxFile.tax_rate,
                                    total = (itemPrice1 + itemTax1),
                                };
                                _context.Add(newEtaxFileItem);

                                if (bodyUserform.dataQr.service != "0")
                                {
                                    double itemPrice2 = double.Parse(bodyUserform.dataQr.service);
                                    double itemTax2 = itemPrice2 * newEtaxFile.tax_rate / 100;
                                    EtaxFileItem newEtaxFileItem2 = new EtaxFileItem()
                                    {
                                        etax_file_id = newEtaxFile.id,
                                        code = "",
                                        name = "ค่าบริการ",
                                        qty = 1,
                                        unit = "",
                                        price = itemPrice2,
                                        discount = 0,
                                        tax = itemTax2,
                                        tax_rate = newEtaxFile.tax_rate,
                                        total = (itemPrice2 + itemTax2),
                                    };
                                    _context.Add(newEtaxFileItem2);
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
                    }
                    else
                    {
                        //if (etaxFile.update_count >= 2)
                        //    return StatusCode(404, new { message = "ไม่สามารถแก้ไขข้อมูลได้", });


                        if (bodyUserform.type == 1)
                            etaxFile.buyer_branch_code = bodyUserform.branch_code;
                        etaxFile.buyer_name = bodyUserform.name;
                        etaxFile.buyer_tax_id = bodyUserform.tax_id;
                        etaxFile.buyer_address = bodyUserform.address + " " + bodyUserform.district + " " + bodyUserform.amphoe + " " + bodyUserform.province;
                        etaxFile.buyer_zipcode = bodyUserform.zipcode;
                        etaxFile.buyer_tel = bodyUserform.tel;
                        etaxFile.buyer_email = bodyUserform.email;

                        etaxFile.gen_xml_status = "pending";
                        etaxFile.gen_pdf_status = "pending";
                        etaxFile.add_email_status = "pending";

                        etaxFile.update_count = etaxFile.update_count + 1;


                        EtaxFile etaxFileRef = await _context.etax_files
                        .Where(x => x.member_id == bodyUserform.member_id && x.etax_id == bodyUserform.dataQr.billIDRef)
                        .FirstOrDefaultAsync();

                        if (etaxFileRef != null)
                            etaxFileRef.delete_status = 1;

                        await _context.SaveChangesAsync();
                    }

                    return StatusCode(200, new
                    {
                        message = "แก้ไขข้อมูลสำเร็จ",
                    });
                }
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
                if (bodyCancel.cancelPassword != "87654321")
                    return StatusCode(401, new { message = "รหัสลับในการลบใบกำกับภาษีไม่ถูกต้อง", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        EtaxFile etaxFile = await _context.etax_files
                        .Where(x => x.member_id == bodyCancel.member_id && x.ref_etax_id == bodyCancel.billID)
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

        [HttpPost]
        [Route("find_legal_entity/{tax_id}")]
        public async Task<IActionResult> FindLegalEntity(string tax_id)
        {
            try
            {
                WalkinCertData data = MocApi.getDataMoc(tax_id);
                if (data != null)
                {

                    string address = "";

                    if (data.addressDetail.buildingName != null)
                        address += data.addressDetail.buildingName + " ";
                    if (data.addressDetail.roomNo != null)
                        address += data.addressDetail.roomNo + " ";
                    if (data.addressDetail.floor != null)
                        address += data.addressDetail.floor + " ";
                    if (data.addressDetail.villageName != null)
                        address += data.addressDetail.villageName + " ";
                    if (data.addressDetail.houseNumber != null)
                        address += data.addressDetail.houseNumber + " ";
                    if (data.addressDetail.moo != null)
                        address += "ม." + data.addressDetail.moo + " ";
                    if (data.addressDetail.soi != null)
                        address += "ซอย" + data.addressDetail.soi + " ";
                    if (data.addressDetail.street != null)
                        address += "ถนน" + data.addressDetail.street + " ";


                    Province province = null;
                    Amphoe amphoe = null;
                    District district = null;


                    province = _context.province.Where(x => x.province_th == data.addressDetail.province).FirstOrDefault();
                    if (province == null)
                        return StatusCode(404, new { message = "ไม่พบข้อมูล", });


                    List<Amphoe> amphoes = _context.amphoe.Where(x => x.province_code == province.province_code).ToList();
                    if (amphoes.Count > 1)
                    {
                        foreach (Amphoe amp in amphoes)
                        {
                            if (amp.amphoe_th.Contains(data.addressDetail.subDistrict))
                            {
                                amphoe = amp;
                                break;
                            }
                        }
                    }


                    //List<District> districts = _context.district.Where(x => x.zipcode == zipcode).ToList();
                    //if (districts.Count > 1)
                    //{
                    //    foreach (District dis in districts)
                    //    {
                    //        if (dis.district_th.Contains(district_address))
                    //        {
                    //            district = dis;
                    //            break;
                    //        }
                    //    }
                    //}

                    //amphoe = _context.amphoe.Where(x => x.amphoe_code == district.amphoe_code).FirstOrDefault();
                    //province = _context.province.Where(x => x.province_code == amphoe.province_code).FirstOrDefault();


                    string newAddress = "";
                    //foreach (string a in listAddress)
                    //{
                    //    bool status = false;
                    //    if (a.Contains(district.district_th_s))
                    //        status = true;
                    //    else if (a.Contains(amphoe.amphoe_th_s))
                    //        status = true;
                    //    else if (a.Contains(province.province_th))
                    //        status = true;
                    //    else if (a.Contains(zipcode))
                    //        status = true;


                    //    if (!status)
                    //        newAddress += a + " ";
                    //}


                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            tax_id = data.juristicID,
                            name = data.juristicNameTH,
                            address = newAddress.Trim(),
                            district = district,
                            amphoe = amphoe,
                            province = province,
                        },
                    });
                }
                else
                {
                    return StatusCode(404, new { message = "ไม่พบข้อมูล", });
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
