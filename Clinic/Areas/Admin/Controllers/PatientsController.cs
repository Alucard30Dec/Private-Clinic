using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using Clinic.Models;

namespace Clinic.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PatientsController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: Admin/Patients
        public async Task<ActionResult> Index(string q = null)
        {
            ViewBag.Nav = "patients";
            var patients = _db.Patients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                patients = patients.Where(p =>
                    p.FullName.Contains(q) || p.Email.Contains(q) ||
                    p.PhoneNumber.Contains(q) || p.Address.Contains(q));
            }

            var list = await patients.OrderBy(p => p.FullName).ToListAsync();
            return View(list);
        }

        // GET: Admin/Patients/Create
        public ActionResult Create()
        {
            ViewBag.Nav = "patients";
            return View();
        }

        // POST: Admin/Patients/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include =
            "FullName,UserId,Email,PhoneNumber,DateOfBirth,Address")] Patient p)
        {
            ViewBag.Nav = "patients";
            if (!ModelState.IsValid) return View(p);

            _db.Patients.Add(p);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Đã thêm bệnh nhân.";
            return RedirectToAction("Index");
        }

        // GET: Admin/Patients/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            ViewBag.Nav = "patients";
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var p = await _db.Patients.FindAsync(id);
            if (p == null) return HttpNotFound();
            return View(p);
        }

        // POST: Admin/Patients/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include =
            "Id,FullName,UserId,Email,PhoneNumber,DateOfBirth,Address")] Patient input)
        {
            ViewBag.Nav = "patients";
            if (!ModelState.IsValid) return View(input);

            var p = await _db.Patients.FindAsync(input.Id);
            if (p == null) return HttpNotFound();

            p.FullName = input.FullName?.Trim();
            p.UserId = string.IsNullOrWhiteSpace(input.UserId) ? null : input.UserId.Trim();
            p.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
            p.PhoneNumber = string.IsNullOrWhiteSpace(input.PhoneNumber) ? null : input.PhoneNumber.Trim();
            p.DateOfBirth = input.DateOfBirth;
            p.Address = input.Address;

            await _db.SaveChangesAsync();
            TempData["ok"] = "Đã cập nhật bệnh nhân.";
            return RedirectToAction("Index");
        }

        // GET: Admin/Patients/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            ViewBag.Nav = "patients";
            if (id == null) { TempData["warn"] = "Thiếu mã."; return RedirectToAction("Index"); }
            var p = await _db.Patients.FindAsync(id);
            if (p == null) { TempData["warn"] = "Không tìm thấy."; return RedirectToAction("Index"); }
            return View(p);
        }

        // POST: Admin/Patients/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var p = await _db.Patients.FindAsync(id);
            if (p == null) return HttpNotFound();
            _db.Patients.Remove(p);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Đã xóa bệnh nhân.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing) { if (disposing) _db.Dispose(); base.Dispose(disposing); }
    }
}
