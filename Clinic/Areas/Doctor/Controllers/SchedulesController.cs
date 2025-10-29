using Clinic.Models;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Clinic.Areas.Doctor.Data; // <<< *** ENSURE THIS USING DIRECTIVE IS PRESENT AND CORRECT ***
using System; // For DateTime

namespace Clinic.Areas.Doctor.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class SchedulesController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        private async Task<int?> GetCurrentDoctorIdAsync()
        {
            var uid = User.Identity.GetUserId();
            var dto = await _db.Doctors
                               .Where(d => d.UserId == uid)
                               .Select(d => new { d.Id })
                               .FirstOrDefaultAsync();
            return dto?.Id;
        }

        // GET: /Doctor/Schedules
        public async Task<ActionResult> Index()
        {
            ViewBag.Title = "Lịch khám của tôi";
            ViewBag.Nav = "schedules";

            var doctorId = await GetCurrentDoctorIdAsync();
            // CS0472 Warning (similar to PatientsController, comparing nullable int? to null is valid)
            if (doctorId == null) return HttpNotFound("Không tìm thấy hồ sơ bác sĩ");

            var rows = await _db.Appointments
                .Where(a => a.DoctorId == doctorId.Value)
                .OrderByDescending(a => a.StartTime)
                .Select(a => new AppointmentRowVM // Map to correct ViewModel (Now found)
                {
                    Id = a.Id,
                    PatientFullName = a.Patient.FullName, // Assuming Patient navigation property
                    ServiceFullName = a.Service.Name,   // Assuming Service navigation property
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Status = (int)a.Status, // Cast enum to int
                    Notes = a.Notes
                })
                .ToListAsync();

            return View(rows);
        }

        // GET: /Doctor/Schedules/Details/5
        public async Task<ActionResult> Details(int id) // Parameter 'id' is non-nullable, no need for `id == null` check.
        {
            ViewBag.Title = "Chi tiết lịch khám";
            ViewBag.Nav = "schedules";

            var doctorId = await GetCurrentDoctorIdAsync();
            // CS0472 Warning (similar to PatientsController, comparing nullable int? to null is valid)
            if (doctorId == null) return HttpNotFound();

            var a = await _db.Appointments
                .Include(x => x.Patient) // Keep Includes for navigation properties used below
                .Include(x => x.Service)
                .Where(x => x.Id == id && x.DoctorId == doctorId.Value)
                .Select(x => new AppointmentDetailVM // Map to correct ViewModel (Now found)
                {
                    Id = x.Id,
                    PatientFullName = x.Patient.FullName,
                    PatientPhone = x.Patient.PhoneNumber,
                    PatientEmail = x.Patient.Email,
                    ServiceFullName = x.Service.Name,
                    // ServiceDesc = x.Service.Description, // Uncomment and map if Service has a Description field
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    Status = (int)x.Status, // Cast enum to int
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
