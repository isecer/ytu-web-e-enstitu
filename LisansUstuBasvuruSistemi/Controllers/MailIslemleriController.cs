using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.SystemData;

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
            var filteredMailsQuery = _entities.GonderilenMaillers.Where(p => p.Silindi == false && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod));

            if (!model.Aciklama.IsNullOrWhiteSpace()) filteredMailsQuery = filteredMailsQuery.Where(p => p.Aciklama.Contains(model.Aciklama));

            var q = from s in filteredMailsQuery
                    join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID 
                    select new
                    {
                        s.GonderilenMailID,
                        s.Tarih,
                        s.EnstituKod,
                        e.EnstituAd,
                        s.Konu,
                        MailGonderen = k.Ad + " " + k.Soyad,
                        s.Aciklama,
                        s.Gonderildi,
                        s.HataMesaji,
                        EkSayisi =s.GonderilenMailEkleris.Count
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



        public ActionResult MailIstatitik()
        {
            return MailIstatitik(new FmMailIstatistikDto { AyId = DateTime.Now.Month, Yil = DateTime.Now.Year });
        }
        [HttpPost]
        public ActionResult MailIstatitik(FmMailIstatistikDto model)
        {
            //var filteredMailsQuery = _entities.GonderilenMaillers.Where(p => p.Tarih.Year == model.Yil);

            //if (model.AyId.HasValue) filteredMailsQuery = filteredMailsQuery.Where(p => p.Tarih.Month == model.AyId);


            //var q = from gm in filteredMailsQuery
            //        group new { gm.EnstituKod } by new { gm.Tarih.Year, gm.Tarih.Month, gm.Tarih.Day }
            //    into g1
            //        select new
            //        {
            //            g1.Key.Year,
            //            g1.Key.Month,
            //            g1.Key.Day,
            //            FbeCount = g1.Count(p => p.EnstituKod == EnstituKodlariEnum.FenBilimleri),
            //            SbeCount = g1.Count(p => p.EnstituKod == EnstituKodlariEnum.SosyalBilimleri),
            //            TetCount = g1.Count(p => p.EnstituKod == EnstituKodlariEnum.TemizEnerjiTeknolojileri),
            //            ToplamCount = g1.Count()
            //        };

            var q = from gm in _entities.GonderilenMaillers
                where gm.Tarih.Year == model.Yil && (!model.AyId.HasValue || gm.Tarih.Month == model.AyId)
                group gm by new { gm.Tarih.Year, gm.Tarih.Month, gm.Tarih.Day } into g1
                select new
                {
                    g1.Key.Year,
                    g1.Key.Month,
                    g1.Key.Day,
                    FbeCount = g1.Count(p => p.EnstituKod == EnstituKodlariEnum.FenBilimleri),
                    SbeCount = g1.Count(p => p.EnstituKod == EnstituKodlariEnum.SosyalBilimleri),
                    TetCount = g1.Count(p => p.EnstituKod == EnstituKodlariEnum.TemizEnerjiTeknolojileri),
                    ToplamCount = g1.Count()
                };
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.Year).ThenByDescending(t => t.Month).ThenByDescending(o => o.Day);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList().Select(s => new FrIstatistikDto
            {
                Tarih = new DateTime(s.Year, s.Month, s.Day),
                FbeCount = s.FbeCount,
                SbeCount = s.SbeCount,
                TetCount = s.TetCount,
                ToplamCount = s.ToplamCount,


            }).ToList();

            var aylars = SrTalepleriBus.GetCmbAylar(true);
            var yillars = ComboData.GetCmbGonderilenMailYil();
            ViewBag.AyId = new SelectList(aylars, "Value", "Caption", model.AyId);
            ViewBag.Yil = new SelectList(yillars, "Value", "Caption", model.Yil);

            return View(model);
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
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
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
