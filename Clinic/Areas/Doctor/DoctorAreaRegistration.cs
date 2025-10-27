using System.Web.Mvc;

namespace Clinic.Areas.Doctor
{
    public class DoctorAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Doctor";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            // /Doctor/{controller}/{action}/{id}
            context.MapRoute(
                name: "Doctor_default",
                url: "Doctor/{controller}/{action}/{id}",
                defaults: new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
