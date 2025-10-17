using System;
using System.ComponentModel.DataAnnotations;

namespace Clinic.Models
{
    public class PatientProfile
    {
        public int Id { get; set; }

        // mapping sang AspNetUsers
        [Required]
        public string UserId { get; set; }

        [StringLength(200)]
        public string FullName { get; set; }

        [StringLength(200)]
        public string Email { get; set; }          // <--- THÊM

        [StringLength(30)]
        public string PhoneNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }
        [StringLength(300)]
        public string Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // <--- THÊM
        public DateTime? UpdatedAt { get; set; }
    }
}
