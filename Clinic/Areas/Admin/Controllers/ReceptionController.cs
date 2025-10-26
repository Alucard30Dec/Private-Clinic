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

        // MODIFIED: Accepts filter and a single searchQuery
        public ActionResult Index(string filter = "today", string searchQuery = null)
        {
            ViewBag.Nav = "reception_appointments";
            ViewBag.CurrentFilter = filter;
            ViewBag.SearchQuery = searchQuery; // Pass search query back to view

            var query = _db.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.Service)
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

            // --- UPDATED SEARCH FILTERING (Single Box) ---
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                string searchLower = searchQuery.ToLower().Trim();
                query = query.Where(a =>
                    (a.Doctor != null && a.Doctor.Name.ToLower().Contains(searchLower)) ||
                    (a.Patient != null && a.Patient.FullName.ToLower().Contains(searchLower)) ||
                    (a.Patient != null && a.Patient.Email.ToLower().Contains(searchLower)) ||
                    (a.Service != null && a.Service.Name.ToLower().Contains(searchLower))
                );
            }
            // --- END SEARCH FILTERING ---

            var list = query
                .OrderBy(a => a.StartTime)
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