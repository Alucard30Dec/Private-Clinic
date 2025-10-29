using System;
using System.ComponentModel.DataAnnotations; // Keep this
using System.ComponentModel.DataAnnotations.Schema; // Keep this
using System.Linq;
// using System.Reflection; // No longer needed here
using System.Web.Mvc; // Add this using for Controller base class
using Clinic.Models; // Add this using for ClinicDbContext, etc.
using System.Threading.Tasks; // Add this for async methods
using System.Collections.Generic; // Add this for List
using System.Data.Entity; // Add this for Include/FirstOrDefaultAsync

// *** FIX: Namespace should be Clinic.Controllers ***
namespace Clinic.Controllers
{
    // Definitions for ExamType, AppointmentStatus, Appointment, and EnumExtensions
    // ONLY exist in Clinic.Models namespace (e.g., in Appointment.cs)

    public class AppointmentController : Controller // Inherit from Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: Appointment (or Appointment/Index)
        public async Task<ActionResult> Index()
        {
            ViewBag.Title = "Make an appointment";
            var specialties = await _db.Specialties // *** Query Specialties table ***
                                     .Where(s => s.IsVisible)
                                     .OrderBy(s => s.Name)
                                     .Select(s => new { s.Name })
                                     .ToListAsync();

            ViewBag.Specialties = new SelectList(specialties, "Name", "Name");

            return View(new AppointmentRequest());
        }

        // POST: Appointment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AppointmentRequest model)
        {
            // Reload specialties if validation fails
            Func<Task> reloadSpecialties = async () => {
                var specialties = await _db.Specialties // *** Query Specialties table ***
                                         .Where(s => s.IsVisible)
                                         .OrderBy(s => s.Name)
                                         .Select(s => new { s.Name })
                                         .ToListAsync();
                ViewBag.Specialties = new SelectList(specialties, "Name", "Name", model.Specialty);
            };

            if (!ModelState.IsValid)
            {
                await reloadSpecialties();
                return View("Index", model);
            }

            // Timezone Handling (Keep as is)
            if (model.RequestedSlot.Kind == DateTimeKind.Unspecified)
            {
                model.RequestedSlot = DateTime.SpecifyKind(model.RequestedSlot, DateTimeKind.Local);
            }
            else if (model.RequestedSlot.Kind == DateTimeKind.Utc)
            {
                model.RequestedSlot = model.RequestedSlot.ToLocalTime();
            }
            DateTime requestedSlotUtc = model.RequestedSlot.ToUniversalTime();

            // Validation (Keep as is)
            if (requestedSlotUtc <= DateTime.UtcNow)
            {
                ModelState.AddModelError("RequestedSlot", "Không thể đặt lịch hẹn trong quá khứ.");
                await reloadSpecialties();
                return View("Index", model);
            }
            if (model.RequestedSlot.DayOfWeek == DayOfWeek.Sunday)
            {
                ModelState.AddModelError("RequestedSlot", "Phòng khám nghỉ Chủ nhật, vui lòng chọn ngày khác.");
                await reloadSpecialties();
                return View("Index", model);
            }

            // Save Request (Keep as is)
            var requestToSave = new AppointmentRequest
            {
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Specialty = model.Specialty,
                RequestedSlot = requestedSlotUtc,
                Message = model.Message,
                CreatedAt = DateTime.UtcNow,
                IsHandled = false
            };
            _db.AppointmentRequests.Add(requestToSave);
            await _db.SaveChangesAsync();

            ViewBag.Success = "Yêu cầu đặt lịch của bạn đã được gửi thành công! Lễ tân sẽ liên hệ xác nhận sớm nhất.";
            await reloadSpecialties();
            return View("Index", new AppointmentRequest());
        }


