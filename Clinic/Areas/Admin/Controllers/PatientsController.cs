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
                q = q.Trim().ToLower(); // Chuyển sang chữ thường để tìm kiếm không phân biệt hoa thường
                patients = patients.Where(p =>
                    (p.FullName != null && p.FullName.ToLower().Contains(q)) ||
                    (p.Email != null && p.Email.ToLower().Contains(q)) ||
                    (p.PhoneNumber != null && p.PhoneNumber.Contains(q)) || // SĐT thường không cần ToLower
                    (p.Address != null && p.Address.ToLower().Contains(q)) ||
                    (p.Gender != null && p.Gender.ToLower().Contains(q)) // Thêm tìm kiếm theo giới tính
                 );
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
            // *** THÊM CÁC TRƯỜNG MỚI VÀO BIND ***
            "FullName,UserId,Email,PhoneNumber,DateOfBirth,Address," +
            "Gender,BloodType,MedicalHistory,Allergies,EmergencyContactName,EmergencyContactPhone"
            )] Patient p)
        {
            ViewBag.Nav = "patients";

            // Trim các trường string trước khi validate và lưu
            p.FullName = p.FullName?.Trim();
            p.UserId = string.IsNullOrWhiteSpace(p.UserId) ? null : p.UserId.Trim(); // UserId có thể null
            p.Email = string.IsNullOrWhiteSpace(p.Email) ? null : p.Email.Trim().ToLower();
            p.PhoneNumber = p.PhoneNumber?.Trim();
            p.Address = p.Address?.Trim();
            p.Gender = p.Gender?.Trim();
            p.BloodType = p.BloodType?.Trim();
            p.MedicalHistory = p.MedicalHistory?.Trim();
            p.Allergies = p.Allergies?.Trim();
            p.EmergencyContactName = p.EmergencyContactName?.Trim();
            p.EmergencyContactPhone = p.EmergencyContactPhone?.Trim();
            p.CreatedAt = System.DateTime.UtcNow; // Set thời gian tạo

            // Kiểm tra trùng Email (nếu có nhập)
            if (!string.IsNullOrEmpty(p.Email))
            {
                bool emailExists = await _db.Patients.AnyAsync(pa => pa.Email == p.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Địa chỉ email này đã được sử dụng.");
                }
            }
            // Kiểm tra trùng Số điện thoại (nếu có nhập và là bắt buộc)
            if (!string.IsNullOrEmpty(p.PhoneNumber))
            {
                bool phoneExists = await _db.Patients.AnyAsync(pa => pa.PhoneNumber == p.PhoneNumber);
                if (phoneExists)
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được sử dụng.");
                }
            }


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
            // *** THÊM CÁC TRƯỜNG MỚI VÀO BIND ***
            "Id,FullName,UserId,Email,PhoneNumber,DateOfBirth,Address," +
            "Gender,BloodType,MedicalHistory,Allergies,EmergencyContactName,EmergencyContactPhone"
            )] Patient input)
        {
            ViewBag.Nav = "patients";

            // Lấy bản ghi gốc từ DB
            var p = await _db.Patients.FindAsync(input.Id);
            if (p == null) return HttpNotFound();

            // Trim dữ liệu đầu vào
            input.FullName = input.FullName?.Trim();
            input.UserId = string.IsNullOrWhiteSpace(input.UserId) ? null : input.UserId.Trim();
            input.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim().ToLower();
            input.PhoneNumber = input.PhoneNumber?.Trim();
            input.Address = input.Address?.Trim();
            input.Gender = input.Gender?.Trim();
            input.BloodType = input.BloodType?.Trim();
            input.MedicalHistory = input.MedicalHistory?.Trim();
            input.Allergies = input.Allergies?.Trim();
            input.EmergencyContactName = input.EmergencyContactName?.Trim();
            input.EmergencyContactPhone = input.EmergencyContactPhone?.Trim();

            // Kiểm tra trùng Email (nếu thay đổi và có nhập)
            if (!string.IsNullOrEmpty(input.Email) && input.Email != p.Email)
            {
                bool emailExists = await _db.Patients.AnyAsync(pa => pa.Id != input.Id && pa.Email == input.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Địa chỉ email này đã được sử dụng.");
                }
            }
            // Kiểm tra trùng Số điện thoại (nếu thay đổi và có nhập)
            if (!string.IsNullOrEmpty(input.PhoneNumber) && input.PhoneNumber != p.PhoneNumber)
            {
                bool phoneExists = await _db.Patients.AnyAsync(pa => pa.Id != input.Id && pa.PhoneNumber == input.PhoneNumber);
                if (phoneExists)
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được sử dụng.");
                }
            }


            if (!ModelState.IsValid) return View(input); // Trả về view với lỗi

            // Cập nhật các trường cho bản ghi gốc
            p.FullName = input.FullName;
            p.UserId = input.UserId;
            p.Email = input.Email;
            p.PhoneNumber = input.PhoneNumber;
            p.DateOfBirth = input.DateOfBirth;
            p.Address = input.Address;
            p.Gender = input.Gender;
            p.BloodType = input.BloodType;
            p.MedicalHistory = input.MedicalHistory;
            p.Allergies = input.Allergies;
            p.EmergencyContactName = input.EmergencyContactName;
            p.EmergencyContactPhone = input.EmergencyContactPhone;
            p.UpdatedAt = System.DateTime.UtcNow; // Cập nhật thời gian sửa đổi

            // Đánh dấu là đã sửa đổi (không bắt buộc nếu bạn lấy trực tiếp từ context)
            // _db.Entry(p).State = EntityState.Modified;

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
