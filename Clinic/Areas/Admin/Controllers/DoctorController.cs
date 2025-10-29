using Clinic.Models;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // Thêm List

// Đặt alias tránh trùng tên namespace
using DoctorEntity = Clinic.Models.Doctor;

namespace Clinic.Areas.Admin.Controllers
{
    // *** ViewModel cho Edit Doctor (Giữ nguyên) ***
    public class AdminDoctorEditViewModel : DoctorEntity
    {
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới (Để trống nếu không đổi)")]
        [StringLength(100, ErrorMessage = "{0} phải dài ít nhất {2} ký tự.", MinimumLength = 6)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [System.ComponentModel.DataAnnotations.Compare("NewPassword", ErrorMessage = "Mật khẩu mới và xác nhận mật khẩu không khớp.")]
        public string ConfirmPassword { get; set; }
    }

    [Authorize(Roles = "Admin")]
    public class DoctorController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();
        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get => _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            private set => _userManager = value;
        }

        // --- HELPER: Tải danh sách Chuyên khoa cho Dropdown ---
        private async Task LoadSpecialtiesAsync(int? selectedSpecialtyId = null)
        {
            var specialties = await _db.Specialties
                                       .Where(s => s.IsVisible) // Chỉ lấy chuyên khoa hiển thị
                                       .OrderBy(s => s.Name)
                                       .Select(s => new { s.Id, s.Name })
                                       .ToListAsync();
            ViewBag.SpecialtyId = new SelectList(specialties, "Id", "Name", selectedSpecialtyId);
        }
        // --- KẾT THÚC HELPER ---

        // GET: Admin/Doctor
        public async Task<ActionResult> Index(string q = null)
        {
            ViewBag.Nav = "doctors";

            // *** THÊM .Where(d => d.IsVisible) ***
            // *** SỬA: Include Specialty để hiển thị tên chuyên khoa ***
            var doctorsQuery = _db.Doctors
                                  .Include(d => d.Specialty) // Include để lấy tên Specialty
                                  .Where(d => d.IsVisible); // Chỉ lấy bác sĩ đang hiển thị

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                doctorsQuery = doctorsQuery.Where(d =>
                    (d.Name != null && d.Name.ToLower().Contains(q)) ||
                    (d.Specialty != null && d.Specialty.Name.ToLower().Contains(q)) || // Tìm theo tên chuyên khoa
                    (d.Email != null && d.Email.ToLower().Contains(q)) ||
                    (d.PhoneNumber != null && d.PhoneNumber.Contains(q)) ||
                    (d.Gender != null && d.Gender.ToLower().Contains(q)) ||
                    (d.NationalId != null && d.NationalId.Contains(q)) ||
                    (d.Address != null && d.Address.ToLower().Contains(q)));
            }

            var list = await doctorsQuery
                .OrderBy(d => d.Name)
                .ToListAsync();

