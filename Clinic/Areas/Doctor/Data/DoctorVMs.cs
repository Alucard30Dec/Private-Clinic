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
        public int TotalVisits { get; set; }
        public DateTime? LastVisit { get; set; }
    }

    // Hồ sơ chi tiết 1 bệnh nhân
    public class PatientDetailVM
    {
        public int PatientId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public DateTime? DOB { get; set; }

        public IEnumerable<PatientVisitRowVM> Visits { get; set; }
    }

    public class PatientVisitRowVM
    {
        public int AppointmentId { get; set; }
        public string ServiceName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Status { get; set; }
        public string Notes { get; set; }
    }

    // Hồ sơ khám (toàn bộ lịch hẹn của bác sĩ)
    public class RecordRowVM
    {
        public int AppointmentId { get; set; }
        public string PatientName { get; set; }
        public string ServiceName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Status { get; set; }
        public string Notes { get; set; }
    }
}
