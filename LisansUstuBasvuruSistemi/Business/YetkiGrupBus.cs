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
    public class YetkiGrupBus
    {
        public static List<Roller> GetYetkiGrupRoles(int yetkiGrupId)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var grupRolleris = db.YetkiGrupRolleris.Where(p => p.YetkiGrupID == yetkiGrupId).ToList();

                var rolIDs = grupRolleris.Select(s => s.RolID).ToList();
                return db.Rollers.Where(p => rolIDs.Contains(p.RolID)).ToList();


            }
        }
    }
}