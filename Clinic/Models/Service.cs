using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clinic.Models
{
    [Table("Services")]
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(200)]
        [Display(Name = "Tên dịch vụ")]
        public string Name { get; set; }

        [Column(TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
        [Display(Name = "Chi phí")]
        public decimal Fee { get; set; }

        [Required]
        [Display(Name = "Loại hình")]
        public ExamType ExamType { get; set; } = ExamType.Service;

        [Display(Name = "Thời lượng (phút)")]
        public int? DurationMinutes { get; set; }

        // Thuộc tính cho Soft Delete
        public bool IsVisible { get; set; } = true; // Mặc định là hiển thị
    }
}
