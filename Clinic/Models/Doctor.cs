using System.ComponentModel.DataAnnotations;

namespace Clinic.Models
{
    public class Doctor
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; }
        [Required] public string Specialty { get; set; }
        public string PhotoUrl { get; set; }
        public string UserId { get; set; } // map tài khoản bác sĩ (nếu cần)
    }
}
