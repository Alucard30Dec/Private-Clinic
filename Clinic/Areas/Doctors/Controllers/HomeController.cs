using System.Web.Mvc;

namespace Clinic.Areas.Doctors.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Bác sĩ Panel";
            ViewBag.Nav = "dashboard";

            // demo số liệu (có thể lấy DB)
            ViewBag.TodayPatients = 24;
            ViewBag.TotalAppointments = 12;
            ViewBag.OnDutyDoctors = 5;
            ViewBag.RevenueLabel = "35.5M";
            return View(); // Areas/Doctors/Views/Home/Index.cshtml
        }
    }
}
