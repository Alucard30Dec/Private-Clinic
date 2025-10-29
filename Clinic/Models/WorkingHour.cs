using System;
using System.ComponentModel.DataAnnotations.Schema; // Thêm để dùng ForeignKey

namespace Clinic.Models
{
    public class WorkingHour
    {
        public int Id { get; set; }

        // Khóa ngoại đến bảng Doctors
        [ForeignKey("Doctor")] // Chỉ rõ khóa ngoại này liên kết với thuộc tính Doctor bên dưới
        public int DoctorId { get; set; }

        public DayOfWeek DayOfWeek { get; set; } // Mon=1 … Sun=0
        public TimeSpan Start { get; set; }      // 08:00
        public TimeSpan End { get; set; }        // 17:00

        // *** THUỘC TÍNH ĐIỀU HƯỚNG ***
        public virtual Doctor Doctor { get; set; }
    }
}

