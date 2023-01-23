using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class GelenBasvurularController : Controller
    {

        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();

        [Authorize(Roles = RoleNames.GelenBasvurular)]
        public ActionResult Index(string EKD, int? BelgeDetailBasvuruID = null)
        {
            var model = new fmBasvurular() { PageSize = 10, Expand = false };

            model.BasvuruSurecID = Management.getAktifBasvuruSurecID(Management.getSelectedEnstitu(EKD), BasvuruSurecTipi.LisansustuBasvuru);
            model.Expand = model.BasvuruSurecID.HasValue;
            return Index(model, EKD, null, false, BelgeDetailBasvuruID);
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.GelenBasvurular)]
        public ActionResult Index(fmBasvurular model, string EKD, List<string> ProgramKod = null, bool export = false, int? BelgeDetailBasvuruID = null)
        {

            var nowDate = DateTime.Now;
            ProgramKod = ProgramKod ?? new List<string>();
            var _EnstituKod = Management.getSelectedEnstitu(EKD);

            var q = from s in db.Basvurulars
                    join en in db.Enstitulers on new { s.BasvuruSurec.EnstituKod } equals new { en.EnstituKod }
                    join bs in db.BasvuruSurecs.Where(p => p.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) on s.BasvuruSurecID equals bs.BasvuruSurecID
                    join d in db.Donemlers on new { bs.DonemID } equals new { d.DonemID }
                    join ktip in db.KullaniciTipleris on new { s.Kullanicilar.KullaniciTipID } equals new { ktip.KullaniciTipID }
                    join bdrm in db.BasvuruDurumlaris on new { s.BasvuruDurumID } equals new { bdrm.BasvuruDurumID }

                    where en.EnstituKisaAd.Contains(EKD)
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
                        TcPasaPortNo = s.TcKimlikNo ?? s.PasaportNo,
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
                        BasvurularTercihleris = s.BasvurularTercihleris.Select(s => new { s.OgrenimTipKod, s.ProgramKod }),
                        BasvurularSinavBilgis = s.BasvurularSinavBilgis.Select(s => new { s.SinavTipKod, s.IsTaahhutVar }),
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
            if (ProgramKod.Count > 0) q = q.Where(p => p.BasvurularTercihleris.Any(a => ProgramKod.Contains(a.ProgramKod)));
            if (model.BasvuruDurumID.HasValue)
            {
                if (model.BasvuruDurumID.Value == BasvuruDurumu.Gonderildi) q = q.Where(p => p.KayitliTercihVar);
                else q = q.Where(p => p.BasvuruDurumID == model.BasvuruDurumID);
            }
            if (model.MulakatSonucTipID.HasValue) q = q.Where(p => p.MulakatSonucTipIDs.Any(a => a == model.MulakatSonucTipID.Value));
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.BasvurularTercihleris.Any(a => a.OgrenimTipKod == model.OgrenimTipKod));
            if (model.KullaniciTipID.HasValue) q = q.Where(p => p.KullaniciTipID == model.KullaniciTipID);
            if (model.CinsiyetID.HasValue) q = q.Where(p => p.CinsiyetID == model.CinsiyetID);
            if (!model.AdSoyad.IsNullOrWhiteSpace()) q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.TcPasaPortNo == model.AdSoyad);
            if (model.SinavTipKod.HasValue && model.IsTaahhutVar.HasValue) q = q.Where(p => p.BasvurularSinavBilgis.Any(a => a.SinavTipKod == model.SinavTipKod && a.IsTaahhutVar == (model.IsTaahhutVar == false ? null : model.IsTaahhutVar)));
            else if (model.SinavTipKod.HasValue) q = q.Where(p => p.BasvurularSinavBilgis.Any(a => a.SinavTipKod == model.SinavTipKod));
            else if (model.IsTaahhutVar.HasValue) q = q.Where(p => p.BasvurularSinavBilgis.Any(a => a.IsTaahhutVar == (model.IsTaahhutVar == false ? null : model.IsTaahhutVar)));
            if (model.UyrukKod.HasValue) q = q.Where(p => p.UyrukKod == model.UyrukKod);
            bool isFiltered = false;
            if (q != q2)
                isFiltered = true;

            model.RowCount = q.Count();
            var IndexModel = new MIndexBilgi();
            var KayitCountDurum = db.BasvuruDurumlaris.Where(p => p.BasvuruDurumID == BasvuruDurumu.Onaylandı).Select(s => new { s.BasvuruDurumID, s.BasvuruDurumAdi, s.ClassName, s.Color }).FirstOrDefault();
            if (KayitCountDurum != null)
            {
                IndexModel.ListB.Add(new mxRowModel { Key = "Toplam", ClassName = "", Color = KayitCountDurum.Color, Toplam = model.RowCount });
            }

            IndexModel.Toplam = model.RowCount;
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.BasvuruTarihi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;

            var qdata = q.Skip(PS.StartRowIndex).Take(model.PageSize).Select(s =>
            new frBasvurular
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
                TcPasaPortNo = s.TcPasaPortNo,
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
                var BasvuruIDs = q.Select(s => s.BasvuruID).ToList();
                var qx = (from s in db.Basvurulars.Where(p => BasvuruIDs.Contains(p.BasvuruID))
                          join un in db.Universitelers on s.LUniversiteID equals un.UniversiteID into def
                          from defUn in def.DefaultIfEmpty()
                          join lb in db.OgrenciBolumleris on s.LOgrenciBolumID equals lb.OgrenciBolumID into deflb
                          from lOb in deflb.DefaultIfEmpty()
                          join lo in db.OgrenimDurumlaris on s.LOgrenimDurumID equals lo.OgrenimDurumID into deflod
                          from lOd in deflod.DefaultIfEmpty()
                          let os = (from sq in db.BasvurularTercihleris.Where(p => p.MulakatSonuclaris.Any(a => a.KayitDurumID == KayitDurumu.KayitOldu) && p.BasvuruID == s.BasvuruID)
                                    join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == _EnstituKod) on sq.OgrenimTipKod equals ot.OgrenimTipKod
                                    select new { sq.OgrenimTipKod, ot.OgrenimTipAdi }).FirstOrDefault()
                          select new
                          {
                              TcPasaPortNo = s.TcKimlikNo ?? s.PasaportNo,
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



            ViewBag.IndexModel = IndexModel;
            ViewBag.BasvuruSurecID = new SelectList(Management.getbasvuruSurecleri(_EnstituKod, BasvuruSurecTipi.LisansustuBasvuru, true), "Value", "Caption", model.BasvuruSurecID);
            ViewBag.OgrenimTipKod = new SelectList(Management.cmbAktifOgrenimTipleri(_EnstituKod, true, true), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.BasvuruDurumID = new SelectList(Management.cmbBasvuruDurumListe(true, true), "Value", "Caption", model.BasvuruDurumID);
            ViewBag.MulakatSonucTipID = new SelectList(Management.cmbMulakatSonucTip(true), "Value", "Caption", model.MulakatSonucTipID);
            ViewBag.ProgramKod = new SelectList(Management.cmbGetAktifProgramlar(_EnstituKod, false), "Value", "Caption", model.ProgramKod);
            ViewBag.LOgrenimDurumID = new SelectList(Management.cmbAktifOgrenimDurumu2(true, IsBasvurudaGozuksun: true), "Value", "Caption", model.LOgrenimDurumID);
            ViewBag.SinavTipKod = new SelectList(Management.cmbGetBSAktifSinavlar(_EnstituKod, new List<int> { SinavTipGrup.DilSinavlari, SinavTipGrup.Tomer, SinavTipGrup.Ales_Gree }, true), "Value", "Caption", model.SinavTipKod);
            ViewBag.KullaniciTipID = new SelectList(Management.cmbKullaniciTipleriOgrenciler(true), "Value", "Caption", model.KullaniciTipID);
            ViewBag.CinsiyetID = new SelectList(Management.cmbCinsiyetler(true), "Value", "Caption", model.CinsiyetID);
            ViewBag.IsTaahhutVar = new SelectList(Management.cmbSinavBelgeTaahhut(true), "Value", "Caption", model.IsTaahhutVar);
            ViewBag.UyrukKod = new SelectList(Management.cmbUyruk(true), "Value", "Caption", model.UyrukKod);
            if (isFiltered)
            {
                ViewBag.kIds = q.Select(s => s.KullaniciID).ToList();
            }
            else ViewBag.kIds = new List<int>();
            ViewBag.SelectedPrograms = ProgramKod;
            return View(model);
        }

        [Authorize(Roles = RoleNames.GelenBasvurularSil)]
        public ActionResult Sil(int id)
        {


            var mmMessage = Management.getBasvuruSilKontrol(id, BasvuruSurecTipi.LisansustuBasvuru);
            if (mmMessage.IsSuccess)
            {
                var kayit = db.Basvurulars.Where(p => p.BasvuruID == id).FirstOrDefault();

                try
                {
                    mmMessage.Title = "Uyarı";
                    db.Basvurulars.Remove(kayit);
                    db.SaveChanges();
                    mmMessage.Messages.Add(kayit.Ad + " " + kayit.Soyad + " isimli başvuru sahibine ait başvuru silindi.");
                    mmMessage.MessageType = Msgtype.Success;
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(kayit.Ad + " " + kayit.Soyad + " isimli başvuru sahibine ait başvuru silinemedi.");
                    mmMessage.Title = "Hata";
                    Management.SistemBilgisiKaydet(ex.ToExceptionMessage(), "GelenBasvurular/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }
            }
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);

            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = RoleNames.GelenBasvurularKayit)]
        public ActionResult TaslagaCevir(int id)
        {


            var mmMessage = new MmMessage();
            mmMessage.Title = "Başvuruyu Taslağa Çevirme İşlemi";
            mmMessage.IsSuccess = true;
            var basvuru = db.Basvurulars.Where(p => p.BasvuruID == id).First();
            if (basvuru.BasvuruDurumID == BasvuruDurumu.Onaylandı)
            {
                var kayit = db.Basvurulars.Where(p => p.BasvuruID == id).FirstOrDefault();
                var adSoyad = kayit.Kullanicilar.Ad + " " + kayit.Kullanicilar.Soyad;
                var tarih = kayit.BasvuruTarihi.ToString();
                try
                {
                    basvuru.BasvuruDurumID = BasvuruDurumu.Taslak;
                    basvuru.IslemTarihi = DateTime.Now;
                    basvuru.IslemYapanID = UserIdentity.Current.Id;
                    basvuru.IslemYapanIP = UserIdentity.Ip;
                    db.SaveChanges();
                    mmMessage.Messages.Add(adSoyad + " Öğrencisine ait başvuru taslak durumuna çevrildi");
                    mmMessage.MessageType = Msgtype.Success;
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(adSoyad + " Öğrencisine ait başvuru taslak durumuna çevrilemedi. Hata:" + ex.ToExceptionMessage());
                    Management.SistemBilgisiKaydet(ex.ToExceptionMessage(), "GelenBasvurular/TaslagaCevir<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }
            }
            else
            {
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Başvuru Taslağa Çevrilemedi! Sadece Onaylanan Başvurular Taslağa Çevrilebilir.");
            }
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);

            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
