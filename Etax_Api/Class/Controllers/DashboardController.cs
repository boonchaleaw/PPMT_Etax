
using Etax_Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class DashboardController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IJwtService _jwtService;
        public DashboardController(ApplicationDbContext context, IConfiguration config, IExceptionLogger exceptionLogger, IJwtService jwtService)
        {
            _config = config;
            _context = context;
            _context.Database.SetCommandTimeout(180);
            _exceptionLogger = exceptionLogger;
            _jwtService = jwtService;
        }

        [HttpPost]
        [Route("get_dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุแล้ว", });

                var price_type = await (from mpt in _context.member_price_type
                                        where mpt.member_id == jwtStatus.member_id
                                        select mpt).FirstOrDefaultAsync();

                var totalCount = new
                {
                    xml_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.gen_xml_status != "no" && x.xml_payment_status == "pending").CountAsync(),
                    pdf_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.gen_pdf_status != "no" && x.pdf_payment_status == "pending").CountAsync(),
                    email_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.add_email_status != "no" && x.pdf_payment_status == "pending").CountAsync(),
                    sms_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.add_sms_status != "no" && x.pdf_payment_status == "pending").CountAsync(),
                    ebxml_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.add_ebxml_status != "no" && x.xml_payment_status == "pending").CountAsync(),
                };

                var pendingCount = new
                {
                    xml_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.gen_xml_status == "pending" && x.xml_payment_status == "pending" && x.delete_status == 0).CountAsync(),
                    pdf_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.gen_pdf_status == "pending" && x.pdf_payment_status == "pending" && x.delete_status == 0).CountAsync(),
                    email_count = await _context.view_send_email.Where(x => x.member_id == jwtStatus.member_id && x.send_email_status == "pending" && x.payment_status == "pending").CountAsync(),
                    sms_count = await _context.view_send_sms.Where(x => x.member_id == jwtStatus.member_id && x.send_sms_status == "pending" && x.payment_status == "pending").CountAsync(),
                    ebxml_count = await _context.view_send_ebxml.Where(x => x.member_id == jwtStatus.member_id && x.send_ebxml_status == "pending" && x.payment_status == "pending").CountAsync(),
                };

                var successCount = new
                {
                    xml_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.gen_xml_status == "success" && x.xml_payment_status == "pending").CountAsync(),
                    pdf_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.gen_pdf_status == "success" && x.pdf_payment_status == "pending").CountAsync(),
                    email_count = await _context.view_send_email.Where(x => x.member_id == jwtStatus.member_id && x.send_email_status == "success" && x.payment_status == "pending").SumAsync(x => x.send_count),
                    ebxml_count = await _context.view_send_ebxml.Where(x => x.member_id == jwtStatus.member_id && x.send_ebxml_status == "success" && x.payment_status == "pending").CountAsync(),
                    sms_cradit_count = await _context.view_send_sms.Where(x => x.member_id == jwtStatus.member_id && x.send_sms_status == "success" && x.payment_status == "pending").SumAsync(x => x.message_count),
                    sms_count = await _context.view_send_sms.Where(x => x.member_id == jwtStatus.member_id && x.send_sms_status == "success" && x.payment_status == "pending").SumAsync(x => x.send_count),
                };

                var errorCount = new
                {
                    xml_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.gen_xml_status == "fail" && x.xml_payment_status == "pending" && x.delete_status == 0).CountAsync(),
                    pdf_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.gen_pdf_status == "fail" && x.pdf_payment_status == "pending" && x.delete_status == 0).CountAsync(),

                    email_count = await _context.view_send_email.Where(x =>
                     (x.member_id == jwtStatus.member_id && x.send_email_status == "fail" && x.payment_status == "pending") ||
                     (x.member_id == jwtStatus.member_id && x.send_email_status == "fail" && x.payment_status == null) ||
                     (x.member_id == jwtStatus.member_id && x.email_status == "fail" && x.payment_status == "pending") ||
                     (x.member_id == jwtStatus.member_id && x.email_status == "fail" && x.payment_status == null))
                    .CountAsync(),

                    sms_count = await _context.view_send_sms.Where(x =>
                     (x.member_id == jwtStatus.member_id && x.send_sms_status == "fail" && x.payment_status == "pending") ||
                     (x.member_id == jwtStatus.member_id && x.send_sms_status == "fail" && x.payment_status == null))
                    .CountAsync(),

                    ebxml_count = await _context.view_send_ebxml.Where(x =>
                     (x.member_id == jwtStatus.member_id && x.send_ebxml_status == "fail" && x.payment_status == "pending") ||
                     (x.member_id == jwtStatus.member_id && x.send_ebxml_status == "fail" && x.payment_status == null) ||
                     (x.member_id == jwtStatus.member_id && x.etax_status == "fail" && x.payment_status == "pending") ||
                     (x.member_id == jwtStatus.member_id && x.etax_status == "fail" && x.payment_status == null))
                    .CountAsync(),
                };


                List<MemberPriceXml> listMemberPriceXml = await _context.member_price_xml.Where(x => x.member_id == jwtStatus.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPricePdf> listMemberPricePdf = await _context.member_price_pdf.Where(x => x.member_id == jwtStatus.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPriceEmail> listMemberPriceEmail = await _context.member_price_email.Where(x => x.member_id == jwtStatus.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPriceEbxml> listMemberPriceEbxml = await _context.member_price_ebxml.Where(x => x.member_id == jwtStatus.member_id).OrderByPropertyDescending("count").ToListAsync();
                List<MemberPriceSms> listMemberPriceSms = await _context.member_price_sms.Where(x => x.member_id == jwtStatus.member_id).OrderByPropertyDescending("count").ToListAsync();

                double xml_price = 0;
                int xml_count = successCount.xml_count;
                foreach (MemberPriceXml memberPriceXml in listMemberPriceXml)
                {
                    if (xml_count > memberPriceXml.count)
                    {
                        xml_price += (xml_count - memberPriceXml.count) * memberPriceXml.price;
                        xml_count = memberPriceXml.count;
                    }
                }

                double pdf_price = 0;
                int pdf_count = successCount.pdf_count;
                foreach (MemberPricePdf memberPricePdf in listMemberPricePdf)
                {
                    if (pdf_count > memberPricePdf.count)
                    {
                        pdf_price += (pdf_count - memberPricePdf.count) * memberPricePdf.price;
                        pdf_count = memberPricePdf.count;
                    }
                }

                double email_price = 0;
                if (price_type != null && price_type.email_price_type == "tran")
                {
                    var email_tran = await _context.view_send_email_list
                    .Where(x => x.member_id == jwtStatus.member_id && x.send_email_status == "success" && x.payment_status == "pending")
                    .GroupBy(x => x.etax_file_id)
                    .Select(x => new
                    {
                        etax_id = x.Key,
                        count = x.Count()
                    })
                    .ToListAsync();

                    foreach (var et in email_tran)
                    {
                        int email_count = et.count;
                        foreach (MemberPriceEmail memberPriceEmail in listMemberPriceEmail)
                        {
                            if (email_count > memberPriceEmail.count)
                            {
                                email_price += (email_count - memberPriceEmail.count) * memberPriceEmail.price;
                                email_count = memberPriceEmail.count;
                            }
                        }
                    }
                }
                else
                {
                    int email_count = successCount.email_count;
                    foreach (MemberPriceEmail memberPriceEmail in listMemberPriceEmail)
                    {
                        if (email_count > memberPriceEmail.count)
                        {
                            email_price += (email_count - memberPriceEmail.count) * memberPriceEmail.price;
                            email_count = memberPriceEmail.count;
                        }
                    }
                }

                double ebxml_price = 0;
                int ebxml_count = successCount.ebxml_count;
                foreach (MemberPriceEbxml memberPriceEbxml in listMemberPriceEbxml)
                {
                    if (ebxml_count > memberPriceEbxml.count)
                    {
                        ebxml_price += (ebxml_count - memberPriceEbxml.count) * memberPriceEbxml.price;
                        ebxml_count = memberPriceEbxml.count;
                    }
                }

                double sms_price = 0;
                int sms_count = successCount.sms_cradit_count;
                foreach (MemberPriceSms memberPriceSms in listMemberPriceSms)
                {
                    if (sms_count > memberPriceSms.count)
                    {
                        sms_price += (sms_count - memberPriceSms.count) * memberPriceSms.price;
                        sms_count = memberPriceSms.count;
                    }
                }


                return StatusCode(200, new
                {
                    message = "เรียกดูข้อมูลสำเร็จ",
                    data = new
                    {
                        total_count = totalCount,
                        pending_count = pendingCount,
                        error_count = errorCount,
                        total_price = new
                        {
                            xml_price = double.Parse(xml_price.ToString("0.00")),
                            pdf_price = double.Parse(pdf_price.ToString("0.00")),
                            email_price = double.Parse(email_price.ToString("0.00")),
                            ebxml_price = double.Parse(ebxml_price.ToString("0.00")),
                            sms_price = double.Parse(sms_price.ToString("0.00")),
                        }
                    }
                });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("get_chart_count_day")]
        public async Task<IActionResult> GetChartCountWeek()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                int dateCount = 7;
                DateTime dateMax = DateTime.Today;
                DateTime dateMin = dateMax.AddDays(-dateCount);

                List<ViewEtaxCountDay> listEtaxCountDay = await _context.view_etax_count_day
                .Where(x => x.member_id == jwtStatus.member_id && x.create_date >= dateMin && x.create_date <= dateMax)
                .ToListAsync();

                List<ViewEtaxCountDay> listEtaxCountDayNew = new List<ViewEtaxCountDay>();
                for (int i = 0; i < dateCount; i++)
                {
                    DateTime dateCheck = dateMax.AddDays(-i);
                    ViewEtaxCountDay etaxCountDayNew = listEtaxCountDay.Find(x => x.create_date == dateCheck);
                    if (etaxCountDayNew != null)
                    {
                        listEtaxCountDayNew.Add(etaxCountDayNew);
                    }
                    else
                    {
                        listEtaxCountDayNew.Add(new ViewEtaxCountDay()
                        {
                            create_date = dateCheck.Date,
                            file_total = 0,
                        });
                    }
                }

                if (listEtaxCountDayNew != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            etax_count_day = listEtaxCountDayNew,
                        }
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
        [Route("get_chart_count_month")]
        public async Task<IActionResult> GetChartCountMonth()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                int monthCount = 12;

                DateTime dateMax = DateTime.ParseExact(DateTime.Today.AddMonths(1).ToString("yyyy-MM-", new CultureInfo("en-US")) + "01", "yyyy-MM-dd", new CultureInfo("en-US")).AddDays(-1);
                DateTime dateMin = DateTime.ParseExact(DateTime.Today.AddMonths(-monthCount).ToString("yyyy-MM-", new CultureInfo("en-US")) + "01", "yyyy-MM-dd", new CultureInfo("en-US"));

                List<ViewEtaxCountMonth> listEtaxCountMonth = await _context.view_etax_count_month
                .Where(x => x.member_id == jwtStatus.member_id && x.create_date_min >= dateMin && x.create_date_max < dateMax)
                .ToListAsync();

                List<ViewEtaxCountMonth> listEtaxCountMonthNew = new List<ViewEtaxCountMonth>();
                for (int i = 0; i < monthCount; i++)
                {
                    DateTime dateCheck = dateMax.AddMonths(-i);
                    ViewEtaxCountMonth etaxCountMonthyNew = listEtaxCountMonth.Find(x => x.date_month == dateCheck.Month && x.date_year == dateCheck.Year);
                    if (etaxCountMonthyNew != null)
                    {
                        listEtaxCountMonthNew.Add(etaxCountMonthyNew);
                    }
                    else
                    {
                        listEtaxCountMonthNew.Add(new ViewEtaxCountMonth()
                        {
                            date_month = dateCheck.Month,
                            date_year = dateCheck.Year,
                            file_total = 0,
                        });
                    }
                }

                if (listEtaxCountMonthNew != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            etax_count_day = listEtaxCountMonthNew,
                        }
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
        [Route("get_chart_etax_price")]
        public async Task<IActionResult> GetEtaxPrice()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                int monthCount = 12;

                DateTime dateMax = DateTime.ParseExact(DateTime.Today.AddMonths(1).ToString("yyyy-MM-", new CultureInfo("en-US")) + "01", "yyyy-MM-dd", new CultureInfo("en-US")).AddDays(-1);
                DateTime dateMin = DateTime.ParseExact(DateTime.Today.AddMonths(-monthCount).ToString("yyyy-MM-", new CultureInfo("en-US")) + "01", "yyyy-MM-dd", new CultureInfo("en-US"));

                List<ViewEtaxPrice> listViewEtaxPrice = await _context.view_etax_price
                .Where(x => x.member_id == jwtStatus.member_id && x.create_date_min >= dateMin && x.create_date_max < dateMax)
                .ToListAsync();

                List<ViewEtaxPrice> listEtaxPriceNew = new List<ViewEtaxPrice>();
                for (int i = 0; i < monthCount; i++)
                {
                    DateTime dateCheck = dateMax.AddMonths(-i);
                    ViewEtaxPrice etaxPriceNew = listViewEtaxPrice.Find(x => x.date_month == dateCheck.Month && x.date_year == dateCheck.Year);
                    if (etaxPriceNew != null)
                    {
                        listEtaxPriceNew.Add(etaxPriceNew);
                    }
                    else
                    {
                        listEtaxPriceNew.Add(new ViewEtaxPrice()
                        {
                            date_month = dateCheck.Month,
                            date_year = dateCheck.Year,
                            price = 0,
                            tax = 0,
                            total = 0,
                        });
                    }
                }

                if (listEtaxPriceNew != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            etax_price = listEtaxPriceNew,
                        }
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
        [Route("admin/get_chart_count_day")]
        public async Task<IActionResult> GetChartCountWeekAdmin()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                int dateCount = 7;
                DateTime dateMax = DateTime.Today;
                DateTime dateMin = dateMax.AddDays(-dateCount);

                List<ViewEtaxCountDay> listEtaxCountDay = await _context.view_etax_count_day
                .Where(x => x.create_date >= dateMin && x.create_date <= dateMax)
                .ToListAsync();

                List<ViewEtaxCountDay> listEtaxCountDayNew = new List<ViewEtaxCountDay>();
                for (int i = 0; i < dateCount; i++)
                {
                    DateTime dateCheck = dateMax.AddDays(-i);
                    ViewEtaxCountDay etaxCountDayNew = listEtaxCountDay.Find(x => x.create_date == dateCheck);
                    if (etaxCountDayNew != null)
                    {
                        listEtaxCountDayNew.Add(etaxCountDayNew);
                    }
                    else
                    {
                        listEtaxCountDayNew.Add(new ViewEtaxCountDay()
                        {
                            create_date = dateCheck.Date,
                            file_total = 0,
                        });
                    }
                }

                if (listEtaxCountDayNew != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            etax_count_day = listEtaxCountDayNew,
                        }
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
        [Route("admin/get_chart_count_month")]
        public async Task<IActionResult> GetChartCountMonthAdmin()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                int monthCount = 12;

                DateTime dateMax = DateTime.ParseExact(DateTime.Today.AddMonths(1).ToString("yyyy-MM-", new CultureInfo("en-US")) + "01", "yyyy-MM-dd", new CultureInfo("en-US")).AddDays(-1);
                DateTime dateMin = DateTime.ParseExact(DateTime.Today.AddMonths(-monthCount).ToString("yyyy-MM-", new CultureInfo("en-US")) + "01", "yyyy-MM-dd", new CultureInfo("en-US"));

                List<int> membereId = await (from um in _context.user_members
                                             where um.user_id == jwtStatus.user_id
                                             select um.member_id).ToListAsync();

                List<ViewEtaxCountMonth> listEtaxCountMonth = await _context.view_etax_count_month
                .Where(x => membereId.Contains(x.member_id) && x.create_date_min >= dateMin && x.create_date_max < dateMax)
                .ToListAsync();

                List<ViewEtaxCountMonth> listEtaxCountMonthNew = new List<ViewEtaxCountMonth>();
                for (int i = 0; i < monthCount; i++)
                {
                    DateTime dateCheck = dateMax.AddMonths(-i);
                    List<ViewEtaxCountMonth> etaxCountMonthyNew = listEtaxCountMonth.Where(x => x.date_month == dateCheck.Month && x.date_year == dateCheck.Year).ToList();
                    if (etaxCountMonthyNew.Count() > 0)
                    {
                        int file_total = 0;
                        foreach (ViewEtaxCountMonth e in etaxCountMonthyNew)
                        {
                            file_total += e.file_total;
                        }

                        listEtaxCountMonthNew.Add(new ViewEtaxCountMonth()
                        {
                            date_month = dateCheck.Month,
                            date_year = dateCheck.Year,
                            file_total = file_total,
                        });
                    }
                    else
                    {
                        listEtaxCountMonthNew.Add(new ViewEtaxCountMonth()
                        {
                            date_month = dateCheck.Month,
                            date_year = dateCheck.Year,
                            file_total = 0,
                        });
                    }
                }

                if (listEtaxCountMonthNew != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            etax_count_day = listEtaxCountMonthNew,
                        }
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
        [Route("admin/get_dashboard")]
        public async Task<IActionResult> GetDashboardAdmin([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                List<ReturnMemberEtaxCount> listMemberEtaxCount = new List<ReturnMemberEtaxCount>();

                List<Member> listMember = _context.members.ToList();

                DateTime dateEnd = DateTime.ParseExact(bodyDtParameters.dateEnd.ToString("yyyy-MM-dd", new CultureInfo("en-US")), "yyyy-MM-dd", new CultureInfo("en-US"));
                DateTime dateStart = DateTime.ParseExact(bodyDtParameters.dateStart.ToString("yyyy-MM-dd", new CultureInfo("en-US")), "yyyy-MM-dd", new CultureInfo("en-US"));
                dateEnd = dateEnd.AddDays(1);

                //List<ViewTotalReport> listTotal = _context.view_total_report
                //    .Where(x => x.Date >= dateStart && x.Date <= dateEnd)
                //    .Select(x => new ViewTotalReport
                //    {
                //        id = x.id,
                //        member_name = "",
                //        xml_count = x.xml_count,
                //        pdf_count = x.pdf_count,
                //        email_count = x.email_count,
                //        sms_count = x.sms_count,
                //        sms_message_count = x.sms_message_count,
                //        ebxml_count = x.ebxml_count
                //    })
                //    .ToList();



                List<int> membereId = await (from um in _context.user_members
                                             where um.user_id == jwtStatus.user_id
                                             select um.member_id).ToListAsync();

                //แก้ไขการดึงข้อมูลให้ดึงข้อมูลตามวันที่ที่เลือก
                List<ViewTotalReport> listTotal = await (from ef in _context.etax_files
                                                         join sem in _context.send_email on ef.id equals sem.etax_file_id into semGroup
                                                         from sem in semGroup.DefaultIfEmpty()
                                                         join se in _context.send_sms on ef.id equals se.etax_file_id into seGroup
                                                         from se in seGroup.DefaultIfEmpty()
                                                         join seb in _context.send_ebxml on ef.id equals seb.etax_file_id into sebGroup
                                                         from seb in sebGroup.DefaultIfEmpty()
                                                         where ef.create_date >= dateStart && ef.create_date < dateEnd && membereId.Contains(ef.member_id)
                                                         group new { ef, sem, se, seb } by ef.member_id into g
                                                         select new ViewTotalReport
                                                         {
                                                             id = g.Key,
                                                             xml_count = g.Sum(x => x.ef.gen_xml_status == "success" ? 1 : 0),
                                                             pdf_count = g.Sum(x => x.ef.gen_pdf_status == "success" ? 1 : 0),
                                                             email_count = g.Sum(x => x.sem != null && x.sem.send_email_status == "success" ? x.sem.send_count : 0),
                                                             sms_count = g.Sum(x => x.se != null && x.se.send_sms_status == "success" ? x.se.send_count : 0),
                                                             sms_message_count = g.Sum(x => x.se != null && x.se.send_sms_status == "success" ? x.se.message_count : 0),
                                                             ebxml_count = g.Sum(x => x.seb != null && x.seb.send_ebxml_status == "success" ? 1 : 0)
                                                         }).ToListAsync();

                foreach (ViewTotalReport total in listTotal)
                {
                    total.member_name = listMember.Where(x => x.id == total.id).First().name;
                }

                return StatusCode(200, new
                {
                    message = "เรียกดูข้อมูลสำเร็จ",
                    data = listTotal,
                });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

    }
}
