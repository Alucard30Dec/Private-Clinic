using Clinic.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web;
using Microsoft.Owin.Security;
using System.Net;
using System.ComponentModel.DataAnnotations; // *** THÊM USING NÀY ***
// *** KHÔNG CẦN using System.Web.Mvc.CompareAttribute; ***

// *** NAMESPACE SỬA LẠI ĐỂ KHỚP VỚI AREA ADMIN ***
namespace Clinic.Areas.Admin.Controllers
{
    // ViewModel để bao gồm mật khẩu mới khi edit
    public class AdminPatientEditViewModel : Patient
    {
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        [StringLength(100, ErrorMessage = "{0} phải dài ít nhất {2} ký tự.", MinimumLength = 6)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        // *** SỬA Ở ĐÂY: Chỉ định rõ namespace ***
        [System.ComponentModel.DataAnnotations.Compare("NewPassword", ErrorMessage = "Mật khẩu mới và xác nhận mật khẩu không khớp.")]
        public string ConfirmPassword { get; set; }
    }


    // *** CLASS NAME SỬA LẠI THÀNH PatientsController ĐỂ KHỚP VỚI ROUTE ***
    [Authorize(Roles = "Admin")] // Chỉ Admin mới vào được controller này
    public class PatientsController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // *** SỬA: Khai báo _userManager và _signInManager để dùng trong Dispose ***
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;


        // Helpers để lấy UserManager và SignInManager
        // *** SỬA: Gán giá trị cho _signInManager/_userManager nếu chúng null ***
        public ApplicationSignInManager SignInManager
        {
            get => _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            private set => _signInManager = value;
        }
        public ApplicationUserManager UserManager
        {
            get => _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            private set => _userManager = value;
        }
        // *** KẾT THÚC SỬA ***
        private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;

        // GET: /Admin/Patients
        public async Task<ActionResult> Index(string q = null)
        {
            ViewBag.Nav = "patients"; // Active menu admin

            var patients = _db.Patients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower(); // Chuẩn hóa tìm kiếm
                patients = patients.Where(p =>
                    (p.FullName != null && p.FullName.ToLower().Contains(q)) ||
                    (p.Email != null && p.Email.ToLower().Contains(q)) ||
                    (p.PhoneNumber != null && p.PhoneNumber.Contains(q)) ||
                    (p.Address != null && p.Address.ToLower().Contains(q)) ||
                    (p.Gender != null && p.Gender.ToLower().Contains(q))
                );
            }

            var list = await patients.OrderBy(p => p.FullName).ToListAsync();
            return View(list); // Trả về View Areas/Admin/Views/Patients/Index.cshtml
        }


        // GET: /Admin/Patients/Create
        public ActionResult Create()
        {
            ViewBag.Nav = "patients";
            return View(new Patient()); // Trả về View Areas/Admin/Views/Patients/Create.cshtml
        }

