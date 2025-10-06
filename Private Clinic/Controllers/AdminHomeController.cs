using System.Web.Mvc;
using Private_Clinic.Filters;
using Private_Clinic.Models;

[AuthorizeRole(UserRole.Admin)]
public class AdminHomeController : Controller
{
    public ActionResult Index() => View();
}
