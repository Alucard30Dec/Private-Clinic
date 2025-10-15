using System.ComponentModel.DataAnnotations;

namespace Clinic.Models
{
    public class PatientProfile
    {
        public int Id { get; set; }
        [Required] public string FullName { get; set; }
        [Required] public string Phone { get; set; }
        public string UserId { get; set; }
    }
}
