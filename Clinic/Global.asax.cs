using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using Microsoft.Owin.Security;

namespace Clinic
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        // *** SỬA LẠI PHƯƠNG THỨC NÀY ***
        protected void Application_AuthenticateRequest(Object sender, EventArgs e)
        {
            // Check if the user is authenticated via OWIN Identity
            if (Context.User != null && Context.User.Identity.IsAuthenticated)
            {
                string requestedPath = Context.Request.Path.ToLowerInvariant();
                string appPath = VirtualPathUtility.ToAbsolute("~/").ToLowerInvariant(); // Lấy đường dẫn gốc ứng dụng

                // Check if the request is for the root or the home controller index
                // So sánh chính xác hơn với đường dẫn gốc
                bool isRootOrHome = (requestedPath == appPath ||
                                     requestedPath == appPath + "home" ||
                                     requestedPath == appPath + "home/index");

                // Only redirect if accessing the root/home page AND the user has a specific internal role
                if (isRootOrHome)
                {
                    string redirectUrl = null;

                    // *** SỬ DỤNG Context.User.IsInRole() TRỰC TIẾP ***
                    if (Context.User.IsInRole("Admin"))
                    {
                        redirectUrl = "~/Admin";
                    }
                    else if (Context.User.IsInRole("Doctor"))
                    {
                        redirectUrl = "~/Doctor";
                    }
                    else if (Context.User.IsInRole("Receptionist"))
                    {
                        redirectUrl = "~/Reception/Dashboard"; // Hoặc "~/Reception"
                    }
                    // Patients will have redirectUrl = null and stay on the home page.

                    if (redirectUrl != null)
                    {
                        // *** SỬ DỤNG Response.Redirect() ĐƠN GIẢN HƠN ***
                        Context.Response.Redirect(VirtualPathUtility.ToAbsolute(redirectUrl), true); // true = end response
                                                                                                     // Không cần return vì Response.Redirect(..., true) sẽ kết thúc request
                    }
                }
                // If not accessing root/home OR is a Patient, continue normally.
            }
            // If not authenticated, continue normally.
        }
        // *** KẾT THÚC SỬA PHƯƠNG THỨC ***
    }
}

