using Clinic.Models;
using System;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Linq.Expressions; // <<< ADDED using

// Alias kept
using DoctorModel = Clinic.Models.Doctor;

// *** CORRECTED NAMESPACE ***
namespace Clinic.Areas.Admin.Controllers // Changed from Reception to Admin
{
    // ViewModels kept
    public class ReceptionWorkShiftViewModel // Name might be confusing, consider renaming to AdminWorkShiftViewModel later
    {
        public List<DoctorModel> AllDoctors { get; set; }
        public IEnumerable<WorkingHour> FilteredShifts { get; set; }
        public int? SelectedDoctorId { get; set; }
        // *** REMOVED unused variable 'SelectedDoctorName' ***
    }

    public class DoctorShiftDetailViewModel // Name might be confusing, consider renaming later
    {
        public DoctorModel Doctor { get; set; }
        public List<WorkingHour> Shifts { get; set; }
    }


    [Authorize(Roles = "Receptionist,Admin")] // Keep roles for now, refine later if needed
    public class WorkShiftsController : Controller // Class name is correct
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: Admin/WorkShifts (Implicitly handled by routing)
        // Renamed from Index to avoid conflict IF merged, but separate files okay.
        // Let's assume routing handles /Admin/WorkShifts correctly and keep name Index
        public async Task<ActionResult> Index(int? doctorIdFilter = null)
        {
            // *** Note: ViewBag.Nav probably should be different for Admin vs Reception ***
            ViewBag.Nav = "workshifts"; // Changed from "reception_workshifts"
            ViewBag.Title = "Quản lý Ca làm việc Bác sĩ"; // Admin title

            // Get all visible doctors
            var allDoctors = await _db.Doctors
                .Where(d => d.IsVisible) // <<< Filter visible doctors
                .OrderBy(d => d.Name)
                .ToListAsync();

            // *** NEW: Add SelectList for dropdown in Admin View ***
            ViewBag.DoctorList = new SelectList(allDoctors, "Id", "Name", doctorIdFilter);
            ViewBag.SelectedDoctorId = doctorIdFilter; // Pass selected ID to view
            // *** END NEW ***


            // Get shifts based on filter
            var query = _db.WorkingHours.Include(wh => wh.Doctor).AsQueryable(); // Include Doctor for display if needed
            if (doctorIdFilter.HasValue)
            {
                query = query.Where(wh => wh.DoctorId == doctorIdFilter.Value);
                var selectedDoctor = allDoctors.FirstOrDefault(d => d.Id == doctorIdFilter.Value);
                if (selectedDoctor != null)
                {
                    ViewBag.Title = $"Ca làm việc của BS: {selectedDoctor.Name}";
                }
            }

            var shifts = await query
               .OrderBy(wh => wh.Doctor.Name) // Order by Doctor first if showing all
               .ThenBy(wh => wh.DayOfWeek)
               .ThenBy(wh => wh.Start)
               .ToListAsync();

            // *** RENDER ADMIN VIEW ***
            // Pass the list of shifts directly to the Admin Index view
            // The Admin Index view needs the dropdown logic added.
            return View(shifts);
            // This now returns Views/Admin/WorkShifts/Index.cshtml expecting IEnumerable<WorkingHour>
        }


        // GET: Admin/WorkShifts/Create
        public async Task<ActionResult> Create(int? doctorId) // Receive optional pre-selected doctor
        {
            ViewBag.Nav = "workshifts";
            ViewBag.Title = "Thêm Ca làm việc";
            await LoadDoctorAndDayListsAsync(doctorId); // Helper to load dropdowns
            var model = new WorkingHour
            {
                DoctorId = doctorId ?? 0 // Pre-select doctor if provided
            };
            return View(model); // Returns Views/Admin/WorkShifts/Create.cshtml
        }

