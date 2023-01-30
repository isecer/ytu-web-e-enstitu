using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize]
    public class OnlineOdemeController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();

        public ActionResult Index(string BTID, string EKD, int? KullaniciID, bool IsPopup = false)
        {
            var model = new KmDekontBilgi();

            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            int? BasvuruSurecID = null;
            bool yetkili = RoleNames.BasvuruSureciKayit.InRoleCurrent();
            int kuLID;
            if (KullaniciID.HasValue) kuLID = KullaniciID.Value;
            if (yetkili && BTID.IsNullOrWhiteSpace() == false)
            {
                var gd = new Guid(BTID);
                var tercih = db.BasvurularTercihleris.Where(p => p.UniqueID == gd).FirstOrDefault();
                _EnstituKod = tercih.Basvurular.BasvuruSurec.EnstituKod;
                kuLID = tercih.Basvurular.KullaniciID;
            }
            else kuLID = UserIdentity.Current.Id;
            var kul = db.Kullanicilars.Where(p => p.KullaniciID == kuLID).First();
            var enstitu = db.Enstitulers.Where(p => p.EnstituKod == _EnstituKod).First();
            model.EnstituAdi = enstitu.EnstituAd;
            model.EnstituKod = enstitu.EnstituKod;
            model.AdSoyad = kul.Ad + " " + kul.Soyad;
            model.KullaniciTipID = kul.KullaniciTipID;
            model.TcPasaportNo = kul.KullaniciTipleri.Yerli ? kul.TcKimlikNo : kul.PasaportNo;
            model.KullaniciAktif = true;
            model.KullaniciID = kuLID;
            if (BTID.IsNullOrWhiteSpace() == false)
            {
                var gd = new Guid(BTID);
                var tercih = db.BasvurularTercihleris.Where(p => p.UniqueID == gd).FirstOrDefault();
                if (tercih != null)
                {
                    BasvuruSurecID = tercih.Basvurular.BasvuruSurecID;
                    model.ProgramBilgi = Management.getKontenjanProgramBilgi(tercih.ProgramKod, tercih.OgrenimTipKod, BasvuruSurecID.Value, tercih.Basvurular.KullaniciTipID.Value);


                }
            }
            ViewBag.EnstituKod = _EnstituKod;
            ViewBag.IsPopup = IsPopup;

            ViewBag.BasvuruSurecID = new SelectList(Management.getbasvuruSurecleri(kuLID, _EnstituKod, BasvuruSurecTipi.LisansustuBasvuru, true), "Value", "Caption", BasvuruSurecID);
            ViewBag.UniqueID = new SelectList(Management.getbasvuruSurecleriTercihlerOdeme(BasvuruSurecID, kuLID, true, true), "Value", "Caption", BTID);
            return View(model);
        }

        public ActionResult getProgramlar(int BasvuruSurecID, int? KullaniciID)
        {
            var yetki = RoleNames.BasvuruSureciKayit.InRoleCurrent();
            if (!yetki)
            {
                KullaniciID = UserIdentity.Current.Id;
            }

            var bolm = Management.getbasvuruSurecleriTercihlerOdeme(BasvuruSurecID, KullaniciID.Value, true, true);
            return bolm.Select(s => new { s.Value, s.Caption }).toJsonResult();
        }


        public ActionResult getPRdetay(string UniqueID)
        {


            var mdl = Management.GetOnlineOdemeProgramDetay(UniqueID, false, true, true);
            ViewBag.ExpireYear = new SelectList(Management.GetKrediKartAktifYilList(false), "Value", "Caption", DateTime.Now.Year);
            ViewBag.ExpireMonth = new SelectList(Management.GetAYList(false), "Value", "Caption", 12);
            ViewBag.MaximumTipID = new SelectList(Management.CmbCardMaximumType(), "Value", "Caption");
            ViewBag.Taksit = new SelectList(Management.CmbTaksitList(), "Value", "Caption");

            return View(mdl);
        }

        public ActionResult getOgrenciDetay(int KullaniciID, string EKD)
        {
            var model = new KmDekontBilgi();

            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            int? BasvuruSurecID = null;
            bool yetkili = RoleNames.BasvuruSureciKayit.InRoleCurrent();
            int kuLID = KullaniciID;
            if (!yetkili)
            {
                kuLID = UserIdentity.Current.Id;
            }
            var kul = db.Kullanicilars.Where(p => p.KullaniciID == kuLID).First();
            var enstitu = db.Enstitulers.Where(p => p.EnstituKod == _EnstituKod).First();
            model.EnstituAdi = enstitu.EnstituAd;
            model.EnstituKod = enstitu.EnstituKod;
            model.AdSoyad = kul.Ad + " " + kul.Soyad;
            model.KullaniciTipID = kul.KullaniciTipID;
            model.TcPasaportNo = kul.KullaniciTipleri.Yerli ? kul.TcKimlikNo : kul.PasaportNo;
            model.KullaniciAktif = true;
            model.KullaniciID = kuLID;

            ViewBag.EnstituKod = _EnstituKod;
            ViewBag.IsPopup = false;

            ViewBag.BasvuruSurecID = new SelectList(Management.getbasvuruSurecleri(kuLID, _EnstituKod, BasvuruSurecTipi.LisansustuBasvuru, true), "Value", "Caption", BasvuruSurecID);
            ViewBag.UniqueID = new SelectList(Management.getbasvuruSurecleriTercihlerOdeme(BasvuruSurecID, kuLID, true, true), "Value", "Caption", null);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Post3DPayment(CreditCardModel model, string EKD, string UniqueID = "")
        { 
            var _MmMessage = new MmMessage();
            var ngid = new Guid(UniqueID);
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var kYetki = RoleNames.BasvuruSureciKayit.InRoleCurrent();
            var OdemeBilgi = Management.GetOnlineOdemeProgramDetay(UniqueID, false, true, true);
            object paymentForm = null;


            #region Kontrol
            if (OdemeBilgi == null)
            {
                _MmMessage.Messages.Add("Seçilen Program Sistemde Bulunamadı!");
            }
            else if (!OdemeBilgi.IsOgrenimUcretiOrKatkiPayi.HasValue || !OdemeBilgi.IsOdemeVar)
            {
                _MmMessage.Messages.Add("Seçtiğiniz program için herhangi bir ödeme işlemi gözükmemektedir.");
            }
            else if (!OdemeBilgi.IsOdemeIslemiAcik)
            {
                _MmMessage.Messages.Add("Sadece Ödeme Tarih Aralığında Ödeme İşlemi Yapılabilir.");
            }
            else
            {
                if (model.HolderName.IsNullOrWhiteSpace())
                {
                    _MmMessage.Messages.Add("Kredi Kartı Sahibi Ad Soyad Bilgisi Boş Bırakılamaz");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "HolderName" });
                }
                else
                {
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "HolderName" });
                }
                if (model.CardNumber.IsNullOrWhiteSpace())
                {
                    _MmMessage.Messages.Add("Kredi Kartı Numarası Boş Bırakılamaz");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CardNumber" });
                }
                else if (model.CardNumber.Trim().Length != 16)
                {
                    _MmMessage.Messages.Add("Kredi Kartı Numarası 16 Haneden Oluşması Gerekmektedir.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CardNumber" });
                }
                else if (!model.CardNumber.Trim().IsNumber())
                {
                    _MmMessage.Messages.Add("Kredi Kartı Numarası Sadece Sayıdan Oluşması Gerekmektedir.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CardNumber" });
                }
                 
                if (model.CV2.IsNullOrWhiteSpace())
                {
                    _MmMessage.Messages.Add("Güvenlik Kodu Boş Bırakılamaz");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CV2" });
                }
                else if (model.CV2.Length != 3)
                {
                    _MmMessage.Messages.Add("Güvenlik Kodu 3 Haneden Oluşması Gerekmektedir.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CV2" });
                }
                else if (!model.CV2.IsNumber())
                {
                    _MmMessage.Messages.Add("Güvenlik Kodu Sadece Sayıdan Oluşması Gerekmektedir.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CV2" });
                }
                else
                {
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "CV2" });
                }
                 
            }
            #endregion

            if (_MmMessage.Messages.Count == 0)
            {
                try
                {
                    var hashCode = "";
                    var cardModel = model;
                    var UrlInfo = Request.Url.toUrlInfo();

                    #region paymentCollection
                    string transactionType = "Auth";//İşlem tipi

                    string clientId = "190020074";//Mağaza numarası gerçek
                    string storeKey = "SLUY1914";//Mağaza anahtarı gerçek

                    //clientId = "190300000";//Mağaza numarası test
                    //storeKey = "123456";//Mağaza anahtarı test

                    string storeType = "3d_pay_hosting";//SMS onaylı ödeme modeli 3DPay olarak adlandırılıyor.
                    string paymentResponse = UrlInfo.DefaultUri + "Ajax/PaymentResponse";// GeriBildirim
                    string randomKey = DateTime.Now.ToString(); //ThreeDHelper.CreateRandomValue(10, false, false, true, false);
                    string taksit = "";// model.MaximumTipID == 1 ? model.Taksit.toEmptyStringZero() : "";//Taksit 
                    string currencyCode = "949"; //TL ISO code | EURO "978" | Dolar "840"
                    string languageCode = "tr";// veya "en"
                                               // string cardType = CardTypeID.ToString(); //Kart Ailesi Visa 1 | MasterCard 2 | Amex 3
                    string amount = OdemeBilgi.OdenecekUcret.ToString();//Decimal seperator nokta olmalı!


                    string SiparisNo = Management.DekontNoUret(OdemeBilgi.DonemBaslangicYil, OdemeBilgi.DonemID, OdemeBilgi.OdemeDonemNo, OdemeBilgi.ProgramKod, _EnstituKod);
                    var prgIDX = db.BasvurularTercihleris.Where(p => p.UniqueID == ngid && (p.Basvurular.KullaniciID == (kYetki ? p.Basvurular.KullaniciID : UserIdentity.Current.Id))).FirstOrDefault();

                    prgIDX.BasvurularTercihleriKayitOdemeleris.Add(new BasvurularTercihleriKayitOdemeleri
                    {
                        IsDekontGirisOrSanalPos = false,
                        DonemNo = OdemeBilgi.OdemeDonemNo,
                        DekontNo = SiparisNo,
                        Ucret = OdemeBilgi.Ucret,
                        DekontTarih = null,
                        IsOdendi = false,
                        Aciklama = OdemeBilgi.Aciklama,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip
                    });
                    db.SaveChanges();



                    string oid = SiparisNo;//Sipariş numarası

                    //Güvenlik amaçlı olarak birleştirip şifreliyoruz. Banka decode edip bilgilerin doğruluğunu kontrol ediyor. Alanların sırasına dikkat etmeliyiz.
                    string hashstr = clientId + oid + amount + paymentResponse + paymentResponse + transactionType + taksit + randomKey + paymentResponse + storeKey;

                    var paymentCollection = new NameValueCollection();

                    System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1CryptoServiceProvider();
                    byte[] hashbytes = System.Text.Encoding.GetEncoding("ISO-8859-9").GetBytes(hashstr);
                    byte[] inputbytes = sha.ComputeHash(hashbytes);

                    hashCode = Convert.ToBase64String(inputbytes);
                    //Mağaza bilgileri
                    paymentCollection.Add("hash", hashCode);
                    paymentCollection.Add("clientid", clientId);
                    paymentCollection.Add("storetype", storeType);
                    paymentCollection.Add("rnd", randomKey);
                    paymentCollection.Add("okUrl", paymentResponse);
                    paymentCollection.Add("failUrl", paymentResponse);
                    paymentCollection.Add("callbackurl", paymentResponse);
                    paymentCollection.Add("islemtipi", transactionType);
                    paymentCollection.Add("refreshtime", "0");
                    //Ödeme bilgileri
                    paymentCollection.Add("currency", currencyCode);
                    paymentCollection.Add("lang", languageCode);
                    paymentCollection.Add("amount", amount);
                    paymentCollection.Add("oid", oid);
                    //Kredi kart bilgileri
                    paymentCollection.Add("pan", cardModel.CardNumber);
                    paymentCollection.Add("cardHolderName", cardModel.HolderName);
                    paymentCollection.Add("cv2", cardModel.CV2);
                    var yil = cardModel.ExpireYear.Substring(2, 2);
                    paymentCollection.Add("Ecom_Payment_Card_ExpDate_Year", yil);
                    var month = string.Format("{0:00}", cardModel.ExpireMonth);
                    paymentCollection.Add("Ecom_Payment_Card_ExpDate_Month", month);
                    paymentCollection.Add("taksit", taksit);
                    paymentCollection.Add("cartType", "");
                    paymentCollection.Add("Email", OdemeBilgi.EMail);

                    paymentCollection.Add("Fismi", OdemeBilgi.AdSoyad + " " + OdemeBilgi.TcKimlikNo);
                    if (OdemeBilgi.CepTel.IsNullOrWhiteSpace() == false) paymentCollection.Add("tel", OdemeBilgi.CepTel);
                    OdemeBilgi.ProgramAdi = OdemeBilgi.ProgramAdi.Replace("(", " ").Replace(")", " ");
                    var description = OdemeBilgi.DonemBaslangicYil + "-" + OdemeBilgi.DonemID + " " + OdemeBilgi.ProgramKod + " " + OdemeBilgi.OdemeDonemNo + ".Donem " + (OdemeBilgi.IsOgrenimUcretiOrKatkiPayi == true ? "Ogrenim Ucreti" : "Katki Payi") + " Odemesi";

                    if (description.Length > 128)
                    {
                        var fazlakarakterSayisi = description.Length - 128;
                        var EndLength = (OdemeBilgi.ProgramAdi.Length - fazlakarakterSayisi) - 1;
                        string ProgramAdi = OdemeBilgi.ProgramAdi.Substring(0, EndLength);
                        description = OdemeBilgi.DonemBaslangicYil + "-" + OdemeBilgi.DonemID + " " + OdemeBilgi.ProgramKod + " " + OdemeBilgi.OdemeDonemNo + ".Donem " + (OdemeBilgi.IsOgrenimUcretiOrKatkiPayi == true ? "Ogrenim Ucreti" : "Katki Payi") + " Odemesi";

                    }
                    paymentCollection.Add("description", description);

                    #endregion
                    //Test Kredi Kart Bilgileri
                    //Kart Numarası (Visa) : d
                    //Kart Numarası(Master Card) : 5406675406675403
                    //Son Kullanma Tarihi: 12 / 18
                    //Güvenlik Numarası : 000
                    //Kart 3D Secure Şifresi : a

                    paymentForm = ThreeDHelper.PrepareForm("https://sanalpos2.ziraatbank.com.tr/fim/est3Dgate", paymentCollection); //Orjinal 
                    //paymentForm = ThreeDHelper.PrepareForm("https://entegrasyon.asseco-see.com.tr/fim/est3Dgate", paymentCollection);//test
                    _MmMessage.IsSuccess = true;
                }
                catch 
                {
                    _MmMessage.Title = "Ödeme İşlemi Yarıda Kesildi! Hata!";
                    _MmMessage.Messages.Add("Kredi kartı doğrulama işlemi yapılırken bir hata oluştu!");
                }


            }
            else
            {
                _MmMessage.Title = "Ödeme işlemini yapabilmek için aşağıdaki uyarıları kontrol ediniz.";
            }
            _MmMessage.MessageType = _MmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            return Json(new { MmMessage = _MmMessage, _Content = paymentForm }, "application/json", JsonRequestBehavior.AllowGet);
        }



    }
}