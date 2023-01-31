using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Models.ObsService;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public class EnstituBus
    {
        public static List<Enstituler> Enstitulers = new List<Enstituler>();

        public static List<Enstituler> GetEnstituler()
        {
            if (EnstituBus.Enstitulers.Any()) return EnstituBus.Enstitulers;
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                EnstituBus.Enstitulers = db.Enstitulers.Where(p => p.IsAktif).OrderBy(o => o.EnstituAd).ToList();
            } 
            return EnstituBus.Enstitulers;

        }

        public static Enstituler[] GetEnstituler(bool sadeceYetkiliOlduguEnstituler = false)
        {
            var enst = EnstituBus.Enstitulers.AsQueryable();
            if (sadeceYetkiliOlduguEnstituler && UserIdentity.Current.IsAdmin == false) enst = enst.Where(p => UserIdentity.Current.EnstituKods.Contains(p.EnstituKod));
            return enst.Where(p => p.IsAktif).OrderBy(o => o.EnstituAd).ToArray();
        }

        public static bool IsContainsEnstitu(string ekod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                ekod = ekod.ToLower();
                var sdils = db.Enstitulers.Where(p => p.IsAktif).ToList();
                return sdils.Select(s => s.EnstituKisaAd.ToLower()).Any(a => a == ekod);
            }
        }

        public static string GetSelectedEnstitu(string ekd)
        {
            return Enstitulers.First(p => p.EnstituKisaAd.ToLower() == ekd.ToLower()).EnstituKod;
        }

        public static List<CmbStringDto> GetCmbAktifEnstituler(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = null, Caption = "" });
            var data = EnstituBus.Enstitulers.Where(p => p.IsAktif).OrderBy(o => o.EnstituAd).ToList();
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
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = EnstituBus.Enstitulers.Where(p => enstKods.Contains(p.EnstituKod)).OrderBy(o => o.EnstituAd).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.EnstituKod, Caption = item.EnstituAd });
                }
            }
            return dct;

        }
    }
}