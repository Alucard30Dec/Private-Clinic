using Clinic.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin;

namespace Clinic.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController() { }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get { return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>(); }
            private set { _signInManager = value; }
        }

        public ApplicationUserManager UserManager
        {
            get { return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
            private set { _userManager = value; }
        }

        private IAuthenticationManager AuthenticationManager
            => HttpContext.GetOwinContext().Authentication;

        // =============== LOGIN / REGISTER / EXTERNAL ===============
        [AllowAnonymous]
        public async Task<ActionResult> Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await UserManager.FindByNameAsync(User.Identity.Name)
                           ?? await UserManager.FindByEmailAsync(User.Identity.Name);
                if (user != null)
                    return await RedirectByRoleAsync(user.Id, returnUrl);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await UserManager.FindByNameAsync(model.Email)
                       ?? await UserManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại.");
                return View(model);
            }

            if (await UserManager.IsInRoleAsync(user.Id, "Doctor"))
            {
                using (var clinicDb = new ClinicDbContext())
                {
                    bool doctorProfileExists = await clinicDb.Doctors.AnyAsync(d => d.UserId == user.Id);
                    if (!doctorProfileExists)
                    {
                        ModelState.AddModelError("", "Tài khoản bác sĩ chưa được liên kết với hồ sơ. Vui lòng liên hệ Admin.");
                        return View(model);
                    }
                }
            }

            var result = await SignInManager.PasswordSignInAsync(
                user.UserName,
                model.Password,
                model.RememberMe,
                shouldLockout: false);

            switch (result)
            {
                case SignInStatus.Success:
                    return await RedirectByRoleAsync(user.Id, returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                default:
                    ModelState.AddModelError("", "Đăng nhập không hợp lệ.");
                    return View(model);
            }
        }

        [AllowAnonymous]
        public ActionResult Register() => View();

        // [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        // public async Task<ActionResult> Register(RegisterViewModel model)
        // {
        //     if (!ModelState.IsValid) return View(model);

        //     var user = new ApplicationUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
        //     var result = await UserManager.CreateAsync(user, model.Password);
        //     if (result.Succeeded)
        //     {
        //         await UserManager.AddToRoleAsync(user.Id, "Patient");

        //         // === SỬA ĐỔI QUAN TRỌNG: KHÔNG TỰ ĐỘNG ĐĂNG NHẬP VÀ CHUYỂN LOGIC TẠO PROFILE ===

        //         // Dùng TempData để lưu UserId vừa tạo và chuyển hướng đến trang hoàn thiện hồ sơ (Patient/CompleteRequired)
        //         TempData["NewPatientUserId"] = user.Id;
        //         TempData["NewPatientEmail"] = user.Email;
        //         // TempData["NewPatientName"] = user.UserName; // Không cần Name ở bước này

        //         // Chuyển hướng đến trang hoàn thiện hồ sơ BẮT BUỘC
        //         return RedirectToAction("CompleteRequired", "Patient", new { area = "" });
        //     }

        //     AddErrors(result);
        //     return View(model);
        // }

        // === PHIÊN BẢN SỬA ĐỔI CỦA Register (POST) ===
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // *** BƯỚC 1: Kiểm tra Email đã tồn tại chưa (quan trọng) ***
            var existingUser = await UserManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Địa chỉ email này đã được sử dụng.");
                return View(model);
            }

            // *** BƯỚC 2: Chỉ mã hóa mật khẩu, KHÔNG tạo user ***
            var hashedPassword = UserManager.PasswordHasher.HashPassword(model.Password);

            // *** BƯỚC 3: Lưu thông tin tạm vào TempData ***
            TempData["PendingRegEmail"] = model.Email;
            TempData["PendingRegHashedPassword"] = hashedPassword;
            // TempData["PendingRegUserName"] = model.Email; // Lưu UserName (chính là Email)

            // *** BƯỚC 4: Chuyển hướng đến trang hoàn thiện hồ sơ ***
            TempData["IsNewRegistrationProcess"] = true; // Đánh dấu đây là quy trình đăng ký mới
            return RedirectToAction("CompleteRequired", "Patient", new { area = "" });
        }
        // === KẾT THÚC SỬA ĐỔI ===


        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null) return RedirectToAction("Login");

            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);

            switch (result)
            {
                case SignInStatus.Success:
                    var signedUser = await UserManager.FindAsync(loginInfo.Login);
                    if (signedUser == null) return RedirectToAction("Login");
                    return await RedirectByRoleAsync(signedUser.Id, returnUrl); // RedirectByRoleAsync sẽ kiểm tra hồ sơ Patient

                case SignInStatus.LockedOut:
                    return View("Lockout");

                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });

                default: // User chưa có tài khoản -> tạo mới và yêu cầu hoàn thiện hồ sơ
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    // Chuyển đến trang xác nhận thông tin (nơi user nhập/confirm email)
                    return View("ExternalLoginConfirmation",
                        new ExternalLoginConfirmationViewModel
                        {
                            Email = loginInfo.Email,
                            // Lấy tên gợi ý từ thông tin external identity
                            SuggestedName = loginInfo.ExternalIdentity?.Name
                        });
            }
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Manage");

            // *** SỬA ĐỔI: Không tạo User ngay, chỉ lưu thông tin tạm ***
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var info = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (info == null) return View("ExternalLoginFailure");

            // *** Kiểm tra Email đã tồn tại chưa ***
            var existingUser = await UserManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                // Nếu email đã tồn tại và CÓ liên kết ngoài trùng khớp -> Lỗi logic (đáng lẽ phải Success ở Callback)
                // Nếu email đã tồn tại nhưng CHƯA có liên kết ngoài này -> Có thể gợi ý user đăng nhập bằng email/pass rồi liên kết sau, HOẶC báo lỗi email đã dùng.
                // --> Tạm thời báo lỗi email đã dùng cho đơn giản.
                ModelState.AddModelError("Email", "Địa chỉ email này đã được sử dụng bởi một tài khoản khác.");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            // *** Lưu thông tin tạm vào TempData ***
            TempData["PendingExternalLoginInfo"] = info; // Lưu thông tin đăng nhập ngoài
            TempData["PendingExternalEmail"] = model.Email; // Email user đã xác nhận/nhập
            TempData["PendingExternalName"] = model.SuggestedName; // Tên gợi ý

            // *** Chuyển hướng đến trang hoàn thiện hồ sơ ***
            TempData["IsNewRegistrationProcess"] = true; // Đánh dấu là quy trình đăng ký mới
            return RedirectToAction("CompleteRequired", "Patient", new { area = "" });
            // *** KẾT THÚC SỬA ĐỔI ***
        }


        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(
                DefaultAuthenticationTypes.ApplicationCookie,
                DefaultAuthenticationTypes.ExternalCookie,
                DefaultAuthenticationTypes.TwoFactorCookie,
                DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie
            );
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [AllowAnonymous]
        public ActionResult ExternalLoginFailure() => View();

        // ===================== HELPERS =============================
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error);
        }

        private bool IsSafeLocalUrl(string url)
        {
            return !string.IsNullOrWhiteSpace(url) && Url.IsLocalUrl(url);
        }

        private async Task<ActionResult> RedirectByRoleAsync(string userId, string returnUrl)
        {
            var roles = await UserManager.GetRolesAsync(userId);
            bool isAdmin = roles.Contains("Admin");
            bool isDoctor = roles.Contains("Doctor");
            bool isReceptionist = roles.Contains("Receptionist");

            ActionResult defaultRedirect;
            if (isAdmin)
                defaultRedirect = RedirectToAction("Index", "Home", new { area = "Admin" });
            else if (isDoctor)
                defaultRedirect = RedirectToAction("Index", "Home", new { area = "Doctor" });
            else if (isReceptionist)
                defaultRedirect = RedirectToAction("Index", "Dashboard", new { area = "Reception" });
            else // Patient
            {
                // *** KIỂM TRA HỒ SƠ BỆNH NHÂN (Patient Profile) BẮT BUỘC ***
                using (var clinicDb = new ClinicDbContext())
                {
                    bool patientProfileExists = await clinicDb.Patients.AnyAsync(p => p.UserId == userId);
                    if (!patientProfileExists)
                    {
                        // Nếu chưa có hồ sơ, BẮT BUỘC ĐĂNG XUẤT và chuyển hướng đến trang hoàn thiện
                        AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie); // Đăng xuất ngay
                        TempData["ForceCompleteUserId"] = userId; // Gửi ID để hoàn thiện (dùng key khác để phân biệt)
                        var user = await UserManager.FindByIdAsync(userId); // Lấy lại thông tin user để điền form
                        TempData["ForceCompleteEmail"] = user?.Email;
                        TempData["ForceCompleteName"] = user?.UserName; // Hoặc tên từ nguồn khác nếu có
                        TempData["IsNewRegistrationProcess"] = false; // Đánh dấu không phải đăng ký mới từ đầu
                        return RedirectToAction("CompleteRequired", "Patient", new { area = "" });
                    }
                }
                // Nếu đã có hồ sơ, về trang chủ người dùng
                defaultRedirect = RedirectToAction("Index", "Home", new { area = "" });
            }


            // Xử lý returnUrl an toàn
            if (IsSafeLocalUrl(returnUrl))
            {
                // Ngăn Patient truy cập area của role khác
                if (!isAdmin && !isDoctor && !isReceptionist)
                {
                    if (returnUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase) ||
                        returnUrl.StartsWith("/Doctor", StringComparison.OrdinalIgnoreCase) ||
                        returnUrl.StartsWith("/Reception", StringComparison.OrdinalIgnoreCase))
                    {
                        return defaultRedirect; // Chuyển về trang mặc định của Patient nếu cố vào area khác
                    }
                }
                return Redirect(returnUrl); // Cho phép truy cập returnUrl nếu hợp lệ
            }

            // Nếu returnUrl không an toàn hoặc không có, dùng defaultRedirect
            return defaultRedirect;
        }


        private const string XsrfKey = "XsrfId";
        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri) : this(provider, redirectUri, null) { }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider; RedirectUri = redirectUri; UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null) properties.Dictionary[XsrfKey] = UserId;
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
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
                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
