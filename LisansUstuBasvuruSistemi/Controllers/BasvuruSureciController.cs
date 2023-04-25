using BiskaUtil;
using Ionic.Zip;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.BasvuruSureci)]
    public class BasvuruSureciController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();

        public ActionResult Index()
        {
            return Index(new FmBasvuruSureciDto() { PageSize = 15 });
        }
        [HttpPost]
        public ActionResult Index(FmBasvuruSureciDto model)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in db.BasvuruSurecs.Where(p => p.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru)
                    join kt in db.BasvuruSurecKontrolTipleris on s.Kota_BasvuruSurecKontrolTipID equals kt.BasvuruSurecKontrolTipID
                    join e in db.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                    join d in db.Donemlers on new { s.DonemID } equals new { d.DonemID }
                    join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where EnstKods.Contains(e.EnstituKod)
                    select new
                    {
                        s.EnstituKod,
                        e.EnstituAd,
                        s.BaslangicYil,
                        s.BitisYil,
                        s.DonemID,
                        d.DonemAdi,
                        s.BasvuruSurecID,
                        s.BaslangicTarihi,
                        s.BitisTarihi,
                        s.SonucGirisBaslangicTarihi,
                        s.SonucGirisBitisTarihi,
                        s.AGNOGirisBaslangicTarihi,
                        s.AGNOGirisBitisTarihi,
                        s.Kota_BasvuruSurecKontrolTipID,
                        Kota_BasvuruSurecKontrolTipAdi = kt.BasvuruSurecKontrolTipAdi,
                        s.ToplamKota,
                        OTCount = s.BasvuruSurecOgrenimTipleris.Count,
                        s.IsAktif,
                        Hesaplandi = s.MulakatSonuclaris.Any(a => a.MulakatSonucTipID != MulakatSonucTipi.Hesaplanmadı),
                        s.IslemTarihi,
                        s.IslemYapanID,
                        IslemYapan = k.Ad + " " + k.Soyad,
                        s.IslemYapanIP,
                        CmbOgrenimTipBilgi = (from s2 in s.BasvuruSurecOgrenimTipleris
                                              join ot in db.OgrenimTipleris on new { s.EnstituKod, s2.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                                              where s.IsAktif
                                              select new CmbIntDto
                                              {
                                                  Value = s2.OgrenimTipKod,
                                                  Caption = ot.OgrenimTipAdi,
                                              }).OrderBy(t => t.Caption).ToList()

                    };


            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);

            model.RowCount = q.Count();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            if (!model.Sort.IsNullOrWhiteSpace())
            {
                if (model.Sort.Contains("OgretimYili"))
                {
                    if (model.Sort.Contains(" DESC")) q = q.OrderByDescending(o => o.BaslangicYil).ThenByDescending(t => t.DonemID);
                    else q = q.OrderBy(o => o.BaslangicYil).ThenBy(t => t.DonemID);
                }
                else q = q.OrderBy(model.Sort);
            }
            else q = q.OrderByDescending(o => o.BaslangicTarihi);
            var qdata = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new FrBasvuruSureciDto
            {
                Hesaplandi = s.Hesaplandi,
                EnstituKod = s.EnstituKod,
                EnstituAdi = s.EnstituAd,
                BaslangicYil = s.BaslangicYil,
                BitisYil = s.BitisYil,
                DonemID = s.DonemID,
                DonemAdi = s.DonemAdi,
                Kota_BasvuruSurecKontrolTipID = s.Kota_BasvuruSurecKontrolTipID,
                Kota_BasvuruSurecKontrolTipAdi = s.Kota_BasvuruSurecKontrolTipAdi,
                BasvuruSurecID = s.BasvuruSurecID,
                BaslangicTarihi = s.BaslangicTarihi,
                BitisTarihi = s.BitisTarihi,
                SonucGirisBaslangicTarihi = s.SonucGirisBaslangicTarihi,
                SonucGirisBitisTarihi = s.SonucGirisBitisTarihi,
                AGNOGirisBaslangicTarihi = s.AGNOGirisBaslangicTarihi,
                AGNOGirisBitisTarihi = s.AGNOGirisBitisTarihi,
                ToplamKota = s.ToplamKota,
                IsAktif = s.IsAktif,
                OTCount = s.OTCount,
                IslemTarihi = s.IslemTarihi,
                IslemYapanID = s.IslemYapanID,
                IslemYapan = s.IslemYapan,
                IslemYapanIP = s.IslemYapanIP,
                CmbOgrenimTipBilgi = s.CmbOgrenimTipBilgi
            }).ToList();

            model.FrBasvuruSureciDtos = qdata;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IndexModel = IndexModel;

            return View(model);
        }
        [Authorize(Roles = RoleNames.BasvuruSureciKayit)]
        public ActionResult Kayit(int? id, string EKD)
        {
            string _EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            var MmMessage = new MmMessage();
            ViewBag.MmMessage = MmMessage;
            var model = new kmBasvuruSurec();
            model.IsAktif = true;
            var eoY = DateTime.Now.ToEgitimOgretimYilBilgi();
            model.OgretimYili = eoY.BaslangicYili + "/" + eoY.BitisYili + "/" + eoY.Donem;
            model.FarkliOgrenimTipleriAyniBasvurudaAlinabilsin = true;

            var bsMList = Management.getBsMailZamanData();
            if (id.HasValue && id > 0)
            {
                var data = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == id).FirstOrDefault();

                var bsmailData = data.BasvuruSurecOtoMails.ToList();
                foreach (var item in bsMList)
                {
                    var bsm = bsmailData.Where(p => p.ZamanTipID == item.ZamanTipID && p.Zaman == item.Zaman).FirstOrDefault();
                    if (bsm != null)
                    {
                        item.Checked = true;
                        item.Zaman = bsm.Zaman;
                        item.ZamanTipID = bsm.ZamanTipID;
                        item.BasvuruSurecID = bsm.BasvuruSurecID;
                        item.Gonderildi = bsm.Gonderildi;
                        item.GonderilenCount = bsm.GonderilenCount;
                    }

                }

                if (data != null)
                {
                    model.BasvuruSurecID = id.Value;
                    model.EnstituKod = data.EnstituKod;
                    model.BaslangicYil = data.BaslangicYil;
                    model.BitisYil = data.BitisYil;
                    model.BaslangicTarihi = data.BaslangicTarihi;
                    model.BitisTarihi = data.BitisTarihi;
                    model.SonucGirisBaslangicTarihi = data.SonucGirisBaslangicTarihi;
                    model.SonucGirisBitisTarihi = data.SonucGirisBitisTarihi;
                    model.AGNOGirisBaslangicTarihi = data.AGNOGirisBaslangicTarihi;
                    model.AGNOGirisBitisTarihi = data.AGNOGirisBitisTarihi;
                    model.DonemID = data.DonemID;
                    model.Kota_BasvuruSurecKontrolTipID = data.Kota_BasvuruSurecKontrolTipID;
                    model.ToplamKota = data.ToplamKota;
                    model.IsAktif = data.IsAktif;
                    model.FarkliOgrenimTipleriAyniBasvurudaAlinabilsin = data.FarkliOgrenimTipleriAyniBasvurudaAlinabilsin;
                    model.isYuzdeGirisBolum = data.isYuzdeGirisBolum;
                    model.OgretimYili = data.BaslangicYil + "/" + data.BitisYil + "/" + data.DonemID;
                    model.AnketID = data.AnketID;
                    model.KayitOlmayanlarAnketID = data.KayitOlmayanlarAnketID;
                    model.IsBelgeYuklemeVar = data.IsBelgeYuklemeVar;
                    model.IsKayittaBelgeOnayiZorunlu = data.IsKayittaBelgeOnayiZorunlu;
                }

            }
            else
            {
                var enst = db.Enstitulers.Where(p => p.EnstituKod == _EnstituKod).First();
                model.ToplamKota = enst.ToplamKayitKota;
            }
            model.MulakatSTurModel = (from ms in db.MulakatSinavTurleris
                                      join bsm in db.BasvuruSurecMulakatSinavTurleris on new { ms.MulakatSinavTurID, BasvuruSurecID = id.Value } equals new { bsm.MulakatSinavTurID, bsm.BasvuruSurecID } into defbsm
                                      from bsM in defbsm.DefaultIfEmpty()
                                      orderby ms.MulakatSinavTurAdi
                                      select new MulakatSturModel
                                      {
                                          MulakatSinavTurID = ms.MulakatSinavTurID,
                                          SinavTurAdi = ms.MulakatSinavTurAdi,
                                          YuzdeOran = bsM != null ? bsM.YuzdeOran : (model.isYuzdeGirisBolum ? (int?)null : ms.YuzdeOran),
                                          Zorunlu = bsM != null ? bsM.Zorunlu : false,

                                      }).ToList();
            model.OgrenimTipModel.IsBelgeYuklemeVar = model.IsBelgeYuklemeVar;
            model.OgrenimTipModel.OgrenimTipleriDataList = (from o in db.OgrenimTipleris.Where(p => p.EnstituKod == _EnstituKod)
                                                            join s in db.BasvuruSurecOgrenimTipleris on new { o.OgrenimTipKod, BasvuruSurecID = id.Value } equals new { s.OgrenimTipKod, s.BasvuruSurecID } into def1
                                                            from defS in def1.DefaultIfEmpty()
                                                            join ot in db.OgrenimTipleris on o.OgrenimTipID equals ot.OgrenimTipID
                                                            where o.IsAktif || defS.IsAktif
                                                            select new CheckObject<KrOgrenimTip>
                                                            {
                                                                Value = new KrOgrenimTip
                                                                {
                                                                    EnstituKod = _EnstituKod,

                                                                    BasvuruSurecOgrenimTipID = defS != null ? defS.BasvuruSurecOgrenimTipID : 0,
                                                                    OrjinalVeri = defS == null,
                                                                    OgrenimTipKod = o.OgrenimTipKod,
                                                                    GrupGoster = defS != null ? defS.GrupGoster : o.GrupGoster,
                                                                    GrupKodu = defS != null ? defS.GrupKodu : o.GrupKodu,
                                                                    Kota = defS != null ? defS.Kota : o.Kota,
                                                                    GBNFormulu = defS != null ? defS.GBNFormulu : o.GBNFormulu,
                                                                    YedekOgrenciSayisiKotaCarpani = defS != null ? defS.YedekOgrenciSayisiKotaCarpani : o.YedekOgrenciSayisiKotaCarpani,
                                                                    GBNFormuluAlessiz = defS != null ? defS.GBNFormuluAlessiz : o.GBNFormuluAlessiz,
                                                                    GBNFormuluMulakatsiz = defS != null ? defS.GBNFormuluMulakatsiz : o.GBNFormuluMulakatsiz,
                                                                    LEgitimBilgisiIste = defS != null ? defS.LEgitimBilgisiIste : o.LEgitimBilgisiIste,
                                                                    YLEgitimBilgisiIste = defS != null ? defS.YLEgitimBilgisiIste : o.YLEgitimBilgisiIste,
                                                                    BasariNotOrtalamasi = defS != null ? defS.BasariNotOrtalamasi : o.BasariNotOrtalamasi,
                                                                    MulakatSurecineGirecek = defS != null ? defS.MulakatSurecineGirecek : o.MulakatSurecineGirecek,
                                                                    AlanIciBilimselHazirlik = defS != null ? defS.AlanIciBilimselHazirlik : o.AlanIciBilimselHazirlik,
                                                                    AlanDisiBilimselHazirlik = defS != null ? defS.AlanDisiBilimselHazirlik : o.AlanDisiBilimselHazirlik,
                                                                    YokOgrenciKontroluYap = defS != null ? defS.YokOgrenciKontroluYap : o.YokOgrenciKontroluYap,
                                                                    IstenecekKatkiPayiTutari = defS != null ? defS.IstenecekKatkiPayiTutari : o.IstenecekKatkiPayiTutari,
                                                                    BelgeYuklemeAsilBasTar = defS.BelgeYuklemeAsilBasTar,
                                                                    BelgeYuklemeAsilBitTar = defS.BelgeYuklemeAsilBitTar,
                                                                    BelgeYuklemeYedekBasTar = defS.BelgeYuklemeYedekBasTar,
                                                                    BelgeYuklemeYedekBitTar = defS.BelgeYuklemeYedekBitTar,
                                                                    OgrenimTipAdi = ot.OgrenimTipAdi,
                                                                    GrupAdi = ot.GrupAdi,
                                                                },
                                                                Checked = db.BasvuruSurecOgrenimTipleris.Any(p => p.BasvuruSurecID == model.BasvuruSurecID && p.OgrenimTipKod == o.OgrenimTipKod && defS.IsAktif)
                                                            }).ToList();
            foreach (var item in model.OgrenimTipModel.OgrenimTipleriDataList)
            {
                if (item.Value.OrjinalVeri)
                {
                    item.Value.SecilenBSOTIDs.AddRange(db.OgrenimTipleriOrtBasvrs.Where(p => p.OgrenimTipKod == item.Value.OgrenimTipKod).Select(s => s.OgrenimTipKod2).ToList());
                    item.Value.SecilenBSOTIDs.AddRange(db.OgrenimTipleriOrtBasvrs.Where(p => p.OgrenimTipKod2 == item.Value.OgrenimTipKod).Select(s => s.OgrenimTipKod).ToList());

                }
                else
                {
                    item.Value.SecilenBSOTIDs.AddRange(db.BasvuruSurecOTOrtBasvrs.Where(p => p.BasvuruSurecID == model.BasvuruSurecID && p.OgrenimTipKod == item.Value.OgrenimTipKod).Select(s => s.OgrenimTipKod2).ToList());
                    item.Value.SecilenBSOTIDs.AddRange(db.BasvuruSurecOTOrtBasvrs.Where(p => p.BasvuruSurecID == model.BasvuruSurecID && p.OgrenimTipKod2 == item.Value.OgrenimTipKod).Select(s => s.OgrenimTipKod).ToList());
                }
            }
            model.OgrenimTipModel.EnstituOgrenimTipleri = OgrenimTipleriBus.CmbAktifOgrenimTipleri(_EnstituKod, false, true);



            ViewBag.Kota_BasvuruSurecKontrolTipID = new SelectList(Management.cmbGetKontrolTipleri(true), "Value", "Caption", model.Kota_BasvuruSurecKontrolTipID);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod ?? _EnstituKod);
            ViewBag.OgretimYili = new SelectList(DonemlerBus.GetCmbAkademikTarih(), "Value", "Caption", model.OgretimYili);
            ViewBag.AnketID = new SelectList(Management.cmbGetAktifAnketler(_EnstituKod, true, model.AnketID), "Value", "Caption", model.AnketID);
            ViewBag.KayitOlmayanlarAnketID = new SelectList(Management.cmbGetAktifAnketler(_EnstituKod, true, model.KayitOlmayanlarAnketID), "Value", "Caption", model.KayitOlmayanlarAnketID);
            ViewBag.BsOtoMmail = bsMList;
            return View(model);
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.BasvuruSureciKayit)]
        public ActionResult Kayit(kmBasvuruSurec kModel)
        {
            var MmMessage = new MmMessage();


            var qgID = kModel.gID.Select((s, inx) => new { Key = s, Inx = inx }).ToList();
            var qBasvuruSurecOtoMailID = kModel.BasvuruSurecOtoMailID.Select((s, inx) => new { gID = s.Split('_')[0].ToInt().Value, Key = s.Split('_')[1], Inx = inx }).ToList();
            var qZamanTipID = kModel.ZamanTipID.Select((s, inx) => new { gID = s.Split('_')[0].ToInt().Value, Key = s.Split('_')[1], Inx = inx }).ToList();
            var qZaman = kModel.Zaman.Select((s, inx) => new { gID = s.Split('_')[0].ToInt().Value, Key = s.Split('_')[1], Inx = inx }).ToList();
            var qMailZamanlari = (from s in qgID
                                  join b in qBasvuruSurecOtoMailID on s.Key equals b.gID
                                  join z in qZamanTipID on s.Key equals z.gID
                                  join za in qZaman on s.Key equals za.gID
                                  select new KmBsOtoMail
                                  {
                                      BasvuruSurecID = b.Key.ToInt().Value,
                                      ZamanTipID = z.Key.ToInt().Value,
                                      Zaman = za.Key.ToInt().Value

                                  }).ToList();

            var secilenOtipleris = kModel.SeciliOgrenimTipKod.Where(p => !p.IsNullOrWhiteSpace()).Select(s => new { OgrenimTipKod = s.Split('_')[0].ToInt().Value, SecilenOgrenimTipKod = s.Split('_')[1].ToInt().Value }).ToList();
            var sBSOtipleriD = kModel.BasvuruSurecOgrenimTipID.Select((s, inx) => new { Inx = inx, BasvuruSurecOgrenimTipID = s }).ToList();
            var OgrenimTipleri = kModel.OgrenimTipKods.Select((s, inx) => new { Inx = inx, OgrenimTipKod = s }).ToList();
            var sMulakataGirecek = kModel.MulakatSurecineGirecek.Where(p => !p.IsNullOrWhiteSpace()).Select(s => new { OgrenimTipKod = s.Split('_')[0].ToInt().Value, MulakatSurecineGirecek = s.Split('_')[1].ToBooleanObj().Value }).ToList();
            var sAlanIciBilimselHazirlik = kModel.AlanIciBilimselHazirlik.Where(p => !p.IsNullOrWhiteSpace()).Select(s => new { OgrenimTipKod = s.Split('_')[0].ToInt().Value, AlanIciBilimselHazirlik = s.Split('_')[1].ToBooleanObj().Value }).ToList();
            var sAlanDisiBilimselHazirlik = kModel.AlanDisiBilimselHazirlik.Where(p => !p.IsNullOrWhiteSpace()).Select(s => new { OgrenimTipKod = s.Split('_')[0].ToInt().Value, AlanDisiBilimselHazirlik = s.Split('_')[1].ToBooleanObj().Value }).ToList();
            var sKotalar = kModel.Kota.Select((s, inx) => new { Inx = inx, Kota = s }).ToList();
            var sGBN = kModel.BasariNotOrtalamasi.Select((s, inx) => new { Inx = inx, BasariNotOrtalamasi = s }).ToList();
            var AsilBasTars = kModel.AsilBasTar.Select((s, inx) => new { Inx = inx, AsilBasTar = s }).ToList();
            var AsilBitTars = kModel.AsilBitTar.Select((s, inx) => new { Inx = inx, AsilBitTar = s }).ToList();
            var YedekBasTars = kModel.YedekBasTar.Select((s, inx) => new { Inx = inx, YedekBasTar = s }).ToList();
            var YedekBitTars = kModel.YedekBitTar.Select((s, inx) => new { Inx = inx, YedekBitTar = s }).ToList();


            var SurecOgrenimTipBilgileri = (from ot in OgrenimTipleri
                                            join abat in AsilBasTars on ot.Inx equals abat.Inx
                                            join abit in AsilBitTars on ot.Inx equals abit.Inx
                                            join ybat in YedekBasTars on ot.Inx equals ybat.Inx
                                            join ybit in YedekBitTars on ot.Inx equals ybit.Inx
                                            join ko in sKotalar on ot.Inx equals ko.Inx
                                            join gbn in sGBN on ot.Inx equals gbn.Inx
                                            join sbso in sBSOtipleriD on ot.Inx equals sbso.Inx
                                            select new
                                            {
                                                Inx = ot.Inx,
                                                Secildi = kModel.OgrenimTipKod.Contains(ot.OgrenimTipKod),
                                                abat.AsilBasTar,
                                                abit.AsilBitTar,
                                                ybat.YedekBasTar,
                                                ybit.YedekBitTar,
                                                BasvuruSurecOgrenimTipID = sbso.BasvuruSurecOgrenimTipID,
                                                OgrenimTipKod = ot.OgrenimTipKod,
                                                Kota = ko.Kota,
                                                BasariNotOrtalamasi = gbn.BasariNotOrtalamasi,
                                                SecilenOgrenimTipleri = secilenOtipleris.Where(p => p.OgrenimTipKod == ot.OgrenimTipKod).Select(s => s.SecilenOgrenimTipKod).ToList(),
                                                MulakatSurecineGirecek = sMulakataGirecek.Any(a => a.OgrenimTipKod == ot.OgrenimTipKod),
                                                AlanIciBilimselHazirlik = sAlanIciBilimselHazirlik.Any(a => a.OgrenimTipKod == ot.OgrenimTipKod),
                                                AlanDisiBilimselHazirlik = sAlanDisiBilimselHazirlik.Any(a => a.OgrenimTipKod == ot.OgrenimTipKod),

                                            }).ToList();

            if (kModel.MulakatSinavTurID == null) kModel.MulakatSinavTurID = new List<int>();
            if (kModel.MulakatSinavTurIDSecilen == null) kModel.MulakatSinavTurIDSecilen = new List<int>();
            if (kModel.YuzdeOran == null) kModel.YuzdeOran = new List<int?>();

            var sMulakatSinavTurIDSecilen = kModel.MulakatSinavTurIDSecilen.Select((s, inx) => new CmbMultyTypeDto { Key = s, Inx = inx }).ToList();
            var sMulakatSinavTurID = kModel.MulakatSinavTurID.Select((s, inx) => new CmbMultyTypeDto { Key = s, Inx = inx }).ToList();
            var sYuzdeOran = kModel.YuzdeOran.Select((s, inx) => new { Key = s, Inx = inx }).ToList();

            var bsMS = (from msid in sMulakatSinavTurID
                        join yo in sYuzdeOran on msid.Inx equals yo.Inx

                        select new MulakatSturModel
                        {
                            IndexNo = msid.Inx,
                            Zorunlu = kModel.MulakatSinavTurIDSecilen.Any(a => a == msid.Key),
                            MulakatSinavTurID = msid.Key,
                            YuzdeOran = yo.Key
                        }).ToList();
            var bsMSsecilenler = bsMS.Where(p => p.Zorunlu).ToList();
            #region Kontrol
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                string msg = "Enstitü Seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });

            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });

            if (kModel.BaslangicTarihi == DateTime.MinValue)
            {
                string msg = "Geçerli Bir Başlangıç Tarih Giriniz.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BaslangicTarihi" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BaslangicTarihi" });
            if (kModel.BitisTarihi == DateTime.MinValue)
            {
                string msg = "Geçerli Bir Bitiş Tarih Giriniz.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitisTarihi" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BitisTarihi" });
            if (kModel.ToplamKota <= 0)
            {
                string msg = "Toplam kota bilgisi girişi yapmak zorunludur ve 0 dan büyük olmalıdır.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ToplamKota" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ToplamKota" });

            if (kModel.Kota_BasvuruSurecKontrolTipID <= 0)
            {
                string msg = "Kota hesap tipini seçiniz.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Kota_BasvuruSurecKontrolTipID" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Kota_BasvuruSurecKontrolTipID" });
            if (SurecOgrenimTipBilgileri.Any(a => a.Secildi) == false)
            {
                string msg = "Kayıt işlemini yapabilmek için en az 1 Öğrenim tipi seçimi yapmanız gerekmektedir!";
                MmMessage.Messages.Add(msg);

            }
            else
            {
                if (SurecOgrenimTipBilgileri.Any(a => a.Kota <= 0))
                    MmMessage.Messages.Add("Öğrenim tiplerindeki kota bilgisi boş bırakılamaz ve 0 dan büyük olmalıdır!");
                if (SurecOgrenimTipBilgileri.Any(a => a.BasariNotOrtalamasi <= 0 || a.BasariNotOrtalamasi > 100))
                    MmMessage.Messages.Add("Öğrenim tiplerindeki başarı not ortalaması bilgisi boş bırakılamaz ve en düşük 1 en yüksek 100 değeri olmalıdır!");
                if (kModel.IsBelgeYuklemeVar)
                    if (SurecOgrenimTipBilgileri.Any(a => !a.AsilBasTar.HasValue) ||
                          SurecOgrenimTipBilgileri.Any(a => !a.AsilBitTar.HasValue) ||
                          SurecOgrenimTipBilgileri.Any(a => !a.YedekBasTar.HasValue) ||
                          SurecOgrenimTipBilgileri.Any(a => !a.YedekBitTar.HasValue))
                        MmMessage.Messages.Add("Öğrenim tiplerindeki belge yükleme tarihleri boş bırakılamaz!");
                    else if (SurecOgrenimTipBilgileri.Any(a => a.AsilBasTar > a.AsilBitTar) || SurecOgrenimTipBilgileri.Any(a => a.YedekBasTar > a.YedekBitTar))
                        MmMessage.Messages.Add("Öğrenim tiplerindeki belge yükleme tarihlerini kontrol ediniz!");
            }

            if (MmMessage.Messages.Count == 0)
            {


                if (kModel.BaslangicTarihi >= kModel.BitisTarihi)
                {
                    string msg = "Başlangıç tarihi bitiş tarihinden büyük olamaz!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BaslangicTarihi" });
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitisTarihi" });
                }

                if (MmMessage.Messages.Count == 0 && SurecOgrenimTipBilgileri.Any(a => a.Secildi && a.MulakatSurecineGirecek))
                {

                    if (kModel.SonucGirisBaslangicTarihi.HasValue == false || kModel.SonucGirisBitisTarihi.HasValue == false)
                    {
                        string msg = "Mülakat sonuç giriş başlangıç ve bitiş tarihi boş bırakılamaz";
                        MmMessage.Messages.Add(msg);
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SonucGirisBaslangicTarihi" });
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SonucGirisBitisTarihi" });
                    }
                    else if (kModel.SonucGirisBaslangicTarihi >= kModel.SonucGirisBitisTarihi)
                    {
                        string msg = "Mülakat sonuç giriş Başlangıç tarihi bitiş tarihinden büyük olamaz!";
                        MmMessage.Messages.Add(msg);
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SonucGirisBaslangicTarihi" });
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SonucGirisBitisTarihi" });
                    }
                    else
                    {
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SonucGirisBaslangicTarihi" });
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SonucGirisBitisTarihi" });
                    }

                    if (MmMessage.Messages.Count == 0 && kModel.SonucGirisBaslangicTarihi.HasValue && kModel.SonucGirisBitisTarihi.HasValue)
                    {
                        if (kModel.BitisTarihi > kModel.SonucGirisBaslangicTarihi.Value)
                        {
                            string msg = "Mülakat sonuç giriş başlangıç tarihi başvuru süreç başlangıç tarihinden küçük ya da başvuru süreç tarihleri arasında olamaz!";
                            MmMessage.Messages.Add(msg);
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SonucGirisBaslangicTarihi" });
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SonucGirisBitisTarihi" });
                        }

                    }
                    if (MmMessage.Messages.Count == 0 && SurecOgrenimTipBilgileri.Any(a => a.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans) && (kModel.AGNOGirisBaslangicTarihi.HasValue || kModel.AGNOGirisBitisTarihi.HasValue))
                    {
                        if (kModel.AGNOGirisBaslangicTarihi.HasValue == false || kModel.AGNOGirisBitisTarihi.HasValue == false)
                        {
                            string msg = "AGNO giriş başlangıç ve bitiş tarihi boş bırakılamaz";
                            MmMessage.Messages.Add(msg);
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AGNOGirisBaslangicTarihi" });
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AGNOGirisBitisTarihi" });
                        }
                        else if (kModel.AGNOGirisBaslangicTarihi >= kModel.AGNOGirisBitisTarihi)
                        {
                            string msg = "AGNO giriş Başlangıç tarihi bitiş tarihinden büyük olamaz!";
                            MmMessage.Messages.Add(msg);
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AGNOGirisBaslangicTarihi" });
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AGNOGirisBitisTarihi" });
                        }
                        else if (kModel.BitisTarihi > kModel.AGNOGirisBaslangicTarihi.Value)
                        {
                            string msg = "AGNO giriş başlangıç tarihi başvuru süreç bitiş tarihinden küçük olamaz!";
                            MmMessage.Messages.Add(msg);
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AGNOGirisBaslangicTarihi" });
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AGNOGirisBitisTarihi" });
                        }
                        else if (kModel.SonucGirisBitisTarihi < kModel.AGNOGirisBitisTarihi.Value)
                        {
                            string msg = "AGNO giriş bitiş tarihi mülakat sonuç giriş bitiş tarihinden büyük olamaz!";
                            MmMessage.Messages.Add(msg);
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AGNOGirisBaslangicTarihi" });
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AGNOGirisBitisTarihi" });
                        }
                        else
                        {
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AGNOGirisBaslangicTarihi" });
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AGNOGirisBitisTarihi" });
                        }
                    }



                    if (!kModel.isYuzdeGirisBolum)
                    {
                        foreach (var item in bsMSsecilenler)
                        {
                            if (item.YuzdeOran.HasValue == false || item.YuzdeOran <= 0 || item.YuzdeOran > 100)
                            {
                                string msg = "Seçilen mülakat sınav türünün yüde oranı 0 dan büyük, 100 den küçük ya da eşit olması gerekmektedir!";
                                MmMessage.Messages.Add(msg);
                                item.Success = false;
                            }
                        }


                        if (bsMS.Count == 1)
                        {
                            bsMSsecilenler.First().YuzdeOran = 100;
                            bsMSsecilenler.First().Success = true;
                        }
                        else
                        {
                            var top = bsMS.Sum(s => s.YuzdeOran);
                            if (top != 100)
                            {
                                string msg = "Seçilen mülakat sınav tiplerinin toplam yüzde oranı %100 olması gerekmektedir!";
                                MmMessage.Messages.Add(msg);
                                foreach (var item in bsMSsecilenler)
                                {
                                    item.Success = false;
                                }

                            }
                        }
                    }
                    else
                    {
                        if (bsMSsecilenler.Count == 1)
                        {
                            var item = bsMSsecilenler.First();
                            if (item.YuzdeOran.HasValue == false && (item.YuzdeOran <= 0 || item.YuzdeOran > 100))
                            {
                                string msg = "Zorunlu seçilen mülakat sınav türünün alt sınır yüzdesi 0 dan büyük, 100 den küçük ya da eşit olması gerekmektedir!";
                                MmMessage.Messages.Add(msg);
                                item.Success = false;
                            }
                        }
                        else if (bsMSsecilenler.Count > 2 && (bsMS.Count - bsMSsecilenler.Count > 1))
                        {
                            var secilenHasV = bsMSsecilenler.Where(p => p.YuzdeOran.HasValue == true).ToList();
                            if (secilenHasV.Sum(s => s.YuzdeOran.Value) < 100)
                            {
                                string msg = "Zorunlu seçilen mülakat sınav türünün alt sınır yüzdeleri toplamı %100 den küçük olmalıdır!";
                                MmMessage.Messages.Add(msg);
                                foreach (var item in secilenHasV)
                                {
                                    item.Success = false;
                                }
                            }
                            else
                                foreach (var item in bsMSsecilenler)
                                {
                                    if (item.YuzdeOran.HasValue == false && (item.YuzdeOran <= 0 || item.YuzdeOran > 100))
                                    {
                                        string msg = "Zorunlu seçilen mülakat sınav türünün alt sınır yüzdesi 0 dan büyük, 100 den küçük ya da eşit olması gerekmektedir!";
                                        MmMessage.Messages.Add(msg);
                                        item.Success = false;
                                    }
                                }
                        }


                    }
                }

            }
            var _EOyilBilgi = new EgitimOgretimDonemDto();
            if (kModel.OgretimYili.IsNullOrWhiteSpace() == false)
            {
                var oy = kModel.OgretimYili.Split('/').ToList();
                _EOyilBilgi.BaslangicYili = oy[0].ToInt().Value;
                _EOyilBilgi.BitisYili = oy[1].ToInt().Value;
                _EOyilBilgi.Donem = oy[2].ToInt().Value;
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgretimYili" });
            }
            else
            {

                string msg = "Öğretim yılı seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgretimYili" });

            }

            if (MmMessage.Messages.Count == 0)
            {

                var qBasS = db.BasvuruSurecs.Where(p => p.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru && p.EnstituKod == kModel.EnstituKod && p.BasvuruSurecID != kModel.BasvuruSurecID && ((p.BaslangicTarihi <= kModel.BaslangicTarihi && p.BitisTarihi >= kModel.BaslangicTarihi) || (p.BaslangicTarihi <= kModel.BitisTarihi && p.BitisTarihi >= kModel.BitisTarihi))).Count();
                if (qBasS > 0)
                {
                    string msg = "Girmiş olduğunuz tarihler için daha önceden başvuru süreci kayıt edilmiştir.";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BaslangicTarihi" });
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitisTarihi" });
                }
            }


            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                int OldBSurecID = kModel.BasvuruSurecID;
                bool IsnewOrEdit = kModel.BasvuruSurecID <= 0;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.BaslangicYil = _EOyilBilgi.BaslangicYili;
                kModel.BitisYil = _EOyilBilgi.BitisYili;
                kModel.DonemID = _EOyilBilgi.Donem;
                var BelgeTipleri = new List<BasvuruSurecBelgeTipleri>();
                if (kModel.IsBelgeYuklemeVar)
                {
                    db.BasvuruBelgeTipleris.Where(p => p.IsAktif).ToList().ForEach(f =>
                    {
                        var btR = new BasvuruSurecBelgeTipleri
                        {
                            BasvuruBelgeTipID = f.BasvuruBelgeTipID,
                            IsZorunlu = f.IsZorunlu,
                            IsYerliOrYabanci = f.IsYerliOrYabanci

                        };
                        f.BasvuruBelgeTipleriYuklemeSeklis.ToList().ForEach(fd =>
                        {


                            btR.BasvuruSurecBelgeTipleriYuklemeSeklis.Add(new BasvuruSurecBelgeTipleriYuklemeSekli { BasvuruBelgeTipID = f.BasvuruBelgeTipID, SinavTipKod = fd.SinavTipKod, IsKayitSonrasiGetirilecek = fd.IsKayitSonrasiGetirilecek });
                        });
                        BelgeTipleri.Add(btR);
                    });
                }
                else kModel.IsKayittaBelgeOnayiZorunlu = false;

                bool IsYeniKayit = false;
                var tablo = new BasvuruSurec();
                if (kModel.BasvuruSurecID <= 0)
                {
                    IsYeniKayit = true;
                    tablo = db.BasvuruSurecs.Add(new BasvuruSurec
                    {
                        EnstituKod = kModel.EnstituKod,
                        BasvuruSurecTipID = BasvuruSurecTipi.LisansustuBasvuru,
                        BaslangicYil = kModel.BaslangicYil,
                        BitisYil = kModel.BitisYil,
                        DonemID = kModel.DonemID,
                        Kota_BasvuruSurecKontrolTipID = kModel.Kota_BasvuruSurecKontrolTipID,
                        BaslangicTarihi = kModel.BaslangicTarihi,
                        BitisTarihi = kModel.BitisTarihi,
                        SonucGirisBaslangicTarihi = kModel.SonucGirisBaslangicTarihi,
                        SonucGirisBitisTarihi = kModel.SonucGirisBitisTarihi,
                        AGNOGirisBaslangicTarihi = kModel.AGNOGirisBaslangicTarihi,
                        AGNOGirisBitisTarihi = kModel.AGNOGirisBitisTarihi,
                        ToplamKota = kModel.ToplamKota,
                        FarkliOgrenimTipleriAyniBasvurudaAlinabilsin = kModel.FarkliOgrenimTipleriAyniBasvurudaAlinabilsin,
                        IsAktif = kModel.IsAktif,
                        isYuzdeGirisBolum = kModel.isYuzdeGirisBolum,
                        IslemTarihi = kModel.IslemTarihi,
                        IslemYapanID = kModel.IslemYapanID,
                        IslemYapanIP = kModel.IslemYapanIP,
                        AnketID = kModel.AnketID,
                        KayitOlmayanlarAnketID = kModel.KayitOlmayanlarAnketID,
                        BasvuruSurecBelgeTipleris = BelgeTipleri,
                        IsBelgeYuklemeVar = kModel.IsBelgeYuklemeVar,
                        IsKayittaBelgeOnayiZorunlu = kModel.IsKayittaBelgeOnayiZorunlu,
                        IsOgrenciSonucListesindePuanGozuksun = kModel.IsOgrenciSonucListesindePuanGozuksun
                    });
                    db.SaveChanges();
                    kModel.BasvuruSurecID = tablo.BasvuruSurecID;
                }
                else
                {
                    tablo = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID).First();
                    tablo.EnstituKod = kModel.EnstituKod;
                    tablo.BaslangicYil = kModel.BaslangicYil;
                    tablo.BitisYil = kModel.BitisYil;
                    tablo.Kota_BasvuruSurecKontrolTipID = kModel.Kota_BasvuruSurecKontrolTipID;
                    tablo.ToplamKota = kModel.ToplamKota;
                    tablo.FarkliOgrenimTipleriAyniBasvurudaAlinabilsin = kModel.FarkliOgrenimTipleriAyniBasvurudaAlinabilsin;
                    tablo.DonemID = kModel.DonemID;
                    tablo.IsAktif = kModel.IsAktif;
                    tablo.BaslangicTarihi = kModel.BaslangicTarihi;
                    tablo.BitisTarihi = kModel.BitisTarihi;
                    tablo.SonucGirisBaslangicTarihi = kModel.SonucGirisBaslangicTarihi;
                    tablo.SonucGirisBitisTarihi = kModel.SonucGirisBitisTarihi;
                    tablo.AGNOGirisBaslangicTarihi = kModel.AGNOGirisBaslangicTarihi;
                    tablo.AGNOGirisBitisTarihi = kModel.AGNOGirisBitisTarihi;
                    tablo.isYuzdeGirisBolum = kModel.isYuzdeGirisBolum;
                    tablo.AnketID = kModel.AnketID;
                    tablo.KayitOlmayanlarAnketID = kModel.KayitOlmayanlarAnketID;
                    tablo.IsBelgeYuklemeVar = kModel.IsBelgeYuklemeVar;
                    tablo.IsKayittaBelgeOnayiZorunlu = kModel.IsKayittaBelgeOnayiZorunlu;
                    tablo.IsOgrenciSonucListesindePuanGozuksun = kModel.IsOgrenciSonucListesindePuanGozuksun;
                    tablo.IslemTarihi = DateTime.Now;
                    tablo.IslemYapanID = kModel.IslemYapanID;
                    tablo.IslemYapanIP = kModel.IslemYapanIP;

                    db.BasvuruSurecBelgeTipleris.RemoveRange(tablo.BasvuruSurecBelgeTipleris);
                    tablo.BasvuruSurecBelgeTipleris = BelgeTipleri;

                }

                var silinenler = db.BasvuruSurecOgrenimTipleris.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID).ToList();
                db.BasvuruSurecOgrenimTipleris.RemoveRange(silinenler);
                var Eklenecekler = new List<BasvuruSurecOgrenimTipleri>();
                foreach (var item in SurecOgrenimTipBilgileri)
                {
                    var ot = db.OgrenimTipleris.Where(p => p.EnstituKod == kModel.EnstituKod && p.OgrenimTipKod == item.OgrenimTipKod).First();
                    var eklenecek = db.BasvuruSurecOgrenimTipleris.Add(new BasvuruSurecOgrenimTipleri
                    {
                        BasvuruSurecID = kModel.BasvuruSurecID,
                        OgrenimTipKod = item.OgrenimTipKod,
                        GrupGoster = ot.GrupGoster,
                        GrupKodu = ot.GrupKodu,
                        Kota = item.Kota,
                        BasariNotOrtalamasi = item.BasariNotOrtalamasi,
                        GBNFormulu = ot.GBNFormulu,
                        GBNFormuluAlessiz = ot.GBNFormuluAlessiz,
                        GBNFormuluMulakatsiz = ot.GBNFormuluMulakatsiz,
                        GBNFormuluD = ot.GBNFormuluD,
                        GBNFormuluDDosyasiz = ot.GBNFormuluDDosyasiz,
                        GBNFormuluDMulakatsiz = ot.GBNFormuluDMulakatsiz,
                        LEgitimBilgisiIste = ot.LEgitimBilgisiIste,
                        YLEgitimBilgisiIste = ot.YLEgitimBilgisiIste,

                        MulakatSurecineGirecek = item.MulakatSurecineGirecek,
                        AlanIciBilimselHazirlik = item.AlanIciBilimselHazirlik,
                        AlanDisiBilimselHazirlik = item.AlanDisiBilimselHazirlik,
                        YokOgrenciKontroluYap = ot.YokOgrenciKontroluYap,
                        IstenecekKatkiPayiTutari = ot.IstenecekKatkiPayiTutari,
                        YedekOgrenciSayisiKotaCarpani = ot.YedekOgrenciSayisiKotaCarpani,
                        BelgeYuklemeAsilBasTar = item.AsilBasTar,
                        BelgeYuklemeAsilBitTar = item.AsilBitTar,
                        BelgeYuklemeYedekBasTar = item.YedekBasTar,
                        BelgeYuklemeYedekBitTar = item.YedekBitTar,
                        IsAktif = item.Secildi,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip

                    });
                    Eklenecekler.Add(eklenecek);
                    var ots = db.BasvuruSurecOTOrtBasvrs.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID).ToList();
                    if (ots.Count > 0) db.BasvuruSurecOTOrtBasvrs.RemoveRange(ots);
                    foreach (var item2 in item.SecilenOgrenimTipleri)
                    {
                        db.BasvuruSurecOTOrtBasvrs.Add(new BasvuruSurecOTOrtBasvr { BasvuruSurecID = kModel.BasvuruSurecID, OgrenimTipKod = item.OgrenimTipKod, OgrenimTipKod2 = item2 });
                    }
                }
                var bsurecMGST = db.BasvuruSurecMulakatSinavTurleris.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID).ToList();
                db.BasvuruSurecMulakatSinavTurleris.RemoveRange(bsurecMGST);
                foreach (var item in bsMS)
                {
                    db.BasvuruSurecMulakatSinavTurleris.Add(new BasvuruSurecMulakatSinavTurleri
                    {
                        BasvuruSurecID = kModel.BasvuruSurecID,
                        MulakatSinavTurID = item.MulakatSinavTurID,
                        YuzdeOran = item.YuzdeOran,
                        Zorunlu = item.Zorunlu
                    });
                }
                var zamanList = db.BasvuruSurecOtoMails.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID).ToList();
                var silinecekler = zamanList.Where(p => !qMailZamanlari.Any(a => a.ZamanTipID == p.ZamanTipID && a.Zaman == p.Zaman)).ToList();
                var eklenecekle = qMailZamanlari.Where(p => !zamanList.Any(a => a.ZamanTipID == p.ZamanTipID && a.Zaman == p.Zaman)).ToList();

                db.BasvuruSurecOtoMails.RemoveRange(silinecekler);
                db.SaveChanges();
                foreach (var item in eklenecekle)
                {
                    db.BasvuruSurecOtoMails.Add(new BasvuruSurecOtoMail
                    {
                        BasvuruSurecID = kModel.BasvuruSurecID,
                        ZamanTipID = item.ZamanTipID,
                        Zaman = item.Zaman,
                        Gonderildi = item.Gonderildi,
                        GonderilenCount = item.GonderilenCount,
                        Gonderilenler = item.Gonderilenler
                    });

                }

                db.SaveChanges();
                LogIslemleri.LogEkle("BasvuruSurec", IsYeniKayit ? IslemTipi.Insert : IslemTipi.Update, tablo.ToJson());
                LogIslemleri.LogEkle("BasvuruSurecOgrenimTipleri", IsYeniKayit ? IslemTipi.Insert : IslemTipi.Update, Eklenecekler.ToJson());
                if (IsYeniKayit)
                {
                    KBKopyala(kModel.BasvuruSurecID, kModel.EnstituKod);
                    SBKopyala(kModel.BasvuruSurecID, kModel.EnstituKod);
                }
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
            kModel.MulakatSTurModel = (from ms in db.MulakatSinavTurleris
                                       join bsm in db.BasvuruSurecMulakatSinavTurleris on new { ms.MulakatSinavTurID, kModel.BasvuruSurecID } equals new { bsm.MulakatSinavTurID, bsm.BasvuruSurecID } into defbsm
                                       from bsM in defbsm.DefaultIfEmpty()
                                       orderby ms.MulakatSinavTurAdi
                                       select new MulakatSturModel
                                       {
                                           Zorunlu = bsM != null ? bsM.Zorunlu : false,
                                           MulakatSinavTurID = ms.MulakatSinavTurID,
                                           SinavTurAdi = ms.MulakatSinavTurAdi,
                                           YuzdeOran = bsM != null ? bsM.YuzdeOran : ms.YuzdeOran
                                       }).ToList();

            foreach (var item in kModel.MulakatSTurModel)
            {
                var itemMsT = bsMS.Where(p => p.MulakatSinavTurID == item.MulakatSinavTurID && p.Zorunlu).FirstOrDefault();
                if (itemMsT != null)
                {
                    item.YuzdeOran = kModel.isYuzdeGirisBolum ? (int?)null : itemMsT.YuzdeOran;
                    item.Zorunlu = true;
                    item.Success = itemMsT.Success;
                }
                else
                {
                    item.Zorunlu = false;
                }
            }

            kModel.OgrenimTipModel.IsBelgeYuklemeVar = kModel.IsBelgeYuklemeVar;
            kModel.OgrenimTipModel.OgrenimTipleriDataList = (from o in db.OgrenimTipleris.Where(p => p.EnstituKod == kModel.EnstituKod)
                                                             join s in db.BasvuruSurecOgrenimTipleris on new { o.OgrenimTipKod, kModel.BasvuruSurecID } equals new { s.OgrenimTipKod, s.BasvuruSurecID } into def1
                                                             from defS in def1.DefaultIfEmpty()
                                                             where o.IsAktif || defS.IsAktif
                                                             select new CheckObject<KrOgrenimTip>
                                                             {
                                                                 Value = new KrOgrenimTip
                                                                 {

                                                                     EnstituKod = kModel.EnstituKod,
                                                                     BasvuruSurecOgrenimTipID = defS != null ? defS.BasvuruSurecOgrenimTipID : 0,
                                                                     OrjinalVeri = defS != null,
                                                                     OgrenimTipKod = o.OgrenimTipKod,
                                                                     GrupGoster = defS != null ? defS.GrupGoster : o.GrupGoster,
                                                                     GrupKodu = defS != null ? defS.GrupKodu : o.GrupKodu,
                                                                     Kota = defS != null ? defS.Kota : o.Kota,
                                                                     GBNFormulu = defS != null ? defS.GBNFormulu : o.GBNFormulu,
                                                                     GBNFormuluAlessiz = defS != null ? defS.GBNFormuluAlessiz : o.GBNFormuluAlessiz,
                                                                     GBNFormuluMulakatsiz = defS != null ? defS.GBNFormuluMulakatsiz : o.GBNFormuluMulakatsiz,
                                                                     GBNFormuluD = defS != null ? defS.GBNFormuluD : o.GBNFormuluD,
                                                                     GBNFormuluDDosyasiz = defS != null ? defS.GBNFormuluDDosyasiz : o.GBNFormuluDDosyasiz,
                                                                     GBNFormuluDMulakatsiz = defS != null ? defS.GBNFormuluDMulakatsiz : o.GBNFormuluDMulakatsiz,
                                                                     BasariNotOrtalamasi = defS != null ? defS.BasariNotOrtalamasi : o.BasariNotOrtalamasi,
                                                                     LEgitimBilgisiIste = defS != null ? defS.LEgitimBilgisiIste : o.LEgitimBilgisiIste,
                                                                     YLEgitimBilgisiIste = defS != null ? defS.YLEgitimBilgisiIste : o.YLEgitimBilgisiIste,
                                                                     MulakatSurecineGirecek = defS != null ? defS.MulakatSurecineGirecek : o.MulakatSurecineGirecek,
                                                                     AlanIciBilimselHazirlik = defS != null ? defS.AlanIciBilimselHazirlik : o.AlanIciBilimselHazirlik,
                                                                     AlanDisiBilimselHazirlik = defS != null ? defS.AlanDisiBilimselHazirlik : o.AlanDisiBilimselHazirlik,
                                                                     YedekOgrenciSayisiKotaCarpani = defS != null ? defS.YedekOgrenciSayisiKotaCarpani : o.YedekOgrenciSayisiKotaCarpani,
                                                                     OgrenimTipAdi = o.OgrenimTipAdi,
                                                                     GrupAdi = o.GrupAdi
                                                                 },
                                                                 Checked = kModel.OgrenimTipKod.Contains(o.OgrenimTipID)
                                                             }).ToList();
            foreach (var item in kModel.OgrenimTipModel.OgrenimTipleriDataList)
            {
                var sItem = SurecOgrenimTipBilgileri.Where(p => p.OgrenimTipKod == item.Value.OgrenimTipKod).First();
                item.Value.BelgeYuklemeAsilBasTar = sItem.AsilBasTar;
                item.Value.BelgeYuklemeAsilBitTar = sItem.AsilBitTar;
                item.Value.BelgeYuklemeYedekBasTar = sItem.YedekBasTar;
                item.Value.BelgeYuklemeYedekBitTar = sItem.YedekBitTar;
                item.Value.Kota = sItem.Kota;
                item.Value.BasariNotOrtalamasi = sItem.BasariNotOrtalamasi;
                item.Value.AlanIciBilimselHazirlik = sItem.AlanIciBilimselHazirlik;
                item.Value.AlanDisiBilimselHazirlik = sItem.AlanDisiBilimselHazirlik;
                item.Value.SecilenBSOTIDs = secilenOtipleris.Where(p => p.OgrenimTipKod == item.Value.OgrenimTipKod).Select(s => s.SecilenOgrenimTipKod).ToList();

                if (item.Checked == true)
                {
                    if (item.Value.Kota <= 0) item.Value.Success = false;
                    if (item.Value.BasariNotOrtalamasi <= 0 || item.Value.BasariNotOrtalamasi > 100) item.Value.Success = false;
                    if (!item.Value.BelgeYuklemeAsilBasTar.HasValue || !item.Value.BelgeYuklemeAsilBitTar.HasValue || !item.Value.BelgeYuklemeYedekBasTar.HasValue || !item.Value.BelgeYuklemeYedekBitTar.HasValue)
                        item.Value.Success = false;
                    if (item.Value.Success != false && (item.Value.BelgeYuklemeAsilBasTar > item.Value.BelgeYuklemeAsilBitTar || item.Value.BelgeYuklemeYedekBasTar > item.Value.BelgeYuklemeYedekBitTar))
                        item.Value.Success = false;
                }
            }
            kModel.OgrenimTipModel.EnstituOgrenimTipleri = OgrenimTipleriBus.CmbAktifOgrenimTipleri(kModel.EnstituKod, false, true);

            var bsMList = Management.getBsMailZamanData();
            ViewBag.BsOtoMmail = bsMList;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.Kota_BasvuruSurecKontrolTipID = new SelectList(Management.cmbGetKontrolTipleri(true), "Value", "Caption", kModel.Kota_BasvuruSurecKontrolTipID);
            ViewBag.OgretimYili = new SelectList(DonemlerBus.GetCmbAkademikTarih(), "Value", "Caption", kModel.OgretimYili);
            ViewBag.AnketID = new SelectList(Management.cmbGetAktifAnketler(kModel.EnstituKod, true, kModel.AnketID), "Value", "Caption", kModel.AnketID);
            ViewBag.KayitOlmayanlarAnketID = new SelectList(Management.cmbGetAktifAnketler(kModel.EnstituKod, true, kModel.KayitOlmayanlarAnketID), "Value", "Caption", kModel.KayitOlmayanlarAnketID);
            ViewBag.MmMessage = MmMessage;
            return View(kModel);
        }
        [Authorize(Roles = RoleNames.BasvuruSureciKayit)]
        public ActionResult Kopyala(int basvuruSurecID, string EnstituKod, bool IsSinavOrKota)
        {
            var mmMessage = new MmMessage();
            mmMessage.Title = (IsSinavOrKota ? "Sınav" : "Kota") + " Bilgileri Kopyalama İşlemi";
            string Msg = "";
            try
            {
                if (IsSinavOrKota) SBKopyala(basvuruSurecID, EnstituKod);
                else KBKopyala(basvuruSurecID, EnstituKod);
                mmMessage.IsSuccess = true;
                Msg = (IsSinavOrKota ? "Sınav" : "Kota") + " Bilgileri Kopyalandı!";
            }
            catch (Exception ex)
            {
                Msg = (IsSinavOrKota ? "Sınav" : "Kota") + " Bilgileri Kopyalanırken Bir Hata Oluştu! </br>Hata:" + ex.ToExceptionMessage();
                SistemBilgilendirmeBus.SistemBilgisiKaydet(ex, Msg, LogType.Hata);
            }
            mmMessage.Messages.Add(Msg);
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public void SBKopyala(int basvuruSurecID, string EnstituKod)
        {
            var bsstOld = db.BasvuruSurecSinavTipleris.Where(p => p.BasvuruSurecID == basvuruSurecID).ToList();
            db.BasvuruSurecSinavTipleris.RemoveRange(bsstOld);
            var bssSOtNotAralik = db.BasvuruSurecSinavTipleriOTNotAraliklaris.Where(p => p.BasvuruSurecID == basvuruSurecID).ToList();
            db.BasvuruSurecSinavTipleriOTNotAraliklaris.RemoveRange(bssSOtNotAralik);
            var sinavlar = db.SinavTipleris.Where(p => p.EnstituKod == EnstituKod && p.IsAktif).ToList();

            var itemSPNDel = db.BasvuruSurecSinavTipleriOT_SNA.Where(p => p.BasvuruSurecID == basvuruSurecID).ToList();
            db.BasvuruSurecSinavTipleriOT_SNA.RemoveRange(itemSPNDel);
            var EklenenSinavTipleris = new List<BasvuruSurecSinavTipleri>();
            foreach (var item in sinavlar)
            {
                var bsst = db.BasvuruSurecSinavTipleris.Add(new BasvuruSurecSinavTipleri
                {
                    BasvuruSurecID = basvuruSurecID,
                    SinavTipID = item.SinavTipID,
                    EnstituKod = item.EnstituKod,
                    SinavTipGrupID = item.SinavTipGrupID,
                    SinavTipKod = item.SinavTipKod,
                    //LocalService = item.LocalService,
                    WebService = item.WebService,
                    WebServiceKod = item.WebServiceKod,
                    WsSinavCekimTipID = item.WsSinavCekimTipID,
                    TarihGirisMaxGecmisYil = item.TarihGirisMaxGecmisYil,
                    OzelTarih = item.OzelTarih,
                    OzelTarihTipID = item.OzelTarihTipID,
                    Tarih1 = item.Tarih1,
                    Tarih2 = item.Tarih2,
                    OzelNot = item.OzelNot,
                    OzelNotTipID = item.OzelNotTipID,
                    NotDonusum = item.NotDonusum,
                    NotDonusumFormulu = item.NotDonusumFormulu,
                    KusuratVar = item.KusuratVar,
                    Min = item.Min,
                    Max = item.Max,
                    GIsTaahhutVar = item.WebService ? false : item.GIsTaahhutVar,
                    IsAktif = item.IsAktif,
                    IslemTarihi = DateTime.Now,
                    IslemYapanID = UserIdentity.Current.Id,
                    IslemYapanIP = UserIdentity.Ip,
                    BasvuruSurecSinavTipleriDils = item.SinavTipleriDils.Select(s => new BasvuruSurecSinavTipleriDil
                    {
                        SinavDilID = s.SinavDilID
                    }).ToList(),
                    BasvuruSurecSinavTiplerSubSinavAraliks = item.SinavTiplerSubSinavAraliks.Select(s => new BasvuruSurecSinavTiplerSubSinavAralik
                    {
                        SubSinavAralikID = s.SubSinavAralikID,
                        SubSinavAralikAdi = s.SubSinavAralikAdi,
                        SubSinavMin = s.SubSinavMin,
                        SubSinavMax = s.SubSinavMax,
                        NotDonusum = s.NotDonusum,
                        NotDonusumFormulu = s.NotDonusumFormulu

                    }).ToList(),
                    BasvuruSurecSinavTipleriDonems = item.SinavTipleriDonems.Select(s => new BasvuruSurecSinavTipleriDonem
                    {
                        Yil = s.Yil,
                        SinavDilID = s.SinavDilID,
                        WsDonemKod = s.WsDonemKod,
                        WsDonemAd = s.WsDonemAd,
                        IsTaahhutVar = s.IsTaahhutVar
                    }).ToList(),
                    BasvuruSurecSinavTarihleris = item.SinavTarihleris.Select(s => new BasvuruSurecSinavTarihleri
                    {
                        SinavTarihi = s.SinavTarihi

                    }).ToList(),
                    BasvuruSurecSinavNotlaris = item.SinavNotlaris.Select(s => new BasvuruSurecSinavNotlari
                    {
                        SinavNotAdi = s.SinavNotAdi,
                        SinavNotDeger = s.SinavNotDeger
                    }).ToList(),

                });
                EklenenSinavTipleris.Add(bsst);
                var stOtNotAr = item.SinavTipleriOTNotAraliklaris.Where(p => p.EnstituKod == EnstituKod).Select(s2 =>
                    new BasvuruSurecSinavTipleriOTNotAraliklari
                    {
                        BasvuruSurecID = basvuruSurecID,
                        OgrenimTipKod = s2.OgrenimTipKod,
                        SinavTipID = item.SinavTipID,
                        Ingilizce = s2.Ingilizce,
                        IsGecerli = s2.IsGecerli,
                        IsIstensin = s2.IsIstensin,
                        IsOzelNotAralik = s2.IsOzelNotAralik,
                        Min = s2.Min,
                        Max = s2.Max,
                        BasvuruSurecSinavTipleriOTNotAraliklariGecersizProgramlars = s2.SinavTipleriOTNotAraliklariGecersizProgramlars.Select(s => new BasvuruSurecSinavTipleriOTNotAraliklariGecersizProgramlar
                        {
                            ProgramKod = s.ProgramKod

                        }).ToList(),
                    }).ToList();
                db.BasvuruSurecSinavTipleriOTNotAraliklaris.AddRange(stOtNotAr);

                if (item.SinavTipleriOT_SNA.Count > 0)
                {
                    var _SinavTipleriOT_SNA = item.SinavTipleriOT_SNA.Select(s2 => new BasvuruSurecSinavTipleriOT_SNA
                    {
                        BasvuruSurecID = basvuruSurecID,
                        SinavTipID = s2.SinavTipID,
                        BasvuruSurecSinavTipleriOT_SNA_PR = s2.SinavTipleriOT_SNA_PR.Select(s => new BasvuruSurecSinavTipleriOT_SNA_PR
                        {

                            ProgramKod = s.ProgramKod

                        }).ToList(),
                        BasvuruSurecSinavTipleriOT_SNA_OT = s2.SinavTipleriOT_SNA_OT.Select(s => new BasvuruSurecSinavTipleriOT_SNA_OT
                        {
                            Ingilizce = s.Ingilizce,
                            IsGecerli = s.IsGecerli,
                            IsIstensin = s.IsIstensin,
                            IsOzelNotAralik = s.IsOzelNotAralik,
                            Max = s.Max,
                            Min = s.Min,
                            OgrenimTipKod = s.OgrenimTipKod
                        }).ToList(),
                    }).ToList();
                    db.BasvuruSurecSinavTipleriOT_SNA.AddRange(_SinavTipleriOT_SNA);
                }

            }
            db.SaveChanges();

            LogIslemleri.LogEkle("BasvuruSurecSinavTipleri", IslemTipi.Delete, bsstOld.ToJson());
            LogIslemleri.LogEkle("BasvuruSurecSinavTipleri", IslemTipi.Insert, EklenenSinavTipleris.ToJson());

        }
        public void KBKopyala(int basvuruSurecID, string EnstituKod)
        {
            var Surec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == basvuruSurecID).First();
            var bsstOld = Surec.BasvuruSurecKotalars.ToList();
            db.BasvuruSurecKotalars.RemoveRange(bsstOld);

            var bsurecOts = db.BasvuruSurecOgrenimTipleris.Where(p => p.BasvuruSurecID == basvuruSurecID && p.IsAktif).Select(s => s.OgrenimTipKod).Distinct().ToList();
            var kotalar = (from s in db.Kotalars.Where(p => p.BasvuruSurecTipID == Surec.BasvuruSurecTipID)
                           join pr in db.Programlars on s.ProgramKod equals pr.ProgramKod
                           where s.EnstituKod == EnstituKod && bsurecOts.Contains(s.OgrenimTipKod)
                           select new
                           {
                               s.OgrenimTipKod,
                               s.KotaID,
                               s.ProgramKod,
                               s.OrtakKota,
                               s.OrtakKotaSayisi,
                               s.AlanIciKota,
                               s.AlanDisiKota,
                               s.MinAles,
                               s.MulakatSurecineGirecek,
                               s.IsAlesYerineDosyaNotuIstensin,
                               pr.Ingilizce,
                               pr.IsAlandisiBolumKisitlamasi,
                               pr.ProgramlarAlandisiBolumKisitlamalaris,
                               pr.YokOgrenciKontroluYap,
                               MinAGNO = (s.OgrenimTipKod == OgrenimTipi.Doktra ? pr.DAgnoKriteri : (s.OgrenimTipKod == OgrenimTipi.ButunlesikDoktora ? pr.BDAgnoKriteri : (s.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans ? pr.TYLAgnoKriteri : (s.OgrenimTipKod == OgrenimTipi.TezsizYuksekLisans ? pr.YLAgnoKriteri : 0))))
                           }
                          ).ToList();
            var stAlesMinPr = db.SinavTipleriOT_SNA_OT.Where(p => p.SinavTipleriOT_SNA.SinavTipID == 37).ToList();

            var EklenenKotalar = new List<BasvuruSurecKotalar>();
            foreach (var item in kotalar)
            {
                var prMinAles = stAlesMinPr.Where(p => p.OgrenimTipKod == item.OgrenimTipKod && p.Ingilizce == item.Ingilizce && p.SinavTipleriOT_SNA.SinavTipleriOT_SNA_PR.Any(a => a.ProgramKod == item.ProgramKod)).FirstOrDefault();
                var bsst = db.BasvuruSurecKotalars.Add(new BasvuruSurecKotalar
                {
                    BasvuruSurecID = basvuruSurecID,
                    OgrenimTipKod = item.OgrenimTipKod,
                    KotaID = item.KotaID,
                    ProgramKod = item.ProgramKod,
                    OrtakKota = item.OrtakKota,
                    OrtakKotaSayisi = item.OrtakKotaSayisi,
                    AlanIciKota = item.AlanIciKota,
                    AlanDisiKota = item.AlanDisiKota,
                    MinAles = prMinAles != null ? prMinAles.Min : null,
                    MinAGNO = item.MinAGNO,
                    MulakatSurecineGirecek = item.MulakatSurecineGirecek,
                    IsAlesYerineDosyaNotuIstensin = item.IsAlesYerineDosyaNotuIstensin,
                    IsAlandisiBolumKisitlamasi = item.IsAlandisiBolumKisitlamasi,
                    YokOgrenciKontroluYap = item.YokOgrenciKontroluYap


                });
                EklenenKotalar.Add(bsst);
                if (item.IsAlandisiBolumKisitlamasi)
                {
                    db.BasvuruSurecProgramlarAlandisiBolumKisitlamalaris.RemoveRange(db.BasvuruSurecProgramlarAlandisiBolumKisitlamalaris.Where(p => p.BasvuruSurecID == basvuruSurecID && p.ProgramKod == item.ProgramKod));
                    db.BasvuruSurecProgramlarAlandisiBolumKisitlamalaris.AddRange(item.ProgramlarAlandisiBolumKisitlamalaris.Select(s => new BasvuruSurecProgramlarAlandisiBolumKisitlamalari
                    {
                        BasvuruSurecID = basvuruSurecID,
                        ProgramKod = item.ProgramKod,
                        OgrenciBolumID = s.OgrenciBolumID

                    }));
                }
            }
            db.SaveChanges();
            LogIslemleri.LogEkle("BasvuruSurecKotalar", IslemTipi.Delete, bsstOld.ToJson());
            LogIslemleri.LogEkle("BasvuruSurecKotalar", IslemTipi.Insert, EklenenKotalar.ToJson());
        }
        public ActionResult getBsDetail(int id, int tbInx, bool IsDelete)
        {
            var mdl = (from s in db.BasvuruSurecs.Where(p => p.BasvuruSurecID == id)
                       join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                       join e in db.Enstitulers on s.EnstituKod equals e.EnstituKod
                       join d in db.Donemlers on s.DonemID equals d.DonemID
                       select new BasvuruSurecDetayDto
                       {
                           EnstituKod = s.EnstituKod,
                           EnstituAdi = e.EnstituAd,
                           BaslangicYil = s.BaslangicYil,
                           BitisYil = s.BitisYil,
                           AnketID = s.AnketID,
                           DonemID = s.DonemID,
                           DonemAdi = d.DonemAdi,
                           Kota_BasvuruSurecKontrolTipID = s.Kota_BasvuruSurecKontrolTipID,
                           Kota_BasvuruSurecKontrolTipAdi = s.BasvuruSurecKontrolTipleri.BasvuruSurecKontrolTipAdi,
                           BasvuruSurecID = s.BasvuruSurecID,
                           BaslangicTarihi = s.BaslangicTarihi,
                           BitisTarihi = s.BitisTarihi,
                           AGNOGirisBaslangicTarihi = s.AGNOGirisBaslangicTarihi,
                           AGNOGirisBitisTarihi = s.AGNOGirisBitisTarihi,
                           ToplamKota = s.ToplamKota,
                           FarkliOgrenimTipleriAyniBasvurudaAlinabilsin = s.FarkliOgrenimTipleriAyniBasvurudaAlinabilsin,
                           SonucGirisBaslangicTarihi = s.SonucGirisBaslangicTarihi,
                           SonucGirisBitisTarihi = s.SonucGirisBitisTarihi,
                           SinavYerBilgisiOgrenciMailiGonderildi = s.SinavYerBilgisiOgrenciMailiGonderildi,
                           SinavYerBilgisiOgrenciMailiGonderimTarihi = s.SinavYerBilgisiOgrenciMailiGonderimTarihi,
                           SinavYerBilgisiBolumMailiGonderildi = s.SinavYerBilgisiBolumMailiGonderildi,
                           SinavYerBilgisiBolumMailiGonderimTarihi = s.SinavYerBilgisiBolumMailiGonderimTarihi,
                           SinavNotBilgisiBolumMailiGonderildi = s.SinavNotBilgisiBolumMailiGonderildi,
                           SinavNotBilgisiBolumMailiGonderimTarihi = s.SinavNotBilgisiBolumMailiGonderimTarihi,
                           SinavSonucBilgisiOgrenciMailiGonderildi = s.SinavSonucBilgisiOgrenciMailiGonderildi,
                           SinavSonucBilgisiOgrenciMailiGonderimTarihi = s.SinavSonucBilgisiOgrenciMailiGonderimTarihi,
                           KayitOlmayanOgrencilereAnketLinkiGonderildi = s.KayitOlmayanOgrencilereAnketLinkiGonderildi,
                           KayitOlmayanOgrencilereAnketLinkiGonderimTarihi = s.KayitOlmayanOgrencilereAnketLinkiGonderimTarihi,
                           SinavSonucBilgisiBolumMailiGonderildi = s.SinavSonucBilgisiBolumMailiGonderildi,
                           SinavSonucBilgisiBolumMailiGonderimTarihi = s.SinavSonucBilgisiBolumMailiGonderimTarihi,
                           IsAktif = s.IsAktif,
                           IslemTarihi = s.IslemTarihi,
                           IslemYapanID = s.IslemYapanID,
                           IslemYapan = (k.Ad + " " + k.Soyad),
                           IslemYapanIP = s.IslemYapanIP
                       }).First();

            ViewBag.IsDelete = IsDelete;
            mdl.SelectedTabIndex = tbInx;

            return View(mdl);
        }
        public ActionResult getOtBilgi(string EnstituKod, int BasvuruSurecID)
        {
            var Model = new kmBasvuruSurecOgrenimTipModel();
            Model.OgrenimTipleriDataList = (from o in db.OgrenimTipleris.Where(p => p.EnstituKod == EnstituKod)
                                            join s in db.BasvuruSurecOgrenimTipleris on new { o.OgrenimTipKod, BasvuruSurecID } equals new { s.OgrenimTipKod, s.BasvuruSurecID } into def1
                                            from defS in def1.DefaultIfEmpty()
                                            where o.IsAktif || defS.IsAktif
                                            select new CheckObject<KrOgrenimTip>
                                            {
                                                Value = new KrOgrenimTip
                                                {
                                                    EnstituKod = EnstituKod,
                                                    BasvuruSurecOgrenimTipID = defS != null ? defS.BasvuruSurecOgrenimTipID : 0,
                                                    OrjinalVeri = defS == null,
                                                    OgrenimTipKod = o.OgrenimTipKod,
                                                    GrupGoster = defS != null ? defS.GrupGoster : o.GrupGoster,
                                                    GrupKodu = defS != null ? defS.GrupKodu : o.GrupKodu,
                                                    Kota = defS != null ? defS.Kota : o.Kota,
                                                    LEgitimBilgisiIste = defS != null ? defS.LEgitimBilgisiIste : o.LEgitimBilgisiIste,
                                                    YLEgitimBilgisiIste = defS != null ? defS.YLEgitimBilgisiIste : o.YLEgitimBilgisiIste,
                                                    BasariNotOrtalamasi = defS != null ? defS.BasariNotOrtalamasi : o.BasariNotOrtalamasi,
                                                    MulakatSurecineGirecek = defS != null ? defS.MulakatSurecineGirecek : o.MulakatSurecineGirecek,
                                                    AlanIciBilimselHazirlik = defS != null ? defS.AlanIciBilimselHazirlik : o.AlanIciBilimselHazirlik,
                                                    AlanDisiBilimselHazirlik = defS != null ? defS.AlanDisiBilimselHazirlik : o.AlanDisiBilimselHazirlik,
                                                    YedekOgrenciSayisiKotaCarpani = defS != null ? defS.YedekOgrenciSayisiKotaCarpani : o.YedekOgrenciSayisiKotaCarpani,
                                                    OgrenimTipAdi = o.OgrenimTipAdi,
                                                    GrupAdi = o.GrupAdi
                                                },
                                                Checked = db.BasvuruSurecOgrenimTipleris.Any(p => p.BasvuruSurecID == BasvuruSurecID && p.OgrenimTipKod == o.OgrenimTipKod && defS.IsAktif)
                                            }).ToList();
            foreach (var item in Model.OgrenimTipleriDataList)
            {
                if (item.Value.OrjinalVeri)
                {
                    item.Value.SecilenBSOTIDs.AddRange(db.OgrenimTipleriOrtBasvrs.Where(p => p.OgrenimTipKod == item.Value.OgrenimTipKod).Select(s => s.OgrenimTipKod2).ToList());
                    item.Value.SecilenBSOTIDs.AddRange(db.OgrenimTipleriOrtBasvrs.Where(p => p.OgrenimTipKod2 == item.Value.OgrenimTipKod).Select(s => s.OgrenimTipKod).ToList());

                }
                else
                {
                    item.Value.SecilenBSOTIDs.AddRange(db.BasvuruSurecOTOrtBasvrs.Where(p => p.BasvuruSurecID == BasvuruSurecID && p.OgrenimTipKod == item.Value.OgrenimTipKod).Select(s => s.OgrenimTipKod2).ToList());
                    item.Value.SecilenBSOTIDs.AddRange(db.BasvuruSurecOTOrtBasvrs.Where(p => p.BasvuruSurecID == BasvuruSurecID && p.OgrenimTipKod2 == item.Value.OgrenimTipKod).Select(s => s.OgrenimTipKod).ToList());
                }
            }
            Model.EnstituOgrenimTipleri = OgrenimTipleriBus.CmbAktifOgrenimTipleri(EnstituKod, false, true);
            return View(Model);
        }
        public ActionResult getBsSubData(int id, int tbInx, bool IsDelete)
        {
            string page = "";
            var mdl = (from s in db.BasvuruSurecs.Where(p => p.BasvuruSurecID == id)
                       join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                       join e in db.Enstitulers on s.EnstituKod equals e.EnstituKod
                       join d in db.Donemlers on s.DonemID equals d.DonemID
                       select new BasvuruSurecDetayDto
                       {
                           EnstituKod = s.EnstituKod,
                           EnstituAdi = e.EnstituAd,
                           BaslangicYil = s.BaslangicYil,
                           BitisYil = s.BitisYil,
                           AnketID = s.AnketID,
                           DonemID = s.DonemID,
                           DonemAdi = d.DonemAdi,
                           Kota_BasvuruSurecKontrolTipID = s.Kota_BasvuruSurecKontrolTipID,
                           Kota_BasvuruSurecKontrolTipAdi = s.BasvuruSurecKontrolTipleri.BasvuruSurecKontrolTipAdi,
                           BasvuruSurecID = s.BasvuruSurecID,
                           BaslangicTarihi = s.BaslangicTarihi,
                           BitisTarihi = s.BitisTarihi,
                           AGNOGirisBaslangicTarihi = s.AGNOGirisBaslangicTarihi,
                           AGNOGirisBitisTarihi = s.AGNOGirisBitisTarihi,
                           ToplamKota = s.ToplamKota,
                           SonucGirisBaslangicTarihi = s.SonucGirisBaslangicTarihi,
                           SonucGirisBitisTarihi = s.SonucGirisBitisTarihi,
                           FarkliOgrenimTipleriAyniBasvurudaAlinabilsin = s.FarkliOgrenimTipleriAyniBasvurudaAlinabilsin,
                           SinavYerBilgisiOgrenciMailiGonderildi = s.SinavYerBilgisiOgrenciMailiGonderildi,
                           SinavYerBilgisiOgrenciMailiGonderimTarihi = s.SinavYerBilgisiOgrenciMailiGonderimTarihi,
                           SinavYerBilgisiBolumMailiGonderildi = s.SinavYerBilgisiBolumMailiGonderildi,
                           SinavYerBilgisiBolumMailiGonderimTarihi = s.SinavYerBilgisiBolumMailiGonderimTarihi,
                           SinavNotBilgisiBolumMailiGonderildi = s.SinavNotBilgisiBolumMailiGonderildi,
                           SinavNotBilgisiBolumMailiGonderimTarihi = s.SinavNotBilgisiBolumMailiGonderimTarihi,
                           SinavSonucBilgisiOgrenciMailiGonderildi = s.SinavSonucBilgisiOgrenciMailiGonderildi,
                           SinavSonucBilgisiOgrenciMailiGonderimTarihi = s.SinavSonucBilgisiOgrenciMailiGonderimTarihi,
                           KayitOlmayanOgrencilereAnketLinkiGonderildi = s.KayitOlmayanOgrencilereAnketLinkiGonderildi,
                           KayitOlmayanOgrencilereAnketLinkiGonderimTarihi = s.KayitOlmayanOgrencilereAnketLinkiGonderimTarihi,
                           SinavSonucBilgisiBolumMailiGonderildi = s.SinavSonucBilgisiBolumMailiGonderildi,
                           SinavSonucBilgisiBolumMailiGonderimTarihi = s.SinavSonucBilgisiBolumMailiGonderimTarihi,
                           KayitOlmayanlarAnketID = s.KayitOlmayanlarAnketID,
                           IsBelgeYuklemeVar = s.IsBelgeYuklemeVar,
                           IsKayittaBelgeOnayiZorunlu = s.IsKayittaBelgeOnayiZorunlu,
                           IsAktif = s.IsAktif,
                           IslemTarihi = s.IslemTarihi,
                           IslemYapanID = s.IslemYapanID,
                           IslemYapan = (k.Ad + " " + k.Soyad),
                           IslemYapanIP = s.IslemYapanIP
                       }).First();
            mdl.CmbOgrenimTipBilgi = (from s in db.BasvuruSurecOgrenimTipleris.Where(p => p.BasvuruSurecID == mdl.BasvuruSurecID)
                                      join ot in db.OgrenimTipleris on new { s.BasvuruSurec.EnstituKod, s.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                                      where s.IsAktif
                                      select new CmbIntDto
                                      {
                                          Value = s.OgrenimTipKod,
                                          Caption = ot.OgrenimTipAdi,
                                      }).OrderBy(t => t.Caption).ToList();
            if (tbInx == 1)
            {
                #region AnaBilgi
                var IndexModel = new MIndexBilgi();
                var btDurulari = Management.cmbBasvuruDurumListeDBilgi();
                foreach (var item in btDurulari.Where(p => p.BasvuruDurumID != BasvuruDurumu.Gonderildi))
                {
                    var tipCount = db.Basvurulars.Where(p => p.BasvuruSurecID == mdl.BasvuruSurecID && p.BasvuruDurumID == item.BasvuruDurumID).Count();
                    if (item.BasvuruDurumID == BasvuruDurumu.Onaylandı)
                    {
                        IndexModel.ListB.Add(new mxRowModel { ID = -1, Key = "Toplam Tercih", ClassName = item.ClassName, Color = item.Color, Toplam = db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı && p.Basvurular.BasvuruSurecID == id).Count() });

                    }
                    IndexModel.ListB.Add(new mxRowModel { ID = item.BasvuruDurumID, Key = item.BasvuruDurumAdi, ClassName = item.ClassName, Color = item.Color, Toplam = tipCount });
                }

                var bdrmG = db.BasvuruDurumlaris.Where(p => p.BasvuruDurumID == BasvuruDurumu.Gonderildi).First();
                IndexModel.ListB.Add(new mxRowModel { ID = bdrmG.BasvuruDurumID, Key = bdrmG.BasvuruDurumAdi, ClassName = bdrmG.ClassName, Color = bdrmG.Color, Toplam = db.MulakatSonuclaris.Where(p => p.BasvuruSurecID == id && p.KayitDurumID.HasValue && p.KayitDurumlari.IsKayitOldu == true).Count() });

                IndexModel.Toplam = IndexModel.ListB.Sum(s => s.Toplam);
                mdl.ToplamBasvuruBilgisi = IndexModel;
                mdl.MulakatSTurModel = (from ms in db.MulakatSinavTurleris
                                        join bsm in db.BasvuruSurecMulakatSinavTurleris on new { ms.MulakatSinavTurID, mdl.BasvuruSurecID } equals new { bsm.MulakatSinavTurID, bsm.BasvuruSurecID }
                                        orderby ms.MulakatSinavTurAdi
                                        select new MulakatSturModel
                                        {
                                            MulakatSinavTurID = ms.MulakatSinavTurID,
                                            SinavTurAdi = ms.MulakatSinavTurAdi,
                                            YuzdeOran = bsm.YuzdeOran,
                                            Zorunlu = bsm.Zorunlu
                                        }).ToList();
                mdl.OgrenimTipleriLst = (from s in db.BasvuruSurecOgrenimTipleris.Where(p => p.BasvuruSurecID == mdl.BasvuruSurecID)
                                         join ot in db.OgrenimTipleris on new { s.BasvuruSurec.EnstituKod, s.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                                         where s.IsAktif
                                         select new KrOgrenimTip
                                         {
                                             EnstituKod = s.BasvuruSurec.EnstituKod,
                                             BasvuruSurecOgrenimTipID = s.BasvuruSurecOgrenimTipID,
                                             OrjinalVeri = false,
                                             OgrenimTipKod = s.OgrenimTipKod,
                                             GrupGoster = s.GrupGoster,
                                             GrupKodu = s.GrupKodu,
                                             Kota = s.Kota,
                                             BelgeYuklemeAsilBasTar = s.BelgeYuklemeAsilBasTar,
                                             BelgeYuklemeAsilBitTar = s.BelgeYuklemeAsilBitTar,
                                             BelgeYuklemeYedekBasTar = s.BelgeYuklemeYedekBasTar,
                                             BelgeYuklemeYedekBitTar = s.BelgeYuklemeYedekBitTar,
                                             GBNFormulu = s.GBNFormulu,
                                             GBNFormuluAlessiz = s.GBNFormuluAlessiz,
                                             GBNFormuluMulakatsiz = s.GBNFormuluMulakatsiz,
                                             GBNFormuluD = s.GBNFormuluD,
                                             GBNFormuluDDosyasiz = s.GBNFormuluDDosyasiz,
                                             GBNFormuluDMulakatsiz = s.GBNFormuluDMulakatsiz,
                                             LEgitimBilgisiIste = s.LEgitimBilgisiIste,
                                             YLEgitimBilgisiIste = s.YLEgitimBilgisiIste,
                                             BasariNotOrtalamasi = s.BasariNotOrtalamasi,
                                             MulakatSurecineGirecek = s.MulakatSurecineGirecek,
                                             AlanIciBilimselHazirlik = s.AlanIciBilimselHazirlik,
                                             AlanDisiBilimselHazirlik = s.AlanDisiBilimselHazirlik,
                                             YokOgrenciKontroluYap = s.YokOgrenciKontroluYap,
                                             IstenecekKatkiPayiTutari = s.IstenecekKatkiPayiTutari,
                                             OgrenimTipAdi = ot.OgrenimTipAdi,
                                             GrupAdi = ot.GrupAdi
                                         }).OrderBy(t => t.OgrenimTipAdi).ToList();
                #endregion
                page = ViewRenderHelper.RenderPartialView("BasvuruSureci", "getBsDetAnaBilgi", mdl);
            }
            if (tbInx == 2)
            {
                #region SinavTipleri
                mdl.SinavTipleri = (from s in db.BasvuruSurecSinavTipleris.Where(p => p.BasvuruSurecID == mdl.BasvuruSurecID && p.IsAktif)
                                    join sg in db.SinavTipGruplaris on s.SinavTipGrupID equals sg.SinavTipGrupID
                                    join sl in db.SinavTipleris on new { s.SinavTipID } equals new { sl.SinavTipID }
                                    select new KrSinavTipleri
                                    {
                                        BasvuruSurecID = id,
                                        SinavTipID = s.SinavTipID,
                                        SinavTipKod = s.SinavTipKod,
                                        SinavAdi = sl.SinavAdi,
                                        EnstituKod = s.EnstituKod,
                                        TarihGirisMaxGecmisYil = s.TarihGirisMaxGecmisYil,
                                        SinavTipGrupID = s.SinavTipGrupID,
                                        SinavTipGrupAdi = sg.SinavTipGrupAdi,
                                        WebService = s.WebService,
                                        WebServiceKod = s.WebServiceKod,
                                        OzelTarih = s.OzelTarih,
                                        OzelTarihTipID = s.OzelTarihTipID,
                                        Tarih1 = s.Tarih1,
                                        Tarih2 = s.Tarih2,
                                        OzelNot = s.OzelNot,
                                        KusuratVar = s.KusuratVar,
                                        Min = s.Min,
                                        Max = s.Max,
                                        NotDonusum = s.NotDonusum,
                                        NotDonusumFormulu = s.NotDonusumFormulu,
                                        GIsTaahhutVar = s.GIsTaahhutVar,
                                        IsAktif = s.IsAktif,
                                        IslemTarihi = s.IslemTarihi,
                                        IslemYapan = s.Kullanicilar.KullaniciAdi,
                                        IslemYapanID = s.IslemYapanID,
                                        IslemYapanIP = s.IslemYapanIP,
                                        BasvuruSurecSinavTiplerSubSinavAraliks = s.BasvuruSurecSinavTiplerSubSinavAraliks.ToList(),
                                        BasvuruSurecSinavNotlaris = s.BasvuruSurecSinavNotlaris.ToList(),
                                        BasvuruSurecSinavTarihleris = s.BasvuruSurecSinavTarihleris.ToList(),
                                        SinavTipleriDonems = (from sq in s.BasvuruSurecSinavTipleriDonems
                                                              select new KrSinavTipleriDonem
                                                              {
                                                                  SinavTipID = sq.BasvuruSurecSinavTipID,
                                                                  SinavTipDonemID = sq.BasvuruSurecSinavTipDonemID,
                                                                  Yil = sq.Yil,
                                                                  WsDonemKod = sq.WsDonemKod,
                                                                  WsDonemAd = sq.WsDonemAd,
                                                                  IsTaahhutVar = sq.IsTaahhutVar
                                                              }).ToList(),
                                        SinavTipleriOtNotAraliklariList = (from s2 in db.BasvuruSurecSinavTipleriOTNotAraliklaris.Where(p => p.BasvuruSurecID == s.BasvuruSurecID && p.SinavTipID == s.SinavTipID)
                                                                           join ot in db.OgrenimTipleris on new { s2.BasvuruSurec.EnstituKod, s2.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }

                                                                           select new KrSinavTipleriOtNotAraliklari
                                                                           {
                                                                               OgrenimTipKod = ot.OgrenimTipKod,
                                                                               OgrenimTipAdi = ot.OgrenimTipAdi,
                                                                               Ingilizce = s2.Ingilizce,
                                                                               IsIstensin = s2.IsIstensin,
                                                                               IsGecerli = s2.IsGecerli,
                                                                               Min = s2.Min,
                                                                               Max = s2.Max,
                                                                               IstenmeyenProgramlar = s2.BasvuruSurecSinavTipleriOTNotAraliklariGecersizProgramlars.Select(sq => new CmbStringDto { Value = sq.ProgramKod, Caption = sq.Programlar.ProgramAdi }).ToList()
                                                                           }).ToList()
                                    }).ToList();

                #endregion
                page = ViewRenderHelper.RenderPartialView("BasvuruSureci", "getBsSinavTipleri", mdl);
            }
            if (tbInx == 3)
            {
                #region ProgramKotalari
                mdl.ProgramKotaLst = (from k in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == id)
                                      join bsOt in db.BasvuruSurecOgrenimTipleris on new { k.BasvuruSurecID, k.OgrenimTipKod } equals new { bsOt.BasvuruSurecID, bsOt.OgrenimTipKod }
                                      join ot in db.OgrenimTipleris on new { k.BasvuruSurec.EnstituKod, k.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                                      join otl in db.OgrenimTipleris on new { ot.OgrenimTipID } equals new { otl.OgrenimTipID }
                                      join s in db.Programlars on k.ProgramKod equals s.ProgramKod
                                      join e in db.AnabilimDallaris on new { s.AnabilimDaliKod, mdl.EnstituKod } equals new { e.AnabilimDaliKod, e.EnstituKod }
                                      join at in db.AlesTipleris on new { s.AlesTipID } equals new { at.AlesTipID }
                                      join enst in db.Enstitulers on new { e.EnstituKod } equals new { enst.EnstituKod }
                                      join ktl in db.KullaniciTipleris on s.KullaniciTipID equals ktl.KullaniciTipID

                                      select new FrKotalarDto
                                      {
                                          KotaID = k.KotaID,
                                          OgrenimTipKod = ot.OgrenimTipKod,
                                          OgrenimTipAdi = otl.OgrenimTipAdi,
                                          OrtakKota = k.OrtakKota,
                                          OrtakKotaSayisi = k.OrtakKotaSayisi,
                                          AlanIciKota = k.AlanIciKota,
                                          AlanDisiKota = k.AlanDisiKota,
                                          MinAles = k.MinAles,
                                          MinAgno = k.MinAGNO,
                                          EnstituKod = enst.EnstituKod,
                                          EnstituAd = enst.EnstituAd,
                                          AnabilimDaliKod = e.AnabilimDaliKod,
                                          AnabilimDaliAdi = e.AnabilimDaliAdi,
                                          AlesTipID = s.AlesTipID,
                                          AlesTipAdi = at.AlesTipAdi,
                                          ProgramKod = s.ProgramKod,
                                          ProgramAdi = s.ProgramAdi,
                                          MulakatSurecineGirecek = k.MulakatSurecineGirecek ?? bsOt.MulakatSurecineGirecek,
                                          IsAlesYerineDosyaNotuIstensin = k.IsAlesYerineDosyaNotuIstensin ?? false,
                                          Ingilizce = s.Ingilizce,
                                          IsAktif = s.IsAktif,
                                          Ucret = s.Ucret,
                                          Ucretli = s.Ucretli,
                                          IslemTarihi = s.IslemTarihi,
                                          IslemYapanID = s.IslemYapanID,
                                          IslemYapanIP = s.IslemYapanIP,
                                          IslemYapan = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                                          IsAlandisiBolumKisitlamasi = k.IsAlandisiBolumKisitlamasi,
                                          KullaniciTipAdi = ktl.KullaniciTipAdi,

                                      }).OrderBy(o => o.AnabilimDaliAdi).ThenBy(t => t.ProgramAdi).ToList();
                #endregion
                page = ViewRenderHelper.RenderPartialView("BasvuruSureci", "getBsKotalar", mdl);
            }
            if (tbInx == 4)
            {
                var prkods = UserBus.GetUserProgramKods(UserIdentity.Current.Id, mdl.EnstituKod);

                #region MulakatSınavBilgi
                mdl.ProgramKotaLst = (from k in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == id)
                                      join bsOt in db.BasvuruSurecOgrenimTipleris on new
                                      {
                                          k.BasvuruSurecID,
                                          k.OgrenimTipKod
                                      }
                                        equals new
                                        {
                                            bsOt.BasvuruSurecID,
                                            bsOt.OgrenimTipKod
                                        }
                                      join ot in db.OgrenimTipleris on new { k.BasvuruSurec.EnstituKod, k.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                                      join s in db.Programlars on k.ProgramKod equals s.ProgramKod
                                      join e in db.AnabilimDallaris on new { s.AnabilimDaliKod, mdl.EnstituKod } equals new { e.AnabilimDaliKod, e.EnstituKod }
                                      join kt in db.KullaniciTipleris on s.KullaniciTipID equals kt.KullaniciTipID
                                      where prkods.Contains(s.ProgramKod)
                                      select new FrKotalarDto
                                      {
                                          KotaID = k.KotaID,
                                          OgrenimTipKod = k.OgrenimTipKod,
                                          OgrenimTipAdi = ot.OgrenimTipAdi,
                                          AlanIciKota = k.AlanIciKota,
                                          AlanDisiKota = k.AlanDisiKota,
                                          MinAles = k.MinAles,
                                          MinAgno = k.MinAGNO,
                                          AnabilimDaliKod = e.AnabilimDaliKod,
                                          AnabilimDaliAdi = e.AnabilimDaliAdi,
                                          ProgramKod = s.ProgramKod,
                                          ProgramAdi = s.ProgramAdi,
                                          MulakatSurecineGirecek = k.MulakatSurecineGirecek ?? bsOt.MulakatSurecineGirecek,
                                          IsAlesYerineDosyaNotuIstensin = k.IsAlesYerineDosyaNotuIstensin ?? false,
                                          Ingilizce = s.Ingilizce,
                                          IsAktif = s.IsAktif,
                                          IslemTarihi = s.IslemTarihi,
                                          IslemYapanID = s.IslemYapanID,
                                          IslemYapanIP = s.IslemYapanIP,
                                          IslemYapan = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                                          KullaniciTipAdi = kt.KullaniciTipAdi,
                                          Ucret = s.Ucret,
                                          Ucretli = s.Ucretli,
                                      }).ToList();

                var tmpMulakats = db.Mulakats.Where(p => p.BasvuruSurecID == mdl.BasvuruSurecID).ToList();
                // var tmpMulakatSonuclari = Management.getMulakatSonucHesapList(mdl.BasvuruSurecID);
                var tmpBasvuruTercihleri = db.BasvurularTercihleris.Where(p => prkods.Contains(p.ProgramKod) && p.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı && p.Basvurular.BasvuruSurecID == mdl.BasvuruSurecID).Select(s => new { s.OgrenimTipKod, s.ProgramKod, s.AlanTipID, s.Basvurular.BasvuruDurumID }).ToList();


                var tmpMulakatSonuclari = db.MulakatSonuclaris.Where(p => prkods.Contains(p.BasvurularTercihleri.ProgramKod) && p.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı && p.BasvuruSurecID == id).Select(s => new krMulakatSonuc
                {
                    BasvuruTercihID = s.BasvuruTercihID,
                    MulakatID = s.MulakatID,
                    OgrenimTipKod = s.BasvurularTercihleri.OgrenimTipKod,
                    ProgramKod = s.BasvurularTercihleri.ProgramKod,
                    AlanTipID = s.AlanTipID,
                    AlesNotuOrDosyaNotu = s.AlesNotuOrDosyaNotu,
                    SinavaGirmediS = s.SinavaGirmediS,
                    SinavaGirmediY = s.SinavaGirmediY,
                    SozluNotu = s.SozluNotu,
                    YaziliNotu = s.YaziliNotu
                }).ToList();
                var mulDetays = db.MulakatDetays.Where(p => p.Mulakat.BasvuruSurecID == mdl.BasvuruSurecID).ToList();
                foreach (var kbtercih in tmpMulakatSonuclari)
                {
                    var PkBilgi = mdl.ProgramKotaLst.Where(p => p.OgrenimTipKod == kbtercih.OgrenimTipKod && p.ProgramKod == kbtercih.ProgramKod).FirstOrDefault();
                    if (PkBilgi != null)
                    {
                        bool successRow = true;

                        var mulSinavTurs = mulDetays.Where(p => p.MulakatID == kbtercih.MulakatID).ToList();
                        var YaziliSinaviIstensin = mulSinavTurs.Any(a => a.MulakatSinavTurID == MulakatSinavTur.Yazili);
                        var SozluSinaviIstensin = mulSinavTurs.Any(a => a.MulakatSinavTurID == MulakatSinavTur.Sozlu);

                        if (YaziliSinaviIstensin)
                        {
                            if (kbtercih.SinavaGirmediY.HasValue == false)
                            {
                                if (kbtercih.YaziliNotu.HasValue == false) successRow = false;
                            }
                            else
                            {
                                if (kbtercih.SinavaGirmediY.Value == false && kbtercih.YaziliNotu.HasValue == false) successRow = false;
                            }

                        }
                        if (SozluSinaviIstensin)
                        {
                            if (kbtercih.SinavaGirmediS.HasValue == false)
                            {
                                if (kbtercih.SozluNotu.HasValue == false) successRow = false;
                            }
                            else
                            {
                                if (kbtercih.SinavaGirmediS.Value == false && kbtercih.SozluNotu.HasValue == false) successRow = false;
                            }
                        }
                        if (PkBilgi.IsAlesYerineDosyaNotuIstensin)
                        {
                            if (kbtercih.AlesNotuOrDosyaNotu.HasValue == false) successRow = false;

                        }
                        kbtercih.SuccessRow = successRow;
                    }
                }
                var mlktBilgi = (from s2 in (from s in mdl.ProgramKotaLst
                                             join ml in db.Mulakats on new { mdl.BasvuruSurecID, s.ProgramKod, s.OgrenimTipKod } equals new { ml.BasvuruSurecID, ml.ProgramKod, ml.OgrenimTipKod } into defM
                                             from mlk in defM.DefaultIfEmpty()
                                             select new
                                             {
                                                 s.OgrenimTipKod,
                                                 s.OgrenimTipAdi,
                                                 s.AlanIciKota,
                                                 s.AlanDisiKota,
                                                 s.MulakatSurecineGirecek,
                                                 s.IsAlesYerineDosyaNotuIstensin,
                                                 MulakatID = mlk != null ? mlk.MulakatID : (int?)null,
                                                 s.AnabilimDaliKod,
                                                 s.AnabilimDaliAdi,
                                                 s.ProgramKod,
                                                 s.ProgramAdi,
                                                 s.KullaniciTipAdi,
                                                 s.Ucret,
                                                 s.Ucretli
                                             })
                                 group new
                                 {
                                     s2.MulakatID,
                                     s2.MulakatSurecineGirecek,
                                     s2.KullaniciTipAdi,
                                     s2.AnabilimDaliKod,
                                     s2.AnabilimDaliAdi,
                                     s2.ProgramKod,
                                     s2.ProgramAdi,
                                     s2.AlanIciKota,
                                     s2.AlanDisiKota,
                                     s2.OgrenimTipKod,
                                     s2.OgrenimTipAdi,
                                 }
                                 by new
                                 {
                                     s2.MulakatID,
                                     s2.AnabilimDaliKod,
                                     s2.AnabilimDaliAdi,
                                     s2.ProgramKod,
                                     s2.ProgramAdi,
                                     s2.KullaniciTipAdi,
                                     s2.AlanIciKota,
                                     s2.AlanDisiKota,
                                     s2.OgrenimTipKod,
                                     s2.OgrenimTipAdi,
                                     s2.MulakatSurecineGirecek,
                                     s2.IsAlesYerineDosyaNotuIstensin,
                                     s2.Ucret,
                                     s2.Ucretli,
                                 } into g1
                                 select new FrMulakatNotGirisDetay
                                 {
                                     MulakatSurecineGirecek = g1.Key.MulakatSurecineGirecek,
                                     IsAlesYerineDosyaNotuIstensin = g1.Key.IsAlesYerineDosyaNotuIstensin,
                                     KullaniciTipAdi = g1.Key.KullaniciTipAdi,
                                     MulakatID = g1.Key.MulakatID,
                                     OgrenimTipKod = g1.Key.OgrenimTipKod,
                                     OgrenimTipAdi = g1.Key.OgrenimTipAdi,
                                     AnabilimDaliKod = g1.Key.AnabilimDaliKod,
                                     AnabilimDaliAdi = g1.Key.AnabilimDaliAdi,
                                     ProgramKod = g1.Key.ProgramKod,
                                     ProgramAdi = g1.Key.ProgramAdi,
                                     AlanIciKota = g1.Key.AlanIciKota,
                                     AlanDisiKota = g1.Key.AlanDisiKota,
                                     YerJuriBilgisiGirildi = tmpMulakats.Where(p => p.OgrenimTipKod == g1.Key.OgrenimTipKod && p.ProgramKod == g1.Key.ProgramKod && p.MulakatJuris.Count > 0 && p.MulakatDetays.Count > 0).Any(),
                                     ToplamBasvuru = tmpBasvuruTercihleri.Where(p => p.OgrenimTipKod == g1.Key.OgrenimTipKod && p.ProgramKod == g1.Key.ProgramKod && p.BasvuruDurumID == BasvuruDurumu.Onaylandı).Count(),
                                     ToplamMGiris = tmpMulakatSonuclari.Where(p => p.OgrenimTipKod == g1.Key.OgrenimTipKod && p.ProgramKod == g1.Key.ProgramKod && p.SuccessRow).Count(),
                                     AIToplamBasvuru = tmpBasvuruTercihleri.Where(p => p.OgrenimTipKod == g1.Key.OgrenimTipKod && p.ProgramKod == g1.Key.ProgramKod && p.AlanTipID == AlanTipi.AlanIci && p.BasvuruDurumID == BasvuruDurumu.Onaylandı).Count(),
                                     AIMToplamBasvuru = tmpMulakatSonuclari.Where(p => p.OgrenimTipKod == g1.Key.OgrenimTipKod && p.ProgramKod == g1.Key.ProgramKod && p.AlanTipID == AlanTipi.AlanIci && p.SuccessRow).Count(),
                                     AIMNotGirildiCount = tmpMulakatSonuclari.Where(p => p.OgrenimTipKod == g1.Key.OgrenimTipKod && p.ProgramKod == g1.Key.ProgramKod && p.AlanTipID == AlanTipi.AlanIci && p.SuccessRow).Count(),
                                     ADToplamBasvuru = tmpBasvuruTercihleri.Where(p => p.OgrenimTipKod == g1.Key.OgrenimTipKod && p.ProgramKod == g1.Key.ProgramKod && p.AlanTipID == AlanTipi.AlanDisi && p.BasvuruDurumID == BasvuruDurumu.Onaylandı).Count(),
                                     ADMToplamBasvuru = tmpMulakatSonuclari.Where(p => p.OgrenimTipKod == g1.Key.OgrenimTipKod && p.ProgramKod == g1.Key.ProgramKod && p.AlanTipID == AlanTipi.AlanDisi && p.SuccessRow).Count(),
                                     ADMNotGirildiCount = tmpMulakatSonuclari.Where(p => p.OgrenimTipKod == g1.Key.OgrenimTipKod && p.ProgramKod == g1.Key.ProgramKod && p.AlanTipID == AlanTipi.AlanDisi && p.SuccessRow).Count(),
                                     Ucret = g1.Key.Ucret,
                                     Ucretli = g1.Key.Ucretli
                                 }).ToList();
                var tmpT = new List<FrMulakatNotGirisDetay>();
                tmpT.AddRange(mlktBilgi);

                foreach (var item in tmpT)
                {
                    item.SinavNotGirisiYapildi = item.ToplamMGiris == item.ToplamBasvuru;
                }
                mdl.MulakatBilgi.MulakatNotGirisDetay = tmpT.OrderByDescending(o => o.MulakatSurecineGirecek).ThenBy(o => o.YerJuriBilgisiGirildi).ThenBy(t => t.SinavNotGirisiYapildi).ThenByDescending(t => t.ToplamBasvuru > 0).ThenBy(t => t.AnabilimDaliAdi).ThenBy(t => t.ProgramAdi).ToList();
                mdl.MulakatBilgi.YerJuriBilgisiGirisProgramCount = tmpT.Where(p => p.YerJuriBilgisiGirildi).Count();
                mdl.MulakatBilgi.SinavBilgisiGirilenProgramCount = tmpT.Where(p => p.SinavNotGirisiYapildi).Count();
                mdl.MulakatBilgi.ToplamBasvuru = tmpT.Sum(p => p.ToplamBasvuru);
                mdl.MulakatBilgi.ToplamMGiris = tmpT.Sum(p => p.ToplamMGiris);
                mdl.MulakatBilgi.AIToplamBasvuru = tmpT.Sum(p => p.AIToplamBasvuru);
                mdl.MulakatBilgi.AIMToplamBasvuru = tmpT.Sum(p => p.AIMToplamBasvuru);
                mdl.MulakatBilgi.AIMNotGirildiCount = tmpT.Sum(p => p.AIMNotGirildiCount);
                mdl.MulakatBilgi.ADToplamBasvuru = tmpT.Sum(p => p.ADToplamBasvuru);
                mdl.MulakatBilgi.ADMToplamBasvuru = tmpT.Sum(p => p.ADMToplamBasvuru);
                mdl.MulakatBilgi.ADMNotGirildiCount = tmpT.Sum(p => p.ADMNotGirildiCount);
                mdl.MulakatBilgi.YerJuriBilgisiGirisCount = tmpT.Where(p => p.SinavNotGirisiYapildi).Count();
                #endregion
                mdl.IsDelete = IsDelete;
                page = ViewRenderHelper.RenderPartialView("BasvuruSureci", "getBsMulakat", mdl);
            }
            if (tbInx == 5)
            {

                #region BaşvuruSonuclari

                var KullaniciProgramKods = UserBus.GetUserProgramKods(UserIdentity.Current.Id, mdl.EnstituKod);
                var vW = db.vW_ProgramBasvuruSonucSayisal.Where(p => p.BasvuruSurecID == id && KullaniciProgramKods.Contains(p.ProgramKod)).Select(s =>
                         new FrMulakatSonucDetay
                         {
                             MulakatSurecineGirecek = s.MulakatSurecineGirecek,
                             OgrenimTipKod = s.OgrenimTipKod,
                             OgrenimTipAdi = s.OgrenimTipAdi,
                             AnabilimDaliKod = s.AnabilimDaliKod,
                             AnabilimDaliAdi = s.AnabilimDaliAdi,
                             ProgramKod = s.ProgramKod,
                             ProgramAdi = s.ProgramAdi,
                             AIKota = s.AlanIciKota ?? 0,
                             ADKota = s.AlanDisiKota ?? 0,
                             Ucret = s.Ucret,
                             Ucretli = s.Ucretli,
                             KullaniciTipAdi = s.KullaniciTipAdi,
                             ToplamBasvuru = s.ToplamBasvuru ?? 0,
                             AIKayitCount = s.AIKayitCount ?? 0,
                             AIAsilCount = s.AIAsilCount ?? 0,
                             AIYedekCount = s.AIYedekCount ?? 0,
                             AIKazanamayanCount = s.AIKazanamayanCount ?? 0,
                             ADKayitCount = s.ADKayitCount ?? 0,
                             ADAsilCount = s.ADAsilCount ?? 0,
                             ADYedekCount = s.ADYedekCount ?? 0,
                             ADKazanamayanCount = s.ADKazanamayanCount ?? 0


                         }).ToList();



                mdl.MulakatSonucu.MulakatSonucDetay = vW;

                mdl.MulakatSonucu.ToplamTercihCount = db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruSurecID == id && p.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı).Count();
                mdl.MulakatSonucu.HesaplananTercihCount = vW.Sum(s => s.ToplamBasvuru);
                mdl.MulakatSonucu.ToplamProgramCount = db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruSurecID == id && p.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı).Select(s => new { s.OgrenimTipKod, s.ProgramKod }).Distinct().Count();
                mdl.MulakatSonucu.HesaplananProgramCount = vW.Count;
                mdl.MulakatSonucu.HesaplamaYapildi = vW.Any();
                mdl.MulakatSonucu.TumProgramlarHesaplandi = db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == id).Count() == mdl.MulakatSonucu.MulakatSonucDetay.Count;

                #endregion
                mdl.IsDelete = IsDelete;
                page = ViewRenderHelper.RenderPartialView("BasvuruSureci", "getBsSonuc", mdl);
            }
            if (tbInx == 6)
            {

                #region BaşvuruAnketSonuclari
                mdl.ToplamOnaylananBasvuru = db.Basvurulars.Where(p => p.BasvuruSurecID == mdl.BasvuruSurecID && p.BasvuruDurumID == BasvuruDurumu.Onaylandı).Count();
                var qModel = (from s in db.Ankets.Where(p => p.AnketID == mdl.AnketID)
                              join sa in db.AnketSorus on s.AnketID equals sa.AnketID
                              select new FrAnketDetayDto
                              {
                                  AnketSoruID = sa.AnketSoruID,
                                  AnketID = sa.AnketID,
                                  SoruAdi = sa.SoruAdi,
                                  SiraNo = sa.SiraNo,
                                  FrAnketSecenekDetay = (from ss in sa.AnketSoruSeceneks
                                                         select new FrAnketSecenekDetayDto
                                                         {
                                                             AnketSoruID = ss.AnketSoruID,
                                                             AnketSoruSecenekID = ss.AnketSoruSecenekID,
                                                             SiraNo = sa.SiraNo,
                                                             SecenekAdi = ss.SecenekAdi,
                                                             IsEkAciklamaGir = ss.IsEkAciklamaGir,
                                                             Count = ss.AnketCevaplaris.Where(p => p.Basvurular.BasvuruSurecID == mdl.BasvuruSurecID && p.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı).Count(),
                                                             AnketCevaplaris = ss.AnketCevaplaris.Where(p => p.Basvurular.BasvuruSurecID == mdl.BasvuruSurecID && p.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı).ToList()
                                                         }
                                                       ).OrderBy(o => o.SiraNo).ToList(),

                                  AnketCevaplaris = sa.AnketCevaplaris.ToList()
                              }).OrderBy(o => o.SiraNo).ToList();
                mdl.AnketDetay = qModel;
                #endregion
                mdl.IsDelete = IsDelete;
                page = ViewRenderHelper.RenderPartialView("BasvuruSureci", "getBsAnket", mdl);
            }
            return Content(page, "text/html");
        }
        public ActionResult getBsDetAnaBilgi(BasvuruSurecDetayDto model)
        {
            return View(model);
        }
        public ActionResult getBsSinavTipleri(BasvuruSurecDetayDto model)
        {
            return View(model);
        }
        public ActionResult getBsKotalar(BasvuruSurecDetayDto model)
        {
            return View(model);
        }
        public ActionResult getBsMulakat(BasvuruSurecDetayDto model)
        {
            return View();
        }
        public ActionResult getBsSonuc(BasvuruSurecDetayDto model)
        {
            return View();
        }
        public ActionResult getBsAnket(BasvuruSurecDetayDto model)
        {
            return View();
        }
        [Authorize(Roles = RoleNames.BasvuruSureciKayit)]
        public ActionResult BasvuruSonucHesap(int id, string ProgramKod = null, int? OgrenimTipKod = null, int? OrtakKotaSayisi = null, int? AlanIciKota = null, int? AlanDisiKota = null, int? AlanIciEkKota = null, int? AlanDisiEkKota = null)
        {

            var mmMessage = new MmMessage();

            try
            {
                var bSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == id).First();
                var msonucL = new List<krMulakatSonuc>();
                mmMessage.Title = "Başvuru sonucu hesaplama işlemi";
                if (!ProgramKod.IsNullOrWhiteSpace() && OgrenimTipKod.HasValue)
                {
                    var qKayit = bSurec.MulakatSonuclaris.Where(p => p.KayitDurumID.HasValue && p.KayitDurumlari.IsKayitOldu == true && p.BasvurularTercihleri.ProgramKod == ProgramKod && p.BasvurularTercihleri.OgrenimTipKod == OgrenimTipKod);

                    if (qKayit.Any())
                    {
                        mmMessage.IsSuccess = true;
                        var otip = bSurec.BasvuruSurecOgrenimTipleris.Where(p => p.OgrenimTipKod == OgrenimTipKod).First();
                        var dataAI = bSurec.MulakatSonuclaris.Where(p => p.BasvurularTercihleri.ProgramKod == ProgramKod && p.BasvurularTercihleri.OgrenimTipKod == OgrenimTipKod && p.MulakatSonucTipID != MulakatSonucTipi.Asil && p.AlanTipID == AlanTipi.AlanIci && (p.Aciklama == "Sıralama Dışı" || p.Aciklama == null || p.Aciklama == "")).OrderByDescending(o => o.GenelBasariNotu).ToList();
                        var dataAD = bSurec.MulakatSonuclaris.Where(p => p.BasvurularTercihleri.ProgramKod == ProgramKod && p.BasvurularTercihleri.OgrenimTipKod == OgrenimTipKod && p.MulakatSonucTipID != MulakatSonucTipi.Asil && p.AlanTipID == AlanTipi.AlanDisi && (p.Aciklama == "Sıralama Dışı" || p.Aciklama == null || p.Aciklama == "")).OrderByDescending(o => o.GenelBasariNotu).ToList();
                        var kota = bSurec.BasvuruSurecKotalars.Where(p => p.ProgramKod == ProgramKod && p.OgrenimTipKod == OgrenimTipKod).First();

                        kota.AlanIciEkKota = AlanIciEkKota;
                        kota.AlanDisiEkKota = AlanDisiEkKota;

                        var AIKota = (kota.OrtakKota ? 0 : ((kota.AlanIciKota * otip.YedekOgrenciSayisiKotaCarpani) + (kota.AlanIciEkKota ?? 0)));
                        var ADKota = ((kota.OrtakKota ? kota.OrtakKotaSayisi.Value : kota.AlanDisiKota) * otip.YedekOgrenciSayisiKotaCarpani) + (kota.AlanDisiEkKota ?? 0);
                        int Syc = 1;
                        foreach (var item in dataAI)
                        {
                            if (Syc <= AIKota)
                            {
                                item.IslemYapanIP = UserIdentity.Ip;
                                item.IslemTarihi = DateTime.Now;
                                item.IslemYapanID = UserIdentity.Current.Id;
                                item.MulakatSonucTipID = MulakatSonucTipi.Yedek; Syc++;
                            }
                            else break;
                        }
                        Syc = 1;
                        foreach (var item in dataAD)
                        {
                            if (Syc <= ADKota)
                            {
                                item.IslemYapanIP = UserIdentity.Ip;
                                item.IslemTarihi = DateTime.Now;
                                item.IslemYapanID = UserIdentity.Current.Id;
                                item.MulakatSonucTipID = MulakatSonucTipi.Yedek; Syc++;
                            }
                            else break;
                        }
                        db.SaveChanges();
                        mmMessage.Messages.Add("Hesaplama işlemi gerçekleştirildi!");
                    }
                    else
                    {
                        var prKota = bSurec.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == id && p.OgrenimTipKod == OgrenimTipKod && p.ProgramKod == ProgramKod).First();
                        if (prKota.OrtakKota)
                        {
                            prKota.OrtakKotaSayisi = OrtakKotaSayisi.Value;
                        }
                        else
                        {
                            prKota.AlanIciKota = AlanIciKota.Value;
                            prKota.AlanDisiKota = AlanDisiKota.Value;
                        }
                        db.SaveChanges();

                        if (ProgramKod.IsNullOrWhiteSpace() == false && OgrenimTipKod.HasValue)
                        {
                            var qKayitlar = bSurec.MulakatSonuclaris.Where(p => p.BasvurularTercihleri.ProgramKod == ProgramKod && p.BasvurularTercihleri.OgrenimTipKod == OgrenimTipKod).ToList();
                            db.MulakatSonuclaris.RemoveRange(qKayitlar);
                        }
                        msonucL = Management.getMulakatSonucHesapList(id, ProgramKod, OgrenimTipKod);
                    }

                }
                else
                {

                    msonucL = Management.getMulakatSonucHesapList(id, null, null);
                    var HesapDisiProgramlar = msonucL.Where(p => p.KayitDurumID.HasValue && p.KayitDurumlari.IsKayitOldu == true || (p.MulakatSurecineGirecek
                                                                                      ?
                                                                                         (
                                                                                           (p.YaziliSinaviIstensin ? (p.SinavaGirmediY == true ? false : !p.YaziliNotu.HasValue) : false)
                                                                                           ||
                                                                                           (p.SozluSinaviIstensin ? (p.SinavaGirmediS == true ? false : !p.SozluNotu.HasValue) : false)
                                                                                           ||
                                                                                           (p.IsAlesYerineDosyaNotuIstensin ? !p.AlesNotuOrDosyaNotu.HasValue : false)
                                                                                          )
                                                                                       :
                                                                                          (p.IsAlesYerineDosyaNotuIstensin ? !p.AlesNotuOrDosyaNotu.HasValue : false)

                                                                                      )

                                        ).Select(s => new { s.OgrenimTipKod, s.ProgramKod }).ToList();



                    msonucL = msonucL.Where(p => !HesapDisiProgramlar.Any(a => a.ProgramKod == p.ProgramKod && a.OgrenimTipKod == p.OgrenimTipKod)).ToList();

                    var SilinecekSonuclar = bSurec.MulakatSonuclaris.Where(p => !HesapDisiProgramlar.Any(a => a.ProgramKod == p.BasvurularTercihleri.ProgramKod && a.OgrenimTipKod == p.BasvurularTercihleri.OgrenimTipKod)).ToList();
                    db.MulakatSonuclaris.RemoveRange(SilinecekSonuclar);


                }
                mmMessage.IsSuccess = true;

                var qGrup = (from ms in msonucL
                             group new { ms.ProgramKod, ms.OgrenimTipKod, ms.AlanTipID, ms.AlanKota } by new { ms.ProgramKod, ms.OgrenimTipKod, ms.AlanTipID, ms.AlanKota, ms.AlanKotaYedek } into g1
                             select new
                             {
                                 g1.Key.ProgramKod,
                                 g1.Key.OgrenimTipKod,
                                 g1.Key.AlanTipID,
                                 g1.Key.AlanKota,
                                 g1.Key.AlanKotaYedek
                             }).ToList();


                #region Hesap

                var basvuruSurecOgrenimTipleris = bSurec.BasvuruSurecOgrenimTipleris.Where(p => p.BasvuruSurecID == id).ToList();

                var mulSinavTursAll = db.MulakatDetays.Where(p => p.Mulakat.BasvuruSurecID == id).ToList();
                foreach (var itemG in qGrup)
                {
                    var grupList = msonucL.Where(p => p.ProgramKod == itemG.ProgramKod && p.OgrenimTipKod == itemG.OgrenimTipKod && p.AlanTipID == itemG.AlanTipID).ToList();

                    var ogrenimBilgi = basvuruSurecOgrenimTipleris.Where(p => p.OgrenimTipKod == itemG.OgrenimTipKod).First();

                    foreach (var itemP in grupList)
                    {

                        var mulSinavTurs = mulSinavTursAll.Where(p => p.MulakatID == itemP.MulakatID).ToList();
                        var yaziliBilgi = mulSinavTurs.Where(p => p.MulakatSinavTurID == MulakatSinavTur.Yazili).FirstOrDefault();
                        var sozluBilgi = mulSinavTurs.Where(p => p.MulakatSinavTurID == MulakatSinavTur.Sozlu).FirstOrDefault();
                        if (itemP.MulakatSurecineGirecek)
                        {

                            if (itemP.MinAGNO.HasValue && itemP.MinAGNO > itemP.Agno)
                            {
                                //Kazanamadı (Agno Yetersiz)
                            }
                            if ((itemP.YaziliSinaviIstensin ? itemP.SinavaGirmediY.HasValue && !itemP.SinavaGirmediY.Value : true) && (itemP.SozluSinaviIstensin ? (itemP.SinavaGirmediS.HasValue && !itemP.SinavaGirmediS.Value) : true))
                            {


                                if (itemP.YaziliSinaviIstensin && itemP.SozluSinaviIstensin)
                                {
                                    itemP.GirisSinavNotu = ((itemP.YaziliNotu.Value * yaziliBilgi.YuzdeOran) / (double)100) + ((itemP.SozluNotu.Value * sozluBilgi.YuzdeOran) / (double)100);
                                }
                                else if (itemP.YaziliSinaviIstensin && itemP.SozluSinaviIstensin == false)
                                {
                                    itemP.GirisSinavNotu = itemP.YaziliNotu;
                                }
                                else if (itemP.YaziliSinaviIstensin == false && itemP.SozluSinaviIstensin)
                                {
                                    itemP.GirisSinavNotu = itemP.SozluNotu;
                                }
                                itemP.GenelBasariNotu = itemP.Agno.Value.ToGenelBasariNotu(itemP.MulakatSurecineGirecek, ogrenimBilgi, itemP.IsAlesYerineDosyaNotuIstensin, itemP.AlesNotuOrDosyaNotu, itemP.GirisSinavNotu.Value);

                            }
                        }
                        else
                        {
                            itemP.GenelBasariNotu = itemP.Agno.Value.ToGenelBasariNotu(itemP.MulakatSurecineGirecek, ogrenimBilgi, itemP.IsAlesYerineDosyaNotuIstensin, itemP.AlesNotuOrDosyaNotu, null);
                        }

                    }
                    int inx = 0;
                    int inxKazanan = 0;
                    int inxYedek = 0;
                    foreach (var item in grupList.OrderByDescending(o => o.GenelBasariNotu).ThenByDescending(t => t.MulakatSurecineGirecek ? t.GirisSinavNotu : 1).ThenByDescending(t => t.AlesNotuOrDosyaNotu).ThenByDescending(t => t.Agno))
                    {
                        inx++;
                        item.Aciklama = null;
                        var mulSinavTurs = mulSinavTursAll.Where(p => p.MulakatID == item.MulakatID).ToList();
                        var YaziliSinaviIstensin = mulSinavTurs.Any(a => a.MulakatSinavTurID == MulakatSinavTur.Yazili);
                        var SozluSinaviIstensin = mulSinavTurs.Any(a => a.MulakatSinavTurID == MulakatSinavTur.Sozlu);
                        var yaziliBilgi = mulSinavTurs.Where(p => p.MulakatSinavTurID == MulakatSinavTur.Yazili).FirstOrDefault();
                        var sozluBilgi = mulSinavTurs.Where(p => p.MulakatSinavTurID == MulakatSinavTur.Sozlu).FirstOrDefault();
                        if (item.MulakatSurecineGirecek && ((YaziliSinaviIstensin ? item.SinavaGirmediY == true : false) || (SozluSinaviIstensin ? item.SinavaGirmediS == true : false)))
                        {
                            item.MulakatSonucTipID = MulakatSonucTipi.Kazanamadı;
                            item.Aciklama = "Sınava Girmedi";
                        }
                        else if (item.MinAGNO.HasValue && item.MinAGNO.Value > item.Agno)
                        {
                            item.MulakatSonucTipID = MulakatSonucTipi.Kazanamadı;
                            item.Aciklama = "Agno Yetersiz";

                        }
                        else if (ogrenimBilgi.BasariNotOrtalamasi > item.GenelBasariNotu.Value)
                        {
                            item.MulakatSonucTipID = MulakatSonucTipi.Kazanamadı;
                            item.Aciklama = "Puan Yetersiz";
                        }
                        else
                        {
                            if (inxKazanan < item.AlanKota)
                            {
                                inxKazanan++;
                                item.MulakatSonucTipID = MulakatSonucTipi.Asil;
                            }
                            else if (inxYedek < itemG.AlanKotaYedek)
                            {
                                inxYedek++;
                                item.MulakatSonucTipID = MulakatSonucTipi.Yedek;
                            }
                            else
                            {
                                item.MulakatSonucTipID = MulakatSonucTipi.Yedek;
                                item.Aciklama = "Sıralama Dışı";
                            }
                        }

                        item.SiraNo = inx;

                    }
                }
                #endregion


                var addNewData = msonucL.Select(item => new MulakatSonuclari
                {
                    MulakatSonucTipID = item.MulakatSonucTipID,
                    BasvuruSurecID = item.BasvuruSurecID,
                    MulakatID = item.MulakatSurecineGirecek ? item.MulakatID : null,
                    BasvuruID = item.BasvuruID,
                    BasvuruTercihID = item.BasvuruTercihID,
                    AlanTipID = item.AlanTipID,
                    SiraNo = item.SiraNo,
                    AlesNotuOrDosyaNotu = item.AlesNotuOrDosyaNotu,
                    Agno = item.Agno,
                    SinavaGirmediY = item.SinavaGirmediY,
                    SinavaGirmediS = item.SinavaGirmediS,
                    YaziliNotu = item.YaziliNotu,
                    SozluNotu = item.SozluNotu,
                    GirisSinavNotu = item.GirisSinavNotu,
                    GenelBasariNotu = item.GenelBasariNotu,
                    Aciklama = item.Aciklama,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemTarihi = DateTime.Now,
                    IslemYapanID = UserIdentity.Current.Id
                }).ToList();

                db.MulakatSonuclaris.AddRange(addNewData);

                db.SaveChanges();

                LogIslemleri.LogEkle("MulakatSonuclari", IslemTipi.Update, addNewData.ToJson());
                mmMessage.Messages.Add("Hesaplama işlemi gerçekleştirildi!");

            }
            catch (Exception ex)
            {
                mmMessage.IsSuccess = false;
                var msg = "Hesaplama işlemi yapılırken bir hata oluştu! hata:" + ex.ToExceptionMessage();
                mmMessage.Messages.Add(msg);
                SistemBilgilendirmeBus.SistemBilgisiKaydet(msg, "BasvuruSureci/BasvuruSonucHesap<br/><br/>" + ex.ToExceptionStackTrace(), LogType.Kritik);
            }

            if (mmMessage.IsSuccess) mmMessage.MessageType = Msgtype.Success;
            else mmMessage.MessageType = Msgtype.Error;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);

        }
        public ActionResult getBasvuruSonucDetay(int id, string ProgramKod, int OgrenimTipKod, int tbInx, bool IsDelete, bool IsBootBox = false)
        {
            var model = new BasvuruMulakatDetayDto();
            var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == id).FirstOrDefault();
            var _EnstituKod = bsurec.EnstituKod;
            var program = db.Programlars.Where(p => p.ProgramKod == ProgramKod).First();
            var ot = bsurec.BasvuruSurecOgrenimTipleris.Where(p => p.OgrenimTipKod == OgrenimTipKod).First();
            var kota = bsurec.BasvuruSurecKotalars.Where(p => p.ProgramKod == ProgramKod && p.OgrenimTipKod == OgrenimTipKod).First();
            model.SelectedTabIndex = tbInx;
            model.BasvuruSurecID = bsurec.BasvuruSurecID;
            model.ProgramKod = ProgramKod;
            model.OgrenimTipKod = OgrenimTipKod;
            model.ProgramAdi = program.ProgramAdi;
            model.AnabilimDaliKod = program.AnabilimDaliKod;
            model.AnabilimDaliAdi = program.AnabilimDallari.AnabilimDaliAdi;
            model.OrtakKota = kota.OrtakKota;
            model.OrtakKotaSayisi = kota.OrtakKotaSayisi;
            model.AlanIciKota = kota.OrtakKota ? 0 : kota.AlanIciKota;
            model.AlanDisiKota = kota.OrtakKota ? kota.OrtakKotaSayisi.Value : kota.AlanDisiKota;
            model.AlanIciEkKota = kota.AlanIciEkKota ?? 0;
            model.AlanDisiEkKota = kota.AlanDisiEkKota ?? 0;
            model.OgrenimTipAdi = db.OgrenimTipleris.Where(p => p.EnstituKod == bsurec.EnstituKod && p.OgrenimTipKod == OgrenimTipKod).First().OgrenimTipAdi;
            model.BasariNotOrtalamasi = ot.BasariNotOrtalamasi;
            model.BasvuruSurecAdi = bsurec.BaslangicYil + " / " + bsurec.BitisYil + " " + bsurec.Donemler.DonemAdi;
            model.BaslangicTarihi = bsurec.BaslangicTarihi;
            model.BitisTarihi = bsurec.BitisTarihi;
            model.SonucGirisBaslangicTarihi = bsurec.SonucGirisBaslangicTarihi;
            model.SonucGirisBitisTarihi = bsurec.SonucGirisBitisTarihi;
            model.BaslangicYil = bsurec.BaslangicYil;
            model.BitisYil = bsurec.BitisYil;
            model.DonemAdi = bsurec.Donemler.DonemAdi;
            model.MulakatSurecineGirecek = kota.MulakatSurecineGirecek ?? ot.MulakatSurecineGirecek;
            model.IsBelgeYuklemeVar = bsurec.IsBelgeYuklemeVar;
            model.IsAlesYerineDosyaNotuIstensin = kota.IsAlesYerineDosyaNotuIstensin == true;
            model.AlanIciBilimselHazirlik = ot.AlanIciBilimselHazirlik;
            model.AlanDisiBilimselHazirlik = ot.AlanDisiBilimselHazirlik;
            model.MulakatSonuc = Management.getMulakatSonucHesapList(id, ProgramKod, OgrenimTipKod).OrderBy(o => o.AlanTipID).ThenBy(t => t.MulakatSonucTipID == 0 ? 3 : t.MulakatSonucTipID).ThenBy(o => o.SiraNo).ToList();
            model.Ucretli = program.Ucretli;
            model.IsUcretliKayit = model.Ucretli || model.MulakatSonuc.Any(a => a.DekontNo != null || a.DekontTarihi.HasValue);
            model.Ucret = program.Ucret;

            var alanTips = new Dictionary<int, int> { { AlanTipi.AlanIci, model.AlanIciKota }, { AlanTipi.AlanDisi, model.AlanDisiKota } };

            foreach (var atips in alanTips)
            {
                int Kayit = 0;
                int AlanKota = atips.Value;
                int AsilIslem = 0;
                var EkYedekKota = (atips.Key == AlanTipi.AlanIci ? model.AlanIciEkKota : model.AlanDisiEkKota);
                int KalanHk = AlanKota + EkYedekKota;
                var ToplamAlanYedekKota = (AlanKota * ot.YedekOgrenciSayisiKotaCarpani) + EkYedekKota;
                foreach (var item in model.MulakatSonuc.Where(p => p.AlanTipID == atips.Key))
                {

                    item.SuccessRow = false;
                    if (item.MulakatSonucTipID == MulakatSonucTipi.Asil)
                    {
                        if (item.KayitDurumID.HasValue)
                        {
                            AsilIslem++;
                            if (item.KayitDurumlari.IsKayitOldu)
                            {
                                Kayit++;
                                KalanHk--;
                                item.SuccessRow = true;
                            }
                            else item.SuccessRow = true;
                        }
                        else
                        {
                            KalanHk--;
                            item.SuccessRow = true;
                        }


                    }
                    else if (item.MulakatSonucTipID == MulakatSonucTipi.Yedek)
                    {
                        if (AsilIslem == AlanKota && KalanHk > 0 && Kayit < (ToplamAlanYedekKota))
                        {
                            if (item.KayitDurumID.HasValue)
                            {
                                if (item.KayitDurumlari.IsKayitOldu)
                                {
                                    Kayit++;
                                    KalanHk--;
                                    item.SuccessRow = true;
                                }
                                else item.SuccessRow = true;
                            }
                            else
                            {
                                KalanHk--;
                                item.SuccessRow = true;
                            }
                        }
                    }
                }
            }


            if (model.MulakatSurecineGirecek)
            {
                model.YaziliNotuIstensin = db.MulakatDetays.Any(a => a.Mulakat.BasvuruSurecID == model.BasvuruSurecID && a.Mulakat.ProgramKod == model.ProgramKod && a.Mulakat.OgrenimTipKod == model.OgrenimTipKod && a.MulakatSinavTurID == MulakatSinavTur.Yazili);
                model.SozluNotuIstensin = db.MulakatDetays.Any(a => a.Mulakat.BasvuruSurecID == model.BasvuruSurecID && a.Mulakat.ProgramKod == model.ProgramKod && a.Mulakat.OgrenimTipKod == model.OgrenimTipKod && a.MulakatSinavTurID == MulakatSinavTur.Sozlu);
            }
            ViewBag.IsBootBox = IsBootBox;
            ViewBag.IsDelete = IsDelete;
            return View(model);
        }
        [Authorize(Roles = RoleNames.BasvuruSureciSil)]
        public ActionResult Sil(int id)
        {
            var mmMessage = new MmMessage();

            var kayit = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == id).FirstOrDefault();

            string message = "";
            if (kayit != null)
            {
                var qBil = (from s in db.BasvuruSurecs
                            join e in db.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                            join d in db.Donemlers on new { s.DonemID } equals new { d.DonemID }
                            join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                            where s.BasvuruSurecID == id
                            select new
                            {
                                s.BaslangicYil,
                                s.BitisYil,
                                d.DonemAdi
                            }).First();
                try
                {
                    message = "'" + qBil.BaslangicYil + "/" + qBil.BitisYil + " " + qBil.DonemAdi + "' Dönemine ait başvuru süreci silindi!";
                    db.BasvuruSurecs.Remove(kayit);
                    db.SaveChanges();

                    LogIslemleri.LogEkle("BasvuruSurec", IslemTipi.Delete, kayit.ToJson());
                    mmMessage.Title = "Uyarı";
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = Msgtype.Success;
                    mmMessage.IsSuccess = true;
                }
                catch (Exception ex)
                {
                    message = "'" + qBil.BaslangicYil + "/" + qBil.BitisYil + " " + qBil.DonemAdi + "' Dönemine ait başvuru süreci silinirken bir hata oluştu! </br> Hata:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "BasvuruSureci/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                    mmMessage.Title = "Hata";
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                }
            }
            else
            {
                message = "Silmek istediğiniz başvuru süreci sistemde bulunamadı!";
                mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message);
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = true;
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = RoleNames.BasvuruSureciKayit)]
        public ActionResult mailGonderJuriYer(int id, bool JuriBilgisiGonderilsin = false)
        {
            var mmMessage = new MmMessage();
            var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == id).First();
            string SurecAdiB = bsurec.BaslangicYil + " / " + bsurec.BitisYil + " " + db.Donemlers.Where(p => p.DonemID == bsurec.DonemID).First().DonemAdi;
            var bsurecSturs = bsurec.BasvuruSurecMulakatSinavTurleris.ToList();
            try
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet(SurecAdiB + " Başvuru sürecine ait Mülakat Sınav yeri ve jüri bilgisi mail gönderim işlemi başlatıldı!", "BasvuruSureci/mailGonderJuriYer", LogType.Bilgi);
                var data = from s in db.BasvurularTercihleris
                           join bot in db.BasvuruSurecOgrenimTipleris.Where(p => p.BasvuruSurecID == id) on s.OgrenimTipKod equals bot.OgrenimTipKod
                           join kt in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == id) on new { s.ProgramKod, s.OgrenimTipKod } equals new { kt.ProgramKod, kt.OgrenimTipKod }
                           where (kt.MulakatSurecineGirecek.HasValue ? kt.MulakatSurecineGirecek.Value : bot.MulakatSurecineGirecek) == true && s.Basvurular.BasvuruSurecID == id && s.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı
                           select new
                           {
                               s.ProgramKod,
                               s.OgrenimTipKod,
                               s.BasvuruID,
                               s.AlanTipID,
                               s.Basvurular.KullaniciID,
                               EMail = s.Basvurular.EMail
                           };

                var qMulakats = (from s in db.Mulakats
                                 join pr in db.Programlars on s.ProgramKod equals pr.ProgramKod
                                 join b in db.AnabilimDallaris on new { pr.AnabilimDaliKod, bsurec.EnstituKod } equals new { b.AnabilimDaliKod, b.EnstituKod }
                                 join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == bsurec.EnstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod
                                 where s.BasvuruSurecID == id
                                 select new MulakatSinavYerJuriBilgiMailDto
                                 {
                                     MulakatID = s.MulakatID,
                                     BasvuruSurecID = id,
                                     OgrenimTipKod = s.OgrenimTipKod,
                                     AnabilimDaliKod = b.AnabilimDaliKod,
                                     AnabilimDaliAdi = b.AnabilimDaliAdi,
                                     ProgramKod = pr.ProgramKod,
                                     ProgramAdi = pr.ProgramAdi,
                                     OgrenimTipAdi = ot.OgrenimTipAdi
                                 }).ToList();

                var sinavTurleri = db.MulakatSinavTurleris.ToList();
                var kampusler = db.Kampuslers.ToList();
                foreach (var item in qMulakats)
                {
                    var qMD = db.MulakatDetays.Where(p => p.MulakatID == item.MulakatID).ToList();
                    var nQMD = new List<krMulakatDetay>();
                    foreach (var itemQD in qMD)
                    {
                        var sT = sinavTurleri.Where(p => p.MulakatSinavTurID == itemQD.MulakatSinavTurID).FirstOrDefault();
                        var yO = bsurecSturs.Where(p => p.MulakatSinavTurID == itemQD.MulakatSinavTurID).FirstOrDefault();
                        var kA = kampusler.Where(p => p.KampusID == itemQD.KampusID).FirstOrDefault();

                        nQMD.Add(new krMulakatDetay
                        {
                            MulakatID = itemQD.MulakatID,
                            MulakatSinavTurID = itemQD.MulakatSinavTurID,
                            MulakatSinavTurAdi = sT.MulakatSinavTurAdi,
                            YuzdeOran = itemQD.YuzdeOran,
                            SinavTarihi = itemQD.SinavTarihi,
                            KampusAdi = kA.KampusAdi,
                            KampusID = itemQD.KampusID,
                            YerAdi = itemQD.YerAdi

                        });
                    }
                    item.MulakatDetayB = nQMD.OrderBy(o => o.SinavTarihi).ToList();
                    item.MulakatJuriB = db.MulakatJuris.Where(p => p.MulakatID == item.MulakatID).OrderByDescending(o => o.IsAsil).ThenBy(t => t.SiraNo).ToList();
                    item.GonderilecekMails = data.Where(p => p.ProgramKod == item.ProgramKod && p.OgrenimTipKod == item.OgrenimTipKod).Select(s => new CmbIntDto { Value = s.KullaniciID, Caption = s.EMail }).ToList();
                }
                var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(bsurec.EnstituKod);
                var _ea = mailBilgi.SistemErisimAdresi;
                var WurlAddr = _ea.Split('/').ToList();
                if (_ea.Contains("//"))
                    _ea = WurlAddr[0] + "//" + WurlAddr.Skip(2).Take(1).First();
                else
                    _ea = "http://" + WurlAddr.First();

                var sablons = db.MailSablonlaris.Where(p => p.MailSablonTipleri.SistemMaili && p.EnstituKod == bsurec.EnstituKod).ToList();
                var enstitus = db.Enstitulers.ToList();
                foreach (var item in qMulakats)
                {
                    if (item.GonderilecekMails.Count > 0)
                    {
                        var mmmC = new MailMainContentDto();
                        var enstitu = enstitus.Where(p => p.EnstituKod == bsurec.EnstituKod).First();

                        var sablon = sablons.Where(p => p.MailSablonTipID == MailSablonTipi.OtoMailSinavYerBilgi).FirstOrDefault();
                        var webadresLink = "<a href='" + enstitu.SistemErisimAdresi + "' target='_blank'>" + enstitu.SistemErisimAdresi + "</a>";
                        var sablonHtml = sablon.SablonHtml.Replace("@EnstituAdi", enstitu.EnstituAd).Replace("@WebAdresi", webadresLink);
                        mmmC.EnstituAdi = enstitu.EnstituAd;
                        mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
                        mmmC.LogoPath = _ea + "/Content/assets/images/ytu_logo_tr.png";
                        string SurecAdi = bsurec.BaslangicYil + " / " + bsurec.BitisYil + " " + db.Donemlers.Where(p => p.DonemID == bsurec.DonemID).First().DonemAdi;


                        var mtc = new MailTableContentDto(); 
                        mtc.Detaylar.Add(new MailTableRowDto { Baslik = "Başvuru Dönemi", Aciklama = SurecAdi });
                        mtc.Detaylar.Add(new MailTableRowDto { Baslik = "Anabilim Dalı / Program", Aciklama = (item.AnabilimDaliAdi + " / " + item.ProgramAdi) });
                        mtc.Detaylar.Add(new MailTableRowDto { Baslik = "Öğrenim Seviyesi", Aciklama = item.OgrenimTipAdi });
                        var mdHtml = new List<string>();
                        foreach (var itmMD in item.MulakatDetayB)
                        {
                            var mtcSinavB = new MailTableContentDto();
                            mtcSinavB.GrupBasligi = itmMD.MulakatSinavTurAdi;
                            mtcSinavB.Detaylar.Add(new MailTableRowDto { Baslik = "Kampüs", Aciklama = itmMD.KampusAdi });
                            mtcSinavB.Detaylar.Add(new MailTableRowDto { Baslik = "Sınav Yeri", Aciklama = itmMD.YerAdi });
                            mtcSinavB.Detaylar.Add(new MailTableRowDto { Baslik = "Sınav Tarihi", Aciklama = itmMD.SinavTarihi.ToFormatDateAndTime() });
                            mtcSinavB.Detaylar.Add(new MailTableRowDto { Baslik = "Sınav Tipi", Aciklama = itmMD.MulakatSinavTurAdi }); 
                            mdHtml.Add(ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", mtcSinavB));

                        }
                        mtc.Detaylar.Add(new MailTableRowDto
                        {
                            Colspan2 = true,
                            Aciklama = string.Join(" ", mdHtml)
                        });
                        if (JuriBilgisiGonderilsin)
                        {
                            var mtcSinavJ = new MailTableContentDto();
                            mtcSinavJ.IsJuriBilgi = true;
                            mtcSinavJ.GrupBasligi = "Jüri Bilgisi";
                            foreach (var itemJr in item.MulakatJuriB)
                            {
                                mtcSinavJ.Detaylar.Add(new MailTableRowDto { SiraNo = itemJr.SiraNo, Baslik = itemJr.JuriAdi, Aciklama = itemJr.IsAsil ? "Asil" : "Yedek" });
                            }
                            mtc.Detaylar.Add(new MailTableRowDto
                            {
                                Colspan2 = true,
                                Aciklama = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", mtcSinavJ)
                            });
                        }
                        var tavleContent = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", mtc);
                        mmmC.Content = sablonHtml + "<br/>" + tavleContent;
                        string htmlMail = ViewRenderHelper.RenderPartialView("Ajax", "getMailContent", mmmC);

                        var User = mailBilgi.SmtpKullaniciAdi;
                        var EMailList = item.GonderilecekMails.Distinct().Select(s => new MailSendList { EMail = s.Caption, ToOrBcc = false }).ToList();
                        var snded = MailManager.SendMail(mailBilgi.EnstituKod, sablon.SablonAdi, htmlMail, EMailList, null);
                        if (snded)
                        {

                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = enstitu.EnstituAd;

                            kModel.EnstituKod = mailBilgi.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = sablon.SablonAdi + ": " + (item.AnabilimDaliAdi + " / " + item.ProgramAdi);
                            kModel.IslemYapanID = UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = "";
                            kModel.AciklamaHtml = htmlMail ?? "";
                            kModel.Gonderildi = true;
                            var eklenen = db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                            foreach (var itemGm in item.GonderilecekMails.Distinct())
                            {
                                db.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGm.Caption, GonderilenMailID = kModel.GonderilenMailID, KullaniciID = itemGm.Value });
                            }
                            eklenen.Gonderildi = true;
                            db.SaveChanges();
                        }
                    }
                }
                bsurec.SinavYerBilgisiOgrenciMailiGonderildi = true;
                bsurec.SinavYerBilgisiOgrenciMailiGonderimTarihi = DateTime.Now;
                db.SaveChanges();
                SistemBilgilendirmeBus.SistemBilgisiKaydet(SurecAdiB + " Başvuru sürecine ait Mülakat Sınav yeri ve jüri bilgisi mail gönderim işlemi tamamlandı! " + data.Select(s => s.EMail).Distinct().Count() + " \r\nkişiye mail gönderildi!", "BasvuruSureci/mailGonderJuriYer", LogType.Bilgi);
                var message = "'" + SurecAdiB + "'  başvuru sürecine ait yer / jüri bilgisi mail başarılı bir şekilde gönderildi!";
                mmMessage.IsSuccess = true;
                mmMessage.Title = "Mail Gönderim İşlemi Başarılı";
                mmMessage.Messages.Add(message);
                mmMessage.MessageType = Msgtype.Success;
            }
            catch (Exception ex)
            {

                var title = "'" + SurecAdiB + "'  başvuru sürecine ait yer / jüri bilgisi mail gönderilirken bir hata oluştu!";
                var hata = "</br> Hata:" + ex.ToExceptionMessage();
                SistemBilgilendirmeBus.SistemBilgisiKaydet(title + "\r\n Hata:" + ex.ToExceptionMessage(), "BasvuruSureci/mailGonderJuriYer", LogType.Hata);
                mmMessage.Title = title;
                mmMessage.Messages.Add(hata);
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;

            }

            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = RoleNames.BasvuruSureciKayit)]
        public ActionResult mailGonderBolumSinavYerBilgiGiris(int id, bool isSinavYerOrSinavNotGiris)
        {
            var mmMessage = new MmMessage();
            var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == id).First();
            string SurecAdiB = bsurec.BaslangicYil + " / " + bsurec.BitisYil + " " + db.Donemlers.Where(p => p.DonemID == bsurec.DonemID).First().DonemAdi;
            string SinavBilgiGirisSurec = bsurec.BaslangicTarihi.ToFormatDateAndTime() + " - " + bsurec.BitisTarihi.ToFormatDateAndTime();
            var SinavBilgiGirisSureckalanGun = (bsurec.BitisTarihi - DateTime.Now).Days;
            string SinavNotGirisSurec = bsurec.SonucGirisBaslangicTarihi.Value.ToFormatDateAndTime() + " - " + bsurec.SonucGirisBitisTarihi.Value.ToFormatDateAndTime();
            var SinavNotGirisSureckalanGun = (bsurec.SonucGirisBitisTarihi.Value - DateTime.Now).Days;


            var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(bsurec.EnstituKod);
            var erisimAdresi = mailBilgi.SistemErisimAdresi + "/MulakatSureci/Index";
            SistemBilgilendirmeBus.SistemBilgisiKaydet(SurecAdiB + " Başvuru sürecine ait Mülakat " + (isSinavYerOrSinavNotGiris ? "Sınav yer ve Jüri bilgileri" : "Sınav Notu") + " girişi için Anabilim Dallarına bilgi maili gönderim işlemi başlatıldı!", "BasvuruSureci/mailGonderBolumSinavYerBilgiGiris", LogType.Bilgi);
            try
            {
                var rollers = new List<string> { RoleNames.MulakatSureci, RoleNames.MulakatKayıt };
                var qData = (from s in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == id)
                             join p in db.Programlars on s.ProgramKod equals p.ProgramKod
                             join b in db.AnabilimDallaris.Where(p => p.EnstituKod == bsurec.EnstituKod) on p.AnabilimDaliKod equals b.AnabilimDaliKod
                             join ot in db.BasvuruSurecOgrenimTipleris.Where(p => p.BasvuruSurecID == id) on s.OgrenimTipKod equals ot.OgrenimTipKod
                             join otOr in db.OgrenimTipleris.Where(p => p.EnstituKod == bsurec.EnstituKod) on ot.OgrenimTipKod equals otOr.OgrenimTipKod
                             where (s.MulakatSurecineGirecek.HasValue ? s.MulakatSurecineGirecek : ot.MulakatSurecineGirecek) == true //programa özel mülakat var ise ona bakılsın yoksa öğrenim tipine göre bakılsın
                             group new
                             {
                                 s.BasvuruSurecID,
                                 b.AnabilimDaliKod,
                                 b.AnabilimDaliAdi,
                                 p.ProgramKod,
                                 s.AlanIciKota,
                                 s.AlanDisiKota,
                                 p.ProgramAdi,
                                 ot.OgrenimTipKod,
                                 otOr.OgrenimTipAdi
                             } by new
                             {
                                 s.BasvuruSurecID,
                                 b.AnabilimDaliKod,
                                 b.AnabilimDaliAdi
                             } into g1
                             select new MulakatSinavYerJuriBilgiBolumMailDto
                             {
                                 AnabilimDaliKod = g1.Key.AnabilimDaliKod,
                                 AnabilimDaliAdi = g1.Key.AnabilimDaliAdi,

                                 GonderilecekMails = db.KullaniciProgramlaris.Where(p =>
                                                             (p.Kullanicilar.YetkiGruplari.YetkiGrupRolleris.Any(a => rollers.Contains(a.Roller.RolAdi)) || p.Kullanicilar.Rollers.Any(a => rollers.Contains(a.RolAdi)))
                                                             && g1.Select(s => s.ProgramKod).Contains(p.ProgramKod)).Select(s2 => s2.Kullanicilar.EMail).ToList(),
                                 MulakatSinavYerJuriBilgiBolumDetayMailDtos = g1.Where(p => p.AnabilimDaliKod == g1.Key.AnabilimDaliKod).Select(s => new MulakatSinavYerJuriBilgiBolumDetayMailDto
                                 {
                                     EksikBilgiSinavYerBilgi = db.Mulakats.Where(p => p.BasvuruSurecID == g1.Key.BasvuruSurecID && p.ProgramKod == s.ProgramKod && p.OgrenimTipKod == s.OgrenimTipKod).Any() == false,
                                     ProgramKod = s.ProgramKod,
                                     ProgramAdi = s.ProgramAdi,
                                     OgrenimTipKod = s.OgrenimTipKod,
                                     OgrenimTipAdi = s.OgrenimTipAdi,
                                     AlaniciKota = s.AlanIciKota,
                                     AlandisiKota = s.AlanDisiKota,
                                     AlaniciBasvuranCount = db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruSurecID == g1.Key.BasvuruSurecID && p.OgrenimTipKod == s.OgrenimTipKod && p.ProgramKod == s.ProgramKod && p.AlanTipID == AlanTipi.AlanIci && p.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı).Count(),
                                     AlandisiBasvuranCount = db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruSurecID == g1.Key.BasvuruSurecID && p.OgrenimTipKod == s.OgrenimTipKod && p.ProgramKod == s.ProgramKod && p.AlanTipID == AlanTipi.AlanDisi && p.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı).Count()
                                 }).ToList()
                             }).AsQueryable();

                var qDataList = new List<MulakatSinavYerJuriBilgiBolumMailDto>();
                if (isSinavYerOrSinavNotGiris)
                {
                    qDataList = qData.Where(p => p.MulakatSinavYerJuriBilgiBolumDetayMailDtos.Any(a => a.EksikBilgiSinavYerBilgi)).ToList();
                }
                else
                {
                    var tercihS = db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruSurecID == id && p.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı && p.Basvurular.BasvuruSurec.BasvuruSurecOgrenimTipleris.Any(a => a.OgrenimTipKod == p.OgrenimTipKod && a.MulakatSurecineGirecek)).ToList();
                    qDataList = qData.ToList();
                    foreach (var item in qDataList)
                    {
                        var mdetay = new List<MulakatSinavYerJuriBilgiBolumDetayMailDto>();
                        foreach (var item2 in item.MulakatSinavYerJuriBilgiBolumDetayMailDtos)
                        {
                            var sTips = db.MulakatDetays.Where(p => p.Mulakat.ProgramKod == item2.ProgramKod && p.Mulakat.OgrenimTipKod == item2.OgrenimTipKod && p.Mulakat.BasvuruSurecID == id).ToList();
                            var qBCount = tercihS.Where(p => p.ProgramKod == item2.ProgramKod && item2.OgrenimTipKod == p.OgrenimTipKod).Count();

                            var yaziliNotGirisiIstensin = sTips.Any(a => a.MulakatSinavTurID == MulakatSinavTur.Yazili);
                            var sozluNotGirisiIstensin = sTips.Any(a => a.MulakatSinavTurID == MulakatSinavTur.Sozlu);
                            var qsonucs = db.MulakatSonuclaris.Where(p => p.BasvuruSurecID == id && p.BasvurularTercihleri.ProgramKod == item2.ProgramKod && p.BasvurularTercihleri.OgrenimTipKod == item2.OgrenimTipKod);
                            var qdatatat = qsonucs.ToList();
                            if (yaziliNotGirisiIstensin)
                            {
                                qsonucs = qsonucs.Where(p => p.SinavaGirmediY.HasValue);
                            }
                            if (sozluNotGirisiIstensin)
                            {
                                qsonucs = qsonucs.Where(p => p.SinavaGirmediS.HasValue);
                            }
                            var notGirisCount = qsonucs.Count();
                            if (qBCount > 0 && qBCount != notGirisCount)
                            {
                                mdetay.Add(item2);
                            }

                        }
                        item.MulakatSinavYerJuriBilgiBolumDetayMailDtos = mdetay;

                    }
                    qDataList = qDataList.Where(p => p.MulakatSinavYerJuriBilgiBolumDetayMailDtos.Count > 0).ToList();

                }

                var _ea = mailBilgi.SistemErisimAdresi;
                var WurlAddr = _ea.Split('/').ToList();
                if (_ea.Contains("//"))
                    _ea = WurlAddr[0] + "//" + WurlAddr.Skip(2).Take(1).First();
                else
                    _ea = "http://" + WurlAddr.First();
                foreach (var item in qDataList)
                {
                    var mmmC = new MailMainContentDto();
                    var enstituAdi = db.Enstitulers.Where(p => p.EnstituKod == bsurec.EnstituKod).First().EnstituAd;
                    mmmC.EnstituAdi = enstituAdi;
                    mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
                    mmmC.LogoPath = _ea + "/Content/assets/images/ytu_logo_tr.png";

                    var mtc = new MailTableContentDto();
                    mtc.AciklamaBasligi = "";
                    mtc.Detaylar.Add(new MailTableRowDto { Baslik = "Başvuru Dönem Adı", Aciklama = SurecAdiB });
                    mtc.Detaylar.Add(new MailTableRowDto { Baslik = "Anabilim Dalı Adı", Aciklama = item.AnabilimDaliAdi });
                    var qdetay = item.MulakatSinavYerJuriBilgiBolumDetayMailDtos.AsQueryable();
                    if (isSinavYerOrSinavNotGiris)
                    {
                        qdetay = qdetay.Where(a => a.EksikBilgiSinavYerBilgi);
                        mtc.Detaylar.Add(new MailTableRowDto { Baslik = "Mülakat Bilgi Giriş Süreci", Aciklama = SinavBilgiGirisSurec });
                        mtc.Detaylar.Add(new MailTableRowDto { Baslik = "Bilgi Girişi Kalan Süre", Aciklama = SinavBilgiGirisSureckalanGun == 0 ? "Bu gün bilgi girişi için son gün" : (SinavBilgiGirisSureckalanGun + " Gün") });
                        mtc.Detaylar.Add(new MailTableRowDto { Colspan2 = true, Aciklama = "Aşağıda bulunan programlara ait Sınav giriş ve Jüri bilgileri " + bsurec.BitisTarihi.ToFormatDateAndTime() + " Tarihine kadar eksiksiz bir şekilde girilmesi gerekmektedir." });
                    }
                    else
                    {

                        mtc.Detaylar.Add(new MailTableRowDto { Baslik = "Mülakat Not Giriş Süreci", Aciklama = SinavNotGirisSurec });
                        mtc.Detaylar.Add(new MailTableRowDto { Baslik = "Not Girişi İçin Kalan Süre", Aciklama = SinavNotGirisSureckalanGun == 0 ? "Bu gün not girişi için son gün" : (SinavNotGirisSureckalanGun + " Gün") });
                        mtc.Detaylar.Add(new MailTableRowDto { Colspan2 = true, Aciklama = "Aşağıda bulunan programlara ait Sınav notlarının " + bsurec.SonucGirisBitisTarihi.Value.ToFormatDateAndTime() + " Tarihine kadar eksiksiz bir şekilde girilmesi gerekmektedir." });
                    }
                    var mdHtml = new List<string>();
                    var qdetayList = qdetay.ToList();
                    foreach (var itemD in qdetayList)
                    {
                        var mtcSinavB = new MailTableContentDto();
                        mtcSinavB.GrupBasligi = itemD.ProgramAdi + " [" + itemD.OgrenimTipAdi + "]";
                        mtcSinavB.Detaylar.Add(new MailTableRowDto { Baslik = "Program Adı", Aciklama = itemD.ProgramAdi });
                        mtcSinavB.Detaylar.Add(new MailTableRowDto { Baslik = "Öğrenim Seviyesi", Aciklama = itemD.OgrenimTipAdi });
                        mtcSinavB.Detaylar.Add(new MailTableRowDto { Baslik = "Alaniçi Kota", Aciklama = itemD.AlaniciKota.ToString() });
                        mtcSinavB.Detaylar.Add(new MailTableRowDto { Baslik = "Alandışı Kota", Aciklama = itemD.AlandisiKota.ToString() });
                        if (isSinavYerOrSinavNotGiris == false)
                        {
                            mtcSinavB.Detaylar.Add(new MailTableRowDto { Baslik = "Alaniçi Başvuran", Aciklama = itemD.AlaniciBasvuranCount > 0 ? (itemD.AlaniciBasvuranCount + " Kişi") : "Başvuran Yok" });
                            mtcSinavB.Detaylar.Add(new MailTableRowDto { Baslik = "Alandışı Başvuran", Aciklama = itemD.AlandisiBasvuranCount > 0 ? (itemD.AlandisiBasvuranCount + " Kişi") : "Başvuran Yok" });
                        }
                        mdHtml.Add(ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", mtcSinavB));
                    }
                    mtc.Detaylar.Add(new MailTableRowDto
                    {
                        Colspan2 = true,
                        Aciklama = string.Join(" ", mdHtml)
                    });
                    mtc.Detaylar.Add(new MailTableRowDto { Baslik = "Erişim Adresi", Aciklama = "<a href='" + erisimAdresi + "' target='_blank'>Sisteme giriş yapmak için tıklayınız.</a>" });
                    var tavleContent = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", mtc);
                    mmmC.Content = tavleContent;
                    string htmlMail = ViewRenderHelper.RenderPartialView("Ajax", "getMailContent", mmmC);

                    var User = mailBilgi.SmtpKullaniciAdi;
                    var MailList = item.GonderilecekMails.Distinct().Select(s => new MailSendList { EMail = s, ToOrBcc = false }).ToList();
                    var snded = MailManager.SendMail(mailBilgi.EnstituKod, enstituAdi, htmlMail, MailList, null);
                    if (snded)
                    {
                        using (var db1 = new LisansustuBasvuruSistemiEntities())
                        {
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = mailBilgi.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Gonderildi = true;
                            kModel.Konu = enstituAdi + (isSinavYerOrSinavNotGiris ? " Sınav Yer/Jüri Bilgi Girişi Hk." : " Sınav Notu Girişi Hk.");
                            kModel.IslemYapanID = UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Silindi = false;
                            kModel.Aciklama = "";
                            kModel.AciklamaHtml = htmlMail;

                            var eklenen = db1.GonderilenMaillers.Add(kModel);
                            db1.SaveChanges();
                            foreach (var item2 in item.GonderilecekMails.Distinct())
                            {
                                db1.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar
                                {

                                    Email = item2,
                                    GonderilenMailID = eklenen.GonderilenMailID,
                                    KullaniciID = null
                                });
                            }
                            db1.SaveChanges();
                        }


                    }

                }
                if (isSinavYerOrSinavNotGiris)
                {
                    bsurec.SinavYerBilgisiBolumMailiGonderildi = true;
                    bsurec.SinavYerBilgisiBolumMailiGonderimTarihi = DateTime.Now;
                }
                else
                {
                    bsurec.SinavNotBilgisiBolumMailiGonderildi = true;
                    bsurec.SinavNotBilgisiBolumMailiGonderimTarihi = DateTime.Now;
                }
                db.SaveChanges();
                SistemBilgilendirmeBus.SistemBilgisiKaydet(SurecAdiB + " Başvuru sürecine ait Mülakat " + (isSinavYerOrSinavNotGiris ? "Sınav giriş ve Jüri bilgileri" : "Sınav Notu") + " girişi için Anabilim Dallarına bilgi maili gönderimi tamamlandı!  \r\n" + qDataList.Count() + " Anabilim Dalına mail gönderildi!", "BasvuruSureci/mailGonderBolumSinavYerBilgiGiris", LogType.Bilgi);
                var message = "'" + SurecAdiB + "'  Başvuru sürecine ait Mülakat " + (isSinavYerOrSinavNotGiris ? "Sınav giriş ve Jüri bilgileri" : "Sınav Notu") + " girişi için \r\n" + qDataList.Count() + " Bölüme başarılı bir şekilde mail gönderildi!";
                mmMessage.IsSuccess = true;
                mmMessage.Title = "Mail Gönderim İşlemi Başarılı";
                mmMessage.Messages.Add(message);
                mmMessage.MessageType = Msgtype.Success;
            }
            catch (Exception ex)
            {

                var message = "'" + SurecAdiB + "'  Başvuru sürecine ait Mülakat " + (isSinavYerOrSinavNotGiris ? "Sınav giriş ve Jüri bilgileri" : "Sınav Notu") + " girişi için Anabilim Dallarına bilgi maili gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "BasvuruSureci/mailGonderBolumSinavYerBilgiGiris", LogType.Hata);
                mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;

            }

            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new
            {
                IsSuccess = mmMessage.IsSuccess,
                Messages = strView
            }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = RoleNames.BasvuruSureciKayit)]
        public ActionResult basvuruSonucMailGonderim(int id, bool IsSonucOrAnket, string OgrenimTipKods)
        {
            var mmMessage = new MmMessage();
            var OgrenimTipKodList = new List<int>();
            if (!OgrenimTipKods.IsNullOrWhiteSpace()) OgrenimTipKodList = OgrenimTipKods.Split(',').Select(s => s.ToInt().Value).ToList();
            var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == id).First();
            string SurecAdiB = bsurec.BaslangicYil + " / " + bsurec.BitisYil + " " + db.Donemlers.Where(p => p.DonemID == bsurec.DonemID).First().DonemAdi;

            if (!OgrenimTipKods.Any())
            {
                mmMessage.Messages.Add("Toplu başvuru sonucu maili gönderebilmek içimn en az 1 öğrenim seviyesi seçmelisiniz.");
            }

            if (mmMessage.Messages.Count == 0)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet(SurecAdiB + " Başvuru sürecine ait başvuru " + (IsSonucOrAnket ? "sonuçları" : "Anket giriş bilgileri") + " öğrencilere gönderilmeye başlandı!", "BasvuruSureci/basvuruSonucMailGonderim", LogType.Bilgi);
                try
                {
                    var Bsonuc = (from s in db.MulakatSonuclaris.Where(p => p.BasvuruSurecID == id && (!IsSonucOrAnket ? (p.KayitDurumID.HasValue && p.KayitDurumlari.IsKayitOldu != true && p.MulakatSonucTipID == MulakatSonucTipi.Asil) : 1 == 1))
                                  join bt in db.BasvurularTercihleris.Where(p => OgrenimTipKodList.Contains(p.OgrenimTipKod)) on s.BasvuruTercihID equals bt.BasvuruTercihID
                                  join b in db.Basvurulars.Where(p => (!IsSonucOrAnket ? !p.AnketCevaplaris.Any() : 1 == 1)) on bt.BasvuruID equals b.BasvuruID
                                  join mst in db.MulakatSonucTipleris on new { s.MulakatSonucTipID } equals new { mst.MulakatSonucTipID }
                                  join bs in db.BasvuruSurecs on b.BasvuruSurecID equals bs.BasvuruSurecID
                                  join bd in db.Donemlers on new { bs.DonemID } equals new { bd.DonemID }
                                  join pr in db.Programlars on new { bt.ProgramKod } equals new { pr.ProgramKod }
                                  join abd in db.AnabilimDallaris on new { bs.EnstituKod, pr.AnabilimDaliKod } equals new { abd.EnstituKod, abd.AnabilimDaliKod }
                                  join enst in db.Enstitulers on new { bs.EnstituKod } equals new { enst.EnstituKod }

                                  //where bt.ProgramKod == "701" && bt.OgrenimTipKod == 1
                                  select new MailBsonucModel
                                  {
                                      BasvuruID = b.BasvuruID,
                                      RowID = b.RowID,
                                      SiraNo = s.SiraNo.Value,
                                      AdSoyad = b.Ad + " " + b.Soyad,
                                      EgitimOgretimYili = bs.BaslangicYil + " / " + bs.BitisYil + " " + bd.DonemAdi,
                                      AnabilimDaliAdi = abd.AnabilimDaliAdi,
                                      ProgramAdi = pr.ProgramAdi,
                                      GirisSinavNotu = s.GirisSinavNotu,
                                      GenelBasariNotu = s.GenelBasariNotu ?? 0,
                                      MulakatSonucTipID = mst.MulakatSonucTipID,
                                      MulakatSonucTipAdi = mst.MulakatSonucTipAdi,
                                      EnstituAdi = enst.EnstituAd,
                                      WebAdresi = enst.WebAdresi,
                                      Email = b.EMail,
                                      KullaniciID = b.KullaniciID
                                  }).OrderBy(o => o.AnabilimDaliAdi).ThenBy(t => t.ProgramAdi).ThenBy(o => o.SiraNo).ToList();//.OrderBy(o => new Guid()).ToList();

                    var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(bsurec.EnstituKod);

                    var _ea = mailBilgi.SistemErisimAdresi.ToLower();
                    var WurlAddr = _ea.Split('/').ToList();
                    if (_ea.Contains("//"))
                        _ea = WurlAddr[0] + "//" + WurlAddr.Skip(2).Take(1).First();
                    else
                        _ea = "http://" + WurlAddr.First();

                    var mailContents = db.MailSablonlaris.Where(p => p.EnstituKod == bsurec.EnstituKod && p.MailSablonTipleri.SistemMaili && p.IsAktif).ToList();
                    var Enstitu = db.Enstitulers.Where(p => p.EnstituKod == bsurec.EnstituKod).First();

                    var attchL = new Dictionary<string, List<System.Net.Mail.Attachment>>();
                    foreach (var itemC in mailContents)
                    {
                        var attach = new List<System.Net.Mail.Attachment>();
                        foreach (var itemAttc in itemC.MailSablonlariEkleris)
                        {
                            var ekTamYol = Server.MapPath("~" + itemAttc.EkDosyaYolu);
                            var ekAdi = itemAttc.EkAdi + "." + itemAttc.EkDosyaYolu.Split('.').Last();
                            if (System.IO.File.Exists(ekTamYol))
                                attach.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)), ekAdi, System.Net.Mime.MediaTypeNames.Application.Octet));

                        }
                        if (attach.Count > 0) attchL.Add(itemC.MailSablonTipID + "_", attach);
                    }
                    foreach (var item in Bsonuc)
                    {
                        var mmmC = new MailMainContentDto();
                        item.WebAdresi = mailBilgi.WebAdresi;
                        item.Link = mailBilgi.SistemErisimAdresi + "/home/index?basvuruid=" + item.BasvuruID + "&rowid=" + item.RowID;
                        mmmC.EnstituAdi = Enstitu.EnstituAd;
                        mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";


                        mmmC.LogoPath = _ea + "/Content/assets/images/ytu_logo_tr.png";
                        string content = "";
                        string Konu = "";
                        var _cont = new MailSablonlari();
                        if (IsSonucOrAnket)
                        {
                            if (item.MulakatSonucTipID == MulakatSonucTipi.Asil)
                            {
                                _cont = mailContents.Where(p => p.MailSablonTipID == MailSablonTipi.OtoMailAsil).First();
                                content = _cont.SablonHtml;
                                Konu = _cont.SablonAdi;
                            }
                            else if (item.MulakatSonucTipID == MulakatSonucTipi.Yedek)
                            {
                                _cont = mailContents.Where(p => p.MailSablonTipID == MailSablonTipi.OtoMailYedek).First();
                                content = _cont.SablonHtml;
                                Konu = _cont.SablonAdi;
                            }
                            else if (item.MulakatSonucTipID == MulakatSonucTipi.Kazanamadı)
                            {
                                _cont = mailContents.Where(p => p.MailSablonTipID == MailSablonTipi.OtoMailKazanamadi).First();
                                content = _cont.SablonHtml;
                                Konu = _cont.SablonAdi;
                            }
                        }
                        else
                        {
                            _cont = mailContents.Where(p => p.MailSablonTipID == MailSablonTipi.OtoMailAnketBilgi).First();
                            content = _cont.SablonHtml;
                            Konu = _cont.SablonAdi;
                        }
                        #region replaces
                        content = content.Replace("{{", "{{_removeRw_");
                        var contentStrList = content.Split(new string[] { "{{", "}}" }, StringSplitOptions.None).ToList();

                        if (item.AdSoyad.IsNullOrWhiteSpace())
                        {
                            contentStrList = contentStrList.Where(p => p.Contains("@AdSoyad") == false && p.Contains("_removeRw_")).ToList();
                        }
                        if (item.EgitimOgretimYili.IsNullOrWhiteSpace())
                        {
                            contentStrList = contentStrList.Where(p => p.Contains("@OgretimYili") == false && p.Contains("_removeRw_")).ToList();
                        }
                        if (item.AnabilimDaliAdi.IsNullOrWhiteSpace())
                        {
                            contentStrList = contentStrList.Where(p => p.Contains("@AnabilimdaliAdi") == false && p.Contains("_removeRw_")).ToList();
                        }
                        if (item.ProgramAdi.IsNullOrWhiteSpace())
                        {
                            contentStrList = contentStrList.Where(p => p.Contains("@ProgramAdi") == false && p.Contains("_removeRw_")).ToList();
                        }
                        if (!item.GirisSinavNotu.HasValue)
                        {
                            contentStrList = contentStrList.Where(p => p.Contains("@GSN") == false && p.Contains("_removeRw_") == false).ToList();
                        }
                        if (!item.GenelBasariNotu.HasValue)
                        {
                            contentStrList = contentStrList.Where(p => p.Contains("@GBNO") == false && p.Contains("_removeRw_")).ToList();
                        }
                        if (item.EnstituAdi.IsNullOrWhiteSpace())
                        {
                            contentStrList = contentStrList.Where(p => p.Contains("@EnstituAdi") == false && p.Contains("_removeRw_")).ToList();
                        }
                        if (item.WebAdresi.IsNullOrWhiteSpace())
                        {
                            contentStrList = contentStrList.Where(p => p.Contains("@WebAdresi") == false && p.Contains("_removeRw_")).ToList();
                        }
                        if (item.Link.IsNullOrWhiteSpace())
                        {
                            contentStrList = contentStrList.Where(p => p.Contains("@Link") == false && p.Contains("_removeRw_")).ToList();
                        }
                        content = string.Join("", contentStrList);
                        if (item.AdSoyad.IsNullOrWhiteSpace() == false)
                        {
                            content = content.Replace("@AdSoyad", item.AdSoyad);
                        }
                        if (item.EgitimOgretimYili.IsNullOrWhiteSpace() == false)
                        {
                            content = content.Replace("@OgretimYili", item.EgitimOgretimYili);
                        }
                        if (item.AnabilimDaliAdi.IsNullOrWhiteSpace() == false)
                        {
                            content = content.Replace("@AnabilimdaliAdi", item.AnabilimDaliAdi);
                        }
                        if (item.ProgramAdi.IsNullOrWhiteSpace() == false)
                        {
                            content = content.Replace("@ProgramAdi", item.ProgramAdi);
                        }
                        if (item.GirisSinavNotu.HasValue)
                        {
                            content = content.Replace("@GSN", item.GirisSinavNotu.Value.ToString());
                        }
                        if (item.GenelBasariNotu.HasValue)
                        {
                            content = content.Replace("@GBNO", item.GenelBasariNotu.Value.ToString());
                        }
                        if (item.EnstituAdi.IsNullOrWhiteSpace() == false)
                        {
                            content = content.Replace("@EnstituAdi", mmmC.EnstituAdi);
                        }
                        if (item.WebAdresi.IsNullOrWhiteSpace() == false)
                        {
                            var webadresLink = "<a href='" + item.WebAdresi + "' target='_blank'>" + item.WebAdresi + "</a>";

                            content = content.Replace("@WebAdresi", webadresLink);
                        }
                        if (item.Link.IsNullOrWhiteSpace() == false)
                        {
                            var webadresLink = "<a href='" + item.Link + "' target='_blank'>" + item.Link + "</a>";
                            content = content.Replace("@Link", webadresLink);
                        }
                        #endregion

                        mmmC.Content = content.Replace("_removeRw_", "");
                        string htmlMail = ViewRenderHelper.RenderPartialView("Ajax", "getMailContent", mmmC);

                        var selectedAttachL = attchL.Where(p => p.Key == (_cont.MailSablonTipID + "_")).FirstOrDefault();
                        var attach = new List<System.Net.Mail.Attachment>();
                        if (attchL.Any(a => a.Key == (_cont.MailSablonTipID + "_"))) attach = selectedAttachL.Value;

                        var snded = MailManager.SendMail(mailBilgi.EnstituKod, Konu, htmlMail, item.Email, attach);//item.Email
                        if (snded)
                        {

                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = mailBilgi.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = (IsSonucOrAnket ? "Başvuru Sonucu: " : "Anket Bilgisi Gönderimi:") + " " + item.ProgramAdi + ", " + item.AdSoyad + " (" + item.MulakatSonucTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = "";
                            kModel.AciklamaHtml = htmlMail ?? "";
                            kModel.Gonderildi = true;
                            var eklenen = db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                            db.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = item.Email, GonderilenMailID = kModel.GonderilenMailID, KullaniciID = item.KullaniciID });
                            eklenen.Gonderildi = true;
                            db.SaveChanges();
                        }
                    }
                    if (IsSonucOrAnket)
                    {
                        bsurec.SinavSonucBilgisiOgrenciMailiGonderildi = true;
                        bsurec.SinavSonucBilgisiOgrenciMailiGonderimTarihi = DateTime.Now;

                    }
                    else
                    {
                        bsurec.KayitOlmayanOgrencilereAnketLinkiGonderildi = true;
                        bsurec.KayitOlmayanOgrencilereAnketLinkiGonderimTarihi = DateTime.Now;
                    }
                    db.SaveChanges();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(SurecAdiB + " Başvuru sürecine ait başvuru " + (IsSonucOrAnket ? "sonuçları" : "Anket giriş bilgileri") + " Öğrencilere mail olarak gönderildi! \r\n  " + Bsonuc.Count + " Öğrenciye mail gönderildi!", "BasvuruSureci/basvuruSonucMailGonderim", LogType.Bilgi);
                    var message = "'" + SurecAdiB + "'  Başvuru sürecine ait başvuru " + (IsSonucOrAnket ? "sonuçları" : "Anket giriş bilgileri") + "  " + Bsonuc.Count + " öğrenciye mail olarak gönderildi!";
                    mmMessage.IsSuccess = true;
                    mmMessage.Title = "Mail Gönderim İşlemi Başarılı";
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = Msgtype.Success;
                }
                catch (Exception ex)
                {
                    var message = "'" + SurecAdiB + "'  Başvuru sürecine ait başvuru " + (IsSonucOrAnket ? "sonuçları" : "Anket giriş bilgileri") + "  Öğrencilere mail olarak gönderilirken bir hata oluştu!";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "BasvuruSureci/basvuruSonucMailGonderim", LogType.Hata);
                    mmMessage.Title = "Hata";
                    mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                }
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new
            {
                IsSuccess = mmMessage.IsSuccess,
                Messages = strView
            }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = (RoleNames.BasvuruSureciOgrenciKayit))]
        public ActionResult KazananKayit(int MulakatSonucID, int? KayitDurumID, bool YokODKontrolYap = true, bool? IsSave = null)
        {
            var mmMessage = new MmMessage();
            mmMessage.IsSuccess = true;
            mmMessage.Title = "Başvuru Sonucu Kesin Kayıt İşlemi";
            var btercih = db.MulakatSonuclaris.Where(p => p.MulakatSonucID == MulakatSonucID).First();
            if (new List<int?> { KayitDurumu.KayitOldu, KayitDurumu.OnKayit }.Contains(KayitDurumID) == false) YokODKontrolYap = false;
            var ProgramBilgi = Management.GetOnlineOdemeProgramDetay(btercih.BasvurularTercihleri.UniqueID.ToString(), false, true, YokODKontrolYap);
            bool IsReloadPage = true;

            var kul = db.Kullanicilars.Where(p => p.KullaniciID == ProgramBilgi.KullaniciID).First();

            bool bilimselHazirlik = btercih.BasvurularTercihleri.OgrenimTipKod == OgrenimTipi.TezsizYuksekLisans ? false : (btercih.AlanTipID == AlanTipi.AlanDisi ? true : btercih.BilimselHazirlikVar ?? false);

            var topKayit = db.MulakatSonuclaris.Where(p => p.MulakatSonucID != MulakatSonucID &&
                                                          p.BasvurularTercihleri.ProgramKod == ProgramBilgi.ProgramKod &&
                                                          p.BasvurularTercihleri.OgrenimTipKod == ProgramBilgi.OgrenimTipKod &&
                                                          p.BasvurularTercihleri.AlanTipID == ProgramBilgi.AlanTipID &&
                                                          p.BasvuruSurecID == ProgramBilgi.BasvuruSurecID &&
                                                          p.KayitDurumID.HasValue &&
                                                          p.KayitDurumlari.IsKayitOldu).Count();
            var ogrenciKayitBilgi = KullanicilarBus.StudentControl(ProgramBilgi.TcKimlikNo);
            string OgrenciBilgi = ProgramBilgi.AdSoyad + " Adlı ve " + ProgramBilgi.TcKimlikNo.ToString() + (ProgramBilgi.IsYerliOgrenci ? " Kimlik Nolu öğrenci" : " Pasaport Nolu öğrenci");
            var TumKayitIDs = new List<int> { KayitDurumu.KayitOldu, KayitDurumu.OnKayit };
            //if (btercih.KayitDurumID.HasValue && btercih.KayitDurumlari.IsKayitOldu && KayitDurumID.HasValue && KayitDurumID == KayitDurumu.KayitOldu)
            //{
            //    mmMessage.Messages.Add(OgrenciBilgi + " Seçilen programa zaten kayıtlıdır. Tekrar kayıt edilemez!");
            //    mmMessage.IsSuccess = false;
            //}
            bool IsProgramKotasiDoldu = false;
            if (TumKayitIDs.Contains(KayitDurumID ?? -1) && ProgramBilgi.AlanKota <= topKayit)
            {
                mmMessage.Messages.Add(OgrenciBilgi + " Program Kayıt kotası dolduğundan kayıt edilememiştir!");
                mmMessage.IsSuccess = false;
                IsProgramKotasiDoldu = true;
            }

            if (mmMessage.IsSuccess)
            {


                if (!ogrenciKayitBilgi.Hata)
                {
                    if (ogrenciKayitBilgi.KayitVar)
                    {
                        var programKod =ogrenciKayitBilgi.OgrenciInfo.PROGRAM_ID;
                        var program = db.Programlars.Where(p => p.ProgramKod == programKod).FirstOrDefault();
                        var _enstituKod = program.AnabilimDallari.EnstituKod;
                        if ((_enstituKod == ProgramBilgi.EnstituKod) && KayitDurumID.HasValue && KayitDurumID == KayitDurumu.KayitOldu)
                        {
                            var msg = OgrenciBilgi + " GSIS sistemi '" + program.ProgramAdi + "' programında kaydı gözükmektedir! Kayıt aktarım işlemi yapılamaz!";
                            mmMessage.Messages.Add(msg);
                            mmMessage.IsSuccess = false;
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(msg, "BasvuruSureci/KazananKayit", LogType.Uyarı);
                        }
                        else if ((programKod == ProgramBilgi.ProgramKod) && btercih.KayitDurumID.HasValue && KayitDurumID.HasValue && KayitDurumID == KayitDurumu.KayitOlmadi)
                        {
                            var msg = OgrenciBilgi + " GSIS sistemi " + program.ProgramAdi + " programında kaydı gözükmektedir! Kayıt üstünde herhangi bir işlem yapılamaz!";
                            mmMessage.Messages.Add(msg);
                            mmMessage.IsSuccess = false;
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(msg, "BasvuruSureci/KazananKayit", LogType.Uyarı);
                        }
                    }
                }
                else
                {
                    var msg = OgrenciBilgi + " si için OBS sisteminde kayıt kontrolü başarısız oldu! Lütfen sistem yöneticisine başvurunuz!";
                    mmMessage.Messages.Add(msg);
                    mmMessage.IsSuccess = false;
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(msg, "BasvuruSureci/KazananKayit", LogType.Kritik);
                }
            }
            if (mmMessage.IsSuccess && ProgramBilgi.YokOgrenciKontrolHataVar)
            {
                mmMessage.Messages.Add(ProgramBilgi.AciklamaSelectedLng);
                mmMessage.IsSuccess = false;
            }
            if (!IsProgramKotasiDoldu && mmMessage.IsSuccess)
            {
                var YedekToplamTercih = btercih.BasvuruSurec.Basvurulars.Where(p => p.KullaniciID == ProgramBilgi.KullaniciID).SelectMany(s => s.MulakatSonuclaris).Where(p => p.MulakatSonucTipID == MulakatSonucTipi.Yedek).ToList();
                var IsYedekCokluTercih = YedekToplamTercih.Count > 1;
                if (IsYedekCokluTercih && btercih.BasvurularTercihleri.KayitSiraNo == 2)
                {
                    var DigerTercih = YedekToplamTercih.Where(p => p.BasvuruTercihID != btercih.BasvuruTercihID).First();
                    var BasvuruTercihi = DigerTercih.BasvurularTercihleri;
                    if (BasvuruTercihi.IsSecilenTercih == true && !DigerTercih.KayitDurumID.HasValue)
                    {
                        var Kota = db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == DigerTercih.BasvuruSurecID && p.ProgramKod == BasvuruTercihi.ProgramKod && p.OgrenimTipKod == BasvuruTercihi.OgrenimTipKod).First();
                        var AlanKota = BasvuruTercihi.AlanTipID == AlanTipi.AlanIci ? (Kota.AlanIciKota + (Kota.AlanIciEkKota ?? 0)) : (Kota.AlanDisiKota + (Kota.AlanDisiEkKota ?? 0));

                        var KayitTop = db.MulakatSonuclaris.Where(p =>
                                                           p.BasvurularTercihleri.ProgramKod == BasvuruTercihi.ProgramKod &&
                                                           p.BasvurularTercihleri.OgrenimTipKod == BasvuruTercihi.OgrenimTipKod &&
                                                           p.BasvurularTercihleri.AlanTipID == BasvuruTercihi.AlanTipID &&
                                                           p.BasvuruSurecID == DigerTercih.BasvuruSurecID &&
                                                           p.KayitDurumID.HasValue &&
                                                           p.KayitDurumlari.IsKayitOldu).Count();

                        if (AlanKota > KayitTop)
                        {
                            var DigerTercihProgrami = BasvuruTercihi.Programlar;
                            var msg = OgrenciBilgi + " için yedek program kayıt önceliği tercihinde " + BasvuruTercihi.KayitSiraNo + ". öncelik olarak '" + DigerTercihProgrami.ProgramAdi + "' programına kayıt olmak istemektedir. Bu program için işlem yapılmadan " + ProgramBilgi.ProgramAdi + " programına kayıt işemi yapılamaz!";
                            mmMessage.Messages.Add(msg);
                            mmMessage.IsSuccess = false;
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(msg, "BasvuruSureci/KazananKayit", LogType.Uyarı);
                        }
                    }
                }

            }
            bool dekontBilgisiniAl = false;
            bool dekontIste = false;
            if (mmMessage.IsSuccess)
            {
                if (ProgramBilgi.IsOgrenimUcretiOrKatkiPayi.HasValue && TumKayitIDs.Contains(KayitDurumID ?? -1))
                {
                    dekontBilgisiniAl = true;
                    if (ProgramBilgi.Ucretli && !ProgramBilgi.OdemeListesi.Any(a => a.DonemNo == 1))
                    {
                        mmMessage.Messages.Add(OgrenciBilgi + " <br/>Program Kayıt Ücreti ödediğine herhangi bir bilgiye rastlanmadı. Kesin Kayıt işleminin tamamlanabilmesi için Kayıt ücretinin ödenmesi gerekmektedir.");
                        mmMessage.Messages.Add("Not: Kayıt ücreti Sanal Pos Sistemi üzerinden ödenebilir ya da Banka araclığı ile yapılan ödemeye ait Dekont bilgisi sisteme girilebilir");
                        mmMessage.IsSuccess = false;
                        dekontIste = true;
                    }
                    else if (ProgramBilgi.YokOgrenciKontroluYap && !ProgramBilgi.OdemeListesi.Any(a => a.DonemNo == 1))
                    {
                        OgrenciBilgi += " <br/>Aşağıda bulunan Üniversitede aktif olarak okuduğu gözükmektedir.Kesin Kayıt işleminin yapılabilmesi için kaydını sildirmeli ya da Katkı Payı ücretini yatırmalıdır.";
                        mmMessage.Messages.Add(OgrenciBilgi);
                        foreach (var item in ProgramBilgi.AktifOgrenimListesi)
                            mmMessage.Messages.Add(item);
                        mmMessage.Messages.Add("Not: Katkı payı ücreti Sanal Pos Sistemi üzerinden ödenebilir ya da Banka araclığı ile yapılan ödemeye ait Dekont bilgisi sisteme girilebilir");

                        mmMessage.IsSuccess = false;
                        dekontIste = true;
                    }

                }
                if (KayitDurumID == KayitDurumu.OnKayit && btercih.MulakatSonucTipID == MulakatSonucTipi.Yedek)
                {

                    mmMessage.IsSuccess = true;
                    dekontIste = false;
                    dekontBilgisiniAl = false;
                    if (IsSave != true)
                    {
                        mmMessage.Messages = new List<string>();
                    }

                }
            }

            if (mmMessage.IsSuccess)
            {

                if (ProgramBilgi.IsBelgeYuklemesiVar && (TumKayitIDs.Contains(KayitDurumID ?? -1) && (!btercih.KayitDurumID.HasValue || btercih.KayitDurumlari.IsKayitOldu != true))) // belge yüklemesi isteniyor ve yeni kayıt işlemi yapılıyor ise
                {
                    if (!btercih.Basvurular.BasvurularTercihleris.Any(a => a.IsSecilenTercih == true))
                    {
                        mmMessage.Messages.Add(OgrenciBilgi + "  kayıt olmak istediği tercihi seçmediği için kayıt işlemi yapılamaz.");
                        mmMessage.IsSuccess = false;
                    }
                    else
                    {
                        var SecilenTercih = btercih.Basvurular.BasvurularTercihleris.Where(a => a.IsSecilenTercih == true).First();
                        if (btercih.BasvuruTercihID != SecilenTercih.BasvuruTercihID)
                        {
                            var Prl = SecilenTercih.Programlar;
                            mmMessage.Messages.Add(OgrenciBilgi + "  kayıt olmak istediği tercih '" + Prl.ProgramAdi + "' programıdır. Bu program harici programa kayıt yapılamaz.");
                            mmMessage.IsSuccess = false;
                        }
                    }

                    if (mmMessage.IsSuccess)
                    {
                        if (IsSave == true)
                        {
                            IsReloadPage = false;
                            ProgramBilgi.IsBelgeDialogYuklemeShow = false;
                            if (ProgramBilgi.BelgeKontrolMessages.Any())
                            {
                                mmMessage.Messages.Add(OgrenciBilgi + " nin Kayıt işlemi için aşağıdaki uyarıları kontrol ediniz.");
                                mmMessage.Messages.AddRange(ProgramBilgi.BelgeKontrolMessages);
                                mmMessage.IsSuccess = false;
                            }
                            else ProgramBilgi.IsBelgeDialogYuklemeClose = true;
                        }
                        else
                        {
                            ProgramBilgi.IsBelgeDialogYuklemeShow = true;
                            IsReloadPage = false;
                            mmMessage.IsSuccess = false;

                        }
                    }
                }
            }
            if (mmMessage.IsSuccess)
            {
                string OgrenciNo = "";
                if (KayitDurumID == KayitDurumu.KayitOldu)
                {
                    #region vsKayit
                    DataSet ds = new DataSet();
                    DataTable dt = new DataTable();
                    dt.Columns.Add("AnabilimDali");
                    dt.Columns.Add("EgitimTipi");
                    dt.Columns.Add("Donem");//5 hane
                    dt.Columns.Add("Ad");
                    dt.Columns.Add("Soyad");
                    dt.Columns.Add("AnneAdi");
                    dt.Columns.Add("BabaAdi");
                    dt.Columns.Add("KizlikSoyadi");
                    dt.Columns.Add("TcKimlikNo");
                    dt.Columns.Add("Uyruk");
                    dt.Columns.Add("Cinsiyet");
                    dt.Columns.Add("DogumTarihi");
                    dt.Columns.Add("DogumYeri");
                    dt.Columns.Add("CiltNo");
                    dt.Columns.Add("AileSiraNo");
                    dt.Columns.Add("SiraNo");
                    dt.Columns.Add("Il");
                    dt.Columns.Add("Ilce");
                    dt.Columns.Add("Adres1");
                    dt.Columns.Add("Adres2");
                    dt.Columns.Add("PostaKodu");
                    dt.Columns.Add("AdresIli");//sehir
                    dt.Columns.Add("SabitTel");
                    dt.Columns.Add("CepTel");
                    dt.Columns.Add("E-Posta");
                    dt.Columns.Add("HazirlikDurumu");
                    dt.Columns.Add("AlesTipi");
                    dt.Columns.Add("AlesPuani");
                    dt.Columns.Add("AlanTipi");
                    dt.Columns.Add("OdemeZamani");
                    dt.Columns.Add("OdemeDekontNo");
                    dt.Columns.Add("OdemeTutari");
                    DataRow dr = dt.NewRow();
                    dr["AnabilimDali"] = ProgramBilgi.ProgramKod;
                    dr["EgitimTipi"] = ProgramBilgi.OgrenimTipKod;
                    dr["Donem"] = ProgramBilgi.DonemBaslangicYil.ToString() + ProgramBilgi.DonemID.ToString();//5 hane
                    dr["Ad"] = ProgramBilgi.BasvuruBilgi.Ad.ToUpper();
                    dr["Soyad"] = ProgramBilgi.BasvuruBilgi.Soyad.ToUpper();
                    dr["AnneAdi"] = ProgramBilgi.BasvuruBilgi.AnaAdi.ToUpper();
                    dr["BabaAdi"] = ProgramBilgi.BasvuruBilgi.BabaAdi.ToUpper();
                    dr["KizlikSoyadi"] = "";
                    dr["TcKimlikNo"] = ProgramBilgi.TcKimlikNo.ToString();
                    dr["Uyruk"] = ProgramBilgi.BasvuruBilgi.UyrukKod;
                    dr["Cinsiyet"] = ProgramBilgi.BasvuruBilgi.CinsiyetID;
                    dr["DogumTarihi"] = ProgramBilgi.BasvuruBilgi.DogumTarihi.Value.ToString("yyyy-MM-dd");
                    dr["DogumYeri"] = ProgramBilgi.BasvuruBilgi.DogumYeriKod;
                    dr["CiltNo"] = ProgramBilgi.BasvuruBilgi.CiltNo.HasValue ? ProgramBilgi.BasvuruBilgi.CiltNo.Value.ToString() : "";
                    dr["AileSiraNo"] = ProgramBilgi.BasvuruBilgi.AileNo.HasValue ? ProgramBilgi.BasvuruBilgi.AileNo.Value.ToString() : "";
                    dr["SiraNo"] = ProgramBilgi.BasvuruBilgi.SiraNo.HasValue ? ProgramBilgi.BasvuruBilgi.SiraNo.Value.ToString() : "";

                    if (ProgramBilgi.IsYerliOgrenci)
                    {
                        if (ProgramBilgi.BasvuruBilgi.NufusilIlceKod == 9999)
                        {

                            dr["Il"] = 9999;
                            dr["Ilce"] = 9999;
                        }
                        else
                        {
                            var ilceIdStr = ProgramBilgi.BasvuruBilgi.NufusilIlceKod.Value.ToString();
                            var ilceIdLenght = ilceIdStr.Length;
                            var ilceIdStartNum = ilceIdStr.Substring(0, 1);
                            int? IlID = null;
                            if (ilceIdStr.Length == 3) IlID = Convert.ToInt32(ilceIdStartNum + "00");
                            else
                            {
                                var ilceIdStartNum2 = ilceIdStr.Substring(1, 1);
                                IlID = Convert.ToInt32(ilceIdStartNum + ilceIdStartNum2 + "00");
                            }
                            dr["Il"] = IlID;
                            dr["Ilce"] = ProgramBilgi.BasvuruBilgi.NufusilIlceKod;
                        }
                    }
                    else
                    {
                        dr["Il"] = 9100;
                        dr["Ilce"] = 9100;
                    }
                    if (ProgramBilgi.BasvuruBilgi.EvTel.IsNullOrWhiteSpace() == false)
                    {
                        var evT = ProgramBilgi.BasvuruBilgi.EvTel.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                        if (evT.Trim().Length > 0)
                        {
                            if (evT.Substr(0, 1) == "") evT = evT.Substr(1, evT.Length);
                            if (evT.Length > 11) evT = evT.Substr(evT.Length - 11, evT.Length);
                            ProgramBilgi.BasvuruBilgi.EvTel = evT;
                        }
                        else ProgramBilgi.BasvuruBilgi.EvTel = "";
                    }
                    else ProgramBilgi.BasvuruBilgi.EvTel = "";
                    if (ProgramBilgi.BasvuruBilgi.CepTel.IsNullOrWhiteSpace() == false)
                    {
                        var cpT = ProgramBilgi.BasvuruBilgi.CepTel.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                        if (cpT.Trim().Length > 0)
                        {
                            if (cpT.Substr(0, 1) == "0") cpT = cpT.Substr(1, cpT.Length);
                            if (cpT.Length > 11) cpT = cpT.Substr(cpT.Length - 11, cpT.Length);
                            ProgramBilgi.BasvuruBilgi.CepTel = cpT;
                        }
                        else ProgramBilgi.BasvuruBilgi.CepTel = "";
                    }
                    else ProgramBilgi.BasvuruBilgi.CepTel = "";

                    dr["Adres1"] = (ProgramBilgi.BasvuruBilgi.Adres.Length > 40 ? ProgramBilgi.BasvuruBilgi.Adres.Substring(0, 40).Replace("'", " ") : ProgramBilgi.BasvuruBilgi.Adres.Replace("'", " "));
                    dr["Adres2"] = ProgramBilgi.BasvuruBilgi.Adres2 != null ? (ProgramBilgi.BasvuruBilgi.Adres2.Length > 40 ? ProgramBilgi.BasvuruBilgi.Adres2.Substring(0, 40).Replace("'", " ") : ProgramBilgi.BasvuruBilgi.Adres2.Replace("'", " ")) : "";
                    dr["PostaKodu"] = "0";
                    dr["AdresIli"] = ProgramBilgi.BasvuruBilgi.SehirKod;
                    dr["SabitTel"] = (ProgramBilgi.BasvuruBilgi.EvTel ?? "");
                    dr["CepTel"] = (ProgramBilgi.BasvuruBilgi.CepTel ?? "");
                    dr["E-Posta"] = ProgramBilgi.BasvuruBilgi.EMail;
                    dr["HazirlikDurumu"] = bilimselHazirlik ? "1" : "0";
                    dr["AlesTipi"] = ProgramBilgi.AlesTipID;
                    dr["AlesPuani"] = btercih.AlesNotuOrDosyaNotu.HasValue ? btercih.AlesNotuOrDosyaNotu.Value.ToString().Replace(",", ".") : "0";
                    dr["AlanTipi"] = btercih.AlanTipID;

                    var DekontBilgi = btercih.BasvurularTercihleri.BasvurularTercihleriKayitOdemeleris.Where(p => p.DonemNo == 1 && p.IsOdendi).FirstOrDefault();
                    if (dekontBilgisiniAl)
                    {

                        var tarih = DekontBilgi.DekontTarih.Value.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        string otutar2;
                        if (DekontBilgi.Ucret.HasValue == false)
                        {
                            var prg = db.Programlars.Where(p => p.ProgramKod == ProgramBilgi.ProgramKod).First();
                            if (prg.Ucretli && prg.Ucret.HasValue) { otutar2 = prg.Ucret.Value.ToString("n2").ToDecimal().Value.ToString().Replace(",", "."); }
                            else
                            {
                                otutar2 = btercih.BasvurularTercihleri.ProgramUcret.Value.ToString("n2").ToDecimal().Value.ToString().Replace(",", ".");
                            }
                        }
                        else
                        {
                            otutar2 = DekontBilgi.Ucret.Value.ToString("n2").ToDecimal().Value.ToString().Replace(",", ".");

                        }
                        dr["OdemeZamani"] = tarih;
                        dr["OdemeTutari"] = otutar2;
                        dr["OdemeDekontNo"] = DekontBilgi.DekontNo;
                    }
                    else
                    {
                        dr["OdemeZamani"] = "";
                        dr["OdemeTutari"] = "";
                        dr["OdemeDekontNo"] = "";
                    }
                    dt.Rows.Add(dr);
                    ds.Tables.Add(dt);
                    ds.Tables[0].TableName = "KesinKayit";
                    #endregion
                    StringWriter sw = new StringWriter();
                    ds.WriteXml(sw);

                    var returnVal = Management.gsisKayitAktar(sw.ToString());
                    if (returnVal != "HATA")
                    {
                        kul.YtuOgrencisi = true;
                        kul.ProgramKod = ProgramBilgi.ProgramKod;
                        kul.OgrenciNo = OgrenciNo = returnVal;
                        kul.OgrenimDurumID = OgrenimDurum.HalenOğrenci;
                        kul.OgrenimTipKod = ProgramBilgi.OgrenimTipKod;
                        btercih.KayitDurumID = KayitDurumID;

                        mmMessage.Messages.Add(OgrenciBilgi + "nin Kayıt işlemi tamamlandı ve GSIS  sistemine aktarıldı. <br/>Öğrenci No:" + returnVal);
                        mmMessage.IsSuccess = true;
                        var mulSonucs = db.MulakatSonuclaris.Where(p => p.BasvuruSurecID == btercih.BasvuruSurecID && p.Basvurular.KullaniciID == ProgramBilgi.KullaniciID && p.MulakatSonucID != MulakatSonucID).ToList();
                        foreach (var item in mulSonucs.Where(p => TumKayitIDs.Contains(p.KayitDurumID ?? -1)))
                        {
                            var program = item.BasvurularTercihleri.Programlar;
                            var atip = db.AlanTipleris.Where(p => p.AlanTipID == item.AlanTipID).First();
                            var sonucTip = item.MulakatSonucTipleri;
                            if (KayitDurumID.HasValue && KayitDurumID == KayitDurumu.KayitOldu && (item.MulakatSonucTipID == MulakatSonucTipi.Asil || item.MulakatSonucTipID == MulakatSonucTipi.Yedek))
                            {
                                mmMessage.Messages.Add(OgrenciBilgi + "nin '" + program.ProgramAdi + " (" + atip.AlanTipAdi + ")' Programına da '" + sonucTip.MulakatSonucTipAdi + "' olarak kayıt olduğu gözükmekte.");
                                mmMessage.Messages.Add("'" + program.ProgramAdi + " (" + atip.AlanTipAdi + ")' Programı için kayıt olma seçeneği 'Kayıt olmadı' Şeklinde güncellenmiştir.");
                                mmMessage.IsSuccess = true;
                                item.KayitDurumID = KayitDurumu.KayitOlmadi;
                                db.SaveChanges();
                            }
                        }

                        btercih.IslemYapanIP = UserIdentity.Ip;
                        btercih.IslemYapanID = UserIdentity.Current.Id;
                        btercih.IslemTarihi = DateTime.Now;

                        db.SaveChanges();


                    }
                    else
                    {
                        mmMessage.Messages.Add(OgrenciBilgi + "nin Kayıt işlemi sırasında bir hata oluştu! GSIS sistemi hata döndürdü! <br/> Mesaj:" + returnVal);
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(OgrenciBilgi + "nin Kayıt işlemi sırasında bir hata oluştu! GSIS sistemi hata döndürdü! <br/> Mesaj:" + returnVal + " <br/><br/>" + sw, "BasvuruSureci/KazananKayit", LogType.Kritik);
                        mmMessage.IsSuccess = false;
                    }
                    IsReloadPage = true;
                }
                else
                {

                    btercih.KayitDurumID = KayitDurumID;
                    if (kul.ProgramKod == btercih.BasvurularTercihleri.ProgramKod && kul.OgrenimTipKod == btercih.BasvurularTercihleri.OgrenimTipKod && !TumKayitIDs.Contains(KayitDurumID ?? -1))
                    {
                        kul.YtuOgrencisi = false;
                        kul.ProgramKod = null;
                        kul.OgrenciNo = null;
                        kul.OgrenimDurumID = null;
                        kul.OgrenimTipKod = null;
                    }
                    btercih.IslemTarihi = DateTime.Now;
                    btercih.IslemYapanID = UserIdentity.Current.Id;
                    btercih.IslemYapanIP = UserIdentity.Ip;
                    db.SaveChanges();
                    mmMessage.Messages.Add(OgrenciBilgi + "nin Kayıt bilgisi " + (KayitDurumID == KayitDurumu.OnKayit ? "Ön Kayıt" : "Kayıt Olmadı") + " şeklinde güncellenmiştir.");
                    mmMessage.IsSuccess = true;
                    IsReloadPage = true;
                }
                LogIslemleri.LogEkle("MulakatSonuclari", "Update", btercih.ToJson());
                #region SendMail
                if (KayitDurumID == KayitDurumu.KayitOldu || KayitDurumID == KayitDurumu.OnKayit)
                {
                    var Basvuru = btercih.Basvurular;
                    var EnstituL = Basvuru.BasvuruSurec.Enstituler;
                    var ParamereDegerleri = new List<MailReplaceParameterDto>();
                    var MailSablonTipID = KayitDurumID == KayitDurumu.KayitOldu ? MailSablonTipi.OtoMailKayitOldu : (KayitDurumID == KayitDurumu.OnKayit && ProgramBilgi.IsOgrenimUcretiOrKatkiPayi == false) ? MailSablonTipi.OtoMailOnKayitOldu : MailSablonTipi.OtoMailOnKayitOldu1;
                    var Sablon = db.MailSablonlaris.Where(p => p.MailSablonTipID == MailSablonTipID && p.EnstituKod == EnstituL.EnstituKod).FirstOrDefault();
                    if (Sablon != null)
                    {
                        var EMailList = new List<MailSendList>() { new MailSendList { EMail = Basvuru.EMail.Trim(), ToOrBcc = true } };
                        if (Sablon.GonderilecekEkEpostalar.IsNullOrWhiteSpace() == false)
                            EMailList.AddRange(Sablon.GonderilecekEkEpostalar.Split(',').ToList().Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                        var Parametreler = Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        var Attachments = new List<System.Net.Mail.Attachment>();
                        foreach (var itemSe in Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var FExtension = System.IO.Path.GetExtension(ekTamYol);
                                Attachments.Add(new System.Net.Mail.Attachment(new System.IO.MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(FExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "ApplicationClock", LogType.Uyarı);
                        }
                        if (Parametreler.Any(a => a == "@EnstituAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = EnstituL.EnstituAd });
                        if (Parametreler.Any(a => a == "@WebAdresi"))
                            ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = EnstituL.WebAdresi, IsLink = true });
                        if (Parametreler.Any(a => a == "@AdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = Basvuru.Ad + " " + Basvuru.Soyad });

                        if (Parametreler.Any(a => a == "@ProgramAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = ProgramBilgi.ProgramAdi });
                        if (Parametreler.Any(a => a == "@OgrenciNo"))
                            ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = OgrenciNo });
                        if (Parametreler.Any(a => a == "@OgrenimSeviyesiAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenimSeviyesiAdi", Value = ProgramBilgi.OgrenimTipAdi });

                        var mCOntent = SystemMails.GetSystemMailContent(EnstituL.EnstituAd, Sablon.SablonHtml, Sablon.SablonAdi, ParamereDegerleri);
                        try
                        {
                            //EMailList.Clear();
                            //EMailList.Add(new MailSendList { EMail = "irfansecer@gmail.com", ToOrBcc = true });
                            var snded = MailManager.SendMail(EnstituL.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, EMailList, Attachments);

                            if (snded)
                            {
                                var kModel = new GonderilenMailler();
                                kModel.Tarih = DateTime.Now;
                                kModel.EnstituKod = EnstituL.EnstituKod;
                                kModel.BasvuruID = Basvuru.BasvuruID;
                                kModel.MesajID = null;
                                kModel.IslemTarihi = DateTime.Now;
                                kModel.Konu = Sablon.SablonAdi + " (" + Basvuru.Ad + " " + Basvuru.Soyad + ")";
                                kModel.Aciklama = "";
                                kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                                kModel.IslemYapanID = UserIdentity.Current.Id;
                                kModel.IslemYapanIP = UserIdentity.Ip;
                                kModel.Gonderildi = true;
                                kModel.GonderilenMailKullanicilars = EMailList.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                                kModel.GonderilenMailEkleris = gonderilenMEkleris;
                                db.GonderilenMaillers.Add(kModel);
                                db.SaveChanges();
                            }
                        }
                        catch (Exception ex)
                        {

                            mmMessage.Messages.Add("Mail gönderilirken bir hata oluştu!");
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Kayıt işlemi yapılan öğrenciye mail gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogType.Hata);
                        }

                    }
                }
                #endregion

                if (mmMessage.IsSuccess) ProgramBilgi.IsBelgeDialogYuklemeClose = true;
            }

            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new
            {
                DekontIste = dekontIste,
                ProgramBilgi.IsBelgeDialogYuklemeShow,
                ProgramBilgi.IsBelgeDialogYuklemeClose,
                IsSuccess = mmMessage.IsSuccess,
                IsReloadPage = IsReloadPage,
                Messages = strView
            }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = RoleNames.BasvuruSureciKayit)]

        public ActionResult TopluKayitKapatma(int BasvuruSurecID, int MulakatSonucTipID, string OgrenimTipKods)
        {
            var mMessage = new MmMessage
            {
                IsSuccess = true,
                Title = "Toplu Kayıt Kapatma İşlemi"
            };
            var OgrenimTipKodList = new List<int>();
            if (!OgrenimTipKods.IsNullOrWhiteSpace()) OgrenimTipKodList = OgrenimTipKods.Split(',').Select(s => s.ToInt().Value).ToList();
            else mMessage.Messages.Add("Toplu kayıt kapatma işlemi için en az bir Öğrenim Seviyesi seçilmesi gerekmektedir.");

            if (mMessage.Messages.Count == 0)
            {
                if (MulakatSonucTipID == MulakatSonucTipi.Asil)
                {
                    var qData = db.MulakatSonuclaris.Where(p => p.Basvurular.BasvuruSurecID == BasvuruSurecID &&
                                                                OgrenimTipKodList.Contains(p.BasvurularTercihleri
                                                                    .OgrenimTipKod)
                                                                && p.MulakatSonucTipID == MulakatSonucTipi.Asil
                                                                && p.KayitDurumID == null).ToList();
                    foreach (var item in qData)
                    {
                        item.KayitDurumID = KayitDurumu.KayitOlmadi;
                        item.IslemTarihi = DateTime.Now;
                        item.IslemYapanID = UserIdentity.Current.Id;
                        item.IslemYapanIP = UserIdentity.Ip;
                    }
                }
                else
                {
                    var qgrupPrg = (from s in db.BasvurularTercihleris.Where(p => OgrenimTipKodList.Contains(p.OgrenimTipKod))
                                    where s.Basvurular.BasvuruSurecID == BasvuruSurecID
                                    group new { s.ProgramKod, s.OgrenimTipKod, s.AlanTipID } by new { s.ProgramKod, s.OgrenimTipKod, s.AlanTipID } into g1
                                    select new
                                    {

                                        g1.Key.ProgramKod,
                                        g1.Key.OgrenimTipKod,
                                        g1.Key.AlanTipID
                                    }).ToList();
                    var mulSonucData = db.MulakatSonuclaris
                        .Where(p => p.Basvurular.BasvuruSurecID == BasvuruSurecID).ToList();
                    var sMulSonucData = mulSonucData.Where(p => p.KayitDurumID.HasValue).Select(s => new
                    {
                        s.MulakatSonucID,
                        s.AlanTipID,
                        s.BasvurularTercihleri.OgrenimTipKod,
                        s.BasvurularTercihleri.ProgramKod,
                        s.MulakatSonucTipID,
                        KayitOldu = s.KayitDurumlari.IsKayitOldu
                    }).ToList();
                    foreach (var itemx in qgrupPrg)
                    {
                        var qAsilKOlmayanCount = sMulSonucData.Where(p => p.AlanTipID == itemx.AlanTipID && p.ProgramKod == itemx.ProgramKod && p.OgrenimTipKod == itemx.OgrenimTipKod && p.MulakatSonucTipID == MulakatSonucTipi.Asil && p.KayitOldu == false).Count();
                        var qAsilKOlanYedekCount = sMulSonucData.Where(p => p.AlanTipID == itemx.AlanTipID && p.ProgramKod == itemx.ProgramKod && p.OgrenimTipKod == itemx.OgrenimTipKod && p.MulakatSonucTipID == MulakatSonucTipi.Yedek && p.KayitOldu == true).Count();
                        var kalan = qAsilKOlmayanCount - qAsilKOlanYedekCount;
                        var qData = mulSonucData.Where(p => p.AlanTipID == itemx.AlanTipID && p.BasvurularTercihleri.ProgramKod == itemx.ProgramKod && p.BasvurularTercihleri.OgrenimTipKod == itemx.OgrenimTipKod && p.MulakatSonucTipID == MulakatSonucTipi.Yedek && p.KayitDurumID == null).OrderBy(o => o.SiraNo).Take(kalan).ToList();
                        foreach (var item in qData)
                        {
                            item.KayitDurumID = KayitDurumu.KayitOlmadi;
                            item.IslemTarihi = DateTime.Now;
                            item.IslemYapanID = UserIdentity.Current.Id;
                            item.IslemYapanIP = UserIdentity.Ip;
                        }

                    }
                }
                db.SaveChanges();
                mMessage.Messages.Add("Toplu kayıt kapatma işlemi tamamlandı.");
            }
            else
            {
                mMessage.IsSuccess = false;
            }

            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Information;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);
            return Json(new { IsSuccess = true, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);

        }

        [Authorize(Roles = RoleNames.BasvuruSureciKayit)]
        public ActionResult TopluKayit(int BasvuruSurecID, string ProgramKod, int OgrenimTipKod, int AlanTipID)
        {

            var qData = db.MulakatSonuclaris.Where(p => p.Basvurular.BasvuruSurecID == BasvuruSurecID
                    && p.BasvurularTercihleri.ProgramKod == ProgramKod && p.BasvurularTercihleri.OgrenimTipKod == OgrenimTipKod && p.AlanTipID == AlanTipID && p.MulakatSonucTipID == MulakatSonucTipi.Asil && p.KayitDurumID == null).ToList();
            foreach (var item in qData)
            {
                item.KayitDurumID = KayitDurumu.KayitOlmadi;
                item.IslemTarihi = DateTime.Now;
                item.IslemYapanID = UserIdentity.Current.Id;
                item.IslemYapanIP = UserIdentity.Ip;
            }
            db.SaveChanges();
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", new MmMessage { IsSuccess = true, Messages = { "Toplu Kayıt Kapatma İşlemi Başarılı" }, Title = "Toplu Kayıt Kapatma İşlemi" });
            return Json(new { IsSuccess = true, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);

        }
        [Authorize(Roles = RoleNames.BasvuruSureciKayit)]
        public ActionResult TopluKayitY(int BasvuruSurecID, string ProgramKod, int OgrenimTipKod, int AlanTipID)
        {

            var qAsilKOlmayanCount = db.MulakatSonuclaris.Where(p => p.Basvurular.BasvuruSurecID == BasvuruSurecID && p.AlanTipID == AlanTipID && p.BasvurularTercihleri.ProgramKod == ProgramKod && p.BasvurularTercihleri.OgrenimTipKod == OgrenimTipKod && p.MulakatSonucTipID == MulakatSonucTipi.Asil && p.KayitDurumID.HasValue && p.KayitDurumlari.IsKayitOldu == false).Count();
            var qAsilKOlanYedekCount = db.MulakatSonuclaris.Where(p => p.Basvurular.BasvuruSurecID == BasvuruSurecID && p.AlanTipID == AlanTipID && p.BasvurularTercihleri.ProgramKod == ProgramKod && p.BasvurularTercihleri.OgrenimTipKod == OgrenimTipKod && p.MulakatSonucTipID == MulakatSonucTipi.Yedek && p.KayitDurumID.HasValue && p.KayitDurumlari.IsKayitOldu == true).Count();
            var kalan = qAsilKOlmayanCount - qAsilKOlanYedekCount;
            var qData = db.MulakatSonuclaris.Where(p => p.Basvurular.BasvuruSurecID == BasvuruSurecID && p.AlanTipID == AlanTipID && p.BasvurularTercihleri.ProgramKod == ProgramKod && p.BasvurularTercihleri.OgrenimTipKod == OgrenimTipKod && p.MulakatSonucTipID == MulakatSonucTipi.Yedek && p.KayitDurumID == null).OrderBy(o => o.SiraNo).Take(kalan).ToList();
            foreach (var item in qData)
            {
                item.KayitDurumID = KayitDurumu.KayitOlmadi;
                item.IslemTarihi = DateTime.Now;
                item.IslemYapanID = UserIdentity.Current.Id;
                item.IslemYapanIP = UserIdentity.Ip;
            }


            db.SaveChanges();
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", new MmMessage { IsSuccess = true, Messages = { "Toplu Kayıt Kapatma İşlemi Başarılı" }, Title = "Toplu Kayıt Kapatma İşlemi" });
            return Json(new { IsSuccess = true, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);

        }
        [Authorize(Roles = RoleNames.BasvuruSureciKayit)]

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }


        public List<CmbStringDto> GetImagesPath(String folderName)
        {

            DirectoryInfo Folder;
            FileInfo[] Images;
            folderName = Server.MapPath(folderName);
            Folder = new DirectoryInfo(folderName);
            Images = Folder.GetFiles();
            List<CmbStringDto> imagesList = new List<CmbStringDto>();

            for (int i = 0; i < Images.Length; i++)
            {

                imagesList.Add(new CmbStringDto { Caption = String.Format(@"{0}/{1}", folderName, Images[i].Name), Value = Images[i].Name });
                // Console.WriteLine(String.Format(@"{0}/{1}", folderName, Images[i].Name));
            }


            return imagesList;
        }


        public ActionResult GetImages(int id)
        {

            string alinacakYol = "~/Images/KullaniciResimleri";
            var Images = GetImagesPath(alinacakYol);
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var bs = db.BasvuruSurecs.First(p => p.BasvuruSurecID == id);
                var kayitOlanlar = db.MulakatSonuclaris.Where(p => p.BasvuruSurecID == id && p.KayitDurumID.HasValue && p.KayitDurumlari.IsKayitOldu == true).Select(s => new { s.Basvurular.Kullanicilar.TcKimlikNo, s.BasvurularTercihleri.Basvurular.ResimAdi }).Distinct().ToList();

                using (ZipFile zipFile = new ZipFile())
                {
                    var Eklenenler = new List<string>();
                    foreach (var item in kayitOlanlar)
                    {
                        var Resim = Images.FirstOrDefault(p => p.Value == item.ResimAdi);
                        if (Resim != null)
                        {

                            string pictureName = Resim.Caption;
                            using (MemoryStream tempstream = new MemoryStream())
                            {
                                Image userImage = Image.FromFile(pictureName);
                                userImage.Save(tempstream, ImageFormat.Jpeg);

                                tempstream.Seek(0, SeekOrigin.Begin);
                                byte[] imageData = new byte[tempstream.Length];
                                tempstream.Read(imageData, 0, imageData.Length);
                                var fileName =  item.TcKimlikNo + ".jpeg";
                                if (!Eklenenler.Contains(fileName)) zipFile.AddEntry(fileName, imageData);
                                Eklenenler.Add(fileName);
                            }
                        }
                    }
                    var cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = bs.Enstituler.EnstituKisaAd + "_" + bs.BitisTarihi.ToString().Replace(".", "_") + "_LisansustuKullaniciResimleri.zip",
                        Inline = false,
                    };
                    Response.AppendHeader("Content-Disposition", cd.ToString());
                    var memStream = new MemoryStream();
                    zipFile.Save(memStream);
                    memStream.Position = 0; // Else it will try to read starting at the end
                    return File(memStream, "application/zip", cd.FileName);

                }

            }
        }


        public ActionResult GetProgramKotalari(int id)
        {
            var Surec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == id).First();
            var KullaniciProgramKods = UserBus.GetUserProgramKods(UserIdentity.Current.Id, Surec.EnstituKod);
            var vW = db.vW_ProgramBasvuruSonucSayisal.Where(p => p.BasvuruSurecID == id && KullaniciProgramKods.Contains(p.ProgramKod)).Select(s =>
                new
                {
                    s.OgrenimTipAdi,
                    s.AnabilimDaliAdi,
                    s.ProgramKod,
                    s.ProgramAdi,
                    s.AlanIciKota,
                    s.AlanDisiKota,
                    AlanIciKalanKota = s.AlanIciKota - (s.AIKayitCount ?? 0),
                    AlanDisiKalanKota = s.AlanDisiKota - (s.ADKayitCount ?? 0),


                }).ToList();
            var gv = new System.Web.UI.WebControls.GridView();
            gv.DataSource = vW;
            gv.DataBind();
            Response.ContentType = "application/ms-excel";
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
            StringWriter sw = new StringWriter();
            HtmlTextWriter htw = new HtmlTextWriter(sw);
            gv.RenderControl(htw);
            return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "KalanKontenjanBilgileri_" + DateTime.Now.ToString("dd.MM.yyyy") + ".xls");

        }




    }
}
