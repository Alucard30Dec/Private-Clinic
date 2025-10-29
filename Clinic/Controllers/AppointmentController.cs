using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Clinic.Models;

namespace Clinic.Controllers
{
    public class AppointmentController : Controller
    {
        // Dùng DbContext domain của bạn
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: /Appointment
        [AllowAnonymous]
        public async Task<ActionResult> Index()
        {
            // Lấy danh sách chuyên khoa từ các bác sĩ
            var specialties = await _db.Doctors
                                        .Select(d => d.Specialty)
                                        .Where(s => s != null && s != "")
                                        .Distinct()
                                        .OrderBy(s => s)
                                        .ToListAsync();

            ViewBag.Specialties = new SelectList(specialties);

            return View(new AppointmentRequest());
        }

        // POST: /Appointment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<ActionResult> Create([Bind(Include = "Name,Email,Phone,Specialty,RequestedSlot,Message")] AppointmentRequest model)
        {
            if (model.RequestedSlot.Kind == DateTimeKind.Unspecified)
            {
                // Giả định thời gian người dùng nhập là giờ Local
                model.RequestedSlot = DateTime.SpecifyKind(model.RequestedSlot, DateTimeKind.Local);
            }

            // Chuyển sang UTC để lưu trữ
            model.RequestedSlot = model.RequestedSlot.ToUniversalTime();


            if (!ModelState.IsValid)
            {
                // Trả lại form kèm lỗi
                var specialties = await _db.Doctors
                                .Select(d => d.Specialty)
                                .Where(s => s != null && s != "")
                                .Distinct()
                                .OrderBy(s => s)
                                .ToListAsync();
                ViewBag.Specialties = new SelectList(specialties, model.Specialty);
                return View("Index", model);
            }

            // Bảo vệ: nếu chưa set thời điểm tạo thì set UTC
            if (model.CreatedAt == default(DateTime))
                model.CreatedAt = DateTime.UtcNow;

            _db.AppointmentRequests.Add(model);
            await _db.SaveChangesAsync();

            // Hiển thị lời cảm ơn NGAY TRÊN TRANG (không redirect layout khác)
            ModelState.Clear(); // xóa dữ liệu cũ trong form
            ViewBag.Success = "Cảm ơn bạn! Chúng tôi đã nhận yêu cầu đặt lịch. Lễ tân sẽ sớm liên hệ qua Email hoặc SĐT để xác nhận.";

            // Tải lại danh sách chuyên khoa cho form rỗng
            var specialtiesReload = await _db.Doctors
                                    .Select(d => d.Specialty)
                                    .Where(s => s != null && s != "")
                                    .Distinct()
                                    .OrderBy(s => s)
                                    .ToListAsync();
            ViewBag.Specialties = new SelectList(specialtiesReload);

            // Trả về Index với form rỗng + thông báo thành công
            return View("Index", new AppointmentRequest());
        }

