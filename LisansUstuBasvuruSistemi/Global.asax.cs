using BiskaUtil;
using CaptchaMvc.Infrastructure;
using CaptchaMvc.Interface;
using CaptchaMvc.Models;
using DevExpress.XtraReports.Security;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.WebPages;
using System.Xml;

namespace LisansUstuBasvuruSistemi
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {

            AreaRegistration.RegisterAllAreas(); 
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BiskaUtil.Membership.OnRequireUserIdentity += Membership_OnRequireUserIdentity;
            BiskaUtil.SystemInformation.OnEvent += SystemInformation_OnEvent;
            Management.Update();

            Management.SistemDilleris = Management.GetDiller();
            Management.Enstitulers = Management.GetEnstituler();
            Management.Roles = Management.GetAllRoles();
            Management.Menulers = Management.GetAllMenu();


            bool OtomatikMailBilgilendirmeServisiniCalistir = SistemAyar.OtomatikMailBilgilendirmeServisiniCalistir.getAyar().toBooleanObj() ?? false;
            if (OtomatikMailBilgilendirmeServisiniCalistir)
            {
                ApplicationClock ap = new ApplicationClock();
                ap.Start();
            }
            ScriptPermissionManager.GlobalInstance = new ScriptPermissionManager(ExecutionMode.Unrestricted);
            DevExpress.XtraReports.Web.WebDocumentViewer.Native.WebDocumentViewerBootstrapper.SessionState = System.Web.SessionState.SessionStateBehavior.Disabled;
            DevExpress.XtraReports.Web.ASPxWebDocumentViewer.StaticInitialize();

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                    | SecurityProtocolType.Tls11
                    | SecurityProtocolType.Tls12
                    | SecurityProtocolType.Ssl3;



             
            try
            {
                DevExpress.XtraReports.Web.Native.ClientControls.Services.DefaultLoggingService.SetInstance(new MyLoggingService());
            }
            catch 
            {

            }


            var captchaManager = (DefaultCaptchaManager)CaptchaUtils.CaptchaManager;
            captchaManager.CharactersFactory = () => "my characters";
            captchaManager.PlainCaptchaPairFactory = length =>
            {
                string randomText = RandomText.Generate(("1234567890").ToUpper(), length);
                bool ignoreCase = true;
                return new KeyValuePair<string, ICaptchaValue>(Guid.NewGuid().ToString("N"), new StringCaptchaValue(randomText, randomText, ignoreCase));
            };

        }
      
        //protected void Application_Error(object sender, EventArgs e)
        //{
        //    var err = Server.GetLastError();
        //    if (HttpContext.Current.Response != null)
        //    {
        //        var sCode = HttpContext.Current.Response.StatusCode;
        //        if (sCode == 404 || sCode == 200)
        //        {
        //           // Response.Redirect("/PageNotFound/Index");
        //        }

        //    } 

        //}

        protected void Application_Error(object sender, EventArgs e)
        {

            Exception exception = Server.GetLastError();
          //  Management.SistemBilgisiKaydet("Application_Error: " + exception.ToExceptionMessage(), exception.ToExceptionStackTrace(), BilgiTipi.Hata);

            RouteData routeData = new RouteData();

            IController errorController = new LisansUstuBasvuruSistemi.Controllers.HomeController();
            if (exception == null)
            {
                routeData.Values.Add("controller", "Home");
                routeData.Values.Add("action", "Index");
            }
            else //It's an Http Exception, Let's handle it.
            {
               
                var errCode = HttpContext.Current.Response.StatusCode;
                if (errCode == HttpDurumKod.NotFound || errCode == HttpDurumKod.Unauthorized)
                {
                    var url = HttpContext.Current.Request.Url;
                    routeData.Values.Add("url", url);
                    routeData.Values.Add("ErrC", errCode);
                    errorController = new LisansUstuBasvuruSistemi.Controllers.AppEventController();
                    routeData.Values.Add("controller", "AppEvent");
                    routeData.Values.Add("action", "PageNotFound");
                    Response.TrySkipIisCustomErrors = true;
                    Server.ClearError();
                    errorController.Execute(new RequestContext(new HttpContextWrapper(Context), routeData));
                }
                else
                {
                    //routeData.Values.Add("controller", "Home");
                    //routeData.Values.Add("action", "Index");
                    errorController = new LisansUstuBasvuruSistemi.Controllers.AppEventController();
                    var url = HttpContext.Current.Request.Url;
                    routeData.Values.Add("url", url);
                    routeData.Values.Add("ErrC", errCode);
                    routeData.Values.Add("controller", "AppEvent");
                    routeData.Values.Add("action", "Error");
                    routeData.Values.Add("exception", exception);

                    Response.TrySkipIisCustomErrors = true;
                    Server.ClearError();
                    errorController.Execute(new RequestContext(new HttpContextWrapper(Context), routeData));
                }

            }



        }
        //protected void Application_BeginRequest(object sender, EventArgs e)
        //{
        //    var ipAdd2 = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
        //    var ipAdd1 = Request.ServerVariables["REMOTE_ADDR"];



        //    try
        //    {

        //        var FileName = DateTime.Now.ToString("yyyy-MM-dd") + "_Logs.txt";
        //        var _path = Server.MapPath("/LogS/" + FileName);

        //        if (!File.Exists(_path))
        //        {

        //            File.Create(_path).Dispose();
        //        }
        //        else
        //        {
        //            var ulr = Request.Url;
        //            StreamWriter write = new StreamWriter(_path, true);
        //            string UserName = "Guest";
        //            if (false && UserIdentity.Current != null && UserIdentity.Current.IsAuthenticated)
        //            {
        //                UserName = UserIdentity.Current.Name;
        //            }
        //            write.WriteLine("B:\t" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + ulr + "\t" + ipAdd1);
        //            write.Close();
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

        //protected void Application_EndRequest(Object sender, EventArgs e)
        //{
        //    var ipAdd2 = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
        //    var ipAdd1 = Request.ServerVariables["REMOTE_ADDR"]; 

        //    try
        //    {

        //        var FileName = DateTime.Now.ToString("yyyy-MM-dd") + "_Logs.txt";
        //        var _path = Server.MapPath("/LogS/" + FileName);
        //        if (!File.Exists(_path))
        //        { 
        //            File.Create(_path).Dispose();
        //        }
        //        else
        //        {
        //            var ulr = Request.Url;
        //            StreamWriter write = new StreamWriter(_path, true);
        //            string UserName = "Guest";
        //            if (UserIdentity.Current.IsAuthenticated)
        //            {
        //                UserName = UserIdentity.Current.Name;
        //            }
        //            write.WriteLine("E:\t" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + ulr + "\t" + ipAdd1); 
        //            write.Close();
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //    }


        //    //if (HttpContext.Current.Response.Status.StartsWith(HttpDurumKod.MovedTemporarily.ToString()))
        //    //{
        //    //    HttpContext.Current.Response.ClearContent();
        //    //    var url = HttpContext.Current.Request.Url;
        //    //    IController loginC = new LisansUstuBasvuruSistemi.Controllers.AccountController();
        //    //    RouteData routeData = new RouteData();
        //    //    var culture = "";
        //    //    var reqC = Request.RawUrl.Split('/').ToList();
        //    //    foreach (var item in reqC)
        //    //    { 
        //    //        if (item.IsContainsCulture())
        //    //        {
        //    //            culture = item.ToLower();
        //    //            break;
        //    //        }
        //    //    }
        //    //    Response.Clear();
        //    //    routeData.Values.Add("controller", "Account");
        //    //    routeData.Values.Add("action", "Login");
        //    //    routeData.Values.Add("ReturnUrl", url);
        //    //    routeData.Values.Add("Culture"
        //    //    loginC.Execute(new RequestContext(new HttpContextWrapper(Context), routeData));
        //    //    // Server.Execute("/account/login?ReturnUrl" + url);
        //    //}
        //}
        void SystemInformation_OnEvent(BiskaUtil.SystemInformation info)
        {
            Management.AddMessage(info);
        }

        void Membership_OnRequireUserIdentity(string UserName, ref BiskaUtil.UserIdentity userIdentity)
        {
            userIdentity = Management.GetUserIdentity(UserName);
        }
        protected void Application_AcquireRequestState(Object sender, EventArgs e)
        {
            BiskaUtil.UserIdentity.SetCurrent();
            if (true)
            {
                var pathCorolu = Request.Url.LocalPath;


                var session = HttpContext.Current.Session;
                var Ouser = HttpContext.Current.User;
                if (session != null)
                {
                    string browser = "";
                    string platform = "";
                    string version = "";
                    if (HttpContext.Current.Request.Browser.IsMobileDevice)
                    { platform = HttpContext.Current.Request.Browser.MobileDeviceManufacturer + " " + HttpContext.Current.Request.Browser.MobileDeviceModel; }
                    else { platform = HttpContext.Current.Request.Browser.Platform; }
                    browser = HttpContext.Current.Request.Browser.Browser;
                    version = HttpContext.Current.Request.Browser.Version;
                    //var q = HttpContext.Current.Request.UserAgent.ToString().toDeviceType();  

                    //var userAgent = HttpContext.Current.Request.UserAgent; 
                    var UniqueId = Session["UserId"].toStrObj();

                    if (UniqueId != null)
                    {
                        var usr = OnlineUsers.users.Where(p => p.UniqueId == UniqueId).FirstOrDefault();
                        if (usr != null)
                        {
                            if (User.Identity.IsAuthenticated)
                            {
                                var user = Management.GetUser();
                                usr.KullaniciID = user.KullaniciID;
                                usr.Name = user.Ad + " " + user.Soyad;
                                usr.UserName = user.KullaniciAdi;
                                usr.Platform = platform;
                                usr.Browser = browser;
                                usr.Version = version;
                                usr.KullaniciTipi = user.KullaniciTipAdi;
                                usr.ResimAdi = user.ResimAdi.toKullaniciResim();
                                usr.IsAuthenticated = true;
                            }
                            else
                            {
                                usr.Name = "Misafir";
                                usr.ResimAdi = "".toKullaniciResim();
                                usr.KullaniciTipi = "";
                                usr.Platform = platform;
                                usr.Browser = browser;
                                usr.Version = version;
                                usr.IsAuthenticated = false;
                            }
                        }
                    }
                }
            }

        }
        void Session_Start(object sender, EventArgs e)
        {
            if (true)
            {
                var UniqueId = Guid.NewGuid().ToString();
                Session["UserId"] = UniqueId;
                OnlineUsers.AddUser(UniqueId, null);
            }

            //StringBuilder strb = new StringBuilder();
            //strb.AppendFormat("User Agent: {0}{1}", Request.ServerVariables["http_user_agent"].ToString(), Environment.NewLine);
            //strb.AppendFormat("Browser: {0}{1}", Request.Browser.Browser.ToString(), Environment.NewLine);
            //strb.AppendFormat("Version: {0}{1}", Request.Browser.Version.ToString(), Environment.NewLine);
            //strb.AppendFormat("Major Version: {0}{1}", Request.Browser.MajorVersion.ToString(), Environment.NewLine);
            //strb.AppendFormat("Minor Version: {0}{1}", Request.Browser.MinorVersion.ToString(), Environment.NewLine);
            //strb.AppendFormat("Platform: {0}{1}", Request.Browser.Platform.ToString(), Environment.NewLine);
            //strb.AppendFormat("ECMA Script version: {0}{1}", Request.Browser.EcmaScriptVersion.ToString(), Environment.NewLine);
            //strb.AppendFormat("Type: {0}{1}", Request.Browser.Type.ToString(), Environment.NewLine);
            //strb.AppendFormat("-------------------------------------------------------------------------------{0}", Environment.NewLine);
            //strb.AppendFormat("ActiveX Controls: {0}{1}", Request.Browser.ActiveXControls.ToString(), Environment.NewLine);
            //strb.AppendFormat("Background Sounds: {0}{1}", Request.Browser.BackgroundSounds.ToString(), Environment.NewLine);
            //strb.AppendFormat("AOL: {0}{1}", Request.Browser.AOL.ToString(), Environment.NewLine);
            //strb.AppendFormat("Beta: {0}{1}", Request.Browser.Beta.ToString(), Environment.NewLine);
            //strb.AppendFormat("CDF: {0}{1}", Request.Browser.CDF.ToString(), Environment.NewLine);
            //strb.AppendFormat("ClrVersion: {0}{1}", Request.Browser.ClrVersion.ToString(), Environment.NewLine);
            //strb.AppendFormat("Cookies: {0}{1}", Request.Browser.Cookies.ToString(), Environment.NewLine);
            //strb.AppendFormat("Crawler: {0}{1}", Request.Browser.Crawler.ToString(), Environment.NewLine);
            //strb.AppendFormat("Frames: {0}{1}", Request.Browser.Frames.ToString(), Environment.NewLine);
            //strb.AppendFormat("Tables: {0}{1}", Request.Browser.Tables.ToString(), Environment.NewLine);
            //strb.AppendFormat("JavaApplets: {0}{1}", Request.Browser.JavaApplets.ToString(), Environment.NewLine);
            //strb.AppendFormat("JavaScript: {0}{1}", Request.Browser.JavaScript.ToString(), Environment.NewLine);
            //strb.AppendFormat("MSDomVersion: {0}{1}", Request.Browser.MSDomVersion.ToString(), Environment.NewLine);
            //strb.AppendFormat("TagWriter: {0}{1}", Request.Browser.TagWriter.ToString(), Environment.NewLine);
            //strb.AppendFormat("VBScript: {0}{1}", Request.Browser.VBScript.ToString(), Environment.NewLine);
            //strb.AppendFormat("W3CDomVersion: {0}{1}", Request.Browser.W3CDomVersion.ToString(), Environment.NewLine);
            //strb.AppendFormat("Win16: {0}{1}", Request.Browser.Win16.ToString(), Environment.NewLine);
            //strb.AppendFormat("Win32: {0}{1}", Request.Browser.Win32.ToString(), Environment.NewLine);
            //strb.AppendFormat("-------------------------------------------------------------------------------{0}", Environment.NewLine);
            //strb.AppendFormat("MachineName: {0}{1}", Environment.MachineName, Environment.NewLine);
            //strb.AppendFormat("OSVersion: {0}{1}", Environment.OSVersion, Environment.NewLine);
            //strb.AppendFormat("ProcessorCount: {0}{1}", Environment.ProcessorCount, Environment.NewLine);
            //strb.AppendFormat("UserName: {0}{1}", Environment.UserName, Environment.NewLine);
            //strb.AppendFormat("Version: {0}{1}", Environment.Version, Environment.NewLine);
            //strb.AppendFormat("UserInteractive: {0}{1}", Environment.UserInteractive, Environment.NewLine);
            //strb.AppendFormat("UserDomainName: {0}{1}", Environment.UserDomainName, Environment.NewLine);
        }
        void Session_End(object sender, EventArgs e)
        {
            if (true && Session["UserId"] != null)
            {
                var UniqueId = Session["UserId"].ToString();
                var oUser = OnlineUsers.GetByID(UniqueId);//.users.Where(p => p.UniqueId == UniqueId).FirstOrDefault();
                if (oUser != null && oUser.KullaniciID.HasValue)
                {
                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var kul = db.Kullanicilars.Where(p => p.KullaniciID == oUser.KullaniciID).FirstOrDefault();
                        if (kul != null)
                        {
                            kul.LastLogonDate = DateTime.Now;
                            db.SaveChanges();
                        }
                    }

                }
                OnlineUsers.RemoveUser(UniqueId);
            }
        }



    }
}
