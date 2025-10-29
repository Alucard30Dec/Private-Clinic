using Clinic.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Mvc;


namespace Clinic.Areas.Reception.Controllers
{
    [Authorize(Roles = "Receptionist,Admin")]
    public class ReceptionController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // Helper functions (GetEnumDisplayName, CreateExamTypeList, CreateStatusList)
        private string GetEnumDisplayName(Enum value)
        { /* ... Giữ nguyên ... */
            try
            {
                return value.GetType()
                            .GetMember(value.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString();
            }
            catch { return value.ToString(); }
        }
        private SelectList CreateExamTypeList(int? selectedValue = null)
        { /* ... Giữ nguyên ... */
            return new SelectList(
                Enum.GetValues(typeof(ExamType)).Cast<ExamType>().Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = GetEnumDisplayName(e)
                }),
                "Value", "Text", selectedValue);
        }
        private SelectList CreateStatusList(int? selectedValue = null)
        { /* ... Giữ nguyên ... */
            return new SelectList(
               Enum.GetValues(typeof(AppointmentStatus)).Cast<AppointmentStatus>()
                   .Select(s => new SelectListItem { Value = ((int)s).ToString(), Text = s.ToString() }),
               "Value", "Text", selectedValue);
        }

        // GET: /Reception/Reception/AppointmentsList
        public async Task<ActionResult> AppointmentsList(string filter = "today", string searchQuery = null)
        { /* ... Giữ nguyên ... */
            ViewBag.Nav = "reception_appointments";
            ViewBag.CurrentFilter = filter;
            ViewBag.SearchQuery = searchQuery;

            var query = _db.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.Service)
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
                    (a.Service != null && a.Service.Name.ToLower().Contains(searchLower))
                );
            }

            var list = await query.OrderBy(a => a.StartTime).ToListAsync();
            return View("~/Areas/Reception/Views/Reception/AppointmentsList.cshtml", list);
        }


        // GET: /Reception/Reception/CreateAppointmentForRequest?requestId=...
        public async Task<ActionResult> CreateAppointmentForRequest(int requestId)
        { /* ... Giữ nguyên ... */
            var request = await _db.AppointmentRequests.FindAsync(requestId);
            if (request == null || request.IsHandled)
            {
                TempData["err"] = "Không tìm thấy hoặc yêu cầu đã được xử lý.";
                return RedirectToAction("Index", "Requests", new { area = "Reception" });
            }

            var doctors = await _db.Doctors
                .Where(d => d.Specialty == request.Specialty)
                .OrderBy(d => d.Name)
                .ToListAsync();

            ViewBag.DoctorId = new SelectList(doctors, "Id", "Name");
            ViewBag.ExamTypeList = CreateExamTypeList((int)ExamType.Service);
            ViewBag.StatusList = CreateStatusList((int)AppointmentStatus.Confirmed);

            var requestedSlotUtc = request.RequestedSlot;
            var defaultService = await _db.Services
                                       .FirstOrDefaultAsync(s => s.ExamType == ExamType.Service);

            int defaultServiceId = defaultService?.Id ?? 0;
            int duration = defaultService?.DurationMinutes ?? 30;

            var model = new Appointment
            {
                Notes = $"Từ đơn #{request.Id} ({request.Specialty}): {request.Name} ({request.Email} / {request.Phone})\nLời nhắn: {request.Message}",
                StartTime = requestedSlotUtc,
                EndTime = requestedSlotUtc.AddMinutes(duration),
                Status = AppointmentStatus.Confirmed,
                ServiceId = defaultServiceId,
                ExamType = ExamType.Service
            };

            ViewBag.RequestInfo = request;
            ViewBag.Title = "Đặt lịch cho Khách vãng lai";
            return View("~/Areas/Reception/Views/Reception/CreateAppointmentForRequest.cshtml", model);
        }


        // POST: /Reception/Reception/CreateAppointmentForRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAppointmentForRequest(
            [Bind(Include = "DoctorId,ExamType,ServiceId,StartTime,EndTime,Status,Notes")] Appointment appointment,
            int requestId)
        {
            var request = await _db.AppointmentRequests.FindAsync(requestId);
            if (request == null || request.IsHandled)
            {
                TempData["err"] = "Yêu cầu đã được xử lý hoặc không tồn tại.";
                return RedirectToAction("Index", "Requests", new { area = "Reception" });
            }

            var selectedService = await _db.Services
                                            .FirstOrDefaultAsync(s => s.ExamType == appointment.ExamType);

            appointment.ServiceId = selectedService?.Id ?? 0;
            int duration = selectedService?.DurationMinutes ?? 30;

            Func<Task> reloadViewBags = async () =>
            {
                var doctors = await _db.Doctors
                   .Where(d => d.Specialty == request.Specialty)
                   .OrderBy(d => d.Name)
                   .ToListAsync();
                ViewBag.DoctorId = new SelectList(doctors, "Id", "Name", appointment.DoctorId);
                ViewBag.ExamTypeList = CreateExamTypeList((int)appointment.ExamType);
                ViewBag.StatusList = CreateStatusList((int)appointment.Status);
                ViewBag.RequestInfo = request;
                ViewBag.Title = "Đặt lịch cho Khách vãng lai";
            };

            if (appointment.ServiceId == 0)
            {
                ModelState.AddModelError("ExamType", $"Không tìm thấy dịch vụ phù hợp cho loại hình khám '{GetEnumDisplayName(appointment.ExamType)}'.");
            }
            if (appointment.DoctorId == 0)
            {
                ModelState.AddModelError("DoctorId", "Vui lòng chọn bác sĩ.");
            }

            int patientId = 0;
            try
            {
                patientId = await GetOrCreatePatientIdForRequestAsync(request);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            if (patientId > 0)
            {
                appointment.PatientId = patientId;
            }
            appointment.CreatedAt = DateTime.UtcNow;

            if (appointment.StartTime.Kind == DateTimeKind.Unspecified)
                appointment.StartTime = DateTime.SpecifyKind(appointment.StartTime, DateTimeKind.Local).ToUniversalTime();
            else if (appointment.StartTime.Kind == DateTimeKind.Local)
                appointment.StartTime = appointment.StartTime.ToUniversalTime();
            appointment.EndTime = appointment.StartTime.AddMinutes(duration);
            if (appointment.EndTime <= appointment.StartTime)
            {
                ModelState.AddModelError("EndTime", "Thời gian kết thúc không hợp lệ.");
            }

            if (ModelState.IsValid)
            {
                var svc = new AppointmentService(_db);
                if (!svc.IsSlotAvailable(appointment.DoctorId, appointment.StartTime, appointment.EndTime))
                {
                    ModelState.AddModelError("", "Khung giờ đã có người đặt hoặc không hợp lệ.");
                    await reloadViewBags();
                    return View("~/Areas/Reception/Views/Reception/CreateAppointmentForRequest.cshtml", appointment);
                }

                _db.Appointments.Add(appointment);
                request.IsHandled = true;
                await _db.SaveChangesAsync();

                TempData["ok"] = $"Đã đặt lịch thành công cho khách {request.Name}.";
                // *** SỬA REDIRECT Ở ĐÂY ***
                return RedirectToAction("AppointmentsList", "Reception", new { area = "Reception" }); // Chuyển đến trang danh sách lịch hẹn
            }

            await reloadViewBags();
            return View("~/Areas/Reception/Views/Reception/CreateAppointmentForRequest.cshtml", appointment);
        }


        // GET: /Reception/Reception/GetServicePrice
        [HttpGet]
        public async Task<JsonResult> GetServicePrice(int examType)
        { /* ... Giữ nguyên ... */
            try
            {
                var examTypeValue = (ExamType)examType;
                var service = await _db.Services
                                       .FirstOrDefaultAsync(s => s.ExamType == examTypeValue);

                if (service != null)
                {
                    return Json(new { success = true, serviceName = service.Name, price = service.Fee }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = $"Không tìm thấy dịch vụ cho '{GetEnumDisplayName(examTypeValue)}'." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetServicePrice: {ex.Message}");
                return Json(new { success = false, message = "Lỗi hệ thống khi lấy giá dịch vụ." }, JsonRequestBehavior.AllowGet);
            }
        }

        // --- Hàm trợ giúp GetOrCreatePatientIdForRequestAsync ---
        private async Task<int> GetOrCreatePatientIdForRequestAsync(AppointmentRequest request)
        { /* ... Giữ nguyên ... */
            Patient patient = null;
            if (!string.IsNullOrWhiteSpace(request.Email))
                patient = await _db.Patients.FirstOrDefaultAsync(p => p.Email == request.Email);
            if (patient == null && !string.IsNullOrWhiteSpace(request.Phone))
                patient = await _db.Patients.FirstOrDefaultAsync(p => p.PhoneNumber == request.Phone);

            if (patient != null) return patient.Id;

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new InvalidOperationException("Không thể tạo bệnh nhân mới mà không có tên.");
            }

            patient = new Patient
            {
                FullName = request.Name,
                Email = request.Email,
                PhoneNumber = request.Phone,
                CreatedAt = DateTime.UtcNow
            };
            _db.Patients.Add(patient);
            // ID sẽ được gán sau khi SaveChanges ở action POST
            return 0; // Trả về 0 để biết là bệnh nhân mới
        }


        // Dispose DbContext
        protected override void Dispose(bool disposing) { if (disposing) { _db.Dispose(); } base.Dispose(disposing); }
    }
}

