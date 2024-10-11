using System;
using System.Collections.Generic;


namespace Etax_Api
{
    public class BodyMemberUserPermission
    {
        public bool per_branch_view { get; set; }
        public bool per_branch_manage { get; set; }
        public bool per_user_view { get; set; }
        public bool per_user_manage { get; set; }
        public bool per_raw_view { get; set; }
        public bool per_raw_manage { get; set; }
        public bool per_xml_view { get; set; }
        public bool per_xml_manage { get; set; }
        public bool per_pdf_view { get; set; }
        public bool per_email_view { get; set; }
        public bool per_email_manage { get; set; }
        public bool per_sms_view { get; set; }
        public bool per_sms_manage { get; set; }
        public bool per_ebxml_view { get; set; }
        public bool per_setting_manage { get; set; }
        public bool per_report_view { get; set; }
        public bool view_self_only { get; set; }
        public bool view_branch_only { get; set; }

        public List<BodyPermissionBranch> branchs { get; set; }

    }
}
