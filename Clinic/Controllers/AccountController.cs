using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Clinic.Models;

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

        // ===================== LOGIN =====================

        [AllowAnonymous]
        public async Task<ActionResult> Login(string returnUrl)
        {
            // Nếu đã đăng nhập thì điều hướng theo role luôn
            if (User.Identity.IsAuthenticated)
            {
                var user = await UserManager.FindByNameAsync(User.Identity.Name)
                           ?? await UserManager.FindByEmailAsync(User.Identity.Name);
                if (user != null) return await RedirectByRoleAsync(user.Id, returnUrl);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid) return View(model);

            // Cho phép nhập Email hoặc Username
            var user = await UserManager.FindByNameAsync(model.Email)
                       ?? await UserManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại.");
                return View(model);
            }

            // PasswordSignInAsync phải truyền UserName thật
            var result = await SignInManager.PasswordSignInAsync(
                user.UserName, model.Password, model.RememberMe, shouldLockout: false);

            switch (result)
            {
                case SignInStatus.Success:
                    return await RedirectByRoleAsync(user.Id, returnUrl);

                case SignInStatus.LockedOut:
                    return View("Lockout");

                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, model.RememberMe });

                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Đăng nhập không hợp lệ.");
                    return View(model);
            }
        }

        // ===================== REGISTER (tùy dùng) =====================

        [AllowAnonymous]
        public ActionResult Register() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Ở template mặc định: dùng Email làm UserName
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await UserManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Mặc định: Patient
                await UserManager.AddToRoleAsync(user.Id, "Patient");
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                return await RedirectByRoleAsync(user.Id, null);
            }

            AddErrors(result);
            return View(model);
        }

        // ===================== EXTERNAL LOGIN =====================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
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
                    return await RedirectByRoleAsync(signedUser.Id, returnUrl);

                case SignInStatus.LockedOut:
                    return View("Lockout");

                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });

                case SignInStatus.Failure:
                default:
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation",
                        new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Manage");
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var info = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (info == null) return View("ExternalLoginFailure");

            var user = new ApplicationUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
            var result = await UserManager.CreateAsync(user);
            if (result.Succeeded)
            {
                await UserManager.AddToRoleAsync(user.Id, "Patient");
                result = await UserManager.AddLoginAsync(user.Id, info.Login);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    return await RedirectByRoleAsync(user.Id, returnUrl);
                }
            }

            AddErrors(result);
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        // ===================== LOGOFF =====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [AllowAnonymous]
        public ActionResult ExternalLoginFailure() => View();

        // ===================== Helpers =====================

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error);
        }

        /// <summary>
        /// Điều hướng sau đăng nhập theo role.
        /// Ưu tiên ReturnUrl nếu là local URL và không trỏ vào file .cshtml.
        /// </summary>
        private async Task<ActionResult> RedirectByRoleAsync(string userId, string returnUrl)
        {
            if (IsSafeLocalUrl(returnUrl))
                return Redirect(returnUrl);

            var roles = await UserManager.GetRolesAsync(userId);

            // Ưu tiên Admin > Doctor > Receptionist > Patient
            if (roles.Contains("Admin"))
                return RedirectToAction("Index", "Home", new { area = "Admin" });

            if (roles.Contains("Doctor"))
                return RedirectToAction("Index", "Home", new { area = "Doctors" });

            if (roles.Contains("Receptionist"))
                return RedirectToAction("Today", "Reception", new { area = "Admin" });

            // Mặc định: Patient / no role
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        /// <summary>
        /// Chỉ chấp nhận URL nội bộ và không chứa đuôi .cshtml (tránh mở file view trực tiếp).
        /// </summary>
        private bool IsSafeLocalUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Url.IsLocalUrl(url)) return false;

            // chặn việc truy cập trực tiếp view .cshtml
            var lower = url.ToLowerInvariant();
            if (lower.EndsWith(".cshtml")) return false;

            return true;
        }

        // XSRF khi thêm external login
        private const string XsrfKey = "XsrfId";

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null) { }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
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
    }
}
