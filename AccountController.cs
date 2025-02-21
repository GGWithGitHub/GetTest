using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using ExposureTrack.Core.Authorization;
using ExposureTrack.Data;
using ExposureTrack.Website.Models;
//using ExposureTrack.Website.Models.SignalR;
using System.Globalization;
using Recaptcha;
using System.IO;
using System.Text;
using ExposureTrack.Website.Services;
using System.Web.Helpers;
using System.Web.Script.Serialization;
using ExposureTrack.Core.Entities;
using System.Data.Entity;
using Twilio.WebMatrix;
using ExposureTrack.Website.Utility;
using Microsoft.VisualBasic.ApplicationServices;

namespace ExposureTrack.Website.Controllers
{
    public class AccountController : BaseController
    {
        //
        // GET: /Account/
        [Authorize()]
        public ActionResult Index(string id)
        {
            return View();
        }

        public ActionResult Logout()
        {
            string isRegionvalue = string.Empty;
            try
            {
                //start : SRX 02535 SignalR
                var user = User as LoginUser;

                isRegionvalue = user.CurrentOrg.Id.ToString();
                // var UserRole = new ExposureTrack.Data.DatabasModelRepository(new ExposureTrack.Data.ExposureTrackEntities()).GetUserRole(user.Username);

                if (LoginUser.Current.HasRole(UserRole.Instructor))
                {
                    CourseSessionService.GetInstance().ClearAll();
                }
                //End : SRX 02535 SignalR

                //Does this really work?
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
                    //Added because routing isn't working...
                    AssignRegionByHostname();

                    //It should always be null if we abandon sesson
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
            catch { }



            return RedirectToAction("Home", "Hazready");
        }


        public static void AddLoginLog(HttpRequestBase request)
        {
            var bc = request.Browser;
            string userBrowser = "User Browser is:" + bc.Browser + " version " + bc.Version + ". Server variable of HTTP_USER_AGENT: " + request.ServerVariables["HTTP_USER_AGENT"];
            string userIpAddress = request.ServerVariables["REMOTE_ADDR"];
            DB.LoginLogs.Add(new LoginLog() { browser = userBrowser, dateCreated = DateTime.Now, ipAddress = userIpAddress, loginlogID = Guid.NewGuid(), studentGuid = LoginUser.Current.Id });
            DB.SaveChanges();
        }

        public ActionResult CssCreation()
        {
            //var Repository = new ExposureTrackRepository();
            //Organization objOrganization = new Organization();
            //objOrganization=Repository.OrganizationManager.GetOrganizationFullDetail(Guid.Parse("B2087C85-E677-44B1-B989-01F8A3E6D914"));
            //Typesofcolor objThemeColor = new Typesofcolor();

            //string str = WebConfigSiteMode.JsonSerializer<Typesofcolor>(objThemeColor);
            //objThemeColor= WebConfigSiteMode.DeserializeJSon<Typesofcolor>(objOrganization.ThemeCss.ToString());
            //UpdateOrganizationDetail(Guid.Parse("7A4E8761-D541-41F8-9958-11DB578D84E1"), "#11111", "#22222", "#333333");  
            return View();
        }
        //Worked for  "23MayHazready.docx" on 03 jun 2014
        public JsonResult GetOrganizationDetail(Guid organizationid)
        {
            try
            {
                // this is alredy included     -> var Repository = new ExposureTrackRepository();
                Organization objOrganization = Repository.OrganizationManager.GetOrganizationFullDetail(organizationid);
                OrganizationDetail objOrganizationDetail = new OrganizationDetail();
                objOrganizationDetail.Name = objOrganization.Name;
                objOrganizationDetail.Notes = objOrganization.Notes;
                objOrganizationDetail.keepPrivate = objOrganization.KeepPrivate;

                // Location Info
                objOrganizationDetail.Address1 = objOrganization.Address1;
                objOrganizationDetail.Address2 = objOrganization.Adresss2;
                objOrganizationDetail.City = objOrganization.City;
                objOrganizationDetail.State = objOrganization.State;
                objOrganizationDetail.PostalCode = objOrganization.PostalCode;
                objOrganizationDetail.Country = Convert.ToString(objOrganization.CountryCodeID);
                objOrganizationDetail.GoogleMapLink = objOrganization.GoogleMapLink;
                objOrganizationDetail.latitude = Convert.ToString(objOrganization.latitude);
                objOrganizationDetail.longitude = Convert.ToString(objOrganization.longitude);
                objOrganizationDetail.TimeZone = Convert.ToString(objOrganization.TimeZoneID);
                //Contact Info
                objOrganizationDetail.Phone = objOrganization.Phone;
                objOrganizationDetail.Fax = objOrganization.Fax;
                objOrganizationDetail.Email = objOrganization.siteEmail;

                if (objOrganization.ParentOrganizationId != null)
                {
                    objOrganizationDetail.ParentOrganizationId = objOrganization.ParentOrganizationId.Value;
                    //parent organization value
                    var parentorgqanization = Repository.OrganizationManager.GetOrganizationFullDetail(objOrganizationDetail.ParentOrganizationId);
                    if (parentorgqanization != null)
                        objOrganizationDetail.ParentOrganizationName = parentorgqanization.Name;
                }
                else
                    objOrganizationDetail.ParentOrganizationId = Guid.Empty;



                if (objOrganization.PrimaryAdmin != null)
                    objOrganizationDetail.ParentAdmin = objOrganization.PrimaryAdmin.Value;
                else
                    objOrganizationDetail.ParentAdmin = Guid.Empty;

                // if (objOrganization.ThemeCss != null )
                if (!String.IsNullOrEmpty(objOrganization.ThemeCss))
                {
                    objOrganizationDetail.bannerBackground = WebConfigSiteMode.DeserializeJSon<Typesofcolor>(objOrganization.ThemeCss).bannerBackground;
                    objOrganizationDetail.bannerFontColor = WebConfigSiteMode.DeserializeJSon<Typesofcolor>(objOrganization.ThemeCss).bannerFontColor;
                    objOrganizationDetail.bannerLinkRollover = WebConfigSiteMode.DeserializeJSon<Typesofcolor>(objOrganization.ThemeCss).bannerLinkRollover;
                    objOrganizationDetail.buttonText = WebConfigSiteMode.DeserializeJSon<Typesofcolor>(objOrganization.ThemeCss).buttonText;
                    objOrganizationDetail.buttonColor = WebConfigSiteMode.DeserializeJSon<Typesofcolor>(objOrganization.ThemeCss).buttonColor;
                    objOrganizationDetail.buttonTextRollover = WebConfigSiteMode.DeserializeJSon<Typesofcolor>(objOrganization.ThemeCss).buttonTextRollover;
                    objOrganizationDetail.buttonRollover = WebConfigSiteMode.DeserializeJSon<Typesofcolor>(objOrganization.ThemeCss).buttonRollover;
                }

                //get the xApi fields
                objOrganizationDetail.xApiEndPoint = objOrganization.xApiEndPoint;
                objOrganizationDetail.xApiUserName = objOrganization.xApiUserName;
                objOrganizationDetail.xApiPassword = objOrganization.xApiPassword;



                return this.Json(objOrganizationDetail);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    data = new
                    {
                        message = ex.Message
                        ,
                        details = !Request.IsLocal ? "not available" : ex.StackTrace
                    }
                }, JsonRequestBehavior.AllowGet);

            }
        }

