namespace Clinic.Migrations.IdentityMigrations
{
    using System.Data.Entity.Migrations;
    using System.Linq;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using Clinic.Models;
    using System; // Required for Exception
    using System.Data.SqlClient; // Required for SqlException

    internal sealed class Configuration : DbMigrationsConfiguration<Clinic.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            MigrationsDirectory = @"Migrations\IdentityMigrations";
        }

        protected override void Seed(Clinic.Models.ApplicationDbContext context)
        {
            // 1) Roles
            var roleMgr = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            foreach (var r in new[] { "Admin", "Doctor", "Receptionist", "Patient" })
                if (!roleMgr.RoleExists(r)) roleMgr.Create(new IdentityRole(r));

            // 2) Users + Roles
            var userMgr = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
            userMgr.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireDigit = false,
                RequireLowercase = false,
                RequireNonLetterOrDigit = false,
                RequireUppercase = false
            };

            void EnsureEmailUser(string email, string password, params string[] roles)
            {
                var user = userMgr.FindByEmail(email);
                if (user == null)
                {
                    user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                    var create = userMgr.Create(user, password);
                    if (!create.Succeeded)
                        throw new System.Exception("Seed user failed: " + string.Join("; ", create.Errors));
                }
                else
                {
                    // Ensure UserName matches Email if it already exists
                    if (user.UserName != email)
                    {
                        user.UserName = email;
                        var updateResult = userMgr.Update(user); // Use Update method
                        if (!updateResult.Succeeded)
                        {
                            // Log or handle the update failure if necessary
                            Console.WriteLine($"Failed to update UserName for {email}: {string.Join("; ", updateResult.Errors)}");
                        }
                    }
                }

                var currentRoles = userMgr.GetRoles(user.Id);
                foreach (var r in roles)
                    if (!currentRoles.Contains(r))
                        userMgr.AddToRole(user.Id, r);
            }

            // Admin + Lễ tân
            EnsureEmailUser("admin@clinic.local", "12345678", "Admin");
            EnsureEmailUser("recept01@clinic.local", "12345678", "Receptionist");

            // *** START MODIFICATION: Make Doctor seeding safer ***
            // 3) Seed 15 bác sĩ trùng email trong bảng Doctors (*.vn) + gán UserId
            try
            {
                // Check if the Doctors table exists before trying to query it
                // This requires running raw SQL because DbContext might throw if the table doesn't exist
                bool doctorsTableExists = context.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Doctors'"
                ).FirstOrDefault() > 0;

                if (doctorsTableExists)
                {
                    // Use a separate DbContext for Clinic data, ONLY if the table exists
                    using (var clinic = new Clinic.Models.ClinicDbContext())
                    {
                        var doctorEmails = clinic.Doctors
                            .Where(d => d.Email != null && d.Email != "")
                            .Select(d => d.Email)
                            .Distinct()
                            .ToList();

                        foreach (var email in doctorEmails)
                            EnsureEmailUser(email, "12345678", "Doctor");

                        // Đồng bộ UserId + set Password (thuộc tính trên Doctor) = '12345678' cho khớp mẫu
                        var doctors = clinic.Doctors.ToList();
                        bool changesMade = false;
                        foreach (var d in doctors.Where(x => !string.IsNullOrEmpty(x.Email)))
                        {
                            var u = userMgr.FindByEmail(d.Email);
                            if (u != null)
                            {
                                if (d.UserId != u.Id)
                                {
                                    d.UserId = u.Id;
                                    changesMade = true;
                                }
                                // Only set default password if it's currently null/empty
                                if (string.IsNullOrEmpty(d.Password))
                                {
                                    d.Password = "12345678";
                                    changesMade = true;
                                }
                            }
                        }
                        if (changesMade)
                        {
                            clinic.SaveChanges();
                        }
                    }
                }
                else
                {
                    // Log or output a warning that the Doctors table doesn't exist yet
                    Console.WriteLine("Warning: dbo.Doctors table not found during Identity seeding. Skipping Doctor user creation and linking.");
                }
            }
            catch (Exception ex) // Catch potential exceptions during ClinicDbContext access
            {
                // Log the exception details for debugging
                Console.WriteLine($"Error during Doctor seeding/linking in Identity Configuration: {ex.Message}");
                // Optionally re-throw if this is critical, or just let Identity seeding continue without doctor linking
                // throw; // Uncomment to make the seeding fail if doctor linking fails
            }
            // *** END MODIFICATION ***

            // 4) Bệnh nhân mẫu
            EnsureEmailUser("patient01@clinic.local", "12345678", "Patient");
            EnsureEmailUser("patient02@clinic.local", "12345678", "Patient");
        }
    }
}
