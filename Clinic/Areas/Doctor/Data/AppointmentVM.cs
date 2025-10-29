using Clinic.Models; // Sử dụng các model gốc, không định nghĩa lại
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Clinic.Areas.Doctor.Data
{
    /// <summary>
    /// ViewModel đại diện cho một cuộc hẹn trong lịch trình của bác sĩ.
    /// </summary>
    public class AppointmentVM
    {
        public int Id { get; set; }

        [Display(Name = "Bệnh nhân")]
        public string PatientName { get; set; }

        [Display(Name = "Giờ bắt đầu")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime Start { get; set; }

        [Display(Name = "Giờ kết thúc")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime End { get; set; }

        [Display(Name = "Trạng thái")]
        public AppointmentStatus Status { get; set; } // Sử dụng enum từ Clinic.Models

        [Display(Name = "Loại khám")]
        public ExamType ExamType { get; set; } // Sử dụng enum từ Clinic.Models

        public int PatientId { get; set; }
    }

    /// <summary>
    /// ViewModel tóm tắt thông tin bệnh nhân cho bác sĩ xem.
    /// </summary>
    public class PatientSummaryVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NationalId { get; set; }
        public string PhoneNumber { get; set; }
    }

    //
    // LƯU Ý: 
    // Các định nghĩa trùng lặp cho:
    // - public enum AppointmentStatus { ... }
    // - public enum ExamType { ... }
    // - public class Appointment { ... }
    // - public static class EnumExtensions { ... }
    // ĐÃ BỊ XÓA KHỎI TỆP NÀY VÌ CHÚNG ĐÃ TỒN TẠI TRONG Clinic.Models
    //
}
