using System;
using System.Collections.Generic;

namespace Clinic.Areas.Doctor.Data
{
    // Bệnh nhân của tôi (list)
    public class MyPatientRowVM
    {
        public int PatientId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public DateTime? DOB { get; set; }
        public string Gender { get; set; } // Thêm giới tính
        public int TotalVisits { get; set; }
        public DateTime? LastVisit { get; set; }
        public int? Age { get; set; } // Thêm tuổi (tính toán)
    }

    // Hồ sơ chi tiết 1 bệnh nhân
    public class PatientDetailVM
    {
        public int PatientId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public DateTime? DOB { get; set; }
        public string Gender { get; set; } // Thêm giới tính
        public string BloodType { get; set; } // Thêm nhóm máu
        public string Address { get; set; } // Thêm địa chỉ
        public string MedicalHistory { get; set; } // Thêm tiền sử
        public string Allergies { get; set; } // Thêm dị ứng
        public string EmergencyContactName { get; set; } // Thêm liên hệ KC
        public string EmergencyContactPhone { get; set; } // Thêm SĐT liên hệ KC
        public int? Age { get; set; } // Thêm tuổi (tính toán)

        public IEnumerable<PatientVisitRowVM> Visits { get; set; }
    }

    public class PatientVisitRowVM
    {
        public int AppointmentId { get; set; }
        public string ServiceName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Status { get; set; } // Có thể đổi thành enum string nếu muốn
        public string Notes { get; set; }
        // Thêm các trường liên quan đến khám bệnh nếu cần (ví dụ: Chẩn đoán, Chỉ định...)
    }

    // Hồ sơ khám (toàn bộ lịch hẹn của bác sĩ) - Giữ nguyên
    public class RecordRowVM
    {
        public int AppointmentId { get; set; }
        public string PatientName { get; set; }
        public string ServiceName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Status { get; set; } // Có thể đổi thành enum string
        public string Notes { get; set; }
    }
}
