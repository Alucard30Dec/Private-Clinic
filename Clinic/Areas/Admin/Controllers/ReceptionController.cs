using System;
using System.Linq;
using System.Web.Mvc;
using Clinic.Models;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using System.Collections.Generic;

namespace Clinic.Areas.Admin.Controllers
{
    [Authorize(Roles = "Receptionist,Admin")]
    public class ReceptionController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: /Admin/Reception/Index (Hoặc Today nếu bạn giữ tên cũ)
        // Hiển thị danh sách lịch hẹn (đã có tìm kiếm)
        // Sửa lại action name thành Index để khớp link menu
        public ActionResult Index(string filter = "today", string searchQuery = null)
        {
            ViewBag.Nav = "reception_appointments"; // Key cho menu highlight
            ViewBag.CurrentFilter = filter;
            ViewBag.SearchQuery = searchQuery;

            var query = _db.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.Service) // Vẫn include Service để phòng trường hợp View cần
                .Where(a => a.Status != AppointmentStatus.Canceled);

            if (filter == "today")
            {
                var localStart = DateTime.Today;
                var localEnd = localStart.AddDays(1);
                var startUtc = DateTime.SpecifyKind(localStart, DateTimeKind.Local).ToUniversalTime();
                var endUtc = DateTime.SpecifyKind(localEnd, DateTimeKind.Local).ToUniversalTime();
                query = query.Where(a => a.StartTime >= startUtc && a.StartTime < endUtc);
                ViewBag.Title = "Lịch hẹn Hôm nay";
            }
            else { ViewBag.Title = "Tất cả Lịch hẹn"; }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                string searchLower = searchQuery.ToLower().Trim();
                query = query.Where(a =>
                    (a.Doctor != null && a.Doctor.Name.ToLower().Contains(searchLower)) ||
                    (a.Patient != null && a.Patient.FullName.ToLower().Contains(searchLower)) ||
                    (a.Patient != null && a.Patient.Email.ToLower().Contains(searchLower)) ||
                    (a.Service != null && a.Service.Name.ToLower().Contains(searchLower)) // Vẫn tìm theo Service Name nếu có
                );
            }

            var list = query.OrderBy(a => a.StartTime).ToList();
            return View(list); // Trả về View Index.cshtml (nếu bạn đã đổi tên)
                               // hoặc Today.cshtml (nếu bạn giữ tên cũ)
        }


        // GET: /Admin/Reception/CreateAppointmentForRequest?requestId=...
        // Hiển thị form đặt lịch cho khách vãng lai (Đã bỏ ServiceId khỏi ViewBag)
        public ActionResult CreateAppointmentForRequest(int requestId)
        {
            var request = _db.AppointmentRequests.Find(requestId);
            if (request == null || request.IsHandled) { /*...*/ return RedirectToAction("Index", "Requests"); }

            ViewBag.DoctorId = new SelectList(_db.Doctors.OrderBy(d => d.Name), "Id", "Name");
            ViewBag.StatusList = new SelectList(Enum.GetValues(typeof(AppointmentStatus)).Cast<AppointmentStatus>()
                                                .Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString() }),
                                                "Value", "Text", AppointmentStatus.Confirmed);

            var model = new Appointment
            {
                Notes = $"Từ đơn #{request.Id}: {request.Name} ({request.Email} / {request.Phone})\nNgày mong muốn: {request.DesiredDate:dd/MM/yyyy}\nLời nhắn: {request.Message}",
                StartTime = GetNextAvailableSlotStart(DateTime.Now.AddMinutes(15)),
                EndTime = GetNextAvailableSlotStart(DateTime.Now.AddMinutes(15)).AddMinutes(30), // Vẫn giữ 30 phút mặc định
                Status = AppointmentStatus.Confirmed,
                // === QUAN TRỌNG: Gán ServiceId hợp lệ ===
                ServiceId = 2 /* <-- THAY BẰNG ID CỦA MỘT DỊCH VỤ CÓ THẬT TRONG BẢNG Services (ví dụ: Khám tổng quát) */
            };

            ViewBag.RequestInfo = request;
            ViewBag.Title = "Đặt lịch cho Khách vãng lai";
            return View(model); // Trả về View CreateAppointmentForRequest.cshtml
        }


        // POST: /Admin/Reception/CreateAppointmentForRequest
        // Xử lý submit form đặt lịch (Đã gán cứng ServiceId)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAppointmentForRequest([Bind(Include = "Id,DoctorId,StartTime,EndTime,Status,Notes")] Appointment appointment, int requestId)
        {
            var request = _db.AppointmentRequests.Find(requestId);
            if (request == null || request.IsHandled) { /*...*/ return RedirectToAction("Index", "Requests"); }

            int patientId = GetOrCreatePatientIdForRequest(request);
            appointment.PatientId = patientId;
            appointment.CreatedAt = DateTime.UtcNow;
            // === QUAN TRỌNG: Gán ServiceId hợp lệ ===
            appointment.ServiceId = 2; /* <-- THAY BẰNG ID CỦA MỘT DỊCH VỤ CÓ THẬT TRONG BẢNG Services (phải giống số ở trên) */

            // Chuyển giờ Local sang UTC
            if (appointment.StartTime.Kind == DateTimeKind.Unspecified) appointment.StartTime = DateTime.SpecifyKind(appointment.StartTime, DateTimeKind.Local).ToUniversalTime(); else if (appointment.StartTime.Kind == DateTimeKind.Local) appointment.StartTime = appointment.StartTime.ToUniversalTime();
            if (appointment.EndTime.Kind == DateTimeKind.Unspecified) appointment.EndTime = DateTime.SpecifyKind(appointment.EndTime, DateTimeKind.Local).ToUniversalTime(); else if (appointment.EndTime.Kind == DateTimeKind.Local) appointment.EndTime = appointment.EndTime.ToUniversalTime();

            if (ModelState.IsValid)
            {
                // (Thêm kiểm tra trùng lịch ở đây nếu bạn triển khai AppointmentService)
                var svc = new AppointmentService(_db); // Giả sử bạn có class này
                if (!svc.IsSlotAvailable(appointment.DoctorId, appointment.StartTime, appointment.EndTime))
                {
                    ModelState.AddModelError("", "Khung giờ đã có người đặt hoặc không hợp lệ.");
                    // Tải lại dữ liệu cho form và hiển thị lỗi
                    ViewBag.DoctorId = new SelectList(_db.Doctors.OrderBy(d => d.Name), "Id", "Name", appointment.DoctorId);
                    ViewBag.StatusList = new SelectList(Enum.GetValues(typeof(AppointmentStatus)).Cast<AppointmentStatus>()
                                                        .Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString() }),
                                                        "Value", "Text", appointment.Status);
                    ViewBag.RequestInfo = request;
                    ViewBag.Title = "Đặt lịch cho Khách vãng lai";
                    return View(appointment);
                }


                _db.Appointments.Add(appointment);
                request.IsHandled = true;
                _db.SaveChanges();

                TempData["ok"] = $"Đã đặt lịch thành công cho khách {request.Name}.";
                return RedirectToAction("Index", "Requests"); // Về danh sách đơn
            }

            // Nếu lỗi ModelState, tải lại dữ liệu cho form
            ViewBag.DoctorId = new SelectList(_db.Doctors.OrderBy(d => d.Name), "Id", "Name", appointment.DoctorId);
            ViewBag.StatusList = new SelectList(Enum.GetValues(typeof(AppointmentStatus)).Cast<AppointmentStatus>()
                                                .Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString() }),
                                                "Value", "Text", appointment.Status);
            ViewBag.RequestInfo = request;
            ViewBag.Title = "Đặt lịch cho Khách vãng lai";
            return View(appointment);
        }

        // --- Hàm trợ giúp (Giữ nguyên) ---
        private int GetOrCreatePatientIdForRequest(AppointmentRequest request)
        {
            Patient patient = null;
            if (!string.IsNullOrWhiteSpace(request.Email)) patient = _db.Patients.FirstOrDefault(p => p.Email == request.Email);
            if (patient == null && !string.IsNullOrWhiteSpace(request.Phone)) patient = _db.Patients.FirstOrDefault(p => p.PhoneNumber == request.Phone);
            if (patient != null) return patient.Id;
            patient = new Patient { FullName = request.Name, Email = request.Email, PhoneNumber = request.Phone, CreatedAt = DateTime.UtcNow };
            _db.Patients.Add(patient); _db.SaveChanges(); return patient.Id;
        }
        private DateTime GetNextAvailableSlotStart(DateTime nowLocal, int intervalMinutes = 15)
        {
            long ticks = nowLocal.Ticks; long intervalTicks = TimeSpan.FromMinutes(intervalMinutes).Ticks; long roundedTicks = ((ticks + intervalTicks - 1) / intervalTicks) * intervalTicks; return new DateTime(roundedTicks, nowLocal.Kind);
        }

        // Dispose DbContext (Giữ nguyên)
        protected override void Dispose(bool disposing) { if (disposing) { _db.Dispose(); } base.Dispose(disposing); }
    }
}