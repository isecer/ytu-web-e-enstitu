using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Configuration;

namespace BiskaUtil
{
    public sealed class WinImpersonate
    {
        public const int LOGON32_LOGON_INTERACTIVE = 2;
        public const int LOGON32_PROVIDER_DEFAULT = 0;

        WindowsImpersonationContext impersonationContext;

        [DllImport("advapi32.dll")]
        public static extern int LogonUserA(string lpszUserName,
            string lpszDomain,
            String lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            ref IntPtr phToken);
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int DuplicateToken(IntPtr hToken,
            int impersonationLevel,
            ref IntPtr hNewToken);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool RevertToSelf();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool CloseHandle(IntPtr handle);
 

        public WindowsIdentity GetWindowsIdentity(string userName, string password)
        {
            return GetWindowsIdentity(userName, "", password);
        }
        public WindowsIdentity GetWindowsIdentity(string userName, string domain, string password)
        {
            return GetWindowsIdentity(userName, domain, password, out _);
        }
        public WindowsIdentity GetWindowsIdentity(string userName, string domain, string password,out Exception errorx)
        {
            errorx = null;
            try
            {
                IntPtr token = IntPtr.Zero;
                IntPtr tokenDuplicate = IntPtr.Zero;

                if (RevertToSelf())
                {
                    if (LogonUserA(userName, domain, password, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, ref token) != 0)
                    {
                        if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
                        {
                            var tempWindowsIdentity = new WindowsIdentity(tokenDuplicate);
                            return tempWindowsIdentity;
                        }
                    }
                }
                if (token != IntPtr.Zero)
                    CloseHandle(token);
                if (tokenDuplicate != IntPtr.Zero)
                    CloseHandle(tokenDuplicate);
                return null;
            }
            catch (Exception error)
            {
                errorx = error;
                return null;
            }
        }
         
    }
}