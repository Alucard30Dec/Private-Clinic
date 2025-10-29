using Clinic.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web;
using Microsoft.Owin.Security;

namespace Clinic.Controllers
{
    public class PatientController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        private ApplicationSignInManager SignInManager => HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
        private ApplicationUserManager UserManager => HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
        private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;

        [HttpGet]
        public async Task<ActionResult> CompleteRequired()
        {
            ViewBag.Title = "Hoàn thiện Hồ sơ Bệnh nhân";
            ViewBag.Layout = "~/Views/Shared/_Layout.cshtml";

            // Ưu tiên 1: Quy trình đăng ký mới
            if (TempData["IsNewRegistrationProcess"] as bool? == true)
            {
                TempData.Keep();
                var model = new Patient();
                bool isExternal = false;

                if (TempData["PendingRegEmail"] is string email && TempData["PendingRegHashedPassword"] is string hashedPassword)
                {
                    model.Email = email;
                }
                else if (TempData["PendingExternalLoginInfo"] is ExternalLoginInfo loginInfo && TempData["PendingExternalEmail"] is string externalEmail)
                {
                    model.Email = externalEmail;
                    model.FullName = TempData["PendingExternalName"] as string;
                    isExternal = true;
                }
                else
                {
                    TempData.Clear();
                    TempData["err"] = "Phiên đăng ký không hợp lệ hoặc đã hết hạn. Vui lòng thử lại.";
                    return RedirectToAction("Register", "Account");
                }

                ViewBag.IsNewRegistration = true;
                ViewBag.IsExternalRegistration = isExternal;
                return View("Complete", model);
            }

            // Ưu tiên 2: Bắt buộc hoàn thiện sau đăng nhập
            if (TempData["ForceCompleteUserId"] is string forceUserId)
            {
                TempData.Keep();
                var model = new Patient
                {
                    UserId = forceUserId,
                    Email = TempData["ForceCompleteEmail"] as string,
                    FullName = TempData["ForceCompleteName"] as string
                };
                ViewBag.IsNewRegistration = false;
                ViewBag.IsForcedCompletion = true;
                return View("Complete", model);
            }

            // Ưu tiên 3: Đã đăng nhập -> chuyển sang trang chỉnh sửa
            if (User.Identity.IsAuthenticated)
            {
                var uid = User.Identity.GetUserId();
                var profile = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == uid);

                if (profile == null)
                {
                    AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                    TempData["err"] = "Hồ sơ của bạn chưa được tạo. Vui lòng đăng nhập lại để hoàn tất.";
                    return RedirectToAction("Login", "Account");
                }
                return RedirectToAction("Complete");
            }

