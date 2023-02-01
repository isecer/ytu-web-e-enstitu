using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using System;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]

    public class KDuyurularController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        static readonly object lockObject = new object();
        public ActionResult Index(string EKD)
        {

            return Index(new FmDuyurularDto() { PageSize = 10 }, EKD);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(FmDuyurularDto model, string EKD)
        {
            var q = from s in db.Duyurulars
                    join e in db.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                    join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where s.IsAktif && (s.YayinSonTarih.HasValue ? s.YayinSonTarih.Value >= DateTime.Now : 1 == 1) && e.EnstituKisaAd.Contains(EKD) 
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
                        Ekler = s.DuyuruEkleris
                    };

            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod); 
            if (!model.Baslik.IsNullOrWhiteSpace()) q = q.Where(p => p.Baslik.Contains(model.Baslik));
            if (!model.Aciklama.IsNullOrWhiteSpace()) q = q.Where(p => p.Aciklama.Contains(model.Aciklama));

            if (model.Tarih.HasValue)
            {
                var t1 = model.Tarih.Value.TodateToShortDate();
                var t2 = Convert.ToDateTime(model.Tarih.Value.ToShortDateString() + " 23:59:59");
                q = q.Where(p => p.Tarih >= t1 && p.Tarih <= t2);

            }
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.Tarih);
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
                DuyuruEkleris = s.Ekler
            }).ToList();
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod); 
            return View(model);
        }


        public ActionResult getDuyuruJson(int PopupTipID, string EKD)
        {

            
            string _EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            var fModel = new FmDuyurularDto(); 
            fModel.EnstituKod = _EnstituKod;
            var q = from s in db.Duyurulars
                    join e in db.Enstitulers on new {  s.EnstituKod } equals new {  e.EnstituKod }
                    join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where s.IsAktif && s.Tarih <= DateTime.Now && (s.YayinSonTarih.HasValue ? s.YayinSonTarih.Value >= DateTime.Now : 1 == 1) && e.EnstituKod == _EnstituKod  
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
                        s.AnaSayfaPopupAc,
                        s.BasvuruPopupAc,
                        s.TDOBasvuruPopupAc,
                        s.TIBasvuruPopupAc,
                        s.MezuniyetBasvuruPopupAc,
                        s.TalepYaparkenPopupAc,
                        s.YayinSonTarih,
                        s.IsAktif
                    };
            if (PopupTipID == DuyuruPopupTipleri.AnaSayfa) q = q.Where(p => p.AnaSayfaPopupAc);
            else if (PopupTipID == DuyuruPopupTipleri.LisansustuBasvuru) q = q.Where(p => p.BasvuruPopupAc);
            else if (PopupTipID == DuyuruPopupTipleri.TalepYap) q = q.Where(p => p.TalepYaparkenPopupAc);
            else if (PopupTipID == DuyuruPopupTipleri.TIBasvuru) q = q.Where(p => p.TIBasvuruPopupAc);
            else if (PopupTipID == DuyuruPopupTipleri.TDOBasvuru) q = q.Where(p => p.TDOBasvuruPopupAc);
            else q = q.Where(p => p.MezuniyetBasvuruPopupAc);
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
                AnaSayfadaGozuksun = s.AnaSayfadaGozuksun,
                AnaSayfaPopupAc = s.AnaSayfaPopupAc,
                BasvuruPopupAc = s.BasvuruPopupAc,
                MezuniyetBasvuruPopupAc = s.MezuniyetBasvuruPopupAc,
                TalepYaparkenPopupAc = s.TalepYaparkenPopupAc,
                YayinSonTarih = s.YayinSonTarih
            }).OrderByDescending(o => o.Tarih).ToList();

            string htmlDuyuru = ViewRenderHelper.RenderPartialView("KDuyurular", "DuyuruHtml", fModel);
            return Json(new { ShowMessage = fModel.DuyurularDtos.Count() > 0, HtmlMessage = htmlDuyuru });
        }
        public ActionResult DuyuruHtml(FmDuyurularDto model)
        {

            return View(model);
        }

    }
}
