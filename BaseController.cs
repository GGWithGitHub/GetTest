using ExposureTrack.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using ExposureTrack.Data;
using ExposureTrack.Core.Authorization;
using ExposureTrack.Website.Models;
using ExposureTrack.Core.Extensions.MVC;
using ExposureTrack.Website.Services;

namespace ExposureTrack.Website.Controllers
{
    public class BaseController : Controller
    {
        public bool JsonResponseExpected {
            get {
                //return as JSON for appropriate requests	
                var isJson = Request.AcceptTypes.Where(at => at.IndexOf("json") >= 0).Count() > 0;
                var isJsonP = !string.IsNullOrWhiteSpace(Request.QueryString["callback"]);
                //var isJS = Request.AcceptTypes.Where(at => at.IndexOf("javascript") >= 0).Count() > 0;
                return isJson || isJsonP;
                //if (!(isJsonP || isJson || isJS)) return false;
            }
        }

        public LoginUser CurrentUser {
            get {
                return LoginUser.Current;
            }
        }

		public static ExposureTrackRepository Repository {
			get {
				return ExposureTrackRepository.Current;
			}
		}

		public static ExposureTrackEntities DB {
			get {
				return Repository.DB;
			}
		}

        //This is hardcoded guids for short hand reference so I don't have to type them out in code. 
        //I can just reference by name "Tnec" to take away the guessing game
        public static Guid OrgIdByName(string name)
        {
            switch(name)
            {
                case "Tnec":
                    return Guid.Parse("14fd457c-4b44-4125-a6de-eb104e4219fe");
                default:
                    return Guid.Parse("d9800465-205e-4650-b445-c53dd35846e5"); //Demo org
            }
        }

        new public JsonResult Json(object data)
        {
            return new JsonNetResult(data);
        }

        public JsonResult Json(object data, int? httpStatusCode = null, bool prettyFormat = false)
        {
            return new JsonNetResult(data, httpStatusCode, prettyFormat);
        }

		protected override void OnActionExecuted(ActionExecutedContext filterContext)
		{
			//Apply Menu Here...
			//if (!filterContext.Canceled && filterContext.Result is ViewResult) {
			//	//add menu items only if the result is a view result (display html)
			//	this.AddMenuItem(new MenuItem("test"));
			//}
            
			base.OnActionExecuted(filterContext);
		}

        protected void Login(LoginUser user, bool remember = false) {
            Session["LoginuserOrganizationID"] = Session["loginuserorgid"] = (user == null || user.CurrentOrg == null) ? null : (Guid?)user.CurrentOrg.Id;
            Session["NumberOFFailed"] = 0;
            HttpContext.User = user;
            HttpContext.ApplicationInstance.ClearAuthenticationTicket();
            HttpContext.ApplicationInstance.SetAuthenticationTicket(user, remember);
            DB.SiteUsers.Where(su => su.SiteUserId == user.Id).FirstOrDefault().LastLogon = DateTime.UtcNow;
            DB.SaveChanges();
        }

        protected void Logout()
        {
            string isRegionvalue = string.Empty;
                //start : SRX 02535 SignalR
                var user = User as LoginUser;

                isRegionvalue = user.CurrentOrg.Id.ToString();
                // var UserRole = new ExposureTrack.Data.DatabasModelRepository(new ExposureTrack.Data.ExposureTrackEntities()).GetUserRole(user.Username);

                if (LoginUser.Current.HasRole(UserRole.Instructor))
                {
                    CourseSessionService.GetInstance().ClearAll();
                }
                //End : SRX 02535 SignalR

                Session.Abandon();
                HttpContext.ApplicationInstance.ClearAuthenticationTicket();
                HttpContext.Application["HealthVaultToken"] = string.Empty; // Clear HealthVault Token Info.
                HttpContext.Application["LicenseAgreementGuid"] = string.Empty; // Clear LicenseAgreementGuid.


                //create cookie
                if (LoginUser.Current.HasRole(UserRole.SuperAdmin))
                {
                }
                else
                {
                    if (Session["referrer"] != null)
                    {
                        var cookie = new HttpCookie("UserCuurentORgID");
                        cookie.Value = Session["referrer"].ToString();
                        cookie.Expires.AddHours(1);
                        Response.Cookies.Add(cookie);
                    }
                }

                HttpCookie aCookie;
                string cookieName;
                int limit = Request.Cookies.Count;
                for (int i = 0; i < limit; i++)
                {
                    cookieName = Request.Cookies[i].Name;
                    aCookie = new HttpCookie(cookieName);
                    aCookie.Expires = DateTime.Now.AddDays(-1);
                    Response.Cookies.Add(aCookie);
                }
        }

