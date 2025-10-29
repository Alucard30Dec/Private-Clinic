using System;
using System.Configuration; // Thêm dòng này
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google; // Đảm bảo có using này
using Owin;
using Clinic.Models;

namespace Clinic
{
    public partial class Startup
    {
        // For more info: https://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Per-request contexts (Identity)
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            // App cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                Provider = new CookieAuthenticationProvider
                {
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
                }
            });

            // External cookie (giữ phiên khi đi qua Google)
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // 2FA cookies (nếu bạn dùng)
            // app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));
            // app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

            // ===== GOOGLE OAUTH2 - KÍCH HOẠT VÀ CẤU HÌNH =====
            // Lấy ClientId và ClientSecret từ AppSettings (Web.config hoặc AppSecrets.config)
            var googleClientId = ConfigurationManager.AppSettings["GoogleClientId"];
            var googleClientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"];

            // Chỉ kích hoạt nếu có đủ thông tin cấu hình
            if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                var googleOptions = new GoogleOAuth2AuthenticationOptions
                {
                    ClientId = googleClientId,
                    ClientSecret = googleClientSecret,

                    // Quan trọng: CallbackPath phải khớp với URI bạn đăng ký trên Google Cloud Console
                    // Mặc định là /signin-google, thường không cần đổi
                    CallbackPath = new PathString("/signin-google"),

                    // (Tùy chọn) Luôn hiển thị chọn tài khoản khi đăng nhập Google
                    // Nếu không có Provider này, Google có thể tự động chọn tài khoản đã đăng nhập trước đó
                    Provider = new GoogleOAuth2AuthenticationProvider
                    {
                        OnApplyRedirect = context =>
                        {
                            string redirect = context.RedirectUri;
                            // Thêm tham số prompt=select_account nếu chưa có
                            if (!redirect.Contains("prompt="))
                            {
                                redirect += (redirect.Contains("?") ? "&" : "?") + "prompt=select_account";
                            }
                            context.Response.Redirect(redirect);
                        }
                    }
                };
                app.UseGoogleAuthentication(googleOptions);
            }
            else
            {
                // Ghi log hoặc cảnh báo nếu thiếu cấu hình Google Auth
                System.Diagnostics.Debug.WriteLine("Cảnh báo: Google ClientId hoặc ClientSecret chưa được cấu hình trong AppSettings. Đăng nhập Google sẽ không hoạt động.");
            }
            // ===================================================

            // Cấu hình các nhà cung cấp đăng nhập ngoài khác (nếu có)
            // Ví dụ: Facebook, Microsoft Account,...
            // app.UseFacebookAuthentication(...)
            // app.UseMicrosoftAccountAuthentication(...)
        }
    }
}