        [AllowAnonymous]
        public ActionResult FastClassLogin(string ReturnUrl = null)
        {
            if (!MvcApplication.InstantClassSessionsEnabled)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = ReturnUrl;
            // return View();
            return PartialView("_FastClassLogin");
        }

        public JsonResult UpdateOrganizationDetail(string organizationid, string organizationTitle, string notes, string bannerBackground, string bannerFontColor, string bannerLinkRollover, string buttonText, string buttonColor, string buttonTextRollover, string buttonRollover, string mode)
        {
            try
            {
                var Repository = new ExposureTrackRepository();
                Organization objOrganization = new Organization();

                if (mode == "Add")
                {
                    objOrganization.OrganizationId = Guid.NewGuid();
                }
                if (mode == "Edit")
                {
                    objOrganization.OrganizationId = Guid.Parse(organizationid);
                }

                Typesofcolor objThemeColor = new Typesofcolor();
                objThemeColor.bannerBackground = bannerBackground;
                objThemeColor.bannerFontColor = bannerFontColor;
                objThemeColor.bannerLinkRollover = bannerLinkRollover;
                objThemeColor.buttonText = buttonText;
                objThemeColor.buttonColor = buttonColor;
                objThemeColor.buttonTextRollover = buttonTextRollover;
                objThemeColor.buttonRollover = buttonRollover;
                objOrganization.ThemeCss = WebConfigSiteMode.JsonSerializer<Typesofcolor>(objThemeColor).ToString();
                objOrganization.Name = organizationTitle;
                objOrganization.Notes = notes;

                string path = Server.MapPath("~/css/site/" + objOrganization.OrganizationId.ToString().ToLower() + ".css");
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }

                int iversion = 0;

                if (mode == "Add")
                {
                    objOrganization.cssLogoVersion = iversion;
                    objOrganization.KeepPrivate = true;
                    objOrganization.Active = true;
                    Repository.OrganizationManager.UpdateOrganization(objOrganization, true);
                }
                if (mode == "Edit")
                {
                    iversion = Repository.OrganizationManager.GetOrganizationVersion(objOrganization.OrganizationId);
                    iversion += 1;
                    objOrganization.cssLogoVersion = iversion;

                    Repository.OrganizationManager.UpdateOrganization(objOrganization, false);
                }
                string siteCssTemplateText = System.IO.File.ReadAllText(Server.MapPath("~/css/site/SiteSpecifcTemplate.css"));
                siteCssTemplateText = siteCssTemplateText.Replace("@bannerBackground", bannerBackground).Replace("@bannerFontColor", bannerFontColor).Replace("@bannerLinkRollover", bannerLinkRollover).Replace("@buttonText", buttonText).Replace("@buttonColor", buttonColor).Replace("@buttonTextRollover", buttonTextRollover).Replace("@buttonRollover", buttonRollover);
                System.IO.File.WriteAllText(path, siteCssTemplateText);


                return this.Json("sucess");
            }
            catch (Exception ex)
            {
                return this.Json("error");

            }
        }


        [HttpGet]
        public ActionResult Login()
        {
            //already logged in
            if (Request.IsAuthenticated) return CheckAgreements() ?? RedirectToAction("Index", "Home");

            //if you were logged in before, clears the current role setting
            LoginUser.CurrentRole = null;

            //this.AddAlert(new Alert("plain test"));
            //this.AddAlert(new Alert("test alert", AlertLevel.Info, "Info Title"));
            //this.AddAlert(new Alert("test alert", AlertLevel.Success, "Success Title", FontAwesomeIcon.music));
            //this.AddAlert(new Alert("test alert", AlertLevel.Warning, "Warning Title", FontAwesomeIcon.play));
            //this.AddAlert(new Alert("test alert", AlertLevel.Danger, "Danger Title", FontAwesomeIcon.phone));

            if (Session["Message"] != null)  // Updated for SRX02484 on 08 March 2014
            {
                ViewBag.Message = "Account Created Successfully!";
                Session.Remove("Message");

            }
            var model = new AccountLoginModel();
            Session["NumberOFFailed"] = "0";
            model.NoOfFailed = 0;
            return View(model);
        }

        [HttpPost, RecaptchaControlMvc.CaptchaValidator]
        public ActionResult Login(AccountLoginModel model, bool captchaValid, string captchaErrorMessage, string returnUrl)
        {
            //already logged in
            if (Request.IsAuthenticated) return CheckPassword(model.Password) ?? (LoginUser.CurrentRole == null ? RedirectToAction("SelectRole", new { returnUrl }) : RedirectToAction("Index", "Home"));

            var user = Repository.SiteUser.GetUserForLogin(model.Username.Trim(), model.Password.Trim());
            int icheck = Convert.ToInt32(Session["NumberOFFailed"] ?? 0);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid Usename or Password specified.");
                icheck += 1;
                Session["NumberOFFailed"] = icheck.ToString();
                model.NoOfFailed = icheck;
                ViewBag.JS = false;
                return View(model);
            }

            //user's organization has an orgadmin role.
            var orgRole = user.Roles.First();
            Session["orgid"] = orgRole != null ? orgRole.OrganizationId : Guid.Empty;

            if (icheck > 2)
            {
                var isValid = IsValidCaptcha();
                if (!isValid)
                {
                    ModelState.AddModelError("captcha","Captch is required");
                    model.NoOfFailed = icheck;
                    return View(model);
                }
            }


            //save last logged in date/time
            user.JsonData["LastLogin"] = null;
            user.JsonData["LastLogin"] = DateTime.UtcNow;

            //Start :- See SRX02604
            //var theme = Json(db.GetOrgTheme(user.Organization));
            try
            {
                var orgId = user.Roles.Select(r => (Guid?)r.OrganizationId).FirstOrDefault();
                var theme = Json(Repository.Model.GetOrgTheme(orgId));
                user.JsonData["Theme"] = theme.ToString();
            }
            catch (Exception ex) { }

            if (model.DoNotAskAgainForSMSS)
            {
                Repository.SiteUser.UpdateSmssAskAgain(user);
            }

            //defer login credential change to base
            base.Login(user.AsLoginUser(user.Roles.FirstOrDefault().OrganizationId), model.RememberMe);

            AddLoginLog(Request);

            return CheckPassword(model.Password.Trim()) ?? RedirectToAction("SelectRole", new { returnUrl = returnUrl });
        }

        [HttpPost]
        public ActionResult CheckSmssAskAgainFlag(string username, string pwd)
        {
            var user = Repository.SiteUser.GetUserForLogin(username.Trim(), pwd.Trim());
            if (user != null)
            {
                if ((user.SMSSDoNotAskAgain != null && (bool)user.SMSSDoNotAskAgain) || (!string.IsNullOrEmpty(user.SMSPhoneNumber)))
                {
                    return Json(new { success = true, askAgainFlag = true });
                }
                else
                {
                    return Json(new { success = true, askAgainFlag = false });
                }
            }
            else
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public ActionResult UpdatePhoneNumber(string username, string pwd, string smsPhoneNumber)
        {
            try
            {
                var user = Repository.SiteUser.GetUserForLogin(username.Trim(), pwd.Trim());
                Repository.SiteUser.UpdatePhoneNumber(user, smsPhoneNumber);
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }

        private ActionResult CheckPassword(string password)
        {
            var u = DB.SiteUsers.Where(su => su.SiteUserId == LoginUser.Current.Id).FirstOrDefault();

            bool isDevMode = false;
            try { isDevMode = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["isDevMode"]); } catch (Exception) { }
            if (isDevMode && password == "Artificial2!") {
                return null;
            }

            //if the password is no longer training, return null
            if (!u.MatchesPassword("training")) {
                var current = HashedPassword.FromString(u.pvt_Password);
                var entered = HashedPassword.FromPassword(password, current.Salt, current.Method);
                var matches = (Convert.ToBase64String(current.Hash) == Convert.ToBase64String(entered.Hash));

                if ((!u.TempExpires.HasValue || u.TempExpires.Value > DateTime.UtcNow) && (matches)) return null;
            }
            
            var model = new AccountLoginModel();
            Session["NumberOFFailed"] = "0";
            model.NoOfFailed = 0;
            ViewBag.JS = true;
            model.SiteUserId = LoginUser.Current.Id.ToString();
            return View("Login", model);
        }


        public ActionResult CheckAgreements()
        {
            // check user entry in LicenseActivationLog table.
            var LicenseAgreement = Data.LicenseAgreementLog.FirstMissingLogsForUser_Org(LoginUser.Current.Id, LoginUser.Current.CurrentOrg.Id, DB); //Repository.LicenseAgreements.GetUserLicenseAgreementLogs(LoginUser.Current.Id);

            //does the user need to accept the license condition
            if (LicenseAgreement != null)
            {
                return RedirectToAction("Index", "LicenseAgreementLog");
            }

            return null;
        }

        public ActionResult SelectRole(UserRole? role, string returnUrl)
        {

            if (LoginUser.Current == null) return RedirectToAction("Login", new { returnUrl });

            LoginUser.CurrentRole = UserRole.None; //reset current role, force selection

            var roles = LoginUser.Current.CurrentOrg.Roles;

            if (role.HasValue && roles.Contains(role.Value))
            {
                LoginUser.CurrentRole = role;
                return RedirectToUrlOrHome(returnUrl);
            }

            if (roles.Count > 1 && !role.HasValue)
            {
                return View(roles);
            }

            if (roles.Count() == 0)
            {
                LoginUser.CurrentRole = null;
            }

            if (roles.Count == 1)
            {
                LoginUser.CurrentRole = roles[0];
            }

            return RedirectToUrlOrHome(returnUrl);
        }

        private ActionResult RedirectToUrlOrHome(string returnUrl)
        {
            //returnUrl specified, use it
            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }



        [Authorize]
        public ActionResult New()
        {
            //not logged in
            if (!Request.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            return View();
        }


        [HttpPost]
        public ActionResult changePassword(AccountLoginModel model)
        {

            var userName = model.Username;
            //Needs validation before progressing...
            if (Repository.SiteUser.GetUserByUsername(userName) != null)
            {
                var user = Repository.SiteUser.GetUserByUsername(userName);

                //set the user's password to something random, send an email of what the password is, save the user.
                Random random = new Random((int)DateTime.Now.Ticks);
                string newPassword = "";
                for (int x = 0; x < 4; x++)
                {
                    newPassword += Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                }
                System.Diagnostics.Debug.WriteLine(newPassword);

                SmtpClient client = new SmtpClient();


                client.Host = ConfigurationManager.AppSettings["SystemEmailHost"]; ;//mail.exposuretrack.com
                client.Timeout = 10000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["SystemEmailFrom"], ConfigurationManager.AppSettings["SystemEmailPassword"]);//"donotreply@exposuretrack.com", "donotreply5"

                //donotreply@exposuretrack.com
                MailMessage mm = new MailMessage(ConfigurationManager.AppSettings["SystemEmailFrom"], user.EmailAddress, "Change Password",
                "" + user.Username + "'s password has been changed.\nThe new password is now " + newPassword
                + "\nUse your new password to access your account. You can change your password anytime in your Profile"

                );


                try
                {
                    client.Send(mm);
                    user.SetPassword(newPassword);
                    Repository.SiteUser.SaveUser(user);
                    this.AddAlert(new Alert("An email has been sent to the address associated with your username. Please check your email for your new password. Delivery may take several minutes.", AlertLevel.Info, "Email Sent!", FontAwesomeIcon.exclamation));

                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }

            }
            else //No such username found
            {
                this.AddAlert(new Alert("No such username found", AlertLevel.Danger, "Invalid Username", FontAwesomeIcon.exclamation));
            }

            return RedirectToAction("Index", "Account/Login");
        }

        public ActionResult ForgotPassword(string id)
        {
            //if (!string.IsNullOrEmpty(id))
            //{
            //    ViewBag.IsValidLink = string.Empty;
            //    ViewBag.UserId = string.Empty;

            //    var DecodedUrl = new Base64EncodeDecode().Base64Decode(id);
            //    string[] words = DecodedUrl.Split('_');
            //    ViewBag.UserId = words[0];

            //    DateTime dateValid = Convert.ToDateTime(words[1].ToString());
            //    dateValid = dateValid.AddDays(2);

            //    DateTime CurrDate = DateTime.Now;
            //    if (dateValid < CurrDate)
            //    {
            //        ViewBag.IsValidLink = "No";
            //    }
            //    else
            //    {
            //        ViewBag.IsValidLink = "Yes";
            //    }
            //}
            //What is the purpose of above validation?


            ViewBag.IsValidLink = "Yes";
            return View();
        }

        public JsonResult ResetPassword(string UserId, string Password)
        {
            try
            {
                var user = Repository.SiteUser.GetUser(Guid.Parse(UserId));
                user.SetPassword(Password);
                Repository.SiteUser.SaveUser(user);

                return this.Json("Success!");
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = new
                    {
                        message = ex.Message
                        ,
                        details = !Request.IsLocal ? "not available" : ex.StackTrace
                    }
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult changePasswordNew(string username)
        {
            var alertMessage = "An email has been sent to the email address for the provided user. Please check your email for your new password. Delivery may take several minutes.";
            var user = DB.SiteUsers.Where(u => u.Username == username).FirstOrDefault();
            var userNotExistMessage = string.Empty;
            if (user != null)
            {
                var scriptToAddRoleIfNotExists = @"INSERT INTO Roles(OrganizationId, SiteUserId, pvt_JsonData)
                     SELECT TOP(1) CASE WHEN Event_1.OrganizationID IS NULL THEN Event.OrganizationID ELSE Event_1.OrganizationID END AS OrganizationId, SiteUser.SiteUserId, '{""roles"":[""Student""]}' AS pvt_JsonData
                     FROM Event INNER JOIN
                     TransactionLog ON Event.EventId = TransactionLog.EventId RIGHT OUTER JOIN
                     SiteUser ON TransactionLog.BillingEmail = SiteUser.EmailAddress LEFT OUTER JOIN
                     Event AS Event_1 INNER JOIN
                     EventRegistration ON Event_1.EventId = EventRegistration.EventId ON SiteUser.SiteUserId = EventRegistration.SiteUserId LEFT OUTER JOIN
                     Roles AS Roles_1 ON SiteUser.SiteUserId = Roles_1.SiteUserId
                     WHERE(Roles_1.SiteUserId IS NULL) AND(SiteUser.EmailAddress = N'" + username + "')";
                
                // Script will add role if doesn't exists
                DB.Database.ExecuteSqlCommand(scriptToAddRoleIfNotExists);

                // Need to add code to generate random password (can get from the bshifter)
                // somtihng like DECLARE @temp varchar(MAX) = LEFT(CAST(NEWID() AS varchar(36)),8)
                var newPassword = Guid.NewGuid().ToString().Substring(0, Guid.NewGuid().ToString().IndexOf("-"));
                //user.SetPassword("training");
                 user.SetNewPassword(newPassword);
                // Needs to add new columns DateNewPasswordExpire and NewPassword
                DB.SaveChanges();
                MailController.SendMailForgotPassowrd(user,newPassword);
                if (!string.IsNullOrEmpty(user.SMSPhoneNumber))
                {
                    var smsBody = "";
                    if (string.IsNullOrEmpty(newPassword))
                        smsBody = "Please log in with your new password \"training\". You will be prompted to change your password upon login.";
                    else
                        smsBody = "Please log in with your new password \"" + newPassword + "\" . To login and reset your password please use the temporary password we have provided which is valid for 24 hours. You will be prompted to change your password upon login.";
                    
                    bool isSmsSent = SmsSender.SendSmsOnPhone(user.SMSPhoneNumber, smsBody);

                    if (isSmsSent)
                    {
                        alertMessage = "Notifications have been sent to the provided email address and phone number. Please check your email and SMS for your new password. Delivery may take several minutes.";
                    }
                }
            } else {
                alertMessage+= " Carefully check your email provided: '<b>" + username + " </b>'  if you do not have an account on this system you will not receive an email. Your mail provider may have greylisting which delays delivery or your spam/junk folder may contain the message.";
            }

            AlertHelpers.AddAlert(Session, new Alert(alertMessage));

            return Json(new { message = alertMessage });
        }

        public ActionResult changePassword(string username)
        {
            var user = DB.SiteUsers.Where(u => u.Username == username).FirstOrDefault();

            if (user != null)
            {
                user.SetPassword("training");
                DB.SaveChanges();
                MailController.SendMailForgotPassowrd(user);
            }

            AlertHelpers.AddAlert(Session, new Alert("An email has been sent to the email address for the provided user. Please check your email for your new password. Delivery may take several minutes."));

            return RedirectToAction("Index", "Account/Login");
        }

        [HttpPost]
        public ActionResult ForgotUsername(AccountLoginModel model)
        {

            var userName = model.Username;
            //Needs validation before progressing...
            if (Repository.SiteUser.GetUserByEmail(userName) != null)
            {
                var user = Repository.SiteUser.GetUserByEmail(userName);

                //string Url = user.SiteUserId.ToString() + "_" + DateTime.Now.ToString("MM/dd/yyyy");
                SmtpClient client = new SmtpClient();
                client.Host = ConfigurationManager.AppSettings["SystemEmailHost"];
                client.Timeout = 10000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["SystemEmailFrom"], ConfigurationManager.AppSettings["SystemEmailPassword"]);//"donotreply@exposuretrack.com", "donotreply5"

                MailMessage mm = new MailMessage(ConfigurationManager.AppSettings["SystemEmailFrom"], "" + user.EmailAddress + "", "Recover UserName",
                "Dear Customer your user name is : " + user.Username
                + "\n\n\nThanks"
                );

                try
                {
                    client.Send(mm);
                    this.AddAlert(new Alert("An email has been sent to the address associated with your username. Please check your email for your new password. Delivery may take several minutes.", AlertLevel.Info, "Email Sent!", FontAwesomeIcon.exclamation));

                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }
            }
            else //No such username found
            {
                this.AddAlert(new Alert("No such username found", AlertLevel.Danger, "Invalid Username", FontAwesomeIcon.exclamation));
            }

            return RedirectToAction("Index", "Account/Login");
        }

        public bool IsValidCaptcha()
        {
            string resp = Request["g-recaptcha-response"];
            var req = (HttpWebRequest)WebRequest.Create
                      ("https://www.google.com/recaptcha/api/siteverify?secret=" +System.Configuration.ConfigurationManager.AppSettings["RecaptchaPrivateKey"] + "&response=" + resp);
            using (WebResponse wResponse = req.GetResponse())
            {
                using (StreamReader readStream = new StreamReader(wResponse.GetResponseStream()))
                {
                    string jsonResponse = readStream.ReadToEnd();
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    // Deserialize Json
                    CaptchaResult data = js.Deserialize<CaptchaResult>(jsonResponse);
                    if (Convert.ToBoolean(data.success))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public class CaptchaResult
        {
            public string success { get; set; }
        }

    }
}
