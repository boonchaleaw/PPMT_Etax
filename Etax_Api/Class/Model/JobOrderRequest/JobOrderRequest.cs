using System;
using System.Collections.Generic;

namespace Etax_Api.Class.Model.JobOrderRequest
{
    public class JobOrderRequest
    {
        public string JobName { get; set; }

        public string? WorkOrder { get; set; }

        public DateTime? Cycle { get; set; }

        public string Status { get; set; } = "Send"; // ค่าเริ่มต้น

        public int Total { get; set; }

        public int TotaPost { get; set; }

        public int TotalEmail { get; set; }

        public int TotalByHand { get; set; }

        public string JobNo { get; set; }

        public string? RevisedReason { get; set; }
        public List<string>? FilesToDelete { get; set; }
    }
}
