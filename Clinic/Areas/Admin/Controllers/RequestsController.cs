using System.Linq;
using System.Web.Mvc;
using Clinic.Models;
using System.Data.Entity; // Thêm để dùng DbContext

namespace Clinic.Areas.Admin.Controllers
{
    // Chỉ Admin hoặc Lễ tân xem được
    [Authorize(Roles = "Admin,Receptionist")]
    public class RequestsController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: /Admin/Requests
        // Hiển thị danh sách đơn đăng ký, mới nhất lên đầu
        public ActionResult Index()
        {
            ViewBag.Nav = "requests"; // Thêm để highlight menu sidebar
            var items = _db.AppointmentRequests
                           .OrderByDescending(x => x.CreatedAt)
                           .ToList();
            return View(items); // Trả về Views/Requests/Index.cshtml
        }

        // Partial view để đếm số đơn chờ xử lý (hiển thị badge)
        [ChildActionOnly]
        public PartialViewResult _PendingBadge()
        {
            int pending = _db.AppointmentRequests.Count(x => !x.IsHandled);
            ViewBag.Pending = pending;
            return PartialView();
        }

        // POST: /Admin/Requests/MarkHandled/{id}
        // Action này chỉ để đánh dấu thủ công (nếu cần),
        // việc đặt lịch thực tế sẽ tự động đánh dấu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkHandled(int id)
        {
            var item = _db.AppointmentRequests.Find(id);
            if (item == null)
            {
                TempData["err"] = "Không tìm thấy đơn đăng ký.";
                return RedirectToAction("Index");
            }
            item.IsHandled = true;
            _db.SaveChanges();
            TempData["ok"] = "Đã đánh dấu đã xử lý.";
            return RedirectToAction("Index"); // Về trang danh sách
        }

        // Thêm Dispose để giải phóng DbContext
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