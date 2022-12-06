using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.OgrenimTipleri)]
    public class OgrenimTipleriController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmOgrenimTipleri { });
        }
        [HttpPost]
        public ActionResult Index(FmOgrenimTipleri model)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in db.OgrenimTipleris 
                    join ea in db.Enstitulers on new { s.EnstituKod} equals new { ea.EnstituKod }
                    where EnstKods.Contains(s.EnstituKod)
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
                        BasvurulabilecekDigerOgrenimTipleri = (from ob in db.OgrenimTipleriOrtBasvrs.Where(p => p.EnstituKod == s.EnstituKod &&( p.OgrenimTipKod == s.OgrenimTipKod || p.OgrenimTipKod2 == s.OgrenimTipKod))
                                                               join ott in db.OgrenimTipleris on new { ob.OgrenimTipKod, ob.EnstituKod } equals new { ott.OgrenimTipKod, ott.EnstituKod }
                                                               join otl in db.OgrenimTipleris on ott.OgrenimTipID equals otl.OgrenimTipID
                                                               join ott2 in db.OgrenimTipleris on new { OgrenimTipKod = ob.OgrenimTipKod2, s.EnstituKod } equals new { ott2.OgrenimTipKod, ott2.EnstituKod }
                                                               join otl2 in db.OgrenimTipleris on ott2.OgrenimTipID equals otl2.OgrenimTipID
                                                               select ott.OgrenimTipKod == s.OgrenimTipKod ? otl2.OgrenimTipAdi : otl.OgrenimTipAdi
                                                               
                                                              ).ToList()
                    };
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (model.EnstituKod.IsNullOrWhiteSpace() == false) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.OgrenimTipKod == model.OgrenimTipKod);
            if (!model.OgrenimTipAd.IsNullOrWhiteSpace()) q = q.Where(p => p.OgrenimTipAdi.Contains(model.OgrenimTipAd));
            if (!model.GrupAd.IsNullOrWhiteSpace()) q = q.Where(p => p.GrupAdi.Contains(model.GrupAd) || p.GrupKodu.Contains(model.GrupAd));
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace())  q = q.OrderBy(model.Sort);
            else q = q.OrderBy(o => o.GrupKodu).OrderBy(o => o.OgrenimTipAdi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            var datx = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToArray(); 
             
            model.data = datx;
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            IndexModel.Aktif = q.Where(p => p.IsAktif).Count();
            IndexModel.Pasif = q.Where(p => !p.IsAktif).Count();
            ViewBag.IndexModel = IndexModel;
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id, string dlgid)
        {
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            ViewBag.MmMessage = MmMessage;
            var secilenID = new List<int>();
            var model = new OgrenimTipleri();
            if (id.HasValue)
            {
                model = db.OgrenimTipleris.Where(p => p.OgrenimTipID == id).First();
                if (!UserIdentity.Current.EnstituKods.Contains(model.EnstituKod)) model = new OgrenimTipleri();
                secilenID.AddRange(db.OgrenimTipleriOrtBasvrs.Where(p => p.EnstituKod == model.EnstituKod && p.OgrenimTipKod == model.OgrenimTipKod).Select(s => s.OgrenimTipKod2).ToList());
                secilenID.AddRange(db.OgrenimTipleriOrtBasvrs.Where(p => p.EnstituKod == model.EnstituKod && p.OgrenimTipKod2 == model.OgrenimTipKod).Select(s => s.OgrenimTipKod).ToList());


            }
           
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.GrupGoster = new SelectList(Management.getGrupGoster(), "Value", "Caption", model.GrupGoster); 
            ViewBag.OgrenimTipleri = Management.cmbAktifOgrenimTipleri(model.EnstituKod, false, true, model.OgrenimTipKod);
            ViewBag.YedekOgrenciSayisiKotaCarpani = new SelectList(Management.cmbOTYedekCarpanData(false), "Value", "Caption", model.YedekOgrenciSayisiKotaCarpani);
            ViewBag.secilenID = secilenID;
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(OgrenimTipleri kModel, List<int> secilenID)
        {
            var MmMessage = new MmMessage(); 
            #region Kontrol
      

            if (secilenID == null) secilenID = new List<int>();
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                string msg = "Enstitü seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });

            if (kModel.OgrenimTipKod <= 0)
            {
                string msg = "Kayıt işlemini yapabilmeni için Öğrenim Tipi Kod kısmını doldurmanız gerekmektedir!";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimTipKod" });

            if (kModel.OgrenimTipAdi.IsNullOrWhiteSpace())
            { 
                MmMessage.Messages.Add("Öğrenim Tip Adı Giriniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipAdi" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimTipAdi" });

            if (kModel.GrupGoster)
            {
                if (kModel.GrupKodu.IsNullOrWhiteSpace())
                {
                    MmMessage.Messages.Add("Grup Kodu Giriniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "GrupKodu" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "GrupKodu" });
                if (kModel.GrupAdi.IsNullOrWhiteSpace())
                {
                    MmMessage.Messages.Add("Grup Adı Giriniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "GrupAdi" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "GrupAdi" });
            }
            
            if (kModel.Kota <= 0)
            {
                string msg = "Tercih Sayı Kriteri 0 dan büyük bir değer olmalıdır.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Kota" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Kota" });
            string MulktName = kModel.MulakatSurecineGirecek ? "Mülakat+" : "";
            if (kModel.GBNFormulu.IsNullOrWhiteSpace())
            {
                string msg = "Genel başarı notu hesaplaması için formül giriniz! (Ales+" + MulktName + "Agno)";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "GBNFormulu" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "GBNFormulu" });
            if (kModel.GBNFormuluAlessiz.IsNullOrWhiteSpace())
            {
                string msg = "Genel başarı notu hesaplaması için formül giriniz!  (" + MulktName + "Agno)";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "GBNFormuluAlessiz" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "GBNFormuluAlessiz" });
            if (kModel.MulakatSurecineGirecek && kModel.GBNFormuluMulakatsiz.IsNullOrWhiteSpace())
            {
                string msg = "Genel başarı notu hesaplaması için formül giriniz!  (Ales+Agno))";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "GBNFormuluMulakatsiz" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "GBNFormuluMulakatsiz" });
            if (kModel.GBNFormuluD.IsNullOrWhiteSpace())
            {
                string msg = "Dosya Genel başarı notu hesaplaması için formül giriniz! (Dosya+" + MulktName + "Agno)";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "GBNFormuluD" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "GBNFormuluD" });
            if (kModel.GBNFormuluDDosyasiz.IsNullOrWhiteSpace())
            {
                string msg = "Dosya Genel başarı notu hesaplaması için formül giriniz! (" + MulktName + "Agno)";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "GBNFormuluDDosyasiz" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "GBNFormuluDDosyasiz" });
            if (kModel.MulakatSurecineGirecek && kModel.GBNFormuluDMulakatsiz.IsNullOrWhiteSpace())
            {
                string msg = "Dosya Genel başarı notu hesaplaması için formül giriniz! (Dosya+Agno)";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "GBNFormuluDMulakatsiz" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "GBNFormuluDMulakatsiz" });


            if (!(kModel.BasariNotOrtalamasi > 0 && kModel.BasariNotOrtalamasi <= 100))
            {
                string msg = "Başarı not ortalaması 1 ile 100 arasında bir değer olmalıdır!";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasariNotOrtalamasi" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasariNotOrtalamasi" });

            if (kModel.YokOgrenciKontroluYap)
            {
                if (!kModel.IstenecekKatkiPayiTutari.HasValue)
                {
                    string msg = "Yök Web Servisi ile öğrenci kayıt kontrolü yapılacak ise katkı payı tutarının girilmesi zorunludur.";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IstenecekKatkiPayiTutari" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "IstenecekKatkiPayiTutari" });
            }
            if (kModel.IsMezuniyetBasvurusuYapabilir)
            {
                if (kModel.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari.IsNullOrWhiteSpace())
                {
                    string msg = "Son döneminde kayıt yaptırması gereken ders kodlarını giriniz!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari" });
                if (kModel.MBasvuruToplamKrediKriteri.HasValue == false)
                {
                    string msg = "Toplam Kredi kriteri bilgisini giriniz!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBasvuruToplamKrediKriteri" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBasvuruToplamKrediKriteri" });
                if (kModel.MBasvuruAGNOKriteri.HasValue == false || !(kModel.MBasvuruAGNOKriteri > 0 && kModel.MBasvuruAGNOKriteri <= 4))
                {
                    string msg = "Genel AGNO kriteri 1 ile 4 arasında bir değer olmalıdır!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBasvuruAGNOKriteri" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBasvuruAGNOKriteri" });
                if (kModel.MBasvuruAKTSKriteri.HasValue == false)
                {
                    string msg = "Toplam AKTS kriteri bilgisini giriniz!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBasvuruAKTSKriteri" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBasvuruAKTSKriteri" });

                if (kModel.MBSinavUzatmaSuresiGun.HasValue == false)
                {
                    string msg = "Sınav Uzatma Süresi Gün bilgisi boş bırakılamaz.";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBSinavUzatmaSuresiGun" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBSinavUzatmaSuresiGun" });
                if (kModel.MBTezTeslimSuresiGun.HasValue == false)
                {
                    string msg = "Tez Teslim Süresi Gün bilgisi boş bırakılamaz.";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBTezTeslimSuresiGun" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBTezTeslimSuresiGun" });
                if (kModel.MBSRTalebiKacGunSonraAlabilir.HasValue == false)
                {
                    string msg = "SR Talebi Kaç Gün Sonra Alabilir bilgisi boş bırakılamaz.";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBSRTalebiKacGunSonraAlabilir" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBSRTalebiKacGunSonraAlabilir" });

            }
           
            if (MmMessage.Messages.Count == 0)
            {

                var cnt = db.OgrenimTipleris.Any(p => p.OgrenimTipID != kModel.OgrenimTipID && p.EnstituKod == kModel.EnstituKod && p.OgrenimTipKod == kModel.OgrenimTipKod);
                if (cnt)
                {
                    string msg = "Tanımlamak istediğiniz Öğrenim Tipi Kodu seçilen Enstitü için daha önceden sisteme tanımlanmıştır, tekrar tanımlanamaz!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
                }
            }
            #endregion
            if (MmMessage.Messages.Count == 0)
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
                    var ogrnt = db.OgrenimTipleris.Add(new OgrenimTipleri
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
                    db.SaveChanges();
                }
                else
                {
                    var Kayit = db.OgrenimTipleris.Where(p => p.OgrenimTipID == kModel.OgrenimTipID).First();

                    Kayit.EnstituKod = kModel.EnstituKod;
                    Kayit.OgrenimTipKod = kModel.OgrenimTipKod;
                    Kayit.OgrenimTipAdi = kModel.OgrenimTipAdi;
                    Kayit.GrupGoster = kModel.GrupGoster;
                    Kayit.GrupKodu = kModel.GrupKodu;
                    kModel.GrupAdi = kModel.GrupAdi;
                    Kayit.Kota = kModel.Kota;
                    Kayit.GBNFormulu = kModel.GBNFormulu;
                    Kayit.GBNFormuluAlessiz = kModel.GBNFormuluAlessiz;
                    Kayit.GBNFormuluMulakatsiz = kModel.GBNFormuluMulakatsiz;
                    Kayit.GBNFormuluD = kModel.GBNFormuluD;
                    Kayit.GBNFormuluDDosyasiz = kModel.GBNFormuluDDosyasiz;
                    Kayit.GBNFormuluDMulakatsiz = kModel.GBNFormuluDMulakatsiz;
                    Kayit.LEgitimBilgisiIste = kModel.LEgitimBilgisiIste;
                    Kayit.YLEgitimBilgisiIste = kModel.YLEgitimBilgisiIste;
                    Kayit.MulakatSurecineGirecek = kModel.MulakatSurecineGirecek;
                    Kayit.AlanIciBilimselHazirlik = kModel.AlanIciBilimselHazirlik;
                    Kayit.AlanDisiBilimselHazirlik = kModel.AlanDisiBilimselHazirlik;
                    Kayit.BasariNotOrtalamasi = kModel.BasariNotOrtalamasi;
                    Kayit.YokOgrenciKontroluYap = kModel.YokOgrenciKontroluYap;
                    Kayit.IstenecekKatkiPayiTutari = kModel.IstenecekKatkiPayiTutari;
                    Kayit.YedekOgrenciSayisiKotaCarpani = kModel.YedekOgrenciSayisiKotaCarpani;
                    Kayit.IsMezuniyetBasvurusuYapabilir = kModel.IsMezuniyetBasvurusuYapabilir;
                    Kayit.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = kModel.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari;
                    Kayit.MBasvuruToplamKrediKriteri = kModel.MBasvuruToplamKrediKriteri;
                    Kayit.MBasvuruAGNOKriteri = kModel.MBasvuruAGNOKriteri;
                    Kayit.MBasvuruAKTSKriteri = kModel.MBasvuruAKTSKriteri;
                    Kayit.MBSinavUzatmaSuresiGun = kModel.MBSinavUzatmaSuresiGun;
                    Kayit.MBTezTeslimSuresiGun = kModel.MBTezTeslimSuresiGun;
                    Kayit.MBSRTalebiKacGunSonraAlabilir = kModel.MBSRTalebiKacGunSonraAlabilir;
                    Kayit.IsAktif = kModel.IsAktif;
                    Kayit.IslemYapanID = UserIdentity.Current.Id;
                    Kayit.IslemYapanIP = UserIdentity.Ip;
                    Kayit.IslemTarihi = DateTime.Now;  
                }
                
                var ots = db.OgrenimTipleriOrtBasvrs.Where(p => p.EnstituKod == kModel.EnstituKod && (p.OgrenimTipKod == kModel.OgrenimTipKod || p.OgrenimTipKod2 == kModel.OgrenimTipKod)).ToList();
                if (ots.Count > 0) db.OgrenimTipleriOrtBasvrs.RemoveRange(ots);
                foreach (var item in secilenID)
                {
                    db.OgrenimTipleriOrtBasvrs.Add(new OgrenimTipleriOrtBasvr { EnstituKod = kModel.EnstituKod, OgrenimTipKod = kModel.OgrenimTipKod, OgrenimTipKod2 = item });
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }

          

            ViewBag.MmMessage = MmMessage;
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.GrupGoster = new SelectList(Management.getGrupGoster(), "Value", "Caption", kModel.GrupGoster); 
            ViewBag.OgrenimTipleri = Management.cmbAktifOgrenimTipleri(kModel.EnstituKod, false, true, kModel.OgrenimTipKod);
            ViewBag.YedekOgrenciSayisiKotaCarpani = new SelectList(Management.cmbOTYedekCarpanData(false), "Value", "Caption", kModel.YedekOgrenciSayisiKotaCarpani);
            ViewBag.secilenID = secilenID;
            return View(kModel);
        }
        public ActionResult Sil(int? id)
        {
            var kayit = db.OgrenimTipleris.Where(p => p.OgrenimTipID == id).FirstOrDefault();
            var ot = db.OgrenimTipleris.Where(p => p.OgrenimTipID == kayit.OgrenimTipID).FirstOrDefault();
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + ot.OgrenimTipAdi + "' İsimli Öğrenim tipi Silindi!";
                    db.OgrenimTipleris.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + ot.OgrenimTipAdi + "' İsimli Öğrenim tipi Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "OgrenimTipleri/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
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
