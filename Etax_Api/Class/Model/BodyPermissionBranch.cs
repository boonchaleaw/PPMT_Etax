using System;


namespace Etax_Api
{
    public class BodyPermissionBranch
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public string branch_code { get; set; }
        public string name { get; set; }
        public bool select { get; set; }
    }
}
