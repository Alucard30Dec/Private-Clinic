using System.Web.Mvc;
using Private_Clinic.Filters;
using Private_Clinic.Models;

[AuthorizeRole(UserRole.Patient)]
public class PatientController : Controller
{
    public ActionResult Dashboard() => View();
}
