using Clinic.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Clinic.Areas.Doctor.Data;

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
        // Bộ lọc đơn giản: q (tên bệnh nhân), từ ngày, đến ngày, trạng thái
        public async Task<ActionResult> Index(string q = null, DateTime? from = null, DateTime? to = null, int? status = null)
        {
            ViewBag.Title = "Hồ sơ khám";
            ViewBag.Nav = "records";

            var did = await CurrentDoctorIdAsync();
            if (did == null) return HttpNotFound();

            var query = _db.Appointments
                .Where(a => a.DoctorId == did.Value)
                .Select(a => new RecordRowVM
                {
                    AppointmentId = a.Id,
                    PatientName = a.Patient.FullName,
                    ServiceName = a.Service.Name,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Status = (int)a.Status,
                    Notes = a.Notes
                });

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(x => x.PatientName.Contains(q) || x.ServiceName.Contains(q));
            }
            if (from.HasValue) query = query.Where(x => x.StartTime >= from.Value);
            if (to.HasValue) query = query.Where(x => x.StartTime < to.Value.AddDays(1));
            if (status.HasValue) query = query.Where(x => x.Status == status.Value);

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
