using Clinic.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Globalization;

namespace Clinic.Areas.Doctor.Controllers
{
    // ViewModel để hiển thị form và danh sách
    public class DoctorWorkShiftViewModel
    {
        public List<WorkingHour> ExistingShifts { get; set; } = new List<WorkingHour>();
        public WorkingHour NewShift { get; set; } = new WorkingHour();
        public SelectList DayOfWeekList { get; set; }
    }

    [Authorize(Roles = "Doctor")]
    public class WorkShiftsController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // Hàm helper lấy DoctorId từ UserId hiện tại
        private async Task<int?> GetCurrentDoctorIdAsync()
        {
            var userId = User.Identity.GetUserId();
            var doctor = await this._db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            return doctor?.Id;
        }

        // Tạo SelectList cho DayOfWeek (loại bỏ Chủ Nhật)
        private SelectList CreateDayOfWeekList(int? selectedValue = null)
        {
            return new SelectList(
                Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>()
                    .Where(d => d != DayOfWeek.Sunday) // *** LOẠI BỎ CHỦ NHẬT ***
                    .Select(d => new SelectListItem
                    {
                        Value = ((int)d).ToString(),
                        Text = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(d) // Hiển thị tên tiếng Việt
                                                                                       //Text = d.ToString() // Hoặc giữ tên tiếng Anh nếu muốn
                    }),
                "Value", "Text", selectedValue
            );
        }


        // GET: Doctor/WorkShifts
        public async Task<ActionResult> Index()
        {
            ViewBag.Nav = "workshifts";
            ViewBag.Title = "Đăng ký ca làm";

            var doctorId = await GetCurrentDoctorIdAsync();
            if (doctorId == null)
            {
                TempData["error"] = "Không tìm thấy thông tin bác sĩ.";
                return RedirectToAction("Index", "Home", new { area = "Doctor" });
            }

            var existingShifts = await this._db.WorkingHours
                .Where(wh => wh.DoctorId == doctorId.Value)
                .OrderBy(wh => wh.DayOfWeek)
                .ThenBy(wh => wh.Start)
                .ToListAsync();

            var viewModel = new DoctorWorkShiftViewModel
            {
                ExistingShifts = existingShifts,
                DayOfWeekList = CreateDayOfWeekList() // Sử dụng helper mới
            };

            return View(viewModel);
        }

