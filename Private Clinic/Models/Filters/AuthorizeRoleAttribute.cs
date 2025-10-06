using System.Linq;
using System.Web;
using System.Web.Mvc;
using Private_Clinic.Models;

namespace Private_Clinic.Filters
{
    public class AuthorizeRoleAttribute : AuthorizeAttribute
    {
        private readonly UserRole[] _roles;
        public AuthorizeRoleAttribute(params UserRole[] roles) { _roles = roles; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var roleObj = httpContext.Session?["Role"];
            if (roleObj == null) return false;
            var role = (UserRole)roleObj;
            return _roles.Contains(role);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectResult("~/");
        }
    }
}
