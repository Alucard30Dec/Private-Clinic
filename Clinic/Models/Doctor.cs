using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Thêm để dùng ForeignKey

namespace Clinic.Models
{
    public class Doctor
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        [Display(Name = "Họ và tên")]
        public string Name { get; set; }

        // --- THAY ĐỔI: Sử dụng Khóa ngoại ---
        [Required(ErrorMessage = "Vui lòng chọn chuyên khoa.")]
        [Display(Name = "Chuyên khoa")]
        [ForeignKey("Specialty")] // Liên kết với navigation property Specialty
        public int SpecialtyId { get; set; }
        // --- KẾT THÚC THAY ĐỔI ---

        [StringLength(256)]
        [Display(Name = "Ảnh")]
        public string PhotoUrl { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        [Display(Name = "Giới tính")]
        public string Gender { get; set; }

        [EmailAddress, StringLength(120)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone, StringLength(30)]
        [Display(Name = "Điện thoại")]
        public string PhoneNumber { get; set; }

        [StringLength(20)]
        [Display(Name = "Số CCCD")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Số CCCD chỉ được chứa chữ số.")]
        public string NationalId { get; set; }

        [StringLength(300)]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        public string UserId { get; set; }

        [Range(0, 60)]
        [Display(Name = "Số năm kinh nghiệm")]
        public int? YearsOfExperience { get; set; }

        [StringLength(800)]
        [Display(Name = "Giới thiệu")]
        public string Bio { get; set; }

        // Thuộc tính cho Soft Delete
        public bool IsVisible { get; set; } = true; // Mặc định là hiển thị

        // --- THÊM: Navigation property cho Specialty ---
        public virtual Specialty Specialty { get; set; }
        // --- KẾT THÚC THÊM ---

        // >>> Chỉ seed demo (KHÔNG dùng để xác thực thực tế)
        [StringLength(128)]
        [Display(Name = "Mật khẩu mặc định")]
        public string Password { get; set; } // Giữ lại nếu bạn vẫn cần seed password
    }
}
