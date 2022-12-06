using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Duyurular)]
    public class DuyurularController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string EKD)
        {
            return Index(new fmDuyurular() { PageSize = 15 }, EKD);
        }
        [HttpPost]
        public ActionResult Index(fmDuyurular model, string EKD)
        {



            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in db.Duyurulars
                    join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    join ens in db.Enstitulers on new { s.EnstituKod } equals new { ens.EnstituKod }
                    // where ens.Enstituler.EnstituKisaAd.Contains(EKD)
                    where EnstKods.Contains(s.EnstituKod)
                    select new
                    {

                        s.EnstituKod,
                        ens.EnstituAd,
                        s.DuyuruID,
                        s.Tarih,
                        s.Baslik,
                        s.Aciklama,
                        s.AciklamaHtml,
                        DuyuruYapan = k.Ad + " " + k.Soyad,
                        s.IslemYapanIP,
                        EkSayisi = s.DuyuruEkleris.Count,
                        Ekler = s.DuyuruEkleris,
                        s.IsAktif,
                        s.AnaSayfadaGozuksun,
                        s.AnaSayfaPopupAc,
                        s.MezuniyetBasvuruPopupAc,
                        s.BasvuruPopupAc,
                        s.TIBasvuruPopupAc,
                        s.TDOBasvuruPopupAc,
                        s.TalepYaparkenPopupAc,
                        s.YayinSonTarih,
                    };

            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.Baslik.IsNullOrWhiteSpace()) q = q.Where(p => p.Baslik.Contains(model.Baslik));
            if (!model.Aciklama.IsNullOrWhiteSpace()) q = q.Where(p => p.Aciklama.Contains(model.Aciklama));
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (model.Tarih.HasValue)
            {
                var trih = model.Tarih.Value.TodateToShortDate();
                q = q.Where(p => p.Tarih == trih);

            }
            model.RowCount = q.Count();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.Tarih);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new frDuyurular
            {
                EnstituAdi = s.EnstituAd,
                EnstituKod = s.EnstituKod,
                DuyuruID = s.DuyuruID,
                Baslik = s.Baslik,
                Aciklama = s.Aciklama,
                AciklamaHtml = s.AciklamaHtml,
                Tarih = s.Tarih,
                DuyuruYapan = s.DuyuruYapan,
                IslemYapanIP = s.IslemYapanIP,
                EkSayisi = s.EkSayisi,
                DuyuruEkleris = s.Ekler,
                IsAktif = s.IsAktif,
                AnaSayfadaGozuksun = s.AnaSayfadaGozuksun,
                AnaSayfaPopupAc = s.AnaSayfaPopupAc,
                BasvuruPopupAc = s.BasvuruPopupAc,
                TIBasvuruPopupAc = s.TIBasvuruPopupAc,
                TDOBasvuruPopupAc = s.TDOBasvuruPopupAc,
                MezuniyetBasvuruPopupAc = s.MezuniyetBasvuruPopupAc,
                TalepYaparkenPopupAc = s.TalepYaparkenPopupAc,
                YayinSonTarih = s.YayinSonTarih
            }).ToList();
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IndexModel = IndexModel;
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id, string EKD)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var MmMessage = new MmMessage();
            ViewBag.MmMessage = MmMessage;
            var model = new Duyurular();
            if (id.HasValue && id > 0)
            {
                var data = db.Duyurulars.Where(p => p.DuyuruID == id).FirstOrDefault();
                if (data != null) model = data;
            }
            string sEnstituKod = "";
            if (EnstKods.Count == 1)
            {
                sEnstituKod = EnstKods.First();
            }
            else sEnstituKod = Management.getSelectedEnstitu(EKD);
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod ?? sEnstituKod);
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }


        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Kayit(Duyurular kModel, List<string> DosyaEkiAdi, List<HttpPostedFileBase> DosyaEki, List<int?> DuyuruDosyaEkID)
        {
            var MmMessage = new MmMessage();
            DuyuruDosyaEkID = DuyuruDosyaEkID == null ? new List<int?>() : DuyuruDosyaEkID;
            DosyaEkiAdi = DosyaEkiAdi == null ? new List<string>() : DosyaEkiAdi;
            DosyaEki = DosyaEki == null ? new List<HttpPostedFileBase>() : DosyaEki;
            var qDosyaEkAdi = DosyaEkiAdi.Select((s, inx) => new { s, inx }).ToList();
            var qDosyaEki = DosyaEki.Select((s, inx) => new { s, inx }).ToList();
            var qDuyuruDosyaEkID = DuyuruDosyaEkID.Select((s, inx) => new { s, inx }).ToList();
            var qDosyalar = (from EkGirilenAd in qDosyaEkAdi
                             join EklenenEk in qDosyaEki on EkGirilenAd.inx equals EklenenEk.inx
                             select new { EkGirilenAd.inx, DosyaEkAdi = EkGirilenAd.s, Dosya = EklenenEk.s }).ToList();

            var qVarolanlar = (from s in qDosyaEkAdi
                               join sid in qDuyuruDosyaEkID on s.inx equals sid.inx
                               select new { s.inx, DosyaEkAdi = s.s, DuyuruDosyaEkID = sid.s });
            #region Kontrol
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                string msg = "Duyurunun Yayınlanacağı Enstitüyü Seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });

            }

            if (kModel.Tarih == DateTime.MinValue)
            {
                string msg = "Geçerli Bir Tarih Giriniz.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
            }
            else
            {
                if (kModel.YayinSonTarih.HasValue)
                {
                    if (kModel.YayinSonTarih.Value <= kModel.Tarih)
                    {
                        string msg = "Duyurunun yayınlanacağı son tarih Duyuru tarihinden tarihten küçük ya da eşit olamaz! ";
                        MmMessage.Messages.Add(msg);
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YayinSonTarih" });
                    }
                }
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Tarih" });
            }
            if (kModel.Baslik.IsNullOrWhiteSpace())
            {
                string msg = "Başlık Giriniz.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Baslik" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Baslik" });

            if (kModel.Aciklama.IsNullOrWhiteSpace() && kModel.AciklamaHtml.IsNullOrWhiteSpace())
            {
                string msg = "Aciklama Giriniz.";
                MmMessage.Messages.Add(msg);
            }

            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.Aciklama = kModel.Aciklama ?? "";
                if (kModel.DuyuruID <= 0)
                {
                    kModel.IsAktif = true;
                    var eklenen = db.Duyurulars.Add(kModel);

                    foreach (var item in qDosyalar)
                    {
                        string DosyaYolu = "/DuyuruDosyaları/" + item.DosyaEkAdi.ToFileNameAddGuid(item.Dosya.FileName.GetFileExtension());
                        item.Dosya.SaveAs(Server.MapPath("~" + DosyaYolu));

                        db.DuyuruEkleris.Add(new DuyuruEkleri
                        {
                            DuyuruID = eklenen.DuyuruID,
                            DosyaEkAdi = item.DosyaEkAdi,
                            DosyaYolu = DosyaYolu
                        });
                    }
                }
                else
                {
                    var data = db.Duyurulars.Where(p => p.DuyuruID == kModel.DuyuruID).First();
                    data.Baslik = kModel.Baslik;
                    data.Aciklama = kModel.Aciklama;
                    data.AciklamaHtml = kModel.AciklamaHtml;
                    data.Tarih = kModel.Tarih;
                    data.YayinSonTarih = kModel.YayinSonTarih;
                    data.AnaSayfadaGozuksun = kModel.AnaSayfadaGozuksun;
                    data.AnaSayfaPopupAc = kModel.AnaSayfaPopupAc;
                    data.BasvuruPopupAc = kModel.BasvuruPopupAc;
                    data.MezuniyetBasvuruPopupAc = kModel.MezuniyetBasvuruPopupAc;
                    data.TalepYaparkenPopupAc = kModel.TalepYaparkenPopupAc;
                    data.TDOBasvuruPopupAc = kModel.TDOBasvuruPopupAc;
                    data.TIBasvuruPopupAc = kModel.TIBasvuruPopupAc;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemTarihi = DateTime.Now;
                    data.IslemYapanID = kModel.IslemYapanID;
                    data.IslemYapanIP = kModel.IslemYapanIP;

                    var SilinenDuyuruEkleri = db.DuyuruEkleris.Where(p => DuyuruDosyaEkID.Contains(p.DuyuruDosyaEkID) == false && p.DuyuruID == data.DuyuruID).ToList();
                    var VarolanDuyuruEkleri = db.DuyuruEkleris.Where(p => DuyuruDosyaEkID.Contains(p.DuyuruDosyaEkID) && p.DuyuruID == data.DuyuruID).ToList();
                    foreach (var item in VarolanDuyuruEkleri)
                    {
                        var qd = qVarolanlar.Where(p => p.DuyuruDosyaEkID == item.DuyuruDosyaEkID).FirstOrDefault();
                        if (qd != null)
                        {
                            item.DosyaEkAdi = qd.DosyaEkAdi;
                        }
                    }
                    db.DuyuruEkleris.RemoveRange(SilinenDuyuruEkleri);
                    foreach (var item in qDosyalar)
                    {
                        string DosyaYolu = "/DuyuruDosyaları/" + item.Dosya.FileName.ToFileNameAddGuid();
                        item.Dosya.SaveAs(Server.MapPath("~" + DosyaYolu));

                        db.DuyuruEkleris.Add(new DuyuruEkleri
                        {
                            DuyuruID = data.DuyuruID,
                            DosyaEkAdi = item.DosyaEkAdi,
                            DosyaYolu = DosyaYolu
                        });
                    }
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
            ViewBag.MmMessage = MmMessage;
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", kModel.IsAktif);
            return View(kModel);
        }
        public ActionResult Sil(int id)
        {
            var kayit = db.Duyurulars.Where(p => p.DuyuruID == id).FirstOrDefault();
            string message = "";
            bool success = true;
            if (kayit != null)
            {
                try
                {
                    message = "'" + kayit.Baslik + "' Başlıklı Duyuru Silindi!";
                    var dosyalar = kayit.DuyuruEkleris.ToList();

                    db.Duyurulars.Remove(kayit);
                    db.SaveChanges();
                    foreach (var item in dosyalar)
                    {
                        System.IO.File.Delete(Server.MapPath("~" + item.DosyaYolu));
                    }
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.Baslik + "' Başlıklı Duyuru! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "Duyurular/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Duyuru sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }




        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
