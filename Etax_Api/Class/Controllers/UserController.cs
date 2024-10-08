
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
    public class UserController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public UserController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        //////////////////////////Admin//////////////////////////

        [HttpPost]
        [Route("admin/get_user_tabel")]
        public async Task<IActionResult> GetUserTabel([FromBody] BodyDtParameters bodyDtParameters)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

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

                var result = _context.users.Where(x => x.delete_status == 0).AsQueryable();

                if (!string.IsNullOrEmpty(searchBy))
                {
                    result = result.Where(r =>
                        r.id.ToString().ToUpper().Contains(searchBy.ToUpper()) ||
                        r.firstname != null && r.firstname.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.lastname != null && r.lastname.ToUpper().Contains(searchBy.ToUpper()) ||
                        r.username != null && r.username.ToUpper().Contains(searchBy.ToUpper())
                    );
                }

                result = orderAscendingDirection ? result.OrderByProperty(orderCriteria) : result.OrderByPropertyDescending(orderCriteria);

                var filteredResultsCount = await result.CountAsync();
                var totalResultsCount = await _context.users.Where(x => x.delete_status == 0).CountAsync();

                if (bodyDtParameters.Length == -1)
                {
                    var data = await result
                    .Select(x => new
                    {
                        x.id,
                        x.firstname,
                        x.lastname,
                        x.username,
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
                        x.firstname,
                        x.lastname,
                        x.username,
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
        [Route("admin/get_user_detail/{id}")]
        public async Task<IActionResult> GetUserDetail(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var user = await _context.users
                .Where(x => x.id == id)
                .Select(x => new
                {
                    x.id,
                    x.firstname,
                    x.lastname,
                    x.username,
                })
                .FirstOrDefaultAsync();

                if (user != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = user,
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
        [Route("admin/add_user")]
        public async Task<IActionResult> AddUser([FromBody] BodyUser bodyUser)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                if (String.IsNullOrEmpty(bodyUser.firstname))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อ", });

                if (String.IsNullOrEmpty(bodyUser.lastname))
                    return StatusCode(400, new { message = "กรุณากำหนดนามสกุล", });

                if (String.IsNullOrEmpty(bodyUser.username))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อเข้าใช้งาน", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        DateTime now = DateTime.Now;
                        User user = new User()
                        {
                            firstname = bodyUser.firstname,
                            lastname = bodyUser.lastname,
                            username = bodyUser.username,
                            update_date = now,
                            create_date = now,
                        };
                        _context.Add(user);
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
        [Route("admin/update_user")]
        public async Task<IActionResult> UpdateUser([FromBody] BodyUser bodyUser)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                if (String.IsNullOrEmpty(bodyUser.firstname))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อ", });

                if (String.IsNullOrEmpty(bodyUser.lastname))
                    return StatusCode(400, new { message = "กรุณากำหนดนามสกุล", });

                if (String.IsNullOrEmpty(bodyUser.username))
                    return StatusCode(400, new { message = "กรุณากำหนดชื่อเข้าใช้งาน", });


                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var user = await _context.users
                        .Where(x => x.id == bodyUser.id)
                        .FirstOrDefaultAsync();

                        if (user == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        DateTime now = DateTime.Now;
                        user.firstname = bodyUser.firstname;
                        user.lastname = bodyUser.lastname;
                        user.username = bodyUser.username;
                        user.update_date = now;

                        _context.Update(user);
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
        [Route("admin/delete_user/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenUser(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var user = await _context.users
                        .Where(x => x.id == id)
                        .FirstOrDefaultAsync();

                        if (user == null)
                            return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                        DateTime now = DateTime.Now;
                        user.delete_status = 1;
                        user.update_date = now;

                        _context.Update(user);
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
