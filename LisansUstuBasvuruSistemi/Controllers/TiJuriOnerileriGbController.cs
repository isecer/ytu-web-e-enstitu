using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.TiJuriOnerileriGb)]
    public class TiJuriOnerileriGbController : Controller
    {
        // GET: TikOneriGb
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(Guid? selectedBasvuruUniqueId, string ekd)
        {
            return Index(new FmTijBasvuru() { SelectedBasvuruUniqueId = selectedBasvuruUniqueId, PageSize = 50 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmTijBasvuru model, string ekd)
        {
            model.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            model.KullaniciID = model.KullaniciID ?? UserIdentity.Current.Id;
            TezIzlemeJuriOneriBus.TezIzlemeJuriOneriSenkronizasyon(model.KullaniciID.Value);
            var q = from s in _entities.TijBasvurus.Where(p=>p.EnstituKod== model.EnstituKod)
                    join k in _entities.Kullanicilars on s.KullaniciID equals k.KullaniciID
                    select new
                    {
                        s.TijBasvuruID,
                        s.UniqueID,
                        s.EnstituKod,
                        s.BasvuruTarihi,
                        s.Programlar.AnabilimDaliID,
                        s.KullaniciID, 
                        AdSoyad = k.Ad + " " + k.Soyad,
                        s.OgrenciNo,
                        k.ResimAdi,
                        TezDanismanIds=s.TijBasvuruOneris.Select(sd=>sd.TezDanismanID).ToList(),
                        SonBasvuru = s.TijBasvuruOneris.Select(s2 => new TijBasvuruOneriDetayDto
                        {
                            TijBasvuruOneriID = s2.TijBasvuruOneriID,
                            TijFormTipID = s2.TijFormTipID,
                            //TijFormTipAdi = s2.TijFormTipleri.TikFormTipAdi,
                            TijDegisiklikTipID = s2.TijDegisiklikTipID,
                           // TijDegisiklikTipAdi = s2.TijDegisiklikTipleri.TijDegisiklikTipAdi,
                            IsObsData = s2.IsObsData,
                            BasvuruTarihi = s2.BasvuruTarihi,
                            DonemBaslangicYil = s2.DonemBaslangicYil,
                            DonemID = s2.DonemID,
                            DonemAdi = s2.DonemBaslangicYil + "/" + (s2.DonemBaslangicYil + 1) + " " + s.Donemler.DonemAdi,
                            TezDanismanID = s2.TezDanismanID,
                            IsDilTaahhutuOnaylandi = s2.IsDilTaahhutuOnaylandi,
                            DanismanOnayladi = s2.DanismanOnayladi,
                            EYKYaGonderildi = s2.EYKYaGonderildi,
                            EYKDaOnaylandi = s2.EYKDaOnaylandi,

                        }).OrderByDescending(o => o.TijBasvuruOneriID).FirstOrDefault(),
                        KayitVar = s.TijBasvuruOneris.Any()

                    };
            int? danismanId = null;
            var tiJuriOnerileriOgrenciAdina = RoleNames.TiJuriOnerileriOgrenciAdina.InRoleCurrent();
            var tiJuriOnerileriYetkili = RoleNames.TiJuriOnerileriEykYaGonder.InRoleCurrent();
            if (tiJuriOnerileriOgrenciAdina && !tiJuriOnerileriYetkili)
                danismanId = UserIdentity.Current.Id;
            if (danismanId.HasValue)
            {
                q = q.Where(p =>  p.TezDanismanIds.Contains(danismanId));
            }
            if (model.TijFormTipID.HasValue)
                q = q.Where(p => p.SonBasvuru != null && p.SonBasvuru.TijFormTipID == model.TijFormTipID);
            if (model.AnabilimDaliID.HasValue)
                q = q.Where(p => p.AnabilimDaliID == model.AnabilimDaliID);
            if (!model.AktifTijDonemId.IsNullOrWhiteSpace())
                q = q.Where(p => p.SonBasvuru != null && (p.SonBasvuru.DonemBaslangicYil + "" + p.SonBasvuru.DonemID) == model.AktifTijDonemId);
            if (model.AktifDurumID.HasValue)
            {
                if (model.AktifDurumID == TijBasvuruDurum.DanismanOnayiBekliyor) q = q.Where(p => p.SonBasvuru != null && !p.SonBasvuru.DanismanOnayladi.HasValue);
                else if (model.AktifDurumID == TijBasvuruDurum.DanismanTarafindanOnaylandi) q = q.Where(p => p.SonBasvuru != null && p.SonBasvuru.DanismanOnayladi == true && !p.SonBasvuru.EYKYaGonderildi.HasValue);
                else if (model.AktifDurumID == TijBasvuruDurum.DanismanTarafindanOnaylanmadi) q = q.Where(p => p.SonBasvuru != null && p.SonBasvuru.DanismanOnayladi == false && !p.SonBasvuru.EYKYaGonderildi.HasValue);
                else if (model.AktifDurumID == TijBasvuruDurum.EykYaGonderimOnayiBekleniyor) q = q.Where(p => p.SonBasvuru != null && p.SonBasvuru.DanismanOnayladi == true && !p.SonBasvuru.EYKYaGonderildi.HasValue);
                else if (model.AktifDurumID == TijBasvuruDurum.EykYaGonderimiOnaylandi) q = q.Where(p => p.SonBasvuru != null && p.SonBasvuru.EYKYaGonderildi == true && !p.SonBasvuru.EYKDaOnaylandi.HasValue);
                else if (model.AktifDurumID == TijBasvuruDurum.EykYaGonderimiOnaylanmadi) q = q.Where(p => p.SonBasvuru != null && p.SonBasvuru.EYKYaGonderildi == false && !p.SonBasvuru.EYKDaOnaylandi.HasValue);
                else if (model.AktifDurumID == TijBasvuruDurum.EykDaOnayBekleniyor) q = q.Where(p => p.SonBasvuru != null && p.SonBasvuru.EYKYaGonderildi == true && !p.SonBasvuru.EYKDaOnaylandi.HasValue);
                else if (model.AktifDurumID == TijBasvuruDurum.EykDaOnaylandi) q = q.Where(p => p.SonBasvuru != null && p.SonBasvuru.EYKDaOnaylandi == true);
                else if (model.AktifDurumID == TijBasvuruDurum.EykDaOnaylanmadi) q = q.Where(p => p.SonBasvuru != null && p.SonBasvuru.EYKDaOnaylandi == false);
            }


            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi();
            if (model.SelectedBasvuruUniqueId.HasValue)
            {
                q = q.OrderBy(o => o.UniqueID == model.SelectedBasvuruUniqueId ? 1 : 2).ThenByDescending(o => o.BasvuruTarihi);
            }
            else q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.SonBasvuru!=null?o.SonBasvuru.BasvuruTarihi:o.BasvuruTarihi);
            model.Data = q.Select(s => new FrTijBasvuru
            {
                TijBasvuruID = s.TijBasvuruID,
                UniqueID = s.UniqueID,
                EnstituKod = s.EnstituKod,
                BasvuruTarihi = s.BasvuruTarihi,
                KullaniciID = s.KullaniciID,
                AdSoyad = s.AdSoyad,
                OgrenciNo = s.OgrenciNo,
                ResimAdi = s.ResimAdi,
                KayitVar = s.KayitVar,
                SonBasvuru = s.SonBasvuru

            }).Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.IndexModel = indexModel;
            ViewBag.AktifTijDonemId = new SelectList(TezIzlemeJuriOneriBus.CmbTiDonemListe(model.EnstituKod, true), "Value", "Caption", model.AktifTijDonemId);
            ViewBag.AnabilimDaliID = new SelectList(TezIzlemeJuriOneriBus.GetCmbFilterTiAnabilimDallari(model.EnstituKod, true), "Value", "Caption", model.AnabilimDaliID);
            ViewBag.AktifDurumID = new SelectList(TezIzlemeJuriOneriBus.CmbTdoOneriDurumListe(true), "Value", "Caption", model.AktifDurumID);
            ViewBag.TijFormTipID = new SelectList(TezIzlemeJuriOneriBus.CmbTijOneriTipListe(true), "Value", "Caption", model.TijFormTipID);

            return View(model);

        }
    }
}