using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq; // <-- THÊM DÒNG NÀY
using System.Reflection; // <-- THÊM DÒNG NÀY (Cần cho GetCustomAttribute)

namespace Clinic.Models
{
    // Thêm Enum định nghĩa loại hình khám
    public enum ExamType
    {
        [Display(Name = "Khám Dịch vụ")]
        Service = 0, // Giá trị mặc định

        [Display(Name = "Khám BHYT")]
        HealthInsurance = 1
    }

    public enum AppointmentStatus { Pending, Confirmed, Completed, Canceled, Rescheduled }

    public class Appointment
    {
        public int Id { get; set; }

        [Required] public int DoctorId { get; set; }
        [Required] public int ServiceId { get; set; } // Giữ lại ServiceId để biết khám cụ thể gì
        [Required] public int PatientId { get; set; }

        [Required] public DateTime StartTime { get; set; }
        [Required] public DateTime EndTime { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        [Required]
        [Display(Name = "Loại hình khám")]
        public ExamType ExamType { get; set; } = ExamType.Service; // Mặc định là Khám Dịch vụ

        [StringLength(2000)]
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties (giữ nguyên)
        public virtual Doctor Doctor { get; set; }
        public virtual Service Service { get; set; }
        public virtual Patient Patient { get; set; }

        // Thuộc tính không map vào DB để hiển thị tên Enum (tùy chọn)
        [NotMapped]
        public string ExamTypeName => ExamType.GetType()
                                .GetMember(ExamType.ToString())
                                .First() // Bây giờ sẽ hoạt động
                                .GetCustomAttribute<DisplayAttribute>()?.GetName() ?? ExamType.ToString();
    }
}

