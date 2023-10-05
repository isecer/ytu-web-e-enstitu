using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
using BiskaUtil;
using DevExpress.XtraPrinting;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Models.ObsService;
using LisansUstuBasvuruSistemi.Raporlar.Mezuniyet;
using LisansUstuBasvuruSistemi.Raporlar.TezDanismanOneri;
using LisansUstuBasvuruSistemi.Raporlar.TezIzlemeJuriOneri;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

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
            var q = from s in _entities.TijBasvurus.Where(p => p.EnstituKod == model.EnstituKod && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod))
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
                        TezDanismanIds = s.TijBasvuruOneris.Select(sd => sd.TezDanismanID).ToList(),
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
                q = q.Where(p => p.TezDanismanIds.Contains(danismanId));
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
            else q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.SonBasvuru != null ? o.SonBasvuru.BasvuruTarihi : o.BasvuruTarihi);
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

            var jsonResult = TezIzlemeJuriOneriBus.GetFilterOgrenciJsonResult(term, enstituKod, danismanId);
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
                MessageType = Msgtype.Success,
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

                var ogrencilers = TezIzlemeJuriOneriBus.GetDanismanOgrencileriKullaniciId(danismanId.Value);
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
                        mMessage.Messages.Add("Başvuru yapacağınız öğrencinin yeterlik sınavından başarılı olması gerekmetkedir.");
                    }
                    else
                    {
                        var devamEdenBasvuru = _entities.TijBasvurus.FirstOrDefault(f =>
                            f.KullaniciID == kullaniciId && f.OgrenciNo == kul.OgrenciNo &&
                            !f.IsYeniBasvuruYapilabilir);
                        if (devamEdenBasvuru == null)
                        {
                            if (TezDanismanOneriBus.IsAktifDanismanOneriVar(kul.KullaniciID))
                            {
                                mMessage.Messages.Add("Öğrencinin yapmış olduğu bir Tez Danışman Öneri başvurusu bulunmakta. Jüri önerisi yapılabilmesi bu sürecinin tamamlanması gerekmektedir.");
                            }
                            else if (TezDanismanOneriBus.IsAktifEsDanismanOneriVar(kul.KullaniciID))
                            {
                                mMessage.Messages.Add("Öğrencinin yapmış olduğu bir Tez Eş Danışman Öneri başvurusu bulunmakta. Jüri önerisi yapılabilmesi bu sürecinin tamamlanması gerekmektedir.");
                            }
                            else if (enstituKod != kul.Programlar.AnabilimDallari.EnstituKod)
                            {
                                var ogrenciEnstitu = _entities.Enstitulers.First(f =>
                                    f.EnstituKod == kul.Programlar.AnabilimDallari.EnstituKod);
                                mMessage.Messages.Add("Sistemde seçili olan ensitü " + enstituAdi + " Öğrenci okuduğu enstitü " + ogrenciEnstitu.EnstituAd + ". sistem üzerinden " + ogrenciEnstitu.EnstituAd + " enstitüsüne geçiş yapıp tekrar öneri yapmayı deneyizi.");

                            }
                            else
                            {
                                basvuruUniqueId = TezIzlemeJuriOneriBus.TezIzlemeJuriOneriSenkronizasyon(kullaniciId);
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
                mMessage.MessageType = Msgtype.Warning;
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
                MessageType = Msgtype.Success,
                IsSuccess = true
            };

            var tijBasvuru = _entities.TijBasvurus.FirstOrDefault(f => f.UniqueID == basvuruUniqueId);
            TijBasvuruOneri tijBasvuruOneri = null;


            var unvanlar = UnvanlarBus.GetCmbJuriUnvanlar(true);
            var universiteler = Management.cmbGetAktifUniversiteler(true);


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
                SListTijDegisiklikTip = new SelectList(TezIzlemeJuriOneriBus.CmbTijDegisiklikTipListe(true), "Value", "Caption", tijBasvuruOneri?.TijDegisiklikTipID),
                SListUnvanAdi = new SelectList(unvanlar, "Value", "Caption"),
                SListUniversiteID = new SelectList(universiteler, "Value", "Caption"),
            };
            model.SListTijFormTip = new SelectList(TezIzlemeJuriOneriBus.CmbTijFormTipListe(true, model.IsIlkOneri), "Value", "Caption",
                tijBasvuruOneri?.TijFormTipID);



            StudentControl ogrenciInfo;
            if (tijBasvuruOneri != null)
            {
                ogrenciInfo = KullanicilarBus.OgrenciKontrol(tijBasvuruOneri.TijBasvuru.OgrenciNo);
                model.OgrenciAdSoyad = kul.Ad + " " + kul.Soyad + " - " + tijBasvuruOneri.TijBasvuru.OgrenciNo;
                model.TijBasvuruID = tijBasvuruOneri.TijBasvuruID;
                model.TijDegisiklikTipID = tijBasvuruOneri.TijDegisiklikTipID;
                model.TijFormTipID = tijBasvuruOneri.TijFormTipID;
                model.IsTezDiliTr = ogrenciInfo.IsTezDiliTr;
                model.TezBaslikTr = ogrenciInfo.OgrenciTez.TEZ_BASLIK;
                model.TezBaslikEn = ogrenciInfo.OgrenciTez.TEZ_BASLIK_ENG;
                model.JoFormJuriList = tijBasvuruOneri.TijBasvuruOneriJurilers.Select(s => new KrTijOneriFormuJurileri
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
                    model.TijFormTipID = TijFormTipi.YeniForm;

            }

            if (!model.JoFormJuriList.Any(a => a.IsTezDanismani))
            {
                if (!ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.IsNullOrWhiteSpace())
                {
                    var tdBilgi = new KrTijOneriFormuJurileri
                    {
                        IsTezDanismani = true,
                        IsYtuIciJuri = true,
                        UniversiteID = Management.UniversiteYtuKod,
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
                    mMessage.Messages.Add("Obs sisteminden öğrenci bilgisi sorgulanırken bir hata oluştu!");
                }
                else
                {
                    if (ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.IsNullOrWhiteSpace())
                        mMessage.Messages.Add("Danışman Bilgisi Çekilemedi.");

                    if (mMessage.Messages.Count > 0)
                    {
                        mMessage.MessageType = Msgtype.Warning;
                        mMessage.Messages.Add("Jüri öneri formunu oluşturabilmeniz için bu durumu enstitü yetkililerine iletiniz.");
                    }
                }

            }
            if (mMessage.Messages.Count == 0)
            {


                view = ViewRenderHelper.RenderPartialView("TiJuriOnerileriGb", "TijOneriFormu", model);
            }
            else { mMessage.IsSuccess = false; mMessage.MessageType = Msgtype.Warning; }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);

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
                MessageType = Msgtype.Success,
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
                mMessage.Messages.Add("Jür öneri formu kayıt işlemi için yetkili değilsiniz.");
            }
            else if (!isBasvuruAcik)
            {
                mMessage.Messages.Add("Jüri öneri formu başvuru süreci kapalıdır. Detaylı bilgi almak için " + enstituAdi + " ile iletişime geçiniz.");
            }
            else if (danismanId.HasValue && TezIzlemeJuriOneriBus.GetDanismanOgrencileriKullaniciId(danismanId.Value).All(a => a != kul.KullaniciID))
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
                    var messages = TezIzlemeJuriOneriBus.GetTijBasvuruDetayIslemKontrol(tijBasvuruOneri.UniqueID);
                    mMessage.Messages.AddRange(messages.Messages);
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
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = kModel.TijFormTipID <= 0 ? Msgtype.Warning : Msgtype.Success, PropertyName = "TijFormTipID" });

                    if (kModel.TijFormTipID != TijFormTipi.YeniForm && !kModel.TijDegisiklikTipID.HasValue)
                    {
                        mMessage.Messages.Add("Değiştirilecek jüri grubu seçiniz.");
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = !kModel.TijDegisiklikTipID.HasValue ? Msgtype.Warning : Msgtype.Success, PropertyName = "TijDegisiklikTipID" });

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
                    if (kModel.TijDegisiklikTipID == TijDegisiklikTipi.YtuIciDegisiklik)
                    {
                        qGroup = qGroup.Where(p => p.IsYtuIciJuri);
                        qData = qData.Where(p => p.IsYtuIciJuri).ToList();
                    }
                    else if (kModel.TijDegisiklikTipID == TijDegisiklikTipi.YtuDisiDegisiklik)
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
                            var adSoyadMsgType = item2.s.AdSoyadSuccess ? Msgtype.Success : Msgtype.Warning;
                            var unvanAdiMsgType = item2.s.UnvanAdiSuccess ? Msgtype.Success : Msgtype.Warning;
                            var emailMsgType = item2.s.EMailSuccess ? Msgtype.Success : Msgtype.Warning;
                            var universiteIdMsgType = item2.s.UniversiteIDSuccess ? Msgtype.Success : Msgtype.Warning;
                            var anabilimdaliMsgType = item2.s.AnabilimdaliAdiSuccess ? Msgtype.Success : Msgtype.Warning;
                            if (!isZorunlu && item.IsNullValues)
                            {
                                adSoyadMsgType = Msgtype.Nothing;
                                unvanAdiMsgType = Msgtype.Nothing;
                                emailMsgType = Msgtype.Nothing;
                                universiteIdMsgType = Msgtype.Nothing;
                                anabilimdaliMsgType = Msgtype.Nothing;
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
                        var unilers = _entities.Universitelers.ToList();
                        var kData = qData.Where(p => p.AdSoyadSuccess).ToList();
                        var isDegisiklikVar = false;
                        var varolanJurilers = tijBasvuruOneri.TijBasvuruOneriJurilers.ToList();
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
                            var uni = unilers.First(p => p.UniversiteID == item.UniversiteID);

                            tijBasvuruOneri.TijBasvuruOneriJurilers.Add(
                                new TijBasvuruOneriJuriler
                                {
                                    RowNum = item.RowNum,
                                    IsYtuIciJuri = item.IsYtuIciJuri,
                                    IsTezDanismani = item.IsTezDanismani,
                                    UnvanAdi = item.UnvanAdi.ToUpper(),
                                    AdSoyad = item.AdSoyad.ToUpper(),
                                    EMail = item.EMail,
                                    UniversiteID = item.UniversiteID,
                                    UniversiteAdi = uni.Ad,
                                    AnabilimdaliAdi = item.AnabilimdaliAdi,
                                    IsAsil = item.IsTezDanismani

                                });
                        } 

                        StudentControl obsOgrenci = KullanicilarBus.OgrenciKontrol(tijBasvuru != null ? tijBasvuru.OgrenciNo : kul.OgrenciNo);

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
                            var universitelers = _entities.Universitelers.ToList();
                            var donemBilgi = (tijBasvuruOneri.TijBasvuruOneriID > 0 ? tijBasvuru.BasvuruTarihi : DateTime.Now).ToAraRaporDonemBilgi();
                            tijBasvuru.TijBasvuruOneris.Add(new TijBasvuruOneri
                            {
                                UniqueID = uniqueId,
                                FormKodu = uniqueId.ToString().Substring(0, 8).ToUpper(),
                                TijFormTipID = kModel.TijFormTipID,
                                TijDegisiklikTipID = kModel.TijDegisiklikTipID,
                                IsObsData = false,
                                TezDanismanID = tijBasvuruOneri.TezDanismanID ?? kul.DanismanID,
                                DanismanOnayTarihi = danismanId.HasValue ? DateTime.Now : (DateTime?)null,
                                DanismanOnayladi = danismanId.HasValue ? true : (bool?)null,
                                BasvuruTarihi = tijBasvuruOneri.TijBasvuruOneriID > 0 ? tijBasvuruOneri.BasvuruTarihi : DateTime.Now,
                                DonemBaslangicYil = donemBilgi.BaslangicYil,
                                DonemID = donemBilgi.DonemID,
                                SozluSinavBasariTarihi = ogrenciYeterlikBilgi.DR_YET_SOZ_SNV_TARIH.ToDate().Value,
                                IsTezDiliTr = obsOgrenci.IsTezDiliTr,
                                TezBaslikTr = obsOgrenci.OgrenciTez.TEZ_BASLIK,
                                TezBaslikEn = obsOgrenci.OgrenciTez.TEZ_BASLIK_ENG,
                                TijBasvuruOneriJurilers = kData.Where(p => p.AdSoyadSuccess).Select(s =>
                                        new TijBasvuruOneriJuriler
                                        {
                                            IsAsil = s.IsTezDanismani,
                                            IsYtuIciJuri = s.IsYtuIciJuri,
                                            IsTezDanismani = s.IsTezDanismani,
                                            RowNum = s.RowNum,
                                            UnvanAdi = s.UnvanAdi,
                                            AdSoyad = s.AdSoyad,
                                            EMail = s.EMail,
                                            UniversiteID = s.UniversiteID,
                                            UniversiteAdi = universitelers.First(f => f.UniversiteID == s.UniversiteID)
                                                .Ad,
                                            AnabilimdaliAdi = s.AnabilimdaliAdi
                                        }).OrderBy(o =>
                                        o.RowNum).ToList()
                            });

                            if (!isAnaBasvuruVar) _entities.TijBasvurus.Add(tijBasvuru);
                            _entities.SaveChanges();
                            if (tijBasvuruOneri.TijBasvuruOneriID <= 0)
                            {
                                if (tijBasvuruOneri.DanismanOnayladi == true)
                                {
                                    TezIzlemeJuriOneriBus.SendMailDanismanOnay(uniqueId);

                                }
                                else if (!tijBasvuruOneri.DanismanOnayladi.HasValue)
                                {
                                    TezIzlemeJuriOneriBus.SendMailBasvuruYapildi(uniqueId);
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
                mMessage.MessageType = Msgtype.Warning;
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
                    LogIslemleri.LogEkle("TiJuriOnerileriGb", IslemTipi.Update, tijBasvuruOneri.ToJson());
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(isDanismanOnay.HasValue ? (isDanismanOnay.Value ? "Jüri öneri formu Onaylandı." : "Jüri öneri formu Ret Edildi.") : "Onaylama İşlemi Geril Alındı.");
                    if (sendMail)
                    {
                        var resul = TezIzlemeJuriOneriBus.SendMailDanismanOnay(tijBasvuruOneri.UniqueID);
                        if (!resul.IsSuccess)
                        {
                            mmMessage.Messages.AddRange(resul.Messages);
                        }
                    }



                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            return Json(new { Messages = mmMessage }, "application/json", JsonRequestBehavior.AllowGet); 
        } 
        public ActionResult JuriAsilYedekDurumKayit(int tijBasvuruOneriJuriId, bool? isAsil)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Jüri öneri formu Asil/Yedek seçimi işlemi",
                MessageType = Msgtype.Warning
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
                if (tijBasvuruOneri.EYKYaGonderildi == false)
                {
                    mmMessage.Messages.Add("İşlem yapılmak istenen jüri öneri formu EYK'ya gönderildi seçeneği ile kayıt edilmediğinden Asil/Yedek jüri adayı seçimi yapamazsınız!");
                }
                else if (tijBasvuruOneri.EYKDaOnaylandi == true)
                {
                    mmMessage.Messages.Add("İşlem yapılmak istenen jüri öneri formu EYK'da onaylandı seçeneği ile kayıt edildiğinden Asil/Yedek jüri adayı seçimi yapamazsınız!");
                }
                if (mmMessage.Messages.Count == 0 && isAsil.HasValue)
                {

                    var adayCount = tijBasvuruOneri.TijBasvuruOneriJurilers.Count(p => p.IsAsil == true);
                    if (isAsil == true && adayCount >= 3)
                        mmMessage.Messages.Add("Jüri adayı önerisinden toplamda " + 3 + " asil jüri seçilebilir.");  
                }
            } 
            if (mmMessage.Messages.Count == 0)
            {
                juri.IsAsil = isAsil;
                _entities.SaveChanges();
                LogIslemleri.LogEkle("TijBasvuruOneriJuriler", IslemTipi.Update, juri.ToJson());

                mmMessage.IsSuccess = true;
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return new
            {
                mmMessage.IsSuccess,
                Messages = strView
            }.ToJsonResult();
        }

        public ActionResult JuriOneriFormuOnayDurumKayit(Guid tijBasvuruOneriUniqueId, bool eykDaOnayOrEykYaGonderim, bool? onaylandi, string aciklama, DateTime? onayTarihi)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Jüri öneri formu " + (eykDaOnayOrEykYaGonderim ? "EYK'da onay" : "EYK'ya gönderim") + " işlemi",
                MessageType = Msgtype.Warning
            };

            var tijBasvuruOneri = _entities.TijBasvuruOneris.FirstOrDefault(p => p.UniqueID == tijBasvuruOneriUniqueId);

            if (!eykDaOnayOrEykYaGonderim && !RoleNames.MezuniyetGelenBasvurularJuriOneriFormuOnay.InRoleCurrent())
            {
                mmMessage.Messages.Add("Jüri öneri formunda onay yetkisine sahip değilsiniz!");
            }
            else if (eykDaOnayOrEykYaGonderim && !RoleNames.MezuniyetGelenBasvurularJuriOneriFormuEykOnay.InRoleCurrent())
            {
                mmMessage.Messages.Add("Jüri öneri formunda EYK'da onay yetkisine sahip değilsiniz!");
            }
            else if (tijBasvuruOneri == null)
            {
                mmMessage.Messages.Add("İşlem yapılmak istenen jüri öneri formu sistemde bulunamadı!");
            }
            else
            {
                if (eykDaOnayOrEykYaGonderim)
                {
                    if (tijBasvuruOneri.EYKYaGonderildi != true)
                    {
                        mmMessage.Messages.Add("EYK Ya gönderilmeyen jüri öneri formu üzerinde EYK Onayı işlemi yapılamaz!");
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
                else
                {

                    if (tijBasvuruOneri.EYKDaOnaylandi.HasValue)
                    {
                        mmMessage.Messages.Add("EYK onay işlemi yapılan bir form da ön onay işlemi gerçekleştirilemez!");
                    }
                    else if (onaylandi == false && aciklama.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("EYK'ya gönderiminin onaylanmama sebebini giriniz!");
                    }
                }
            }
            if (mmMessage.Messages.Count == 0 && eykDaOnayOrEykYaGonderim && onaylandi == true)
            {
                var asilCount = tijBasvuruOneri.TijBasvuruOneriJurilers.Count(p => p.IsAsil == true);
                if (asilCount != 3)
                    mmMessage.Messages.Add("Jüri öneri formunda EYK'da onaylandı işlemini yapabilmeniz için:<br />* Jüri adayı önerisinden " + 3 + " Asil aday belirlemeniz gerekmektedi.");
            }

            if (mmMessage.Messages.Count == 0 && tijBasvuruOneri != null)
            {
                var isDegisiklikVar = false;
                if (eykDaOnayOrEykYaGonderim)
                {
                    tijBasvuruOneri.TijBasvuru.IsYeniBasvuruYapilabilir = onaylandi.HasValue;
                    isDegisiklikVar = tijBasvuruOneri.EYKDaOnaylandi != onaylandi || aciklama != tijBasvuruOneri.EYKDaOnaylanmadiDurumAciklamasi || tijBasvuruOneri.EYKTarihi != onayTarihi;
                    tijBasvuruOneri.EYKDaOnaylandi = onaylandi;
                    if (onaylandi.HasValue) { tijBasvuruOneri.EYKTarihi = onayTarihi; }
                    tijBasvuruOneri.EYKDaOnaylandiIslemYapanID = UserIdentity.Current.Id;
                    tijBasvuruOneri.EYKDaOnaylandiIslemTarihi = DateTime.Now;
                    tijBasvuruOneri.EYKDaOnaylanmadiDurumAciklamasi = onaylandi == false ? aciklama : "";


                }
                else
                {
                    tijBasvuruOneri.TijBasvuru.IsYeniBasvuruYapilabilir = onaylandi == false;

                    isDegisiklikVar = tijBasvuruOneri.EYKYaGonderildi != onaylandi || aciklama != tijBasvuruOneri.EYKYaGonderimDurumAciklamasi;
                    tijBasvuruOneri.EYKYaGonderimDurumAciklamasi = onaylandi == false ? aciklama : "";
                    tijBasvuruOneri.EYKYaGonderildi = onaylandi;
                    tijBasvuruOneri.EYKYaGonderildiIslemTarihi = DateTime.Now;
                    tijBasvuruOneri.EYKYaGonderildiIslemYapanID = UserIdentity.Current.Id;
                }
                _entities.SaveChanges();
                mmMessage.MessageType = Msgtype.Success;
                mmMessage.IsSuccess = true;
                mmMessage.Messages.Add("Form " + (onaylandi.HasValue ? (onaylandi.Value ? "'Onaylandı'" : "'Onaylanmadı'") : "İşlem bekliyor") + " şeklinde güncellendi...");

                LogIslemleri.LogEkle("TiJuriOnerileriGb", IslemTipi.Update, tijBasvuruOneri.ToJson());

                if (onaylandi.HasValue && isDegisiklikVar)
                {
                    var result = TezIzlemeJuriOneriBus.SendMailEykOnay(tijBasvuruOneriUniqueId, eykDaOnayOrEykYaGonderim,
                            onaylandi.Value);
                }


            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
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
                MessageType = Msgtype.Success,
                IsSuccess = true
            };
            if (!basTar.HasValue)
            {
                mMessage.IsSuccess = false;
                mMessage.Messages.Add("Başlangıç tarihini giriniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasTar" });
            }
            else mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "BasTar" });
            if (!basTar.HasValue)
            {
                mMessage.IsSuccess = false;
                mMessage.Messages.Add("Bitiş tarihini giriniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitTar" });
            }
            else mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "BitTar" });
            if (basTar.HasValue && bitTar.HasValue)
            {
                if (basTar > bitTar)
                {
                    mMessage.IsSuccess = false;
                    mMessage.Messages.Add("Başlangıç tarihi bitiş tarihinden büyük olamaz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasTar" });
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitTar" });
                }
                else
                {
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "BasTar" });
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "BitTar" });
                }
            } 
            if (!mMessage.IsSuccess)
            {

                mMessage.Title = "Tutanak çıktısı oluşturulamadı";
                mMessage.MessageType = Msgtype.Warning;
            } 
            return mMessage.ToJsonResult(); 
        }
        public ActionResult GetTutanakRaporuExport(string basTar, string bitTar, bool exportWordOrExcel, string ekd)
        {

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var baslangicTarihi = basTar.ToDate().Value;
            var bitisTarihi = bitTar.ToDate().Value;

            var qData = (from tijBasvuruOneri in _entities.TijBasvuruOneris.Where(p => !p.IsObsData)
                         join tijBasvuru in _entities.TijBasvurus on tijBasvuruOneri.TijBasvuruID equals tijBasvuru.TijBasvuruID
                         join programlar in _entities.Programlars on tijBasvuru.ProgramKod equals programlar.ProgramKod
                         join anabilimDallari in _entities.AnabilimDallaris on programlar.AnabilimDaliID equals anabilimDallari
                             .AnabilimDaliID
                         join ogrenci in _entities.Kullanicilars on tijBasvuru.KullaniciID equals ogrenci.KullaniciID
                         where tijBasvuru.EnstituKod == enstituKod &&
                               tijBasvuruOneri.EYKDaOnaylandi == true &&
                               tijBasvuruOneri.EYKTarihi >= baslangicTarihi &&
                               tijBasvuruOneri.EYKTarihi <= bitisTarihi
                         select new
                         {
                             tijBasvuruOneri.TijFormTipID,
                             tijBasvuruOneri.EYKTarihi,
                             ogrenci.OgrenciNo,
                             OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                             anabilimDallari.AnabilimDaliAdi,
                             programlar.ProgramAdi,
                             tijBasvuruOneri.IsTezDiliTr,
                             tijBasvuruOneri.TezBaslikTr,
                             tijBasvuruOneri.TezBaslikEn,
                             Juriler = tijBasvuruOneri.TijBasvuruOneriJurilers.ToList()

                         }).OrderBy(o => o.EYKTarihi).ToList();


            var rprTijTutanakModels = new List<RprTijTutanakModel>();
            var rprTijTutanakModel = new RprTijTutanakModel
            {
                TutanakAdi = "Doktora - Tez İzleme Komitesi Atanması Hk.",
                Aciklama = "Doktora yeterlik sınavında başarılı olan öğrenciler için tez izleme komitelerinin aşağıda adı yazılı öğretim üyelerinden oluşturulmasına," +
                " Tez izleme komitesince 'YTÜ Lisansüstü Eğitim-Öğretim Yönetmeliği Senato Esasları' nın ilgili maddesine göre(yeterlik sınav tarihinden başlayarak)" +
                " 6(altı) ay içerisinde tez önerisi savunması yapılarak sonucun bir tutanakla ilgili anabilim dalı başkanlığı aracılığıyla Enstitümüze bildirilmesine oybirliğiyle karar verildi."
            };

            foreach (var itemO in qData.Where(p => p.TijFormTipID == TijFormTipi.YeniForm))
            {
                var uyeler = itemO.Juriler.Where(p => p.IsAsil == true).ToList();
                var danisman = uyeler.First(p => p.IsTezDanismani);
                var asilUye1 = uyeler.Where(p => !p.IsTezDanismani).ToList()[0];
                var asilUye2 = uyeler.Where(p => !p.IsTezDanismani).ToList()[1];

                rprTijTutanakModel.DetayData.Add(new RprTijTutanakRowModel
                {
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
            RprTijTutanak rpr = new RprTijTutanak();
            rpr.DataSource = rprTijTutanakModels.Count > 0 ? rprTijTutanakModels[0] : new RprTijTutanakModel();
            rpr.CreateDocument();


            var rprTijTutanakModels2 = new List<RprTijTutanakModel>();
            var rprTijTutanakModel2 = new RprTijTutanakModel
            {



                TutanakAdi = "Tez izleme Komitesi Değişikliği Hk.",
                Aciklama = "Enstitümüz İlgili Anabilim Dalı Başkanlığından iletilen muhtelif sayılı yazılar okunarak;" +
                            "aşağıda bilgileri verilen doktora öğrencilerine ilişkin tez izleme komitesi değişiklik önerileri görüşüldü. " +
                            "Yapılan görüşmeler sonunda, ilgili doktora öğrencilerine ilişkin tez izleme komitesi değişiklik önerilerinin aşağıda belirtildiği şekilde kabul edilmesine," +
                            "oy birliğiyle karar verildi."
            };

            foreach (var itemO in qData.Where(p => p.TijFormTipID != TijFormTipi.YeniForm))
            {
                var uyeler = itemO.Juriler.Where(p => p.IsAsil == true).ToList();
                var danisman = uyeler.First(p => p.IsTezDanismani);
                var asilUye1 = uyeler.Where(p => !p.IsTezDanismani).ToList()[0];
                var asilUye2 = uyeler.Where(p => !p.IsTezDanismani).ToList()[1];

                rprTijTutanakModel2.DetayData.Add(new RprTijTutanakRowModel
                {
                    OgrenciBilgi = itemO.OgrenciNo + " " + itemO.OgrenciAdSoyad + " (" + itemO.AnabilimDaliAdi + " / " + itemO.ProgramAdi + ")",
                    DanismanAdSoyad = danisman.UnvanAdi + " " + danisman.AdSoyad,
                    DanismanUni = danisman.UniversiteID.HasValue ? danisman.Universiteler.Ad : danisman.UniversiteAdi,
                    AsilUye1 = asilUye1.UnvanAdi + " " + asilUye1.AdSoyad,
                    AsilUye1Uni = asilUye1.UniversiteID.HasValue ? asilUye1.Universiteler.Ad : asilUye1.UniversiteAdi,
                    AsilUye2 = asilUye2.UnvanAdi + " " + asilUye2.AdSoyad,
                    AsilUye2Uni = asilUye2.UniversiteID.HasValue ? asilUye2.Universiteler.Ad : asilUye2.UniversiteAdi,
                    TezKonusu = itemO.IsTezDiliTr ? itemO.TezBaslikTr : itemO.TezBaslikEn
                });

                rprTijTutanakModels2.Add(rprTijTutanakModel2);
            }
            RprTijTutanak rpr2 = new RprTijTutanak();
            rpr2.DataSource = rprTijTutanakModels2.Count > 0 ? rprTijTutanakModels2[0] : new RprTijTutanakModel();
            rpr2.CreateDocument();

            rpr.Pages.AddRange(rpr2.Pages);
            rpr.ExportOptions.Html.ExportMode = HtmlExportMode.SingleFilePageByPage;
            string html = "";
            using (MemoryStream ms = new MemoryStream())
            {
                rpr.ExportToHtml(ms);
                ms.Position = 0;
                var sr = new StreamReader(ms);
                html = sr.ReadToEnd();
            }
            var raporAdi = "Doktra Tez Sınav Jürileri Atama Önerileri";
            return File(System.Text.Encoding.UTF8.GetBytes(html), (exportWordOrExcel ? "application/vnd.ms-word" : "application/ms-excel"), raporAdi + " (" + basTar.Replace("-", ".") + "-" + bitTar.Replace("-", ".") + ")." + (exportWordOrExcel ? "doc" : "xls"));
        }
        public ActionResult SilDetay(Guid tijBasvuruOneriUniqueId)
        {
            var mmMessage = TezIzlemeJuriOneriBus.GetTijBasvuruDetayIslemKontrol(tijBasvuruOneriUniqueId);
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
                    LogIslemleri.LogEkle("TiJuriOnerileriGb", IslemTipi.Delete, kayit.ToJson());
                    if (removedAllData)
                    {
                        LogIslemleri.LogEkle("TiJuriOnerileriGb", IslemTipi.Delete, kayit.ToJson());
                    }
                    mmMessage.Messages.Add(kayit.BasvuruTarihi + " Tarihli tez izleme jüri önerisi silindi.");
                    mmMessage.MessageType = Msgtype.Success;

                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(kayit.BasvuruTarihi + " Tarihli tez izleme jüri önerisi silinemedi.");
                    mmMessage.Title = "Hata";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), "TijBasvuru/SilDetay<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }

            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView, removedAllData }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}