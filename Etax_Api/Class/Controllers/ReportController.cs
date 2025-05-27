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
				/*Check Token*/
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_report_view).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });
				
				//Check Datetime Range
				if(bodyCostReport.dateStart <= bodyCostReport.dateEnd)
				{
					bodyCostReport.dateStart = DateTime.Parse(bodyCostReport.dateStart.ToString()).Date;
					bodyCostReport.dateEnd = bodyCostReport.dateEnd.AddDays(+1).AddMilliseconds(-1);
				}
				else
				{
					return StatusCode(422, new
					{
						message = "การระบุช่วงของวันเวลาผิดพลาด",
						data = new ReturnCostReport(),
					});
				}
				
				ReturnCostReport returnCostReport = await CountAllTransaction(jwtStatus.member_id,bodyCostReport.dateStart,bodyCostReport.dateEnd);
				if(returnCostReport.total_xml_count > 0) //If no XML is generated yet, no need to calculate
				{
					returnCostReport = await CalculateTransactionCost(returnCostReport,jwtStatus.member_id,bodyCostReport.dateStart,bodyCostReport.dateEnd);
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

		/*Count Functions: To be service*/
		private async Task<ReturnCostReport> CountAllTransaction(int memberID,DateTime fromDate,DateTime toDate)
		{
			//Fill initial data
			ReturnCostReport returnCostReport = new ReturnCostReport();
			returnCostReport.listReturnCostReportData = new List<ReturnCostReportData>();
			
			/****Count Raw Transaction****/
			List<ViewPaymentRawData> listPayment = await _context.view_payment_rawdata
			.Where(x => x.member_id == memberID && 
						x.create_date >= fromDate && 
						x.create_date <= toDate)
			.ToListAsync();
			
			int listCount = listPayment.Count;
			if(listCount > 0)
			{
				foreach (ViewPaymentRawData payment in listPayment)
				{
					// /*Add Count detail into listReturnCostReportData
					returnCostReport.listReturnCostReportData.Add(new ReturnCostReportData()
					{
						row_name = payment.row_name,
						xml_count = payment.xml_count,
						pdf_count = payment.pdf_count,
						email_count = payment.email_count,
						ebxml_count = payment.ebxml_count,
						sms_count = payment.sms_message_count,
					});

					// /*Count total
					returnCostReport.total_xml_count += payment.xml_count;
					returnCostReport.total_pdf_count += payment.pdf_count;
					returnCostReport.total_email_count += payment.email_count;
					returnCostReport.total_ebxml_count += payment.ebxml_count;
					returnCostReport.total_sms_count += payment.sms_message_count;
				}
			}
			
			/****Count API Transaction****/
			int api_xml_count = await CountCreateByCategory(memberID,"xml",fromDate,toDate);
			int api_pdf_count = await CountCreateByCategory(memberID,"pdf",fromDate,toDate);
			int api_email_count = await CountCreateByCategory(memberID,"email",fromDate,toDate);
			int api_sms_count = await CountCreateByCategory(memberID,"sms",fromDate,toDate);
			int api_ebxml_count = await CountCreateByCategory(memberID,"ebxml",fromDate,toDate);
			
			//Add up API counter into total
			returnCostReport.listReturnCostReportData.Add(new ReturnCostReportData()
			{
				row_name = "Api",
				xml_count = api_xml_count,
				pdf_count = api_pdf_count,
				email_count = api_email_count,
				sms_count = api_sms_count,
				ebxml_count = api_ebxml_count
			});

			returnCostReport.total_xml_count += api_xml_count;
			returnCostReport.total_pdf_count += api_pdf_count;
			returnCostReport.total_email_count += api_email_count;
			returnCostReport.total_sms_count += api_sms_count;
			returnCostReport.total_ebxml_count += api_ebxml_count;
			
			return returnCostReport;
		}

		private async Task<List<int>> GetSameGroupMembers(int memberID)
		{
			// Get the group name of memberID
			string groupName = await _context.members.Where(x => x.id == memberID)
				.Select(x => x.group_name)
				.FirstOrDefaultAsync();
				
			// Find all ID with the same group name as memberID
			List<int> groupMembers = await _context.members
				.Where(x => x.group_name == groupName && x.id != memberID)
				.Select(x => x.id)
				.ToListAsync();
			
			return groupMembers;
		}
		
		private async Task<int> CountVolumeTransaction(List<int> groupMembers,string category,int initCount,DateTime fromDate,DateTime toDate)
		{
			int totalCount = initCount;
			int sameGroupCount = groupMembers.Count;
			if(sameGroupCount > 0)
			{
				//Count every transaction of other company in the same group
				for(int i=0;i<sameGroupCount;i++)
				{
					//Check if the other member has Volume type pricing
					bool isVolumeType = await _context.member_price_type
												.Where(x => x.member_id == groupMembers[i] &&
															x.xml_price_type == "volume")
												.AnyAsync();
					
					if(!isVolumeType) //If a company pricing is different or not contribute into the pool, skip
						continue;
					
					totalCount += await CountIndividualByCategory(groupMembers[i],category,fromDate,toDate);
				}
			}
			
			return totalCount;
		}
		
		private async Task<int> CountIndividualByCategory(int memberID,string category,DateTime fromDate,DateTime toDate)
		{
			int totalCounter = 0;
			totalCounter = await CountRawByCategory(memberID,category,fromDate,toDate);
			totalCounter += await CountCreateByCategory(memberID,category,fromDate,toDate);
			
			return totalCounter;
		}
		
		private async Task<int> CountRawByCategory(int memberID,string category,DateTime fromDate,DateTime toDate)
		{
			int totalCounter = 0;
			if(category == "xml")
			{
				totalCounter = await _context.view_payment_rawdata
												.Where(x => x.member_id == memberID && 
															x.create_date >= fromDate && 
															x.create_date <= toDate)
												.SumAsync(x => x.xml_count);
			}
			else if(category == "pdf")
			{
				totalCounter = await _context.view_payment_rawdata
												.Where(x => x.member_id == memberID && 
															x.create_date >= fromDate && 
															x.create_date <= toDate)
												.SumAsync(x => x.pdf_count);
			}
			else if(category == "email")
			{
				totalCounter = await _context.view_payment_rawdata
												.Where(x => x.member_id == memberID && 
															x.create_date >= fromDate && 
															x.create_date <= toDate)
												.SumAsync(x => x.email_count);
			}
			else if(category == "sms")
			{
				totalCounter = await _context.view_payment_rawdata
												.Where(x => x.member_id == memberID && 
															x.create_date >= fromDate && 
															x.create_date <= toDate)
												.SumAsync(x => x.sms_count);
			}
			else if(category == "ebxml")
			{
				totalCounter = await _context.view_payment_rawdata
												.Where(x => x.member_id == memberID && 
															x.create_date >= fromDate && 
															x.create_date <= toDate)
												.SumAsync(x => x.ebxml_count);
			}
			
			return totalCounter;
		}
		
		private async Task<int> CountCreateByCategory(int memberID,string category,DateTime fromDate,DateTime toDate)
		{
			int totalCounter = 0;
			if(category == "xml")
			{
				totalCounter = await _context.etax_files.
									Where(x => x.member_id == memberID && 
									x.create_type == "api" &&
									x.gen_xml_status == "success" &&
									x.create_date >= fromDate &&
									x.create_date <= toDate)
									.CountAsync();
			}
			else if(category == "pdf")
			{
				totalCounter = await _context.etax_files.
									Where(x => x.member_id == memberID && 
									x.create_type == "api" &&
									x.gen_pdf_status == "success" &&
									x.create_date >= fromDate &&
									x.create_date <= toDate)
									.CountAsync();
			}
			else if(category == "email")
			{
				totalCounter = await _context.view_send_email.
									Where(x => x.member_id == memberID && 
									x.create_type == "api" &&
									x.send_email_status == "success" &&
									x.create_date >= fromDate &&
									x.create_date <= toDate)
									.SumAsync(x => x.send_count);
			}
			else if(category == "sms")
			{
				totalCounter = await _context.view_send_sms.
									Where(x => x.member_id == memberID && 
									x.create_type == "api" &&
									x.send_sms_status == "success" &&
									x.create_date >= fromDate &&
									x.create_date <= toDate)
									.SumAsync(x => x.message_count);
			}
			else if(category == "ebxml")
			{
				totalCounter = await _context.view_send_ebxml.
									Where(x => x.member_id == memberID && 
									x.create_type == "api" &&
									x.send_ebxml_status == "success" &&
									x.create_date >= fromDate &&
									x.create_date <= toDate)
									.CountAsync();
			}
			
			return totalCounter;
		}
		
		/*Calculation Functions: To be service*/
		private async Task<ReturnCostReport> CalculateTransactionCost(ReturnCostReport returnCostReport,int thisMemberID,DateTime fromDate,DateTime toDate)
		{
			//Fetch cost calculation type
			var servicePriceType = await _context.member_price_type.Where(x => x.member_id == thisMemberID)
																	   .FirstOrDefaultAsync();
				
			if(servicePriceType != null) //This member has cost to calculate
			{
				// Console.WriteLine("This member XML price type is: "+servicePriceType.xml_price_type);
				// Console.WriteLine("This member PDF price type is: "+servicePriceType.pdf_price_type);
				// Console.WriteLine("This member Email price type is: "+servicePriceType.email_price_type);
				// Console.WriteLine("This member SMS price type is: "+servicePriceType.sms_price_type);
				// Console.WriteLine("This member EBXML price type is: "+servicePriceType.ebxml_price_type);
				
				int[] tierNum;
				double[] tierPrice;
				
				//Check if need to fetch group members
				List<int> groupMembers = null;
				if(servicePriceType.xml_price_type == "volume" ||
				   // servicePriceType.pdf_price_type == "volume" ||
				   servicePriceType.email_price_type == "volume" ||
				   servicePriceType.sms_price_type == "volume" //||
				   // servicePriceType.ebxml_price_type == "volume"
				   )
				{
					groupMembers = await GetSameGroupMembers(thisMemberID);
				}
				
				//XML Price
				List<MemberPriceXml> listMemberPriceXml = await _context.member_price_xml.Where(x => x.member_id == thisMemberID).OrderByPropertyDescending("count").ToListAsync();
				tierNum = listMemberPriceXml.Select(p => p.count).ToArray();
				tierPrice = listMemberPriceXml.Select(p => p.price).ToArray();
				if(servicePriceType.xml_price_type != "volume") //If not volume price type (Such as Mk group), proceed normally
				{
					returnCostReport.total_xml_price = CalculateTotalPrice(servicePriceType.xml_price_type,tierNum,tierPrice,returnCostReport.total_xml_count);
				}
				else //Count other company transaction in the group to sum the volume
				{
					int totalCount = await CountVolumeTransaction(groupMembers,
															"xml",
															returnCostReport.total_xml_count,
															fromDate,
															toDate);
					returnCostReport.total_xml_price = CalculateVolumeType(tierNum,tierPrice,returnCostReport.total_xml_count,totalCount);
				}
				
				//*****PDF Price: Always Free/Calculated with XML generation
				// List<MemberPricePdf> listMemberPricePdf = await _context.member_price_pdf.Where(x => x.member_id == thisMemberID).OrderByPropertyDescending("count").ToListAsync();
				
				//Email Price
				List<MemberPriceEmail> listMemberPriceEmail = await _context.member_price_email.Where(x => x.member_id == thisMemberID).OrderByPropertyDescending("count").ToListAsync();
				tierNum = listMemberPriceEmail.Select(p => p.count).ToArray();
				tierPrice = listMemberPriceEmail.Select(p => p.price).ToArray();
				if(servicePriceType.email_price_type == "tran")
				{
					var email_tran = await _context.view_send_email_list
                    .Where(x => x.member_id == thisMemberID && 
								x.send_email_status == "success" && 
								x.create_date >= fromDate && 
								x.create_date <= toDate)
                    .GroupBy(x => x.etax_file_id)
                    .Select(x => new
                    {
                        etax_id = x.Key,
                        count = x.Count()
                    })
                    .ToListAsync();
					double sumPrice = 0;
					foreach (var et in email_tran)
                    {
                        sumPrice += CalculateTotalPrice("tier",tierNum,tierPrice,et.count);
                    }
					returnCostReport.total_email_price = sumPrice;
				}
				else if(servicePriceType.email_price_type == "volume")
				{
					int totalCount = await CountVolumeTransaction(groupMembers,
                                                            "email",
                                                            returnCostReport.total_email_count,
                                                            fromDate,
                                                            toDate);
                    returnCostReport.total_email_price = CalculateVolumeType(tierNum, tierPrice, returnCostReport.total_email_count, totalCount);
				}
                else
                {
					returnCostReport.total_email_price = CalculateTotalPrice(servicePriceType.email_price_type,tierNum,tierPrice,returnCostReport.total_email_count);
                }
				
				//*****Ebxml Price: Already calculate along with XML generation
				// List<MemberPriceEbxml> listMemberPriceEbxml = await _context.member_price_ebxml.Where(x => x.member_id == thisMemberID).OrderByPropertyDescending("count").ToListAsync();
				
				//SMS Price
				List<MemberPriceSms> listMemberPriceSms = await _context.member_price_sms.Where(x => x.member_id == thisMemberID).OrderByPropertyDescending("count").ToListAsync();
				tierNum = listMemberPriceSms.Select(p => p.count).ToArray();
				tierPrice = listMemberPriceSms.Select(p => p.price).ToArray();
				if(servicePriceType.email_price_type != "volume")
				{
					returnCostReport.total_sms_price = CalculateTotalPrice(servicePriceType.sms_price_type,tierNum,tierPrice,returnCostReport.total_sms_count);
				}
				else
				{
					int totalCount = await CountVolumeTransaction(groupMembers,
															"sms",
															returnCostReport.total_email_count,
															fromDate,
															toDate);
					returnCostReport.total_sms_price = CalculateVolumeType(tierNum,tierPrice,returnCostReport.total_sms_count,totalCount);
				}
			}
			else
			{
				return returnCostReport;
			}
			
			return returnCostReport;
		}
		
		//Calculation functions
		private double CalculateTotalPrice(string calType,int[] tierNum,double[] tierPrice,int selfNum,int totalNum = -1)
		{
			if(calType == "free")
				return 0;
			
			double totalPrice = 0;
			if(calType == "tier")
			{
				totalPrice = CalculateTierType(tierNum,tierPrice,selfNum);
			}
			else if(calType == "flat")
			{
                Console.WriteLine(tierPrice.Length);
				totalPrice = tierPrice[0] * selfNum;
			}
			else if(calType == "volume")
			{
				totalPrice = CalculateVolumeType(tierNum,tierPrice,selfNum);
			}
			else
			{
				totalPrice = CalculateTierType(tierNum,tierPrice,selfNum);
			}
			
			return totalPrice;
		}
		
		private double CalculateTierType(int[] tierNum,double[] tierPrice,int num)
		{
			double totalPrice = 0;
			int tempCount = num;
			int tiers = tierNum.Length;
			
			for(int i=0;i<tiers;i++)
			{
				if (tempCount > tierNum[i])
				{
					totalPrice += (tempCount - tierNum[i]) * tierPrice[i];
					tempCount = tierNum[i];
				}
			}
			
			return totalPrice;
		}
		
		private double CalculateVolumeType(int[] tierNum,double[] tierPrice,int selfNum,int totalNum = -1)
		{
			int tiers = tierNum.Length;
			if(totalNum == -1)
				totalNum = selfNum;
			
			for(int i=0;i<tiers;i++)
			{
				if(totalNum >= tierNum[i])
				{
					return selfNum * tierPrice[i];
				}
			}
			return 0;
		}
		
		/*>>>End of extracted function*/
		
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

                double sumTotalNoVat = 0;
                if (member.group_name == "Isuzu")
                {
                    foreach (ViewTaxReport data in result)
                    {
                        string[] other2Array = data.other2.Split('|');
                        double totalNoVat = double.Parse(other2Array[1]);

                        if (data.document_type_id == 3)
                        {
                            totalNoVat = -totalNoVat;
                        }


                        sumTotalNoVat += totalNoVat;

                    }
                }
                else
                {
                    sumTotalNoVat = sumPrice - sumDiscount; 
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
                        sumTotalNoVat = sumTotalNoVat.ToString("0.00"),
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
                        sumTotalNoVat = sumTotalNoVat.ToString("0.00"),
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


                var permission_branch = await _context.member_user_branch
                                        .Where(x => x.member_user_id == jwtStatus.user_id && x.member_id == jwtStatus.member_id)
                                        .Select(x => new
                                        {
                                            x.branch_id,
                                        }).ToListAsync();

                

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

                var result =  _context.view_send_email_list.Where(x => x.member_id == jwtStatus.member_id).AsQueryable();

                if(permission_branch.Count > 0)
                {
                    var permission_branch_ids = permission_branch.Select(pb => pb.branch_id).ToList();

                     result = result.Where(x => permission_branch_ids.Contains(x.branch_id));

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

				/*Has no usage*/
				// List<ViewPayment> listPaymentOther = await _context.view_payment
               // .Where(x => x.member_id == jwtStatus.member_id)
               // .ToListAsync();
			   
                if(bodyCostReport.dateStart <= bodyCostReport.dateEnd)
				{
					bodyCostReport.dateStart = DateTime.Parse(bodyCostReport.dateStart.ToString()).Date;
					bodyCostReport.dateEnd = bodyCostReport.dateEnd.AddDays(+1).AddMilliseconds(-1);
				}
				else
				{
					return StatusCode(422, new
					{
						message = "การระบุช่วงของวันเวลาผิดพลาด",
						data = new ReturnCostReport(),
					});
				}
				
				ReturnCostReport returnCostReport = await CountAllTransaction(jwtStatus.member_id,bodyCostReport.dateStart,bodyCostReport.dateEnd);
				if(returnCostReport.total_xml_count > 0) //If no XML is generated yet, no need to calculate
				{
					returnCostReport = await CalculateTransactionCost(returnCostReport,jwtStatus.member_id,bodyCostReport.dateStart,bodyCostReport.dateEnd);
				}

				Member member = await _context.members
                .Where(x => x.id == jwtStatus.member_id)
                .FirstOrDefaultAsync();
				
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
                else if (member.group_name == "Isuzu")
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

                Report.DefaultCancelReport(output + pathExcel, bodyDtParameters, listData, sumOriginalPrice, sumPrice, sumDiscount, sumTax, sumTotal);


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

                //Check Datetime Range
				if(bodyCostReportAdmin.dateStart <= bodyCostReportAdmin.dateEnd)
				{
					bodyCostReportAdmin.dateStart = DateTime.Parse(bodyCostReportAdmin.dateStart.ToString()).Date;
					bodyCostReportAdmin.dateEnd = bodyCostReportAdmin.dateEnd.AddDays(+1).AddMilliseconds(-1);
				}
				else
				{
					return StatusCode(422, new
					{
						message = "การระบุช่วงของวันเวลาผิดพลาด",
						data = new ReturnCostReport(),
					});
				}
				
				ReturnCostReport returnCostReport = await CountAllTransaction(bodyCostReportAdmin.member_id,bodyCostReportAdmin.dateStart,bodyCostReportAdmin.dateEnd);
				if(returnCostReport.total_xml_count > 0) //If no XML is generated yet, no need to calculate
				{
					returnCostReport = await CalculateTransactionCost(returnCostReport,bodyCostReportAdmin.member_id,bodyCostReportAdmin.dateStart,bodyCostReportAdmin.dateEnd);
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

                //Check Datetime Range
				if(bodyCostReportAdmin.dateStart <= bodyCostReportAdmin.dateEnd)
				{
					bodyCostReportAdmin.dateStart = DateTime.Parse(bodyCostReportAdmin.dateStart.ToString()).Date;
					bodyCostReportAdmin.dateEnd = bodyCostReportAdmin.dateEnd.AddDays(+1).AddMilliseconds(-1);
				}
				else
				{
					return StatusCode(422, new
					{
						message = "การระบุช่วงของวันเวลาผิดพลาด",
						data = new ReturnCostReport(),
					});
				}
				
				ReturnCostReport returnCostReport = await CountAllTransaction(bodyCostReportAdmin.member_id,bodyCostReportAdmin.dateStart,bodyCostReportAdmin.dateEnd);
				if(returnCostReport.total_xml_count > 0) //If no XML is generated yet, no need to calculate
				{
					returnCostReport = await CalculateTransactionCost(returnCostReport,bodyCostReportAdmin.member_id,bodyCostReportAdmin.dateStart,bodyCostReportAdmin.dateEnd);
				}

				Member member = await _context.members
                .Where(x => x.id == bodyCostReportAdmin.member_id)
                .FirstOrDefaultAsync();
				
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
        public async Task<IActionResult> GetTaxSummaryReportAdmin([FromBody] BodyAdminDtParameters bodyDtParameters)
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

                if (bodyDtParameters.fileGroup.Count > 0)
                {
                    List<string> listfileGroup = new List<string>();
                    foreach (FileGroup fg in bodyDtParameters.fileGroup)
                        listfileGroup.Add(fg.text);

                    result = result.Where(x => listfileGroup.Contains(x.group_name));
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
        public async Task<IActionResult> GetEmailSummaryReportAdmin([FromBody] BodyAdminDtParameters bodyDtParameters)
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

                var result = _context.view_send_email_list.Where(x => listDocumentTypeID.Contains(x.document_type_id)).AsQueryable();

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

                if (bodyDtParameters.fileGroup.Count > 0)
                {
                    List<string> listfileGroup = new List<string>();
                    foreach (FileGroup fg in bodyDtParameters.fileGroup)
                        listfileGroup.Add(fg.text);

                    result = result.Where(x => listfileGroup.Contains(x.group_name));
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
        public async Task<IActionResult> GetEbxmlSummaryReportAdmin([FromBody] BodyAdminDtParameters bodyDtParameters)
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

                if (bodyDtParameters.fileGroup.Count > 0)
                {
                    List<string> listfileGroup = new List<string>();
                    foreach (FileGroup fg in bodyDtParameters.fileGroup)
                        listfileGroup.Add(fg.text);

                    result = result.Where(x => listfileGroup.Contains(x.group_name));
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
