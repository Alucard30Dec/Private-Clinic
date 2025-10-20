using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clinic.Models
{
    [Table("Services")] // map đúng bảng trong DB
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; }

        // decimal(18,2) — nếu DB của bạn là money thì có thể đổi sang [Column(TypeName="money")]
        [Column(TypeName = "decimal")]
        public decimal Fee { get; set; }

        public int? DurationMinutes { get; set; }
    }
}
