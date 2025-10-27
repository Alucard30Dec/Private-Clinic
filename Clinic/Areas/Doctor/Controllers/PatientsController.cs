using Clinic.Models;
using Microsoft.AspNet.Identity;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Clinic.Areas.Doctor.Data;

namespace Clinic.Areas.Doctor.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class PatientsController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        private async Task<int?> CurrentDoctorIdAsync()
        {
            var uid = User.Identity.GetUserId(); // map sang Doctor.UserId
            var dto = await _db.Doctors
                               .Where(d => d.UserId == uid)
                               .Select(d => new { d.Id })
                               .FirstOrDefaultAsync();
            return dto?.Id;
        }

        // GET: /Doctor/Patients
        public async Task<ActionResult> Index(string q = null)
        {
            ViewBag.Title = "Bệnh nhân của tôi";
            ViewBag.Nav = "mypatients";

            var did = await CurrentDoctorIdAsync();
            if (did == null) return HttpNotFound("Không tìm thấy hồ sơ bác sĩ.");

            var query = _db.Appointments
                           .Where(a => a.DoctorId == did.Value)
                           .GroupBy(a => new
                           {
                               a.PatientId,
                               a.Patient.FullName,
                               a.Patient.PhoneNumber,
                               a.Patient.Email,
                               a.Patient.DateOfBirth
                           })
                           .Select(g => new MyPatientRowVM
                           {
                               PatientId = g.Key.PatientId,
                               FullName = g.Key.FullName,
                               PhoneNumber = g.Key.PhoneNumber,
                               Email = g.Key.Email,
                               DOB = g.Key.DateOfBirth,
                               TotalVisits = g.Count(),
                               LastVisit = g.Max(x => (System.DateTime?)x.StartTime)
                           });

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(x =>
                    x.FullName.Contains(q) ||
                    x.PhoneNumber.Contains(q) ||
                    x.Email.Contains(q));
            }

            var list = await query.OrderBy(x => x.FullName).ToListAsync();
            ViewBag.q = q;
            return View(list);
        }

        // GET: /Doctor/Patients/Details/5
        public async Task<ActionResult> Details(int id)
        {
            ViewBag.Title = "Hồ sơ bệnh nhân";
            ViewBag.Nav = "mypatients";

            var did = await CurrentDoctorIdAsync();
            if (did == null) return HttpNotFound();

            var patient = await _db.Patients
                                   .Where(p => p.Id == id)
                                   .Select(p => new
                                   {
                                       p.Id,
                                       p.FullName,
                                       p.PhoneNumber,
                                       p.Email,
                                       p.DateOfBirth
                                   })
                                   .FirstOrDefaultAsync();

            if (patient == null) return HttpNotFound();

            // Chỉ lấy các lần khám với chính bác sĩ này
            var visits = await _db.Appointments
                .Where(a => a.PatientId == id && a.DoctorId == did.Value)
                .OrderByDescending(a => a.StartTime)
                .Select(a => new PatientVisitRowVM
                {
                    AppointmentId = a.Id,
                    ServiceName = a.Service.Name,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Status = (int)a.Status,
                    Notes = a.Notes
                })
                .ToListAsync();

            var vm = new PatientDetailVM
            {
                PatientId = patient.Id,
                FullName = patient.FullName,
                PhoneNumber = patient.PhoneNumber,
                Email = patient.Email,
                DOB = patient.DateOfBirth,
                Visits = visits
            };

            return View(vm);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
