using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using System.Collections.Generic;
using System.Linq;

namespace LisansUstuBasvuruSistemi.Business
{
    public class YaziSablonTipleriBus
    {


        public static List<CmbIntDto> GetCmbYaziSablonlari(string enstituKodu, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.YaziSablonlaris.Where(p => p.EnstituKod == enstituKodu && p.IsAktif).OrderBy(o => o.Konu).ToList();
                dct.AddRange(data.Select(item => new CmbIntDto { Value = item.YaziSablonlariID, Caption = item.Konu }));
            }

            return dct;

        }

        public static List<CmbIntDto> GetCmbYaziSablonTipleri(string enstituKod, bool bosSecimVar = false, bool? isOlusturulmayanlar = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.YaziSablonTipleris.Where(p => !isOlusturulmayanlar.HasValue || p.YaziSablonlaris.All(a => a.EnstituKod != enstituKod) == isOlusturulmayanlar).OrderBy(o => o.SablonTipAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.YaziSablonTipID, Caption = item.SablonTipAdi });
                }
            }
            return dct;

        }
    }

}