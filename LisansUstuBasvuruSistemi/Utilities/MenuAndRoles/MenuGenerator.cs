using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.MenuAndRoles
{
    public static class MenuGenerator
    {
        public static IHtmlString GetMenuIHtml(HttpRequestBase request)
        {
            var root = GlobalSistemSetting.GetRoot();
            var absolutePath = request.Url.AbsolutePath.TrimStart('/'); // Başındaki / işaretini kaldırır 
            var enstituKisaAdi = absolutePath.IndexOf('/') >= 0 ? absolutePath.Substring(0, absolutePath.IndexOf('/')).ToLower() : "fbe";
            var enstituKod = EnstituBus.GetSelectedEnstitu(enstituKisaAdi); 
            root += (EnstituBus.IsContainsEnstitu(enstituKisaAdi) ? enstituKisaAdi : "fbe") + "/";
             

          
            var allMenus = MenulerBus.Menulers.Where(p => p.YetkiliEnstitu == null || p.YetkiliEnstitu.Contains(enstituKod)).ToList();
            var userAllMenus = UserBus.GetUserMenus().ToList();

            IEnumerable<Menuler> eklenecekler;
            do
            {
                var bagliMenuIds = userAllMenus.Where(p => p.BagliMenuID.HasValue && p.BagliMenuID.Value > 0).Select(s => s.BagliMenuID.Value).ToArray();
                var allMenuIds = userAllMenus.Select(s => s.MenuID).ToArray();
                var eksikOlanMenuIds = bagliMenuIds.Except(allMenuIds);
                eklenecekler = allMenus.Where(p => eksikOlanMenuIds.Contains(p.MenuID)).ToArray();
                userAllMenus.AddRange(eklenecekler);
            }
            while (eklenecekler.Any());
            userAllMenus = userAllMenus.OrderBy(o => o.SiraNo).ToList();

            var menuStringBuilder = new System.Text.StringBuilder();
            Func<int?, bool> fxMenu = null; 
            fxMenu = parentId =>
            {
                var filteredMenus = parentId.HasValue ? userAllMenus.Where(p => p.BagliMenuID == parentId) : userAllMenus.Where(p => p.BagliMenuID == 0);
                foreach (var menu in filteredMenus)
                {
                    var hassubmenu = userAllMenus.Any(a => a.BagliMenuID == menu.MenuID);
                    var menuAd = parentId.HasValue ? menu.MenuAdi : $"<span class='xn-text'>{menu.MenuAdi}</span>";
                    var liClassAttributeValue = hassubmenu ? "class='xn-openable'" : "";
                    var aUrlAttributeValue = root + menu.MenuUrl.ToLower().Replace("ı", "i");
                    var aHrefAttributeValue = !menu.AuthenticationControl.IsNullOrWhiteSpace() ? "javascript:void(0);" : aUrlAttributeValue;
                    var aOnclickEventValue = !menu.AuthenticationControl.IsNullOrWhiteSpace() ? $"onclick='{menu.AuthenticationControl}'" : "";

                    menuStringBuilder.AppendLine($"<li {liClassAttributeValue}>");
                    menuStringBuilder.AppendLine($"<a href='{aHrefAttributeValue}' {aOnclickEventValue} menuurl='{aUrlAttributeValue}'>");
                    menuStringBuilder.AppendLine($"<span class='{menu.MenuCssClass}'></span> {menuAd}");
                    menuStringBuilder.AppendLine("</a>");

                    if (hassubmenu)
                    {
                        menuStringBuilder.AppendLine("<ul>");
                        fxMenu(menu.MenuID);
                        menuStringBuilder.AppendLine("</ul>");
                    }

                    menuStringBuilder.AppendLine("</li>");
                }
                return true;
            };

            fxMenu(null);
            return new HtmlString(menuStringBuilder.ToString());
        } 
    }
}