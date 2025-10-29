using System.Web.Mvc;

namespace Clinic.Areas.Reception
{
    public class ReceptionAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Reception";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            // Route cho /Reception (trang chính của Lễ tân)
            var receptionHome = context.MapRoute(
               name: "Reception_Home",
               url: "Reception",
               defaults: new { controller = "Dashboard", action = "Index" }, // Trỏ đến DashboardController mới
               namespaces: new[] { "Clinic.Areas.Reception.Controllers" }
           );
            receptionHome.DataTokens["UseNamespaceFallback"] = false;

            // Route chung cho các controller khác trong Area Reception
            var receptionDefault = context.MapRoute(
               name: "Reception_default",
               url: "Reception/{controller}/{action}/{id}",
               defaults: new { controller = "Dashboard", action = "Index", id = UrlParameter.Optional }, // Mặc định về Dashboard
                namespaces: new[] { "Clinic.Areas.Reception.Controllers" }
           );
            receptionDefault.DataTokens["UseNamespaceFallback"] = false;
        }
    }
}
