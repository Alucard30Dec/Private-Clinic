using Clinic.Models; // For ApplicationUser, ApplicationDbContext
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.Identity.EntityFramework; // For RoleManager
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations; // For ViewModels

namespace Clinic.Areas.Admin.Controllers
{
    // ViewModels specific to Receptionist Management
    public class ReceptionistListViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime? LockoutEndDateUtc { get; set; }
    }

    public class ReceptionistCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(100, ErrorMessage = "{0} phải dài ít nhất {2} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
        public string ConfirmPassword { get; set; }
    }

    public class ReceptionistEditViewModel
    {
        public string Id { get; set; } // Hidden

        [Required]
        [EmailAddress]
        [Display(Name = "Email (Không thể thay đổi)")]
        public string Email { get; set; }

        [StringLength(100, ErrorMessage = "{0} phải dài ít nhất {2} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới (Để trống nếu không đổi)")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [System.ComponentModel.DataAnnotations.Compare("NewPassword", ErrorMessage = "Mật khẩu mới và xác nhận mật khẩu không khớp.")]
        public string ConfirmPassword { get; set; }

        // Optional: Add Lockout management fields if needed
        [Display(Name = "Khóa tài khoản?")]
        public bool IsLockedOut { get; set; }
    }


    [Authorize(Roles = "Admin")]
    public class ReceptionistsController : Controller
    {
        private ApplicationUserManager _userManager;
        // private RoleManager<IdentityRole> _roleManager; // Get RoleManager if needed

        public ApplicationUserManager UserManager
        {
            get => _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            private set => _userManager = value;
        }

        /* // Initialize RoleManager if needed
        public RoleManager<IdentityRole> RoleManager
        {
            get => _roleManager ?? new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(HttpContext.GetOwinContext().Get<ApplicationDbContext>()));
            private set => _roleManager = value;
        }
        */

        // GET: Admin/Receptionists
        public async Task<ActionResult> Index()
        {
            ViewBag.Nav = "receptionists"; // For layout menu
            var receptionistRoleId = (await UserManager.FindByNameAsync("recept01@clinic.local"))?.Roles.FirstOrDefault()?.RoleId; // Find role Id dynamically if needed or hardcode
                                                                                                                                   // Assuming RoleManager is initialized:
                                                                                                                                   // var receptionistRole = await RoleManager.FindByNameAsync("Receptionist");
                                                                                                                                   // if (receptionistRole == null) { /* Handle role not found */ }
                                                                                                                                   // var userIdsInRole = receptionistRole.Users.Select(u => u.UserId).ToList();
                                                                                                                                   // var users = await UserManager.Users.Where(u => userIdsInRole.Contains(u.Id)).ToListAsync();

            // Simpler approach: Iterate through all users and check roles (might be slower for many users)
            var allUsers = await UserManager.Users.ToListAsync();
            var receptionists = new List<ReceptionistListViewModel>();
            foreach (var user in allUsers)
            {
                if (await UserManager.IsInRoleAsync(user.Id, "Receptionist"))
                {
                    receptionists.Add(new ReceptionistListViewModel
                    {
                        Id = user.Id,
                        Email = user.Email,
                        IsLockedOut = user.LockoutEndDateUtc.HasValue && user.LockoutEndDateUtc.Value > DateTime.UtcNow,
                        LockoutEndDateUtc = user.LockoutEndDateUtc
                    });
                }
            }

            return View(receptionists.OrderBy(r => r.Email).ToList());
        }

        // GET: Admin/Receptionists/Create
        public ActionResult Create()
        {
            ViewBag.Nav = "receptionists";
            return View();
        }

        // POST: Admin/Receptionists/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ReceptionistCreateViewModel model)
        {
            ViewBag.Nav = "receptionists";
            if (ModelState.IsValid)
            {
                var existingUser = await UserManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại.");
                    return View(model);
                }

                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true }; // Confirm email immediately for admin creation
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Assign the Receptionist role
                    var roleResult = await UserManager.AddToRoleAsync(user.Id, "Receptionist");
                    if (roleResult.Succeeded)
                    {
                        TempData["ok"] = $"Đã tạo tài khoản lễ tân {user.Email} thành công.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        // Role assignment failed - cleanup? Delete user?
                        await UserManager.DeleteAsync(user); // Attempt to delete the created user
                        AddErrors(roleResult);
                    }
                }
                else
                {
                    AddErrors(result);
                }
            }
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // GET: Admin/Receptionists/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            ViewBag.Nav = "receptionists";
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = await UserManager.FindByIdAsync(id);
            if (user == null || !(await UserManager.IsInRoleAsync(user.Id, "Receptionist")))
            {
                return HttpNotFound("Không tìm thấy tài khoản lễ tân.");
            }

            var viewModel = new ReceptionistEditViewModel
            {
                Id = user.Id,
                Email = user.Email,
                IsLockedOut = user.LockoutEndDateUtc.HasValue && user.LockoutEndDateUtc.Value > DateTime.UtcNow
                // Password fields are empty by default
            };

            return View(viewModel);
        }

        // POST: Admin/Receptionists/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(ReceptionistEditViewModel model)
        {
            ViewBag.Nav = "receptionists";
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByIdAsync(model.Id);
                if (user == null || !(await UserManager.IsInRoleAsync(user.Id, "Receptionist")))
                {
                    return HttpNotFound("Không tìm thấy tài khoản lễ tân.");
                }

                bool passwordChanged = false;
                // Update password if provided
                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    // Additional check for ConfirmPassword mismatch just in case client-side fails
                    if (model.NewPassword != model.ConfirmPassword)
                    {
                        ModelState.AddModelError("ConfirmPassword", "Mật khẩu mới và xác nhận mật khẩu không khớp.");
                        return View(model);
                    }

                    string resetToken = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                    var passwordResult = await UserManager.ResetPasswordAsync(user.Id, resetToken, model.NewPassword);
                    if (!passwordResult.Succeeded)
                    {
                        AddErrors(passwordResult);
                        return View(model); // Return with password errors
                    }
                    passwordChanged = true;
                }

                // Update Lockout status if needed (Example)
                // bool currentlyLocked = user.LockoutEndDateUtc.HasValue && user.LockoutEndDateUtc.Value > DateTime.UtcNow;
                // if (model.IsLockedOut && !currentlyLocked) {
                //    await UserManager.SetLockoutEndDateAsync(user.Id, DateTime.UtcNow.AddYears(10)); // Lock indefinitely
                // } else if (!model.IsLockedOut && currentlyLocked) {
                //    await UserManager.SetLockoutEndDateAsync(user.Id, DateTime.UtcNow.AddMinutes(-1)); // Unlock
                // }

                TempData["ok"] = $"Đã cập nhật thông tin lễ tân {user.Email}." + (passwordChanged ? " Mật khẩu đã được đặt lại." : "");
                return RedirectToAction("Index");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }


        // GET: Admin/Receptionists/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            ViewBag.Nav = "receptionists";
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = await UserManager.FindByIdAsync(id);
            // Also check if they are actually a receptionist
            if (user == null || !(await UserManager.IsInRoleAsync(id, "Receptionist")))
            {
                TempData["warn"] = "Không tìm thấy tài khoản lễ tân.";
                return RedirectToAction("Index");
            }

            // Prevent deleting the main admin account if it's also a receptionist?
            if (user.Email.Equals("admin@clinic.local", StringComparison.OrdinalIgnoreCase))
            {
                TempData["err"] = "Không thể xóa tài khoản Admin chính.";
                return RedirectToAction("Index");
            }


            return View(user); // Pass ApplicationUser directly to Delete view for display
        }

        // POST: Admin/Receptionists/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user == null || !(await UserManager.IsInRoleAsync(id, "Receptionist")))
            {
                TempData["err"] = "Không tìm thấy tài khoản lễ tân hoặc thao tác không hợp lệ.";
                return RedirectToAction("Index");
            }

            // Prevent deleting admin
            if (user.Email.Equals("admin@clinic.local", StringComparison.OrdinalIgnoreCase))
            {
                TempData["err"] = "Không thể xóa tài khoản Admin chính.";
                return RedirectToAction("Index");
            }


            // Option 1: Delete the user entirely
            var result = await UserManager.DeleteAsync(user);

            // Option 2: Just remove the Receptionist role (user can still login if they have other roles or were Patient before)
            // var result = await UserManager.RemoveFromRoleAsync(user.Id, "Receptionist");

            if (result.Succeeded)
            {
                TempData["ok"] = $"Đã xóa tài khoản lễ tân {user.Email}.";
            }
            else
            {
                TempData["err"] = "Xóa tài khoản lễ tân thất bại: " + string.Join(", ", result.Errors);
            }
            return RedirectToAction("Index");
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }
                // Dispose RoleManager if initialized
                // if (_roleManager != null) { _roleManager.Dispose(); _roleManager = null; }
            }
            base.Dispose(disposing);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }
    }
}