        // POST: /Admin/Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(
             [Bind(Include = "FullName,Email,PhoneNumber,DateOfBirth,Address," +
                            "Gender, NationalId, BloodType, MedicalHistory, Allergies, " +
                            "EmergencyContactName, EmergencyContactRelationship, EmergencyContactPhone, " +
                            "UserId")] Patient patient
            )
        {
            ViewBag.Nav = "patients";

            // Trim dữ liệu string
            patient.FullName = patient.FullName?.Trim();
            patient.Email = patient.Email?.Trim();
            patient.PhoneNumber = patient.PhoneNumber?.Trim();
            patient.Address = patient.Address?.Trim();
            patient.Gender = patient.Gender?.Trim();
            patient.NationalId = patient.NationalId?.Trim();
            patient.BloodType = patient.BloodType?.Trim();
            patient.MedicalHistory = patient.MedicalHistory?.Trim();
            patient.Allergies = patient.Allergies?.Trim();
            patient.EmergencyContactName = patient.EmergencyContactName?.Trim();
            patient.EmergencyContactRelationship = patient.EmergencyContactRelationship?.Trim();
            patient.EmergencyContactPhone = patient.EmergencyContactPhone?.Trim();
            patient.UserId = string.IsNullOrWhiteSpace(patient.UserId) ? null : patient.UserId.Trim();

            // --- Validation bổ sung phía server ---
            if (string.IsNullOrWhiteSpace(patient.FullName))
                ModelState.AddModelError("FullName", "Họ tên là bắt buộc.");
            if (string.IsNullOrWhiteSpace(patient.PhoneNumber)) // Bắt buộc SĐT trong Admin context
                ModelState.AddModelError("PhoneNumber", "Số điện thoại là bắt buộc.");
            if (!string.IsNullOrWhiteSpace(patient.Email))
            {
                bool emailExists = await _db.Patients.AnyAsync(p => p.Email == patient.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Địa chỉ email này đã được sử dụng.");
                }
            }
            if (!string.IsNullOrWhiteSpace(patient.UserId))
            {
                bool userIdExists = await _db.Patients.AnyAsync(p => p.UserId == patient.UserId);
                if (userIdExists)
                {
                    ModelState.AddModelError("UserId", "UserId này đã được liên kết với bệnh nhân khác.");
                }
                var identityUserExists = await UserManager.FindByIdAsync(patient.UserId) != null;
                if (!identityUserExists)
                {
                    ModelState.AddModelError("UserId", "UserId không tồn tại trong hệ thống tài khoản.");
                }
            }

            if (ModelState.IsValid)
            {
                patient.CreatedAt = DateTime.UtcNow; // Gán thời gian tạo
                _db.Patients.Add(patient);
                await _db.SaveChangesAsync();
                TempData["ok"] = "Đã thêm bệnh nhân mới thành công.";
                return RedirectToAction("Index");
            }

            // Nếu không hợp lệ, trả về View Create với lỗi
            return View(patient);
        }

        // GET: /Admin/Patients/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            ViewBag.Nav = "patients";
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var patient = await _db.Patients.FindAsync(id);
            if (patient == null) return HttpNotFound();

            // Chuyển Patient sang ViewModel để truyền sang View
            var viewModel = new AdminPatientEditViewModel
            {
                // Copy các thuộc tính từ patient sang viewModel
                Id = patient.Id,
                FullName = patient.FullName,
                Email = patient.Email,
                PhoneNumber = patient.PhoneNumber,
                DateOfBirth = patient.DateOfBirth,
                Address = patient.Address,
                Gender = patient.Gender,
                NationalId = patient.NationalId,
                BloodType = patient.BloodType,
                MedicalHistory = patient.MedicalHistory,
                Allergies = patient.Allergies,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactRelationship = patient.EmergencyContactRelationship,
                EmergencyContactPhone = patient.EmergencyContactPhone,
                UserId = patient.UserId,
                CreatedAt = patient.CreatedAt,
                UpdatedAt = patient.UpdatedAt
            };

            ViewBag.HasAccount = !string.IsNullOrEmpty(patient.UserId);

            return View(viewModel); // Trả về View Areas/Admin/Views/Patients/Edit.cshtml với ViewModel
        }

        // POST: /Admin/Patients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(AdminPatientEditViewModel formViewModel) // Nhận ViewModel từ form
        {
            ViewBag.Nav = "patients";
            bool hasAccount = !string.IsNullOrEmpty(formViewModel.UserId);
            ViewBag.HasAccount = hasAccount; // Truyền lại trạng thái tài khoản cho View nếu có lỗi

            // Trim dữ liệu string trên ViewModel
            formViewModel.FullName = formViewModel.FullName?.Trim();
            formViewModel.Email = formViewModel.Email?.Trim();
            formViewModel.PhoneNumber = formViewModel.PhoneNumber?.Trim();
            formViewModel.Address = formViewModel.Address?.Trim();
            formViewModel.Gender = formViewModel.Gender?.Trim();
            formViewModel.NationalId = formViewModel.NationalId?.Trim();
            formViewModel.BloodType = formViewModel.BloodType?.Trim();
            formViewModel.MedicalHistory = formViewModel.MedicalHistory?.Trim();
            formViewModel.Allergies = formViewModel.Allergies?.Trim();
            formViewModel.EmergencyContactName = formViewModel.EmergencyContactName?.Trim();
            formViewModel.EmergencyContactRelationship = formViewModel.EmergencyContactRelationship?.Trim();
            formViewModel.EmergencyContactPhone = formViewModel.EmergencyContactPhone?.Trim();
            formViewModel.UserId = string.IsNullOrWhiteSpace(formViewModel.UserId) ? null : formViewModel.UserId.Trim();


            // --- Validation bổ sung phía server ---
            if (string.IsNullOrWhiteSpace(formViewModel.FullName))
                ModelState.AddModelError("FullName", "Họ tên là bắt buộc.");
            if (string.IsNullOrWhiteSpace(formViewModel.PhoneNumber)) // Bắt buộc SĐT
                ModelState.AddModelError("PhoneNumber", "Số điện thoại là bắt buộc.");
            if (!string.IsNullOrWhiteSpace(formViewModel.Email))
            {
                bool emailExists = await _db.Patients.AnyAsync(p => p.Id != formViewModel.Id && p.Email == formViewModel.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Địa chỉ email này đã được sử dụng.");
                }
            }
            if (!string.IsNullOrWhiteSpace(formViewModel.UserId))
            {
                bool userIdExists = await _db.Patients.AnyAsync(p => p.Id != formViewModel.Id && p.UserId == formViewModel.UserId);
                if (userIdExists)
                {
                    ModelState.AddModelError("UserId", "UserId này đã được liên kết với bệnh nhân khác.");
                }
                var identityUserExists = await UserManager.FindByIdAsync(formViewModel.UserId) != null;
                if (!identityUserExists)
                {
                    ModelState.AddModelError("UserId", "UserId không tồn tại trong hệ thống tài khoản.");
                }
            }

            // Validation cho mật khẩu mới (chỉ khi có nhập)
            bool isUpdatingPassword = !string.IsNullOrEmpty(formViewModel.NewPassword);
            if (isUpdatingPassword)
            {
                if (!hasAccount)
                {
                    ModelState.AddModelError("", "Bệnh nhân này không có tài khoản để đặt mật khẩu.");
                }
                else
                {
                    if (string.IsNullOrEmpty(formViewModel.ConfirmPassword))
                    {
                        ModelState.AddModelError("ConfirmPassword", "Vui lòng xác nhận mật khẩu mới.");
                    }
                    // Các validation khác (Compare, StringLength) đã được xử lý bởi DataAnnotations
                }
            }


            if (ModelState.IsValid)
            {
                var patientInDb = await _db.Patients.FindAsync(formViewModel.Id);
                if (patientInDb == null) return HttpNotFound();

                // Cập nhật thông tin bệnh nhân từ ViewModel
                patientInDb.FullName = formViewModel.FullName;
                patientInDb.Email = formViewModel.Email;
                patientInDb.PhoneNumber = formViewModel.PhoneNumber;
                patientInDb.DateOfBirth = formViewModel.DateOfBirth;
                patientInDb.Address = formViewModel.Address;
                patientInDb.Gender = formViewModel.Gender;
                patientInDb.NationalId = formViewModel.NationalId;
                patientInDb.BloodType = formViewModel.BloodType;
                patientInDb.MedicalHistory = formViewModel.MedicalHistory;
                patientInDb.Allergies = formViewModel.Allergies;
                patientInDb.EmergencyContactName = formViewModel.EmergencyContactName;
                patientInDb.EmergencyContactRelationship = formViewModel.EmergencyContactRelationship;
                patientInDb.EmergencyContactPhone = formViewModel.EmergencyContactPhone;
                patientInDb.UserId = formViewModel.UserId; // Cập nhật UserId
                patientInDb.UpdatedAt = DateTime.UtcNow;

                // Xử lý cập nhật mật khẩu nếu có
                if (isUpdatingPassword && hasAccount) // Chỉ cập nhật nếu có mk mới, có tài khoản, và ModelState hợp lệ cho phần thông tin
                {
                    if (string.IsNullOrEmpty(patientInDb.UserId))
                    {
                        TempData["err"] = "Lỗi: Không tìm thấy tài khoản liên kết để cập nhật mật khẩu.";
                        return RedirectToAction("Edit", new { id = formViewModel.Id });
                    }

                    var hasPassword = await UserManager.HasPasswordAsync(patientInDb.UserId);
                    IdentityResult passwordResult;

                    if (hasPassword)
                    {
                        passwordResult = await UserManager.RemovePasswordAsync(patientInDb.UserId);
                        if (passwordResult.Succeeded)
                        {
                            passwordResult = await UserManager.AddPasswordAsync(patientInDb.UserId, formViewModel.NewPassword);
                        }
                    }
                    else
                    {
                        passwordResult = await UserManager.AddPasswordAsync(patientInDb.UserId, formViewModel.NewPassword);
                    }

                    if (!passwordResult.Succeeded)
                    {
                        AddErrors(passwordResult);
                        // QUAN TRỌNG: Trả về View với lỗi mật khẩu, KHÔNG lưu thông tin patient
                        return View(formViewModel);
                    }
                    // Nếu thành công, mật khẩu đã được cập nhật
                }


                // Đánh dấu Patient là đã sửa
                _db.Entry(patientInDb).State = EntityState.Modified;
                await _db.SaveChangesAsync(); // Lưu cả thông tin patient và mật khẩu (nếu thành công)

                TempData["ok"] = "Đã cập nhật thông tin bệnh nhân." + (isUpdatingPassword ? " Mật khẩu cũng đã được cập nhật." : "");
                return RedirectToAction("Index");
            }

            // Trả về View Edit với ViewModel chứa lỗi (bao gồm cả lỗi mật khẩu nếu có)
            return View(formViewModel);
        }

        // GET: /Admin/Patients/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            ViewBag.Nav = "patients";
            if (id == null)
            {
                TempData["warn"] = "Thiếu mã bệnh nhân cần xoá.";
                return RedirectToAction("Index");
            }

            var patient = await _db.Patients.FindAsync(id);
            if (patient == null)
            {
                TempData["warn"] = "Không tìm thấy bệnh nhân.";
                return RedirectToAction("Index");
            }

            return View(patient);
        }


        // POST: /Admin/Patients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var patient = await _db.Patients.FindAsync(id);
            if (patient == null) return HttpNotFound();

            bool hasAppointments = await _db.Appointments.AnyAsync(a => a.PatientId == id);
            if (hasAppointments)
            {
                TempData["err"] = "Không thể xóa bệnh nhân này vì đã có lịch hẹn liên quan. Vui lòng xóa các lịch hẹn trước.";
                return RedirectToAction("Index");
            }

            _db.Patients.Remove(patient);
            await _db.SaveChangesAsync();

            TempData["ok"] = "Đã xoá bệnh nhân.";
            return RedirectToAction("Index");
        }


        // Giải phóng DbContext
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db?.Dispose(); // Thêm kiểm tra null
                                // *** SỬA: Dispose _userManager và _signInManager nếu chúng đã được khởi tạo ***
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }
                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
                // *** KẾT THÚC SỬA ***
            }
            base.Dispose(disposing);
        }

        // Helper thêm lỗi Identity vào ModelState
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }
    }
}

