using Clinic.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System;
using System.Data.Entity; // Cần cho AnyAsync
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
// using Microsoft.Owin; // Có thể không cần dòng này nữa

namespace Clinic.Controllers
{
    [Authorize] // Áp dụng cho toàn bộ Controller, trừ các action có [AllowAnonymous]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        // Thêm DbContext để kiểm tra hồ sơ Patient
        private readonly ClinicDbContext _clinicDbContext = new ClinicDbContext();

        // Constructors và Properties cho UserManager, SignInManager giữ nguyên
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

        private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;

        // =============== LOGIN / REGISTER / EXTERNAL ===============

        // GET: /Account/Login (Giữ nguyên logic kiểm tra đăng nhập và RedirectByRoleAsync)
        [AllowAnonymous]
        public async Task<ActionResult> Login(string returnUrl)
        {
            // Nếu đã đăng nhập, thử chuyển hướng theo vai trò
            if (User.Identity.IsAuthenticated)
            {
                var user = await UserManager.FindByNameAsync(User.Identity.Name)
                           ?? await UserManager.FindByEmailAsync(User.Identity.Name); // Thêm tìm bằng Email
                if (user != null)
                {
                    // RedirectByRoleAsync sẽ kiểm tra hồ sơ Patient nếu cần
                    return await RedirectByRoleAsync(user.Id, returnUrl);
                }
                else
                {
                    // Nếu không tìm thấy user dù đã authenticated (lạ?), đăng xuất và về trang login
                    AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                }
            }
            ViewBag.ReturnUrl = returnUrl;
            return View(); // Trả về view Views/Account/Login.cshtml
        }

