using Clinic.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Keep this
using System.Data.Entity;
using System.Linq;
using System.Reflection; // Keep this
using System.Threading.Tasks;
using System.Web.Mvc;


namespace Clinic.Areas.Reception.Controllers
{
    [Authorize(Roles = "Receptionist,Admin")]
    public class ReceptionController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // Helper functions (GetEnumDisplayName, CreateExamTypeList, CreateStatusList - Keep as is)
        private string GetEnumDisplayName(Enum value)
        {
            try { return value.GetType().GetMember(value.ToString()).First().GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString(); } catch { return value.ToString(); }
        }
        private SelectList CreateExamTypeList(int? selectedValue = null)
        {
            return new SelectList(
               Enum.GetValues(typeof(ExamType)).Cast<ExamType>().Select(e => new SelectListItem
               {
                   Value = ((int)e).ToString(),
                   Text = GetEnumDisplayName(e) // Using helper
               }),
               "Value", "Text", selectedValue);
        }
        private SelectList CreateStatusList(int? selectedValue = null)
        {
            return new SelectList(
               Enum.GetValues(typeof(AppointmentStatus)).Cast<AppointmentStatus>().Select(s => new SelectListItem
               {
                   Value = ((int)s).ToString(),
                   Text = s.ToString() // Enum name is fine for status
               }),
               "Value", "Text", selectedValue);
        }


        // GET: /Reception/Reception/AppointmentsList (Keep as is)
        public async Task<ActionResult> AppointmentsList(string filter = "today", string searchQuery = null)
        {
            ViewBag.Nav = "reception_appointments";
            ViewBag.CurrentFilter = filter;
            ViewBag.SearchQuery = searchQuery;

            var query = _db.Appointments
                           .Include(a => a.Doctor)
                           .Include(a => a.Patient)
                           .Include(a => a.Service)
                           .Where(a => a.Status != AppointmentStatus.Canceled); // Exclude canceled

            // Time filtering
            var todayStartUtc = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Local).ToUniversalTime();
            var todayEndUtc = todayStartUtc.AddDays(1);

            if (filter == "today")
            {
                query = query.Where(a => a.StartTime >= todayStartUtc && a.StartTime < todayEndUtc);
                ViewBag.Title = "Lịch hẹn Hôm nay";
            }
            else // filter == "all" or anything else
            {
                ViewBag.Title = "Tất cả Lịch hẹn";
            }

