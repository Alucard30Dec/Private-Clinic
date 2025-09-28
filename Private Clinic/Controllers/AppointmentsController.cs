using System.Linq;
using System.Web.Mvc;
using Private_Clinic.Models;

namespace Private_Clinic.Controllers
{
    public class AppointmentController : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult QuickRegister(QuickRegisterVM model)
        {
            if (!ModelState.IsValid)
            {
                // Gom lỗi thành 1 chuỗi ngắn gọn để hiển thị
                var firstError = ModelState.Values
                                           .SelectMany(v => v.Errors)
                                           .Select(e => e.ErrorMessage)
                                           .FirstOrDefault() ?? "Vui lòng kiểm tra lại thông tin.";
                TempData["Error"] = firstError;

                // Lưu lại dữ liệu người dùng đã nhập để fill lại form sau redirect
                TempData["FormData"] = model;
                return RedirectToAction("Index", "Home");
            }

            // TODO: Lưu DB sau này (model.FullName, model.Email, ...)
            TempData["Success"] = "Đăng ký thành công! Chúng tôi sẽ liên hệ xác nhận trong thời gian sớm nhất.";
            return RedirectToAction("Index", "Home");
        }
    }
}