        // POST: /Account/Login (Giữ nguyên logic xử lý đăng nhập bằng mật khẩu)
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid) return View(model);

            // Thử tìm user bằng Email trước (phổ biến hơn)
            var user = await UserManager.FindByEmailAsync(model.Email)
                       ?? await UserManager.FindByNameAsync(model.Email); // Fallback tìm bằng UserName

            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng."); // Thông báo chung chung
                return View(model);
            }

            // Kiểm tra hồ sơ Doctor (nếu đăng nhập với role Doctor)
            if (await UserManager.IsInRoleAsync(user.Id, "Doctor"))
            {
                bool doctorProfileExists = await _clinicDbContext.Doctors.AnyAsync(d => d.UserId == user.Id);
                if (!doctorProfileExists)
                {
                    ModelState.AddModelError("", "Tài khoản bác sĩ chưa được liên kết với hồ sơ. Vui lòng liên hệ Admin.");
                    return View(model);
                }
            }

            // Tiến hành đăng nhập bằng PasswordSignInAsync
            // Lưu ý: PasswordSignInAsync dùng UserName, nên cần user.UserName ở đây
            var result = await SignInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, shouldLockout: true); // Bật lockout

            switch (result)
            {
                case SignInStatus.Success:
                    // RedirectByRoleAsync sẽ kiểm tra hồ sơ Patient nếu cần
                    return await RedirectByRoleAsync(user.Id, returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout"); // Trả về view Views/Shared/Lockout.cshtml
                case SignInStatus.RequiresVerification:
                    // Chuyển hướng đến action xác thực 2 yếu tố (nếu có cấu hình)
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Email hoặc mật khẩu không đúng."); // Thông báo chung chung
                    return View(model);
            }
        }

        // GET: /Account/Register (Giữ nguyên)
        [AllowAnonymous]
        public ActionResult Register()
        {
            // Chỉ trả về view Views/Account/Register.cshtml
            return View();
        }

        // POST: /Account/Register (Giữ nguyên logic chuyển hướng sang PatientController)
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra Email đã tồn tại chưa
            var existingUser = await UserManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Địa chỉ email này đã được sử dụng.");
                return View(model);
            }

            // Chỉ mã hóa mật khẩu, KHÔNG tạo user ngay
            var hashedPassword = UserManager.PasswordHasher.HashPassword(model.Password);

            // Lưu thông tin tạm vào TempData
            TempData["PendingRegEmail"] = model.Email;
            TempData["PendingRegHashedPassword"] = hashedPassword;
            // TempData["PendingRegUserName"] = model.Email; // UserName sẽ là Email

            // Chuyển hướng đến trang hoàn thiện hồ sơ BẮT BUỘC
            TempData["IsNewRegistrationProcess"] = true; // Đánh dấu đây là quy trình đăng ký mới
            return RedirectToAction("CompleteRequired", "Patient", new { area = "" }); // Trỏ đến PatientController ở root
        }

        // --- CÁC ACTION XỬ LÝ ĐĂNG NHẬP NGOÀI (GOOGLE) ---

        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Gửi yêu cầu thách thức (challenge) đến nhà cung cấp dịch vụ ngoài (Google)
            // OWIN middleware sẽ xử lý việc chuyển hướng người dùng đến trang đăng nhập Google
            // và sau đó gọi lại vào ExternalLoginCallback với kết quả.
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            // Lấy thông tin đăng nhập từ nhà cung cấp ngoài (Google) sau khi người dùng xác thực thành công
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                // Nếu không lấy được thông tin (ví dụ: user hủy), quay lại trang đăng nhập
                TempData["error"] = "Không thể lấy thông tin đăng nhập từ nhà cung cấp dịch vụ.";
                return RedirectToAction("Login");
            }

            // Thử đăng nhập người dùng bằng thông tin đăng nhập ngoài này
            // (Kiểm tra xem đã có liên kết trong bảng AspNetUserLogins chưa)
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);

            switch (result)
            {
                case SignInStatus.Success:
                    // Đăng nhập thành công (đã có user và liên kết)
                    // Tìm lại user để lấy UserId và chuyển hướng theo vai trò
                    var signedInUser = await UserManager.FindAsync(loginInfo.Login); // Tìm user bằng LoginProvider và ProviderKey
                    if (signedInUser == null)
                    {
                        // Trường hợp lạ: Đăng nhập thành công nhưng không tìm thấy user? -> Về trang Login
                        AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                        TempData["error"] = "Có lỗi xảy ra trong quá trình đăng nhập.";
                        return RedirectToAction("Login");
                    }
                    // Chuyển hướng theo vai trò (sẽ kiểm tra hồ sơ Patient nếu là Patient)
                    return await RedirectByRoleAsync(signedInUser.Id, returnUrl);

                case SignInStatus.LockedOut:
                    // Tài khoản bị khóa
                    return View("Lockout");

                case SignInStatus.RequiresVerification:
                    // Yêu cầu xác thực 2 yếu tố (ít gặp với đăng nhập ngoài)
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });

                case SignInStatus.Failure:
                default:
                    // Đăng nhập thất bại -> Thường là do user chưa có tài khoản local hoặc chưa liên kết
                    // Chuyển đến trang xác nhận để tạo tài khoản mới và liên kết
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;

                    // Chuẩn bị ViewModel cho trang xác nhận, lấy Email và Tên gợi ý từ Google
                    var confirmationViewModel = new ExternalLoginConfirmationViewModel
                    {
                        Email = loginInfo.Email, // Email từ Google trả về
                        // Thử lấy tên từ claim 'Name' (thường có)
                        SuggestedName = loginInfo.ExternalIdentity?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                                        ?? loginInfo.DefaultUserName // Fallback nếu không có claim Name
                    };

                    // Trả về view Views/Account/ExternalLoginConfirmation.cshtml
                    return View("ExternalLoginConfirmation", confirmationViewModel);
            }
        }

        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            // Xử lý khi người dùng xác nhận Email trên trang ExternalLoginConfirmation

            // Nếu user đã đăng nhập bằng cách nào đó rồi thì thôi
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage"); // Chuyển đến trang quản lý tài khoản
            }

            if (ModelState.IsValid)
            {
                // Lấy lại thông tin đăng nhập ngoài đã lưu tạm trong cookie
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    // Nếu mất thông tin (cookie hết hạn?), báo lỗi
                    return View("ExternalLoginFailure"); // Trả về view Views/Account/ExternalLoginFailure.cshtml
                }

                // --- QUY TRÌNH MỚI: Không tạo user ngay ---
                // 1. Kiểm tra Email đã tồn tại chưa
                var existingUser = await UserManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    // Nếu email đã dùng bởi user khác (chưa liên kết với login ngoài này) -> Báo lỗi
                    // Lưu ý: Nếu user này ĐÃ liên kết với login ngoài này thì đáng lẽ phải Success ở Callback
                    ModelState.AddModelError("Email", "Địa chỉ email này đã được sử dụng bởi một tài khoản khác. Vui lòng đăng nhập bằng email đó và liên kết tài khoản Google trong phần quản lý tài khoản.");
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = info.Login.LoginProvider; // Cần cho view
                    // Gán lại SuggestedName để hiển thị lại form
                    model.SuggestedName = info.ExternalIdentity?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? info.DefaultUserName;
                    return View(model); // Trả về view Confirmation với lỗi
                }

                // 2. Lưu thông tin tạm vào TempData
                TempData["PendingExternalLoginInfo"] = info; // Lưu thông tin đăng nhập ngoài (chứa ProviderKey, LoginProvider)
                TempData["PendingExternalEmail"] = model.Email; // Email user đã xác nhận/nhập
                TempData["PendingExternalName"] = model.SuggestedName; // Tên gợi ý để điền sẵn form hồ sơ

                // 3. Chuyển hướng đến trang hoàn thiện hồ sơ BẮT BUỘC
                TempData["IsNewRegistrationProcess"] = true; // Đánh dấu là quy trình đăng ký mới
                return RedirectToAction("CompleteRequired", "Patient", new { area = "" }); // Trỏ đến PatientController ở root
                // --- KẾT THÚC QUY TRÌNH MỚI ---
            }

            // Nếu ModelState không hợp lệ, hiển thị lại form
            ViewBag.ReturnUrl = returnUrl;
            // Cần lấy lại LoginProvider để hiển thị lại view
            var failedLoginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(); // Lấy lại info
            ViewBag.LoginProvider = failedLoginInfo?.Login?.LoginProvider ?? "dịch vụ ngoài";
            // Gán lại SuggestedName nếu có
            model.SuggestedName = failedLoginInfo?.ExternalIdentity?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? failedLoginInfo?.DefaultUserName;
            return View(model);
        }

        // --- KẾT THÚC CÁC ACTION ĐĂNG NHẬP NGOÀI ---

        // POST: /Account/LogOff (Giữ nguyên)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            // Thực hiện đăng xuất khỏi các loại cookie xác thực
            AuthenticationManager.SignOut(
                DefaultAuthenticationTypes.ApplicationCookie,
                DefaultAuthenticationTypes.ExternalCookie,
                DefaultAuthenticationTypes.TwoFactorCookie,
                DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie
            );
            Session.Clear(); // Xóa session nếu có dùng
            Session.Abandon();
            // Chuyển hướng về trang chủ
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        // GET: /Account/ExternalLoginFailure (Giữ nguyên)
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            // Trả về view Views/Account/ExternalLoginFailure.cshtml
            return View();
        }

        // ===================== HELPERS =============================

        // Hàm AddErrors (Giữ nguyên)
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        // Hàm IsSafeLocalUrl (Giữ nguyên)
        private bool IsSafeLocalUrl(string url)
        {
            // Kiểm tra xem URL có phải là URL nội bộ và an toàn không
            return !string.IsNullOrWhiteSpace(url)
                   && Url.IsLocalUrl(url)
                   // Thêm kiểm tra để ngăn redirect đến các file nguy hiểm (tùy chọn)
                   && !url.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                   && !url.EndsWith(".css", StringComparison.OrdinalIgnoreCase);
            // Có thể thêm các kiểm tra khác nếu cần
        }

        // Hàm RedirectByRoleAsync (Giữ nguyên logic kiểm tra hồ sơ Patient)
        private async Task<ActionResult> RedirectByRoleAsync(string userId, string returnUrl)
        {
            var roles = await UserManager.GetRolesAsync(userId);
            bool isAdmin = roles.Contains("Admin");
            bool isDoctor = roles.Contains("Doctor");
            bool isReceptionist = roles.Contains("Receptionist");
            // Mặc định là Patient nếu không có role nào khác
            bool isPatient = !isAdmin && !isDoctor && !isReceptionist;

            ActionResult defaultRedirect;

            if (isAdmin)
                defaultRedirect = RedirectToAction("Index", "Home", new { area = "Admin" });
            else if (isDoctor)
                defaultRedirect = RedirectToAction("Index", "Home", new { area = "Doctor" });
            else if (isReceptionist)
                // Đảm bảo trỏ đúng DashboardController của Reception Area
                defaultRedirect = RedirectToAction("Index", "Dashboard", new { area = "Reception" });
            else // Patient
            {
                // *** KIỂM TRA HỒ SƠ BỆNH NHÂN (Patient Profile) ***
                bool patientProfileExists = await _clinicDbContext.Patients.AnyAsync(p => p.UserId == userId);
                if (!patientProfileExists)
                {
                    // Nếu chưa có hồ sơ, BẮT BUỘC ĐĂNG XUẤT và chuyển hướng đến trang hoàn thiện
                    AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie); // Đăng xuất ngay
                    TempData["ForceCompleteUserId"] = userId; // Gửi ID để PatientController biết cần tạo hồ sơ cho ai
                    var user = await UserManager.FindByIdAsync(userId); // Lấy lại thông tin user để điền form
                    TempData["ForceCompleteEmail"] = user?.Email;
                    // Lấy tên từ UserName làm gợi ý
                    TempData["ForceCompleteName"] = user?.UserName;
                    TempData["IsNewRegistrationProcess"] = false; // Đánh dấu không phải đăng ký mới từ đầu
                    // Chuyển hướng đến PatientController ở root
                    return RedirectToAction("CompleteRequired", "Patient", new { area = "" });
                }
                // Nếu đã có hồ sơ, về trang chủ người dùng (hoặc trang danh sách bác sĩ)
                defaultRedirect = RedirectToAction("Index", "Doctors", new { area = "" }); // Ví dụ: chuyển đến trang Doctors
            }

            // Xử lý returnUrl an toàn
            if (IsSafeLocalUrl(returnUrl))
            {
                // Ngăn Patient truy cập area của role khác một cách trực tiếp qua returnUrl
                if (isPatient)
                {
                    var lowerReturnUrl = returnUrl.ToLowerInvariant();
                    if (lowerReturnUrl.StartsWith("/admin") ||
                        lowerReturnUrl.StartsWith("/doctor") ||
                        lowerReturnUrl.StartsWith("/reception"))
                    {
                        // Nếu Patient cố vào area khác, chuyển về trang mặc định của Patient
                        return defaultRedirect;
                    }
                }
                // Nếu returnUrl hợp lệ và đúng quyền, cho phép truy cập
                return Redirect(returnUrl);
            }

            // Nếu returnUrl không an toàn hoặc không có, dùng defaultRedirect đã xác định ở trên
            return defaultRedirect;
        }


        // Class ChallengeResult (Giữ nguyên)
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


        // Dispose (Giải phóng cả ClinicDbContext)
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
                // Giải phóng ClinicDbContext
                _clinicDbContext?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
