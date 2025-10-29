namespace Clinic.Migrations.IdentityMigrations
{
    using System.Data.Entity.Migrations;
    using System.Linq;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using Clinic.Models;
    using System; // Required for Exception
    // using System.Data.SqlClient; // No longer needed for SqlException check

    internal sealed class Configuration : DbMigrationsConfiguration<Clinic.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            MigrationsDirectory = @"Migrations\IdentityMigrations";
            // ContextKey = "Clinic.Models.ApplicationDbContext"; // Optional
        }

        protected override void Seed(Clinic.Models.ApplicationDbContext context)
        {
            System.Diagnostics.Debug.WriteLine("Starting Identity Seed..."); // Log start

            // 1) Roles
            var roleMgr = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            foreach (var r in new[] { "Admin", "Doctor", "Receptionist", "Patient" })
            {
                if (!roleMgr.RoleExists(r))
                {
                    var roleResult = roleMgr.Create(new IdentityRole(r));
                    if (!roleResult.Succeeded) throw new Exception($"Failed to create role {r}: {string.Join(", ", roleResult.Errors)}");
                    System.Diagnostics.Debug.WriteLine($"Created role: {r}");
                }
            }
            context.SaveChanges(); // Save roles first

            // 2) User Manager Setup
            var userStore = new UserStore<ApplicationUser>(context);
            var userMgr = new UserManager<ApplicationUser>(userStore);

            // *** TẠM THỜI NỚI LỎNG PasswordValidator TRONG SEED ***
            // Lưu ý: Cấu hình trong IdentityConfig.cs vẫn được áp dụng khi tạo user qua UI.
            userMgr.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6, // Giữ lại độ dài tối thiểu
                RequireDigit = false,
                RequireLowercase = false,
                RequireNonLetterOrDigit = false,
                RequireUppercase = false
            };
            // *** KẾT THÚC NỚI LỎNG ***

            // Helper function to ensure user exists, has password, and roles
            void EnsureEmailUser(string email, string password, params string[] roles)
            {
                ApplicationUser user = userMgr.FindByEmail(email);
                IdentityResult result;

                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Creating user: {email}");
                    user = new ApplicationUser
                    {
                        UserName = email, // QUAN TRỌNG: UserName phải là Email
                        Email = email,
                        EmailConfirmed = true // Xác nhận email luôn khi tạo trong Seed
                    };
                    result = userMgr.Create(user, password);
                    if (!result.Succeeded)
                    {
                        throw new Exception($"Seed user failed for {email}: {string.Join("; ", result.Errors)}");
                    }
                    System.Diagnostics.Debug.WriteLine($"User {email} created successfully.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"User {email} found.");
                    // Ensure UserName matches Email
                    if (user.UserName != email)
                    {
                        user.UserName = email;
                        result = userMgr.Update(user);
                        if (!result.Succeeded) System.Diagnostics.Debug.WriteLine($"Warning: Failed to update UserName for {email}: {string.Join("; ", result.Errors)}");
                        else System.Diagnostics.Debug.WriteLine($"Updated UserName for {email}.");
                    }

                    // Check if user has a password, add if missing
                    if (!userMgr.HasPassword(user.Id))
                    {
                        System.Diagnostics.Debug.WriteLine($"User {email} missing password. Adding default password...");
                        result = userMgr.AddPassword(user.Id, password);
                        if (!result.Succeeded)
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Failed to add password for {email}: {string.Join("; ", result.Errors)}");
                            // Don't throw exception here, maybe the password was set previously but HasPassword failed? Continue to role check.
                        }
                        else System.Diagnostics.Debug.WriteLine($"Added default password for {email}.");
                    }
                    // Optional: Reset password if it exists but might be wrong (use with caution)
                    // else {
                    //    string resetToken = userMgr.GeneratePasswordResetToken(user.Id);
                    //    result = userMgr.ResetPassword(user.Id, resetToken, password);
                    //    if (!result.Succeeded) Debug.WriteLine($"Warning: Failed to reset password for {email}: {string.Join("; ", result.Errors)}");
                    //    else Debug.WriteLine($"Reset password for {email}.");
                    // }
                }

                // Assign roles
                var currentRoles = userMgr.GetRoles(user.Id);
                foreach (var r in roles)
                {
                    if (!currentRoles.Contains(r))
                    {
                        result = userMgr.AddToRole(user.Id, r);
                        if (!result.Succeeded) System.Diagnostics.Debug.WriteLine($"Warning: Failed to add role '{r}' to {email}: {string.Join("; ", result.Errors)}");
                        else System.Diagnostics.Debug.WriteLine($"Added role '{r}' to {email}.");
                    }
                }
            } // End EnsureEmailUser

            // --- Seed Specific Users ---
            try
            {
                System.Diagnostics.Debug.WriteLine("Seeding Admin/Receptionist...");
                EnsureEmailUser("admin@clinic.local", "12345678", "Admin");
                EnsureEmailUser("recept01@clinic.local", "12345678", "Receptionist");
                context.SaveChanges(); // Save admin/receptionist

                System.Diagnostics.Debug.WriteLine("Seeding Doctors...");
                // *** ĐẢM BẢO SEED BÁC SĨ an@clinic.vn ***
                EnsureEmailUser("an@clinic.vn", "12345678", "Doctor");
                // Thêm các bác sĩ khác bạn muốn seed tài khoản Identity
                EnsureEmailUser("binh@clinic.vn", "12345678", "Doctor");
                EnsureEmailUser("chau@clinic.vn", "12345678", "Doctor");
                EnsureEmailUser("quan@clinic.vn", "12345678", "Doctor");
                EnsureEmailUser("trang@clinic.vn", "12345678", "Doctor");
                // ... (Thêm các email bác sĩ khác từ Clinic seed nếu cần)
                EnsureEmailUser("van@clinic.vn", "12345678", "Doctor");
                EnsureEmailUser("hung@clinic.vn", "12345678", "Doctor");
                EnsureEmailUser("ha@clinic.vn", "12345678", "Doctor");
                EnsureEmailUser("minh_tm@clinic.vn", "12345678", "Doctor");
                EnsureEmailUser("lan@clinic.vn", "12345678", "Doctor");
                EnsureEmailUser("phong@clinic.vn", "12345678", "Doctor");
                EnsureEmailUser("thao@clinic.vn", "12345678", "Doctor");
                EnsureEmailUser("khanh_ntq@clinic.vn", "12345678", "Doctor");
                EnsureEmailUser("dung_ctch@clinic.vn", "12345678", "Doctor");
                EnsureEmailUser("linh@clinic.vn", "12345678", "Doctor");
                context.SaveChanges(); // Save doctors

                System.Diagnostics.Debug.WriteLine("Seeding Patients...");
                EnsureEmailUser("patient01@clinic.local", "12345678", "Patient");
                EnsureEmailUser("patient02@clinic.local", "12345678", "Patient");
                context.SaveChanges(); // Save patients
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FATAL ERROR during User Seeding: {ex.Message}\n{ex.StackTrace}");
                // Ghi log lỗi chi tiết hơn
                // Ném lại lỗi để dừng quá trình Update-Database nếu việc seed user là bắt buộc
                throw;
            }

            // --- Link UserId back to Doctors Table (Improved Error Handling) ---
            System.Diagnostics.Debug.WriteLine("Attempting to link UserIds to Doctors table...");
            try
            {
                // Sử dụng DbContext riêng cho Clinic data
                using (var clinicDb = new Clinic.Models.ClinicDbContext())
                {
                    // Kiểm tra xem bảng Doctors có tồn tại không
                    bool doctorsTableExists = clinicDb.Database.SqlQuery<int>(
                      "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Doctors'"
                    ).FirstOrDefault() > 0;

                    if (!doctorsTableExists)
                    {
                        System.Diagnostics.Debug.WriteLine("Warning: dbo.Doctors table not found in ClinicDbContext. Skipping UserId linking.");
                        goto EndSeed; // Nhảy đến cuối nếu bảng không tồn tại
                    }


                    var doctorsToLink = clinicDb.Doctors
                                               .Where(d => d.Email != null && d.Email != "" && d.UserId == null) // Chỉ lấy những bác sĩ có email và chưa có UserId
                                               .ToList();

                    if (!doctorsToLink.Any())
                    {
                        System.Diagnostics.Debug.WriteLine("No Doctors found needing UserId link.");
                        goto EndSeed; // Nhảy đến cuối
                    }

                    System.Diagnostics.Debug.WriteLine($"Found {doctorsToLink.Count} doctors to potentially link.");
                    bool changesMade = false;
                    foreach (var doctor in doctorsToLink)
                    {
                        var identityUser = userMgr.FindByEmail(doctor.Email); // Tìm user Identity bằng email
                        if (identityUser != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Found Identity user {identityUser.Id} for Doctor {doctor.Id} ({doctor.Email}). Linking...");
                            doctor.UserId = identityUser.Id; // Gán UserId
                            clinicDb.Entry(doctor).State = System.Data.Entity.EntityState.Modified;
                            changesMade = true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Could not find Identity user for Doctor {doctor.Id} ({doctor.Email}). UserId remains null.");
                        }
                    }

                    if (changesMade)
                    {
                        int affectedRows = clinicDb.SaveChanges();
                        System.Diagnostics.Debug.WriteLine($"Saved UserId links to Doctors table. Affected rows: {affectedRows}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No changes made to Doctors table for UserId linking.");
                    }
                } // using clinicDb ends here
            }
            catch (System.Data.Entity.Core.EntityException ex) // Bắt lỗi cụ thể liên quan đến kết nối/schema CSDL
            {
                System.Diagnostics.Debug.WriteLine($"ERROR accessing ClinicDbContext (EntityException): {ex.Message}. Check connection string 'ClinicDb' and ensure migrations ran.");
                // Không ném lại lỗi, cho phép Identity seed hoàn thành
            }
            catch (Exception ex) // Bắt các lỗi khác
            {
                System.Diagnostics.Debug.WriteLine($"ERROR during UserId linking: {ex.Message}\n{ex.StackTrace}");
                // Không ném lại lỗi, cho phép Identity seed hoàn thành
            }

        EndSeed: // Nhãn để nhảy đến
            System.Diagnostics.Debug.WriteLine("Identity Seed finished.");
        } // End Seed method
    } // End Configuration class
} // End namespace
