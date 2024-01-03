using System.Collections.Generic;
using System.Linq;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Business
{
    public class ProgramlarBus
    {
        public static List<CmbStringDto> CmbGetAktifProgramlarX(int anabilimDaliId, int ogrenimTipKod, int basvuruSurecId, int kullaniciTipId, bool sadeceKotasiOlanlar = true)
        {
            var dct = new List<CmbStringDto>();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
               
                var q = from p in db.Programlars
                        join k in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == basvuruSurecId) on new { p.ProgramKod, OgrenimTipKod = ogrenimTipKod } equals new { k.ProgramKod, k.OgrenimTipKod }
                        where p.AnabilimDaliID == anabilimDaliId
                        group new { p.ProgramKod, p.ProgramAdi, k.AlanIciKota, k.AlanDisiKota, k.OrtakKota, k.OrtakKotaSayisi } by new { p.ProgramKod, p.ProgramAdi } into g1
                        orderby g1.Key.ProgramAdi
                        select new
                        {
                            g1.Key.ProgramKod,
                            g1.Key.ProgramAdi,
                            cnt = g1.Count(p => p.AlanIciKota > 0 || p.AlanDisiKota > 0 || (p.OrtakKota && p.OrtakKotaSayisi > 0))

                        };
                if (sadeceKotasiOlanlar) q = q.Where(p => p.cnt > 0);
                var qdata = q.ToList();
                if (qdata.Count > 0) dct.Add(new CmbStringDto { Value = null, Caption = "" });

                foreach (var item in qdata)
                {
                    dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi + " " });
                }


            }
            return dct;
        }

        public static List<CmbStringDto> CmbGetAktifProgramlar(bool bosSecimVar , int? anabilimDaliId)
        {

            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Programlars.Where(p => p.AnabilimDaliID == anabilimDaliId && p.IsAktif).OrderBy(o => o.ProgramAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi });
                }
            }
            return dct;
        }
        public static List<CmbStringDto> CmbGetAktifProgramlar(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Programlars.Where(p => p.IsAktif).OrderBy(o => o.ProgramAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi });
                }
            }
            return dct;
        }
        public static List<CmbStringDto> CmbGetAktifProgramlar(string enstituKod, bool bosSecimVar = false, bool isAbdShow = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Programlars.Where(p => p.AnabilimDallari.IsAktif && p.IsAktif && p.AnabilimDallari.EnstituKod == enstituKod).OrderBy(o => o.ProgramAdi).ToList();
                foreach (var item in data)
                {
                    if (isAbdShow)
                    {
                        var abdL = item.AnabilimDallari;
                        dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = abdL.AnabilimDaliAdi + " / " + item.ProgramAdi });
                    }
                    else
                    {
                        dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi });
                    }
                }
            }
            return dct.OrderBy(o => o.Caption).ToList();
        }
        public static List<CmbStringDto> CmbGetBsTumProgramlar(int basvuruSurecId, bool isBolumOrOgrenci, List<int> ogrenimTipKods, bool isBSonucOrMulakat)
        {
            List<CmbStringDto> dct;

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var basvuruSureci = db.BasvuruSurecs.First(p => p.BasvuruSurecID == basvuruSurecId);
                var kullaniciProgramKods = UserBus.GetUserProgramKods(UserIdentity.Current.Id, basvuruSureci.EnstituKod);

                if (isBSonucOrMulakat)
                {
                    dct = (from vw in db.vW_ProgramBasvuruSonucSayisal.Where(p => p.BasvuruSurecID == basvuruSurecId && ogrenimTipKods.Contains(p.OgrenimTipKod))
                           where
                                 (isBolumOrOgrenci || (vw.AIAsilCount > 0 || vw.ADAsilCount > 0))
                                 && kullaniciProgramKods.Contains(vw.ProgramKod)
                           select new
                           {
                               vw.OgrenimTipKod,
                               //IsOnayliBasvuruVar = (IsBolumOrOgrenci ? true : (IsMulakatOrSonuc ? (Vw.ToplamBasvuru > 0) : (Vw.AIAsilCount > 0 || Vw.ADAsilCount > 0))),
                               Value = vw.ProgramKod,
                               Caption = vw.OgrenimTipAdi + " > " + vw.ProgramAdi,

                           }).Select(s => new CmbStringDto
                           {
                               Value = s.Value,
                               Caption = s.Caption,

                           }).ToList();

                }
                else
                {
                    if (isBolumOrOgrenci)
                    {
                        dct = (from s in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == basvuruSurecId && ogrenimTipKods.Contains(p.OgrenimTipKod))
                               join pl in db.Programlars on s.ProgramKod equals pl.ProgramKod
                               join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == basvuruSureci.EnstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod
                               where kullaniciProgramKods.Contains(s.ProgramKod)
                               select new CmbStringDto
                               {
                                   Value = s.ProgramKod,
                                   Caption = ot.OgrenimTipAdi + " > " + pl.ProgramAdi,

                               }).OrderBy(o => o.Caption).ToList();
                    }
                    else
                    {
                        var bDurums = new List<int> { BasvuruDurumuEnum.Onaylandı, BasvuruDurumuEnum.Gonderildi };
                        dct = (from s in db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruSurecID == basvuruSurecId && bDurums.Contains(p.Basvurular.BasvuruDurumID) && ogrenimTipKods.Contains(p.OgrenimTipKod))
                               join pl in db.Programlars on s.ProgramKod equals pl.ProgramKod
                               join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == basvuruSureci.EnstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod
                               where kullaniciProgramKods.Contains(s.ProgramKod)
                               select new CmbStringDto
                               {
                                   Value = s.ProgramKod,
                                   Caption = ot.OgrenimTipAdi + " > " + pl.ProgramAdi,

                               }).Distinct().OrderBy(o => o.Caption).ToList();
                    }


                }
            }
            return dct;

        }
    }
}