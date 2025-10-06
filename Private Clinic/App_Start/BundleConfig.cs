using System.Web.Optimization;

namespace Private_Clinic
{
    public class BundleConfig
    {
        // Gọi từ Global.asax -> Application_Start
        public static void RegisterBundles(BundleCollection bundles)
        {
            // (Tuỳ chọn) jQuery & Validate — giữ nếu bạn có dùng
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                "~/Scripts/jquery-{version}.js"
            ));
            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                "~/Scripts/jquery.validate*",
                "~/Scripts/jquery.unobtrusive*"
            ));

            // ===== CSS: Site người dùng (KHỚP THƯ MỤC HIỆN TẠI) =====
            bundles.Add(new StyleBundle("~/Content/appcss")
                .Include("~/Content/app-core.css", new CssRewriteUrlTransform())
                .Include("~/Content/app-layout.css", new CssRewriteUrlTransform())
                .Include("~/Content/app-components.css", new CssRewriteUrlTransform())
                .Include("~/Content/app-pages.css", new CssRewriteUrlTransform())
            );

            // ===== CSS: Khu Admin (nếu dùng _AdminLayout.cshtml) =====
            bundles.Add(new StyleBundle("~/Content/admincss")
                .Include("~/Content/app-core.css", new CssRewriteUrlTransform())
                .Include("~/Content/app-admin.css", new CssRewriteUrlTransform())
            );

            bundles.Add(new ScriptBundle("~/bundles/appjs")
                .Include(
                         "~/Scripts/app/app-nav.js",
                          "~/Scripts/app/auth-login.js",
                          "~/Scripts/app/auth-forgot.js"
));


            // Bật khi release (hoặc đặt <compilation debug="false"/>)
            // BundleTable.EnableOptimizations = true;
        }
    }
}
