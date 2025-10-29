using Clinic.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web;
using Microsoft.Owin.Security;

namespace Clinic.Controllers
{
    // Đổi tên Controller cho nhất quán (PatientProfileController -> PatientController)
    public class PatientController : Controller
    {
        private readonly ClinicDbContext _db = new ClinicDbContext();

        // Helpers để lấy UserManager và SignInManager (giữ nguyên)
        private ApplicationSignInManager SignInManager => HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
        private ApplicationUserManager UserManager => HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
        private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;

        // GET: /Patient/CompleteRequired
        // Logic xác định xem nên hiển thị form tạo mới hay chuyển hướng đến form sửa (giữ nguyên logic)
        [HttpGet]
        public async Task<ActionResult> CompleteRequired()
        {
            ViewBag.Title = "Hoàn thiện Hồ sơ Bệnh nhân";
            ViewBag.Layout = "~/Views/Shared/_Layout.cshtml"; // Đảm bảo dùng layout public

            // Ưu tiên 1: Quy trình đăng ký mới (từ AccountController)
            if (TempData["IsNewRegistrationProcess"] as bool? == true)
            {
                TempData.Keep(); // Giữ lại TempData để action POST xử lý
                var model = new Patient();
                bool isExternal = false;

                // Lấy thông tin từ đăng ký bằng mật khẩu
                if (TempData["PendingRegEmail"] is string email && TempData["PendingRegHashedPassword"] is string hashedPassword)
                {
                    model.Email = email; // Gán Email cho model
                }
                // Lấy thông tin từ đăng ký bằng Google/External
                else if (TempData["PendingExternalLoginInfo"] is ExternalLoginInfo loginInfo && TempData["PendingExternalEmail"] is string externalEmail)
                {
                    model.Email = externalEmail; // Gán Email
                    model.FullName = TempData["PendingExternalName"] as string; // Gán tên gợi ý
                    isExternal = true;
                }
                // Nếu không có thông tin hợp lệ -> Lỗi
                else
                {
                    TempData.Clear(); // Xóa TempData lỗi
                    TempData["err"] = "Phiên đăng ký không hợp lệ hoặc đã hết hạn. Vui lòng thử lại.";
                    return RedirectToAction("Register", "Account");
                }

                // Đặt ViewBag để View biết cách hiển thị
                ViewBag.IsNewRegistration = true;
                ViewBag.IsExternalRegistration = isExternal;
                // Trả về View "Complete" với model rỗng (hoặc có sẵn Email/Tên)
                return View("Complete", model);
            }

            // Ưu tiên 2: Bắt buộc hoàn thiện sau đăng nhập (tài khoản đã có nhưng thiếu hồ sơ Patient)
            if (TempData["ForceCompleteUserId"] is string forceUserId)
            {
                TempData.Keep(); // Giữ lại để POST xử lý
                var model = new Patient
                {
                    UserId = forceUserId, // Gán UserId
                    Email = TempData["ForceCompleteEmail"] as string, // Gán Email (nếu có)
                    FullName = TempData["ForceCompleteName"] as string // Gán Tên (nếu có)
                };
                ViewBag.IsNewRegistration = false; // Không phải quy trình đăng ký mới từ đầu
                ViewBag.IsForcedCompletion = true; // Đánh dấu là bắt buộc hoàn thiện
                return View("Complete", model);
            }

            // Ưu tiên 3: Đã đăng nhập và là Patient -> chuyển sang trang chỉnh sửa hồ sơ hiện có
            if (User.Identity.IsAuthenticated)
            {
                var uid = User.Identity.GetUserId();
                // Kiểm tra xem đã có hồ sơ Patient chưa
                var profile = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == uid);

                // Nếu chưa có hồ sơ (dù đã đăng nhập) -> Đăng xuất và bắt hoàn thiện
                if (profile == null)
                {
                    AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie); // Đăng xuất
                    TempData["err"] = "Hồ sơ của bạn chưa được tạo. Vui lòng đăng nhập lại để hoàn tất.";
                    return RedirectToAction("Login", "Account");
                }
                // Nếu đã có hồ sơ -> Chuyển đến action Complete (GET) để chỉnh sửa
                return RedirectToAction("Complete"); // Gọi action Complete (GET) bên dưới
            }

