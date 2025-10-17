using System;
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
                AvailableSlotsLocal = svc.SuggestSlots(doctorId, DateTime.Now)
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(BookingVM vm)
        {
            var doctor = _db.Doctors.Find(vm.DoctorId);
            if (doctor == null) return HttpNotFound();

            if (!ModelState.IsValid)
            {
                var s = new AppointmentService(_db);
                vm.DoctorName = doctor.Name;
                vm.AvailableSlotsLocal = s.SuggestSlots(vm.DoctorId, DateTime.Now);
                return View(vm);
            }

            // chuẩn bị time (slot 30')
            var startUtc = vm.SelectedStartLocal.ToUniversalTime();
            var endUtc = startUtc.AddMinutes(30);

            // 1) Lấy hoặc tự tạo hồ sơ BN
            var pid = GetOrCreatePatientProfileIdForCurrentUser(out bool justCreated);

            // 2) kiểm tra trùng lịch
            var svc = new AppointmentService(_db);
            if (!svc.IsSlotAvailable(vm.DoctorId, startUtc, endUtc))
            {
                TempData["err"] = "Khung giờ bị trùng. Vui lòng chọn giờ khác hoặc bác sĩ khác.";
                return RedirectToAction("Create", new { doctorId = vm.DoctorId, serviceId = vm.ServiceId });
            }

            // 3) tạo lịch
            var appt = svc.Create(vm.DoctorId, vm.ServiceId, pid, startUtc, endUtc, vm.Notes);
            TrySendConfirmation(appt);

            // 4) thông báo + gợi ý bổ sung hồ sơ
            TempData["ok"] = $"Đặt lịch thành công: {appt.StartTime.ToLocalTime():dd/MM/yyyy HH:mm}.";
            TempData["apptId"] = appt.Id;

            // nếu vừa auto-tạo hoặc thiếu dữ liệu cơ bản -> hiển thị banner kèm link bổ sung
            var profile = _db.PatientProfiles.Find(pid);
            if (justCreated || string.IsNullOrWhiteSpace(profile?.PhoneNumber))
            {
                TempData["profileNotice"] = "Bạn vừa đặt lịch thành công. Vui lòng bổ sung hồ sơ để phòng khám liên hệ thuận tiện.";
                TempData["completeProfileUrl"] = Url.Action("Complete", "PatientProfile");
            }

            // vẫn quay về trang bác sĩ như flow của bạn
            return RedirectToAction("Details", "Doctors", new { id = vm.DoctorId });
        }

        /// <summary>
        /// Lấy PatientProfile.Id của user hiện tại; nếu chưa có thì tự tạo hồ sơ tối thiểu.
        /// </summary>
        private int GetOrCreatePatientProfileIdForCurrentUser(out bool justCreated)
        {
            justCreated = false;
            var uid = User.Identity.GetUserId(); // Id của AspNetUsers

            var profile = _db.PatientProfiles.FirstOrDefault(p => p.UserId == uid);
            if (profile != null) return profile.Id;

            // chưa có -> tạo mới từ thông tin Identity
            using (var idb = new ApplicationDbContext())
            {
                var user = idb.Users.FirstOrDefault(u => u.Id == uid);

                profile = new PatientProfile
                {
                    UserId = uid,
                    FullName = user?.UserName ?? User.Identity.Name,
                    Email = user?.Email,
                    CreatedAt = DateTime.UtcNow
                };
            }
            _db.PatientProfiles.Add(profile);
            _db.SaveChanges();
            justCreated = true;
            return profile.Id;
        }

        private void TrySendConfirmation(Appointment appt)
        {
            try { System.Diagnostics.Debug.WriteLine($"[CONFIRM] #{appt.Id} sent."); } catch { }
        }
    }
}
