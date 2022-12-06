using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public class OnlineUser
    {
        public bool IsAuthenticated { get; set; }
        public int? KullaniciID { get; set; }
        public string Tc { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Platform { get; set; }
        public string Browser { get; set; }
        public string Version { get; set; }
        public DateTime LoginTime { get; set; }
        public string KullaniciTipi { get; set; }
        public string UniqueId { get; set; }
        public string ResimAdi { get; set; }
        public string Ip { get; set; } 
    }
    public static class OnlineUsersHelper
    {
        static readonly List<OnlineUser> _users = null;
        public static int OnlineUserCount = 0;
        static object lockObject = new object();
        static OnlineUsersHelper(){
            _users = new List<OnlineUser>();
         }
        public static OnlineUser[] users { get {
                return _users.AsReadOnly().ToArray();
            } }
        public static void AddUser(string UserId, string ip)
        {

            Monitor.Enter(lockObject);
            try
            {
                var clientIP = ip ?? HttpContext.Current.Request.UserHostAddress;
                if (_users.Where(p => p.UniqueId == UserId).Count() == 0)
                    _users.Add(new OnlineUser { Tc = "Misafir", Name = "Misafir", LoginTime = DateTime.Now, UniqueId = UserId, Ip = clientIP });


                OnlineUserCount = _users.Count;
            }
            catch { }
            finally
            {
                Monitor.Exit(lockObject);
            }
        }
        public static void RemoveUser(string UserId)
        {

            Monitor.Enter(lockObject);
            try
            {
                _users.RemoveAll(p => p.UniqueId == UserId);
                OnlineUserCount = _users.Count;
            }
            catch { }
            finally
            {
                Monitor.Exit(lockObject);
            }
        }

        public static OnlineUser GetByID(string UniqueId)
        {
            Monitor.Enter(lockObject);
            try
            {
                return _users.Where(p => p.UniqueId == UniqueId).FirstOrDefault();
            }
            catch {
                return null;
            }
            finally
            {
                Monitor.Exit(lockObject);
            }
        }
    }
}