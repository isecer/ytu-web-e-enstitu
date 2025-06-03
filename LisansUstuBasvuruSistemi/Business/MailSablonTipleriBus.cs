using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using System.Collections.Generic;
using System.Linq;

namespace LisansUstuBasvuruSistemi.Business
{
    public class MailSablonTipleriBus
    {


        public static List<CmbIntDto> GetCmbMailSablonlari(string enstituKodu, bool bosSecimVar = false, bool? sistemMailFiltre = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.MailSablonlaris.Where(p => p.EnstituKod == enstituKodu && p.IsAktif && p.MailSablonTipleri.SistemMaili == (sistemMailFiltre ?? p.MailSablonTipleri.SistemMaili)).OrderBy(o => o.SablonAdi).ToList();
                dct.AddRange(data.Select(item => new CmbIntDto { Value = item.MailSablonlariID, Caption = item.SablonAdi }));
            }

            return dct;

        }

        public static List<CmbIntDto> GetCmbMailSablonTipleri(string enstituKod, bool? sistemMaili = null, bool bosSecimVar = false, bool? isOlusturulmayanlar = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var qdata = entities.MailSablonTipleris.AsQueryable();
                if (sistemMaili.HasValue) qdata = qdata.Where(p => p.SistemMaili == sistemMaili.Value);
                if (isOlusturulmayanlar.HasValue && isOlusturulmayanlar == true)
                    qdata = qdata.Where(p => p.MailSablonlaris.All(a => a.EnstituKod != enstituKod));
                var data = qdata.OrderBy(o => o.SablonTipAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MailSablonTipID, Caption = item.SablonTipAdi });
                }
            }
            return dct;

        }
    }

}