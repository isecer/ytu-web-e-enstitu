using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using System;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Filters;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]

    public class KDuyurularController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();

        public ActionResult Index(string ekd)
        {
            return Index(new FmDuyurularDto() { PageSize = 10 }, ekd);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(FmDuyurularDto model, string ekd)
        {
            var q = from s in _entities.Duyurulars
                    join e in _entities.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where s.IsAktif && (!s.YayinSonTarih.HasValue || s.YayinSonTarih.Value >= DateTime.Now) && e.EnstituKisaAd.Contains(ekd)
                    select new
                    {
                        s.EnstituKod,
                        e.EnstituAd,
                        s.DuyuruID,
                        s.Tarih,
                        s.Baslik,
                        s.Aciklama,
                        s.AciklamaHtml,
                        DuyuruYapan = k.Ad + " " + k.Soyad,
                        s.IslemYapanIP,
                        EkSayisi = s.DuyuruEkleris.Count,
                        Ekler = s.DuyuruEkleris,
                        s.IsEnUsteSabitle,
                    };

            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.Baslik.IsNullOrWhiteSpace()) q = q.Where(p => p.Baslik.Contains(model.Baslik));
            if (!model.Aciklama.IsNullOrWhiteSpace()) q = q.Where(p => p.Aciklama.Contains(model.Aciklama));

            if (model.Tarih.HasValue)
            {
                var t1 = model.Tarih.Value.Date;
                var t2 = Convert.ToDateTime(model.Tarih.Value.ToShortDateString() + " 23:59:59");
                q = q.Where(p => p.Tarih >= t1 && p.Tarih <= t2);

            }
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.IsEnUsteSabitle ? 1 : 2).ThenByDescending(o => o.Tarih);
            model.DuyurularDtos = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new FrDuyurularDto
            {
                EnstituAdi = s.EnstituAd,
                EnstituKod = s.EnstituKod,
                DuyuruID = s.DuyuruID,
                Baslik = s.Baslik,
                Aciklama = s.Aciklama,
                AciklamaHtml = s.AciklamaHtml,
                Tarih = s.Tarih,
                DuyuruYapan = s.DuyuruYapan,
                IslemYapanIP = s.IslemYapanIP,
                EkSayisi = s.EkSayisi,
                DuyuruEkleris = s.Ekler,
                IsEnUsteSabitle = s.IsEnUsteSabitle
            }).ToList();
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            return View(model);
        }


        public ActionResult GetDuyuruJson(int? popupTipId, string ekd)
        {
            if (!popupTipId.HasValue || ekd.IsNullOrWhiteSpace())
                return Json(new { ShowMessage = false, HtmlMessage = "" });

            string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var fModel = new FmDuyurularDto
            {
                EnstituKod = enstituKod
            };
            var q = from s in _entities.Duyurulars
                    join e in _entities.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where s.IsAktif && s.Tarih <= DateTime.Now && (!s.YayinSonTarih.HasValue || s.YayinSonTarih.Value >= DateTime.Now) && e.EnstituKod == enstituKod
                    select new
                    {
                        s.EnstituKod,
                        e.EnstituAd,
                        s.DuyuruID,
                        s.Tarih,
                        s.Baslik,
                        s.Aciklama,
                        s.AciklamaHtml,
                        DuyuruYapan = k.Ad + " " + k.Soyad,
                        s.IslemYapanIP,
                        EkSayisi = s.DuyuruEkleris.Count,
                        Ekler = s.DuyuruEkleris,
                        s.AnaSayfadaGozuksun,
                        s.DuyuruPopuplars,
                        s.IsEnUsteSabitle,
                        s.YayinSonTarih,
                        s.IslemTarihi,
                        s.IsAktif
                    };
            q = q.Where(p => p.DuyuruPopuplars.Any(a => a.DuyuruPopupTipID == popupTipId.Value));
            fModel.DuyurularDtos = q.Select(s => new FrDuyurularDto
            {
                EnstituAdi = s.EnstituAd,
                EnstituKod = s.EnstituKod,
                DuyuruID = s.DuyuruID,
                Baslik = s.Baslik,
                Aciklama = s.Aciklama,
                AciklamaHtml = s.AciklamaHtml,
                Tarih = s.Tarih,
                DuyuruYapan = s.DuyuruYapan,
                IslemYapanIP = s.IslemYapanIP,
                EkSayisi = s.EkSayisi,
                DuyuruEkleris = s.Ekler,
                YayinSonTarih = s.YayinSonTarih,
                IslemTarihi = s.IslemTarihi,
                IsEnUsteSabitle = s.IsEnUsteSabitle,
            }).OrderBy(o => o.IsEnUsteSabitle ? 1 : 2).ThenByDescending(o => o.Tarih).ToList();
            var duyuruKey = string.Join("_", fModel.DuyurularDtos.Select(s => s.DuyuruID + " " + s.IslemTarihi).ToList());
            string htmlDuyuru = ViewRenderHelper.RenderPartialView("KDuyurular", "DuyuruHtml", fModel);
            return Json(new { ShowMessage = fModel.DuyurularDtos.Any(), duyuruKey, HtmlMessage = htmlDuyuru });
        }
        public ActionResult DuyuruHtml(FmDuyurularDto model)
        {
            return View(model);
        }

    }
}
