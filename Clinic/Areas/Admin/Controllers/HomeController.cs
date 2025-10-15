using System.Web.Mvc;

namespace Clinic.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,Doctor,Receptionist")]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View(); // tìm Areas/Admin/Views/Home/Index.cshtml
        }
    }
}
