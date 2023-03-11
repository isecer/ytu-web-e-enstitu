using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Enstituler)]
    public class EnstitulerController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmEnstitulerDto { });
        }
        [HttpPost]
        public ActionResult Index(FmEnstitulerDto model)
        {

            var q = from s in db.Enstitulers
                    select new FrEnstitulerDto
                    {

                        EnstituKod = s.EnstituKod,
                        WebAdresi = s.WebAdresi,
                        SmtpHost = s.SmtpHost,
                        SmtpKullaniciAdi = s.SmtpKullaniciAdi,
                        SmtpMailAdresi = s.SmtpMailAdresi,
                        SmtpPortAdresi = s.SmtpPortAdresi,
                        SmtpSSL = s.SmtpSSL,
                        SmtpSifre = s.SmtpSifre,
                        SistemErisimAdresi = s.SistemErisimAdresi,
                        IsAktif = s.IsAktif,
                        EnstituAd = s.EnstituAd,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP,
                        IslemYapan = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad
                    };

            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.EnstituAd.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituAd.Contains(model.EnstituAd));
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.EnstituAd); 
            model.EnstitulerDtos = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount,
                Aktif = q.Count(p => p.IsAktif),
                Pasif = q.Count(p => !p.IsAktif)
            };
            ViewBag.IndexModel = indexModel;
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(string id)
        {
            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;
            var model = new Enstituler();
            if (!id.IsNullOrWhiteSpace())
            {

                var data = db.Enstitulers.FirstOrDefault(p => p.EnstituKod == id);
                if (data != null)
                {
                    model = data;

                }
            }
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(Enstituler kModel)
        {
            var mmMessage = new MmMessage();
            #region Kontrol 
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Enstitü Kodu Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });
            if (kModel.SmtpHost.IsNullOrWhiteSpace())
            {

                mmMessage.Messages.Add("Smtp Host Adresi Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SmtpHost" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SmtpHost" });
            if (kModel.SmtpKullaniciAdi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Smtp Kullanıcı Adı Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SmtpKullaniciAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SmtpKullaniciAdi" });
            if (kModel.SmtpMailAdresi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Smtp E-Posta Adresi Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SmtpMailAdresi" });
            }
            else if (kModel.SmtpMailAdresi.ToIsValidEmail())
            {
                mmMessage.Messages.Add("Lütfen E-Posta Adresini Doğru Formatta Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SmtpMailAdresi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SmtpMailAdresi" });
            if (kModel.SmtpPortAdresi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Smtp Port Bilgisini Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SmtpPortAdresi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SmtpPortAdresi" });
            if (kModel.SmtpSifre.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Smtp E-Posta Şifresi Bilgisini Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SmtpSifre" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SmtpSifre" });
            if (kModel.SistemErisimAdresi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Sistem Erişim Adresi Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SistemErisimAdresi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SistemErisimAdresi" });

            if (kModel.WebAdresi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Web Adresi Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "WebAdresi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "WebAdresi" });

            if (kModel.ToplamKayitKota <= 0)
            { 
                mmMessage.Messages.Add("Toplam Kayıt Kotasını Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ToplamKayitKota" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ToplamKayitKota" }); 

            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                if (kModel.EnstituKod.IsNullOrWhiteSpace())
                {
                    kModel.IsAktif = true;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                    kModel.IslemYapanIP = UserIdentity.Ip;
                    kModel.IslemTarihi = DateTime.Now;
                    db.Enstitulers.Add(kModel);

                }
                else
                {
                    var data = db.Enstitulers.First(p => p.EnstituKod == kModel.EnstituKod);
                    data.EnstituKod = kModel.EnstituKod;
                    data.EnstituAd = kModel.EnstituAd;
                    data.EnstituKisaAd = kModel.EnstituKisaAd;
                    data.SmtpHost = kModel.SmtpHost;
                    data.SmtpKullaniciAdi = kModel.SmtpKullaniciAdi;
                    data.SmtpMailAdresi = kModel.SmtpMailAdresi;
                    data.SmtpPortAdresi = kModel.SmtpPortAdresi;
                    data.SmtpSSL = kModel.SmtpSSL;
                    data.SmtpSifre = kModel.SmtpSifre;
                    data.SistemErisimAdresi = kModel.SistemErisimAdresi;
                    data.WebAdresi = kModel.WebAdresi;
                    data.ToplamKayitKota = kModel.ToplamKayitKota;
                    data.LUBMailGonder = kModel.LUBMailGonder;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now;
                   

                } 
                db.SaveChanges();
                EnstituBus.Enstitulers = db.Enstitulers.ToList();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
            

            ViewBag.MmMessage = mmMessage;  
            return View(kModel);
        }
        public ActionResult Sil(string id)
        {
            var kayit = db.Enstitulers.FirstOrDefault(p => p.EnstituKod == id);
            string message = "";
            bool success = true;
            if (kayit != null)
            {
                 
                try
                {
                    message = "'" + kayit.EnstituAd + "' İsimli Enstitü Silindi!";
                    db.Enstitulers.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.EnstituAd + "' İsimli Enstitü Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Enstituler/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Enstitü sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