            // Mặc định: Về trang đăng nhập
            TempData["err"] = "Vui lòng đăng nhập hoặc đăng ký để tiếp tục.";
            return RedirectToAction("Login", "Account");
        }

        [Authorize(Roles = "Patient")]
        public async Task<ActionResult> Complete(string returnUrl)
        {
            ViewBag.Title = "Chỉnh sửa Hồ sơ Bệnh nhân";
            ViewBag.Layout = "~/Views/Shared/_Layout.cshtml";
            if (TempData["warn"] != null) ViewBag.Warn = TempData["warn"];

            var uid = User.Identity.GetUserId();
            var profile = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == uid);

            if (profile == null)
            {
                AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                TempData["err"] = "Không tìm thấy hồ sơ của bạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.IsNewRegistration = false;
            return View(profile);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Complete(
            // *** CẬP NHẬT BIND ĐỂ BAO GỒM CÁC TRƯỜNG MỚI ***
            [Bind(Include = "FullName,PhoneNumber,DateOfBirth,Address,Email," +
                           "Gender,BloodType,MedicalHistory,Allergies,EmergencyContactName,EmergencyContactPhone")] Patient form,
            string returnUrl)
        {
            ViewBag.Layout = "~/Views/Shared/_Layout.cshtml";
            bool isNewRegistrationProcess = TempData["IsNewRegistrationProcess"] as bool? ?? false;
            bool isForcedCompletion = TempData["ForceCompleteUserId"] != null;
            ViewBag.IsNewRegistration = isNewRegistrationProcess;
            ViewBag.IsForcedCompletion = isForcedCompletion;

            // Trim dữ liệu
            form.FullName = form.FullName?.Trim();
            form.PhoneNumber = form.PhoneNumber?.Trim();
            form.Address = form.Address?.Trim();
            form.Gender = form.Gender?.Trim();
            form.BloodType = form.BloodType?.Trim();
            form.MedicalHistory = form.MedicalHistory?.Trim();
            form.Allergies = form.Allergies?.Trim();
            form.EmergencyContactName = form.EmergencyContactName?.Trim();
            form.EmergencyContactPhone = form.EmergencyContactPhone?.Trim();

            // --- Validation cơ bản ---
            if (string.IsNullOrWhiteSpace(form.FullName))
                ModelState.AddModelError("FullName", "Họ tên là bắt buộc.");
            if (string.IsNullOrWhiteSpace(form.PhoneNumber))
                ModelState.AddModelError("PhoneNumber", "Số điện thoại là bắt buộc để phòng khám liên hệ.");
            // Thêm các validation khác nếu cần...

            if (!ModelState.IsValid)
            {
                TempData.Keep();
                if (isNewRegistrationProcess)
                    form.Email = TempData.Peek("PendingRegEmail") as string ?? TempData.Peek("PendingExternalEmail") as string;
                return View(form);
            }

            // --- XỬ LÝ QUY TRÌNH ĐĂNG KÝ MỚI ---
            if (isNewRegistrationProcess)
            {
                ApplicationUser user = null;
                IdentityResult result = null;
                ExternalLoginInfo loginInfo = TempData["PendingExternalLoginInfo"] as ExternalLoginInfo;
                string email = null; // Khai báo email ở đây để dùng chung

                // A. Tạo User (Identity)
                if (TempData["PendingRegEmail"] is string regEmail && TempData["PendingRegHashedPassword"] is string hashedPassword)
                {
                    email = regEmail; // Gán email
                    var existingUser = await UserManager.FindByEmailAsync(email);
                    if (existingUser != null)
                    { /* Lỗi email trùng */
                        ModelState.AddModelError("Email", "Địa chỉ email này đã được sử dụng trong lúc bạn nhập thông tin. Vui lòng thử lại.");
                        TempData.Keep();
                        form.Email = email;
                        return View(form);
                    }
                    user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, PasswordHash = hashedPassword };
                    result = await UserManager.CreateAsync(user);
                }
                else if (loginInfo != null && TempData["PendingExternalEmail"] is string externalEmail)
                {
                    email = externalEmail; // Gán email
                    var existingUser = await UserManager.FindByEmailAsync(email);
                    if (existingUser != null)
                    { /* Lỗi email trùng */
                        ModelState.AddModelError("Email", "Địa chỉ email này đã được sử dụng trong lúc bạn nhập thông tin. Vui lòng thử lại.");
                        TempData.Keep();
                        form.Email = email;
                        return View(form);
                    }
                    user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                    result = await UserManager.CreateAsync(user);
                    if (result.Succeeded)
                        result = await UserManager.AddLoginAsync(user.Id, loginInfo.Login);
                }
                else
                { /* Lỗi TempData */
                    TempData["err"] = "Phiên đăng ký không hợp lệ hoặc đã hết hạn. Vui lòng thử lại.";
                    return RedirectToAction("Register", "Account");
                }

                // B. Xử lý kết quả tạo User
                if (result.Succeeded)
                {
                    await UserManager.AddToRoleAsync(user.Id, "Patient");

                    // C. Tạo hồ sơ Patient (bao gồm các trường mới)
                    var patientProfile = new Patient
                    {
                        UserId = user.Id,
                        FullName = form.FullName,
                        Email = user.Email, // Email từ Identity user
                        PhoneNumber = form.PhoneNumber,
                        DateOfBirth = form.DateOfBirth,
                        Address = form.Address,
                        Gender = form.Gender,
                        BloodType = form.BloodType,
                        MedicalHistory = form.MedicalHistory,
                        Allergies = form.Allergies,
                        EmergencyContactName = form.EmergencyContactName,
                        EmergencyContactPhone = form.EmergencyContactPhone,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Patients.Add(patientProfile);
                    await _db.SaveChangesAsync();

                    // D. Đăng nhập và chuyển hướng
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    TempData["ok"] = "Đăng ký và hoàn thiện hồ sơ thành công! Chào mừng bạn.";

                    if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    return RedirectToAction("Index", "Doctors");
                }
                else
                { /* Lỗi tạo User */
                    AddErrors(result);
                    TempData.Keep();
                    form.Email = email; // Điền lại email
                    return View(form);
                }
            }
            // --- XỬ LÝ CẬP NHẬT/BẮT BUỘC HOÀN THIỆN ---
            else
            {
                string userIdToUpdate = isForcedCompletion ? TempData["ForceCompleteUserId"] as string : User.Identity.GetUserId();
                if (string.IsNullOrEmpty(userIdToUpdate))
                { /* Lỗi phiên */
                    TempData["err"] = "Phiên làm việc không hợp lệ.";
                    return RedirectToAction("Login", "Account");
                }

                var profile = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == userIdToUpdate);

                // Nếu là bắt buộc hoàn thiện mà chưa có profile -> Tạo mới
                if (profile == null && isForcedCompletion)
                {
                    profile = new Patient
                    {
                        UserId = userIdToUpdate,
                        Email = TempData["ForceCompleteEmail"] as string,
                        FullName = form.FullName,
                        PhoneNumber = form.PhoneNumber,
                        DateOfBirth = form.DateOfBirth,
                        Address = form.Address,
                        Gender = form.Gender,
                        BloodType = form.BloodType,
                        MedicalHistory = form.MedicalHistory,
                        Allergies = form.Allergies,
                        EmergencyContactName = form.EmergencyContactName,
                        EmergencyContactPhone = form.EmergencyContactPhone,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Patients.Add(profile);
                    await _db.SaveChangesAsync();
                    TempData["ok"] = "Hoàn thiện hồ sơ thành công.";

                    var user = await UserManager.FindByIdAsync(userIdToUpdate);
                    if (user != null) await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                    if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    return RedirectToAction("Index", "Doctors");
                }
                // Nếu là chỉnh sửa thông thường (đã có profile)
                else if (profile != null && !isForcedCompletion && profile.UserId == User.Identity.GetUserId())
                {
                    // Cập nhật các trường (bao gồm cả trường mới)
                    profile.FullName = form.FullName;
                    profile.PhoneNumber = form.PhoneNumber;
                    profile.DateOfBirth = form.DateOfBirth;
                    profile.Address = form.Address;
                    profile.Gender = form.Gender;
                    profile.BloodType = form.BloodType;
                    profile.MedicalHistory = form.MedicalHistory;
                    profile.Allergies = form.Allergies;
                    profile.EmergencyContactName = form.EmergencyContactName;
                    profile.EmergencyContactPhone = form.EmergencyContactPhone;
                    profile.UpdatedAt = DateTime.UtcNow;

                    await _db.SaveChangesAsync();
                    TempData["ok"] = "Đã cập nhật hồ sơ.";

                    if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    return RedirectToAction("Index", "Manage"); // Về trang quản lý tài khoản sau khi sửa
                }
                else
                { /* Lỗi không tìm thấy/không có quyền */
                    TempData["err"] = "Không tìm thấy hồ sơ hoặc bạn không có quyền chỉnh sửa.";
                    return RedirectToAction("Index", "Home");
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                if (error.ToLower().Contains("email"))
                    ModelState.AddModelError("Email", error);
                else if (error.ToLower().Contains("name is already taken") && error.ToLower().Contains(TempData.Peek("PendingRegEmail") as string ?? TempData.Peek("PendingExternalEmail") as string ?? ""))
                    ModelState.AddModelError("Email", "Địa chỉ email này đã được đăng ký.");
                else
                    ModelState.AddModelError("", error);
            }
        }
    }
}
