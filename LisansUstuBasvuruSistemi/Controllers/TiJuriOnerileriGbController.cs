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
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Raporlar.TezIzlemeJuriOneri;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using LisansUstuBasvuruSistemi.WebServiceData.ObsService;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.TiJuriOnerileriGb)]
    public class TiJuriOnerileriGbController : Controller
    {
        // GET: TikOneriGb
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        public ActionResult Index(Guid? selectedBasvuruUniqueId, string ekd)
        {
            return Index(new FmTijBasvuru() { SelectedBasvuruUniqueId = selectedBasvuruUniqueId, PageSize = 50 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmTijBasvuru model, string ekd, bool export = false)
        {
            model.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            model.KullaniciID = model.KullaniciID ?? UserIdentity.Current.Id;
            //TezIzlemeJuriOneriBus.TezIzlemeJuriOneriSenkronizasyon(model.KullaniciID.Value);
            var q = from s in _entities.TijBasvurus.Where(p => p.EnstituKod == model.EnstituKod && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod))
                    join ogrenci in _entities.Kullanicilars on s.KullaniciID equals ogrenci.KullaniciID
                    select new
                    {
                        s.TijBasvuruID,
                        s.UniqueID,
                        s.EnstituKod,
                        s.BasvuruTarihi,
                        s.Programlar.AnabilimDaliID,
                        s.KullaniciID,
                        ogrenci.UserKey,
                        AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                        s.OgrenciNo,
                        ogrenci.EMail,
                        ogrenci.CepTel,
                        ogrenci.ResimAdi,
                        s.Programlar.ProgramAdi,
                        s.Programlar.AnabilimDallari.AnabilimDaliAdi,
                        s.TezDanismanID,
                        TezDanismanIds = s.TijBasvuruOneris.Select(sd => sd.TezDanismanID).ToList(),
                        JuriAdis = s.TijBasvuruOneris.SelectMany(s2 => s2.TijBasvuruOneriJurilers.Select(sm => sm.AdSoyad)).ToList(),
                        SonBasvuru = s.TijBasvuruOneris.Select(s2 => new TijBasvuruOneriDetayDto
                        {
                            TijBasvuruOneriID = s2.TijBasvuruOneriID,
                            TijFormTipID = s2.TijFormTipID,
                            TijFormTipAdi = s2.TijFormTipleri.TikFormTipAdi,
                            TijDegisiklikTipID = s2.TijDegisiklikTipID,
                            TijDegisiklikTipAdi = s2.TijDegisiklikTipleri.TijDegisiklikTipAdi,
                            DegisiklikAciklamasi = s2.DegisiklikAciklamasi,
                            //TijBasvuruOneriJurilers=s2.TijBasvuruOneriJurilers.ToList(),
                            IsObsData = s2.IsObsData,
                            BasvuruTarihi = s2.BasvuruTarihi,
                            DonemBaslangicYil = s2.DonemBaslangicYil,
                            DonemID = s2.DonemID,
                            DonemAdi = s2.DonemBaslangicYil + "/" + (s2.DonemBaslangicYil + 1) + " " + s2.Donemler.DonemAdi,
                            TezDanismanID = s2.TezDanismanID,
                            IsDilTaahhutuOnaylandi = s2.IsDilTaahhutuOnaylandi,
                            TezBaslikTr = s2.TezBaslikTr,
                            TezBaslikEn = s2.TezBaslikEn,
                            IsTezDiliTr = s2.IsTezDiliTr,
                            DanismanOnayladi = s2.DanismanOnayladi,
                            EYKYaGonderildi = s2.EYKYaGonderildi,
                            EYKYaHazirlandi = s2.EYKYaHazirlandi,
                            EYKTarihi = s2.EYKTarihi,
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
                q = q.Where(p => p.TezDanismanIds.Contains(danismanId));
            }
            bool isFiltered = false;
            if (!model.AdSoyad.IsNullOrWhiteSpace())
            {
                isFiltered = true;
                q = q.Where(p =>
                    p.AdSoyad.Contains(model.AdSoyad)
                    || p.OgrenciNo.Contains(model.AdSoyad)
                    || p.ProgramAdi.Contains(model.AdSoyad)
                    || p.JuriAdis.Contains(model.AdSoyad)
                    );
            }
            var tijFormTips = TijBus.CmbTijOneriTipListe(true);
            if (model.TijFormTipID.HasValue)
            {
                isFiltered = true;
                var formTipIds = model.TijFormTipID == TijFormTipiEnum.TumDegisiklikler
                    ? tijFormTips.Where(p => p.Value != TijFormTipiEnum.YeniForm && p.Value.HasValue).Select(s => s.Value.Value).ToList()
                    : new List<int> { model.TijFormTipID.Value };
                q = q.Where(p => p.SonBasvuru != null && formTipIds.Contains(p.SonBasvuru.TijFormTipID));
            }

            if (model.AnabilimDaliID.HasValue)
            {
                isFiltered = true;
                q = q.Where(p => p.AnabilimDaliID == model.AnabilimDaliID);
            }

            if (!model.AktifTijDonemId.IsNullOrWhiteSpace())
            {
                isFiltered = true;
                q = q.Where(p => p.SonBasvuru != null && (p.SonBasvuru.DonemBaslangicYil + "" + p.SonBasvuru.DonemID) == model.AktifTijDonemId);
            }
            if (model.AktifDurumID.HasValue)
            {
                isFiltered = true;
                if (model.AktifDurumID == TijBasvuruDurumEnum.DanismanOnayiBekliyor) q = q.Where(p => p.SonBasvuru != null && !p.SonBasvuru.IsObsData && !p.SonBasvuru.DanismanOnayladi.HasValue);
                else if (model.AktifDurumID == TijBasvuruDurumEnum.DanismanTarafindanOnaylandi) q = q.Where(p => p.SonBasvuru != null && !p.SonBasvuru.IsObsData && p.SonBasvuru.DanismanOnayladi == true && !p.SonBasvuru.EYKYaGonderildi.HasValue);
                else if (model.AktifDurumID == TijBasvuruDurumEnum.DanismanTarafindanOnaylanmadi) q = q.Where(p => p.SonBasvuru != null && !p.SonBasvuru.IsObsData && p.SonBasvuru.DanismanOnayladi == false && !p.SonBasvuru.EYKYaGonderildi.HasValue);
                else if (model.AktifDurumID == TijBasvuruDurumEnum.EykYaGonderimOnayiBekleniyor) q = q.Where(p => p.SonBasvuru != null && !p.SonBasvuru.IsObsData && p.SonBasvuru.DanismanOnayladi == true && !p.SonBasvuru.EYKYaGonderildi.HasValue);
                else if (model.AktifDurumID == TijBasvuruDurumEnum.EykYaGonderimiOnaylandi) q = q.Where(p => p.SonBasvuru != null && !p.SonBasvuru.IsObsData && p.SonBasvuru.EYKYaGonderildi == true && !p.SonBasvuru.EYKYaHazirlandi.HasValue);
                else if (model.AktifDurumID == TijBasvuruDurumEnum.EykYaGonderimiOnaylanmadi) q = q.Where(p => p.SonBasvuru != null && !p.SonBasvuru.IsObsData && p.SonBasvuru.EYKYaGonderildi == false && !p.SonBasvuru.EYKYaHazirlandi.HasValue);
                else if (model.AktifDurumID == TijBasvuruDurumEnum.EykYaHazirlandi) q = q.Where(p => p.SonBasvuru != null && !p.SonBasvuru.IsObsData && p.SonBasvuru.EYKYaHazirlandi == true && !p.SonBasvuru.EYKDaOnaylandi.HasValue);
                else if (model.AktifDurumID == TijBasvuruDurumEnum.EykDaOnayBekleniyor) q = q.Where(p => p.SonBasvuru != null && !p.SonBasvuru.IsObsData && p.SonBasvuru.EYKYaHazirlandi == true && !p.SonBasvuru.EYKDaOnaylandi.HasValue);
                else if (model.AktifDurumID == TijBasvuruDurumEnum.EykDaOnaylandi) q = q.Where(p => p.SonBasvuru != null && !p.SonBasvuru.IsObsData && p.SonBasvuru.EYKDaOnaylandi == true);
                else if (model.AktifDurumID == TijBasvuruDurumEnum.EykDaOnaylanmadi) q = q.Where(p => p.SonBasvuru != null && !p.SonBasvuru.IsObsData && p.SonBasvuru.EYKDaOnaylandi == false);
            }
            #region export
            if (export && model.RowCount > 0)
            {
                var gv = new GridView();
                var data = q.Where(p => p.SonBasvuru != null && !p.SonBasvuru.IsObsData).Select(s => new
                {
                    s.OgrenciNo,
                    s.AdSoyad,
                    s.AnabilimDaliAdi,
                    s.ProgramAdi,
                    s.SonBasvuru.BasvuruTarihi,
                    s.SonBasvuru.DonemAdi,
                    s.SonBasvuru.TijFormTipAdi,
                    s.SonBasvuru.TijDegisiklikTipAdi,
                    s.SonBasvuru.DegisiklikAciklamasi,
                    s.SonBasvuru.TezDanismanID,
                    TezDili = s.SonBasvuru.IsTezDiliTr ? "Türkçe" : "İngilizce",
                    s.SonBasvuru.TezBaslikTr,
                    s.SonBasvuru.TezBaslikEn,
                    DanismanOnayladi = s.SonBasvuru.DanismanOnayladi.HasValue ? (s.SonBasvuru.DanismanOnayladi == true ? "Onayladı" : "Onaylamadı") : "İşlem Bekleniyor",
                    EYKYaGonderildi = s.SonBasvuru.EYKYaGonderildi.HasValue ? (s.SonBasvuru.EYKYaGonderildi == true ? "Eyk'ya Gönderildi" : "Eyk'ya Gönderilmedi") : "İşlem Bekleniyor",
                    EykYaHazirlandi = s.SonBasvuru.EYKYaHazirlandi.HasValue ? (s.SonBasvuru.EYKYaHazirlandi == true ? "Eyk'ya Hazırlandı" : "-") : "İşlem Bekleniyor",
                    s.SonBasvuru.EYKTarihi,
                    EYKDaOnaylandi = s.SonBasvuru.EYKDaOnaylandi.HasValue ? (s.SonBasvuru.EYKDaOnaylandi == true ? "Onaylandı" : "Onaylanmadı") : "İşlem Bekleniyor",
                }).ToList();
                var danismanIds = data.Select(s => s.TezDanismanID).ToList();
                var danismans = _entities.Kullanicilars.Where(p => danismanIds.Contains(p.KullaniciID))
                    .Select(s => new { s.KullaniciID, Danisman = s.Unvanlar.UnvanAdi + " " + s.Ad + " " + s.Soyad }).ToList();

                var exportData = (from s in data
                                  join d in danismans on s.TezDanismanID equals d.KullaniciID
                                  select new
                                  {
                                      s.DonemAdi,
                                      s.BasvuruTarihi,
                                      FormTipAdi = s.TijFormTipAdi,
                                      DegisiklikTipAdi = s.TijDegisiklikTipAdi,
                                      s.DegisiklikAciklamasi,
                                      s.OgrenciNo,
                                      OgrenciAdSoyad = s.AdSoyad,
                                      s.AnabilimDaliAdi,
                                      s.ProgramAdi,
                                      s.TezDili,
                                      s.TezBaslikTr,
                                      s.TezBaslikEn,
                                      d.Danisman,
                                      s.DanismanOnayladi,
                                      s.EYKYaGonderildi,
                                      s.EYKTarihi,
                                      s.EYKDaOnaylandi,
                                  }).ToList();

                gv.DataSource = exportData;
                gv.DataBind();
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                var sw = new StringWriter();
                var htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_TezIzlemeJuriOneriListesi_" + DateTime.Now.ToFormatDate() + ".xls");
            }
            #endregion

            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi();
            if (model.SelectedBasvuruUniqueId.HasValue)
            {
                q = q.OrderBy(o => o.UniqueID == model.SelectedBasvuruUniqueId ? 1 : 2).ThenByDescending(o => o.BasvuruTarihi);
            }
            else q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.SonBasvuru != null ? o.SonBasvuru.BasvuruTarihi : o.BasvuruTarihi);
            model.Data = q.Select(s => new FrTijBasvuru
            {
                TijBasvuruID = s.TijBasvuruID,
                UniqueID = s.UniqueID,
                EnstituKod = s.EnstituKod,
                BasvuruTarihi = s.BasvuruTarihi,
                KullaniciID = s.KullaniciID,
                UserKey = s.UserKey,
                AdSoyad = s.AdSoyad,
                OgrenciNo = s.OgrenciNo,
                ResimAdi = s.ResimAdi,
                KayitVar = s.KayitVar,
                SonBasvuru = s.SonBasvuru

            }).Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.filteredOgrenciIds = isFiltered ? q.Select(s => s.KullaniciID).ToList() : new List<int>();
            ViewBag.filteredDanismanIds = isFiltered ? q.Where(p => p.SonBasvuru != null && p.SonBasvuru.TezDanismanID.HasValue).Select(s => s.SonBasvuru.TezDanismanID.Value).Distinct().ToList() : new List<int>();

            ViewBag.IndexModel = indexModel;
            ViewBag.AktifTijDonemId = new SelectList(TijBus.CmbTiDonemListe(model.EnstituKod, true), "Value", "Caption", model.AktifTijDonemId);
            ViewBag.AnabilimDaliID = new SelectList(TijBus.GetCmbFilterTiAnabilimDallari(model.EnstituKod, true), "Value", "Caption", model.AnabilimDaliID);
            ViewBag.AktifDurumID = new SelectList(TijBus.CmbTdoOneriDurumListe(true), "Value", "Caption", model.AktifDurumID);
            tijFormTips.Add(new CmbIntDto { Caption = "Tüm Değişenler", Value = 1000 });
            ViewBag.TijFormTipID = new SelectList(tijFormTips, "Value", "Caption", model.TijFormTipID);

            return View(model);

        }

        public ActionResult TijOneriOgrenciSecim()
        {
            return View();
        }

        public ActionResult GetTijOngerciSeciOgrenci(string term, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            int? danismanId = null;
            var tiJuriOnerileriOgrenciAdina = RoleNames.TiJuriOnerileriOgrenciAdina.InRoleCurrent();
            if (tiJuriOnerileriOgrenciAdina && !UserIdentity.Current.IsYetkiliTij)
                danismanId = UserIdentity.Current.Id;

            var jsonResult = TijBus.GetFilterOgrenciJsonResult(term, enstituKod, danismanId);
            return jsonResult;
        }

        public ActionResult OgrenciBasvuruKontrol(int kullaniciId, string ekd)
        {
            Guid? basvuruUniqueId = null;
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var enstituAdi = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod).EnstituAd;
            var isBasvuruAcik = TiAyar.TikOneriAlimiAcik.GetAyarTi(enstituKod, "false").ToBoolean(false);

            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                IsSuccess = true
            };
            var kul = _entities.Kullanicilars.First(f => f.KullaniciID == kullaniciId);


            int? danismanId = null;
            var tiJuriOnerileriOgrenciAdina = RoleNames.TiJuriOnerileriOgrenciAdina.InRoleCurrent();
            if (tiJuriOnerileriOgrenciAdina && !UserIdentity.Current.IsYetkiliTij)
                danismanId = UserIdentity.Current.Id;
            if (!isBasvuruAcik)
            {
                mMessage.Messages.Add("Jüri öneri formu başvuru süreci kapalıdır. Detaylı bilgi almak için " + enstituAdi + " ile göreşebilirsiniz.");
            }
            else if (danismanId.HasValue)
            {

                KullanicilarBus.OgrenciBilgisiGuncelleObs(kul.KullaniciID);

                var ogrencilers = TijBus.GetDanismanOgrencileriKullaniciId(danismanId.Value);
                if (ogrencilers.All(a => a != kullaniciId))
                {
                    mMessage.Messages.Add("Sadece danışmanı olduğunuz öğrenciler için jüri öneri formu oluşturabilirsiniz.");
                }

            }
            if (!mMessage.Messages.Any())
            {
                var obsOgrenci = KullanicilarBus.OgrenciKontrol(kul.OgrenciNo);
                if (!obsOgrenci.Hata)
                {
                    var ogrenciYeterlikBilgi =
                        obsOgrenci.OgrenciYeters.FirstOrDefault(p => p.DR_YET_GNL_SNV_DURUM == "Başarılı" && p.DR_YET_SOZ_SNV_DURUM == "Başarılı");
                    if (ogrenciYeterlikBilgi == null)
                    {
                        mMessage.Messages.Add("Başvuru yapacağınız öğrencinin yeterlik sınavından başarılı olması gerekmektedir.");
                    }
                    else if (ogrenciYeterlikBilgi.DR_YET_SOZ_SNV_TARIH.IsNullOrWhiteSpace())
                    {
                        mMessage.Messages.Add("Başvuru yapacağınız öğrencinin yeterlik sözlü sınav tarihi bilgisi OBS sisteminden boş gelmektedir. Bu durumu enstitü yetkililerine iletiniz.");
                    }
                    else
                    {
                        var devamEdenBasvuru = _entities.TijBasvurus.FirstOrDefault(f =>
                            f.KullaniciID == kullaniciId && f.OgrenciNo == kul.OgrenciNo &&
                            !f.IsYeniBasvuruYapilabilir);
                        if (devamEdenBasvuru == null)
                        {
                            if (TdoBus.IsAktifDanismanOneriVar(kul.KullaniciID))
                            {
                                mMessage.Messages.Add("Öğrencinin yapmış olduğu bir Tez Danışman Öneri başvurusu bulunmakta. Jüri önerisi yapılabilmesi bu sürecinin tamamlanması gerekmektedir.");
                            }
                            //else if (TdoBus.IsAktifEsDanismanOneriVar(kul.KullaniciID))
                            //{
                            //    mMessage.Messages.Add("Öğrencinin yapmış olduğu bir Tez Eş Danışman Öneri başvurusu bulunmakta. Jüri önerisi yapılabilmesi bu sürecinin tamamlanması gerekmektedir.");
                            //}
                            else if (enstituKod != kul.Programlar.AnabilimDallari.EnstituKod)
                            {
                                var ogrenciEnstitu = _entities.Enstitulers.First(f =>
                                    f.EnstituKod == kul.Programlar.AnabilimDallari.EnstituKod);
                                mMessage.Messages.Add("Sistemde seçili olan ensitü " + enstituAdi + " Öğrenci okuduğu enstitü " + ogrenciEnstitu.EnstituAd + ". sistem üzerinden " + ogrenciEnstitu.EnstituAd + " enstitüsüne geçiş yapıp tekrar öneri yapmayı deneyizi.");

                            }
                            else if (obsOgrenci.TezIzlJuriBilgileri.Count > 0 && obsOgrenci.TezIzlJuriBilgileri.Count <= 2)
                            {
                                mMessage.Messages.Add("Jüri bilgileri eksik olduğu için başvuru başlatılamadı. Bu durumu Enstitünüze iletiniz.");
                            }
                            else
                            {
                                basvuruUniqueId = TijBus.TezIzlemeJuriOneriSenkronizasyon(kullaniciId);
                            }
                        }
                        else
                        {
                            mMessage.Messages.Add("Seçilen öğrencinin devam eden bir jüri öneri işlemi bulunmaktadır. Jüri öneri işlemi tamamlanmadan yeni bir öneri yapılamaz.");
                        }

                    }
                }
                else
                {
                    mMessage.Messages.Add("Öğrenci bilgisi OBS sistemindek kontrol edilirken aşağıdaki hata bilgisi dönmüştür. Bu durumu enstitü yetkililerine iletiniz.");
                    mMessage.Messages.Add("Hata: " + obsOgrenci.HataMsj);
                }

            }
            mMessage.IsSuccess = mMessage.Messages.Count == 0;
            if (mMessage.Messages.Count > 0)
            {
                mMessage.Title = "Jüri Öneri Formu Oluşturma İşlemi Başlatılamadı";
                mMessage.IsSuccess = false;
                mMessage.MessageType = MsgTypeEnum.Warning;
            }
            return new { mMessage.IsSuccess, mMessage, basvuruUniqueId }.ToJsonResult();
        }
        public ActionResult TijOneriFormu()
        {

            return View();
        }
        public ActionResult GetTijOneriFormu(int? kullaniciId, Guid? basvuruUniqueId, Guid? basvuruJuriOneriUniqueId)
        {
            string view = "";
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                IsSuccess = true
            };

            var tijBasvuru = _entities.TijBasvurus.FirstOrDefault(f => f.UniqueID == basvuruUniqueId);
            TijBasvuruOneri tijBasvuruOneri = null;


            var unvanlar = UnvanlarBus.GetCmbJuriUnvanlar(true);
            var universiteler = UniversitelerBus.CmbGetAktifUniversiteler(true);


            if (!RoleNames.TiJuriOnerileriOgrenciAdina.InRoleCurrent()) kullaniciId = UserIdentity.Current.Id;
            var kul = _entities.Kullanicilars.First(f => f.KullaniciID == kullaniciId);

            if (tijBasvuru == null)
            {
                tijBasvuru = _entities.TijBasvurus.FirstOrDefault(f =>
                    f.KullaniciID == kul.KullaniciID && f.OgrenciNo == kul.OgrenciNo);
            }

            if (tijBasvuru != null)
                tijBasvuruOneri = tijBasvuru.TijBasvuruOneris.FirstOrDefault(f => f.UniqueID == basvuruJuriOneriUniqueId);

            var model = new TijOneriFormuKayitDto
            {
                TijBasvuruOneriID = tijBasvuruOneri?.TijBasvuruOneriID ?? 0,
                IsIlkOneri = tijBasvuru == null || !tijBasvuru.TijBasvuruOneris.Any(a => a.UniqueID != basvuruJuriOneriUniqueId && (a.EYKDaOnaylandi == true || a.IsObsData)),
                KullaniciId = tijBasvuru?.KullaniciID ?? kullaniciId.Value,
                TezDanismanID = tijBasvuru?.TezDanismanID ?? kul.DanismanID,
                TijBasvuruID = tijBasvuru?.TijBasvuruID ?? 0,
                SListTijDegisiklikTip = new SelectList(TijBus.CmbTijDegisiklikTipListe(true), "Value", "Caption", tijBasvuruOneri?.TijDegisiklikTipID),
                SListUnvanAdi = new SelectList(unvanlar, "Value", "Caption"),
                SListUniversiteID = new SelectList(universiteler, "Value", "Caption"),
            };
            model.SListTijFormTip = new SelectList(TijBus.CmbTijFormTipListe(true, model.IsIlkOneri), "Value", "Caption",
                tijBasvuruOneri?.TijFormTipID);



            StudentControl ogrenciInfo;
            if (tijBasvuruOneri != null)
            {
                ogrenciInfo = KullanicilarBus.OgrenciKontrol(tijBasvuruOneri.TijBasvuru.OgrenciNo);
                model.OgrenciAdSoyad = kul.Ad + " " + kul.Soyad + " - " + tijBasvuruOneri.TijBasvuru.OgrenciNo;
                model.TijBasvuruID = tijBasvuruOneri.TijBasvuruID;
                model.TijDegisiklikTipID = tijBasvuruOneri.TijDegisiklikTipID;
                model.TijFormTipID = tijBasvuruOneri.TijFormTipID;
                model.DegisiklikAciklamasi = tijBasvuruOneri.DegisiklikAciklamasi;
                model.IsTezDiliTr = ogrenciInfo.IsTezDiliTr;
                model.TezBaslikTr = ogrenciInfo.OgrenciTez.TEZ_BASLIK;
                model.TezBaslikEn = ogrenciInfo.OgrenciTez.TEZ_BASLIK_ENG;
                model.JoFormJuriList = tijBasvuruOneri.TijBasvuruOneriJurilers.Where(p => p.IsYeniOrOnceki).Select(s => new KrTijOneriFormuJurileri
                {
                    TijBasvuruOneriJuriID = s.TijBasvuruOneriJuriID,
                    TijBasvuruOneriID = s.TijBasvuruOneriID,
                    RowNum = s.RowNum,
                    IsTezDanismani = s.IsTezDanismani,
                    IsYtuIciJuri = s.IsYtuIciJuri,
                    UnvanAdi = s.UnvanAdi,
                    AdSoyad = s.AdSoyad,
                    EMail = s.EMail,
                    UniversiteID = s.UniversiteID,
                    UniversiteAdi = s.UniversiteAdi,
                    AnabilimdaliAdi = s.AnabilimdaliAdi,
                    SListUniversiteID = new SelectList(universiteler, "Value", "Caption", s.UniversiteID),
                    SlistUnvanAdi = new SelectList(unvanlar, "Value", "Caption", s.UnvanAdi),
                }).ToList();


            }
            else
            {
                ogrenciInfo = KullanicilarBus.OgrenciKontrol(kul.OgrenciNo);
                model.OgrenciAdSoyad = kul.Ad + " " + kul.Soyad + " - " + kul.OgrenciNo;
                model.IsTezDiliTr = ogrenciInfo.IsTezDiliTr;
                model.TezBaslikTr = ogrenciInfo.OgrenciTez.TEZ_BASLIK;
                model.TezBaslikEn = ogrenciInfo.OgrenciTez.TEZ_BASLIK_ENG;

                if (model.IsIlkOneri)
                    model.TijFormTipID = TijFormTipiEnum.YeniForm;

            }

            if (!model.JoFormJuriList.Any(a => a.IsTezDanismani))
            {
                if (!ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.IsNullOrWhiteSpace())
                {
                    var tdBilgi = new KrTijOneriFormuJurileri
                    {
                        IsTezDanismani = true,
                        IsYtuIciJuri = true,
                        UniversiteID = GlobalSistemSetting.UniversiteYtuKod,
                        UniversiteAdi = "Yıldız Teknik Üniversitesi",
                        UnvanAdi = ogrenciInfo.OgrenciInfo.DANISMAN_UNVAN1.ToJuriUnvanAdi(),
                        AdSoyad = ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.ToUpper(),
                        EMail = ogrenciInfo.DanismanInfo.E_POSTA1,
                        AnabilimdaliAdi = ogrenciInfo.OgrenciInfo.ANABILIMDALI_AD,

                    };
                    tdBilgi.SlistUnvanAdi = new SelectList(unvanlar, "Value", "Caption", tdBilgi.UnvanAdi);
                    tdBilgi.SListUniversiteID = new SelectList(universiteler, "Value", "Caption", tdBilgi.UniversiteID);
                    model.JoFormJuriList.Add(tdBilgi);
                }
                else
                {
                    model.JoFormJuriList.Add(new KrTijOneriFormuJurileri
                    {
                        IsTezDanismani = true,
                        IsYtuIciJuri = true,
                        SlistUnvanAdi = new SelectList(unvanlar, "Value", "Caption"),
                        SListUniversiteID = new SelectList(universiteler, "Value", "Caption")
                    });
                }
            }


            if (mMessage.Messages.Count == 0)
            {
                if (ogrenciInfo.Hata)
                {
                    mMessage.Messages.Add("Obs sisteminden öğrenci bilgisi sorgulanırken bir hata oluştu! " + ogrenciInfo.HataMsj);
                }
                else
                {

                    if (tijBasvuru != null && tijBasvuru.TijBasvuruOneris.Any(a => a.IsObsData || (a.TijFormTipID == TijFormTipiEnum.YeniForm && a.EYKDaOnaylandi == true)))
                    {
                        mMessage.Messages.AddRange(TijBus.TezIzlemeJuriOneriSenkronizasyonMsg(kul.KullaniciID));
                    }

                    if (!mMessage.Messages.Any())
                    {

                        if (ogrenciInfo.OgrenciTez.TEZ_DILI.IsNullOrWhiteSpace())
                        {
                            mMessage.Messages.Add("Tez dili bilgisi OBS sisteminden boş gelmektedir.");
                        }
                        if (ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.IsNullOrWhiteSpace())
                            mMessage.Messages.Add("Danışman bilgisi OBS sisteminden boş gelmektedir.");

                        if (mMessage.Messages.Count > 0)
                        {
                            mMessage.MessageType = MsgTypeEnum.Warning;
                            mMessage.Messages.Add("Jüri öneri formunu oluşturabilmeniz için bu durumu enstitü yetkililerine iletiniz.");
                        }
                    }
                }

            }
            if (mMessage.Messages.Count == 0)
            {
                view = ViewRenderHelper.RenderPartialView("TiJuriOnerileriGb", "TijOneriFormu", model);
            }
            else { mMessage.IsSuccess = false; mMessage.MessageType = MsgTypeEnum.Warning; }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage);

            return new
            {
                mMessage.IsSuccess,
                Content = view,
                Messages = strView
            }.ToJsonResult();
        }


        public ActionResult TijOneriFormuPost(TijOneriFormuKayitDto kModel)
        {
            var kul = _entities.Kullanicilars.First(k => k.KullaniciID == kModel.KullaniciId);

            var enstituKod = kul.EnstituKod;
            var enstitu = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod);
            var enstituAdi = enstitu.EnstituAd;

            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                IsSuccess = true
            };
            int selectedJuriNum = 0;
            bool isJuriOnerisiVar = true;
            var isBasvuruAcik = TiAyar.TikOneriAlimiAcik.GetAyarTi(enstituKod, "false").ToBoolean(false);



            var tijBasvuru = _entities.TijBasvurus.FirstOrDefault(p => p.TijBasvuruID == kModel.TijBasvuruID);
            var isAnaBasvuruVar = tijBasvuru != null;

            var kayitYetki = RoleNames.TiJuriOnerileriOgrenciAdina.InRole() ||
                             kul.KullaniciID == UserIdentity.Current.Id;
            int? danismanId = null;
            var tiJuriOnerileriOgrenciAdina = RoleNames.TiJuriOnerileriOgrenciAdina.InRoleCurrent();
            if (tiJuriOnerileriOgrenciAdina && !UserIdentity.Current.IsYetkiliTij)
                danismanId = UserIdentity.Current.Id;


            if (!kayitYetki)
            {
                mMessage.Messages.Add("Jüri öneri formu kayıt işlemi için yetkili değilsiniz.");
            }
            else if (!isBasvuruAcik)
            {
                mMessage.Messages.Add("Jüri öneri formu başvuru süreci kapalıdır. Detaylı bilgi almak için " + enstituAdi + " ile iletişime geçiniz.");
            }
            else if (danismanId.HasValue && TijBus.GetDanismanOgrencileriKullaniciId(danismanId.Value).All(a => a != kul.KullaniciID))
            {
                mMessage.Messages.Add("Sadece danışmanı olduğunuz öğrenciler için jüri öneri formu oluşturabilirsiniz.");
            }
            else
            {
                var tijBasvuruOneri = tijBasvuru?.TijBasvuruOneris.FirstOrDefault(f => f.TijBasvuruOneriID == kModel.TijBasvuruOneriID);

                if (kModel.TijBasvuruOneriID <= 0)
                {

                    if (isAnaBasvuruVar && !tijBasvuru.IsYeniBasvuruYapilabilir)
                    {
                        mMessage.Messages.Add("Seçilen öğrencinin devam eden bir jüri öneri işlemi bulunmaktadır. Jüri öneri işlemi tamamlanmadan yeni bir öneri yapılamaz.");
                    }


                }
                else
                {
                    var messages = TijBus.GetTijBasvuruDetayIslemKontrol(tijBasvuruOneri.UniqueID);
                    mMessage.Messages.AddRange(messages.Messages);
                }


                if (kModel.TijFormTipID != TijFormTipiEnum.YeniForm)
                {
                    mMessage.Messages.AddRange(TijBus.TezIzlemeJuriOneriSenkronizasyonMsg(kul.KullaniciID));
                }



                if (tijBasvuruOneri != null)
                {
                    isJuriOnerisiVar = false;

                    if (tijBasvuruOneri.EYKDaOnaylandi == true)
                        mMessage.Messages.Add("Jüri öneri formunuzun EYK'da onaylandığından Form üzerinden herhangi bir değişiklik yapamazsınız!");
                    else if (tijBasvuruOneri.EYKYaGonderildi == true)
                        mMessage.Messages.Add("Jüri öneri formunuzun EYK'ya gönderimi yapıldığından Form üzerinden herhangi bir değişiklik yapamazsınız!");

                }

                if (!mMessage.Messages.Any())
                {
                    if (kModel.TijFormTipID <= 0)
                    {
                        mMessage.Messages.Add("Değişiklik nedenini seçiniz.");
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = kModel.TijFormTipID <= 0 ? MsgTypeEnum.Warning : MsgTypeEnum.Success, PropertyName = "TijFormTipID" });
                    if (kModel.TijFormTipID != TijFormTipiEnum.YeniForm &&
                        kModel.DegisiklikAciklamasi.IsNullOrWhiteSpace())
                    {
                        mMessage.Messages.Add("Değişiklik Açıklaması bilgisini giriniz.");
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = kModel.DegisiklikAciklamasi.IsNullOrWhiteSpace() ? MsgTypeEnum.Warning : MsgTypeEnum.Success, PropertyName = "DegisiklikAciklamasi" });
                    if (kModel.TijFormTipID != TijFormTipiEnum.YeniForm && !kModel.TijDegisiklikTipID.HasValue)
                    {
                        mMessage.Messages.Add("Değiştirilecek jüri grubu seçiniz.");
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = !kModel.TijDegisiklikTipID.HasValue ? MsgTypeEnum.Warning : MsgTypeEnum.Success, PropertyName = "TijDegisiklikTipID" });

                }
                if (mMessage.Messages.Count == 0)
                {

                    var rowNums = kModel.RowNum.Select((s, i) => new { RowNum = s, Inx = (i + 1) }).ToList();
                    var isTezDanismanis = kModel.IsTezDanismani.Select((s, i) => new { IsTezDanismani = s, Inx = (i + 1) }).ToList();
                    var isYtuIciJuris = kModel.IsYtuIciJuri.Select((s, i) => new { IsYtuIciJuri = s, Inx = (i + 1) }).ToList();
                    var adSoyads = kModel.AdSoyad.Select((s, i) => new { AdSoyad = s, Inx = (i + 1) }).ToList();
                    var unvanAdis = kModel.UnvanAdi.Select((s, i) => new { UnvanAdi = s, Inx = (i + 1) }).ToList();
                    var eMails = kModel.EMail.Select((s, i) => new { EMail = s.Trim(), Inx = (i + 1) }).ToList();
                    var universiteIDs = kModel.UniversiteID.Select((s, i) => new { UniversiteID = s, Inx = (i + 1) }).ToList();
                    var anabilimdaliAdis = kModel.AnabilimdaliAdi.Select((s, i) => new { AnabilimdaliAdi = s, Inx = (i + 1) }).ToList();

                    var qData = (from ad in adSoyads
                                 join rwn in rowNums on ad.Inx equals rwn.Inx
                                 join td in isTezDanismanis on ad.Inx equals td.Inx
                                 join yj in isYtuIciJuris on ad.Inx equals yj.Inx
                                 join un in unvanAdis on ad.Inx equals un.Inx
                                 join em in eMails on ad.Inx equals em.Inx
                                 join uni in universiteIDs on ad.Inx equals uni.Inx
                                 join abd in anabilimdaliAdis on ad.Inx equals abd.Inx

                                 select new
                                 {
                                     ad.Inx,
                                     rwn.RowNum,
                                     td.IsTezDanismani,
                                     yj.IsYtuIciJuri,
                                     ad.AdSoyad,
                                     AdSoyadSuccess = !ad.AdSoyad.IsNullOrWhiteSpace(),
                                     un.UnvanAdi,
                                     UnvanAdiSuccess = !un.UnvanAdi.IsNullOrWhiteSpace(),
                                     em.EMail,
                                     EMailSuccess = !em.EMail.IsNullOrWhiteSpace() && !em.EMail.ToIsValidEmail(),
                                     uni.UniversiteID,
                                     UniversiteIDSuccess = uni.UniversiteID.HasValue,
                                     abd.AnabilimdaliAdi,
                                     AnabilimdaliAdiSuccess = !abd.AnabilimdaliAdi.IsNullOrWhiteSpace(),


                                 }).ToList();

                    var qGroup = (from s in qData
                                  group new { s } by new
                                  {
                                      s.Inx,
                                      s.RowNum,
                                      s.IsTezDanismani,
                                      s.IsYtuIciJuri,
                                      s.AdSoyadSuccess,
                                      s.UnvanAdiSuccess,
                                      s.EMailSuccess,
                                      s.UniversiteIDSuccess,
                                      s.AnabilimdaliAdi,
                                      s.AnabilimdaliAdiSuccess,
                                      IsSuccessRow = s.AdSoyadSuccess && s.UnvanAdiSuccess && s.EMailSuccess && s.UniversiteIDSuccess && s.AnabilimdaliAdiSuccess,
                                      IsNullValues = !s.AdSoyadSuccess && !s.UnvanAdiSuccess && !s.EMailSuccess && !s.UniversiteIDSuccess && !s.AnabilimdaliAdiSuccess
                                  }

                into g1
                                  select new
                                  {
                                      g1.Key.Inx,
                                      g1.Key.RowNum,
                                      g1.Key.IsTezDanismani,
                                      g1.Key.IsYtuIciJuri,
                                      g1.Key.IsSuccessRow,
                                      g1.Key.IsNullValues,
                                      DetayData = g1.ToList()
                                  }).AsQueryable();

                    qGroup = qGroup.Where(p => !p.IsTezDanismani);
                    if (kModel.TijDegisiklikTipID == TijDegisiklikTipiEnum.YtuIciDegisiklik)
                    {
                        qGroup = qGroup.Where(p => p.IsYtuIciJuri);
                        qData = qData.Where(p => p.IsYtuIciJuri).ToList();
                    }
                    else if (kModel.TijDegisiklikTipID == TijDegisiklikTipiEnum.YtuDisiDegisiklik)
                    {
                        qGroup = qGroup.Where(p => !p.IsYtuIciJuri);
                        qData = qData.Where(p => !p.IsYtuIciJuri).ToList();
                    }

                    var groupData = qGroup.ToList();

                    var rowInx = 0;
                    foreach (var item in groupData)
                    {
                        rowInx++;
                        if (rowInx > 3) rowInx = 1;
                        var isZorunlu = rowInx % 3 != 0;

                        if (!item.IsSuccessRow && (isZorunlu || !item.IsNullValues))
                        {
                            mMessage.Messages.Add("YTÜ " + (item.IsYtuIciJuri ? "İçi " : "Dışı ") + rowInx + ". Jüri önerisinde hatalı veri girişleri mevcut!");
                            if (selectedJuriNum == 0) selectedJuriNum = item.RowNum;
                        }


                        foreach (var item2 in item.DetayData)
                        {
                            var adSoyadMsgType = item2.s.AdSoyadSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
                            var unvanAdiMsgType = item2.s.UnvanAdiSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
                            var emailMsgType = item2.s.EMailSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
                            var universiteIdMsgType = item2.s.UniversiteIDSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
                            var anabilimdaliMsgType = item2.s.AnabilimdaliAdiSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
                            if (!isZorunlu && item.IsNullValues)
                            {
                                adSoyadMsgType = MsgTypeEnum.Nothing;
                                unvanAdiMsgType = MsgTypeEnum.Nothing;
                                emailMsgType = MsgTypeEnum.Nothing;
                                universiteIdMsgType = MsgTypeEnum.Nothing;
                                anabilimdaliMsgType = MsgTypeEnum.Nothing;
                            }

                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = adSoyadMsgType, PropertyName = "AdSoyad_" + item.RowNum });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = unvanAdiMsgType, PropertyName = "UnvanAdi_" + item.RowNum });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = emailMsgType, PropertyName = "EMail_" + item.RowNum });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = universiteIdMsgType, PropertyName = "UniversiteID_" + item.RowNum });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = anabilimdaliMsgType, PropertyName = "AnabilimdaliAdi_" + item.RowNum });

                        }

                    }
                    if (mMessage.Messages.Count == 0)
                    {
                        tijBasvuruOneri = isJuriOnerisiVar ? new TijBasvuruOneri() : tijBasvuruOneri;
                        var kData = qData.Where(p => p.AdSoyadSuccess).ToList();
                        bool isDegisiklikVar;
                        var varolanJurilers = tijBasvuruOneri.TijBasvuruOneriJurilers.Where(p => p.IsYeniOrOnceki).ToList();
                        isDegisiklikVar = tijBasvuruOneri.TijBasvuruOneriJurilers.Count != kData.Count || tijBasvuruOneri.TijDegisiklikTipID != kModel.TijDegisiklikTipID || tijBasvuruOneri.TijFormTipID != kModel.TijFormTipID;
                        foreach (var item in kData)
                        {
                            if (!isDegisiklikVar)
                            {

                                var rw = varolanJurilers.First(p => p.RowNum == item.RowNum);
                                if (rw.IsTezDanismani != item.IsTezDanismani || rw.IsYtuIciJuri != item.IsYtuIciJuri || rw.AdSoyad != item.AdSoyad || rw.UnvanAdi != item.UnvanAdi ||
                                    rw.EMail != item.EMail || rw.UnvanAdi != item.UnvanAdi ||
                                    rw.UniversiteID != item.UniversiteID ||
                                    rw.AnabilimdaliAdi != item.AnabilimdaliAdi) isDegisiklikVar = true;
                            }
                        }
                        StudentControl obsOgrenci = KullanicilarBus.OgrenciKontrol(tijBasvuru != null ? tijBasvuru.OgrenciNo : kul.OgrenciNo);

                        var universitelers = _entities.Universitelers.ToList();

                        var jurilers = kData.Where(p => p.AdSoyadSuccess && p.IsTezDanismani == (kModel.TijFormTipID == TijFormTipiEnum.YeniForm && p.IsTezDanismani)).Select(s =>
                                                new TijBasvuruOneriJuriler
                                                {
                                                    IsYeniOrOnceki = true,
                                                    IsAsil = s.IsTezDanismani,
                                                    IsYtuIciJuri = s.IsYtuIciJuri,
                                                    IsTezDanismani = s.IsTezDanismani,
                                                    RowNum = s.RowNum,
                                                    UnvanAdi = s.UnvanAdi.ToUpper(),
                                                    AdSoyad = s.AdSoyad.ToUpper(),
                                                    EMail = s.EMail,
                                                    UniversiteID = s.UniversiteID,
                                                    UniversiteAdi = universitelers.First(f => f.UniversiteID == s.UniversiteID).Ad.ToUpper(),
                                                    AnabilimdaliAdi = s.AnabilimdaliAdi.ToUpper()
                                                }).OrderBy(o =>
                                                o.RowNum).ToList();
                        if (kModel.TijFormTipID != TijFormTipiEnum.YeniForm)
                        {

                            foreach (var juri in obsOgrenci.TezIzlJuriBilgileri)
                            {
                                int? universiteId = GlobalSistemSetting.UniversiteYtuKod;
                                var isTezDanismani = false;
                                var isYtuIciJuri = false;
                                if (juri.TEZ_DANISMAN == "1") isTezDanismani = true;

                                if (juri.TEZ_IZLEME_JURI_UNIVER.Contains("Yıldız Teknik"))
                                {
                                    isYtuIciJuri = true;
                                }
                                else
                                {
                                    universiteId = null;
                                }

                                jurilers.Add(new TijBasvuruOneriJuriler
                                {
                                    IsYeniOrOnceki = false,
                                    RowNum = isTezDanismani ? 0 : 1,
                                    IsYtuIciJuri = isYtuIciJuri,
                                    IsTezDanismani = isTezDanismani,
                                    UnvanAdi = juri.TEZ_IZLEME_JURI_UNVAN.ToJuriUnvanAdi(),
                                    AdSoyad = juri.TEZ_IZLEME_JURI_ADSOY.ToUpper(),
                                    EMail = juri.TEZ_IZLEME_JURI_EPOSTA,
                                    IsAsil = true,
                                    UniversiteID = universiteId,
                                    UniversiteAdi = juri.TEZ_IZLEME_JURI_UNIVER.ToUpper(),
                                    AnabilimdaliAdi = juri.TEZ_IZLEME_JURI_ANABLMDAL.ToUpper()
                                });
                            }

                        }

                        var ogrenciYeterlikBilgi =
                            obsOgrenci.OgrenciYeters.FirstOrDefault(p => p.DR_YET_GNL_SNV_DURUM == "Başarılı" && p.DR_YET_SOZ_SNV_DURUM == "Başarılı");

                        if (isDegisiklikVar || tijBasvuruOneri.TijBasvuruOneriID <= 0)
                        {
                            if (tijBasvuru == null)
                            {
                                var ogrenimTip =
                                    _entities.OgrenimTipleris.First(f => f.OgrenimTipKod == kul.OgrenimTipKod);

                                tijBasvuru = new TijBasvuru
                                {
                                    UniqueID = Guid.NewGuid(),
                                    EnstituKod = enstituKod,
                                    BasvuruTarihi = DateTime.Now,
                                    KullaniciID = kModel.KullaniciId,
                                    OgrenciNo = kul.OgrenciNo,
                                    OgrenimTipID = ogrenimTip.OgrenimTipID,
                                    ProgramKod = kul.ProgramKod,
                                    TezDanismanID = kul.DanismanID,
                                    KayitOgretimYiliBaslangic = kul.KayitYilBaslangic,
                                    KayitOgretimYiliDonemID = kul.KayitDonemID,
                                    KayitTarihi = kul.KayitTarihi,
                                    IslemTarihi = DateTime.Now,
                                    IslemYapanIP = UserIdentity.Ip,
                                    IslemYapanID = kModel.KullaniciId
                                };
                            }

                            tijBasvuru.IsYeniBasvuruYapilabilir = false;

                            var uniqueId = Guid.NewGuid();
                            if (tijBasvuruOneri != null && tijBasvuruOneri.TijBasvuruOneriID > 0)
                            {

                                _entities.TijBasvuruOneris.Remove(tijBasvuruOneri);
                            }
                            var donemBilgi = (tijBasvuruOneri.TijBasvuruOneriID > 0 ? tijBasvuru.BasvuruTarihi : DateTime.Now).ToAkademikDonemBilgi();

                            tijBasvuru.TijBasvuruOneris.Add(new TijBasvuruOneri
                            {
                                UniqueID = uniqueId,
                                FormKodu = uniqueId.ToString().Substring(0, 8).ToUpper(),
                                TijFormTipID = kModel.TijFormTipID,
                                TijDegisiklikTipID = kModel.TijDegisiklikTipID,
                                IsObsData = false,
                                TezDanismanID = tijBasvuruOneri?.TezDanismanID ?? kul.DanismanID,
                                DanismanOnayTarihi = danismanId.HasValue ? DateTime.Now : (DateTime?)null,
                                DanismanOnayladi = danismanId.HasValue ? true : (bool?)null,
                                BasvuruTarihi = tijBasvuruOneri?.TijBasvuruOneriID > 0 ? tijBasvuruOneri.BasvuruTarihi : DateTime.Now,
                                DonemBaslangicYil = donemBilgi.BaslangicYil,
                                DonemID = donemBilgi.DonemId,
                                DegisiklikAciklamasi = kModel.TijFormTipID != TijFormTipiEnum.YeniForm ? kModel.DegisiklikAciklamasi : null,
                                SozluSinavBasariTarihi = ogrenciYeterlikBilgi.DR_YET_SOZ_SNV_TARIH.ToDate().Value,
                                IsTezDiliTr = obsOgrenci.IsTezDiliTr,
                                TezBaslikTr = obsOgrenci.OgrenciTez.TEZ_BASLIK,
                                TezBaslikEn = obsOgrenci.OgrenciTez.TEZ_BASLIK_ENG,
                                TijBasvuruOneriJurilers = jurilers
                            });

                            if (!isAnaBasvuruVar) _entities.TijBasvurus.Add(tijBasvuru);
                            _entities.SaveChanges();
                            if (tijBasvuruOneri.TijBasvuruOneriID <= 0)
                            {
                                if (tijBasvuruOneri.DanismanOnayladi == true)
                                {
                                    TijBus.SendMailDanismanOnay(uniqueId);

                                }
                                else if (!tijBasvuruOneri.DanismanOnayladi.HasValue)
                                {
                                    TijBus.SendMailBasvuruYapildi(uniqueId);
                                }
                            }
                        }
                    }
                }
            }
            mMessage.IsSuccess = mMessage.Messages.Count == 0;
            if (mMessage.Messages.Count > 0)
            {
                mMessage.Title = "Jüri Öneri Formu Aşağıdaki Sebeplerden Dolayı Oluşturulamadı.";
                mMessage.IsSuccess = false;
                mMessage.MessageType = MsgTypeEnum.Warning;
            }
            return new
            {
                mMessage,
                selectedJuriNum,
                IsYeniJO = isJuriOnerisiVar
            }.ToJsonResult();
        }
        public ActionResult DanismanOnayKayit(Guid tijBasvuruOneriUniqueId, bool? isDanismanOnay, string danismanOnaylanmamaAciklamasi)
        {
            var mmMessage = new MmMessage
            {
                Title = "Jüri öneri formu danışman onay işlemi"
            };

            var tijBasvuruOneri = _entities.TijBasvuruOneris.First(p => p.UniqueID == tijBasvuruOneriUniqueId);

            if (!UserIdentity.Current.IsYetkiliTij)
            {
                if (tijBasvuruOneri.TezDanismanID != UserIdentity.Current.Id)
                {
                    mmMessage.Messages.Add("Danışman olarak atanmadığınız jüri önerisi için onay işlemi yapamazsınız!");
                }
            }

            if (!mmMessage.Messages.Any())
            {
                if (tijBasvuruOneri.EYKYaGonderildi.HasValue)
                {
                    mmMessage.Messages.Add("Eyk'ya gönderim işlemi yapıldıktan sonra oyan işlemi yapılamaz.");
                }
                else if (isDanismanOnay == false && danismanOnaylanmamaAciklamasi.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Ret seçeneği için Açıklama girilmesi zorunludur.");
                }
                bool sendMail = false;
                if (mmMessage.Messages.Count == 0)
                {

                    if (isDanismanOnay.HasValue && isDanismanOnay != tijBasvuruOneri.DanismanOnayladi)
                    {
                        sendMail = true;
                        var uniqueId = Guid.NewGuid();
                        tijBasvuruOneri.UniqueID = uniqueId;
                        tijBasvuruOneri.FormKodu = uniqueId.ToString().Substring(0, 8).ToUpper();
                    }
                    tijBasvuruOneri.DanismanOnayladi = isDanismanOnay;
                    tijBasvuruOneri.DanismanOnaylanmamaAciklamasi = danismanOnaylanmamaAciklamasi;
                    tijBasvuruOneri.DanismanOnayTarihi = DateTime.Now;

                    tijBasvuruOneri.TijBasvuru.IsYeniBasvuruYapilabilir = isDanismanOnay == false;



                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("TiJuriOnerileriGb", LogCrudType.Update, tijBasvuruOneri.ToJson());
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(isDanismanOnay.HasValue ? (isDanismanOnay.Value ? "Jüri öneri formu Onaylandı." : "Jüri öneri formu Ret Edildi.") : "Onaylama İşlemi Geril Alındı.");
                    if (sendMail)
                    {
                        var resul = TijBus.SendMailDanismanOnay(tijBasvuruOneri.UniqueID);
                        if (!resul.IsSuccess)
                        {
                            mmMessage.Messages.AddRange(resul.Messages);
                        }
                    }



                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            return Json(new { Messages = mmMessage }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult JuriAsilYedekDurumKayit(int tijBasvuruOneriJuriId, bool? isAsil)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Jüri öneri formu Asil/Yedek seçimi işlemi",
                MessageType = MsgTypeEnum.Warning
            };
            var juri = _entities.TijBasvuruOneriJurilers.FirstOrDefault(f =>
                f.TijBasvuruOneriJuriID == tijBasvuruOneriJuriId);

            if (!RoleNames.MezuniyetGelenBasvurularJuriOneriFormuEykOnay.InRoleCurrent())
            {
                mmMessage.Messages.Add("Jüri öneri formunda Asil/Yedek jüri adayı seçimi yetkisine sahip değilsiniz!");
            }
            else if (juri == null)
            {
                mmMessage.Messages.Add("İşlem yapılmak istenen jüri öneri formu sistemde bulunamadı!");
            }
            else
            {
                var tijBasvuruOneri = juri.TijBasvuruOneri;
                if (tijBasvuruOneri.EYKYaHazirlandi == false)
                {
                    mmMessage.Messages.Add("İşlem yapılmak istenen jüri öneri formu EYK'ya hazırlandı seçeneği ile kayıt edilmediğinden Asil/Yedek jüri adayı seçimi yapamazsınız!");
                }
                else if (tijBasvuruOneri.EYKDaOnaylandi == true)
                {
                    mmMessage.Messages.Add("İşlem yapılmak istenen jüri öneri formu EYK'da onaylandı seçeneği ile kayıt edildiğinden Asil/Yedek jüri adayı seçimi yapamazsınız!");
                }
                if (mmMessage.Messages.Count == 0 && isAsil.HasValue)
                {
                    var isOnayVar = tijBasvuruOneri.TijBasvuruOneriJurilers.Any(p =>
                        p.IsYeniOrOnceki && p.IsAsil == true && !p.IsTezDanismani && p.IsYtuIciJuri == juri.IsYtuIciJuri);

                    if (isOnayVar)
                    {
                        mmMessage.Messages.Add((juri.IsYtuIciJuri ? "YTU içinden" : "YTU Dışından") + " asil olarak seçilmiş bir jüri önerisi bulunmaktadır.");
                    }

                    //if (juri.IsYtuIciJuri)
                    //{
                    //    tijBasvuruOneri.TijBasvuruOneriJurilers.Where(p => p.IsYeniOrOnceki && p.).Count(p => p.IsAsil == true);
                    //}


                    //var asilKriterCount = tijBasvuruOneri.TijFormTipID == TijFormTipi.YeniForm ? 3 : (tijBasvuruOneri.TijDegisiklikTipID == TijDegisiklikTipi.YtuIciVeDisiDegisiklik ? 2 : 1);

                    //var adayCount = tijBasvuruOneri.TijBasvuruOneriJurilers.Where(p => p.IsYeniOrOnceki).Count(p => p.IsAsil == true);
                    //if (isAsil == true && adayCount >= asilKriterCount)
                    //    mmMessage.Messages.Add("Jüri adayı önerisinde toplamda " + asilKriterCount + " asil jüri seçilebilir.");

                }
            }
            if (mmMessage.Messages.Count == 0)
            {
                juri.IsAsil = isAsil;
                _entities.SaveChanges();
                LogIslemleri.LogEkle("TijBasvuruOneriJuriler", LogCrudType.Update, juri.ToJson());

                mmMessage.IsSuccess = true;
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return new
            {
                mmMessage.IsSuccess,
                Messages = strView
            }.ToJsonResult();
        }

        public ActionResult JuriOneriFormuOnayDurumKayit(Guid tijBasvuruOneriUniqueId, int onayTipId, bool? onaylandi, string aciklama, DateTime? onayTarihi)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Jüri öneri formu " + (onayTipId == EykTipEnum.EykDaOnaylandi ? "EYK'da onay" : (onayTipId == EykTipEnum.EykYaHazirlandi ? "EYK'ya Hazırlık" : "EYK'ya gönderim")) + " işlemi",
                MessageType = MsgTypeEnum.Warning
            };

            var tijBasvuruOneri = _entities.TijBasvuruOneris.FirstOrDefault(p => p.UniqueID == tijBasvuruOneriUniqueId);

            if (onayTipId == EykTipEnum.EykYaGonderildi && !RoleNames.TiJuriOnerileriEykYaGonder.InRoleCurrent())
            {
                mmMessage.Messages.Add("Jüri öneri formunda EYK'ya gönderme yetkisine sahip değilsiniz!");
            }
            else if (onayTipId == EykTipEnum.EykDaOnaylandi && !RoleNames.TiJuriOnerileriEykyaHazirlandiYetkisi.InRoleCurrent())
            {
                mmMessage.Messages.Add("Jüri öneri formunda EYK'ya hazırlık yetkisine sahip değilsiniz!");
            }
            else if (onayTipId == EykTipEnum.EykDaOnaylandi && !RoleNames.TiJuriOnerileriEykDaOnay.InRoleCurrent())
            {
                mmMessage.Messages.Add("Jüri öneri formunda EYK'da onay yetkisine sahip değilsiniz!");
            }
            else if (tijBasvuruOneri == null)
            {
                mmMessage.Messages.Add("İşlem yapılmak istenen jüri öneri formu sistemde bulunamadı!");
            }
            else
            {
                //  eykDaOnayOrEykYaGonderim
                if (onayTipId == EykTipEnum.EykDaOnaylandi)
                {
                    if (tijBasvuruOneri.EYKYaHazirlandi != true)
                    {
                        mmMessage.Messages.Add("EYK Ya hazırlanmayan jüri öneri formu üzerinde EYK Onayı işlemi yapılamaz!");
                    }
                    else if (onaylandi == true && !onayTarihi.HasValue)
                    {
                        mmMessage.Messages.Add("EYK'da onay tarihini giriniz!");
                    }
                    else if (onaylandi == false && aciklama.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("EYK'da onaylanmama sebebini giriniz!");
                    }
                    else if (tijBasvuruOneri.TijBasvuru.TijBasvuruOneris.Any(a => a.TijBasvuruOneriID > tijBasvuruOneri.TijBasvuruOneriID))
                    {
                        mmMessage.Messages.Add("Yeni bir jüri önerisi başvurusu varken önceki jüri önerisi eyk onay durumu değiştirilemez!");
                    }
                }
                else if (onayTipId == EykTipEnum.EykYaGonderildi)
                {
                    if (tijBasvuruOneri.EYKYaHazirlandi.HasValue)
                    {
                        mmMessage.Messages.Add("EYK ya hazırlama işlemi yapılan bir form da Eyk'ya gönderim işlemi gerçekleştirilemez!");
                    }
                    else if (onaylandi == false && aciklama.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("EYK'ya gönderiminin onaylanmama sebebini giriniz!");
                    }
                }

                if (mmMessage.Messages.Count == 0 && onayTipId == EykTipEnum.EykDaOnaylandi && onaylandi == true)
                {
                    var asilKriterCount = tijBasvuruOneri.TijFormTipID == TijFormTipiEnum.YeniForm ? 3 : (tijBasvuruOneri.TijDegisiklikTipID == TijDegisiklikTipiEnum.YtuIciVeDisiDegisiklik ? 2 : 1);
                    var asilCount = tijBasvuruOneri.TijBasvuruOneriJurilers.Where(p => p.IsYeniOrOnceki).Count(p => p.IsAsil == true);
                    if (asilCount != asilKriterCount)
                        mmMessage.Messages.Add("Jüri öneri formunda EYK'da onaylandı işlemini yapabilmeniz için:<br />* Jüri adayı önerisinde toplamda " + asilKriterCount + " Asil aday belirlenmesi gerekmektedir.");



                }

                if (!mmMessage.Messages.Any())
                {

                    var isEykdaOnaylandiOrGonderildiDurum = onayTipId == EykTipEnum.EykYaGonderildi ? tijBasvuruOneri.EYKYaGonderildi : (onayTipId == EykTipEnum.EykYaHazirlandi
                        ? tijBasvuruOneri.EYKYaHazirlandi
                        : tijBasvuruOneri.EYKDaOnaylandi);

                    // eyk yada eykya gönderimi onay işlemi gördü yada yeni onay durumu onaylanmadı değil ise öğrencinin aktiflik durumunu kontrol et
                    if (isEykdaOnaylandiOrGonderildiDurum.HasValue || onaylandi != false)
                    {
                        var ogrenciObsBilgi =
                            KullanicilarBus.OgrenciBilgisiGuncelleObs(tijBasvuruOneri.TijBasvuru.KullaniciID);

                        if (!ogrenciObsBilgi.KayitVar)
                        {
                            mmMessage.Messages.Add(
                                "Öğrenci OBS sisteminde aktif öğrenci olarak gözükmemektedir. Onay işlemi yapılamaz.");
                        }
                        else if (tijBasvuruOneri.TijBasvuru.OgrenciNo != ogrenciObsBilgi.OgrenciInfo.OGR_NO)
                        {
                            mmMessage.Messages.Add(
                                "Ana başvurunuzdaki öğrenci numarası ile güncel öğrenci numarası uyuşmuyor. Öğrencinin kaydı silinip farklı bir programa kaydolmuş olabilir ya da numarası değişmiş olabilir. Onay işlemi yapılamaz.");
                        }
                    }
                }

                if (mmMessage.Messages.Count == 0)
                {
                    var isDegisiklikVar = false;
                    if (onayTipId == EykTipEnum.EykYaGonderildi)
                    {
                        tijBasvuruOneri.TijBasvuru.IsYeniBasvuruYapilabilir = onaylandi == false;

                        isDegisiklikVar = tijBasvuruOneri.EYKYaGonderildi != onaylandi || aciklama != tijBasvuruOneri.EYKYaGonderimDurumAciklamasi;
                        tijBasvuruOneri.EYKYaGonderimDurumAciklamasi = onaylandi == false ? aciklama : "";
                        tijBasvuruOneri.EYKYaGonderildi = onaylandi;
                        tijBasvuruOneri.EYKYaGonderildiIslemTarihi = DateTime.Now;
                        tijBasvuruOneri.EYKYaGonderildiIslemYapanID = UserIdentity.Current.Id;
                        mmMessage.Messages.Add("Form EYK ya " + (onaylandi.HasValue ? (onaylandi.Value ? "'Gönderildi'" : "'Gönderilmedi'") : "Gönderilmesi bekleniyor") + " şeklinde güncellendi...");
                    }
                    else if (onayTipId == EykTipEnum.EykYaHazirlandi)
                    {

                        tijBasvuruOneri.EYKYaHazirlandi = onaylandi;
                        tijBasvuruOneri.EYKYaHazirlandiIslemTarihi = DateTime.Now;
                        tijBasvuruOneri.EYKYaHazirlandiIslemYapanID = UserIdentity.Current.Id;
                        mmMessage.Messages.Add("Form EYK ya " + (onaylandi.HasValue ? (onaylandi.Value ? "'Hazırlandı'" : "'Hazırlanmadı'") : " Hazırlanması bekleniyor") + " şeklinde güncellendi...");
                    }
                    else if (onayTipId == EykTipEnum.EykDaOnaylandi)
                    {
                        tijBasvuruOneri.TijBasvuru.IsYeniBasvuruYapilabilir = onaylandi.HasValue;
                        isDegisiklikVar = tijBasvuruOneri.EYKDaOnaylandi != onaylandi || aciklama != tijBasvuruOneri.EYKDaOnaylanmadiDurumAciklamasi || tijBasvuruOneri.EYKTarihi != onayTarihi;
                        tijBasvuruOneri.EYKDaOnaylandi = onaylandi;
                        if (onaylandi.HasValue) { tijBasvuruOneri.EYKTarihi = onayTarihi; }
                        tijBasvuruOneri.EYKDaOnaylandiIslemYapanID = UserIdentity.Current.Id;
                        tijBasvuruOneri.EYKDaOnaylandiIslemTarihi = DateTime.Now;
                        tijBasvuruOneri.EYKDaOnaylanmadiDurumAciklamasi = onaylandi == false ? aciklama : "";

                        mmMessage.Messages.Add("Form EYK da " + (onaylandi.HasValue ? (onaylandi.Value ? "'Onaylandı'" : "'Onaylanmadı'") : "İşlem bekliyor") + " şeklinde güncellendi...");


                    }
                    _entities.SaveChanges();
                    mmMessage.MessageType = MsgTypeEnum.Success;
                    mmMessage.IsSuccess = true;

                    LogIslemleri.LogEkle("TijBasvuruOneri", LogCrudType.Update, tijBasvuruOneri.ToJson());

                    if (onaylandi.HasValue && isDegisiklikVar)
                    {
                        var eykDaOnayOrGonderim = onayTipId == EykTipEnum.EykDaOnaylandi;
                        TijBus.SendMailEykOnay(tijBasvuruOneriUniqueId, eykDaOnayOrGonderim, onaylandi.Value);
                    }

                }
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return new
            {
                mmMessage.IsSuccess,
                Messages = strView,
                mmMessage
            }.ToJsonResult();
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
        public ActionResult GetTutanakRaporuExport(string basTar, string bitTar, bool exportWordOrExcel, bool isDegisiklik, int enstituOnayDurumId, string ekd)
        {

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var baslangicTarihi = basTar.ToDate(DateTime.Now);
            var bitisTarihi = bitTar.ToDate(DateTime.Now);

            var qData = (from tijBasvuruOneri in _entities.TijBasvuruOneris.Where(p => !p.IsObsData)
                         join tijBasvuru in _entities.TijBasvurus on tijBasvuruOneri.TijBasvuruID equals tijBasvuru.TijBasvuruID
                         join tezDanisman in _entities.Kullanicilars on tijBasvuruOneri.TezDanismanID equals tezDanisman.KullaniciID
                         join programlar in _entities.Programlars on tijBasvuru.ProgramKod equals programlar.ProgramKod
                         join anabilimDallari in _entities.AnabilimDallaris on programlar.AnabilimDaliID equals anabilimDallari
                             .AnabilimDaliID
                         join ogrenci in _entities.Kullanicilars on tijBasvuru.KullaniciID equals ogrenci.KullaniciID
                         let oncekiBasvuru = tijBasvuruOneri.TijFormTipID == TijFormTipiEnum.YeniForm
                                            ? null
                                            : tijBasvuru.TijBasvuruOneris.Where(p => (p.IsObsData || p.EYKDaOnaylandi == true) && p.TijBasvuruOneriID != tijBasvuruOneri.TijBasvuruOneriID).OrderByDescending(o => o.TijBasvuruOneriID).FirstOrDefault()

                         where tijBasvuru.EnstituKod == enstituKod &&
                               enstituOnayDurumId == 1 ? (
                                                        tijBasvuruOneri.EYKYaGonderildi == true &&
                                                        tijBasvuruOneri.EYKYaGonderildiIslemTarihi >= baslangicTarihi &&
                                                        tijBasvuruOneri.EYKYaGonderildiIslemTarihi <= bitisTarihi)
                                                    : (enstituOnayDurumId == 2 ? (
                                                                                tijBasvuruOneri.EYKYaHazirlandi == true &&
                                                                                tijBasvuruOneri.EYKYaHazirlandiIslemTarihi >= baslangicTarihi &&
                                                                                tijBasvuruOneri.EYKYaHazirlandiIslemTarihi <= bitisTarihi)
                                                                            :
                                                                               (tijBasvuruOneri.EYKDaOnaylandi == true &&
                                                                                    tijBasvuruOneri.EYKTarihi >= baslangicTarihi &&
                                                                                    tijBasvuruOneri.EYKTarihi <= bitisTarihi)
                                                                                ) &&
                               tijBasvuruOneri.TijFormTipleri.IsDegisiklik == isDegisiklik
                         select new
                         {
                             tijBasvuruOneri.TijFormTipID,
                             tijBasvuruOneri.EYKTarihi,
                             ogrenci.OgrenciNo,
                             TezDanismanAdSoyad = tezDanisman.Ad + " " + tezDanisman.Soyad,
                             TezDanismanUnvanAdi = tezDanisman.Unvanlar.UnvanAdi,
                             OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                             anabilimDallari.AnabilimDaliAdi,
                             programlar.ProgramAdi,
                             tijBasvuruOneri.IsTezDiliTr,
                             tijBasvuruOneri.TezBaslikTr,
                             tijBasvuruOneri.TezBaslikEn,
                             Juriler = tijBasvuruOneri.TijBasvuruOneriJurilers.ToList(),
                             OncekiDanisman = tijBasvuruOneri.TijFormTipID == TijFormTipiEnum.YeniForm
                                            ? null
                                            : _entities.Kullanicilars.FirstOrDefault(f => f.KullaniciID == oncekiBasvuru.TezDanismanID),

                         }).OrderBy(o => o.EYKTarihi).ToList();


            var rprTijTutanakModels = new List<RprTijTutanakModel>();
            var rprTijTutanakModel = new RprTijTutanakModel
            {
                TutanakAdi = "Doktora - Tez İzleme Komitesi Atanması Hk.",
                Aciklama = "Doktora yeterlik sınavında başarılı olan öğrenciler için tez izleme komitelerinin aşağıda adı yazılı öğretim üyelerinden oluşturulmasına," +
                " Tez izleme komitesince 'YTÜ Lisansüstü Eğitim-Öğretim Yönetmeliği Senato Esasları' nın ilgili maddesine göre(yeterlik sınav tarihinden başlayarak)" +
                " 6(altı) ay içerisinde tez önerisi savunması yapılarak sonucun bir tutanakla ilgili anabilim dalı başkanlığı aracılığıyla Enstitümüze bildirilmesine oybirliğiyle karar verildi."
            };

            foreach (var itemO in qData.Where(p => p.TijFormTipID == TijFormTipiEnum.YeniForm))
            {
                var uyeler = itemO.Juriler.Where(p => p.IsAsil == true).ToList();
                var danisman = uyeler.First(p => p.IsTezDanismani);
                var asilUye1 = uyeler.Where(p => !p.IsTezDanismani).ToList()[0];
                var asilUye2 = uyeler.Where(p => !p.IsTezDanismani).ToList()[1];

                rprTijTutanakModel.DetayData.Add(new RprTijTutanakRowModel
                {
                    IsNewOrEdit = true,
                    OgrenciBilgi = itemO.OgrenciNo + " " + itemO.OgrenciAdSoyad + " (" + itemO.AnabilimDaliAdi + " / " + itemO.ProgramAdi + ")",
                    DanismanAdSoyad = danisman.UnvanAdi + " " + danisman.AdSoyad,
                    DanismanUni = danisman.UniversiteID.HasValue ? danisman.Universiteler.Ad : danisman.UniversiteAdi,
                    AsilUye1 = asilUye1.UnvanAdi + " " + asilUye1.AdSoyad,
                    AsilUye1Uni = asilUye1.UniversiteID.HasValue ? asilUye1.Universiteler.Ad : asilUye1.UniversiteAdi,
                    AsilUye2 = asilUye2.UnvanAdi + " " + asilUye2.AdSoyad,
                    AsilUye2Uni = asilUye2.UniversiteID.HasValue ? asilUye2.Universiteler.Ad : asilUye2.UniversiteAdi,
                    TezKonusu = itemO.IsTezDiliTr ? itemO.TezBaslikTr : itemO.TezBaslikEn
                });

                rprTijTutanakModels.Add(rprTijTutanakModel);
            }



            var rprTijTutanakModels2 = new List<RprTijTutanakModel>();
            var rprTijTutanakModel2 = new RprTijTutanakModel
            {


                TutanakAdi = "Tez izleme Komitesi Değişikliği Hk.",
                Aciklama = "Enstitümüz İlgili Anabilim Dalı Başkanlığından iletilen muhtelif sayılı yazılar okunarak;" +
                            "aşağıda bilgileri verilen doktora öğrencilerine ilişkin tez izleme komitesi değişiklik önerileri görüşüldü. " +
                            "Yapılan görüşmeler sonunda, ilgili doktora öğrencilerine ilişkin tez izleme komitesi değişiklik önerilerinin aşağıda belirtildiği şekilde kabul edilmesine," +
                            "oy birliğiyle karar verildi."
            };

            foreach (var itemO in qData.Where(p => p.TijFormTipID != TijFormTipiEnum.YeniForm))
            {
                var varolanUyeler = itemO.Juriler.Where(p => !p.IsYeniOrOnceki && p.IsAsil == true && !p.IsTezDanismani).ToList();
                var varolanAsilUyeYtuIci = varolanUyeler.First(p => p.IsYtuIciJuri);
                var varolanAsilUyeYtuDisi = varolanUyeler.First(p => !p.IsYtuIciJuri);
                var varolanDanismanAdSoyad = itemO.OncekiDanisman.Unvanlar.UnvanAdi + " " + itemO.OncekiDanisman.Ad + " " + itemO.OncekiDanisman.Soyad;


                var yeniUyeler = itemO.Juriler.Where(p => p.IsYeniOrOnceki && p.IsAsil == true && !p.IsTezDanismani).ToList();
                var yeniAsilUyeYtuIci = yeniUyeler.FirstOrDefault(p => p.IsYtuIciJuri) ?? varolanAsilUyeYtuIci;
                var yeniAsilUyeYtuDisi = yeniUyeler.FirstOrDefault(p => !p.IsYtuIciJuri) ?? varolanAsilUyeYtuDisi;



                rprTijTutanakModel2.DetayData.Add(new RprTijTutanakRowModel
                {
                    IsNewOrEdit = false,
                    OgrenciBilgi = itemO.OgrenciNo + " " + itemO.OgrenciAdSoyad + " (" + itemO.AnabilimDaliAdi + " / " + itemO.ProgramAdi + ")",
                    DanismanAdSoyad = varolanDanismanAdSoyad,
                    DanismanUni = "YILDIZ TEKNİK ÜNİVERSTESİ",
                    AsilUye1 = varolanAsilUyeYtuIci.UnvanAdi + " " + varolanAsilUyeYtuIci.AdSoyad,
                    AsilUye1Uni = "YILDIZ TEKNİK ÜNİVERSTESİ",
                    AsilUye2 = varolanAsilUyeYtuDisi.UnvanAdi + " " + varolanAsilUyeYtuDisi.AdSoyad,
                    AsilUye2Uni = varolanAsilUyeYtuDisi.UniversiteID.HasValue ? varolanAsilUyeYtuDisi.Universiteler.Ad : varolanAsilUyeYtuDisi.UniversiteAdi,
                    YeniDanismanAdSoyad = itemO.TezDanismanUnvanAdi + " " + itemO.TezDanismanAdSoyad,
                    YeniDanismanUni = "YILDIZ TEKNİK ÜNİVERSTESİ",
                    YeniAsilUye1 = yeniAsilUyeYtuIci.UnvanAdi + " " + yeniAsilUyeYtuIci.AdSoyad,
                    YeniAsilUye1Uni = "YILDIZ TEKNİK ÜNİVERSTESİ",
                    YeniAsilUye2 = yeniAsilUyeYtuDisi.UnvanAdi + " " + yeniAsilUyeYtuDisi.AdSoyad,
                    YeniAsilUye2Uni = yeniAsilUyeYtuDisi.UniversiteID.HasValue ? yeniAsilUyeYtuDisi.Universiteler.Ad : yeniAsilUyeYtuDisi.UniversiteAdi,
                    TezKonusu = itemO.IsTezDiliTr ? itemO.TezBaslikTr : itemO.TezBaslikEn
                });
                rprTijTutanakModels2.Add(rprTijTutanakModel2);
            }

            var report = new XtraReport();

            if (rprTijTutanakModels.Count > 0)
            {
                var rpr = new RprTijTutanak();
                rpr.DataSource = rprTijTutanakModels[0];
                rpr.CreateDocument();

                report.Pages.AddRange(rpr.Pages);
            }
            if (rprTijTutanakModels2.Count > 0)
            {
                var rpr2 = new RprTijTutanak();

                rpr2.DataSource = rprTijTutanakModels2[0];
                rpr2.CreateDocument();
                report.Pages.AddRange(rpr2.Pages);
            }

            report.ExportOptions.Html.ExportMode = HtmlExportMode.SingleFilePageByPage;
            using (MemoryStream ms = new MemoryStream())
            {
                report.ExportToHtml(ms);
                ms.Position = 0;
                var sr = new StreamReader(ms);
                var html = sr.ReadToEnd();
                var raporAdi = (isDegisiklik ? "Tez izleme komite değişiklikleri" : "Tez izleme komite önerileri");
                return File(System.Text.Encoding.UTF8.GetBytes(html), (exportWordOrExcel ? "application/vnd.ms-word" : "application/ms-excel"), raporAdi + " (" + basTar.Replace("-", ".") + "-" + bitTar.Replace("-", ".") + ")." + (exportWordOrExcel ? "doc" : "xls"));

            }
        }
        public ActionResult SilDetay(Guid tijBasvuruOneriUniqueId)
        {
            var mmMessage = TijBus.GetTijBasvuruDetayIslemKontrol(tijBasvuruOneriUniqueId);
            var removedAllData = false;
            if (mmMessage.IsSuccess)
            {
                var kayit = _entities.TijBasvuruOneris.First(p => p.UniqueID == tijBasvuruOneriUniqueId);
                try
                {
                    if (kayit.TijBasvuru.TijBasvuruOneris.Count == 1)
                    {
                        removedAllData = true;
                        _entities.TijBasvurus.Remove(kayit.TijBasvuru);
                    }
                    else
                    {
                        kayit.TijBasvuru.IsYeniBasvuruYapilabilir = true;
                    }

                    _entities.TijBasvuruOneris.Remove(kayit);
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("TiJuriOnerileriGb", LogCrudType.Delete, kayit.ToJson());
                    if (removedAllData)
                    {
                        LogIslemleri.LogEkle("TiJuriOnerileriGb", LogCrudType.Delete, kayit.ToJson());
                    }
                    mmMessage.Messages.Add(kayit.BasvuruTarihi + " Tarihli tez izleme jüri önerisi silindi.");
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(kayit.BasvuruTarihi + " Tarihli tez izleme jüri önerisi silinemedi.");
                    mmMessage.Title = "Hata";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }

            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView, removedAllData }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}