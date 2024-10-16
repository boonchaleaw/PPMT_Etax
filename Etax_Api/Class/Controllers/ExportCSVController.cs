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

namespace Etax_Api.Class.Controllers
{
    [Produces("application/json")]
    public class ExportCSVController : Controller
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public ExportCSVController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }


        #region ExportxmlFiles

        [HttpPost]
        [Route("admin/export_etaxfiles_csv")]
        public async Task<IActionResult> ExportEtaxFilesCSV([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_xml_menu).FirstOrDefaultAsync();

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

                var result = _context.view_etax_files.Where(x => listDocumentTypeID.Contains(x.document_type_id) && x.delete_status == 0).AsQueryable();

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


                List<ViewEtaxFile> listData = await result.ToListAsync();


                string output = _config["Path:Share"];
                string pathExcel = "/admin/" + jwtStatus.user_id + "/excel/";
                Directory.CreateDirectory(output + pathExcel);
                pathExcel += "XML_Report_CSV.csv";


                ExportxmlFiles(output + pathExcel, bodyDtParameters, listData, _context);



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

        public static void ExportxmlFiles(string path, BodyDtParameters bodyDtParameters, List<ViewEtaxFile> listData, ApplicationDbContext _context)
        {

            using (StreamWriter outputFile = new StreamWriter(path, false, Encoding.UTF8))
            {
                var list = from data in listData
                           join docType in _context.document_type
                           on data.document_type_id equals docType.id
                           select new
                           {
                               data.etax_id,
                               data.member_name,
                               docType.name,
                               data.gen_xml_status,
                               data.issue_date,
                               data.gen_xml_finish,
                               data.member_id,
                               data.group_name
                           };

                outputFile.WriteLine("เลขที่เอกสาร,ผู้ประกอบการ,ประเภทเอกสาร,สถานะ,ออกเอกสาร,วันที่สร้าง");


                foreach (var data in list)
                {
                    string formattedIssueDate = data.issue_date.HasValue ? data.issue_date.Value.ToString("dd-MM-yyyy") : "N/A";
                    string formattedGenXmlFinish = data.gen_xml_finish.HasValue ? data.gen_xml_finish.Value.ToString("dd-MM-yyyy HH:mm:ss") : "N/A";

                    outputFile.WriteLine(
                        "\"" + data.etax_id.Replace("\"", "\"\"") + "\"," +
                        "\"" + data.member_name.Replace("\"", "\"\"").Replace("\r\n", " ").Replace(",", " ") + "\"," +
                        "\"" + data.name + "\"," +
                        "\"" + data.gen_xml_status + "\"," +
                        "\"" + formattedIssueDate + "\"," +
                        "\"" + formattedGenXmlFinish + "\""
                    );


                }
            }
        }

        #endregion

        #region ExportpdfFiles

        [HttpPost]
        [Route("admin/export_pdffiles_csv")]
        public async Task<IActionResult> ExportPdfFilesCSV([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_pdf_menu).FirstOrDefaultAsync();

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
                                                      select td.id
                                                      ).ToListAsync();

                var result = _context.view_etax_files.Where(x => listDocumentTypeID.Contains(x.document_type_id) && x.delete_status == 0).AsQueryable();

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


                List<ViewEtaxFile> listData = await result.ToListAsync();


                string output = _config["Path:Share"];
                string pathExcel = "/admin/" + jwtStatus.user_id + "/excel/";
                Directory.CreateDirectory(output + pathExcel);
                pathExcel += "PDF_Report_CSV.csv";


                ExportpdfFiles(output + pathExcel, bodyDtParameters, listData, _context);



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

        public static void ExportpdfFiles(string path, BodyDtParameters bodyDtParameters, List<ViewEtaxFile> listData, ApplicationDbContext _context)
        {

            using (StreamWriter outputFile = new StreamWriter(path, false, Encoding.UTF8))
            {
                var list = from data in listData
                           join docType in _context.document_type
                           on data.document_type_id equals docType.id
                           select new
                           {
                               data.etax_id,
                               data.member_name,
                               docType.name,
                               gen_xml_status = data.gen_xml_status == "pending" ? "รอดำเนินการ" :
                                         data.gen_xml_status == "success" ? "ดำเนินการสำเร็จ" :
                                         data.gen_xml_status == "fail" ? "ดำเนินการล้มเหลว" : data.gen_xml_status,
                               data.issue_date,
                               data.gen_xml_finish,
                               data.member_id,
                               data.group_name
                           };

                outputFile.WriteLine("เลขที่เอกสาร,ผู้ประกอบการ,ประเภทเอกสาร,สถานะ,ออกเอกสาร,วันที่สร้าง");


                foreach (var data in list)
                {
                    // ฟอร์แมตวันที่
                    string formattedIssueDate = data.issue_date.HasValue ? data.issue_date.Value.ToString("dd/MM/yyyy") : "";
                    string formattedGenXmlFinish = data.gen_xml_finish.HasValue ? data.gen_xml_finish.Value.ToString("dd/MM/yyyy HH:mm:ss") : "";

                    // จัดการกับค่าที่มี " หรือ , หรือ \r\n
                    string etaxId = data.etax_id.Replace("\"", "\"\"");
                    string memberName = data.member_name.Replace("\"", "\"\"").Replace("\r\n", " ").Replace(",", " ");
                    string name = data.name.Replace("\"", "\"\"");
                    string status = data.gen_xml_status.Replace("\"", "\"\"");

                    // เขียนข้อมูลลงไฟล์
                    outputFile.WriteLine(
                        $"\"{etaxId}\"," +
                        $"\"{memberName}\"," +
                        $"\"{name}\"," +
                        $"\"{status}\"," +
                        $"\"{formattedIssueDate}\"," +
                        $"\"{formattedGenXmlFinish}\""
                    );


                }
            }
        }
        #endregion

        #region ExportsendemailFiles

        [HttpPost]
        [Route("admin/export_sendemailfiles_csv")]
        public async Task<IActionResult> ExportSendeMailFilesCSV([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_email_menu).FirstOrDefaultAsync();

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


                var result = _context.view_send_email.Where(x => listDocumentTypeID.Contains(x.document_type_id)).AsQueryable();

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
                                (r.etax_id.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
                                (r.raw_name.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
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
                                (r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.name.Contains(searchBy) && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                            );
                        }
                        else
                        {
                            result = result.Where(r =>
                                (r.document_type_id == document_id && r.etax_id.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
                                (r.document_type_id == document_id && r.raw_name.Contains(searchBy) && r.send_email_finish >= bodyDtParameters.dateStart && r.send_email_finish <= bodyDtParameters.dateEnd) ||
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

                List<ViewSendEmail> listData = result.ToList();


                string output = _config["Path:Share"];
                string pathExcel = "/admin/" + jwtStatus.user_id + "/excel/";
                Directory.CreateDirectory(output + pathExcel);
                pathExcel += "Email_Report_CSV" + jwtStatus.user_id + ".csv";


                ExportsendmailFiles(output + pathExcel, bodyDtParameters, listData, _context);



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

        public static void ExportsendmailFiles(string path, BodyDtParameters bodyDtParameters, List<ViewSendEmail> listData, ApplicationDbContext _context)
        {

            using (StreamWriter outputFile = new StreamWriter(path, false, Encoding.UTF8))
            {
                var list = from data in listData
                           join docType in _context.document_type
                           on data.document_type_id equals docType.id
                           select new
                           {
                               data.etax_id,
                               data.member_name,
                               docType.name,
                               data.buyer_email,
                               send_email_status = data.send_email_status == "pending" ? "รอดำเนินการ" :
                                         data.send_email_status == "success" ? "การส่งสำเร็จ" :
                                         data.send_email_status == "fail" ? "การส่งล้มเหลว" : data.send_email_status,
                               email_status = data.email_status == "pending" ? "รอตรวจสอบ" :
                                         data.email_status == "success" ? "ถึงลูกค้า" :
                                         data.email_status == "open" ? "เปิดอ่านแล้ว" :
                                         data.email_status == "fail" ? "ไม่ถึงลูกค้า" : "",
                               send_email_finish = data.send_email_finish
                           };

                outputFile.WriteLine("เลขที่เอกสาร,ผู้ประกอบการ,ประเภทเอกสาร,อีเมล,สถานะการส่ง,สถานะการรับ,วันที่ส่ง");


                foreach (var data in list)
                {
                    // ฟอร์แมตวันที่
                    string formattedIssueDate = data.send_email_finish.HasValue ? data.send_email_finish.Value.ToString("dd/MM/yyyy HH:mm:ss") : "";

                    // จัดการกับค่าที่มี " หรือ , หรือ \r\n
                    string etaxId = data.etax_id.Replace("\"", "\"\"");
                    string memberName = data.member_name.Replace("\"", "\"\"").Replace("\r\n", " ").Replace(",", " ");
                    string name = data.name.Replace("\"", "\"\"");
                    string email_status = data.email_status.Replace("\"", "\"\"");
                    string buyer_email = data.buyer_email.Replace("\"", "\"\"");
                    string send_email_status = data.send_email_status.Replace("\"", "\"\"");


                    // เขียนข้อมูลลงไฟล์
                    outputFile.WriteLine(
                        $"\"{etaxId}\"," +
                        $"\"{memberName}\"," +
                        $"\"{name}\"," +
                        $"\"{buyer_email}\"," +
                        $"\"{send_email_status}\"," +
                        $"\"{email_status}\"," +
                        $"\"{formattedIssueDate}\""
                    );


                }
            }
        }
        #endregion

        #region ExportsendemailFiles

        [HttpPost]
        [Route("admin/export_sendsmsfiles_csv")]
        public async Task<IActionResult> ExportSendeSMSFilesCSV([FromBody] BodyDtParameters bodyDtParameters)
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

                List<ViewSendSms> listData = result.ToList();


                string output = _config["Path:Share"];
                string pathExcel = "/admin/" + jwtStatus.user_id + "/excel/";
                Directory.CreateDirectory(output + pathExcel);
                pathExcel += "SMS_Report_CSV.csv";


                ExportsendsmsFiles(output + pathExcel, bodyDtParameters, listData, _context);



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

        public static void ExportsendsmsFiles(string path, BodyDtParameters bodyDtParameters, List<ViewSendSms> listData, ApplicationDbContext _context)
        {

            using (StreamWriter outputFile = new StreamWriter(path, false, Encoding.UTF8))
            {
                var list = from data in listData
                           join docType in _context.document_type
                           on data.document_type_id equals docType.id
                           select new
                           {
                               data.etax_id,
                               data.member_name,
                               docType.name,
                               buyer_tel = data.buyer_tel.ToString(),
                               send_sms_status = data.send_sms_status == "pending" ? "รอดำเนินการ" :
                                         data.send_sms_status == "success" ? "การส่งสำเร็จ" :
                                         data.send_sms_status == "fail" ? "การส่งล้มเหลว" : data.send_sms_status,
                               open_sms_status = data.open_sms_status != "success" ? "ไม่มีการเปิด" : "เปิดแล้ว",
                               data.send_sms_finish,
                               data.open_sms_finish
                           };

                outputFile.WriteLine("เลขที่เอกสาร,ผู้ประกอบการ,ประเภทเอกสาร,เบอร์โทรศัพท์,สถานะการส่ง,วันที่ส่ง,สถานะการเปิด,วันที่เปิด");


                foreach (var data in list)
                {
                    // ฟอร์แมตวันที่
                    string send_sms_finish = data.send_sms_finish.HasValue ? data.send_sms_finish.Value.ToString("dd/MM/yyyy HH:mm:ss") : "";
                    string open_sms_finish = data.open_sms_finish.HasValue ? data.open_sms_finish.Value.ToString("dd/MM/yyyy HH:mm:ss") : "";

                    // จัดการกับค่าที่มี " หรือ , หรือ \r\n
                    string etaxId = data.etax_id.Replace("\"", "\"\"");
                    string memberName = data.member_name.Replace("\"", "\"\"").Replace("\r\n", " ").Replace(",", " ");
                    string name = data.name.Replace("\"", "\"\"");
                    string open_sms_status = data.open_sms_status.Replace("\"", "\"\"");
                    string buyer_tel = data.buyer_tel.ToString().Replace("\"", "\"\"");
                    string send_sms_status = data.send_sms_status.Replace("\"", "\"\"");


                    // เขียนข้อมูลลงไฟล์
                    outputFile.WriteLine(
                        $"\"{etaxId}\"," +
                        $"\"{memberName}\"," +
                        $"\"{name}\"," +
                        $"\"{buyer_tel}\"," +
                        $"\"{send_sms_status}\"," +
                        $"\"{send_sms_finish.ToString()}\"," +
                        $"\"{open_sms_status}\"," +
                        $"\"{open_sms_finish.ToString()}\""
                    );


                }
            }
        }
        #endregion

        #region ExportsendemailFiles

        [HttpPost]
        [Route("admin/export_sendebxmlfiles_csv")]
        public async Task<IActionResult> ExportSendeEbxmlFilesCSV([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_ebxml_menu).FirstOrDefaultAsync();

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

                List<ViewSendEbxml> listData = result.ToList();


                string output = _config["Path:Share"];
                string pathExcel = "/admin/" + jwtStatus.user_id + "/excel/";
                Directory.CreateDirectory(output + pathExcel);
                pathExcel += "Send RD CSV.csv";


                ExportsendebxmlFiles(output + pathExcel, bodyDtParameters, listData, _context);



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

        public static void ExportsendebxmlFiles(string path, BodyDtParameters bodyDtParameters, List<ViewSendEbxml> listData, ApplicationDbContext _context)
        {

            using (StreamWriter outputFile = new StreamWriter(path, false, Encoding.UTF8))
            {
                var list = from data in listData
                           join docType in _context.document_type
                           on data.document_type_id equals docType.id
                           select new
                           {
                               data.etax_id,
                               data.member_name,
                               docType.name,
                               send_ebxml_status = data.send_ebxml_status == "pending" ? "รอดำเนินการ" :
                                         data.send_ebxml_status == "success" ? "การส่งสำเร็จ" :
                                         data.send_ebxml_status == "fail" ? "การส่งล้มเหลว" : "",
                               etax_status = data.etax_status == "pending" ? "รอตรวจสอบ" :
                                         data.etax_status == "success" ? "ถูกต้อง" :
                                         data.etax_status == "fail" ? "ไม่ถูกต้อง" : "",
                               data.send_ebxml_finish
                           };

                outputFile.WriteLine("เลขที่เอกสาร,ผู้ประกอบการ,ประเภทเอกสาร,สถานะการส่ง,ความถูกต้อง,วันที่ส่ง");


                foreach (var data in list)
                {
                    // ฟอร์แมตวันที่
                    string send_ebxml_finish = data.send_ebxml_finish.HasValue ? data.send_ebxml_finish.Value.ToString("dd/MM/yyyy HH:mm:ss") : "";


                    // จัดการกับค่าที่มี " หรือ , หรือ \r\n
                    string etaxId = data.etax_id.Replace("\"", "\"\"");
                    string memberName = data.member_name.Replace("\"", "\"\"").Replace("\r\n", " ").Replace(",", " ");
                    string name = data.name.Replace("\"", "\"\"");
                    string send_ebxml_status = data.send_ebxml_status.Replace("\"", "\"\"");
                    string etax_status = data.etax_status.ToString().Replace("\"", "\"\"");


                    // เขียนข้อมูลลงไฟล์
                    outputFile.WriteLine(
                        $"\"{etaxId}\"," +
                        $"\"{memberName}\"," +
                        $"\"{name}\"," +
                        $"\"{send_ebxml_status}\"," +
                        $"\"{etax_status}\"," +
                        $"\"{send_ebxml_finish}\""
                    );


                }
            }
        }
        #endregion

        private IQueryable<ViewEtaxFile> ApplySearchAndDateFilters(IQueryable<ViewEtaxFile> query, string searchBy, BodyDtParameters bodyDtParameters, int document_id)
        {
            if (document_id == 0)
            {
                if (string.IsNullOrEmpty(bodyDtParameters.statusType1))
                {
                    return bodyDtParameters.dateType == "issue_date"
                        ? query.Where(r => (r.etax_id.Contains(searchBy) || r.raw_name.Contains(searchBy) || r.name.Contains(searchBy))
                            && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                        : query.Where(r => (r.etax_id.Contains(searchBy) || r.raw_name.Contains(searchBy) || r.name.Contains(searchBy))
                            && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd);
                }
                else
                {
                    return bodyDtParameters.dateType == "issue_date"
                        ? query.Where(r => r.gen_xml_status == bodyDtParameters.statusType1
                            && (r.etax_id.Contains(searchBy) || r.raw_name.Contains(searchBy) || r.name.Contains(searchBy))
                            && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                        : query.Where(r => r.gen_xml_status == bodyDtParameters.statusType1
                            && (r.etax_id.Contains(searchBy) || r.raw_name.Contains(searchBy) || r.name.Contains(searchBy))
                            && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd);
                }
            }
            else
            {
                return bodyDtParameters.dateType == "issue_date"
                    ? query.Where(r => r.document_type_id == document_id
                        && (r.etax_id.Contains(searchBy) || r.raw_name.Contains(searchBy) || r.name.Contains(searchBy))
                        && r.issue_date >= bodyDtParameters.dateStart && r.issue_date <= bodyDtParameters.dateEnd)
                    : query.Where(r => r.document_type_id == document_id
                        && (r.etax_id.Contains(searchBy) || r.raw_name.Contains(searchBy) || r.name.Contains(searchBy))
                        && r.create_date >= bodyDtParameters.dateStart && r.create_date <= bodyDtParameters.dateEnd);
            }
        }


    }
}
