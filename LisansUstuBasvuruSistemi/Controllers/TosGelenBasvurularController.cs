using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models.ObsService;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class TosGelenBasvurularController : Controller
    {
        // GET: TosBasvuru
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd, Guid? uniqueId, int? kullaniciId, Guid? isDegerlendirme = null)
        {
            if (!UserIdentity.Current.IsAuthenticated && isDegerlendirme == null) return RedirectToActionPermanent("Login", "Account");

            return Index(new FmTosBasvuru() { UniqueId = uniqueId, KullaniciID = kullaniciId, IsDegerlendirme = isDegerlendirme, PageSize = 10 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmTosBasvuru model, string ekd)
        {
            int? baslangicYil = null;
            int? donemId = null;
            if (!model.AktifDonemID.IsNullOrWhiteSpace())
            {
                baslangicYil = model.AktifDonemID.Substring(0, 4).ToInt(0);
                donemId = model.AktifDonemID.Substring(4, 1).ToInt(0);
            }
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
             
            var q = from s in _entities.ToBasvurus.Where(p => !model.IsDegerlendirme.HasValue || p.ToBasvuruSavunmas.Any(a => a.ToBasvuruSavunmaKomites.Any(a2 => a2.UniqueID == model.IsDegerlendirme)))
                    join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in _entities.Kullanicilars on s.KullaniciID equals k.KullaniciID
                    join o in _entities.OgrenimTipleris on new { s.OgrenimTipKod, e.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod }
                    join pr in _entities.Programlars on k.ProgramKod equals pr.ProgramKod
                    join ab in _entities.AnabilimDallaris on k.Programlar.AnabilimDaliKod equals ab.AnabilimDaliKod
                    join en in _entities.Enstitulers on e.EnstituKod equals en.EnstituKod
                    let ard = _entities.ToBasvuruSavunmas.Where(p => p.ToBasvuruID == s.ToBasvuruID).OrderByDescending(ot => ot.ToBasvuruSavunmaID).FirstOrDefault()
                     where s.EnstituKod == enstituKod 
                    select new FrTosBasvuru()
                    {
                        UniqueID = s.UniqueID,
                        ToBasvuruID = s.ToBasvuruID,
                        TezDanismanID = s.TezDanismanID,
                        BasvuruTarihi = s.BasvuruTarihi,
                        EnstituKod = en.EnstituKod,
                        EnstituAdi = en.EnstituAd,
                        OgrenimTipAdi = o.OgrenimTipAdi,
                        AnabilimdaliAdi = ab.AnabilimDaliAdi,
                        ProgramAdi = pr.ProgramAdi,
                        KullaniciID = s.KullaniciID,
                        AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                        OgrenciNo = s.OgrenciNo,
                        Kullanicilar = s.Kullanicilar,
                        ResimAdi = s.Kullanicilar.ResimAdi,
                        OgrenimTipKod = s.OgrenimTipKod,
                        KayitOgretimYiliBaslangic = s.KayitOgretimYiliBaslangic,
                        KayitOgretimYiliDonemID = s.KayitOgretimYiliDonemID,
                        AktifSavunmaNo = ard != null ? ard.SavunmaNo : (int?)null,
                        AktifDonemAdi = ard == null ? "Tez Öneri Savunma Sınavı Yok" : (ard.DonemBaslangicYil + " / " + (ard.DonemBaslangicYil + 1) + " " + (ard.DonemID == 1 ? "Güz" : "Bahar")),
                        AktifDonemID = ard == null ? null : (ard.DonemBaslangicYil + "" + ard.DonemID),
                        DurumID = ard == null ? 0 : ard.ToBasvuruSavunmaDurumID,
                        IsOyBirligiOrCoklugu = ard != null ? ard.IsOyBirligiOrCoklugu : (bool?)null,
                        DurumModel = new TosDurumDto
                        {
                            ToBasvuruSavunmaDurumID = ard.ToBasvuruSavunmaDurumID,
                            IsSrTalebiYapildi = ard != null && ard.SRTalepleris.Any(),
                            DegerlendirmeBasladi = ard != null && ard.ToBasvuruSavunmaKomites.Any(a => a.ToBasvuruSavunmaDurumID.HasValue),
                            IsOyBirligiOrCoklugu = ard.IsOyBirligiOrCoklugu
                        },
                    };


            var q2 = q;
            var isFiltered = !Equals(q, q2);
            //if (baslangicYil.HasValue) q = q.Where(p => p.AraRaporDanismanID.HasValue);
            if (!model.AktifDonemID.IsNullOrWhiteSpace()) q = q.Where(p => p.AktifDonemID == model.AktifDonemID);
           
            // if (model.AktifAraRaporSayisi.HasValue) q = q.Where(p => p.AraRaporSayisi == model.AktifAraRaporSayisi);
            if (model.AnabilimDaliID.HasValue) q = q.Where(p => p.AnabilimDaliID == model.AnabilimDaliID);

            if (!model.AdSoyad.IsNullOrWhiteSpace())
                q = q.Where(p =>
                    p.AdSoyad.Contains(model.AdSoyad)
                    || p.OgrenciNo.Contains(model.AdSoyad)
                    || p.TcKimlikNo.Contains(model.AdSoyad));

            var tezDegerlendirme = RoleNames.TiTezDegerlendirmeYap.InRoleCurrent();
            var mbGelenBKayitYetki = RoleNames.TiGelenBasvuruKayit.InRoleCurrent();
            //if (tezDegerlendirme && !mbGelenBKayitYetki)
            //{
            //    q = q.Where(p => p.TezDanismanID == UserIdentity.Current.Id);
            //    q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.TIAraRaporRaporDurumID ?? 999).ThenByDescending(o => o.RaporTarihi ?? o.BasvuruTarihi);
            //}
            //else
            //{
            //    q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.RaporTarihi ?? o.BasvuruTarihi);
            //}

            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.filteredOgrenciIds = isFiltered && !model.AktifDonemID.IsNullOrWhiteSpace() ? q.Select(s => s.KullaniciID).ToList() : new List<int>();
            ViewBag.filteredDanismanIds = isFiltered && !model.AktifDonemID.IsNullOrWhiteSpace() ? q.Where(p => p.DanismanID.HasValue).Select(s => s.DanismanID.Value).Distinct().ToList() : new List<int>();

            ViewBag.AktifDonemID = new SelectList(TezIzlemeBus.CmbTiDonemListe(enstituKod, true), "Value", "Caption", model.AktifDonemID);
            ViewBag.AktifDurumID = new SelectList(TezIzlemeBus.CmbTiAraRaporDurumListe(true), "Value", "Caption", model.AktifDurumID); 
            ViewBag.AnabilimDaliID = new SelectList(TezIzlemeBus.GetCmbFilterTiAnabilimDallari(enstituKod, true), "Value", "Caption", model.AnabilimDaliID);
            ViewBag.SavunmaNo = new SelectList(TezIzlemeBus.CmbAraRaporSayisi(true), "Value", "Caption", model.SavunmaNo); 

            ViewBag.IndexModel = indexModel; 
            return View(model);
        }
 

    }
}