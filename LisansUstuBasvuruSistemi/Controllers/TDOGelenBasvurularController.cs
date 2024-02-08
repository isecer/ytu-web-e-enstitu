using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Raporlar.TezDanismanOneri;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize(Roles = RoleNames.TdoGelenBasvuru)]
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class TdoGelenBasvurularController : Controller
    {
        // GET: TDOGelenBasvurular
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd, int? tdoBasvuruId, int? kullaniciId)
        {

            return Index(new FmTdoBasvuruDto() { TDOBasvuruID = tdoBasvuruId, KullaniciID = kullaniciId, PageSize = 50 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmTdoBasvuruDto model, string ekd, bool export = false)
        {

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var tdoDanismanOnayYetkisi = RoleNames.TdoDanismanOnayYetkisi.InRoleCurrent();
            var tdoGelenBasvuruKayit = RoleNames.TdoGelenBasvuruKayit.InRoleCurrent();

            var q = from s in _entities.TDOBasvurus
                    join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in _entities.Kullanicilars on s.KullaniciID equals k.KullaniciID
                    join o in _entities.OgrenimTipleris on new { s.OgrenimTipKod, e.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod }
                    join pr in _entities.Programlars on s.ProgramKod equals pr.ProgramKod
                    join ab in _entities.AnabilimDallaris on pr.AnabilimDaliKod equals ab.AnabilimDaliKod
                    join en in _entities.Enstitulers on e.EnstituKod equals en.EnstituKod
                    join ktip in _entities.KullaniciTipleris on k.KullaniciTipID equals ktip.KullaniciTipID
                    join ard in _entities.TDOBasvuruDanismen on s.AktifTDOBasvuruDanismanID equals ard.TDOBasvuruDanismanID into defard
                    from ard in defard.DefaultIfEmpty()
                    let ardEs = s.TDOBasvuruDanismen.SelectMany(sm => sm.TDOBasvuruEsDanismen).OrderByDescending(oe => oe.TDOBasvuruEsDanismanID).FirstOrDefault(p => p.TDOBasvuruDanismanID == ard.TDOBasvuruDanismanID)

                    select new FrTdoBasvuruDto
                    {
                        TezDanismanID = ard.TezDanismanID,
                        TDOBasvuruID = s.TDOBasvuruID,
                        BasvuruTarihi = s.BasvuruTarihi,
                        EnstituKod = en.EnstituKod,
                        EnstituAdi = en.EnstituAd,
                        OgrenimTipAdi = o.OgrenimTipAdi,
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
                        AktifTDOBasvuruDanismanID = s.AktifTDOBasvuruDanismanID,
                        TDODanismanTalepTipID = ard != null ? ard.TDODanismanTalepTipID : (int?)null,
                        AktifDonemID = ard == null ? null : (ard.DonemBaslangicYil + "" + ard.DonemID),
                        AktifDonemAdi = ard == null ? "Danışman Önerisi Yok" : (ard.DonemBaslangicYil + " / " + (ard.DonemBaslangicYil + 1) + " " + (ard.DonemID == 1 ? "Güz" : "Bahar")),
                        EYKYaGonderildiIslemTarihi = ard == null ? null : ard.EYKYaGonderildiIslemTarihi,
                        EYKYaGonderildiIslemTarihiES = ardEs == null ? null : ardEs.EYKYaGonderildiIslemTarihi,
                        TDOBasvuruDanisman = ard,
                        VarolanTezDanismanID = ard != null ? ard.VarolanTezDanismanID : null,
                        VarolanDanismanOnayladi = ard != null ? ard.VarolanDanismanOnayladi : null,
                        DanismanOnayladi = ard != null ? ard.DanismanOnayladi : null,
                        EYKYaGonderildi = ard != null ? ard.EYKYaGonderildi : null,
                        EYKYaHazirlandi = ard != null ? ard.EYKYaHazirlandi : null,

                        EYKDaOnaylandi = ard != null ? ard.EYKDaOnaylandi : null,
                        EsDanismanOnerisiVar = ardEs != null,
                        Es_EYKYaGonderildi = ardEs != null ? ardEs.EYKYaGonderildi : null,
                        Es_EYKDaOnaylandi = ardEs != null ? ardEs.EYKDaOnaylandi : null,
                        RowDate = (ardEs.EYKYaGonderildi == true && !ardEs.EYKDaOnaylandi.HasValue ? ardEs.EYKYaGonderildiIslemTarihi.Value : (ard.EYKYaGonderildi == true && !ard.EYKDaOnaylandi.HasValue ? ard.EYKYaGonderildiIslemTarihi.Value : (ard != null ? ard.BasvuruTarihi : DateTime.MinValue))),
                        Sira = (ard != null && (ard.EYKYaGonderildi == true && ard.EYKDaOnaylandi == null) || (ardEs != null && ardEs.EYKYaGonderildi == null)) ? 0 : 1,
                        TDODanismanDetayModels = (from x in s.TDOBasvuruDanismen
                                                  join xd in _entities.TDOBasvuruEsDanismen on x.TDOBasvuruDanismanID equals xd.TDOBasvuruDanismanID into defX
                                                  from xD in defX.DefaultIfEmpty()
                                                  select new TdoDanismanFiltreModel
                                                  {
                                                      FormKodu = x.FormKodu,
                                                      RaporDonemID = x.DonemBaslangicYil + "" + x.DonemID,
                                                      DanismanAdSoyad = x.TDAdSoyad,
                                                      Es_DanismanAdSoyad = xD != null ? xD.AdSoyad : "",
                                                      TDODanismanTalepTipID = x.TDODanismanTalepTipID,
                                                      VarolanDanismanOnayladi = x.VarolanDanismanOnayladi,
                                                      DanismanOnayladi = x.DanismanOnayladi,
                                                      EYKYaGonderildi = x.EYKYaGonderildi,
                                                      EYKDaOnaylandi = x.EYKDaOnaylandi,
                                                      Es_EYKYaGonderildi = xD != null ? xD.EYKYaGonderildi : null,
                                                      Es_EYKDaOnaylandi = xD != null ? xD.EYKDaOnaylandi : null,
                                                      Es_FormKodu = xD != null ? xD.FormKodu : null
                                                  }).ToList(),
                    };
            var q2 = q;
            if (tdoDanismanOnayYetkisi && !tdoGelenBasvuruKayit)
            {
                q = q.Where(p => p.TezDanismanID == UserIdentity.Current.Id || p.VarolanTezDanismanID == UserIdentity.Current.Id);
            }
            q = q.Where(p => p.EnstituKod == enstituKod && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod));
            if (!model.AktifDonemID.IsNullOrWhiteSpace()) q = q.Where(p => p.AktifDonemID == model.AktifDonemID);
            if (model.TDODanismanTalepTipID.HasValue) q = q.Where(p => p.TDODanismanTalepTipID == model.TDODanismanTalepTipID);
            if (model.AktifDurumID.HasValue)
            {
                if (model.AktifDurumID == TdoDansimanDurumuEnum.DanismanOnayiBekliyor) q = q.Where(p => !p.DanismanOnayladi.HasValue);
                else if (model.AktifDurumID == TdoDansimanDurumuEnum.DanismanTarafindanOnaylandi) q = q.Where(p => p.DanismanOnayladi == true && !p.EYKYaGonderildi.HasValue);
                else if (model.AktifDurumID == TdoDansimanDurumuEnum.DanismanTarafindanOnaylanmadi) q = q.Where(p => p.DanismanOnayladi == false && !p.EYKYaGonderildi.HasValue);
                else if (model.AktifDurumID == TdoDansimanDurumuEnum.EykYaGonderimOnayiBekleniyor) q = q.Where(p => p.DanismanOnayladi == true && !p.EYKYaGonderildi.HasValue);
                else if (model.AktifDurumID == TdoDansimanDurumuEnum.EykYaGonderimiOnaylandi) q = q.Where(p => p.EYKYaGonderildi == true && !p.EYKYaHazirlandi.HasValue);
                else if (model.AktifDurumID == TdoDansimanDurumuEnum.EykYaGonderimiOnaylanmadi) q = q.Where(p => p.EYKYaGonderildi == false && !p.EYKYaHazirlandi.HasValue);
                else if (model.AktifDurumID == TdoDansimanDurumuEnum.EykYaHazirlanmaBekleniyor) q = q.Where(p => p.EYKYaGonderildi == true && !p.EYKYaHazirlandi.HasValue);
                else if (model.AktifDurumID == TdoDansimanDurumuEnum.EykYaHazirlandi) q = q.Where(p => p.EYKYaHazirlandi == true && !p.EYKDaOnaylandi.HasValue);
                else if (model.AktifDurumID == TdoDansimanDurumuEnum.EykDaOnayBekleniyor) q = q.Where(p => p.EYKYaHazirlandi == true && !p.EYKDaOnaylandi.HasValue);
                else if (model.AktifDurumID == TdoDansimanDurumuEnum.EykDaOnaylandi) q = q.Where(p => p.EYKDaOnaylandi == true);
                else if (model.AktifDurumID == TdoDansimanDurumuEnum.EykDaOnaylanmadi) q = q.Where(p => p.EYKDaOnaylandi == false);
            }
            if (model.AktifEsDurumID.HasValue)
            {
                if (model.AktifEsDurumID == TdoDansimanDurumuEnum.EykYaGonderimOnayiBekleniyor) q = q.Where(p => p.EsDanismanOnerisiVar && !p.Es_EYKYaGonderildi.HasValue);
                else if (model.AktifEsDurumID == TdoDansimanDurumuEnum.EykYaGonderimiOnaylandi) q = q.Where(p => p.Es_EYKYaGonderildi == true);
                else if (model.AktifEsDurumID == TdoDansimanDurumuEnum.EykYaGonderimiOnaylanmadi) q = q.Where(p => p.Es_EYKYaGonderildi == false);
                else if (model.AktifEsDurumID == TdoDansimanDurumuEnum.EykDaOnayBekleniyor) q = q.Where(p => p.Es_EYKYaGonderildi == true && !p.Es_EYKDaOnaylandi.HasValue);
                else if (model.AktifEsDurumID == TdoDansimanDurumuEnum.EykDaOnaylandi) q = q.Where(p => p.Es_EYKDaOnaylandi == true);
                else if (model.AktifEsDurumID == TdoDansimanDurumuEnum.EykDaOnaylanmadi) q = q.Where(p => p.Es_EYKDaOnaylandi == false);
            }
            if (model.TDOBasvuruID.HasValue) q = q.Where(p => p.TDOBasvuruID == model.TDOBasvuruID);
            //if (!model.DonemID.IsNullOrWhiteSpace()) q = q.Where(p => p.TDODanismanDetayModels.Any(a => a.RaporDonemID == model.DonemID));
            //if (model.DurumID.HasValue)
            //{
            //    if (model.DurumID == TdoDansimanDurumuEnum.DanismanOnayiBekliyor) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => !p2.DanismanOnayladi.HasValue));
            //    else if (model.DurumID == TdoDansimanDurumuEnum.DanismanTarafindanOnaylandi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.DanismanOnayladi == true));
            //    else if (model.DurumID == TdoDansimanDurumuEnum.DanismanTarafindanOnaylanmadi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.DanismanOnayladi == false));
            //    else if (model.DurumID == TdoDansimanDurumuEnum.EykYaGonderimOnayiBekleniyor) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.DanismanOnayladi == true && !p2.EYKYaGonderildi.HasValue));
            //    else if (model.DurumID == TdoDansimanDurumuEnum.EykYaGonderimiOnaylandi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.EYKYaGonderildi == true));
            //    else if (model.DurumID == TdoDansimanDurumuEnum.EykYaGonderimiOnaylanmadi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.EYKYaGonderildi == false));
            //    else if (model.DurumID == TdoDansimanDurumuEnum.EykDaOnayBekleniyor) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.EYKYaGonderildi == true && !p2.EYKDaOnaylandi.HasValue));
            //    else if (model.DurumID == TdoDansimanDurumuEnum.EykDaOnaylandi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.EYKDaOnaylandi == true));
            //    else if (model.DurumID == TdoDansimanDurumuEnum.EykDaOnaylanmadi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.EYKDaOnaylandi == false));
            //}
            //if (model.EsDurumID.HasValue)
            //{
            //    if (model.EsDurumID == TdoDansimanDurumuEnum.EykYaGonderimOnayiBekleniyor) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => !p2.Es_EYKYaGonderildi.HasValue));
            //    else if (model.EsDurumID == TdoDansimanDurumuEnum.EykYaGonderimiOnaylandi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.Es_EYKYaGonderildi == true));
            //    else if (model.EsDurumID == TdoDansimanDurumuEnum.EykYaGonderimiOnaylanmadi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.Es_EYKYaGonderildi == false));
            //    else if (model.EsDurumID == TdoDansimanDurumuEnum.EykDaOnayBekleniyor) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.Es_EYKYaGonderildi == true && !p2.Es_EYKDaOnaylandi.HasValue));
            //    else if (model.EsDurumID == TdoDansimanDurumuEnum.EykDaOnaylandi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.Es_EYKDaOnaylandi == true));
            //    else if (model.EsDurumID == TdoDansimanDurumuEnum.EykDaOnaylanmadi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.Es_EYKDaOnaylandi == false));
            //}

            if (!model.AdSoyad.IsNullOrWhiteSpace())
                q = q.Where(p =>
                                 p.AdSoyad.Contains(model.AdSoyad)
                                 || p.OgrenciNo.Contains(model.AdSoyad)
                                 || p.TDODanismanDetayModels.Any(a => a.FormKodu == model.AdSoyad || a.Es_FormKodu == model.AdSoyad || a.DanismanAdSoyad.Contains(model.AdSoyad)));

            var isFiltered = !Equals(q, q2);


            #region export
            if (export && model.RowCount > 0)
            {
                var gv = new GridView();
                var qExp = q.ToList();
                gv.DataSource = qExp.Select(s => new
                {
                    s.AktifDonemAdi,
                    s.OgrenimTipAdi,
                    s.AnabilimdaliAdi,
                    s.ProgramAdi,
                    s.AdSoyad,
                    s.TcKimlikNo,
                    s.OgrenciNo,
                    s.EMail,
                    s.CepTel,
                    DanismanAdSoyad = s.TDOBasvuruDanisman != null ? (s.TDOBasvuruDanisman.TDUnvanAdi + " " + s.TDOBasvuruDanisman.TDAdSoyad) : "Danışman Yok",
                    DanismanOnayladi = s.TDOBasvuruDanisman != null ? (s.DanismanOnayladi.HasValue ? (s.DanismanOnayladi.Value ? "Danışman Onayladı" : "Danışman Onaylamadı") : "Danışman Onayı Bekleniyor") : "Danışman Yok",
                    EYKYaGonderildi = s.TDOBasvuruDanisman != null ? (s.EYKYaGonderildi.HasValue ? (s.EYKYaGonderildi.Value ? "EYK'ya Gönderimi Onaylandı" : "EYK'ya Gönderimi Onaylanmadı") : "EYK'ya Gönderim Onayı Bekleniyor") : "Danışman Yok",
                    EYKYaHazirlandi = s.TDOBasvuruDanisman != null ? (s.EYKYaHazirlandi.HasValue ? (s.EYKYaHazirlandi.Value ? "EYK'ya Hazırlandı" : "EYK'ya Hazrılanmadı") : "EYK'ya Hazırlanması Bekleniyor") : "Danışman Yok",
                    EYKDaOnaylandi = s.TDOBasvuruDanisman != null ? (s.EYKYaGonderildi.HasValue ? (s.EYKYaGonderildi.Value ? "EYK'ya Gönderimi Onaylandı" : "EYK'ya Gönderimi Onaylanmadı") : "EYK'ya Gönderim Onayı Bekleniyor") : "Danışman Yok",
                    EsDanismanAdSoyad = s.TDOBasvuruDanisman != null && s.TDOBasvuruDanismen.Any(s2 => s2.TDOBasvuruEsDanismen.Any()) ? (s.TDOBasvuruDanisman.TDUnvanAdi + " " + s.TDOBasvuruDanisman.TDAdSoyad) : "Danışman Yok",
                    EsDanismanOnayladi = s.TDOBasvuruDanisman != null && s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.Any() ? (s.DanismanOnayladi.HasValue ? (s.DanismanOnayladi.Value ? "Danışman Onayladı" : "Danışman Onaylamadı") : "Danışman Onayı Bekleniyor") : "Danışman Yok",
                    EsDanismanEYKYaGonderildi = s.TDOBasvuruDanisman != null && s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.Any() ? (s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.First().EYKYaGonderildi.HasValue ? (s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.First().EYKYaGonderildi.Value ? "EYK'ya Gönderimi Onaylandı" : "EYK'ya Gönderimi Onaylanmadı") : "EYK'ya Gönderim Onayı Bekleniyor") : "Eş Danışman Yok",
                    EsDanismanEYKDaOnaylandi = s.TDOBasvuruDanisman != null && s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.Any() ? (s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.First().EYKYaGonderildi.HasValue ? (s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.First().EYKYaGonderildi.Value ? "EYK'ya Gönderimi Onaylandı" : "EYK'ya Gönderimi Onaylanmadı") : "EYK'ya Gönderim Onayı Bekleniyor") : "Eş Danışman Yok",

                }).ToList();
                gv.DataBind();
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                var sw = new StringWriter();
                var htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_DanışmanÖneriListesi_" + DateTime.Now.ToFormatDate() + ".xls");
            }
            #endregion


            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else if (model.AktifDurumID == 5 || model.DurumID == 5)
                q = q.OrderBy(o => o.EYKYaGonderildiIslemTarihi);
            else if (model.AktifEsDurumID == 5 || model.EsDurumID == 5)
                q = q.OrderBy(o => o.EYKYaGonderildiIslemTarihiES);
            else q = q.OrderBy(o => o.Sira).ThenByDescending(o => o.RowDate);
            model.TdoBasvuruDtos = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.filteredOgrenciIds = isFiltered ? q.Select(s => s.KullaniciID).Distinct().ToList() : new List<int>();
            ViewBag.filteredDanismanIds = isFiltered ? q.Where(p => p.TezDanismanID.HasValue).Select(s => s.TezDanismanID.Value).Distinct().ToList() : new List<int>();
            ViewBag.AktifDonemID = new SelectList(TdoBus.CmbTdoDonemListe(enstituKod, true), "Value", "Caption", model.AktifDonemID);
            ViewBag.TDODanismanTalepTipID = new SelectList(TdoBus.CmbTdoDanismanTalepTip(true), "Value", "Caption", model.TDODanismanTalepTipID);
            ViewBag.AktifDurumID = new SelectList(TdoBus.CmbTdoOneriDurumListe(true), "Value", "Caption", model.AktifDurumID);
            ViewBag.DurumID = new SelectList(TdoBus.CmbTdoOneriDurumListe(true), "Value", "Caption", model.DurumID);
            ViewBag.AktifEsDurumID = new SelectList(TdoBus.CmbTdoEsOneriDurumListe(true), "Value", "Caption", model.AktifEsDurumID);
            ViewBag.EsDurumID = new SelectList(TdoBus.CmbTdoEsOneriDurumListe(true), "Value", "Caption", model.EsDurumID);
            return View(model);
        }

        public ActionResult GetTutanakRaporu()
        {
            return View();
        }
        public ActionResult GetTutanakRaporuKontrolu(DateTime? basTar, DateTime? bitTar)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                IsSuccess = true
            };
            if (!basTar.HasValue)
            {
                mMessage.IsSuccess = false;
                mMessage.Messages.Add("Başlangıç tarihini giriniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BasTar" });
            }
            else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "BasTar" });
            if (!basTar.HasValue)
            {
                mMessage.IsSuccess = false;
                mMessage.Messages.Add("Bitiş tarihini giriniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BitTar" });
            }
            else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "BitTar" });
            if (basTar.HasValue && bitTar.HasValue)
            {
                if (basTar > bitTar)
                {
                    mMessage.IsSuccess = false;
                    mMessage.Messages.Add("Başlangıç tarihi bitiş tarihinden büyük olamaz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BasTar" });
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BitTar" });
                }
                else
                {
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "BasTar" });
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "BitTar" });
                }
            }

            //if (raporTarihi.IsNullOrWhiteSpace())
            //{
            //    mMessage.IsSuccess = false;
            //    mMessage.Messages.Add("Rapor tarihi giriniz.");
            //}
            //mMessage.MessagesDialog.Add(new MrMessage { MessageType = raporTarihi.IsNullOrWhiteSpace() ? Msgtype.Warning : Msgtype.Success, PropertyName = "RaporTarihi" });
            //if (!sayi.HasValue)
            //{
            //    mMessage.IsSuccess = false;
            //    mMessage.Messages.Add("Rapor sayısı giriniz.");
            //}
            //mMessage.MessagesDialog.Add(new MrMessage { MessageType = !sayi.HasValue ? Msgtype.Warning : Msgtype.Success, PropertyName = "Sayi" });

            if (!mMessage.IsSuccess)
            {

                mMessage.Title = "Tutanak çıktısı oluşturulamadı";
                mMessage.MessageType = MsgTypeEnum.Warning;
            }

            return mMessage.ToJsonResult();



        }
        public ActionResult GetTutanakRaporuExport(int tutanakTipId, bool? isDegisiklikOrYeniOneri, int enstituOnayDurumId, string basTar, string bitTar, string ekd)
        {

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var baslangicTarihi = basTar.ToDate().Value;
            var bitisTarihi = bitTar.ToDate().Value.AddDays(1).AddMilliseconds(-1);
            string raporAdi;

            if (tutanakTipId == 4)
            {
                var qes = _entities.TDOBasvuruEsDanismen.Where(p => p.TDOBasvuruDanisman.TDOBasvuru.EnstituKod == enstituKod && p.IsDegisiklikTalebi == (isDegisiklikOrYeniOneri ?? p.IsDegisiklikTalebi));

                if (enstituOnayDurumId == 1)
                {
                    qes = qes.Where(p => p.EYKYaGonderildi == true && !p.EYKDaOnaylandi.HasValue && p.EYKYaGonderildiIslemTarihi >= baslangicTarihi && p.EYKYaGonderildiIslemTarihi <= bitisTarihi);
                    qes = qes.OrderBy(o => o.EYKYaGonderildiIslemTarihi).ThenBy(t => t.IslemTarihi);
                }
                else if (enstituOnayDurumId == 3)
                {
                    qes = qes.Where(p => p.EYKDaOnaylandi == true && p.EYKDaOnaylandiOnayTarihi >= baslangicTarihi && p.EYKDaOnaylandiOnayTarihi <= bitisTarihi);
                    qes = qes.OrderBy(o => o.EYKDaOnaylandiOnayTarihi).ThenBy(t => t.IslemTarihi);

                }


                var gv = new GridView();
                var dataEsList = qes.ToList().Select((s, inx) => new
                {
                    SiraNo = inx + 1,
                    EsDanismanOnerisi_EsDanısmanDegisikligi = s.IsDegisiklikTalebi ? "Eş Danışman Değişikliği" : "Eş Danışman Önerisi",
                    EykYaGonderimTarihi = s.EYKYaGonderildiIslemTarihi,
                    EYKTarihi = s.EYKDaOnaylandiOnayTarihi,
                    s.TDOBasvuruDanisman.TDOBasvuru.OgrenciNo,
                    OgrenciAdSoyad = s.TDOBasvuruDanisman.TDOBasvuru.Kullanicilar.Ad + " " + s.TDOBasvuruDanisman.TDOBasvuru.Kullanicilar.Soyad,
                    OgrenciAnabilimDali = s.TDOBasvuruDanisman.TDOBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi + " / " + s.TDOBasvuruDanisman.TDOBasvuru.Programlar.ProgramAdi,
                    YL_DR = s.TDOBasvuruDanisman.TDOBasvuru.OgrenimTipKod.IsDoktora() ? "DR" : "YL",
                    DanismanAdSoyad = s.TDAdSoyad.IsNullOrWhiteSpace() ? s.TDOBasvuruDanisman.TDUnvanAdi + " " + s.TDOBasvuruDanisman.TDAdSoyad : (s.TDUnvanAdi + " " + s.TDAdSoyad),
                    DanismanAnabilimDali = s.TDAdSoyad.IsNullOrWhiteSpace() ? s.TDOBasvuruDanisman.TDAnabilimDaliAdi : s.TDAnabilimDaliAdi,
                    EsDanismanOncekiAdSoyad = s.OncekiEsDanismanAdi,
                    EsDanismanAdSoyad = s.UnvanAdi + " " + s.AdSoyad,
                    EsDanismanKurumAdi = s.UniversiteAdi

                }
                 ).ToList();
                gv.DataSource = dataEsList;
                gv.DataBind();
                raporAdi = $"{(enstituOnayDurumId == 3 ? "EYKda ONAYLANAN" : "EYKya GÖNDERİLEN")} TEZ EŞ DANIŞMAN ATAMALARI ENSTİTÜ YÖNETİM KURULU.xls";
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                var stringWriter = new StringWriter();
                var htw = new HtmlTextWriter(stringWriter);
                gv.RenderControl(htw);
                return File(System.Text.Encoding.UTF8.GetBytes(stringWriter.ToString()), Response.ContentType, raporAdi);
            }


            List<int> danismanDegisiklikTipIds;
            if (!isDegisiklikOrYeniOneri.HasValue)
                danismanDegisiklikTipIds = new List<int>
                {
                    TdoDanismanTalepTipEnum.TezDanismaniOnerisi,
                    TdoDanismanTalepTipEnum.TezDanismaniDegisikligi,
                    TdoDanismanTalepTipEnum.TezBasligiDegisikligi,
                    TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi
                };
            else if (isDegisiklikOrYeniOneri.Value)
                danismanDegisiklikTipIds = new List<int>  {
                TdoDanismanTalepTipEnum.TezDanismaniDegisikligi,
                TdoDanismanTalepTipEnum.TezBasligiDegisikligi,
                TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi
                };
            else danismanDegisiklikTipIds = new List<int>
            {
                TdoDanismanTalepTipEnum.TezDanismaniOnerisi
            };







            var qds =

                from tdoBasvuruDanisman in _entities.TDOBasvuruDanismen.Where(p => !p.IsObsData && p.TDOBasvuru.EnstituKod == enstituKod && danismanDegisiklikTipIds.Contains(p.TDODanismanTalepTipID))
                join tdoBasvuru in _entities.TDOBasvurus on tdoBasvuruDanisman.TDOBasvuruID equals tdoBasvuru.TDOBasvuruID
                join ogrenci in _entities.Kullanicilars on tdoBasvuru.KullaniciID equals ogrenci.KullaniciID
                join program in _entities.Programlars on tdoBasvuru.ProgramKod equals program.ProgramKod
                join anabilimDali in _entities.AnabilimDallaris on program.AnabilimDaliID equals anabilimDali.AnabilimDaliID
                join ogrenimTipi in _entities.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod) on tdoBasvuru.OgrenimTipKod equals ogrenimTipi.OgrenimTipKod
                join tdoTalepTipi in _entities.TDODanismanTalepTipleris on tdoBasvuruDanisman.TDODanismanTalepTipID equals tdoTalepTipi.TDODanismanTalepTipID
                select new
                {
                    tdoBasvuruDanisman.TDODanismanTalepTipID,
                    tdoTalepTipi.TalepTipAdi,
                    tdoBasvuru.OgrenciNo,
                    OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                    tdoBasvuru.OgrenimTipKod,
                    ogrenimTipi.OgrenimTipAdi,
                    program.ProgramAdi,
                    anabilimDali.AnabilimDaliAdi,
                    DanismanAdSoyad = tdoBasvuruDanisman.TDUnvanAdi + " " + tdoBasvuruDanisman.TDAdSoyad,
                    DanismanAnabilimDali = tdoBasvuruDanisman.TDAnabilimDaliAdi,
                    TezDili = tdoBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce",
                    tdoBasvuruDanisman.IsTezDiliTr,
                    tdoBasvuruDanisman.TezBaslikTr,
                    tdoBasvuruDanisman.TezBaslikEn,
                    tdoBasvuruDanisman.IsYeniTezDiliTr,
                    tdoBasvuruDanisman.YeniTezBaslikTr,
                    tdoBasvuruDanisman.YeniTezBaslikEn,
                    tdoBasvuruDanisman.VarolanTDAdSoyad,
                    tdoBasvuruDanisman.VarolanTDUnvanAdi,
                    tdoBasvuruDanisman.TDAdSoyad,
                    tdoBasvuruDanisman.TDUnvanAdi,
                    DanismanYukYlDrSayi = (tdoBasvuruDanisman.TDOgrenciSayisiDR ?? 0) + (tdoBasvuruDanisman.TDOgrenciSayisiYL ?? 0),
                    MezunSayisi = (tdoBasvuruDanisman.TDTezSayisiDR ?? 0) + (tdoBasvuruDanisman.TDTezSayisiYL ?? 0),
                    tdoBasvuruDanisman.EYKYaGonderildi,
                    tdoBasvuruDanisman.EYKYaGonderildiIslemTarihi,
                    tdoBasvuruDanisman.EYKYaHazirlandi,
                    tdoBasvuruDanisman.EYKYaHazirlandiIslemTarihi,
                    tdoBasvuruDanisman.EYKDaOnaylandi,
                    tdoBasvuruDanisman.EYKDaOnaylandiOnayTarihi,
                    tdoBasvuruDanisman.IslemTarihi
                };
            if (enstituOnayDurumId == 1)
            {
                qds = qds.Where(p => p.EYKYaGonderildi == true && !p.EYKYaHazirlandi.HasValue && p.EYKYaGonderildiIslemTarihi >= baslangicTarihi && p.EYKYaGonderildiIslemTarihi <= bitisTarihi)
                    .OrderBy(o => o.EYKYaGonderildiIslemTarihi).ThenBy(t => t.IslemTarihi);
            }
            else if (enstituOnayDurumId == 2)
            {
                qds = qds.Where(p => p.EYKYaHazirlandi == true && !p.EYKDaOnaylandi.HasValue && p.EYKYaHazirlandiIslemTarihi >= baslangicTarihi && p.EYKYaHazirlandiIslemTarihi <= bitisTarihi)
                    .OrderBy(o => o.EYKYaHazirlandiIslemTarihi).ThenBy(t => t.IslemTarihi);

            }
            else if (enstituOnayDurumId == 3)
            {
                qds = qds.Where(p => p.EYKDaOnaylandi == true && p.EYKDaOnaylandiOnayTarihi >= baslangicTarihi && p.EYKDaOnaylandiOnayTarihi <= bitisTarihi)
                    .OrderBy(o => o.EYKDaOnaylandiOnayTarihi).ThenBy(t => t.IslemTarihi);

            }

            if (tutanakTipId == 1)
            {
                var data = qds.ToList().Select((s, inx) => new
                {
                    SiraNo = inx + 1,
                    s.TalepTipAdi,
                    EYKTarihi = s.EYKDaOnaylandi == true ? s.EYKDaOnaylandiOnayTarihi : null,
                    s.OgrenciNo,
                    s.OgrenciAdSoyad,
                    OgrenciAnabilimdaliProgram = s.AnabilimDaliAdi + " / " + s.ProgramAdi,
                    s.OgrenimTipAdi,
                    s.DanismanAdSoyad,
                    s.DanismanAnabilimDali,
                    s.TezBaslikTr,
                    s.TezBaslikEn,
                    s.TezDili,
                    s.DanismanYukYlDrSayi,
                    s.MezunSayisi
                }).ToList();
                var gv = new GridView();
                gv.DataSource = data;
                gv.DataBind();
                raporAdi = $"{(enstituOnayDurumId == 2 ? "EykYa HAZIRLANAN" : (enstituOnayDurumId == 3 ? "EYKda ONAYLANAN" : "EYKya GÖNDERİLEN"))} TEZ DANIŞMAN ATAMALARI ENSTİTÜ YÖNETİM KURULU.xls";
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                var stringWriter = new StringWriter();
                var htw = new HtmlTextWriter(stringWriter);
                gv.RenderControl(htw);
                return File(System.Text.Encoding.UTF8.GetBytes(stringWriter.ToString()), Response.ContentType, raporAdi);
            }



            var raporData = qds.ToList();

            var raporListData = new List<RprTdoEykDto>();
            var raporRowNum = 0;
            var talepTipi = isDegisiklikOrYeniOneri.HasValue ? (isDegisiklikOrYeniOneri.Value ? "DEĞİŞİKLİKLERİ" : "DANIŞMAN ÖNERİLERİ") : "DANIŞMAN ÖNERİLERİ VE DEĞİŞİKLİKLERİ";
            foreach (var item in raporData)
            {
                raporRowNum++;
                var isDoktora = item.OgrenimTipKod.IsDoktora();
                var isDoktoraStr = isDoktora ? "doktora" : "yüksek lisans";
                var row = new RprTdoEykDto
                {
                    SiraNo = raporRowNum + 1,
                    OgrenciBilgi = item.OgrenciNo + " " + item.OgrenciAdSoyad +
                                   " (" + item.AnabilimDaliAdi + " / " +
                                   item.ProgramAdi + ")",
                    Title = (isDoktora ? "DOKTORA :" : "YÜKSEK LİSANS :") + talepTipi,
                    IsDoktora = item.OgrenimTipKod.IsDoktora(),
                    TDODanismanTalepTipID = item.TDODanismanTalepTipID,
                    TezDili = item.IsTezDiliTr ? "Türkçe" : "İngilizce",
                    TezBaslikTr = item.TezBaslikTr.ToUpper(),
                    TezBaslikEn = item.TezBaslikEn.Replace("i", "ı").ToUpper(),
                    YeniTezDili = item.IsYeniTezDiliTr.HasValue ? (item.IsYeniTezDiliTr.Value ? "Türkçe" : "İngilizce") : "",
                    YeniTezBaslikTr = (item.YeniTezBaslikTr ?? "").ToUpper(),
                    YeniTezBaslikEn = (item.YeniTezBaslikEn ?? "").Replace("i", "ı").ToUpper()
                };
                switch (item.TDODanismanTalepTipID)
                {
                    case TdoDanismanTalepTipEnum.TezDanismaniOnerisi:
                        row.DanismanAdSoyad = item.TDUnvanAdi + " " + item.TDAdSoyad;
                        row.DegisiklikTipID = 0;
                        row.Aciklama = "Yapılan görüşmeler sonunda, aşağıda adı ve programı belirtilen <b>" +
                                       isDoktoraStr +
                                       "</b> öğrencisi için<b> tez danışman atamasına</b> ilişkin önerinin aşağıda belirtildiği şekilde <b>kabul edilmesine, oybirliğiyle karar verildi.</b>";
                        break;
                    case TdoDanismanTalepTipEnum.TezDanismaniDegisikligi:
                        row.DanismanAdSoyad = item.VarolanTDUnvanAdi + " " + item.VarolanTDAdSoyad;
                        row.YeniTezDanismaniAdSoyad = item.TDUnvanAdi + " " + item.TDAdSoyad;
                        row.DegisiklikTipID = 1;
                        row.Aciklama = "Yapılan görüşmeler sonunda, aşağıda adı ve programı belirtilen <b>" +
                                       isDoktoraStr +
                                       "</b> öğrencisi için<b> tez danışman değişikliğine</b> ilişkin önerinin aşağıda belirtildiği şekilde <b>kabul edilmesine, oybirliğiyle karar verildi.</b>";
                        break;
                    case TdoDanismanTalepTipEnum.TezBasligiDegisikligi:
                        {
                            row.DanismanAdSoyad = item.TDUnvanAdi + " " + item.TDAdSoyad;
                            row.DegisiklikTipID = item.IsTezDiliTr == (item.IsYeniTezDiliTr ?? item.IsTezDiliTr) ? 2 : 3;
                            var subTipAdi = row.DegisiklikTipID == 2 ? "tez dili ve başlığı" : "tez başlığı";
                            row.Aciklama = "Yapılan görüşmeler sonunda, aşağıda adı ve programı belirtilen <b>" +
                                           isDoktoraStr + "</b> öğrencisi için<b> " + subTipAdi +
                                           " değişikliğine</b> ilişkin önerinin aşağıda belirtildiği şekilde <b>kabul edilmesine, oybirliğiyle karar verildi.</b>";
                            break;
                        }
                    case TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi:
                        {
                            row.DanismanAdSoyad = item.VarolanTDUnvanAdi + " " + item.VarolanTDAdSoyad;
                            row.YeniTezDanismaniAdSoyad = item.TDUnvanAdi + " " + item.TDAdSoyad;
                            row.DegisiklikTipID = item.IsTezDiliTr == (item.IsYeniTezDiliTr ?? item.IsTezDiliTr) ? 4 : 5;
                            var subTipAdi = row.DegisiklikTipID == 4
                                ? "tez danışmanı ve tez başlığı "
                                : "tez dili, tez başlığı ve tez danışmanı";
                            row.Aciklama = "Yapılan görüşmeler sonunda, aşağıda adı ve programı belirtilen <b>" +
                                           isDoktoraStr + "</b> öğrencisi için<b> " + subTipAdi +
                                           " değişikliğine</b> ilişkin önerinin aşağıda belirtildiği şekilde <b>kabul edilmesine, oybirliğiyle karar verildi.</b>";
                            break;
                        }
                }

                raporListData.Add(row);
            }

            var rpr = new RprTdoTutanak();
            rpr.DataSource = raporListData;
            rpr.CreateDocument();
            var displayName = $"{(enstituOnayDurumId == 2 ? "EykYa HAZIRLANAN" : (enstituOnayDurumId == 3 ? "EYKda ONAYLANAN" : "EYKya GÖNDERİLEN"))} Tez_dil_konu_danışman öneri ve değişiklikleri Tutanağı";
            string html;
            using (MemoryStream ms = new MemoryStream())
            {
                rpr.ExportToHtml(ms);
                ms.Position = 0;
                var sr = new StreamReader(ms);
                html = sr.ReadToEnd();
            }

            var exportWordOrExcel = tutanakTipId == 2;
            return File(System.Text.Encoding.UTF8.GetBytes(html), (exportWordOrExcel ? "application/vnd.ms-word" : "application/ms-excel"), displayName + " (" + basTar.Replace("-", ".") + "-" + bitTar.Replace("-", ".") + ")." + (exportWordOrExcel ? "doc" : "xls"));




        }

      
        [Authorize(Roles = RoleNames.TdoEykdaOnayYetkisi)]
        public ActionResult EYKDaOnay(string aktifDonemId)
        {
            var qDanismans = (from s in _entities.TDOBasvurus
                              join ard in _entities.TDOBasvuruDanismen on s.AktifTDOBasvuruDanismanID equals ard.TDOBasvuruDanismanID
                              where ard.DanismanOnayladi == true && ard.EYKYaHazirlandi == true && !ard.EYKDaOnaylandi.HasValue && (ard.DonemBaslangicYil + "" + ard.DonemID) == aktifDonemId
                              select s.TDOBasvuruDanisman
                ).ToList();
            foreach (var item in qDanismans)
            {
                item.EYKDaOnaylandi = true;
                item.EYKDaOnaylandiOnayTarihi = DateTime.Now;
                item.EYKDaOnaylandiIslemYapanID = UserIdentity.Current.Id;
            }
            _entities.SaveChanges();
            foreach (var item in qDanismans)
            {
                TdoBus.SendMailTdoEykOnay(item.TDOBasvuruDanismanID, true);
            }
            return new { qDanismans.Count }.ToJsonResult();
        }


    }
}