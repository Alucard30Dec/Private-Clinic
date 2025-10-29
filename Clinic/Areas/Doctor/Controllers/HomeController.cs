using System.Web.Mvc;

namespace Clinic.Areas.Doctor.Controllers // Namespace phải đúng
{
    [Authorize(Roles = "Doctor")] // Đảm bảo chỉ Doctor mới vào được
    public class HomeController : Controller
    {
        // GET: Doctor/Home/Index (hoặc chỉ /Doctor theo route mới)
        public ActionResult Index()
        {
            // Code hiện tại của bạn để hiển thị dashboard bác sĩ
            ViewBag.Title = "Bác sĩ Panel";
            ViewBag.Nav = "dashboard";

            // ... (lấy dữ liệu nếu cần) ...

            return View(); // Trả về view Areas/Doctor/Views/Home/Index.cshtml
        }
    }
}
