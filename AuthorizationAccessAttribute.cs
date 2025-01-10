using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace mfc.Attributes
{
    public class AuthorizationAccessAttribute : ActionFilterAttribute
    {
        public string Roles { get; set; }
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var user = filterContext.HttpContext.Session["Username"];
            var strAccessLevel = Convert.ToString(filterContext.HttpContext.Session["AccessLevel"]);

            if (user == null)
                filterContext.Result = new RedirectResult("/Account/Login");

            if (!string.IsNullOrEmpty(Roles))
            {
                var roleList = Roles.Split(',').Select(x => x.Trim());
                if (!(strAccessLevel.IndexOf("SuperAdmin", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    if (user != null && !roleList.Any(x => strAccessLevel == x))
                        filterContext.Result = new RedirectResult("/Dashboard/Index");
                }
            }
        }
    }
}