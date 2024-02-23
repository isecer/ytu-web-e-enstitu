using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Business
{
    public class AnabilimDallariBus
    {
        public static List<CmbStringDto> CmbGetAktifAnabilimDallariStr(string enstituKod, bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.AnabilimDallaris.Where(p => p.IsAktif && p.EnstituKod == enstituKod).OrderBy(o => o.AnabilimDaliAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.AnabilimDaliAdi, Caption = item.AnabilimDaliAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> CmbGetAktifAnabilimDallari(string enstituKod, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.AnabilimDallaris.Where(p => p.IsAktif && p.EnstituKod == enstituKod).OrderBy(o => o.AnabilimDaliAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.AnabilimDaliID, Caption = item.AnabilimDaliAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> CmbGetYetkiliAnabilimDallari(bool bosSecimVar = false, string enstituKod = "")
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.AnabilimDallaris.Where(p => enstKods.Contains(p.EnstituKod));
                if (enstituKod.IsNullOrWhiteSpace() == false) data = data.Where(p => p.EnstituKod == enstituKod);
                var data2 = data.OrderBy(o => o.AnabilimDaliAdi).ToList();
                foreach (var item in data2)
                {
                    dct.Add(new CmbIntDto { Value = item.AnabilimDaliID, Caption = item.AnabilimDaliAdi });
                }
            }
            return dct;

        }
    }
}