        // API Endpoint cho JavaScript
        // GET: /Appointment/GetAvailableSlots?date=2025-10-30&specialty=Nhi
        [AllowAnonymous]
        [HttpGet]
        public async Task<JsonResult> GetAvailableSlots(DateTime date, string specialty)
        {
            const int slotDurationMinutes = 30; // 30 phút mỗi slot
            var availableSlots = new List<TimeSpan>();

            // 1. Lấy DayOfWeek (Chủ nhật = 0, Thứ 2 = 1, ...)
            var dayOfWeek = date.DayOfWeek;

            // 2. Chủ nhật không làm việc
            if (dayOfWeek == DayOfWeek.Sunday)
            {
                return Json(new { slots = availableSlots, message = "Chủ nhật phòng khám không làm việc." }, JsonRequestBehavior.AllowGet);
            }

            // Giờ hiện tại (Local)
            var now = DateTime.Now;

            // 3. Tìm các bác sĩ thuộc chuyên khoa
            var doctorIds = await _db.Doctors
                .Where(d => d.Specialty == specialty)
                .Select(d => d.Id)
                .ToListAsync();

            if (!doctorIds.Any())
            {
                return Json(new { slots = availableSlots, message = "Không tìm thấy bác sĩ cho chuyên khoa này." }, JsonRequestBehavior.AllowGet);
            }

            // 4. Lấy tất cả các khung giờ làm việc của các bác sĩ này VÀO NGÀY ĐÓ
            var workBlocks = await _db.WorkingHours
                .Where(wh => doctorIds.Contains(wh.DoctorId) && wh.DayOfWeek == dayOfWeek)
                .ToListAsync();

            if (!workBlocks.Any())
            {
                return Json(new { slots = availableSlots, message = "Không có lịch làm việc cho ngày này." }, JsonRequestBehavior.AllowGet);
            }

            // 5. Lấy tất cả các lịch hẹn (Appointments) đã có của các bác sĩ này VÀO NGÀY ĐÓ
            var dayStartLocal = date.Date;
            var dayEndLocal = dayStartLocal.AddDays(1);

            // Chuyển sang UTC để query CSDL (vì Appointment lưu giờ UTC)
            var dayStartUtc = dayStartLocal.ToUniversalTime();
            var dayEndUtc = dayEndLocal.ToUniversalTime();

            var existingAppts = await _db.Appointments
                .Where(a => doctorIds.Contains(a.DoctorId) &&
                            a.StartTime >= dayStartUtc && a.StartTime < dayEndUtc &&
                            a.Status != AppointmentStatus.Canceled)
                .ToListAsync();

            // 6. Tạo danh sách các slot tiềm năng
            // Tìm giờ bắt đầu sớm nhất và kết thúc muộn nhất
            var minStart = workBlocks.Min(wb => wb.Start);
            var maxEnd = workBlocks.Max(wb => wb.End);

            for (var slotTime = minStart; slotTime < maxEnd; slotTime = slotTime.Add(TimeSpan.FromMinutes(slotDurationMinutes)))
            {
                var slotStartLocal = date.Date.Add(slotTime);

                // Bỏ qua slot trong quá khứ
                if (slotStartLocal < now)
                {
                    continue;
                }

                var slotEndLocal = slotStartLocal.AddMinutes(slotDurationMinutes);

                // 7. Kiểm tra xem slot này có "khả dụng" không
                // "Khả dụng" = Có ít nhất 1 bác sĩ RẢNH vào giờ này.
                // "Rảnh" = Bác sĩ đó có LỊCH LÀM VIỆC (workBlocks) VÀ không có LỊCH HẸN (existingAppts)

                // Tìm các bác sĩ CÓ LỊCH LÀM VIỆC trong khung giờ này
                var workingDoctorIds = workBlocks
                    .Where(wb => slotTime >= wb.Start && slotEndLocal.TimeOfDay <= wb.End)
                    .Select(wb => wb.DoctorId)
                    .Distinct()
                    .ToList();

                if (!workingDoctorIds.Any())
                {
                    continue; // Không có bác sĩ nào làm việc giờ này
                }

                // Chuyển slot sang UTC để so sánh
                var slotStartUtc = slotStartLocal.ToUniversalTime();
                var slotEndUtc = slotEndLocal.ToUniversalTime();

                // Tìm các bác sĩ BẬN (đã có lịch hẹn) trong khung giờ này
                var bookedDoctorIds = existingAppts
                    .Where(a => a.StartTime < slotEndUtc && a.EndTime > slotStartUtc) // Logic kiểm tra overlap
                    .Select(a => a.DoctorId)
                    .ToHashSet(); // Dùng HashSet để kiểm tra nhanh

                // Kiểm tra xem có bác sĩ nào LÀM VIỆC mà KHÔNG BẬN không
                bool isSlotAvailable = workingDoctorIds.Any(docId => !bookedDoctorIds.Contains(docId));

                if (isSlotAvailable)
                {
                    availableSlots.Add(slotTime);
                }
            }

            var formattedSlots = availableSlots.Select(ts => ts.ToString(@"hh\:mm")).ToList();
            string successMessage = formattedSlots.Any() ?
                $"Tìm thấy {formattedSlots.Count} khung giờ khả dụng." :
                "Đã hết khung giờ khả dụng cho ngày này. Vui lòng chọn ngày khác.";

            return Json(new { slots = formattedSlots, message = successMessage }, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        public ActionResult SubmittedReception()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
