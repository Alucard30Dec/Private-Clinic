using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Clinic.Models;

namespace Clinic.Controllers
{
    public class DoctorsController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: /Doctors
        public ActionResult Index(DoctorsFilterVm filter)
        {
            if (filter == null) filter = new DoctorsFilterVm();

            // Chuẩn hoá input
            var query = (filter.Query ?? string.Empty).Trim();
            var specialty = (filter.Specialty ?? string.Empty).Trim();

            // Bảo vệ Page/PageSize
            if (filter.Page < 1) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 9;           // mặc định hợp lý
            if (filter.PageSize > 48) filter.PageSize = 48;          // giới hạn chống lạm dụng

            var q = _db.Doctors.AsQueryable();

            // Tìm theo tên (không phân biệt hoa/thường, an toàn null)
            if (!string.IsNullOrEmpty(query))
            {
                var key = query.ToLower();
                q = q.Where(d => (d.Name ?? "").ToLower().Contains(key));
            }

            // Lọc theo chuyên khoa (so sánh chính xác, có thể đổi sang ToLower() nếu muốn bỏ qua hoa/thường)
            if (!string.IsNullOrEmpty(specialty))
            {
                q = q.Where(d => (d.Specialty ?? "") == specialty);
            }

            var total = q.Count();

            var skip = (filter.Page - 1) * filter.PageSize;
            if (skip < 0) skip = 0;

            var items = q
                .OrderBy(d => d.Name)
                .Skip(skip)
                .Take(filter.PageSize)
                .ToList();

            var specialties = _db.Doctors
                .Select(d => d.Specialty)
                .Where(s => s != null && s != "")
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            var vm = new DoctorsIndexVm
            {
                Filter = filter,
                Specialties = specialties,
                Result = new PagedResult<Doctor>
                {
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalItems = total,
                    Items = items
                }
            };

            return View(vm);
        }

        // GET: /Doctors/Details/5
        public ActionResult Details(int id)
        {
            // Tìm bác sĩ theo Id, trả 404 nếu không có
            var doctor = _db.Doctors.SingleOrDefault(d => d.Id == id);
            if (doctor == null)
            {
                return HttpNotFound(); // 404
            }

            return View(doctor); // View: Views/Doctors/Details.cshtml (đã gửi bạn trước đó)
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
