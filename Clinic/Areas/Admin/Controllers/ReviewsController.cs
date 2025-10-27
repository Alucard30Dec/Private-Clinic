using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Clinic.Models;

namespace Clinic.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,Receptionist")]
    public class ReviewsController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET: Admin/Reviews
        // MODIFIED: Added optional rating parameter
        public ActionResult Index(int? rating = null) // Accept rating filter (nullable int)
        {
            ViewBag.Nav = "reviews";
            ViewBag.Title = "Quản lý Đánh giá";
            ViewBag.SelectedRating = rating; // Pass selected rating to View

            // Start query
            var reviewsQuery = _db.AppointmentReviews
                .Include(r => r.Appointment.Patient)
                .Include(r => r.Appointment.Doctor)
                .AsQueryable(); // Use AsQueryable() to build the query

            // --- ADD RATING FILTER ---
            if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
            {
                reviewsQuery = reviewsQuery.Where(r => r.Rating == rating.Value);
                ViewBag.Title = $"Đánh giá {rating.Value} sao"; // Update title if filtered
            }
            // --- END RATING FILTER ---

            // Execute query and order
            var reviews = reviewsQuery
                .OrderByDescending(r => r.ReviewDate)
                .ToList();

            return View(reviews);
        }

        // POST: Admin/Reviews/ToggleApproval/5 (Keep as before)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult ToggleApproval(int id)
        {
            var review = _db.AppointmentReviews.Find(id);
            if (review != null)
            {
                review.IsApproved = !review.IsApproved;
                _db.SaveChanges();
                TempData["ok"] = review.IsApproved ? "Đã duyệt đánh giá." : "Đã bỏ duyệt đánh giá.";
            }
            else { TempData["err"] = "Không tìm thấy đánh giá."; }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _db.Dispose(); }
            base.Dispose(disposing);
        }
    }
}