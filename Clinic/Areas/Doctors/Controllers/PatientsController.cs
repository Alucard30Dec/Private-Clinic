using System.Web.Mvc;

namespace Clinic.Areas.Doctors.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class PatientsController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Nav = "mypatients";
            return View();
        }
    }
}
