using System;
using System.Collections.Generic;
using System.Linq; // <-- THÊM: Cần thiết cho .OrderBy() và .Take()
using System.Web;
using System.Web.Mvc;
using Clinic.Models; // <-- THÊM: Cần thiết để dùng ClinicDbContext và Doctor

namespace Clinic.Controllers
{
    public class HomeController : Controller
    {
        // THÊM: Khai báo DbContext để truy cập CSDL
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // SỬA: Hàm Index() để lấy 3 bác sĩ
        public ActionResult Index()
        {
            // Lấy 3 bác sĩ đại diện
            // (Bạn có thể đổi .OrderBy(d => d.Name) thành .OrderByDescending(d => d.YearsOfExperience)
            // để lấy 3 bác sĩ nhiều kinh nghiệm nhất)
            var featuredDoctors = _db.Doctors
                .OrderBy(d => d.Name)
                .Take(3)
                .ToList();

            // Gửi danh sách 3 bác sĩ này đến View (Views/Home/Index.cshtml)
            return View(featuredDoctors);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        // THÊM: Phương thức Dispose để giải phóng DbContext
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}