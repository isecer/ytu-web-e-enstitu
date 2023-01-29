using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize(Roles = RoleNames.MailSablonlari)]
    [System.Web.Mvc.OutputCache(NoStore = false, Duration = 0, VaryByParam = "*")]
    public class MailSablonlariController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string EKD)
        {
            return Index(new fmMailSablonlari() { PageSize = 15 }, EKD);
        }
        [HttpPost]
        public ActionResult Index(fmMailSablonlari model, string EKD)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in db.MailSablonlaris
                    join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    join ens in db.Enstitulers on new { s.EnstituKod } equals new { ens.EnstituKod } 
                    where EnstKods.Contains(s.EnstituKod) && s.MailSablonTipleri.SistemMaili==false
                    select new frMailSablonlari
                    {
                        EnstituKod = s.EnstituKod,
                        EnstituAdi = ens.EnstituAd,
                        MailSablonlariID = s.MailSablonlariID,
                        SablonAdi = s.SablonAdi,
                        EkSayisi = s.MailSablonlariEkleris.Count,
                        MailSablonlariEkleris = s.MailSablonlariEkleris.ToList(), 
                        Sablon = s.Sablon,
                        SablonHtml = s.SablonHtml,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP,
                        IslemYapan = k.Ad + " " + k.Soyad,
                    };

            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.SablonAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.SablonAdi.Contains(model.SablonAdi) || p.Sablon.Contains(model.SablonAdi));
            if (model.MailSablonTipID.HasValue) q = q.Where(p => p.MailSablonTipID == model.MailSablonTipID);
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);

            model.RowCount = q.Count();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.IslemTarihi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler( true), "Value", "Caption", model.EnstituKod); 
            ViewBag.IndexModel = IndexModel;
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id,  string EKD )
        { 
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var MmMessage = new MmMessage(); 
            ViewBag.MmMessage = MmMessage;
            var model = new MailSablonlari();
            if (id.HasValue && id > 0)
            {
                var data = db.MailSablonlaris.Where(p => p.MailSablonlariID == id && p.MailSablonTipleri.SistemMaili==false).FirstOrDefault();
                if (data != null) model = data;
            }
            string sEnstituKod = "";
            if (EnstKods.Count == 1)
            {
                sEnstituKod = EnstKods.First();
            }
            else sEnstituKod = Management.getSelectedEnstitu(EKD);
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler( true), "Value", "Caption", model.EnstituKod ?? sEnstituKod); 
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }


        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Kayit(MailSablonlari kModel, List<string> EkAdi, List<HttpPostedFileBase> DosyaEki, List<int?> MailSablonlariEkiID)
        {
            var MmMessage = new MmMessage(); 
            MailSablonlariEkiID = MailSablonlariEkiID == null ? new List<int?>() : MailSablonlariEkiID;
            EkAdi = EkAdi == null ? new List<string>() : EkAdi;
            DosyaEki = DosyaEki == null ? new List<HttpPostedFileBase>() : DosyaEki;
            var qDosyaEkAdi = EkAdi.Select((s, inx) => new { s, inx }).ToList();
            var qDosyaEki = DosyaEki.Select((s, inx) => new { s, inx }).ToList();
            var qDuyuruDosyaEkID = MailSablonlariEkiID.Select((s, inx) => new { s, inx }).ToList();
            var qDosyalar = (from EkGirilenAd in qDosyaEkAdi
                             join EklenenEk in qDosyaEki on EkGirilenAd.inx equals EklenenEk.inx
                             select new { EkGirilenAd.inx, DosyaEkAdi = EkGirilenAd.s, Dosya = EklenenEk.s }).ToList();

            var qVarolanlar = (from s in qDosyaEkAdi
                               join sid in qDuyuruDosyaEkID on s.inx equals sid.inx
                               select new { s.inx, DosyaEkAdi = s.s, MailSablonlariEkiID = sid.s });
            #region Kontrol
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                string msg = "Şablonun Ekleneceği Enstitüyü Seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });

            }
            
            if (kModel.SablonAdi.IsNullOrWhiteSpace())
            {
                string msg = "Şablon Adı Giriniz.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SablonAdi" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SablonAdi" });

            if (kModel.Sablon.IsNullOrWhiteSpace() && kModel.SablonHtml.IsNullOrWhiteSpace())
            {
                string msg = "Sablon Açıklaması Giriniz.";
                MmMessage.Messages.Add(msg);
            }
            
            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.Sablon = kModel.Sablon ?? "";
                if (kModel.MailSablonlariID <= 0)
                {
                    kModel.MailSablonTipID = MailSablonTipi.Normal; 
                    kModel.IsAktif = true;
                    var eklenen = db.MailSablonlaris.Add(kModel);

                    foreach (var item in qDosyalar)
                    {
                       
                        string DosyaYolu = "/DuyuruDosyaları/" + item.DosyaEkAdi.ToFileNameAddGuid(item.Dosya.FileName.GetFileExtension());
                        item.Dosya.SaveAs(Server.MapPath("~" + DosyaYolu));

                        db.MailSablonlariEkleris.Add(new MailSablonlariEkleri
                        {
                            MailSablonlariID = eklenen.MailSablonlariID,
                            EkAdi = item.DosyaEkAdi,
                            EkDosyaYolu = DosyaYolu
                        });
                    }
                }
                else
                {
                    var data = db.MailSablonlaris.Where(p => p.MailSablonlariID == kModel.MailSablonlariID && p.MailSablonTipleri.SistemMaili == false).First();
                    data.EnstituKod = kModel.EnstituKod;
                    data.SablonAdi = kModel.SablonAdi;
                    data.Sablon = kModel.Sablon;
                    data.SablonHtml = kModel.SablonHtml; 
                    data.IsAktif = kModel.IsAktif;
                    data.IslemTarihi = DateTime.Now;
                    data.IslemYapanID = kModel.IslemYapanID;
                    data.IslemYapanIP = kModel.IslemYapanIP;

                    var SilinenDuyuruEkleri = db.MailSablonlariEkleris.Where(p => MailSablonlariEkiID.Contains(p.MailSablonlariEkiID) == false && p.MailSablonlariID == data.MailSablonlariID).ToList();
                    var VarolanDuyuruEkleri = db.MailSablonlariEkleris.Where(p => MailSablonlariEkiID.Contains(p.MailSablonlariEkiID) && p.MailSablonlariID == data.MailSablonlariID).ToList();
                    foreach (var item in VarolanDuyuruEkleri)
                    {
                        var qd = qVarolanlar.Where(p => p.MailSablonlariEkiID == item.MailSablonlariEkiID).FirstOrDefault();
                        if (qd != null)
                        {
                            item.EkAdi = qd.DosyaEkAdi;
                        }
                    }
                    db.MailSablonlariEkleris.RemoveRange(SilinenDuyuruEkleri);
                    foreach (var item in qDosyalar)
                    {
                        var dosyaTipi = item.Dosya.FileName.Split('.').Last();
                        var DosyaAdi = item.Dosya.FileName.Replace('.' + dosyaTipi, "_" + Guid.NewGuid().ToString().Substr(0, 4) + "." + dosyaTipi);
                        string DosyaYolu = "/DuyuruDosyaları/" + DosyaAdi;
                        item.Dosya.SaveAs(Server.MapPath("~" + DosyaYolu));

                        db.MailSablonlariEkleris.Add(new MailSablonlariEkleri
                        {
                            MailSablonlariID = data.MailSablonlariID,
                            EkAdi = item.DosyaEkAdi,
                            EkDosyaYolu = DosyaYolu
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
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler( true), "Value", "Caption", kModel.EnstituKod); 
            ViewBag.MmMessage = MmMessage;
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", kModel.IsAktif);
            return View(kModel);
        }
        public ActionResult Sil(int id)
        {
            var kayit = db.MailSablonlaris.Where(p => p.MailSablonlariID == id && p.MailSablonTipleri.SistemMaili == false).FirstOrDefault();
            string message = "";
            bool success = true;
            if (kayit != null)
            {
                try
                {
                    message = "'" + kayit.SablonAdi + "' Şablon Şablon Silindi!";
                    var dosyalar = kayit.MailSablonlariEkleris.ToList();

                    db.MailSablonlaris.Remove(kayit);
                    db.SaveChanges();
                    foreach (var item in dosyalar)
                    {
                        System.IO.File.Delete(Server.MapPath("~" + item.EkDosyaYolu));
                    }
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.SablonAdi + "' Başlıklı Şablon! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "MailSablonlari/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Şablon sistemde bulunamadı!";
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