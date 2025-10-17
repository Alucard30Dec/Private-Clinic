using System;
using System.ComponentModel.DataAnnotations;

namespace Clinic.Models
{
    public class AppointmentRequest
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; }

        [Phone, StringLength(30)]
        public string Phone { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime DesiredDate { get; set; }

        [StringLength(100)]
        public string Department { get; set; }

        [StringLength(2000)]
        public string Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsHandled { get; set; } = false; // lễ tân đã xử lý chưa
    }
}
