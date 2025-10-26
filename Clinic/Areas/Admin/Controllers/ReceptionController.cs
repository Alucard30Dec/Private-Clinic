using System;
using System.Linq;
using System.Web.Mvc;
using Clinic.Models;
using System.Data.Entity;

namespace Clinic.Areas.Admin.Controllers
{
    [Authorize(Roles = "Receptionist,Admin")]
    public class ReceptionController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // MODIFIED: Accepts filter and search parameters
        public ActionResult Index(string filter = "today", string doctorName = null, DateTime? date = null, string patientEmail = null, string serviceName = null)
        {
            ViewBag.Nav = "reception_appointments";
            ViewBag.CurrentFilter = filter;

            // Store search terms for the view
            ViewBag.DoctorName = doctorName;
            ViewBag.Date = date;
            ViewBag.PatientEmail = patientEmail;
            ViewBag.ServiceName = serviceName;

            var query = _db.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.Service) // Make sure to include Service
                .Where(a => a.Status != AppointmentStatus.Canceled);

            // Apply date filter ("today" or "all") first
            if (filter == "today")
            {
                var localStart = DateTime.Today;
                var localEnd = localStart.AddDays(1);
                var startUtc = DateTime.SpecifyKind(localStart, DateTimeKind.Local).ToUniversalTime();
                var endUtc = DateTime.SpecifyKind(localEnd, DateTimeKind.Local).ToUniversalTime();

                query = query.Where(a => a.StartTime >= startUtc && a.StartTime < endUtc);
                ViewBag.Title = "Lịch hẹn Hôm nay";
            }
            else // filter == "all"
            {
                ViewBag.Title = "Tất cả Lịch hẹn";
            }

            // --- ADD SEARCH FILTERING ---
            if (!string.IsNullOrWhiteSpace(doctorName))
            {
                string nameLower = doctorName.ToLower().Trim();
                query = query.Where(a => a.Doctor != null && a.Doctor.Name.ToLower().Contains(nameLower));
            }
            if (date.HasValue)
            {
                DateTime searchDate = date.Value.Date; // Get only the date part
                // Compare date parts in UTC
                query = query.Where(a => DbFunctions.TruncateTime(a.StartTime) == searchDate);
            }
            if (!string.IsNullOrWhiteSpace(patientEmail))
            {
                string emailLower = patientEmail.ToLower().Trim();
                // Assuming Patient model has Email property linked via UserId -> ApplicationUser
                // If Patient model directly has Email, use: a.Patient.Email.ToLower().Contains(emailLower)
                query = query.Where(a => a.Patient != null && a.Patient.Email.ToLower().Contains(emailLower));
            }
            if (!string.IsNullOrWhiteSpace(serviceName))
            {
                string serviceLower = serviceName.ToLower().Trim();
                query = query.Where(a => a.Service != null && a.Service.Name.ToLower().Contains(serviceLower));
            }
            // --- END SEARCH FILTERING ---


            var list = query
                .OrderBy(a => a.StartTime) // Keep chronological order
                .ToList();

            return View(list); // Returns Index.cshtml
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _db.Dispose(); }
            base.Dispose(disposing);
        }
    }
}