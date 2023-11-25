using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

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
                    join pr in _entities.Programlars on s.ProgramKod equals pr.ProgramKod
                    join ab in _entities.AnabilimDallaris on s.Programlar.AnabilimDaliKod equals ab.AnabilimDaliKod
                    let ard = _entities.ToBasvuruSavunmas.Where(p => (!baslangicYil.HasValue || (p.ToBasvuruID == s.ToBasvuruID && p.DonemID == donemId && p.DonemBaslangicYil == baslangicYil)) && p.ToBasvuruID == s.ToBasvuruID).OrderByDescending(ot => ot.ToBasvuruSavunmaID).FirstOrDefault()
                    select new FrTosBasvuru()
                    {
                        UniqueID = s.UniqueID,
                        ToBasvuruID = s.ToBasvuruID,
                        TezDanismanID = s.TezDanismanID,
                        BasvuruTarihi = s.BasvuruTarihi,
                        EnstituKod = e.EnstituKod,
                        EnstituAdi = e.EnstituAd,
                        OgrenimTipAdi = o.OgrenimTipAdi,
                        AnabilimDaliID = ab.AnabilimDaliID,
                        AnabilimdaliAdi = ab.AnabilimDaliAdi,
                        ProgramAdi = pr.ProgramAdi,
                        KullaniciID = s.KullaniciID,
                        UserKey = k.UserKey,
                        AdSoyad = k.Ad + " " + k.Soyad,
                        OgrenciNo = s.OgrenciNo,
                        TcKimlikNo = k.TcKimlikNo,
                        ResimAdi = k.ResimAdi,
                        OgrenimTipKod = s.OgrenimTipKod,
                        KayitOgretimYiliBaslangic = s.KayitOgretimYiliBaslangic,
                        KayitOgretimYiliDonemID = s.KayitOgretimYiliDonemID,
                        SavunmaBasvuruTarihi = ard == null ? (DateTime?)null : ard.SavunmaBasvuruTarihi,
                        IsSinavBilgisiGirildi = ard != null && ard.SRTalepleris.Any(),
                        IsDegerlendirmeSuvecinde = ard != null && ard.ToBasvuruSavunmaKomites.Any(a => a.ToBasvuruSavunmaDurumID.HasValue),
                        AktifSavunmaNo = ard == null ? (int?)null : ard.SavunmaNo,
                        AktifDonemAdi = ard == null ? "----" : (ard.DonemBaslangicYil + " / " + (ard.DonemBaslangicYil + 1) + " " + (ard.DonemID == 1 ? "Güz" : "Bahar")),
                        AktifDonemID = ard == null ? null : (ard.DonemBaslangicYil + "" + ard.DonemID),
                        DurumID = ard == null ? null : ard.ToBasvuruSavunmaDurumID,
                        IsOyBirligiOrCoklugu = ard != null ? ard.IsOyBirligiOrCoklugu : (bool?)null,
                        DurumModel = new TosDurumDto
                        {
                            IsTezOnerisiVar = ard != null,
                            ToBasvuruSavunmaDurumID = ard.ToBasvuruSavunmaDurumID,
                            IsSrTalebiYapildi = ard != null && ard.SRTalepleris.Any(),
                            DegerlendirmeBasladi = ard != null && ard.ToBasvuruSavunmaKomites.Any(a => a.ToBasvuruSavunmaDurumID.HasValue),
                            IsOyBirligiOrCoklugu = ard.IsOyBirligiOrCoklugu
                        },
                    };


            var q2 = q;
            q = q.Where(p => p.EnstituKod == enstituKod && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod));
            if (baslangicYil.HasValue) q = q.Where(p => p.AktifSavunmaNo.HasValue);
            if (!model.AktifDonemID.IsNullOrWhiteSpace()) q = q.Where(p => p.AktifDonemID == model.AktifDonemID);

            if (model.SavunmaNo.HasValue) q = q.Where(p => p.AktifSavunmaNo == model.SavunmaNo);
            if (model.AnabilimDaliID.HasValue) q = q.Where(p => p.AnabilimDaliID == model.AnabilimDaliID);
            if (model.AktifDurumID.HasValue)
            {
                if (model.AktifDurumID == 999)
                {
                    q = q.Where(p => !p.DurumModel.IsTezOnerisiVar);
                }
                else if (model.AktifDurumID == 1000)
                {
                    q = q.Where(p => p.DurumModel.IsTezOnerisiVar && !p.IsSinavBilgisiGirildi);
                }
                else if (model.AktifDurumID == 1001)
                {
                    q = q.Where(p => p.DurumModel.IsTezOnerisiVar && p.IsSinavBilgisiGirildi && !p.IsDegerlendirmeSuvecinde && !p.DurumID.HasValue);
                }
                else if (model.AktifDurumID == 1002)
                {
                    q = q.Where(p => p.DurumModel.IsTezOnerisiVar && p.IsSinavBilgisiGirildi && p.IsDegerlendirmeSuvecinde && !p.DurumID.HasValue);
                }
                else if (model.AktifDurumID == 1003)
                {
                    q = q.Where(p => p.DurumModel.IsTezOnerisiVar && p.DurumID.HasValue);
                }
                else q = q.Where(p => p.DurumModel.IsTezOnerisiVar && p.DurumID == model.AktifDurumID);
            }
            if (!model.AdSoyad.IsNullOrWhiteSpace())
                q = q.Where(p =>
                    p.AdSoyad.Contains(model.AdSoyad)
                    || p.OgrenciNo.Contains(model.AdSoyad)
                    || p.TcKimlikNo.Contains(model.AdSoyad));

            var tezDegerlendirme = RoleNames.TosDegerlendirmeYap.InRoleCurrent();
            var mbGelenBKayitYetki = RoleNames.TosGelenBasvuruKayit.InRoleCurrent();
            if (tezDegerlendirme && !mbGelenBKayitYetki)
            {
                q = q.Where(p => p.TezDanismanID == UserIdentity.Current.Id);
                q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.DurumID ?? 999).ThenByDescending(o => o.SavunmaBasvuruTarihi ?? o.BasvuruTarihi);
            }
            else
            {
                q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.SavunmaBasvuruTarihi ?? o.BasvuruTarihi);
            }

            var isFiltered = !Equals(q, q2);
            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.filteredOgrenciIds = isFiltered && !model.AktifDonemID.IsNullOrWhiteSpace() ? q.Select(s => s.KullaniciID).ToList() : new List<int>();
            ViewBag.filteredDanismanIds = isFiltered && !model.AktifDonemID.IsNullOrWhiteSpace() ? q.Where(p => p.TezDanismanID.HasValue).Select(s => s.TezDanismanID.Value).Distinct().ToList() : new List<int>();

            ViewBag.AktifDonemID = new SelectList(TezOneriSavunmaBus.CmbDonemListe(true), "Value", "Caption", model.AktifDonemID);
            ViewBag.AktifDurumID = new SelectList(TezOneriSavunmaBus.CmbTosDurumListe(true), "Value", "Caption", model.AktifDurumID);
            ViewBag.AnabilimDaliID = new SelectList(TezOneriSavunmaBus.GetCmbFilterAnabilimDallari(enstituKod, true), "Value", "Caption", model.AnabilimDaliID);
            ViewBag.SavunmaNo = new SelectList(TezOneriSavunmaBus.CmbTosNumarasi(true), "Value", "Caption", model.SavunmaNo);

            ViewBag.IndexModel = indexModel;
            return View(model);
        }


    }
}