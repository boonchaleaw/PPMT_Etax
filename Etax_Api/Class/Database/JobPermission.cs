using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api.Class.Database
{
    public class JobPermission
    {
        public int Id { get; set; }
        public int? Requester { get; set; }
        public int? Approver { get; set; }
        public int? JobId { get; set; }
        public int? UserMemberId { get; set; }

        // Optional Navigation
        [ForeignKey(nameof(JobId))]
        public JobName? JobName { get; set; }
    }
}
