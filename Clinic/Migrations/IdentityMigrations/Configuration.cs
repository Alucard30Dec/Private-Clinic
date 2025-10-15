namespace Clinic.Migrations.IdentityMigrations
{
    using System.Data.Entity.Migrations;
    using System.Linq;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using Clinic.Models;

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
                // Username = Email (chuẩn hoá)
                var user = userMgr.FindByEmail(email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };
                    var create = userMgr.Create(user, password);
                    if (!create.Succeeded)
                        throw new System.Exception("Seed user failed: " + string.Join("; ", create.Errors));
                }
                else
                {
                    // nếu trước đây UserName khác Email -> đồng bộ
                    if (user.UserName != email)
                    {
                        user.UserName = email;
                        context.SaveChanges();
                    }
                }

                // gán role nếu thiếu
                var currentRoles = userMgr.GetRoles(user.Id);
                foreach (var r in roles)
                    if (!currentRoles.Contains(r))
                        userMgr.AddToRole(user.Id, r);
            }

            // Admin + Lễ tân
            EnsureEmailUser("admin@clinic.local", "12345678", "Admin");
            EnsureEmailUser("recept01@clinic.local", "12345678", "Receptionist");

            // 5 bác sĩ (Topic 2)
            EnsureEmailUser("dr.john@clinic.local", "12345678", "Doctor");
            EnsureEmailUser("dr.anna@clinic.local", "12345678", "Doctor");
            EnsureEmailUser("dr.mike@clinic.local", "12345678", "Doctor");
            EnsureEmailUser("dr.sara@clinic.local", "12345678", "Doctor");
            EnsureEmailUser("dr.david@clinic.local", "12345678", "Doctor");

            // 2 bệnh nhân mẫu
            EnsureEmailUser("patient01@clinic.local", "12345678", "Patient");
            EnsureEmailUser("patient02@clinic.local", "12345678", "Patient");
        }
    }
}