        // POST: Doctor/WorkShifts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(DoctorWorkShiftViewModel viewModel)
        {
            ViewBag.Nav = "workshifts";
            ViewBag.Title = "Đăng ký ca làm";

            var doctorId = await GetCurrentDoctorIdAsync();
            if (doctorId == null)
            {
                TempData["error"] = "Không tìm thấy thông tin bác sĩ.";
                return RedirectToAction("Index", "Home", new { area = "Doctor" });
            }

            var newShift = viewModel.NewShift;
            newShift.DoctorId = doctorId.Value;

            // --- VALIDATION THỜI GIAN LÀM VIỆC (CẬP NHẬT LOGIC) ---
            bool isValidTime = true; // Giả định là hợp lệ ban đầu
            string timeErrorMsg = null;
            TimeSpan morningStart = TimeSpan.Parse("08:00");
            TimeSpan morningEnd = TimeSpan.Parse("11:30");
            TimeSpan afternoonStart = TimeSpan.Parse("13:00");
            TimeSpan afternoonEnd = TimeSpan.Parse("17:00");

            // 1. Kiểm tra Start < End
            if (newShift.Start >= newShift.End)
            {
                ModelState.AddModelError("NewShift.End", "Giờ kết thúc phải sau giờ bắt đầu.");
                isValidTime = false;
            }

            // 2. Kiểm tra theo ngày
            if (isValidTime) // Chỉ kiểm tra tiếp nếu Start < End
            {
                if (newShift.DayOfWeek == DayOfWeek.Sunday)
                {
                    ModelState.AddModelError("NewShift.DayOfWeek", "Không thể đăng ký làm việc vào Chủ nhật.");
                    isValidTime = false;
                }
                else if (newShift.DayOfWeek == DayOfWeek.Saturday)
                {
                    // Thứ 7: Chỉ trong khoảng [08:00, 11:30]
                    if (!(newShift.Start >= morningStart && newShift.End <= morningEnd))
                    {
                        timeErrorMsg = "Thứ 7 chỉ làm việc từ 08:00 đến 11:30.";
                        isValidTime = false;
                    }
                }
                else // Thứ 2 đến Thứ 6
                {
                    // Hoặc nằm hoàn toàn trong ca sáng HOẶC nằm hoàn toàn trong ca chiều
                    bool inMorning = (newShift.Start >= morningStart && newShift.End <= morningEnd);
                    bool inAfternoon = (newShift.Start >= afternoonStart && newShift.End <= afternoonEnd);

                    if (!inMorning && !inAfternoon)
                    {
                        timeErrorMsg = "Giờ làm việc không hợp lệ. Chỉ chấp nhận ca sáng (08:00-11:30) hoặc chiều (13:00-17:00) các ngày trong tuần.";
                        isValidTime = false;
                    }
                }

                // Nếu có lỗi về khoảng thời gian, thêm vào ModelState
                if (!isValidTime && timeErrorMsg != null)
                {
                    ModelState.AddModelError("NewShift.Start", timeErrorMsg);
                    // Không cần add cho End nữa vì lỗi chung cho cả khoảng thời gian
                }
            }
            // --- KẾT THÚC VALIDATION ---

            // Kiểm tra trùng lặp ca làm (giữ nguyên)
            bool isOverlapping = false;
            if (ModelState.IsValid) // Chỉ kiểm tra trùng nếu các validation cơ bản đã qua
            {
                isOverlapping = await this._db.WorkingHours
                   .AnyAsync(wh => wh.DoctorId == doctorId.Value
                                  && wh.DayOfWeek == newShift.DayOfWeek
                                  && wh.Start < newShift.End // Ca mới bắt đầu trước khi ca cũ kết thúc
                                  && newShift.Start < wh.End); // Ca mới kết thúc sau khi ca cũ bắt đầu

                if (isOverlapping)
                {
                    ModelState.AddModelError("", "Ca làm đăng ký bị trùng với ca đã có.");
                }
            }


            if (ModelState.IsValid) // ModelState.IsValid đã bao gồm các lỗi AddModelError ở trên
            {
                this._db.WorkingHours.Add(newShift);
                await this._db.SaveChangesAsync();
                TempData["ok"] = "Đã thêm ca làm mới.";
                return RedirectToAction("Index");
            }

            // Nếu lỗi, tải lại danh sách ca làm hiện có và SelectList để hiển thị lại form
            viewModel.ExistingShifts = await this._db.WorkingHours
                .Where(wh => wh.DoctorId == doctorId.Value)
                .OrderBy(wh => wh.DayOfWeek)
                .ThenBy(wh => wh.Start)
                .ToListAsync();

            // Sử dụng helper và giữ lại ngày đã chọn
            viewModel.DayOfWeekList = CreateDayOfWeekList((int)newShift.DayOfWeek);

            return View("Index", viewModel); // Hiển thị lại View Index với lỗi
        }

        // POST: Doctor/WorkShifts/Delete/5 (Giữ nguyên)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (doctorId == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden, "Không tìm thấy thông tin bác sĩ.");
            }

            var shiftToDelete = await this._db.WorkingHours.FirstOrDefaultAsync(wh => wh.Id == id && wh.DoctorId == doctorId.Value);

            if (shiftToDelete == null)
            {
                TempData["error"] = "Không tìm thấy ca làm hoặc bạn không có quyền xóa.";
                return RedirectToAction("Index");
            }

            this._db.WorkingHours.Remove(shiftToDelete);
            await this._db.SaveChangesAsync();
            TempData["ok"] = "Đã xóa ca làm.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

