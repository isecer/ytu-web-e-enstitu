using System;
using System.Collections.Generic;
using System.Linq;
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
using System.IO;
using LisansUstuBasvuruSistemi.Raporlar.Genel;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using static DevExpress.XtraPrinting.Native.ExportOptionsPropertiesNames;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.YaziSablonlari)]
    public class YaziSablonlariController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        public ActionResult Index(string ekd)
        {
            return Index(new FmYaziSablonlariDto() { PageSize = 15 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmYaziSablonlariDto model, string ekd)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in _entities.YaziSablonlaris
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    join ens in _entities.Enstitulers on new { s.EnstituKod } equals new { ens.EnstituKod }
                    where enstKods.Contains(s.EnstituKod)
                    select new FrYaziSablonlariDto
                    {
                        EnstituKod = s.EnstituKod,
                        EnstituAdi = ens.EnstituAd,
                        YaziSablonTipID = s.YaziSablonTipID,
                        SablonTipAdi = s.YaziSablonTipleri.SablonTipAdi,
                        Parametreler = s.YaziSablonTipleri.Parametreler,
                        YaziSablonlariID = s.YaziSablonlariID,
                        Konu = s.Konu,
                        Sablon = s.Sablon,
                        SablonHtml = s.SablonHtml,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP,
                        IslemYapan = k.Ad + " " + k.Soyad,
                    };

            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.Konu.IsNullOrWhiteSpace()) q = q.Where(p => p.SablonTipAdi.Contains(model.Konu) || p.Konu.Contains(model.Konu) || p.Sablon.Contains(model.Konu));
            if (model.YaziSablonTipID.HasValue) q = q.Where(p => p.YaziSablonTipID == model.YaziSablonTipID);
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);

            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount
            };
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort)
                : q.OrderBy(o => o.EnstituAdi).ThenBy(t => t.SablonTipAdi);
            model.YaziSablonlariDtos = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.IndexModel = indexModel;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.YaziSablonTipID = new SelectList(YaziSablonTipleriBus.GetCmbYaziSablonTipleri(true, true), "Value", "Caption", model.YaziSablonTipID);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id, string ekd, bool isKopya = false)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;
            var model = new YaziSablonlari();
            if (id > 0)
            {
                var data = _entities.YaziSablonlaris.FirstOrDefault(p => p.YaziSablonlariID == id);
                if (data != null)
                {
                    model = data;
                    if (isKopya) model.YaziSablonlariID = 0;
                }
            }

            var sEnstituKod = enstKods.Count == 1 ? enstKods.First() : EnstituBus.GetSelectedEnstitu(ekd);
            ViewBag.SablonTipi = _entities.YaziSablonTipleris.FirstOrDefault(p => p.YaziSablonTipID == model.YaziSablonTipID);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod ?? sEnstituKod);
            ViewBag.YaziSablonTipID = new SelectList(YaziSablonTipleriBus.GetCmbYaziSablonTipleri(true, true, model.YaziSablonlariID <= 0), "Value", "Caption", model.YaziSablonTipID);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }


        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Kayit(YaziSablonlari kModel)
        {
            var mmMessage = new MmMessage();
            #region Kontrol
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Şablonun Ekleneceği Enstitüyü Seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EnstituKod" });

            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EnstituKod" });

            if (kModel.YaziSablonTipID <= 0)
            {
                mmMessage.Messages.Add("Şablon Tipini Seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YaziSablonTipID" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "YaziSablonTipID" });
            if (kModel.Konu.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Mail Konusu Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Konu" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "Konu" });

            if (kModel.Sablon.IsNullOrWhiteSpace() && kModel.SablonHtml.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Sablon Açıklaması Giriniz.");
            }
            #endregion

            if (mmMessage.Messages.Count == 0)
            {
                if (_entities.YaziSablonlaris.Any(p => p.EnstituKod == kModel.EnstituKod && p.YaziSablonlariID != kModel.YaziSablonlariID && p.YaziSablonTipID == kModel.YaziSablonTipID))
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

                if (kModel.YaziSablonlariID <= 0)
                {
                    kModel.IsAktif = true;
                    _entities.YaziSablonlaris.Add(kModel);

                }
                else
                {
                    var yaziSablonu = _entities.YaziSablonlaris.First(p => p.YaziSablonlariID == kModel.YaziSablonlariID);
                    yaziSablonu.EnstituKod = kModel.EnstituKod;
                    yaziSablonu.YaziSablonTipID = kModel.YaziSablonTipID;
                    yaziSablonu.Konu = kModel.Konu;
                    yaziSablonu.Sablon = kModel.Sablon;
                    yaziSablonu.SablonHtml = kModel.SablonHtml;
                    yaziSablonu.YaziSablonTipID = kModel.YaziSablonTipID;
                    yaziSablonu.IsAktif = kModel.IsAktif;
                    yaziSablonu.IslemTarihi = DateTime.Now;
                    yaziSablonu.IslemYapanID = kModel.IslemYapanID;
                    yaziSablonu.IslemYapanIP = kModel.IslemYapanIP;

                    _entities.SaveChanges();
                }

                _entities.SaveChanges();
                return RedirectToAction("Index");
            }

            MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            ViewBag.MmMessage = mmMessage;
            ViewBag.SablonTipi = _entities.YaziSablonTipleris.FirstOrDefault(p => p.YaziSablonTipID == kModel.YaziSablonTipID);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.YaziSablonTipID = new SelectList(YaziSablonTipleriBus.GetCmbYaziSablonTipleri(true, true, kModel.YaziSablonlariID <= 0), "Value", "Caption", kModel.YaziSablonTipID);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", kModel.IsAktif);
            return View(kModel);
        }
        public ActionResult GetSablonTipParametre(int yaziSablonTipId)
        {
            var stip = _entities.YaziSablonTipleris.First(p => p.YaziSablonTipID == yaziSablonTipId);
            return Json(stip.Parametreler, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult Sil(int id)
        {
            var kayit = _entities.YaziSablonlaris.FirstOrDefault(p => p.YaziSablonlariID == id);
            string message;
            var success = true;
            if (kayit != null)
            {
                try
                {
                    message = "'" + kayit.Konu + "' Şablon Şablon Silindi!";

                    _entities.YaziSablonlaris.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.Konu + "' Başlıklı Şablon! <br/> Bilgi:" + ex.ToExceptionMessage();
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
        [HttpPost]
        [ValidateInput(false)] // HTML içeriğini doğrulama hatalarını önlemek için kullanılır.
        public ActionResult SaveHtml(string key, string html)
        {
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(html))
            {
                // HTML verisini Session'a kaydet
                Session[key] = html;
                return Json(new { success = true, message = "HTML başarıyla kaydedildi." });
            }

            return Json(new { success = false, message = "Key veya HTML boş olamaz." });
        }
        public ActionResult ShowYaziP(string ekd, string key, string konu)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var enstitu = _entities.Enstitulers.First(f => f.EnstituKod == enstituKod);

            // Key ile saklanan HTML'yi session'dan çekiyoruz
            var html = HttpContext.Session[key]?.ToString(); 
            var rprX = new RprYaziSablonOlusturucu(enstitu, html, konu);


            var memoryStream = new MemoryStream();
            rprX.ExportToPdf(memoryStream);
            rprX.ExportOptions.Pdf.Compressed = true;
            memoryStream.Seek(0, SeekOrigin.Begin);
            Response.AddHeader("Content-Disposition", "inline;filename=\"" + rprX.DisplayName + ".pdf\"");
            return new FileStreamResult(memoryStream, "application/pdf");
        }
        public ActionResult ShowYazi(int? id, string ekd, string key, string konu)
        {

            var sablon = _entities.YaziSablonlaris.First(f => f.YaziSablonlariID == id);
            var enstitu = sablon.Enstituler;
          
            var rprX = new RprYaziSablonOlusturucu(enstitu, sablon.SablonHtml, sablon.Konu);



            var memoryStream = new MemoryStream();
            rprX.ExportToPdf(memoryStream);
            rprX.ExportOptions.Pdf.Compressed = true;
            memoryStream.Seek(0, SeekOrigin.Begin);
            Response.AddHeader("Content-Disposition", "inline;filename=\"" + rprX.DisplayName + ".pdf\"");
            return new FileStreamResult(memoryStream, "application/pdf");
        }
        protected override void Dispose(bool disposing)
        {
            _entities.Dispose();
            base.Dispose(disposing);
        }
    }
}