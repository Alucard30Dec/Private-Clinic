using System.Linq;
using System.Web.Mvc;
using Clinic.Models;
using System.Data.Entity;
using System.Threading.Tasks; // Thêm Task

// *** THAY ĐỔI NAMESPACE ***
namespace Clinic.Areas.Reception.Controllers
{
    // Chỉ Lễ tân xem được (Admin có thể truy cập qua URL trực tiếp nếu cần)
    [Authorize(Roles = "Receptionist")]
    public class RequestsController : Controller
    {
        // Sử dụng this._db để tham chiếu rõ ràng đến biến thành viên của class
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: /Reception/Requests
        public async Task<ActionResult> Index()
        {
            ViewBag.Nav = "requests"; // Key cho menu layout Reception
            var items = await this._db.AppointmentRequests // Sử dụng this._db
                           .OrderByDescending(x => x.CreatedAt)
                           .ToListAsync();
            // *** THAY ĐỔI ĐƯỜNG DẪN VIEW ***
            return View("~/Areas/Reception/Views/Requests/Index.cshtml", items);
        }

        // Partial view để đếm số đơn chờ xử lý (hiển thị badge trong layout Reception)
        [ChildActionOnly]
        public PartialViewResult _PendingBadge()
        {
            // Nên dùng async ở đây nếu có thể, nhưng ChildActionOnly không hỗ trợ trực tiếp
            // Tạm thời giữ lại non-async để đơn giản
            int pending = this._db.AppointmentRequests.Count(x => !x.IsHandled); // Sử dụng this._db
            ViewBag.Pending = pending;
            // *** THAY ĐỔI ĐƯỜNG DẪN VIEW ***
            return PartialView("~/Areas/Reception/Views/Requests/_PendingBadge.cshtml");
        }

        // POST: /Reception/Requests/MarkHandled/{id}
        // Action này chỉ để đánh dấu thủ công (nếu cần)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> MarkHandled(int id)
        {
            var item = await this._db.AppointmentRequests.FindAsync(id); // Sử dụng this._db
            if (item == null)
            {
                TempData["err"] = "Không tìm thấy đơn đăng ký.";
                // *** THAY ĐỔI REDIRECT ***
                return RedirectToAction("Index", "Requests", new { area = "Reception" });
            }
            item.IsHandled = true;
            await this._db.SaveChangesAsync(); // Sử dụng this._db
            TempData["ok"] = "Đã đánh dấu đã xử lý.";
            // *** THAY ĐỔI REDIRECT ***
            return RedirectToAction("Index", "Requests", new { area = "Reception" });
        }

        // POST: /Reception/Requests/Delete/{id} (Thêm chức năng xóa)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var item = await this._db.AppointmentRequests.FindAsync(id); // Sử dụng this._db
            if (item == null)
            {
                TempData["err"] = "Không tìm thấy đơn đăng ký.";
            }
            else
            {
                this._db.AppointmentRequests.Remove(item); // Sử dụng this._db
                await this._db.SaveChangesAsync(); // Sử dụng this._db
                TempData["ok"] = "Đã xóa đơn đăng ký.";
            }
            // *** THAY ĐỔI REDIRECT ***
            return RedirectToAction("Index", "Requests", new { area = "Reception" });
        }


        // Thêm Dispose để giải phóng DbContext
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

