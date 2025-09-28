using System.ComponentModel.DataAnnotations;

namespace Private_Clinic.Models
{
    public class QuickRegisterVM
    {
        [Required(ErrorMessage = "Vui lòng nhập Họ và tên")]
        [StringLength(100, ErrorMessage = "Họ và tên quá dài")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ (phải chứa @)")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Số điện thoại")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có 10-11 số")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Địa chỉ")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Lời nhắn")]
        [StringLength(500, ErrorMessage = "Lời nhắn tối đa 500 ký tự")]
        public string Note { get; set; }
    }
}
