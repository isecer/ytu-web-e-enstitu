using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
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
    [Authorize(Roles = RoleNames.TiGelenBasvuru)]
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class TiGelenBasvurularController : Controller
    {
        // GET: TIGelenBasvurular
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        public ActionResult Index(string ekd)
        {
            var model = new FmTiBasvuru() { PageSize = 50 };
            //var DonemBilgi = DateTime.Now.ToAraRaporDonemBilgi();
            //if (RoleNames.TIGelenBasvuruKayit.InRoleCurrent())
            //{
            //    model.AktifTIAraRaporDonemID = DonemBilgi.BaslangicYil + "" + DonemBilgi.DonemID;
            //    model.Expand = true;
            //}
            return Index(model, ekd); ;
        }
        [HttpPost]
        public ActionResult Index(FmTiBasvuru model, string ekd, bool export = false)
        {
            int? baslangicYil = null;
            int? donemId = null;
            if (!model.AktifTIAraRaporDonemID.IsNullOrWhiteSpace())
            {
                baslangicYil = model.AktifTIAraRaporDonemID.Substring(0, 4).ToInt(0);
                donemId = model.AktifTIAraRaporDonemID.Substring(4, 1).ToInt(0);
            }
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var isDegerlendirmeSurecinde = model.AktifTIAraRaporRaporDurumID == TiAraRaporDurumuEnum.DegerlendirmeSureciBaslatildi;
            var q = from s in _entities.TIBasvurus
                    join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in _entities.Kullanicilars on s.KullaniciID equals k.KullaniciID
                    join o in _entities.OgrenimTipleris on new { s.OgrenimTipKod, e.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod }
                    join pr in _entities.Programlars on s.ProgramKod equals pr.ProgramKod
                    join ab in _entities.AnabilimDallaris on pr.AnabilimDaliKod equals ab.AnabilimDaliKod
                    join en in _entities.Enstitulers on e.EnstituKod equals en.EnstituKod
                    let ard =
                      s.TIBasvuruAraRapors.FirstOrDefault(p => baslangicYil.HasValue ? (p.TIBasvuruID == s.TIBasvuruID && p.DonemID == donemId && p.DonemBaslangicYil == baslangicYil) : p.TIBasvuruAraRaporID == s.AktifTIBasvuruAraRaporID)


                    select new FrTiBasvuru
                    {
                        TIBasvuruID = s.TIBasvuruID,
                        TezDanismanID = s.TezDanismanID,
                        BasvuruTarihi = s.BasvuruTarihi,
                        EnstituKod = en.EnstituKod,
                        EnstituAdi = en.EnstituAd,
                        OgrenimTipAdi = o.OgrenimTipAdi,
                        AnabilimDaliID = ab.AnabilimDaliID,
                        AnabilimdaliAdi = ab.AnabilimDaliAdi,
                        ProgramAdi = pr.ProgramAdi,
                        KullaniciID = s.KullaniciID,
                        UserKey = k.UserKey,
                        AdSoyad = k.Ad + " " + k.Soyad,
                        EMail = k.EMail,
                        CepTel = k.CepTel,
                        TcKimlikNo = k.TcKimlikNo,
                        OgrenciNo = s.OgrenciNo,
                        ResimAdi = k.ResimAdi,
                        OgrenimTipKod = s.OgrenimTipKod,
                        KayitOgretimYiliBaslangic = s.KayitOgretimYiliBaslangic,
                        KayitOgretimYiliDonemID = s.KayitOgretimYiliDonemID,
                        AktifTIBasvuruAraRaporID = s.AktifTIBasvuruAraRaporID,
                        AraRaporDanismanID = ard == null ? null : ard.TezDanismanID,
                        TiAraRaporAktifDonemAdi = ard == null ? "Rapor Girişi Yapılmadı" : (ard.DonemBaslangicYil + " / " + (ard.DonemBaslangicYil + 1) + " " + (ard.DonemID == 1 ? "Güz" : "Bahar")),
                        TIAraRaporRaporDurumAdi = ard == null ? "Rapor Girişi Yapılmadı" : ard.TIBasvuruAraRaporDurumlari.TIBasvuruAraRaporDurumAdi,
                        AraRaporSayisi = ard != null ? ard.AraRaporSayisi : (int?)null,
                        TIAraRaporAktifDonemID = ard == null ? null : (ard.DonemBaslangicYil + "" + ard.DonemID),
                        TIAraRaporRaporDurumID = ard != null ? ard.TIBasvuruAraRaporDurumID : (int?)null,
                        RaporTarihi = ard != null ? ard.RaporTarihi : (DateTime?)null,
                        ToplantiTarihi = baslangicYil.HasValue ? ard.SRTalepleris.Select(sr => sr.Tarih).FirstOrDefault() : (DateTime?)null,
                        ToplantiSaati = baslangicYil.HasValue ? ard.SRTalepleris.Select(sr => sr.BasSaat).FirstOrDefault() : (TimeSpan?)null,
                        OnayYapmayanKomiteEmails = ard.TIBasvuruAraRaporKomites.Where(p => isDegerlendirmeSurecinde && p.IsLinkGonderildi == true && !p.IsBasarili.HasValue).Select(ss => ss.EMail).ToList(),
                        tIAraraporFiltreModels = s.TIBasvuruAraRapors.Select(ti => new TiAraraporFiltreModel
                        {
                            AraRaporSayisi = ti.AraRaporSayisi,
                            FormKodu = ti.FormKodu,
                            KomiteUyeleri = ti.TIBasvuruAraRaporKomites.Select(s2 => s2.AdSoyad).ToList(),
                            RaporDonemID = ti.DonemBaslangicYil + "" + ti.DonemID,
                            TIBasvuruAraRaporDurumID = ti.TIBasvuruAraRaporDurumID
                        }).ToList(),
                        IsOyBirligiOrCoklugu = ard != null ? ard.IsOyBirligiOrCoklugu : null,
                        IsBasariliOrBasarisiz = ard != null ? ard.IsBasariliOrBasarisiz : null

                    };

            q = q.Where(p => p.EnstituKod == enstituKod && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod));
            var q2 = q;
            if (baslangicYil.HasValue) q = q.Where(p => p.AraRaporDanismanID.HasValue);
            if (!model.AktifTIAraRaporDonemID.IsNullOrWhiteSpace()) q = q.Where(p => p.TIAraRaporAktifDonemID == model.AktifTIAraRaporDonemID);
            if (model.AktifTIAraRaporRaporDurumID.HasValue)
            {
                q = model.AktifTIAraRaporRaporDurumID < 1000 ? q.Where(p => p.TIAraRaporRaporDurumID == model.AktifTIAraRaporRaporDurumID)
                    : q.Where(p => p.TIAraRaporRaporDurumID == TiAraRaporDurumuEnum.DegerlendirmeSureciTamamlandi && p.IsBasariliOrBasarisiz == (model.AktifTIAraRaporRaporDurumID.Value == TiAraRaporDurumuEnum.DegerlendirmeBasariliOlanlar));
            }
            if (model.AktifAraRaporSayisi.HasValue) q = q.Where(p => p.AraRaporSayisi == model.AktifAraRaporSayisi);
            if (model.AnabilimDaliID.HasValue) q = q.Where(p => p.AnabilimDaliID == model.AnabilimDaliID);

            if (!model.TIAraRaporDonemID.IsNullOrWhiteSpace()) q = q.Where(p => p.tIAraraporFiltreModels.Any(a => a.RaporDonemID == model.TIAraRaporDonemID));
            if (model.TIAraRaporRaporDurumID.HasValue) q = q.Where(p => p.tIAraraporFiltreModels.Any(a => a.TIBasvuruAraRaporDurumID == model.TIAraRaporRaporDurumID.Value));
            if (model.TIAraRaporSayisi.HasValue) q = q.Where(p => p.tIAraraporFiltreModels.Any(a => a.AraRaporSayisi == model.TIAraRaporSayisi.Value));

            if (!model.AdSoyad.IsNullOrWhiteSpace())
                q = q.Where(p =>
                    p.AdSoyad.Contains(model.AdSoyad)
                    || p.OgrenciNo.Contains(model.AdSoyad)
                    || p.TcKimlikNo.Contains(model.AdSoyad)
                    || p.tIAraraporFiltreModels.Any(a => a.FormKodu == model.AdSoyad || a.KomiteUyeleri.Any(ak => ak.Contains(model.AdSoyad))));

            var tezDegerlendirme = RoleNames.TiTezDegerlendirmeYap.InRoleCurrent();
            var mbGelenBKayitYetki = RoleNames.TiGelenBasvuruKayit.InRoleCurrent();
            if (tezDegerlendirme && !mbGelenBKayitYetki)
            {
                q = q.Where(p => p.TezDanismanID == UserIdentity.Current.Id);
                q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.TIAraRaporRaporDurumID ?? 999).ThenByDescending(o => o.RaporTarihi ?? o.BasvuruTarihi);
            }
            else
            {
                q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.RaporTarihi ?? o.BasvuruTarihi);
            }


            #region export
            if (export && model.RowCount > 0)
            {
                var gv = new GridView();
                var data = q.Select(s => new
                {
                    s.KullaniciID,
                    s.OgrenciNo,
                    s.TcKimlikNo,
                    s.AdSoyad,
                    s.EMail,
                    s.CepTel,
                    s.AnabilimdaliAdi,
                    s.ProgramAdi,
                    s.AraRaporDanismanID,
                    DonemAdi = s.TiAraRaporAktifDonemAdi,
                    s.ToplantiTarihi,
                    s.ToplantiSaati,
                    s.TIAraRaporRaporDurumAdi
                }).ToList();
                var danismanIds = data.Select(s => s.AraRaporDanismanID).ToList();
                var danismans = _entities.Kullanicilars.Where(p => danismanIds.Contains(p.KullaniciID))
                    .Select(s => new { s.KullaniciID, Danisman = s.Unvanlar.UnvanAdi + " " + s.Ad + " " + s.Soyad }).ToList();

                var exportData = (from s in data
                                  join d in danismans on s.AraRaporDanismanID equals d.KullaniciID
                                  select new
                                  {
                                      s.OgrenciNo,
                                      s.TcKimlikNo,
                                      s.AdSoyad,
                                      s.EMail,
                                      s.CepTel,
                                      s.AnabilimdaliAdi,
                                      s.ProgramAdi,
                                      d.Danisman,
                                      s.DonemAdi,
                                      s.ToplantiTarihi,
                                      s.ToplantiSaati,
                                      AraRaporDurumu = s.TIAraRaporRaporDurumAdi
                                  }).ToList();

                gv.DataSource = exportData;
                gv.DataBind();
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                var sw = new StringWriter();
                var htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_TezIzlemeAraRaporListesi_" + DateTime.Now.ToFormatDate() + ".xls");
            }
            #endregion

            var isFiltered = !Equals(q, q2);
            model.RowCount = q.Count(); 
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.filteredOgrenciIds = isFiltered && !model.AktifTIAraRaporDonemID.IsNullOrWhiteSpace() ? q.Select(s => s.KullaniciID).ToList() : new List<int>();
            ViewBag.filteredDanismanIds = isFiltered && !model.AktifTIAraRaporDonemID.IsNullOrWhiteSpace() ? q.Where(p => p.AraRaporDanismanID.HasValue).Select(s => s.AraRaporDanismanID.Value).Distinct().ToList() : new List<int>();
            ViewBag.onayYapmayanKomiteEmails = isFiltered && isDegerlendirmeSurecinde ? q.SelectMany(s => s.OnayYapmayanKomiteEmails).Distinct().ToList() : new List<string>();

            ViewBag.AktifTIAraRaporDonemID = new SelectList(TiBus.CmbTiDonemListe(enstituKod, true), "Value", "Caption", model.AktifTIAraRaporDonemID);
            ViewBag.AktifTIAraRaporRaporDurumID = new SelectList(TiBus.CmbTiAraRaporDurumListe(true), "Value", "Caption", model.AktifTIAraRaporRaporDurumID);
            ViewBag.TIAraRaporRaporDurumID = new SelectList(TiBus.CmbTiAraRaporDurumListe(true), "Value", "Caption", model.TIAraRaporRaporDurumID);
            ViewBag.AnabilimDaliID = new SelectList(TiBus.GetCmbFilterTiAnabilimDallari(enstituKod, true), "Value", "Caption", model.AnabilimDaliID);
            ViewBag.AktifAraRaporSayisi = new SelectList(TiBus.CmbAraRaporSayisi(true), "Value", "Caption", model.AktifAraRaporSayisi);
            ViewBag.TIAraRaporSayisi = new SelectList(TiBus.CmbAraRaporSayisi(true), "Value", "Caption", model.TIAraRaporSayisi);
             return View(model);
        }

    }
}