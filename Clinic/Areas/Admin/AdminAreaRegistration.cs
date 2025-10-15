using System.Web.Mvc;

namespace Clinic.Areas.Admin
{
    public class AdminAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Admin";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            var route = context.MapRoute(
                name: "Admin_default",
                url: "Admin/{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "Clinic.Areas.Admin.Controllers" }   // <-- quan trọng
            );
            // Không fallback sang namespace khác
            route.DataTokens["UseNamespaceFallback"] = false;
        }
    }
}
