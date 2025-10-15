using System.Linq;
using System.Web.Mvc;
using Clinic.Models;

namespace Clinic.Controllers
{
    public class HealthController : Controller
    {
        // 1) Kiểm tra số lượng dữ liệu đã populate
        public ActionResult Db()
        {
            using (var db = new ClinicDbContext())
            {
                var dr = db.Doctors.Count();
                var sv = db.Services.Count();
                var wh = db.WorkingHours.Count();
                return Content($"Doctors={dr}; Services={sv}; WorkingHours={wh}");
            }
        }

        // 2) Xem nhanh danh sách Doctor/Service (đảm bảo đọc từ DB)
        public ActionResult Peek()
        {
            using (var db = new ClinicDbContext())
            {
                var topDocs = db.Doctors.OrderBy(x => x.Id).Take(5).ToList();
                var topSv = db.Services.OrderBy(x => x.Id).Take(5).ToList();
                return Json(new
                {
                    Doctors = topDocs.Select(d => new { d.Id, d.Name, d.Specialty }),
                    Services = topSv.Select(s => new { s.Id, s.Name, s.Fee, s.DurationMinutes })
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // 3) Kiểm tra quyền Admin (phải bị chặn nếu chưa login hoặc không phải Admin)
        [Authorize(Roles = "Admin")]
        public ActionResult AdminOnly()
        {
            return Content("OK: Bạn có quyền Admin");
        }
    }
}
