using System;


namespace Etax_Api
{
    public class BodyMemberUserPermissionAdmin
    {
        public int id { get; set; }
        public bool per_branch_view { get; set; }
        public bool per_branch_manage { get; set; }
        public bool per_user_view { get; set; }
        public bool per_user_manage { get; set; }
        public bool per_raw_view { get; set; }
        public bool per_raw_manage { get; set; }
        public bool per_xml_view { get; set; }
        public bool per_pdf_view { get; set; }
        public bool per_email_view { get; set; }
        public bool per_ebxml_view { get; set; }
        public bool per_report_view { get; set; }

    }
}
