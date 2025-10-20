using System.Linq;
using System.Web.Mvc;
using Clinic.Models;
using Microsoft.AspNet.Identity;

namespace Clinic.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // GET /Patient/Complete?returnUrl=/Bookings/Create?doctorId=5
        public ActionResult Complete(string returnUrl)
        {
            if (TempData["warn"] != null) ViewBag.Warn = TempData["warn"];

            var uid = User.Identity.GetUserId();
            var profile = _db.Patients.FirstOrDefault(p => p.UserId == uid);
            if (profile == null) return HttpNotFound();

            ViewBag.ReturnUrl = returnUrl; // để render hidden field
            return View(profile);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Complete(
            [Bind(Include = "Id,FullName,PhoneNumber,DateOfBirth,Address")] Patient form,
            string returnUrl)
        {
            var uid = User.Identity.GetUserId();
            var p = _db.Patients.FirstOrDefault(x => x.Id == form.Id && x.UserId == uid);
            if (p == null) return HttpNotFound();

            p.FullName = form.FullName;
            p.PhoneNumber = form.PhoneNumber;
            p.DateOfBirth = form.DateOfBirth;
            p.Address = form.Address;
            _db.SaveChanges();

            TempData["ok"] = "Đã cập nhật hồ sơ bệnh nhân.";

            if (!string.IsNullOrWhiteSpace(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Doctors");
        }
    }
}