        // GET: Appointment/GetAvailableSlots
        [HttpGet]
        public async Task<JsonResult> GetAvailableSlots(string date, string specialty)
        {
            if (string.IsNullOrEmpty(date) || string.IsNullOrEmpty(specialty))
            {
                return Json(new { slots = new List<string>(), message = "Vui lòng chọn chuyên khoa và ngày." }, JsonRequestBehavior.AllowGet);
            }
            if (!DateTime.TryParse(date, out DateTime selectedLocalDate))
            {
                return Json(new { slots = new List<string>(), message = "Ngày không hợp lệ." }, JsonRequestBehavior.AllowGet);
            }

            // Timezone Handling & Past/Sunday checks (Keep as is)
            DateTime startOfDayLocal = selectedLocalDate.Date;
            DateTime endOfDayLocal = startOfDayLocal.AddDays(1);
            DateTime startOfDayUtc = DateTime.SpecifyKind(startOfDayLocal, DateTimeKind.Local).ToUniversalTime();
            DateTime endOfDayUtc = DateTime.SpecifyKind(endOfDayLocal, DateTimeKind.Local).ToUniversalTime();
            DateTime nowUtc = DateTime.UtcNow;
            if (startOfDayUtc < nowUtc && endOfDayUtc > nowUtc) startOfDayUtc = nowUtc;
            else if (endOfDayUtc <= nowUtc) return Json(new { slots = new List<string>(), message = "Ngày đã chọn nằm trong quá khứ." }, JsonRequestBehavior.AllowGet);
            if (selectedLocalDate.DayOfWeek == DayOfWeek.Sunday) return Json(new { slots = new List<string>(), message = "Phòng khám nghỉ Chủ nhật." }, JsonRequestBehavior.AllowGet);

            try
            {
                var availableSlotsLocal = new List<string>();
                int slotDuration = 30; // minutes
                DayOfWeek selectedDayOfWeekLocal = selectedLocalDate.DayOfWeek;

                // *** FIX: Query doctors and their WORKING HOURS correctly ***
                // 1. Find doctors with the specialty
                var doctorIdsWithSpecialty = await _db.Doctors
                    .Where(d => d.IsVisible && d.Specialty.Name == specialty) // Compare Specialty.Name
                    .Select(d => d.Id)
                    .ToListAsync();

                if (!doctorIdsWithSpecialty.Any())
                {
                    return Json(new { slots = new List<string>(), message = "Không có bác sĩ nào thuộc chuyên khoa này." }, JsonRequestBehavior.AllowGet);
                }

                // 2. Get working hours for those doctors on the selected day
                var workingHoursForDay = await _db.WorkingHours
                    .Where(wh => doctorIdsWithSpecialty.Contains(wh.DoctorId) && wh.DayOfWeek == selectedDayOfWeekLocal)
                    .Select(wh => new { wh.DoctorId, wh.Start, wh.End })
                    .ToListAsync();

                if (!workingHoursForDay.Any())
                {
                    return Json(new { slots = new List<string>(), message = "Không có bác sĩ nào làm việc vào ngày đã chọn." }, JsonRequestBehavior.AllowGet);
                }

                // Generate potential slots (Keep as is)
                var potentialSlotsLocal = new HashSet<DateTime>();
                foreach (var shift in workingHoursForDay) // Loop through the fetched working hours
                {
                    DateTime shiftStartLocal = startOfDayLocal.Add(shift.Start);
                    DateTime shiftEndLocal = startOfDayLocal.Add(shift.End);
                    for (DateTime slotStart = shiftStartLocal; slotStart.AddMinutes(slotDuration) <= shiftEndLocal; slotStart = slotStart.AddMinutes(slotDuration))
                    {
                        if (slotStart >= DateTime.SpecifyKind(startOfDayUtc.ToLocalTime(), DateTimeKind.Local))
                        {
                            potentialSlotsLocal.Add(slotStart);
                        }
                    }
                }

                // *** FIX: Get booked slots using Doctor.Specialty.Name ***
                // Include Doctor and Specialty to filter by Specialty.Name
                var bookedSlotsUtc = await _db.Appointments
                   .Include(a => a.Doctor.Specialty) // Include Doctor and Specialty
                   .Where(a => a.Doctor.Specialty.Name == specialty // Filter by Specialty Name
                              && a.StartTime >= startOfDayUtc && a.StartTime < endOfDayUtc
                              && a.Status != AppointmentStatus.Canceled)
                   .Select(a => a.StartTime)
                   .ToListAsync();

                // Filter potential slots (Keep as is)
                var bookedSlotsLocalSet = new HashSet<DateTime>(bookedSlotsUtc.Select(bst => DateTime.SpecifyKind(bst.ToLocalTime(), DateTimeKind.Local)));
                foreach (DateTime potentialSlot in potentialSlotsLocal.OrderBy(s => s))
                {
                    if (!bookedSlotsLocalSet.Contains(potentialSlot))
                    {
                        availableSlotsLocal.Add(potentialSlot.ToString("HH:mm"));
                    }
                }

                // Return result (Keep as is)
                if (!availableSlotsLocal.Any()) return Json(new { slots = new List<string>(), message = "Đã hết lịch trống cho ngày này. Vui lòng chọn ngày khác." }, JsonRequestBehavior.AllowGet);
                return Json(new { slots = availableSlotsLocal, message = "Các khung giờ còn trống:" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting slots: {ex.Message} \n {ex.StackTrace}"); // Log stack trace too
                return Json(new { slots = new List<string>(), message = "Lỗi khi tải lịch hẹn. Vui lòng thử lại." }, JsonRequestBehavior.AllowGet);
            }
        }

        // Dispose DbContext (Keep as is)
        protected override void Dispose(bool disposing) { if (disposing) _db.Dispose(); base.Dispose(disposing); }
    }
}