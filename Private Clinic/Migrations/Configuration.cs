using System.Data.Entity.Migrations;
using System.Linq;
using Private_Clinic.DAL;     // DbContext của bạn
using Private_Clinic.Models;  // AppUser, UserRole
using Private_Clinic.Helpers; // PasswordHelper

namespace Private_Clinic.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<Private_Clinic.DAL.ClinicDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false; // hoặc true nếu bạn muốn
        }

        protected override void Seed(ClinicDbContext db)
        {
            // SEED USERS (Admin + Receptionist + Doctor + Patient)
            if (!db.Users.Any())
            {
                db.Users.AddOrUpdate(u => u.UserName,
                    new AppUser
                    {
                        UserName = "admin",
                        Email = "admin@clinic.com",
                        PasswordHash = PasswordHelper.Hash("123456"),
                        Role = UserRole.Admin,
                        FullName = "Quản trị"
                    },
                    new AppUser
                    {
                        UserName = "reception",
                        Email = "reception@clinic.com",
                        PasswordHash = PasswordHelper.Hash("123456"),
                        Role = UserRole.Receptionist,
                        FullName = "Lễ tân"
                    },
                    new AppUser
                    {
                        UserName = "dr.tran",
                        Email = "dr.tran@clinic.com",
                        PasswordHash = PasswordHelper.Hash("123456"),
                        Role = UserRole.Doctor,
                        FullName = "BS Trần A"
                    },
                    new AppUser
                    {
                        UserName = "patient1",
                        Email = "p1@mail.com",
                        PasswordHash = PasswordHelper.Hash("123456"),
                        Role = UserRole.Patient,
                        FullName = "Nguyễn Văn P"
                    }
                );
            }

            // bạn có thể Seed thêm dữ liệu khác ở đây
        }
    }
}
