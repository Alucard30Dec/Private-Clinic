using System;
using System.Linq;
using System.Web.Mvc;
using Clinic.Models;
using Microsoft.AspNet.Identity;

namespace Clinic.Areas.Admin.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // Lịch của tôi (hôm nay)
        public ActionResult Today()
        {
            ViewBag.Nav = "today";

            // 1) Lấy "hôm nay" theo LOCAL, rồi chuyển sang UTC ở ngoài LINQ
            var localStart = DateTime.Today;                 // 00:00 local
            var localEnd = localStart.AddDays(1);          // 00:00 ngày mai local

            var startUtc = DateTime.SpecifyKind(localStart, DateTimeKind.Local).ToUniversalTime();
            var endUtc = DateTime.SpecifyKind(localEnd, DateTimeKind.Local).ToUniversalTime();

            // 2) Dùng biến UTC trong Where (KHÔNG gọi ToUniversalTime() trong LINQ)
            var list = _db.Appointments
                .Where(a => a.Status != AppointmentStatus.Canceled
                         && a.StartTime >= startUtc
                         && a.StartTime < endUtc)
                .OrderBy(a => a.StartTime)
                .ToList();

            ViewBag.Date = localStart;
            return View(list);
        }

    }
}
