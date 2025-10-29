using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clinic.Models
{
    [Table("Specialties")] // Tên bảng trong CSDL
    public class Specialty
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên chuyên khoa.")]
        [StringLength(100)]
        [Display(Name = "Tên chuyên khoa")]
        public string Name { get; set; }

        // Thuộc tính cho Soft Delete
        public bool IsVisible { get; set; } = true; // Mặc định là hiển thị
    }
}
