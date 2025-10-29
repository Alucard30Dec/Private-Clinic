using Clinic.Models;
using System;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Collections.Generic; // For List

namespace Clinic.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class WorkShiftsController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // Helper tạo SelectList bác sĩ (có mục "Tất cả" cho Index)
        private async Task<SelectList> CreateDoctorListAsync(int? selectedDoctorId = null)
        {
            var doctors = await _db.Doctors.OrderBy(d => d.Name).ToListAsync();
            // Thêm mục "-- Tất cả bác sĩ --" cho trang Index
            var doctorListItems = new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Tất cả bác sĩ --" } };
            doctorListItems.AddRange(doctors.Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }));
            // Sử dụng selectedDoctorId?.ToString() ?? "" để xử lý null
            return new SelectList(doctorListItems, "Value", "Text", selectedDoctorId?.ToString() ?? "");
        }

        // Helper tạo SelectList bác sĩ (không có mục "Tất cả" cho form Create/Edit)
        private async Task<SelectList> CreateDoctorListForFormAsync(int? selectedDoctorId = null)
        {
            var doctors = await _db.Doctors.OrderBy(d => d.Name).ToListAsync();
            return new SelectList(doctors, "Id", "Name", selectedDoctorId);
        }

        // Helper tạo SelectList DayOfWeek (loại bỏ Chủ Nhật)
        private SelectList CreateDayOfWeekList(int? selectedValue = null)
        {
            // Lấy culture hiện tại để hiển thị tên Thứ
            var currentCulture = CultureInfo.CurrentCulture;
            return new SelectList(
                Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>()
                    .Where(d => d != DayOfWeek.Sunday) // Loại bỏ Chủ Nhật
                    .Select(d => new SelectListItem
                    {
                        Value = ((int)d).ToString(),
                        Text = currentCulture.DateTimeFormat.GetDayName(d) // Hiển thị tên Thứ theo culture
                    }),
                "Value", "Text", selectedValue
            );
        }

        // --- Logic Validation Thời gian làm việc ---
        private bool IsValidWorkTime(WorkingHour shift, out string errorMessage)
        {
            errorMessage = null;
            TimeSpan morningStart = TimeSpan.Parse("08:00");
            TimeSpan morningEnd = TimeSpan.Parse("11:30");
            TimeSpan afternoonStart = TimeSpan.Parse("13:00");
            TimeSpan afternoonEnd = TimeSpan.Parse("17:00");

            if (shift.Start >= shift.End)
            {
                errorMessage = "Giờ kết thúc phải sau giờ bắt đầu.";
                return false;
            }

            if (shift.DayOfWeek == DayOfWeek.Sunday)
            {
                errorMessage = "Không thể đăng ký làm việc vào Chủ nhật.";
                return false;
            }
            else if (shift.DayOfWeek == DayOfWeek.Saturday)
            {
                // Thứ 7: Chỉ trong khoảng [08:00, 11:30]
                if (!(shift.Start >= morningStart && shift.End <= morningEnd))
                {
                    errorMessage = "Thứ 7 chỉ làm việc từ 08:00 đến 11:30.";
                    return false;
                }
            }
            else // Thứ 2 đến Thứ 6
            {
                // Hoặc nằm hoàn toàn trong ca sáng HOẶC nằm hoàn toàn trong ca chiều
                bool inMorning = (shift.Start >= morningStart && shift.End <= morningEnd);
                bool inAfternoon = (shift.Start >= afternoonStart && shift.End <= afternoonEnd);
                if (!inMorning && !inAfternoon)
                {
                    errorMessage = "Giờ làm việc không hợp lệ. Chỉ chấp nhận ca sáng (08:00-11:30) hoặc chiều (13:00-17:00) các ngày trong tuần.";
                    return false;
                }
            }
            return true; // Hợp lệ
        }

        // --- Logic Kiểm tra Trùng lặp Ca làm ---
        private async Task<bool> IsOverlappingAsync(WorkingHour shift)
        {
            // Kiểm tra xem có ca làm nào khác của cùng bác sĩ, cùng ngày
            // mà thời gian bắt đầu/kết thúc bị chồng lấn không
            return await _db.WorkingHours
                .AnyAsync(wh => wh.Id != shift.Id // Loại trừ chính ca đang kiểm tra (quan trọng khi Edit)
                               && wh.DoctorId == shift.DoctorId
                               && wh.DayOfWeek == shift.DayOfWeek
                               && wh.Start < shift.End // Ca mới bắt đầu trước khi ca cũ kết thúc
                               && shift.Start < wh.End); // Ca mới kết thúc sau khi ca cũ bắt đầu
        }
        // --- Kết thúc Logic Validation ---


        // GET: Admin/WorkShifts
        public async Task<ActionResult> Index(int? doctorIdFilter = null)
        {
            ViewBag.Nav = "workshifts";
            ViewBag.DoctorList = await CreateDoctorListAsync(doctorIdFilter); // Dùng list có "-- Tất cả --"
            ViewBag.SelectedDoctorId = doctorIdFilter; // Truyền ID đã chọn sang View

            var query = _db.WorkingHours.Include(wh => wh.Doctor).AsQueryable();

            // Áp dụng bộ lọc nếu có
            if (doctorIdFilter.HasValue)
            {
                query = query.Where(wh => wh.DoctorId == doctorIdFilter.Value);
            }

            var list = await query
                .OrderBy(wh => wh.Doctor.Name) // Sắp xếp theo tên BS trước
                .ThenBy(wh => wh.DayOfWeek)
                .ThenBy(wh => wh.Start)
                .ToListAsync();

            return View(list); // Trả về View Areas/Admin/Views/WorkShifts/Index.cshtml
        }

        // GET: Admin/WorkShifts/Create
        public async Task<ActionResult> Create(int? doctorId = null) // Nhận doctorId từ nút "Thêm ca" nếu có
        {
            ViewBag.Nav = "workshifts";
            ViewBag.DoctorList = await CreateDoctorListForFormAsync(doctorId); // Dùng list cho form
            ViewBag.DayOfWeekList = CreateDayOfWeekList();

            var model = new WorkingHour
            {
                DoctorId = doctorId ?? 0, // Gán nếu được truyền, nếu không là 0 để yêu cầu chọn
                Start = TimeSpan.Parse("08:00"), // Giờ mặc định
                End = TimeSpan.Parse("11:30")
            };
            return View(model); // Trả về View Areas/Admin/Views/WorkShifts/Create.cshtml
        }

        // POST: Admin/WorkShifts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "DoctorId,DayOfWeek,Start,End")] WorkingHour workShift)
        {
            ViewBag.Nav = "workshifts";

            // --- Thực hiện Validation ---
            if (workShift.DoctorId == 0) // Bắt buộc chọn bác sĩ
            {
                ModelState.AddModelError("DoctorId", "Vui lòng chọn bác sĩ.");
            }
            if (!IsValidWorkTime(workShift, out string timeError))
            {
                // Phân biệt lỗi cho Start hay DayOfWeek dựa vào nội dung lỗi
                if (timeError != null && timeError.Contains("Chủ nhật"))
                    ModelState.AddModelError("DayOfWeek", timeError);
                else if (timeError != null) // Các lỗi giờ khác
                    ModelState.AddModelError("Start", timeError);
            }
            // Chỉ kiểm tra trùng lặp nếu DoctorId hợp lệ và thời gian hợp lệ
            if (workShift.DoctorId > 0 && timeError == null && await IsOverlappingAsync(workShift))
            {
                ModelState.AddModelError("", "Ca làm đăng ký bị trùng với ca đã có của bác sĩ này."); // Lỗi chung
            }
            // --- Kết thúc Validation ---

            if (ModelState.IsValid)
            {
                _db.WorkingHours.Add(workShift);
                await _db.SaveChangesAsync();
                TempData["ok"] = "Đã thêm ca làm việc mới.";
                // Chuyển về trang Index và lọc theo bác sĩ vừa thêm ca
                return RedirectToAction("Index", new { doctorIdFilter = workShift.DoctorId });
            }

            // Nếu ModelState không hợp lệ, tải lại SelectLists để hiển thị lại form
            ViewBag.DoctorList = await CreateDoctorListForFormAsync(workShift.DoctorId); // Giữ lại BS đã chọn
            ViewBag.DayOfWeekList = CreateDayOfWeekList((int?)workShift.DayOfWeek); // Giữ lại ngày đã chọn
            return View(workShift); // Trả về View Create với lỗi
        }

        // GET: Admin/WorkShifts/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            ViewBag.Nav = "workshifts";
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // Lấy ca làm cần sửa, include Doctor để hiển thị tên
            var workShift = await _db.WorkingHours.Include(wh => wh.Doctor).FirstOrDefaultAsync(wh => wh.Id == id);
            if (workShift == null) return HttpNotFound();

            // *** SỬA Ở ĐÂY: Tải lại danh sách bác sĩ cho ViewBag ***
            // Mặc dù sẽ bị disable ở view, nhưng partial view cần nó để không bị lỗi
            ViewBag.DoctorList = await CreateDoctorListForFormAsync(workShift.DoctorId);
            // *** KẾT THÚC SỬA ***

            ViewBag.DayOfWeekList = CreateDayOfWeekList((int)workShift.DayOfWeek); // Load list ngày và chọn ngày hiện tại
            return View(workShift); // Trả về View Areas/Admin/Views/WorkShifts/Edit.cshtml
        }

        // POST: Admin/WorkShifts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,DoctorId,DayOfWeek,Start,End")] WorkingHour workShiftFormInput) // Dùng tên khác để rõ ràng
        {
            ViewBag.Nav = "workshifts";

            // Lấy DoctorId thực tế từ DB để đảm bảo không bị sửa đổi qua form
            // và để dùng cho validation trùng lặp
            var existingShiftFromDb = await _db.WorkingHours.AsNoTracking().FirstOrDefaultAsync(wh => wh.Id == workShiftFormInput.Id);
            if (existingShiftFromDb == null) return HttpNotFound();
            // Gán DoctorId đúng vào đối tượng nhận từ form để kiểm tra trùng lặp chính xác
            workShiftFormInput.DoctorId = existingShiftFromDb.DoctorId;

            // --- Thực hiện Validation ---
            if (!IsValidWorkTime(workShiftFormInput, out string timeError))
            {
                if (timeError != null && timeError.Contains("Chủ nhật"))
                    ModelState.AddModelError("DayOfWeek", timeError);
                else if (timeError != null)
                    ModelState.AddModelError("Start", timeError);
            }
            // Kiểm tra trùng lặp (đã có DoctorId đúng)
            if (timeError == null && await IsOverlappingAsync(workShiftFormInput))
            {
                ModelState.AddModelError("", "Ca làm cập nhật bị trùng với ca đã có của bác sĩ này.");
            }
            // --- Kết thúc Validation ---

            if (ModelState.IsValid)
            {
                // Lấy lại đối tượng từ DB để cập nhật (để EF theo dõi thay đổi)
                var shiftToUpdate = await _db.WorkingHours.FindAsync(workShiftFormInput.Id);
                if (shiftToUpdate == null) return HttpNotFound(); // Kiểm tra lại

                // Chỉ cập nhật các trường được phép sửa
                shiftToUpdate.DayOfWeek = workShiftFormInput.DayOfWeek;
                shiftToUpdate.Start = workShiftFormInput.Start;
                shiftToUpdate.End = workShiftFormInput.End;
                // Không cập nhật shiftToUpdate.DoctorId

                _db.Entry(shiftToUpdate).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                TempData["ok"] = "Đã cập nhật ca làm việc.";
                // Chuyển về trang Index và lọc theo bác sĩ của ca vừa sửa
                return RedirectToAction("Index", new { doctorIdFilter = shiftToUpdate.DoctorId });
            }

            // Nếu ModelState không hợp lệ, tải lại DayOfWeekList và DoctorList
            ViewBag.DayOfWeekList = CreateDayOfWeekList((int?)workShiftFormInput.DayOfWeek); // Giữ lại ngày đã chọn
            // *** SỬA Ở ĐÂY: Tải lại DoctorList khi validation fail ***
            ViewBag.DoctorList = await CreateDoctorListForFormAsync(workShiftFormInput.DoctorId);
            // *** KẾT THÚC SỬA ***

            // Cần lấy lại thông tin Doctor để hiển thị tên trong form (vì AsNoTracking không load navigation property)
            // Hoặc có thể truyền Doctor từ action GET qua input hidden nếu muốn tối ưu
            var doctor = await _db.Doctors.FindAsync(workShiftFormInput.DoctorId);
            if (doctor != null) workShiftFormInput.Doctor = doctor; // Gán lại

            return View(workShiftFormInput); // Trả về View Edit với lỗi
        }


        // GET: Admin/WorkShifts/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            ViewBag.Nav = "workshifts";
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // Lấy ca làm và thông tin bác sĩ để hiển thị xác nhận
            var workShift = await _db.WorkingHours.Include(wh => wh.Doctor).FirstOrDefaultAsync(wh => wh.Id == id);
            if (workShift == null) return HttpNotFound();

            return View(workShift); // Trả về View Areas/Admin/Views/WorkShifts/Delete.cshtml
        }

        // POST: Admin/WorkShifts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id) // Nhận id từ route
        {
            var workShift = await _db.WorkingHours.FindAsync(id); // Tìm ca làm theo id
            if (workShift == null) return HttpNotFound();

            int? doctorIdFilter = workShift.DoctorId; // Lưu lại DoctorId để redirect về đúng bộ lọc

            _db.WorkingHours.Remove(workShift);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Đã xóa ca làm việc.";
            // Chuyển về trang Index và lọc theo bác sĩ của ca vừa xóa
            return RedirectToAction("Index", new { doctorIdFilter = doctorIdFilter });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
