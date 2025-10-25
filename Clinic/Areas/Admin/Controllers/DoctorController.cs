using Clinic.Models;                 // chứa ClinicDbContext
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

// === ĐẶT ALIAS TRÁNH TRÙNG TÊN VỚI NAMESPACE Clinic.Areas.Doctor ===
using DoctorEntity = Clinic.Models.Doctor;

namespace Clinic.Areas.Admin.Controllers
{
    // === THAY ĐỔI QUAN TRỌNG: Thêm [Area("Admin")] ===
    // Điều này chỉ định rõ ràng cho MVC biết Controller này thuộc Area "Admin"
    // để phân biệt với Controller "DoctorsController" ở bên ngoài.
    
    [Authorize(Roles = "Admin")]
    public class DoctorController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: Admin/Doctor
        public async Task<ActionResult> Index(string q = null)
        {
            ViewBag.Nav = "doctors"; // dùng để active menu trong layout

            var doctors = _db.Doctors.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                doctors = doctors.Where(d =>
                    d.Name.Contains(q) ||
                    d.Specialty.Contains(q) ||
                    d.Email.Contains(q) ||
                    d.PhoneNumber.Contains(q) ||
                    d.Gender.Contains(q));
            }

            var list = await doctors
                .OrderBy(d => d.Name)
                .ToListAsync();

            return View(list);
        }

        // GET: Admin/Doctor/Create
        public ActionResult Create()
        {
            ViewBag.Nav = "doctors";
            return View();
        }

        // POST: Admin/Doctor/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include =
            "Name,Specialty,PhotoUrl,UserId,DateOfBirth,Gender,Email,PhoneNumber,YearsOfExperience,Bio")] DoctorEntity doctor)
        {
            ViewBag.Nav = "doctors";

            if (!ModelState.IsValid) return View(doctor);

            // (Tuỳ chọn) Kiểm tra trùng email
            if (!string.IsNullOrWhiteSpace(doctor.Email))
            {
                var emailTaken = await _db.Doctors.AnyAsync(d => d.Email == doctor.Email);
                if (emailTaken)
                {
                    ModelState.AddModelError("Email", "Email này đã tồn tại.");
                    return View(doctor);
                }
            }

            // Chuẩn hoá nhẹ
            doctor.Name = doctor.Name?.Trim();
            doctor.Specialty = doctor.Specialty?.Trim();
            doctor.PhotoUrl = doctor.PhotoUrl?.Trim();
            doctor.UserId = string.IsNullOrWhiteSpace(doctor.UserId) ? null : doctor.UserId.Trim();
            doctor.Gender = string.IsNullOrWhiteSpace(doctor.Gender) ? null : doctor.Gender.Trim();
            doctor.Email = string.IsNullOrWhiteSpace(doctor.Email) ? null : doctor.Email.Trim();
            doctor.PhoneNumber = string.IsNullOrWhiteSpace(doctor.PhoneNumber) ? null : doctor.PhoneNumber.Trim();
            // YearsOfExperience (int?) & DateOfBirth (DateTime?) giữ nguyên
            // Bio giữ nguyên

            _db.Doctors.Add(doctor);
            await _db.SaveChangesAsync();

            TempData["ok"] = "Đã tạo bác sĩ mới.";
            return RedirectToAction("Index");
        }

        // GET: Admin/Doctor/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            ViewBag.Nav = "doctors";
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var doctor = await _db.Doctors.FindAsync(id);
            if (doctor == null) return HttpNotFound();

            return View(doctor);
        }

        // POST: Admin/Doctor/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(
            [Bind(Include = "Id,Name,Specialty,PhotoUrl,UserId,DateOfBirth,Gender,Email,PhoneNumber,YearsOfExperience,Bio")]
            DoctorEntity input,
            HttpPostedFileBase photo // <-- file upload từ form
        )
        {
            ViewBag.Nav = "doctors";
            if (!ModelState.IsValid) return View(input);

            var doctor = await _db.Doctors.FindAsync(input.Id);
            if (doctor == null) return HttpNotFound();

            // (tuỳ chọn) kiểm tra trùng email
            if (!string.IsNullOrWhiteSpace(input.Email))
            {
                var taken = await _db.Doctors.AnyAsync(d => d.Id != input.Id && d.Email == input.Email);
                if (taken)
                {
                    ModelState.AddModelError("Email", "Email này đã tồn tại.");
                    return View(input);
                }
            }

            // Gán các thuộc tính text
            doctor.Name = input.Name?.Trim();
            doctor.Specialty = input.Specialty?.Trim();
            doctor.UserId = string.IsNullOrWhiteSpace(input.UserId) ? null : input.UserId.Trim();
            doctor.DateOfBirth = input.DateOfBirth;
            doctor.Gender = string.IsNullOrWhiteSpace(input.Gender) ? null : input.Gender.Trim();
            doctor.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
            doctor.PhoneNumber = string.IsNullOrWhiteSpace(input.PhoneNumber) ? null : input.PhoneNumber.Trim();
            doctor.YearsOfExperience = input.YearsOfExperience;
            doctor.Bio = input.Bio;

            // === Upload ảnh nếu có ===
            if (photo != null && photo.ContentLength > 0)
            {
                // kiểm tra định dạng cơ bản
                var ext = Path.GetExtension(photo.FileName)?.ToLower();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("", "Định dạng ảnh không hợp lệ (chỉ .jpg, .jpeg, .png, .gif, .webp).");
                    return View(input);
                }

                // tạo thư mục /Content/uploads/doctors nếu chưa có
                var folder = Server.MapPath("~/Content/uploads/doctors");
                Directory.CreateDirectory(folder);

                // tên file duy nhất
                var fileName = $"doctor_{doctor.Id}_{DateTime.UtcNow.Ticks}{ext}";
                var physicalPath = Path.Combine(folder, fileName);

                // lưu file
                photo.SaveAs(physicalPath);

                // (tuỳ chọn) Xóa ảnh cũ nếu bạn muốn
                if (!string.IsNullOrWhiteSpace(doctor.PhotoUrl))
                {
                    try
                    {
                        var oldPath = Server.MapPath(doctor.PhotoUrl);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                    catch { /* ignore */ }
                }

                // đường dẫn để hiển thị trên web
                doctor.PhotoUrl = Url.Content($"~/Content/uploads/doctors/{fileName}");
            }
            else
            {
                // Nếu không upload mới, giữ nguyên PhotoUrl từ input (trong form có hidden)
                doctor.PhotoUrl = input.PhotoUrl;
            }

            await _db.SaveChangesAsync();
            TempData["ok"] = "Đã cập nhật thông tin bác sĩ.";
            return RedirectToAction("Index");
        }

        // GET: Admin/Doctor/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            ViewBag.Nav = "doctors";
            if (id == null)
            {
                TempData["warn"] = "Thiếu mã bác sĩ cần xoá.";
                return RedirectToAction("Index");
            }

            var doctor = await _db.Doctors.FindAsync(id);
            if (doctor == null)
            {
                TempData["warn"] = "Không tìm thấy bác sĩ.";
                return RedirectToAction("Index");
            }

            return View(doctor);
        }

        // POST: Admin/Doctor/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _db.Doctors.FindAsync(id);
            if (doctor == null) return HttpNotFound();

            _db.Doctors.Remove(doctor);
            await _db.SaveChangesAsync();

            TempData["ok"] = "Đã xoá bác sĩ.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}