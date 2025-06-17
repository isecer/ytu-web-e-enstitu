using BiskaUtil;
using CaptchaMvc.Infrastructure;
using CaptchaMvc.Interface;
using CaptchaMvc.Models;
using DevExpress.XtraReports.Security;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using LisansUstuBasvuruSistemi.Utilities.ApplicationTasks;
using LisansUstuBasvuruSistemi.Controllers;

namespace LisansUstuBasvuruSistemi
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            Membership.OnRequireUserIdentity += Membership_OnRequireUserIdentity;
            SystemInformation.OnEvent += SystemInformation_OnEvent;
             
            SistemBilgilendirmeBus.SistemBilgisiKaydet("Application_Start()", ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Bilgi);
           
            RollerBus.UpdateRoles();
            MenulerBus.UpdateMenus();

            EnstituBus.Enstitulers = EnstituBus.GetEnstituler();
            RollerBus.Roles = RollerBus.GetAllRoles();
            MenulerBus.Menulers = MenulerBus.GetAllMenu(); 

            MailTaskRunner.Start();
            ObsStudentControlTaskRunner.Start();

            ScriptPermissionManager.GlobalInstance = new ScriptPermissionManager(ExecutionMode.Unrestricted);
            DevExpress.XtraReports.Web.WebDocumentViewer.Native.WebDocumentViewerBootstrapper.SessionState = System.Web.SessionState.SessionStateBehavior.Disabled;
            DevExpress.XtraReports.Web.ASPxWebDocumentViewer.StaticInitialize();
     
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                    | SecurityProtocolType.Tls11
                    | SecurityProtocolType.Tls12
                    | SecurityProtocolType.Ssl3;


            var captchaManager = (DefaultCaptchaManager)CaptchaUtils.CaptchaManager;
            captchaManager.CharactersFactory = () => "my characters";
            captchaManager.PlainCaptchaPairFactory = length =>
            {
                string randomText = RandomText.Generate(("1234567890").ToUpper(), length);
                return new KeyValuePair<string, ICaptchaValue>(Guid.NewGuid().ToString("N"), new StringCaptchaValue(randomText, randomText, true));
            };

        }
        void Application_BeginRequest(object sender, EventArgs e)
        {
            //try
            //{
            //    var request = HttpContext.Current.Request;
            //    var url = request.Url.AbsoluteUri;
            //    var method = request.HttpMethod;
            //    var ip = request.UserHostAddress;
            //    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            //    var logMessage = string.Format("{0} {1} from {2}", method, url, ip);
            //    var logFilePath = @"C:\inetpub\wwwroot\e-enstitu.yildiz.edu.tr\Log.txt";
            //    using (StreamWriter writer = File.AppendText(logFilePath))
            //    {
            //        writer.WriteLine(string.Format("{0} | {1} ", timestamp, logMessage));
            //    }
            //}
            //catch (Exception exception)
            //{
            //}

            //string dosyaYolu = @"C:\inetpub\wwwroot\LUBS\blockIP.txt";
            //if (File.Exists(dosyaYolu))
            //{
            //    List<string> satirlar = new List<string>();

            //    using (StreamReader sr = new StreamReader(dosyaYolu))
            //    {
            //        string satir;
            //        while ((satir = sr.ReadLine()) != null)
            //        {
            //            satirlar.Add(satir);
            //        }
            //    }
            //    satirlar = satirlar.Where(p => !p.IsNullOrWhiteSpace()).ToList();
            //    string ipAddress = Request.UserHostAddress;
            //    if (satirlar.Any() && satirlar.Contains(ipAddress))
            //    {
            //        Response.StatusCode = 403;
            //        Response.End();
            //    }
            //}
        }

        protected void Application_Error(object sender, EventArgs e)
        {

            var exception = Server.GetLastError();
            //  Management.SistemBilgisiKaydet("Application_Error: " + exception.ToExceptionMessage(), exception.ToExceptionStackTrace(), BilgiTipi.Hata);

            var routeData = new RouteData();

            if (exception == null)
            {
                routeData.Values.Add("controller", "Home");
                routeData.Values.Add("action", "Index");
            }
            else //It's an Http Exception, Let's handle it.
            {

                var errCode = HttpContext.Current.Response.StatusCode;
                IController errorController;
                if (errCode == HttpDurumKodEnum.NotFound || errCode == HttpDurumKodEnum.Unauthorized || (exception.Message.Contains("The controller for path") && exception.Message.Contains("was not found or does not implement IController")))
                {
                    var url = HttpContext.Current.Request.Url;
                    routeData.Values.Add("url", url);
                    routeData.Values.Add("ErrC", errCode);
                    errorController = new Controllers.AppEventController();
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
                    errorController = new Controllers.AppEventController();
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
    





        //protected void Application_EndRequest(Object sender, EventArgs e)
        //{
        //    var ipAdd2 = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
        //    var ipAdd1 = Request.ServerVariables["REMOTE_ADDR"]; 

        //    try
        //    {

        //        var FileName = DateTime.Now.ToFormatDate() + "_Logs.txt";
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
        void SystemInformation_OnEvent(SystemInformation info)
        {
            SistemBilgilendirmeBus.SistemBilgisiKaydet(info.Message, info.StackTrace, (byte)info.InfoType);
        }

        private static void Membership_OnRequireUserIdentity(string userName, ref UserIdentity userIdentity)
        {
            userIdentity = UserBus.GetUserIdentity(userName);
        }
        protected void Application_AcquireRequestState(object sender, EventArgs e)
        {
            UserIdentity.SetCurrent();
            if (true)
            {
                var session = HttpContext.Current.Session;
                if (session == null) return;
                string platform;
                if (HttpContext.Current.Request.Browser.IsMobileDevice)
                {
                    platform = HttpContext.Current.Request.Browser.MobileDeviceManufacturer + " " + HttpContext.Current.Request.Browser.MobileDeviceModel;
                }
                else
                {
                    platform = HttpContext.Current.Request.Browser.Platform;
                }
                var uniqueId = Session["UserId"].ToStrObj();

                if (uniqueId == null) return;
                var usr = OnlineUsersHelper.GetUsers.FirstOrDefault(p => p.UniqueId == uniqueId);
                if (usr == null) return;
                if (User.Identity.IsAuthenticated)
                {
                    var user = UserBus.GetUser();
                    usr.KullaniciId = user.KullaniciID;
                    usr.UserKey = user.UserKey;
                    usr.Name = user.Ad + " " + user.Soyad;
                    usr.UserName = user.KullaniciAdi;
                    usr.Platform = platform;
                    usr.Browser = HttpContext.Current.Request.Browser.Browser;
                    usr.Version = HttpContext.Current.Request.Browser.Version;
                    usr.KullaniciTipi = user.KullaniciTipAdi;
                    usr.ResimAdi = user.ResimAdi.ToKullaniciResim();
                    usr.IsAuthenticated = true;
                }
                else
                {
                    usr.Name = "Misafir";
                    usr.ResimAdi = "".ToKullaniciResim();
                    usr.KullaniciTipi = "";
                    usr.Platform = platform;
                    usr.Browser = HttpContext.Current.Request.Browser.Browser;
                    usr.Version = HttpContext.Current.Request.Browser.Version;
                    usr.IsAuthenticated = false;
                }
            }

        }
        void Session_Start(object sender, EventArgs e)
        {

            var uniqueId = Guid.NewGuid().ToString();
            Session["UserId"] = uniqueId;
            OnlineUsersHelper.AddUser(uniqueId, null);


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
            if (Session["UserId"] == null) return;
            var uniqueId = Session["UserId"].ToString();
            var oUser = OnlineUsersHelper.GetById(uniqueId);
            if (oUser?.KullaniciId != null)
            {
                using (var entities = new LubsDbEntities())
                {
                    var kul = entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == oUser.KullaniciId);
                    if (kul != null)
                    {
                        kul.LastLogonDate = DateTime.Now;
                        entities.SaveChanges();
                    }
                }
            }
            OnlineUsersHelper.RemoveUser(uniqueId);
        }



    }
}
