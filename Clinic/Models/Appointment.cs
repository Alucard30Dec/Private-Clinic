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
        public string Notes { get; set; }
    }
}
