using System.Web.Mvc;

namespace Clinic.Areas.Doctor // <-- Đổi namespace (nếu cần)
{
    public class DoctorAreaRegistration : AreaRegistration
    {
        // SỬA 1: Đổi tên AreaName
        public override string AreaName
        {
            get
            {
                return "Doctor"; // <-- SỬA TÊN NÀY (từ "Doctors")
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            // SỬA 2: Đổi đường dẫn URL
            context.MapRoute(
                "Doctor_default",
                "Doctor/{controller}/{action}/{id}", // <-- SỬA ĐƯỜNG DẪN NÀY
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}