using System.Web.Mvc;

namespace Clinic.Areas.Admin
{
    // Đổi tên class để tránh trùng với controller (nếu có)
    public class AdminAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Admin";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            // ===== DOCTORS =====
            var doctorsRoot = context.MapRoute(
                name: "Admin_Doctors_root",
                url: "Admin/Doctors",
                defaults: new { controller = "Doctor", action = "Index" },
                namespaces: new[] { "Clinic.Areas.Admin.Controllers" }
            );
            doctorsRoot.DataTokens["UseNamespaceFallback"] = false;

            var doctorsActions = context.MapRoute(
                name: "Admin_Doctors_actions",
                url: "Admin/Doctors/{action}/{id}",
                defaults: new { controller = "Doctor", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "Clinic.Areas.Admin.Controllers" }
            );
            doctorsActions.DataTokens["UseNamespaceFallback"] = false;

            // ===== PATIENTS =====
            var patientsRoot = context.MapRoute(
                name: "Admin_Patients_root",
                url: "Admin/Patients",
                defaults: new { controller = "Patients", action = "Index" },
                namespaces: new[] { "Clinic.Areas.Admin.Controllers" }
            );
            patientsRoot.DataTokens["UseNamespaceFallback"] = false;

            var patientsActions = context.MapRoute(
                name: "Admin_Patients_actions",
                url: "Admin/Patients/{action}/{id}",
                defaults: new { controller = "Patients", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "Clinic.Areas.Admin.Controllers" }
            );
            patientsActions.DataTokens["UseNamespaceFallback"] = false;

            // ===== SERVICES =====
            var servicesRoot = context.MapRoute(
                name: "Admin_Services_root",
                url: "Admin/Services",
                defaults: new { controller = "Services", action = "Index" },
                namespaces: new[] { "Clinic.Areas.Admin.Controllers" }
            );
            servicesRoot.DataTokens["UseNamespaceFallback"] = false;

            var servicesActions = context.MapRoute(
                name: "Admin_Services_actions",
                url: "Admin/Services/{action}/{id}",
                defaults: new { controller = "Services", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "Clinic.Areas.Admin.Controllers" }
            );
            servicesActions.DataTokens["UseNamespaceFallback"] = false;

            // ===== APPOINTMENTS (Quản lý chung bởi Admin) =====
            var appsRoot = context.MapRoute(
                name: "Admin_Appointments_root",
                url: "Admin/Appointments",
                defaults: new { controller = "Appointments", action = "Index" },
                namespaces: new[] { "Clinic.Areas.Admin.Controllers" }
            );
            appsRoot.DataTokens["UseNamespaceFallback"] = false;

            var appsActions = context.MapRoute(
                name: "Admin_Appointments_actions",
                url: "Admin/Appointments/{action}/{id}",
                defaults: new { controller = "Appointments", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "Clinic.Areas.Admin.Controllers" }
            );
            appsActions.DataTokens["UseNamespaceFallback"] = false;

            // ===== REVIEWS (Quản lý bởi Admin) =====
            var reviewsRoot = context.MapRoute(
                name: "Admin_Reviews_root",
                url: "Admin/Reviews",
                defaults: new { controller = "Reviews", action = "Index" },
                namespaces: new[] { "Clinic.Areas.Admin.Controllers" }
            );
            reviewsRoot.DataTokens["UseNamespaceFallback"] = false;

            var reviewsActions = context.MapRoute(
                name: "Admin_Reviews_actions",
                url: "Admin/Reviews/{action}/{id}",
                defaults: new { controller = "Reviews", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "Clinic.Areas.Admin.Controllers" }
            );
            reviewsActions.DataTokens["UseNamespaceFallback"] = false;


            // --- ĐÃ XÓA CÁC ROUTE CHO Reception VÀ Requests ---

            // ===== DEFAULT (để CUỐI CÙNG) =====
            // Route này sẽ bắt các URL Admin không khớp với các route cụ thể ở trên
            var adminDefault = context.MapRoute(
                name: "Admin_default",
                url: "Admin/{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }, // Trang chủ mặc định của Admin
                namespaces: new[] { "Clinic.Areas.Admin.Controllers" }
            );
            adminDefault.DataTokens["UseNamespaceFallback"] = false;
        }
    }
}
