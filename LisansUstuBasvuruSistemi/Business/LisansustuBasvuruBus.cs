using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Business
{
    public class LisansustuBasvuruBus
    {
        public static List<CmbIntDto> GetbasvuruSurecleri(string enstituKod, int basvuruSurecTipId, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.BasvuruSurecs.Where(p => p.EnstituKod == enstituKod && p.BasvuruSurecTipID == basvuruSurecTipId)
                    join d in db.Donemlers on s.DonemID equals d.DonemID
                    orderby s.BaslangicTarihi descending
                    select new
                    {
                        s.BasvuruSurecID,
                        s.BaslangicYil,
                        s.BitisYil,
                        d.DonemAdi,
                        s.BaslangicTarihi,
                        s.BitisTarihi
                    }).ToList();
                foreach (var item in data)
                {
                    lst.Add(new CmbIntDto { Value = item.BasvuruSurecID, Caption = (item.BaslangicYil + "/" + item.BitisYil + " " + item.DonemAdi + " (" + item.BaslangicTarihi.ToFormatDate() + " - " + item.BitisTarihi.ToFormatDate() + ")") });
                }
            }
            return lst;
        }

        public static List<CmbIntDto> CmbBasvuruDurumListe(bool bosSecimVar = false, bool isTumu = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.BasvuruDurumlaris.Where(p => (isTumu || p.BasvuranGorsun)).OrderBy(o => o.BasvuruDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.BasvuruDurumID, Caption = item.BasvuruDurumAdi });
                }
            }

            return dct;

        }

        public static List<CmbIntDto> CmbMulakatSonucTip(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MulakatSonucTipleris.Where(p => p.MulakatSonucTipID > 0).OrderBy(o => o.MulakatSonucTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MulakatSonucTipID, Caption = item.MulakatSonucTipAdi });
                }
            }

            return dct;

        }

        public static List<CmbBoolDto> CmbSinavBelgeTaahhut(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Taahhüt Olan" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Taahhüt Olmayan" });
            return dct;
        }

        public static List<CmbBoolDto> CmbBolumOrOgrenci(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Anabilim Dalları" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Öğrenciler" });
            return dct;

        }

        public static List<CmbIntDto> CmbKayitDurum()
        {
            var dct = new List<CmbIntDto>();
            dct.Add(new CmbIntDto { Value = null, Caption = "İşlem Görmeyenler" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.KayitDurumlaris.ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.KayitDurumID, Caption = item.KayitDurumAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> CmbOtYedekCarpanData(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            dct.Add(new CmbIntDto { Value = 1, Caption = "Asil Öğrenci Kadar Yedek Öğrenci" });
            dct.Add(new CmbIntDto { Value = 2, Caption = "Asil Öğrencinin 2 Katı Kadar Yedek Öğrenci" });
            dct.Add(new CmbIntDto { Value = 3, Caption = "Asil Öğrencinin 3 Katı Kadar Yedek Öğrenci" });
            dct.Add(new CmbIntDto { Value = 4, Caption = "Asil Öğrencinin 4 Katı Kadar Yedek Öğrenci" });
            return dct;

        }

        public static List<CmbIntDto> CmbGetOgrenciBolumleri(string enstituKod, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.OgrenciBolumleris.Where(p => p.EnstituKod == enstituKod && p.IsAktif).OrderBy(o => o.BolumAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.OgrenciBolumID, Caption = item.BolumAdi });
                }
            }
            return dct;

        }

        public static List<CmbIntDto> CmbGetNotSistemleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.NotSistemleris.Where(p => p.IsAktif).OrderBy(o => o.NotSistemID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.NotSistemID, Caption = item.NotSistemAdi });
                }
            }
            return dct;

        }

        public static int? GetAktifBasvuruSurecId(string enstituKod, int basvuruSurecTipId, int? basvuruSurecId = null, bool? isMulakatDurum = null)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var nowDate = DateTime.Now;
                var bf = db.BasvuruSurecs.Where(p => p.BasvuruSurecTipID == basvuruSurecTipId && (p.BaslangicTarihi <= nowDate && p.BitisTarihi >= nowDate) && p.IsAktif && (p.EnstituKod == enstituKod) && p.BasvuruSurecID == (basvuruSurecId ?? p.BasvuruSurecID));
                if (isMulakatDurum.HasValue) bf = bf.Where(p => p.SonucGirisBaslangicTarihi.HasValue == isMulakatDurum.Value);
                var qBf = bf.FirstOrDefault();
                return qBf?.BasvuruSurecID;
            }
        }

        public static bool ResimBilgisiLazimOlanKayitVarMi(int kullaniciId)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                return db.Basvurulars.Any(p => p.KullaniciID == kullaniciId) || db.MezuniyetBasvurularis.Any(a => a.KullaniciID == kullaniciId);
            }

        }

        public static List<CmbIntDto> CmbUyruk(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Uyruklars.OrderBy(o => o.UyrukKod == 3009 ? 0 : 1).ThenBy(t => t.Ad).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.UyrukKod, Caption = !string.IsNullOrEmpty(item.KisaAd) ? item.Ad + " (" + item.KisaAd + ")" : item.Ad });
                }
            }
            return dct;

        }

        public static NotSistemleri GetNotSistemi(int notSistemId)
        {

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                return db.NotSistemleris.Where(p => p.NotSistemID == notSistemId).OrderBy(o => o.NotSistemID).FirstOrDefault();

            }

        }

        public static List<CmbIntDto> CmbGetAktifOgrenimTipleri(int basvuruSurecId, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var otS = from bs in db.BasvuruSurecs.Where(p => p.BasvuruSurecID == basvuruSurecId)
                    join s in db.BasvuruSurecOgrenimTipleris.Where(p => p.IsAktif) on bs.BasvuruSurecID equals s.BasvuruSurecID
                    join ot in db.OgrenimTipleris on new { bs.EnstituKod, s.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                    select new
                    {
                        Kod = s.OgrenimTipKod,
                        Ad = ot.OgrenimTipAdi

                    };

                var qdata = otS.ToList();
                foreach (var item in qdata)
                {
                    dct.Add(new CmbIntDto { Value = item.Kod, Caption = item.Ad });
                }
            }
            return dct;

        }
    }
}