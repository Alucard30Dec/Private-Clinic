using Clinic.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Clinic.Areas.Doctor.Data; // <<< *** ENSURE THIS USING DIRECTIVE IS PRESENT AND CORRECT ***

namespace Clinic.Areas.Doctor.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class RecordsController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        private async Task<int?> CurrentDoctorIdAsync()
        {
            var uid = User.Identity.GetUserId();
            var dto = await _db.Doctors.Where(d => d.UserId == uid)
                                       .Select(d => new { d.Id })
                                       .FirstOrDefaultAsync();
            return dto?.Id;
        }

        // GET: /Doctor/Records
        public async Task<ActionResult> Index(string q = null, DateTime? from = null, DateTime? to = null, int? status = null)
        {
            ViewBag.Title = "Hồ sơ khám";
            ViewBag.Nav = "records";

            var did = await CurrentDoctorIdAsync();
            // CS0472 Warning (similar to PatientsController, comparing nullable int? to null is valid)
            if (did == null) return HttpNotFound();

            var query = _db.Appointments
                .Where(a => a.DoctorId == did.Value)
                .Select(a => new RecordRowVM // Map to correct ViewModel (Now found)
                {
                    AppointmentId = a.Id,
                    PatientName = a.Patient.FullName, // Assuming Patient navigation property
                    ServiceName = a.Service.Name,   // Assuming Service navigation property
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Status = (int)a.Status, // Cast enum to int
                    Notes = a.Notes
                });

            // Filtering logic
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(x => (x.PatientName != null && x.PatientName.Contains(q)) || (x.ServiceName != null && x.ServiceName.Contains(q)));
            }
            if (from.HasValue)
            {
                DateTime fromUtc = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Local).ToUniversalTime();
                query = query.Where(x => x.StartTime >= fromUtc);
            }
            if (to.HasValue)
            {
                DateTime toUtc = DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime();
                query = query.Where(x => x.StartTime < toUtc);
            }
            if (status.HasValue && Enum.IsDefined(typeof(AppointmentStatus), status.Value)) // Check if status is valid enum value
            {
                var statusEnum = (AppointmentStatus)status.Value;
                // Compare int Status property with the integer value of the enum
                query = query.Where(x => x.Status == (int)statusEnum);
            }


            var list = await query.OrderByDescending(x => x.StartTime).ToListAsync();

            ViewBag.q = q;
            ViewBag.from = from?.ToString("yyyy-MM-dd");
            ViewBag.to = to?.ToString("yyyy-MM-dd");
            ViewBag.status = status;

            return View(list);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
