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

            var isDanisman = !RoleNames.DonemProjesiEnstituBasvuruOnayYetkisi.InRoleCurrent() && !RoleNames.DonemProjesiEykYaGonder.InRoleCurrent() && !RoleNames.DonemProjesiSinavDegerlendirmeDuzeltme.InRoleCurrent();

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
                        AnabilimDaliID = programlar.AnabilimDaliID,
                        ProgramAdi = programlar.ProgramAdi,
                        AnabilimDaliAdi = programlar.AnabilimDallari.AnabilimDaliAdi,
                        DonemAdi = sonBasvuru == null ? "Başvuru Yapılmadı" : (sonBasvuru.BasvuruYil + " / " + (sonBasvuru.BasvuruYil + 1) + " " + (sonBasvuru.BasvuruDonemID == 1 ? "Güz" : "Bahar")),
                        ToplantiTarihi = sonBasvuru != null ? sonBasvuru.SRTalepleris.Select(sr => sr.Tarih).FirstOrDefault() : (DateTime?)null,
                        ToplantiSaati = sonBasvuru != null ? sonBasvuru.SRTalepleris.Select(sr => sr.BasSaat).FirstOrDefault() : (TimeSpan?)null,
                        DonemProjesiDurumID = sonBasvuru != null ? sonBasvuru.DonemProjesiDurumID : DonemProjesiDurumEnum.BasvuruTamamlanmadi,
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
                        TezDanismanID = sonBasvuru.TezDanismanID,
                        OnayYapmayanJuriEmails = sonBasvuru.DonemProjesiJurileris.Where(p => isDegerlendirmeSurecinde && p.IsLinkGonderildi == true && !p.DonemProjesiJuriOnayDurumID.HasValue).Select(ss => ss.EMail).ToList(),
                        FilterJuriAdiKeys = sonBasvuru.DonemProjesiJurileris.Select(s => s.AdSoyad).ToList()
                    };
            q = q.Where(p => p.EnstituKod == enstituKod && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod));
            var q2 = q;
            if (!model.AkademikDonemID.IsNullOrWhiteSpace()) q = q.Where(p => p.AkademikDonemID == model.AkademikDonemID);

            if (model.AnabilimDaliID.HasValue) q = q.Where(p => p.AnabilimDaliID == model.AnabilimDaliID);

            if (model.DonemProjesiDurumID.HasValue)
            {
                if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.BasvuruTamamlanmadi) q = q.Where(p => p.SonBasvuruDurum == null);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EnstituOnayiBekliyor) q = q.Where(p => p.SonBasvuruDurum != null && !p.SonBasvuruDurum.DonemProjesiEnstituOnayDurumID.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EnstituTarafindanOnaylandi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiEnstituOnayDurumID.HasValue && p.SonBasvuruDurum.DonemProjesiEnstituOnayDurumID == DonemProjesiEnstituOnayDurumEnum.KabulEdildi && !p.SonBasvuruDurum.IsDanismanOnay.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EnstituTarafindanOnaylanmadi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiEnstituOnayDurumID.HasValue && p.SonBasvuruDurum.DonemProjesiEnstituOnayDurumID != DonemProjesiEnstituOnayDurumEnum.KabulEdildi && !p.SonBasvuruDurum.IsDanismanOnay.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.DanismanOnayiBekliyor) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.DanismanOnaySureci && !p.SonBasvuruDurum.IsDanismanOnay.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.DanismanTarafindanOnaylandi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.DanismanOnaySureci && p.SonBasvuruDurum.IsDanismanOnay == true);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.DanismanTarafindanOnaylanmadi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.DanismanOnaySureci && p.SonBasvuruDurum.IsDanismanOnay == false);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.JuriSinavOlusturmaSureci) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.JuriSinavOlusturmaSureci);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.SinavDegerlendirmeSureci) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.SinavDegerlendirmeSureci);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EykYaGonderimOnayiBekleniyor) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && !p.SonBasvuruDurum.EYKYaGonderildi.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EykYaGonderimiOnaylandi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && p.SonBasvuruDurum.EYKYaGonderildi == true && !p.SonBasvuruDurum.EYKYaHazirlandi.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EykYaGonderimiOnaylanmadi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && p.SonBasvuruDurum.EYKYaGonderildi == false && !p.SonBasvuruDurum.EYKYaHazirlandi.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EykYaHazirlandi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && p.SonBasvuruDurum.EYKYaHazirlandi == true && !p.SonBasvuruDurum.EYKDaOnaylandi.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EykDaOnayBekleniyor) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && !p.SonBasvuruDurum.EYKDaOnaylandi.HasValue && !p.SonBasvuruDurum.EYKYaHazirlandi.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EykDaOnaylandi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && p.SonBasvuruDurum.EYKDaOnaylandi == true && !p.SonBasvuruDurum.EYKYaHazirlandi.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.EykDaOnaylanmadi) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && p.SonBasvuruDurum.EYKDaOnaylandi == false && !p.SonBasvuruDurum.EYKYaHazirlandi.HasValue);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.BasariliOlanlar) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && p.SonBasvuruDurum.EYKDaOnaylandi == true && p.SonBasvuruDurum.DonemProjesiJuriOnayDurumID == DonemProjesiJuriOnayDurumEnum.Basarili);
                else if (model.DonemProjesiDurumID == DpBasvuruDurumEnum.BasarisizOlanlar) q = q.Where(p => p.SonBasvuruDurum != null && p.SonBasvuruDurum.DonemProjesiDurumID == DonemProjesiDurumEnum.EnstituYonetimKuruluSureci && p.SonBasvuruDurum.EYKDaOnaylandi == true && p.SonBasvuruDurum.DonemProjesiJuriOnayDurumID != DonemProjesiJuriOnayDurumEnum.Basarili);
            }

            if (!model.AdSoyad.IsNullOrWhiteSpace())
                q = q.Where(p =>
                    p.AdSoyad.Contains(model.AdSoyad)
                    || p.OgrenciNo.Contains(model.AdSoyad)
                    || p.TcKimlikNo.Contains(model.AdSoyad)
                    || p.FilterJuriAdiKeys.Any(a => a.Contains(model.AdSoyad))
                    );

            model.IsFiltered = !Equals(q, q2);
            if (isDanisman)
            {
                q = q.Where(p => p.TezDanismanID == UserIdentity.Current.Id);
                q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.DonemProjesiDurumID).ThenByDescending(o => o.BasvuruTarihi);
            }
            else
            {
                q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
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
                    s.TezDanismanID,
                    s.DonemAdi,
                    s.DonemProjesiDurumAdi,
                    EnstituBasvuruOnayDurumu = s.SonBasvuruDurum.DonemProjesiEnstituOnayDurumID.HasValue ? s.SonBasvuruDurum.EnstituOnayDurumAdi : "İşlem Bekliyor",
                    DanismanOnayDurumu = s.SonBasvuruDurum.IsDanismanOnay.HasValue ? s.SonBasvuruDurum.IsDanismanOnay.Value ? "Danışman Onayladı" : "Danışman Onaylamadı" : "İşlem Bekliyor",
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
                    .Select(s => new { s.KullaniciID, Danisman = s.Unvanlar.UnvanAdi + " " + s.Ad + " " + s.Soyad }).ToList();

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
                                      d.Danisman,
                                      s.EnstituBasvuruOnayDurumu,
                                      s.DanismanOnayDurumu,
                                      s.ToplantiTarihi,
                                      s.ToplantiSaati,
                                      s.SinavSonucu,
                                      s.EYKYaGonderildi,
                                      s.EYKYaHazirlandi,
                                      s.EYKDaOnaylandi
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

            model.RowCount = q.Count();
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.filteredOgrenciIds = model.IsFiltered ? q.Select(s => s.KullaniciID).ToList() : new List<int>();
            ViewBag.filteredDanismanIds = model.IsFiltered ? q.Where(p => p.TezDanismanID > 0).Select(s => s.TezDanismanID.Value).Distinct().ToList() : new List<int>();
            ViewBag.onayYapmayanJuriEmails = model.IsFiltered && isDegerlendirmeSurecinde ? q.SelectMany(s => s.OnayYapmayanJuriEmails).Distinct().ToList() : new List<string>();

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
            var baslangicTarihi = basTar.ToDate(DateTime.Now);
            var bitisTarihi = bitTar.ToDate(DateTime.Now);

            var qData = (from donemProjesiBasvuru in _entities.DonemProjesiBasvurus
                         join donemProjesi in _entities.DonemProjesis on donemProjesiBasvuru.DonemProjesiID equals donemProjesi.DonemProjesiID
                         join tezDanisman in _entities.Kullanicilars on donemProjesiBasvuru.TezDanismanID equals tezDanisman.KullaniciID
                         join program in _entities.Programlars on donemProjesi.ProgramKod equals program.ProgramKod
                         join anabilimDali in _entities.AnabilimDallaris on program.AnabilimDaliID equals anabilimDali
                             .AnabilimDaliID
                         join ogrenci in _entities.Kullanicilars on donemProjesi.KullaniciID equals ogrenci.KullaniciID
                         where donemProjesi.EnstituKod == enstituKod &&
                           enstituOnayDurumId == 1 ? (
                                                    donemProjesiBasvuru.EYKYaGonderildi == true &&
                                                    donemProjesiBasvuru.EYKYaGonderildiIslemTarihi >= baslangicTarihi &&
                                                    donemProjesiBasvuru.EYKYaGonderildiIslemTarihi <= bitisTarihi)
                                                : (enstituOnayDurumId == 2 ? (
                                                                            donemProjesiBasvuru.EYKYaHazirlandi == true &&
                                                                            donemProjesiBasvuru.EYKYaHazirlandiIslemTarihi >= baslangicTarihi &&
                                                                            donemProjesiBasvuru.EYKYaHazirlandiIslemTarihi <= bitisTarihi)
                                                                        :
                                                                           (donemProjesiBasvuru.EYKDaOnaylandi == true &&
                                                                               donemProjesiBasvuru.EYKTarihi >= baslangicTarihi &&
                                                                               donemProjesiBasvuru.EYKTarihi <= bitisTarihi)
                                                                            )
                         select new
                         {
                             donemProjesiBasvuru.EYKTarihi,
                             ogrenci.OgrenciNo,
                             TezDanismanAdSoyad = tezDanisman.Ad + " " + tezDanisman.Soyad,
                             TezDanismanUnvanAdi = tezDanisman.Unvanlar.UnvanAdi,
                             OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                             anabilimDali.AnabilimDaliAdi,
                             program.ProgramAdi,
                             donemProjesiBasvuru.ProjeBasligi,
                             donemProjesiBasvuru.ProjeOzeti,
                             Juriler = donemProjesiBasvuru.DonemProjesiJurileris.ToList()

                         }).OrderBy(o => o.EYKTarihi).ToList();


            var rprDpJuriTutanakModels = new List<DpJuriTutanakModel>();
            var rprDpJuriTutanakModel = new DpJuriTutanakModel
            {
                TutanakAdi = "Tezsiz Yüksek Lisans - Dönem Projesi Sınavı Hk.",
                Aciklama = "Doktora yeterlik sınavında başarılı olan öğrenciler için tez izleme komitelerinin aşağıda adı yazılı öğretim üyelerinden oluşturulmasına," +
                " Tez izleme komitesince 'YTÜ Lisansüstü Eğitim-Öğretim Yönetmeliği Senato Esasları' nın ilgili maddesine göre(yeterlik sınav tarihinden başlayarak)" +
                " 6(altı) ay içerisinde tez önerisi savunması yapılarak sonucun bir tutanakla ilgili anabilim dalı başkanlığı aracılığıyla Enstitümüze bildirilmesine oybirliğiyle karar verildi."
            };

            foreach (var itemO in qData)
            {
                var uyeler = itemO.Juriler.ToList();
                var danisman = uyeler.First(p => p.IsTezDanismani);
                var uye1 = uyeler.Where(p => !p.IsTezDanismani).ToList()[0];
                var uye2 = uyeler.Where(p => !p.IsTezDanismani).ToList()[1];

                rprDpJuriTutanakModel.DetayData.Add(new RprDpjuriTutanakRowModel()
                {
                    OgrenciBilgi = itemO.OgrenciNo + " " + itemO.OgrenciAdSoyad + " (" + itemO.AnabilimDaliAdi + " / " + itemO.ProgramAdi + ")",
                    DanismanAdSoyad = danisman.UnvanAdi + " " + danisman.AdSoyad,
                    DanismanUni = "Yıldız Teknik Üni",
                    Uye1 = uye1.UnvanAdi + " " + uye1.AdSoyad,
                    Uye1Uni = "Yıldız Teknik Üni",
                    Uye2 = uye2.UnvanAdi + " " + uye2.AdSoyad,
                    Uye2Uni = "Yıldız Teknik Üni",
                    ProjeBasligi = itemO.ProjeBasligi
                });

                rprDpJuriTutanakModels.Add(rprDpJuriTutanakModel);
            }



            var report = new XtraReport();

            if (rprDpJuriTutanakModels.Count > 0)
            {
                var rpr = new RprDpJuriTutanak();
                rpr.DataSource = rprDpJuriTutanakModels[0];
                rpr.CreateDocument();

                report.Pages.AddRange(rpr.Pages);
            }


            report.ExportOptions.Html.ExportMode = HtmlExportMode.SingleFilePageByPage;
            using (MemoryStream ms = new MemoryStream())
            {
                report.ExportToHtml(ms);
                ms.Position = 0;
                var sr = new StreamReader(ms);
                var html = sr.ReadToEnd();
                var raporAdi = "Dönem Projesi Sınavı Tutanağı";
                return File(System.Text.Encoding.UTF8.GetBytes(html), (exportWordOrExcel ? "application/vnd.ms-word" : "application/ms-excel"), raporAdi + " (" + basTar.Replace("-", ".") + "-" + bitTar.Replace("-", ".") + ")." + (exportWordOrExcel ? "doc" : "xls"));

            }
        }
    }
}