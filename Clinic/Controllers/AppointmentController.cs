using System;
using System.Web.Mvc;
using Clinic.Models;

namespace Clinic.Controllers
{
    public class AppointmentController : Controller
    {
        // Dùng DbContext domain của bạn
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: /Appointment
        [AllowAnonymous]
        public ActionResult Index()
        {
            return View(new AppointmentRequest());
        }

        // POST: /Appointment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Create(AppointmentRequest model)
        {
            if (!ModelState.IsValid)
            {
                // Trả lại form kèm lỗi
                return View("Index", model);
            }

            // Bảo vệ: nếu chưa set thời điểm tạo thì set UTC
            if (model.CreatedAt == default(DateTime))
                model.CreatedAt = DateTime.UtcNow;

            _db.AppointmentRequests.Add(model);
            _db.SaveChanges();

            // Hiển thị lời cảm ơn NGAY TRÊN TRANG (không redirect layout khác)
            ModelState.Clear(); // xóa dữ liệu cũ trong form
            ViewBag.Success = "Cảm ơn bạn! Chúng tôi đã nhận yêu cầu đặt lịch. Lễ tân sẽ sớm liên hệ.";

            // Trả về Index với form rỗng + thông báo thành công
            return View("Index", new AppointmentRequest());
        }

        // (Giữ lại nếu sau này muốn dùng trang cảm ơn riêng)
        [AllowAnonymous]
        public ActionResult SubmittedReception()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
