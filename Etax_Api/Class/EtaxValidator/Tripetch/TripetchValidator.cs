using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Etax_Api.Class.EtaxValidator.Tripetch
{
    public static class TripetchValidator
    {
        public static IActionResult ValidateInput(BodyApiTripetchCreateEtax body, string msgId)
        {
            if (string.IsNullOrEmpty(body.document_type_code))
                return TripetchEtaxResponseHelper.BadRequest("2001", "กรุณากำหนดประเภทเอกสาร", msgId);

            if (string.IsNullOrEmpty(body.etax_id))
                return TripetchEtaxResponseHelper.BadRequest("2002", "กรุณากำหนดหมายเลขเอกสาร", msgId);

            if (string.IsNullOrEmpty(body.issue_date))
                return TripetchEtaxResponseHelper.BadRequest("2003", "กรุณากำหนดวันที่สร้างเอกสาร", msgId);

            if (body.document_type_code == "2" || body.document_type_code == "3")
            {
                if (string.IsNullOrEmpty(body.ref_etax_id))
                    return TripetchEtaxResponseHelper.BadRequest("2004", "กรุณากำหนดหมายเลขเอกสารอ้างอิง", msgId);

                if (string.IsNullOrEmpty(body.ref_issue_date))
                    return TripetchEtaxResponseHelper.BadRequest("2005", "กรุณากำหนดวันที่สร้างเอกสารอ้างอิง", msgId);
            }

            if (string.IsNullOrEmpty(body.buyer?.name))
                return TripetchEtaxResponseHelper.BadRequest("2006", "กรุณากำหนดชื่อผู้ซื้อ", msgId);

            if (string.IsNullOrEmpty(body.buyer.tax_id))
                return TripetchEtaxResponseHelper.BadRequest("2007", "กรุณากำหนดเลขประจำตัวผู้เสียภาษี", msgId);

            if (string.IsNullOrEmpty(body.buyer.address))
                return TripetchEtaxResponseHelper.BadRequest("2008", "กรุณากำหนดที่อยู่", msgId);

            if (string.IsNullOrEmpty(body.buyer.zipcode) || body.buyer.zipcode.Length != 5)
                return TripetchEtaxResponseHelper.BadRequest("2010", "กรุณากำหนดรหัสไปรษณีย์", msgId);

            ////if (bodyApiCreateEtax.buyer.branch_code.Length != 5)
            ////    return StatusCode(400, new { error_code = "2009", message = "กรุณากำหนดรหัสสาขา 5 หลัก", });


            if (body.items == null || !body.items.Any())
                return TripetchEtaxResponseHelper.BadRequest("2026", "กรุณากำหนดรายการสินค้า", msgId);


            if (string.IsNullOrEmpty(body.basisamount.ToString()))
                return TripetchEtaxResponseHelper.BadRequest("2013", "กรุณากำหนดจำนวนเงินส่วนลด" , msgId);
            if (string.IsNullOrEmpty(body.taxbasis_totalamount.ToString()))
                return TripetchEtaxResponseHelper.BadRequest("2014", "กรุณากำหนดภาษีส่วนลด", msgId);


            int i = 1;
            foreach (var item in body.items)
            {
                //if (String.IsNullOrEmpty(item.code))
                //    return StatusCode(400, new { error_code = "2011", message = "กรุณากำหนดรหัสสินค้า รายการสินค้าที่ " + itemLine, });

                if (string.IsNullOrEmpty(item.name))
                    return TripetchEtaxResponseHelper.BadRequest("2012", $"กรุณากำหนดชื่อสินค้า รายการที่ {i}", msgId);

                if (string.IsNullOrEmpty(item.price.ToString()))
                    return TripetchEtaxResponseHelper.BadRequest("2013", $"กรุณากำหนดจำนวนเงิน รายการสินค้าที่ {i}", msgId);

                if (string.IsNullOrEmpty(item.total.ToString()))
                    return TripetchEtaxResponseHelper.BadRequest("2014", $"กรุณากำหนดภาษี รายการสินค้าที่ {i}" + item, msgId);

                if (string.IsNullOrEmpty(item.basisamount.ToString()))
                    return TripetchEtaxResponseHelper.BadRequest("2014", $"กรุณากำหนดภาษีส่วนลด รายการสินค้าที่ {i}" + item, msgId);


                i++;
            }

            return null; // ผ่านการตรวจสอบ
        }



    }


}
