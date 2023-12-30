using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{
    public class GlobalSistemSetting
    {
        public static string GetRoot()
        {
            var root = HttpRuntime.AppDomainAppVirtualPath;
            root = root.EndsWith("/") ? root : root + "/";
            return root;
        }

        public static string Tuz => "@BİSKAmcumu";
        public static int UniversiteYtuKod => 67;
        public static int SystemDefaultAdminKullaniciId => 1; 
        public static int PageTableRowSize = 15;
    }
}