        // POST: Admin/WorkShifts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "DoctorId,DayOfWeek,Start,End")] WorkingHour workingHour)
        {
            ViewBag.Nav = "workshifts";
            ViewBag.Title = "Thêm Ca làm việc";

            // Basic validation
            if (workingHour.Start >= workingHour.End)
            {
                ModelState.AddModelError("End", "Giờ kết thúc phải sau giờ bắt đầu.");
            }
            // Add more specific time validation if needed (e.g., within clinic hours)

            // Overlap validation
            bool isOverlapping = false;
            if (ModelState.IsValid)
            {
                isOverlapping = await _db.WorkingHours
                   .AnyAsync(wh => wh.DoctorId == workingHour.DoctorId
                                  && wh.DayOfWeek == workingHour.DayOfWeek
                                  && wh.Start < workingHour.End
                                  && workingHour.Start < wh.End);
                if (isOverlapping)
                {
                    ModelState.AddModelError("", "Ca làm đăng ký bị trùng với ca đã có.");
                }
            }


            if (ModelState.IsValid)
            {
                _db.WorkingHours.Add(workingHour);
                await _db.SaveChangesAsync();
                TempData["ok"] = "Đã thêm ca làm việc mới.";
                // Redirect back to the index, possibly filtering for the added doctor
                return RedirectToAction("Index", new { doctorIdFilter = workingHour.DoctorId });
            }

            // If error, reload dropdowns and return view
            await LoadDoctorAndDayListsAsync(workingHour.DoctorId, (int)workingHour.DayOfWeek);
            return View(workingHour); // Return Create view with errors
        }

        // GET: Admin/WorkShifts/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            ViewBag.Nav = "workshifts";
            ViewBag.Title = "Sửa Ca làm việc";
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var workingHour = await _db.WorkingHours.FindAsync(id);
            if (workingHour == null) return HttpNotFound();

            await LoadDoctorAndDayListsAsync(workingHour.DoctorId, (int)workingHour.DayOfWeek);
            return View(workingHour); // Returns Views/Admin/WorkShifts/Edit.cshtml
        }

        // POST: Admin/WorkShifts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,DoctorId,DayOfWeek,Start,End")] WorkingHour workingHour)
        {
            ViewBag.Nav = "workshifts";
            ViewBag.Title = "Sửa Ca làm việc";

            // Basic validation
            if (workingHour.Start >= workingHour.End)
            {
                ModelState.AddModelError("End", "Giờ kết thúc phải sau giờ bắt đầu.");
            }
            // Add more specific time validation if needed

            // Overlap validation (exclude self)
            bool isOverlapping = false;
            if (ModelState.IsValid)
            {
                isOverlapping = await _db.WorkingHours
                   .AnyAsync(wh => wh.Id != workingHour.Id // Exclude the current shift
                                  && wh.DoctorId == workingHour.DoctorId
                                  && wh.DayOfWeek == workingHour.DayOfWeek
                                  && wh.Start < workingHour.End
                                  && workingHour.Start < wh.End);
                if (isOverlapping)
                {
                    ModelState.AddModelError("", "Ca làm đăng ký bị trùng với ca đã có.");
                }
            }

            if (ModelState.IsValid)
            {
                _db.Entry(workingHour).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                TempData["ok"] = "Đã cập nhật ca làm việc.";
                return RedirectToAction("Index", new { doctorIdFilter = workingHour.DoctorId });
            }

            // If error, reload dropdowns and return view
            await LoadDoctorAndDayListsAsync(workingHour.DoctorId, (int)workingHour.DayOfWeek);
            return View(workingHour); // Return Edit view with errors
        }

        // GET: Admin/WorkShifts/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            ViewBag.Nav = "workshifts";
            ViewBag.Title = "Xóa Ca làm việc";
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var workingHour = await _db.WorkingHours.Include(wh => wh.Doctor).FirstOrDefaultAsync(wh => wh.Id == id);
            if (workingHour == null) return HttpNotFound();

            return View(workingHour); // Returns Views/Admin/WorkShifts/Delete.cshtml
        }

        // POST: Admin/WorkShifts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var workingHour = await _db.WorkingHours.FindAsync(id);
            if (workingHour == null) return HttpNotFound();

            int doctorId = workingHour.DoctorId; // Store before deleting
            _db.WorkingHours.Remove(workingHour);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Đã xóa ca làm việc.";
            return RedirectToAction("Index", new { doctorIdFilter = doctorId }); // Redirect back, possibly filtering
        }


        // --- Helper Methods ---
        private async Task LoadDoctorAndDayListsAsync(int? selectedDoctorId = null, int? selectedDay = null)
        {
            var doctors = await _db.Doctors
                                  .Where(d => d.IsVisible)
                                  .OrderBy(d => d.Name)
                                  .ToListAsync();
            ViewBag.DoctorList = new SelectList(doctors, "Id", "Name", selectedDoctorId);

            ViewBag.DayOfWeekList = new SelectList(
                Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>()
                    .Select(d => new SelectListItem
                    {
                        Value = ((int)d).ToString(),
                        Text = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(d)
                    }),
                "Value", "Text", selectedDay
            );
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
