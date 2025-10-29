using Clinic.Models; // For AppointmentStatus
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Clinic.Areas.Doctor.Data
{
    // ViewModel for the Doctor's patient list (/Doctor/Patients/Index)
    public class MyPatientRowVM
    {
        public int PatientId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public DateTime? DOB { get; set; }
        public string Gender { get; set; }
        public int TotalVisits { get; set; }
        public DateTime? LastVisit { get; set; }

        // Calculated property (set in controller)
        [Display(Name = "Tuổi")]
        public int? Age { get; set; }
    }

    // ViewModel for a single visit row in patient details (/Doctor/Patients/Details)
    public class PatientVisitRowVM
    {
        public int AppointmentId { get; set; }
        public string ServiceName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Status { get; set; } // Keep as int to match controller query
        public string Notes { get; set; }
    }

    // ViewModel for patient details (/Doctor/Patients/Details)
    public class PatientDetailVM
    {
        public int PatientId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public DateTime? DOB { get; set; }
        public string Gender { get; set; }
        public string BloodType { get; set; }
        public string Address { get; set; }
        public string MedicalHistory { get; set; }
        public string Allergies { get; set; }
        public string EmergencyContactName { get; set; }
        public string EmergencyContactPhone { get; set; }

        // Calculated property (set in controller)
        [Display(Name = "Tuổi")]
        public int? Age { get; set; }

        // List of past visits with this doctor
        public List<PatientVisitRowVM> Visits { get; set; } = new List<PatientVisitRowVM>();
    }

    // ViewModel for the doctor's appointment records list (/Doctor/Records/Index)
    public class RecordRowVM
    {
        public int AppointmentId { get; set; }
        public string PatientName { get; set; }
        public string ServiceName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Status { get; set; } // Keep as int to match controller query
        public string Notes { get; set; }
    }

    // ViewModel for the doctor's schedule list (/Doctor/Schedules/Index)
    public class AppointmentRowVM
    {
        public int Id { get; set; }
        public string PatientFullName { get; set; }
        public string ServiceFullName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Status { get; set; } // Keep as int to match controller query
        public string Notes { get; set; }
    }

    // ViewModel for appointment details (/Doctor/Schedules/Details)
    public class AppointmentDetailVM
    {
        public int Id { get; set; }
        public string PatientFullName { get; set; }
        public string PatientPhone { get; set; }
        public string PatientEmail { get; set; }
        public string ServiceFullName { get; set; }
        public string ServiceDesc { get; set; } // Add this if needed from Service model
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Status { get; set; } // Keep as int to match controller query
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