            // Search logic
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                string sqLower = searchQuery.Trim().ToLower();
                query = query.Where(a =>
                    (a.Doctor != null && a.Doctor.Name.ToLower().Contains(sqLower)) ||
                    (a.Patient != null && a.Patient.FullName.ToLower().Contains(sqLower)) ||
                    (a.Patient != null && a.Patient.Email != null && a.Patient.Email.ToLower().Contains(sqLower)) ||
                    (a.Service != null && a.Service.Name.ToLower().Contains(sqLower))
                );
            }

            var list = await query.OrderBy(a => a.StartTime).ToListAsync();
            return View("~/Areas/Reception/Views/Reception/AppointmentsList.cshtml", list);
        }


        // GET: /Reception/Reception/CreateAppointmentForRequest?requestId=...
        public async Task<ActionResult> CreateAppointmentForRequest(int requestId)
        {
            var request = await _db.AppointmentRequests.FindAsync(requestId);
            if (request == null || request.IsHandled)
            {
                TempData["err"] = "Không tìm thấy hoặc yêu cầu đã được xử lý.";
                return RedirectToAction("Index", "Requests", new { area = "Reception" });
            }

            // *** FIX: Compare Doctor.Specialty.Name ***
            var doctors = await _db.Doctors
                .Include(d => d.Specialty) // Include Specialty to access its Name
                .Where(d => d.IsVisible && d.Specialty != null && d.Specialty.Name == request.Specialty) // Filter by Specialty.Name
                .OrderBy(d => d.Name)
                .ToListAsync();

            ViewBag.DoctorId = new SelectList(doctors, "Id", "Name");
            ViewBag.ExamTypeList = CreateExamTypeList((int)ExamType.Service); // Default to Service type
            ViewBag.StatusList = CreateStatusList((int)AppointmentStatus.Confirmed); // Default to Confirmed

            var requestedSlotUtc = request.RequestedSlot;
            // Find a visible service matching the Service ExamType
            var defaultService = await _db.Services.FirstOrDefaultAsync(s => s.ExamType == ExamType.Service && s.IsVisible);

            int defaultServiceId = defaultService?.Id ?? 0;
            int duration = defaultService?.DurationMinutes ?? 30; // Default 30 mins if no service found

            var model = new Appointment
            {
                // Pre-fill notes with request details
                Notes = $"Từ đơn #{request.Id} ({request.Specialty}): {request.Name} ({request.Email} / {request.Phone})\nLời nhắn: {request.Message}",
                StartTime = requestedSlotUtc, // Use UTC time from request
                EndTime = requestedSlotUtc.AddMinutes(duration), // Calculate EndTime based on default duration
                Status = AppointmentStatus.Confirmed, // Default status
                ServiceId = defaultServiceId, // Assign default service ID
                ExamType = ExamType.Service // Default exam type
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

            // Find default visible service for the selected ExamType
            var selectedService = await _db.Services
                                          .FirstOrDefaultAsync(s => s.ExamType == appointment.ExamType && s.IsVisible);

            // Assign ServiceId based on ExamType BEFORE validation
            appointment.ServiceId = selectedService?.Id ?? 0; // Use 0 if no service found
            int duration = selectedService?.DurationMinutes ?? 30; // Default duration

            // Validation: Check if ServiceId is valid (i.e., service was found)
            if (appointment.ServiceId == 0)
            {
                ModelState.AddModelError("ExamType", $"Không tìm thấy dịch vụ phù hợp cho loại hình khám '{GetEnumDisplayName(appointment.ExamType)}'.");
            }

            // Helper to reload ViewBag data on error
            Func<Task> reloadViewBags = async () =>
            {
                var doctors = await _db.Doctors
                   .Include(d => d.Specialty)
                   .Where(d => d.IsVisible && d.Specialty != null && d.Specialty.Name == request.Specialty)
                   .OrderBy(d => d.Name)
                   .ToListAsync();
                ViewBag.DoctorId = new SelectList(doctors, "Id", "Name", appointment.DoctorId);
                ViewBag.ExamTypeList = CreateExamTypeList((int)appointment.ExamType);
                ViewBag.StatusList = CreateStatusList((int)appointment.Status);
                ViewBag.RequestInfo = request; // Keep request info
                ViewBag.Title = "Đặt lịch cho Khách vãng lai"; // Reset title
            };

            // --- Validation ---
            // Doctor Selection
            if (appointment.DoctorId == 0) ModelState.AddModelError("DoctorId", "Vui lòng chọn bác sĩ.");

            // Get or Create Patient (Can throw InvalidOperationException)
            int patientId = 0;
            try
            {
                patientId = await GetOrCreatePatientIdForRequestAsync(request);
            }
            catch (InvalidOperationException ex) // *** FIX: Use 'ex' variable ***
            {
                // Log the exception or display a user-friendly message
                System.Diagnostics.Debug.WriteLine($"Error getting/creating patient: {ex.Message}");
                ModelState.AddModelError("", ex.Message); // Add error to ModelState
            }
            if (patientId > 0) appointment.PatientId = patientId;
            else if (patientId == 0 && ModelState.IsValid) // If patient needs creation and other fields are valid
            {
                // If GetOrCreate returned 0, it added a new Patient to the context.
                // We'll save it later.
            }

            // Time Validation and Conversion to UTC
            appointment.CreatedAt = DateTime.UtcNow; // Set creation time
            if (appointment.StartTime.Kind == DateTimeKind.Unspecified)
                appointment.StartTime = DateTime.SpecifyKind(appointment.StartTime, DateTimeKind.Local); // Assume local if unspecified
            // Convert StartTime to UTC if it's Local
            if (appointment.StartTime.Kind == DateTimeKind.Local)
                appointment.StartTime = appointment.StartTime.ToUniversalTime();

            // Calculate EndTime in UTC based on duration
            appointment.EndTime = appointment.StartTime.AddMinutes(duration);

            // Check if EndTime is valid
            if (appointment.EndTime <= appointment.StartTime)
                ModelState.AddModelError("EndTime", "Thời gian kết thúc không hợp lệ.");

            // --- End Validation ---

            // Final check and Save
            if (ModelState.IsValid)
            {
                var svc = new AppointmentService(_db);
                if (!svc.IsSlotAvailable(appointment.DoctorId, appointment.StartTime, appointment.EndTime))
                {
                    ModelState.AddModelError("", "Khung giờ đã có người đặt hoặc không hợp lệ.");
                    await reloadViewBags();
                    return View("~/Areas/Reception/Views/Reception/CreateAppointmentForRequest.cshtml", appointment);
                }

                _db.Appointments.Add(appointment); // Add appointment to context

                // Mark request as handled BEFORE saving changes
                request.IsHandled = true;
                _db.Entry(request).State = EntityState.Modified;

                // Save Changes (handles both new Patient and new Appointment)
                await _db.SaveChangesAsync();

                // If a new patient was created, get their ID AFTER saving
                if (patientId == 0)
                {
                    // The appointment.PatientId should have been set by EF relationship fixup,
                    // or retrieve it if needed:
                    // var newPatient = _db.Patients.Local.FirstOrDefault(p => p.Email == request.Email || p.PhoneNumber == request.Phone);
                    // if (newPatient != null) appointment.PatientId = newPatient.Id;
                    // _db.Entry(appointment).State = EntityState.Modified; // Mark appointment as modified if PatientId was updated
                    // await _db.SaveChangesAsync(); // Save again if needed
                }


                TempData["ok"] = $"Đã đặt lịch thành công cho khách {request.Name}.";
                return RedirectToAction("AppointmentsList", "Reception", new { area = "Reception" });
            }

            // If validation failed, reload and return view
            await reloadViewBags();
            return View("~/Areas/Reception/Views/Reception/CreateAppointmentForRequest.cshtml", appointment);
        }

        // GET: /Reception/Reception/GetServicePrice (Keep as is - maybe add IsVisible check)
        [HttpGet]
        public async Task<JsonResult> GetServicePrice(string specialty, int examType) // Added specialty parameter
        {
            try
            {
                var examTypeValue = (ExamType)examType;
                // Find a visible service matching the exam type. Specialty is not directly linked to service.
                var service = await _db.Services.FirstOrDefaultAsync(s => s.ExamType == examTypeValue && s.IsVisible);

                if (service != null)
                {
                    return Json(new { success = true, serviceName = service.Name, price = service.Fee }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = $"Không tìm thấy dịch vụ phù hợp cho '{GetEnumDisplayName(examTypeValue)}'." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex) // *** FIX: Use 'ex' variable ***
            {
                System.Diagnostics.Debug.WriteLine($"Error getting service price: {ex.Message}");
                return Json(new { success = false, message = "Lỗi hệ thống khi lấy giá dịch vụ." }, JsonRequestBehavior.AllowGet);
            }
        }


        // --- Helper: GetOrCreatePatientIdForRequestAsync (Keep as is) ---
        private async Task<int> GetOrCreatePatientIdForRequestAsync(AppointmentRequest request)
        {
            Patient patient = null;
            // Try finding by Email first (more unique)
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                patient = await _db.Patients.FirstOrDefaultAsync(p => p.Email == request.Email);
            }
            // If not found by email, try finding by Phone
            if (patient == null && !string.IsNullOrWhiteSpace(request.Phone))
            {
                patient = await _db.Patients.FirstOrDefaultAsync(p => p.PhoneNumber == request.Phone);
            }

            // If found, return existing ID
            if (patient != null)
            {
                return patient.Id;
            }

            // If not found, create a new Patient
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                // Cannot create without a name
                throw new InvalidOperationException("Không thể tạo bệnh nhân mới mà không có tên.");
            }

            patient = new Patient
            {
                FullName = request.Name,
                Email = request.Email, // Can be null
                PhoneNumber = request.Phone, // Can be null, but usually provided
                CreatedAt = DateTime.UtcNow
                // Other fields like DOB, Address etc. are initially null for walk-ins
            };
            _db.Patients.Add(patient); // Add to context, SaveChanges will happen in the POST action

            return 0; // Return 0 to indicate a new patient needs saving
        }


        // Dispose DbContext (Keep as is)
        protected override void Dispose(bool disposing) { if (disposing) { _db.Dispose(); } base.Dispose(disposing); }
    }
}
