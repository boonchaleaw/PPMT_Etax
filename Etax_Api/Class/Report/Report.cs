using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Etax_Api
{
    public static class Report
    {
        public static void DefaultCostReport(string path, Member member, DateTime dateStart, DateTime dateEnd, ReturnCostReport returnCostReport)
        {
            using (StreamWriter outputFile = new StreamWriter(path, false, Encoding.UTF8))
            {
                string date = "";
                string dateStartM = dateStart.ToString("MMMM");
                string dateStartY = dateStart.ToString("yyyy");
                string dateEndM = dateEnd.ToString("MMMM");
                string dateEndY = dateEnd.ToString("yyyy");
                if (dateStartM == dateEndM && dateStartY == dateEndY)
                {
                    date = "เดือน " + dateStartM + " ปี " + dateStartY;
                }
                else
                {
                    date = "เดือน " + dateStartM + " ปี " + dateStartY + " ถึง เดือน " + dateEndM + " ปี " + dateEndY;
                }

                outputFile.WriteLine(member.name + ",,,,");

                outputFile.WriteLine("รายงานค่าใช้จ่าย,,,,");
                outputFile.WriteLine(date + ",,,,");
                outputFile.WriteLine(",,,,");

                outputFile.WriteLine("การนำเข้า,สร้างไฟ XML,สร้างไฟ PDF,ส่งอีเมล,ส่ง SMS,ส่งสรรพากร");

                int xml_count_total = 0;
                int pdf_count_total = 0;
                int email_count_total = 0;
                int sms_count_total = 0;
                int ebxml_count_total = 0;
                double xml_price_total = 0;
                double pdf_price_total = 0;
                double email_price_total = 0;
                double sms_price_total = 0;
                double ebxml_price_total = 0;

                foreach (ReturnCostReportData data in returnCostReport.listReturnCostReportData)
                {
                    outputFile.WriteLine(
                    data.row_name.ToString().Replace("\r\n", " ").Replace(",", " ") + "," +
                      data.xml_count.ToString() + "," +
                      data.pdf_count.ToString() + "," +
                      data.email_count.ToString() + "," +
                      data.sms_count.ToString() + "," +
                      data.ebxml_count.ToString()
                    );
                }

                xml_count_total = returnCostReport.total_xml_count;
                pdf_count_total = returnCostReport.total_pdf_count;
                email_count_total = returnCostReport.total_email_count;
                sms_count_total = returnCostReport.total_sms_count;
                ebxml_count_total = returnCostReport.total_ebxml_count;


                xml_price_total = returnCostReport.total_xml_price;
                pdf_price_total = returnCostReport.total_pdf_price;
                email_price_total = returnCostReport.total_email_price;
                sms_price_total = returnCostReport.total_sms_price;
                ebxml_price_total = returnCostReport.total_ebxml_price;


                outputFile.WriteLine("");
                outputFile.WriteLine("จำนวนรวม," + xml_count_total.ToString() + "," + pdf_count_total.ToString() + "," + email_count_total.ToString() + "," + sms_count_total.ToString() + "," + ebxml_count_total.ToString());
                outputFile.WriteLine("ราคารวม," + xml_price_total.ToString("0.00") + "," + pdf_price_total.ToString("0.00") + "," + email_price_total.ToString("0.00") + "," + sms_price_total.ToString("0.00") + "," + ebxml_price_total.ToString("0.00"));
                outputFile.WriteLine("ราคารวมทั้งหมด,,,,," + (xml_price_total + pdf_price_total + email_price_total + ebxml_price_total + sms_price_total).ToString("0.00"));

            }

        }
        public static void DefaultTexReport(string path, BodyDtParameters bodyDtParameters, List<ViewTaxCsvReport> listData, double sumOriginalPrice, double sumPrice, double sumDiscount, double sumTax, double sumTotal)
        {
            using (StreamWriter outputFile = new StreamWriter(path, false, Encoding.UTF8))
            {
                //if (bodyDtParameters.docType == "DBN")
                //{
                //    outputFile.WriteLine("รหัสไฟล์,ประเภทเอกสาร,หมายเลขเอกสาร,หมายเลขผู้เสียภาษี,ชื่อผู้เสียภาษี,สาขา,ที่อยู่,ออกเอกสาร,วันที่สร้าง,มูลค่าเดิม,มูลค่าใหม่,ผลต่าง,ภาษี,รวม,สถานะส่งอีเมล,สถานะส่งสรรพากร");

                //    foreach (ViewTaxCsvReport data in listData)
                //    {
                //        string issue_date = "";
                //        if (data.issue_date != null)
                //            issue_date = ((DateTime)data.issue_date).ToString("dd/MM/yyyy");
                //        else
                //            issue_date = "";

                //        string gen_xml_finish = "";
                //        if (data.gen_xml_finish != null)
                //            gen_xml_finish = ((DateTime)data.gen_xml_finish).ToString("dd/MM/yyyy HH:mm:ss");
                //        else
                //            gen_xml_finish = "";


                //        string status_email = "ยังไม่ส่ง";
                //        if (data.send_email_status == "success")
                //        {
                //            status_email = "ส่งแล้ว";
                //        }

                //        string status_ebxml = "ยังไม่ส่ง";
                //        if (data.send_ebxml_status == "success")
                //        {
                //            status_ebxml = "ส่งแล้ว";
                //        }

                //        outputFile.WriteLine(
                //            data.id.ToString() + "," +
                //            data.document_type_name + "," +
                //            data.etax_id + "," +
                //            "'" + data.buyer_tax_id + "," +
                //            data.buyer_name.Replace("\r\n", " ").Replace(",", " ") + "," +
                //            "'" + data.buyer_branch_code + "," +
                //            data.buyer_address.Replace("\r\n", " ").Replace(",", " ") + "," +
                //            issue_date + "," +
                //            gen_xml_finish + "," +
                //            data.original_price.ToString("0.00") + "," +
                //            data.price.ToString("0.00") + "," +
                //            Math.Abs(data.original_price - data.price).ToString("0.00") + "," +
                //            data.tax.ToString("0.00") + "," +
                //        data.total.ToString("0.00") + "," +
                //        status_email + "," +
                //        status_ebxml
                //            );
                //    }
                //    outputFile.WriteLine("");
                //    outputFile.WriteLine(",,,,,,,,รวม," + sumOriginalPrice.ToString("0.00") + "," + sumPrice.ToString("0.00") + "," + Math.Abs(sumOriginalPrice - sumPrice).ToString("0.00") + "," + sumTax.ToString("0.00") + "," + sumTotal.ToString("0.00"));

                //}
                //else if (bodyDtParameters.docType == "CDN")
                //{
                //    outputFile.WriteLine("รหัสไฟล์,ประเภทเอกสาร,หมายเลขเอกสาร,หมายเลขผู้เสียภาษี,ชื่อผู้เสียภาษี,สาขา,ที่อยู่,ออกเอกสาร,วันที่สร้าง,มูลค่าเดิม,มูลค่าใหม่,ผลต่าง,ภาษี,รวม,สถานะส่งอีเมล,สถานะส่งสรรพากร");

                //    foreach (ViewTaxCsvReport data in listData)
                //    {
                //        string issue_date = "";
                //        if (data.issue_date != null)
                //            issue_date = ((DateTime)data.issue_date).ToString("dd/MM/yyyy");
                //        else
                //            issue_date = "";

                //        string gen_xml_finish = "";
                //        if (data.gen_xml_finish != null)
                //            gen_xml_finish = ((DateTime)data.gen_xml_finish).ToString("dd/MM/yyyy HH:mm:ss");
                //        else
                //            gen_xml_finish = "";


                //        string status_email = "ยังไม่ส่ง";
                //        if (data.send_email_status == "success")
                //        {
                //            status_email = "ส่งแล้ว";
                //        }

                //        string status_ebxml = "ยังไม่ส่ง";
                //        if (data.send_ebxml_status == "success")
                //        {
                //            status_ebxml = "ส่งแล้ว";
                //        }

                //        outputFile.WriteLine(
                //            data.id.ToString() + "," +
                //            data.document_type_name + "," +
                //            data.etax_id + "," +
                //            "'" + data.buyer_tax_id + "," +
                //            data.buyer_name.Replace("\r\n", " ").Replace(",", " ") + "," +
                //            "'" + data.buyer_branch_code + "," +
                //            data.buyer_address.Replace("\r\n", " ").Replace(",", " ") + "," +
                //            issue_date + "," +
                //            gen_xml_finish + "," +
                //            data.original_price.ToString("0.00") + "," +
                //            data.price.ToString("0.00") + "," +
                //            Math.Abs(data.original_price - data.price).ToString("0.00") + "," +
                //            data.tax.ToString("0.00") + "," +
                //        data.total.ToString("0.00") + "," +
                //        status_email + "," +
                //        status_ebxml
                //            );
                //    }
                //    outputFile.WriteLine("");
                //    outputFile.WriteLine(",,,,,,,,รวม," + sumOriginalPrice.ToString("0.00") + "," + sumPrice.ToString("0.00") + "," + Math.Abs(sumOriginalPrice - sumPrice).ToString("0.00") + "," + sumTax.ToString("0.00") + "," + sumTotal.ToString("0.00"));
                //}
                //else
                //{
                outputFile.WriteLine("รหัสไฟล์,ประเภทเอกสาร,หมายเลขเอกสาร,วันที่ออกเอกสาร,หมายเลขเอกสารเดิม,วันที่ออกเอกสารเดิม,หมายเลขผู้เสียภาษี,ชื่อผู้เสียภาษี,สาขา,ที่อยู่,วันที่สร้าง,ยอดขาย,ส่วนลด,ยอดขายสุทธิ,ภาษี,รวม,สถานะส่งอีเมล,สถานะส่งสรรพากร,");

                List<string> checkList = new List<string>();

                foreach (ViewTaxCsvReport data in listData)
                {
                    string duplicate = "";
                    if (!checkList.Contains(data.etax_id))
                        checkList.Add(data.etax_id);
                    else
                        duplicate = "duplicate";

                    string issue_date = "";
                    if (data.issue_date != null)
                        issue_date = ((DateTime)data.issue_date).ToString("dd/MM/yyyy");
                    else
                        issue_date = "";

                    string ref_issue_date = "";
                    if (data.ref_issue_date != null)
                        ref_issue_date = ((DateTime)data.ref_issue_date).ToString("dd/MM/yyyy");
                    else
                        ref_issue_date = "";

                    string gen_xml_finish = "";
                    if (data.gen_xml_finish != null)
                        gen_xml_finish = ((DateTime)data.gen_xml_finish).ToString("dd/MM/yyyy HH:mm:ss");
                    else
                        gen_xml_finish = "";


                    string status_email = "ยังไม่ส่ง";
                    if (data.send_email_status == "success")
                    {
                        status_email = "ส่งแล้ว";
                    }

                    string status_ebxml = "ยังไม่ส่ง";
                    if (data.send_ebxml_status == "success")
                    {
                        status_ebxml = "ส่งแล้ว";
                    }

                    if (data.document_type_id == 3)
                    {
                        data.price = -data.price;
                        data.discount = -data.discount;
                        data.tax = -data.tax;
                        data.total = -data.total;
                    }

                    outputFile.WriteLine(
                        data.id.ToString() + "," +
                        data.document_type_name + "," +
                        data.etax_id + "," +
                        issue_date + "," +
                        data.ref_etax_id + "," +
                        ref_issue_date + "," +
                        "'" + data.buyer_tax_id + "," +
                        data.buyer_name.Replace("\r\n", " ").Replace(",", " ") + "," +
                        "'" + data.buyer_branch_code + "," +
                        data.buyer_address.Replace("\r\n", " ").Replace("\n", " ").Replace(",", " ") + "," +
                        gen_xml_finish + "," +
                        data.price.ToString("0.00") + "," +
                        data.discount.ToString("0.00") + "," +
                        (data.price - data.discount).ToString("0.00") + "," +
                        data.tax.ToString("0.00") + "," +
                    data.total.ToString("0.00") + "," +
                    status_email + "," +
                    status_ebxml + "," +
                    duplicate
                        );
                }
                outputFile.WriteLine("");
                outputFile.WriteLine(",,,,,,,,รวม," + sumPrice.ToString("0.00") + "," + sumDiscount.ToString("0.00") + "," + (sumPrice - sumDiscount).ToString("0.00") + "," + sumTax.ToString("0.00") + "," + sumTotal.ToString("0.00"));
                //}
            }
        }
        public static void DefaultEmailReport(string path, BodyDtParameters bodyDtParameters, List<ViewSendEmailList> listData)
        {
            using (StreamWriter outputFile = new StreamWriter(path, false, Encoding.UTF8))
            {
                outputFile.WriteLine("รหัสไฟล์,หมายเลขเอกสาร,วันที่ออกเอกสาร,หมายเลขเอกสารเดิม,วันที่ออกเอกสารเดิม,ประเภทเอกสาร,หมายเลขผู้เสียภาษี,ชื่อผู้เสียภาษี,อีเมล,วันที่ส่ง,");

                List<int> checkList = new List<int>();

                foreach (ViewSendEmailList data in listData)
                {
                    string duplicate = "";
                    if (!checkList.Contains(data.etax_file_id))
                        checkList.Add(data.etax_file_id);
                    else
                        duplicate = "duplicate";


                    string issue_date = "";
                    if (data.issue_date != null)
                        issue_date = ((DateTime)data.issue_date).ToString("dd/MM/yyyy");
                    else
                        issue_date = "";

                    string ref_etax_id = "";
                    if (data.ref_etax_id != null)
                        ref_etax_id = data.ref_etax_id;
                    else
                        ref_etax_id = "";

                    string ref_issue_date = "";
                    if (data.ref_issue_date != null)
                        ref_issue_date = ((DateTime)data.ref_issue_date).ToString("dd/MM/yyyy");
                    else
                        ref_issue_date = "";

                    string send_email_finish = "";
                    if (data.send_email_finish != null)
                        send_email_finish = ((DateTime)data.send_email_finish).ToString("dd/MM/yyyy HH:mm:ss");
                    else
                        send_email_finish = "";

                    string email = data.email;
                    if (email == null)
                        email = data.buyer_email;

                    outputFile.WriteLine(
                        data.etax_file_id.ToString() + "," +
                        data.etax_id.ToString() + "," +
                        issue_date + "," +
                        ref_etax_id + "," +
                        ref_issue_date + "," +
                        data.document_type_name + "," +
                        "'" + data.buyer_tax_id + "," +
                        data.buyer_name.Replace("\r\n", " ").Replace("\n", " ").Replace(",", " ") + "," +
                        email.Replace(",", " |") + "," +
                        send_email_finish + "," +
                        duplicate
                        );
                }
            }
        }
        public static void DefaultEbxmlReport(string path, BodyDtParameters bodyDtParameters, List<ViewSendEbxml> listData)
        {
            using (StreamWriter outputFile = new StreamWriter(path, false, Encoding.UTF8))
            {
                outputFile.WriteLine("หมายเลขเอกสาร,วันที่ออกเอกสาร,หมายเลขเอกสารเดิม,วันที่ออกเอกสารเดิม,ประเภทเอกสาร,หมายเลขผู้เสียภาษี,ชื่อผู้เสียภาษี,วันที่ส่ง");

                foreach (ViewSendEbxml data in listData)
                {
                    string issue_date = "";
                    if (data.issue_date != null)
                        issue_date = ((DateTime)data.issue_date).ToString("dd/MM/yyyy");
                    else
                        issue_date = "";

                    string ref_etax_id = "";
                    if (data.ref_etax_id != null)
                        ref_etax_id = data.ref_etax_id;
                    else
                        ref_etax_id = "";

                    string ref_issue_date = "";
                    if (data.ref_issue_date != null)
                        ref_issue_date = ((DateTime)data.ref_issue_date).ToString("dd/MM/yyyy");
                    else
                        ref_issue_date = "";

                    string send_ebxml_finish = "";
                    if (data.send_ebxml_finish != null)
                        send_ebxml_finish = ((DateTime)data.send_ebxml_finish).ToString("dd/MM/yyyy HH:mm:ss");
                    else
                        send_ebxml_finish = "";

                    outputFile.WriteLine(
                        data.etax_id.ToString() + "," +
                        issue_date + "," +
                        ref_etax_id + "," +
                        ref_issue_date + "," +
                        data.document_type_name + "," +
                        "'" + data.buyer_tax_id + "," +
                        data.buyer_name.Replace("\r\n", " ").Replace(",", " ") + "," +
                        send_ebxml_finish
                        );
                }
            }
        }
        public static void DefaultCancelReport(string path, BodyDtParameters bodyDtParameters, List<ViewTaxCsvReport> listData, double sumOriginalPrice, double sumPrice, double sumDiscount, double sumTax, double sumTotal)
        {
            using (StreamWriter outputFile = new StreamWriter(path, false, Encoding.UTF8))
            {
                outputFile.WriteLine("รหัสไฟล์,ประเภทเอกสาร,หมายเลขเอกสาร,วันที่ออกเอกสาร,หมายเลขเอกสารเดิม,วันที่ออกเอกสารเดิม,หมายเลขผู้เสียภาษี,ชื่อผู้เสียภาษี,สาขา,ที่อยู่,วันที่สร้าง,ยอดขาย,ส่วนลด,ยอดขายสุทธิ,ภาษี,รวม,สถานะส่งอีเมล,สถานะส่งสรรพากร,");

                List<string> checkList = new List<string>();

                foreach (ViewTaxCsvReport data in listData)
                {
                    string duplicate = "";
                    if (!checkList.Contains(data.etax_id))
                        checkList.Add(data.etax_id);
                    else
                        duplicate = "duplicate";

                    string issue_date = "";
                    if (data.issue_date != null)
                        issue_date = ((DateTime)data.issue_date).ToString("dd/MM/yyyy");
                    else
                        issue_date = "";

                    string ref_issue_date = "";
                    if (data.ref_issue_date != null)
                        ref_issue_date = ((DateTime)data.ref_issue_date).ToString("dd/MM/yyyy");
                    else
                        ref_issue_date = "";

                    string gen_xml_finish = "";
                    if (data.gen_xml_finish != null)
                        gen_xml_finish = ((DateTime)data.gen_xml_finish).ToString("dd/MM/yyyy HH:mm:ss");
                    else
                        gen_xml_finish = "";


                    string status_email = "ยังไม่ส่ง";
                    if (data.send_email_status == "success")
                    {
                        status_email = "ส่งแล้ว";
                    }

                    string status_ebxml = "ยังไม่ส่ง";
                    if (data.send_ebxml_status == "success")
                    {
                        status_ebxml = "ส่งแล้ว";
                    }

                    if (data.document_type_id == 3)
                    {
                        data.price = -data.price;
                        data.discount = -data.discount;
                        data.tax = -data.tax;
                        data.total = -data.total;
                    }

                    outputFile.WriteLine(
                        data.id.ToString() + "," +
                        data.document_type_name + "," +
                        data.etax_id + "," +
                        issue_date + "," +
                        data.ref_etax_id + "," +
                        ref_issue_date + "," +
                        "'" + data.buyer_tax_id + "," +
                        data.buyer_name.Replace("\r\n", " ").Replace(",", " ") + "," +
                        "'" + data.buyer_branch_code + "," +
                        data.buyer_address.Replace("\r\n", " ").Replace(",", " ") + "," +
                        gen_xml_finish + "," +
                        data.price.ToString("0.00") + "," +
                        data.discount.ToString("0.00") + "," +
                        (data.price - data.discount).ToString("0.00") + "," +
                        data.tax.ToString("0.00") + "," +
                    data.total.ToString("0.00") + "," +
                    status_email + "," +
                    status_ebxml + "," +
                    duplicate
                        );
                }
                outputFile.WriteLine("");
                outputFile.WriteLine(",,,,,,,,,,รวม," + sumPrice.ToString("0.00") + "," + sumDiscount.ToString("0.00") + "," + (sumPrice - sumDiscount).ToString("0.00") + "," + sumTax.ToString("0.00") + "," + sumTotal.ToString("0.00"));
            }
        }


        public static void DefaultTexReportOutsource(string path, BodyDtParameters bodyDtParameters, List<ViewTaxReportOutsource> listData, double sumOriginalPrice, double sumPrice, double sumDiscount, double sumTax, double sumTotal)
        {
            using (StreamWriter outputFile = new StreamWriter(path, false, Encoding.UTF8))
            {
                if (bodyDtParameters.docType == "2")
                {
                    outputFile.WriteLine("รหัสไฟล์,ประเภทเอกสาร,หมายเลขเอกสาร,หมายเลขผู้เสียภาษี,ชื่อผู้เสียภาษี,สาขา,ที่อยู่,ออกเอกสาร,วันที่สร้าง,มูลค่าเดิม,มูลค่าใหม่,ผลต่าง,ภาษี,รวม");

                    foreach (ViewTaxReportOutsource data in listData)
                    {
                        string issue_date = "";
                        if (data.issue_date != null)
                            issue_date = ((DateTime)data.issue_date).ToString("dd/MM/yyyy");
                        else
                            issue_date = "";

                        outputFile.WriteLine(
                            data.id.ToString() + "," +
                            data.document_type_name + "," +
                            data.etax_id + "," +
                            "'" + data.buyer_tax_id + "," +
                            data.buyer_name.Replace("\r\n", " ").Replace(",", " ") + "," +
                            "'" + data.buyer_branch_code + "," +
                            data.buyer_address.Replace("\r\n", " ").Replace(",", " ") + "," +
                            issue_date + "," +
                            data.create_date.ToString("dd/MM/yyyy") + "," +
                            data.original_price.ToString("0.00") + "," +
                            data.price.ToString("0.00") + "," +
                            Math.Abs(data.original_price - data.price).ToString("0.00") + "," +
                            data.tax.ToString("0.00") + "," +
                        data.total.ToString("0.00")
                            );
                    }
                    outputFile.WriteLine("");
                    outputFile.WriteLine(",,,,,,,,รวม," + sumOriginalPrice.ToString("0.00") + "," + sumPrice.ToString("0.00") + "," + Math.Abs(sumOriginalPrice - sumPrice).ToString("0.00") + "," + sumTax.ToString("0.00") + "," + sumTotal.ToString("0.00"));

                }
                else if (bodyDtParameters.docType == "3")
                {
                    outputFile.WriteLine("รหัสไฟล์,ประเภทเอกสาร,หมายเลขเอกสาร,หมายเลขผู้เสียภาษี,ชื่อผู้เสียภาษี,สาขา,ที่อยู่,ออกเอกสาร,วันที่สร้าง,มูลค่าเดิม,มูลค่าใหม่,ผลต่าง,ภาษี,รวม");

                    foreach (ViewTaxReportOutsource data in listData)
                    {
                        string issue_date = "";
                        if (data.issue_date != null)
                            issue_date = ((DateTime)data.issue_date).ToString("dd/MM/yyyy");
                        else
                            issue_date = "";

                        outputFile.WriteLine(
                            data.id.ToString() + "," +
                            data.document_type_name + "," +
                            data.etax_id + "," +
                            "'" + data.buyer_tax_id + "," +
                            data.buyer_name.Replace("\r\n", " ").Replace(",", " ") + "," +
                            "'" + data.buyer_branch_code + "," +
                            data.buyer_address.Replace("\r\n", " ").Replace(",", " ") + "," +
                            issue_date + "," +
                            data.create_date.ToString("dd/MM/yyyy") + "," +
                            data.original_price.ToString("0.00") + "," +
                            data.price.ToString("0.00") + "," +
                            Math.Abs(data.original_price - data.price).ToString("0.00") + "," +
                            data.tax.ToString("0.00") + "," +
                        data.total.ToString("0.00")
                            );
                    }
                    outputFile.WriteLine("");
                    outputFile.WriteLine(",,,,,,,,รวม," + sumOriginalPrice.ToString("0.00") + "," + sumPrice.ToString("0.00") + "," + Math.Abs(sumOriginalPrice - sumPrice).ToString("0.00") + "," + sumTax.ToString("0.00") + "," + sumTotal.ToString("0.00"));
                }
                else
                {
                    outputFile.WriteLine("รหัสไฟล์,ประเภทเอกสาร,หมายเลขเอกสาร,หมายเลขผู้เสียภาษี,ชื่อผู้เสียภาษี,สาขา,ที่อยู่,ออกเอกสาร,วันที่สร้าง,ยอดขาย,ส่วนลด,ยอดขายสุทธิ,ภาษี,รวม");

                    foreach (ViewTaxReportOutsource data in listData)
                    {
                        string issue_date = "";
                        if (data.issue_date != null)
                            issue_date = ((DateTime)data.issue_date).ToString("dd/MM/yyyy");
                        else
                            issue_date = "";


                        outputFile.WriteLine(
                            data.id.ToString() + "," +
                            data.document_type_name + "," +
                            data.etax_id + "," +
                            "'" + data.buyer_tax_id + "," +
                            data.buyer_name.Replace("\r\n", " ").Replace(",", " ") + "," +
                            "'" + data.buyer_branch_code + "," +
                            data.buyer_address.Replace("\r\n", " ").Replace(",", " ") + "," +
                            issue_date + "," +
                             data.create_date.ToString("dd/MM/yyyy") + "," +
                            data.price.ToString("0.00") + "," +
                            data.discount.ToString("0.00") + "," +
                            (data.price - data.discount).ToString("0.00") + "," +
                            data.tax.ToString("0.00") + "," +
                            data.total.ToString("0.00")
                            );
                    }
                    outputFile.WriteLine("");
                    outputFile.WriteLine(",,,,,,,,รวม," + sumPrice.ToString("0.00") + "," + sumDiscount.ToString("0.00") + "," + (sumPrice - sumDiscount).ToString("0.00") + "," + sumTax.ToString("0.00") + "," + sumTotal.ToString("0.00"));
                }
            }
        }


        public static void ThaibmaCostReport(string path, Member member, DateTime dateStart, DateTime dateEnd, ReturnCostReport returnCostReport)
        {
            using (StreamWriter outputFile = new StreamWriter(path, false, Encoding.UTF8))
            {

                string date = "";
                string dateStartM = dateStart.ToString("MMMM");
                string dateStartY = dateStart.ToString("yyyy");
                string dateEndM = dateEnd.ToString("MMMM");
                string dateEndY = dateEnd.ToString("yyyy");
                if (dateStartM == dateEndM && dateStartY == dateEndY)
                {
                    date = "เดือน " + dateStartM + " ปี " + dateStartY;
                }
                else
                {
                    date = "เดือน " + dateStartM + " ปี " + dateStartY + " ถึง เดือน " + dateEndM + " ปี " + dateEndY;
                }

                outputFile.WriteLine(member.name + ",,,,");

                outputFile.WriteLine("รายงานค่าใช้จ่าย,,,,");
                outputFile.WriteLine(date + ",,,,");
                outputFile.WriteLine(",,,,");
                outputFile.WriteLine("ผู้ประกอบการ : สมาคมตลาดตราสารหนี้ไทย,,,,เลขประจำตัวผู้เสียภาษีอากร :  0994000156065");
                outputFile.WriteLine("สถานประกอบการ : 900 อาคารต้นสนทาวเวอร์ ห้องเลขที่ 10A D ชั้น 10 ถนนเพลินจิต แขวงลุมพินี เขตปทุมวัน กรุงเทพฯ 10330,,,,สำนักงานใหญ่/สาขาที่ 00000");
                outputFile.WriteLine(",,,,");

                int xml_count_total = 0;
                int pdf_count_total = 0;
                int email_count_total = 0;
                int ebxml_count_total = 0;
                int sms_count_total = 0;
                double xml_price_total = 0;
                double pdf_price_total = 0;
                double email_price_total = 0;
                double ebxml_price_total = 0;
                double sms_price_total = 0;

                outputFile.WriteLine("การนำเข้า,สร้างไฟ XML,สร้างไฟ PDF,ส่งอีเมล,ส่งสรรพากร");

                foreach (ReturnCostReportData data in returnCostReport.listReturnCostReportData)
                {
                    outputFile.WriteLine(
                    data.row_name.ToString().Replace("\r\n", " ").Replace(",", " ") + "," +
                      data.xml_count.ToString() + "," +
                      data.pdf_count.ToString() + "," +
                      data.email_count.ToString() + "," +
                      data.ebxml_count.ToString()
                    );
                }

                xml_count_total = returnCostReport.total_xml_count;
                pdf_count_total = returnCostReport.total_pdf_count;
                email_count_total = returnCostReport.total_email_count;
                ebxml_count_total = returnCostReport.total_ebxml_count;
                sms_count_total = returnCostReport.total_sms_count;

                xml_price_total = returnCostReport.total_xml_price;
                pdf_price_total = returnCostReport.total_pdf_price;
                email_price_total = returnCostReport.total_email_price;
                ebxml_price_total = returnCostReport.total_ebxml_price;
                sms_price_total = returnCostReport.total_sms_price;

                outputFile.WriteLine("");
                outputFile.WriteLine("จำนวนรวม," + xml_count_total.ToString() + "," + pdf_count_total.ToString() + "," + email_count_total.ToString() + "," + ebxml_count_total.ToString());
                outputFile.WriteLine("ราคารวม," + xml_price_total.ToString("0.00") + "," + pdf_price_total.ToString("0.00") + "," + email_price_total.ToString("0.00") + "," + ebxml_price_total.ToString("0.00"));
                outputFile.WriteLine("ราคารวมทั้งหมด,,,," + (xml_price_total + pdf_price_total + email_price_total + ebxml_price_total).ToString("0.00"));
            }

        }
        public static void ThaibmaTaxReport(string path, BodyDtParameters bodyDtParameters, List<ViewTaxCsvReport> listData, double sumOriginalPrice, double sumPrice, double sumDiscount, double sumTax, double sumTotal)
        {
            using (StreamWriter outputFile = new StreamWriter(path, false, Encoding.UTF8))
            {

                string date = "";
                string dateStartM = bodyDtParameters.dateStart.ToString("MMMM");
                string dateStartY = bodyDtParameters.dateStart.ToString("yyyy");
                string dateEndM = bodyDtParameters.dateEnd.AddDays(-1).ToString("MMMM");
                string dateEndY = bodyDtParameters.dateEnd.AddDays(-1).ToString("yyyy");
                if (dateStartM == dateEndM && dateStartY == dateEndY)
                {
                    date = "เดือน " + dateStartM + " ปี " + dateStartY;
                }
                else
                {
                    date = "เดือน " + dateStartM + " ปี " + dateStartY + " ถึง เดือน " + dateEndM + " ปี " + dateEndY;
                }

                outputFile.WriteLine("สมาคมตลาดตราสารหนี้ไทย,,,,,,,,,,,,,");


                if (bodyDtParameters.docType == "DBN")
                {
                    outputFile.WriteLine("รายงานภาษีขาย (Debit Note Electronics),,,,,,,,,,,,,");
                    outputFile.WriteLine(date + ",,,,,,,,,,,,,");
                    outputFile.WriteLine(",,,,,,,,,,,,,");
                    outputFile.WriteLine("ผู้ประกอบการ : สมาคมตลาดตราสารหนี้ไทย,,,,,,,,,,,,,เลขประจำตัวผู้เสียภาษีอากร :  0994000156065");
                    outputFile.WriteLine("สถานประกอบการ : 900 อาคารต้นสนทาวเวอร์ ห้องเลขที่ 10A D ชั้น 10 ถนนเพลินจิต แขวงลุมพินี เขตปทุมวัน กรุงเทพฯ 10330,,,,,,,,,,,,,สำนักงานใหญ่/สาขาที่ 00000");
                    outputFile.WriteLine(",,,,,,,,,,,,,");


                    outputFile.WriteLine("รหัสไฟล์,ประเภทเอกสาร,หมายเลขเอกสาร,หมายเลขผู้เสียภาษี,ชื่อผู้เสียภาษี,สาขา,ที่อยู่,ออกเอกสาร,วันที่สร้าง,มูลค่าเดิม,มูลค่าใหม่,ผลต่าง,ภาษี,รวม,สถานะส่งอีเมล,สถานะส่งสรรพากร");

                    foreach (ViewTaxCsvReport data in listData)
                    {
                        string issue_date = "";
                        if (data.issue_date != null)
                            issue_date = ((DateTime)data.issue_date).ToString("dd/MM/yyyy");
                        else
                            issue_date = "";

                        string gen_xml_finish = "";
                        if (data.gen_xml_finish != null)
                            gen_xml_finish = ((DateTime)data.gen_xml_finish).ToString("dd/MM/yyyy HH:mm:ss");
                        else
                            gen_xml_finish = "";

                        string status_email = "ยังไม่ส่ง";
                        if (data.send_email_status == "success")
                        {
                            status_email = "ส่งแล้ว";
                        }

                        string status_ebxml = "ยังไม่ส่ง";
                        if (data.send_ebxml_status == "success")
                        {
                            status_ebxml = "ส่งแล้ว";
                        }

                        outputFile.WriteLine(
                        data.id.ToString() + "," +
                        data.document_type_name + "," +
                        data.etax_id + "," +
                        "'" + data.buyer_tax_id + "," +
                        data.buyer_name.Replace("\r\n", " ").Replace(",", " ") + "," +
                        "'" + data.buyer_branch_code + "," +
                        data.buyer_address.Replace("\r\n", " ").Replace(",", " ") + "," +
                        issue_date + "," +
                        gen_xml_finish + "," +
                        data.original_price.ToString("0.00") + "," +
                        data.price.ToString("0.00") + "," +
                        Math.Abs(data.original_price - data.price).ToString("0.00") + "," +
                        data.tax.ToString("0.00") + "," +
                        data.total.ToString("0.00") + "," +
                        status_email + "," +
                        status_ebxml
                        );
                    }
                    outputFile.WriteLine("");
                    outputFile.WriteLine(",,,,,,,,รวม," + sumOriginalPrice.ToString("0.00") + "," + sumPrice.ToString("0.00") + "," + Math.Abs(sumOriginalPrice - sumPrice).ToString("0.00") + "," + sumTax.ToString("0.00") + "," + sumTotal.ToString("0.00"));

                }
                else if (bodyDtParameters.docType == "CDN")
                {
                    outputFile.WriteLine("รายงานภาษีขาย (Credit Note Electronics),,,,,,,,,,,,,");
                    outputFile.WriteLine(date + ",,,,,,,,,,,,,");
                    outputFile.WriteLine(",,,,,,,,,,,,,");
                    outputFile.WriteLine("ผู้ประกอบการ : สมาคมตลาดตราสารหนี้ไทย,,,,,,,,,,,,,เลขประจำตัวผู้เสียภาษีอากร :  0994000156065");
                    outputFile.WriteLine("สถานประกอบการ : 900 อาคารต้นสนทาวเวอร์ ห้องเลขที่ 10A D ชั้น 10 ถนนเพลินจิต แขวงลุมพินี เขตปทุมวัน กรุงเทพฯ 10330,,,,,,,,,,,,,สำนักงานใหญ่/สาขาที่ 00000");
                    outputFile.WriteLine(",,,,,,,,,,,,,");


                    outputFile.WriteLine("รหัสไฟล์,ประเภทเอกสาร,หมายเลขเอกสาร,หมายเลขผู้เสียภาษี,ชื่อผู้เสียภาษี,สาขา,ที่อยู่,ออกเอกสาร,วันที่สร้าง,มูลค่าเดิม,มูลค่าใหม่,ผลต่าง,ภาษี,รวม,สถานะส่งอีเมล,สถานะส่งสรรพากร");

                    foreach (ViewTaxCsvReport data in listData)
                    {
                        string issue_date = "";
                        if (data.issue_date != null)
                            issue_date = ((DateTime)data.issue_date).ToString("dd/MM/yyyy");
                        else
                            issue_date = "";

                        string gen_xml_finish = "";
                        if (data.gen_xml_finish != null)
                            gen_xml_finish = ((DateTime)data.gen_xml_finish).ToString("dd/MM/yyyy HH:mm:ss");
                        else
                            gen_xml_finish = "";

                        string status_email = "ยังไม่ส่ง";
                        if (data.send_email_status == "success")
                        {
                            status_email = "ส่งแล้ว";
                        }

                        string status_ebxml = "ยังไม่ส่ง";
                        if (data.send_ebxml_status == "success")
                        {
                            status_ebxml = "ส่งแล้ว";
                        }

                        outputFile.WriteLine(
                            data.id.ToString() + "," +
                            data.document_type_name + "," +
                            data.etax_id + "," +
                            "'" + data.buyer_tax_id + "," +
                            data.buyer_name.Replace("\r\n", " ").Replace(",", " ") + "," +
                            "'" + data.buyer_branch_code + "," +
                            data.buyer_address.Replace("\r\n", " ").Replace(",", " ") + "," +
                            issue_date + "," +
                            gen_xml_finish + "," +
                            data.original_price.ToString("0.00") + "," +
                            data.price.ToString("0.00") + "," +
                            Math.Abs(data.original_price - data.price).ToString("0.00") + "," +
                            data.tax.ToString("0.00") + "," +
                            data.total.ToString("0.00") + "," +
                            status_email + "," +
                            status_ebxml
                            );
                    }
                    outputFile.WriteLine("");
                    outputFile.WriteLine(",,,,,,,,รวม," + sumOriginalPrice.ToString("0.00") + "," + sumPrice.ToString("0.00") + "," + Math.Abs(sumOriginalPrice - sumPrice).ToString("0.00") + "," + sumTax.ToString("0.00") + "," + sumTotal.ToString("0.00"));
                }
                else if (bodyDtParameters.docType == "TIV_Edit")
                {
                    outputFile.WriteLine("รายงานภาษีขาย(ใบแทน) (e-Tax invoice & e-Receipt),,,,,,,,,,,,,");
                    outputFile.WriteLine(date + ",,,,,,,,,,,,,");
                    outputFile.WriteLine(",,,,,,,,,,,,,");
                    outputFile.WriteLine("ผู้ประกอบการ : สมาคมตลาดตราสารหนี้ไทย,,,,,,,,,,,,,เลขประจำตัวผู้เสียภาษีอากร :  0994000156065");
                    outputFile.WriteLine("สถานประกอบการ : 900 อาคารต้นสนทาวเวอร์ ห้องเลขที่ 10A D ชั้น 10 ถนนเพลินจิต แขวงลุมพินี เขตปทุมวัน กรุงเทพฯ 10330,,,,,,,,,,,,,สำนักงานใหญ่/สาขาที่ 00000");
                    outputFile.WriteLine(",,,,,,,,,,,,,");


                    outputFile.WriteLine("รหัสไฟล์,ประเภทเอกสาร,หมายเลขเอกสาร,หมายเลขผู้เสียภาษี,ชื่อผู้เสียภาษี,สาขา,ที่อยู่,ออกเอกสาร,วันที่สร้าง,ยอดขาย,ส่วนลด,ยอดขายสุทธิ,ภาษี,รวม,สถานะส่งอีเมล,สถานะส่งสรรพากร");

                    foreach (ViewTaxCsvReport data in listData)
                    {
                        string issue_date = "";
                        if (data.issue_date != null)
                            issue_date = ((DateTime)data.issue_date).ToString("dd/MM/yyyy");
                        else
                            issue_date = "";

                        string gen_xml_finish = "";
                        if (data.gen_xml_finish != null)
                            gen_xml_finish = ((DateTime)data.gen_xml_finish).ToString("dd/MM/yyyy HH:mm:ss");
                        else
                            gen_xml_finish = "";

                        string status_email = "ยังไม่ส่ง";
                        if (data.send_email_status == "success")
                        {
                            status_email = "ส่งแล้ว";
                        }

                        string status_ebxml = "ยังไม่ส่ง";
                        if (data.send_ebxml_status == "success")
                        {
                            status_ebxml = "ส่งแล้ว";
                        }

                        outputFile.WriteLine(
                            data.id.ToString() + "," +
                            data.document_type_name + "," +
                            data.etax_id + "," +
                            "'" + data.buyer_tax_id + "," +
                            data.buyer_name.Replace("\r\n", " ").Replace(",", " ") + "," +
                            "'" + data.buyer_branch_code + "," +
                            data.buyer_address.Replace("\r\n", " ").Replace(",", " ") + "," +
                            issue_date + "," +
                            gen_xml_finish + "," +
                            data.price.ToString("0.00") + "," +
                            data.discount.ToString("0.00") + "," +
                            (data.price - data.discount).ToString("0.00") + "," +
                            data.tax.ToString("0.00") + "," +
                            data.total.ToString("0.00") + "," +
                            status_email + "," +
                            status_ebxml
                            );
                    }
                    outputFile.WriteLine("");
                    outputFile.WriteLine(",,,,,,,,รวม," + sumPrice.ToString("0.00") + "," + sumDiscount.ToString("0.00") + "," + (sumPrice - sumDiscount).ToString("0.00") + "," + sumTax.ToString("0.00") + "," + sumTotal.ToString("0.00"));
                }
                else
                {
                    outputFile.WriteLine("รายงานภาษีขาย (e-Tax invoice & e-Receipt),,,,,,,,,,,,,");
                    outputFile.WriteLine(date + ",,,,,,,,,,,,,");
                    outputFile.WriteLine(",,,,,,,,,,,,,");
                    outputFile.WriteLine("ผู้ประกอบการ : สมาคมตลาดตราสารหนี้ไทย,,,,,,,,,,,,,เลขประจำตัวผู้เสียภาษีอากร :  0994000156065");
                    outputFile.WriteLine("สถานประกอบการ : 900 อาคารต้นสนทาวเวอร์ ห้องเลขที่ 10A D ชั้น 10 ถนนเพลินจิต แขวงลุมพินี เขตปทุมวัน กรุงเทพฯ 10330,,,,,,,,,,,,,สำนักงานใหญ่/สาขาที่ 00000");
                    outputFile.WriteLine(",,,,,,,,,,,,,");


                    outputFile.WriteLine("รหัสไฟล์,ประเภทเอกสาร,หมายเลขเอกสาร,หมายเลขผู้เสียภาษี,ชื่อผู้เสียภาษี,สาขา,ที่อยู่,ออกเอกสาร,วันที่สร้าง,ยอดขาย,ส่วนลด,ยอดขายสุทธิ,ภาษี,รวม,สถานะส่งอีเมล,สถานะส่งสรรพากร");

                    foreach (ViewTaxCsvReport data in listData)
                    {
                        string issue_date = "";
                        if (data.issue_date != null)
                            issue_date = ((DateTime)data.issue_date).ToString("dd/MM/yyyy");
                        else
                            issue_date = "";

                        string gen_xml_finish = "";
                        if (data.gen_xml_finish != null)
                            gen_xml_finish = ((DateTime)data.gen_xml_finish).ToString("dd/MM/yyyy HH:mm:ss");
                        else
                            gen_xml_finish = "";

                        string status_email = "ยังไม่ส่ง";
                        if (data.send_email_status == "success")
                        {
                            status_email = "ส่งแล้ว";
                        }

                        string status_ebxml = "ยังไม่ส่ง";
                        if (data.send_ebxml_status == "success")
                        {
                            status_ebxml = "ส่งแล้ว";
                        }

                        outputFile.WriteLine(
                            data.id.ToString() + "," +
                            data.document_type_name + "," +
                            data.etax_id + "," +
                            "'" + data.buyer_tax_id + "," +
                            data.buyer_name.Replace("\r\n", " ").Replace(",", " ") + "," +
                            "'" + data.buyer_branch_code + "," +
                            data.buyer_address.Replace("\r\n", " ").Replace(",", " ") + "," +
                            issue_date + "," +
                            gen_xml_finish + "," +
                            data.price.ToString("0.00") + "," +
                            data.discount.ToString("0.00") + "," +
                            (data.price - data.discount).ToString("0.00") + "," +
                            data.tax.ToString("0.00") + "," +
                            data.total.ToString("0.00") + "," +
                            status_email + "," +
                            status_ebxml
                            );
                    }
                    outputFile.WriteLine("");
                    outputFile.WriteLine(",,,,,,,,รวม," + sumPrice.ToString("0.00") + "," + sumDiscount.ToString("0.00") + "," + (sumPrice - sumDiscount).ToString("0.00") + "," + sumTax.ToString("0.00") + "," + sumTotal.ToString("0.00"));
                }
            }
        }
        public static void ThaibmaEmailReport(string path, BodyDtParameters bodyDtParameters, List<ViewSendEmailList> listData)
        {
            using (StreamWriter outputFile = new StreamWriter(path, false, Encoding.UTF8))
            {

                string date = "";
                string dateStartM = bodyDtParameters.dateStart.ToString("MMMM");
                string dateStartY = bodyDtParameters.dateStart.ToString("yyyy");
                string dateEndM = bodyDtParameters.dateEnd.AddDays(-1).ToString("MMMM");
                string dateEndY = bodyDtParameters.dateEnd.AddDays(-1).ToString("yyyy");
                if (dateStartM == dateEndM && dateStartY == dateEndY)
                {
                    date = "เดือน " + dateStartM + " ปี " + dateStartY;
                }
                else
                {
                    date = "เดือน " + dateStartM + " ปี " + dateStartY + " ถึง เดือน " + dateEndM + " ปี " + dateEndY;
                }

                outputFile.WriteLine("สมาคมตลาดตราสารหนี้ไทย,,,,,");
                outputFile.WriteLine("รายงานการส่งอีเมล,,,,,");
                outputFile.WriteLine(date + ",,,,,");
                outputFile.WriteLine(",,,,,");
                outputFile.WriteLine("ผู้ประกอบการ : สมาคมตลาดตราสารหนี้ไทย,,,,,เลขประจำตัวผู้เสียภาษีอากร :  0994000156065");
                outputFile.WriteLine("สถานประกอบการ : 900 อาคารต้นสนทาวเวอร์ ห้องเลขที่ 10A D ชั้น 10 ถนนเพลินจิต แขวงลุมพินี เขตปทุมวัน กรุงเทพฯ 10330,,,,,สำนักงานใหญ่/สาขาที่ 00000");
                outputFile.WriteLine(",,,,,");


                outputFile.WriteLine("หมายเลขเอกสาร,ประเภทเอกสาร,หมายเลขผู้เสียภาษี,ชื่อผู้เสียภาษี,อีเมล,ออกเอกสาร,วันที่ส่ง");

                foreach (ViewSendEmailList data in listData)
                {
                    string issue_date = "";
                    if (data.issue_date != null)
                        issue_date = ((DateTime)data.issue_date).ToString("dd/MM/yyyy");
                    else
                        issue_date = "";

                    string send_email_finish = "";
                    if (data.send_email_finish != null)
                        send_email_finish = ((DateTime)data.send_email_finish).ToString("dd/MM/yyyy HH:mm:ss");
                    else
                        send_email_finish = "";

                    outputFile.WriteLine(
                        data.etax_id.ToString() + "," +
                        data.document_type_name + "," +
                        "'" + data.buyer_tax_id + "," +
                        data.buyer_name.Replace("\r\n", " ").Replace(",", " ") + "," +
                        data.email.Replace(",", " |") + "," +
                        issue_date + "," +
                        send_email_finish
                        );
                }
            }
        }
        public static void ThaibmaEbxmlReport(string path, BodyDtParameters bodyDtParameters, List<ViewSendEbxml> listData)
        {
            using (StreamWriter outputFile = new StreamWriter(path, false, Encoding.UTF8))
            {

                string date = "";
                string dateStartM = bodyDtParameters.dateStart.ToString("MMMM");
                string dateStartY = bodyDtParameters.dateStart.ToString("yyyy");
                string dateEndM = bodyDtParameters.dateEnd.AddDays(-1).ToString("MMMM");
                string dateEndY = bodyDtParameters.dateEnd.AddDays(-1).ToString("yyyy");
                if (dateStartM == dateEndM && dateStartY == dateEndY)
                {
                    date = "เดือน " + dateStartM + " ปี " + dateStartY;
                }
                else
                {
                    date = "เดือน " + dateStartM + " ปี " + dateStartY + " ถึง เดือน " + dateEndM + " ปี " + dateEndY;
                }

                outputFile.WriteLine("สมาคมตลาดตราสารหนี้ไทย,,,,,");
                outputFile.WriteLine("รายงานการส่งสรรพากร,,,,,");
                outputFile.WriteLine(date + ",,,,,");
                outputFile.WriteLine(",,,,,");
                outputFile.WriteLine("ผู้ประกอบการ : สมาคมตลาดตราสารหนี้ไทย,,,,,เลขประจำตัวผู้เสียภาษีอากร :  0994000156065");
                outputFile.WriteLine("สถานประกอบการ : 900 อาคารต้นสนทาวเวอร์ ห้องเลขที่ 10A D ชั้น 10 ถนนเพลินจิต แขวงลุมพินี เขตปทุมวัน กรุงเทพฯ 10330,,,,,สำนักงานใหญ่/สาขาที่ 00000");
                outputFile.WriteLine(",,,,,");


                outputFile.WriteLine("หมายเลขเอกสาร,ประเภทเอกสาร,หมายเลขผู้เสียภาษี,ชื่อผู้เสียภาษี,ออกเอกสาร,วันที่ส่ง");

                foreach (ViewSendEbxml data in listData)
                {
                    string issue_date = "";
                    if (data.issue_date != null)
                        issue_date = ((DateTime)data.issue_date).ToString("dd/MM/yyyy");
                    else
                        issue_date = "";

                    string send_ebxml_finish = "";
                    if (data.send_ebxml_finish != null)
                        send_ebxml_finish = ((DateTime)data.send_ebxml_finish).ToString("dd/MM/yyyy HH:mm:ss");
                    else
                        send_ebxml_finish = "";

                    outputFile.WriteLine(
                        data.etax_id.ToString() + "," +
                        data.document_type_name + "," +
                        "'" + data.buyer_tax_id + "," +
                        data.buyer_name.Replace("\r\n", " ").Replace(",", " ") + "," +
                        issue_date + "," +
                        send_ebxml_finish
                        );
                }
            }
        }

        public static void IsuzuTexReport(string path, BodyDtParameters bodyDtParameters, List<ViewTaxCsvReport> listData, double sumOriginalPrice, double sumPrice, double sumDiscount, double sumTax, double sumTotal)
        {
            using (StreamWriter outputFile = new StreamWriter(path, false, Encoding.UTF8))
            {
                outputFile.WriteLine("รหัสไฟล์,ประเภทเอกสาร,หมายเลขเอกสาร,หมายเลขผู้เสียภาษี,ชื่อผู้เสียภาษี,สาขา,ที่อยู่,ออกเอกสาร,วันที่สร้าง,ยอดขาย,ส่วนลด,ยอดขายสุทธิ,ภาษี,รวม,สถานะส่งอีเมล,สถานะส่งสรรพากร,");

                List<string> checkList = new List<string>();
                double sumTotalNoVat = 0;

                foreach (ViewTaxCsvReport data in listData)
                {
                    string duplicate = "";
                    if (!checkList.Contains(data.etax_id))
                        checkList.Add(data.etax_id);
                    else
                        duplicate = "duplicate";

                    string issue_date = "";
                    if (data.issue_date != null)
                        issue_date = ((DateTime)data.issue_date).ToString("dd/MM/yyyy");
                    else
                        issue_date = "";

                    string gen_xml_finish = "";
                    if (data.gen_xml_finish != null)
                        gen_xml_finish = ((DateTime)data.gen_xml_finish).ToString("dd/MM/yyyy HH:mm:ss");
                    else
                        gen_xml_finish = "";


                    string status_email = "ยังไม่ส่ง";
                    if (data.send_email_status == "success")
                    {
                        status_email = "ส่งแล้ว";
                    }

                    string status_ebxml = "ยังไม่ส่ง";
                    if (data.send_ebxml_status == "success")
                    {
                        status_ebxml = "ส่งแล้ว";
                    }

                    string[] other2Array = data.other2.Split('|');
                    double totalNoVat = double.Parse(other2Array[1]);

                    if (data.document_type_id == 3)
                    {
                        data.price = -data.price;
                        data.discount = -data.discount;
                        data.tax = -data.tax;
                        data.total = -data.total;
                        totalNoVat = -totalNoVat;
                    }

                    outputFile.WriteLine(
                        data.id.ToString() + "," +
                        data.document_type_name + "," +
                        data.etax_id + "," +
                        "'" + data.buyer_tax_id + "," +
                        data.buyer_name.Replace("\r\n", " ").Replace(",", " ") + "," +
                        "'" + data.buyer_branch_code + "," +
                        data.buyer_address.Replace("\r\n", " ").Replace(",", " ") + "," +
                        issue_date + "," +
                        gen_xml_finish + "," +
                        data.price.ToString("0.00") + "," +
                        data.discount.ToString("0.00") + "," +
                        totalNoVat.ToString("0.00") + "," +
                        data.tax.ToString("0.00") + "," +
                    data.total.ToString("0.00") + "," +
                    status_email + "," +
                    status_ebxml + "," +
                    duplicate
                        );

                    sumTotalNoVat += totalNoVat;
                }
                outputFile.WriteLine("");
                outputFile.WriteLine(",,,,,,,,รวม," + sumPrice.ToString("0.00") + "," + sumDiscount.ToString("0.00") + "," + sumTotalNoVat.ToString("0.00") + "," + sumTax.ToString("0.00") + "," + sumTotal.ToString("0.00"));
            }
        }

    }
}
