using System.Web.Mvc;

namespace Clinic.Areas.Admin
{
    public class AdminAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Admin";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            // === DOCTORS === (Giữ nguyên)
            MapControllerRoutes(context, "Doctor", "Doctors");

            // === PATIENTS === (Giữ nguyên)
            MapControllerRoutes(context, "Patients", "Patients");

            // === SERVICES === (Giữ nguyên)
            MapControllerRoutes(context, "Services", "Services");

            // === APPOINTMENTS === (Giữ nguyên)
            MapControllerRoutes(context, "Appointments", "Appointments");

            // === WORK SHIFTS === (Giữ nguyên)
            MapControllerRoutes(context, "WorkShifts", "WorkShifts");

            // === REVIEWS === (Giữ nguyên)
            MapControllerRoutes(context, "Reviews", "Reviews");

            // === RECEPTIONISTS === (Giữ nguyên)
            MapControllerRoutes(context, "Receptionists", "Receptionists");

            // *** THÊM ROUTE CHO SPECIALTIES ***
            MapControllerRoutes(context, "Specialties", "Specialties");
            // *** KẾT THÚC THÊM ***

            // === DEFAULT (để CUỐI CÙNG) === (Giữ nguyên)
            var adminDefault = context.MapRoute(
                name: "Admin_default",
                url: "Admin/{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "Clinic.Areas.Admin.Controllers" }
            );
            adminDefault.DataTokens["UseNamespaceFallback"] = false;
        }

        // --- Helper function to avoid repetition ---
        private void MapControllerRoutes(AreaRegistrationContext context, string controllerName, string urlPrefix)
        {
            var rootRoute = context.MapRoute(
               name: $"Admin_{controllerName}_root",
               url: $"Admin/{urlPrefix}",
               defaults: new { controller = controllerName, action = "Index" },
               namespaces: new[] { "Clinic.Areas.Admin.Controllers" }
           );
            rootRoute.DataTokens["UseNamespaceFallback"] = false;

            var actionsRoute = context.MapRoute(
                name: $"Admin_{controllerName}_actions",
                url: $"Admin/{urlPrefix}/{{action}}/{{id}}",
                defaults: new { controller = controllerName, action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "Clinic.Areas.Admin.Controllers" }
            );
            actionsRoute.DataTokens["UseNamespaceFallback"] = false;
        }
    }
}
