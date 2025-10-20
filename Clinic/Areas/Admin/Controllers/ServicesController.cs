using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using Clinic.Models;

namespace Clinic.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ServicesController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        public async Task<ActionResult> Index(string q = null)
        {
            ViewBag.Nav = "services";
            var sv = _db.Services.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                sv = sv.Where(s => s.Name.Contains(q));
            }
            var list = await sv.OrderBy(s => s.Name).ToListAsync();
            return View(list);
        }

        public ActionResult Create() { ViewBag.Nav = "services"; return View(); }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Name,Fee,DurationMinutes")] Service s)
        {
            ViewBag.Nav = "services";
            if (!ModelState.IsValid) return View(s);
            _db.Services.Add(s);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Đã thêm dịch vụ.";
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Edit(int? id)
        {
            ViewBag.Nav = "services";
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var s = await _db.Services.FindAsync(id);
            if (s == null) return HttpNotFound();
            return View(s);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Name,Fee,DurationMinutes")] Service input)
        {
            ViewBag.Nav = "services";
            if (!ModelState.IsValid) return View(input);
            var s = await _db.Services.FindAsync(input.Id);
            if (s == null) return HttpNotFound();

            s.Name = input.Name?.Trim();
            s.Fee = input.Fee;
            s.DurationMinutes = input.DurationMinutes;

            await _db.SaveChangesAsync();
            TempData["ok"] = "Đã cập nhật dịch vụ.";
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Delete(int? id)
        {
            ViewBag.Nav = "services";
            if (id == null) { TempData["warn"] = "Thiếu mã."; return RedirectToAction("Index"); }
            var s = await _db.Services.FindAsync(id);
            if (s == null) { TempData["warn"] = "Không tìm thấy."; return RedirectToAction("Index"); }
            return View(s);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var s = await _db.Services.FindAsync(id);
            if (s == null) return HttpNotFound();
            _db.Services.Remove(s);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Đã xóa dịch vụ.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing) { if (disposing) _db.Dispose(); base.Dispose(disposing); }
    }
}
