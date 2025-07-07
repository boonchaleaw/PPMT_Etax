
using Etax_Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class ApiSftpController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private Regex rxField = new Regex(@"[A-Z]{1}[0-9]{2}-");
        private readonly IExceptionLogger _exceptionLogger;
        public ApiSftpController(ApplicationDbContext context, IConfiguration config, IExceptionLogger exceptionLogger)
        {
            _config = config;
            _context = context;
            _context.Database.SetCommandTimeout(180);
            _exceptionLogger = exceptionLogger;
        }

        [HttpPost]
        [Route("ppmt_create_etax_file_sftp_benz")]
        public async Task<IActionResult> ApiCreateEtaxFileSftpBenz([FromForm] BodyApiCreateEtaxFileMwa bodyApiCreateEtaxFile)
        {
            try
            {

                if (bodyApiCreateEtaxFile.APIKey != "l60leBwJDveYgG5Ar67HXLgUhJL6ANS9G756Cccz0c0fvIAWmNRSgQyb4o4NjhBl640ivaONHQGHr8Ad5p4eiyE0Xob6gp5LHVvm2OVdUpIlYHV5hKqpJSeen6Bg4ARA")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E001",
                        errorMessage = "APIKey ไม่ถูกต้อง",
                    });

                if (bodyApiCreateEtaxFile.UserCode != "benz" || bodyApiCreateEtaxFile.AccessKey != "P@ssw0rd")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E002",
                        errorMessage = "ชื่อผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง",
                    });

                if (bodyApiCreateEtaxFile.SellerTaxId != "0105539109901")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E003",
                        errorMessage = "ไม่พบ Tax ID ในระบบ",
                    });

                if (bodyApiCreateEtaxFile.SellerBranchId != "00000")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E004",
                        errorMessage = "ไม่พบสาขาที่ต้องการ",
                    });

                if (bodyApiCreateEtaxFile.TextContent == null)
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E005",
                        errorMessage = "ไม่พบไฟล์ข้อมูล",
                    });


                DateTime now = DateTime.Now;
                int member_id = 1013;
                int user_id = 1039;

                string raw_file_name = bodyApiCreateEtaxFile.TextContent.FileName;
                string input = _config["Path:Input"] + "/" + member_id + "/" + now.ToString("yyyyMM") + "/" + raw_file_name;
                string url = "/" + member_id + "/" + now.ToString("yyyyMM") + "/" + raw_file_name;
                string output = _config["Path:Output"];

                var checkRawdataFiles = _context.rawdata_files
                .Where(x => x.file_name == raw_file_name)
                .FirstOrDefault();

                if (checkRawdataFiles != null)
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E006",
                        errorMessage = "ชื่อไฟล์ซ้ำในระบบ",
                    });


                var branch = _context.branchs
                .Where(x => x.member_id == member_id && x.branch_code == bodyApiCreateEtaxFile.SellerBranchId)
                .Select(x => new
                {
                    x.id,
                    x.branch_code,
                })
                .FirstOrDefault();

                try
                {
                    int document_type_id = 1;

                    if (bodyApiCreateEtaxFile.PdfTemplateId == "Bill-Payment")
                        document_type_id = 101;
                    else if (bodyApiCreateEtaxFile.PdfTemplateId == "Temp-Receipt")
                        document_type_id = 102;
                    else
                    {
                        return StatusCode(400, new
                        {
                            status = "ER",
                            errorCode = "E008",
                            errorMessage = "ไม่พบรูปแบบเอกสารที่ต้องการ",
                        });
                    }

                    string load_file_status = "no";
                    string gen_pdf_status = "no";
                    string send_email_status = "no";
                    string send_sms_status = "no";
                    string send_ebxml_status = "no";

                    if (bodyApiCreateEtaxFile.ServiceCode == "S01")
                    {
                        load_file_status = "pending";
                        gen_pdf_status = "no";
                        send_email_status = "no";
                        send_sms_status = "no";
                        send_ebxml_status = "no";
                    }
                    else if (bodyApiCreateEtaxFile.ServiceCode == "S02")
                    {
                        load_file_status = "pending";
                        gen_pdf_status = "pending";
                        send_email_status = "no";
                        send_sms_status = "no";
                        send_ebxml_status = "no";
                    }
                    else if (bodyApiCreateEtaxFile.ServiceCode == "S03")
                    {
                        load_file_status = "pending";
                        gen_pdf_status = "pending";
                        send_email_status = "pending";
                        send_sms_status = "no";
                        send_ebxml_status = "no";
                    }
                    else if (bodyApiCreateEtaxFile.ServiceCode == "S04")
                    {
                        load_file_status = "pending";
                        gen_pdf_status = "pending";
                        send_email_status = "no";
                        send_sms_status = "pending";
                        send_ebxml_status = "no";
                    }
                    else if (bodyApiCreateEtaxFile.ServiceCode == "S05")
                    {
                        load_file_status = "pending";
                        gen_pdf_status = "pending";
                        send_email_status = "pending";
                        send_sms_status = "pending";
                        send_ebxml_status = "no";
                    }
                    else
                    {
                        load_file_status = "pending";
                        gen_pdf_status = "pending";
                        send_email_status = "no";
                        send_sms_status = "no";
                        send_ebxml_status = "no";
                    }

                    string share_path = Encryption.SHA512("path_" + now.ToString("dd-MM-yyyy"));

                    byte[] bytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        bodyApiCreateEtaxFile.TextContent.OpenReadStream().CopyTo(memoryStream);
                        bytes = memoryStream.ToArray();
                    }
                    string base64 = Convert.ToBase64String(bytes);

                    if (ApiFileTransfer.UploadFile(_config["Path:FileTransfer"], url, base64, _config["Path:Mode"]))
                    {
                        RawDataFile rawDataFile = new RawDataFile()
                        {
                            member_id = member_id,
                            branch_id = branch.id,
                            member_user_id = user_id,
                            document_type_id = document_type_id,
                            file_name = raw_file_name,
                            input_path = input,
                            output_path = output,
                            load_file_status = load_file_status,
                            gen_pdf_status = gen_pdf_status,
                            send_email_status = send_email_status,
                            send_sms_status = send_sms_status,
                            send_ebxml_status = send_ebxml_status,
                            comment = "",
                            template_pdf = bodyApiCreateEtaxFile.PdfTemplateId,
                            template_email = bodyApiCreateEtaxFile.PdfTemplateId,
                            mode = "normal",
                            create_date = DateTime.Now,
                        };
                        _context.Add(rawDataFile);
                        _context.SaveChanges();

                        return StatusCode(200, new
                        {
                            status = "OK",
                        });
                    }
                    else return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "",
                        errorMessage = "อัพโหลดไฟล์ข้อมูลไม่สำเร็จ",
                    });
                }
                catch (Exception ex)
                {
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E099",
                        errorMessage = "เกิดความผิดพลาดบ้างอย่างในระบบ กรุณาติดต่อเจ้าหน้าที่",
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new
                {
                    status = "ER",
                    errorCode = "E099",
                    errorMessage = "เกิดความผิดพลาดบ้างอย่างในระบบ กรุณาติดต่อเจ้าหน้าที่",
                });
            }
        }

        [HttpPost]
        [Route("ppmt_create_etax_file_sftp_mk")]
        public async Task<IActionResult> ApiCreateEtaxFileSftpMk([FromForm] BodyApiCreateEtaxFileMwa bodyApiCreateEtaxFile)
        {
            try
            {
				//Check API Key
                if (bodyApiCreateEtaxFile.APIKey != "l60leBwJDveYgG5Ar67HXLgUhJL6ANS9G756Cccz0c0fvIAWmNRSgQyb4o4NjhBl640ivaONHQGHr8Ad5p4eiyE0Xob6gp5LHVvm2OVdUpIlYHV5hKqpJSeen6Bg4ARA")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E001",
                        errorMessage = "APIKey ไม่ถูกต้อง",
                    });
				
				//Check fixed username - password
                if (bodyApiCreateEtaxFile.UserCode != "mk" || bodyApiCreateEtaxFile.AccessKey != "P@ssw0rd")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E002",
                        errorMessage = "ชื่อผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง",
                    });

				//Check fixed Tax ID
                if (bodyApiCreateEtaxFile.SellerTaxId != "0115561013130")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E003",
                        errorMessage = "ไม่พบ Tax ID ในระบบ",
                    });

				//Check Branch ID
                if (bodyApiCreateEtaxFile.SellerBranchId != "00000")
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E004",
                        errorMessage = "ไม่พบสาขาที่ต้องการ",
                    });

				//Check contents
                if (bodyApiCreateEtaxFile.TextContent == null)
                    return StatusCode(400, new
                    {
                        status = "ER",
                        errorCode = "E005",
                        errorMessage = "ไม่พบไฟล์ข้อมูล",
                    });

                TextContentDataJson data;

				//Extract JSON content into MkDataJson
                try
                {
                    string text = "";
                    using (var memoryStream = new MemoryStream())
                    {
                        bodyApiCreateEtaxFile.TextContent.OpenReadStream().CopyTo(memoryStream);
                        text = Encoding.UTF8.GetString(memoryStream.ToArray());
                    }

                    MatchCollection matchField = rxField.Matches(text);
                    foreach (Match match in matchField)
                    {
                        text = text.Replace(match.Value, match.Value.Replace("-", "_"));
                    }


                    MkDataJson DataJson = Newtonsoft.Json.JsonConvert.DeserializeObject<MkDataJson>(text);
                    data = DataJson.TextContent;
                }
                catch (Exception ex)
                {
                    return StatusCode(400, new { message = "รูปแบบไฟล์ไม่ถูกต้อง" });
                }

				//If the data is from ERP
                if (bodyApiCreateEtaxFile.PdfTemplateId == "Erp")
                {
					//Check tax_id if matched in the DB
                    var member = await (from m in _context.members
                                        where m.tax_id == bodyApiCreateEtaxFile.SellerTaxId
                                        select m).FirstOrDefaultAsync();
                    if (member == null)
                        return StatusCode(400, new { message = "ไม่พบผู้ขายที่ต้องการ" });

					//Check if branch code is valid
                    var branch = await (from b in _context.branchs
                                        where b.member_id == member.id && b.branch_code == data.C02_SELLER_BRANCH_ID
                                        select b).FirstOrDefaultAsync();
                    if (branch == null)
                        return StatusCode(400, new { message = "ไม่พบผู้สาขาที่ต้องการ" });

					//Check if found any etax_id with that same H03_DOCUMENT_ID, repeated data if founded
                    var etax_file = await (from ef in _context.etax_files
                                           where ef.etax_id == data.H03_DOCUMENT_ID.Trim() && ef.delete_status == 0
                                           select ef).FirstOrDefaultAsync();
                    if (etax_file != null)
                        return StatusCode(400, new { message = "มีข้อมูลซ้ำในระบบ" });


                    DateTime now = DateTime.Now;
					
					//Define document type
                    int document_type_id = 0;
                    if (data.H01_DOCUMENT_TYPE_CODE == "T03")
                        document_type_id = 7;
                    else if (data.H01_DOCUMENT_TYPE_CODE == "T04")
                        document_type_id = 8;
                    else if (data.H01_DOCUMENT_TYPE_CODE == "80")
                        document_type_id = 2;
                    else if (data.H01_DOCUMENT_TYPE_CODE == "81")
                        document_type_id = 3;
                    else
                        return StatusCode(400, new { message = "ไม่รองรับประเภทไฟล์นี้" });
					
					//Check if DOCUMENT_ID is empty
                    if (data.H03_DOCUMENT_ID.Trim() == "")
                        return StatusCode(400, new { message = "ไม่พบหมายเลขเอกสาร" });

                    using (var transaction = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            EtaxFile etaxFile = new EtaxFile();
                            etaxFile.member_id = member.id;
                            etaxFile.branch_id = branch.id;
                            etaxFile.member_user_id = 0;
                            etaxFile.document_type_id = document_type_id;
                            etaxFile.create_type = "api";
                            etaxFile.gen_xml_status = "pending";
                            etaxFile.gen_pdf_status = "pending";
							etaxFile.add_email_status = "pending";
                            //etaxFile.add_email_status = "no"; //Old condition, not sent any E-mail
                            etaxFile.add_sms_status = "no";
                            etaxFile.add_ebxml_status = "no";
                            etaxFile.send_xml_status = "N";
                            etaxFile.send_other_status = "N";
                            etaxFile.error = "";
                            etaxFile.output_path = _config["Path:Output"];
                            etaxFile.etax_id = data.H03_DOCUMENT_ID.Trim();
                            etaxFile.issue_date = DateTime.ParseExact(data.H04_DOCUMENT_ISSUE_DTM.Trim(), "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                            etaxFile.buyer_branch_code = data.B05_BUYER_BRANCH_ID.Trim();
                            etaxFile.buyer_id = data.B01_BUYER_ID.Trim();
                            etaxFile.buyer_name = data.B02_BUYER_NAME.Trim();
                            etaxFile.buyer_tax_type = data.B03_BUYER_TAX_ID_TYPE.Trim();
                            etaxFile.buyer_tax_id = data.B04_BUYER_TAX_ID.Trim();
                            etaxFile.buyer_address = data.B13_BUYER_ADDRESS_LINE1.Trim() + " " + data.B14_BUYER_ADDRESS_LINE2.Trim();
                            etaxFile.buyer_zipcode = data.B10_BUYER_POST_CODE.Trim();
                            etaxFile.buyer_tel = "";
                            etaxFile.buyer_fax = "";
                            etaxFile.buyer_country_code = data.B25_BUYER_COUNTRY_ID.Trim();
                            etaxFile.buyer_email = data.B08_BUYER_URIID.Trim();
							//If email not specify, use default
							if(etaxFile.buyer_email == "")
								etaxFile.buyer_email = "issara.ru@markone.co.th";
                            etaxFile.price = Convert.ToDouble(data.F46_TAX_BASIS_TOTAL_AMOUNT.Trim());
                            etaxFile.discount = Convert.ToDouble(data.F29_ALLOWANCE_ACTUAL_AMOUNT.Trim());
                            etaxFile.tax_rate = (int)Convert.ToDouble(data.F05_TAX_CAL_RATE1.Trim());
                            etaxFile.tax = Convert.ToDouble(data.F48_TAX_TOTAL_AMOUNT.Trim());
                            etaxFile.total = Convert.ToDouble(data.F50_GRAND_TOTAL_AMOUNT.Trim());
                            etaxFile.remark = "";
                            etaxFile.other = "";

                            //Request 02/07/2025, remove PDF password
                            //if (document_type_id != 7)
                            //{
                            //    etaxFile.password = etaxFile.buyer_tax_id.Substring(etaxFile.buyer_tax_id.Length - 5);
                            //}

                            if (document_type_id == 8)
                            {
                                etaxFile.other2 = data.H11_DELIVERY_TYPE_CODE.Trim() + "|" + data.H12_BUYER_ORDER_ASSIGN_ID.Trim();
                            }
                            else if(document_type_id == 3)
                            {
                                etaxFile.other2 = data.H28_RETURN_ORDER_NUMBER.Trim();
                            }
                            else
                            {
                                etaxFile.other2 = "";
                            }

                            etaxFile.group_name = "";
                            etaxFile.template_pdf = bodyApiCreateEtaxFile.PdfTemplateId;
                            etaxFile.template_email = "";
                            etaxFile.mode = "normal";
                            etaxFile.xml_payment_status = "pending";
                            etaxFile.pdf_payment_status = "pending";
                            etaxFile.create_date = now;

                            if (document_type_id == 2 || document_type_id == 3)
                            {
                                etaxFile.ref_etax_id = data.H07_ADDITIONAL_REF_ASSIGN_ID.Trim();
                                etaxFile.ref_issue_date = DateTime.ParseExact(data.H08_ADDITIONAL_REF_ISSUE_DTM.Trim(), "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                                etaxFile.ref_document_type = data.H09_ADDITIONAL_REF_TYPE_CODE.Trim();
                                etaxFile.remark = data.H05_CREATE_PURPOSE_CODE.Trim() + " - " + data.H06_CREATE_PURPOSE.Trim();
                                etaxFile.original_price = Convert.ToDouble(data.F36_ORIGINAL_TOTAL_AMOUNT.Trim());
                                etaxFile.new_price = Convert.ToDouble(data.F38_LINE_TOTAL_AMOUNT.Trim());
                            }

                            _context.Add(etaxFile);
                            await _context.SaveChangesAsync();

                            if (document_type_id == 2 || document_type_id == 3)
                            {
                                foreach (LineItemDataJson item in data.LINE_ITEM_INFORMATION)
                                {
                                    _context.Add(new EtaxFileItem()
                                    {
                                        etax_file_id = etaxFile.id,
                                        code = item.L02_PRODUCT_ID,
                                        name = item.L03_PRODUCT_NAME,
                                        qty = Convert.ToDouble(item.L17_PRODUCT_QUANTITY.Trim()),
                                        unit = item.L18_PRODUCT_UNIT_CODE.Trim(),
                                        price = Convert.ToDouble(item.L37_PRODUCT_REMARK1.Trim()),
                                        discount = 0,
                                        tax = 0,
                                        total = Convert.ToDouble(item.L27_LINE_ALLOWANCE_ACTUAL_AMOUNT.Trim()),
                                        tax_type = item.L20_LINE_TAX_TYPE_CODE.Trim(),
                                        tax_rate = (int)Convert.ToDouble(item.L21_LINE_TAX_CAL_RATE.Trim()),
                                        other = item.L10_PRODUCT_CHARGE_AMOUNT.Trim() + "|" + item.L38_PRODUCT_REMARK2.Trim() + "|" + item.L13_PRODUCT_ALLOWANCE_ACTUAL_AMOUNT + "|" + item.L33_LINE_NET_TOTAL_AMOUNT.Trim(),
                                    });
                                }
                            }
                            else
                            {
                                foreach (LineItemDataJson item in data.LINE_ITEM_INFORMATION)
                                {
                                    _context.Add(new EtaxFileItem()
                                    {
                                        etax_file_id = etaxFile.id,
                                        code = item.L02_PRODUCT_ID,
                                        name = item.L03_PRODUCT_NAME,
                                        qty = Convert.ToDouble(item.L17_PRODUCT_QUANTITY.Trim()),
                                        unit = item.L18_PRODUCT_UNIT_CODE.Trim(),
                                        price = Convert.ToDouble(item.L10_PRODUCT_CHARGE_AMOUNT.Trim()),
                                        discount = 0,
                                        tax = 0,
                                        total = Convert.ToDouble(item.L33_LINE_NET_TOTAL_AMOUNT.Trim()),
                                        tax_type = item.L20_LINE_TAX_TYPE_CODE.Trim(),
                                        tax_rate = (int)Convert.ToDouble(item.L21_LINE_TAX_CAL_RATE.Trim()),
                                        other = "",
                                    });
                                }
                            }
                            await _context.SaveChangesAsync();
                            transaction.Commit();

                            return StatusCode(200, new
                            {
                                data = new
                                {
                                    etax_id = etaxFile.etax_id,
                                },
                                message = "อัพโหลดไฟล์ข้อมูลสำเร็จ",
                            });

                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return StatusCode(400, new
                            {
                                status = "ER",
                                errorCode = "E099",
                                errorMessage = "เกิดความผิดพลาดบ้างอย่างในระบบ กรุณาติดต่อเจ้าหน้าที่",
                            });
                        }
                    }
                }
                else if (bodyApiCreateEtaxFile.PdfTemplateId == "Pos")
                {
                    var member = await (from m in _context.members
                                        where m.tax_id == bodyApiCreateEtaxFile.SellerTaxId
                                        select m).FirstOrDefaultAsync();
                    if (member == null)
                        return StatusCode(400, new { message = "ไม่พบผู้ขายที่ต้องการ" });


                    var branch = await (from b in _context.branchs
                                        where b.member_id == member.id && b.branch_code == data.C02_SELLER_BRANCH_ID
                                        select b).FirstOrDefaultAsync();
                    if (branch == null)
                        return StatusCode(400, new { message = "ไม่พบผู้สาขาที่ต้องการ" });

                    var etax_file = await (from ef in _context.etax_files
                                           where ef.etax_id == data.H03_DOCUMENT_ID.Trim() && ef.delete_status == 0
                                           select ef).FirstOrDefaultAsync();
                    if (etax_file != null)
                        return StatusCode(400, new { message = "มีข้อมูลซ้ำในระบบ" });


                    DateTime now = DateTime.Now;
                    int document_type_id = 7;

                    if (data.H03_DOCUMENT_ID.Trim() == "")
                        return StatusCode(400, new { message = "ไม่พบหมายเลขเอกสาร" });

                    using (var transaction = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            EtaxFile etaxFile = new EtaxFile();
                            etaxFile.member_id = member.id;
                            etaxFile.branch_id = branch.id;
                            etaxFile.member_user_id = 0;
                            etaxFile.document_type_id = document_type_id;
                            etaxFile.create_type = "api";
                            etaxFile.gen_xml_status = "pending";
                            etaxFile.gen_pdf_status = "pending";
                            etaxFile.add_email_status = "no";
                            etaxFile.add_sms_status = "no";
                            etaxFile.add_ebxml_status = "no";
                            etaxFile.send_xml_status = "N";
                            etaxFile.send_other_status = "N";
                            etaxFile.error = "";
                            etaxFile.output_path = _config["Path:Output"];
                            etaxFile.etax_id = data.H03_DOCUMENT_ID.Trim();
                            etaxFile.issue_date = DateTime.ParseExact(data.H04_DOCUMENT_ISSUE_DTM.Trim(), "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                            etaxFile.ref_etax_id = data.H03_DOCUMENT_ID.Trim();
                            etaxFile.ref_issue_date = DateTime.ParseExact(data.H04_DOCUMENT_ISSUE_DTM.Trim(), "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                            etaxFile.buyer_branch_code = data.B05_BUYER_BRANCH_ID.Trim();
                            etaxFile.buyer_id = data.B01_BUYER_ID.Trim();
                            etaxFile.buyer_name = "";
                            etaxFile.buyer_tax_type = "";
                            etaxFile.buyer_tax_id = "";
                            etaxFile.buyer_address = "";
                            etaxFile.buyer_zipcode = "";
                            etaxFile.buyer_tel = "";
                            etaxFile.buyer_fax = "";
                            etaxFile.buyer_country_code = data.B25_BUYER_COUNTRY_ID.Trim();
                            etaxFile.buyer_email = "";
                            etaxFile.price = Convert.ToDouble(data.F06_BASIS_AMOUNT1.Trim()) + Convert.ToDouble(data.F12_BASIS_AMOUNT2.Trim()) + Math.Abs(Convert.ToDouble(data.F29_ALLOWANCE_ACTUAL_AMOUNT.Trim()));
                            etaxFile.discount = Math.Abs(Convert.ToDouble(data.F29_ALLOWANCE_ACTUAL_AMOUNT.Trim()));
                            etaxFile.tax_rate = (int)Convert.ToDouble(data.F05_TAX_CAL_RATE1.Trim());
                            etaxFile.tax = Convert.ToDouble(data.F08_TAX_CAL_AMOUNT1.Trim());
                            etaxFile.total = Convert.ToDouble(data.F50_GRAND_TOTAL_AMOUNT.Trim());
                            etaxFile.remark = "";
                            etaxFile.other = "";
                            etaxFile.other2 = data.F06_BASIS_AMOUNT1.Trim() + "|" + data.F12_BASIS_AMOUNT2.Trim() ; 
                            etaxFile.group_name = "";
                            etaxFile.template_pdf = bodyApiCreateEtaxFile.PdfTemplateId;
                            etaxFile.template_email = "";
                            etaxFile.mode = "form";
                            etaxFile.xml_payment_status = "pending";
                            etaxFile.pdf_payment_status = "pending";
                            etaxFile.create_date = now;

                            if (document_type_id == 2 || document_type_id == 3)
                            {
                                etaxFile.ref_etax_id = data.H07_ADDITIONAL_REF_ASSIGN_ID.Trim();
                                etaxFile.ref_issue_date = DateTime.ParseExact(data.H08_ADDITIONAL_REF_ISSUE_DTM.Trim(), "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                                etaxFile.ref_document_type = data.H09_ADDITIONAL_REF_TYPE_CODE.Trim();
                                etaxFile.remark = data.H05_CREATE_PURPOSE_CODE.Trim() + " - " + data.H06_CREATE_PURPOSE.Trim();
                                etaxFile.original_price = Convert.ToDouble(data.F36_ORIGINAL_TOTAL_AMOUNT.Trim());
                                etaxFile.new_price = Convert.ToDouble(data.F38_LINE_TOTAL_AMOUNT.Trim());
                            }

                            _context.Add(etaxFile);
                            await _context.SaveChangesAsync();


                            foreach (LineItemDataJson item in data.LINE_ITEM_INFORMATION)
                            {
                                _context.Add(new EtaxFileItem()
                                {
                                    etax_file_id = etaxFile.id,
                                    code = item.L02_PRODUCT_ID,
                                    name = item.L03_PRODUCT_NAME,
                                    qty = Convert.ToDouble(item.L17_PRODUCT_QUANTITY.Trim()),
                                    unit = item.L18_PRODUCT_UNIT_CODE.Trim(),
                                    price = Convert.ToDouble(item.L10_PRODUCT_CHARGE_AMOUNT.Trim()),
                                    discount = 0,
                                    tax = 0,
                                    total = Convert.ToDouble(item.L33_LINE_NET_TOTAL_AMOUNT.Trim()),
                                    tax_type = item.L20_LINE_TAX_TYPE_CODE.Trim(),
                                    tax_rate = (int)Convert.ToDouble(item.L21_LINE_TAX_CAL_RATE.Trim()),
                                    other = "",
                                });
                            }
                            await _context.SaveChangesAsync();
                            transaction.Commit();

                            return StatusCode(200, new
                            {
                                data = new
                                {
                                    etax_id = etaxFile.etax_id,
                                },
                                message = "อัพโหลดไฟล์ข้อมูลสำเร็จ",
                            });

                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return StatusCode(400, new
                            {
                                status = "ER",
                                errorCode = "E099",
                                errorMessage = "เกิดความผิดพลาดบ้างอย่างในระบบ กรุณาติดต่อเจ้าหน้าที่",
                            });
                        }
                    }
                }
                else return StatusCode(400, new { message = "มีข้อมูลซ้ำในระบบ" });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new
                {
                    status = "ER",
                    errorCode = "E099",
                    errorMessage = "เกิดความผิดพลาดบ้างอย่างในระบบ กรุณาติดต่อเจ้าหน้าที่",
                });
            }
        }

    }
}
