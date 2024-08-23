
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
    public class MemberController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public MemberController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        [HttpPost]
        [Route("get_member_detail")]
        public async Task<IActionResult> GetMemberDetail()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var member = await _context.members
                .Where(x => x.id == jwtStatus.member_id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                })
                .FirstOrDefaultAsync();

                var branch = await _context.branchs
                .Where(x => x.member_id == jwtStatus.member_id && x.branch_code == "00000")
                .Select(x => new
                {
                    x.id,
                    x.building_number,
                    x.building_name,
                    x.street_name,
                    x.district_code,
                    x.district_name,
                    x.amphoe_code,
                    x.amphoe_name,
                    x.province_code,
                    x.province_name,
                    x.zipcode
                })
                .FirstOrDefaultAsync();


                if (member != null && branch != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            id = member.id,
                            name = member.name,
                            tax_id = member.tax_id,
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
        [Route("update_member")]
        public async Task<IActionResult> UpdateMember([FromBody] BodyMember bodyMember)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                if (String.IsNullOrEmpty(bodyMember.name))
                    return StatusCode(400, new { message = "กรุณากำหนดข้อมูลชื่อบริษัท", });

                if (String.IsNullOrEmpty(bodyMember.tax_id))
                    return StatusCode(400, new { message = "กรุณากำหนดข้อมูลเลขประจําตัวผู้เสียภาษี", });

                if (String.IsNullOrEmpty(bodyMember.building_number))
                    return StatusCode(400, new { message = "กรุณากำหนดข้อมูลเลขที่", });

                if (String.IsNullOrEmpty(bodyMember.district_name))
                    return StatusCode(400, new { message = "กรุณากำหนดข้อมูลเขต/ตำบล", });

                if (String.IsNullOrEmpty(bodyMember.amphoe_name))
                    return StatusCode(400, new { message = "กรุณากำหนดข้อมูลแขวง/อำเภอ", });

                if (String.IsNullOrEmpty(bodyMember.province_name))
                    return StatusCode(400, new { message = "กรุณากำหนดข้อมูลจังหวัด", });

                if (String.IsNullOrEmpty(bodyMember.zipcode))
                    return StatusCode(400, new { message = "กรุณากำหนดข้อมูลรหัสไปรษณีย์", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var member = _context.members
                    .Where(x => x.id == bodyMember.id)
                    .FirstOrDefault();

                        member.name = bodyMember.name;
                        member.tax_id = bodyMember.tax_id;
                        await _context.SaveChangesAsync();

                        var branch = _context.branchs
                            .Where(x => x.member_id == bodyMember.id && x.branch_code == "00000")
                            .FirstOrDefault();

                        branch.building_number = bodyMember.building_number;
                        branch.building_name = bodyMember.building_name;
                        branch.street_name = bodyMember.street_name;
                        branch.district_code = bodyMember.district_code;
                        branch.district_name = bodyMember.district_name;
                        branch.amphoe_code = bodyMember.amphoe_code;
                        branch.amphoe_name = bodyMember.amphoe_name;
                        branch.province_code = bodyMember.province_code;
                        branch.province_name = bodyMember.province_name;
                        branch.zipcode = bodyMember.zipcode;
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "แก้ไขข้อมูลสำเร็จ",
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

        //////////////////////////Admin//////////////////////////


        [HttpPost]
        [Route("admin/get_members")]
        public async Task<IActionResult> GetMemberAdmin()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var members = await _context.members
                .Select(x => new
                {
                    x.id,
                    x.name,
                })
                .ToListAsync();


                if (members != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = members,
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
        [Route("admin/get_member_tabel")]
        public async Task<IActionResult> GetMemberTabel([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

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

                var result = _context.members.Where(x=>x.delete_status == 0).AsQueryable();

                if (!string.IsNullOrEmpty(searchBy))
                {
                    result = result.Where(r =>
                        r.id.ToString().ToUpper().Contains(searchBy.ToUpper()) ||
                        r.name != null && r.name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.tax_id != null && r.tax_id.ToUpper().Contains(searchBy.ToUpper())
                    );
                }

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.CountAsync();
                var totalResultsCount = await _context.members.Where(x => x.delete_status == 0).CountAsync();

                if (bodyDtParameters.Length == -1)
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.name,
                        x.tax_id,
                        create_date = (x.create_date == null) ? "" : ((DateTime)x.create_date).ToString("dd/MM/yyyy HH:mm:ss"),
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
                        x.name,
                        x.tax_id,
                        create_date = (x.create_date == null) ? "" : ((DateTime)x.create_date).ToString("dd/MM/yyyy HH:mm:ss"),
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
        [Route("admin/get_member_detail/{id}")]
        public async Task<IActionResult> GetMemberDetail(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var member = await _context.members
                .Where(x => x.id == id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.tax_id,
                    create_date = (x.create_date == null) ? "" : ((DateTime)x.create_date).ToString("dd/MM/yyyy HH:mm:ss"),
                })
                .FirstOrDefaultAsync();

                var branchs = await _context.branchs
                .Where(x => x.member_id == id)
                .Select(x => new
                {
                    building_number = x.building_number,
                    building_name = x.building_name,
                    street_name = x.street_name,
                    district_code = x.district_code,
                    amphoe_code = x.amphoe_code,
                    province_code = x.province_code,
                    zipcode = x.zipcode,
                })
                .FirstOrDefaultAsync();

                var member_document_type = await _context.view_member_document_type
                .Where(x => x.member_id == id)
                .Select(x => new
                {
                    id = x.document_type_id,
                    name = x.document_type_name,
                })
                .ToListAsync();

                if (member != null)
                {
                    var province = await _context.province
                    .Where(x => x.province_code == branchs.province_code)
                    .Select(x => new
                    {
                        x.province_code,
                        x.province_th,
                    })
                    .FirstOrDefaultAsync();

                    var amphoe = await _context.amphoe
                    .Where(x => x.amphoe_code == branchs.amphoe_code)
                    .Select(x => new
                    {
                        x.amphoe_code,
                        x.province_code,
                        x.amphoe_th,
                    })
                    .FirstOrDefaultAsync();

                    var amphoes = await _context.amphoe
                    .Where(x => x.province_code == branchs.province_code)
                    .Select(x => new
                    {
                        x.amphoe_code,
                        x.province_code,
                        x.amphoe_th,
                    })
                    .ToListAsync();

                    var district = await _context.district
                    .Where(x => x.district_code == branchs.district_code)
                    .Select(x => new
                    {
                        x.district_code,
                        x.amphoe_code,
                        x.district_th,
                        x.zipcode,
                    })
                    .FirstOrDefaultAsync();

                    var districts = await _context.district
                    .Where(x => x.amphoe_code == branchs.amphoe_code)
                    .Select(x => new
                    {
                        x.district_code,
                        x.amphoe_code,
                        x.district_th,
                        x.zipcode,
                    })
                    .ToListAsync();

                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = new
                        {
                            id = member.id,
                            name = member.name,
                            tax_id = member.tax_id,
                            building_number = branchs.building_number,
                            building_name = branchs.building_name,
                            street_name = branchs.street_name,
                            district = district,
                            amphoe = amphoe,
                            province = province,
                            zipcode = branchs.zipcode,
                            listDocumentType = member_document_type,
                            amphoes = amphoes,
                            districts = districts,

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
        [Route("admin/add_member")]
        public async Task<IActionResult> AddMember([FromBody] BodyMemberAdmin bodyMemberAdmin)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                if (String.IsNullOrEmpty(bodyMemberAdmin.name))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อลูกค้า", });

                if (String.IsNullOrEmpty(bodyMemberAdmin.tax_id))
                    return StatusCode(400, new { message = "กรุณากำหนดเลขประจําตัวผู้เสียภาษี", });

                if (bodyMemberAdmin.tax_id.Length != 13)
                    return StatusCode(400, new { message = "เลขประจําตัวผู้เสียภาษีไม่ถูกต้อง", });

                if (bodyMemberAdmin.listDocumentType.Count == 0)
                    return StatusCode(400, new { message = "กรุณากำหนดประเภทเอกสาร", });

                if (String.IsNullOrEmpty(bodyMemberAdmin.building_number))
                    return StatusCode(400, new { message = "กรุณากำหนดบ้านเลขที่", });

                if (bodyMemberAdmin.province == null)
                    return StatusCode(400, new { message = "กรุณากำหนดจังหวัด", });

                if (bodyMemberAdmin.amphoe == null)
                    return StatusCode(400, new { message = "กรุณากำหนดอำเภอ", });

                if (bodyMemberAdmin.district == null)
                    return StatusCode(400, new { message = "กรุณากำหนดตำบล", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        DateTime now = DateTime.Now;
                        Member member = new Member()
                        {
                            name = bodyMemberAdmin.name,
                            tax_id = bodyMemberAdmin.tax_id,
                            update_date = now,
                            create_date = now,
                        };
                        _context.Add(member);
                        await _context.SaveChangesAsync();

                        Branch branch = new Branch()
                        {
                            member_id = member.id,
                            name = "สาขาสำนักงานใหญ่",
                            branch_code = "00000",
                            building_number = bodyMemberAdmin.building_number,
                            building_name = bodyMemberAdmin.building_name,
                            street_name = bodyMemberAdmin.street_name,
                            district_code = bodyMemberAdmin.district.district_code,
                            district_name = bodyMemberAdmin.district.district_th,
                            amphoe_code = bodyMemberAdmin.amphoe.amphoe_code,
                            amphoe_name = bodyMemberAdmin.amphoe.amphoe_th,
                            province_code = bodyMemberAdmin.province.province_code,
                            province_name = bodyMemberAdmin.province.province_th,
                            zipcode = bodyMemberAdmin.district.zipcode,
                            update_date = now,
                            create_date = now,
                        };
                        _context.Add(branch);

                        foreach (DocumentType documentType in bodyMemberAdmin.listDocumentType)
                        {
                            MemberDocumentType member_document_type = new MemberDocumentType()
                            {
                                member_id = member.id,
                                document_type_id = documentType.id,
                            };
                            _context.Add(member_document_type);
                        }

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
        [Route("admin/update_member")]
        public async Task<IActionResult> UpdateMember([FromBody] BodyMemberAdmin bodyMemberAdmin)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                if (String.IsNullOrEmpty(bodyMemberAdmin.name))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อลูกค้า", });

                if (String.IsNullOrEmpty(bodyMemberAdmin.tax_id))
                    return StatusCode(400, new { message = "กรุณากำหนดเลขประจําตัวผู้เสียภาษี", });

                if (bodyMemberAdmin.tax_id.Length != 13)
                    return StatusCode(400, new { message = "เลขประจําตัวผู้เสียภาษีไม่ถูกต้อง", });

                if (bodyMemberAdmin.listDocumentType.Count == 0)
                    return StatusCode(400, new { message = "กรุณากำหนดประเภทเอกสาร", });

                if (String.IsNullOrEmpty(bodyMemberAdmin.building_number))
                    return StatusCode(400, new { message = "กรุณากำหนดบ้านเลขที่", });

                if (bodyMemberAdmin.province == null)
                    return StatusCode(400, new { message = "กรุณากำหนดจังหวัด", });

                if (bodyMemberAdmin.amphoe == null)
                    return StatusCode(400, new { message = "กรุณากำหนดอำเภอ", });

                if (bodyMemberAdmin.district == null)
                    return StatusCode(400, new { message = "กรุณากำหนดตำบล", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        DateTime now = DateTime.Now;
                        var member = await _context.members
                        .Where(x => x.id == bodyMemberAdmin.id)
                        .FirstOrDefaultAsync();

                        if (member == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        member.name = bodyMemberAdmin.name;
                        member.tax_id = bodyMemberAdmin.tax_id;
                        member.update_date = now;
                        _context.Update(member);

                        var branch = await _context.branchs
                        .Where(x => x.member_id == bodyMemberAdmin.id)
                        .FirstOrDefaultAsync();

                        if (branch == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        branch.building_number = bodyMemberAdmin.building_number;
                        branch.building_name = bodyMemberAdmin.building_name;
                        branch.street_name = bodyMemberAdmin.street_name;
                        branch.district_code = bodyMemberAdmin.district.district_code;
                        branch.district_name = bodyMemberAdmin.district.district_th;
                        branch.amphoe_code = bodyMemberAdmin.amphoe.amphoe_code;
                        branch.amphoe_name = bodyMemberAdmin.amphoe.amphoe_th;
                        branch.province_code = bodyMemberAdmin.province.province_code;
                        branch.province_name = bodyMemberAdmin.province.province_th;
                        branch.zipcode = bodyMemberAdmin.district.zipcode;
                        branch.update_date = now;
                        _context.Update(branch);

                        var memberDocumentTypeList = await _context.member_document_type
                        .Where(x => x.member_id == member.id)
                        .ToListAsync();

                        foreach (DocumentType documentType in bodyMemberAdmin.listDocumentType)
                        {
                            var find = memberDocumentTypeList.Find(x => x.document_type_id == documentType.id);
                            if (find == null)
                            {
                                MemberDocumentType member_document_type = new MemberDocumentType()
                                {
                                    member_id = member.id,
                                    document_type_id = documentType.id,
                                };
                                _context.Add(member_document_type);
                            }
                        }

                        foreach (var mdt in memberDocumentTypeList)
                        {
                            var find = bodyMemberAdmin.listDocumentType.Find(x => x.id == mdt.document_type_id);
                            if (find == null)
                            {
                                _context.Remove(mdt);
                            }
                        }

                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "แก้ไขข้อมูลสำเร็จ",
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
        [Route("admin/delete_mamber/{id}")]
        public async Task<IActionResult> DeleteMemberAdmin(int id)
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
                        var members = await _context.members
                        .Where(x => x.id == id)
                        .FirstOrDefaultAsync();

                        if (members == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        DateTime now = DateTime.Now;
                        members.delete_status = 1;
                        members.update_date = now;

                        _context.Update(members);
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
