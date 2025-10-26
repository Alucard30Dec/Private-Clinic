using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Clinic.Models;
using Microsoft.AspNet.Identity;

namespace Clinic.Controllers
{
    [Authorize(Roles = "Patient")]
    public class BookingsController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // Action to display booking history
        public ActionResult Index()
        {
            var patientId = GetOrCreatePatientIdForCurrentUser(out bool _);
            var myBookings = _db.Appointments
                .Include(b => b.Doctor)
                .Where(b => b.PatientId == patientId)
                .OrderByDescending(b => b.StartTime)
                .ToList();
            return View(myBookings);
        }

        // Action to cancel a booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancel(int id)
        {
            var patientId = GetOrCreatePatientIdForCurrentUser(out bool _);
            var appt = _db.Appointments.FirstOrDefault(b => b.Id == id && b.PatientId == patientId);
            if (appt == null) { TempData["err"] = "Không tìm thấy lịch hẹn."; return RedirectToAction("Index"); }
            if (appt.Status == AppointmentStatus.Canceled || appt.Status == AppointmentStatus.Completed) { TempData["err"] = "Lịch hẹn này đã được xử lý."; return RedirectToAction("Index"); }
            if (appt.StartTime <= DateTime.UtcNow) { TempData["err"] = "Không thể hủy lịch hẹn đã hoặc đang diễn ra."; return RedirectToAction("Index"); }
            appt.Status = AppointmentStatus.Canceled;
            _db.SaveChanges();
            TempData["ok"] = "Đã hủy lịch hẹn thành công.";
            return RedirectToAction("Index");
        }

        // GET Action for creating a booking
        public ActionResult Create(int doctorId, int serviceId = 1)
        {
            var doctor = _db.Doctors.Find(doctorId);
            if (doctor == null) return HttpNotFound();
            var svc = new AppointmentService(_db);
            var vm = new BookingVM
            {
                DoctorId = doctorId,
                ServiceId = serviceId,
                DoctorName = doctor.Name,
                // Use DateTime.Now to suggest slots based on current local time
                AvailableSlotsLocal = svc.SuggestSlots(doctorId, DateTime.Now)
            };
            return View(vm);
        }

        // POST Action for creating a booking (with daily limit)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(BookingVM vm) // Use BookingVM as input
        {
            var doctor = _db.Doctors.Find(vm.DoctorId);
            if (doctor == null) return HttpNotFound();

            // Helper to reload doctor info if validation fails
            Action reloadDoctorInfo = () => {
                var s = new AppointmentService(_db);
                vm.DoctorName = doctor.Name;
                // Use DateTime.Now to suggest slots based on current local time
                vm.AvailableSlotsLocal = s.SuggestSlots(vm.DoctorId, DateTime.Now);
            };

            if (!ModelState.IsValid)
            {
                reloadDoctorInfo();
                return View(vm);
            }

            var startLocal = vm.SelectedStartLocal;
            var startUtc = startLocal.ToUniversalTime();
            var endUtc = startUtc.AddMinutes(30); // Assuming 30 min slots
            var pid = GetOrCreatePatientIdForCurrentUser(out bool justCreated);

            // --- Booking Limit Check ---
            var appointmentDate = startLocal.Date; // Date part only (local)
            var existingBookingsCount = _db.Appointments.Count(a =>
                a.PatientId == pid &&
                // Compare date part using DbFunctions
                DbFunctions.TruncateTime(a.StartTime) == appointmentDate &&
                a.Status != AppointmentStatus.Canceled // Don't count canceled appointments
            );

            int maxBookingsPerDay = 1; // Define the limit
            if (existingBookingsCount >= maxBookingsPerDay)
            {
                // *** UPDATED ERROR MESSAGE HERE ***
                ModelState.AddModelError("", $"Bạn đã có lịch hẹn vào ngày {appointmentDate:dd/MM/yyyy}. Vui lòng hủy lịch hẹn cũ trước khi đặt lịch mới trong cùng ngày.");
                // *** END OF UPDATE ***

                reloadDoctorInfo();
                return View(vm); // Return the view to show the error
            }
            // --- End of Booking Limit Check ---


            // Check slot availability (using UTC times)
            var svc = new AppointmentService(_db);
            if (!svc.IsSlotAvailable(vm.DoctorId, startUtc, endUtc))
            {
                // Use TempData for error message when returning the view might be better
                ModelState.AddModelError("", "Khung giờ bị trùng. Vui lòng chọn giờ khác.");
                //TempData["err"] = "Khung giờ bị trùng. Vui lòng chọn giờ khác hoặc bác sĩ khác.";
                reloadDoctorInfo();
                return View(vm); // Return view with error
            }

            // Create appointment
            var appt = svc.Create(vm.DoctorId, vm.ServiceId, pid, startUtc, endUtc, vm.Notes);
            TrySendConfirmation(appt);

            TempData["ok"] = $"Đặt lịch thành công: {appt.StartTime.ToLocalTime():dd/MM/yyyy HH:mm}.";
            TempData["apptId"] = appt.Id; // Pass appointment ID for potential immediate cancellation

            // Check if patient profile needs completion
            var profile = _db.Patients.Find(pid);
            if (justCreated || string.IsNullOrWhiteSpace(profile?.PhoneNumber))
            {
                TempData["profileNotice"] = "Bạn vừa đặt lịch thành công. Vui lòng bổ sung hồ sơ để phòng khám liên hệ thuận tiện.";
                // Pass the correct controller name if you renamed PatientProfileController to PatientController
                TempData["completeProfileUrl"] = Url.Action("Complete", "Patient");
            }

            // Redirect back to the doctor's details page
            return RedirectToAction("Details", "Doctors", new { id = vm.DoctorId });
        }

        // Helper to get or create patient ID for the current user
        private int GetOrCreatePatientIdForCurrentUser(out bool justCreated)
        {
            justCreated = false;
            var uid = User.Identity.GetUserId();
            var profile = _db.Patients.FirstOrDefault(p => p.UserId == uid);
            if (profile != null) return profile.Id;
            using (var idb = new ApplicationDbContext())
            {
                var user = idb.Users.FirstOrDefault(u => u.Id == uid);
                profile = new Patient { UserId = uid, FullName = user?.UserName ?? User.Identity.Name, Email = user?.Email, CreatedAt = DateTime.UtcNow };
            }
            _db.Patients.Add(profile); _db.SaveChanges(); justCreated = true; return profile.Id;
        }

        // Placeholder for sending confirmation
        private void TrySendConfirmation(Appointment appt)
        {
            try { System.Diagnostics.Debug.WriteLine($"[CONFIRM] Appointment #{appt.Id} confirmation simulated."); } catch { }
        }

        // Dispose DbContext
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