using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Etax_Api.Class.Database
{
    public class JobName
    {

        public int Id { get; set; }


        public string ThaiName { get; set; }

        public string EngName { get; set; }


        public string JobCode { get; set; }

        public string? TempalteEmail { get; set; }

        public int MemberId { get; set; }


        public string? SenderEmail { get; set; }

        public string? SenderName { get; set; }

        public string? PathSftp { get; set; }
    }
}
