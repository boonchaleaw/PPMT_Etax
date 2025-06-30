using System.ComponentModel.DataAnnotations;

namespace Etax_Api.Class.Database
{
    public class JobNoRunning
    {
        [Key]
        [MaxLength(6)]
        public string YearMonth { get; set; }

        public int LastNumber { get; set; }
    }
}
