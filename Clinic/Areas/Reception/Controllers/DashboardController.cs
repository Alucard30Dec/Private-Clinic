using System.Threading.Tasks;
using System.Web.Mvc;
using Clinic.Models;
using System.Data.Entity;
using System;
using System.Linq;

namespace Clinic.Areas.Reception.Controllers
{
    [Authorize(Roles = "Receptionist")]
    public class DashboardController : Controller // *** Đảm bảo tên class là DashboardController ***
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: Reception/Dashboard/Index (Hoặc chỉ /Reception)
        public async Task<ActionResult> Index()
        {
            ViewBag.Nav = "dashboard"; // For layout menu highlighting
            ViewBag.Title = "Reception Dashboard";

            // Lấy số liệu thống kê nhanh
            var todayStartUtc = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Local).ToUniversalTime();
            var todayEndUtc = todayStartUtc.AddDays(1);

            // Sử dụng this._db cho nhất quán (mặc dù không bắt buộc ở đây)
            ViewBag.PendingRequests = await this._db.AppointmentRequests.CountAsync(r => !r.IsHandled);
            ViewBag.TodayAppointments = await this._db.Appointments.CountAsync(a => a.StartTime >= todayStartUtc && a.StartTime < todayEndUtc && a.Status != AppointmentStatus.Canceled);
            ViewBag.TotalPatients = await this._db.Patients.CountAsync(); // Tổng số bệnh nhân có hồ sơ

            // Lấy danh sách lịch hẹn hôm nay (gần giống action Index của ReceptionController cũ)
            var todayAppointmentsList = await this._db.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.Service)
                .Where(a => a.StartTime >= todayStartUtc && a.StartTime < todayEndUtc && a.Status != AppointmentStatus.Canceled)
                .OrderBy(a => a.StartTime)
                .ToListAsync();


            // *** THAY ĐỔI ĐƯỜNG DẪN VIEW ***
            return View("~/Areas/Reception/Views/Dashboard/Index.cshtml", todayAppointmentsList); // Truyền danh sách lịch hẹn vào View
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._db.Dispose(); // Sử dụng this._db
            }
            base.Dispose(disposing);
        }
    }
}

