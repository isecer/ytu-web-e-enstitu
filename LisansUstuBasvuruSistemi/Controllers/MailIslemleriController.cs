using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MailManager;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.MailIslemleri)]
    public class MailIslemleriController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        public ActionResult Index()
        {
            return Index(new FmMailGondermeDto() { PageSize = 15 });
        }
        [HttpPost]
        public ActionResult Index(FmMailGondermeDto model)
        {
            var q = from s in _entities.GonderilenMaillers.Where(p => model.Aciklama == null || model.Aciklama.Trim() == "" || p.Aciklama.Contains(model.Aciklama))
                    join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where s.Silindi == false && UserIdentity.Current.EnstituKods.Contains(s.EnstituKod)
                    select new
                    {
                        s.GonderilenMailID,
                        s.Tarih,
                        s.EnstituKod,
                        e.EnstituAd,
                        s.Konu,
                        MailGonderen = k.Ad + " " + k.Soyad,
                        s.Gonderildi,
                        s.HataMesaji,
                        EkSayisi = s.GonderilenMailEkleris.Count
                    };

            if (!model.Konu.IsNullOrWhiteSpace()) q = q.Where(p => p.Konu.Contains(model.Konu));
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.MailGonderen.IsNullOrWhiteSpace()) q = q.Where(p => p.MailGonderen.Contains(model.MailGonderen));
            if (model.IsEkVar.HasValue) q = q.Where(p => p.EkSayisi > 0 == model.IsEkVar);
            if (model.Tarih.HasValue)
            {
                var trih = model.Tarih.Value.TodateToShortDate();
                q = q.Where(p => p.Tarih == trih);

            }
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.Tarih);
            model.MailGondermeDtos = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new FrMailGondermeDto
            {
                GonderilenMailID = s.GonderilenMailID,
                Tarih = s.Tarih,
                EnstituAdi = s.EnstituAd,
                Konu = s.Konu,
                MailGonderen = s.MailGonderen,
                Gonderildi = s.Gonderildi,
                HataMesaji = s.HataMesaji,
                EkSayisi = s.EkSayisi

            }).ToList();
            ViewBag.IsEkVar = new SelectList(GonderilenMaillerBus.GetCmbMailEkKontrol(true), "Value", "Caption", model.IsEkVar);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            return View(model);
        }
        public ActionResult MailDetay(int gonderilenMailId)
        {

            var data = (from s in _entities.GonderilenMaillers
                        join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod into def
                        from xDef in def.DefaultIfEmpty()
                        join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                        where s.GonderilenMailID == gonderilenMailId
                        select new FrMailGondermeDto
                        {
                            GonderilenMailID = s.GonderilenMailID,
                            EnstituAdi = xDef != null ? xDef.EnstituAd : "Sistem",
                            Tarih = s.Tarih,
                            Konu = s.Konu,
                            Aciklama = s.Aciklama,
                            AciklamaHtml = s.AciklamaHtml,
                            MailGonderen = k.Ad + " " + k.Soyad,
                            UserKey = k.UserKey,
                            IslemYapanID = s.IslemYapanID,
                            IslemYapanIP = s.IslemYapanIP,
                            EkSayisi = s.GonderilenMailEkleris.Count,
                            KisiSayisi = s.GonderilenMailKullanicilars.Count,
                            GonderilenMailEkleris = s.GonderilenMailEkleris.ToList()

                        }).First();
            var dataK = (from s in _entities.GonderilenMailKullanicilars
                         orderby s.Kullanicilar.Ad, s.Kullanicilar.Soyad
                         where s.GonderilenMailID == gonderilenMailId
                         select new MailKullaniciBilgi
                         {
                             AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                             Email = s.Email
                         }).ToList();
            ViewBag.DataK = dataK;
            return View(data);
        }

        
        public ActionResult Sil(int id)
        {
            var kayit = _entities.GonderilenMaillers.FirstOrDefault(p => p.GonderilenMailID == id);
            var message = "";
            var success = true;
            if (kayit != null)
            {
                try
                {

                    if (message == "")
                    {
                        kayit.Silindi = true;
                        _entities.SaveChanges();
                        message = "'" + kayit.Konu + "' konulu email Silindi!";
                    }

                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.Konu + "' Konulu Mail Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message,  ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz mail bilgisi sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
