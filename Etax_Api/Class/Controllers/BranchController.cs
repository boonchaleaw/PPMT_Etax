
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
    public class BranchController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IJwtService _jwtService;
        public BranchController(ApplicationDbContext context, IConfiguration config, IExceptionLogger exceptionLogger, IJwtService jwtService)
        {
            _config = config;
            _context = context;
            _context.Database.SetCommandTimeout(180);
            _exceptionLogger = exceptionLogger;
            _jwtService = jwtService;
        }

        [HttpPost]
        [Route("get_branch_tabel")]
        public async Task<IActionResult> GetBranchDataTabel([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_branch_view).FirstOrDefaultAsync();
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

                var result = _context.branchs.Where(x => x.member_id == jwtStatus.member_id && x.delete_status == 0).AsQueryable();

                if (!string.IsNullOrEmpty(searchBy))
                {
                    result = result.Where(r =>
                        r.id.ToString().ToUpper().Contains(searchBy.ToUpper()) ||
                        r.name != null && r.name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.building_number != null && r.building_number.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.building_name != null && r.building_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.street_name != null && r.street_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.district_name != null && r.district_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.amphoe_name != null && r.amphoe_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.province_name != null && r.province_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.zipcode != null && r.zipcode.ToUpper().Contains(searchBy.ToUpper())
                    );
                }

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.CountAsync();
                var totalResultsCount = await _context.branchs.Where(x => x.member_id == jwtStatus.member_id && x.delete_status == 0).CountAsync();

                var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.name,
                        x.branch_code,
                        x.building_number,
                        x.building_name,
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
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("create_branch")]
        public async Task<IActionResult> CreateBranch([FromBody] BodyBranch bodyBranch)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_branch_manage).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                if (String.IsNullOrEmpty(bodyBranch.name))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อสาขา", });

                if (bodyBranch.branch_code.Length != 5)
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสสาขา 5 หลัก", });

                if (String.IsNullOrEmpty(bodyBranch.building_number))
                    return StatusCode(400, new { message = "กรุณากำหนดเลขที่", });

                if (String.IsNullOrEmpty(bodyBranch.district_name))
                    return StatusCode(400, new { message = "กรุณากำหนดเขต/ตำบล", });

                if (String.IsNullOrEmpty(bodyBranch.amphoe_name))
                    return StatusCode(400, new { message = "กรุณากำหนดแขวง/อำเภอ", });

                if (String.IsNullOrEmpty(bodyBranch.province_name))
                    return StatusCode(400, new { message = "กรุณากำหนดจังหวัด", });

                if (String.IsNullOrEmpty(bodyBranch.zipcode))
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสไปรษณีย์", });

                var changeBranch = _context.branchs
                    .Where(x => x.member_id == jwtStatus.member_id && x.branch_code == bodyBranch.branch_code)
                    .FirstOrDefault();

                if (changeBranch != null)
                    return StatusCode(400, new { message = "รหัสสาขานี้มีในระบบแล้ว", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        Branch branch = new Branch()
                        {
                            member_id = jwtStatus.member_id,
                            name = bodyBranch.name,
                            branch_code = bodyBranch.branch_code,
                            building_number = bodyBranch.building_number,
                            building_name = bodyBranch.building_name,
                            street_name = bodyBranch.street_name,
                            district_code = bodyBranch.district_code,
                            district_name = bodyBranch.district_name,
                            amphoe_code = bodyBranch.amphoe_code,
                            amphoe_name = bodyBranch.amphoe_name,
                            province_code = bodyBranch.province_code,
                            province_name = bodyBranch.province_name,
                            zipcode = bodyBranch.zipcode,
                            create_date = DateTime.Now,
                        };
                        _context.Add(branch);
                        await _context.SaveChangesAsync();

                        LogBranch logBranch = new LogBranch()
                        {
                            member_id = jwtStatus.member_id,
                            user_modify_id = jwtStatus.user_id,
                            branch_id = branch.id,
                            action_type = "create",
                            create_date = DateTime.Now,
                        };
                        _context.Add(logBranch);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "เพิ่มผู้ใช้งานสำเร็จ",
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
        [Route("get_branch_detail/{id}")]
        public async Task<IActionResult> GetBranchDetail(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_branch_view).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                var branch = await _context.branchs
                .Where(x => x.id == id && x.member_id == jwtStatus.member_id)
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

                if (branch != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = branch,
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
        [Route("update_branch_user")]
        public async Task<IActionResult> UpdateBranch([FromBody] BodyBranch bodyBranch)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_branch_manage).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                if (String.IsNullOrEmpty(bodyBranch.name))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อสาขา", });

                if (bodyBranch.branch_code.Length != 5)
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสสาขา 5 หลัก", });

                if (String.IsNullOrEmpty(bodyBranch.building_number))
                    return StatusCode(400, new { message = "กรุณากำหนดเลขที่", });

                if (String.IsNullOrEmpty(bodyBranch.district_name))
                    return StatusCode(400, new { message = "กรุณากำหนดเขต/ตำบล", });

                if (String.IsNullOrEmpty(bodyBranch.amphoe_name))
                    return StatusCode(400, new { message = "กรุณากำหนดแขวง/อำเภอ", });

                if (String.IsNullOrEmpty(bodyBranch.province_name))
                    return StatusCode(400, new { message = "กรุณากำหนดจังหวัด", });

                if (String.IsNullOrEmpty(bodyBranch.zipcode))
                    return StatusCode(400, new { message = "กรุณากำหนดรหัสไปรษณีย์", });

                var branch = _context.branchs
                    .Where(x => x.id == bodyBranch.id)
                    .FirstOrDefault();

                if (branch == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try

                    {
                        branch.name = bodyBranch.name;
                        branch.branch_code = bodyBranch.branch_code;
                        branch.building_number = bodyBranch.building_number;
                        branch.building_name = bodyBranch.building_name;
                        branch.street_name = bodyBranch.street_name;
                        branch.district_code = bodyBranch.district_code;
                        branch.district_name = bodyBranch.district_name;
                        branch.amphoe_code = bodyBranch.amphoe_code;
                        branch.amphoe_name = bodyBranch.amphoe_name;
                        branch.province_code = bodyBranch.province_code;
                        branch.province_name = bodyBranch.province_name;
                        branch.zipcode = bodyBranch.zipcode;
                        await _context.SaveChangesAsync();

                        LogBranch logBranch = new LogBranch()
                        {
                            member_id = jwtStatus.member_id,
                            user_modify_id = jwtStatus.user_id,
                            branch_id = branch.id,
                            action_type = "update",
                            create_date = DateTime.Now,
                        };
                        _context.Add(logBranch);
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
        [Route("get_branch")]
        public async Task<IActionResult> GetBranch()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenMember(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                object branchs = await _context.branchs
                .Select(x => new
                {
                    x.id,
                    x.member_id,
                    x.branch_code,
                    x.name,
                })
                .Where(x => x.member_id == jwtStatus.member_id)
                .ToListAsync();


                if (branchs != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = branchs,
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
        [Route("admin/get_branch_tabel")]
        public async Task<IActionResult> GetBranchTabelAdmin([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_user_detail).FirstOrDefaultAsync();

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

                var table = _context.branchs.Where(x => x.delete_status == 0).AsQueryable();

                if (bodyDtParameters.id > 0)
                    table = table.Where(x => x.member_id == bodyDtParameters.id);

                if (!string.IsNullOrEmpty(searchBy))
                {
                    table = table.Where(r =>
                        r.id.ToString().ToUpper().Contains(searchBy.ToUpper()) ||
                        r.name != null && r.name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.branch_code != null && r.branch_code.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.building_number != null && r.building_number.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.building_name != null && r.building_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.street_name != null && r.street_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.district_name != null && r.district_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.amphoe_name != null && r.amphoe_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.province_name != null && r.province_name.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.zipcode != null && r.zipcode.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.name != null && r.name.ToUpper().Contains(searchBy.ToUpper())
                    );
                }

                table = orderAscendingDirection ? table.OrderByProperty(orderCriteria) : table.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await table.CountAsync();

                var tableCount = _context.branchs.Where(x => x.delete_status == 0).AsQueryable();

                if (bodyDtParameters.id > 0)
                    tableCount = tableCount.Where(x => x.member_id == bodyDtParameters.id);

                var totalResultsCount = await tableCount.CountAsync();


                if (bodyDtParameters.Length == -1)
                {
                    var data = await table
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
                    var data = await table
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
        [Route("admin/get_branch_detail/{id}")]
        public async Task<IActionResult> GetBranchDetailAdmin(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_user_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                var branchs = await _context.branchs
                .Where(x => x.id == id)
                .Select(x => new
                {
                    id = x.id,
                    name = x.name,
                    branch_code = x.branch_code,
                    building_number = x.building_number,
                    building_name = x.building_name,
                    street_name = x.street_name,
                    district_code = x.district_code,
                    amphoe_code = x.amphoe_code,
                    province_code = x.province_code,
                    zipcode = x.zipcode,
                })
                .FirstOrDefaultAsync();

                if (branchs != null)
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
                            id = branchs.id,
                            name = branchs.name,
                            branch_code = branchs.branch_code,
                            building_number = branchs.building_number,
                            building_name = branchs.building_name,
                            street_name = branchs.street_name,
                            district = district,
                            amphoe = amphoe,
                            province = province,
                            zipcode = branchs.zipcode,
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
        [Route("admin/add_branch")]
        public async Task<IActionResult> AddBranchAdmin([FromBody] BodyBranchAdmin bodyBranchAdmin)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_user_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                if (String.IsNullOrEmpty(bodyBranchAdmin.name))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อลูกค้า", });

                if (String.IsNullOrEmpty(bodyBranchAdmin.branch_code))
                    return StatusCode(400, new { message = "กรุณากำหนดเลขที่สาขา", });

                if (bodyBranchAdmin.branch_code.Length != 5)
                    return StatusCode(400, new { message = "เลขที่สาขาไม่ถูกต้อง", });

                if (String.IsNullOrEmpty(bodyBranchAdmin.building_number))
                    return StatusCode(400, new { message = "กรุณากำหนดบ้านเลขที่", });

                if (bodyBranchAdmin.province == null)
                    return StatusCode(400, new { message = "กรุณากำหนดจังหวัด", });

                if (bodyBranchAdmin.amphoe == null)
                    return StatusCode(400, new { message = "กรุณากำหนดอำเภอ", });

                if (bodyBranchAdmin.district == null)
                    return StatusCode(400, new { message = "กรุณากำหนดตำบล", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        DateTime now = DateTime.Now;
                        Branch branch = new Branch()
                        {
                            member_id = bodyBranchAdmin.member_id,
                            name = bodyBranchAdmin.name,
                            branch_code = bodyBranchAdmin.branch_code,
                            building_number = bodyBranchAdmin.building_number,
                            building_name = bodyBranchAdmin.building_name,
                            street_name = bodyBranchAdmin.street_name,
                            district_code = bodyBranchAdmin.district.district_code,
                            district_name = bodyBranchAdmin.district.district_th,
                            amphoe_code = bodyBranchAdmin.amphoe.amphoe_code,
                            amphoe_name = bodyBranchAdmin.amphoe.amphoe_th,
                            province_code = bodyBranchAdmin.province.province_code,
                            province_name = bodyBranchAdmin.province.province_th,
                            zipcode = bodyBranchAdmin.district.zipcode,
                            update_date = now,
                            create_date = now,
                        };
                        _context.Add(branch);
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
        [Route("admin/update_branch")]
        public async Task<IActionResult> UpdateBranchAdmin([FromBody] BodyBranchAdmin bodyBranchAdmin)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_user_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                if (String.IsNullOrEmpty(bodyBranchAdmin.name))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อลูกค้า", });

                if (String.IsNullOrEmpty(bodyBranchAdmin.branch_code))
                    return StatusCode(400, new { message = "กรุณากำหนดเลขที่สาขา", });

                if (bodyBranchAdmin.branch_code.Length != 5)
                    return StatusCode(400, new { message = "เลขที่สาขาไม่ถูกต้อง", });

                if (String.IsNullOrEmpty(bodyBranchAdmin.building_number))
                    return StatusCode(400, new { message = "กรุณากำหนดบ้านเลขที่", });

                if (bodyBranchAdmin.province == null)
                    return StatusCode(400, new { message = "กรุณากำหนดจังหวัด", });

                if (bodyBranchAdmin.amphoe == null)
                    return StatusCode(400, new { message = "กรุณากำหนดอำเภอ", });

                if (bodyBranchAdmin.district == null)
                    return StatusCode(400, new { message = "กรุณากำหนดตำบล", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        DateTime now = DateTime.Now;
                        var branch = await _context.branchs
                        .Where(x => x.id == bodyBranchAdmin.id)
                        .FirstOrDefaultAsync();

                        if (branch == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        branch.name = bodyBranchAdmin.name;
                        branch.branch_code = bodyBranchAdmin.branch_code;
                        branch.building_name = bodyBranchAdmin.building_name;
                        branch.building_number = bodyBranchAdmin.building_number;
                        branch.building_name = bodyBranchAdmin.building_name;
                        branch.street_name = bodyBranchAdmin.street_name;
                        branch.district_code = bodyBranchAdmin.district.district_code;
                        branch.district_name = bodyBranchAdmin.district.district_th;
                        branch.amphoe_code = bodyBranchAdmin.amphoe.amphoe_code;
                        branch.amphoe_name = bodyBranchAdmin.amphoe.amphoe_th;
                        branch.province_code = bodyBranchAdmin.province.province_code;
                        branch.province_name = bodyBranchAdmin.province.province_th;
                        branch.zipcode = bodyBranchAdmin.district.zipcode;
                        branch.update_date = now;
                        _context.Update(branch);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return StatusCode(200, new
                        {
                            message = "แก้ไขข้อมูลสำเร็จ",
                        });
                    }
                    catch (Exception ex)
                    {
                      await  transaction.RollbackAsync();
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
        [Route("admin/delete_branch/{id}")]
        public async Task<IActionResult> DeleteBranchAdmin(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = _jwtService.ValidateJwtTokenUser(token);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from up in _context.user_permission
                                        where up.user_id == jwtStatus.user_id
                                        select up.per_user_detail).FirstOrDefaultAsync();

                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var branch = await _context.branchs
                        .Where(x => x.id == id)
                        .FirstOrDefaultAsync();

                        if (branch == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        DateTime now = DateTime.Now;
                        branch.delete_status = 1;
                        branch.update_date = now;

                        _context.Update(branch);
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
