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
    public class RollerBus
    {
        public static Roller[] Roles { get; set; }

        public static Roller[] GetAllRoles()
        {
            if (RollerBus.Roles == null)
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    RollerBus.Roles = db.Rollers.Include("Menulers").ToArray();
                }
            }
            return RollerBus.Roles;
        }

        public static void UpdateRoles2()
        {
            var roleAttrs = Membership.Roles();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var dbRoller = db.Rollers.ToArray();
                foreach (var attr in roleAttrs)
                {
                    var dbrole = dbRoller.FirstOrDefault(p => p.RolID == attr.RolID);

                    if (dbrole == null)
                    {
                        db.Rollers.Add(new Roller
                        {
                            RolID = attr.RolID,
                            GorunurAdi = attr.GorunurAdi,
                            Aciklama = attr.Aciklama,
                            Kategori = attr.Kategori,
                            RolAdi = attr.RolAdi
                        });
                    }
                    else
                    {
                        dbrole.RolID = attr.RolID;
                        dbrole.GorunurAdi = attr.GorunurAdi;
                        dbrole.Aciklama = attr.Aciklama;
                        dbrole.Kategori = attr.Kategori;
                        dbrole.RolAdi = attr.RolAdi;
                    }
                    db.SaveChanges();
                }
            }
        }
    }
}