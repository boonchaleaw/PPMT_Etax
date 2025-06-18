using System.ComponentModel.DataAnnotations;
using System;

namespace Etax_Api.Class.Database
{
    public class JobFile
    {

        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string FileName { get; set; }

        [Required]
        [MaxLength(10)]
        public string Status { get; set; } = "Active"; // ค่าเริ่มต้น

        [Required]
        [MaxLength(30)]
        public string Policy { get; set; } = "Customer"; // ค่าเริ่มต้น

        [Required]
        public int JobId { get; set; }

        [Required]
        [MaxLength(200)]
        public string UploadName { get; set; }

        public DateTime? DateModify { get; set; }

        public long? Size { get; set; }

        [MaxLength(200)]
        public string? LocalPath { get; set; }
    }
}
