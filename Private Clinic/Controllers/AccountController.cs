using System.Linq;
using System.Web.Mvc;
using Private_Clinic.DAL;
using Private_Clinic.Helpers;
using Private_Clinic.Models;

namespace Private_Clinic.Controllers
{
    public class AccountController : Controller
    {
        private readonly ClinicDbContext db = new ClinicDbContext();

        // DÙNG MODAL: Không render view riêng, cứ quay lại Home
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return RedirectToAction("Index", "Home");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(string UserNameOrEmail, string Password, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(UserNameOrEmail) || string.IsNullOrWhiteSpace(Password))
            {
                TempData["LoginError"] = "Vui lòng nhập đầy đủ thông tin.";
                return RedirectToAction("Index", "Home");
            }

            var user = db.Users.FirstOrDefault(u => u.UserName == UserNameOrEmail || u.Email == UserNameOrEmail);
            if (user == null || !PasswordHelper.Verify(Password, user.PasswordHash))
            {
                TempData["LoginError"] = "Thông tin đăng nhập không đúng.";
                return RedirectToAction("Index", "Home");
            }

            Session["UserId"] = user.Id;
            Session["Role"] = user.Role;
            Session["FullName"] = user.FullName ?? user.UserName;

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);

            switch (user.Role)
            {
                case UserRole.Admin: return RedirectToAction("Index", "AdminHome");
                case UserRole.Receptionist: return RedirectToAction("Index", "Reception");
                case UserRole.Doctor: return RedirectToAction("MyDay", "Doctor");
                default: return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string UserName, string NewPassword, string ConfirmPassword)
        {
            if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(NewPassword))
            {
                TempData["Error"] = "Thiếu thông tin.";
                return RedirectToAction("Index", "Home");
            }
            if (NewPassword != ConfirmPassword)
            {
                TempData["Error"] = "Mật khẩu xác nhận không khớp.";
                return RedirectToAction("Index", "Home");
            }

            var user = db.Users.FirstOrDefault(u => u.UserName == UserName);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("Index", "Home");
            }

            user.PasswordHash = PasswordHelper.Hash(NewPassword);
            db.SaveChanges();

            TempData["Success"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Index", "Home");
        }
    }
}
