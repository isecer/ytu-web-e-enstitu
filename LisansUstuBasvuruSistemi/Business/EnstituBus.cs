using System.Collections.Generic;
using System.Linq;
using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Business
{
    public class EnstituBus
    {
        public static List<Enstituler> Enstitulers = new List<Enstituler>();

        public static List<Enstituler> GetEnstituler()
        {
            if (Enstitulers.Any()) return Enstitulers;
            using (var entities = new LubsDbEntities())
            {
                Enstitulers = entities.Enstitulers.Where(p => p.IsAktif).OrderBy(o => o.EnstituAd).ToList();
            }
            return Enstitulers;

        }

        public static Enstituler[] GetEnstituler(bool sadeceYetkiliOlduguEnstituler)
        {
            var enst = Enstitulers.AsQueryable();
            if (sadeceYetkiliOlduguEnstituler && UserIdentity.Current.IsAdmin == false) enst = enst.Where(p => UserIdentity.Current.EnstituKods.Contains(p.EnstituKod));
            return enst.Where(p => p.IsAktif).OrderBy(o => o.EnstituAd).ToArray();
        }
        public static Enstituler GetEnstitu(string ekd)
        {
            return Enstitulers.First(p => p.EnstituKod == ekd);
        }

        public static bool IsContainsEnstitu(string ekod)
        {

            ekod = ekod.ToLower();
            var sdils = Enstitulers.Where(p => p.IsAktif).ToList();
            return sdils.Select(s => s.EnstituKisaAd.ToLower()).Any(a => a == ekod);

        }

        public static string GetSelectedEnstitu(string ekd)
        {  
            return Enstitulers.First(p => p.EnstituKisaAd.ToLower() == ekd.ToLower()).EnstituKod;
        }

        public static List<CmbStringDto> GetCmbAktifEnstituler(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = null, Caption = "" });
            var data = Enstitulers.Where(p => p.IsAktif).OrderBy(o => o.EnstituAd).ToList();
            foreach (var item in data)
            {
                dct.Add(new CmbStringDto { Value = item.EnstituKod, Caption = item.EnstituAd });
            }
            return dct;

        }

        public static List<CmbStringDto> GetCmbYetkiliEnstituler(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();

            if (bosSecimVar) dct.Add(new CmbStringDto { Value = null, Caption = "" });

            var data = Enstitulers.Where(p => enstKods.Contains(p.EnstituKod)).OrderBy(o => o.EnstituAd).ToList();
            foreach (var item in data)
            {
                dct.Add(new CmbStringDto { Value = item.EnstituKod, Caption = item.EnstituAd });
            }

            return dct;

        }
    }
}