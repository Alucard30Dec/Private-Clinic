using System.Web.Mvc;

namespace Clinic.Areas.Doctors
{
    public class DoctorsAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Doctors";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "Doctors_default",
                "Doctors/{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
