using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize(Roles = RoleNames.MailSablonlari)]
    [System.Web.Mvc.OutputCache(NoStore = false, Duration = 0, VaryByParam = "*")]
    public class MailSablonlariController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {
            return Index(new FmMailSablonlariDto() { PageSize = 15 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmMailSablonlariDto model, string ekd)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in _entities.MailSablonlaris
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    join ens in _entities.Enstitulers on new { s.EnstituKod } equals new { ens.EnstituKod } 
                    where enstKods.Contains(s.EnstituKod) && s.MailSablonTipleri.SistemMaili==false
                    select new FrMailSablonlariDto
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
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount
            };
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.IslemTarihi);
            model.MailSablonlariDtos = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler( true), "Value", "Caption", model.EnstituKod); 
            ViewBag.IndexModel = indexModel;
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id,  string ekd )
        { 
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var mmMessage = new MmMessage(); 
            ViewBag.MmMessage = mmMessage;
            var model = new MailSablonlari();
            if (id.HasValue && id > 0)
            {
                var data = _entities.MailSablonlaris.FirstOrDefault(p => p.MailSablonlariID == id && p.MailSablonTipleri.SistemMaili==false);
                if (data != null) model = data;
            }
            string sEnstituKod = "";
            sEnstituKod = enstKods.Count == 1 ? enstKods.First() : EnstituBus.GetSelectedEnstitu(ekd);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler( true), "Value", "Caption", model.EnstituKod ?? sEnstituKod); 
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }


        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Kayit(MailSablonlari kModel, List<string> ekAdi, List<HttpPostedFileBase> dosyaEki, List<int?> mailSablonlariEkiId)
        {
            var mmMessage = new MmMessage(); 
            mailSablonlariEkiId = mailSablonlariEkiId ?? new List<int?>();
            ekAdi = ekAdi ?? new List<string>();
            dosyaEki = dosyaEki ?? new List<HttpPostedFileBase>();
            var qDosyaEkAdi = ekAdi.Select((s, inx) => new { s, inx }).ToList();
            var qDosyaEki = dosyaEki.Select((s, inx) => new { s, inx }).ToList();
            var qDuyuruDosyaEkId = mailSablonlariEkiId.Select((s, inx) => new { s, inx }).ToList();
            var qDosyalar = (from ekGirilenAd in qDosyaEkAdi
                             join eklenenEk in qDosyaEki on ekGirilenAd.inx equals eklenenEk.inx
                             select new { ekGirilenAd.inx, DosyaEkAdi = ekGirilenAd.s, Dosya = eklenenEk.s }).ToList();

            var qVarolanlar = (from s in qDosyaEkAdi
                               join sid in qDuyuruDosyaEkId on s.inx equals sid.inx
                               select new { s.inx, DosyaEkAdi = s.s, MailSablonlariEkiID = sid.s });
            #region Kontrol
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Şablonun Ekleneceği Enstitüyü Seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });

            }
            
            if (kModel.SablonAdi.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Şablon Adı Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SablonAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SablonAdi" });

            if (kModel.Sablon.IsNullOrWhiteSpace() && kModel.SablonHtml.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Sablon Açıklaması Giriniz.");
            }
            
            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.Sablon = kModel.Sablon ?? "";
                if (kModel.MailSablonlariID <= 0)
                {
                    kModel.MailSablonTipID = MailSablonTipi.Normal; 
                    kModel.IsAktif = true;
                    var eklenen = _entities.MailSablonlaris.Add(kModel);

                    foreach (var item in qDosyalar)
                    { 
                        string dosyaYolu = "/DuyuruDosyaları/" + item.DosyaEkAdi.ToFileNameAddGuid(item.Dosya.FileName.GetFileExtension());
                        item.Dosya.SaveAs(Server.MapPath("~" + dosyaYolu)); 
                        _entities.MailSablonlariEkleris.Add(new MailSablonlariEkleri
                        {
                            MailSablonlariID = eklenen.MailSablonlariID,
                            EkAdi = item.DosyaEkAdi,
                            EkDosyaYolu = dosyaYolu
                        });
                    }
                }
                else
                {
                    var data = _entities.MailSablonlaris.First(p => p.MailSablonlariID == kModel.MailSablonlariID && p.MailSablonTipleri.SistemMaili == false);
                    data.EnstituKod = kModel.EnstituKod;
                    data.SablonAdi = kModel.SablonAdi;
                    data.Sablon = kModel.Sablon;
                    data.SablonHtml = kModel.SablonHtml; 
                    data.IsAktif = kModel.IsAktif;
                    data.IslemTarihi = DateTime.Now;
                    data.IslemYapanID = kModel.IslemYapanID;
                    data.IslemYapanIP = kModel.IslemYapanIP;

                    var silinenDuyuruEkleri = _entities.MailSablonlariEkleris.Where(p => mailSablonlariEkiId.Contains(p.MailSablonlariEkiID) == false && p.MailSablonlariID == data.MailSablonlariID).ToList();
                    var varolanDuyuruEkleri = _entities.MailSablonlariEkleris.Where(p => mailSablonlariEkiId.Contains(p.MailSablonlariEkiID) && p.MailSablonlariID == data.MailSablonlariID).ToList();
                    foreach (var item in varolanDuyuruEkleri)
                    {
                        var qd = qVarolanlar.FirstOrDefault(p => p.MailSablonlariEkiID == item.MailSablonlariEkiID);
                        if (qd != null)
                        {
                            item.EkAdi = qd.DosyaEkAdi;
                        }
                    }
                    _entities.MailSablonlariEkleris.RemoveRange(silinenDuyuruEkleri);
                    foreach (var item in qDosyalar)
                    {
                        var dosyaTipi = item.Dosya.FileName.Split('.').Last();
                        var dosyaAdi = item.Dosya.FileName.Replace('.' + dosyaTipi, "_" + Guid.NewGuid().ToString().Substr(0, 4) + "." + dosyaTipi);
                        string dosyaYolu = "/DuyuruDosyaları/" + dosyaAdi;
                        item.Dosya.SaveAs(Server.MapPath("~" + dosyaYolu));

                        _entities.MailSablonlariEkleris.Add(new MailSablonlariEkleri
                        {
                            MailSablonlariID = data.MailSablonlariID,
                            EkAdi = item.DosyaEkAdi,
                            EkDosyaYolu = dosyaYolu
                        });
                    }
                }
                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler( true), "Value", "Caption", kModel.EnstituKod); 
            ViewBag.MmMessage = mmMessage;
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", kModel.IsAktif);
            return View(kModel);
        }
        public ActionResult Sil(int id)
        {
            var kayit = _entities.MailSablonlaris.FirstOrDefault(p => p.MailSablonlariID == id && p.MailSablonTipleri.SistemMaili == false);
            string message = "";
            bool success = true;
            if (kayit != null)
            {
                try
                {
                    message = "'" + kayit.SablonAdi + "' Şablon Şablon Silindi!";
                    var dosyalar = kayit.MailSablonlariEkleris.ToList();

                    _entities.MailSablonlaris.Remove(kayit);
                    _entities.SaveChanges();
                    foreach (var item in dosyalar)
                    {
                        System.IO.File.Delete(Server.MapPath("~" + item.EkDosyaYolu));
                    }
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.SablonAdi + "' Başlıklı Şablon! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "MailSablonlari/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Şablon sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }




        protected override void Dispose(bool disposing)
        {
            _entities.Dispose();
            base.Dispose(disposing);
        }
    }
}