using System;

namespace Clinic.Areas.Doctors.Data
{
    public class AppointmentRowVM
    {
        public int Id { get; set; }
        public string PatientFullName { get; set; }
        public string ServiceFullName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Status { get; set; }
        public string Notes { get; set; }
    }

    public class AppointmentDetailVM : AppointmentRowVM
    {
        public string PatientPhone { get; set; }
        public string PatientEmail { get; set; }
        public string ServiceDesc { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