        public bool AssignRegionByHostname()
        {
            var host = System.Web.HttpContext.Current.Request.Url.DnsSafeHost;
            host = host.ToLower();
            bool isSession = Session == null;
            var tmp_Session = Session;
            if (tmp_Session == null)
                tmp_Session = new System.Web.HttpSessionStateWrapper(System.Web.HttpContext.Current.Session);

            //ExposureTrack.Website.Services.ExceptionService.writer.addException(new Exception("DNS Safe host responded with: " + host + ". session default is null?:" + isSession + " new session is null?: " + (tmp_Session == null)));


            if (host.Contains("p2racademy"))
            {
                tmp_Session["orgid"] = "36b1aa04-de38-441d-8048-d4605d1113b3";
            }
            else if (host.Contains("tnec"))
            {
                //throw new InvalidDataException("TNEC org not implemented yet");
                tmp_Session["orgid"] = "14fd457c-4b44-4125-a6de-eb104e4219fe";
            }
            else if (host.Contains("pilot"))
            {
                tmp_Session["orgid"] = "d9800465-205e-4650-b445-c53dd35846e5";
            }
            else if (host.Contains("devhazreadyv2"))  //SET SAME AS TNEC FOR TESTING WITH TNEC PEOPLE
            {
                //below is tnec org, no longer accurate for default
                //Session["orgid"] = "14fd457c-4b44-4125-a6de-eb104e4219fe";
                tmp_Session["orgid"] = null; //go to org chooser
            }
            else if (host.Contains("local"))  //SET SAME AS TNEC FOR TESTING WITH TNEC PEOPLE
            {
                //this shouldn't matter in live
                //Session["orgid"] = "36b1aa04-de38-441d-8048-d4605d1113b3";
                tmp_Session["orgid"] = null;
            }
            else if (host.Contains("seamist"))
            {
                tmp_Session["orgid"] = "cb72e489-d384-4b63-9f08-6791fd8007ea";
            }
            else if (host.Contains("toyota"))
            {
                tmp_Session["orgid"] = "a367e820-911d-4ac6-a802-3b77c7baa575";
            }
            else if (host.Contains("scoeh"))
            {
                tmp_Session["orgid"] = "36b1aa04-de38-441d-8048-d4605d1113b3";
            }
            else if (host.Contains("mchwwt"))
            {
                Session["orgid"] = "8d979c17-09a5-4f63-bfcb-4e4c9ab7182c";
            }
            else if (host.Contains("cert."))
            {
                Session["orgid"] = "dcb50847-3599-4987-bed9-75d1bde1c633";
            }
            else
            {
                Session["orgid"] = null;//"d9800465-205e-4650-b445-c53dd35846e5";
                return  false;
            }
            //ExposureTrack.Website.Services.ExceptionService.writer.addException(new Exception("DNS Safe host responded with: " + host + ". session default is null?:" + isSession + " new session is null?: " + (tmp_Session == null)));
            return true;
        }

        protected void Impersonate(Guid userId) {
            var userTologin = Repository.SiteUser.GetUser(userId);
            var userToImpersonate = userTologin.AsLoginUser(userTologin.Roles.FirstOrDefault().OrganizationId);
            var user = CurrentUser.Impersonate(userToImpersonate);
            Login(user, false);
        }

        protected void Unimpersonate() {
            var user = CurrentUser.UnImpersonate();
            Login(user, false);
        }

	}

}
