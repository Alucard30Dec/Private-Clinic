using System;
using System.ComponentModel.DataAnnotations;

namespace Clinic.Models
{
    public class AppointmentRequest
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email"), EmailAddress, StringLength(200)]
        public string Email { get; set; }

        [Phone, StringLength(30)]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn chuyên khoa")]
        [StringLength(100)]
        public string Specialty { get; set; } // Thay thế cho Department

        [Required(ErrorMessage = "Vui lòng chọn ngày và giờ hẹn")]
        [DataType(DataType.DateTime)]
        public DateTime RequestedSlot { get; set; } // Thay thế cho DesiredDate

        [StringLength(2000)]
        public string Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsHandled { get; set; } = false; // lễ tân đã xử lý chưa
    }
}
