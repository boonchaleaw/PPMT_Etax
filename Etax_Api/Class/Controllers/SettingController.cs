
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
    public class SettingController : ControllerBase
    {
        private IConfiguration _config;
        private ApplicationDbContext _context;
        public SettingController(IConfiguration config)
        {
            _config = config;
            _context = new ApplicationDbContext(_config);
            _context.Database.SetCommandTimeout(180);
        }

        [HttpPost]
        [Route("get_setting")]
        public async Task<IActionResult> GetSetting()
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_setting_manage).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });


                var setting = await _context.setting
                .Where(x =>  x.member_id == jwtStatus.member_id)
                .Select(x => new
                {
                    x.sendemail,
                    x.sendemail_day,
                    x.sendemail_dayweek,
                    x.sendemail_time_hh,
                    x.sendemail_time_mm,
                    x.sendebxml,
                    x.sendebxml_day,
                    x.sendebxml_dayweek,
                    x.sendebxml_time_hh,
                    x.sendebxml_time_mm,
                })
                .FirstOrDefaultAsync();

                if (setting != null)
                {
                    return StatusCode(200, new
                    {
                        message = "เรียกดูข้อมูลสำเร็จ",
                        data = setting,
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
        [Route("update_setting")]
        public async Task<IActionResult> UpdateSetting([FromBody] BodySetting bodySetting)
        {
            try
            {
                string token = Request.Headers[HeaderNames.Authorization].ToString();
                JwtStatus jwtStatus = Jwt.ValidateJwtTokenMember(token, _config);

                if (!jwtStatus.status)
                    return StatusCode(401, new { message = "token ไม่ถูกต้องหรือหมดอายุ", });

                var permission = await (from mup in _context.member_user_permission
                                        where mup.member_user_id == jwtStatus.user_id
                                        select mup.per_setting_manage).FirstOrDefaultAsync();
                if (permission != "Y")
                    return StatusCode(401, new { message = "ไม่มีสิทธิในการใช้งานส่วนนี้", });

                var setting = _context.setting
                    .Where(x => x.member_id == jwtStatus.member_id)
                    .FirstOrDefault();

                if (setting == null)
                    return StatusCode(400, new { message = "ไม่พบข้อมูลที่ต้องการ", });

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        setting.sendemail = bodySetting.sendemail;
                        setting.sendemail_day = bodySetting.sendemail_day;
                        setting.sendemail_dayweek = bodySetting.sendemail_dayweek;
                        setting.sendemail_time_hh = bodySetting.sendemail_time_hh;
                        setting.sendemail_time_mm = bodySetting.sendemail_time_mm;
                        setting.sendebxml = bodySetting.sendebxml;
                        setting.sendebxml_day = bodySetting.sendebxml_day;
                        setting.sendebxml_dayweek = bodySetting.sendebxml_dayweek;
                        setting.sendebxml_time_hh = bodySetting.sendebxml_time_hh;
                        setting.sendebxml_time_mm = bodySetting.sendebxml_time_mm;
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

    }
}
