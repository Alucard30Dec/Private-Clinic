using System.Web.Mvc;

namespace Clinic.Areas.Doctor.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class RecordsController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Nav = "records";
            return View();
        }
    }
}
