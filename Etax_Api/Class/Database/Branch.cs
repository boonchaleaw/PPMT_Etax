using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class Branch
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public string name { get; set; }
        public string name_en { get; set; }
        public string branch_code { get; set; }
        public string building_number { get; set; }
        public string building_name { get; set; }
        public string building_name_en { get; set; }
        public string street_name { get; set; }
        public string street_name_en { get; set; }
        public int district_code { get; set; }
        public string district_name { get; set; }
        public string district_name_en { get; set; }
        public int amphoe_code { get; set; }
        public string amphoe_name { get; set; }
        public string amphoe_name_en { get; set; }
        public int province_code { get; set; }
        public string province_name { get; set; }
        public string province_name_en { get; set; }
        public string zipcode { get; set; }
        public DateTime? update_date { get; set; }
        public DateTime? create_date { get; set; }
        public int delete_status { get; set; }

    }
}
