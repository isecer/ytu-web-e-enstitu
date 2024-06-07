using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
using BiskaUtil;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Raporlar.DonemProjesi;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize(Roles = RoleNames.DonemProjesiGelenBasvurular)]
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class DpGelenBasvurularController : Controller
    {
        // GET: DpGelenBasvurular
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        [AllowAnonymous]
        public ActionResult Index(string ekd)
        {
            if (!UserIdentity.Current.IsAuthenticated) return RedirectToActionPermanent("Login", "Account");

            return Index(new FmDonemProjesiBasvuruDto() { PageSize = 50 }, ekd);
        }
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Index(FmDonemProjesiBasvuruDto model, string ekd, bool export = false)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var isDegerlendirmeSurecinde = model.DonemProjesiDurumID == DonemProjesiDurumEnum.SinavDegerlendirmeSureci;


            if (!UserIdentity.Current.IsAuthenticated && !model.IsDegerlendirme.HasValue) return RedirectToActionPermanent("Login", "Account");

            var q =
                    from donemProjesi in _entities.DonemProjesis
                    join ogrenci in _entities.Kullanicilars on donemProjesi.KullaniciID equals ogrenci.KullaniciID
                    join programlar in _entities.Programlars on donemProjesi.ProgramKod equals programlar.ProgramKod
                    join ogrenimTipleri in _entities.OgrenimTipleris on new { donemProjesi.EnstituKod, donemProjesi.OgrenimTipKod } equals new { ogrenimTipleri.EnstituKod, ogrenimTipleri.OgrenimTipKod }
                    let sonBasvuru = donemProjesi.DonemProjesiBasvurus.OrderByDescending(o => o.BasvuruTarihi).FirstOrDefault()

                    select new FrDonemProjesiBasvuruDto
                    {
                        EnstituKod = donemProjesi.EnstituKod,
                        DonemProjesiID = donemProjesi.DonemProjesiID,
                        UniqueID = donemProjesi.UniqueID,
                        DonemProjesiDurumAdi = sonBasvuru != null ? sonBasvuru.DonemProjesiDurumlari.DonemProjesiDurumAdi : "Başvuru Tamamlanmadı",
                        IsYeniBasvuruYapilabilir = donemProjesi.IsYeniBasvuruYapilabilir,
                        BasvuruTarihi = sonBasvuru != null ? sonBasvuru.BasvuruTarihi : donemProjesi.BasvuruTarihi,
                        ResimAdi = ogrenci.ResimAdi,
                        UserKey = ogrenci.UserKey,
                        AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                        TcKimlikNo = ogrenci.TcKimlikNo,
                        OgrenciNo = donemProjesi.OgrenciNo,
                        EMail = ogrenci.EMail,
                        OgrenimTipAdi = ogrenimTipleri.OgrenimTipAdi,
                        AnabilimDaliId = programlar.AnabilimDaliID,
                        ProgramAdi = programlar.ProgramAdi,
                        AnabilimDaliAdi = programlar.AnabilimDallari.AnabilimDaliAdi,
                        DonemAdi = sonBasvuru == null ? "Başvuru Yapılmadı" : (sonBasvuru.BasvuruYil + " / " + (sonBasvuru.BasvuruYil + 1) + " " + (sonBasvuru.BasvuruDonemID == 1 ? "Güz" : "Bahar")),
                        ToplantiTarihi = sonBasvuru != null ? sonBasvuru.SRTalepleris.Select(sr => sr.Tarih).FirstOrDefault() : (DateTime?)null,
                        ToplantiSaati = sonBasvuru != null ? sonBasvuru.SRTalepleris.Select(sr => sr.BasSaat).FirstOrDefault() : (TimeSpan?)null,
                        DonemProjesiDurumID = sonBasvuru != null ? sonBasvuru.DonemProjesiDurumID : DonemProjesiDurumEnum.BasvuruTamamlanmadi,
                        FormKodu = sonBasvuru != null ? sonBasvuru.FormKodu : "",
                        DonemProjesiBasvuruID = sonBasvuru != null ? sonBasvuru.DonemProjesiBasvuruID : 0,
                        SonBasvuruDurum = sonBasvuru != null ? new DpBasvuruDurumDto
                        {
                            DonemProjesiID = sonBasvuru.DonemProjesiID,
                            DonemProjesiBasvuruID = sonBasvuru.DonemProjesiBasvuruID,
                            BasvuruYil = sonBasvuru.BasvuruYil,
                            BasvuruDonemID = sonBasvuru.BasvuruDonemID,
                            BasvuruDonemAdi = sonBasvuru.Donemler.DonemAdi,
                            BasvuruTarihi = sonBasvuru.BasvuruTarihi,
                            IsDanismanOnay = sonBasvuru.IsDanismanOnay,
                            DanismanOnayAciklama = sonBasvuru.DanismanOnayAciklama,
                            EYKYaGonderildi = sonBasvuru.EYKYaGonderildi,
                            EYKYaGonderimDurumAciklamasi = sonBasvuru.EYKYaGonderimDurumAciklamasi,
                            EYKYaHazirlandi = sonBasvuru.EYKYaHazirlandi,
                            EYKDaOnaylandi = sonBasvuru.EYKDaOnaylandi,
                            EYKDaOnaylanmadiDurumAciklamasi = sonBasvuru.EYKDaOnaylanmadiDurumAciklamasi,
                            IsOyBirligiOrCoklugu = sonBasvuru.IsOyBirligiOrCoklugu,
                            DonemProjesiEnstituOnayDurumID = sonBasvuru.DonemProjesiEnstituOnayDurumID,
                            EnstituOnayDurumAdi = sonBasvuru.DonemProjesiEnstituOnayDurumlari.EnstituOnayDurumAdi,
                            EnstituOnayAciklama = sonBasvuru.EnstituOnayAciklama,
                            DonemProjesiDurumID = sonBasvuru.DonemProjesiDurumID,
                            DonemProjesiJuriOnayDurumID = sonBasvuru.DonemProjesiJuriOnayDurumID,
                            DonemProjesiJuriOnayDurumAdi = sonBasvuru.DonemProjesiJuriOnayDurumlari.JuriOnayDurumAdi,
                            EykTarihi = sonBasvuru.EYKTarihi,
                            IsJuriOlusturuldu = sonBasvuru.DonemProjesiJurileris.Any(),
                            IsSrTalebiYapildi = sonBasvuru.SRTalepleris.Any(),

                        } : null,
                        AkademikDonemID = (sonBasvuru.BasvuruYil + "" + sonBasvuru.BasvuruDonemID),
                        KullaniciID = donemProjesi.KullaniciID,
                        TezDanismanId = sonBasvuru.TezDanismanID,
                        OnayYapmayanJuriEmails = sonBasvuru.DonemProjesiJurileris.Where(p => isDegerlendirmeSurecinde && p.IsLinkGonderildi == true && !p.DonemProjesiJuriOnayDurumID.HasValue).Select(ss => ss.EMail).ToList()
                    };
            q = q.Where(p => p.EnstituKod == enstituKod && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod));
            var q2 = q;
            if (!model.AkademikDonemID.IsNullOrWhiteSpace()) q = q.Where(p => p.AkademikDonemID == model.AkademikDonemID);

            if (model.AnabilimDaliID.HasValue) q = q.Where(p => p.AnabilimDaliId == model.AnabilimDaliID);

            if (model.DonemProjesiDurumID.HasValue)
            {
                if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.BasvuruTamamlanmadi) q = q.Where(p => p.SonBasvuruDurum == null);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EnstituOnayiBekliyor) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituOnaySureci && !p.SonBasvuruDurum.DonemProjesiEnstituOnayDurumID.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EnstituTarafindanOnaylandi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituOnaySureci && p.SonBasvuruDurum.DonemProjesiEnstituOnayDurumID == DonemProjesiEnstituOnayDurumEnum.KabulEdildi);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EnstituTarafindanOnaylanmadi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituOnaySureci && p.SonBasvuruDurum.DonemProjesiEnstituOnayDurumID.HasValue && p.SonBasvuruDurum.DonemProjesiEnstituOnayDurumID != DonemProjesiEnstituOnayDurumEnum.KabulEdildi);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.YurutucuOnayiBekliyor) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.YurutucuOnaySureci && !p.SonBasvuruDurum.IsDanismanOnay.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.YurutucuTarafindanOnaylandi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.YurutucuOnaySureci && p.SonBasvuruDurum.IsDanismanOnay == true);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.YurutucuTarafindanOnaylanmadi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.YurutucuOnaySureci && p.SonBasvuruDurum.IsDanismanOnay == false);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.JuriSinavOlusturmaSureci) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.JuriSinavOlusturmaSureci);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.SinavDegerlendirmeSureci) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.SinavDegerlendirmeSureci);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EykYaGonderimOnayiBekleniyor) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && !p.SonBasvuruDurum.EYKYaGonderildi.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EykYaGonderimiOnaylandi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && p.SonBasvuruDurum.EYKYaGonderildi == true && !p.SonBasvuruDurum.EYKYaHazirlandi.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EykYaGonderimiOnaylanmadi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && p.SonBasvuruDurum.EYKYaGonderildi == false && !p.SonBasvuruDurum.EYKYaHazirlandi.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EykYaHazirlandi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && p.SonBasvuruDurum.EYKYaHazirlandi == true && !p.SonBasvuruDurum.EYKDaOnaylandi.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EykDaOnaylandi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && p.SonBasvuruDurum.EYKDaOnaylandi == true && p.SonBasvuruDurum.EYKYaHazirlandi.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EykDaOnaylanmadi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && p.SonBasvuruDurum.EYKDaOnaylandi == false && p.SonBasvuruDurum.EYKYaHazirlandi.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.BasariliOlanlar) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && !p.SonBasvuruDurum.EYKYaGonderildi.HasValue && p.SonBasvuruDurum.DonemProjesiJuriOnayDurumID == DonemProjesiJuriOnayDurumEnum.Basarili);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.BasarisizOlanlar) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && !p.SonBasvuruDurum.EYKDaOnaylandi.HasValue && p.SonBasvuruDurum.DonemProjesiJuriOnayDurumID != DonemProjesiJuriOnayDurumEnum.Basarili);
            }


            model.IsFiltered = !Equals(q, q2);
            if (DonemProjesiBus.IsProjeYurutucusu())
            {
                q = q.Where(p => p.TezDanismanId == UserIdentity.Current.Id);
                q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.DonemProjesiDurumID).ThenByDescending(o => o.BasvuruTarihi);
            }
            else
            {
                q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            }
            var sonbasvurularIds = q.Select(s => s.DonemProjesiBasvuruID).ToList();

            if (!model.AdSoyad.IsNullOrWhiteSpace())
            {
                var donemProjesiIds = q.Select(s => s.DonemProjesiID).ToList();
                var formKodusAnyIds = _entities.DonemProjesiBasvurus
                    .Where(p => donemProjesiIds.Contains(p.DonemProjesiID) && p.FormKodu.Contains(model.AdSoyad))
                    .Select(s => s.DonemProjesiID).Distinct().ToList();

                var sonbasvurularJuriAnyIds = _entities.DonemProjesiJurileris
                     .Where(p => sonbasvurularIds.Contains(p.DonemProjesiBasvuruID) && p.AdSoyad.Contains(model.AdSoyad))
                     .Select(s => s.DonemProjesiBasvuruID).ToList();


                q = q.Where(p =>
                    p.AdSoyad.Contains(model.AdSoyad)
                    || p.OgrenciNo.Contains(model.AdSoyad)
                    || p.TcKimlikNo.Contains(model.AdSoyad)
                    || formKodusAnyIds.Any(a => a == p.DonemProjesiID)
                    || sonbasvurularJuriAnyIds.Any(a => a == p.DonemProjesiBasvuruID)
                    );

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
                    s.AnabilimDaliAdi,
                    s.ProgramAdi,
                    TezDanismanID = s.TezDanismanId,
                    s.DonemAdi,
                    s.DonemProjesiDurumAdi,
                    EnstituBasvuruOnayDurumu = s.SonBasvuruDurum.DonemProjesiEnstituOnayDurumID.HasValue ? s.SonBasvuruDurum.EnstituOnayDurumAdi : "İşlem Bekliyor",
                    YurutucuOnayDurumu = s.SonBasvuruDurum.IsDanismanOnay.HasValue ? s.SonBasvuruDurum.IsDanismanOnay.Value ? "P.Yürütücüsü Onayladı" : "P.Yürütücüsü Onaylamadı" : "İşlem Bekliyor",
                    s.ToplantiTarihi,
                    s.ToplantiSaati,
                    SinavSonucu = s.SonBasvuruDurum.DonemProjesiJuriOnayDurumID.HasValue ? (s.SonBasvuruDurum.IsOyBirligiOrCoklugu == true ? "Oy Birliği İle " : "Oy Çokluğu İle ") + s.SonBasvuruDurum.DonemProjesiJuriOnayDurumAdi : "İşlem Bekliyor",
                    EYKYaGonderildi = s.SonBasvuruDurum.EYKYaGonderildi.HasValue ? s.SonBasvuruDurum.EYKYaGonderildi.Value ? "Eyk'ya gönderildi" : "Eyk'ya gönderilmedi" : "İşlem Bekliyor",
                    EYKYaHazirlandi = s.SonBasvuruDurum.EYKYaHazirlandi.HasValue ? "Eyk'ya hazırlandı" : "İşlem Bekliyor",
                    EYKTarihi = s.SonBasvuruDurum.EYKDaOnaylandi.HasValue ? s.SonBasvuruDurum.EykTarihi : null,
                    EYKDaOnaylandi = s.SonBasvuruDurum.EYKDaOnaylandi.HasValue ? s.SonBasvuruDurum.EYKDaOnaylandi.Value ? "Eyk'da onaylandı" : "Eyk'ya onaylanmadı" : "İşlem Bekliyor",

                }).ToList();
                var danismanIds = data.Select(s => s.TezDanismanID).ToList();
                var danismans = _entities.Kullanicilars.Where(p => danismanIds.Contains(p.KullaniciID))
                    .Select(s => new { s.KullaniciID, ProjeYurutucusu = s.Unvanlar.UnvanAdi + " " + s.Ad + " " + s.Soyad }).ToList();

                var exportData = (from s in data
                                  join d in danismans on s.TezDanismanID equals d.KullaniciID
                                  select new
                                  {
                                      BasvuruDonemi = s.DonemAdi,
                                      BasvuruDurumu = s.DonemProjesiDurumAdi,
                                      s.OgrenciNo,
                                      s.TcKimlikNo,
                                      s.AdSoyad,
                                      s.EMail,
                                      s.AnabilimDaliAdi,
                                      s.ProgramAdi,
                                      d.ProjeYurutucusu,
                                      s.EnstituBasvuruOnayDurumu,
                                      s.YurutucuOnayDurumu,
                                      s.ToplantiTarihi,
                                      s.ToplantiSaati,
                                      s.SinavSonucu,
                                      s.EYKYaGonderildi,
                                      s.EYKYaHazirlandi,
                                      s.EYKDaOnaylandi,
                                      s.EYKTarihi
                                  }).ToList();

                gv.DataSource = exportData;
                gv.DataBind();
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                var sw = new StringWriter();
                var htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_DonemProjesiBasvuruListesi_" + DateTime.Now.ToFormatDate() + ".xls");
            }
            #endregion

            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();

            ViewBag.filteredOgrenciIds = model.IsFiltered ? q.Select(s => s.KullaniciID).ToList() : new List<int>();
            ViewBag.filteredDanismanIds = model.IsFiltered ? q.Where(p => p.TezDanismanId > 0).Select(s => s.TezDanismanId.Value).Distinct().ToList() : new List<int>();

            if (model.IsFiltered && isDegerlendirmeSurecinde)
            {
                var sonBasvuruDonemProjesiBasvuruIds = q.Select(s => s.DonemProjesiBasvuruID).ToList();
                var onayYapmayanJuriEmails = _entities.DonemProjesiJurileris
                      .Where(p => sonBasvuruDonemProjesiBasvuruIds.Contains(p.DonemProjesiBasvuruID) &&
                                  p.IsLinkGonderildi == true && !p.DonemProjesiJuriOnayDurumID.HasValue)
                      .Select(ss => ss.EMail).Distinct().ToList();


                ViewBag.onayYapmayanJuriEmails = onayYapmayanJuriEmails;
            }
            else
            {
                ViewBag.onayYapmayanJuriEmails = new List<string>();
            }


            ViewBag.AkademikDonemID = new SelectList(DonemProjesiBus.CmbDpDonemListe(enstituKod, true), "Value", "Caption", model.AkademikDonemID);
            ViewBag.DonemProjesiDurumID = new SelectList(DonemProjesiBus.CmbDpDurumListe(true), "Value", "Caption", model.DonemProjesiDurumID);
            ViewBag.AnabilimDaliID = new SelectList(DonemProjesiBus.GetCmbFilterDpAnabilimDallari(enstituKod, true), "Value", "Caption", model.AnabilimDaliID);

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
            if (!mMessage.IsSuccess)
            {

                mMessage.Title = "Tutanak çıktısı oluşturulamadı";
                mMessage.MessageType = MsgTypeEnum.Warning;
            }
            return mMessage.ToJsonResult();
        }
        public ActionResult GetTutanakRaporuExport(string basTar, string bitTar, bool exportWordOrExcel, int enstituOnayDurumId, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var enstitu = _entities.Enstitulers.First(f => f.EnstituKod == enstituKod);
            var baslangicTarihi = basTar.ToDate(DateTime.Now);
            var bitisTarihi = bitTar.ToDate(DateTime.Now);

            var tutanakData = (from donemProjesiBasvuru in _entities.DonemProjesiBasvurus
                               join donemProjesi in _entities.DonemProjesis on donemProjesiBasvuru.DonemProjesiID equals donemProjesi.DonemProjesiID
                               join tezDanisman in _entities.Kullanicilars on donemProjesiBasvuru.TezDanismanID equals tezDanisman.KullaniciID
                               join program in _entities.Programlars on donemProjesi.ProgramKod equals program.ProgramKod
                               join anabilimDali in _entities.AnabilimDallaris on program.AnabilimDaliID equals anabilimDali
                                   .AnabilimDaliID
                               join ogrenci in _entities.Kullanicilars on donemProjesi.KullaniciID equals ogrenci.KullaniciID
                               where donemProjesi.EnstituKod == enstituKod &&
                                 (enstituOnayDurumId == 2 ? donemProjesiBasvuru.EYKYaHazirlandi == true &&
                                                            !donemProjesiBasvuru.EYKDaOnaylandi.HasValue &&
                                                            donemProjesiBasvuru.EYKYaHazirlandiIslemTarihi >= baslangicTarihi &&
                                                            donemProjesiBasvuru.EYKYaHazirlandiIslemTarihi <= bitisTarihi
                                                           :
                                                             donemProjesiBasvuru.EYKDaOnaylandi == true &&
                                                             !donemProjesiBasvuru.EYKYaHazirlandi == true &&
                                                             donemProjesiBasvuru.EYKTarihi >= baslangicTarihi &&
                                                             donemProjesiBasvuru.EYKTarihi <= bitisTarihi)
                               select new DpTutanakDto
                               {

                                   OgrenciNo = ogrenci.OgrenciNo,
                                   OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                                   AnabilimDaliAdi = anabilimDali.AnabilimDaliAdi,
                                   ProgramAdi = program.ProgramAdi,
                                   YurutucuAdSoyad = tezDanisman.Unvanlar.UnvanAdi + " " + tezDanisman.Ad + " " + tezDanisman.Soyad,
                                   MezuniyetDonemAdi = donemProjesiBasvuru.BasvuruYil + "/" + (donemProjesiBasvuru.BasvuruYil + 1) + " " + donemProjesiBasvuru.Donemler.DonemAdi,
                                   MezuniyetEykTarihi = donemProjesiBasvuru.EYKTarihi,

                               }).OrderBy(o => o.MezuniyetEykTarihi).ToList();








            var report = new XtraReport();


            var rpr = new RprDpTutanak(enstitu.EnstituAd);
            rpr.DataSource = tutanakData;
            rpr.CreateDocument();
            report.Pages.AddRange(rpr.Pages);

            report.ExportOptions.Html.ExportMode = HtmlExportMode.SingleFilePageByPage;
            using (MemoryStream ms = new MemoryStream())
            {
                report.ExportToHtml(ms);
                ms.Position = 0;
                var sr = new StreamReader(ms);
                var html = sr.ReadToEnd();
                var raporAdi = $"Dönem Projesi Mezuniyet Tutanağı - {(enstituOnayDurumId == 2 ? "EYKya Hazırlananlar" : "EYKda Onaylananlar")}";
                return File(System.Text.Encoding.UTF8.GetBytes(html), (exportWordOrExcel ? "application/vnd.ms-word" : "application/ms-excel"), raporAdi + " (" + basTar.Replace("-", ".") + "-" + bitTar.Replace("-", ".") + ")." + (exportWordOrExcel ? "doc" : "xls"));

            }
        }
    }
}