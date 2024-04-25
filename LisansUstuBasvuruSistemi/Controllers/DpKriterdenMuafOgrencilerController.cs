using System;
using System.Linq;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Controllers
{

    [Authorize(Roles = RoleNames.DonemProjesiKriterdenMuafOgrenciler)]
    public class DpKriterdenMuafOgrencilerController : Controller
    {
        // GET: DonemProjesiKriterdenMuafOgrenciler
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        public ActionResult Index(string ekd)
        {
            return Index(new FmDonemProjesiMuafOgrenciler(), ekd);
        }
        [HttpPost]
        public ActionResult Index(FmDonemProjesiMuafOgrenciler model, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var q = from donemProjesiMuafOgrenci in _entities.DonemProjesiMuafOgrencilers
                    join ogrenci in _entities.Kullanicilars on donemProjesiMuafOgrenci.KullaniciID equals ogrenci.KullaniciID
                    where donemProjesiMuafOgrenci.EnstituKod == enstituKod
                    select new FrDonemProjesiMuafOgrenciler
                    {
                        DonemProjesiMuafOgrenciID = donemProjesiMuafOgrenci.DonemProjesiMuafOgrenciID,
                        EnstituKod = donemProjesiMuafOgrenci.EnstituKod,
                        KullaniciID = donemProjesiMuafOgrenci.KullaniciID,
                        AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                        OgrenciNo = donemProjesiMuafOgrenci.OgrenciNo,
                        IslemTarihi = donemProjesiMuafOgrenci.IslemTarihi
                    };

            if (model.AdSoyad.IsNullOrWhiteSpace() == false) q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.OgrenciNo == model.AdSoyad);
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.IslemTarihi);
            model.DonemProjesiMuafOgrencilers = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();

            return View(model);
        }

        public ActionResult Sil(int id)
        {
            var kayit = _entities.DonemProjesiMuafOgrencilers.FirstOrDefault(p => p.DonemProjesiMuafOgrenciID == id);
            string message;
            var success = true;
            if (kayit != null)
            {

                try
                {
                    message = $"'{kayit.Kullanicilar.Ad} {kayit.Kullanicilar.Soyad}' İsimli ve '{kayit.OgrenciNo}' numaralı öğrenci kriter muaf bilgisi silindi!";
                    _entities.DonemProjesiMuafOgrencilers.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = $"'{kayit.Kullanicilar.Ad} {kayit.Kullanicilar.Soyad}' İsimli ve '{kayit.OgrenciNo}' numaralı öğrenci kriter muaf bilgisi silinemedi!";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Kriterden Muaf Öğrenci sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult KriterMuafOgrenciler()
        {
            return View();
        }
        public ActionResult KriterMuafOgrenciEkle(string ekd, int? ogrenciId)
        {
            var success = false;
            var message = "";
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            if (!ogrenciId.HasValue)
            {
                message = "Öğrenci seçiniz.";
            }
            else
            {
                var ogrenci = _entities.Kullanicilars.First(f => f.KullaniciID == ogrenciId);
                if (_entities.DonemProjesiMuafOgrencilers.Any(p => p.EnstituKod == enstituKod && p.KullaniciID == ogrenciId.Value && p.OgrenciNo == ogrenci.OgrenciNo))
                {
                    message = $"{ogrenci.OgrenciNo} numaralı {ogrenci.Ad} {ogrenci.Soyad} isimli öğrenci kriterden muaf öğrenci listesine daha önceden zaten eklendi."; 
                }
                else
                {
                    _entities.DonemProjesiMuafOgrencilers.Add(new DonemProjesiMuafOgrenciler()
                    {
                        EnstituKod = enstituKod,
                        KullaniciID = ogrenci.KullaniciID,
                        OgrenciNo = ogrenci.OgrenciNo,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip
                    });
                    _entities.SaveChanges();
                    message = $"{ogrenci.OgrenciNo} numaralı {ogrenci.Ad} {ogrenci.Soyad} isimli öğrenci kriterden muaf öğrenci listesine eklendi.";
                    success = true;
                }
            }
            return new { success, message }.ToJsonResult();

        }
        public ActionResult KriterMuafOgrenciSil(int donemProjesiMuafOgrenciId)
        {

            if (_entities.DonemProjesiMuafOgrencilers.Any(p => p.DonemProjesiMuafOgrenciID == donemProjesiMuafOgrenciId))
            {
                var ogrenci = _entities.DonemProjesiMuafOgrencilers.First(p =>
                    p.KullaniciID == donemProjesiMuafOgrenciId);
                _entities.DonemProjesiMuafOgrencilers.Remove(ogrenci);
                _entities.SaveChanges();
            }

            return true.ToJsonResult();
        }
        public ActionResult GetFilterKullanici(string term, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            return KullanicilarBus.GetFilterOgrenciJsonResult(term, enstituKod);
        }
        protected override void Dispose(bool disposing)
        {
            _entities.Dispose();
            base.Dispose(disposing);
        }

    }
}