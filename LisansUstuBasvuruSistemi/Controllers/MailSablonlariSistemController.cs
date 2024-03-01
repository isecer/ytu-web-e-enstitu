using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.MailSablonlariSistem)]
    public class MailSablonlariSistemController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();
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
                    where enstKods.Contains(s.EnstituKod) && s.MailSablonTipleri.SistemMaili
                    select new FrMailSablonlariDto
                    {
                        EnstituKod = s.EnstituKod,
                        EnstituAdi = ens.EnstituAd,
                        MailSablonTipID = s.MailSablonTipID,
                        SablonTipAdi = s.MailSablonTipleri.SablonTipAdi,
                        Parametreler = s.MailSablonTipleri.Parametreler,
                        MailSablonlariID = s.MailSablonlariID,
                        SablonAdi = s.SablonAdi,
                        EkSayisi = s.MailSablonlariEkleris.Count,
                        MailSablonlariEkleris = s.MailSablonlariEkleris.ToList(),
                        GonderilecekEkEpostalar = s.GonderilecekEkEpostalar,
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
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort)
                : q.OrderBy(o => o.EnstituAdi).ThenBy(t => t.SablonTipAdi);
            model.MailSablonlariDtos = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.IndexModel = indexModel;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.MailSablonTipID = new SelectList(MailSablonTipleriBus.GetCmbMailSablonTipleri(true, true), "Value", "Caption", model.MailSablonTipID);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id, string ekd)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;
            var model = new MailSablonlari();
            if (id > 0)
            {
                var data = _entities.MailSablonlaris.FirstOrDefault(p => p.MailSablonlariID == id && p.MailSablonTipleri.SistemMaili);
                if (data != null) model = data;
            }

            var sEnstituKod = enstKods.Count == 1 ? enstKods.First() : EnstituBus.GetSelectedEnstitu(ekd);
            ViewBag.SablonTipi = _entities.MailSablonTipleris.FirstOrDefault(p => p.MailSablonTipID == model.MailSablonTipID);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod ?? sEnstituKod);
            ViewBag.MailSablonTipID = new SelectList(MailSablonTipleriBus.GetCmbMailSablonTipleri(true, true, !(id > 0)), "Value", "Caption", model.MailSablonTipID);
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
            var qSablonEkId = mailSablonlariEkiId.Select((s, inx) => new { s, inx }).ToList();
            var eklenecekDosyalar = (from ekGirilenAd in qDosyaEkAdi
                             join eklenenEk in qDosyaEki on ekGirilenAd.inx equals eklenenEk.inx
                             select new { ekGirilenAd.inx, DosyaEkAdi = ekGirilenAd.s, Dosya = eklenenEk.s }).Where(p => p.Dosya != null).ToList();

            var varolanDosyalar = (from s in qDosyaEkAdi
                               join sid in qSablonEkId on s.inx equals sid.inx
                               select new { s.inx, DosyaEkAdi = s.s, MailSablonlariEkiID = sid.s });
            #region Kontrol
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Şablonun Ekleneceği Enstitüyü Seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EnstituKod" });

            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EnstituKod" });

            if (kModel.MailSablonTipID <= 0)
            {
                mmMessage.Messages.Add("Şablon Tipini Seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MailSablonTipID" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "MailSablonTipID" });
            if (kModel.SablonAdi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Mail Konusu Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "SablonAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "SablonAdi" });

            if (kModel.Sablon.IsNullOrWhiteSpace() && kModel.SablonHtml.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Sablon Açıklaması Giriniz.");
            }
            #endregion

            if (mmMessage.Messages.Count == 0)
            {
                if (_entities.MailSablonlaris.Any(p => p.EnstituKod == kModel.EnstituKod && p.MailSablonlariID != kModel.MailSablonlariID && p.MailSablonTipleri.SistemMaili && p.MailSablonTipID == kModel.MailSablonTipID))
                {
                    mmMessage.Messages.Add("Sistem mail şablonu bir Enstitü için aynı dilde bir kere tanımlanabilir!");
                }
            }
            if (mmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.Sablon = kModel.Sablon ?? "";

                MailSablonlari mailSablonu;
                if (kModel.MailSablonlariID <= 0)
                {
                    kModel.IsAktif = true;
                    mailSablonu = _entities.MailSablonlaris.Add(kModel);
                    
                }
                else
                {
                    mailSablonu = _entities.MailSablonlaris.First(p => p.MailSablonlariID == kModel.MailSablonlariID && p.MailSablonTipleri.SistemMaili);
                    mailSablonu.EnstituKod = kModel.EnstituKod;
                    mailSablonu.MailSablonTipID = kModel.MailSablonTipID;
                    mailSablonu.GonderilecekEkEpostalar = kModel.GonderilecekEkEpostalar;
                    mailSablonu.SablonAdi = kModel.SablonAdi;
                    mailSablonu.Sablon = kModel.Sablon;
                    mailSablonu.SablonHtml = kModel.SablonHtml;
                    mailSablonu.MailSablonTipID = kModel.MailSablonTipID;
                    mailSablonu.IsAktif = kModel.IsAktif;
                    mailSablonu.IslemTarihi = DateTime.Now;
                    mailSablonu.IslemYapanID = kModel.IslemYapanID;
                    mailSablonu.IslemYapanIP = kModel.IslemYapanIP;

                    var silinenEkler = _entities.MailSablonlariEkleris.Where(p => mailSablonlariEkiId.Contains(p.MailSablonlariEkiID) == false && p.MailSablonlariID == mailSablonu.MailSablonlariID).ToList();
                    var varolanEkler = _entities.MailSablonlariEkleris.Where(p => mailSablonlariEkiId.Contains(p.MailSablonlariEkiID) && p.MailSablonlariID == mailSablonu.MailSablonlariID).ToList();
                    foreach (var item in varolanEkler)
                    {
                        var qd = varolanDosyalar.FirstOrDefault(p => p.MailSablonlariEkiID == item.MailSablonlariEkiID);
                        if (qd != null)
                        {
                            item.EkAdi = qd.DosyaEkAdi.GetFileName(item.EkDosyaYolu);
                        }
                    }
                    var mailSablonlariEkleriPaths = silinenEkler.Select(s => s.EkDosyaYolu).ToList();
                    _entities.MailSablonlariEkleris.RemoveRange(silinenEkler);
                    _entities.SaveChanges();
                    FileHelper.DeleteFiles(mailSablonlariEkleriPaths);
                }

                var eklenecekSablonEkleris = eklenecekDosyalar.Select(s => new MailSablonlariEkleri
                {
                    MailSablonlariID = mailSablonu.MailSablonlariID,
                    EkAdi = s.DosyaEkAdi.GetFileName(s.Dosya.FileName),
                    EkDosyaYolu = FileHelper.SaveMailSablonDosya(s.Dosya)
                });
                _entities.MailSablonlariEkleris.AddRange(eklenecekSablonEkleris);
                if (eklenecekSablonEkleris.Any()) _entities.SaveChanges();
                _entities.SaveChanges();
                return RedirectToAction("Index");
            }

            MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            ViewBag.MmMessage = mmMessage;
            ViewBag.SablonTipi = _entities.MailSablonTipleris.FirstOrDefault(p => p.MailSablonTipID == kModel.MailSablonTipID);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.MailSablonTipID = new SelectList(MailSablonTipleriBus.GetCmbMailSablonTipleri(true, true, kModel.MailSablonTipID <= 0), "Value", "Caption", kModel.MailSablonTipID);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", kModel.IsAktif);
            return View(kModel);
        }
        public ActionResult GetSablonTipParametre(int mailSablonTipId)
        {
            var stip = _entities.MailSablonTipleris.First(p => p.MailSablonTipID == mailSablonTipId);
            return Json(stip.Parametreler, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult Sil(int id)
        {
            var kayit = _entities.MailSablonlaris.FirstOrDefault(p => p.MailSablonlariID == id && p.MailSablonTipleri.SistemMaili);
            string message;
            var success = true;
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
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
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