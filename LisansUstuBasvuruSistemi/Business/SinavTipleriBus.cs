using System.Collections.Generic;
using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Business
{
    public class SinavTipleriBus
    {
        public static List<CmbIntDto> CmbGetAktifSinavlar(string enstituKodu, int? sinavTipGrupId = null, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = (from s in entities.SinavTipleris.Where(s2 => s2.EnstituKod == enstituKodu && s2.IsAktif)
                            join stl in entities.SinavTipleris on new { s.SinavTipID } equals new { stl.SinavTipID }
                            orderby stl.SinavAdi
                            select new
                            {
                                s.SinavTipID,
                                s.SinavTipKod,
                                s.SinavTipGrupID,
                                stl.SinavAdi
                            }).AsQueryable();
                if (sinavTipGrupId.HasValue) data = data.Where(p => p.SinavTipGrupID == sinavTipGrupId.Value);
                var qdata = data.ToList();
                foreach (var item in qdata)
                {
                    dct.Add(new CmbIntDto { Value = item.SinavTipID, Caption = item.SinavAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> CmbGetBsAktifSinavlar(string enstituKodu, List<int> sinavTipGrupIDs, bool bosSecimVar = false)
        {
            sinavTipGrupIDs = sinavTipGrupIDs ?? new List<int>();
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var bssT = entities.BasvuruSurecSinavTipleris.Where(p => p.EnstituKod == enstituKodu).Select(s => s.SinavTipID).Distinct();
                var data = (from s in entities.SinavTipleris.Where(s2 => s2.EnstituKod == enstituKodu && bssT.Contains(s2.SinavTipID))

                            join stl in entities.SinavTipleris on new { s.SinavTipID } equals new { stl.SinavTipID }
                            orderby stl.SinavAdi
                            select new
                            {
                                s.SinavTipKod,
                                s.SinavTipGrupID,
                                stl.SinavAdi
                            }).AsQueryable();
                if (sinavTipGrupIDs.Count > 0) data = data.Where(p => sinavTipGrupIDs.Contains(p.SinavTipGrupID));
                var qdata = data.ToList();
                foreach (var item in qdata)
                {
                    dct.Add(new CmbIntDto { Value = item.SinavTipKod, Caption = item.SinavAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> CmbGetdAktifSinavlar(List<CmbMultyTypeDto> filterM, int basvuruSurecId, int sinavTipGrupId, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            using (var entities = new LubsDbEntities())
            {
                var data = (from s in entities.BasvuruSurecSinavTipleris.Where(s2 => s2.IsAktif && s2.BasvuruSurecID == basvuruSurecId)
                            join stl in entities.SinavTipleris on new { s.SinavTipID } equals new { stl.SinavTipID }
                            orderby stl.SinavAdi
                            where s.SinavTipGrupID == sinavTipGrupId
                            select new
                            {
                                s.SinavTipID,
                                s.SinavTipGrupID,
                                stl.SinavAdi
                            }).ToList();

                var qSinavOt = entities.BasvuruSurecSinavTipleriOTNotAraliklaris.Where(p => p.BasvuruSurecID == basvuruSurecId).ToList();
                var qJoin = (from s in qSinavOt
                             join fl in filterM on new { s.OgrenimTipKod, s.Ingilizce } equals new { OgrenimTipKod = fl.Value, Ingilizce = fl.ValueB }
                             group new { s.SinavTipID, s.OgrenimTipKod, s.IsGecerli, s.IsIstensin, s.Ingilizce, ProgramKod = fl.ValueS2 } by new { s.SinavTipID, s.OgrenimTipKod, s.IsGecerli, s.IsIstensin, s.Ingilizce, ProgramKod = fl.ValueS2 } into g1
                             select new
                             {
                                 g1.Key.SinavTipID,
                                 g1.Key.OgrenimTipKod,
                                 g1.Key.IsGecerli,
                                 g1.Key.IsIstensin,
                                 IsIstensin2 = entities.BasvuruSurecSinavTipleriOTNotAraliklariGecersizProgramlars.Any(p => p.BasvuruSurecSinavTipleriOTNotAraliklari.BasvuruSurecID == basvuruSurecId && p.BasvuruSurecSinavTipleriOTNotAraliklari.SinavTipID == g1.Key.SinavTipID && p.BasvuruSurecSinavTipleriOTNotAraliklari.OgrenimTipKod == g1.Key.OgrenimTipKod && p.BasvuruSurecSinavTipleriOTNotAraliklari.Ingilizce == g1.Key.Ingilizce && p.ProgramKod == g1.Key.ProgramKod) == false,
                                 g1.Key.Ingilizce,
                                 g1.Key.ProgramKod,

                             }).ToList();

                var programKods = filterM.Select(s => s.ValueS2).ToList();
                int inxBosR = 0;

                foreach (var item in data)
                {

                    var qnyGecersiz = qJoin.Any(p => p.SinavTipID == item.SinavTipID && (p.IsGecerli == false));
                    var qGecerliAmaIstenmesin = qJoin.Where(p => p.SinavTipID == item.SinavTipID && p.IsGecerli && (p.IsIstensin == false || p.IsIstensin2 == false)).Select(s => new { s.SinavTipID, s.OgrenimTipKod, s.ProgramKod }).Distinct().ToList();
                    var isIstensin = qGecerliAmaIstenmesin.Count != programKods.Count;
                    var sinavBVarmi = qJoin.Any(p => p.SinavTipID == item.SinavTipID && filterM.Any(a => a.Value == p.OgrenimTipKod && a.ValueB == p.Ingilizce));

                    if (!qnyGecersiz && isIstensin && sinavBVarmi)
                    {
                        if (inxBosR == 0)
                        {
                            inxBosR++;
                            if (bosSecimVar) dct.Add(new CmbIntDto { Caption = "" });
                        }
                        dct.Add(new CmbIntDto { Value = item.SinavTipID, Caption = item.SinavAdi });
                    }

                }
            }
            return dct;

        }
        public static List<CmbDoubleDto> CmbGetSinavTipOzelNot(int sinavTipId, bool bosSecimVar = false)
        {
            var dct = new List<CmbDoubleDto>();
            if (bosSecimVar) dct.Add(new CmbDoubleDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.SinavTipleris.Where(p => p.SinavTipID == sinavTipId).SelectMany(s => s.SinavNotlaris).Select(s => new CmbDoubleDto
                {
                    Value = s.SinavNotDeger,
                    Caption = s.SinavNotAdi + " (Yüzlük karşılığı: " + s.SinavNotDeger + ")"
                }).OrderBy(o => o.Value).ToList();
                dct.AddRange(data);
            }
            return dct;

        }

        public static List<CmbIntDto> CmbGetSinavTipGruplari(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.SinavTipGruplaris.OrderBy(o => o.SinavTipGrupAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SinavTipGrupID, Caption = item.SinavTipGrupAdi });
                }
            }
            return dct;

        }

        public static List<CmbIntDto> CmbGetOzelNotTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.OzelNotTipleris.OrderBy(o => o.OzelNotTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.OzelNotTipID, Caption = item.OzelNotTipAdi });
                }
            }
            return dct;

        }
    }
}