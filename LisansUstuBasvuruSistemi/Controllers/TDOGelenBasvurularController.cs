using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
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
        private readonly LubsDbEntities _entities = new LubsDbEntities();
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
                    let ardEs = _entities.TDOBasvuruEsDanismen.Where(p => p.TDOBasvuruDanismanID == ard.TDOBasvuruDanismanID).OrderByDescending(oe => oe.TDOBasvuruEsDanismanID).FirstOrDefault()

                    select new FrTdoBasvuruDto
                    {
                        TezDanismanID = ard.TezDanismanID,
                        TDOBasvuruID = s.TDOBasvuruID,
                        BasvuruTarihi = ard.BasvuruTarihi,
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
                        AktifTdBasvuruEsDanismanID = ardEs != null ? (int?)ardEs.TDOBasvuruEsDanismanID : null,
                        Es_EYKYaGonderildi = ardEs != null ? ardEs.EYKYaGonderildi : null,
                        Es_EYKYaHazirlandi = ardEs != null ? ardEs.EYKYaHazirlandi : null,
                        Es_EYKDaOnaylandi = ardEs != null ? ardEs.EYKDaOnaylandi : null

                    };
            var q2 = q;
            if (tdoDanismanOnayYetkisi && !tdoGelenBasvuruKayit)
            {
                q = q.Where(p => p.TezDanismanID == UserIdentity.Current.Id || p.VarolanTezDanismanID == UserIdentity.Current.Id);
            }
            q = q.Where(p => p.EnstituKod == enstituKod && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod));
            if (!model.AktifDonemID.IsNullOrWhiteSpace()) q = q.Where(p => p.AktifDonemID == model.AktifDonemID);
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.OgrenimTipKod == model.OgrenimTipKod);
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
                else if (model.AktifEsDurumID == TdoDansimanDurumuEnum.EykYaHazirlanmaBekleniyor) q = q.Where(p => p.Es_EYKYaGonderildi == true && !p.Es_EYKYaHazirlandi.HasValue);
                else if (model.AktifEsDurumID == TdoDansimanDurumuEnum.EykYaHazirlandi) q = q.Where(p => p.Es_EYKYaHazirlandi == true && !p.Es_EYKDaOnaylandi.HasValue);
                else if (model.AktifEsDurumID == TdoDansimanDurumuEnum.EykDaOnayBekleniyor) q = q.Where(p => p.Es_EYKYaGonderildi == true && !p.Es_EYKDaOnaylandi.HasValue);
                else if (model.AktifEsDurumID == TdoDansimanDurumuEnum.EykDaOnaylandi) q = q.Where(p => p.Es_EYKDaOnaylandi == true);
                else if (model.AktifEsDurumID == TdoDansimanDurumuEnum.EykDaOnaylanmadi) q = q.Where(p => p.Es_EYKDaOnaylandi == false);
            }
            if (model.TDOBasvuruID.HasValue) q = q.Where(p => p.TDOBasvuruID == model.TDOBasvuruID);

            if (!model.AdSoyad.IsNullOrWhiteSpace())
            {
                var tdoBasvuruIds = q.Select(s => s.TDOBasvuruID).ToList();
                var formKoduBasvuruIds = _entities.TDOBasvuruDanismen.Where(p => tdoBasvuruIds.Contains(p.TDOBasvuruID) && p.FormKodu == model.AdSoyad).Select(s => s.TDOBasvuruID).Distinct().ToList();
                var formKoduEsBasvuruIds = _entities.TDOBasvuruEsDanismen.Where(p => tdoBasvuruIds.Contains(p.TDOBasvuruDanisman.TDOBasvuruID) && p.FormKodu == model.AdSoyad).Select(s => s.TDOBasvuruDanisman.TDOBasvuruID).Distinct().ToList();
                var danismanAdSoyadBasvuruIds = _entities.TDOBasvuruDanismen.Where(p => tdoBasvuruIds.Contains(p.TDOBasvuruID) && p.TDAdSoyad.Contains(model.AdSoyad)).Select(s => s.TDOBasvuruID).Distinct().ToList();
                formKoduBasvuruIds.AddRange(formKoduEsBasvuruIds);
                formKoduBasvuruIds.AddRange(danismanAdSoyadBasvuruIds);
                q = q.Where(p =>
                     p.AdSoyad.Contains(model.AdSoyad)
                     || p.OgrenciNo.Contains(model.AdSoyad)
                     || formKoduBasvuruIds.Contains(p.TDOBasvuruID));
            }

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
                    s.TDOBasvuruDanisman?.BasvuruTarihi,
                    DanismanUnvanAdi = s.TDOBasvuruDanisman != null ? (s.TDOBasvuruDanisman.TDUnvanAdi) : "Danışman Yok",
                    DanismanAdSoyad = s.TDOBasvuruDanisman != null ? (s.TDOBasvuruDanisman.TDAdSoyad) : "Danışman Yok",
                    DanismanOnayladi = s.TDOBasvuruDanisman != null ? (s.DanismanOnayladi.HasValue ? (s.DanismanOnayladi.Value ? "Danışman Onayladı" : "Danışman Onaylamadı") : "Danışman Onayı Bekleniyor") : "Danışman Yok",
                    EYKYaGonderildi = s.TDOBasvuruDanisman != null ? (s.EYKYaGonderildi.HasValue ? (s.EYKYaGonderildi.Value ? "EYK'ya Gönderimi Onaylandı" : "EYK'ya Gönderimi Onaylanmadı") : "EYK'ya Gönderim Onayı Bekleniyor") : "Danışman Yok",
                    EykYaGonderilmeTarihi = s.TDOBasvuruDanisman?.EYKYaGonderildiIslemTarihi,
                    EYKYaHazirlandi = s.TDOBasvuruDanisman != null ? (s.EYKYaHazirlandi.HasValue ? (s.EYKYaHazirlandi.Value ? "EYK'ya Hazırlandı" : "EYK'ya Hazrılanmadı") : "EYK'ya Hazırlanması Bekleniyor") : "Danışman Yok",
                    EykYaHazirlanmaTarihi = s.TDOBasvuruDanisman?.EYKYaHazirlandiIslemTarihi,
                    EYKDaOnaylandi = s.TDOBasvuruDanisman != null ? (s.EYKDaOnaylandi.HasValue ? (s.EYKDaOnaylandi.Value ? "EYK'da Onaylandı" : "EYK'da Onaylanmadı") : "EYK'da Onay işlemi Bekleniyor") : "Danışman Yok",
                    EykDaOnaylanmaTarihi = s.TDOBasvuruDanisman?.EYKDaOnaylandiOnayTarihi,
                    EsDanismanAdSoyad = s.TDOBasvuruDanisman != null && s.TDOBasvuruDanismen.Any(s2 => s2.TDOBasvuruEsDanismen.Any()) ? (s.TDOBasvuruDanisman.TDUnvanAdi + " " + s.TDOBasvuruDanisman.TDAdSoyad) : "Danışman Yok",
                    EsDanismanOnayladi = s.TDOBasvuruDanisman != null && s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.Any() ? (s.DanismanOnayladi.HasValue ? (s.DanismanOnayladi.Value ? "Danışman Onayladı" : "Danışman Onaylamadı") : "Danışman Onayı Bekleniyor") : "Danışman Yok",
                    EsDanismanEYKYaGonderildi = s.TDOBasvuruDanisman != null && s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.Any() ? (s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.First().EYKYaGonderildi.HasValue ? (s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.First().EYKYaGonderildi.Value ? "EYK'ya Gönderimi Onaylandı" : "EYK'ya Gönderimi Onaylanmadı") : "EYK'ya Gönderim Onayı Bekleniyor") : "Eş Danışman Yok",
                    EsDanismanEYKDaOnaylandi = s.TDOBasvuruDanisman != null && s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.Any() ? (s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.First().EYKDaOnaylandi.HasValue ? (s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.First().EYKDaOnaylandi.Value ? "EYK'da Onaylandı" : "EYK'da Onaylanmadı") : "EYK'da Onay işlemi Bekleniyor") : "Eş Danışman Yok",

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

            if (model.AktifDurumID == TdoDansimanDurumuEnum.EykYaHazirlandi || model.AktifDurumID == TdoDansimanDurumuEnum.EykDaOnaylandi)
            {
                model.SelectedTdoBasvuruDanismanIds = q.Where(p => p.AktifTDOBasvuruDanismanID.HasValue).Select(s => s.AktifTDOBasvuruDanismanID.Value).ToList();
            }
            if (model.AktifEsDurumID == TdoDansimanDurumuEnum.EykYaHazirlandi || model.AktifEsDurumID == TdoDansimanDurumuEnum.EykDaOnaylandi)
            {
                model.SelectedTdoBasvuruEsDanismanIds = q.Where(p => p.EsDanismanOnerisiVar).Select(s => s.AktifTdBasvuruEsDanismanID.Value).ToList();
            }
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else if (model.AktifDurumID == 5 || model.DurumID == 5)
                q = q.OrderBy(o => o.EYKYaGonderildiIslemTarihi);
            else if (model.AktifEsDurumID == 5 || model.EsDurumID == 5)
                q = q.OrderBy(o => o.EYKYaGonderildiIslemTarihiES);
            else q = q.OrderByDescending(o => o.BasvuruTarihi);
            model.TdoBasvuruDtos = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.filteredOgrenciIds = isFiltered ? q.Select(s => s.KullaniciID).Distinct().ToList() : new List<int>();
            ViewBag.filteredDanismanIds = isFiltered ? q.Where(p => p.TezDanismanID.HasValue).Select(s => s.TezDanismanID.Value).Distinct().ToList() : new List<int>();
            ViewBag.AktifDonemID = new SelectList(TdoBus.CmbTdoDonemListe(enstituKod, true), "Value", "Caption", model.AktifDonemID);
            ViewBag.TDODanismanTalepTipID = new SelectList(TdoBus.CmbTdoDanismanTalepTip(true), "Value", "Caption", model.TDODanismanTalepTipID);
            ViewBag.AktifDurumID = new SelectList(TdoBus.CmbTdoOneriDurumListe(true), "Value", "Caption", model.AktifDurumID);
            ViewBag.DurumID = new SelectList(TdoBus.CmbTdoOneriDurumListe(true), "Value", "Caption", model.DurumID);
            ViewBag.AktifEsDurumID = new SelectList(TdoBus.CmbTdoEsOneriDurumListe(true), "Value", "Caption", model.AktifEsDurumID);
            ViewBag.EsDurumID = new SelectList(TdoBus.CmbTdoEsOneriDurumListe(true), "Value", "Caption", model.EsDurumID);
            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipKodYuksekLisans(enstituKod, true), "Value", "Caption", model.OgrenimTipKod);
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
            var enstitu = EnstituBus.GetEnstitu(enstituKod);
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
                var data = qes.ToList().Select((s, inx) => new
                {
                    SiraNo = inx + 1,
                    EsDanismanOnerisi_EsDanısmanDegisikligi = s.IsDegisiklikTalebi ? "Eş Danışman Değişikliği" : "Eş Danışman Önerisi",
                    s.TDOBasvuruDanisman.TDOBasvuru.OgrenciNo,
                    OgrenciAdSoyad = s.TDOBasvuruDanisman.TDOBasvuru.Kullanicilar.Ad + " " + s.TDOBasvuruDanisman.TDOBasvuru.Kullanicilar.Soyad,
                    OgrenciAnabilimDali = s.TDOBasvuruDanisman.TDOBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi + " / " + s.TDOBasvuruDanisman.TDOBasvuru.Programlar.ProgramAdi,
                    YL_DR = s.TDOBasvuruDanisman.TDOBasvuru.OgrenimTipKod.IsDoktora() ? "DR" : "YL",
                    DanismanAdSoyad = s.TDAdSoyad.IsNullOrWhiteSpace() ? s.TDOBasvuruDanisman.TDUnvanAdi + " " + s.TDOBasvuruDanisman.TDAdSoyad : (s.TDUnvanAdi + " " + s.TDAdSoyad),
                    EsDanismanOncekiAdSoyad = s.OncekiEsDanismanAdi,
                    EsDanismanAdSoyad = s.UnvanAdi + " " + s.AdSoyad,
                    EsDanismanKurumAdi = s.UniversiteAdi

                }
                 ).ToList();
                //gv.DataSource = dataEsList;
                //gv.DataBind();
                //raporAdi = $"{(enstituOnayDurumId == 3 ? "EYKda ONAYLANAN" : "EYKya GÖNDERİLEN")} TEZ EŞ DANIŞMAN ATAMALARI ENSTİTÜ YÖNETİM KURULU.xls";
                //Response.ContentType = "application/ms-excel";
                //Response.ContentEncoding = System.Text.Encoding.UTF8;
                //Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                //var stringWriter = new StringWriter();
                //var htw = new HtmlTextWriter(stringWriter);
                //gv.RenderControl(htw);
                //return File(System.Text.Encoding.UTF8.GetBytes(stringWriter.ToString()), Response.ContentType, raporAdi);


                gv.AutoGenerateColumns = false; // Sütunları manuel olarak tanımlamak için gerekli

                // Sütunları tanımlayın ve başlıklarını belirleyin
                gv.Columns.Add(new BoundField { DataField = "SiraNo", HeaderText = "Sıra No" });
                gv.Columns.Add(new BoundField { DataField = "EsDanismanOnerisi_EsDanısmanDegisikligi", HeaderText = "Eş Danışman Önerisi / Değişikliği" });
                gv.Columns.Add(new BoundField { DataField = "OgrenciNo", HeaderText = "Öğrenci No" });
                gv.Columns.Add(new BoundField { DataField = "OgrenciAdSoyad", HeaderText = "Öğrenci Ad Soyad" });
                gv.Columns.Add(new BoundField { DataField = "OgrenciAnabilimDali", HeaderText = "Öğrenci Anabilim Dalı" });
                gv.Columns.Add(new BoundField { DataField = "YL_DR", HeaderText = "YL_DR" });
                gv.Columns.Add(new BoundField { DataField = "DanismanAdSoyad", HeaderText = "Danışman Ad Soyad" });
                gv.Columns.Add(new BoundField { DataField = "EsDanismanOncekiAdSoyad", HeaderText = "Önceki Eş Danışman Ad Soyad" });
                gv.Columns.Add(new BoundField { DataField = "EsDanismanAdSoyad", HeaderText = "Eş Danışman Ad Soyad" });
                gv.Columns.Add(new BoundField { DataField = "EsDanismanKurumAdi", HeaderText = "Eş Danışman Kurum Adı" });

                gv.DataSource = data;
                gv.DataBind();

                raporAdi = $"{(enstituOnayDurumId == 3 ? "EYKda ONAYLANAN" : "EYKya GÖNDERİLEN")} TEZ EŞ DANIŞMAN ATAMALARI ENSTİTÜ YÖNETİM KURULU.xls";
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());

                var stringWriter = new StringWriter();
                var htw = new HtmlTextWriter(stringWriter);

                var title =
                    $"YTÜ-{enstitu.EnstituAd.ToUpper()} <br>EŞ DANIŞMAN ATAMALARI<br>..../.. GÜN VE SAYILI ENSTİTÜ YÖNETİM KURULU  <br>(EK-..)";

                // HTML formatında başlık ekleyin
                htw.Write("<table>");
                htw.Write("<tr>");
                htw.Write("<td colspan='10' style='font-weight: bold; text-align: center;'>");
                htw.Write(title);
                htw.Write("</td>");
                htw.Write("</tr>");
                htw.Write("</table>");

                // GridView'i oluşturun
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
                    tdoBasvuruDanisman.EYKYaHazirlandiAciklamasi,
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
                    s.OgrenciNo,
                    s.OgrenciAdSoyad,
                    OgrenciAnabilimdaliProgram = s.AnabilimDaliAdi + " / " + s.ProgramAdi,
                    s.OgrenimTipAdi,
                    s.DanismanAdSoyad,
                    s.TezBaslikTr,
                    s.TezBaslikEn,
                    s.TezDili,
                    s.TalepTipAdi
                }).ToList();

                var gv = new GridView();
                gv.AutoGenerateColumns = false; // Sütunları manuel olarak tanımlamak için gerekli

                // Sütunları tanımlayın ve başlıklarını belirleyin
                gv.Columns.Add(new BoundField { DataField = "SiraNo", HeaderText = "Sıra No" });
                gv.Columns.Add(new BoundField { DataField = "OgrenciNo", HeaderText = "Öğrenci No" });
                gv.Columns.Add(new BoundField { DataField = "OgrenciAdSoyad", HeaderText = "Öğrenci Ad Soyad" });
                gv.Columns.Add(new BoundField { DataField = "OgrenciAnabilimdaliProgram", HeaderText = "Öğrenci Anabilim Dalı / Program" });
                gv.Columns.Add(new BoundField { DataField = "OgrenimTipAdi", HeaderText = "YL_DR" });
                gv.Columns.Add(new BoundField { DataField = "DanismanAdSoyad", HeaderText = "Danışman Ad Soyad" });
                gv.Columns.Add(new BoundField { DataField = "TezBaslikTr", HeaderText = "Tez Başlığı Türkçe" });
                gv.Columns.Add(new BoundField { DataField = "TezBaslikEn", HeaderText = "Tez Başlığı İngilizce" });
                gv.Columns.Add(new BoundField { DataField = "TezDili", HeaderText = "Tez Dili" });
                gv.Columns.Add(new BoundField { DataField = "TalepTipAdi", HeaderText = "Talep Tipi" });

                gv.DataSource = data;
                gv.DataBind();

                raporAdi = $"{(enstituOnayDurumId == 2 ? "EykYa HAZIRLANAN" : (enstituOnayDurumId == 3 ? "EYKda ONAYLANAN" : "EYKya GÖNDERİLEN"))} TEZ DANIŞMAN ATAMALARI ENSTİTÜ YÖNETİM KURULU.xls";
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());

                var stringWriter = new StringWriter();
                var htw = new HtmlTextWriter(stringWriter);

                var title =
                    $"YTÜ-{enstitu.EnstituAd.ToUpper()} <br>TEZ DANIŞMAN ATAMALARI<br>..../.. GÜN VE SAYILI ENSTİTÜ YÖNETİM KURULU  <br>(EK-..)";

                // HTML formatında başlık ekleyin
                htw.Write("<table>");
                htw.Write("<tr>");
                htw.Write("<td colspan='10' style='font-weight: bold; text-align: center;'>");
                htw.Write(title);
                htw.Write("</td>");
                htw.Write("</tr>");
                htw.Write("</table>");

                // GridView'i oluşturun
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
                    OgrenciNo = item.OgrenciNo,
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
                    YeniTezBaslikEn = (item.YeniTezBaslikEn ?? "").Replace("i", "ı").ToUpper(),
                    EYKYaHazirlandiAciklamasi = item.EYKYaHazirlandiAciklamasi
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
            var exportWordOrExcel = tutanakTipId == 2;

            var strOgrenciNos = "";
            if (!exportWordOrExcel)
            {
                var ogrenciNos = raporListData.Select(s => s.OgrenciNo).Distinct().Where(p => !p.IsNullOrWhiteSpace()).ToList();
                strOgrenciNos = string.Join(" ", ogrenciNos);
            }

            var rpr = new RprTdoTutanak(strOgrenciNos);
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

            return File(System.Text.Encoding.UTF8.GetBytes(html), (exportWordOrExcel ? "application/vnd.ms-word" : "application/ms-excel"), displayName + " (" + basTar.Replace("-", ".") + "-" + bitTar.Replace("-", ".") + ")." + (exportWordOrExcel ? "doc" : "xls"));




        }


        [Authorize(Roles = RoleNames.TdoEykdaOnayYetkisi)]
        public ActionResult EYKDaOnay(List<int> selectedTdoBasvuruDanismanIds)
        {
            selectedTdoBasvuruDanismanIds = selectedTdoBasvuruDanismanIds ?? new List<int>();
            var qDanismans = _entities.TDOBasvuruDanismen.Where(p =>
                    selectedTdoBasvuruDanismanIds.Contains(p.TDOBasvuruDanismanID) &&
                    p.DanismanOnayladi == true && p.EYKYaHazirlandi == true && !p.EYKDaOnaylandi.HasValue)
                .ToList();
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

        [Authorize(Roles = RoleNames.TdoEykdaOnayYetkisi)]
        public ActionResult EYKDaOnayEs(List<int> selectedTdoBasvuruEsDanismanIds)
        {
            selectedTdoBasvuruEsDanismanIds = selectedTdoBasvuruEsDanismanIds ?? new List<int>();
            var qDanismans = _entities.TDOBasvuruEsDanismen.Where(p =>
                    selectedTdoBasvuruEsDanismanIds.Contains(p.TDOBasvuruEsDanismanID) &&
                    p.EYKYaHazirlandi == true && !p.EYKDaOnaylandi.HasValue)
                .ToList();
            foreach (var item in qDanismans)
            {
                item.EYKDaOnaylandi = true;
                item.EYKDaOnaylandiOnayTarihi = DateTime.Now;
                item.EYKDaOnaylandiIslemYapanID = UserIdentity.Current.Id;
            }
            _entities.SaveChanges();
            foreach (var item in qDanismans)
            {
                TdoBus.SendMailTdoEsEykOnay(item.TDOBasvuruEsDanismanID, true);
            }
            return new { qDanismans.Count }.ToJsonResult();
        }
    }
}