using System;
using System.ComponentModel.DataAnnotations;

namespace Clinic.Models
{
    public class Doctor
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        [Display(Name = "Họ và tên")]
        public string Name { get; set; }

        [Required, StringLength(80)]
        [Display(Name = "Chuyên khoa")]
        public string Specialty { get; set; }

        [StringLength(256)]
        [Display(Name = "Ảnh")]
        public string PhotoUrl { get; set; }

        // --- Thông tin hồ sơ ---
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        [Display(Name = "Giới tính")] // "Nam" / "Nữ" / "Khác"
        public string Gender { get; set; }

        [EmailAddress, StringLength(120)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone, StringLength(30)]
        [Display(Name = "Điện thoại")]
        public string PhoneNumber { get; set; }

        // Liên kết tài khoản (nếu dùng Identity cho bác sĩ)
        public string UserId { get; set; }

        // Tuỳ chọn thêm:
        [Range(0, 60)]
        [Display(Name = "Số năm kinh nghiệm")]
        public int? YearsOfExperience { get; set; }

        [StringLength(800)]
        [Display(Name = "Giới thiệu")]
        public string Bio { get; set; }
    }
}
