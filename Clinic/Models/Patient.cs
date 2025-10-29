using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Thêm để dùng NotMapped

namespace Clinic.Models
{
    public class Patient
    {
        public int Id { get; set; }

        // Liên kết với AspNetUsers (có thể null nếu là bệnh nhân vãng lai)
        public string UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên bệnh nhân.")]
        [StringLength(200)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [StringLength(200)]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")] // Thêm Required
        [StringLength(30)]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(300)]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        // --- Thuộc tính mới ---
        [StringLength(10)]
        [Display(Name = "Giới tính")]
        public string Gender { get; set; } // "Nam", "Nữ", "Khác"

        [StringLength(5)]
        [Display(Name = "Nhóm máu")]
        public string BloodType { get; set; } // A+, A-, B+, B-, AB+, AB-, O+, O-

        [DataType(DataType.MultilineText)]
        [Display(Name = "Tiền sử bệnh")]
        public string MedicalHistory { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Dị ứng")]
        public string Allergies { get; set; }

        [StringLength(200)]
        [Display(Name = "Người liên hệ khẩn cấp")]
        public string EmergencyContactName { get; set; }

        [StringLength(30)]
        [Phone(ErrorMessage = "Số điện thoại liên hệ khẩn cấp không hợp lệ.")]
        [Display(Name = "SĐT liên hệ khẩn cấp")]
        public string EmergencyContactPhone { get; set; }
        // --- Kết thúc thuộc tính mới ---

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // --- Helper không map CSDL ---
        [NotMapped]
        [Display(Name = "Tuổi")]
        public int? Age
        {
            get
            {
                if (!DateOfBirth.HasValue) return null;
                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Value.Year;
                if (DateOfBirth.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }
    }
}
