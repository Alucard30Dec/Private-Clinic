using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using Clinic.Models;

namespace Clinic.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SpecialtiesController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: Admin/Specialties
        public async Task<ActionResult> Index(string q = null)
        {
            ViewBag.Nav = "specialties"; // Key cho layout menu

            // Chỉ lấy các chuyên khoa đang hiển thị (IsVisible = true)
            var specialtiesQuery = _db.Specialties.Where(s => s.IsVisible);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                specialtiesQuery = specialtiesQuery.Where(s => s.Name.ToLower().Contains(q));
            }

            var list = await specialtiesQuery.OrderBy(s => s.Name).ToListAsync();
            return View(list); // Trả về View Areas/Admin/Views/Specialties/Index.cshtml
        }

        // GET: Admin/Specialties/Create
        public ActionResult Create()
        {
            ViewBag.Nav = "specialties";
            return View(new Specialty()); // Trả về View Areas/Admin/Views/Specialties/Create.cshtml
        }

        // POST: Admin/Specialties/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Name")] Specialty specialty)
        {
            ViewBag.Nav = "specialties";
            specialty.Name = specialty.Name?.Trim(); // Trim whitespace

            // Kiểm tra trùng tên (không phân biệt hoa thường, chỉ xét IsVisible = true)
            if (await _db.Specialties.AnyAsync(s => s.IsVisible && s.Name.ToLower() == specialty.Name.ToLower()))
            {
                ModelState.AddModelError("Name", "Tên chuyên khoa này đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                specialty.IsVisible = true; // Đảm bảo là true khi tạo mới
                _db.Specialties.Add(specialty);
                await _db.SaveChangesAsync();
                TempData["ok"] = "Đã thêm chuyên khoa mới.";
                return RedirectToAction("Index");
            }

            return View(specialty); // Trả về view Create với lỗi
        }

        // GET: Admin/Specialties/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            ViewBag.Nav = "specialties";
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // Lấy cả chuyên khoa đã ẩn để có thể sửa tên nếu muốn
            var specialty = await _db.Specialties.FindAsync(id);
            if (specialty == null) return HttpNotFound();

            // Chỉ cho sửa chuyên khoa đang hiển thị (IsVisible = true)
            // Hoặc bạn có thể cho sửa cả chuyên khoa ẩn nếu cần
            if (!specialty.IsVisible)
            {
                TempData["warn"] = "Không thể sửa chuyên khoa đã bị ẩn.";
                return RedirectToAction("Index");
            }

            return View(specialty); // Trả về View Areas/Admin/Views/Specialties/Edit.cshtml
        }

        // POST: Admin/Specialties/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Name")] Specialty formSpecialty)
        {
            ViewBag.Nav = "specialties";
            formSpecialty.Name = formSpecialty.Name?.Trim();

            // Kiểm tra trùng tên với chuyên khoa khác (đang hiển thị)
            if (await _db.Specialties.AnyAsync(s => s.IsVisible && s.Id != formSpecialty.Id && s.Name.ToLower() == formSpecialty.Name.ToLower()))
            {
                ModelState.AddModelError("Name", "Tên chuyên khoa này đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                var specialtyInDb = await _db.Specialties.FindAsync(formSpecialty.Id);
                if (specialtyInDb == null || !specialtyInDb.IsVisible) // Chỉ sửa cái đang hiển thị
                {
                    return HttpNotFound();
                }

                specialtyInDb.Name = formSpecialty.Name;
                _db.Entry(specialtyInDb).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                TempData["ok"] = "Đã cập nhật chuyên khoa.";
                return RedirectToAction("Index");
            }

            return View(formSpecialty); // Trả về view Edit với lỗi
        }

        // GET: Admin/Specialties/Delete/5 (Hiển thị xác nhận)
        public async Task<ActionResult> Delete(int? id)
        {
            ViewBag.Nav = "specialties";
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var specialty = await _db.Specialties.FirstOrDefaultAsync(s => s.Id == id && s.IsVisible); // Chỉ lấy cái đang hiển thị
            if (specialty == null)
            {
                TempData["warn"] = "Không tìm thấy chuyên khoa hoặc chuyên khoa đã bị ẩn.";
                return RedirectToAction("Index");
            }

            return View(specialty); // Trả về View Areas/Admin/Views/Specialties/Delete.cshtml
        }

        // POST: Admin/Specialties/Delete/5 (Thực hiện Soft Delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var specialty = await _db.Specialties.FindAsync(id);
            if (specialty == null) return HttpNotFound();

            // Kiểm tra ràng buộc: Có bác sĩ nào đang dùng chuyên khoa này không?
            bool isInUse = await _db.Doctors.AnyAsync(d => d.IsVisible && d.SpecialtyId == id);
            if (isInUse)
            {
                TempData["err"] = "Không thể ẩn chuyên khoa này vì đang có bác sĩ sử dụng.";
                return RedirectToAction("Index");
            }

            // Thực hiện Soft Delete
            specialty.IsVisible = false;
            _db.Entry(specialty).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            TempData["ok"] = $"Đã ẩn chuyên khoa '{specialty.Name}'.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
