using Clinic.Models;
using System;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Collections.Generic;

// === KHAI BÁO ALIAS ĐỂ TRÁNH XUNG ĐỘT VỚI NAMESPACE Clinic.Areas.Doctor ===
using DoctorModel = Clinic.Models.Doctor;

namespace Clinic.Areas.Reception.Controllers
{
    // ViewModel to hold data for the view
    public class ReceptionWorkShiftViewModel
    {
        // SỬ DỤNG ALIAS
        public List<DoctorModel> AllDoctors { get; set; }
        public IEnumerable<WorkingHour> FilteredShifts { get; set; }
        public int? SelectedDoctorId { get; set; }
        public string SelectedDoctorName { get; set; } // To display which doctor's shifts are shown
    }

    // *** THÊM VIEWMODEL CHO TRANG CHI TIẾT ***
    public class DoctorShiftDetailViewModel
    {
        public DoctorModel Doctor { get; set; }
        public List<WorkingHour> Shifts { get; set; }
    }
    // *** KẾT THÚC THÊM ***

    [Authorize(Roles = "Receptionist,Admin")]
    public class WorkShiftsController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: Reception/WorkShifts
        public async Task<ActionResult> Index(int? doctorIdFilter = null)
        {
            ViewBag.Nav = "reception_workshifts";
            ViewBag.Title = "Xem Ca làm việc Bác sĩ";

            // 1. Get all doctors for the list (Sử dụng DoctorModel)
            var allDoctors = await _db.Doctors
                .OrderBy(d => d.Name)
                .ToListAsync();

            // 2. Get shifts only if a doctor is selected (LOGIC NÀY SẼ ĐƯỢC CHUYỂN SANG ACTION DETAILS)
            // Giữ lại phần này nhưng set rỗng để tránh lỗi khi render Index
            IEnumerable<WorkingHour> filteredShifts = new List<WorkingHour>();
            string selectedDoctorName = null;

            // Nếu có doctorIdFilter, CHUYỂN HƯỚNG SANG TRANG DETAILS NGAY LẬP TỨC
            if (doctorIdFilter.HasValue && allDoctors.Any(d => d.Id == doctorIdFilter.Value))
            {
                // *** CHUYỂN HƯỚNG TỚI ACTION DETAILS MỚI ***
                return RedirectToAction("Details", new { id = doctorIdFilter.Value });
            }

            // 3. Create ViewModel and pass to View (Không có FilteredShifts)
            var viewModel = new ReceptionWorkShiftViewModel
            {
                AllDoctors = allDoctors.Cast<DoctorModel>().ToList(), // Cast to the alias type
                FilteredShifts = filteredShifts, // Luôn rỗng
                SelectedDoctorId = null, // Luôn null
                SelectedDoctorName = null // Luôn null
            };

            return View("~/Areas/Reception/Views/WorkShifts/Index.cshtml", viewModel);
        }

        // *** THÊM ACTION DETAILS MỚI ***
        // GET: Reception/WorkShifts/Details/{id} (id là DoctorId)
        public async Task<ActionResult> Details(int id)
        {
            ViewBag.Nav = "reception_workshifts";

            var doctor = await _db.Doctors.FindAsync(id);
            if (doctor == null)
            {
                TempData["warn"] = "Không tìm thấy hồ sơ bác sĩ.";
                return RedirectToAction("Index");
            }

            ViewBag.Title = $"Ca làm việc của BS: {doctor.Name}";

            var shifts = await _db.WorkingHours
                .Where(wh => wh.DoctorId == id)
                .OrderBy(wh => wh.DayOfWeek)
                .ThenBy(wh => wh.Start)
                .ToListAsync();

            var viewModel = new DoctorShiftDetailViewModel
            {
                Doctor = doctor,
                Shifts = shifts
            };

            return View("~/Areas/Reception/Views/WorkShifts/Details.cshtml", viewModel);
        }
        // *** KẾT THÚC THÊM ACTION DETAILS ***

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
