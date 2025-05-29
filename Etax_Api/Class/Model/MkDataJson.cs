using System.Collections.Generic;

namespace Etax_Api
{
    public class MkDataJson
    {
        public string SellerTaxId { get; set; }
        public string SellerBranchId { get; set; }
        public string APIKey { get; set; }
        public string UserCode { get; set; }
        public string AccessKey { get; set; }
        public string ServiceCode { get; set; }
        public TextContentDataJson TextContent { get; set; }
        public string PDFContent { get; set; }
    }

    public class TextContentDataJson
    {
        public string C01_SELLER_TAX_ID { get; set; }
        public string C02_SELLER_BRANCH_ID { get; set; }

        public string H01_DOCUMENT_TYPE_CODE { get; set; }
        public string H02_DOCUMENT_NAME { get; set; }
        public string H03_DOCUMENT_ID { get; set; }
        public string H04_DOCUMENT_ISSUE_DTM { get; set; }
        public string H05_CREATE_PURPOSE_CODE { get; set; }
        public string H06_CREATE_PURPOSE { get; set; }
        public string H07_ADDITIONAL_REF_ASSIGN_ID { get; set; }
        public string H08_ADDITIONAL_REF_ISSUE_DTM { get; set; }
        public string H09_ADDITIONAL_REF_TYPE_CODE { get; set; }
        public string H11_DELIVERY_TYPE_CODE { get; set; }
        public string H12_BUYER_ORDER_ASSIGN_ID { get; set;}
        public string H26_SEND_MAIL_IND { get; set; }
        public string H28_RETURN_ORDER_NUMBER { get; set; }
        


        public string B01_BUYER_ID { get; set; }
        public string B02_BUYER_NAME { get; set; }
        public string B03_BUYER_TAX_ID_TYPE { get; set; }
        public string B04_BUYER_TAX_ID { get; set; }
        public string B05_BUYER_BRANCH_ID { get; set; }
        public string B08_BUYER_URIID { get; set; }
        public string B10_BUYER_POST_CODE { get; set; }
        public string B11_BUYER_BUILDING_NAME { get; set; }
        public string B12_BUYER_BUILDING_NO { get; set; }
        public string B13_BUYER_ADDRESS_LINE1 { get; set; }
        public string B14_BUYER_ADDRESS_LINE2 { get; set; }
        public string B15_BUYER_ADDRESS_LINE3 { get; set; }
        public string B16_BUYER_ADDRESS_LINE4 { get; set; }
        public string B17_BUYER_ADDRESS_LINE5 { get; set; }
        public string B18_BUYER_STREET_NAME { get; set; }
        public string B19_BUYER_CITY_SUB_DIV_ID { get; set; }
        public string B20_BUYER_CITY_SUB_DIV_NAME { get; set; }
        public string B21_BUYER_CITY_ID { get; set; }
        public string B22_BUYER_CITY_NAME { get; set; }
        public string B23_BUYER_COUNTRY_SUB_DIV_ID { get; set; }
        public string B24_BUYER_COUNTRY_SUB_DIV_NAME { get; set; }
        public string B25_BUYER_COUNTRY_ID { get; set; }

        public string F01_LINE_TOTAL_COUNT { get; set; }
        public string F04_TAX_TYPE_CODE1 { get; set; }
        public string F05_TAX_CAL_RATE1 { get; set; }
        public string F06_BASIS_AMOUNT1 { get; set; }
        public string F08_TAX_CAL_AMOUNT1 { get; set; }
        public string F12_BASIS_AMOUNT2 { get; set; }
        public string F29_ALLOWANCE_ACTUAL_AMOUNT { get; set; }
        public string F36_ORIGINAL_TOTAL_AMOUNT { get; set; }
        public string F38_LINE_TOTAL_AMOUNT { get; set; }
        public string F40_ADJUSTED_INFORMATION_AMOUNT { get; set; }
        public string F46_TAX_BASIS_TOTAL_AMOUNT { get; set; }
        public string F48_TAX_TOTAL_AMOUNT { get; set; }
        public string F50_GRAND_TOTAL_AMOUNT { get; set; }
        public List<LineItemDataJson> LINE_ITEM_INFORMATION { get; set; }

    }

    public class LineItemDataJson
    {
        public string L01_LINE_ID { get; set; }
        public string L02_PRODUCT_ID { get; set; }
        public string L03_PRODUCT_NAME { get; set; }
        public string L10_PRODUCT_CHARGE_AMOUNT { get; set; }
        public string L13_PRODUCT_ALLOWANCE_ACTUAL_AMOUNT { get; set; }
        public string L17_PRODUCT_QUANTITY { get; set; }
        public string L18_PRODUCT_UNIT_CODE { get; set; }
        public string L20_LINE_TAX_TYPE_CODE { get; set; }
        public string L21_LINE_TAX_CAL_RATE { get; set; }
        public string L24_LINE_TAX_CAL_AMOUNT { get; set; }
        public string L27_LINE_ALLOWANCE_ACTUAL_AMOUNT { get; set; }
        public string L33_LINE_NET_TOTAL_AMOUNT { get; set; }
        public string L37_PRODUCT_REMARK1 { get; set; }
        public string L38_PRODUCT_REMARK2 { get; set; }
    }
}
