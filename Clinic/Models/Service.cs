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

        [Column(TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)] // Format số tiền
        public decimal Fee { get; set; } // Giá khám dịch vụ

        // *** THÊM THUỘC TÍNH NÀY ***
        [Required]
        [Display(Name = "Loại hình")]
        public ExamType ExamType { get; set; } = ExamType.Service; // Mặc định

        public int? DurationMinutes { get; set; }

        // Optional: Thêm giá BHYT nếu cần quản lý riêng
        // [Column(TypeName = "decimal")]
        // [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
        // public decimal? InsuranceFee { get; set; }
    }
}
