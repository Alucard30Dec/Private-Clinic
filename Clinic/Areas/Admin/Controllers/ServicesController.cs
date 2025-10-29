using Clinic.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Reflection; // Keep for Attribute retrieval if needed by CreateExamTypeList's GetEnumDisplayName
using System.ComponentModel.DataAnnotations; // Keep for DisplayAttribute

namespace Clinic.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ServicesController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        public async Task<ActionResult> Index(string q = null)
        {
            ViewBag.Nav = "services";
            var servicesQuery = _db.Services.Where(s => s.IsVisible);
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                servicesQuery = servicesQuery.Where(s => s.Name.ToLower().Contains(q));
            }
            var list = await servicesQuery.OrderBy(s => s.Name).ToListAsync();
            return View(list);
        }

        public ActionResult Create()
        {
            ViewBag.Nav = "services";
            ViewBag.ExamTypeList = CreateExamTypeList();
            return View(new Service());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Name,Fee,DurationMinutes,ExamType,IsVisible")] Service s) // Include IsVisible if needed
        {
            ViewBag.Nav = "services";
            s.Name = s.Name?.Trim();

            if (await _db.Services.AnyAsync(svc => svc.IsVisible && svc.Name.ToLower() == s.Name.ToLower()))
            {
                ModelState.AddModelError("Name", "Tên dịch vụ này đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                s.IsVisible = true; // Ensure true on create
                _db.Services.Add(s);
                await _db.SaveChangesAsync();
                TempData["ok"] = "Đã thêm dịch vụ.";
                return RedirectToAction("Index");
            }

            ViewBag.ExamTypeList = CreateExamTypeList((int)s.ExamType);
            return View(s);
        }

        public async Task<ActionResult> Edit(int? id)
        {
            ViewBag.Nav = "services";
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var s = await _db.Services.FirstOrDefaultAsync(svc => svc.Id == id && svc.IsVisible);
            if (s == null) return HttpNotFound();
            ViewBag.ExamTypeList = CreateExamTypeList((int)s.ExamType);
            return View(s);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Name,Fee,DurationMinutes,ExamType,IsVisible")] Service input) // Include IsVisible
        {
            ViewBag.Nav = "services";
            input.Name = input.Name?.Trim();

            if (await _db.Services.AnyAsync(svc => svc.IsVisible && svc.Id != input.Id && svc.Name.ToLower() == input.Name.ToLower()))
            {
                ModelState.AddModelError("Name", "Tên dịch vụ này đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                var s = await _db.Services.FirstOrDefaultAsync(svc => svc.Id == input.Id && svc.IsVisible);
                if (s == null) return HttpNotFound();

                s.Name = input.Name;
                s.Fee = input.Fee;
                s.DurationMinutes = input.DurationMinutes;
                s.ExamType = input.ExamType;
                s.IsVisible = input.IsVisible; // Allow updating visibility if needed

                await _db.SaveChangesAsync();
                TempData["ok"] = "Đã cập nhật dịch vụ.";
                return RedirectToAction("Index");
            }

            ViewBag.ExamTypeList = CreateExamTypeList((int)input.ExamType);
            return View(input);
        }

        public async Task<ActionResult> Delete(int? id)
        {
            ViewBag.Nav = "services";
            if (id == null) { TempData["warn"] = "Thiếu mã."; return RedirectToAction("Index"); }
            var s = await _db.Services.FirstOrDefaultAsync(svc => svc.Id == id && svc.IsVisible);
            if (s == null) { TempData["warn"] = "Không tìm thấy hoặc dịch vụ đã bị ẩn."; return RedirectToAction("Index"); }
            return View(s);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var s = await _db.Services.FindAsync(id);
            if (s == null) return HttpNotFound();
            bool isInUse = await _db.Appointments.AnyAsync(a => a.ServiceId == id && a.Status != AppointmentStatus.Completed && a.Status != AppointmentStatus.Canceled && a.StartTime >= DateTime.UtcNow);
            if (isInUse)
            {
                TempData["err"] = "Không thể ẩn dịch vụ này vì đang được sử dụng trong lịch hẹn sắp tới.";
                return RedirectToAction("Index");
            }
            s.IsVisible = false;
            _db.Entry(s).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            TempData["ok"] = $"Đã ẩn dịch vụ '{s.Name}'.";
            return RedirectToAction("Index");
        }

        // --- Helper tạo SelectList cho ExamType ---
        private SelectList CreateExamTypeList(int? selectedValue = null)
        {
            // Use the extension method from Clinic.Models
            return new SelectList(
                Enum.GetValues(typeof(ExamType)).Cast<ExamType>().Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.GetDisplayName() // Use extension method
                }),
                "Value", "Text", selectedValue);
        }

        // *** REMOVED Duplicate GetEnumDisplayName Helper ***

        protected override void Dispose(bool disposing) { if (disposing) _db.Dispose(); base.Dispose(disposing); }
    }
}