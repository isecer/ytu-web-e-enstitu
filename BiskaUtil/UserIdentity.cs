using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace BiskaUtil
{
    [Serializable()]
    public class UserIdentity : IIdentity
    {
        public enum GenderType { None = 0, Male = 1, Female = 2 }

        public string AuthenticationType => "Forms";
        private bool _isAuthenticated = true;
        public bool IsAuthenticated => _isAuthenticated;
        private string _userName;
        public string Name => _userName;

        private List<string> _roles = new List<string>();
        private Dictionary<string, object> _informations = new Dictionary<string, object>();
        public int Id { get; set; }
        public int PersonelId { get; set; }
        public string NameSurname { get; set; }
        public string Description { get; set; }
        public bool IsActiveDirectoryUser { get; set; }
        public bool? IsActiveDirectoryImpersonateWorking { get; set; }
        public string ImagePath { get; set; }
        public string Domain { get; set; }
        public string Password { get; set; }
        public string SeciliEnstituKodu { get; set; }
        public List<string> EnstituKods { get; set; }
        public int KullaniciTipId { get; set; }
        public Dictionary<string, object> Informations
        {
            get => _informations;
            set => _informations = value;
        }

        public List<string> Roles
        {
            get => _roles;
            set => _roles = value;
        }
        public UserIdentity(string name)
        {
            _userName = name;
        }
        public UserIdentity(string name, bool isAuthenticated)
        {
            _userName = name;
            _isAuthenticated = isAuthenticated;
        }
        public UserIdentity(string name, string[] roles)
        {
            _userName = name;
            if (roles != null)
                this.Roles.AddRange(roles);
        }
        public UserPrincipal ToPrincipal()
        {
            var prensip = new UserPrincipal(this);
            return prensip;
        }
        public bool IsAdmin { get; set; }
        public bool HasToChahgePassword { get; set; }
        public bool IsSuperAdmin { get; set; }
        public static string Ip
        {
            get
            {
                try
                {
                    var ip = "";
                    var forwarderFor = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                    if (string.IsNullOrWhiteSpace(forwarderFor) || forwarderFor.ToLower().Contains("unknown"))
                        ip = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                    else if (forwarderFor.Contains(","))
                    {
                        ip = forwarderFor.Substring(0, forwarderFor.IndexOf(",", StringComparison.Ordinal));
                    }
                    else if (forwarderFor.Contains(";"))
                    {
                        ip = forwarderFor.Substring(0, forwarderFor.IndexOf(";", StringComparison.Ordinal));
                    }
                    else ip = forwarderFor;
                    var len = ip.Length > 30 ? 30 : ip.Length;
                    return ip.Substring(0, len).Trim();
                }
                catch
                {
                    return "";
                }
            }
        }

        public void Impersonate()
        {
            #region Impersonate

            HttpContext.Current.User = this.ToPrincipal(); 
            #endregion
        }


        public static void SetUserIdentityOnSession(HttpSessionStateBase session)
        {
            if (session == null)
            {
                SetCurrent();
            }
            else
            {
                if (HttpContext.Current != null && HttpContext.Current.User != null && HttpContext.Current.User.Identity.IsAuthenticated)
                {
                    UserIdentity kimlik = null;
                    if ((session["UserIdentity"] != null))
                    {
                        kimlik = (UserIdentity)session["UserIdentity"];
                        kimlik.Impersonate();
                    }
                    else if (HttpContext.Current.User != null)
                    {
                        if (!(HttpContext.Current.User.Identity is NotAuthenticatedUser))
                        {
                            //kimlik = AccountModel.GetKimlik(HttpContext.Current.User.Identity.Name);
                            kimlik = Membership.GetUserIdentity(HttpContext.Current.User.Identity.Name);
                            if (kimlik.Id > 0)
                            {
                                kimlik.Impersonate();
                                session["Kimlik"] = kimlik;
                            }
                            else
                            {
                                session["Kimlik"] = null;
                                FormsAuthenticationUtil.SignOut();
                                if (HttpContext.Current != null)
                                {
                                    try
                                    {
                                        if (HttpContext.Current.Session != null) HttpContext.Current.Session.Abandon();
                                        // clear authentication cookie
                                        HttpCookie cookie1 = new HttpCookie(FormsAuthentication.FormsCookieName, "");
                                        cookie1.Expires = DateTime.Now.AddYears(-1);
                                        HttpContext.Current.Response.Cookies.Add(cookie1);

                                        // clear session cookie (not necessary for your current problem but i would recommend you do it anyway)
                                        HttpCookie cookie2 = new HttpCookie("ASP.NET_SessionId", "");
                                        cookie2.Expires = DateTime.Now.AddYears(-1);
                                        HttpContext.Current.Response.Cookies.Add(cookie2);
                                    }
                                    catch { }
                                }
                                HttpContext.Current.User = new GenericPrincipal(new NotAuthenticatedUser(), new string[0]);
                                //IPrincipal user = HttpContext.Current.User;
                            }
                        }
                    }
                }
            }
        }

        public static void SetCurrent()
        {
            if (HttpContext.Current != null && HttpContext.Current.User != null && HttpContext.Current.User.Identity.IsAuthenticated)
            {
                UserIdentity kimlik = null;
                HttpSessionState session = HttpContext.Current.Session;
                if ((HttpContext.Current.Session != null) && (HttpContext.Current.Session["UserIdentity"] != null))
                {
                    kimlik = (UserIdentity)session["UserIdentity"];
                    kimlik.Impersonate();
                }
                else if (HttpContext.Current.Session != null && HttpContext.Current.User != null)
                {
                    if (!(HttpContext.Current.User.Identity is NotAuthenticatedUser))
                    {
                        //kimlik = AccountModel.GetKimlik(HttpContext.Current.User.Identity.Name);
                        kimlik = Membership.GetUserIdentity(HttpContext.Current.User.Identity.Name);
                        if (kimlik.Id > 0)
                        {
                            kimlik.Impersonate();
                            session["UserIdentity"] = kimlik;
                        }
                        else
                        {
                            session["UserIdentity"] = null;
                            FormsAuthenticationUtil.SignOut();
                            if (HttpContext.Current != null)
                            {
                                try
                                {
                                    if (HttpContext.Current.Session != null) HttpContext.Current.Session.Abandon();
                                    // clear authentication cookie
                                    HttpCookie cookie1 = new HttpCookie(FormsAuthentication.FormsCookieName, "");
                                    cookie1.Expires = DateTime.Now.AddYears(-1);
                                    HttpContext.Current.Response.Cookies.Add(cookie1);

                                    // clear session cookie (not necessary for your current problem but i would recommend you do it anyway)
                                    HttpCookie cookie2 = new HttpCookie("ASP.NET_SessionId", "");
                                    cookie2.Expires = DateTime.Now.AddYears(-1);
                                    HttpContext.Current.Response.Cookies.Add(cookie2);
                                }
                                catch { }
                            }
                            HttpContext.Current.User = new GenericPrincipal(new NotAuthenticatedUser(), new string[0]);
                            //IPrincipal user = HttpContext.Current.User;
                        }
                    }
                }
            }
        }



        public bool IsYetkiliTij { get; set; }


        public static UserIdentity Current
        {
            get
            {
                if (HttpContext.Current.User.Identity is UserIdentity)
                    return (UserIdentity)HttpContext.Current.User.Identity;
                if (HttpContext.Current != null && HttpContext.Current.User != null && HttpContext.Current.User.Identity.IsAuthenticated)
                    return Membership.GetUserIdentity(HttpContext.Current.User.Identity.Name);
                return new UserIdentity("None", false);
                //return AccountModel.GetKimlik(HttpContext.Current.User.Identity.Name);
            }
        }
    }
    [Serializable()]
    public class UserPrincipal : IPrincipal
    {
        private UserIdentity _kimlik;
        public IIdentity Identity => _kimlik;

        public bool IsInRole(string role)
        {
            return _kimlik.Roles.IndexOf(role) >= 0;
        }
        internal UserPrincipal(UserIdentity kimlik)
        {
            this._kimlik = kimlik;
        }
    }
}