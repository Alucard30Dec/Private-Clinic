using System;
using System.Configuration;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
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
            app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));
            app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

            // ===== GOOGLE OAUTH2 =====
            var googleClientId = ConfigurationManager.AppSettings["GoogleClientId"];
            var googleClientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"];

            if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                var googleOptions = new GoogleOAuth2AuthenticationOptions
                {
                    ClientId = googleClientId,
                    ClientSecret = googleClientSecret,

                    // Đảm bảo callback là /signin-google (mặc định cũng là thế, nhưng ghi rõ cho chắc)
                    CallbackPath = new PathString("/signin-google"),

                    // Luôn hiển thị chọn tài khoản khi đăng nhập Google
                    Provider = new GoogleOAuth2AuthenticationProvider
                    {
                        OnApplyRedirect = context =>
                        {
                            var redirect = context.RedirectUri;
                            if (!redirect.Contains("prompt="))
                                redirect += (redirect.Contains("?") ? "&" : "?") + "prompt=select_account";
                            context.Response.Redirect(redirect);
                        }
                    }
                };

                app.UseGoogleAuthentication(googleOptions);
            }
            // ==========================
        }
    }
}
