
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
        private Regex rxZipCode = new Regex(@"[0-9]{5}");
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


        [HttpPost]
        [Route("get_adjust_address")]
        public async Task<IActionResult> GetAdjustAddress([FromBody] BodyAdjustAddress bodyAdjustAddress)
        {
            try
            {
                string address = bodyAdjustAddress.address;
                string zipcode = "";

            Recheck:
                if (address.Contains("  "))
                {
                    address = address.Replace("  ", " ");
                    goto Recheck;
                }

                string district_address = "";

                List<string> listAddress = address.Split(' ').ToList();
                foreach (string a in listAddress)
                {
                    if (a.Contains("แขวง") || a.Contains("ตำบล") || a.Contains("ต."))
                        district_address = a.Replace("แขวง", "").Replace("ตำบล", "").Replace("ต.", "");
                }

                MatchCollection matcheZipCode = rxZipCode.Matches(address);
                if (matcheZipCode.Count > 0)
                    zipcode = matcheZipCode[matcheZipCode.Count() - 1].Value;

                //if (zipcode == "" && bodyAdjustAddress.zipcode.Length == 5)
                //    zipcode = bodyAdjustAddress.zipcode;

                Province province = new Province();
                Amphoe amphoe = new Amphoe();
                District district = new District();


                List<District> districts = _context.district.Where(x => x.zipcode == zipcode).ToList();
                if (districts.Count > 1)
                {
                    foreach (District dis in districts)
                    {
                        if (dis.district_th.Contains(district_address))
                        {
                            district = dis;
                            break;
                        }
                    }
                }

                amphoe = _context.amphoe.Where(x => x.amphoe_code == district.amphoe_code).FirstOrDefault();
                province = _context.province.Where(x => x.province_code == amphoe.province_code).FirstOrDefault();


                string newAddress = "";
                foreach (string a in listAddress)
                {
                    bool status = false;
                    if (a.Contains(district.district_th_s))
                        status = true;
                    else if (a.Contains(amphoe.amphoe_th_s))
                        status = true;
                    else if (a.Contains(province.province_th))
                        status = true;
                    else if (a.Contains(zipcode))
                        status = true;


                    if (!status)
                        newAddress += a + " ";
                }


                return StatusCode(200, new
                {
                    message = "เรียกดูข้อมูลสำเร็จ",
                    data = new
                    {
                        address = newAddress.Trim(),
                        district = district,
                        amphoe = amphoe,
                        province = province,
                    },
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new
                {
                    message = "ไม่พบข้อมูล",
                });
            }
        }

        [HttpPost]
        [Route("get_remove_address")]
        public async Task<IActionResult> GetRemoveAddress([FromBody] BodyAdjustAddress bodyAdjustAddress)
        {
            try
            {
                string address = bodyAdjustAddress.address;

            Recheck:
                if (address.Contains("  "))
                {
                    address = address.Replace("  ", " ");
                    goto Recheck;
                }

                List<string> listAddress = address.Split(' ').ToList();


                District district = _context.district.Where(x => x.district_code == bodyAdjustAddress.district_code).FirstOrDefault();
                Amphoe amphoe = _context.amphoe.Where(x => x.amphoe_code == bodyAdjustAddress.amphoe_code).FirstOrDefault();
                Province province = _context.province.Where(x => x.province_code == bodyAdjustAddress.province_code).FirstOrDefault();


                string newAddress = "";
                foreach (string a in listAddress)
                {
                    bool status = false;
                    if (district != null && a.Contains(district.district_th_s))
                        status = true;
                    else if (amphoe != null && a.Contains(amphoe.amphoe_th_s))
                        status = true;
                    else if (province != null && a.Contains(province.province_th))
                        status = true;
                    else if (bodyAdjustAddress.zipcode.Length == 5 && a.Contains(bodyAdjustAddress.zipcode))
                        status = true;


                    if (!status)
                        newAddress += a + " ";
                }


                return StatusCode(200, new
                {
                    message = "เรียกดูข้อมูลสำเร็จ",
                    data = new
                    {
                        address = newAddress.Trim(),
                    },
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new
                {
                    message = "ไม่พบข้อมูล",
                });
            }
        }
    
        [HttpPost]
        [Route("get_check_address")]
        public async Task<IActionResult> GetCheckAddress([FromBody] BodyAdjustAddress bodyAdjustAddress)
        {
            try
            {
                string address = bodyAdjustAddress.address;
                List<string> listAddress = address.Split(' ').ToList();

                string district_name = listAddress[listAddress.Count-3];
                string amphoe_name = listAddress[listAddress.Count-2];
                string province_name = listAddress[listAddress.Count-1];

                District district = null;
                List<District> districts = _context.district.Where(x => x.zipcode == bodyAdjustAddress.zipcode).ToList();
                if (districts.Count > 1)
                {
                    foreach (District dis in districts)
                    {
                        if (dis.district_th.Contains(district_name))
                        {
                            district = dis;
                            break;
                        }
                    }
                }

                Amphoe amphoe = _context.amphoe.Where(x => x.amphoe_code == district.amphoe_code && x.amphoe_th_s == amphoe_name).FirstOrDefault();
                Province province = _context.province.Where(x => x.province_code == amphoe.province_code && x.province_th == province_name).FirstOrDefault();


                string newAddress = "";
                foreach (string a in listAddress)
                {
                    bool status = false;
                    if (district != null && a.Contains(district.district_th_s))
                        status = true;
                    else if (amphoe != null && a.Contains(amphoe.amphoe_th_s))
                        status = true;
                    else if (province != null && a.Contains(province.province_th))
                        status = true;
                    else if (bodyAdjustAddress.zipcode.Length == 5 && a.Contains(bodyAdjustAddress.zipcode))
                        status = true;


                    if (!status)
                        newAddress += a + " ";
                }


                return StatusCode(200, new
                {
                    message = "เรียกดูข้อมูลสำเร็จ",
                    data = new
                    {
                        address = newAddress.Trim(),
                        district = district,
                        amphoe = amphoe,
                        province = province,
                    },
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new
                {
                    message = "ไม่พบข้อมูล",
                });
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
