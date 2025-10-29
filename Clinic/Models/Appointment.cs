using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection; // Keep for Attribute retrieval

// Define Enums and Appointment class ONCE within this namespace
namespace Clinic.Models
{
    public enum ExamType
    {
        [Display(Name = "Khám Dịch vụ")]
        Service = 0,
        [Display(Name = "Khám BHYT")]
        HealthInsurance = 1
    }

    public enum AppointmentStatus { Pending, Confirmed, Completed, Canceled, Rescheduled }

    public class Appointment
    {
        public int Id { get; set; }
        [Required] public int DoctorId { get; set; }
        [Required] public int ServiceId { get; set; }
        [Required] public int PatientId { get; set; }
        [Required] public DateTime StartTime { get; set; }
        [Required] public DateTime EndTime { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        [Required]
        [Display(Name = "Loại hình khám")]
        public ExamType ExamType { get; set; } = ExamType.Service; // Use the enum defined above

        [StringLength(2000)]
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Doctor Doctor { get; set; }
        public virtual Service Service { get; set; }
        public virtual Patient Patient { get; set; }

        [NotMapped]
        public string ExamTypeName => ExamType.GetDisplayName();
    }

    // Define EnumExtensions ONCE
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            try
            {
                var memberInfo = enumValue.GetType()
                                    .GetMember(enumValue.ToString())
                                    .FirstOrDefault(); // Use FirstOrDefault for safety

                if (memberInfo != null)
                {
                    // *** FIX: Use older Attribute.GetCustomAttribute syntax if GetCustomAttribute<T> fails ***
                    var displayAttribute = (DisplayAttribute)Attribute.GetCustomAttribute(memberInfo, typeof(DisplayAttribute));
                    // If the above still fails, ensure project targets .NET 4.5+

                    if (displayAttribute != null && !string.IsNullOrEmpty(displayAttribute.GetName()))
                    {
                        return displayAttribute.GetName();
                    }
                }
                return enumValue.ToString(); // Fallback if no attribute or member found
            }
            catch
            {
                return enumValue.ToString(); // Fallback on any error
            }
        }
    }
}