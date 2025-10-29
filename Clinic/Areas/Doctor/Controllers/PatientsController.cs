using Clinic.Models;
using Microsoft.AspNet.Identity;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Clinic.Areas.Doctor.Data; // <<< *** ENSURE THIS USING DIRECTIVE IS PRESENT AND CORRECT ***
using System;

namespace Clinic.Areas.Doctor.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class PatientsController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        private async Task<int?> CurrentDoctorIdAsync()
        {
            var uid = User.Identity.GetUserId();
            var dto = await _db.Doctors
                               .Where(d => d.UserId == uid)
                               .Select(d => new { d.Id })
                               .FirstOrDefaultAsync();
            return dto?.Id;
        }

        // Helper tính tuổi
        private int? CalculateAge(DateTime? dob)
        {
            if (!dob.HasValue) return null;
            var today = DateTime.Today;
            var age = today.Year - dob.Value.Year;
            if (dob.Value.Date > today.AddYears(-age)) age--;
            return age;
        }


        // GET: /Doctor/Patients
        public async Task<ActionResult> Index(string q = null)
        {
            ViewBag.Title = "Bệnh nhân của tôi";
            ViewBag.Nav = "mypatients";

            var did = await CurrentDoctorIdAsync();
            // CS0472 Warning: The result of the expression is always 'true' since a value of type 'int' is never equal to 'null' of type 'int?'
            // This warning is informational. Comparing a nullable int? (did) with null is standard practice and the logic is correct here.
            if (did == null) return HttpNotFound("Không tìm thấy hồ sơ bác sĩ.");

            var query = _db.Appointments
                           .Where(a => a.DoctorId == did.Value && a.PatientId != null)
                           .Select(a => a.Patient)
                           .Where(p => p != null)
                           .Distinct()
                           .Select(p => new // Intermediate anonymous type
                           {
                               Patient = p,
                               TotalVisits = _db.Appointments.Count(ap => ap.PatientId == p.Id && ap.DoctorId == did.Value),
                               LastVisit = _db.Appointments
                                            .Where(ap => ap.PatientId == p.Id && ap.DoctorId == did.Value)
                                            .Max(ap => (DateTime?)ap.StartTime)
                           })
                           .Select(g => new MyPatientRowVM // Map to correct ViewModel (Now found)
                           {
                               PatientId = g.Patient.Id,
                               FullName = g.Patient.FullName,
                               PhoneNumber = g.Patient.PhoneNumber,
                               Email = g.Patient.Email,
                               DOB = g.Patient.DateOfBirth,
                               Gender = g.Patient.Gender,
                               TotalVisits = g.TotalVisits,
                               LastVisit = g.LastVisit,
                           });


            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                query = query.Where(x =>
                    (x.FullName != null && x.FullName.ToLower().Contains(q)) ||
                    (x.PhoneNumber != null && x.PhoneNumber.Contains(q)) ||
                    (x.Email != null && x.Email.ToLower().Contains(q)) ||
                    (x.Gender != null && x.Gender.ToLower().Contains(q))
                 );
            }

            var list = await query.OrderBy(x => x.FullName).ToListAsync();
            list.ForEach(p => p.Age = CalculateAge(p.DOB));

            ViewBag.q = q;
            return View(list);
        }


        // GET: /Doctor/Patients/Details/5
        public async Task<ActionResult> Details(int id) // Parameter 'id' is non-nullable int, no explicit `id == null` check needed.
        {
            ViewBag.Title = "Hồ sơ bệnh nhân";
            ViewBag.Nav = "mypatients";

            var did = await CurrentDoctorIdAsync();
            // CS0472 Warning (similar to above, comparing nullable int? to null is valid)
            if (did == null) return HttpNotFound();

            var patient = await _db.Patients
                                   .Where(p => p.Id == id)
                                   .Select(p => new PatientDetailVM // Map to correct ViewModel (Now found)
                                   {
                                       PatientId = p.Id,
                                       FullName = p.FullName,
                                       PhoneNumber = p.PhoneNumber,
                                       Email = p.Email,
                                       DOB = p.DateOfBirth,
                                       Gender = p.Gender,
                                       BloodType = p.BloodType,
                                       Address = p.Address,
                                       MedicalHistory = p.MedicalHistory,
                                       Allergies = p.Allergies,
                                       EmergencyContactName = p.EmergencyContactName,
                                       EmergencyContactPhone = p.EmergencyContactPhone
                                   })
                                   .FirstOrDefaultAsync();

            if (patient == null) return HttpNotFound();
            patient.Age = CalculateAge(patient.DOB);

            var visits = await _db.Appointments
                .Where(a => a.PatientId == id && a.DoctorId == did.Value)
                .OrderByDescending(a => a.StartTime)
                .Select(a => new PatientVisitRowVM // Map to correct ViewModel (Now found)
                {
                    AppointmentId = a.Id,
                    ServiceName = a.Service.Name, // Assuming Service navigation property exists
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Status = (int)a.Status, // Cast enum to int for the ViewModel
                    Notes = a.Notes
                })
                .ToListAsync();

            patient.Visits = visits;

            return View(patient);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
