using Entities.Entities;
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
            using (var entities = new LubsDbEntities())
            {
                var grupRolleris = entities.YetkiGrupRolleris.Where(p => p.YetkiGrupID == yetkiGrupId).ToList();

                var rolIDs = grupRolleris.Select(s => s.RolID).ToList();
                return entities.Rollers.Where(p => rolIDs.Contains(p.RolID)).ToList();


            }
        }
        public static List<CmbIntDto> CmbYetkiGruplari(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });

            using (var entities = new LubsDbEntities())
            {
                var data = entities.YetkiGruplaris.OrderBy(o => o.YetkiGrupAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.YetkiGrupID, Caption = item.YetkiGrupAdi });
                }
            }
            return dct;

        }
    }
}