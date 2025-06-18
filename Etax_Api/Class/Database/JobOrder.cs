using System;

namespace Etax_Api.Class.Database
{
    public class JobOrder
    {
        public int Id { get; set; }
        public string JobNo { get; set; }
        public string JobName { get; set; }
        public string Status { get; set; }
        public string? StatusSendEmail { get; set; }
        public string? WorkOrder { get; set; }
        public string? SpecialOrders { get; set; }
        public string? RevisedReason { get; set; }
        public DateTime? Cycle { get; set; }
        public int Total { get; set; }
        public int TotaPost { get; set; }
        public int TotalEmail { get; set; }
        public int TotalByHand { get; set; }
        public int TotalEmailSuccess { get; set; }
        public int TotalEmailReject { get; set; }
        public DateTime DateCreate { get; set; }
        public string? CreateBy { get; set; }
        public string ApproveBy { get; set; }
        public string Archive { get; set; }
        public string? JobCode { get; set; }
        public int MemberId { get; set; }
        public string? FileLog { get; set; }
        public string? SummaryFile { get; set; }
        public int? SendBy { get; set; }
        public int? TotalCustomer { get; set; }
        public int? TotalPage { get; set; }
        public int? TotalSheet { get; set; }
        public string? PathReportLocal { get; set; }
        public double? PostCost { get; set; }
        public string? PathArchiveEmailLocal { get; set; }
        public int? IsRerun { get; set; }
        public string? RejectReport { get; set; }
        public string? status_log { get; set; }
        public DateTime? TimeLog { get; set; }
        public string? PathPostReport { get; set; }
        public string? TrackingPost { get; set; }
        public string? Ebxml { get; set; }
        public string? EbxmlReport { get; set; }
        public string? control_file_name { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public int? IsDelete { get; set; }
    }
}