            // Mặc định: Chưa đăng nhập -> Về trang đăng nhập
            TempData["err"] = "Vui lòng đăng nhập hoặc đăng ký để tiếp tục.";
            return RedirectToAction("Login", "Account");
        }


        // GET: /Patient/Complete (để chỉnh sửa hồ sơ hiện có)
        [Authorize(Roles = "Patient")] // Chỉ Patient mới vào được action này
        public async Task<ActionResult> Complete(string returnUrl)
        {
            ViewBag.Title = "Chỉnh sửa Hồ sơ Bệnh nhân";
            ViewBag.Layout = "~/Views/Shared/_Layout.cshtml"; // Layout public
            if (TempData["warn"] != null) ViewBag.Warn = TempData["warn"];

            var uid = User.Identity.GetUserId();
            var profile = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == uid);

            // Nếu không tìm thấy hồ sơ (dù đã đăng nhập với role Patient) -> Lỗi, đăng xuất
            if (profile == null)
            {
                AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                TempData["err"] = "Không tìm thấy hồ sơ của bạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.IsNewRegistration = false; // Đánh dấu là đang chỉnh sửa
            return View(profile); // Trả về View "Complete" với dữ liệu hồ sơ hiện tại
        }


        // POST: /Patient/Complete (Xử lý cả tạo mới và cập nhật)
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Bỏ Authorize vì action này dùng cho cả lúc chưa đăng nhập (đang đăng ký)
        // [Authorize]
        public async Task<ActionResult> Complete(
            // *** CẬP NHẬT [Bind] ĐỂ BAO GỒM CÁC TRƯỜNG MỚI ***
            [Bind(Include = "FullName,PhoneNumber,DateOfBirth,Address,Email," + // Trường cũ
                           "Gender, NationalId, " + // Trường mới
                           "EmergencyContactName, EmergencyContactRelationship, EmergencyContactPhone")] Patient form, // Trường liên hệ khẩn cấp
            string returnUrl)
        {
            // Thiết lập lại ViewBag cho trường hợp validation fail và trả về View
            ViewBag.Layout = "~/Views/Shared/_Layout.cshtml";
            bool isNewRegistrationProcess = TempData["IsNewRegistrationProcess"] as bool? ?? false;
            bool isForcedCompletion = TempData["ForceCompleteUserId"] != null; // Kiểm tra xem có phải bắt buộc hoàn thiện không
            ViewBag.IsNewRegistration = isNewRegistrationProcess;
            ViewBag.IsForcedCompletion = isForcedCompletion;

            // Trim dữ liệu string để tránh lỗi thừa khoảng trắng
            form.FullName = form.FullName?.Trim();
            form.PhoneNumber = form.PhoneNumber?.Trim();
            form.Address = form.Address?.Trim();
            form.Gender = form.Gender?.Trim();
            form.NationalId = form.NationalId?.Trim(); // Trim CMND/CCCD
            form.EmergencyContactName = form.EmergencyContactName?.Trim();
            form.EmergencyContactRelationship = form.EmergencyContactRelationship?.Trim(); // Trim quan hệ
            form.EmergencyContactPhone = form.EmergencyContactPhone?.Trim();

            // --- Validation cơ bản (có thể thêm các rule phức tạp hơn) ---
            if (string.IsNullOrWhiteSpace(form.FullName))
                ModelState.AddModelError("FullName", "Họ tên là bắt buộc.");
            if (string.IsNullOrWhiteSpace(form.PhoneNumber))
                ModelState.AddModelError("PhoneNumber", "Số điện thoại là bắt buộc để phòng khám liên hệ.");
            // Kiểm tra NationalId nếu nhập phải là số (Regex đã làm ở Model, nhưng check lại nếu cần)
            if (!string.IsNullOrWhiteSpace(form.NationalId) && !System.Text.RegularExpressions.Regex.IsMatch(form.NationalId, @"^\d+$"))
            {
                ModelState.AddModelError("NationalId", "Số CMND/CCCD chỉ được chứa chữ số.");
            }
            // Thêm các validation khác nếu cần...

            // Nếu validation thất bại
            if (!ModelState.IsValid)
            {
                TempData.Keep(); // Giữ lại TempData để View biết trạng thái
                // Nếu là đăng ký mới, cần gán lại Email từ TempData vào form để hiển thị lại
                if (isNewRegistrationProcess)
                    form.Email = TempData.Peek("PendingRegEmail") as string ?? TempData.Peek("PendingExternalEmail") as string;
                // Trả về View "Complete" với lỗi validation
                return View(form);
            }

            // --- XỬ LÝ QUY TRÌNH ĐĂNG KÝ MỚI (Tạo cả User Identity và Patient Profile) ---
            if (isNewRegistrationProcess)
            {
                ApplicationUser user = null;
                IdentityResult result = null;
                ExternalLoginInfo loginInfo = TempData["PendingExternalLoginInfo"] as ExternalLoginInfo;
                string email = null; // Khai báo email ở đây để dùng chung

                // A. Tạo User (Identity) dựa trên thông tin từ TempData
                // Trường hợp đăng ký bằng email/password
                if (TempData["PendingRegEmail"] is string regEmail && TempData["PendingRegHashedPassword"] is string hashedPassword)
                {
                    email = regEmail; // Gán email
                    // Kiểm tra lại Email một lần nữa phòng trường hợp người khác đăng ký trong lúc nhập form
                    var existingUser = await UserManager.FindByEmailAsync(email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Email", "Địa chỉ email này đã được sử dụng trong lúc bạn nhập thông tin. Vui lòng thử lại.");
                        TempData.Keep(); // Giữ TempData
                        form.Email = email; // Gán lại email vào form
                        return View(form); // Trả về view với lỗi
                    }
                    // Tạo user mới
                    user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, PasswordHash = hashedPassword };
                    result = await UserManager.CreateAsync(user);
                }
                // Trường hợp đăng ký bằng Google/External
                else if (loginInfo != null && TempData["PendingExternalEmail"] is string externalEmail)
                {
                    email = externalEmail; // Gán email
                    // Kiểm tra lại Email
                    var existingUser = await UserManager.FindByEmailAsync(email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Email", "Địa chỉ email này đã được sử dụng trong lúc bạn nhập thông tin. Vui lòng thử lại.");
                        TempData.Keep();
                        form.Email = email;
                        return View(form);
                    }
                    // Tạo user mới
                    user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                    result = await UserManager.CreateAsync(user);
                    // Nếu tạo user thành công, thêm liên kết External Login
                    if (result.Succeeded)
                        result = await UserManager.AddLoginAsync(user.Id, loginInfo.Login);
                }
                // Nếu TempData không hợp lệ
                else
                {
                    TempData.Clear();
                    TempData["err"] = "Phiên đăng ký không hợp lệ hoặc đã hết hạn. Vui lòng thử lại.";
                    return RedirectToAction("Register", "Account");
                }

                // B. Xử lý kết quả tạo User Identity
                if (result.Succeeded)
                {
                    // Gán vai trò "Patient" cho user mới
                    await UserManager.AddToRoleAsync(user.Id, "Patient");

                    // C. Tạo hồ sơ Patient trong ClinicDbContext (bao gồm các trường mới)
                    var patientProfile = new Patient
                    {
                        UserId = user.Id, // Liên kết với Identity User
                        FullName = form.FullName,
                        Email = user.Email, // Lấy Email từ Identity User đã tạo
                        PhoneNumber = form.PhoneNumber,
                        DateOfBirth = form.DateOfBirth,
                        Address = form.Address,
                        Gender = form.Gender,
                        NationalId = form.NationalId, // Lưu CMND/CCCD
                        EmergencyContactName = form.EmergencyContactName,
                        EmergencyContactRelationship = form.EmergencyContactRelationship, // Lưu quan hệ
                        EmergencyContactPhone = form.EmergencyContactPhone,
                        CreatedAt = DateTime.UtcNow // Thời gian tạo hồ sơ
                    };
                    _db.Patients.Add(patientProfile);
                    await _db.SaveChangesAsync(); // Lưu hồ sơ Patient

                    // D. Đăng nhập user mới tạo và chuyển hướng
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false); // Đăng nhập
                    TempData.Clear(); // Xóa TempData đăng ký
                    TempData["ok"] = "Đăng ký và hoàn thiện hồ sơ thành công! Chào mừng bạn.";

                    // Chuyển hướng đến returnUrl (nếu có) hoặc trang mặc định (vd: danh sách bác sĩ)
                    if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    return RedirectToAction("Index", "Doctors"); // Hoặc trang chào mừng bệnh nhân
                }
                // Nếu tạo User Identity thất bại
                else
                {
                    AddErrors(result); // Thêm lỗi vào ModelState
                    TempData.Keep(); // Giữ TempData
                    form.Email = email; // Điền lại email vào form
                    return View(form); // Trả về view với lỗi
                }
            }
            // --- XỬ LÝ CẬP NHẬT HỒ SƠ (Khi chỉnh sửa hoặc bắt buộc hoàn thiện sau đăng nhập) ---
            else
            {
                string userIdToUpdate = isForcedCompletion ? TempData["ForceCompleteUserId"] as string : User.Identity.GetUserId();
                if (string.IsNullOrEmpty(userIdToUpdate))
                {
                    TempData.Clear();
                    TempData["err"] = "Phiên làm việc không hợp lệ.";
                    // Nếu đang bắt buộc hoàn thiện mà mất session -> về Login
                    // Nếu đang chỉnh sửa mà mất session -> về Login (Authorize sẽ xử lý)
                    return RedirectToAction("Login", "Account");
                }

                // Tìm hồ sơ Patient hiện có bằng UserId
                var profile = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == userIdToUpdate);

                // Trường hợp 1: Bắt buộc hoàn thiện và CHƯA có hồ sơ -> Tạo mới hồ sơ
                if (profile == null && isForcedCompletion)
                {
                    profile = new Patient
                    {
                        UserId = userIdToUpdate,
                        Email = TempData["ForceCompleteEmail"] as string, // Lấy email từ TempData
                        FullName = form.FullName,
                        PhoneNumber = form.PhoneNumber,
                        DateOfBirth = form.DateOfBirth,
                        Address = form.Address,
                        Gender = form.Gender,
                        NationalId = form.NationalId, // Lưu trường mới
                        EmergencyContactName = form.EmergencyContactName,
                        EmergencyContactRelationship = form.EmergencyContactRelationship, // Lưu trường mới
                        EmergencyContactPhone = form.EmergencyContactPhone,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Patients.Add(profile);
                    await _db.SaveChangesAsync(); // Lưu hồ sơ mới
                    TempData.Clear(); // Xóa TempData bắt buộc
                    TempData["ok"] = "Hoàn thiện hồ sơ thành công.";

                    // Đăng nhập lại (phòng trường hợp session bị thay đổi)
                    var user = await UserManager.FindByIdAsync(userIdToUpdate);
                    if (user != null) await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                    // Chuyển hướng
                    if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    return RedirectToAction("Index", "Doctors"); // Hoặc trang hồ sơ bệnh nhân
                }
                // Trường hợp 2: Chỉnh sửa thông thường (ĐÃ có hồ sơ và User hiện tại khớp)
                // Hoặc trường hợp bắt buộc hoàn thiện nhưng hồ sơ đã tồn tại (hiếm gặp, nhưng xử lý như cập nhật)
                else if (profile != null && (isForcedCompletion || profile.UserId == User.Identity.GetUserId()))
                {
                    // Cập nhật các trường từ form (bao gồm cả trường mới)
                    profile.FullName = form.FullName;
                    profile.PhoneNumber = form.PhoneNumber;
                    profile.DateOfBirth = form.DateOfBirth;
                    profile.Address = form.Address;
                    profile.Gender = form.Gender;
                    profile.NationalId = form.NationalId; // Cập nhật trường mới
                    profile.EmergencyContactName = form.EmergencyContactName;
                    profile.EmergencyContactRelationship = form.EmergencyContactRelationship; // Cập nhật trường mới
                    profile.EmergencyContactPhone = form.EmergencyContactPhone;
                    profile.UpdatedAt = DateTime.UtcNow; // Ghi nhận thời gian cập nhật

                    await _db.SaveChangesAsync(); // Lưu thay đổi
                    TempData.Clear(); // Xóa TempData (nếu có từ forced completion)
                    TempData["ok"] = isForcedCompletion ? "Hoàn thiện hồ sơ thành công." : "Đã cập nhật hồ sơ.";

                    // Nếu là bắt buộc hoàn thiện, đăng nhập lại và chuyển hướng
                    if (isForcedCompletion)
                    {
                        var user = await UserManager.FindByIdAsync(userIdToUpdate);
                        if (user != null) await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                            return Redirect(returnUrl);
                        return RedirectToAction("Index", "Doctors");
                    }
                    // Nếu là chỉnh sửa thông thường, chuyển hướng
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                            return Redirect(returnUrl);
                        // Chuyển về trang quản lý tài khoản sau khi sửa thành công
                        return RedirectToAction("Index", "Manage", new { area = "" });
                    }
                }
                // Trường hợp lỗi: Không tìm thấy hồ sơ khi đang chỉnh sửa, hoặc không có quyền
                else
                {
                    TempData.Clear();
                    TempData["err"] = "Không tìm thấy hồ sơ hoặc bạn không có quyền chỉnh sửa.";
                    // Nếu đang đăng nhập thì về trang chủ, nếu không thì về login
                    return User.Identity.IsAuthenticated ? RedirectToAction("Index", "Home") : RedirectToAction("Login", "Account");
                }
            }
        }


        // Giải phóng DbContext
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
                // Không cần Dispose UserManager, SignInManager vì chúng được quản lý bởi OWIN context
            }
            base.Dispose(disposing);
        }

        // Helper thêm lỗi Identity vào ModelState (giữ nguyên)
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                // Cố gắng gán lỗi vào đúng trường Email nếu có thể
                if (error.ToLower().Contains("email"))
                    ModelState.AddModelError("Email", error);
                // Xử lý lỗi trùng username (thường là email)
                else if (error.ToLower().Contains("name is already taken") && TempData.Peek("PendingRegEmail") != null && error.ToLower().Contains(TempData.Peek("PendingRegEmail") as string ?? ""))
                    ModelState.AddModelError("Email", "Địa chỉ email này đã được đăng ký.");
                else if (error.ToLower().Contains("name is already taken") && TempData.Peek("PendingExternalEmail") != null && error.ToLower().Contains(TempData.Peek("PendingExternalEmail") as string ?? ""))
                    ModelState.AddModelError("Email", "Địa chỉ email này đã được đăng ký.");
                // Lỗi chung khác
                else
                    ModelState.AddModelError("", error);
            }
        }
    }
}
