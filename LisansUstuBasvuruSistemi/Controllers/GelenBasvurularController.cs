using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class GelenBasvurularController : Controller
    {

        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();

        [Authorize(Roles = RoleNames.GelenBasvurular)]
        public ActionResult Index(string ekd, int? belgeDetailBasvuruId = null)
        {
            var model = new FmBasvurularDto
            {
                PageSize = 10,
                Expand = false,
                BasvuruSurecID = Management.getAktifBasvuruSurecID(EnstituBus.GetSelectedEnstitu(ekd), BasvuruSurecTipi.LisansustuBasvuru)
            };

            model.Expand = model.BasvuruSurecID.HasValue;
            return Index(model, ekd, null, false, belgeDetailBasvuruId);
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.GelenBasvurular)]
        public ActionResult Index(FmBasvurularDto model, string ekd, List<string> programKod = null, bool export = false, int? belgeDetailBasvuruId = null)
        {

            var nowDate = DateTime.Now;
            programKod = programKod ?? new List<string>();
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            var q = from s in _entities.Basvurulars
                    join en in _entities.Enstitulers on new { s.BasvuruSurec.EnstituKod } equals new { en.EnstituKod }
                    join bs in _entities.BasvuruSurecs.Where(p => p.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) on s.BasvuruSurecID equals bs.BasvuruSurecID
                    join d in _entities.Donemlers on new { bs.DonemID } equals new { d.DonemID }
                    join ktip in _entities.KullaniciTipleris on new { s.Kullanicilar.KullaniciTipID } equals new { ktip.KullaniciTipID }
                    join bdrm in _entities.BasvuruDurumlaris on new { s.BasvuruDurumID } equals new { bdrm.BasvuruDurumID }

                    where en.EnstituKisaAd.Contains(ekd)
                    select new
                    {
                        s.KullaniciID,
                        s.BasvuruSurecID,
                        s.BasvuruID,
                        en.EnstituKod,
                        EnstituAdi = en.EnstituAd,
                        s.UyrukKod,
                        s.CinsiyetID,
                        s.BasvuruDurumAciklamasi,
                        BasvuruSurecAdi = bs.BaslangicYil + "/" + bs.BitisYil + " " + d.DonemAdi,
                        BasTar = bs.BaslangicTarihi,
                        BitTar = bs.BitisTarihi,
                        s.Kullanicilar.ResimAdi,
                        s.TcKimlikNo,
                        AdSoyad = s.Ad + " " + s.Soyad,
                        s.Ad,
                        s.Soyad,
                        s.DogumTarihi,
                        s.Kullanicilar.KullaniciTipID,
                        ktip.KullaniciTipAdi,
                        s.LOgrenimDurumID,
                        s.EMail,
                        Telefon = s.CepTel ?? s.EvTel ?? s.IsTel,
                        TercihSayisi = s.BasvurularTercihleris.Count,
                        s.BasvuruDurumID,
                        bdrm.BasvuruDurumAdi,
                        DurumClassName = bdrm.ClassName,
                        DurumColor = bdrm.Color,
                        s.BasvuruTarihi,
                        BasvurularTercihleris = s.BasvurularTercihleris.Select(s2 => new { s2.OgrenimTipKod, s2.ProgramKod }),
                        BasvurularSinavBilgis = s.BasvurularSinavBilgis.Select(s2 => new { s2.SinavTipKod, s2.IsTaahhutVar }),
                        LNotSistemi = s.LNotSistemID,
                        s.LMezuniyetNotu,
                        s.LMezuniyetNotu100LukSistem,
                        MulakatSonucTipIDs = s.MulakatSonuclaris.Select(s2 => s2.MulakatSonucTipID),
                        KayitliTercihVar = s.BasvurularTercihleris.Any(a => s.BasvuruID == a.BasvuruID && s.BasvuruDurumID == BasvuruDurumu.Onaylandı && s.MulakatSonuclaris.Any(a2 => a2.KayitDurumID.HasValue && a2.KayitDurumlari.IsKayitOldu)),
                        IsNotDuzelt = (s.BasvuruSurec.AGNOGirisBaslangicTarihi.HasValue && s.LUniversiteID == Management.UniversiteYtuKod && (s.BasvuruSurec.AGNOGirisBaslangicTarihi.Value <= nowDate && s.BasvuruSurec.AGNOGirisBitisTarihi.Value >= nowDate && s.BasvurularTercihleris.Any(a => a.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans))),
                    };
            var q2 = q;
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.BasvuruSurecID.HasValue) q = q.Where(p => p.BasvuruSurecID == model.BasvuruSurecID.Value);
            if (model.LOgrenimDurumID.HasValue) q = q.Where(p => p.LOgrenimDurumID == model.LOgrenimDurumID.Value);
            if (programKod.Count > 0) q = q.Where(p => p.BasvurularTercihleris.Any(a => programKod.Contains(a.ProgramKod)));
            if (model.BasvuruDurumID.HasValue)
            {
                if (model.BasvuruDurumID.Value == BasvuruDurumu.Gonderildi) q = q.Where(p => p.KayitliTercihVar);
                else q = q.Where(p => p.BasvuruDurumID == model.BasvuruDurumID);
            }
            if (model.MulakatSonucTipID.HasValue) q = q.Where(p => p.MulakatSonucTipIDs.Any(a => a == model.MulakatSonucTipID.Value));
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.BasvurularTercihleris.Any(a => a.OgrenimTipKod == model.OgrenimTipKod));
            if (model.KullaniciTipID.HasValue) q = q.Where(p => p.KullaniciTipID == model.KullaniciTipID);
            if (model.CinsiyetID.HasValue) q = q.Where(p => p.CinsiyetID == model.CinsiyetID);
            if (!model.AdSoyad.IsNullOrWhiteSpace()) q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.TcKimlikNo == model.AdSoyad);
            if (model.SinavTipKod.HasValue && model.IsTaahhutVar.HasValue) q = q.Where(p => p.BasvurularSinavBilgis.Any(a => a.SinavTipKod == model.SinavTipKod && a.IsTaahhutVar == (model.IsTaahhutVar == false ? null : model.IsTaahhutVar)));
            else if (model.SinavTipKod.HasValue) q = q.Where(p => p.BasvurularSinavBilgis.Any(a => a.SinavTipKod == model.SinavTipKod));
            else if (model.IsTaahhutVar.HasValue) q = q.Where(p => p.BasvurularSinavBilgis.Any(a => a.IsTaahhutVar == (model.IsTaahhutVar == false ? null : model.IsTaahhutVar)));
            if (model.UyrukKod.HasValue) q = q.Where(p => p.UyrukKod == model.UyrukKod);
            bool isFiltered = q != q2;
            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi();
            var kayitCountDurum = _entities.BasvuruDurumlaris.Where(p => p.BasvuruDurumID == BasvuruDurumu.Onaylandı).Select(s => new { s.BasvuruDurumID, s.BasvuruDurumAdi, s.ClassName, s.Color }).FirstOrDefault();
            if (kayitCountDurum != null)
            {
                indexModel.ListB.Add(new mxRowModel { Key = "Toplam", ClassName = "", Color = kayitCountDurum.Color, Toplam = model.RowCount });
            }

            indexModel.Toplam = model.RowCount;
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi); 
            var qdata = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s =>
                new FrBasvurularDto
                {
                    KullaniciID = s.KullaniciID,
                    BasvuruSurecID = s.BasvuruSurecID,
                    BasvuruID = s.BasvuruID,
                    EnstituKod = s.EnstituKod,
                    EnstituAdi = s.EnstituAdi,
                    BasvuruDurumAciklamasi = s.BasvuruDurumAciklamasi,
                    BasvuruSurecAdi = s.BasvuruSurecAdi,
                    BasTar = s.BasTar,
                    BitTar = s.BitTar,
                    Ad = s.Ad,
                    Soyad = s.Soyad,
                    ResimAdi = s.ResimAdi,
                    TcKimlikNo = s.TcKimlikNo,
                    AdSoyad = s.AdSoyad,
                    EMail = s.EMail,
                    CepTel = s.Telefon,
                    KullaniciTipID = s.KullaniciTipID,
                    KullaniciTipAdi = s.KullaniciTipAdi,
                    TercihSayisi = s.TercihSayisi,
                    BasvuruDurumID = s.BasvuruDurumID,
                    BasvuruDurumAdi = s.BasvuruDurumAdi,
                    DurumClassName = s.DurumClassName,
                    DurumColor = s.DurumColor,
                    BasvuruTarihi = s.BasvuruTarihi,
                    IsNotDuzelt = s.IsNotDuzelt,
                    KayitliTercihVar = s.KayitliTercihVar
                }).ToList();
            model.Data = qdata;



            if (export && model.RowCount > 0)
            {
                GridView gv = new GridView();
                var basvuruIDs = q.Select(s => s.BasvuruID).ToList();
                var qx = (from s in _entities.Basvurulars.Where(p => basvuruIDs.Contains(p.BasvuruID))
                          join un in _entities.Universitelers on s.LUniversiteID equals un.UniversiteID into def
                          from defUn in def.DefaultIfEmpty()
                          join lb in _entities.OgrenciBolumleris on s.LOgrenciBolumID equals lb.OgrenciBolumID into deflb
                          from lOb in deflb.DefaultIfEmpty()
                          join lo in _entities.OgrenimDurumlaris on s.LOgrenimDurumID equals lo.OgrenimDurumID into deflod
                          from lOd in deflod.DefaultIfEmpty()
                          let os = (from sq in _entities.BasvurularTercihleris.Where(p => p.MulakatSonuclaris.Any(a => a.KayitDurumID == KayitDurumu.KayitOldu) && p.BasvuruID == s.BasvuruID)
                                    join ot in _entities.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod) on sq.OgrenimTipKod equals ot.OgrenimTipKod
                                    select new { sq.OgrenimTipKod, ot.OgrenimTipAdi }).FirstOrDefault()
                          select new
                          {
                              s.TcKimlikNo,
                              s.Ad,
                              s.Soyad,
                              s.EMail,
                              Telefon = s.CepTel ?? s.EvTel ?? s.IsTel,
                              s.DogumTarihi,
                              UniversiteAdi = defUn.Ad,
                              FakulteAdi = s.LFakulteAdi,
                              lOb.BolumAdi,
                              NotSistyemi = s.LNotSistemID,
                              MezuniyetNotu = s.LMezuniyetNotu,
                              MezuniyetNotu100LukSistem = s.LMezuniyetNotu100LukSistem,
                              KayitOlduguOgrenimSeviyesi = os != null ? os.OgrenimTipAdi : ""
                          }).ToList();
                gv.DataSource = qx;
                gv.DataBind();
                StringWriter sw = new StringWriter();
                HtmlTextWriter htw = new HtmlTextWriter(sw);
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_BasvuruListesi_" + DateTime.Now.ToString("dd.MM.yyyy") + ".xls");
            }



            ViewBag.IndexModel = indexModel;
            ViewBag.BasvuruSurecID = new SelectList(Management.getbasvuruSurecleri(enstituKod, BasvuruSurecTipi.LisansustuBasvuru, true), "Value", "Caption", model.BasvuruSurecID);
            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipleri(enstituKod, true, true), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.BasvuruDurumID = new SelectList(Management.cmbBasvuruDurumListe(true, true), "Value", "Caption", model.BasvuruDurumID);
            ViewBag.MulakatSonucTipID = new SelectList(Management.cmbMulakatSonucTip(true), "Value", "Caption", model.MulakatSonucTipID);
            ViewBag.ProgramKod = new SelectList(Management.cmbGetAktifProgramlar(enstituKod, false), "Value", "Caption", model.ProgramKod);
            ViewBag.LOgrenimDurumID = new SelectList(Management.cmbAktifOgrenimDurumu2(true, IsBasvurudaGozuksun: true), "Value", "Caption", model.LOgrenimDurumID);
            ViewBag.SinavTipKod = new SelectList(Management.cmbGetBSAktifSinavlar(enstituKod, new List<int> { SinavTipGrup.DilSinavlari, SinavTipGrup.Tomer, SinavTipGrup.Ales_Gree }, true), "Value", "Caption", model.SinavTipKod);
            ViewBag.KullaniciTipID = new SelectList(KullanicilarBus.GetCmbKullaniciTipleriOgrenciler(true), "Value", "Caption", model.KullaniciTipID);
            ViewBag.CinsiyetID = new SelectList(Management.cmbCinsiyetler(true), "Value", "Caption", model.CinsiyetID);
            ViewBag.IsTaahhutVar = new SelectList(Management.cmbSinavBelgeTaahhut(true), "Value", "Caption", model.IsTaahhutVar);
            ViewBag.UyrukKod = new SelectList(Management.cmbUyruk(true), "Value", "Caption", model.UyrukKod);
            if (isFiltered)
            {
                ViewBag.kIds = q.Select(s => s.KullaniciID).ToList();
            }
            else ViewBag.kIds = new List<int>();
            ViewBag.SelectedPrograms = programKod;
            return View(model);
        }

        [Authorize(Roles = RoleNames.GelenBasvurularSil)]
        public ActionResult Sil(int id)
        {


            var mmMessage = Management.getBasvuruSilKontrol(id, BasvuruSurecTipi.LisansustuBasvuru);
            if (mmMessage.IsSuccess)
            {
                var kayit = _entities.Basvurulars.FirstOrDefault(p => p.BasvuruID == id);
                try
                {
                    mmMessage.Title = "Uyarı";
                    _entities.Basvurulars.Remove(kayit);
                    _entities.SaveChanges();
                    mmMessage.Messages.Add(kayit.Ad + " " + kayit.Soyad + " isimli başvuru sahibine ait başvuru silindi.");
                    mmMessage.MessageType = Msgtype.Success;
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(kayit.Ad + " " + kayit.Soyad + " isimli başvuru sahibine ait başvuru silinemedi.");
                    mmMessage.Title = "Hata";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), "GelenBasvurular/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);

            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = RoleNames.GelenBasvurularKayit)]
        public ActionResult TaslagaCevir(int id)
        {


            var mmMessage = new MmMessage
            {
                Title = "Başvuruyu Taslağa Çevirme İşlemi",
                IsSuccess = true
            };
            var basvuru = _entities.Basvurulars.First(p => p.BasvuruID == id);
            if (basvuru.BasvuruDurumID == BasvuruDurumu.Onaylandı)
            {
                var kayit = _entities.Basvurulars.FirstOrDefault(p => p.BasvuruID == id);
                var adSoyad = kayit.Kullanicilar.Ad + " " + kayit.Kullanicilar.Soyad;
                var tarih = kayit.BasvuruTarihi.ToString();
                try
                {
                    basvuru.BasvuruDurumID = BasvuruDurumu.Taslak;
                    basvuru.IslemTarihi = DateTime.Now;
                    basvuru.IslemYapanID = UserIdentity.Current.Id;
                    basvuru.IslemYapanIP = UserIdentity.Ip;
                    _entities.SaveChanges();
                    mmMessage.Messages.Add(adSoyad + " Öğrencisine ait başvuru taslak durumuna çevrildi");
                    mmMessage.MessageType = Msgtype.Success;
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(adSoyad + " Öğrencisine ait başvuru taslak durumuna çevrilemedi. Hata:" + ex.ToExceptionMessage());
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), "GelenBasvurular/TaslagaCevir<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Başvuru Taslağa Çevrilemedi! Sadece Onaylanan Başvurular Taslağa Çevrilebilir.");
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);

            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
