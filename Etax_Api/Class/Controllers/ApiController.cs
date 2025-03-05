
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
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
    public class ApiController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private Regex rxZipCode = new Regex(@"[0-9]{5}");
        public ApiController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        [HttpPost]
        [Route("create_etax")]
        public async Task<IActionResult> ApiCreateEtaxNew([FromBody] BodyApiCreateEtax bodyApiCreateEtax)
        {
            try
            {
                DateTime now = DateTime.Now;

                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้องหรือหมดอายุ", });

                if (String.IsNullOrEmpty(bodyApiCreateEtax.document_type_code))
                    return StatusCode(400, new { error_code = "2001", message = "กรุณากำหนดประเภทเอกสาร", });

                if (String.IsNullOrEmpty(bodyApiCreateEtax.etax_id))
                    return StatusCode(400, new { error_code = "2002", message = "กรุณากำหนดหมายเลขเอกสาร", });

                if (String.IsNullOrEmpty(bodyApiCreateEtax.issue_date))
                    return StatusCode(400, new { error_code = "2003", message = "กรุณากำหนดวันที่สร้างเอกสาร", });

                if (bodyApiCreateEtax.document_type_code == "2" || bodyApiCreateEtax.document_type_code == "3")
                {
                    if (String.IsNullOrEmpty(bodyApiCreateEtax.ref_etax_id))
                        return StatusCode(400, new { error_code = "2004", message = "กรุณากำหนดหมายเลขเอกสารอ้างอิง", });

                    if (String.IsNullOrEmpty(bodyApiCreateEtax.ref_issue_date))
                        return StatusCode(400, new { error_code = "2005", message = "กรุณากำหนดวันที่สร้างเอกสารอ้างอิง", });
                }

                if (!bodyApiCreateEtax.gen_form_status)
                {
                    if (String.IsNullOrEmpty(bodyApiCreateEtax.buyer.name))
                        return StatusCode(400, new { error_code = "2006", message = "กรุณากำหนดชื่อผู้ซื้อ", });

                    if (String.IsNullOrEmpty(bodyApiCreateEtax.buyer.tax_id))
                        return StatusCode(400, new { error_code = "2007", message = "กรุณากำหนดเลขประจําตัวผู้เสียภาษี", });

                    if (String.IsNullOrEmpty(bodyApiCreateEtax.buyer.address))
                        return StatusCode(400, new { error_code = "2008", message = "กรุณากำหนดที่อยู่", });

                    MatchCollection match = rxZipCode.Matches(bodyApiCreateEtax.buyer.address);
                    if (match.Count > 0)
                    {
                        bodyApiCreateEtax.buyer.zipcode = match[0].Value;
                    }

                    if (String.IsNullOrEmpty(bodyApiCreateEtax.buyer.zipcode))
                        return StatusCode(400, new { error_code = "2010", message = "กรุณากำหนดรหัสไปรษณีย์", });
                }

                if (bodyApiCreateEtax.buyer.branch_code.Length != 5)
                    return StatusCode(400, new { error_code = "2009", message = "กรุณากำหนดรหัสสาขา 5 หลัก", });


                int itemLine = 1;
                foreach (ItemEtax item in bodyApiCreateEtax.items)
                {
                    if (String.IsNullOrEmpty(item.code))
                        return StatusCode(400, new { error_code = "2011", message = "กรุณากำหนดรหัสสินค้า รายการสินค้าที่ " + itemLine, });

                    if (String.IsNullOrEmpty(item.name))
                        return StatusCode(400, new { error_code = "2012", message = "กรุณากำหนดชื่อสินค้า รายการสินค้าที่ " + itemLine, });

                    if (String.IsNullOrEmpty(item.price.ToString()))
                        return StatusCode(400, new { error_code = "2013", message = "กรุณากำหนดจำนวนเงิน รายการสินค้าที่ " + itemLine, });

                    if (String.IsNullOrEmpty(item.total.ToString()))
                        return StatusCode(400, new { error_code = "2014", message = "กรุณากำหนดภาษี รายการสินค้าที่ " + itemLine, });

                    itemLine++;
                }

                var view_member_document_type = _context.view_member_document_type
                    .Where(x => x.member_id == jwtStatus.member_id && x.document_type_id == int.Parse(bodyApiCreateEtax.document_type_code))
                    .FirstOrDefault();

                if (view_member_document_type == null)
                    return StatusCode(400, new { error_code = "1002", message = "ลูกค้าไม่สามารถสร้างเอกสารประเภทนี้ได้" });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var etax_file = _context.etax_files
                       .Where(x => x.member_id == jwtStatus.member_id && x.etax_id == bodyApiCreateEtax.etax_id && x.delete_status == 0)
                       .FirstOrDefault();

                        if (etax_file != null)
                            return StatusCode(400, new { error_code = "1003", message = "ข้อมูลซ้ำในระบบ", });

                        int branch_id = 0;
                        Branch branch = _context.branchs
                        .Where(x => x.member_id == jwtStatus.member_id && x.branch_code == bodyApiCreateEtax.seller.branch_code)
                        .FirstOrDefault();

                        if (branch == null)
                        {
                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.branch_name_th))
                                return StatusCode(400, new { error_code = "2015", message = "กรุณาระบุชื่อสาขา", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.building_number))
                                return StatusCode(400, new { error_code = "2016", message = "กรุณาระบุบ้านเลขที่", });
                            if (bodyApiCreateEtax.seller.building_number.Length > 16)
                                return StatusCode(400, new { error_code = "2017", message = "กรุณาระบุบ้านเลขที่น้อยกว่า 16 หลัก", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.district_name_th))
                                return StatusCode(400, new { error_code = "2018", message = "กรุณาระบุตำบล/แขวง", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.amphoe_name_th))
                                return StatusCode(400, new { error_code = "2019", message = "กรุณาระบุอำเภอ/เขต", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.province_name_th))
                                return StatusCode(400, new { error_code = "2020", message = "กรุณาระบุจังหวัด", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.zipcode))
                                return StatusCode(400, new { error_code = "2021", message = "กรุณาระบุรหัสไปรษณีย์", });

                            Province province = new Province();
                            Amphoe amphoe = new Amphoe();
                            District district = new District();

                            bodyApiCreateEtax.seller.province_name_th = bodyApiCreateEtax.seller.province_name_th.Replace("จังหวัด", "").Replace("จ.", "").Trim();
                            bodyApiCreateEtax.seller.amphoe_name_th = bodyApiCreateEtax.seller.amphoe_name_th.Replace("เขต", "").Replace("อำเภอ", "").Replace("อ.", "").Trim();
                            bodyApiCreateEtax.seller.district_name_th = bodyApiCreateEtax.seller.district_name_th.Replace("แขวง", "").Replace("ตำบล", "").Replace("ต.", "").Trim();

                            List<Province> provinces = _context.province.Where(x => x.province_th.Contains(bodyApiCreateEtax.seller.province_name_th.Trim())).ToList();
                            if (provinces.Count == 1)
                                province = provinces.First();
                            else
                                return StatusCode(400, new { error_code = "2022", message = "ชื่อจังหวัดไม่ถูกต้อง", });



                            List<Amphoe> amphoes = _context.amphoe.Where(x => x.province_code == province.province_code && x.amphoe_th.Contains(bodyApiCreateEtax.seller.amphoe_name_th.Trim())).ToList();
                            if (amphoes.Count > 1)
                            {
                                foreach (Amphoe a in amphoes)
                                {
                                    string a1 = a.amphoe_th.Replace("เขต", "").Replace("อำเภอ", "");
                                    string a2 = bodyApiCreateEtax.seller.amphoe_name_th.Replace("เขต", "").Replace("อำเภอ", "");

                                    if (a1 == a2)
                                    {
                                        amphoe = a;
                                        break;
                                    }
                                }
                            }
                            else if (amphoes.Count == 1)
                                amphoe = amphoes.First();
                            else if (amphoes.Count == 0)
                                return StatusCode(400, new { error_code = "2023", message = "ชื่ออำเภอ/เขตไม่ถูกต้อง", });
                            if (amphoe.amphoe_code == 0)
                                return StatusCode(400, new { error_code = "2023", message = "ชื่ออำเภอ/เขตไม่ถูกต้อง", });


                            List<District> districts = _context.district.Where(x => x.zipcode == bodyApiCreateEtax.seller.zipcode && x.district_th.Contains(bodyApiCreateEtax.seller.district_name_th.Trim())).ToList();
                            if (districts.Count > 1)
                            {
                                foreach (District d in districts)
                                {
                                    string d1 = d.district_th.Replace("แขวง", "").Replace("ตำบล", "");
                                    string d2 = bodyApiCreateEtax.seller.district_name_th.Replace("แขวง", "").Replace("ตำบล", "");

                                    if (d1 == d2)
                                    {
                                        district = d;
                                        break;
                                    }
                                }


                            }
                            else if (districts.Count == 1)
                                district = districts.First();
                            else if (districts.Count == 0)
                                return StatusCode(400, new { error_code = "2024", message = "ชื่อตำบล/แขวงไม่ถูกต้อง", });
                            if (district.district_code == 0)
                                return StatusCode(400, new { error_code = "2024", message = "ชื่อตำบล/แขวงไม่ถูกต้อง", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.zipcode))
                                bodyApiCreateEtax.seller.zipcode = districts.First().zipcode;
                            else
                                if (bodyApiCreateEtax.seller.zipcode != districts.First().zipcode)
                                return StatusCode(400, new { error_code = "2025", message = "รหัสไปรษณีย์ไม่ตรงกับที่อยู่", });


                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.province_name_en))
                                bodyApiCreateEtax.seller.province_name_en = province.province_en;
                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.amphoe_name_en))
                                bodyApiCreateEtax.seller.amphoe_name_en = amphoe.amphoe_en_s;
                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.district_name_en))
                                bodyApiCreateEtax.seller.district_name_en = district.district_en_s;

                            if (!string.IsNullOrEmpty(bodyApiCreateEtax.seller.building_name_th))
                                if (bodyApiCreateEtax.seller.building_name_th.Length > 70)
                                    bodyApiCreateEtax.seller.building_name_th = bodyApiCreateEtax.seller.building_name_th.Substring(0, 70);

                            Branch newBranch = new Branch();
                            newBranch.member_id = jwtStatus.member_id;
                            newBranch.name = bodyApiCreateEtax.seller.branch_name_th.Trim();
                            newBranch.name_en = bodyApiCreateEtax.seller.branch_name_en.Trim();
                            newBranch.branch_code = bodyApiCreateEtax.seller.branch_code.Trim();
                            newBranch.building_number = bodyApiCreateEtax.seller.building_number.Trim();
                            newBranch.building_name = bodyApiCreateEtax.seller.building_name_th.Trim();
                            newBranch.building_name_en = bodyApiCreateEtax.seller.building_name_en.Trim();
                            newBranch.street_name = bodyApiCreateEtax.seller.street_name_th.Trim();
                            newBranch.street_name_en = bodyApiCreateEtax.seller.street_name_en.Trim();
                            newBranch.district_code = district.district_code;
                            newBranch.district_name = bodyApiCreateEtax.seller.district_name_th;
                            newBranch.district_name_en = bodyApiCreateEtax.seller.district_name_en;
                            newBranch.amphoe_code = amphoe.amphoe_code;
                            newBranch.amphoe_name = bodyApiCreateEtax.seller.amphoe_name_th;
                            newBranch.amphoe_name_en = bodyApiCreateEtax.seller.amphoe_name_en;
                            newBranch.province_code = province.province_code;
                            newBranch.province_name = bodyApiCreateEtax.seller.province_name_th;
                            newBranch.province_name_en = bodyApiCreateEtax.seller.province_name_en;
                            newBranch.zipcode = bodyApiCreateEtax.seller.zipcode;
                            newBranch.update_date = now;
                            newBranch.create_date = now;
                            newBranch.delete_status = 0;

                            _context.Add(newBranch);
                            await _context.SaveChangesAsync();

                            branch_id = newBranch.id;

                        }
                        else if (bodyApiCreateEtax.seller.branch_code != "00000")
                        {
                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.branch_name_th))
                                return StatusCode(400, new { error_code = "2015", message = "กรุณาระบุชื่อสาขา", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.building_number))
                                return StatusCode(400, new { error_code = "2016", message = "กรุณาระบุบ้านเลขที่", });
                            if (bodyApiCreateEtax.seller.building_number.Length > 16)
                                return StatusCode(400, new { error_code = "2017", message = "กรุณาระบุบ้านเลขที่น้อยกว่า 16 หลัก", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.district_name_th))
                                return StatusCode(400, new { error_code = "2018", message = "กรุณาระบุตำบล/แขวง", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.amphoe_name_th))
                                return StatusCode(400, new { error_code = "2019", message = "กรุณาระบุอำเภอ/เขต", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.province_name_th))
                                return StatusCode(400, new { error_code = "2020", message = "กรุณาระบุจังหวัด", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.zipcode))
                                return StatusCode(400, new { error_code = "2021", message = "กรุณาระบุรหัสไปรษณีย์", });

                            Province province = new Province();
                            Amphoe amphoe = new Amphoe();
                            District district = new District();

                            bodyApiCreateEtax.seller.province_name_th = bodyApiCreateEtax.seller.province_name_th.Replace("จังหวัด", "").Replace("จ.", "").Trim();
                            bodyApiCreateEtax.seller.amphoe_name_th = bodyApiCreateEtax.seller.amphoe_name_th.Replace("เขต", "").Replace("อำเภอ", "").Replace("อ.", "").Trim();
                            bodyApiCreateEtax.seller.district_name_th = bodyApiCreateEtax.seller.district_name_th.Replace("แขวง", "").Replace("ตำบล", "").Replace("ต.", "").Trim();

                            List<Province> provinces = _context.province.Where(x => x.province_th.Contains(bodyApiCreateEtax.seller.province_name_th.Trim())).ToList();
                            if (provinces.Count == 1)
                                province = provinces.First();
                            else
                                return StatusCode(400, new { error_code = "2022", message = "ชื่อจังหวัดไม่ถูกต้อง", });



                            List<Amphoe> amphoes = _context.amphoe.Where(x => x.province_code == province.province_code && x.amphoe_th.Contains(bodyApiCreateEtax.seller.amphoe_name_th.Trim())).ToList();
                            if (amphoes.Count > 1)
                            {
                                foreach (Amphoe a in amphoes)
                                {
                                    string a1 = a.amphoe_th.Replace("เขต", "").Replace("อำเภอ", "");
                                    string a2 = bodyApiCreateEtax.seller.amphoe_name_th.Replace("เขต", "").Replace("อำเภอ", "");

                                    if (a1 == a2)
                                    {
                                        amphoe = a;
                                        break;
                                    }
                                }
                            }
                            else if (amphoes.Count == 1)
                                amphoe = amphoes.First();
                            else if (amphoes.Count == 0)
                                return StatusCode(400, new { error_code = "2023", message = "ชื่ออำเภอ/เขตไม่ถูกต้อง", });
                            if (amphoe.amphoe_code == 0)
                                return StatusCode(400, new { error_code = "2023", message = "ชื่ออำเภอ/เขตไม่ถูกต้อง", });


                            List<District> districts = _context.district.Where(x => x.amphoe_code == amphoe.amphoe_code && x.district_th.Contains(bodyApiCreateEtax.seller.district_name_th)).ToList();
                            if (districts.Count > 1)
                            {
                                foreach (District d in districts)
                                {
                                    string d1 = d.district_th.Replace("แขวง", "").Replace("ตำบล", "");
                                    string d2 = bodyApiCreateEtax.seller.district_name_th.Replace("แขวง", "").Replace("ตำบล", "");

                                    if (d1 == d2)
                                    {
                                        district = d;
                                        break;
                                    }
                                }


                            }
                            else if (districts.Count == 1)
                                district = districts.First();
                            else if (districts.Count == 0)
                                return StatusCode(400, new { error_code = "2024", message = "ชื่อตำบล/แขวงไม่ถูกต้อง", });
                            if (district.district_code == 0)
                                return StatusCode(400, new { error_code = "2024", message = "ชื่อตำบล/แขวงไม่ถูกต้อง", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.zipcode))
                                bodyApiCreateEtax.seller.zipcode = districts.First().zipcode;
                            else
                                if (bodyApiCreateEtax.seller.zipcode != districts.First().zipcode)
                                return StatusCode(400, new { error_code = "2025", message = "รหัสไปรษณีย์ไม่ตรงกับที่อยู่", });


                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.province_name_en))
                                bodyApiCreateEtax.seller.province_name_en = province.province_en;
                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.amphoe_name_en))
                                bodyApiCreateEtax.seller.amphoe_name_en = amphoe.amphoe_en_s;
                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.district_name_en))
                                bodyApiCreateEtax.seller.district_name_en = district.district_en_s;

                            if (bodyApiCreateEtax.seller.building_name_th.Length > 70)
                                bodyApiCreateEtax.seller.building_name_th = bodyApiCreateEtax.seller.building_name_th.Substring(0, 70);

                            branch.member_id = jwtStatus.member_id;
                            branch.name = bodyApiCreateEtax.seller.branch_name_th.Trim();
                            branch.name_en = bodyApiCreateEtax.seller.branch_name_en.Trim();
                            branch.branch_code = bodyApiCreateEtax.seller.branch_code.Trim();
                            branch.building_number = bodyApiCreateEtax.seller.building_number.Trim();
                            branch.building_name = bodyApiCreateEtax.seller.building_name_th.Trim();
                            branch.building_name_en = bodyApiCreateEtax.seller.building_name_en.Trim();
                            branch.street_name = bodyApiCreateEtax.seller.street_name_th.Trim();
                            branch.street_name_en = bodyApiCreateEtax.seller.street_name_en.Trim();
                            branch.district_code = district.district_code;
                            branch.district_name = bodyApiCreateEtax.seller.district_name_th;
                            branch.district_name_en = bodyApiCreateEtax.seller.district_name_en;
                            branch.amphoe_code = amphoe.amphoe_code;
                            branch.amphoe_name = bodyApiCreateEtax.seller.amphoe_name_th;
                            branch.amphoe_name_en = bodyApiCreateEtax.seller.amphoe_name_en;
                            branch.province_code = province.province_code;
                            branch.province_name = bodyApiCreateEtax.seller.province_name_th;
                            branch.province_name_en = bodyApiCreateEtax.seller.province_name_en;
                            branch.zipcode = bodyApiCreateEtax.seller.zipcode;
                            branch.update_date = now;
                            branch.create_date = now;
                            branch.delete_status = 0;
                            await _context.SaveChangesAsync();

                            branch_id = branch.id;

                        }
                        else
                            branch_id = branch.id;

                        string gen_xml_status = "pending";
                        string gen_pdf_status = "no";
                        string send_email_status = "no";
                        string send_sms_status = "no";
                        string send_ebxml_status = "no";

                        if (bodyApiCreateEtax.pdf_service == "S0")
                        {
                            gen_pdf_status = "no";
                        }
                        else if (bodyApiCreateEtax.pdf_service == "S1")
                        {
                            gen_pdf_status = "pending";
                        }
                        else if (bodyApiCreateEtax.pdf_service == "S2")
                        {
                            gen_pdf_status = "pending";
                            if (String.IsNullOrEmpty(bodyApiCreateEtax.pdf_base64))
                                return StatusCode(400, new { error_code = "2015", message = "ไม่พบข้อมูลไฟล์ PDF", });
                        }
                        else
                        {
                            return StatusCode(400, new { error_code = "1004", message = "ไม่มีการใให้บริการ pdf service ที่มีการระบุ", });
                        }



                        if (bodyApiCreateEtax.email_service == "S0")
                        {
                            send_email_status = "no";
                        }
                        else if (bodyApiCreateEtax.email_service == "S1")
                        {
                            send_email_status = "pending";
                        }
                        else
                        {
                            return StatusCode(400, new { error_code = "1005", message = "ไม่มีการใให้บริการ email service ที่มีการระบุ", });
                        }


                        if (bodyApiCreateEtax.sms_service == "S0")
                        {
                            send_sms_status = "no";
                        }
                        else if (bodyApiCreateEtax.sms_service == "S1")
                        {
                            send_sms_status = "pending";
                        }
                        else
                        {
                            return StatusCode(400, new { error_code = "1006", message = "ไม่มีการใให้บริการ sms service ที่มีการระบุ", });
                        }


                        if (bodyApiCreateEtax.rd_service == "S0")
                        {
                            send_ebxml_status = "no";
                        }
                        else if (bodyApiCreateEtax.rd_service == "S1")
                        {
                            send_ebxml_status = "pending";
                        }
                        else
                        {
                            return StatusCode(400, new { error_code = "1007", message = "ไม่มีการใให้บริการ rd service ที่มีการระบุ", });
                        }


                        EtaxFile etaxFile = new EtaxFile();
                        etaxFile.member_id = jwtStatus.member_id;
                        etaxFile.branch_id = branch_id;
                        etaxFile.member_user_id = jwtStatus.user_id;
                        etaxFile.document_type_id = int.Parse(bodyApiCreateEtax.document_type_code);
                        etaxFile.create_type = "api";
                        etaxFile.gen_xml_status = gen_xml_status;
                        etaxFile.gen_pdf_status = gen_pdf_status;
                        etaxFile.add_email_status = send_email_status;
                        etaxFile.add_sms_status = send_sms_status;
                        etaxFile.add_ebxml_status = send_ebxml_status;
                        etaxFile.send_xml_status = "N";
                        etaxFile.send_other_status = "N";
                        etaxFile.error = "";
                        etaxFile.output_path = _config["Path:Output"];
                        etaxFile.etax_id = bodyApiCreateEtax.etax_id;
                        etaxFile.buyer_branch_code = bodyApiCreateEtax.buyer.branch_code;
                        etaxFile.issue_date = DateTime.ParseExact(bodyApiCreateEtax.issue_date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                        etaxFile.buyer_id = bodyApiCreateEtax.buyer.id;
                        etaxFile.buyer_name = bodyApiCreateEtax.buyer.name;
                        etaxFile.buyer_tax_id = bodyApiCreateEtax.buyer.tax_id;
                        etaxFile.buyer_address = (bodyApiCreateEtax.buyer.address.Contains(bodyApiCreateEtax.buyer.zipcode)) ? bodyApiCreateEtax.buyer.address : bodyApiCreateEtax.buyer.address + " " + bodyApiCreateEtax.buyer.zipcode;
                        etaxFile.buyer_tel = bodyApiCreateEtax.buyer.tel;
                        etaxFile.buyer_fax = bodyApiCreateEtax.buyer.fax;
                        etaxFile.buyer_country_code = bodyApiCreateEtax.buyer.country_code;
                        etaxFile.buyer_email = bodyApiCreateEtax.buyer.email;
                        etaxFile.price = bodyApiCreateEtax.price;
                        etaxFile.discount = bodyApiCreateEtax.discount;
                        etaxFile.tax = bodyApiCreateEtax.tax;
                        etaxFile.total = bodyApiCreateEtax.total;
                        etaxFile.remark = bodyApiCreateEtax.remark;
                        etaxFile.other = bodyApiCreateEtax.other;
                        etaxFile.template_pdf = (bodyApiCreateEtax.template_pdf == null) ? "" : bodyApiCreateEtax.template_pdf;
                        etaxFile.template_email = (bodyApiCreateEtax.template_email == null) ? "" : bodyApiCreateEtax.template_email;

                        if (bodyApiCreateEtax.gen_form_status)
                            etaxFile.mode = "form";
                        else
                            etaxFile.mode = "adhoc";

                        etaxFile.xml_payment_status = "pending";
                        etaxFile.pdf_payment_status = "pending";
                        etaxFile.create_date = now;

                        if (bodyApiCreateEtax.document_type_code == "2")
                        {
                            etaxFile.ref_etax_id = bodyApiCreateEtax.ref_etax_id;
                            etaxFile.ref_issue_date = DateTime.ParseExact(bodyApiCreateEtax.ref_issue_date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                            etaxFile.original_price = bodyApiCreateEtax.original_price;
                            etaxFile.new_price = bodyApiCreateEtax.new_price - bodyApiCreateEtax.original_price;
                        }
                        else if (bodyApiCreateEtax.document_type_code == "3")
                        {
                            etaxFile.ref_etax_id = bodyApiCreateEtax.ref_etax_id;
                            etaxFile.ref_issue_date = DateTime.ParseExact(bodyApiCreateEtax.ref_issue_date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                            etaxFile.original_price = bodyApiCreateEtax.original_price;
                            etaxFile.new_price = bodyApiCreateEtax.original_price - bodyApiCreateEtax.new_price;
                        }

                        _context.Add(etaxFile);
                        await _context.SaveChangesAsync();

                        foreach (ItemEtax item in bodyApiCreateEtax.items)
                        {
                            _context.Add(new EtaxFileItem()
                            {
                                etax_file_id = etaxFile.id,
                                code = item.code,
                                name = item.name,
                                qty = (double)item.qty,
                                unit = item.unit,
                                price = (double)item.price,
                                discount = (double)item.discount,
                                tax = (double)item.tax,
                                total = (double)item.total,
                                tax_type = item.tax_type,
                                tax_rate = (int)item.tax_rate,
                                other = item.other,
                            });
                        }
                        await _context.SaveChangesAsync();

                        LogEtaxFile logEtaxFile = new LogEtaxFile()
                        {
                            member_id = jwtStatus.member_id,
                            user_modify_id = jwtStatus.user_id,
                            etax_id = etaxFile.id,
                            create_type = "api",
                            action_type = "create",
                            create_date = now,
                        };
                        _context.Add(logEtaxFile);
                        await _context.SaveChangesAsync();
                        //transaction.Commit();

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
                        return StatusCode(400, new { error_code = "9000", error_message = "กรุณาแจ้งเจ้าหน้าที่เพื่อตรวจสอบ" });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { error_code = "9000", message = "กรุณาแจ้งเจ้าหน้าที่เพื่อตรวจสอบ" });
            }
        }

        [HttpPost]
        [Route("create_etax_file")]
        public async Task<IActionResult> ApiCreateEtaxFileNew([FromForm] BodyApiCreateEtaxFile bodyApiCreateEtaxFile)
        {
            try
            {
                DateTime now = DateTime.Now;

                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้องหรือหมดอายุ", });

                if (bodyApiCreateEtaxFile.FileData == null)
                    return StatusCode(400, new { error_code = "2016", message = "ไม่พบไฟล์ข้อมูล", });

                string json = "";
                var result = new StringBuilder();
                using (var reader = new StreamReader(bodyApiCreateEtaxFile.FileData.OpenReadStream()))
                {
                    while (reader.Peek() >= 0)
                        result.AppendLine(await reader.ReadLineAsync());
                }
                json = result.ToString();

                try
                {
                    BodyApiCreateEtax data = JsonConvert.DeserializeObject<BodyApiCreateEtax>(json);

                    if (String.IsNullOrEmpty(data.document_type_code))
                        return StatusCode(400, new { error_code = "2001", message = "กรุณากำหนดประเภทเอกสาร", });

                    if (String.IsNullOrEmpty(data.etax_id))
                        return StatusCode(400, new { error_code = "2002", message = "กรุณากำหนดหมายเลขเอกสาร", });

                    if (String.IsNullOrEmpty(data.issue_date))
                        return StatusCode(400, new { error_code = "2003", message = "กรุณากำหนดวันที่สร้างเอกสาร", });

                    if (data.document_type_code == "2" || data.document_type_code == "3")
                    {
                        if (String.IsNullOrEmpty(data.ref_etax_id))
                            return StatusCode(400, new { error_code = "2004", message = "กรุณากำหนดหมายเลขเอกสารอ้างอิง", });

                        if (String.IsNullOrEmpty(data.ref_issue_date))
                            return StatusCode(400, new { error_code = "2005", message = "กรุณากำหนดวันที่สร้างเอกสารอ้างอิง", });
                    }

                    if (String.IsNullOrEmpty(data.buyer.name))
                        return StatusCode(400, new { error_code = "2006", message = "กรุณากำหนดชื่อผู้ซื้อ", });

                    if (String.IsNullOrEmpty(data.buyer.tax_id))
                        return StatusCode(400, new { error_code = "2007", message = "กรุณากำหนดเลขประจําตัวผู้เสียภาษี", });

                    if (String.IsNullOrEmpty(data.buyer.address))
                        return StatusCode(400, new { error_code = "2008", message = "กรุณากำหนดที่อยู่", });

                    if (data.buyer.branch_code.Length != 5)
                        return StatusCode(400, new { error_code = "2009", message = "กรุณากำหนดรหัสสาขา 5 หลัก", });


                    MatchCollection match = rxZipCode.Matches(data.buyer.address);
                    if (match.Count > 0)
                    {
                        data.buyer.zipcode = match[0].Value;
                    }

                    if (String.IsNullOrEmpty(data.buyer.zipcode))
                        return StatusCode(400, new { error_code = "2010", message = "กรุณากำหนดรหัสไปรษณีย์", });

                    int itemLine = 1;
                    foreach (ItemEtax item in data.items)
                    {
                        if (String.IsNullOrEmpty(item.code))
                            return StatusCode(400, new { error_code = "2011", message = "กรุณากำหนดรหัสสินค้า รายการสินค้าที่ " + itemLine, });

                        if (String.IsNullOrEmpty(item.name))
                            return StatusCode(400, new { error_code = "2012", message = "กรุณากำหนดชื่อสินค้า รายการสินค้าที่ " + itemLine, });

                        if (String.IsNullOrEmpty(item.price.ToString()))
                            return StatusCode(400, new { error_code = "2013", message = "กรุณากำหนดจำนวนเงิน รายการสินค้าที่ " + itemLine, });

                        if (String.IsNullOrEmpty(item.total.ToString()))
                            return StatusCode(400, new { error_code = "2014", message = "กรุณากำหนดภาษี รายการสินค้าที่ " + itemLine, });

                        itemLine++;
                    }

                    var view_member_document_type = _context.view_member_document_type
                        .Where(x => x.member_id == jwtStatus.member_id && x.document_type_id == int.Parse(data.document_type_code))
                        .FirstOrDefault();

                    if (view_member_document_type == null)
                        return StatusCode(400, new { error_code = "1002", message = "ลูกค้าไม่สามารถสร้างเอกสารประเภทนี้ได้" });

                    using (var transaction = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            var etax_file = _context.etax_files
                           .Where(x => x.member_id == jwtStatus.member_id && x.etax_id == data.etax_id && x.delete_status == 0)
                           .FirstOrDefault();

                            if (etax_file != null)
                                return StatusCode(400, new { error_code = "1003", message = "ข้อมูลซ้ำในระบบ", });

                            int branch_id = 0;
                            Branch branch = _context.branchs
                            .Where(x => x.member_id == jwtStatus.member_id && x.branch_code == data.seller.branch_code)
                            .FirstOrDefault();

                            if (branch == null)
                            {
                                if (string.IsNullOrEmpty(data.seller.branch_name_th))
                                    return StatusCode(400, new { error_code = "2015", message = "กรุณาระบุชื่อสาขา", });

                                if (string.IsNullOrEmpty(data.seller.building_number))
                                    return StatusCode(400, new { error_code = "2016", message = "กรุณาระบุบ้านเลขที่", });
                                if (data.seller.building_number.Length > 16)
                                    return StatusCode(400, new { error_code = "2017", message = "กรุณาระบุบ้านเลขที่น้อยกว่า 16 หลัก", });

                                if (string.IsNullOrEmpty(data.seller.district_name_th))
                                    return StatusCode(400, new { error_code = "2018", message = "กรุณาระบุตำบล/แขวง", });

                                if (string.IsNullOrEmpty(data.seller.amphoe_name_th))
                                    return StatusCode(400, new { error_code = "2019", message = "กรุณาระบุอำเภอ/เขต", });

                                if (string.IsNullOrEmpty(data.seller.province_name_th))
                                    return StatusCode(400, new { error_code = "2020", message = "กรุณาระบุจังหวัด", });

                                if (string.IsNullOrEmpty(data.seller.zipcode))
                                    return StatusCode(400, new { error_code = "2021", message = "กรุณาระบุรหัสไปรษณีย์", });

                                Province province = new Province();
                                Amphoe amphoe = new Amphoe();
                                District district = new District();

                                data.seller.province_name_th = data.seller.province_name_th.Replace("จังหวัด", "").Replace("จ.", "").Trim();
                                data.seller.amphoe_name_th = data.seller.amphoe_name_th.Replace("เขต", "").Replace("อำเภอ", "").Replace("อ.", "").Trim();
                                data.seller.district_name_th = data.seller.district_name_th.Replace("แขวง", "").Replace("ตำบล", "").Replace("ต.", "").Trim();

                                List<Province> provinces = _context.province.Where(x => x.province_th.Contains(data.seller.province_name_th.Trim())).ToList();
                                if (provinces.Count == 1)
                                    province = provinces.First();
                                else
                                    return StatusCode(400, new { error_code = "2022", message = "ชื่อจังหวัดไม่ถูกต้อง", });



                                List<Amphoe> amphoes = _context.amphoe.Where(x => x.province_code == province.province_code && x.amphoe_th.Contains(data.seller.amphoe_name_th.Trim())).ToList();
                                if (amphoes.Count > 1)
                                {
                                    foreach (Amphoe a in amphoes)
                                    {
                                        string a1 = a.amphoe_th.Replace("เขต", "").Replace("อำเภอ", "");
                                        string a2 = data.seller.amphoe_name_th.Replace("เขต", "").Replace("อำเภอ", "");

                                        if (a1 == a2)
                                        {
                                            amphoe = a;
                                            break;
                                        }
                                    }
                                }
                                else if (amphoes.Count == 1)
                                    amphoe = amphoes.First();
                                else if (amphoes.Count == 0)
                                    return StatusCode(400, new { error_code = "2023", message = "ชื่ออำเภอ/เขตไม่ถูกต้อง", });
                                if (amphoe.amphoe_code == 0)
                                    return StatusCode(400, new { error_code = "2023", message = "ชื่ออำเภอ/เขตไม่ถูกต้อง", });


                                List<District> districts = _context.district.Where(x => x.zipcode == data.seller.zipcode && x.district_th.Contains(data.seller.district_name_th.Trim())).ToList();
                                if (districts.Count > 1)
                                {
                                    foreach (District d in districts)
                                    {
                                        string d1 = d.district_th.Replace("แขวง", "").Replace("ตำบล", "");
                                        string d2 = data.seller.district_name_th.Replace("แขวง", "").Replace("ตำบล", "");

                                        if (d1 == d2)
                                        {
                                            district = d;
                                            break;
                                        }
                                    }


                                }
                                else if (districts.Count == 1)
                                    district = districts.First();
                                else if (districts.Count == 0)
                                    return StatusCode(400, new { error_code = "2024", message = "ชื่อตำบล/แขวงไม่ถูกต้อง", });
                                if (district.district_code == 0)
                                    return StatusCode(400, new { error_code = "2024", message = "ชื่อตำบล/แขวงไม่ถูกต้อง", });

                                if (string.IsNullOrEmpty(data.seller.zipcode))
                                    data.seller.zipcode = districts.First().zipcode;
                                else
                                    if (data.seller.zipcode != districts.First().zipcode)
                                    return StatusCode(400, new { error_code = "2025", message = "รหัสไปรษณีย์ไม่ตรงกับที่อยู่", });


                                if (string.IsNullOrEmpty(data.seller.province_name_en))
                                    data.seller.province_name_en = province.province_en;
                                if (string.IsNullOrEmpty(data.seller.amphoe_name_en))
                                    data.seller.amphoe_name_en = amphoe.amphoe_en_s;
                                if (string.IsNullOrEmpty(data.seller.district_name_en))
                                    data.seller.district_name_en = district.district_en_s;

                                if (!string.IsNullOrEmpty(data.seller.building_name_th))
                                    if (data.seller.building_name_th.Length > 70)
                                        data.seller.building_name_th = data.seller.building_name_th.Substring(0, 70);

                                Branch newBranch = new Branch();
                                newBranch.member_id = jwtStatus.member_id;
                                newBranch.name = data.seller.branch_name_th.Trim();
                                newBranch.name_en = data.seller.branch_name_en.Trim();
                                newBranch.branch_code = data.seller.branch_code.Trim();
                                newBranch.building_number = data.seller.building_number.Trim();
                                newBranch.building_name = data.seller.building_name_th.Trim();
                                newBranch.building_name_en = data.seller.building_name_en.Trim();
                                newBranch.street_name = data.seller.street_name_th.Trim();
                                newBranch.street_name_en = data.seller.street_name_en.Trim();
                                newBranch.district_code = district.district_code;
                                newBranch.district_name = data.seller.district_name_th;
                                newBranch.district_name_en = data.seller.district_name_en;
                                newBranch.amphoe_code = amphoe.amphoe_code;
                                newBranch.amphoe_name = data.seller.amphoe_name_th;
                                newBranch.amphoe_name_en = data.seller.amphoe_name_en;
                                newBranch.province_code = province.province_code;
                                newBranch.province_name = data.seller.province_name_th;
                                newBranch.province_name_en = data.seller.province_name_en;
                                newBranch.zipcode = data.seller.zipcode;
                                newBranch.update_date = now;
                                newBranch.create_date = now;
                                newBranch.delete_status = 0;

                                _context.Add(newBranch);
                                await _context.SaveChangesAsync();

                                branch_id = newBranch.id;
                            }
                            else if (data.seller.branch_code != "00000")
                            {
                                if (string.IsNullOrEmpty(data.seller.branch_name_th))
                                    return StatusCode(400, new { error_code = "2015", message = "กรุณาระบุชื่อสาขา", });

                                if (string.IsNullOrEmpty(data.seller.building_number))
                                    return StatusCode(400, new { error_code = "2016", message = "กรุณาระบุบ้านเลขที่", });
                                if (data.seller.building_number.Length > 16)
                                    return StatusCode(400, new { error_code = "2017", message = "กรุณาระบุบ้านเลขที่น้อยกว่า 16 หลัก", });

                                if (string.IsNullOrEmpty(data.seller.district_name_th))
                                    return StatusCode(400, new { error_code = "2018", message = "กรุณาระบุตำบล/แขวง", });

                                if (string.IsNullOrEmpty(data.seller.amphoe_name_th))
                                    return StatusCode(400, new { error_code = "2019", message = "กรุณาระบุอำเภอ/เขต", });

                                if (string.IsNullOrEmpty(data.seller.province_name_th))
                                    return StatusCode(400, new { error_code = "2020", message = "กรุณาระบุจังหวัด", });

                                if (string.IsNullOrEmpty(data.seller.zipcode))
                                    return StatusCode(400, new { error_code = "2021", message = "กรุณาระบุรหัสไปรษณีย์", });

                                Province province = new Province();
                                Amphoe amphoe = new Amphoe();
                                District district = new District();

                                data.seller.province_name_th = data.seller.province_name_th.Replace("จังหวัด", "").Replace("จ.", "").Trim();
                                data.seller.amphoe_name_th = data.seller.amphoe_name_th.Replace("เขต", "").Replace("อำเภอ", "").Replace("อ.", "").Trim();
                                data.seller.district_name_th = data.seller.district_name_th.Replace("แขวง", "").Replace("ตำบล", "").Replace("ต.", "").Trim();

                                List<Province> provinces = _context.province.Where(x => x.province_th.Contains(data.seller.province_name_th.Trim())).ToList();
                                if (provinces.Count == 1)
                                    province = provinces.First();
                                else
                                    return StatusCode(400, new { error_code = "2022", message = "ชื่อจังหวัดไม่ถูกต้อง", });



                                List<Amphoe> amphoes = _context.amphoe.Where(x => x.province_code == province.province_code && x.amphoe_th.Contains(data.seller.amphoe_name_th.Trim())).ToList();
                                if (amphoes.Count > 1)
                                {
                                    foreach (Amphoe a in amphoes)
                                    {
                                        string a1 = a.amphoe_th.Replace("เขต", "").Replace("อำเภอ", "");
                                        string a2 = data.seller.amphoe_name_th.Replace("เขต", "").Replace("อำเภอ", "");

                                        if (a1 == a2)
                                        {
                                            amphoe = a;
                                            break;
                                        }
                                    }
                                }
                                else if (amphoes.Count == 1)
                                    amphoe = amphoes.First();
                                else if (amphoes.Count == 0)
                                    return StatusCode(400, new { error_code = "2023", message = "ชื่ออำเภอ/เขตไม่ถูกต้อง", });
                                if (amphoe.amphoe_code == 0)
                                    return StatusCode(400, new { error_code = "2023", message = "ชื่ออำเภอ/เขตไม่ถูกต้อง", });


                                List<District> districts = _context.district.Where(x => x.amphoe_code == amphoe.amphoe_code && x.district_th.Contains(data.seller.district_name_th)).ToList();
                                if (districts.Count > 1)
                                {
                                    foreach (District d in districts)
                                    {
                                        string d1 = d.district_th.Replace("แขวง", "").Replace("ตำบล", "");
                                        string d2 = data.seller.district_name_th.Replace("แขวง", "").Replace("ตำบล", "");

                                        if (d1 == d2)
                                        {
                                            district = d;
                                            break;
                                        }
                                    }


                                }
                                else if (districts.Count == 1)
                                    district = districts.First();
                                else if (districts.Count == 0)
                                    return StatusCode(400, new { error_code = "2024", message = "ชื่อตำบล/แขวงไม่ถูกต้อง", });
                                if (district.district_code == 0)
                                    return StatusCode(400, new { error_code = "2024", message = "ชื่อตำบล/แขวงไม่ถูกต้อง", });

                                if (string.IsNullOrEmpty(data.seller.zipcode))
                                    data.seller.zipcode = districts.First().zipcode;
                                else
                                    if (data.seller.zipcode != districts.First().zipcode)
                                    return StatusCode(400, new { error_code = "2025", message = "รหัสไปรษณีย์ไม่ตรงกับที่อยู่", });


                                if (string.IsNullOrEmpty(data.seller.province_name_en))
                                    data.seller.province_name_en = province.province_en;
                                if (string.IsNullOrEmpty(data.seller.amphoe_name_en))
                                    data.seller.amphoe_name_en = amphoe.amphoe_en_s;
                                if (string.IsNullOrEmpty(data.seller.district_name_en))
                                    data.seller.district_name_en = district.district_en_s;

                                if (data.seller.building_name_th.Length > 70)
                                    data.seller.building_name_th = data.seller.building_name_th.Substring(0, 70);

                                branch.member_id = jwtStatus.member_id;
                                branch.name = data.seller.branch_name_th.Trim();
                                branch.name_en = data.seller.branch_name_en.Trim();
                                branch.branch_code = data.seller.branch_code.Trim();
                                branch.building_number = data.seller.building_number.Trim();
                                branch.building_name = data.seller.building_name_th.Trim();
                                branch.building_name_en = data.seller.building_name_en.Trim();
                                branch.street_name = data.seller.street_name_th.Trim();
                                branch.street_name_en = data.seller.street_name_en.Trim();
                                branch.district_code = district.district_code;
                                branch.district_name = data.seller.district_name_th;
                                branch.district_name_en = data.seller.district_name_en;
                                branch.amphoe_code = amphoe.amphoe_code;
                                branch.amphoe_name = data.seller.amphoe_name_th;
                                branch.amphoe_name_en = data.seller.amphoe_name_en;
                                branch.province_code = province.province_code;
                                branch.province_name = data.seller.province_name_th;
                                branch.province_name_en = data.seller.province_name_en;
                                branch.zipcode = data.seller.zipcode;
                                branch.update_date = now;
                                branch.create_date = now;
                                branch.delete_status = 0;
                                await _context.SaveChangesAsync();

                                branch_id = branch.id;

                            }
                            else
                                branch_id = branch.id;

                            string gen_xml_status = "pending";
                            string gen_pdf_status = "no";
                            string send_email_status = "no";
                            string send_sms_status = "no";
                            string send_ebxml_status = "no";

                            if (data.pdf_service == "S0")
                            {
                                gen_pdf_status = "no";
                            }
                            else if (data.pdf_service == "S1")
                            {
                                gen_pdf_status = "pending";
                            }
                            else if (data.pdf_service == "S2")
                            {
                                gen_pdf_status = "pending";


                                if (bodyApiCreateEtaxFile.FilePdf != null)
                                {
                                    string pdf = "";
                                    result = new StringBuilder();
                                    using (var reader = new StreamReader(bodyApiCreateEtaxFile.FilePdf.OpenReadStream()))
                                    {
                                        while (reader.Peek() >= 0)
                                            result.AppendLine(await reader.ReadLineAsync());
                                    }
                                    pdf = result.ToString();
                                }
                                else
                                    return StatusCode(400, new { error_code = "2015", message = "ไม่พบข้อมูลไฟล์ PDF", });
                            }
                            else
                            {
                                return StatusCode(400, new { error_code = "1004", message = "ไม่มีการใให้บริการ pdf service ที่มีการระบุ", });
                            }



                            if (data.email_service == "S0")
                            {
                                send_email_status = "no";
                            }
                            else if (data.email_service == "S1")
                            {
                                send_email_status = "pending";
                            }
                            else
                            {
                                return StatusCode(400, new { error_code = "1005", message = "ไม่มีการใให้บริการ email service ที่มีการระบุ", });
                            }


                            if (data.sms_service == "S0")
                            {
                                send_sms_status = "no";
                            }
                            else if (data.sms_service == "S1")
                            {
                                send_sms_status = "pending";
                            }
                            else
                            {
                                return StatusCode(400, new { error_code = "1006", message = "ไม่มีการใให้บริการ sms service ที่มีการระบุ", });
                            }


                            if (data.rd_service == "S0")
                            {
                                send_ebxml_status = "no";
                            }
                            else if (data.rd_service == "S1")
                            {
                                send_ebxml_status = "pending";
                            }
                            else
                            {
                                return StatusCode(400, new { error_code = "1007", message = "ไม่มีการใให้บริการ rd service ที่มีการระบุ", });
                            }


                            EtaxFile etaxFile = new EtaxFile();
                            etaxFile.member_id = jwtStatus.member_id;
                            etaxFile.branch_id = branch_id;
                            etaxFile.member_user_id = jwtStatus.user_id;
                            etaxFile.document_type_id = int.Parse(data.document_type_code);
                            etaxFile.create_type = "api";
                            etaxFile.gen_xml_status = gen_xml_status;
                            etaxFile.gen_pdf_status = gen_pdf_status;
                            etaxFile.add_email_status = send_email_status;
                            etaxFile.add_sms_status = send_sms_status;
                            etaxFile.add_ebxml_status = send_ebxml_status;
                            etaxFile.send_xml_status = "N";
                            etaxFile.send_other_status = "N";
                            etaxFile.error = "";
                            etaxFile.output_path = _config["Path:Output"];
                            etaxFile.etax_id = data.etax_id;
                            etaxFile.buyer_branch_code = data.buyer.branch_code;
                            etaxFile.issue_date = DateTime.ParseExact(data.issue_date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                            etaxFile.buyer_id = data.buyer.id;
                            etaxFile.buyer_name = data.buyer.name;
                            etaxFile.buyer_tax_id = data.buyer.tax_id;
                            etaxFile.buyer_address = (data.buyer.address.Contains(data.buyer.zipcode)) ? data.buyer.address : data.buyer.address + " " + data.buyer.zipcode;
                            etaxFile.buyer_tel = data.buyer.tel;
                            etaxFile.buyer_fax = data.buyer.fax;
                            etaxFile.buyer_country_code = data.buyer.country_code;
                            etaxFile.buyer_email = data.buyer.email;
                            etaxFile.price = data.price;
                            etaxFile.discount = data.discount;
                            etaxFile.tax = data.tax;
                            etaxFile.total = data.total;
                            etaxFile.remark = data.remark;
                            etaxFile.other = data.other;
                            etaxFile.template_pdf = (data.template_pdf == null) ? "" : data.template_pdf;
                            etaxFile.template_email = (data.template_email == null) ? "" : data.template_email;
                            etaxFile.mode = "adhoc";
                            etaxFile.xml_payment_status = "pending";
                            etaxFile.pdf_payment_status = "pending";
                            etaxFile.create_date = now;

                            if (data.document_type_code == "2" || data.document_type_code == "3")
                            {
                                etaxFile.ref_etax_id = data.ref_etax_id;
                                etaxFile.ref_issue_date = DateTime.ParseExact(data.ref_issue_date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                                etaxFile.original_price = data.original_price;
                            }

                            _context.Add(etaxFile);
                            await _context.SaveChangesAsync();

                            foreach (ItemEtax item in data.items)
                            {
                                _context.Add(new EtaxFileItem()
                                {
                                    etax_file_id = etaxFile.id,
                                    code = item.code,
                                    name = item.name,
                                    qty = (double)item.qty,
                                    unit = item.unit,
                                    price = (double)item.price,
                                    discount = (double)item.discount,
                                    tax = (double)item.tax,
                                    total = (double)item.total,
                                    tax_type = item.tax_type,
                                    tax_rate = (int)item.tax_rate,
                                    other = item.other,
                                });
                            }
                            await _context.SaveChangesAsync();

                            LogEtaxFile logEtaxFile = new LogEtaxFile()
                            {
                                member_id = jwtStatus.member_id,
                                user_modify_id = jwtStatus.user_id,
                                etax_id = etaxFile.id,
                                create_type = "api",
                                action_type = "create",
                                create_date = now,
                            };
                            _context.Add(logEtaxFile);
                            await _context.SaveChangesAsync();
                            //transaction.Commit();

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
                            return StatusCode(400, new { error_code = "9000", error_message = "กรุณาแจ้งเจ้าหน้าที่เพื่อตรวจสอบ" });
                        }
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(400, new { error_code = "9000", message = "กรุณาแจ้งเจ้าหน้าที่เพื่อตรวจสอบ" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { error_code = "9000", message = "กรุณาแจ้งเจ้าหน้าที่เพื่อตรวจสอบ" });
            }
        }

        [HttpPost]
        [Route("ppmt_create_etax")]
        public async Task<IActionResult> ApiCreateEtax([FromBody] BodyApiCreateEtax bodyApiCreateEtax)
        {
            try
            {
                DateTime now = DateTime.Now;

                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                if (String.IsNullOrEmpty(bodyApiCreateEtax.document_type_code))
                    return StatusCode(400, new { message = "กรุณากำหนดประเภทเอกสาร", });

                if (String.IsNullOrEmpty(bodyApiCreateEtax.etax_id))
                    return StatusCode(400, new { message = "กรุณากำหนดหมายเลขเอกสาร", });

                if (String.IsNullOrEmpty(bodyApiCreateEtax.issue_date))
                    return StatusCode(400, new { message = "กรุณากำหนดวันที่สร้างเอกสาร", });

                if (bodyApiCreateEtax.document_type_code == "2" || bodyApiCreateEtax.document_type_code == "3")
                {
                    if (String.IsNullOrEmpty(bodyApiCreateEtax.ref_etax_id))
                        return StatusCode(400, new { message = "กรุณากำหนดหมายเลขเอกสารอ้างอิง", });

                    if (String.IsNullOrEmpty(bodyApiCreateEtax.ref_issue_date))
                        return StatusCode(400, new { message = "กรุณากำหนดวันที่สร้างเอกสารอ้างอิง", });
                }

                if (!bodyApiCreateEtax.gen_form_status)
                {
                    if (String.IsNullOrEmpty(bodyApiCreateEtax.buyer.name))
                        return StatusCode(400, new { message = "กรุณากำหนดชื่อผู้ซื้อ", });

                    if (String.IsNullOrEmpty(bodyApiCreateEtax.buyer.tax_id))
                        return StatusCode(400, new { message = "กรุณากำหนดเลขประจําตัวผู้เสียภาษี", });

                    if (String.IsNullOrEmpty(bodyApiCreateEtax.buyer.address))
                        return StatusCode(400, new { message = "กรุณากำหนดที่อยู่", });

                    MatchCollection match = rxZipCode.Matches(bodyApiCreateEtax.buyer.address);
                    if (match.Count > 0)
                    {
                        bodyApiCreateEtax.buyer.zipcode = match[0].Value;
                    }

                    if (String.IsNullOrEmpty(bodyApiCreateEtax.buyer.zipcode))
                        return StatusCode(400, new { message = "กรุณากำหนดรหัสไปรษณีย์", });
                }

                if (bodyApiCreateEtax.buyer.branch_code.Length != 5)
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสสาขา 5 หลัก", });


                int itemLine = 1;
                foreach (ItemEtax item in bodyApiCreateEtax.items)
                {
                    if (String.IsNullOrEmpty(item.code))
                        return StatusCode(400, new { message = "กรุณากำหนดรหัสสินค้า รายการสินค้าที่ " + itemLine, });

                    if (String.IsNullOrEmpty(item.name))
                        return StatusCode(400, new { message = "กรุณากำหนดชื่อสินค้า รายการสินค้าที่ " + itemLine, });

                    if (String.IsNullOrEmpty(item.price.ToString()))
                        return StatusCode(400, new { message = "กรุณากำหนดจำนวนเงิน รายการสินค้าที่ " + itemLine, });

                    if (String.IsNullOrEmpty(item.total.ToString()))
                        return StatusCode(400, new { message = "กรุณากำหนดภาษี รายการสินค้าที่ " + itemLine, });

                    itemLine++;
                }

                var view_member_document_type = _context.view_member_document_type
                    .Where(x => x.member_id == jwtStatus.member_id && x.document_type_id == int.Parse(bodyApiCreateEtax.document_type_code))
                    .FirstOrDefault();

                if (view_member_document_type == null)
                    return StatusCode(400, new { message = "ลูกค้าไม่สามารถสร้างเอกสารประเภทนี้ได้" });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var etax_file = _context.etax_files
                       .Where(x => x.member_id == jwtStatus.member_id && x.etax_id == bodyApiCreateEtax.etax_id && x.delete_status == 0)
                       .FirstOrDefault();

                        if (etax_file != null)
                            return StatusCode(400, new { message = "ข้อมูลซ้ำในระบบ", });

                        int branch_id = 0;
                        Branch branch = _context.branchs
                        .Where(x => x.member_id == jwtStatus.member_id && x.branch_code == bodyApiCreateEtax.seller.branch_code)
                        .FirstOrDefault();

                        if (branch == null)
                        {
                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.branch_name_th))
                                return StatusCode(400, new { message = "กรุณาระบุชื่อสาขา", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.building_number))
                                return StatusCode(400, new { message = "กรุณาระบุบ้านเลขที่", });
                            if (bodyApiCreateEtax.seller.building_number.Length > 16)
                                return StatusCode(400, new { message = "กรุณาระบุบ้านเลขที่น้อยกว่า 16 หลัก", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.district_name_th))
                                return StatusCode(400, new { message = "กรุณาระบุตำบล/แขวง", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.amphoe_name_th))
                                return StatusCode(400, new { message = "กรุณาระบุอำเภอ/เขต", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.province_name_th))
                                return StatusCode(400, new { message = "กรุณาระบุจังหวัด", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.zipcode))
                                return StatusCode(400, new { message = "กรุณาระบุรหัสไปรษณีย์", });

                            Province province = new Province();
                            Amphoe amphoe = new Amphoe();
                            District district = new District();

                            bodyApiCreateEtax.seller.province_name_th = bodyApiCreateEtax.seller.province_name_th.Replace("จังหวัด", "").Replace("จ.", "").Trim();
                            bodyApiCreateEtax.seller.amphoe_name_th = bodyApiCreateEtax.seller.amphoe_name_th.Replace("เขต", "").Replace("อำเภอ", "").Replace("อ.", "").Trim();
                            bodyApiCreateEtax.seller.district_name_th = bodyApiCreateEtax.seller.district_name_th.Replace("แขวง", "").Replace("ตำบล", "").Replace("ต.", "").Trim();

                            List<Province> provinces = _context.province.Where(x => x.province_th.Contains(bodyApiCreateEtax.seller.province_name_th.Trim())).ToList();
                            if (provinces.Count == 1)
                                province = provinces.First();
                            else
                                return StatusCode(400, new { message = "ชื่อจังหวัดไม่ถูกต้อง", });



                            List<Amphoe> amphoes = _context.amphoe.Where(x => x.province_code == province.province_code && x.amphoe_th.Contains(bodyApiCreateEtax.seller.amphoe_name_th.Trim())).ToList();
                            if (amphoes.Count > 1)
                            {
                                foreach (Amphoe a in amphoes)
                                {
                                    string a1 = a.amphoe_th.Replace("เขต", "").Replace("อำเภอ", "");
                                    string a2 = bodyApiCreateEtax.seller.amphoe_name_th.Replace("เขต", "").Replace("อำเภอ", "");

                                    if (a1 == a2)
                                    {
                                        amphoe = a;
                                        break;
                                    }
                                }
                            }
                            else if (amphoes.Count == 1)
                                amphoe = amphoes.First();
                            else if (amphoes.Count == 0)
                                return StatusCode(400, new { message = "ชื่ออำเภอ/เขตไม่ถูกต้อง", });
                            if (amphoe.amphoe_code == 0)
                                return StatusCode(400, new { message = "ชื่ออำเภอ/เขตไม่ถูกต้อง", });


                            List<District> districts = _context.district.Where(x => x.zipcode == bodyApiCreateEtax.seller.zipcode && x.district_th.Contains(bodyApiCreateEtax.seller.district_name_th.Trim())).ToList();
                            if (districts.Count > 1)
                            {
                                foreach (District d in districts)
                                {
                                    string d1 = d.district_th.Replace("แขวง", "").Replace("ตำบล", "");
                                    string d2 = bodyApiCreateEtax.seller.district_name_th.Replace("แขวง", "").Replace("ตำบล", "");

                                    if (d1 == d2)
                                    {
                                        district = d;
                                        break;
                                    }
                                }


                            }
                            else if (districts.Count == 1)
                                district = districts.First();
                            else if (districts.Count == 0)
                                return StatusCode(400, new { message = "ชื่อตำบล/แขวงไม่ถูกต้อง", });
                            if (district.district_code == 0)
                                return StatusCode(400, new { message = "ชื่อตำบล/แขวงไม่ถูกต้อง", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.zipcode))
                                bodyApiCreateEtax.seller.zipcode = districts.First().zipcode;
                            else
                                if (bodyApiCreateEtax.seller.zipcode != districts.First().zipcode)
                                return StatusCode(400, new { message = "รหัสไปรษณีย์ไม่ตรงกับที่อยู่", });


                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.province_name_en))
                                bodyApiCreateEtax.seller.province_name_en = province.province_en;
                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.amphoe_name_en))
                                bodyApiCreateEtax.seller.amphoe_name_en = amphoe.amphoe_en_s;
                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.district_name_en))
                                bodyApiCreateEtax.seller.district_name_en = district.district_en_s;

                            if (!string.IsNullOrEmpty(bodyApiCreateEtax.seller.building_name_th))
                                if (bodyApiCreateEtax.seller.building_name_th.Length > 70)
                                    bodyApiCreateEtax.seller.building_name_th = bodyApiCreateEtax.seller.building_name_th.Substring(0, 70);

                            Branch newBranch = new Branch();
                            newBranch.member_id = jwtStatus.member_id;
                            newBranch.name = bodyApiCreateEtax.seller.branch_name_th.Trim();
                            newBranch.name_en = bodyApiCreateEtax.seller.branch_name_en.Trim();
                            newBranch.branch_code = bodyApiCreateEtax.seller.branch_code.Trim();
                            newBranch.building_number = bodyApiCreateEtax.seller.building_number.Trim();
                            newBranch.building_name = bodyApiCreateEtax.seller.building_name_th.Trim();
                            newBranch.building_name_en = bodyApiCreateEtax.seller.building_name_en.Trim();
                            newBranch.street_name = bodyApiCreateEtax.seller.street_name_th.Trim();
                            newBranch.street_name_en = bodyApiCreateEtax.seller.street_name_en.Trim();
                            newBranch.district_code = district.district_code;
                            newBranch.district_name = bodyApiCreateEtax.seller.district_name_th;
                            newBranch.district_name_en = bodyApiCreateEtax.seller.district_name_en;
                            newBranch.amphoe_code = amphoe.amphoe_code;
                            newBranch.amphoe_name = bodyApiCreateEtax.seller.amphoe_name_th;
                            newBranch.amphoe_name_en = bodyApiCreateEtax.seller.amphoe_name_en;
                            newBranch.province_code = province.province_code;
                            newBranch.province_name = bodyApiCreateEtax.seller.province_name_th;
                            newBranch.province_name_en = bodyApiCreateEtax.seller.province_name_en;
                            newBranch.zipcode = bodyApiCreateEtax.seller.zipcode;
                            newBranch.update_date = now;
                            newBranch.create_date = now;
                            newBranch.delete_status = 0;

                            _context.Add(newBranch);
                            await _context.SaveChangesAsync();

                            branch_id = newBranch.id;
                        }
                        else if (bodyApiCreateEtax.seller.branch_code != "00000")
                        {
                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.branch_name_th))
                                return StatusCode(400, new { message = "กรุณาระบุชื่อสาขา", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.building_number))
                                return StatusCode(400, new { message = "กรุณาระบุบ้านเลขที่", });
                            if (bodyApiCreateEtax.seller.building_number.Length > 16)
                                return StatusCode(400, new { message = "กรุณาระบุบ้านเลขที่น้อยกว่า 16 หลัก", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.district_name_th))
                                return StatusCode(400, new { message = "กรุณาระบุตำบล/แขวง", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.amphoe_name_th))
                                return StatusCode(400, new { message = "กรุณาระบุอำเภอ/เขต", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.province_name_th))
                                return StatusCode(400, new { message = "กรุณาระบุจังหวัด", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.zipcode))
                                return StatusCode(400, new { message = "กรุณาระบุรหัสไปรษณีย์", });

                            Province province = new Province();
                            Amphoe amphoe = new Amphoe();
                            District district = new District();

                            bodyApiCreateEtax.seller.province_name_th = bodyApiCreateEtax.seller.province_name_th.Replace("จังหวัด", "").Replace("จ.", "").Trim();
                            bodyApiCreateEtax.seller.amphoe_name_th = bodyApiCreateEtax.seller.amphoe_name_th.Replace("เขต", "").Replace("อำเภอ", "").Replace("อ.", "").Trim();
                            bodyApiCreateEtax.seller.district_name_th = bodyApiCreateEtax.seller.district_name_th.Replace("แขวง", "").Replace("ตำบล", "").Replace("ต.", "").Trim();

                            List<Province> provinces = _context.province.Where(x => x.province_th.Contains(bodyApiCreateEtax.seller.province_name_th.Trim())).ToList();
                            if (provinces.Count == 1)
                                province = provinces.First();
                            else
                                return StatusCode(400, new { message = "ชื่อจังหวัดไม่ถูกต้อง", });



                            List<Amphoe> amphoes = _context.amphoe.Where(x => x.province_code == province.province_code && x.amphoe_th.Contains(bodyApiCreateEtax.seller.amphoe_name_th.Trim())).ToList();
                            if (amphoes.Count > 1)
                            {
                                foreach (Amphoe a in amphoes)
                                {
                                    string a1 = a.amphoe_th.Replace("เขต", "").Replace("อำเภอ", "");
                                    string a2 = bodyApiCreateEtax.seller.amphoe_name_th.Replace("เขต", "").Replace("อำเภอ", "");

                                    if (a1 == a2)
                                    {
                                        amphoe = a;
                                        break;
                                    }
                                }
                            }
                            else if (amphoes.Count == 1)
                                amphoe = amphoes.First();
                            else if (amphoes.Count == 0)
                                return StatusCode(400, new { message = "ชื่ออำเภอ/เขตไม่ถูกต้อง", });
                            if (amphoe.amphoe_code == 0)
                                return StatusCode(400, new { message = "ชื่ออำเภอ/เขตไม่ถูกต้อง", });


                            List<District> districts = _context.district.Where(x => x.amphoe_code == amphoe.amphoe_code && x.district_th.Contains(bodyApiCreateEtax.seller.district_name_th)).ToList();
                            if (districts.Count > 1)
                            {
                                foreach (District d in districts)
                                {
                                    string d1 = d.district_th.Replace("แขวง", "").Replace("ตำบล", "");
                                    string d2 = bodyApiCreateEtax.seller.district_name_th.Replace("แขวง", "").Replace("ตำบล", "");

                                    if (d1 == d2)
                                    {
                                        district = d;
                                        break;
                                    }
                                }


                            }
                            else if (districts.Count == 1)
                                district = districts.First();
                            else if (districts.Count == 0)
                                return StatusCode(400, new { message = "ชื่อตำบล/แขวงไม่ถูกต้อง", });
                            if (district.district_code == 0)
                                return StatusCode(400, new { message = "ชื่อตำบล/แขวงไม่ถูกต้อง", });

                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.zipcode))
                                bodyApiCreateEtax.seller.zipcode = districts.First().zipcode;
                            else
                                if (bodyApiCreateEtax.seller.zipcode != districts.First().zipcode)
                                return StatusCode(400, new { message = "รหัสไปรษณีย์ไม่ตรงกับที่อยู่", });


                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.province_name_en))
                                bodyApiCreateEtax.seller.province_name_en = province.province_en;
                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.amphoe_name_en))
                                bodyApiCreateEtax.seller.amphoe_name_en = amphoe.amphoe_en_s;
                            if (string.IsNullOrEmpty(bodyApiCreateEtax.seller.district_name_en))
                                bodyApiCreateEtax.seller.district_name_en = district.district_en_s;

                            if (bodyApiCreateEtax.seller.building_name_th.Length > 70)
                                bodyApiCreateEtax.seller.building_name_th = bodyApiCreateEtax.seller.building_name_th.Substring(0, 70);

                            branch.member_id = jwtStatus.member_id;
                            branch.name = bodyApiCreateEtax.seller.branch_name_th.Trim();
                            branch.name_en = bodyApiCreateEtax.seller.branch_name_en.Trim();
                            branch.branch_code = bodyApiCreateEtax.seller.branch_code.Trim();
                            branch.building_number = bodyApiCreateEtax.seller.building_number.Trim();
                            branch.building_name = bodyApiCreateEtax.seller.building_name_th.Trim();
                            branch.building_name_en = bodyApiCreateEtax.seller.building_name_en.Trim();
                            branch.street_name = bodyApiCreateEtax.seller.street_name_th.Trim();
                            branch.street_name_en = bodyApiCreateEtax.seller.street_name_en.Trim();
                            branch.district_code = district.district_code;
                            branch.district_name = bodyApiCreateEtax.seller.district_name_th;
                            branch.district_name_en = bodyApiCreateEtax.seller.district_name_en;
                            branch.amphoe_code = amphoe.amphoe_code;
                            branch.amphoe_name = bodyApiCreateEtax.seller.amphoe_name_th;
                            branch.amphoe_name_en = bodyApiCreateEtax.seller.amphoe_name_en;
                            branch.province_code = province.province_code;
                            branch.province_name = bodyApiCreateEtax.seller.province_name_th;
                            branch.province_name_en = bodyApiCreateEtax.seller.province_name_en;
                            branch.zipcode = bodyApiCreateEtax.seller.zipcode;
                            branch.update_date = now;
                            branch.create_date = now;
                            branch.delete_status = 0;
                            await _context.SaveChangesAsync();

                            branch_id = branch.id;

                        }
                        else
                            branch_id = branch.id;


                        string mode = "normal";
                        string form_code = "";
                        string form_url = "";
                        if (bodyApiCreateEtax.gen_form_status)
                        {
                            mode = "form";
                            form_code = jwtStatus.member_id + Encryption.SHA256(now.ToString("yyyyMMddHHmmssfff")).Substring(0, 6).ToUpper() + now.ToString("dd");
                            form_url = "http://localhost:4200/#/userform?code=" + form_code;
                        }

                        EtaxFile etaxFile = new EtaxFile();
                        etaxFile.member_id = jwtStatus.member_id;
                        etaxFile.branch_id = branch_id;
                        etaxFile.member_user_id = jwtStatus.user_id;
                        etaxFile.document_type_id = int.Parse(bodyApiCreateEtax.document_type_code);
                        etaxFile.create_type = "api";
                        etaxFile.gen_xml_status = "pending";
                        etaxFile.gen_pdf_status = (bodyApiCreateEtax.gen_pdf_status == true) ? "pending" : "no";
                        etaxFile.add_email_status = (bodyApiCreateEtax.send_email_status == true) ? "pending" : "no";
                        etaxFile.add_sms_status = (bodyApiCreateEtax.send_sms_status == true) ? "pending" : "no";
                        etaxFile.add_ebxml_status = (bodyApiCreateEtax.send_ebxml_status == true) ? "pending" : "no";
                        etaxFile.send_xml_status = "N";
                        etaxFile.send_other_status = "N";
                        etaxFile.error = "";
                        etaxFile.output_path = _config["Path:Output"];
                        etaxFile.etax_id = bodyApiCreateEtax.etax_id;
                        etaxFile.buyer_branch_code = bodyApiCreateEtax.buyer.branch_code;
                        etaxFile.issue_date = DateTime.ParseExact(bodyApiCreateEtax.issue_date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                        etaxFile.buyer_id = bodyApiCreateEtax.buyer.id;
                        etaxFile.buyer_name = bodyApiCreateEtax.buyer.name;
                        etaxFile.buyer_tax_id = bodyApiCreateEtax.buyer.tax_id;
                        etaxFile.buyer_address = (bodyApiCreateEtax.buyer.address.Contains(bodyApiCreateEtax.buyer.zipcode)) ? bodyApiCreateEtax.buyer.address : bodyApiCreateEtax.buyer.address + " " + bodyApiCreateEtax.buyer.zipcode;
                        etaxFile.buyer_tel = bodyApiCreateEtax.buyer.tel;
                        etaxFile.buyer_fax = bodyApiCreateEtax.buyer.fax;
                        etaxFile.buyer_country_code = bodyApiCreateEtax.buyer.country_code;
                        etaxFile.buyer_email = bodyApiCreateEtax.buyer.email;
                        etaxFile.price = bodyApiCreateEtax.price;
                        etaxFile.discount = bodyApiCreateEtax.discount;
                        etaxFile.tax = bodyApiCreateEtax.tax;
                        etaxFile.total = bodyApiCreateEtax.total;
                        etaxFile.remark = bodyApiCreateEtax.remark;
                        etaxFile.other = bodyApiCreateEtax.other;
                        etaxFile.template_pdf = (bodyApiCreateEtax.template_pdf == null) ? "" : bodyApiCreateEtax.template_pdf;
                        etaxFile.template_email = (bodyApiCreateEtax.template_email == null) ? "" : bodyApiCreateEtax.template_email;
                        etaxFile.mode = mode;
                        etaxFile.xml_payment_status = "pending";
                        etaxFile.pdf_payment_status = "pending";
                        etaxFile.create_date = now;
                        etaxFile.form_code = form_code;

                        if (bodyApiCreateEtax.document_type_code == "2" || bodyApiCreateEtax.document_type_code == "3")
                        {
                            etaxFile.ref_etax_id = bodyApiCreateEtax.ref_etax_id;
                            etaxFile.ref_issue_date = DateTime.ParseExact(bodyApiCreateEtax.ref_issue_date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                            etaxFile.original_price = bodyApiCreateEtax.original_price;
                        }

                        _context.Add(etaxFile);
                        await _context.SaveChangesAsync();

                        foreach (ItemEtax item in bodyApiCreateEtax.items)
                        {
                            _context.Add(new EtaxFileItem()
                            {
                                etax_file_id = etaxFile.id,
                                code = item.code,
                                name = item.name,
                                qty = (double)item.qty,
                                unit = item.unit,
                                price = (double)item.price,
                                discount = (double)item.discount,
                                tax = (double)item.tax,
                                total = (double)item.total,
                                tax_type = item.tax_type,
                                tax_rate = (int)item.tax_rate,
                                other = item.other,
                            });
                        }
                        await _context.SaveChangesAsync();

                        LogEtaxFile logEtaxFile = new LogEtaxFile()
                        {
                            member_id = jwtStatus.member_id,
                            user_modify_id = jwtStatus.user_id,
                            etax_id = etaxFile.id,
                            create_type = "api",
                            action_type = "create",
                            create_date = now,
                        };
                        _context.Add(logEtaxFile);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        if (bodyApiCreateEtax.gen_form_status)
                        {
                            return StatusCode(200, new
                            {
                                data = new
                                {
                                    etax_id = etaxFile.etax_id,
                                    form_code = form_code,
                                    form_url = form_url,
                                },
                                message = "อัพโหลดไฟล์ข้อมูลสำเร็จ",
                            });
                        }
                        else
                        {
                            return StatusCode(200, new
                            {
                                data = new
                                {
                                    etax_id = etaxFile.etax_id,
                                },
                                message = "อัพโหลดไฟล์ข้อมูลสำเร็จ",
                            });
                        }
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
        [Route("ppmt_etax_list")]
        public async Task<IActionResult> ApiGetEtaxList()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var listEtaxFile = _context.etax_files
               .Where(x => x.member_id == jwtStatus.member_id && x.delete_status == 0)
               .Select(x => new
               {
                   x.etax_id,
                   x.gen_xml_status,
                   x.gen_pdf_status,
                   send_email_status = x.add_email_status,
                   send_ebxml_status = x.add_ebxml_status,

               })
               .ToList();

                return StatusCode(200, new
                {
                    data = new
                    {
                        listEtax = listEtaxFile,
                    },
                    message = "เรียกดูข้อมูลสำเร็จ",
                });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("ppmt_etax_detail/{id}")]
        public async Task<IActionResult> ApiGetEtaxDetail(string id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var etaxFile = _context.etax_files
               .Where(x => x.etax_id == id && x.member_id == jwtStatus.member_id && x.delete_status == 0)
               .FirstOrDefault();

                if (etaxFile == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลในระบบ", });

                var branch = _context.branchs
               .Where(x => x.id == etaxFile.branch_id)
               .FirstOrDefault();

                var documentType = _context.document_type
               .Where(x => x.id == etaxFile.document_type_id)
               .FirstOrDefault();

                var listEtaxFileItems = _context.etax_file_items
               .Where(x => x.etax_file_id == etaxFile.id)
               .Select(x => new
               {
                   code = x.code,
                   name = x.name,
                   qty = x.qty,
                   unit = x.unit,
                   price = x.price,
                   discount = x.discount,
                   tax = x.tax,
                   total = x.total,
               })
               .ToList();

                return StatusCode(200, new
                {
                    data = new
                    {
                        document_type_code = etaxFile.document_type_id,
                        document_type_name = documentType.name,
                        seller = new
                        {
                            branch_code = branch.branch_code,
                            name = branch.name,
                            building_number = branch.building_number,
                            building_name = branch.building_name,
                            street_name = branch.street_name,
                            district_name = branch.district_name,
                            amphoe_name = branch.amphoe_name,
                            province_name = branch.province_name,
                            zipcode = branch.zipcode,
                        },
                        buyer = new
                        {
                            branch_code = etaxFile.buyer_branch_code,
                            id = etaxFile.buyer_id,
                            name = etaxFile.buyer_name,
                            tax_id = etaxFile.buyer_tax_id,
                            address = etaxFile.buyer_address,
                            tel = etaxFile.buyer_tel,
                            fax = etaxFile.buyer_fax,
                            email = etaxFile.buyer_email,
                        },
                        gen_pdf_status = etaxFile.gen_pdf_status,
                        send_email_status = etaxFile.add_email_status,
                        send_ebxml_status = etaxFile.add_ebxml_status,
                        etax_id = etaxFile.etax_id,
                        issue_date = etaxFile.issue_date,
                        price = etaxFile.price,
                        discount = etaxFile.discount,
                        tax = etaxFile.tax,
                        total = etaxFile.total,
                        other = etaxFile.other,
                        items = listEtaxFileItems,

                    },
                    message = "เรียกดูข้อมูลสำเร็จ",
                });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("ppmt_delete_etax/{id}")]
        public async Task<IActionResult> ApiDeleteEtax(string id)
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
                        List<EtaxFile> listEtaxFile = await _context.etax_files
                        .Where(x => x.etax_id == id && x.member_id == jwtStatus.member_id)
                        .ToListAsync();

                        if (listEtaxFile.Count == 0)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลในระบบ", });

                        foreach (EtaxFile etax in listEtaxFile)
                        {
                            if (etax.add_ebxml_status == "success")
                                return StatusCode(400, new { message = "ข้อมูลถูกส่งสรรพากรแล้ว ไม่สามารถลบได้", });

                            etax.delete_status = 1;
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
        [Route("get_etax")]
        public async Task<IActionResult> GetEtax([FromBody] BodyApiGetEtax bodyApiGetEtax)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var etax_file = _context.etax_files
                       .Where(x => x.member_id == jwtStatus.member_id && x.etax_id == bodyApiGetEtax.etax_id && x.delete_status == 0)
                       .FirstOrDefault();

                if (etax_file != null)
                {
                    DateTime now = DateTime.Now;
                    string file_name_xml = "/" + etax_file.member_id + "/" + Encryption.SHA256("path_" + now.ToString("dd-MM-yyyy")) + "/xml/" + etax_file.name + ".xml";
                    string file_name_pdf = "/" + etax_file.member_id + "/" + Encryption.SHA256("path_" + now.ToString("dd-MM-yyyy")) + "/pdf/" + etax_file.name + ".pdf";

                    Function.DeleteFile(_config["Path:Share"]);
                    Function.DeleteDirectory(_config["Path:Share"]);

                    if (etax_file.gen_xml_status == "success")
                    {
                        string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etax_file.url_path + "/xml/" + etax_file.name + ".xml", _config["Path:Mode"]);
                        if (fileBase64 != "")
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(_config["Path:Share"] + file_name_xml));
                            System.IO.File.WriteAllBytes(_config["Path:Share"] + file_name_xml, Convert.FromBase64String(fileBase64));
                        }
                    }

                    if (etax_file.gen_pdf_status == "success")
                    {
                        string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etax_file.url_path + "/pdf/" + etax_file.name + ".pdf", _config["Path:Mode"]);
                        if (fileBase64 != "")
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(_config["Path:Share"] + file_name_pdf));
                            System.IO.File.WriteAllBytes(_config["Path:Share"] + file_name_pdf, Convert.FromBase64String(fileBase64));
                        }
                    }

                    RequestEtax request_etax = new RequestEtax()
                    {
                        member_id = jwtStatus.member_id,
                        member_user_id = jwtStatus.user_id,
                        etax_file_id = etax_file.id,
                        xml_path = file_name_xml,
                        pdf_path = file_name_pdf,
                        status = "request",
                        create_date = DateTime.Now,
                    };

                    _context.Add(request_etax);
                    await _context.SaveChangesAsync();

                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            xml_status = etax_file.gen_xml_status,
                            xml_path = _config["Path:Url"] + file_name_xml,
                            pdf_status = etax_file.gen_pdf_status,
                            pdf_path = _config["Path:Url"] + file_name_pdf,
                        }
                    });
                }
                else
                {
                    return StatusCode(400, new
                    {
                        message = "ไม่พบข้อมูลในระบบ",
                    });
                }

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("ppmt_etax_file/{id}")]
        public async Task<IActionResult> ApiGetEtaxFile(string id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var etaxFile = _context.etax_files
               .Where(x => x.etax_id == id && x.member_id == jwtStatus.member_id && x.delete_status == 0)
               .FirstOrDefault();

                if (etaxFile == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลในระบบ", });

                DateTime now = DateTime.Now;
                string file_name_xml = "/" + etaxFile.member_id + "/" + Encryption.SHA256("path_" + now.ToString("dd-MM-yyyy")) + "/xml/" + etaxFile.name + ".xml";
                string file_name_pdf = "/" + etaxFile.member_id + "/" + Encryption.SHA256("path_" + now.ToString("dd-MM-yyyy")) + "/pdf/" + etaxFile.name + ".pdf";

                Function.DeleteFile(_config["Path:Share"]);
                Function.DeleteDirectory(_config["Path:Share"]);

                if (etaxFile.gen_xml_status == "success")
                {
                    string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etaxFile.url_path + "/xml/" + etaxFile.name + ".xml", _config["Path:Mode"]);
                    if (fileBase64 != "")
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(_config["Path:Share"] + file_name_xml));
                        System.IO.File.WriteAllBytes(_config["Path:Share"] + file_name_xml, Convert.FromBase64String(fileBase64));
                    }
                }
                if (etaxFile.gen_pdf_status == "success")
                {
                    string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etaxFile.url_path + "/pdf/" + etaxFile.name + ".pdf", _config["Path:Mode"]);
                    if (fileBase64 != "")
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(_config["Path:Share"] + file_name_pdf));
                        System.IO.File.WriteAllBytes(_config["Path:Share"] + file_name_pdf, Convert.FromBase64String(fileBase64));
                    }
                }

                return StatusCode(200, new
                {
                    data = new
                    {
                        etax_id = etaxFile.etax_id,
                        gen_pdf_status = etaxFile.gen_pdf_status,
                        file_xml = _config["Path:Url"] + file_name_xml,
                        file_pdf = _config["Path:Url"] + file_name_pdf,
                    },
                    message = "เรียกดูข้อมูลสำเร็จ",
                });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("ppmt_update_process")]
        public async Task<IActionResult> ApiUpdateProcess([FromBody] BodyApiProcess bodyProcess)
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
                        .Where(x => x.etax_id == bodyProcess.etax_id && x.delete_status == 0)
                        .FirstOrDefaultAsync();


                        if (etaxFile == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลในระบบ", });

                        etaxFile.send_xml_status = (bodyProcess.send_xml_status) ? "Y" : "N";

                        if (etaxFile.gen_pdf_status == "no")
                            etaxFile.gen_pdf_status = (bodyProcess.gen_pdf_status) ? "pending" : "no";

                        if (etaxFile.add_email_status == "no")
                            etaxFile.add_email_status = (bodyProcess.send_email_status) ? "pending" : "no";

                        if (etaxFile.add_ebxml_status == "no")
                            etaxFile.add_ebxml_status = (bodyProcess.send_ebxml_status) ? "pending" : "no";

                        etaxFile.buyer_email = bodyProcess.buyer_email;
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
        [Route("ppmt_sendemail_detail/{id}")]
        public async Task<IActionResult> ApiSendemailDetail(string id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var etaxFile = _context.etax_files
               .Where(x => x.etax_id == id && x.member_id == jwtStatus.member_id && x.delete_status == 0)
               .FirstOrDefault();

                if (etaxFile == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลในระบบ", });

                if (etaxFile.add_email_status == "no")
                    return StatusCode(400, new { message = "ไม่มีการส่ง email", });

                var send_email = _context.send_email
               .Where(x => x.etax_file_id == etaxFile.id)
               .FirstOrDefault();

                if (send_email == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลในระบบ", });

                var response_email = _context.response_email
               .Where(x => x.send_email_id == send_email.id)
               .Select(x => new
               {
                   email = x.email,
                   status = x.email_status,
               })
               .ToList();

                if (send_email == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลในระบบ", });

                return StatusCode(200, new
                {
                    data = new
                    {
                        etax_id = etaxFile.etax_id,
                        send_status = send_email.send_email_status,
                        send_date = (send_email.send_email_finish != null) ? ((DateTime)send_email.send_email_finish).ToString("dd/MM/yyyy HH:mm:ss") : "",
                        receive_status = response_email,

                    },
                    message = "เรียกดูข้อมูลสำเร็จ",
                });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("ppmt_sendebxml_detail/{id}")]
        public async Task<IActionResult> ApiSendRdDetail(string id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var etaxFile = _context.etax_files
               .Where(x => x.etax_id == id && x.member_id == jwtStatus.member_id && x.delete_status == 0)
               .FirstOrDefault();

                if (etaxFile == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลในระบบ", });

                if (etaxFile.add_ebxml_status == "no")
                    return StatusCode(400, new { message = "ไม่มีการส่งสรรพากร", });

                var send_ebxml = _context.send_ebxml
               .Where(x => x.etax_file_id == etaxFile.id)
               .FirstOrDefault();

                if (send_ebxml == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลในระบบ", });


                return StatusCode(200, new
                {
                    data = new
                    {
                        etax_id = etaxFile.etax_id,
                        send_status = send_ebxml.send_ebxml_status,
                        send_date = send_ebxml.send_ebxml_finish.ToString("dd/MM/yyyy HH:mm:ss"),
                        receive_status = send_ebxml.etax_status,


                    },
                    message = "เรียกดูข้อมูลสำเร็จ",
                });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

    }
}
