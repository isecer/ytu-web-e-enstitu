using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.SystemData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = "Mail İşlemleri")]
    public class MailIslemleriController : Controller
    {
        private readonly LubsDbEntities _entities;

        public MailIslemleriController()
        {
            _entities = new LubsDbEntities();
        }

        public ActionResult Index()
        {
            FmMailGondermeDto model = new FmMailGondermeDto();
            model.PageSize = 15;
            return Index(model);
        }

        [HttpPost]
        public ActionResult Index2(FmMailGondermeDto model)
        {
            // E-posta listesini filtreleme işlemleri
            var query = _entities.GonderilenMaillers
                .Where(p => p.Silindi == false && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod));

            // Ek dosyası olan mailleri filtreleme
            if (model.IsEkVar == true)
            {
                query = query.Where(p => p.GonderilenMailEkleris.Any());
            }

            // Konu filtrelemesi
            if (!string.IsNullOrWhiteSpace(model.Konu))
            {
                string searchTerm = string.Concat("\"", model.Konu, "\"");
                string sql = string.Format("SELECT * FROM {0} WHERE CONTAINS({1}, @p0) AND {2} = 0", "GonderilenMailler", "AciklamaHtml", "Silindi");

                var filteredMailIds = _entities.GonderilenMaillers
                    .SqlQuery(sql, searchTerm)
                    .Select(s => s.GonderilenMailID)
                    .ToList();

                query = query.Where(p => filteredMailIds.Contains(p.GonderilenMailID));
            }

            // Enstitü filtrelemesi
            if (!string.IsNullOrWhiteSpace(model.EnstituKod))
            {
                query = query.Where(p => p.EnstituKod == model.EnstituKod);
            }

            // Tarih filtrelemesi
            if (model.Tarih.HasValue)
            {
                DateTime shortDate = model.Tarih.Value.TodateToShortDate();
                query = query.Where(p => p.Tarih == shortDate);
            }

            // Enstitü ve kullanıcı bilgileriyle joinleme
            var resultQuery = from mail in query
                              join enst in _entities.Enstitulers on mail.EnstituKod equals enst.EnstituKod
                              join kullanici in _entities.Kullanicilars on mail.IslemYapanID equals kullanici.KullaniciID
                              select new
                              {
                                  GonderilenMailID = mail.GonderilenMailID,
                                  Tarih = mail.Tarih,
                                  EnstituKod = mail.EnstituKod,
                                  EnstituAd = enst.EnstituAd,
                                  Konu = mail.Konu,
                                  MailGonderen = kullanici.Ad + " " + kullanici.Soyad,
                                  Gonderildi = mail.Gonderildi,
                                  HataMesaji = mail.HataMesaji,
                                  IslemYapanID = mail.IslemYapanID
                              };

            // Gönderen filtrelemesi
            if (!string.IsNullOrWhiteSpace(model.MailGonderen))
            {
                resultQuery = resultQuery.Where(p => p.MailGonderen.Contains(model.MailGonderen));
            }

            // Toplam kayıt sayısını alma
            model.RowCount = resultQuery.Count();

            // Sıralama
            var orderedQuery = string.IsNullOrWhiteSpace(model.Sort)
                ? resultQuery.OrderByDescending(o => o.Tarih)
                : resultQuery.OrderBy(model.Sort);

            // Sayfalama ve sonuçları FrMailGondermeDto'ya dönüştürme
            var mailList = orderedQuery
                .Skip(model.StartRowIndex)
                .Take(model.PageSize)
                .ToList()
                .Select(s => new FrMailGondermeDto
                {
                    GonderilenMailID = s.GonderilenMailID,
                    Tarih = s.Tarih,
                    EnstituAdi = s.EnstituAd,
                    Konu = s.Konu,
                    MailGonderen = s.MailGonderen,
                    Gonderildi = s.Gonderildi,
                    HataMesaji = s.HataMesaji
                })
                .ToList();

            model.MailGondermeDtos = mailList;

            // E-posta eklerini ve alıcı sayılarını hesaplama
            var gonderilenMailIds = model.MailGondermeDtos.Select(s => s.GonderilenMailID).ToList();
            var ekSayilari = _entities.GonderilenMaillers
                .Where(p => gonderilenMailIds.Contains(p.GonderilenMailID))
                .Select(s => new { s.GonderilenMailID, EkSayisi = s.GonderilenMailEkleris.Count })
                .ToList();

            // Her mail için ek sayısını atama
            foreach (var mail in model.MailGondermeDtos)
            {
                mail.EkSayisi = ekSayilari
                    .Where(f => f.GonderilenMailID == mail.GonderilenMailID)
                    .Select(s => s.EkSayisi)
                    .FirstOrDefault();
            }

            // ViewBag'e combo box verilerini ekleme
            ViewBag.IsEkVar = new SelectList(GonderilenMaillerBus.GetCmbMailEkKontrol(true), "Value", "Caption", model.IsEkVar);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod);

            return View(model);
        }

        [HttpPost]
        public ActionResult Index(FmMailGondermeDto model)
        {
            // Stored procedure kullanarak arama yapma
            ObjectParameter totalCount = new ObjectParameter("TotalCount", typeof(int));

            List<sp_SearchMailsFullText_Result> results = _entities.sp_SearchMailsFullText(
                string.Join(",", UserIdentity.Current.EnstituKods),
                model.IsEkVar,
                !string.IsNullOrWhiteSpace(model.Konu) ? model.Konu : null,
                !string.IsNullOrWhiteSpace(model.EnstituKod) ? model.EnstituKod : null,
                model.Tarih,
                !string.IsNullOrWhiteSpace(model.MailGonderen) ? model.MailGonderen : null,
                model.StartRowIndex,
                model.PageSize,
                totalCount).ToList();

            model.RowCount = (int)totalCount.Value;
            model.MailGondermeDtos = results.Select(r => new FrMailGondermeDto
            {
                GonderilenMailID = r.GonderilenMailID.Value,
                Tarih = r.Tarih.Value,
                EnstituAdi = r.EnstituAd,
                Konu = r.Konu,
                MailGonderen = r.MailGonderen,
                Gonderildi = r.Gonderildi.Value,
                HataMesaji = r.HataMesaji
            }).ToList();

            // Ek sayıları ve alıcı sayılarını hesaplama
            var gonderilenMailIds = model.MailGondermeDtos.Select(s => s.GonderilenMailID).ToList();

            // Mail eklerini hesaplama
            var ekSayilari = _entities.GonderilenMailEkleris
                .Where(e => gonderilenMailIds.Contains(e.GonderilenMailID))
                .GroupBy(e => e.GonderilenMailID)
                .Select(g => new { GonderilenMailID = g.Key, EkSayisi = g.Count() })
                .ToDictionary(x => x.GonderilenMailID, x => x.EkSayisi);

            // Mail alıcılarını hesaplama
            var aliciSayilari = _entities.GonderilenMailKullanicilars
                .Where(a => gonderilenMailIds.Contains(a.GonderilenMailID))
                .GroupBy(a => a.GonderilenMailID)
                .Select(g => new { GonderilenMailID = g.Key, AliciSayisi = g.Count() })
                .ToDictionary(x => x.GonderilenMailID, x => x.AliciSayisi);

            // Her mail için ek ve alıcı sayılarını atama
            foreach (var mail in model.MailGondermeDtos)
            {
                mail.EkSayisi = ekSayilari.ContainsKey(mail.GonderilenMailID) ? ekSayilari[mail.GonderilenMailID] : 0;
                mail.KisiSayisi = aliciSayilari.ContainsKey(mail.GonderilenMailID) ? aliciSayilari[mail.GonderilenMailID] : 0;
            }

            ViewBag.IsEkVar = new SelectList(GonderilenMaillerBus.GetCmbMailEkKontrol(true), "Value", "Caption", model.IsEkVar);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod);

            return View(model);
        }

        public ActionResult MailDetay(int gonderilenMailId)
        {
            // Mail detaylarını getirme
            var mailDetay = (from mail in _entities.GonderilenMaillers
                             join enst in _entities.Enstitulers on mail.EnstituKod equals enst.EnstituKod into enstGroup
                             from enst in enstGroup.DefaultIfEmpty()
                             join k in _entities.Kullanicilars on mail.IslemYapanID equals k.KullaniciID
                             where mail.GonderilenMailID == gonderilenMailId
                             select new FrMailGondermeDto
                             {
                                 GonderilenMailID = mail.GonderilenMailID,
                                 EnstituAdi = enst != null ? enst.EnstituAd : "Sistem",
                                 Tarih = mail.Tarih,
                                 Konu = mail.Konu,
                                 Aciklama = mail.Aciklama,
                                 AciklamaHtml = mail.AciklamaHtml,
                                 MailGonderen = k.Ad + " " + k.Soyad,
                                 UserKey = k.UserKey,
                                 IslemYapanID = mail.IslemYapanID,
                                 IslemYapanIP = mail.IslemYapanIP,
                                 EkSayisi = mail.GonderilenMailEkleris.Count,
                                 KisiSayisi = mail.GonderilenMailKullanicilars.Count,
                                 GonderilenMailEkleris = mail.GonderilenMailEkleris.ToList()
                             }).First();

            // Mail alıcılarını getirme
            var alicilar = _entities.GonderilenMailKullanicilars
                .Where(s => s.GonderilenMailID == gonderilenMailId)
                .OrderBy(s => s.Kullanicilar.Ad)
                .ThenBy(s => s.Kullanicilar.Soyad)
                .Select(s => new MailKullaniciBilgi
                {
                    AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                    Email = s.Email
                })
                .ToList();

            ViewBag.DataK = alicilar;
            return View(mailDetay);
        }

        public ActionResult MailIstatitik()
        {
            FmMailIstatistikDto model = new FmMailIstatistikDto();
            model.AyId = DateTime.Now.Month;
            model.Yil = DateTime.Now.Year;
            return MailIstatitik(model);
        }

        [HttpPost]
        public ActionResult MailIstatitik(FmMailIstatistikDto model)
        {
            // Mail istatistiklerini getirme
            var query = _entities.GonderilenMaillers
                .Where(p => p.Tarih.Year == model.Yil &&
                           (!model.AyId.HasValue || p.Tarih.Month == model.AyId.Value));

            // İstatistikleri hesaplama
            var stats = query
                .Select(s => new { s.EnstituKod, s.Tarih })
                .ToList()
                .GroupBy(g => new
                {
                    Year = g.Tarih.Year,
                    Month = g.Tarih.Month,
                    Day = g.Tarih.Day
                })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Day = g.Key.Day,
                    FbeCount = g.Count(p => p.EnstituKod == "010"),
                    SbeCount = g.Count(p => p.EnstituKod == "020"),
                    TetCount = g.Count(p => p.EnstituKod == "030"),
                    ToplamCount = g.Count()
                })
                .AsQueryable();

            model.RowCount = stats.Count();
            model.ToplamCount = stats.Sum(s => s.ToplamCount);
            model.FbeCount = stats.Sum(s => s.FbeCount);
            model.SbeCount = stats.Sum(s => s.SbeCount);
            model.TetCount = stats.Sum(s => s.TetCount);

            // Sıralama
            var orderedStats = string.IsNullOrWhiteSpace(model.Sort)
                ? stats.OrderByDescending(o => o.Year)
                      .ThenByDescending(t => t.Month)
                      .ThenByDescending(o => o.Day)
                : stats.OrderBy(model.Sort);

            // İstatistik verilerini DTO'ya dönüştürme
            model.Data = orderedStats
                .Skip(model.StartRowIndex)
                .Take(model.PageSize)
                .ToList()
                .Select(s => new FrIstatistikDto
                {
                    Tarih = new DateTime(s.Year, s.Month, s.Day),
                    FbeCount = s.FbeCount,
                    SbeCount = s.SbeCount,
                    TetCount = s.TetCount,
                    ToplamCount = s.ToplamCount
                })
                .ToList();

            // Combo box verilerini getirme
            var cmbAylar = SrTalepleriBus.GetCmbAylar(true);
            var gonderilenMailYil = ComboData.GetCmbGonderilenMailYil();

            ViewBag.AyId = new SelectList(cmbAylar, "Value", "Caption", model.AyId);
            ViewBag.Yil = new SelectList(gonderilenMailYil, "Value", "Caption", model.Yil);

            return View(model);
        }

        public ActionResult Sil(int id)
        {
            // Maili silme (soft delete)
            var gonderilenMail = _entities.GonderilenMaillers.FirstOrDefault(p => p.GonderilenMailID == id);
            string mesaj = "";
            bool basarili = true;

            if (gonderilenMail != null)
            {
                try
                {
                    if (string.IsNullOrEmpty(mesaj))
                    {
                        gonderilenMail.Silindi = true;
                        _entities.SaveChanges();
                        mesaj = string.Concat("'", gonderilenMail.Konu, "' konulu email Silindi!");
                    }
                }
                catch (Exception ex)
                {
                    basarili = false;
                    mesaj = string.Concat("'", gonderilenMail.Konu, "' Konulu Mail Silinemedi! <br/> Bilgi:", ex.ToExceptionMessage());
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(mesaj, ex.ToExceptionStackTrace(), (byte)4);
                }
            }
            else
            {
                basarili = false;
                mesaj = "Silmek istediğiniz mail bilgisi sistemde bulunamadı!";
            }

            return Json(new { basarili, mesaj }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}