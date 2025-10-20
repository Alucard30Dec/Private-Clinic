using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Clinic.Models;

namespace Clinic.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AppointmentsController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // Lấy giá trị enum đầu tiên làm mặc định (tránh phụ thuộc tên "New")
        private static AppointmentStatus DefaultStatus()
        {
            return Enum.GetValues(typeof(AppointmentStatus))
                       .Cast<AppointmentStatus>()
                       .First();
        }

        // Nạp SelectList cho form Create/Edit
        private async Task FillSelects(Appointment a = null)
        {
            ViewBag.PatientId = new SelectList(
                await _db.Patients.OrderBy(p => p.FullName).ToListAsync(),
                "Id", "FullName", a?.PatientId);

            ViewBag.DoctorId = new SelectList(
                await _db.Doctors.OrderBy(d => d.Name).ToListAsync(),
                "Id", "Name", a?.DoctorId);

            ViewBag.ServiceId = new SelectList(
                await _db.Services.OrderBy(s => s.Name).ToListAsync(),
                "Id", "Name", a?.ServiceId);

            var statusItems = Enum.GetValues(typeof(AppointmentStatus))
                                  .Cast<AppointmentStatus>()
                                  .Select(s => new { Value = (int)s, Text = s.ToString() })
                                  .ToList();

            var selectedStatus = (int)(a?.Status ?? DefaultStatus());
            ViewBag.StatusList = new SelectList(statusItems, "Value", "Text", selectedStatus);
        }

        // GET: Admin/Appointments
        public async Task<ActionResult> Index(string q = null)
        {
            ViewBag.Nav = "appointments";

            var apps = _db.Appointments
                          .Include(a => a.Patient)
                          .Include(a => a.Doctor)
                          .Include(a => a.Service)
                          .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                apps = apps.Where(a =>
                    (a.Patient != null && a.Patient.FullName.Contains(q)) ||
                    (a.Doctor != null && a.Doctor.Name.Contains(q)) ||
                    (a.Service != null && a.Service.Name.Contains(q)) ||
                    (a.Notes != null && a.Notes.Contains(q)));
            }

            var list = await apps.OrderByDescending(a => a.StartTime).ToListAsync();
            return View(list);
        }

        // GET: Admin/Appointments/Create
        public async Task<ActionResult> Create()
        {
            ViewBag.Nav = "appointments";

            var now = DateTime.Now;
            var a = new Appointment
            {
                StartTime = now.AddMinutes(15),
                EndTime = now.AddMinutes(45),
                Status = DefaultStatus()
            };

            await FillSelects(a);
            return View(a);
        }

        // POST: Admin/Appointments/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include =
            "PatientId,DoctorId,ServiceId,StartTime,EndTime,Status,Notes")] Appointment a)
        {
            ViewBag.Nav = "appointments";

            // Validate thời gian
            if (a.StartTime >= a.EndTime)
                ModelState.AddModelError("EndTime", "Thời gian kết thúc phải sau thời gian bắt đầu.");

            // Validate FK (tránh nhập rác)
            if (!_db.Patients.Any(p => p.Id == a.PatientId))
                ModelState.AddModelError("PatientId", "Bệnh nhân không hợp lệ.");
            if (!_db.Doctors.Any(d => d.Id == a.DoctorId))
                ModelState.AddModelError("DoctorId", "Bác sĩ không hợp lệ.");
            if (!_db.Services.Any(s => s.Id == a.ServiceId))
                ModelState.AddModelError("ServiceId", "Dịch vụ không hợp lệ.");

            if (!ModelState.IsValid)
            {
                await FillSelects(a);
                return View(a);
            }

            _db.Appointments.Add(a);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Đã tạo lịch hẹn.";
            return RedirectToAction("Index");
        }

        // GET: Admin/Appointments/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            ViewBag.Nav = "appointments";
            if (id == null) return HttpNotFound();

            var a = await _db.Appointments.FindAsync(id);
            if (a == null) return HttpNotFound();

            await FillSelects(a);
            return View(a);
        }

        // POST: Admin/Appointments/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include =
            "Id,PatientId,DoctorId,ServiceId,StartTime,EndTime,Status,Notes")] Appointment input)
        {
            ViewBag.Nav = "appointments";

            if (input.StartTime >= input.EndTime)
                ModelState.AddModelError("EndTime", "Thời gian kết thúc phải sau thời gian bắt đầu.");

            if (!ModelState.IsValid)
            {
                await FillSelects(input);
                return View(input);
            }

            var a = await _db.Appointments.FindAsync(input.Id);
            if (a == null) return HttpNotFound();

            a.PatientId = input.PatientId;
            a.DoctorId = input.DoctorId;
            a.ServiceId = input.ServiceId;
            a.StartTime = input.StartTime;
            a.EndTime = input.EndTime;
            a.Status = input.Status;
            a.Notes = input.Notes;

            await _db.SaveChangesAsync();
            TempData["ok"] = "Đã cập nhật lịch hẹn.";
            return RedirectToAction("Index");
        }

        // GET: Admin/Appointments/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            ViewBag.Nav = "appointments";
            if (id == null) return HttpNotFound();

            var a = await _db.Appointments
                             .Include(x => x.Patient)
                             .Include(x => x.Doctor)
                             .Include(x => x.Service)
                             .FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return HttpNotFound();

            return View(a);
        }

        // POST: Admin/Appointments/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var a = await _db.Appointments.FindAsync(id);
            if (a == null) return HttpNotFound();

            _db.Appointments.Remove(a);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Đã xóa lịch hẹn.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
