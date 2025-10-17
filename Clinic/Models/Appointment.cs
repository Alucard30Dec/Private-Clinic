using System;
using System.ComponentModel.DataAnnotations;

namespace Clinic.Models
{
    public enum AppointmentStatus { Pending, Confirmed, Completed, Canceled, Rescheduled }

    public class Appointment
    {
        public int Id { get; set; }

        [Required] public int DoctorId { get; set; }
        [Required] public int ServiceId { get; set; }
        [Required] public int PatientProfileId { get; set; }

        [Required] public DateTime StartTime { get; set; }
        [Required] public DateTime EndTime { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        [StringLength(2000)]
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // === THÊM (không đổi schema) ===
        public virtual Doctor Doctor { get; set; }
        public virtual Service Service { get; set; }
        public virtual PatientProfile PatientProfile { get; set; }
    }
}
