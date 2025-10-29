using System.Web.Mvc;

namespace Clinic.Areas.Doctor
{
    public class DoctorAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Doctor";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            // Route mặc định cho /Doctor -> Doctor/Home/Index
            var doctorHome = context.MapRoute(
                name: "Doctor_Home",
                url: "Doctor", // URL chỉ là /Doctor
                defaults: new { controller = "Home", action = "Index" }, // Trỏ đến HomeController.Index
                namespaces: new[] { "Clinic.Areas.Doctor.Controllers" } // Chỉ định namespace
            );
            doctorHome.DataTokens["UseNamespaceFallback"] = false; // Ngăn fallback ra namespace gốc

            // Route chung cho các controller khác trong Area Doctor: /Doctor/{controller}/{action}/{id}
            var doctorDefault = context.MapRoute(
                name: "Doctor_default",
                url: "Doctor/{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }, // Mặc định về Home/Index
                namespaces: new[] { "Clinic.Areas.Doctor.Controllers" } // Chỉ định namespace
            );
            doctorDefault.DataTokens["UseNamespaceFallback"] = false; // Ngăn fallback
        }
    }
}