            return View(list);
        }


        // GET: Admin/Doctor/Create
        public async Task<ActionResult> Create() // Thêm async
        {
            ViewBag.Nav = "doctors";
            // *** TẢI DANH SÁCH CHUYÊN KHOA ***
            await LoadSpecialtiesAsync();
            return View(new DoctorEntity()); // Truyền model rỗng
        }

        // POST: Admin/Doctor/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(
            // *** SỬA BIND: Bỏ Specialty, thêm SpecialtyId ***
            [Bind(Include = "Name,SpecialtyId,PhotoUrl,UserId,DateOfBirth,Gender,Email,PhoneNumber,NationalId,Address,YearsOfExperience,Bio")] DoctorEntity doctor,
            HttpPostedFileBase photo)
        {
            ViewBag.Nav = "doctors";

            doctor.Name = doctor.Name?.Trim();
            // Không trim SpecialtyId
            doctor.UserId = string.IsNullOrWhiteSpace(doctor.UserId) ? null : doctor.UserId.Trim();
            doctor.Gender = string.IsNullOrWhiteSpace(doctor.Gender) ? null : doctor.Gender.Trim();
            doctor.Email = string.IsNullOrWhiteSpace(doctor.Email) ? null : doctor.Email.Trim();
            doctor.PhoneNumber = string.IsNullOrWhiteSpace(doctor.PhoneNumber) ? null : doctor.PhoneNumber.Trim();
            doctor.NationalId = doctor.NationalId?.Trim();
            doctor.Address = doctor.Address?.Trim();

            // Validation bổ sung
            if (!string.IsNullOrWhiteSpace(doctor.Email))
            {
                // *** Thêm IsVisible vào check ***
                var emailTaken = await _db.Doctors.AnyAsync(d => d.IsVisible && d.Email == doctor.Email);
                if (emailTaken) ModelState.AddModelError("Email", "Email này đã tồn tại.");
            }
            if (!string.IsNullOrWhiteSpace(doctor.UserId))
            {
                // *** Thêm IsVisible vào check ***
                var userIdTaken = await _db.Doctors.AnyAsync(d => d.IsVisible && d.UserId == doctor.UserId);
                if (userIdTaken) ModelState.AddModelError("UserId", "UserId này đã được gán cho bác sĩ khác.");
                var userExists = await UserManager.FindByIdAsync(doctor.UserId) != null;
                if (!userExists) ModelState.AddModelError("UserId", "UserId không tồn tại trong hệ thống tài khoản.");
            }
            // *** THÊM: Kiểm tra SpecialtyId hợp lệ ***
            if (!_db.Specialties.Any(s => s.IsVisible && s.Id == doctor.SpecialtyId))
            {
                ModelState.AddModelError("SpecialtyId", "Chuyên khoa không hợp lệ.");
            }

            // --- Xử lý upload ảnh (đặt trước SaveChanges) ---
            string newPhotoUrl = null; // Biến lưu URL ảnh mới
            if (photo != null && photo.ContentLength > 0 && ModelState.IsValid) // Chỉ xử lý nếu các lỗi khác đã qua
            {
                var ext = Path.GetExtension(photo.FileName)?.ToLower();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("photo", "Định dạng ảnh không hợp lệ (chỉ .jpg, .jpeg, .png, .gif, .webp).");
                }
                else
                {
                    try
                    {
                        var folder = Server.MapPath("~/Content/uploads/doctors");
                        Directory.CreateDirectory(folder);
                        var fileName = $"doctor_{Guid.NewGuid()}{ext}";
                        var physicalPath = Path.Combine(folder, fileName);
                        photo.SaveAs(physicalPath);
                        newPhotoUrl = Url.Content($"~/Content/uploads/doctors/{fileName}");
                    }
                    catch (Exception ex) // *** FIX: Use 'ex' variable ***
                    {
                        // Log the exception or display a user-friendly message
                        System.Diagnostics.Debug.WriteLine($"Error saving doctor photo: {ex.Message}");
                        ModelState.AddModelError("", $"Lỗi khi lưu ảnh: {ex.Message}"); // Show generic error or ex.Message
                    }
                }
            }
            // --- Kết thúc xử lý ảnh ---


            if (!ModelState.IsValid)
            {
                // *** TẢI LẠI DANH SÁCH CHUYÊN KHOA KHI CÓ LỖI ***
                await LoadSpecialtiesAsync(doctor.SpecialtyId);
                return View(doctor);
            }

            // Gán URL ảnh mới (nếu có) vào model trước khi lưu
            doctor.PhotoUrl = newPhotoUrl ?? doctor.PhotoUrl?.Trim(); // Ưu tiên ảnh mới, nếu không thì giữ URL cũ (ít khi có ở Create)
            doctor.IsVisible = true; // Đảm bảo là true khi tạo mới

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

            // *** Thêm .Where(d => d.IsVisible) ***
            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == id && d.IsVisible);
            if (doctor == null) return HttpNotFound();

            // Chuyển Doctor sang ViewModel
            var viewModel = new AdminDoctorEditViewModel
            {
                Id = doctor.Id,
                Name = doctor.Name,
                SpecialtyId = doctor.SpecialtyId, // *** Gán SpecialtyId ***
                PhotoUrl = doctor.PhotoUrl,
                UserId = doctor.UserId,
                DateOfBirth = doctor.DateOfBirth,
                Gender = doctor.Gender,
                Email = doctor.Email,
                PhoneNumber = doctor.PhoneNumber,
                NationalId = doctor.NationalId,
                Address = doctor.Address,
                YearsOfExperience = doctor.YearsOfExperience,
                Bio = doctor.Bio
                // IsVisible không cần gán vì đang edit cái visible=true
            };

            // *** TẢI DANH SÁCH CHUYÊN KHOA ***
            await LoadSpecialtiesAsync(viewModel.SpecialtyId);
            ViewBag.HasAccount = !string.IsNullOrEmpty(doctor.UserId);

            return View(viewModel); // Trả về View Edit với ViewModel
        }


        // POST: Admin/Doctor/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(AdminDoctorEditViewModel formViewModel, HttpPostedFileBase photo)
        {
            ViewBag.Nav = "doctors";
            bool hasAccount = !string.IsNullOrEmpty(formViewModel.UserId);
            ViewBag.HasAccount = hasAccount;

            // Trim dữ liệu trên ViewModel
            formViewModel.Name = formViewModel.Name?.Trim();
            // Không trim SpecialtyId
            formViewModel.UserId = string.IsNullOrWhiteSpace(formViewModel.UserId) ? null : formViewModel.UserId.Trim();
            formViewModel.Gender = string.IsNullOrWhiteSpace(formViewModel.Gender) ? null : formViewModel.Gender.Trim();
            formViewModel.Email = string.IsNullOrWhiteSpace(formViewModel.Email) ? null : formViewModel.Email.Trim();
            formViewModel.PhoneNumber = string.IsNullOrWhiteSpace(formViewModel.PhoneNumber) ? null : formViewModel.PhoneNumber.Trim();
            formViewModel.NationalId = formViewModel.NationalId?.Trim();
            formViewModel.Address = formViewModel.Address?.Trim();

            // Validation bổ sung
            if (!string.IsNullOrWhiteSpace(formViewModel.Email))
            {
                // *** Thêm IsVisible vào check ***
                var emailTaken = await _db.Doctors.AnyAsync(d => d.IsVisible && d.Id != formViewModel.Id && d.Email == formViewModel.Email);
                if (emailTaken) ModelState.AddModelError("Email", "Email này đã tồn tại.");
            }
            if (!string.IsNullOrWhiteSpace(formViewModel.UserId))
            {
                // *** Thêm IsVisible vào check ***
                var userIdTaken = await _db.Doctors.AnyAsync(d => d.IsVisible && d.Id != formViewModel.Id && d.UserId == formViewModel.UserId);
                if (userIdTaken) ModelState.AddModelError("UserId", "UserId này đã được gán cho bác sĩ khác.");
                var userExists = await UserManager.FindByIdAsync(formViewModel.UserId) != null;
                if (!userExists) ModelState.AddModelError("UserId", "UserId không tồn tại trong hệ thống tài khoản.");
            }
            // *** THÊM: Kiểm tra SpecialtyId hợp lệ ***
            if (!_db.Specialties.Any(s => s.IsVisible && s.Id == formViewModel.SpecialtyId))
            {
                ModelState.AddModelError("SpecialtyId", "Chuyên khoa không hợp lệ.");
            }

            // Validation mật khẩu mới (giữ nguyên)
            bool isUpdatingPassword = !string.IsNullOrEmpty(formViewModel.NewPassword);
            if (isUpdatingPassword)
            {
                if (!hasAccount)
                {
                    ModelState.AddModelError("", "Bác sĩ này không có tài khoản để đặt mật khẩu.");
                }
                else
                {
                    if (string.IsNullOrEmpty(formViewModel.ConfirmPassword))
                    {
                        ModelState.AddModelError("ConfirmPassword", "Vui lòng xác nhận mật khẩu mới.");
                    }
                }
            }

            // --- XỬ LÝ ẢNH --- (Giữ nguyên logic)
            string oldPhotoUrl = null;
            string newPhotoUrl = null;
            if (ModelState.IsValid && photo != null && photo.ContentLength > 0)
            {
                var ext = Path.GetExtension(photo.FileName)?.ToLower();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("photo", "Định dạng ảnh không hợp lệ.");
                }
                else
                {
                    var folder = Server.MapPath("~/Content/uploads/doctors");
                    Directory.CreateDirectory(folder);
                    var fileName = $"doctor_{formViewModel.Id}_{Guid.NewGuid()}{ext}";
                    var physicalPath = Path.Combine(folder, fileName);
                    try
                    {
                        photo.SaveAs(physicalPath);
                        var doctorInDbForPhoto = await _db.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == formViewModel.Id);
                        if (doctorInDbForPhoto != null) oldPhotoUrl = doctorInDbForPhoto.PhotoUrl;
                        newPhotoUrl = Url.Content($"~/Content/uploads/doctors/{fileName}");
                    }
                    catch (Exception ex) // *** FIX: Use 'ex' variable ***
                    {
                        System.Diagnostics.Debug.WriteLine($"Error saving doctor photo on edit: {ex.Message}");
                        ModelState.AddModelError("", $"Lỗi khi lưu ảnh: {ex.Message}");
                    }
                }
            }
            // --- Kết thúc xử lý ảnh ---

            if (ModelState.IsValid)
            {
                // *** Thêm .Where(d => d.IsVisible) ***
                var doctorInDb = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == formViewModel.Id && d.IsVisible);
                if (doctorInDb == null) return HttpNotFound();

                // Cập nhật thông tin bác sĩ từ ViewModel
                doctorInDb.Name = formViewModel.Name;
                doctorInDb.SpecialtyId = formViewModel.SpecialtyId; // *** Cập nhật SpecialtyId ***
                doctorInDb.UserId = formViewModel.UserId;
                doctorInDb.DateOfBirth = formViewModel.DateOfBirth;
                doctorInDb.Gender = formViewModel.Gender;
                doctorInDb.Email = formViewModel.Email;
                doctorInDb.PhoneNumber = formViewModel.PhoneNumber;
                doctorInDb.NationalId = formViewModel.NationalId;
                doctorInDb.Address = formViewModel.Address;
                doctorInDb.YearsOfExperience = formViewModel.YearsOfExperience;
                doctorInDb.Bio = formViewModel.Bio;

                // Cập nhật PhotoUrl nếu có ảnh mới upload
                if (!string.IsNullOrEmpty(newPhotoUrl))
                {
                    doctorInDb.PhotoUrl = newPhotoUrl;
                }
                // Nếu không upload ảnh mới, KHÔNG thay đổi PhotoUrl hiện có trong DB (không lấy từ formViewModel)

                // Xử lý cập nhật mật khẩu (giữ nguyên logic)
                IdentityResult passwordResult = null;
                if (isUpdatingPassword && hasAccount)
                {
                    // ... (code xử lý password giữ nguyên) ...
                    if (string.IsNullOrEmpty(doctorInDb.UserId))
                    {
                        TempData["err"] = "Lỗi: Không tìm thấy tài khoản liên kết để cập nhật mật khẩu.";
                        await LoadSpecialtiesAsync(formViewModel.SpecialtyId); // Load lại dropdown
                        return View(formViewModel);
                    }
                    var user = await UserManager.FindByIdAsync(doctorInDb.UserId);
                    if (user == null)
                    {
                        TempData["err"] = "Lỗi: Không tìm thấy tài khoản Identity liên kết.";
                        await LoadSpecialtiesAsync(formViewModel.SpecialtyId); // Load lại dropdown
                        return View(formViewModel);
                    }
                    string resetToken = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                    passwordResult = await UserManager.ResetPasswordAsync(user.Id, resetToken, formViewModel.NewPassword);
                    if (!passwordResult.Succeeded)
                    {
                        AddErrors(passwordResult);
                        await LoadSpecialtiesAsync(formViewModel.SpecialtyId); // Load lại dropdown
                        return View(formViewModel);
                    }
                }

                _db.Entry(doctorInDb).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                // Xóa ảnh cũ (nếu có và upload ảnh mới thành công)
                if (!string.IsNullOrEmpty(oldPhotoUrl) && !string.IsNullOrEmpty(newPhotoUrl) && oldPhotoUrl != newPhotoUrl)
                {
                    try
                    {
                        var oldPath = Server.MapPath(oldPhotoUrl);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                    catch { /* ignore */ }
                }

                TempData["ok"] = "Đã cập nhật thông tin bác sĩ." + (isUpdatingPassword && passwordResult.Succeeded ? " Mật khẩu cũng đã được đặt lại." : "");
                return RedirectToAction("Index");
            }

            // Nếu ModelState không hợp lệ
            // *** TẢI LẠI DANH SÁCH CHUYÊN KHOA KHI CÓ LỖI ***
            await LoadSpecialtiesAsync(formViewModel.SpecialtyId);
            return View(formViewModel);
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

            // *** Thêm .Where(d => d.IsVisible) ***
            // *** SỬA: Include Specialty ***
            var doctor = await _db.Doctors
                                  .Include(d => d.Specialty) // Include để hiển thị tên Specialty
                                  .FirstOrDefaultAsync(d => d.Id == id && d.IsVisible);
            if (doctor == null)
            {
                TempData["warn"] = "Không tìm thấy bác sĩ hoặc bác sĩ đã bị ẩn.";
                return RedirectToAction("Index");
            }

            return View(doctor);
        }


        // POST: Admin/Doctor/Delete/5 (SOFT DELETE)
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            // *** Sửa .FirstOrDefaultAsync(d => d.Id == id && d.IsVisible) ***
            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == id && d.IsVisible);
            if (doctor == null)
            {
                TempData["warn"] = "Không tìm thấy bác sĩ hoặc bác sĩ đã bị ẩn.";
                return RedirectToAction("Index");
            }

            // Kiểm tra ràng buộc (ví dụ: Lịch hẹn chưa hoàn thành/hủy, Ca làm việc)
            bool hasActiveAppointments = await _db.Appointments.AnyAsync(a => a.DoctorId == id && a.Status != AppointmentStatus.Completed && a.Status != AppointmentStatus.Canceled && a.StartTime >= DateTime.UtcNow);
            bool hasWorkShifts = await _db.WorkingHours.AnyAsync(w => w.DoctorId == id); // Kiểm tra xem còn ca làm ko

            if (hasActiveAppointments)
            {
                TempData["err"] = "Không thể ẩn bác sĩ này vì còn Lịch hẹn sắp tới hoặc đang diễn ra chưa hoàn thành/hủy.";
                return RedirectToAction("Index");
            }
            if (hasWorkShifts)
            {
                // Cân nhắc: Có nên cho ẩn bác sĩ dù còn ca làm không? Hay bắt buộc xóa ca làm trước?
                // Tạm thời cho phép ẩn, nhưng bạn có thể thay đổi logic này.
                // TempData["warn"] = "Bác sĩ này vẫn còn đăng ký ca làm việc.";
            }

            // *** THỰC HIỆN SOFT DELETE ***
            doctor.IsVisible = false;
            _db.Entry(doctor).State = EntityState.Modified;
            // Cân nhắc: Có nên xóa luôn tài khoản Identity liên kết không?
            // Nếu xóa, cần xử lý lỗi nếu xóa Identity thất bại. Tạm thời không xóa.
            /*
            if (!string.IsNullOrEmpty(doctor.UserId))
            {
                var user = await UserManager.FindByIdAsync(doctor.UserId);
                if (user != null)
                {
                    // var result = await UserManager.DeleteAsync(user);
                    // if (!result.Succeeded) { ... }
                }
            }
            */
            // Không cần xóa ảnh khi ẩn

            await _db.SaveChangesAsync();

            TempData["ok"] = $"Đã ẩn bác sĩ '{doctor.Name}'."; // Thông báo là đã ẩn
            return RedirectToAction("Index");
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db?.Dispose();
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }
            }
            base.Dispose(disposing);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }
    }
}
