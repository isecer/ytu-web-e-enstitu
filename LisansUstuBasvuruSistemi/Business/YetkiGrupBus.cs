using LisansUstuBasvuruSistemi.Models;
using System.Collections.Generic;
using System.Linq;
using LisansUstuBasvuruSistemi.Utilities.Dtos;

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
        public static List<CmbIntDto> CmbYetkiGruplari(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.YetkiGruplaris.OrderBy(o => o.YetkiGrupAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.YetkiGrupID, Caption = item.YetkiGrupAdi });
                }
            }
            return dct;

        }
    }
}