using System.Linq;
using System.Web.Mvc;
using Clinic.Models;

namespace Clinic.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,Receptionist")]
    public class RequestsController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: /Admin/Requests
        public ActionResult Index()
        {
            var items = _db.AppointmentRequests
                           .OrderByDescending(x => x.CreatedAt)
                           .ToList();
            return View(items);
        }

        // Optional: small partial for pending-count badge in header
        [ChildActionOnly]
        public PartialViewResult _PendingBadge()
        {
            int pending = _db.AppointmentRequests.Count(x => !x.IsHandled);
            ViewBag.Pending = pending;
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkHandled(int id)
        {
            var item = _db.AppointmentRequests.Find(id);
            if (item == null) return HttpNotFound();
            item.IsHandled = true;
            _db.SaveChanges();
            TempData["ok"] = "Đã đánh dấu đã xử lý.";
            return RedirectToAction("Index");
        }
    }
}
