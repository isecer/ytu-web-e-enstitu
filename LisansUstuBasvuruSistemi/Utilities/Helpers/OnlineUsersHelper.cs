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
        public int? KullaniciId { get; set; }
        public Guid? UserKey { get; set; }
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
        private static readonly List<OnlineUser> Users = null;
        public static int OnlineUserCount = 0;
        private static readonly object LockObject = new object();
        static OnlineUsersHelper(){
            Users = new List<OnlineUser>();
         }
        public static OnlineUser[] GetUsers => Users.AsReadOnly().ToArray();

        public static void AddUser(string userId, string ip)
        {

            Monitor.Enter(LockObject);
            try
            {
                var clientIp = ip ?? HttpContext.Current.Request.UserHostAddress;
                if (Users.All(p => p.UniqueId != userId))
                    Users.Add(new OnlineUser { Tc = "Misafir", Name = "Misafir", LoginTime = DateTime.Now, UniqueId = userId, Ip = clientIp });


                OnlineUserCount = Users.Count;
            }
            catch
            {
                // ignored
            }
            finally
            {
                Monitor.Exit(LockObject);
            }
        }
        public static void RemoveUser(string userId)
        {

            Monitor.Enter(LockObject);
            try
            {
                Users.RemoveAll(p => p.UniqueId == userId);
                OnlineUserCount = Users.Count;
            }
            catch
            {
                // ignored
            }
            finally
            {
                Monitor.Exit(LockObject);
            }
        }

        public static OnlineUser GetById(string uniqueId)
        {
            Monitor.Enter(LockObject);
            try
            {
                return Users.FirstOrDefault(p => p.UniqueId == uniqueId);
            }
            catch {
                return null;
            }
            finally
            {
                Monitor.Exit(LockObject);
            }
        }
    }
}