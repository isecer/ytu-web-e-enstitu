using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Models.ObsService;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Business
{
    public class MenulerBus
    {
        public static Menuler[] Menulers { get; set; }

        public static Menuler[] GetAllMenu()
        {
            if (MenulerBus.Menulers == null)
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    MenulerBus.Menulers = db.Menulers.OrderBy(o => o.SiraNo).ToArray();
                }
            }
            return MenulerBus.Menulers;
        }

        public static void UpdateMenus2()
        {
            var menuAttrs = Membership.Menus();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var err = new List<string>();
                var dbMenus = db.Menulers.ToArray();
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
                        db.Menulers.Add(yeniMenu);
                        if (attr.BagliRoller != null && attr.BagliRoller.Length > 0)
                        {
                            var dbRoller = db.Rollers.Where(p => attr.BagliRoller.Contains(p.RolAdi)).ToArray();
                            foreach (var dbRole in dbRoller)
                            {
                                yeniMenu.Rollers.Add(dbRole);
                            }

                        }
                        db.SaveChanges();
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
                            var dbRoller = db.Rollers.Where(p => attr.BagliRoller.Contains(p.RolAdi)).ToArray();
                            var nRols = dbRoller.Select(s => s.RolID).ToList();
                            var yeni = dbmenu.Rollers.Where(a => !nRols.Contains(a.RolID)).ToList(); 
                            foreach (var dbRole in yeni)
                            {
                                dbmenu.Rollers.Add(dbRole);

                            }
                        }
                        db.SaveChanges();
                    }
                }
             
            }
        }
    }
}