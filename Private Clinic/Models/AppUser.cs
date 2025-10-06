using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Private_Clinic.Models
{
    public enum UserRole { Patient = 1, Receptionist = 2, Doctor = 3, Admin = 4 }

    [Table("AppUser")]
    public class AppUser
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        [Index("UX_AppUser_UserName", IsUnique = true)]
        public string UserName { get; set; }

        [Required, StringLength(150)]
        [Index("UX_AppUser_Email", IsUnique = true)]
        public string Email { get; set; }

        [Required] // lưu dạng salt:hash
        public string PasswordHash { get; set; }

        [Required]
        public UserRole Role { get; set; }

        [StringLength(120)]
        public string FullName { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(200)]
        public string Address { get; set; }
    }
}
