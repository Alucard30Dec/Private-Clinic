using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Clinic.Models;
using Microsoft.AspNet.Identity;
using System.Collections.Generic; // Để dùng List và HashSet

namespace Clinic.Controllers
{
    [Authorize(Roles = "Patient")] // Chỉ bệnh nhân
    public class ReviewsController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: /Reviews/ListReviewable
        // === HÀM NÀY ĐÃ ĐƯỢC SỬA LỖI LINQ ===
        public ActionResult ListReviewable()
        {
            var userId = User.Identity.GetUserId();
            var patient = _db.Patients.FirstOrDefault(p => p.UserId == userId);
            if (patient == null) { return View(new List<Appointment>()); }

            // Lấy lịch hẹn Completed VÀ EndTime đã qua
            var completedAppointments = _db.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == patient.Id
                            && a.Status == AppointmentStatus.Completed
                            && a.EndTime < DateTime.UtcNow) // Đã kết thúc
                .OrderByDescending(a => a.StartTime)
                .ToList();

            // === SỬA LỖI: Lấy list ID ra trước ===
            // 1. Lấy danh sách ID nguyên thủy từ list trên
            var completedAppointmentIds = completedAppointments.Select(a => a.Id).ToList();

            // 2. Dùng danh sách ID đó để truy vấn reviews
            var reviewedAppointmentIds = _db.AppointmentReviews
                                            // Entity Framework có thể dịch .Contains() với list ID nguyên thủy
                                            .Where(r => completedAppointmentIds.Contains(r.AppointmentId))
                                            .Select(r => r.AppointmentId)
                                            .ToHashSet();
            // === KẾT THÚC SỬA ===

            ViewBag.ReviewedIds = reviewedAppointmentIds;
            ViewBag.Title = "Chọn Lịch hẹn để Đánh giá";
            return View(completedAppointments); // Trả về View Views/Reviews/ListReviewable.cshtml
        }


        // GET: Reviews/Create?appointmentId=... (Giữ nguyên)
        public ActionResult Create(int appointmentId)
        {
            var userId = User.Identity.GetUserId();
            var patient = _db.Patients.FirstOrDefault(p => p.UserId == userId);
            if (patient == null) { return RedirectToAction("ListReviewable"); }

            var appointment = _db.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefault(a => a.Id == appointmentId
                                    && a.PatientId == patient.Id
                                    && a.Status == AppointmentStatus.Completed
                                    && a.EndTime < DateTime.UtcNow);

            if (appointment == null)
            {
                TempData["err"] = "Không tìm thấy lịch hẹn đã hoàn thành để đánh giá.";
                return RedirectToAction("ListReviewable");
            }

            bool alreadyReviewed = _db.AppointmentReviews.Any(r => r.AppointmentId == appointmentId);
            if (alreadyReviewed)
            {
                TempData["info"] = "Bạn đã đánh giá lịch hẹn này rồi.";
                return RedirectToAction("ListReviewable");
            }

            var model = new ReviewCreateViewModel
            {
                AppointmentId = appointment.Id,
                DoctorName = appointment.Doctor?.Name ?? "N/A",
                AppointmentDate = appointment.StartTime.ToLocalTime()
            };

            ViewBag.Title = "Viết đánh giá";
            return View(model);
        }

        // POST: Reviews/Create (Giữ nguyên, đã sửa lỗi CS8072 trước đó)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ReviewCreateViewModel model)
        {
            var userId = User.Identity.GetUserId();
            var patient = _db.Patients.FirstOrDefault(p => p.UserId == userId);

            int patientId = -1;
            if (patient == null) { ModelState.AddModelError("", "Không tìm thấy thông tin bệnh nhân."); }
            else { patientId = patient.Id; }

            var appointmentExists = _db.Appointments
                .Any(a => a.Id == model.AppointmentId
                          && a.PatientId == patientId // Dùng biến patientId đã lấy
                          && a.Status == AppointmentStatus.Completed
                          && a.EndTime < DateTime.UtcNow);

            if (!appointmentExists && patient != null)
            { ModelState.AddModelError("", "Lịch hẹn không hợp lệ hoặc chưa kết thúc."); }

            bool alreadyReviewed = _db.AppointmentReviews.Any(r => r.AppointmentId == model.AppointmentId);
            if (alreadyReviewed) { ModelState.AddModelError("", "Bạn đã gửi đánh giá cho lịch hẹn này."); }

            if (ModelState.IsValid)
            {
                var review = new AppointmentReview
                {
                    AppointmentId = model.AppointmentId,
                    Rating = model.Rating,
                    Comments = model.Comments,
                    ReviewDate = DateTime.UtcNow,
                    IsApproved = false
                };
                _db.AppointmentReviews.Add(review);
                _db.SaveChanges();

                TempData["ok"] = "Cảm ơn bạn đã gửi đánh giá!";
                return RedirectToAction("ListReviewable");
            }

            var appointment = _db.Appointments.Include(a => a.Doctor).FirstOrDefault(a => a.Id == model.AppointmentId);
            if (appointment != null) { model.DoctorName = appointment.Doctor?.Name ?? "N/A"; model.AppointmentDate = appointment.StartTime.ToLocalTime(); }
            ViewBag.Title = "Viết đánh giá";
            return View(model);
        }

        // Dispose DbContext (Giữ nguyên)
        protected override void Dispose(bool disposing)
        {
            if (disposing) { _db.Dispose(); }
            base.Dispose(disposing);
        }
    }
}