using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize(Roles = RoleNames.TiGelenBasvuru)]
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class TiGelenBasvurularController : Controller
    {
        // GET: TIGelenBasvurular
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {

            var model = new fmTIBasvuru() { PageSize = 50 };
            //var DonemBilgi = DateTime.Now.ToAraRaporDonemBilgi();
            //if (RoleNames.TIGelenBasvuruKayit.InRoleCurrent())
            //{
            //    model.AktifTIAraRaporDonemID = DonemBilgi.BaslangicYil + "" + DonemBilgi.DonemID;
            //    model.Expand = true;
            //}
            return Index(model, ekd); ;
        }
        [HttpPost]
        public ActionResult Index(fmTIBasvuru model, string ekd)
        {

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);


            var nowDate = DateTime.Now;
            var KullaniciID = UserIdentity.Current.Id;
            var q = from s in _entities.TIBasvurus
                    join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in _entities.Kullanicilars on s.KullaniciID equals k.KullaniciID
                    join o in _entities.OgrenimTipleris on new { s.OgrenimTipKod, e.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod }
                    join pr in _entities.Programlars on k.ProgramKod equals pr.ProgramKod
                    join ab in _entities.AnabilimDallaris on k.Programlar.AnabilimDaliKod equals ab.AnabilimDaliKod
                    join en in _entities.Enstitulers on e.EnstituKod equals en.EnstituKod
                    join ktip in _entities.KullaniciTipleris on k.KullaniciTipID equals ktip.KullaniciTipID
                    join ard in _entities.TIBasvuruAraRapors on s.AktifTIBasvuruAraRaporID equals ard.TIBasvuruAraRaporID into defard
                    from ard in defard.DefaultIfEmpty()
                    where en.EnstituKod == enstituKod
                    select new frTIBasvuru
                    {
                        TIBasvuruID = s.TIBasvuruID,
                        TezDanismanID = s.TezDanismanID,
                        BasvuruTarihi = s.BasvuruTarihi,
                        EnstituKod = en.EnstituKod,
                        EnstituAdi = en.EnstituAd,
                        OgrenimTipAdi = o.OgrenimTipAdi,
                        AnabilimdaliAdi = ab.AnabilimDaliAdi,
                        ProgramAdi = pr.ProgramAdi,
                        KullaniciID = s.KullaniciID,
                        AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                        TcKimlikNo = s.Kullanicilar.TcKimlikNo,
                        OgrenciNo = s.OgrenciNo,
                        Kullanicilar = s.Kullanicilar,
                        ResimAdi = s.Kullanicilar.ResimAdi,
                        KullaniciTipID = s.Kullanicilar.KullaniciTipID,
                        KullaniciTipAdi = ktip.KullaniciTipAdi,
                        OgrenimTipKod = s.OgrenimTipKod,
                        KayitOgretimYiliBaslangic = s.KayitOgretimYiliBaslangic,
                        KayitOgretimYiliDonemID = s.KayitOgretimYiliDonemID,
                        AktifTIBasvuruAraRaporID = s.AktifTIBasvuruAraRaporID,
                        TIAraRaporAktifDonemAdi = ard == null ? "Rapor Girişi Yapılmadı" : (ard.DonemBaslangicYil + " / " + (ard.DonemBaslangicYil + 1) + " " + (ard.DonemID == 1 ? "Güz" : "Bahar")),
                        TIAraRaporRaporDurumAdi = ard == null ? "Rapor Girişi Yapılmadı" : ard.TIBasvuruAraRaporDurumlari.TIBasvuruAraRaporDurumAdi,
                        AraRaporSayisi = ard != null ? ard.AraRaporSayisi : (int?)null,
                        TIAraRaporAktifDonemID = ard == null ? null : (ard.DonemBaslangicYil + "" + ard.DonemID),
                        TIAraRaporRaporDurumID = ard != null ? ard.TIBasvuruAraRaporDurumID : (int?)null,
                        tIAraraporFiltreModels = s.TIBasvuruAraRapors.Select(ti => new TIAraraporFiltreModel
                        {
                            AraRaporSayisi = ti.AraRaporSayisi,
                            FormKodu = ti.FormKodu,
                            KomiteUyeleri = ti.TIBasvuruAraRaporKomites.Select(s2 => s2.AdSoyad).ToList(),
                            RaporDonemID = ti.DonemBaslangicYil + "" + ti.DonemID,
                            TIBasvuruAraRaporDurumID = ti.TIBasvuruAraRaporDurumID
                        }).ToList(),

                        IsOyBirligiOrCouklugu = ard != null ? ard.IsOyBirligiOrCouklugu : (bool?)null,
                        IsBasariliOrBasarisiz = ard != null ? ard.IsBasariliOrBasarisiz : (bool?)null

                    };


            var q2 = q;
            q = q.Where(p => p.EnstituKod == enstituKod);
            if (!model.AktifTIAraRaporDonemID.IsNullOrWhiteSpace()) q = q.Where(p => p.TIAraRaporAktifDonemID == model.AktifTIAraRaporDonemID);
            if (model.AktifTIAraRaporRaporDurumID.HasValue) q = q.Where(p => p.TIAraRaporRaporDurumID == model.AktifTIAraRaporRaporDurumID);
            if (model.AktifAraRaporSayisi.HasValue) q = q.Where(p => p.AraRaporSayisi == model.AktifAraRaporSayisi);

            if (!model.TIAraRaporDonemID.IsNullOrWhiteSpace()) q = q.Where(p => p.tIAraraporFiltreModels.Any(a => a.RaporDonemID == model.TIAraRaporDonemID));
            if (model.TIAraRaporRaporDurumID.HasValue) q = q.Where(p => p.tIAraraporFiltreModels.Any(a => a.TIBasvuruAraRaporDurumID == model.TIAraRaporRaporDurumID.Value));
            if (model.TIAraRaporSayisi.HasValue) q = q.Where(p => p.tIAraraporFiltreModels.Any(a => a.AraRaporSayisi == model.TIAraRaporSayisi.Value));

            if (!model.AdSoyad.IsNullOrWhiteSpace()) q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.OgrenciNo == model.AdSoyad || p.TcKimlikNo == model.AdSoyad || p.KullaniciTipAdi.Contains(model.AdSoyad) || p.tIAraraporFiltreModels.Any(a => a.FormKodu == model.AdSoyad || a.KomiteUyeleri.Contains(model.AdSoyad)));

            var tezDegerlendirme = RoleNames.TiTezDegerlendirmeYap.InRoleCurrent();
            var mbGelenBKayitYetki = RoleNames.TiGelenBasvuruKayit.InRoleCurrent();
            if (tezDegerlendirme && !mbGelenBKayitYetki)
            {
                q = q.Where(p => p.TezDanismanID == UserIdentity.Current.Id);
            }
            var isFiltered = !Equals(q, q2);
            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi();
            //IndexModel.Toplam = model.RowCount;
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi); 
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.kIds = isFiltered ? q.Select(s => s.KullaniciID).ToList() : new List<int>();
            ViewBag.AktifTIAraRaporDonemID = new SelectList(TezIzlemeBus.CmbTiAktifDonemListe(true), "Value", "Caption", model.AktifTIAraRaporDonemID);
            ViewBag.TIAraRaporDonemID = new SelectList(TezIzlemeBus.CmbTiAktifDonemListe(true), "Value", "Caption", model.TIAraRaporDonemID);
            ViewBag.AktifTIAraRaporRaporDurumID = new SelectList(TezIzlemeBus.CmbTiAraRaporDurumListe(true), "Value", "Caption", model.AktifTIAraRaporRaporDurumID);
            ViewBag.TIAraRaporRaporDurumID = new SelectList(TezIzlemeBus.CmbTiAraRaporDurumListe(true), "Value", "Caption", model.TIAraRaporRaporDurumID);
            ViewBag.AktifAraRaporSayisi = new SelectList(TezIzlemeBus.CmbAraRaporSayisi(true), "Value", "Caption", model.AktifAraRaporSayisi);
            ViewBag.TIAraRaporSayisi = new SelectList(TezIzlemeBus.CmbAraRaporSayisi(true), "Value", "Caption", model.TIAraRaporSayisi); 
            ViewBag.IndexModel = indexModel;
            return View(model);
        }

    }
}