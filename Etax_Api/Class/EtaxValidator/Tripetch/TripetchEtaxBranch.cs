using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;

namespace Etax_Api.Class.EtaxValidator.Tripetch
{
    public static class TripetchEtaxBranch
    {
        public static async Task<(Branch branch, IActionResult errorResult)> FindOrCreateBranchAsync(
    ApplicationDbContext context,
    BodyApiTripetchCreateEtax body,
    Member member,
    DateTime now,
    string msgId)
        {
            var branch = await context.branchs
                .FirstOrDefaultAsync(x => x.member_id == member.id && x.branch_code == body.seller.branch_code);

            if (branch != null)
                return (branch, null);

            // (สามารถนำ logic ตรวจสอบจังหวัด/อำเภอ/ตำบล มาแทรกต่อจากนี้)

            if (string.IsNullOrEmpty(body.seller.branch_name_th))
                return (null,TripetchEtaxResponseHelper.BadRequest("2015", "กรุณาระบุชื่อสาขา", msgId));

            if (string.IsNullOrEmpty(body.seller.building_number))
                return (null, TripetchEtaxResponseHelper.BadRequest("2016", "กรุณาระบุบ้านเลขที่", msgId));

            if (body.seller.building_number.Length > 16)
                return (null, TripetchEtaxResponseHelper.BadRequest("2017", "กรุณาระบุบ้านเลขที่น้อยกว่า 16 หลัก", msgId));

            if (string.IsNullOrEmpty(body.seller.district_name_th))
                return (null, TripetchEtaxResponseHelper.BadRequest( "2018",  "กรุณาระบุตำบล/แขวง", msgId));

            if (string.IsNullOrEmpty(body.seller.amphoe_name_th))
                return (null, TripetchEtaxResponseHelper.BadRequest( "2019","กรุณาระบุอำเภอ/เขต",msgId));

            if (string.IsNullOrEmpty(body.seller.province_name_th))
                return (null, TripetchEtaxResponseHelper.BadRequest("2020", "กรุณาระบุจังหวัด", msgId));

            if (string.IsNullOrEmpty(body.seller.zipcode))
                return (null, TripetchEtaxResponseHelper.BadRequest("2021", "กรุณาระบุรหัสไปรษณีย์", msgId));

            var newBranch = new Branch
            {
                member_id = member.id,
                name = body.seller.branch_name_th?.Trim(),
                name_en = body.seller.branch_name_en?.Trim(),
                branch_code = body.seller.branch_code?.Trim(),
                building_number = body.seller.building_number?.Trim(),
                building_name = body.seller.building_name_th?.Trim(),
                building_name_en = body.seller.building_name_en?.Trim(),
                street_name = body.seller.street_name_th?.Trim(),
                street_name_en = body.seller.street_name_en?.Trim(),
                zipcode = body.seller.zipcode,
                update_date = now,
                create_date = now,
                delete_status = 0,
            };

            context.Add(newBranch);
            await context.SaveChangesAsync();
            return (newBranch, null);
        }
    }
}
