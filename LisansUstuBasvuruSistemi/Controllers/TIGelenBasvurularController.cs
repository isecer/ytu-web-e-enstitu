using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize(Roles = RoleNames.TIGelenBasvuru)]
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class TIGelenBasvurularController : Controller
    {
        // GET: TIGelenBasvurular
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string EKD)
        {

            var model = new fmTIBasvuru() { PageSize = 10 };

            //var DonemBilgi = DateTime.Now.ToAraRaporDonemBilgi();
            //if (RoleNames.TIGelenBasvuruKayit.InRoleCurrent())
            //{
            //    model.AktifTIAraRaporDonemID = DonemBilgi.BaslangicYil + "" + DonemBilgi.DonemID;
            //    model.Expand = true;
            //}
            return Index(model, EKD); ;
        }
        [HttpPost]
        public ActionResult Index(fmTIBasvuru model, string EKD)
        {

            var _EnstituKod = Management.getSelectedEnstitu(EKD);


            var nowDate = DateTime.Now;
            var KullaniciID = UserIdentity.Current.Id;
            var q = from s in db.TIBasvurus
                    join e in db.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in db.Kullanicilars on s.KullaniciID equals k.KullaniciID
                    join o in db.OgrenimTipleris on new { s.OgrenimTipKod, e.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod } 
                    join pr in db.Programlars on k.ProgramKod equals pr.ProgramKod
                    join ab in db.AnabilimDallaris on k.Programlar.AnabilimDaliKod equals ab.AnabilimDaliKod
                    join en in db.Enstitulers on e.EnstituKod equals en.EnstituKod
                    join ktip in db.KullaniciTipleris on k.KullaniciTipID equals ktip.KullaniciTipID
                    join ard in db.TIBasvuruAraRapors on s.AktifTIBasvuruAraRaporID equals ard.TIBasvuruAraRaporID into defard
                    from Ard in defard.DefaultIfEmpty()
                    where en.EnstituKod == _EnstituKod
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
                        TcPasaPortNo = s.Kullanicilar.TcKimlikNo != null ? s.Kullanicilar.TcKimlikNo : s.Kullanicilar.PasaportNo,
                        OgrenciNo = s.OgrenciNo,
                        Kullanicilar = s.Kullanicilar,
                        ResimAdi = s.Kullanicilar.ResimAdi,
                        KullaniciTipID = s.Kullanicilar.KullaniciTipID,
                        KullaniciTipAdi = ktip.KullaniciTipAdi,
                        OgrenimTipKod = s.OgrenimTipKod,
                        KayitOgretimYiliBaslangic = s.KayitOgretimYiliBaslangic,
                        KayitOgretimYiliDonemID = s.KayitOgretimYiliDonemID,
                        AktifTIBasvuruAraRaporID = s.AktifTIBasvuruAraRaporID,
                        TIAraRaporAktifDonemAdi = Ard == null ? "Rapor Girişi Yapılmadı" : (Ard.DonemBaslangicYil + " / " + (Ard.DonemBaslangicYil + 1) + " " + (Ard.DonemID == 1 ? "Güz" : "Bahar")),
                        TIAraRaporRaporDurumAdi = Ard == null ? "Rapor Girişi Yapılmadı" : Ard.TIBasvuruAraRaporDurumlari.TIBasvuruAraRaporDurumAdi,
                        AraRaporSayisi = Ard == null ? (int?)null : Ard.AraRaporSayisi,
                        TIAraRaporAktifDonemID = Ard == null ? null : (Ard.DonemBaslangicYil + "" + Ard.DonemID),
                        TIAraRaporRaporDurumID = Ard == null ? 0 : Ard.TIBasvuruAraRaporDurumID,
                        tIAraraporFiltreModels = s.TIBasvuruAraRapors.Select(s => new TIAraraporFiltreModel
                        {
                            AraRaporSayisi = s.AraRaporSayisi,
                            FormKodu = s.FormKodu,
                            KomiteUyeleri = s.TIBasvuruAraRaporKomites.Select(s2 => s2.AdSoyad).ToList(),
                            RaporDonemID = s.DonemBaslangicYil + "" + s.DonemID,
                            TIBasvuruAraRaporDurumID = s.TIBasvuruAraRaporDurumID
                        }).ToList(),

                        IsOyBirligiOrCouklugu = Ard != null ? Ard.IsOyBirligiOrCouklugu : (bool?)null,
                        IsBasariliOrBasarisiz = Ard != null ? Ard.IsBasariliOrBasarisiz : (bool?)null

                    };


            var q2 = q;
            q = q.Where(p => p.EnstituKod == _EnstituKod);
            if (!model.AktifTIAraRaporDonemID.IsNullOrWhiteSpace()) q = q.Where(p => p.TIAraRaporAktifDonemID == model.AktifTIAraRaporDonemID);
            if (model.AktifTIAraRaporRaporDurumID.HasValue) q = q.Where(p => p.TIAraRaporRaporDurumID == model.AktifTIAraRaporRaporDurumID);
            if (model.AktifAraRaporSayisi.HasValue) q = q.Where(p => p.AraRaporSayisi == model.AktifAraRaporSayisi);

            if (!model.TIAraRaporDonemID.IsNullOrWhiteSpace()) q = q.Where(p => p.tIAraraporFiltreModels.Any(a => a.RaporDonemID == model.TIAraRaporDonemID));
            if (model.TIAraRaporRaporDurumID.HasValue) q = q.Where(p => p.tIAraraporFiltreModels.Any(a => a.TIBasvuruAraRaporDurumID == model.TIAraRaporRaporDurumID.Value));
            if (model.TIAraRaporSayisi.HasValue) q = q.Where(p => p.tIAraraporFiltreModels.Any(a => a.AraRaporSayisi == model.TIAraRaporSayisi.Value));

            if (!model.AdSoyad.IsNullOrWhiteSpace()) q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.OgrenciNo == model.AdSoyad || p.TcPasaPortNo == model.AdSoyad || p.KullaniciTipAdi.Contains(model.AdSoyad) || p.tIAraraporFiltreModels.Any(a => a.FormKodu == model.AdSoyad || a.KomiteUyeleri.Contains(model.AdSoyad)));

            var TezDegerlendirme = RoleNames.TITezDegerlendirmeYap.InRoleCurrent();
            var MBGelenBKayitYetki = RoleNames.TIGelenBasvuruKayit.InRoleCurrent();
            if (TezDegerlendirme && !MBGelenBKayitYetki)
            {
                q = q.Where(p => p.TezDanismanID == UserIdentity.Current.Id);
            }
            bool isFiltered = false;
            if (q != q2)
                isFiltered = true;
            model.RowCount = q.Count();
            var IndexModel = new MIndexBilgi();
            //IndexModel.Toplam = model.RowCount;
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.BasvuruTarihi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            var qdata = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToList();
            if (isFiltered)
            {
                ViewBag.kIds = q.Select(s => s.KullaniciID).ToList();
            }
            else ViewBag.kIds = new List<int>();
            ViewBag.AktifTIAraRaporDonemID = new SelectList(Management.cmbTIAktifDonemListe(true), "Value", "Caption", model.AktifTIAraRaporDonemID);
            ViewBag.TIAraRaporDonemID = new SelectList(Management.cmbTIAktifDonemListe(true), "Value", "Caption", model.TIAraRaporDonemID);
            ViewBag.AktifTIAraRaporRaporDurumID = new SelectList(Management.cmbTIAraRaporDurumListe(true), "Value", "Caption", model.AktifTIAraRaporRaporDurumID);
            ViewBag.TIAraRaporRaporDurumID = new SelectList(Management.cmbTIAraRaporDurumListe(true), "Value", "Caption", model.TIAraRaporRaporDurumID);
            ViewBag.AktifAraRaporSayisi = new SelectList(Management.cmbAraRaporSayisi(true), "Value", "Caption", model.AktifAraRaporSayisi);
            ViewBag.TIAraRaporSayisi = new SelectList(Management.cmbAraRaporSayisi(true), "Value", "Caption", model.TIAraRaporSayisi);


            model.Data = qdata;
            ViewBag.IndexModel = IndexModel;
            return View(model);
        }

    }
}