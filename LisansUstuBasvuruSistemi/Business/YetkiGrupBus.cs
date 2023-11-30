using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using System.Collections.Generic;
using System.Linq;

namespace LisansUstuBasvuruSistemi.Business
{
    public class YetkiGrupBus
    {
        public const int TezKontrolYetkiGrupId = 13;
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