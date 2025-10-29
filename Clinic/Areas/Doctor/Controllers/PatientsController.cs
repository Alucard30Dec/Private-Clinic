using Clinic.Models;
using Microsoft.AspNet.Identity;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Clinic.Areas.Doctor.Data;
using System; // Thêm để dùng DateTime

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
            if (did == null) return HttpNotFound("Không tìm thấy hồ sơ bác sĩ.");

            // Lấy danh sách bệnh nhân đã từng khám với bác sĩ này
            // *** CẬP NHẬT TRUY VẤN ĐỂ LẤY THÊM TRƯỜNG VÀ TÍNH TUỔI ***
            var query = _db.Appointments
                           .Where(a => a.DoctorId == did.Value && a.PatientId != null) // Đảm bảo PatientId không null
                           .Select(a => a.Patient) // Lấy đối tượng Patient
                           .Where(p => p != null) // Lọc bỏ Patient null (nếu có lỗi dữ liệu)
                           .Distinct() // Chỉ lấy mỗi bệnh nhân 1 lần
                           .Select(p => new // Tạo ViewModel
                           {
                               Patient = p, // Giữ lại đối tượng Patient để lấy thông tin khác
                               TotalVisits = _db.Appointments.Count(ap => ap.PatientId == p.Id && ap.DoctorId == did.Value),
                               LastVisit = _db.Appointments
                                            .Where(ap => ap.PatientId == p.Id && ap.DoctorId == did.Value)
                                            .Max(ap => (DateTime?)ap.StartTime) // Lấy lần khám cuối cùng
                           })
                           .Select(g => new MyPatientRowVM // Map sang ViewModel cuối cùng
                           {
                               PatientId = g.Patient.Id,
                               FullName = g.Patient.FullName,
                               PhoneNumber = g.Patient.PhoneNumber,
                               Email = g.Patient.Email,
                               DOB = g.Patient.DateOfBirth,
                               Gender = g.Patient.Gender, // Lấy giới tính
                               TotalVisits = g.TotalVisits,
                               LastVisit = g.LastVisit,
                               // Tính tuổi trực tiếp trong Select hoặc sau khi ToListAsync()
                           });


            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                query = query.Where(x =>
                    (x.FullName != null && x.FullName.ToLower().Contains(q)) ||
                    (x.PhoneNumber != null && x.PhoneNumber.Contains(q)) || // SĐT không cần ToLower
                    (x.Email != null && x.Email.ToLower().Contains(q)) ||
                    (x.Gender != null && x.Gender.ToLower().Contains(q)) // Thêm tìm theo giới tính
                 );
            }

            var list = await query.OrderBy(x => x.FullName).ToListAsync();

            // Tính tuổi sau khi đã lấy dữ liệu
            list.ForEach(p => p.Age = CalculateAge(p.DOB));

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

            // Lấy thông tin bệnh nhân (bao gồm các trường mới)
            var patient = await _db.Patients
                                   .Where(p => p.Id == id)
                                   // *** LẤY THÊM TRƯỜNG ***
                                   .Select(p => new PatientDetailVM
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

            // Tính tuổi
            patient.Age = CalculateAge(patient.DOB);

            // Chỉ lấy các lần khám với chính bác sĩ này
            var visits = await _db.Appointments
                .Where(a => a.PatientId == id && a.DoctorId == did.Value)
                .OrderByDescending(a => a.StartTime)
                .Select(a => new PatientVisitRowVM
                {
                    AppointmentId = a.Id,
                    ServiceName = a.Service.Name,
                    StartTime = a.StartTime, // Giữ UTC để tính toán nếu cần
                    EndTime = a.EndTime,     // Giữ UTC
                    Status = (int)a.Status,
                    Notes = a.Notes
                    // Lấy thêm thông tin khám bệnh nếu có
                })
                .ToListAsync();

            patient.Visits = visits; // Gán danh sách visits vào ViewModel

            return View(patient); // Truyền ViewModel đã có visits
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
