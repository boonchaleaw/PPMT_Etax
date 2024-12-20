
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

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class ReportController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public ReportController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        [HttpPost]
        [Route("get_cost_summary_report")]
        public async Task<IActionResult> GetCostSummaryReport([FromBody] BodyCostReport bodyCostReport)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_report_view).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                bodyCostReport.dateStart = DateTime.Parse(bodyCostReport.dateStart.ToString()).Date;
                bodyCostReport.dateEnd = bodyCostReport.dateEnd.AddDays(+1).AddMilliseconds(-1);

                var price_type = await (from mpt in _context.member_price_type
                                        where mpt.member_id == jwtStatus.member_id
                                        select mpt).FirstOrDefaultAsync();

                List<ViewPaymentRawData> listPayment = await _context.view_payment_rawdata
                .Where(x => x.member_id == jwtStatus.member_id && x.create_date >= bodyCostReport.dateStart && x.create_date <= bodyCostReport.dateEnd)
                .ToListAsync();


                ReturnCostReport returnCostReport = new ReturnCostReport();
                returnCostReport.listReturnCostReportData = new List<ReturnCostReportData>();

                foreach (ViewPaymentRawData payment in listPayment)
                {
                    returnCostReport.listReturnCostReportData.Add(new ReturnCostReportData()
                    {
                        row_name = payment.row_name,
                        xml_count = payment.xml_count,
                        pdf_count = payment.pdf_count,
                        email_count = payment.email_count,
                        ebxml_count = payment.ebxml_count,
                        sms_count = payment.sms_message_count,
                    });

                    returnCostReport.total_xml_count += payment.xml_count;
                    returnCostReport.total_pdf_count += payment.pdf_count;
                    returnCostReport.total_email_count += payment.email_count;
                    returnCostReport.total_ebxml_count += payment.ebxml_count;
                    returnCostReport.total_sms_count += payment.sms_message_count;
                }



                var successCount = new
                {
                    xml_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.create_type == "api" && x.gen_xml_status == "success" && x.create_date >= bodyCostReport.dateStart && x.create_date <= bodyCostReport.dateEnd).CountAsync(),
                    pdf_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.create_type == "api" && x.gen_pdf_status == "success" && x.create_date >= bodyCostReport.dateStart && x.create_date <= bodyCostReport.dateEnd).CountAsync(),
                    email_count = await _context.view_send_email.Where(x => x.member_id == jwtStatus.member_id && x.create_type == "api" && x.send_email_status == "success" && x.create_date >= bodyCostReport.dateStart && x.create_date <= bodyCostReport.dateEnd).SumAsync(x => x.send_count),
                    sms_count = await _context.view_send_sms.Where(x => x.member_id == jwtStatus.member_id && x.create_type == "api" && x.send_sms_status == "success" && x.create_date >= bodyCostReport.dateStart && x.create_date <= bodyCostReport.dateEnd).SumAsync(x => x.message_count),
                    ebxml_count = await _context.view_send_ebxml.Where(x => x.member_id == jwtStatus.member_id && x.create_type == "api" && x.send_ebxml_status == "success" && x.create_date >= bodyCostReport.dateStart && x.create_date <= bodyCostReport.dateEnd).CountAsync(),
                };

                returnCostReport.listReturnCostReportData.Add(new ReturnCostReportData()
                {
                    row_name = "Api",
                    xml_count = successCount.xml_count,
                    pdf_count = successCount.pdf_count,
                    email_count = successCount.email_count,
                    sms_count = successCount.sms_count,
                    ebxml_count = successCount.ebxml_count,
                });

                returnCostReport.total_xml_count += successCount.xml_count;
                returnCostReport.total_pdf_count += successCount.pdf_count;
                returnCostReport.total_email_count += successCount.email_count;
                returnCostReport.total_sms_count += successCount.sms_count;
                returnCostReport.total_ebxml_count += successCount.ebxml_count;

                List<MemberPriceXml> listMemberPriceXml = await _context.member_price_xml.Where(x => x.member_id == jwtStatus.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPricePdf> listMemberPricePdf = await _context.member_price_pdf.Where(x => x.member_id == jwtStatus.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPriceEmail> listMemberPriceEmail = await _context.member_price_email.Where(x => x.member_id == jwtStatus.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPriceEbxml> listMemberPriceEbxml = await _context.member_price_ebxml.Where(x => x.member_id == jwtStatus.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPriceSms> listMemberPriceSms = await _context.member_price_sms.Where(x => x.member_id == jwtStatus.member_id).OrderByPropertyDescending("count").ToListAsync();

                int tmp_xml_count = returnCostReport.total_xml_count;
                foreach (MemberPriceXml memberPriceXml in listMemberPriceXml)
                {
                    if (tmp_xml_count > memberPriceXml.count)
                    {
                        returnCostReport.total_xml_price += (tmp_xml_count - memberPriceXml.count) * memberPriceXml.price;
                        tmp_xml_count = memberPriceXml.count;
                    }
                }

                int tmp_pdf_count = returnCostReport.total_pdf_count;
                foreach (MemberPricePdf memberPricePdf in listMemberPricePdf)
                {
                    if (tmp_pdf_count > memberPricePdf.count)
                    {
                        returnCostReport.total_pdf_price += (tmp_pdf_count - memberPricePdf.count) * memberPricePdf.price;
                        tmp_pdf_count = memberPricePdf.count;
                    }
                }

                if (price_type != null && price_type.email_price_type == "tran")
                {
                    var email_tran = await _context.view_send_email_list
                    .Where(x => x.member_id == jwtStatus.member_id && x.send_email_status == "success" && x.create_date >= bodyCostReport.dateStart && x.create_date <= bodyCostReport.dateEnd)
                    .GroupBy(x => x.etax_file_id)
                    .Select(x => new
                    {
                        etax_id = x.Key,
                        count = x.Count()
                    })
                    .ToListAsync();

                    foreach (var et in email_tran)
                    {
                        int tmp_email_count = et.count;
                        foreach (MemberPriceEmail memberPriceEmail in listMemberPriceEmail)
                        {
                            if (tmp_email_count > memberPriceEmail.count)
                            {
                                returnCostReport.total_email_price += (tmp_email_count - memberPriceEmail.count) * memberPriceEmail.price;
                                tmp_email_count = memberPriceEmail.count;
                            }
                        }
                    }
                }
                else
                {
                    int tmp_email_count = returnCostReport.total_email_count;
                    foreach (MemberPriceEmail memberPriceEmail in listMemberPriceEmail)
                    {
                        if (tmp_email_count > memberPriceEmail.count)
                        {
                            returnCostReport.total_email_price += (tmp_email_count - memberPriceEmail.count) * memberPriceEmail.price;
                            tmp_email_count = memberPriceEmail.count;
                        }
                    }
                }


                int tmp_ebxml_count = returnCostReport.total_ebxml_count;
                foreach (MemberPriceEbxml memberPriceEbxml in listMemberPriceEbxml)
                {
                    if (tmp_ebxml_count > memberPriceEbxml.count)
                    {
                        returnCostReport.total_ebxml_price += (tmp_ebxml_count - memberPriceEbxml.count) * memberPriceEbxml.price;
                        tmp_ebxml_count = memberPriceEbxml.count;
                    }
                }

                int tmp_sms_count = returnCostReport.total_sms_count;
                foreach (MemberPriceSms memberPriceSms in listMemberPriceSms)
                {
                    if (tmp_sms_count > memberPriceSms.count)
                    {
                        returnCostReport.total_sms_price += (tmp_sms_count - memberPriceSms.count) * memberPriceSms.price;
                        tmp_sms_count = memberPriceSms.count;
                    }
                }

                return StatusCode(200, new
                {
                    message = "เรียกดูข้อมูลสำเร็จ",
                    data = returnCostReport,
                });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }



        [HttpPost]
        [Route("get_tax_summary_report")]
        public async Task<IActionResult> GetTaxSummaryReport([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_report_view).FirstOrDefaultAsync();
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

                var result = _context.view_tex_report.Where(x => x.member_id == jwtStatus.member_id && x.gen_xml_status == "success" && x.delete_status == 0).AsQueryable();

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
                                (r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.buyer_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
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
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
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

                //double sumOriginalPrice = result.Sum(s => s.original_price);

                double sumPrice = result.Where(x => x.document_type_id != 3).Sum(s => s.price);
                double sumPriceCN = result.Where(x => x.document_type_id == 3).Sum(s => s.price);

                double sumDiscount = result.Where(x => x.document_type_id != 3).Sum(s => s.discount);
                double sumDiscountCN = result.Where(x => x.document_type_id == 3).Sum(s => s.discount);

                double sumTax = result.Where(x => x.document_type_id != 3).Sum(s => s.tax);
                double sumTaxCN = result.Where(x => x.document_type_id == 3).Sum(s => s.tax);

                double sumTotal = result.Where(x => x.document_type_id != 3).Sum(s => s.total);
                double sumTotalCN = result.Where(x => x.document_type_id == 3).Sum(s => s.total);

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.CountAsync();
                var totalResultsCount = 0;


                if (bodyDtParameters.Length == -1)
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.etax_id,
                        x.document_type_id,
                        x.document_type_name,
                        x.buyer_tax_id,
                        x.buyer_name,
                        x.original_price,
                        x.new_price,
                        x.price,
                        x.discount,
                        x.tax,
                        x.total,
                        x.issue_date,
                        x.gen_xml_finish,
                    })
                    .ToListAsync();


                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        countTotal = filteredResultsCount,
                        //sumOriginalPrice = sumOriginalPrice,
                        sumPrice = (sumPrice - sumPriceCN),
                        sumDiscount = (sumDiscount - sumDiscountCN),
                        sumTax = (sumTax - sumTaxCN),
                        sumTotal = (sumTotal - sumTotalCN),
                        data = data,
                    });
                }
                else
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.etax_id,
                        x.document_type_id,
                        x.document_type_name,
                        x.buyer_tax_id,
                        x.buyer_name,
                        x.original_price,
                        x.new_price,
                        x.price,
                        x.discount,
                        x.tax,
                        x.total,
                        x.issue_date,
                        x.gen_xml_finish,
                    })
                    .Skip(bodyDtParameters.Start)
                    .Take(bodyDtParameters.Length)
                    .ToListAsync();


                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        countTotal = filteredResultsCount,
                        //sumOriginalPrice = sumOriginalPrice,
                        sumPrice = (sumPrice - sumPriceCN),
                        sumDiscount = (sumDiscount - sumDiscountCN),
                        sumTax = (sumTax - sumTaxCN),
                        sumTotal = (sumTotal - sumTotalCN),
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
        [Route("get_email_summary_report")]
        public async Task<IActionResult> GetEmailSummaryReport([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_report_view).FirstOrDefaultAsync();
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

                var searchBy = bodyDtParameters.searchText;
                var orderCriteria = "id";
                var orderAscendingDirection = true;

                if (bodyDtParameters.Order != null)
                {
                    orderCriteria = bodyDtParameters.Columns[bodyDtParameters.Order[0].Column].Data;
                    orderAscendingDirection = bodyDtParameters.Order[0].Dir.ToString().ToLower() == "asc";
                }

                var result = _context.view_send_email_list.Where(x => x.member_id == jwtStatus.member_id).AsQueryable();

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
                                (r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
                                (r.buyer_name.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
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
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
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
                var totalResultsCount = await _context.view_send_email_list.Where(x => x.member_id == jwtStatus.member_id && x.document_type_id == document_id).CountAsync();

                if (bodyDtParameters.Length == -1)
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.etax_file_id,
                        x.document_type_id,
                        x.etax_id,
                        x.buyer_tax_id,
                        x.buyer_name,
                        x.buyer_email,
                        x.email,
                        x.send_email_status,
                        x.email_status,
                        x.issue_date,
                        x.send_email_finish,
                    })
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        countTotal = filteredResultsCount,
                        data = data,
                    });
                }
                else
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.etax_file_id,
                        x.document_type_id,
                        x.etax_id,
                        x.buyer_tax_id,
                        x.buyer_name,
                        x.buyer_email,
                        x.email,
                        x.send_email_status,
                        x.email_status,
                        x.issue_date,
                        x.send_email_finish,
                    })
                    .Skip(bodyDtParameters.Start)
                    .Take(bodyDtParameters.Length)
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        countTotal = filteredResultsCount,
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
        [Route("get_ebxml_summary_report")]
        public async Task<IActionResult> GetEbxmlSummaryReport([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_report_view).FirstOrDefaultAsync();
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

                var result = _context.view_send_ebxml.Where(x => x.member_id == jwtStatus.member_id && x.send_ebxml_status == "success").AsQueryable();

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
                                (r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
                                (r.buyer_name.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
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
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
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
                var totalResultsCount = await _context.view_etax_files.Where(x => x.member_id == jwtStatus.member_id && x.gen_xml_status == "success" && x.document_type_id == document_id).CountAsync();

                if (bodyDtParameters.Length == -1)
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.etax_file_id,
                        x.etax_id,
                        x.buyer_tax_id,
                        x.buyer_name,
                        x.send_ebxml_status,
                        x.etax_status,
                        x.issue_date,
                        x.send_ebxml_finish,
                    })
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        countTotal = filteredResultsCount,
                        data = data,
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
                        x.buyer_tax_id,
                        x.buyer_name,
                        x.send_ebxml_status,
                        x.etax_status,
                        x.issue_date,
                        x.send_ebxml_finish,
                    })
                   .Skip(bodyDtParameters.Start)
                   .Take(bodyDtParameters.Length)
                   .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        countTotal = filteredResultsCount,
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
        [Route("get_cancel_report")]
        public async Task<IActionResult> GetCancelReport([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_report_view).FirstOrDefaultAsync();
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

                var result = _context.view_tex_report.Where(x => x.member_id == jwtStatus.member_id && x.gen_xml_status == "success" && x.delete_status == 1).AsQueryable();

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
                                (r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.buyer_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
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
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
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

                //double sumOriginalPrice = result.Sum(s => s.original_price);

                double sumPrice = result.Where(x => x.document_type_id != 3).Sum(s => s.price);
                double sumPriceCN = result.Where(x => x.document_type_id == 3).Sum(s => s.price);

                double sumDiscount = result.Where(x => x.document_type_id != 3).Sum(s => s.discount);
                double sumDiscountCN = result.Where(x => x.document_type_id == 3).Sum(s => s.discount);

                double sumTax = result.Where(x => x.document_type_id != 3).Sum(s => s.tax);
                double sumTaxCN = result.Where(x => x.document_type_id == 3).Sum(s => s.tax);

                double sumTotal = result.Where(x => x.document_type_id != 3).Sum(s => s.total);
                double sumTotalCN = result.Where(x => x.document_type_id == 3).Sum(s => s.total);

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.CountAsync();
                var totalResultsCount = 0;


                if (bodyDtParameters.Length == -1)
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.etax_id,
                        x.document_type_id,
                        x.document_type_name,
                        x.buyer_tax_id,
                        x.buyer_name,
                        x.original_price,
                        x.new_price,
                        x.price,
                        x.discount,
                        x.tax,
                        x.total,
                        x.issue_date,
                        x.gen_xml_finish,
                    })
                    .ToListAsync();


                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        countTotal = filteredResultsCount,
                        //sumOriginalPrice = sumOriginalPrice,
                        sumPrice = (sumPrice - sumPriceCN),
                        sumDiscount = (sumDiscount - sumDiscountCN),
                        sumTax = (sumTax - sumTaxCN),
                        sumTotal = (sumTotal - sumTotalCN),
                        data = data,
                    });
                }
                else
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.etax_id,
                        x.document_type_id,
                        x.document_type_name,
                        x.buyer_tax_id,
                        x.buyer_name,
                        x.original_price,
                        x.new_price,
                        x.price,
                        x.discount,
                        x.tax,
                        x.total,
                        x.issue_date,
                        x.gen_xml_finish,
                    })
                    .Skip(bodyDtParameters.Start)
                    .Take(bodyDtParameters.Length)
                    .ToListAsync();


                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        countTotal = filteredResultsCount,
                        //sumOriginalPrice = sumOriginalPrice,
                        sumPrice = (sumPrice - sumPriceCN),
                        sumDiscount = (sumDiscount - sumDiscountCN),
                        sumTax = (sumTax - sumTaxCN),
                        sumTotal = (sumTotal - sumTotalCN),
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
        [Route("csv_cost_summary_report")]
        public async Task<IActionResult> CsvCostSummaryReport([FromBody] BodyCostReport bodyCostReport)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_report_view).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                bodyCostReport.dateStart = DateTime.Parse(bodyCostReport.dateStart.ToString()).Date;
                bodyCostReport.dateEnd = bodyCostReport.dateEnd.AddDays(+1).AddMilliseconds(-1);

                var price_type = await (from mpt in _context.member_price_type
                                        where mpt.member_id == jwtStatus.member_id
                                        select mpt).FirstOrDefaultAsync();

                Member member = await _context.members
                .Where(x => x.id == jwtStatus.member_id)
                .FirstOrDefaultAsync();

                List<ViewPaymentRawData> listPayment = await _context.view_payment_rawdata
                .Where(x => x.member_id == jwtStatus.member_id && x.create_date >= bodyCostReport.dateStart && x.create_date <= bodyCostReport.dateEnd)
                .ToListAsync();

                List<ViewPayment> listPaymentOther = await _context.view_payment
               .Where(x => x.member_id == jwtStatus.member_id)
               .ToListAsync();

                ReturnCostReport returnCostReport = new ReturnCostReport();
                returnCostReport.listReturnCostReportData = new List<ReturnCostReportData>();

                foreach (ViewPaymentRawData payment in listPayment)
                {
                    returnCostReport.listReturnCostReportData.Add(new ReturnCostReportData()
                    {
                        row_name = payment.row_name,
                        xml_count = payment.xml_count,
                        pdf_count = payment.pdf_count,
                        email_count = payment.email_count,
                        sms_count = payment.sms_message_count,
                        ebxml_count = payment.ebxml_count,
                    });

                    returnCostReport.total_xml_count += payment.xml_count;
                    returnCostReport.total_pdf_count += payment.pdf_count;
                    returnCostReport.total_email_count += payment.email_count;
                    returnCostReport.total_sms_count += payment.sms_message_count;
                    returnCostReport.total_ebxml_count += payment.ebxml_count;
                }



                var successCount = new
                {
                    xml_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.create_type == "api" && x.gen_xml_status == "success").CountAsync(),
                    pdf_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.create_type == "api" && x.gen_pdf_status == "success").CountAsync(),
                    email_count = await _context.view_send_email.Where(x => x.member_id == jwtStatus.member_id && x.create_type == "api" && x.send_email_status == "success").SumAsync(x => x.send_count),
                    sms_count = await _context.view_send_sms.Where(x => x.member_id == jwtStatus.member_id && x.create_type == "api" && x.send_sms_status == "success").SumAsync(x => x.message_count),
                    ebxml_count = await _context.view_send_ebxml.Where(x => x.member_id == jwtStatus.member_id && x.create_type == "api" && x.send_ebxml_status == "success").CountAsync(),
                };

                returnCostReport.listReturnCostReportData.Add(new ReturnCostReportData()
                {
                    row_name = "Api",
                    xml_count = successCount.xml_count,
                    pdf_count = successCount.pdf_count,
                    email_count = successCount.email_count,
                    sms_count = successCount.sms_count,
                    ebxml_count = successCount.ebxml_count,
                });

                returnCostReport.total_xml_count += successCount.xml_count;
                returnCostReport.total_pdf_count += successCount.pdf_count;
                returnCostReport.total_email_count += successCount.email_count;
                returnCostReport.total_sms_count += successCount.sms_count;
                returnCostReport.total_ebxml_count += successCount.ebxml_count;

                List<MemberPriceXml> listMemberPriceXml = await _context.member_price_xml.Where(x => x.member_id == jwtStatus.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPricePdf> listMemberPricePdf = await _context.member_price_pdf.Where(x => x.member_id == jwtStatus.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPriceEmail> listMemberPriceEmail = await _context.member_price_email.Where(x => x.member_id == jwtStatus.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPriceSms> listMemberPriceSms = await _context.member_price_sms.Where(x => x.member_id == jwtStatus.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPriceEbxml> listMemberPriceEbxml = await _context.member_price_ebxml.Where(x => x.member_id == jwtStatus.member_id).OrderByPropertyDescending("count").ToListAsync();



                int tmp_xml_count = returnCostReport.total_xml_count;
                foreach (MemberPriceXml memberPriceXml in listMemberPriceXml)
                {
                    if (tmp_xml_count > memberPriceXml.count)
                    {
                        returnCostReport.total_xml_price += (tmp_xml_count - memberPriceXml.count) * memberPriceXml.price;
                        tmp_xml_count = memberPriceXml.count;
                    }
                }

                int tmp_pdf_count = returnCostReport.total_pdf_count;
                foreach (MemberPricePdf memberPricePdf in listMemberPricePdf)
                {
                    if (tmp_pdf_count > memberPricePdf.count)
                    {
                        returnCostReport.total_pdf_price += (tmp_pdf_count - memberPricePdf.count) * memberPricePdf.price;
                        tmp_pdf_count = memberPricePdf.count;
                    }
                }

                if (price_type != null && price_type.email_price_type == "tran")
                {
                    var email_tran = await _context.view_send_email_list
                    .Where(x => x.member_id == jwtStatus.member_id && x.send_email_status == "success" && x.create_date >= bodyCostReport.dateStart && x.create_date <= bodyCostReport.dateEnd)
                    .GroupBy(x => x.etax_file_id)
                    .Select(x => new
                    {
                        etax_id = x.Key,
                        count = x.Count()
                    })
                    .ToListAsync();

                    foreach (var et in email_tran)
                    {
                        int tmp_email_count = et.count;
                        foreach (MemberPriceEmail memberPriceEmail in listMemberPriceEmail)
                        {
                            if (tmp_email_count > memberPriceEmail.count)
                            {
                                returnCostReport.total_email_price += (tmp_email_count - memberPriceEmail.count) * memberPriceEmail.price;
                                tmp_email_count = memberPriceEmail.count;
                            }
                        }
                    }
                }
                else
                {
                    int tmp_email_count = returnCostReport.total_email_count;
                    foreach (MemberPriceEmail memberPriceEmail in listMemberPriceEmail)
                    {
                        if (tmp_email_count > memberPriceEmail.count)
                        {
                            returnCostReport.total_email_price += (tmp_email_count - memberPriceEmail.count) * memberPriceEmail.price;
                            tmp_email_count = memberPriceEmail.count;
                        }
                    }
                }

                int tmp_sms_count = returnCostReport.total_sms_count;
                foreach (MemberPriceSms memberPriceSms in listMemberPriceSms)
                {
                    if (tmp_sms_count > memberPriceSms.count)
                    {
                        returnCostReport.total_sms_price += (tmp_sms_count - memberPriceSms.count) * memberPriceSms.price;
                        tmp_sms_count = memberPriceSms.count;
                    }
                }

                int tmp_ebxml_count = returnCostReport.total_ebxml_count;
                foreach (MemberPriceEbxml memberPriceEbxml in listMemberPriceEbxml)
                {
                    if (tmp_ebxml_count > memberPriceEbxml.count)
                    {
                        returnCostReport.total_ebxml_price += (tmp_ebxml_count - memberPriceEbxml.count) * memberPriceEbxml.price;
                        tmp_ebxml_count = memberPriceEbxml.count;
                    }
                }

                string output = _config["Path:Share"];
                string pathExcel = "/" + member.id + "/excel/";
                Directory.CreateDirectory(output + pathExcel);
                pathExcel += "รายงานค่าใช้จ่ายปัจจุบัน" + member.name + ".csv";

                if (member.id == 5)
                {
                    Report.ThaibmaCostReport(output + pathExcel, member, bodyCostReport.dateStart, bodyCostReport.dateEnd, returnCostReport);
                }
                else
                {
                    Report.DefaultCostReport(output + pathExcel, member, bodyCostReport.dateStart, bodyCostReport.dateEnd, returnCostReport);
                }

                return StatusCode(200, new
                {
                    message = "สร้างไฟล์ CSV สำเร็จ",
                    data = new
                    {
                        filePath = pathExcel,
                    },
                });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("csv_tax_summary_report")]
        public async Task<IActionResult> CsvTaxSummaryReport([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_report_view).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                var member = await _context.members
                .Where(x => x.id == jwtStatus.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                    x.group_name,
                })
                .FirstOrDefaultAsync();


                var searchBy = bodyDtParameters.searchText;
                var orderCriteria = "id";
                var orderAscendingDirection = true;

                if (bodyDtParameters.Order != null)
                {
                    orderCriteria = bodyDtParameters.Columns[bodyDtParameters.Order[0].Column].Data;
                    orderAscendingDirection = bodyDtParameters.Order[0].Dir.ToString().ToLower() == "asc";
                }

                var result = _context.view_tex_csv_report.Where(x => x.member_id == jwtStatus.member_id && x.gen_xml_status == "success" && x.delete_status == 0).AsQueryable();

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
                                (r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.buyer_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
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
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
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

                double sumOriginalPrice = result.Sum(s => s.original_price);


                double sumPrice = result.Where(x => x.document_type_id != 3).Sum(s => s.price);
                double sumPriceCN = result.Where(x => x.document_type_id == 3).Sum(s => s.price);

                double sumDiscount = result.Where(x => x.document_type_id != 3).Sum(s => s.discount);
                double sumDiscountCN = result.Where(x => x.document_type_id == 3).Sum(s => s.discount);

                double sumTax = result.Where(x => x.document_type_id != 3).Sum(s => s.tax);
                double sumTaxCN = result.Where(x => x.document_type_id == 3).Sum(s => s.tax);

                double sumTotal = result.Where(x => x.document_type_id != 3).Sum(s => s.total);
                double sumTotalCN = result.Where(x => x.document_type_id == 3).Sum(s => s.total);

                sumPrice = sumPrice - sumPriceCN;
                sumDiscount = sumDiscount - sumDiscountCN;
                sumTax = sumTax - sumTaxCN;
                sumTotal = sumTotal - sumTotalCN;

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                List<ViewTaxCsvReport> listData = await result
                    .ToListAsync();

                string output = _config["Path:Share"];
                string pathExcel = "/" + member.id + "/excel/";
                Directory.CreateDirectory(output + pathExcel);
                pathExcel += "รายงานภาษีเงินได้_" + member.name + ".csv";

                if (member.id == 5)
                {
                    Report.ThaibmaTaxReport(output + pathExcel, bodyDtParameters, listData, sumOriginalPrice, sumPrice, sumDiscount, sumTax, sumTotal);
                }
                else if(member.group_name == "Isuzu")
                {
                    Report.IsuzuTexReport(output + pathExcel, bodyDtParameters, listData, sumOriginalPrice, sumPrice, sumDiscount, sumTax, sumTotal);
                }
                else
                {
                    Report.DefaultTexReport(output + pathExcel, bodyDtParameters, listData, sumOriginalPrice, sumPrice, sumDiscount, sumTax, sumTotal);
                }

                return StatusCode(200, new
                {
                    message = "สร้างไฟล์ CSV สำเร็จ",
                    data = new
                    {
                        filePath = pathExcel,
                    },
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("csv_email_summary_report")]
        public async Task<IActionResult> CsvEmailSummaryReport([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_report_view).FirstOrDefaultAsync();
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

                var searchBy = bodyDtParameters.searchText;
                var orderCriteria = "id";
                var orderAscendingDirection = true;

                if (bodyDtParameters.Order != null)
                {
                    orderCriteria = bodyDtParameters.Columns[bodyDtParameters.Order[0].Column].Data;
                    orderAscendingDirection = bodyDtParameters.Order[0].Dir.ToString().ToLower() == "asc";
                }

                var result = _context.view_send_email_list.Where(x => x.member_id == jwtStatus.member_id).AsQueryable();

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
                                (r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
                                (r.buyer_name.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
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
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
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
                var totalResultsCount = await _context.view_send_email_list.Where(x => x.member_id == jwtStatus.member_id && x.document_type_id == document_id).CountAsync();

                List<ViewSendEmailList> listData = await result
                    .ToListAsync();



               string output = _config["Path:Share"];
                string pathExcel = "/" + member.id + "/excel/";
                Directory.CreateDirectory(output + pathExcel);
                pathExcel += "รายงานการส่งอีเมล_" + member.name + ".csv";

                if (member.id == 5)
                {
                    Report.ThaibmaEmailReport(output + pathExcel, bodyDtParameters, listData);
                }
                else
                {
                    Report.DefaultEmailReport(output + pathExcel, bodyDtParameters, listData);
                }

                return StatusCode(200, new
                {
                    message = "สร้างไฟล์ CSV สำเร็จ",
                    data = new
                    {
                        filePath = pathExcel,
                    },
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("csv_ebxml_summary_report")]
        public async Task<IActionResult> CsvEbxmlSummaryReport([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_report_view).FirstOrDefaultAsync();
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

                var searchBy = bodyDtParameters.searchText;
                var orderCriteria = "id";
                var orderAscendingDirection = true;

                if (bodyDtParameters.Order != null)
                {
                    orderCriteria = bodyDtParameters.Columns[bodyDtParameters.Order[0].Column].Data;
                    orderAscendingDirection = bodyDtParameters.Order[0].Dir.ToString().ToLower() == "asc";
                }

                var result = _context.view_send_ebxml.Where(x => x.member_id == jwtStatus.member_id && x.send_ebxml_status == "success").AsQueryable();

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
                                (r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
                                (r.buyer_name.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
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
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
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
                var totalResultsCount = await _context.view_etax_files.Where(x => x.member_id == jwtStatus.member_id && x.gen_xml_status == "success" && x.document_type_id == document_id).CountAsync();

                List<ViewSendEbxml> listData = await result
                    .ToListAsync();

                string output = _config["Path:Share"];
                string pathExcel = "/" + member.id + "/excel/";
                Directory.CreateDirectory(output + pathExcel);
                pathExcel += "รายงานการส่งสรรพากร_" + member.name + ".csv";

                if (member.id == 5)
                {
                    Report.ThaibmaEbxmlReport(output + pathExcel, bodyDtParameters, listData);
                }
                else
                {
                    Report.DefaultEbxmlReport(output + pathExcel, bodyDtParameters, listData);
                }

                return StatusCode(200, new
                {
                    message = "สร้างไฟล์ CSV สำเร็จ",
                    data = new
                    {
                        filePath = pathExcel,
                    },
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }
        [HttpPost]
        [Route("csv_cancel_report")]
        public async Task<IActionResult> CsvCancelReport([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_report_view).FirstOrDefaultAsync();
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


                var searchBy = bodyDtParameters.searchText;
                var orderCriteria = "id";
                var orderAscendingDirection = true;

                if (bodyDtParameters.Order != null)
                {
                    orderCriteria = bodyDtParameters.Columns[bodyDtParameters.Order[0].Column].Data;
                    orderAscendingDirection = bodyDtParameters.Order[0].Dir.ToString().ToLower() == "asc";
                }

                var result = _context.view_tex_csv_report.Where(x => x.member_id == jwtStatus.member_id && x.gen_xml_status == "success" && x.delete_status == 1).AsQueryable();

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
                                (r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.buyer_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
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
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
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

                double sumOriginalPrice = result.Sum(s => s.original_price);


                double sumPrice = result.Where(x => x.document_type_id != 3).Sum(s => s.price);
                double sumPriceCN = result.Where(x => x.document_type_id == 3).Sum(s => s.price);

                double sumDiscount = result.Where(x => x.document_type_id != 3).Sum(s => s.discount);
                double sumDiscountCN = result.Where(x => x.document_type_id == 3).Sum(s => s.discount);

                double sumTax = result.Where(x => x.document_type_id != 3).Sum(s => s.tax);
                double sumTaxCN = result.Where(x => x.document_type_id == 3).Sum(s => s.tax);

                double sumTotal = result.Where(x => x.document_type_id != 3).Sum(s => s.total);
                double sumTotalCN = result.Where(x => x.document_type_id == 3).Sum(s => s.total);

                sumPrice = sumPrice - sumPriceCN;
                sumDiscount = sumDiscount - sumDiscountCN;
                sumTax = sumTax - sumTaxCN;
                sumTotal = sumTotal - sumTotalCN;

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                List<ViewTaxCsvReport> listData = await result
                    .ToListAsync();

                string output = _config["Path:Share"];
                string pathExcel = "/" + member.id + "/excel/";
                Directory.CreateDirectory(output + pathExcel);
                pathExcel += "รายงานยกเลิกเอกสาร_" + member.name + ".csv";

                Report.DefaultTexReport(output + pathExcel, bodyDtParameters, listData, sumOriginalPrice, sumPrice, sumDiscount, sumTax, sumTotal);


                return StatusCode(200, new
                {
                    message = "สร้างไฟล์ CSV สำเร็จ",
                    data = new
                    {
                        filePath = pathExcel,
                    },
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }





        [HttpPost]
        [Route("get_tax_summary_report_outsource")]
        public async Task<IActionResult> GetTaxSummaryReportOutsource([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_report_view).FirstOrDefaultAsync();
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

                var result = _context.view_tax_report_outsource.Where(x => x.member_id == jwtStatus.member_id).AsQueryable();

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
                                (r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.file_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.buyer_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.file_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                            );
                        }
                    }
                    else
                    {
                        if (bodyDtParameters.dateType == "issue_date")
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.file_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.file_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
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

                double sumOriginalPrice = result.Sum(s => s.original_price);
                double sumPrice = result.Sum(s => s.price);
                double sumDiscount = result.Sum(s => s.discount);
                double sumTax = result.Sum(s => s.tax);
                double sumTotal = result.Sum(s => s.total);

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.CountAsync();
                var totalResultsCount = await _context.view_tex_report.Where(x => x.member_id == jwtStatus.member_id && x.gen_xml_status == "success" && x.document_type_id == document_id).CountAsync();


                if (bodyDtParameters.Length == -1)
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.etax_id,
                        x.document_type_name,
                        x.buyer_tax_id,
                        x.buyer_name,
                        x.original_price,
                        x.price,
                        x.discount,
                        x.tax,
                        x.total,
                        x.issue_date,
                        x.create_date,
                    })
                    .ToListAsync();


                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        countTotal = filteredResultsCount,
                        sumOriginalPrice = sumOriginalPrice,
                        sumPrice = sumPrice,
                        sumDiscount = sumDiscount,
                        sumTax = sumTax,
                        sumTotal = sumTotal,
                        data = data,
                    });
                }
                else
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.etax_id,
                        x.document_type_name,
                        x.buyer_tax_id,
                        x.buyer_name,
                        x.original_price,
                        x.price,
                        x.discount,
                        x.tax,
                        x.total,
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
                        countTotal = filteredResultsCount,
                        sumOriginalPrice = sumOriginalPrice,
                        sumPrice = sumPrice,
                        sumDiscount = sumDiscount,
                        sumTax = sumTax,
                        sumTotal = sumTotal,
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
        [Route("csv_tax_summary_report_outsource")]
        public async Task<IActionResult> CsvTaxSummaryReportOutsource([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_report_view).FirstOrDefaultAsync();
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


                var searchBy = bodyDtParameters.searchText;
                var orderCriteria = "id";
                var orderAscendingDirection = true;

                if (bodyDtParameters.Order != null)
                {
                    orderCriteria = bodyDtParameters.Columns[bodyDtParameters.Order[0].Column].Data;
                    orderAscendingDirection = bodyDtParameters.Order[0].Dir.ToString().ToLower() == "asc";
                }

                var result = _context.view_tax_report_outsource.Where(x => x.member_id == jwtStatus.member_id).AsQueryable();

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
                                (r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.file_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.buyer_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.file_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
                            );
                        }
                    }
                    else
                    {
                        if (bodyDtParameters.dateType == "issue_date")
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.file_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.file_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd)
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

                double sumOriginalPrice = result.Sum(s => s.original_price);
                double sumPrice = result.Sum(s => s.price);
                double sumDiscount = result.Sum(s => s.discount);
                double sumTax = result.Sum(s => s.tax);
                double sumTotal = result.Sum(s => s.total);

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.CountAsync();
                var totalResultsCount = await _context.view_tax_report_outsource.Where(x => x.member_id == jwtStatus.member_id && x.document_type_id == document_id).CountAsync();

                List<ViewTaxReportOutsource> listData = await result.ToListAsync();

                string output = _config["Path:Share"];
                string pathExcel = "/" + member.id + "/excel/";
                Directory.CreateDirectory(output + pathExcel);
                pathExcel += "รายงานภาษีเงินได้_" + member.name + ".csv";

                Report.DefaultTexReportOutsource(output + pathExcel, bodyDtParameters, listData, sumOriginalPrice, sumPrice, sumDiscount, sumTax, sumTotal);

                return StatusCode(200, new
                {
                    message = "สร้างไฟล์ CSV สำเร็จ",
                    data = new
                    {
                        filePath = pathExcel,
                    },
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }



        //////////////////////////Admin//////////////////////////

        [HttpPost]
        [Route("admin/get_cost_summary_report")]
        public async Task<IActionResult> GetCostSummaryReportAdmin([FromBody] BodyCostReportAdmin bodyCostReportAdmin)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_report_menu).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                var membere = await (from um in _context.user_members
                                     where um.user_id == jwtStatus.user_id && um.member_id == bodyCostReportAdmin.member_id
                                     select um).FirstOrDefaultAsync();

                if (membere == null)
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                bodyCostReportAdmin.dateStart = DateTime.Parse(bodyCostReportAdmin.dateStart.ToString()).Date;
                bodyCostReportAdmin.dateEnd = bodyCostReportAdmin.dateEnd.AddDays(+1).AddMilliseconds(-1);

                var price_type = await (from mpt in _context.member_price_type
                                        where mpt.member_id == bodyCostReportAdmin.member_id
                                        select mpt).FirstOrDefaultAsync();

                List<ViewPaymentRawData> listPayment = await _context.view_payment_rawdata.Where(x => x.member_id == bodyCostReportAdmin.member_id && x.create_date >= bodyCostReportAdmin.dateStart && x.create_date <= bodyCostReportAdmin.dateEnd).ToListAsync();

                ReturnCostReport returnCostReport = new ReturnCostReport();
                returnCostReport.listReturnCostReportData = new List<ReturnCostReportData>();

                foreach (ViewPaymentRawData payment in listPayment)
                {
                    returnCostReport.listReturnCostReportData.Add(new ReturnCostReportData()
                    {
                        row_name = payment.row_name,
                        xml_count = payment.xml_count,
                        pdf_count = payment.pdf_count,
                        email_count = payment.email_count,
                        sms_count = payment.sms_message_count,
                        ebxml_count = payment.ebxml_count,
                    });

                    returnCostReport.total_xml_count += payment.xml_count;
                    returnCostReport.total_pdf_count += payment.pdf_count;
                    returnCostReport.total_email_count += payment.email_count;
                    returnCostReport.total_sms_count += payment.sms_message_count;
                    returnCostReport.total_ebxml_count += payment.ebxml_count;
                }


                var successCount = new
                {
                    xml_count = await _context.etax_files.Where(x => x.member_id == bodyCostReportAdmin.member_id && x.create_type == "api" && x.gen_xml_status == "success" && x.create_date >= bodyCostReportAdmin.dateStart && x.create_date <= bodyCostReportAdmin.dateEnd).CountAsync(),
                    pdf_count = await _context.etax_files.Where(x => x.member_id == bodyCostReportAdmin.member_id && x.create_type == "api" && x.gen_pdf_status == "success" && x.create_date >= bodyCostReportAdmin.dateStart && x.create_date <= bodyCostReportAdmin.dateEnd).CountAsync(),
                    email_count = await _context.view_send_email.Where(x => x.member_id == bodyCostReportAdmin.member_id && x.create_type == "api" && x.send_email_status == "success" && x.create_date >= bodyCostReportAdmin.dateStart && x.create_date <= bodyCostReportAdmin.dateEnd).SumAsync(x => x.send_count),
                    sms_count = await _context.view_send_sms.Where(x => x.member_id == bodyCostReportAdmin.member_id && x.create_type == "api" && x.send_sms_status == "success" && x.create_date >= bodyCostReportAdmin.dateStart && x.create_date <= bodyCostReportAdmin.dateEnd).SumAsync(x => x.message_count),
                    ebxml_count = await _context.view_send_ebxml.Where(x => x.member_id == bodyCostReportAdmin.member_id && x.create_type == "api" && x.send_ebxml_status == "success" && x.create_date >= bodyCostReportAdmin.dateStart && x.create_date <= bodyCostReportAdmin.dateEnd).CountAsync(),
                };

                returnCostReport.listReturnCostReportData.Add(new ReturnCostReportData()
                {
                    row_name = "Api",
                    xml_count = successCount.xml_count,
                    pdf_count = successCount.pdf_count,
                    email_count = successCount.email_count,
                    sms_count = successCount.sms_count,
                    ebxml_count = successCount.ebxml_count,
                });

                returnCostReport.total_xml_count += successCount.xml_count;
                returnCostReport.total_pdf_count += successCount.pdf_count;
                returnCostReport.total_email_count += successCount.email_count;
                returnCostReport.total_sms_count += successCount.sms_count;
                returnCostReport.total_ebxml_count += successCount.ebxml_count;

                List<MemberPriceXml> listMemberPriceXml = await _context.member_price_xml.Where(x => x.member_id == bodyCostReportAdmin.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPricePdf> listMemberPricePdf = await _context.member_price_pdf.Where(x => x.member_id == bodyCostReportAdmin.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPriceEmail> listMemberPriceEmail = await _context.member_price_email.Where(x => x.member_id == bodyCostReportAdmin.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPriceSms> listMemberPriceSms = await _context.member_price_sms.Where(x => x.member_id == bodyCostReportAdmin.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPriceEbxml> listMemberPriceEbxml = await _context.member_price_ebxml.Where(x => x.member_id == bodyCostReportAdmin.member_id).OrderByPropertyDescending("count").ToListAsync();


                int tmp_xml_count = returnCostReport.total_xml_count;
                foreach (MemberPriceXml memberPriceXml in listMemberPriceXml)
                {
                    if (tmp_xml_count > memberPriceXml.count)
                    {
                        returnCostReport.total_xml_price += (tmp_xml_count - memberPriceXml.count) * memberPriceXml.price;
                        tmp_xml_count = memberPriceXml.count;
                    }
                }

                int tmp_pdf_count = returnCostReport.total_pdf_count;
                foreach (MemberPricePdf memberPricePdf in listMemberPricePdf)
                {
                    if (tmp_pdf_count > memberPricePdf.count)
                    {
                        returnCostReport.total_pdf_price += (tmp_pdf_count - memberPricePdf.count) * memberPricePdf.price;
                        tmp_pdf_count = memberPricePdf.count;
                    }
                }

                if (price_type != null && price_type.email_price_type == "tran")
                {
                    var email_tran = await _context.view_send_email_list
                    .Where(x => x.member_id == bodyCostReportAdmin.member_id && x.send_email_status == "success" && x.create_date >= bodyCostReportAdmin.dateStart && x.create_date <= bodyCostReportAdmin.dateEnd)
                    .GroupBy(x => x.etax_file_id)
                    .Select(x => new
                    {
                        etax_id = x.Key,
                        count = x.Count()
                    })
                    .ToListAsync();

                    foreach (var et in email_tran)
                    {
                        int tmp_email_count = et.count;
                        foreach (MemberPriceEmail memberPriceEmail in listMemberPriceEmail)
                        {
                            if (tmp_email_count > memberPriceEmail.count)
                            {
                                returnCostReport.total_email_price += (tmp_email_count - memberPriceEmail.count) * memberPriceEmail.price;
                                tmp_email_count = memberPriceEmail.count;
                            }
                        }
                    }
                }
                else
                {
                    int tmp_email_count = returnCostReport.total_email_count;
                    foreach (MemberPriceEmail memberPriceEmail in listMemberPriceEmail)
                    {
                        if (tmp_email_count > memberPriceEmail.count)
                        {
                            returnCostReport.total_email_price += (tmp_email_count - memberPriceEmail.count) * memberPriceEmail.price;
                            tmp_email_count = memberPriceEmail.count;
                        }
                    }
                }

                int tmp_sms_count = returnCostReport.total_sms_count;
                foreach (MemberPriceSms memberPriceSms in listMemberPriceSms)
                {
                    if (tmp_sms_count > memberPriceSms.count)
                    {
                        returnCostReport.total_sms_price += (tmp_sms_count - memberPriceSms.count) * memberPriceSms.price;
                        tmp_sms_count = memberPriceSms.count;
                    }
                }

                int tmp_ebxml_count = returnCostReport.total_ebxml_count;
                foreach (MemberPriceEbxml memberPriceEbxml in listMemberPriceEbxml)
                {
                    if (tmp_ebxml_count > memberPriceEbxml.count)
                    {
                        returnCostReport.total_ebxml_price += (tmp_ebxml_count - memberPriceEbxml.count) * memberPriceEbxml.price;
                        tmp_ebxml_count = memberPriceEbxml.count;
                    }
                }

                return StatusCode(200, new
                {
                    message = "เรียกดูข้อมูลสำเร็จ",
                    data = returnCostReport,
                });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("admin/csv_cost_summary_report")]
        public async Task<IActionResult> CsvCostSummaryReportAdmin([FromBody] BodyCostReportAdmin bodyCostReportAdmin)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_report_menu).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                var membere = await (from um in _context.user_members
                                     where um.user_id == jwtStatus.user_id && um.member_id == bodyCostReportAdmin.member_id
                                     select um).FirstOrDefaultAsync();

                if (membere == null)
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                if (bodyCostReportAdmin.member_id == 0)
                    return StatusCode(400, new { message = "กรุณากำหนดลูกค้า", });

                bodyCostReportAdmin.dateStart = DateTime.Parse(bodyCostReportAdmin.dateStart.ToString()).Date;
                bodyCostReportAdmin.dateEnd = bodyCostReportAdmin.dateEnd.AddDays(+1).AddMilliseconds(-1);

                var price_type = await (from mpt in _context.member_price_type
                                        where mpt.member_id == bodyCostReportAdmin.member_id
                                        select mpt).FirstOrDefaultAsync();

                Member member = await _context.members
                .Where(x => x.id == bodyCostReportAdmin.member_id)
                .FirstOrDefaultAsync();

                List<ViewPaymentRawData> listPayment = await _context.view_payment_rawdata.Where(x => x.member_id == bodyCostReportAdmin.member_id && x.create_date >= bodyCostReportAdmin.dateStart && x.create_date <= bodyCostReportAdmin.dateEnd).ToListAsync();

                ReturnCostReport returnCostReport = new ReturnCostReport();
                returnCostReport.listReturnCostReportData = new List<ReturnCostReportData>();

                foreach (ViewPaymentRawData payment in listPayment)
                {
                    returnCostReport.listReturnCostReportData.Add(new ReturnCostReportData()
                    {
                        row_name = payment.row_name,
                        xml_count = payment.xml_count,
                        pdf_count = payment.pdf_count,
                        email_count = payment.email_count,
                        sms_count = payment.sms_message_count,
                        ebxml_count = payment.ebxml_count,
                    });

                    returnCostReport.total_xml_count += payment.xml_count;
                    returnCostReport.total_pdf_count += payment.pdf_count;
                    returnCostReport.total_email_count += payment.email_count;
                    returnCostReport.total_sms_count += payment.sms_message_count;
                    returnCostReport.total_ebxml_count += payment.ebxml_count;
                }

                var successCount = new
                {
                    xml_count = await _context.etax_files.Where(x => x.member_id == bodyCostReportAdmin.member_id && x.create_type == "api" && x.gen_xml_status == "success" && x.create_date >= bodyCostReportAdmin.dateStart && x.create_date <= bodyCostReportAdmin.dateEnd).CountAsync(),
                    pdf_count = await _context.etax_files.Where(x => x.member_id == bodyCostReportAdmin.member_id && x.create_type == "api" && x.gen_pdf_status == "success" && x.create_date >= bodyCostReportAdmin.dateStart && x.create_date <= bodyCostReportAdmin.dateEnd).CountAsync(),
                    email_count = await _context.view_send_email.Where(x => x.member_id == bodyCostReportAdmin.member_id && x.create_type == "api" && x.send_email_status == "success" && x.create_date >= bodyCostReportAdmin.dateStart && x.create_date <= bodyCostReportAdmin.dateEnd).SumAsync(x => x.send_count),
                    sms_count = await _context.view_send_sms.Where(x => x.member_id == bodyCostReportAdmin.member_id && x.create_type == "api" && x.send_sms_status == "success" && x.create_date >= bodyCostReportAdmin.dateStart && x.create_date <= bodyCostReportAdmin.dateEnd).SumAsync(x => x.message_count),
                    ebxml_count = await _context.view_send_ebxml.Where(x => x.member_id == bodyCostReportAdmin.member_id && x.create_type == "api" && x.send_ebxml_status == "success" && x.create_date >= bodyCostReportAdmin.dateStart && x.create_date <= bodyCostReportAdmin.dateEnd).CountAsync(),
                };

                returnCostReport.listReturnCostReportData.Add(new ReturnCostReportData()
                {
                    row_name = "Api",
                    xml_count = successCount.xml_count,
                    pdf_count = successCount.pdf_count,
                    email_count = successCount.email_count,
                    sms_count = successCount.sms_count,
                    ebxml_count = successCount.ebxml_count,
                });

                returnCostReport.total_xml_count += successCount.xml_count;
                returnCostReport.total_pdf_count += successCount.pdf_count;
                returnCostReport.total_email_count += successCount.email_count;
                returnCostReport.total_sms_count += successCount.sms_count;
                returnCostReport.total_ebxml_count += successCount.ebxml_count;

                List<MemberPriceXml> listMemberPriceXml = await _context.member_price_xml.Where(x => x.member_id == bodyCostReportAdmin.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPricePdf> listMemberPricePdf = await _context.member_price_pdf.Where(x => x.member_id == bodyCostReportAdmin.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPriceEmail> listMemberPriceEmail = await _context.member_price_email.Where(x => x.member_id == bodyCostReportAdmin.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPriceSms> listMemberPriceSms = await _context.member_price_sms.Where(x => x.member_id == bodyCostReportAdmin.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPriceEbxml> listMemberPriceEbxml = await _context.member_price_ebxml.Where(x => x.member_id == bodyCostReportAdmin.member_id).OrderByPropertyDescending("count").ToListAsync();




                int tmp_xml_count = returnCostReport.total_xml_count;
                foreach (MemberPriceXml memberPriceXml in listMemberPriceXml)
                {
                    if (tmp_xml_count > memberPriceXml.count)
                    {
                        returnCostReport.total_xml_price += (tmp_xml_count - memberPriceXml.count) * memberPriceXml.price;
                        tmp_xml_count = memberPriceXml.count;
                    }
                }

                int tmp_pdf_count = returnCostReport.total_pdf_count;
                foreach (MemberPricePdf memberPricePdf in listMemberPricePdf)
                {
                    if (tmp_pdf_count > memberPricePdf.count)
                    {
                        returnCostReport.total_pdf_price += (tmp_pdf_count - memberPricePdf.count) * memberPricePdf.price;
                        tmp_pdf_count = memberPricePdf.count;
                    }
                }

                if (price_type != null && price_type.email_price_type == "tran")
                {
                    var email_tran = await _context.view_send_email_list
                    .Where(x => x.member_id == bodyCostReportAdmin.member_id && x.send_email_status == "success" && x.create_date >= bodyCostReportAdmin.dateStart && x.create_date <= bodyCostReportAdmin.dateEnd)
                    .GroupBy(x => x.etax_file_id)
                    .Select(x => new
                    {
                        etax_id = x.Key,
                        count = x.Count()
                    })
                    .ToListAsync();

                    foreach (var et in email_tran)
                    {
                        int tmp_email_count = et.count;
                        foreach (MemberPriceEmail memberPriceEmail in listMemberPriceEmail)
                        {
                            if (tmp_email_count > memberPriceEmail.count)
                            {
                                returnCostReport.total_email_price += (tmp_email_count - memberPriceEmail.count) * memberPriceEmail.price;
                                tmp_email_count = memberPriceEmail.count;
                            }
                        }
                    }
                }
                else
                {
                    int tmp_email_count = returnCostReport.total_email_count;
                    foreach (MemberPriceEmail memberPriceEmail in listMemberPriceEmail)
                    {
                        if (tmp_email_count > memberPriceEmail.count)
                        {
                            returnCostReport.total_email_price += (tmp_email_count - memberPriceEmail.count) * memberPriceEmail.price;
                            tmp_email_count = memberPriceEmail.count;
                        }
                    }
                }

                int tmp_sms_count = returnCostReport.total_sms_count;
                foreach (MemberPriceSms memberPriceSms in listMemberPriceSms)
                {
                    if (tmp_sms_count > memberPriceSms.count)
                    {
                        returnCostReport.total_sms_price += (tmp_sms_count - memberPriceSms.count) * memberPriceSms.price;
                        tmp_sms_count = memberPriceSms.count;
                    }
                }

                int tmp_ebxml_count = returnCostReport.total_ebxml_count;
                foreach (MemberPriceEbxml memberPriceEbxml in listMemberPriceEbxml)
                {
                    if (tmp_ebxml_count > memberPriceEbxml.count)
                    {
                        returnCostReport.total_ebxml_price += (tmp_ebxml_count - memberPriceEbxml.count) * memberPriceEbxml.price;
                        tmp_ebxml_count = memberPriceEbxml.count;
                    }
                }


                string output = _config["Path:Share"];
                string pathExcel = "/Admin/excel/";
                Directory.CreateDirectory(output + pathExcel);
                pathExcel += "รายงานค่าใช้จ่ายปัจจุบัน" + member.name + ".csv";

                if (member.id == 5)
                {
                    Report.ThaibmaCostReport(output + pathExcel, member, bodyCostReportAdmin.dateStart, bodyCostReportAdmin.dateEnd, returnCostReport);
                }
                else
                {
                    Report.DefaultCostReport(output + pathExcel, member, bodyCostReportAdmin.dateStart, bodyCostReportAdmin.dateEnd, returnCostReport);
                }

                return StatusCode(200, new
                {
                    message = "สร้างไฟล์ CSV สำเร็จ",
                    data = new
                    {
                        filePath = pathExcel,
                    },
                });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("admin/get_tax_summary_report")]
        public async Task<IActionResult> GetTaxSummaryReportAdmin([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_report_menu).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                var membere = await (from um in _context.user_members
                                     where um.user_id == jwtStatus.user_id && um.member_id == bodyDtParameters.id
                                     select um).FirstOrDefaultAsync();

                if (membere == null)
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                var searchBy = bodyDtParameters.searchText;
                var orderCriteria = "id";
                var orderAscendingDirection = true;

                if (bodyDtParameters.Order != null)
                {
                    orderCriteria = bodyDtParameters.Columns[bodyDtParameters.Order[0].Column].Data;
                    orderAscendingDirection = bodyDtParameters.Order[0].Dir.ToString().ToLower() == "asc";
                }

                var result = _context.view_tex_report.Where(x => x.member_id == bodyDtParameters.id && x.gen_xml_status == "success" && x.delete_status == 0).AsQueryable();

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
                                (r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.buyer_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
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
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd) ||
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

                double sumPrice = result.Sum(s => s.price);
                double sumDiscount = result.Sum(s => s.discount);
                double sumTax = result.Sum(s => s.tax);
                double sumTotal = result.Sum(s => s.total);

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.CountAsync();
                var totalResultsCount = 0;


                if (bodyDtParameters.Length == -1)
                {
                    var data = await result
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        countTotal = filteredResultsCount,
                        sumPrice = sumPrice,
                        sumDiscount = sumDiscount,
                        sumTax = sumTax,
                        sumTotal = sumTotal,
                        data = data,
                    });
                }
                else
                {
                    var data = await result
                    .Skip(bodyDtParameters.Start)
                    .Take(bodyDtParameters.Length)
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        countTotal = filteredResultsCount,
                        sumPrice = sumPrice,
                        sumDiscount = sumDiscount,
                        sumTax = sumTax,
                        sumTotal = sumTotal,
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
        [Route("admin/get_email_summary_report")]
        public async Task<IActionResult> GetEmailSummaryReportAdmin([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_report_menu).FirstOrDefaultAsync();

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

                var result = _context.view_send_email_list.Where(x => x.member_id == jwtStatus.member_id).AsQueryable();

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
                                (r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
                                (r.buyer_name.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
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
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
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
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        countTotal = filteredResultsCount,
                        data = data,
                    });
                }
                else
                {
                    var data = await result
                    .Skip(bodyDtParameters.Start)
                    .Take(bodyDtParameters.Length)
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        countTotal = filteredResultsCount,
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
        [Route("admin/get_ebxml_summary_report")]
        public async Task<IActionResult> GetEbxmlSummaryReportAdmin([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_report_menu).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


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

                if (bodyDtParameters.id != 0)
                {
                    var membere = await (from um in _context.user_members
                                         where um.user_id == jwtStatus.user_id && um.member_id == bodyDtParameters.id
                                         select um).FirstOrDefaultAsync();

                    if (membere == null)
                        return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

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
                                (r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.etax_id.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
                                (r.buyer_name.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
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
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.buyer_name.Contains(searchBy) && r.send_ebxml_finish >= bodyDtParameters.dateStart && r.send_ebxml_finish <= bodyDtParameters.dateEnd) ||
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
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        countTotal = filteredResultsCount,
                        data = data,
                    });
                }
                else
                {
                    var data = await result
                   .Skip(bodyDtParameters.Start)
                   .Take(bodyDtParameters.Length)
                   .ToListAsync();

                    return StatusCode(200, new
                    {
                        draw = bodyDtParameters.Draw,
                        recordsTotal = totalResultsCount,
                        recordsFiltered = filteredResultsCount,
                        countTotal = filteredResultsCount,
                        data = data,
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
