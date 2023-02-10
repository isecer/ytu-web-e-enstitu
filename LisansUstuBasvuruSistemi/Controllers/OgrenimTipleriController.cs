using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.OgrenimTipleri)]
    public class OgrenimTipleriController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmOgrenimTipleri { });
        }
        [HttpPost]
        public ActionResult Index(FmOgrenimTipleri model)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in _entities.OgrenimTipleris 
                    join ea in _entities.Enstitulers on new { s.EnstituKod} equals new { ea.EnstituKod }
                    where enstKods.Contains(s.EnstituKod)
                    select new FrOgrenimTipleri
                    {
                        OgrenimTipID = s.OgrenimTipID,
                        EnstituKod = s.EnstituKod,
                        EnstituAd = ea.EnstituAd,
                        OgrenimTipKod = s.OgrenimTipKod,
                        GrupGoster = s.GrupGoster,
                        GrupKodu = s.GrupKodu,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP,
                        IslemYapan = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                        OgrenimTipAdi = s.OgrenimTipAdi,
                        GrupAdi = s.GrupAdi, 
                        Kota = s.Kota,

                        MulakatSurecineGirecek = s.MulakatSurecineGirecek,
                        GBNFormulu = s.GBNFormulu,
                        GBNFormuluAlessiz = s.GBNFormuluAlessiz,
                        GBNFormuluMulakatsiz = s.GBNFormuluMulakatsiz,
                        GBNFormuluD = s.GBNFormuluD,
                        GBNFormuluDDosyasiz = s.GBNFormuluDDosyasiz,
                        GBNFormuluDMulakatsiz = s.GBNFormuluDMulakatsiz,
                        LEgitimBilgisiIste = s.LEgitimBilgisiIste,
                        YLEgitimBilgisiIste = s.YLEgitimBilgisiIste,
                        AlanIciBilimselHazirlik = s.AlanIciBilimselHazirlik,
                        AlanDisiBilimselHazirlik = s.AlanDisiBilimselHazirlik,
                        BasariNotOrtalamasi = s.BasariNotOrtalamasi,
                        YokOgrenciKontroluYap = s.YokOgrenciKontroluYap,
                        IstenecekKatkiPayiTutari = s.IstenecekKatkiPayiTutari,
                        YedekOgrenciSayisiKotaCarpani = s.YedekOgrenciSayisiKotaCarpani,
                        IsMezuniyetBasvurusuYapabilir = s.IsMezuniyetBasvurusuYapabilir,
                        MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = s.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari,
                        MBasvuruToplamKrediKriteri = s.MBasvuruToplamKrediKriteri,
                        MBasvuruAGNOKriteri = s.MBasvuruAGNOKriteri,
                        MBasvuruAKTSKriteri = s.MBasvuruAKTSKriteri,
                        MBSinavUzatmaSuresiGun = s.MBSinavUzatmaSuresiGun,
                        MBTezTeslimSuresiGun = s.MBTezTeslimSuresiGun,
                        MBSRTalebiKacGunSonraAlabilir = s.MBSRTalebiKacGunSonraAlabilir, 
                        BasvurulabilecekDigerOgrenimTipleri = (from ob in _entities.OgrenimTipleriOrtBasvrs.Where(p => p.EnstituKod == s.EnstituKod &&( p.OgrenimTipKod == s.OgrenimTipKod || p.OgrenimTipKod2 == s.OgrenimTipKod))
                                                               join ott in _entities.OgrenimTipleris on new { ob.OgrenimTipKod, ob.EnstituKod } equals new { ott.OgrenimTipKod, ott.EnstituKod }
                                                               join otl in _entities.OgrenimTipleris on ott.OgrenimTipID equals otl.OgrenimTipID
                                                               join ott2 in _entities.OgrenimTipleris on new { OgrenimTipKod = ob.OgrenimTipKod2, s.EnstituKod } equals new { ott2.OgrenimTipKod, ott2.EnstituKod }
                                                               join otl2 in _entities.OgrenimTipleris on ott2.OgrenimTipID equals otl2.OgrenimTipID
                                                               select ott.OgrenimTipKod == s.OgrenimTipKod ? otl2.OgrenimTipAdi : otl.OgrenimTipAdi
                                                               
                                                              ).ToList()
                    };
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (model.EnstituKod.IsNullOrWhiteSpace() == false) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.OgrenimTipKod == model.OgrenimTipKod);
            if (!model.OgrenimTipAd.IsNullOrWhiteSpace()) q = q.Where(p => p.OgrenimTipAdi.Contains(model.OgrenimTipAd));
            if (!model.GrupAd.IsNullOrWhiteSpace()) q = q.Where(p => p.GrupAdi.Contains(model.GrupAd) || p.GrupKodu.Contains(model.GrupAd));
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.GrupKodu).OrderBy(o => o.OgrenimTipAdi);
            var ps = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = ps.PageIndex;
            var datx = q.Skip(ps.StartRowIndex).Take(model.PageSize).ToArray(); 
             
            model.data = datx;
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount,
                Aktif = q.Count(p => p.IsAktif),
                Pasif = q.Count(p => !p.IsAktif)
            };
            ViewBag.IndexModel = indexModel;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id, string dlgid)
        {
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };
            ViewBag.MmMessage = mmMessage;
            var secilenId = new List<int>();
            var model = new OgrenimTipleri();
            if (id.HasValue)
            {
                model = _entities.OgrenimTipleris.First(p => p.OgrenimTipID == id);
                if (!UserIdentity.Current.EnstituKods.Contains(model.EnstituKod)) model = new OgrenimTipleri();
                secilenId.AddRange(_entities.OgrenimTipleriOrtBasvrs.Where(p => p.EnstituKod == model.EnstituKod && p.OgrenimTipKod == model.OgrenimTipKod).Select(s => s.OgrenimTipKod2).ToList());
                secilenId.AddRange(_entities.OgrenimTipleriOrtBasvrs.Where(p => p.EnstituKod == model.EnstituKod && p.OgrenimTipKod2 == model.OgrenimTipKod).Select(s => s.OgrenimTipKod).ToList());


            }
           
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.GrupGoster = new SelectList(ComboData.GetCmbGrupGosterData(), "Value", "Caption", model.GrupGoster); 
            ViewBag.OgrenimTipleri = OgrenimTipleriBus.CmbAktifOgrenimTipleri(model.EnstituKod, false, true, model.OgrenimTipKod);
            ViewBag.YedekOgrenciSayisiKotaCarpani = new SelectList(Management.cmbOTYedekCarpanData(false), "Value", "Caption", model.YedekOgrenciSayisiKotaCarpani);
            ViewBag.secilenID = secilenId;
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(OgrenimTipleri kModel, List<int> secilenId)
        {
            var mmMessage = new MmMessage(); 
            #region Kontrol
      

            if (secilenId == null) secilenId = new List<int>();
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Enstitü seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });

            if (kModel.OgrenimTipKod <= 0)
            { 
                mmMessage.Messages.Add("Kayıt işlemini yapabilmeni için Öğrenim Tipi Kod kısmını doldurmanız gerekmektedir!");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimTipKod" });

            if (kModel.OgrenimTipAdi.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Öğrenim Tip Adı Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimTipAdi" });

            if (kModel.GrupGoster)
            {
                if (kModel.GrupKodu.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Grup Kodu Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "GrupKodu" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "GrupKodu" });
                if (kModel.GrupAdi.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Grup Adı Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "GrupAdi" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "GrupAdi" });
            }
            
            if (kModel.Kota <= 0)
            { 
                mmMessage.Messages.Add("Tercih Sayı Kriteri 0 dan büyük bir değer olmalıdır.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Kota" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Kota" });
            string mulktName = kModel.MulakatSurecineGirecek ? "Mülakat+" : "";
            if (kModel.GBNFormulu.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Genel başarı notu hesaplaması için formül giriniz! (Ales+" + mulktName + "Agno)");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "GBNFormulu" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "GBNFormulu" });
            if (kModel.GBNFormuluAlessiz.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Genel başarı notu hesaplaması için formül giriniz!  (" + mulktName + "Agno)");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "GBNFormuluAlessiz" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "GBNFormuluAlessiz" });
            if (kModel.MulakatSurecineGirecek && kModel.GBNFormuluMulakatsiz.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Genel başarı notu hesaplaması için formül giriniz!  (Ales+Agno))");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "GBNFormuluMulakatsiz" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "GBNFormuluMulakatsiz" });
            if (kModel.GBNFormuluD.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Dosya Genel başarı notu hesaplaması için formül giriniz! (Dosya+" + mulktName + "Agno)");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "GBNFormuluD" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "GBNFormuluD" });
            if (kModel.GBNFormuluDDosyasiz.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Dosya Genel başarı notu hesaplaması için formül giriniz! (" + mulktName + "Agno)");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "GBNFormuluDDosyasiz" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "GBNFormuluDDosyasiz" });
            if (kModel.MulakatSurecineGirecek && kModel.GBNFormuluDMulakatsiz.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Dosya Genel başarı notu hesaplaması için formül giriniz! (Dosya+Agno)");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "GBNFormuluDMulakatsiz" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "GBNFormuluDMulakatsiz" });


            if (!(kModel.BasariNotOrtalamasi > 0 && kModel.BasariNotOrtalamasi <= 100))
            { 
                mmMessage.Messages.Add("Başarı not ortalaması 1 ile 100 arasında bir değer olmalıdır!");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasariNotOrtalamasi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasariNotOrtalamasi" });

            if (kModel.YokOgrenciKontroluYap)
            {
                if (!kModel.IstenecekKatkiPayiTutari.HasValue)
                { 
                    mmMessage.Messages.Add("Yök Web Servisi ile öğrenci kayıt kontrolü yapılacak ise katkı payı tutarının girilmesi zorunludur.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IstenecekKatkiPayiTutari" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "IstenecekKatkiPayiTutari" });
            }
            if (kModel.IsMezuniyetBasvurusuYapabilir)
            {
                if (kModel.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari.IsNullOrWhiteSpace())
                { 
                    mmMessage.Messages.Add("Son döneminde kayıt yaptırması gereken ders kodlarını giriniz!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari" });
                if (kModel.MBasvuruToplamKrediKriteri.HasValue == false)
                { 
                    mmMessage.Messages.Add("Toplam Kredi kriteri bilgisini giriniz!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBasvuruToplamKrediKriteri" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBasvuruToplamKrediKriteri" });
                if (kModel.MBasvuruAGNOKriteri.HasValue == false || !(kModel.MBasvuruAGNOKriteri > 0 && kModel.MBasvuruAGNOKriteri <= 4))
                { 
                    mmMessage.Messages.Add("Genel AGNO kriteri 1 ile 4 arasında bir değer olmalıdır!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBasvuruAGNOKriteri" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBasvuruAGNOKriteri" });
                if (kModel.MBasvuruAKTSKriteri.HasValue == false)
                { 
                    mmMessage.Messages.Add("Toplam AKTS kriteri bilgisini giriniz!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBasvuruAKTSKriteri" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBasvuruAKTSKriteri" });

                if (kModel.MBSinavUzatmaSuresiGun.HasValue == false)
                { 
                    mmMessage.Messages.Add("Sınav Uzatma Süresi Gün bilgisi boş bırakılamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBSinavUzatmaSuresiGun" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBSinavUzatmaSuresiGun" });
                if (kModel.MBTezTeslimSuresiGun.HasValue == false)
                { 
                    mmMessage.Messages.Add("Tez Teslim Süresi Gün bilgisi boş bırakılamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBTezTeslimSuresiGun" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBTezTeslimSuresiGun" });
                if (kModel.MBSRTalebiKacGunSonraAlabilir.HasValue == false)
                { 
                    mmMessage.Messages.Add("SR Talebi Kaç Gün Sonra Alabilir bilgisi boş bırakılamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBSRTalebiKacGunSonraAlabilir" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBSRTalebiKacGunSonraAlabilir" });

            }
           
            if (mmMessage.Messages.Count == 0)
            {

                var cnt = _entities.OgrenimTipleris.Any(p => p.OgrenimTipID != kModel.OgrenimTipID && p.EnstituKod == kModel.EnstituKod && p.OgrenimTipKod == kModel.OgrenimTipKod);
                if (cnt)
                { 
                    mmMessage.Messages.Add("Tanımlamak istediğiniz Öğrenim Tipi Kodu seçilen Enstitü için daha önceden sisteme tanımlanmıştır, tekrar tanımlanamaz!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
                }
            }
            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                if (kModel.IsMezuniyetBasvurusuYapabilir == false)
                {
                    kModel.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = "";
                    kModel.MBasvuruToplamKrediKriteri = null;
                    kModel.MBasvuruAGNOKriteri = null;
                    kModel.MBasvuruAKTSKriteri = null;
                    kModel.MBTezTeslimSuresiGun = null;
                    kModel.MBSinavUzatmaSuresiGun = null;
                    kModel.MBSRTalebiKacGunSonraAlabilir = null;
                }
                if (!kModel.GrupGoster) kModel.GrupKodu = null;
                if (kModel.MulakatSurecineGirecek == false) kModel.GBNFormuluMulakatsiz = null;

                if (kModel.OgrenimTipID <= 0)
                {
                    kModel.IsAktif = true;
                    var ogrnt = _entities.OgrenimTipleris.Add(new OgrenimTipleri
                    {
                        EnstituKod = kModel.EnstituKod,
                        OgrenimTipKod = kModel.OgrenimTipKod,
                        OgrenimTipAdi = kModel.OgrenimTipAdi,
                        GrupGoster = kModel.GrupGoster,
                        GrupKodu = kModel.GrupKodu,
                        GrupAdi= kModel.GrupAdi,
                        Kota = kModel.Kota,
                        GBNFormulu = kModel.GBNFormulu,
                        GBNFormuluAlessiz = kModel.GBNFormuluAlessiz,
                        GBNFormuluMulakatsiz = kModel.GBNFormuluMulakatsiz,
                        GBNFormuluD = kModel.GBNFormuluD,
                        GBNFormuluDDosyasiz = kModel.GBNFormuluDDosyasiz,
                        GBNFormuluDMulakatsiz = kModel.GBNFormuluDMulakatsiz,
                        LEgitimBilgisiIste = kModel.LEgitimBilgisiIste,
                        YLEgitimBilgisiIste = kModel.YLEgitimBilgisiIste,
                        MulakatSurecineGirecek = kModel.MulakatSurecineGirecek,
                        AlanIciBilimselHazirlik = kModel.AlanIciBilimselHazirlik,
                        AlanDisiBilimselHazirlik = kModel.AlanDisiBilimselHazirlik,
                        BasariNotOrtalamasi = kModel.BasariNotOrtalamasi,
                        YokOgrenciKontroluYap = kModel.YokOgrenciKontroluYap,
                        IstenecekKatkiPayiTutari = kModel.IstenecekKatkiPayiTutari,
                        YedekOgrenciSayisiKotaCarpani = kModel.YedekOgrenciSayisiKotaCarpani,
                        IsMezuniyetBasvurusuYapabilir = kModel.IsMezuniyetBasvurusuYapabilir,
                        MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = kModel.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari,
                        MBasvuruToplamKrediKriteri = kModel.MBasvuruToplamKrediKriteri,
                        MBasvuruAGNOKriteri = kModel.MBasvuruAGNOKriteri,
                        MBasvuruAKTSKriteri = kModel.MBasvuruAKTSKriteri,
                        MBSinavUzatmaSuresiGun = kModel.MBSinavUzatmaSuresiGun,
                        MBTezTeslimSuresiGun = kModel.MBTezTeslimSuresiGun,

                        MBSRTalebiKacGunSonraAlabilir = kModel.MBSRTalebiKacGunSonraAlabilir,
                        IsAktif = kModel.IsAktif,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip,
                        IslemTarihi = DateTime.Now
                    });
                    _entities.SaveChanges();
                }
                else
                {
                    var kayit = _entities.OgrenimTipleris.First(p => p.OgrenimTipID == kModel.OgrenimTipID);

                    kayit.EnstituKod = kModel.EnstituKod;
                    kayit.OgrenimTipKod = kModel.OgrenimTipKod;
                    kayit.OgrenimTipAdi = kModel.OgrenimTipAdi;
                    kayit.GrupGoster = kModel.GrupGoster;
                    kayit.GrupKodu = kModel.GrupKodu;
                    kModel.GrupAdi = kModel.GrupAdi;
                    kayit.Kota = kModel.Kota;
                    kayit.GBNFormulu = kModel.GBNFormulu;
                    kayit.GBNFormuluAlessiz = kModel.GBNFormuluAlessiz;
                    kayit.GBNFormuluMulakatsiz = kModel.GBNFormuluMulakatsiz;
                    kayit.GBNFormuluD = kModel.GBNFormuluD;
                    kayit.GBNFormuluDDosyasiz = kModel.GBNFormuluDDosyasiz;
                    kayit.GBNFormuluDMulakatsiz = kModel.GBNFormuluDMulakatsiz;
                    kayit.LEgitimBilgisiIste = kModel.LEgitimBilgisiIste;
                    kayit.YLEgitimBilgisiIste = kModel.YLEgitimBilgisiIste;
                    kayit.MulakatSurecineGirecek = kModel.MulakatSurecineGirecek;
                    kayit.AlanIciBilimselHazirlik = kModel.AlanIciBilimselHazirlik;
                    kayit.AlanDisiBilimselHazirlik = kModel.AlanDisiBilimselHazirlik;
                    kayit.BasariNotOrtalamasi = kModel.BasariNotOrtalamasi;
                    kayit.YokOgrenciKontroluYap = kModel.YokOgrenciKontroluYap;
                    kayit.IstenecekKatkiPayiTutari = kModel.IstenecekKatkiPayiTutari;
                    kayit.YedekOgrenciSayisiKotaCarpani = kModel.YedekOgrenciSayisiKotaCarpani;
                    kayit.IsMezuniyetBasvurusuYapabilir = kModel.IsMezuniyetBasvurusuYapabilir;
                    kayit.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = kModel.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari;
                    kayit.MBasvuruToplamKrediKriteri = kModel.MBasvuruToplamKrediKriteri;
                    kayit.MBasvuruAGNOKriteri = kModel.MBasvuruAGNOKriteri;
                    kayit.MBasvuruAKTSKriteri = kModel.MBasvuruAKTSKriteri;
                    kayit.MBSinavUzatmaSuresiGun = kModel.MBSinavUzatmaSuresiGun;
                    kayit.MBTezTeslimSuresiGun = kModel.MBTezTeslimSuresiGun;
                    kayit.MBSRTalebiKacGunSonraAlabilir = kModel.MBSRTalebiKacGunSonraAlabilir;
                    kayit.IsAktif = kModel.IsAktif;
                    kayit.IslemYapanID = UserIdentity.Current.Id;
                    kayit.IslemYapanIP = UserIdentity.Ip;
                    kayit.IslemTarihi = DateTime.Now;  
                }
                
                var ots = _entities.OgrenimTipleriOrtBasvrs.Where(p => p.EnstituKod == kModel.EnstituKod && (p.OgrenimTipKod == kModel.OgrenimTipKod || p.OgrenimTipKod2 == kModel.OgrenimTipKod)).ToList();
                if (ots.Count > 0) _entities.OgrenimTipleriOrtBasvrs.RemoveRange(ots);
                foreach (var item in secilenId)
                {
                    _entities.OgrenimTipleriOrtBasvrs.Add(new OgrenimTipleriOrtBasvr { EnstituKod = kModel.EnstituKod, OgrenimTipKod = kModel.OgrenimTipKod, OgrenimTipKod2 = item });
                }
                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }

          

            ViewBag.MmMessage = mmMessage;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.GrupGoster = new SelectList(ComboData.GetCmbGrupGosterData(), "Value", "Caption", kModel.GrupGoster); 
            ViewBag.OgrenimTipleri = OgrenimTipleriBus.CmbAktifOgrenimTipleri(kModel.EnstituKod, false, true, kModel.OgrenimTipKod);
            ViewBag.YedekOgrenciSayisiKotaCarpani = new SelectList(Management.cmbOTYedekCarpanData(false), "Value", "Caption", kModel.YedekOgrenciSayisiKotaCarpani);
            ViewBag.secilenID = secilenId;
            return View(kModel);
        }
        public ActionResult Sil(int? id)
        {
            var kayit = _entities.OgrenimTipleris.FirstOrDefault(p => p.OgrenimTipID == id);
            var ogrenimTipleri = _entities.OgrenimTipleris.FirstOrDefault(p => p.OgrenimTipID == kayit.OgrenimTipID);
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + ogrenimTipleri.OgrenimTipAdi + "' İsimli Öğrenim tipi Silindi!";
                    _entities.OgrenimTipleris.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + ogrenimTipleri.OgrenimTipAdi + "' İsimli Öğrenim tipi Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "OgrenimTipleri/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Öğrenim tipi sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}
