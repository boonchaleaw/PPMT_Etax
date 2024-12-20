
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Etax_Api.Controllers
{
    [Produces("application/json")]
    public class AddressController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public AddressController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        [HttpPost]
        [Route("get_province")]
        public async Task<IActionResult> GetProvince()
        {
            try
            {
                //string token = Request.Headers[HeaderNames.Authorization].ToString();
                //JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                //if (!jwtStatus.status)
                //    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var province = await _context.province
                .Select(x => new
                {
                    x.province_code,
                    x.province_th,
                })
                .OrderBy(x => x.province_th)
                .ToListAsync();


                if (province != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = province,
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
        [Route("get_amphoe/{province_code}")]
        public async Task<IActionResult> GetAmphoe(int province_code)
        {
            try
            {
                //string token = Request.Headers[HeaderNames.Authorization].ToString();
                //JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                //if (!jwtStatus.status)
                //    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var district = await _context.amphoe
                .Select(x => new
                {
                    x.amphoe_code,
                    x.province_code,
                    amphoe_th = x.amphoe_th_s,
                })
                .Where(x => x.province_code == province_code)
                .ToListAsync();


                if (district != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = district,
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
        [Route("get_district/{amphoe_code}")]
        public async Task<IActionResult> GetDistrict(int amphoe_code)
        {
            try
            {
                //string token = Request.Headers[HeaderNames.Authorization].ToString();
                //JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                //if (!jwtStatus.status)
                //    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var district = await _context.district
                .Select(x => new
                {
                    x.district_code,
                    x.amphoe_code,
                    district_th = x.district_th_s,
                    x.zipcode,
                })
                .Where(x => x.amphoe_code == amphoe_code)
                .ToListAsync();


                if (district != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = district,
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
        [Route("admin/get_province")]
        public async Task<IActionResult> Admin_GetProvince()
        {
            try
            {
                //string token = Request.Headers[HeaderNames.Authorization].ToString();
                //JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                //if (!jwtStatus.status)
                //    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var province = await _context.province
                .Select(x => new
                {
                    x.province_code,
                    x.province_th,
                })
                .OrderBy(x => x.province_th)
                .ToListAsync();


                if (province != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = province,
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
        [Route("admin/get_amphoe/{province_code}")]
        public async Task<IActionResult> Admin_GetAmphoe(int province_code)
        {
            try
            {
                //string token = Request.Headers[HeaderNames.Authorization].ToString();
                //JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                //if (!jwtStatus.status)
                //    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var district = await _context.amphoe
                .Select(x => new
                {
                    x.amphoe_code,
                    x.province_code,
                    amphoe_th = x.amphoe_th_s,
                })
                .Where(x => x.province_code == province_code)
                .ToListAsync();


                if (district != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = district,
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
        [Route("admin/get_district/{amphoe_code}")]
        public async Task<IActionResult> Admin_GetDistrict(int amphoe_code)
        {
            try
            {
                //string token = Request.Headers[HeaderNames.Authorization].ToString();
                //JwtStatus jwtStatus = Jwt.ValidateJwtToken(token);

                //if (!jwtStatus.status)
                //    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });


                var district = await _context.district
                .Select(x => new
                {
                    x.district_code,
                    x.amphoe_code,
                    district_th = x.district_th_s,
                    x.zipcode,
                })
                .Where(x => x.amphoe_code == amphoe_code)
                .ToListAsync();


                if (district != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = district,
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

    }
}
