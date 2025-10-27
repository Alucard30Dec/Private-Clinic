using System.Web.Mvc;

namespace Clinic.Areas.Doctor.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class HomeController : Controller
    {
        // /Doctor/Home
        public ActionResult Index()
        {
            ViewBag.Title = "Bác sĩ Panel";
            ViewBag.Nav = "dashboard";

            // demo số liệu; nếu muốn bạn có thể lấy từ DB
            ViewBag.TodayPatients = 24;
            ViewBag.TotalAppointments = 12;
            ViewBag.OnDutyDoctors = 5;
            ViewBag.RevenueLabel = "35.5M";

            return View();
        }
    }
}
