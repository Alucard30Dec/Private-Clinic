using System.Web.Mvc;
using System.Web.Routing;

namespace Clinic
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            var defaultRoute = routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "Clinic.Controllers" }   // <-- chỉ rõ controller ở root
            );

            // Đảm bảo route này thuộc area rỗng (root)
            defaultRoute.DataTokens = defaultRoute.DataTokens ?? new RouteValueDictionary();
            defaultRoute.DataTokens["area"] = "";
            defaultRoute.DataTokens["UseNamespaceFallback"] = false;
        }
    }
}
