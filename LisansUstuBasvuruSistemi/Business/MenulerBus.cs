using System.Linq;
using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Business
{
    public class MenulerBus
    {
        public static Menuler[] Menulers { get; set; }

        public static Menuler[] GetAllMenu()
        {
            if (Menulers != null) return Menulers;
            using (var entities = new LubsDbEntities())
            {
                Menulers = entities.Menulers.Include("Rollers").OrderBy(o => o.SiraNo).ToArray();
            }
            return Menulers;
        }

        public static void UpdateMenus()
        {
            var menuAttrs = Membership.Menus();
            var menuKeyGroup = menuAttrs.GroupBy(g => g.MenuID).Select(s => new { s.Key, Count = s.Count(), Menus = s.Select(sr => sr.MenuAdi).ToList() });
            var duplicateKeys = menuKeyGroup.Where(p => p.Count > 1).ToList();

            if (duplicateKeys.Any())
            {

                var duplicateStringList = duplicateKeys.Select(s => s.Key + " => " + string.Join(",", s.Menus)).ToList();
                var duplicateString = string.Join("<br/>", duplicateStringList);
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Sistem Menüleri güncellenirken Unique olmayan roll id bilgilerine rastlandı! \r\n " + duplicateString, ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Kritik);
                return;
            }
            using (var entities = new LubsDbEntities())
            {
                var dbMenus = entities.Menulers.ToArray();
                foreach (var attr in menuAttrs)
                {
                    var dbmenu = dbMenus.FirstOrDefault(p => p.MenuID == attr.MenuID);
                    if (dbmenu == null)
                    {
                        var yeniMenu = new Menuler
                        {
                            MenuID = attr.MenuID,
                            MenuUrl = attr.MenuUrl,
                            BagliMenuID = attr.BagliMenuID,
                            MenuAdi = attr.MenuAdi,
                            MenuCssClass = attr.MenuCssClass,
                            MenuIconUrl = attr.MenuIconUrl,
                            DilCeviriYap = attr.DilCeviriYap,
                            YetkisizErisim = attr.YetkisizErisim,
                            YetkiliEnstitu = attr.YetkiliEnstituler,
                            AuthenticationControl = attr.AuthenticationControl,
                            SiraNo = attr.SiraNo
                        };
                        entities.Menulers.Add(yeniMenu);
                        if (attr.BagliRoller != null && attr.BagliRoller.Length > 0)
                        {
                            var dbRoller = entities.Rollers.Where(p => attr.BagliRoller.Contains(p.RolAdi)).ToArray();
                            foreach (var dbRole in dbRoller)
                            {
                                yeniMenu.Rollers.Add(dbRole);
                            }

                        }
                        entities.SaveChanges();
                    }
                    else
                    {
                        dbmenu.MenuUrl = attr.MenuUrl;
                        dbmenu.BagliMenuID = attr.BagliMenuID;
                        dbmenu.MenuAdi = attr.MenuAdi;
                        dbmenu.MenuCssClass = attr.MenuCssClass;
                        dbmenu.MenuIconUrl = attr.MenuIconUrl;
                        dbmenu.DilCeviriYap = attr.DilCeviriYap;
                        dbmenu.YetkisizErisim = attr.YetkisizErisim;
                        dbmenu.YetkiliEnstitu = attr.YetkiliEnstituler;
                        dbmenu.AuthenticationControl = attr.AuthenticationControl;
                        dbmenu.SiraNo = attr.SiraNo;
                        if (attr.BagliRoller != null && attr.BagliRoller.Length > 0)
                        {
                            var dbRoller = entities.Rollers.Where(p => attr.BagliRoller.Contains(p.RolAdi)).ToArray();
                            var yeni = dbRoller.Where(p => dbmenu.Rollers.All(a => a.RolID != p.RolID)).ToList();
                            foreach (var yeniRol in yeni)
                            {
                                dbmenu.Rollers.Add(yeniRol);
                            }
                        }
                    }
                }

                var silinenMenuler = dbMenus.Where(p => menuAttrs.All(a => a.MenuID != p.MenuID)).ToList();

                foreach (var menu in silinenMenuler)
                {
                    entities.Menulers.Remove(menu);

                }



                entities.SaveChanges();
                SistemBilgilendirmeBus.SistemBilgisiKaydet("UpdateMenus", ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Bilgi);
            }
        }
    }
}