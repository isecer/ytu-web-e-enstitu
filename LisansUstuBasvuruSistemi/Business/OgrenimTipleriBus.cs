using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class OgrenimTipleriBus
    {
        public static List<CmbIntDto> CmbGetTumOgrenimTipleri(string enstituKod, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod).OrderBy(o => o.OgrenimTipAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.OgrenimTipKod, Caption = item.OgrenimTipAdi });
                }
            }
            return dct;

        }

        public static List<CmbIntDto> CmbAktifOgrenimTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.OgrenimTipleris.Where(p => p.IsAktif).OrderBy(o => o.OgrenimTipAdi).Select(s => new { s.OgrenimTipKod, s.OgrenimTipAdi }).Distinct().ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.OgrenimTipKod, Caption = item.OgrenimTipAdi });
                }
            }
            return dct;

        }

        public static List<CmbIntDto> CmbAktifOgrenimTipleri(string enstituKod, bool bosSecimVar = false, bool? aktif = true, int? haricOgreniTipKod = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod && (!aktif.HasValue || p.IsAktif == aktif.Value) && (!haricOgreniTipKod.HasValue || p.OgrenimTipKod != haricOgreniTipKod.Value)).OrderBy(o => o.OgrenimTipAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.OgrenimTipKod, Caption = item.OgrenimTipAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> CmbAktifOgrenimTipIdDoktora(string enstituKod, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
             

                var data = db.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod  && p.IsAktif).OrderBy(o => o.OgrenimTipAdi).ToList();
                data = data.Where(p => IsDoktora(p.OgrenimTipKod)).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.OgrenimTipID, Caption = item.OgrenimTipAdi });
                }
            }
            return dct;

        }
        public static bool IsDoktora(this int ogrenimTipKod)
        {
            return new List<int> { OgrenimTipi.ButunlesikDoktora, OgrenimTipi.SanattaYeterlilik, OgrenimTipi.Doktra }
                .Contains(ogrenimTipKod);
        }
        public static bool IsDoktora(this int? ogrenimTipKod)
        {
            return  ogrenimTipKod.HasValue && IsDoktora(ogrenimTipKod.Value); 
        }
    }
}