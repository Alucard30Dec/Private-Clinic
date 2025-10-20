using Clinic.Models;                      // ClinicDbContext
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Clinic.Areas.Doctors.Data;
using System;

namespace Clinic.Areas.Doctors.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class SchedulesController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // Lấy DoctorId từ user hiện tại
        private async Task<int?> GetCurrentDoctorIdAsync()
        {
            var uid = User.Identity.GetUserId();
            var dto = await _db.Doctors
                               .Where(d => d.UserId == uid)
                               .Select(d => new { d.Id })
                               .FirstOrDefaultAsync();
            return dto?.Id;
        }

        // GET: Doctors/Schedules
        public async Task<ActionResult> Index()
        {
            ViewBag.Title = "Lịch khám của tôi";
            ViewBag.Nav = "schedules";

            var doctorId = await GetCurrentDoctorIdAsync();
            if (doctorId == null) return HttpNotFound("Không tìm thấy hồ sơ bác sĩ");

            var rows = await _db.Appointments
                .Where(a => a.DoctorId == doctorId.Value)
                .OrderByDescending(a => a.StartTime)
                .Select(a => new AppointmentRowVM
                {
                    Id = a.Id,
                    PatientFullName = a.Patient.FullName,  
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Notes = a.Notes
                })
                .ToListAsync();

            return View(rows);
        }

        // GET: Doctors/Schedules/Details/5
        public async Task<ActionResult> Details(int id)
        {
            ViewBag.Title = "Chi tiết lịch khám";
            ViewBag.Nav = "schedules";

            var doctorId = await GetCurrentDoctorIdAsync();
            if (doctorId == null) return HttpNotFound();

            var a = await _db.Appointments
                .Include(x => x.Patient)
                .Include(x => x.Service)
                .Where(x => x.Id == id && x.DoctorId == doctorId.Value)
                .Select(x => new AppointmentDetailVM
                {
                    Id = x.Id,
                    PatientFullName = x.Patient.FullName,   
                    PatientPhone = x.Patient.PhoneNumber,
                    PatientEmail = x.Patient.Email,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    Notes = x.Notes,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (a == null) return HttpNotFound();

            return View(a);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
