using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.SistemBilgilendirme)]
    public class SistemBilgilendirmeController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmSistemBilgilendirme());
        }
        [HttpPost]
        public ActionResult Index(FmSistemBilgilendirme model)
        {
            var q = from s in db.SistemBilgilendirmes
                    join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID into defK
                    from kd in defK.DefaultIfEmpty()
                    select new
                    {
                        s.SistemBilgiID,
                        s.BilgiTipi,
                        s.Kategori,
                        s.Message,
                        s.StackTrace,
                        s.IslemYapanID,
                        AdSoyad = s.IslemYapanID.HasValue ? (kd.Ad + " " + kd.Soyad) : (string)null,
                        KullaniciAdi = s.IslemYapanID.HasValue ? "[" + kd.KullaniciAdi + "]" : (string)null,
                        s.IslemTarihi,
                        s.IslemYapanIP
                    };

            if (model.IslemZamani.HasValue)
            {
                var mintar = model.IslemZamani.TodateToShortDate().Value;
                var maxtar = model.IslemZamani.Value.TodateToShortDate().AddDays(1);
                q = q.Where(p => p.IslemTarihi >= mintar && p.IslemTarihi < maxtar);

            }
            if (!model.Message.IsNullOrWhiteSpace()) q = q.Where(p => p.Message.Contains(model.Message));
            if (!model.AdSoyad.IsNullOrWhiteSpace()) q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.KullaniciAdi.Contains(model.AdSoyad) || p.IslemYapanIP.Contains(model.AdSoyad));
            if (model.BilgiTipi.HasValue) q = q.Where(p => p.BilgiTipi == model.BilgiTipi);

            model.RowCount = q.Count(); 
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.IslemTarihi); 
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount
            }; 
            model.FrSistemBilgilendirmes = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new FrSistemBilgilendirme
            {
                SistemBilgiID = s.SistemBilgiID,
                BilgiTipi = s.BilgiTipi,
                Kategori = s.Kategori,
                Message = s.Message,
                StackTrace = s.StackTrace,
                KullaniciAdi = s.KullaniciAdi,
                AdSoyad = s.AdSoyad,
                IslemYapanID = s.IslemYapanID,
                IslemZamani = s.IslemTarihi,
                IpAdresi = s.IslemYapanIP
            }).ToArray();
            var btip = new LogTypeData();
            ViewBag.BilgiTipi = new SelectList(btip.LogTipiData.Select(s => new { s.BilgiTipID, s.BilgiTipAdi }).ToList(), "BilgiTipID", "BilgiTipAdi", model.BilgiTipi);
            ViewBag.IndexModel = indexModel;
            return View(model);
        }
      

    }
}
