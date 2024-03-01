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
    [Authorize(Roles = RoleNames.MailSablonlari)]
    [OutputCache(NoStore = false, Duration = 0, VaryByParam = "*")]
    public class MailSablonlariController : Controller
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
                    where enstKods.Contains(s.EnstituKod) && s.MailSablonTipleri.SistemMaili == false
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
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IndexModel = indexModel;
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id, string ekd)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;
            var model = new MailSablonlari();
            if (id.HasValue && id > 0)
            {
                var data = _entities.MailSablonlaris.FirstOrDefault(p => p.MailSablonlariID == id && p.MailSablonTipleri.SistemMaili == false);
                if (data != null) model = data;
            }

            var sEnstituKod = enstKods.Count == 1 ? enstKods.First() : EnstituBus.GetSelectedEnstitu(ekd);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod ?? sEnstituKod);
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
            var qSablonDosyaEkId = mailSablonlariEkiId.Select((s, inx) => new { s, inx }).ToList();
            var eklenecekDosyalar = (from ekGirilenAd in qDosyaEkAdi
                                     join eklenenEk in qDosyaEki on ekGirilenAd.inx equals eklenenEk.inx
                                     select new { ekGirilenAd.inx, DosyaEkAdi = ekGirilenAd.s, Dosya = eklenenEk.s }).Where(p => p.Dosya != null).ToList();

            var varolanDosyalar = (from s in qDosyaEkAdi
                                   join sid in qSablonDosyaEkId on s.inx equals sid.inx
                                   select new { s.inx, DosyaEkAdi = s.s, MailSablonlariEkiID = sid.s });
            #region Kontrol
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Şablonun Ekleneceği Enstitüyü Seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EnstituKod" });

            }

            if (kModel.SablonAdi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Şablon Adı Giriniz.");
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
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.Sablon = kModel.Sablon ?? "";
                MailSablonlari mailSablonu;
                if (kModel.MailSablonlariID <= 0)
                {
                    kModel.MailSablonTipID = MailSablonTipiEnum.Normal;
                    kModel.IsAktif = true;
                    mailSablonu = _entities.MailSablonlaris.Add(kModel);
                    _entities.SaveChanges();

                }
                else
                {
                    mailSablonu = _entities.MailSablonlaris.First(p => p.MailSablonlariID == kModel.MailSablonlariID && p.MailSablonTipleri.SistemMaili == false);
                    mailSablonu.EnstituKod = kModel.EnstituKod;
                    mailSablonu.SablonAdi = kModel.SablonAdi;
                    mailSablonu.Sablon = kModel.Sablon;
                    mailSablonu.SablonHtml = kModel.SablonHtml;
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
                return RedirectToAction("Index");
            }

            MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.MmMessage = mmMessage;
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", kModel.IsAktif);
            return View(kModel);
        }
        public ActionResult Sil(int id)
        {
            var kayit = _entities.MailSablonlaris.FirstOrDefault(p => p.MailSablonlariID == id && p.MailSablonTipleri.SistemMaili == false);
            string message;
            var success = true;
            if (kayit != null)
            {
                try
                {
                    message = "'" + kayit.SablonAdi + "' Şablon Şablon Silindi!";
                    var dosyalar = kayit.MailSablonlariEkleris.ToList();
                    var filePaths = dosyalar.Select(s => s.EkDosyaYolu).ToList();
                    _entities.MailSablonlaris.Remove(kayit);
                    _entities.SaveChanges();
                    FileHelper.DeleteFiles(filePaths);
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