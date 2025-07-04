
using Etax_Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class PriceController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IJwtService _jwtService;
        public PriceController(ApplicationDbContext context, IConfiguration config, IExceptionLogger exceptionLogger, IJwtService jwtService)
        {
            _config = config;
            _context = context;
            _context.Database.SetCommandTimeout(180);
            _exceptionLogger = exceptionLogger;
            _jwtService = jwtService;
        }
        [HttpPost]
        [Route("get_price_list")]
        public async Task<IActionResult> GetPriceList([FromBody] BodyDateFilter bodyDateFilter)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                bodyDateFilter.dateStart = DateTime.Parse(bodyDateFilter.dateStart.ToString()).Date;
                bodyDateFilter.dateEnd = bodyDateFilter.dateEnd.AddDays(+1).AddMilliseconds(-1);


                var price_type = await (from mpt in _context.member_price_type
                                        where mpt.member_id == jwtStatus.member_id
                                        select mpt).FirstOrDefaultAsync();


                List<ReturnPrice> listPriceXml = await _context.member_price_xml
                .Where(x => x.member_id == jwtStatus.member_id)
                .Select(x => new ReturnPrice()
                {
                    id = x.id,
                    member_id = x.member_id,
                    count = x.count,
                    price = x.price,
                })
                .ToListAsync();

                List<ReturnPrice> listPricePdf = await _context.member_price_pdf
                .Where(x => x.member_id == jwtStatus.member_id)
                .Select(x => new ReturnPrice()
                {
                    id = x.id,
                    member_id = x.member_id,
                    count = x.count,
                    price = x.price,
                })
                .ToListAsync();

                List<ReturnPrice> listPriceEmail = await _context.member_price_email
                .Where(x => x.member_id == jwtStatus.member_id)
                .Select(x => new ReturnPrice()
                {
                    id = x.id,
                    member_id = x.member_id,
                    count = x.count,
                    price = x.price,
                })
                .ToListAsync();

                List<ReturnPrice> listPriceSms = await _context.member_price_sms
                .Where(x => x.member_id == jwtStatus.member_id)
                .Select(x => new ReturnPrice()
                {
                    id = x.id,
                    member_id = x.member_id,
                    count = x.count,
                    price = x.price,
                })
                .ToListAsync();

                List<ReturnPrice> listPriceEbxml = await _context.member_price_ebxml
                .Where(x => x.member_id == jwtStatus.member_id)
                .Select(x => new ReturnPrice()
                {
                    id = x.id,
                    member_id = x.member_id,
                    count = x.count,
                    price = x.price,
                })
                .ToListAsync();

                var successCount = new
                {
                    xml_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.gen_xml_status == "success" && x.create_date >= bodyDateFilter.dateStart && x.create_date <= bodyDateFilter.dateEnd).CountAsync(),
                    pdf_count = await _context.etax_files.Where(x => x.member_id == jwtStatus.member_id && x.gen_pdf_status == "success" && x.create_date >= bodyDateFilter.dateStart && x.create_date <= bodyDateFilter.dateEnd).CountAsync(),
                    email_count = await _context.view_send_email.Where(x => x.member_id == jwtStatus.member_id && x.send_email_status == "success" && x.create_date >= bodyDateFilter.dateStart && x.create_date <= bodyDateFilter.dateEnd).SumAsync(x => x.send_count),
                    sms_count = await _context.view_send_sms.Where(x => x.member_id == jwtStatus.member_id && x.send_sms_status == "success" && x.create_date >= bodyDateFilter.dateStart && x.create_date <= bodyDateFilter.dateEnd).SumAsync(x => x.message_count),
                    ebxml_count = await _context.view_send_ebxml.Where(x => x.member_id == jwtStatus.member_id && x.send_ebxml_status == "success" && x.create_date >= bodyDateFilter.dateStart && x.create_date <= bodyDateFilter.dateEnd).CountAsync(),
                };

                double priceTotalXml = 0;
                int xml_count = successCount.xml_count;
                for (int i = (listPriceXml.Count - 1); i >= 0; i--)
                {
                    if (xml_count > listPriceXml[i].count)
                    {
                        listPriceXml[i].count_use = (xml_count - listPriceXml[i].count);
                        listPriceXml[i].price_use = listPriceXml[i].count_use * listPriceXml[i].price;
                        priceTotalXml += listPriceXml[i].price_use;
                        xml_count = listPriceXml[i].count;
                    }
                }

                double priceTotalPdf = 0;
                int pdf_count = successCount.pdf_count;
                for (int i = (listPricePdf.Count - 1); i >= 0; i--)
                {
                    if (pdf_count > listPricePdf[i].count)
                    {
                        listPricePdf[i].count_use = (pdf_count - listPricePdf[i].count);
                        listPricePdf[i].price_use = listPricePdf[i].count_use * listPricePdf[i].price;
                        priceTotalPdf += listPricePdf[i].price_use;
                        pdf_count = listPricePdf[i].count;
                    }
                }

                double priceTotalEmail = 0;
                if (price_type != null && price_type.email_price_type == "tran")
                {
                    var email_tran = await _context.view_send_email_list
                    .Where(x => x.member_id == jwtStatus.member_id && x.send_email_status == "success" && x.create_date >= bodyDateFilter.dateStart && x.create_date <= bodyDateFilter.dateEnd)
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
                        for (int i = (listPriceEmail.Count - 1); i >= 0; i--)
                        {
                            if (email_count > listPriceEmail[i].count)
                            {
                                int count_use = (email_count - listPriceEmail[i].count);
                                double price_use = count_use * listPriceEmail[i].price;
                                listPriceEmail[i].count_use += count_use;
                                listPriceEmail[i].price_use += price_use;
                                priceTotalEmail += price_use;
                                email_count = listPriceEmail[i].count;
                            }
                        }
                    }
                }
                else
                {
                    int email_count = successCount.email_count;
                    for (int i = (listPriceEmail.Count - 1); i >= 0; i--)
                    {
                        if (email_count > listPriceEmail[i].count)
                        {
                            listPriceEmail[i].count_use = (email_count - listPriceEmail[i].count);
                            listPriceEmail[i].price_use = listPriceEmail[i].count_use * listPriceEmail[i].price;
                            priceTotalEmail += listPriceEmail[i].price_use;
                            email_count = listPriceEmail[i].count;
                        }
                    }
                }

                double priceTotalSms = 0;
                int sms_count = successCount.sms_count;
                for (int i = (listPriceSms.Count - 1); i >= 0; i--)
                {
                    if (sms_count > listPriceSms[i].count)
                    {
                        listPriceSms[i].count_use = (sms_count - listPriceSms[i].count);
                        listPriceSms[i].price_use = listPriceSms[i].count_use * listPriceSms[i].price;
                        priceTotalSms += listPriceSms[i].price_use;
                        sms_count = listPriceSms[i].count;
                    }
                }

                double priceTotalEbxml = 0;
                int ebxml_count = successCount.ebxml_count;
                for (int i = (listPriceEbxml.Count - 1); i >= 0; i--)
                {
                    if (ebxml_count > listPriceEbxml[i].count)
                    {
                        listPriceEbxml[i].count_use = (ebxml_count - listPriceEbxml[i].count);
                        listPriceEbxml[i].price_use = listPriceEbxml[i].count_use * listPriceEbxml[i].price;
                        priceTotalEbxml += listPriceEbxml[i].price_use;
                        ebxml_count = listPriceEbxml[i].count;
                    }
                }

                if (listPriceXml.Count == 0)
                {
                    listPriceXml.Add(new ReturnPrice()
                    {
                        count = 0,
                        price = 0,
                        count_use = successCount.xml_count,
                        price_use = 0,
                    });
                }

                if (listPricePdf.Count == 0)
                {
                    listPricePdf.Add(new ReturnPrice()
                    {
                        count = 0,
                        price = 0,
                        count_use = successCount.pdf_count,
                        price_use = 0,
                    });
                }

                if (listPriceEmail.Count == 0)
                {
                    listPriceEmail.Add(new ReturnPrice()
                    {
                        count = 0,
                        price = 0,
                        count_use = successCount.email_count,
                        price_use = 0,
                    });
                }

                if (listPriceSms.Count == 0)
                {
                    listPriceSms.Add(new ReturnPrice()
                    {
                        count = 0,
                        price = 0,
                        count_use = successCount.sms_count,
                        price_use = 0,
                    });
                }

                if (listPriceEbxml.Count == 0)
                {
                    listPriceEbxml.Add(new ReturnPrice()
                    {
                        count = 0,
                        price = 0,
                        count_use = successCount.ebxml_count,
                        price_use = 0,
                    });
                }


                return StatusCode(200, new
                {
                    message = "เรียกดูข้อมูลสำเร็จ",
                    data = new
                    {
                        priceList = new
                        {
                            countXml = successCount.xml_count,
                            countPdf = successCount.pdf_count,
                            countEmail = successCount.email_count,
                            countSms = successCount.sms_count,
                            countEbxml = successCount.ebxml_count,
                            priceTotalXml = priceTotalXml,
                            priceTotalPdf = priceTotalPdf,
                            priceTotalEmail = priceTotalEmail,
                            priceTotalSms = priceTotalSms,
                            priceTotalEbxml = priceTotalEbxml,
                            listPriceXml = listPriceXml,
                            listPricePdf = listPricePdf,
                            listPriceEmail = listPriceEmail,
                            listPriceSms = listPriceSms,
                            listPriceEbxml = listPriceEbxml,
                        },
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }


        //////////////////////////Admin//////////////////////////

        [HttpPost]
        [Route("admin/get_price_list")]
        public async Task<IActionResult> GetPriceListAdmin([FromBody] BodyDateFilter bodyDateFilter)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var checkMember = await (from um in _context.user_members
                                      where um.user_id == jwtStatus.user_id && um.member_id == bodyDateFilter.member_id
                                      select um).FirstOrDefaultAsync();

                if(checkMember == null)
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                bodyDateFilter.dateStart = DateTime.Parse(bodyDateFilter.dateStart.ToString()).Date;
                bodyDateFilter.dateEnd = bodyDateFilter.dateEnd.AddDays(+1).AddMilliseconds(-1);

                var price_type = await (from mpt in _context.member_price_type
                                        where mpt.member_id == bodyDateFilter.member_id
                                        select mpt).FirstOrDefaultAsync();


                List<ReturnPrice> listPriceXml = await _context.member_price_xml
                .Where(x => x.member_id == bodyDateFilter.member_id)
                .Select(x => new ReturnPrice()
                {
                    id = x.id,
                    member_id = x.member_id,
                    count = x.count,
                    price = x.price,
                })
                .ToListAsync();

                List<ReturnPrice> listPricePdf = await _context.member_price_pdf
                .Where(x => x.member_id == bodyDateFilter.member_id)
                .Select(x => new ReturnPrice()
                {
                    id = x.id,
                    member_id = x.member_id,
                    count = x.count,
                    price = x.price,
                })
                .ToListAsync();

                List<ReturnPrice> listPriceEmail = await _context.member_price_email
                .Where(x => x.member_id == bodyDateFilter.member_id)
                .Select(x => new ReturnPrice()
                {
                    id = x.id,
                    member_id = x.member_id,
                    count = x.count,
                    price = x.price,
                })
                .ToListAsync();

                List<ReturnPrice> listPriceSms = await _context.member_price_sms
                .Where(x => x.member_id == bodyDateFilter.member_id)
                .Select(x => new ReturnPrice()
                {
                    id = x.id,
                    member_id = x.member_id,
                    count = x.count,
                    price = x.price,
                })
                .ToListAsync();

                List<ReturnPrice> listPriceEbxml = await _context.member_price_ebxml
                .Where(x => x.member_id == bodyDateFilter.member_id)
                .Select(x => new ReturnPrice()
                {
                    id = x.id,
                    member_id = x.member_id,
                    count = x.count,
                    price = x.price,
                })
                .ToListAsync();

                var successCount = new
                {
                    xml_count = await _context.etax_files.Where(x => x.member_id == bodyDateFilter.member_id && x.gen_xml_status == "success" && x.create_date >= bodyDateFilter.dateStart && x.create_date <= bodyDateFilter.dateEnd).CountAsync(),
                    pdf_count = await _context.etax_files.Where(x => x.member_id == bodyDateFilter.member_id && x.gen_pdf_status == "success" && x.create_date >= bodyDateFilter.dateStart && x.create_date <= bodyDateFilter.dateEnd).CountAsync(),
                    email_count = await _context.view_send_email.Where(x => x.member_id == bodyDateFilter.member_id && x.send_email_status == "success" && x.create_date >= bodyDateFilter.dateStart && x.create_date <= bodyDateFilter.dateEnd).SumAsync(x => x.send_count),
                    sms_count = await _context.view_send_sms.Where(x => x.member_id == bodyDateFilter.member_id && x.send_sms_status == "success" && x.create_date >= bodyDateFilter.dateStart && x.create_date <= bodyDateFilter.dateEnd).SumAsync(x => x.message_count),
                    ebxml_count = await _context.view_send_ebxml.Where(x => x.member_id == bodyDateFilter.member_id && x.send_ebxml_status == "success" && x.create_date >= bodyDateFilter.dateStart && x.create_date <= bodyDateFilter.dateEnd).CountAsync(),
                };

                double priceTotalXml = 0;
                int xml_count = successCount.xml_count;
                for (int i = (listPriceXml.Count - 1); i >= 0; i--)
                {
                    if (xml_count > listPriceXml[i].count)
                    {
                        listPriceXml[i].count_use = (xml_count - listPriceXml[i].count);
                        listPriceXml[i].price_use = listPriceXml[i].count_use * listPriceXml[i].price;
                        priceTotalXml += listPriceXml[i].price_use;
                        xml_count = listPriceXml[i].count;
                    }
                }

                double priceTotalPdf = 0;
                int pdf_count = successCount.pdf_count;
                for (int i = (listPricePdf.Count - 1); i >= 0; i--)
                {
                    if (pdf_count > listPricePdf[i].count)
                    {
                        listPricePdf[i].count_use = (pdf_count - listPricePdf[i].count);
                        listPricePdf[i].price_use = listPricePdf[i].count_use * listPricePdf[i].price;
                        priceTotalPdf += listPricePdf[i].price_use;
                        pdf_count = listPricePdf[i].count;
                    }
                }

                double priceTotalEmail = 0;
                if (price_type != null && price_type.email_price_type == "tran")
                {
                    var email_tran = await _context.view_send_email_list
                    .Where(x => x.member_id == bodyDateFilter.member_id && x.send_email_status == "success" && x.create_date >= bodyDateFilter.dateStart && x.create_date <= bodyDateFilter.dateEnd)
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
                        for (int i = (listPriceEmail.Count - 1); i >= 0; i--)
                        {
                            if (email_count > listPriceEmail[i].count)
                            {
                                int count_use = (email_count - listPriceEmail[i].count);
                                double price_use = count_use * listPriceEmail[i].price;
                                listPriceEmail[i].count_use += count_use;
                                listPriceEmail[i].price_use += price_use;
                                priceTotalEmail += price_use;
                                email_count = listPriceEmail[i].count;
                            }
                        }
                    }
                }
                else
                {
                    int email_count = successCount.email_count;
                    for (int i = (listPriceEmail.Count - 1); i >= 0; i--)
                    {
                        if (email_count > listPriceEmail[i].count)
                        {
                            listPriceEmail[i].count_use = (email_count - listPriceEmail[i].count);
                            listPriceEmail[i].price_use = listPriceEmail[i].count_use * listPriceEmail[i].price;
                            priceTotalEmail += listPriceEmail[i].price_use;
                            email_count = listPriceEmail[i].count;
                        }
                    }
                }

                double priceTotalSms = 0;
                int sms_count = successCount.sms_count;
                for (int i = (listPriceSms.Count - 1); i >= 0; i--)
                {
                    if (sms_count > listPriceSms[i].count)
                    {
                        listPriceSms[i].count_use = (sms_count - listPriceSms[i].count);
                        listPriceSms[i].price_use = listPriceSms[i].count_use * listPriceSms[i].price;
                        priceTotalSms += listPriceSms[i].price_use;
                        sms_count = listPriceSms[i].count;
                    }
                }

                double priceTotalEbxml = 0;
                int ebxml_count = successCount.ebxml_count;
                for (int i = (listPriceEbxml.Count - 1); i >= 0; i--)
                {
                    if (ebxml_count > listPriceEbxml[i].count)
                    {
                        listPriceEbxml[i].count_use = (ebxml_count - listPriceEbxml[i].count);
                        listPriceEbxml[i].price_use = listPriceEbxml[i].count_use * listPriceEbxml[i].price;
                        priceTotalEbxml += listPriceEbxml[i].price_use;
                        ebxml_count = listPriceEbxml[i].count;
                    }
                }

                if (listPriceXml.Count == 0)
                {
                    listPriceXml.Add(new ReturnPrice()
                    {
                        count = 0,
                        price = 0,
                        count_use = successCount.xml_count,
                        price_use = 0,
                    });
                }

                if (listPricePdf.Count == 0)
                {
                    listPricePdf.Add(new ReturnPrice()
                    {
                        count = 0,
                        price = 0,
                        count_use = successCount.pdf_count,
                        price_use = 0,
                    });
                }

                if (listPriceEmail.Count == 0)
                {
                    listPriceEmail.Add(new ReturnPrice()
                    {
                        count = 0,
                        price = 0,
                        count_use = successCount.email_count,
                        price_use = 0,
                    });
                }

                if (listPriceSms.Count == 0)
                {
                    listPriceSms.Add(new ReturnPrice()
                    {
                        count = 0,
                        price = 0,
                        count_use = successCount.sms_count,
                        price_use = 0,
                    });
                }

                if (listPriceEbxml.Count == 0)
                {
                    listPriceEbxml.Add(new ReturnPrice()
                    {
                        count = 0,
                        price = 0,
                        count_use = successCount.ebxml_count,
                        price_use = 0,
                    });
                }


                return StatusCode(200, new
                {
                    message = "เรียกดูข้อมูลสำเร็จ",
                    data = new
                    {
                        priceList = new
                        {
                            countXml = successCount.xml_count,
                            countPdf = successCount.pdf_count,
                            countEmail = successCount.email_count,
                            countSms = successCount.sms_count,
                            countEbxml = successCount.ebxml_count,
                            priceTotalXml = priceTotalXml,
                            priceTotalPdf = priceTotalPdf,
                            priceTotalEmail = priceTotalEmail,
                            priceTotalSms = priceTotalSms,
                            priceTotalEbxml = priceTotalEbxml,
                            listPriceXml = listPriceXml,
                            listPricePdf = listPricePdf,
                            listPriceEmail = listPriceEmail,
                            listPriceSms = listPriceSms,
                            listPriceEbxml = listPriceEbxml,
                        },
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("admin/get_price_list_edit/{id}")]
        public async Task<IActionResult> GetPriceListEditAdmin(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                List<MemberPriceXml> listMemberPriceXml = await _context.member_price_xml.Where(x => x.member_id == id).OrderByProperty("count").ToListAsync();
                List<MemberPricePdf> listMemberPricePdf = await _context.member_price_pdf.Where(x => x.member_id == id).OrderByProperty("count").ToListAsync();
                List<MemberPriceEmail> listMemberPriceEmail = await _context.member_price_email.Where(x => x.member_id == id).OrderByProperty("count").ToListAsync();
                List<MemberPriceSms> listMemberPriceSms = await _context.member_price_sms.Where(x => x.member_id == id).OrderByProperty("count").ToListAsync();
                List<MemberPriceEbxml> listMemberPriceEbxml = await _context.member_price_ebxml.Where(x => x.member_id == id).OrderByProperty("count").ToListAsync();

                return StatusCode(200, new
                {
                    message = "เรียกดูข้อมูลสำเร็จ",
                    data = new
                    {
                        listMemberPriceXml = listMemberPriceXml,
                        listMemberPricePdf = listMemberPricePdf,
                        listMemberPriceEmail = listMemberPriceEmail,
                        listMemberPriceSms = listMemberPriceSms,
                        listMemberPriceEbxml = listMemberPriceEbxml,
                    },
                });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("admin/add_price_xml")]
        public async Task<IActionResult> AddPriceXml([FromBody] BodyPrice bodyPrice)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                if (bodyPrice.price <= 0)
                    return StatusCode(400, new { message = "กรุณากำหนดราคา", });

                var checkPrice = _context.member_price_xml.Where(x => x.member_id == bodyPrice.member_id && x.count == bodyPrice.count).FirstOrDefault();

                if (checkPrice != null)
                    return StatusCode(400, new { message = "จำนวนนี้มีแล้วในระบบ", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        MemberPriceXml member_price_xml = new MemberPriceXml()
                        {
                            member_id = bodyPrice.member_id,
                            count = bodyPrice.count,
                            price = bodyPrice.price,
                        };
                        _context.Add(member_price_xml);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "เพิ่มข้อมูลสำเร็จ",
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
        [Route("admin/add_price_pdf")]
        public async Task<IActionResult> AddPricePdf([FromBody] BodyPrice bodyPrice)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                if (bodyPrice.price <= 0)
                    return StatusCode(400, new { message = "กรุณากำหนดราคา", });

                var checkPrice = _context.member_price_pdf.Where(x => x.member_id == bodyPrice.member_id && x.count == bodyPrice.count).FirstOrDefault();

                if (checkPrice != null)
                    return StatusCode(400, new { message = "จำนวนนี้มีแล้วในระบบ", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        MemberPricePdf member_price_pdf = new MemberPricePdf()
                        {
                            member_id = bodyPrice.member_id,
                            count = bodyPrice.count,
                            price = bodyPrice.price,
                        };
                        _context.Add(member_price_pdf);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "เพิ่มข้อมูลสำเร็จ",
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
        [Route("admin/add_price_email")]
        public async Task<IActionResult> AddPriceEmail([FromBody] BodyPrice bodyPrice)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                if (bodyPrice.price <= 0)
                    return StatusCode(400, new { message = "กรุณากำหนดราคา", });

                var checkPrice = _context.member_price_email.Where(x => x.member_id == bodyPrice.member_id && x.count == bodyPrice.count).FirstOrDefault();

                if (checkPrice != null)
                    return StatusCode(400, new { message = "จำนวนนี้มีแล้วในระบบ", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        MemberPriceEmail member_price_email = new MemberPriceEmail()
                        {
                            member_id = bodyPrice.member_id,
                            count = bodyPrice.count,
                            price = bodyPrice.price,
                        };
                        _context.Add(member_price_email);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "เพิ่มข้อมูลสำเร็จ",
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
        [Route("admin/add_price_sms")]
        public async Task<IActionResult> AddPriceSms([FromBody] BodyPrice bodyPrice)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                if (bodyPrice.price <= 0)
                    return StatusCode(400, new { message = "กรุณากำหนดราคา", });

                var checkPrice = _context.member_price_sms.Where(x => x.member_id == bodyPrice.member_id && x.count == bodyPrice.count).FirstOrDefault();

                if (checkPrice != null)
                    return StatusCode(400, new { message = "จำนวนนี้มีแล้วในระบบ", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        MemberPriceSms member_price_sms = new MemberPriceSms()
                        {
                            member_id = bodyPrice.member_id,
                            count = bodyPrice.count,
                            price = bodyPrice.price,
                        };
                        _context.Add(member_price_sms);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "เพิ่มข้อมูลสำเร็จ",
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
        [Route("admin/add_price_ebxml")]
        public async Task<IActionResult> AddPriceEbxml([FromBody] BodyPrice bodyPrice)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                if (bodyPrice.price <= 0)
                    return StatusCode(400, new { message = "กรุณากำหนดราคา", });

                var checkPrice = _context.member_price_ebxml.Where(x => x.member_id == bodyPrice.member_id && x.count == bodyPrice.count).FirstOrDefault();

                if (checkPrice != null)
                    return StatusCode(400, new { message = "จำนวนนี้มีแล้วในระบบ", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        MemberPriceEbxml member_price_ebxml = new MemberPriceEbxml()
                        {
                            member_id = bodyPrice.member_id,
                            count = bodyPrice.count,
                            price = bodyPrice.price,
                        };
                        _context.Add(member_price_ebxml);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "เพิ่มข้อมูลสำเร็จ",
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
        [Route("admin/delete_price_xml/{id}")]
        public async Task<IActionResult> DeletePriceXml(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var member_price_xml = await _context.member_price_xml
                        .Where(x => x.id == id)
                        .FirstOrDefaultAsync();

                        if (member_price_xml == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        _context.Remove(member_price_xml);
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
        [Route("admin/delete_price_pdf/{id}")]
        public async Task<IActionResult> DeletePricePdf(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var member_price_pdf = await _context.member_price_pdf
                        .Where(x => x.id == id)
                        .FirstOrDefaultAsync();

                        if (member_price_pdf == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        _context.Remove(member_price_pdf);
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
        [Route("admin/delete_price_email/{id}")]
        public async Task<IActionResult> DeletePriceEmail(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var member_price_email = await _context.member_price_email
                        .Where(x => x.id == id)
                        .FirstOrDefaultAsync();

                        if (member_price_email == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        _context.Remove(member_price_email);
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
        [Route("admin/delete_price_sms/{id}")]
        public async Task<IActionResult> DeletePriceSms(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var member_price_sms = await _context.member_price_sms
                        .Where(x => x.id == id)
                        .FirstOrDefaultAsync();

                        if (member_price_sms == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        _context.Remove(member_price_sms);
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
        [Route("admin/delete_price_ebxml/{id}")]
        public async Task<IActionResult> DeletePriceEbxml(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_member_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var member_price_ebxml = await _context.member_price_ebxml
                        .Where(x => x.id == id)
                        .FirstOrDefaultAsync();

                        if (member_price_ebxml == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        _context.Remove(member_price_ebxml);
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

    }
}
