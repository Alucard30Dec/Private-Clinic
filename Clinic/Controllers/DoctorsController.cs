using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Clinic.Models;
using System.Data.Entity; // Add this
using System.Threading.Tasks; // Add this

namespace Clinic.Controllers
{
    public class DoctorsController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: /Doctors
        public async Task<ActionResult> Index(DoctorsFilterVm filter) // Make async
        {
            if (filter == null) filter = new DoctorsFilterVm();

            var query = (filter.Query ?? string.Empty).Trim();
            var specialtyName = (filter.Specialty ?? string.Empty).Trim(); // Rename variable

            if (filter.Page < 1) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 9;
            if (filter.PageSize > 48) filter.PageSize = 48;

            var q = _db.Doctors.Include(d => d.Specialty).Where(d => d.IsVisible); // Include Specialty, Filter IsVisible

            if (!string.IsNullOrEmpty(query))
            {
                var key = query.ToLower();
                q = q.Where(d => (d.Name ?? "").ToLower().Contains(key));
            }

            // *** FIX: Compare Specialty.Name ***
            if (!string.IsNullOrEmpty(specialtyName))
            {
                // Compare with the Name property of the Specialty navigation property
                q = q.Where(d => d.Specialty != null && d.Specialty.Name == specialtyName);
            }

            var total = await q.CountAsync(); // Use CountAsync

            var skip = (filter.Page - 1) * filter.PageSize;
            if (skip < 0) skip = 0;

            var items = await q // Use await
                .OrderBy(d => d.Name)
                .Skip(skip)
                .Take(filter.PageSize)
                .ToListAsync(); // Use ToListAsync

            // *** FIX: Get Specialty Names from Specialties table ***
            var specialties = await _db.Specialties // Query Specialties table
                .Where(s => s.IsVisible) // Filter visible specialties
                .Select(s => s.Name)     // Select only the name
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync(); // Use ToListAsync

            var vm = new DoctorsIndexVm
            {
                Filter = filter,
                Specialties = specialties, // Assign the list of names
                Result = new PagedResult<Doctor>
                {
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalItems = total,
                    Items = items
                }
            };

            return View(vm);
        }

        // GET: /Doctors/Details/5
        public async Task<ActionResult> Details(int id) // Make async
        {
            // *** FIX: Include Specialty, Filter IsVisible ***
            var doctor = await _db.Doctors
                                 .Include(d => d.Specialty) // Include Specialty
                                 .FirstOrDefaultAsync(d => d.Id == id && d.IsVisible); // Use FirstOrDefaultAsync

            if (doctor == null)
            {
                return HttpNotFound();
            }

            return View(doctor);
        }

        protected override void Dispose(bool disposing) { if (disposing) _db.Dispose(); base.Dispose(disposing); }
    }
}