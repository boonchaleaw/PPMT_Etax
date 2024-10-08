
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class ApiTripetchController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private Regex rxZipCode = new Regex(@"[0-9]{5}");
        private string key_test = "Bearer C8WP9qVA28y5RtUheAdyv7liBucYFFMHp3MMXbh82yabHbtDs9dd38mYGSgRKAOQOft9DtZGeKJP6u7MvbRmHeyLi2Xzh4RW0axdJW4JSclGuBXRPhedtZXpx30t7hpptfKMoVJ3iTcjZDKJuOYAiGtTm8MXyCKiSHKfoqxda0AQurUvzydertygJ8iECJw2B0KY8GW60GyQse2IE9zWa9tx5Zk03j4wdBowkcdH7uM1zQvK3ZkchskyP2gpsxak";
        private string key_pro = "Bearer RRYCwvL3s4KTWI4G2sVhaouD1ce4V2A2q42oIb5bjVRtluco6qZSTkFSPBZ34xYsRASW4aiDH0bB7298pf3MZMmZY8TFgwfLfdSpjEBvkH4JD0VKwimRg2xQDxyzmXWvLwVrmX9udHxWUcQyI1js0FBw286j0xQv4lvXeRyeG0e0GtT0iFR8bXSoqhBOMgezbiM9XdjpkE0LWrx7P1OuVY03Br6H7AmmCFkaT7hU16doI8dHpXBz26RioyLuehYW";
        public ApiTripetchController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        [HttpPost]
        [Route("tripetch/create_etax")]
        public async Task<IActionResult> Tp_ApiCreateEtax([FromBody] BodyApiTripetchCreateEtax bodyApiCreateEtax)
        {
            try
            {
                int user_id = 0;
                DateTime now = DateTime.Now;
                string token = Request.Headers[HeaderNames.Authorization].ToString();

                if (_config["Path:Mode"] == "test")
                {
                    if (token != key_test)
                        return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้อง", });
                }
                else
                {
                    if (token != key_pro)
                        return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้อง", });
                }

                try
                {
                    if (!Log.CheckLogTis(bodyApiCreateEtax.etax_id, now))
                    {
                        return StatusCode(400, new { error_code = "1006", message = "มีข้อมูลซ้ำเข้ามาในเวลาเดียวกัน", });
                    }
                }
                catch { }

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



                if (String.IsNullOrEmpty(bodyApiCreateEtax.buyer.name))
                    return StatusCode(400, new { error_code = "2006", message = "กรุณากำหนดชื่อผู้ซื้อ", });

                //if (String.IsNullOrEmpty(bodyApiCreateEtax.buyer.tax_id))
                //    return StatusCode(400, new { error_code = "2007", message = "กรุณากำหนดเลขประจําตัวผู้เสียภาษี", });

                if (String.IsNullOrEmpty(bodyApiCreateEtax.buyer.tax_id))
                    bodyApiCreateEtax.buyer.tax_id = "";

                if (String.IsNullOrEmpty(bodyApiCreateEtax.buyer.address))
                    return StatusCode(400, new { error_code = "2008", message = "กรุณากำหนดที่อยู่", });

                if (bodyApiCreateEtax.buyer.branch_code.Length != 5)
                    bodyApiCreateEtax.buyer.branch_code = "00000";

                ////if (bodyApiCreateEtax.buyer.branch_code.Length != 5)
                ////    return StatusCode(400, new { error_code = "2009", message = "กรุณากำหนดรหัสสาขา 5 หลัก", });

                if (String.IsNullOrEmpty(bodyApiCreateEtax.buyer.zipcode) || bodyApiCreateEtax.buyer.zipcode.Length != 5)
                    return StatusCode(400, new { error_code = "2010", message = "กรุณากำหนดรหัสไปรษณีย์", });


                if (bodyApiCreateEtax.items.Count() <= 0)
                    return StatusCode(400, new { error_code = "2026", message = "กรุณากำหนดรายการสินค้า", });

                int itemLine = 1;
                foreach (ItemEtax item in bodyApiCreateEtax.items)
                {
                    //if (String.IsNullOrEmpty(item.code))
                    //    return StatusCode(400, new { error_code = "2011", message = "กรุณากำหนดรหัสสินค้า รายการสินค้าที่ " + itemLine, });

                    if (String.IsNullOrEmpty(item.name))
                        return StatusCode(400, new { error_code = "2012", message = "กรุณากำหนดชื่อสินค้า รายการสินค้าที่ " + itemLine, });

                    if (String.IsNullOrEmpty(item.price.ToString()))
                        return StatusCode(400, new { error_code = "2013", message = "กรุณากำหนดจำนวนเงิน รายการสินค้าที่ " + itemLine, });

                    if (String.IsNullOrEmpty(item.total.ToString()))
                        return StatusCode(400, new { error_code = "2014", message = "กรุณากำหนดภาษี รายการสินค้าที่ " + itemLine, });

                    itemLine++;
                }

                var member = await (from m in _context.members
                                    where m.tax_id == bodyApiCreateEtax.seller.tax_id
                                    && m.group_name == "Isuzu"
                                    select m).FirstOrDefaultAsync();

                if (member == null)
                    return StatusCode(400, new { error_code = "1008", message = "ไม่พบผู้ขายที่ต้องการ" });

                var view_member_document_type = _context.view_member_document_type
                    .Where(x => x.member_id == member.id && x.document_type_id == int.Parse(bodyApiCreateEtax.document_type_code))
                    .FirstOrDefault();

                if (view_member_document_type == null)
                    return StatusCode(400, new { error_code = "1002", message = "ลูกค้าไม่สามารถสร้างเอกสารประเภทนี้ได้" });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        int branch_id = 0;
                        Branch branch = _context.branchs
                        .Where(x => x.member_id == member.id && x.branch_code == bodyApiCreateEtax.seller.branch_code)
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
                            newBranch.member_id = member.id;
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
                        else
                            branch_id = branch.id;


                        var etax_file = _context.etax_files
                       .Where(x => x.member_id == member.id && x.branch_id == branch_id && x.etax_id == bodyApiCreateEtax.etax_id && x.delete_status == 0)
                       .FirstOrDefault();

                        if (etax_file != null)
                            return StatusCode(400, new { error_code = "1003", message = "ข้อมูลซ้ำในระบบ", });

                        string gen_xml_status = "pending";
                        string gen_pdf_status = "no";
                        string send_email_status = "no";
                        string send_sms_status = "no";
                        string send_ebxml_status = "no";

                        if (bodyApiCreateEtax.pdf_service == "S0")
                        {
                            gen_pdf_status = "pending";
                            if (String.IsNullOrEmpty(bodyApiCreateEtax.pdf_base64))
                                return StatusCode(400, new { error_code = "3001", message = "ไม่พบข้อมูลไฟล์ PDF", });
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

                        string url = "/" + member.id + "/" + now.ToString("yyyyMM") + "/" + now.ToString("dd");
                        string file_path = url + "/" + bodyApiCreateEtax.etax_id + ".pdf";
                        string output = _config["Path:Output"];

                        if (ApiFileTransfer.UploadFile(_config["Path:FileTransfer"], file_path, bodyApiCreateEtax.pdf_base64, _config["Path:Mode"]))
                        {

                            EtaxFile etaxFile = new EtaxFile();
                            etaxFile.member_id = member.id;
                            etaxFile.branch_id = branch_id;
                            etaxFile.member_user_id = user_id;
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
                            etaxFile.output_path = output;
                            etaxFile.url_path = url;
                            etaxFile.etax_id = bodyApiCreateEtax.etax_id;
                            etaxFile.buyer_branch_code = bodyApiCreateEtax.buyer.branch_code;
                            etaxFile.issue_date = DateTime.ParseExact(bodyApiCreateEtax.issue_date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                            etaxFile.buyer_id = bodyApiCreateEtax.buyer.id;
                            etaxFile.buyer_name = bodyApiCreateEtax.buyer.name;
                            etaxFile.buyer_tax_id = bodyApiCreateEtax.buyer.tax_id;
                            etaxFile.buyer_tax_type = bodyApiCreateEtax.buyer.customer_tax_type;
                            etaxFile.buyer_address = bodyApiCreateEtax.buyer.address;
                            etaxFile.buyer_zipcode = bodyApiCreateEtax.buyer.zipcode;
                            etaxFile.buyer_tel = bodyApiCreateEtax.buyer.tel;
                            etaxFile.buyer_fax = bodyApiCreateEtax.buyer.fax;
                            etaxFile.buyer_country_code = (bodyApiCreateEtax.buyer.country_code == null) ? "" : bodyApiCreateEtax.buyer.country_code;
                            etaxFile.buyer_email = bodyApiCreateEtax.buyer.email;
                            etaxFile.price = bodyApiCreateEtax.price;
                            etaxFile.discount = bodyApiCreateEtax.discount;
                            etaxFile.tax_rate = (int)bodyApiCreateEtax.tax_rate;
                            etaxFile.tax = bodyApiCreateEtax.tax;
                            etaxFile.total = bodyApiCreateEtax.total;
                            etaxFile.remark = bodyApiCreateEtax.remark;
                            etaxFile.other = bodyApiCreateEtax.myisuzu_service + "|" + bodyApiCreateEtax.correct_tax_invoice_amount + "|" + bodyApiCreateEtax.other;
                            etaxFile.group_name = (bodyApiCreateEtax.source_type == null) ? "" : bodyApiCreateEtax.source_type;
                            etaxFile.xml_payment_status = "pending";
                            etaxFile.pdf_payment_status = "pending";
                            etaxFile.password = "";
                            etaxFile.create_date = now;
                            etaxFile.mode = "normal";

                            if (bodyApiCreateEtax.myisuzu_service == "S1")
                            {
                                etaxFile.webhook = 1;
                            }

                            if (bodyApiCreateEtax.document_type_code == "2")
                            {
                                var document_type = await (from dt in _context.document_type
                                                           where dt.id == bodyApiCreateEtax.ref_document_type_code
                                                           select dt).FirstOrDefaultAsync();

                                if (document_type == null)
                                    return StatusCode(400, new { error_code = "1009", message = "ไม่พบเลขที่อ้างอิงเอกสารที่ต้องการ", });

                                etaxFile.ref_etax_id = bodyApiCreateEtax.ref_etax_id;
                                etaxFile.ref_issue_date = DateTime.ParseExact(bodyApiCreateEtax.ref_issue_date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                                etaxFile.ref_document_type = document_type.rd_type;
                                etaxFile.original_price = bodyApiCreateEtax.original_price;
                                etaxFile.new_price = bodyApiCreateEtax.original_price - bodyApiCreateEtax.price;
                            }
                            else if (bodyApiCreateEtax.document_type_code == "3")
                            {
                                var document_type = await (from dt in _context.document_type
                                                           where dt.id == bodyApiCreateEtax.ref_document_type_code
                                                           select dt).FirstOrDefaultAsync();

                                if (document_type == null)
                                    return StatusCode(400, new { error_code = "1009", message = "ไม่พบเลขที่อ้างอิงเอกสารที่ต้องการ", });

                                etaxFile.ref_etax_id = bodyApiCreateEtax.ref_etax_id;
                                etaxFile.ref_issue_date = DateTime.ParseExact(bodyApiCreateEtax.ref_issue_date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                                etaxFile.ref_document_type = document_type.rd_type;
                                etaxFile.original_price = bodyApiCreateEtax.original_price;
                                etaxFile.new_price = bodyApiCreateEtax.price - bodyApiCreateEtax.original_price;
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
                                    tax_rate = (int)item.tax_rate,
                                    tax = (double)item.tax,
                                    total = (double)item.total,
                                    tax_type = item.tax_type,
                                    other = item.other,
                                });
                            }
                            await _context.SaveChangesAsync();

                            LogEtaxFile logEtaxFile = new LogEtaxFile()
                            {
                                member_id = member.id,
                                user_modify_id = user_id,
                                etax_id = etaxFile.id,
                                create_type = "api",
                                action_type = "create",
                                create_date = now,
                            };
                            _context.Add(logEtaxFile);
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
                        else
                        {
                            return StatusCode(400, new { error_code = "3002", error_message = "อัพโหลดไฟล์ PDF ไม่สำเร็จ" });
                        }
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
        [Route("tripetch/get_status/{id}")]
        public async Task<IActionResult> Tp_ApiGetStatus(string id)
        {
            try
            {
                DateTime now = DateTime.Now;
                string token = Request.Headers[HeaderNames.Authorization].ToString();

                if (_config["Path:Mode"] == "test")
                {
                    if (token != key_test)
                        return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้อง", });
                }
                else
                {
                    if (token != key_pro)
                        return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้อง", });
                }

                var membersId = await (from m in _context.members
                                       where m.group_name == "Isuzu"
                                       select m.id).ToListAsync();

                var etaxFile = _context.etax_files
               .Where(x => x.etax_id == id && membersId.Contains(x.member_id) && x.delete_status == 0)
               .FirstOrDefault();

                if (etaxFile == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลในระบบ", });

                string gen_xml_status = etaxFile.gen_xml_status;
                string gen_xml_error = "";

                string gen_pdf_status = etaxFile.gen_pdf_status;
                string gen_pdf_error = "";

                string send_email_status = etaxFile.add_email_status;
                string send_email_error = "";

                string send_rd_status = etaxFile.add_ebxml_status;
                string send_re_error = "";



                //string file_name_xml = "/" + etaxFile.member_id + "/" + Encryption.SHA256("path_" + now.ToString("dd-MM-yyyy")) + "/xml/" + etaxFile.name + ".xml";
                //string file_name_pdf = "/" + etaxFile.member_id + "/" + Encryption.SHA256("path_" + now.ToString("dd-MM-yyyy")) + "/pdf/" + etaxFile.name + ".pdf";

                //Function.DeleteFile(_config["Path:Share"]);
                //Function.DeleteDirectory(_config["Path:Share"]);

                //if (gen_xml_status == "success")
                //{
                //    string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etaxFile.url_path + "/xml/" + etaxFile.name + ".xml", _config["Path:Mode"]);
                //    if (fileBase64 != "")
                //    {
                //        Directory.CreateDirectory(Path.GetDirectoryName(_config["Path:Share"] + file_name_xml));
                //        System.IO.File.WriteAllBytes(_config["Path:Share"] + file_name_xml, Convert.FromBase64String(fileBase64));
                //    }
                //}
                //else
                if (gen_xml_status == "fail")
                {
                    gen_xml_error = etaxFile.error;
                }

                //if (gen_pdf_status == "success")
                //{
                //    string fileBase64 = ApiFileTransfer.DownloadFile(_config["Path:FileTransfer"], etaxFile.url_path + "/pdf/" + etaxFile.name + ".pdf", _config["Path:Mode"]);
                //    if (fileBase64 != "")
                //    {
                //        Directory.CreateDirectory(Path.GetDirectoryName(_config["Path:Share"] + file_name_pdf));
                //        System.IO.File.WriteAllBytes(_config["Path:Share"] + file_name_pdf, Convert.FromBase64String(fileBase64));
                //    }
                //}
                //else
                if (gen_pdf_status == "fail")
                {
                    gen_pdf_error = etaxFile.error;
                }

                if (send_email_status == "success")
                {
                    var sendEmail = _context.send_email
                     .Where(x => x.etax_file_id == etaxFile.id)
                     .FirstOrDefault();

                    if (sendEmail != null)
                    {
                        if (sendEmail.send_email_status == "fail")
                        {
                            send_email_status = sendEmail.send_email_status;
                            send_email_error = sendEmail.error;
                        }
                        else if (sendEmail.email_status == "fail")
                        {
                            send_email_status = sendEmail.email_status;
                            send_email_error = sendEmail.error;
                        }
                    }
                }

                if (send_rd_status == "success")
                {
                    var sendEbxml = _context.send_ebxml
                     .Where(x => x.etax_file_id == etaxFile.id)
                     .FirstOrDefault();

                    if (sendEbxml != null)
                    {
                        if (sendEbxml.send_ebxml_status == "fail")
                        {
                            send_rd_status = sendEbxml.send_ebxml_status;
                            send_re_error = sendEbxml.error;
                        }
                        else if (sendEbxml.etax_status == "fail")
                        {
                            send_rd_status = sendEbxml.etax_status;
                            send_re_error = sendEbxml.error;
                        }
                    }
                }

                return StatusCode(200, new
                {
                    data = new
                    {
                        etax_id = etaxFile.etax_id,
                        gen_xml_status = gen_xml_status,
                        gen_xml_error = gen_xml_error,
                        gen_pdf_status = gen_pdf_status,
                        gen_pdf_error = gen_pdf_error,
                        send_email_status = send_email_status,
                        send_email_error = send_email_error,
                        send_rd_status = send_rd_status,
                        send_re_error = send_re_error,
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
        [Route("tripetch/get_file/{id}")]
        public async Task<IActionResult> Tp_ApiGetFile(string id)
        {
            try
            {
                DateTime now = DateTime.Now;
                string token = Request.Headers[HeaderNames.Authorization].ToString();

                if (_config["Path:Mode"] == "test")
                {
                    if (token != key_test)
                        return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้อง", });
                }
                else
                {
                    if (token != key_pro)
                        return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้อง", });
                }

                var membersId = await (from m in _context.members
                                       where m.group_name == "Isuzu"
                                       select m.id).ToListAsync();

                var etaxFile = _context.etax_files
               .Where(x => x.etax_id == id && membersId.Contains(x.member_id) && x.delete_status == 0)
               .FirstOrDefault();

                if (etaxFile == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลในระบบ", });

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

                if (etaxFile.gen_pdf_status == "success")
                {
                    return StatusCode(200, new
                    {
                        data = new
                        {
                            etax_id = etaxFile.etax_id,
                            gen_xml_status = etaxFile.gen_xml_status,
                            gen_pdf_status = etaxFile.gen_pdf_status,
                            file_xml = _config["Path:Url"] + file_name_xml,
                            file_pdf = _config["Path:Url"] + file_name_pdf,
                        },
                        message = "เรียกดูข้อมูลสำเร็จ",
                    });
                }
                else if (etaxFile.gen_xml_status == "success")
                {
                    return StatusCode(200, new
                    {
                        data = new
                        {
                            etax_id = etaxFile.etax_id,
                            gen_xml_status = etaxFile.gen_xml_status,
                            gen_pdf_status = etaxFile.gen_pdf_status,
                            file_xml = _config["Path:Url"] + file_name_xml,
                            file_pdf = "",
                        },
                        message = "เรียกดูข้อมูลสำเร็จ",
                    });
                }
                else
                {
                    return StatusCode(200, new
                    {
                        data = new
                        {
                            etax_id = etaxFile.etax_id,
                            gen_xml_status = etaxFile.gen_xml_status,
                            gen_pdf_status = etaxFile.gen_pdf_status,
                            file_xml = "",
                            file_pdf = "",
                        },
                        message = "เรียกดูข้อมูลสำเร็จ",
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("tripetch/get_etax_detail/{id}")]
        public async Task<IActionResult> Tp_ApiGetEtaxDetail(string id)
        {
            try
            {
                DateTime now = DateTime.Now;
                string token = Request.Headers[HeaderNames.Authorization].ToString();

                if (_config["Path:Mode"] == "test")
                {
                    if (token != key_test)
                        return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้อง", });
                }
                else
                {
                    if (token != key_pro)
                        return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้อง", });
                }

                var membersId = await (from m in _context.members
                                       where m.group_name == "Isuzu"
                                       select m.id).ToListAsync();

                var etaxFile = _context.etax_files
               .Where(x => x.etax_id == id && membersId.Contains(x.member_id) && x.delete_status == 0)
               .FirstOrDefault();

                if (etaxFile == null)
                    return StatusCode(400, new { error_code = "1002", message = "ไม่พบข้อมูลในระบบ", });

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

                string[] otherArray = etaxFile.other.Split("|");

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
                            building_number = (branch.building_number != null) ? branch.building_number : "",
                            building_name = (branch.building_name != null) ? branch.building_name : "",
                            street_name = (branch.street_name != null) ? branch.street_name : "",
                            district_name = (branch.district_name != null) ? branch.district_name : "",
                            amphoe_name = (branch.amphoe_name != null) ? branch.amphoe_name : "",
                            province_name = (branch.province_name != null) ? branch.province_name : "",
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
                        etax_id = etaxFile.etax_id,
                        issue_date = etaxFile.issue_date.ToString("dd-MM-yyyy"),
                        ref_etax_id = (etaxFile.ref_etax_id != null) ? etaxFile.ref_etax_id : "",
                        ref_issue_date = (etaxFile.ref_issue_date != null) ? ((DateTime)etaxFile.ref_issue_date).ToString("dd-MM-yyyy") : "",
                        original_price = etaxFile.original_price,
                        price = etaxFile.price,
                        correct_tax_invoice_amount = otherArray[1],
                        discount = etaxFile.discount,
                        tax = etaxFile.tax,
                        total = etaxFile.total,
                        remark = etaxFile.remark,
                        other = otherArray[2] + "|" + otherArray[3],
                        items = listEtaxFileItems,
                        gen_xml_status = etaxFile.gen_xml_status,
                        gen_pdf_status = etaxFile.gen_pdf_status,
                        send_email_status = etaxFile.add_email_status,
                        send_rd_status = etaxFile.add_ebxml_status,
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
        [Route("tripetch/delete_etax/{id}")]
        public async Task<IActionResult> Tp_Tp_ApiDeleteEtax(string id)
        {
            try
            {
                DateTime now = DateTime.Now;
                string token = Request.Headers[HeaderNames.Authorization].ToString();

                if (_config["Path:Mode"] == "test")
                {
                    if (token != key_test)
                        return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้อง", });
                }
                else
                {
                    if (token != key_pro)
                        return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้อง", });
                }

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var membersId = await (from m in _context.members
                                               where m.group_name == "Isuzu"
                                               select m.id).ToListAsync();

                        List<EtaxFile> listEtaxFile = await _context.etax_files
                        .Where(x => x.etax_id == id && membersId.Contains(x.member_id))
                        .ToListAsync();

                        if (listEtaxFile.Count == 0)
                            return StatusCode(400, new { error_code = "1002", message = "ไม่พบข้อมูลในระบบ", });

                        foreach (EtaxFile etax in listEtaxFile)
                        {
                            if (etax.add_ebxml_status == "success")
                                return StatusCode(400, new { error_code = "1003", message = "ข้อมูลถูกส่งสรรพากรแล้ว ไม่สามารถลบได้", });

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
        [Route("tripetch/sendemail_detail/{id}")]
        public async Task<IActionResult> ApiSendemailDetail(string id)
        {
            try
            {
                DateTime now = DateTime.Now;
                string token = Request.Headers[HeaderNames.Authorization].ToString();

                if (_config["Path:Mode"] == "test")
                {
                    if (token != key_test)
                        return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้อง", });
                }
                else
                {
                    if (token != key_pro)
                        return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้อง", });
                }

                var membersId = await (from m in _context.members
                                       where m.group_name == "Isuzu"
                                       select m.id).ToListAsync();

                var etaxFile = _context.etax_files
               .Where(x => x.etax_id == id && membersId.Contains(x.member_id) && x.delete_status == 0)
               .FirstOrDefault();

                if (etaxFile == null)
                    return StatusCode(400, new { error_code = "1002", message = "ไม่พบข้อมูลในระบบ", });

                if (etaxFile.add_email_status == "no")
                    return StatusCode(400, new { error_code = "1003", message = "ไม่มีการส่ง email", });

                if (etaxFile.add_email_status == "pending")
                    return StatusCode(400, new { error_code = "1004", message = "ระบบกำลังดำเนินการส่ง email", });

                var send_email = _context.send_email
               .Where(x => x.etax_file_id == etaxFile.id)
               .FirstOrDefault();

                if (send_email == null)
                    return StatusCode(400, new { error_code = "1002", message = "ไม่พบข้อมูลในระบบ", });

                var response_email = _context.response_email
               .Where(x => x.send_email_id == send_email.id)
               .Select(x => new
               {
                   email = x.email,
                   status = x.email_status,
               })
               .ToList();

                if (send_email == null)
                    return StatusCode(400, new { error_code = "1002", message = "ไม่พบข้อมูลในระบบ", });

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
        [Route("tripetch/sendebxml_detail/{id}")]
        public async Task<IActionResult> Tp_ApiSendRdDetail(string id)
        {
            try
            {
                DateTime now = DateTime.Now;
                string token = Request.Headers[HeaderNames.Authorization].ToString();

                if (_config["Path:Mode"] == "test")
                {
                    if (token != key_test)
                        return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้อง", });
                }
                else
                {
                    if (token != key_pro)
                        return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้อง", });
                }

                var membersId = await (from m in _context.members
                                       where m.group_name == "Isuzu"
                                       select m.id).ToListAsync();

                var etaxFile = _context.etax_files
               .Where(x => x.etax_id == id && membersId.Contains(x.member_id) && x.delete_status == 0)
               .FirstOrDefault();

                if (etaxFile == null)
                    return StatusCode(400, new { error_code = "1002", message = "ไม่พบข้อมูลในระบบ", });

                if (etaxFile.add_ebxml_status == "no")
                    return StatusCode(400, new { error_code = "1003", message = "ไม่มีการส่งสรรพากร", });

                var send_ebxml = _context.send_ebxml
               .Where(x => x.etax_file_id == etaxFile.id)
               .FirstOrDefault();

                if (send_ebxml == null)
                    return StatusCode(400, new { error_code = "1002", message = "ไม่พบข้อมูลในระบบ", });


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

        //[HttpPost]
        //[Route("tripetch/test_webhook/{id}")]
        //public async Task<IActionResult> Tp_TestWebhook(string id)
        //{
        //    try
        //    {
        //        DateTime now = DateTime.Now;
        //        string token = Request.Headers[HeaderNames.Authorization].ToString();

        //        if (_config["Path:Mode"] == "test")
        //        {
        //            if (token != key_test)
        //                return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้อง", });
        //        }
        //        else
        //        {
        //            if (token != key_pro)
        //                return StatusCode(401, new { error_code = "1001", message = "token ไม่ถูกต้อง", });
        //        }

        //        var membersId = await (from m in _context.members
        //                               where m.group_name == "Isuzu"
        //                               select m.id).ToListAsync();

        //        var etaxFile = _context.etax_files
        //       .Where(x => x.etax_id == id && membersId.Contains(x.member_id) && x.delete_status == 0)
        //       .FirstOrDefault();

        //        if (etaxFile == null)
        //            return StatusCode(400, new { message = "ไม่พบข้อมูลในระบบ", });

        //        etaxFile.webhook = 1;
        //        _context.SaveChanges();


        //        return StatusCode(200, new
        //        {
        //            data = new
        //            {
        //                etax_id = etaxFile.etax_id,
        //            },
        //            message = "ร้องขอ Webhook สำเร็จ",
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(400, new { message = ex.Message });
        //    }
        //}


        [HttpPost]
        [Route("tripetch/get_report_by_email")]
        public async Task<IActionResult> Tp_GetReportByEmail([FromForm] BodyApiGetReportEmailTis bodyApiGetReportEmailTis)
        {
            try
            {
                if (bodyApiGetReportEmailTis.APIKey != "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoiMTAxMSIsIm1lbWJlcl9pZCI6IjEyIiwiZXhwIjoxNzQ5Nzg3ODMyLCJpc3MiOiJwYXBlcm1hdGVfZXRheCIsImF1ZCI6IjEwMTEifQ.Tj1mbOH4Cf_E3F_1AqndDEFtGtMIVQ-pvw8EqlKIyBEzGbZG1qQx9cfdxhn45AZFCOmrXtNT1HJjeWJQxe_d_A")
                    return StatusCode(401, new { message = "token ไม่ถูกต้อง", });

                DateTime strat = DateTime.ParseExact(bodyApiGetReportEmailTis.date + " " + bodyApiGetReportEmailTis.hour.ToString().PadLeft(2, '0'), "yyyy-MM-dd HH", CultureInfo.InvariantCulture);
                DateTime end = strat.AddHours(1);

                var membersId = await (from m in _context.members
                                       where m.group_name == "Isuzu"
                                       select m.id).ToListAsync();

                List<ReturnReportByEmail> listReport = new List<ReturnReportByEmail>();

                var report1 = await (from ef in _context.etax_files
                                     join se in _context.send_email on ef.id equals se.etax_file_id into seGroup
                                     from se in seGroup.DefaultIfEmpty()
                                     join ss in _context.send_sms on ef.id equals ss.etax_file_id into ssGroup
                                     from ss in ssGroup.DefaultIfEmpty()
                                     where membersId.Contains(ef.member_id) && ef.delete_status == 0 && ef.create_date >= strat && ef.create_date < end
                                     select new
                                     {
                                         ef.group_name,
                                         ef.etax_id,
                                         ef.gen_xml_status,
                                         ef.gen_pdf_status,
                                         ef.add_email_status,
                                         ef.add_sms_status,
                                         ef.error,
                                         se.email_status,
                                         email_error = se.error,
                                         ss.send_sms_status,
                                         sms_error = ss.error,
                                     }).ToListAsync();

                var report2 = await (from ef in _context.etax_files
                                     join se in _context.send_ebxml on ef.id equals se.etax_file_id into seGroup
                                     from se in seGroup.DefaultIfEmpty()
                                     where se.send_ebxml_finish >= strat && se.send_ebxml_finish < end
                                     select new
                                     {
                                         ef.group_name,
                                         ef.etax_id,
                                         se.send_ebxml_status,
                                         se.etax_status,
                                         se.error,
                                     }).ToListAsync();

                foreach (var r in report1)
                {
                    var check = listReport.Where(x => x.group_name == r.group_name).FirstOrDefault();
                    if (check != null)
                    {
                        if (r.gen_xml_status == "success" || r.gen_xml_status == "fail")
                            check.total += 1;

                        if (r.gen_xml_status == "success")
                        {
                            check.xml_count += 1;
                        }
                        else
                        {
                            check.listError.Add(new ReturnReportByEmailError()
                            {
                                type = "Gen XML",
                                group_name = r.group_name,
                                etax_id = r.etax_id,
                                error = r.error,
                            });
                        }

                        if (r.gen_pdf_status == "success")
                        {
                            check.pdf_count += 1;
                        }
                        else
                        {
                            check.listError.Add(new ReturnReportByEmailError()
                            {
                                type = "Gen PDF",
                                group_name = r.group_name,
                                etax_id = r.etax_id,
                                error = r.error,
                            });

                        }

                        if (r.email_status == "success" || r.email_status == "open" || r.send_sms_status == "success")
                        {
                            check.send_to_cus_count += 1;
                        }

                        if (r.add_email_status == "no" && r.add_sms_status == "no")
                        {
                            check.not_send_to_cus_count += 1;
                        }


                        string error = "";
                        if (r.sms_error != null && r.sms_error != "")
                            error = r.sms_error;
                        if (r.email_error != null && r.email_error != "")
                            error = r.email_error;

                        if (error != "")
                            check.listError.Add(new ReturnReportByEmailError()
                            {
                                type = "Submit to Customer",
                                group_name = r.group_name,
                                etax_id = r.etax_id,
                                error = error,
                            });


                        var r2 = report2.Where(x => x.etax_id == r.etax_id).FirstOrDefault();
                        if (r2 != null)
                        {
                            if (r2.send_ebxml_status == "success")
                            {
                                check.send_ebxml_status += 1;
                            }
                            else
                            {
                                check.listError.Add(new ReturnReportByEmailError()
                                {
                                    type = "Submit to RD",
                                    group_name = r.group_name,
                                    etax_id = r.etax_id,
                                    error = r2.error,
                                });
                            }

                            if (r2.etax_status == "success")
                            {
                                check.etax_status += 1;
                            }
                            else
                            {
                                check.listError.Add(new ReturnReportByEmailError()
                                {
                                    type = "From RD",
                                    group_name = r.group_name,
                                    etax_id = r.etax_id,
                                    error = r2.error,
                                });
                            }
                        }
                    }
                    else
                    {
                        ReturnReportByEmail newReport = new ReturnReportByEmail();
                        newReport.listError = new List<ReturnReportByEmailError>();

                        newReport.group_name = r.group_name;

                        if (r.gen_xml_status == "success" || r.gen_xml_status == "fail")
                            newReport.total = 1;

                        if (r.gen_xml_status == "success")
                        {
                            newReport.xml_count = 1;
                        }
                        else
                        {
                            newReport.listError.Add(new ReturnReportByEmailError()
                            {
                                type = "Gen XML",
                                group_name = r.group_name,
                                etax_id = r.etax_id,
                                error = r.error,
                            });
                        }

                        if (r.gen_pdf_status == "success")
                        {
                            newReport.pdf_count = 1;
                        }
                        else
                        {
                            newReport.listError.Add(new ReturnReportByEmailError()
                            {
                                type = "Gen PDF",
                                group_name = r.group_name,
                                etax_id = r.etax_id,
                                error = r.error,
                            });

                        }

                        if (r.email_status == "success" || r.email_status == "open" || r.send_sms_status == "success")
                        {
                            newReport.send_to_cus_count = 1;
                        }

                        if (r.add_email_status == "no" && r.add_sms_status == "no")
                        {
                            newReport.not_send_to_cus_count += 1;
                        }

                        string error = "";
                        if (r.sms_error != null && r.sms_error != "")
                            error = r.sms_error;
                        if (r.email_error != null && r.email_error != "")
                            error = r.email_error;

                        if (error != "")
                            newReport.listError.Add(new ReturnReportByEmailError()
                            {
                                type = "Submit to Customer",
                                group_name = r.group_name,
                                etax_id = r.etax_id,
                                error = error,
                            });

                        var r2 = report2.Where(x => x.etax_id == r.etax_id).FirstOrDefault();
                        if (r2 != null)
                        {
                            if (r2.send_ebxml_status == "success")
                            {
                                newReport.send_ebxml_status = 1;
                            }
                            else
                            {
                                newReport.listError.Add(new ReturnReportByEmailError()
                                {
                                    type = "Submit to RD",
                                    group_name = r.group_name,
                                    etax_id = r.etax_id,
                                    error = r2.error,
                                });
                            }

                            if (r2.etax_status == "success")
                            {
                                newReport.etax_status = 1;
                            }
                            else
                            {
                                newReport.listError.Add(new ReturnReportByEmailError()
                                {
                                    type = "From RD",
                                    group_name = r.group_name,
                                    etax_id = r.etax_id,
                                    error = r2.error,
                                });
                            }
                        }

                        listReport.Add(newReport);
                    }
                }

                return StatusCode(200, new
                {
                    data = listReport,
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