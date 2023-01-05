
using BiskaUtil;
using CaptchaMvc.HtmlHelpers;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Models.FilterModel;
using LisansUstuBasvuruSistemi.Models.ObsService;
using LisansUstuBasvuruSistemi.Raporlar;
using LisansUstuBasvuruSistemi.Utilities.Dtos.CmbDtos;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class AjaxController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();


        public ActionResult GetThemeSetting()
        {
            var k = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).Select(s => new
            {
                s.FixedHeader,
                s.FixedSidebar,
                s.ScrollSidebar,
                s.RightSidebar,
                s.CustomNavigation,
                s.ToggledNavigation,
                s.BoxedOrFullWidth,
                s.ThemeName,
                s.BackgroundImage
            }).FirstOrDefault();
            return Json(k, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult SetThemeSetting(string columnName, string value)
        {

            var k = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).FirstOrDefault();
            if (columnName == "st_head_fixed") k.FixedHeader = value.ToBoolean().Value;
            if (columnName == "st_sb_fixed") k.FixedSidebar = value.ToBoolean().Value;
            if (columnName == "st_sb_scroll") k.ScrollSidebar = value.ToBoolean().Value;
            if (columnName == "st_sb_right") k.RightSidebar = value.ToBoolean().Value;
            if (columnName == "st_sb_custom") k.CustomNavigation = value.ToBoolean().Value;
            if (columnName == "st_sb_toggled") k.ToggledNavigation = value.ToBoolean().Value;
            if (columnName == "st_layout_boxed") k.BoxedOrFullWidth = value.ToBoolean().Value;
            if (columnName == "ThemeName") k.ThemeName = value;
            if (columnName == "BackgroundImage") k.BackgroundImage = value;
            db.SaveChanges();
            if (columnName == "st_head_fixed") UserIdentity.Current.Informations["FixedHeader"] = value.ToBoolean().Value;
            if (columnName == "st_sb_fixed") UserIdentity.Current.Informations["FixedSidebar"] = value.ToBoolean().Value;
            if (columnName == "st_sb_scroll") UserIdentity.Current.Informations["ScrollSidebar"] = value.ToBoolean().Value;
            if (columnName == "st_sb_right") UserIdentity.Current.Informations["RightSidebar"] = value.ToBoolean().Value;
            if (columnName == "st_sb_custom") UserIdentity.Current.Informations["CustomNavigation"] = value.ToBoolean().Value;
            if (columnName == "st_sb_toggled") UserIdentity.Current.Informations["ToggledNavigation"] = value.ToBoolean().Value;
            if (columnName == "st_layout_boxed") UserIdentity.Current.Informations["BoxedOrFullWidth"] = value.ToBoolean().Value;
            if (columnName == "ThemeName") UserIdentity.Current.Informations["ThemeName"] = value;
            if (columnName == "BackgroundImage") UserIdentity.Current.Informations["BackgroundImage"] = value;
            return Json("true", "application/json", JsonRequestBehavior.AllowGet);
        }
        [ValidateInput(false)]
        public ActionResult PaymentResponse()
        {
            return View();
        }

        public ActionResult LoginControl(string UserName, string Password, string CaptchaInputText, bool? RememberMe, string ReturnUrl, string dlgId)
        {

            var MmMessage = new AjaxLoginModel();
            MmMessage.ReturnUrl = ReturnUrl;
            MmMessage.UserName = UserName;
            MmMessage.Password = Password;
            RememberMe = RememberMe ?? false;

            Kullanicilar loginUser = null;
            string Hata = null;
            try
            {
                if (UserName.IsNullOrWhiteSpace())
                {
                    Hata = "Kullanıcı Adı Boş Bırakılamaz.";
                }
                else if (Password.IsNullOrWhiteSpace())
                {
                    Hata = "Şifre Giriniz.";
                }
                else if (CaptchaInputText.IsNullOrWhiteSpace())
                {
                    Hata = "Resimdeki Karakterleri Giriniz.";
                }
                else if (!this.IsCaptchaValid(""))
                {
                    Hata = "Resimdeki Karakterleri Hatalı Girdiniz";
                }
                else
                {
                    string msg = "";
                    var user = Management.GetLoginUser(UserName);

                    if (user != null)
                    {
                        if (user.IsActiveDirectoryUser == false)
                        {
                            loginUser = Management.Login(UserName, Password);
                        }
                        else
                        {
                            LdapService.SecureSoapClient ld = new LdapService.SecureSoapClient();

                            var WsPwd = ConfigurationManager.AppSettings["ldapServicePassword"];
                            var IsSucces = ld.Login(UserName, Password, WsPwd);
                            if (IsSucces)
                            {
                                loginUser = user;

                            }
                            else
                            {
                                MmMessage.IsSuccess = false;
                                msg = "Active Directory Kontrolünden Geçilemedi!";
                                Management.SistemBilgisiKaydet("Active Directory Kontrolünden Geçilemedi! Kullanıcı Adı: " + UserName, "Acconunt/Login", BilgiTipi.LoginHatalari, null, UserIdentity.Ip);
                            }
                        }
                        if (loginUser != null && !loginUser.IsAktif)
                        {
                            Hata = "Kullanıcı Hesabı Pasif Durumda!";
                            MmMessage.IsSuccess = false;
                        }
                        else if (loginUser == null)
                        {
                            Hata = "Kullanıcı Adı veya Şifre Hatalı. " + msg;
                            MmMessage.IsSuccess = false;
                        }
                        else
                        {
                            MmMessage.IsSuccess = true;
                            //if (loginUser.EnstituKod == EnstituKodlari.FenBilimleri)
                            //{
                            //    MmMessage.ReturnUrl = MmMessage.ReturnUrl.Replace("sbe", "fbe");

                            //}
                            //else
                            //{
                            //    MmMessage.ReturnUrl = MmMessage.ReturnUrl.Replace("fbe", "sbe");
                            //}
                        }
                    }
                    else
                    {
                        MmMessage.IsSuccess = false;
                        //Management.SistemBilgisiKaydet("Kullanıcı Sistemde Bulunamadı! Kullanıcı Adı: " + UserName, "Acconunt/Login", BilgiTipi.LoginHatalari, null, UserIdentity.Ip);
                        Hata = "Kullanıcı sistemde bulunamadı.";
                    }
                }

            }
            catch (Exception ex)
            {
                MmMessage.IsSuccess = false;
                Hata = "Sisteme Giriş Yapılırken Bir Hata Oluştu! Hata: " + ex.ToExceptionMessage();
            }
            MmMessage.Message = Hata;
            if (MmMessage.IsSuccess == false)
            {
                var newCaptcha = Management.RenderPartialView("Ajax", "GetCaptcha", new UrlInfoModel());
                MmMessage.NewSrc = newCaptcha;

            }
            else
            {
                FormsAuthenticationUtil.SetAuthCookie(loginUser.KullaniciAdi, "", RememberMe.Value);
                Management.SetLastLogon();
            }
            return MmMessage.toJsonResult();
        }
        public ActionResult SignOut(string ReturnUrl)
        {
            var MmMessage = new AjaxLoginModel();

            if (UserIdentity.Current.IsAuthenticated)
            {
                var kulID = UserIdentity.Current.Id;
                var kul = db.Kullanicilars.Where(p => p.KullaniciID == kulID).First();
                kul.LastLogonDate = DateTime.Now;
                db.SaveChanges();
                FormsAuthenticationUtil.SignOut();
            }

            if (ReturnUrl.IsNullOrWhiteSpace()) MmMessage.ReturnUrl = Url.Action("Index", "Home");
            else MmMessage.ReturnUrl = ReturnUrl;
            MmMessage.IsSuccess = true;
            return MmMessage.toJsonResult();
        }
        [Authorize]
        public ActionResult getImageUpload(int KullaniciID)
        {
            if (RoleNames.KullanicilarKayit.InRoleCurrent() == false) KullaniciID = UserIdentity.Current.Id;
            var kullanici = Management.GetUser(KullaniciID);
            return View(kullanici);
        }
        [Authorize]
        public ActionResult getImageUploadPost(int KullaniciID, HttpPostedFileBase KProfilResmi)
        {
            var mMessage = new MmMessage();
            string YeniResim = "";
            mMessage.Title = "Profil resmi yükleme işlemi başarısız";
            mMessage.IsSuccess = false;
            mMessage.MessageType = Msgtype.Warning;
            bool AnaResmiDegistir = false;
            if (KProfilResmi == null || KProfilResmi.ContentLength <= 0)
            {
                mMessage.Messages.Add("Profil Resmi Yükleyiniz");
            }
            else if (RoleNames.KullanicilarKayit.InRoleCurrent() == false && KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Başka bir kullanıcı adına resim yüklemesi yapmaya yetkili değilsiniz.");
            }
            else
            {
                var contentlength = KProfilResmi.ContentLength;
                string uzanti = KProfilResmi.FileName.GetFileExtension();
                if ((uzanti == ".jpg" || uzanti == ".JPG" || uzanti == ".jpeg" || uzanti == ".JPEG" || uzanti == ".png" || uzanti == ".PNG" || uzanti == ".bmp" || uzanti == ".BMP") == false)

                {
                    mMessage.Messages.Add("Resim türü '.jpg, .JPG, .jpeg, .JPEG, .png, .PNG, .bmp, .BMP' türlerinden biri olmalıdır!");
                }
                else if (contentlength > 2048000)
                {
                    mMessage.Messages.Add("Ekleyeceğiniz resim maksimum 2MB boyutunda olmalıdır!");
                }
                else
                {
                    var kul = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).First();
                    var eskiResim = kul.ResimAdi;
                    kul.ResimAdi = YeniResim = Management.ResimKaydet(KProfilResmi);
                    kul.IslemYapanID = UserIdentity.Current.Id;
                    kul.IslemYapanIP = UserIdentity.Ip;
                    kul.IslemTarihi = DateTime.Now;
                    db.SaveChanges();
                    mMessage.Title = "Profil Resmi başarılı bir şekilde yüklenmiştir.";
                    mMessage.IsSuccess = true;
                    mMessage.MessageType = Msgtype.Success;
                    if (KullaniciID == UserIdentity.Current.Id)
                    {
                        AnaResmiDegistir = true;
                        var userIdentity = Management.GetUserIdentity(UserIdentity.Current.Name);
                        userIdentity.Impersonate();
                        Session["UserIdentity"] = userIdentity;
                    }
                    if (eskiResim.IsNullOrWhiteSpace() == false)
                    {
                        var rsmYol = SistemAyar.KullaniciResimYolu;
                        var rsm = Server.MapPath("~/" + rsmYol + "/" + eskiResim);
                        if (System.IO.File.Exists(rsm))
                            try
                            {
                                System.IO.File.Delete(rsm);
                            }
                            catch (Exception ex)
                            {
                                Management.SistemBilgisiKaydet(ex, BilgiTipi.Hata);
                            }

                    }
                }
            }
            return new { mMessage = mMessage, ResimAdi = YeniResim.toKullaniciResim(), AnaResmiDegistir = AnaResmiDegistir }.toJsonResult();
        }
        [Authorize]
        public ActionResult YetkiYenile(string ReturnUrl)
        {
            var MmMessage = new MmMessage();

            if (UserIdentity.Current.IsAuthenticated)
            {
                var userIdentity = Management.GetUserIdentity(UserIdentity.Current.Name);
                userIdentity.Impersonate();
                Session["UserIdentity"] = userIdentity;
                MmMessage.Messages.Add("Yetkileriniz yeniden yüklenmiştir.");
            }

            if (ReturnUrl.IsNullOrWhiteSpace()) MmMessage.ReturnUrl = Url.Action("Index", "Home");
            else MmMessage.ReturnUrl = ReturnUrl;
            MmMessage.IsSuccess = true;
            return MmMessage.toJsonResult();
        }
        [HttpGet]
        [Authorize]
        public ActionResult GetKullaniciDetay(int kullaniciID)
        {
            if (!(RoleNames.Kullanicilar.InRoleCurrent() == true
                  || RoleNames.GelenBasvurular.InRoleCurrent()
                  || RoleNames.BasvuruSureci.InRoleCurrent()
                  || RoleNames.MulakatSureci.InRoleCurrent()
                  || RoleNames.SRGelenTalepler.InRoleCurrent()
                  || RoleNames.GelenBelgeTalepleri.InRoleCurrent()))
                kullaniciID = UserIdentity.Current.Id;
            var data = db.Kullanicilars.FirstOrDefault(p => p.KullaniciID == kullaniciID);
            ViewBag.ResimVar = data.ResimAdi.IsNullOrWhiteSpace() == false;
            data.ResimAdi = data.ResimAdi.toKullaniciResim();


            #region Enstituler
            var enstroles = Management.GetEnstituler(true);
            var userEnstRoles = Management.GetKullaniciEnstituler(kullaniciID);
            var kullanici = Management.GetUser(kullaniciID);
            var dataEnst = enstroles.Select(s => new CheckObject<Enstituler>
            {
                Value = s,
                Checked = userEnstRoles.Any(p => p.EnstituKod == s.EnstituKod)
            });
            ViewBag.KEnstituler = dataEnst;
            #endregion
            #region yetkiler
            var roles = Management.GetAllRoles().ToList();
            var userRoles = Management.GetUserRoles(kullaniciID);
            ViewBag.EkRollerCount = userRoles.EklenenRoller.Count;
            ViewBag.Kullanici = kullanici;
            var dataR = roles.Select(s => new CheckObject<Roller>
            {
                Value = s,
                Checked = userRoles.TumRoller.Any(a => a.RolID == s.RolID)
            });
            ViewBag.KRoller = dataR;
            var kategr = roles.Select(s => s.Kategori).Distinct().ToArray();
            var menuK = db.Menulers.Where(a => a.BagliMenuID == 0 && kategr.Contains(a.MenuAdi)).ToList();
            var dct = new List<CmbIntDto>();
            foreach (var item in menuK)
            {
                dct.Add(new CmbIntDto { Value = item.SiraNo.Value, Caption = item.MenuAdi });
            }
            ViewBag.cats = dct;
            #endregion

            #region programYetkileri
            var dataKP = Management.GetKullaniciProgramlari(kullaniciID, null);
            ViewBag.KProgramlar = dataKP.Where(p => p.YetkiVar).ToList();
            #endregion
            if (data.KayitDonemID.HasValue)
            {
                ViewBag.Donem = db.Donemlers.FirstOrDefault(p => p.DonemID == data.KayitDonemID.Value);

            }
            ViewBag.Enstitu = db.Enstitulers.First(p => p.EnstituKod == data.EnstituKod);

            ViewBag.YtuOgrenimB = db.OgrenimTipleris.FirstOrDefault(p => p.EnstituKod == kullanici.EnstituKod && p.OgrenimTipKod == kullanici.OgrenimTipKod);
            if (data.DanismanID.HasValue)
                ViewBag.Danisman = db.Kullanicilars.FirstOrDefault(p => p.KullaniciID == data.DanismanID);
            return View(data);
        }


        public ActionResult GetCaptcha()
        {
            return View();
        }


        [HttpGet]
        [Authorize]
        public ActionResult GetBasvuruDetay(int id, int tbInx)
        {
            var KullaniciID = (RoleNames.GelenBasvurular.InRoleCurrent() || RoleNames.MulakatSureci.InRoleCurrent() || RoleNames.BasvuruSureci.InRoleCurrent()) ? (int?)null : UserIdentity.Current.Id;
            var Basvuru = db.Basvurulars.Where(p => p.BasvuruID == id && p.KullaniciID == (KullaniciID ?? p.KullaniciID)).First();


            var mdl = new BasvuruDetayModel();
            mdl.SelectedTabIndex = tbInx;
            mdl.BasvuruID = Basvuru.BasvuruID;
            mdl.BasvuruTarihi = Basvuru.BasvuruTarihi;
            mdl.KullaniciTipAdi = Basvuru.KullaniciTipleri.KullaniciTipAdi;
            mdl.ResimYolu = Basvuru.ResimAdi;
            mdl.AdSoyad = Basvuru.Ad + " " + Basvuru.Soyad;
            mdl.BasvuruDurumID = Basvuru.BasvuruDurumID;
            mdl.DurumClassName = Basvuru.BasvuruDurumlari.ClassName;
            mdl.DurumColor = Basvuru.BasvuruDurumlari.Color;
            mdl.BasvuruDurumAdi = Basvuru.BasvuruDurumlari.BasvuruDurumAdi;
            mdl.BasvuruDurumAciklamasi = Basvuru.BasvuruDurumAciklamasi;

            mdl.IsBelgeYuklemeVar = Basvuru.BasvuruSurec.IsBelgeYuklemeVar;
            mdl.IsYerli = Basvuru.KullaniciTipleri.Yerli;
            if (false && mdl.IsYerli && (RoleNames.GelenBasvurular.InRoleCurrent() || RoleNames.MulakatSureci.InRoleCurrent()))
            {

                var YokKontrol = Management.yokStudentControl(Basvuru.TcKimlikNo.ToLong().Value);
                Basvuru.YokOgrenimKaydiVar = YokKontrol.KayitVar;
                Basvuru.YokOgrenimKontrolTarihi = DateTime.Now;
                mdl.YokStudentControl = YokKontrol;
                db.SaveChanges();
                mdl.YokOgrenimKaydiVar = Basvuru.YokOgrenimKaydiVar;
                mdl.YokOgrenimKontrolTarihi = Basvuru.YokOgrenimKontrolTarihi;
            }

            mdl.SelectedTabIndex = tbInx;
            var page = Management.RenderPartialView("Ajax", "GetBasvuruDetaySablon", mdl);
            return Json(new { page = page, IsAuthenticated = UserIdentity.Current.IsAuthenticated }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        public ActionResult GetBasvuruDetaySablon(BasvuruDetayModel model)
        {
            return View(model);
        }
        [Authorize]
        public ActionResult GetBasvuruSubData(int id, int tbInx, bool IsSave = false)
        {

            string page = "";
            var KullaniciID = RoleNames.GelenBasvurular.InRoleCurrent() || RoleNames.MulakatSureci.InRoleCurrent() || RoleNames.BasvuruSureci.InRoleCurrent() ? (int?)null : UserIdentity.Current.Id;
            var Basvuru = db.Basvurulars.Where(p => p.BasvuruID == id && p.KullaniciID == (KullaniciID ?? p.KullaniciID)).First();

            var mdl = new BasvuruDetayModel();
            mdl.BasvuruSurecID = Basvuru.BasvuruSurecID;
            mdl.IsSave = IsSave;
            mdl.SelectedTabIndex = tbInx;
            #region UstBilgi

            mdl.SelectedTabIndex = tbInx;
            mdl.BasvuruID = Basvuru.BasvuruID;
            mdl.RowID = Basvuru.RowID;
            mdl.BasvuruTarihi = Basvuru.BasvuruTarihi;
            mdl.KullaniciTipID = Basvuru.KullaniciTipID.Value;
            var KullaniciTipBilgi = Basvuru.KullaniciTipleri;
            mdl.IsYerli = KullaniciTipBilgi.Yerli;
            mdl.KullaniciTipAdi = KullaniciTipBilgi.KullaniciTipAdi;
            mdl.ResimYolu = Basvuru.ResimAdi;
            mdl.AdSoyad = Basvuru.Ad + " " + Basvuru.Soyad;
            mdl.BasvuruDurumID = Basvuru.BasvuruDurumID;
            mdl.DurumClassName = Basvuru.BasvuruDurumlari.ClassName;
            mdl.DurumColor = Basvuru.BasvuruDurumlari.Color;
            mdl.BasvuruDurumAdi = Basvuru.BasvuruDurumlari.BasvuruDurumAdi;
            mdl.BasvuruDurumAciklamasi = Basvuru.BasvuruDurumAciklamasi;

            mdl.IsHesaplandi = Basvuru.MulakatSonuclaris.Any(a => a.MulakatSonucTipID != MulakatSonucTipi.Hesaplanmadı);

            mdl.IsBelgeYuklemeVar = Basvuru.BasvuruSurec.IsBelgeYuklemeVar;
            var MulakatSonucTipIDs = new List<int> { MulakatSonucTipi.Asil, MulakatSonucTipi.Yedek };
            var MulakatSonuclaris = Basvuru.BasvuruSurec.Basvurulars.Where(p => p.KullaniciID == Basvuru.KullaniciID).SelectMany(s => s.MulakatSonuclaris).Where(p => p.MulakatSonucTipID == MulakatSonucTipi.Yedek).ToList();
            if (MulakatSonuclaris.Count <= 1)
            {
                MulakatSonuclaris = Basvuru.MulakatSonuclaris.Where(p => MulakatSonucTipIDs.Contains(p.MulakatSonucTipID)).ToList();
            }
            else mdl.IsYedekCokluTercih = true;
            mdl.IsGonderilenMaillerVar = Basvuru.GonderilenMaillers.Any();
            mdl.IsKayitHakkiVar = MulakatSonuclaris.Any();
            mdl.KayitIslemiGordu = MulakatSonuclaris.Any(a => a.KayitDurumID.HasValue && a.KayitDurumlari.IsKayitOldu == true);

            #endregion

            if (tbInx == 1)
            {
                #region KimlikBilgisi
                mdl.PasaportNo = Basvuru.PasaportNo;
                mdl.TcKimlikNo = Basvuru.TcKimlikNo;
                mdl.CinsiyetAdi = Basvuru.Cinsiyetler.CinsiyetAdi;
                mdl.AnaAdi = Basvuru.AnaAdi;
                mdl.BabaAdi = Basvuru.BabaAdi;
                mdl.DogumTarihi = Basvuru.DogumTarihi;
                mdl.CiltNo = Basvuru.CiltNo;
                mdl.AileNo = Basvuru.AileNo;
                mdl.SiraNo = Basvuru.SiraNo;

                mdl.UyrukAdi = db.Uyruklars.Where(p => p.UyrukKod == Basvuru.UyrukKod).First().Ad;
                var IlIlceKods = new List<int?> { Basvuru.DogumYeriKod, Basvuru.NufusilIlceKod, Basvuru.SehirKod };
                var Sehirler = db.Sehirlers.Where(p => IlIlceKods.Contains(p.SehirKod)).ToList();
                mdl.DogumYeriAdi = Sehirler.Where(p => p.SehirKod == Basvuru.DogumYeriKod).First().Ad;
                mdl.YasadigiSehirAdi = Sehirler.Where(p => p.SehirKod == Basvuru.SehirKod).First().Ad;
                if (mdl.IsYerli) mdl.NufusIlIlceAdi = Sehirler.Where(p => p.SehirKod == Basvuru.NufusilIlceKod).First().Ad;

                mdl.CepTel = Basvuru.CepTel;
                mdl.EMail = Basvuru.EMail;
                mdl.Adres = Basvuru.Adres;
                mdl.Adres2 = Basvuru.Adres2;
                #endregion
                page = Management.RenderPartialView("Ajax", "GetBasvuruKimlikBilgisi", mdl);
            }
            else if (tbInx == 2)
            {
                #region TercihBilgileri
                mdl.LUniversiteAdi = db.Universitelers.Where(p => p.UniversiteID == Basvuru.LUniversiteID).FirstOrDefault().Ad;
                mdl.LFakulteAdi = Basvuru.LFakulteAdi;
                mdl.LBolumAdi = db.OgrenciBolumleris.Where(p => p.OgrenciBolumID == Basvuru.LOgrenciBolumID).First().BolumAdi;
                mdl.LNotSistemi = db.NotSistemleris.Where(p => p.NotSistemID == Basvuru.LNotSistemID).FirstOrDefault().NotSistemAdi;
                mdl.LMezuniyetNotu = Basvuru.LMezuniyetNotu;
                mdl.LMezuniyetNotu100LukSistem = Basvuru.LMezuniyetNotu100LukSistem;
                mdl.LEgitimDiliTurkce = Basvuru.LEgitimDiliTurkce;
                mdl.LegitimDilAdi = Basvuru.LEgitimDiliTurkce.HasValue ? (Basvuru.LEgitimDiliTurkce.Value ? "Türkçe" : "İngilizce") : "";

                if (Basvuru.YLUniversiteID.HasValue)
                {
                    mdl.YLUniversiteID = Basvuru.YLUniversiteID;
                    mdl.YLUniversiteAdi = db.Universitelers.Where(p => p.UniversiteID == Basvuru.YLUniversiteID).FirstOrDefault().Ad;
                    mdl.YLFakulteAdi = Basvuru.YLFakulteAdi;
                    mdl.YLBolumAdi = db.OgrenciBolumleris.Where(p => p.OgrenciBolumID == Basvuru.YLOgrenciBolumID).FirstOrDefault().BolumAdi;
                    mdl.YLNotSistemi = db.NotSistemleris.Where(p => p.NotSistemID == Basvuru.YLNotSistemID).FirstOrDefault().NotSistemAdi;
                    mdl.YLMezuniyetNotu = Basvuru.YLMezuniyetNotu;
                    mdl.YLMezuniyetNotu100LukSistem = Basvuru.YLMezuniyetNotu100LukSistem;
                    mdl.YLEgitimDiliTurkce = Basvuru.YLEgitimDiliTurkce;
                    mdl.YLegitimDilAdi = Basvuru.YLEgitimDiliTurkce.HasValue ? (Basvuru.YLEgitimDiliTurkce.Value ? "Türkçe" : "İngilizce") : "";
                }
                if (Basvuru.DRUniversiteID.HasValue)
                {
                    mdl.DRUniversiteID = Basvuru.DRUniversiteID;
                    mdl.DRUniversiteAdi = db.Universitelers.Where(p => p.UniversiteID == Basvuru.DRUniversiteID).FirstOrDefault().Ad;
                    mdl.DRFakulteAdi = Basvuru.DRFakulteAdi;
                    mdl.DRBolumAdi = db.OgrenciBolumleris.Where(p => p.OgrenciBolumID == Basvuru.DROgrenciBolumID).FirstOrDefault().BolumAdi;
                    mdl.DRNotSistemi = db.NotSistemleris.Where(p => p.NotSistemID == Basvuru.DRNotSistemID).FirstOrDefault().NotSistemAdi;
                    mdl.DRMezuniyetNotu = Basvuru.DRMezuniyetNotu;
                    mdl.DRMezuniyetNotu100LukSistem = Basvuru.DRMezuniyetNotu100LukSistem;
                    mdl.DREgitimDiliTurkce = Basvuru.DREgitimDiliTurkce;
                    mdl.DRegitimDilAdi = Basvuru.DREgitimDiliTurkce.HasValue ? (Basvuru.DREgitimDiliTurkce.Value ? "Türkçe" : "İngilizce") : "";
                }
                mdl.Tercihlers = (from s in Basvuru.BasvurularTercihleris
                                  join at in db.AlanTipleris on s.AlanTipID equals at.AlanTipID
                                  join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == Basvuru.BasvuruSurec.EnstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod
                                  join pr in db.Programlars on s.ProgramKod equals pr.ProgramKod
                                  join als in db.AlesTipleris on pr.AlesTipID equals als.AlesTipID
                                  join abd in db.AnabilimDallaris on pr.AnabilimDaliKod equals abd.AnabilimDaliKod
                                  select new FrTercihler
                                  {
                                      BasvuruTercihID = s.BasvuruTercihID,
                                      BasvuruID = s.BasvuruID,
                                      UniqueID = s.UniqueID,
                                      SiraNo = s.SiraNo,
                                      Ingilizce = s.Programlar.Ingilizce,
                                      AlanTipID = at.AlanTipID,
                                      AlanTipAdi = at.AlanTipAdi,
                                      AlesTipAdi = als.AlesTipAdi,
                                      OgrenimTipKod = s.OgrenimTipKod,
                                      OgrenimTipAdi = ot.OgrenimTipAdi,
                                      ProgramKod = s.ProgramKod,
                                      IsSecilenTercih = s.IsSecilenTercih,
                                      AnabilimDaliAdi = abd.AnabilimDaliAdi,
                                      ProgramAdi = pr.ProgramAdi,
                                  }).ToList();
                #endregion
                page = Management.RenderPartialView("Ajax", "GetBasvuruOgrenimTercihBilgisi", mdl);
            }
            else if (tbInx == 3)
            {
                #region SınavBilgileri
                var BasvuruSurec = Basvuru.BasvuruSurec;
                var Sinavlars = Basvuru.BasvurularSinavBilgis.ToList();
                var SinavTipIDs = Sinavlars.Select(s => s.SinavTipID).ToList();
                var SinavDilleris = Sinavlars.Where(p => p.SinavDilleri != null).Select(s => s.SinavDilleri).ToList();

                var BSurecSinavBilgis = Basvuru.BasvuruSurec.BasvuruSurecSinavTipleris.Where(p => SinavTipIDs.Contains(p.SinavTipID)).ToList();
                var SinavTipleris = BSurecSinavBilgis.Select(s => s.SinavTipleri).ToList();
                var SinavTipGroups = Sinavlars.Select(s => s.SinavTipGruplari).ToList();
                var SinavTipleriLngs = SinavTipleris.ToList();
                mdl.LEgitimDiliTurkce = Basvuru.LEgitimDiliTurkce;
                mdl.YLEgitimDiliTurkce = Basvuru.YLEgitimDiliTurkce;
                mdl.IsTurkceProgramVar = Basvuru.BasvurularTercihleris.Any(a => !a.Programlar.Ingilizce);
                mdl.Sinavlars = (from s in Sinavlars
                                 join bs in BSurecSinavBilgis on s.SinavTipID equals bs.SinavTipID
                                 join st in SinavTipleris on s.SinavTipID equals st.SinavTipID
                                 join stl in SinavTipleriLngs on st.SinavTipID equals stl.SinavTipID
                                 join stg in SinavTipGroups on s.SinavTipGrupID equals stg.SinavTipGrupID
                                 select new FrSinavlar
                                 {
                                     EnstituKod = BasvuruSurec.EnstituKod,
                                     IsWebService = bs.WebService,
                                     TarihGirisMaxGecmisYil = bs.TarihGirisMaxGecmisYil,
                                     SinavTipKod = st.SinavTipKod,
                                     SinavTipID = s.SinavTipID,
                                     SinavTipGrupID = s.SinavTipGrupID,
                                     GrupAdi = stg.SinavTipGrupAdi,
                                     GIsTaahhutVar = bs.GIsTaahhutVar,
                                     IsTaahhutVar = s.IsTaahhutVar ?? false,
                                     SinavAdi = stl.SinavAdi,
                                     Yil = s.WsSinavYil,
                                     DonemAdi = "",// bs.WebService ? SinavTipleriDonems.Where(p => p.WsDonemKod == s.WsSinavDonem).FirstOrDefault().WsDonemAd : "",
                                     SinavTarihi = bs.WebService ? s.WsAciklanmaTarihi : s.SinavTarihi,
                                     SinavSubPuani = s.BasvuruSurecSubNot,
                                     SinavPuani = s.SinavNotu,
                                     SinavDilID = s.SinavDilID,
                                     SinavDilAdi = s.SinavDilID.HasValue ? SinavDilleris.Where(p => p.SinavDilID == s.SinavDilID).FirstOrDefault().DilAdi : (bs.WebService && s.SinavTipGrupID == SinavTipGrup.DilSinavlari ? s.WsSinavDili : ""),
                                     AlesXmlModel = s.SinavTipGrupID == SinavTipGrup.Ales_Gree && bs.WebService ? s.WsXmlData.toSinavSonucAlesXmlModel() : null,
                                 }).OrderBy(o => o.SinavTipGrupID).ToList();

                #endregion
                page = Management.RenderPartialView("Ajax", "GetBasvuruSinavBilgileri", mdl);
            }
            else if (tbInx == 4)
            {
                #region Belgeler
                if (mdl.IsBelgeYuklemeVar)
                {

                    var RolAdlari = new List<string>();
                    RolAdlari.Add(RoleNames.GelenBasvurular);
                    RolAdlari.Add(RoleNames.BasvuruSureci);
                    RolAdlari.Add(RoleNames.MulakatSureci);
                    RolAdlari.Add(RoleNames.BasvuruSureciOgrenciKayit);


                    if (mdl.IsHesaplandi)
                    {
                        var TumBilgileriGorsun = UserIdentity.Current.Roles.Any(a => RolAdlari.Contains(a));
                        var BasvuruSurec = Basvuru.BasvuruSurec;

                        if (mdl.IsKayitHakkiVar)
                        {
                            var BelgeKTModel = new List<EntBegeKayitT>();
                            foreach (var item in BasvuruSurec.BasvuruSurecOgrenimTipleris.Where(p => p.BelgeYuklemeAsilBasTar.HasValue))
                            {
                                BelgeKTModel.Add(new EntBegeKayitT { EnstituKod = BasvuruSurec.EnstituKod, OgrenimTipKod = item.OgrenimTipKod, BaslangicTar = item.BelgeYuklemeAsilBasTar.Value, BitisTar = item.BelgeYuklemeAsilBitTar.Value, MulakatSonucTipID = MulakatSonucTipi.Asil });
                                BelgeKTModel.Add(new EntBegeKayitT { EnstituKod = BasvuruSurec.EnstituKod, OgrenimTipKod = item.OgrenimTipKod, BaslangicTar = item.BelgeYuklemeYedekBasTar.Value, BitisTar = item.BelgeYuklemeYedekBitTar.Value, MulakatSonucTipID = MulakatSonucTipi.Yedek });
                            }

                            var BasvurularTercihleris = MulakatSonuclaris.Select(s => s.BasvurularTercihleri).ToList();
                            var OgrenimTipKods = BasvurularTercihleris.Select(s => s.OgrenimTipKod).ToList();
                            var OgrenimTipleris = db.OgrenimTipleris.Where(p => p.EnstituKod == BasvuruSurec.EnstituKod && OgrenimTipKods.Contains(p.OgrenimTipKod)).ToList();
                            var Programlars = BasvurularTercihleris.Select(s => s.Programlar).ToList();

                            var NowDate = DateTime.Now;
                            mdl.Tercihlers = (from s in BasvurularTercihleris
                                              join ms in MulakatSonuclaris on s.BasvuruTercihID equals ms.BasvuruTercihID
                                              join ot in OgrenimTipleris on s.OgrenimTipKod equals ot.OgrenimTipKod
                                              join prl in Programlars on s.ProgramKod equals prl.ProgramKod
                                              select new FrTercihler
                                              {
                                                  IsSeciliBasvuruyaAitTercih = s.BasvuruID == Basvuru.BasvuruID,
                                                  MulakatSonucID = ms.MulakatSonucID,
                                                  MulakaSonucTipID = ms.MulakatSonucTipID,
                                                  BasvuruTercihID = s.BasvuruTercihID,
                                                  BasvuruID = s.BasvuruID,
                                                  UniqueID = s.UniqueID,
                                                  SiraNo = s.SiraNo,
                                                  Ingilizce = s.Programlar.Ingilizce,
                                                  OgrenimTipKod = s.OgrenimTipKod,
                                                  OgrenimTipAdi = ot.OgrenimTipAdi,
                                                  ProgramKod = s.ProgramKod,
                                                  IsSecilenTercih = s.IsSecilenTercih,
                                                  KayitSiraNo = s.KayitSiraNo,
                                                  ProgramAdi = prl.ProgramAdi,
                                                  KayitDurumID = ms.KayitDurumID,
                                                  KayıtOldu = ms.KayitDurumID.HasValue ? ms.KayitDurumlari.IsKayitOldu : (bool?)null,
                                                  IsBelgeYuklemeAktif = TumBilgileriGorsun || BelgeKTModel.Any(a2 => s.OgrenimTipKod == a2.OgrenimTipKod && ms.MulakatSonucTipID == a2.MulakatSonucTipID && a2.BaslangicTar <= NowDate && a2.BitisTar >= NowDate)

                                              }).ToList();

                            //foreach (var item in mdl.Tercihlers)
                            //{
                            //    if (item.KayıtOldu == true)
                            //    {
                            //        item.KayitSiraNo = item.IsSecilenTercih == true ? 1 : (int?)null;
                            //    }
                            //}
                            mdl.Tercihlers = mdl.Tercihlers.OrderBy(o => o.KayitSiraNo ?? 500).ToList();
                            mdl.IsTurkceProgramVar = mdl.Tercihlers.Any(a => !a.Ingilizce);

                            mdl.IsBelgeYuklemeAktif = mdl.Tercihlers.Any(a => a.IsBelgeYuklemeAktif);
                            mdl.IsSecilenTercihVarAsil = mdl.Tercihlers.Any(a => a.MulakaSonucTipID == MulakatSonucTipi.Asil && a.IsSecilenTercih == true);
                            mdl.IsSecilenTercihVarYedek = mdl.Tercihlers.Any(a => a.MulakaSonucTipID == MulakatSonucTipi.Yedek && a.IsSecilenTercih == true);

                            var BasvuruSurecBelgeTipleris = BasvuruSurec.BasvuruSurecBelgeTipleris.ToList();

                            foreach (var item in mdl.Tercihlers.Select(s => s.BasvuruID).Distinct())
                            {
                                #region BelgelerSet
                                var BasvuruB = BasvuruSurec.Basvurulars.Where(p => p.BasvuruID == item).First();
                                var BasvurularYuklenenBelgelers = BasvuruB.BasvurularYuklenenBelgelers.Select(s => new { s.BasvurularYuklenenBelgeID, s.BasvuruBelgeTipID, s.SinavTipID, s.BelgeAdi, s.BelgeYolu, s.IsOnaylandi, s.OnaylamaTarihi, s.IslemTarihi }).ToList();
                                var BsKimlikBelgesi = BasvuruSurecBelgeTipleris.Where(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.KimlikBelgesi).FirstOrDefault();
                                if (BsKimlikBelgesi != null)
                                {
                                    var kb = new BasvuruBelgeModel
                                    {
                                        SiraNo = 1,
                                        BasvuruID = BasvuruB.BasvuruID,
                                        BasvuruBelgeTipID = BasvuruBelgeTipi.KimlikBelgesi,
                                        BasvuruBelgeTipAdi = BsKimlikBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                    };
                                    var KimlikBelgesi = BasvurularYuklenenBelgelers.Where(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.KimlikBelgesi).FirstOrDefault();
                                    if (KimlikBelgesi != null)
                                    {
                                        kb.BasvurularYuklenenBelgeID = KimlikBelgesi.BasvurularYuklenenBelgeID;
                                        kb.BelgeAdi = KimlikBelgesi.BelgeAdi;
                                        kb.BelgeYolu = KimlikBelgesi.BelgeYolu;
                                        kb.IsOnaylandi = KimlikBelgesi.IsOnaylandi;
                                        kb.OnaylamaTarihi = KimlikBelgesi.OnaylamaTarihi;
                                        kb.IslemTarihi = KimlikBelgesi.IslemTarihi;

                                    }
                                    mdl.Belgelers.Add(kb);

                                }
                                var BsLEgitimBelgesi = BasvuruSurecBelgeTipleris.Where(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.LEgitimBelgesi).FirstOrDefault();
                                if (BsLEgitimBelgesi != null)
                                {
                                    var kb = new BasvuruBelgeModel
                                    {
                                        SiraNo = 2,
                                        BasvuruID = BasvuruB.BasvuruID,
                                        BasvuruBelgeTipID = BasvuruBelgeTipi.LEgitimBelgesi,
                                        BasvuruBelgeTipAdi = BsLEgitimBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                    };
                                    var LEgitimBelgesi = BasvurularYuklenenBelgelers.Where(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.LEgitimBelgesi).FirstOrDefault();
                                    if (LEgitimBelgesi != null)
                                    {
                                        kb.BasvurularYuklenenBelgeID = LEgitimBelgesi.BasvurularYuklenenBelgeID;
                                        kb.BelgeAdi = LEgitimBelgesi.BelgeAdi;
                                        kb.BelgeYolu = LEgitimBelgesi.BelgeYolu;
                                        kb.IsOnaylandi = LEgitimBelgesi.IsOnaylandi;
                                        kb.OnaylamaTarihi = LEgitimBelgesi.OnaylamaTarihi;
                                        kb.IslemTarihi = LEgitimBelgesi.IslemTarihi;

                                    }
                                    mdl.Belgelers.Add(kb);
                                    if (!BasvuruB.YLUniversiteID.HasValue)
                                        kb.Not = "Diplomanız hazır değilse e-devlet doğrulanabilir mezuniyet belgenizi yüklemeniz şartıyla bu kısmı boş bırakabilir ancak yüz yüze eğitim başladıktan sonra diplomanızın aslını ibraz ederek fotokopisini Enstitümüze teslim etmeniz gerekmektedir.";


                                }
                                if (BasvuruB.YLUniversiteID.HasValue)
                                {
                                    var BsYLEgitimBelgesi = BasvuruSurecBelgeTipleris.Where(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.YLEgitimBelgesi).FirstOrDefault();
                                    if (BsYLEgitimBelgesi != null)
                                    {
                                        var kb = new BasvuruBelgeModel
                                        {

                                            SiraNo = 8,
                                            BasvuruID = BasvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipi.YLEgitimBelgesi,
                                            BasvuruBelgeTipAdi = BsYLEgitimBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };
                                        var YLEgitimBelgesi = BasvurularYuklenenBelgelers.Where(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.YLEgitimBelgesi).FirstOrDefault();
                                        if (YLEgitimBelgesi != null)
                                        {
                                            kb.BasvurularYuklenenBelgeID = YLEgitimBelgesi.BasvurularYuklenenBelgeID;
                                            kb.BelgeAdi = YLEgitimBelgesi.BelgeAdi;
                                            kb.BelgeYolu = YLEgitimBelgesi.BelgeYolu;
                                            kb.IsOnaylandi = YLEgitimBelgesi.IsOnaylandi;
                                            kb.OnaylamaTarihi = YLEgitimBelgesi.OnaylamaTarihi;
                                            kb.IslemTarihi = YLEgitimBelgesi.IslemTarihi;

                                        }
                                        mdl.Belgelers.Add(kb);
                                        kb.Not = "Diplomanız hazır değilse e-devlet doğrulanabilir mezuniyet belgenizi yüklemeniz şartıyla bu kısmı boş bırakabilir ancak yüz yüze eğitim başladıktan sonra diplomanızın aslını ibraz ederek fotokopisini Enstitümüze teslim etmeniz gerekmektedir.";

                                    }
                                }
                                //  var IsLagnoOrYlAgnoAlinsin = Basvuru.BasvurularTercihleris.Any(a => a.OgrenimTipKod == OgrenimTipi.Doktra ? (a.Programlar.BasvuruAgnoAlimTipID.HasValue ? a.Programlar.BasvuruAgnoAlimTipID.Value == BasvuruAgnoAlimTipi.LisansAlinsin : false) : true);
                                var IsDrBavurusuVar = BasvuruB.BasvurularTercihleris.Any(a => a.OgrenimTipKod == OgrenimTipi.Doktra);
                                var BsMezuniyetBelgesi = BasvuruSurecBelgeTipleris.Where(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.MezuniyetBelgesi).FirstOrDefault();
                                if (BsMezuniyetBelgesi != null)
                                {
                                    var kb = new BasvuruBelgeModel
                                    {
                                        SiraNo = !IsDrBavurusuVar ? 3 : 9,
                                        BasvuruID = BasvuruB.BasvuruID,
                                        BasvuruBelgeTipID = BasvuruBelgeTipi.MezuniyetBelgesi,
                                        BasvuruBelgeTipAdi = BsMezuniyetBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                    };
                                    var MezuniyetBelgesi = BasvurularYuklenenBelgelers.Where(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.MezuniyetBelgesi).FirstOrDefault();
                                    if (MezuniyetBelgesi != null)
                                    {
                                        kb.BasvurularYuklenenBelgeID = MezuniyetBelgesi.BasvurularYuklenenBelgeID;
                                        kb.BelgeAdi = MezuniyetBelgesi.BelgeAdi;
                                        kb.BelgeYolu = MezuniyetBelgesi.BelgeYolu;
                                        kb.IsOnaylandi = MezuniyetBelgesi.IsOnaylandi;
                                        kb.OnaylamaTarihi = MezuniyetBelgesi.OnaylamaTarihi;
                                        kb.IslemTarihi = MezuniyetBelgesi.IslemTarihi;

                                    }

                                    kb.Not = "Tercih edilen program Agno alım kriterlerine göre " + kb.BasvuruBelgeTipAdi + " yüklenecekdir.";

                                    mdl.Belgelers.Add(kb);

                                }
                                if (IsDrBavurusuVar)
                                {
                                    var BsMezuniyetBelgesiYL = BasvuruSurecBelgeTipleris.Where(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.YLMezuniyetBelgesi).FirstOrDefault();
                                    if (BsMezuniyetBelgesiYL != null)
                                    {
                                        var kb = new BasvuruBelgeModel
                                        {
                                            SiraNo = !IsDrBavurusuVar ? 4 : 10,
                                            BasvuruID = BasvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipi.YLMezuniyetBelgesi,
                                            BasvuruBelgeTipAdi = BsMezuniyetBelgesiYL.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };
                                        var MezuniyetBelgesiYL = BasvurularYuklenenBelgelers.Where(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.YLMezuniyetBelgesi).FirstOrDefault();
                                        if (MezuniyetBelgesiYL != null)
                                        {
                                            kb.BasvurularYuklenenBelgeID = MezuniyetBelgesiYL.BasvurularYuklenenBelgeID;
                                            kb.BelgeAdi = MezuniyetBelgesiYL.BelgeAdi;
                                            kb.BelgeYolu = MezuniyetBelgesiYL.BelgeYolu;
                                            kb.IsOnaylandi = MezuniyetBelgesiYL.IsOnaylandi;
                                            kb.OnaylamaTarihi = MezuniyetBelgesiYL.OnaylamaTarihi;
                                            kb.IslemTarihi = MezuniyetBelgesiYL.IslemTarihi;

                                        }

                                        kb.Not = "Tercih edilen program Agno alım kriterlerine göre " + kb.BasvuruBelgeTipAdi + " yüklenecekdir.";

                                        mdl.Belgelers.Add(kb);

                                    }
                                }

                                var BsTranskriptBelgesi = BasvuruSurecBelgeTipleris.Where(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.TranskriptBelgesi).FirstOrDefault();
                                if (BsTranskriptBelgesi != null)
                                {

                                    var BelgeTipAdi = BsTranskriptBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi;
                                    if (BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru)
                                    {
                                        if (IsDrBavurusuVar) BelgeTipAdi = "YL ve DR Transkript Belgesi";
                                        else BelgeTipAdi = "Lisans ve YL Transkript Belgesi";
                                    }
                                    var kb = new BasvuruBelgeModel
                                    {
                                        SiraNo = !IsDrBavurusuVar ? 5 : 11,
                                        BasvuruID = BasvuruB.BasvuruID,
                                        BasvuruBelgeTipID = BasvuruBelgeTipi.TranskriptBelgesi,
                                        BasvuruBelgeTipAdi = BelgeTipAdi,
                                    };
                                    var TranskriptBelgesi = BasvurularYuklenenBelgelers.Where(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.TranskriptBelgesi).FirstOrDefault();
                                    if (TranskriptBelgesi != null)
                                    {
                                        kb.BasvurularYuklenenBelgeID = TranskriptBelgesi.BasvurularYuklenenBelgeID;
                                        kb.BelgeAdi = TranskriptBelgesi.BelgeAdi;
                                        kb.BelgeYolu = TranskriptBelgesi.BelgeYolu;
                                        kb.IsOnaylandi = TranskriptBelgesi.IsOnaylandi;
                                        kb.OnaylamaTarihi = TranskriptBelgesi.OnaylamaTarihi;
                                        kb.IslemTarihi = TranskriptBelgesi.IslemTarihi;
                                    }
                                    if (BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru)
                                    {
                                        if (IsDrBavurusuVar) BelgeTipAdi = "Yüksek lisans ve Doktora Eğitimi Transkript Belgesi Yüklenecektir. (Tek PDF Halinde)";
                                        else BelgeTipAdi = "Lisans ve Yüksek Lisans Eğitimi Transkript Belgesi Yüklenecektir. (Tek PDF Halinde)";
                                        kb.Not = BelgeTipAdi;
                                    }
                                    else
                                    {
                                        kb.Not = !IsDrBavurusuVar ? "Tercih edilen program Agno alım kriterlerine göre Lisans " + kb.BasvuruBelgeTipAdi + " yüklenecekdir." : "Tercih edilen program Agno alım kriterlerine göre Lisans ve Yüksek Lisans " + kb.BasvuruBelgeTipAdi + "  tek pdf dosyası halinde yüklenecekdir.";
                                    }
                                    mdl.Belgelers.Add(kb);

                                }

                                if (!mdl.IsYerli)
                                {
                                    var BsTaninirlikBelgesi = BasvuruSurecBelgeTipleris.Where(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.TaninirlikBelgesi).FirstOrDefault();
                                    if (BsTaninirlikBelgesi != null)
                                    {
                                        var kb = new BasvuruBelgeModel
                                        {
                                            SiraNo = !IsDrBavurusuVar ? 6 : 12,
                                            BasvuruID = BasvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipi.TaninirlikBelgesi,
                                            BasvuruBelgeTipAdi = BsTaninirlikBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };
                                        var TaninirlikBelgesi = BasvurularYuklenenBelgelers.Where(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.TaninirlikBelgesi).FirstOrDefault();
                                        if (TaninirlikBelgesi != null)
                                        {
                                            kb.BasvurularYuklenenBelgeID = TaninirlikBelgesi.BasvurularYuklenenBelgeID;
                                            kb.BelgeAdi = TaninirlikBelgesi.BelgeAdi;
                                            kb.BelgeYolu = TaninirlikBelgesi.BelgeYolu;
                                            kb.IsOnaylandi = TaninirlikBelgesi.IsOnaylandi;
                                            kb.OnaylamaTarihi = TaninirlikBelgesi.OnaylamaTarihi;
                                            kb.IslemTarihi = TaninirlikBelgesi.IslemTarihi;

                                        }
                                        kb.Not = !IsDrBavurusuVar ? "Lisans eğitimi Yurt dışından mezun olunmuş ise " + kb.BasvuruBelgeTipAdi + " yüklenecekdir." : "Yüksek Lisans eğitimi Yurt dışından mezun olunmuş ise " + kb.BasvuruBelgeTipAdi + " yüklenecekdir.";

                                        mdl.Belgelers.Add(kb);

                                    }
                                }
                                else
                                {
                                    var BsDenklikBelgesi = BasvuruSurecBelgeTipleris.Where(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.DenklikBelgesi).FirstOrDefault();
                                    if (BsDenklikBelgesi != null)
                                    {
                                        var kb = new BasvuruBelgeModel
                                        {
                                            SiraNo = !IsDrBavurusuVar ? 6 : 12,
                                            BasvuruID = BasvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipi.DenklikBelgesi,
                                            BasvuruBelgeTipAdi = BsDenklikBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };
                                        var DenklikBelgesi = BasvurularYuklenenBelgelers.Where(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.DenklikBelgesi).FirstOrDefault();
                                        if (DenklikBelgesi != null)
                                        {
                                            kb.BasvurularYuklenenBelgeID = DenklikBelgesi.BasvurularYuklenenBelgeID;
                                            kb.BelgeAdi = DenklikBelgesi.BelgeAdi;
                                            kb.BelgeYolu = DenklikBelgesi.BelgeYolu;
                                            kb.IsOnaylandi = DenklikBelgesi.IsOnaylandi;
                                            kb.OnaylamaTarihi = DenklikBelgesi.OnaylamaTarihi;
                                            kb.IslemTarihi = DenklikBelgesi.IslemTarihi;

                                        }
                                        kb.Not = !IsDrBavurusuVar ? "Lisans eğitimi Yurt dışından mezun olunmuş ise " + kb.BasvuruBelgeTipAdi + " yüklenecekdir." : "Yüksek Lisans eğitimi Yurt dışından mezun olunmuş ise " + kb.BasvuruBelgeTipAdi + " yüklenecekdir.";

                                        mdl.Belgelers.Add(kb);

                                    }
                                }

                                var SinavTipleris = BasvuruB.BasvurularSinavBilgis.ToList();

                                if (SinavTipleris.Any(a => a.SinavTipGrupID == SinavTipGrup.Ales_Gree))
                                {


                                    var BsAlesGreSinavBelgesi = BasvuruSurecBelgeTipleris.Where(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.AlesGreSinaviBelgesi).FirstOrDefault();
                                    if (BsAlesGreSinavBelgesi != null)
                                    {
                                        var AlesGreSinaviBelgesi = BasvurularYuklenenBelgelers.Where(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.AlesGreSinaviBelgesi).FirstOrDefault();
                                        var kb = new BasvuruBelgeModel
                                        {
                                            SiraNo = 13,
                                            BasvuruID = BasvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipi.AlesGreSinaviBelgesi,
                                            BasvuruBelgeTipAdi = BsAlesGreSinavBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };
                                        if (AlesGreSinaviBelgesi != null)
                                        {
                                            kb.BasvurularYuklenenBelgeID = AlesGreSinaviBelgesi.BasvurularYuklenenBelgeID;
                                            kb.SinavTipID = AlesGreSinaviBelgesi.SinavTipID;
                                            kb.BelgeAdi = AlesGreSinaviBelgesi.BelgeAdi;
                                            kb.BelgeYolu = AlesGreSinaviBelgesi.BelgeYolu;
                                            kb.IsOnaylandi = AlesGreSinaviBelgesi.IsOnaylandi;
                                            kb.OnaylamaTarihi = AlesGreSinaviBelgesi.OnaylamaTarihi;
                                            kb.IslemTarihi = AlesGreSinaviBelgesi.IslemTarihi;

                                        }
                                        var DoktoraMezuniyetiVar = SinavTipleris.Any(a => a.SinavTipKod == 99);
                                        if (DoktoraMezuniyetiVar)
                                        {
                                            kb.Not = "Doktora mezuniyetini gösteren belgenin yüklenmesi gerekmektedir.";
                                        }
                                        mdl.Belgelers.Add(kb);

                                    }
                                }
                                if (SinavTipleris.Any(a => a.SinavTipGrupID == SinavTipGrup.DilSinavlari))
                                {
                                    var BsDilSinaviBelgesi = BasvuruSurecBelgeTipleris.Where(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.DilSinaviBelgesi).FirstOrDefault();
                                    if (BsDilSinaviBelgesi != null)
                                    {
                                        var kb = new BasvuruBelgeModel
                                        {
                                            SiraNo = 14,
                                            BasvuruID = BasvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipi.DilSinaviBelgesi,
                                            BasvuruBelgeTipAdi = BsDilSinaviBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };

                                        var DilSinaviBelgesi = BasvurularYuklenenBelgelers.Where(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.DilSinaviBelgesi).FirstOrDefault();
                                        if (DilSinaviBelgesi != null)
                                        {
                                            kb.BasvurularYuklenenBelgeID = DilSinaviBelgesi.BasvurularYuklenenBelgeID;
                                            kb.SinavTipID = DilSinaviBelgesi.SinavTipID;
                                            kb.BelgeAdi = DilSinaviBelgesi.BelgeAdi;
                                            kb.BelgeYolu = DilSinaviBelgesi.BelgeYolu;
                                            kb.IsOnaylandi = DilSinaviBelgesi.IsOnaylandi;
                                            kb.OnaylamaTarihi = DilSinaviBelgesi.OnaylamaTarihi;
                                            kb.IslemTarihi = DilSinaviBelgesi.IslemTarihi;
                                        }
                                        mdl.Belgelers.Add(kb);

                                    }
                                }
                                if (SinavTipleris.Any(a => a.SinavTipGrupID == SinavTipGrup.Tomer))
                                {
                                    var TomerBilgi = SinavTipleris.Where(a => a.SinavTipGrupID == SinavTipGrup.Tomer).FirstOrDefault();
                                    var BsTomerSinaviBelgesi = BasvuruSurecBelgeTipleris.Where(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.TomerSinaviBelgesi).FirstOrDefault();


                                    bool IsBelgeYukleme = false;

                                    string Not = "";
                                    if (TomerBilgi != null)
                                    {
                                        IsBelgeYukleme = true;
                                        if (!mdl.IsTurkceProgramVar)
                                        {
                                            IsBelgeYukleme = true;
                                        }
                                        else if (BasvuruB.LEgitimDiliTurkce == true || BasvuruB.YLEgitimDiliTurkce == true)
                                        {
                                            var BelgeTuruAdi = "";
                                            if (BasvuruB.LEgitimDiliTurkce == true && Basvuru.YLEgitimDiliTurkce == true) BelgeTuruAdi = "Lisans veya Lisansüstü";
                                            else if (BasvuruB.LEgitimDiliTurkce == true) BelgeTuruAdi = "Lisans";
                                            else if (BasvuruB.YLEgitimDiliTurkce == true) BelgeTuruAdi = "Lisansüstü";
                                            IsBelgeYukleme = true;
                                            Not = "Verilen Taahhüte göre Türkiye'de Türkçe eğitim veren " + (BelgeTuruAdi) + " programlarından mezun olunduğunu gösteren karekodlu E'Devlet mezuniyet belgesininin yüklenmesi gerekmektedir.";

                                        }
                                    }

                                    if (BsTomerSinaviBelgesi != null && IsBelgeYukleme)
                                    {
                                        var TomerSinaviBelgesi = BasvurularYuklenenBelgelers.Where(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.TomerSinaviBelgesi).FirstOrDefault();
                                        var kb = new BasvuruBelgeModel
                                        {
                                            SiraNo = 15,
                                            BasvuruID = BasvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipi.TomerSinaviBelgesi,
                                            BasvuruBelgeTipAdi = BsTomerSinaviBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };

                                        if (Not != null) kb.Not = Not;
                                        if (TomerSinaviBelgesi != null)
                                        {
                                            kb.BasvurularYuklenenBelgeID = TomerSinaviBelgesi.BasvurularYuklenenBelgeID;
                                            kb.SinavTipID = TomerSinaviBelgesi.SinavTipID;
                                            kb.BelgeAdi = TomerSinaviBelgesi.BelgeAdi;
                                            kb.BelgeYolu = TomerSinaviBelgesi.BelgeYolu;
                                            kb.IsOnaylandi = TomerSinaviBelgesi.IsOnaylandi;
                                            kb.OnaylamaTarihi = TomerSinaviBelgesi.OnaylamaTarihi;
                                            kb.IslemTarihi = TomerSinaviBelgesi.IslemTarihi;

                                        }
                                        mdl.Belgelers.Add(kb);
                                    }

                                }

                                #endregion
                            }

                        }

                        mdl.Belgelers = mdl.Belgelers.OrderBy(o => o.SiraNo).ToList();

                    }
                }
                #endregion
                page = Management.RenderPartialView("Ajax", "GetBasvuruBelgeYukleme", mdl);
            }



            var json = Json(new
            {
                page = page,

            }, "application/json", JsonRequestBehavior.AllowGet);
            json.MaxJsonLength = int.MaxValue;
            return json;
        }
        [Authorize]
        public ActionResult GetBasvuruKimlikBilgisi(BasvuruDetayModel model)
        {
            return View(model);
        }
        [Authorize]
        public ActionResult GetBasvuruOgrenimTercihBilgisi(BasvuruDetayModel model)
        {
            return View(model);
        }
        [Authorize]
        public ActionResult GetBasvuruSinavBilgileri(BasvuruDetayModel model)
        {
            return View(model);
        }
        [Authorize]
        public ActionResult GetBasvuruBelgeYukleme(BasvuruDetayModel model)
        {
            return View(model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult BasvuruDosyaEklePost(int BasvuruSurecID, int BasvuruID, int KullaniciID, int BasvuruBelgeTipID, HttpPostedFileBase BelgeDosyasi)
        {
            var mMessage = new MmMessage();
            mMessage.MessageType = Msgtype.Warning;
            var KayitYetki = RoleNames.BasvuruSureciOgrenciKayit.InRoleCurrent();
            var Basv = db.Basvurulars.Where(p => p.BasvuruID == BasvuruID).First();
            var VarolanBelge = Basv.BasvurularYuklenenBelgelers.Where(p => p.BasvuruBelgeTipID == BasvuruBelgeTipID).FirstOrDefault();
            var BelgeTipi = Basv.BasvuruSurec.BasvuruSurecBelgeTipleris.Where(p => p.BasvuruBelgeTipID == BasvuruBelgeTipID).First();
            string BasvuruBelgeTipAdi = BelgeTipi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi;
            var mSonucTip = new List<int> { MulakatSonucTipi.Asil, MulakatSonucTipi.Yedek };

            var DonemdekiBasvurulari = Basv.BasvuruSurec.Basvurulars.Where(a => a.KullaniciID == Basv.KullaniciID).ToList();
            var DonemdekiBasvuruSonuclari = DonemdekiBasvurulari.SelectMany(s => s.MulakatSonuclaris).ToList();
            var BasvuruSurec = Basv.BasvuruSurec;

            var BelgeKTModel = new List<EntBegeKayitT>();
            var IsBelgeYuklemeAktif = false;
            if (KayitYetki == false)
            {
                var NowDate = DateTime.Now;
                foreach (var item in BasvuruSurec.BasvuruSurecOgrenimTipleris.Where(p => p.BelgeYuklemeAsilBasTar.HasValue))
                {
                    BelgeKTModel.Add(new EntBegeKayitT { EnstituKod = BasvuruSurec.EnstituKod, OgrenimTipKod = item.OgrenimTipKod, BaslangicTar = item.BelgeYuklemeAsilBasTar.Value, BitisTar = item.BelgeYuklemeAsilBitTar.Value, MulakatSonucTipID = MulakatSonucTipi.Asil });
                    BelgeKTModel.Add(new EntBegeKayitT { EnstituKod = BasvuruSurec.EnstituKod, OgrenimTipKod = item.OgrenimTipKod, BaslangicTar = item.BelgeYuklemeYedekBasTar.Value, BitisTar = item.BelgeYuklemeYedekBitTar.Value, MulakatSonucTipID = MulakatSonucTipi.Yedek });
                }
                foreach (var itemB in Basv.MulakatSonuclaris.Where(p => mSonucTip.Contains(p.MulakatSonucTipID) && p.BasvurularTercihleri.IsSecilenTercih == true))
                {
                    if (!IsBelgeYuklemeAktif) IsBelgeYuklemeAktif = BelgeKTModel.Any(a2 => a2.EnstituKod == BasvuruSurec.EnstituKod && itemB.BasvurularTercihleri.OgrenimTipKod == a2.OgrenimTipKod && itemB.MulakatSonucTipID == a2.MulakatSonucTipID && a2.BaslangicTar <= NowDate && a2.BitisTar >= NowDate);

                }



            }

            if (!KayitYetki && Basv.KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Bu başvuru üstünde işlem yapmaya yetkili değilsiniz.");
            }
            else if (Basv.MulakatSonuclaris.Any(a => a.KayitDurumID.HasValue && a.KayitDurumlari.IsKayitOldu == true))
            {
                mMessage.Messages.Add("Başvurunuz için kayıt işlemi süreci tamamlandığından başvuruda herhangi işlemi yapılamaz!");
            }
            else if (DonemdekiBasvuruSonuclari.Count(p => mSonucTip.Contains(p.MulakatSonucTipID)) == DonemdekiBasvuruSonuclari.Count(a => a.KayitDurumID.HasValue))
            {
                mMessage.Messages.Add("Başvurunuz için kayıt işlemi süreci tamamlandığından başvuruda herhangi işlemi yapılamaz!");

            }
            else if (BelgeDosyasi != null && BelgeDosyasi.ContentLength > (1024 * 1024 * 5))
            {
                mMessage.Messages.Add("Yükleyeceğiniz dosya boyutu en fazla 5MB olmalıdır.");
            }
            else if (!KayitYetki && !IsBelgeYuklemeAktif)
            {
                mMessage.Messages.Add("Sistem belge yükleme işlemlerine kapalıdır.");
            }
            else
            {
                if (VarolanBelge != null && VarolanBelge.IsOnaylandi)
                {
                    mMessage.Messages.Add(VarolanBelge.BelgeAdi + " isimli " + BasvuruBelgeTipAdi + " dosyası onaylandığından belge kayıt işlemi yapılamaz.");
                }
                else if (BelgeDosyasi == null && VarolanBelge == null)
                {
                    mMessage.Messages.Add(Basv.KullaniciTipleri.Yerli ? (BasvuruBelgeTipAdi + " yüklemek için dosya seçiniz.") : "Pasaport belgesini yüklemek için dosya seçiniz.");
                }
                else if (BelgeDosyasi != null && BelgeDosyasi.FileName.Split('.').Last().ToLower() != "pdf")
                {
                    mMessage.Messages.Add("Yükleyeceğiniz " + BasvuruBelgeTipAdi + " dosyası pdf türünde olmalıdır.");
                }
            }
            if (mMessage.Messages.Count == 0)
            {



                string BelgeAdi = "";
                string VarOlanBelgeAdi = "";
                if (BelgeDosyasi != null)
                {
                    int? SinavTipID = null;
                    int? SinavTipGrupID = null;
                    if (BasvuruBelgeTipID == BasvuruBelgeTipi.AlesGreSinaviBelgesi) SinavTipGrupID = SinavTipGrup.Ales_Gree;
                    else if (BasvuruBelgeTipID == BasvuruBelgeTipi.DilSinaviBelgesi) SinavTipGrupID = SinavTipGrup.DilSinavlari;
                    else if (BasvuruBelgeTipID == BasvuruBelgeTipi.TomerSinaviBelgesi) SinavTipGrupID = SinavTipGrup.Tomer;
                    if (SinavTipGrupID.HasValue)
                    {

                        SinavTipID = Basv.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrupID).First().SinavTipID;
                    }
                    string DosyaYolu = "/BasvuruDosyalari/BasvuruKayitBelgeleri/" + BelgeDosyasi.FileName.ToFileNameAddGuid(null, Basv.BasvuruID.ToString());
                    BelgeDosyasi.SaveAs(Server.MapPath("~" + DosyaYolu));
                    BelgeAdi = BelgeDosyasi.FileName.GetFileName();
                    db.BasvurularYuklenenBelgelers.Add(new BasvurularYuklenenBelgeler
                    {
                        BasvuruID = BasvuruID,
                        BasvuruBelgeTipID = BasvuruBelgeTipID,
                        SinavTipID = SinavTipID,
                        BelgeAdi = BelgeAdi,
                        BelgeYolu = DosyaYolu,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip,
                    });


                }
                if (VarolanBelge != null)
                {
                    VarOlanBelgeAdi = VarolanBelge.BelgeAdi;
                    var path = Server.MapPath("~" + VarolanBelge.BelgeYolu);

                    if (System.IO.File.Exists(path))
                    {
                        try
                        {

                            System.IO.File.Delete(path);
                        }
                        catch
                        {

                        }

                    }
                    if (VarolanBelge != null) db.BasvurularYuklenenBelgelers.Remove(VarolanBelge);
                }
                db.SaveChanges();
                if (BelgeDosyasi == null && !VarOlanBelgeAdi.IsNullOrWhiteSpace())
                {
                    mMessage.Title = "Belge silme işlemi başarılı";

                    mMessage.Messages.Add(BelgeAdi + " isimli " + BasvuruBelgeTipAdi + " dosyası sistemden silindi.");
                }
                else
                {
                    mMessage.Title = "Belge yükleme işlemi başarılı";
                    mMessage.Messages.Add(BelgeAdi + " isimli " + BasvuruBelgeTipAdi + " dosyası sisteme yüklendi.");
                }

                mMessage.IsSuccess = true;
                mMessage.MessageType = Msgtype.Success;

            }
            else mMessage.Title = "Belge yükleme işlemi başarısız";
            var strView = Management.RenderPartialView("Ajax", "getMessage", mMessage);
            return Json(new { IsSuccess = mMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        public ActionResult BasvuruDosyaOnayla(int BasvuruID, int BasvuruBelgeTipID, bool IsOnaylandi)
        {
            var mMessage = new MmMessage();

            mMessage.MessageType = Msgtype.Warning;
            var KayitYetki = RoleNames.BasvuruSureciOgrenciKayit.InRoleCurrent();

            var Basv = db.Basvurulars.Where(p => p.BasvuruID == BasvuruID).First();
            var VarolanBelge = Basv.BasvurularYuklenenBelgelers.Where(p => p.BasvuruBelgeTipID == BasvuruBelgeTipID).First();
            var BelgeTipi = Basv.BasvuruSurec.BasvuruSurecBelgeTipleris.Where(p => p.BasvuruBelgeTipID == BasvuruBelgeTipID).First();
            string BasvuruBelgeTipAdi = BelgeTipi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi;

            if (!KayitYetki)
            {
                mMessage.Messages.Add("Bu işlemi yapmaya yetkili değilsiniz.");
            }
            if (mMessage.Messages.Count == 0)
            {
                VarolanBelge.IsOnaylandi = IsOnaylandi;
                VarolanBelge.OnaylamaTarihi = DateTime.Now;
                VarolanBelge.OnaylamaYapanID = UserIdentity.Current.Id;
                VarolanBelge.OnaylamaYapanIP = UserIdentity.Ip;
                db.SaveChanges();
                mMessage.IsSuccess = true;
                mMessage.MessageType = Msgtype.Success;
                mMessage.Messages.Add(VarolanBelge.BelgeAdi + " isimli " + BasvuruBelgeTipAdi + " dosyası " + (IsOnaylandi ? "onaylandı." : "onayı kaldırıldı"));
            }

            var strView = Management.RenderPartialView("Ajax", "getMessage", mMessage);
            return Json(new { IsSuccess = mMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        public ActionResult BasvuruKayitOlunacakTercihSet(int id, string UniqueID, bool? IsSecildi)
        {

            var mMessage = new MmMessage();
            var Kayityetki = RoleNames.BasvuruSureciOgrenciKayit.InRoleCurrent();

            var Basvuru = db.Basvurulars.Where(p => p.BasvuruID == id && p.KullaniciID == (!Kayityetki ? UserIdentity.Current.Id : p.KullaniciID)).First();
            var BasvuruSurec = Basvuru.BasvuruSurec;
            var IsBelgeYuklemeAktif = true;
            var BelgeKTModel = new List<EntBegeKayitT>();

            var DonemdekiBasvurulari = BasvuruSurec.Basvurulars.Where(a => a.KullaniciID == Basvuru.KullaniciID).ToList();
            var DonemdekiBasvuruSonuclari = DonemdekiBasvurulari.SelectMany(s => s.MulakatSonuclaris).ToList();
            var mSonucTip = new List<int> { MulakatSonucTipi.Asil, MulakatSonucTipi.Yedek };

            if (UniqueID.IsNullOrWhiteSpace())
            {
                if (DonemdekiBasvuruSonuclari.Any(a => a.KayitDurumID.HasValue && a.KayitDurumlari.IsKayitOldu == true))
                {
                    mMessage.Messages.Add("Başvurunuz için kayıt işlemi süreci tamamlandığından başvuruda herhangi işlemi yapılamaz!");
                }
                else if (DonemdekiBasvuruSonuclari.Count(a => mSonucTip.Contains(a.MulakatSonucTipID)) == DonemdekiBasvuruSonuclari.Count(a => a.KayitDurumID.HasValue))
                {
                    mMessage.Messages.Add("Başvurunuz için kayıt işlemi süreci tamamlandığından başvuruda herhangi işlemi yapılamaz!");
                }
                else
                {
                    foreach (var item in DonemdekiBasvurulari.SelectMany(s => s.BasvurularTercihleris))
                    {
                        item.IsSecilenTercih = false;
                        item.IslemTarihi = DateTime.Now;
                        item.IslemYapanID = UserIdentity.Current.Id;
                        item.IslemYapanIP = UserIdentity.Ip;
                    }
                    db.SaveChanges();
                    mMessage.IsSuccess = true;
                    mMessage.Messages.Add("Tercih seçimi kaldırıldı!");
                }
            }
            else
            {
                var tGuid = new Guid(UniqueID);
                var Tercih = Basvuru.BasvurularTercihleris.Where(p => p.UniqueID == tGuid).First();

                var MulakatSonucu = Tercih.MulakatSonuclaris.First();

                if (Kayityetki == false)
                {

                    foreach (var item in BasvuruSurec.BasvuruSurecOgrenimTipleris.Where(p => p.BelgeYuklemeAsilBasTar.HasValue))
                    {
                        BelgeKTModel.Add(new EntBegeKayitT { EnstituKod = BasvuruSurec.EnstituKod, OgrenimTipKod = item.OgrenimTipKod, BaslangicTar = item.BelgeYuklemeAsilBasTar.Value, BitisTar = item.BelgeYuklemeAsilBitTar.Value, MulakatSonucTipID = MulakatSonucTipi.Asil });
                        BelgeKTModel.Add(new EntBegeKayitT { EnstituKod = BasvuruSurec.EnstituKod, OgrenimTipKod = item.OgrenimTipKod, BaslangicTar = item.BelgeYuklemeYedekBasTar.Value, BitisTar = item.BelgeYuklemeYedekBitTar.Value, MulakatSonucTipID = MulakatSonucTipi.Yedek });
                    }

                    var NowDate = DateTime.Now;
                    IsBelgeYuklemeAktif = BelgeKTModel.Any(a2 => a2.EnstituKod == BasvuruSurec.EnstituKod && Tercih.OgrenimTipKod == a2.OgrenimTipKod && MulakatSonucu.MulakatSonucTipID == a2.MulakatSonucTipID && a2.BaslangicTar <= NowDate && a2.BitisTar >= NowDate);

                    if (Tercih.MulakatSonuclaris.First().KayitDurumID.HasValue)
                    {
                        mMessage.Messages.Add("Seçtiğiniz tercih işlem gördüğünden herhangi bir değişiklik yapamazsınız!");
                    }
                    else if (DonemdekiBasvuruSonuclari.Any(a => a.BasvuruTercihID != Tercih.BasvuruTercihID && (a.KayitDurumID == KayitDurumu.KayitOldu || a.KayitDurumID == KayitDurumu.OnKayit)))
                    {
                        mMessage.Messages.Add("Kayıt/Ön kayıt işlemi gören tercihiniz bulunduğu için başka tercih seçimi yapamazsınız!");
                    }
                }

                if (!IsBelgeYuklemeAktif)
                {
                    var SonucTipAdi = MulakatSonucu.MulakatSonucTipID == MulakatSonucTipi.Asil ? "Asil" : "Yedek";
                    var TarihKriteri = BelgeKTModel.Where(a2 => a2.EnstituKod == BasvuruSurec.EnstituKod && Tercih.OgrenimTipKod == a2.OgrenimTipKod && MulakatSonucu.MulakatSonucTipID == a2.MulakatSonucTipID).FirstOrDefault();

                    if (TarihKriteri != null)
                    {
                        var Tarih = TarihKriteri.BaslangicTar.ToString("yyyy-MM-dd HH:mm") + " / " + TarihKriteri.BitisTar.ToString("yyyy-MM-dd HH:mm");
                        mMessage.Messages.Add(SonucTipAdi + " kontenjandan kayıt hakkın kazananlar için belge yükleme işlemi " + Tarih + "  tarihleri arasında yapılabilir!");
                    }
                    else mMessage.Messages.Add("Belge yükleme tarih aralığı belirlenmediği için tercih seçimi yapılamaz!");
                }

                else if (DonemdekiBasvuruSonuclari.Count(a => mSonucTip.Contains(a.MulakatSonucTipID)) == DonemdekiBasvuruSonuclari.Count(a => a.KayitDurumID.HasValue))
                {

                    mMessage.Messages.Add("Başvurunuz için kayıt işlemi süreci tamamlandığından başvuruda herhangi işlemi yapılamaz!");

                }

                else
                {
                    Tercih.IsSecilenTercih = IsSecildi == true;
                    var DigerTercihler = DonemdekiBasvurulari.SelectMany(s => s.BasvurularTercihleris).Where(p => p.MulakatSonuclaris.Any(a => a.MulakatSonucTipID == MulakatSonucu.MulakatSonucTipID) && p.IsSecilenTercih != null && p.UniqueID != Tercih.UniqueID).ToList();
                    foreach (var item in DigerTercihler)
                    {
                        item.IsSecilenTercih = false;
                        item.IslemTarihi = DateTime.Now;
                        item.IslemYapanID = UserIdentity.Current.Id;
                        item.IslemYapanIP = UserIdentity.Ip;
                    }
                    Tercih.IslemTarihi = DateTime.Now;
                    Tercih.IslemYapanID = UserIdentity.Current.Id;
                    Tercih.IslemYapanIP = UserIdentity.Ip;
                    db.SaveChanges();
                    mMessage.IsSuccess = true;
                    if (IsSecildi == true)
                        mMessage.Messages.Add("Kayıt olunmak istenen program seçildi.");
                    else
                    {
                        mMessage.Messages.Add("Kayıt olunmak istenen program seçimi kaldırıldı.");
                    }

                    #region sendMail
                    if (IsSecildi == true)
                    {
                        var htmlBigliRow = new List<mailTableRow>();
                        var contentBilgi = new mailTableContent();

                        var OtL = db.OgrenimTipleris.Where(p => p.OgrenimTipKod == Tercih.OgrenimTipKod).First();
                        var Prl = Tercih.Programlar;

                        htmlBigliRow.Add(new mailTableRow { Baslik = "Öğrenim Seviyesi", Aciklama = OtL.OgrenimTipAdi });
                        htmlBigliRow.Add(new mailTableRow { Baslik = "Program Adı", Aciklama = Prl.ProgramAdi });
                        if (BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru)
                        {
                            contentBilgi.GrupBasligi = "Lisansüstü programlarına kayıt olmak için seçtiğiniz tercihi bilgisi aşağıdaki gibidir.";
                        }
                        else if (BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru)
                        {
                            contentBilgi.GrupBasligi = "Lisansüstü yatay geçiş programlarına kayıt olmak için seçtiğiniz tercihi bilgisi aşağıdaki gibidir.";
                        }
                        else contentBilgi.GrupBasligi = "YTu Yeni mezun Lisansüstü programlarına kayıt olmak için seçtiğiniz tercihi bilgisi aşağıdaki gibidir.";
                        contentBilgi.Detaylar = htmlBigliRow;

                        var mmmC = new mdlMailMainContent();
                        var enstituAdi = db.Enstitulers.Where(p => p.EnstituKod == BasvuruSurec.EnstituKod).First().EnstituAd;
                        mmmC.EnstituAdi = enstituAdi;
                        mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
                        var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(BasvuruSurec.EnstituKod);
                        var _ea = mailBilgi.SistemErisimAdresi;
                        var WurlAddr = _ea.Split('/').ToList();
                        if (_ea.Contains("//"))
                            _ea = WurlAddr[0] + "//" + WurlAddr.Skip(2).Take(1).First();
                        else
                            _ea = "http://" + WurlAddr.First();
                        mmmC.LogoPath = _ea + "/Content/assets/images/ytu_logo_tr.png";
                        var HCB = Management.RenderPartialView("Ajax", "getMailTableContent", contentBilgi);
                        mmmC.Content = HCB;

                        string htmlMail = Management.RenderPartialView("Ajax", "getMailContent", mmmC);
                        var snded = MailManager.sendMail(mailBilgi.EnstituKod, "Lisansüstü " + (BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru ? "" : "yatay geçiş ") + "başvurusu kayıt olunmak istenen tercih Hk.", htmlMail, Basvuru.EMail, null);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = BasvuruSurec.EnstituKod;
                            kModel.BasvuruID = Basvuru.BasvuruID;
                            kModel.MesajID = null;
                            kModel.Konu = "Kayıt olunmak istenen tercih seçimi (" + Basvuru.Ad + " " + Basvuru.Soyad + ")";
                            kModel.Aciklama = "";
                            kModel.AciklamaHtml = htmlMail;
                            kModel.IslemYapanID = UserIdentity.Current.Id;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Gonderildi = true;
                            kModel.GonderilenMailKullanicilars = new List<GonderilenMailKullanicilar>();
                            kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = Basvuru.EMail });
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }
                    #endregion

                }
            }
            mMessage.MessageType = Msgtype.Warning;
            var strView = Management.RenderPartialView("Ajax", "getMessage", mMessage);
            return Json(new { IsSuccess = mMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }


        [Authorize]
        public ActionResult BasvuruKayitOlunacakTercihSet2(int id, List<string> UniqueIDs, List<string> IsSecildis, List<string> Onceliks)
        {

            var mMessage = new MmMessage();
            var Kayityetki = RoleNames.BasvuruSureciOgrenciKayit.InRoleCurrent();

            var Basvuru = db.Basvurulars.Where(p => p.BasvuruID == id && p.KullaniciID == (!Kayityetki ? UserIdentity.Current.Id : p.KullaniciID)).First();
            var BasvuruSurec = Basvuru.BasvuruSurec;
            var BelgeKTModel = new List<EntBegeKayitT>();

            var DonemdekiBasvurulari = BasvuruSurec.Basvurulars.Where(a => a.KullaniciID == Basvuru.KullaniciID).ToList();
            var DonemdekiBasvuruSonuclari = DonemdekiBasvurulari.SelectMany(s => s.MulakatSonuclaris).ToList();
            var DonemdekiBasvuruTercihleri = DonemdekiBasvurulari.SelectMany(s => s.BasvurularTercihleris).ToList();
            var mSonucTip = new List<int> { MulakatSonucTipi.Asil, MulakatSonucTipi.Yedek };

            UniqueIDs = UniqueIDs ?? new List<string>();
            IsSecildis = IsSecildis ?? new List<string>();
            Onceliks = Onceliks ?? new List<string>();
            var _UniqueIDs = UniqueIDs.Where(p => !p.IsNullOrWhiteSpace()).Select(s => new Guid(s)).ToList();
            var _IsSecildis = IsSecildis.Where(p => !p.IsNullOrWhiteSpace()).Select(s => s.ToBoolean().Value).ToList();
            var _Onceliks = Onceliks.Select(s => s.toIntObj()).ToList();

            var qUniqueIDs = _UniqueIDs.Select((s, inx) => new { inx, UniqueID = s }).ToList();
            var qIsSecildis = _IsSecildis.Select((s, inx) => new { inx, IsSecildi = s }).ToList();
            var qOnceliks = _Onceliks.Select((s, inx) => new { inx, Oncelik = s }).ToList();
            var qBasvuruIDs = db.BasvurularTercihleris.Where(p => _UniqueIDs.Contains(p.UniqueID)).Select(s => new { s.UniqueID, s.BasvuruID }).ToList();


            var qRows = (from u in qUniqueIDs
                         join b in qBasvuruIDs on u.UniqueID equals b.UniqueID
                         join I in qIsSecildis on u.inx equals I.inx
                         join O in qOnceliks on u.inx equals O.inx
                         select new
                         {
                             u.inx,
                             IslemBasvuruID = id,
                             TercihUniqueID = u.UniqueID,
                             TercihBasvuruID = b.BasvuruID,
                             I.IsSecildi,
                             O.Oncelik
                         }).ToList();

            if (qRows.Count > 0)
            {
                if (DonemdekiBasvuruSonuclari.Any(a => a.KayitDurumID.HasValue))
                {
                    mMessage.Messages.Add("Tercihleriniz yetkili tarafından işlem gördüğü için herhangi bir değişiklik yapılamaz.");
                }
                else
                {
                    var OgrenimTipleriLngs = db.OgrenimTipleris.Where(p => p.EnstituKod == BasvuruSurec.EnstituKod).Select(s => new { s.OgrenimTipKod, s.OgrenimTipAdi }).ToList();
                    var NowDate = DateTime.Now;
                    var IsBelgeYuklemeVar = BasvuruSurec.IsBelgeYuklemeVar;
                    var BelgeYuklemeTarihleris = BasvuruSurec.BasvuruSurecOgrenimTipleris.Where(p => p.BelgeYuklemeAsilBasTar.HasValue).ToList();
                    var TercihUniqueIDs = new List<Guid>();
                    foreach (var item in qRows.OrderBy(o => o.Oncelik ?? 5))
                    {
                        var Tercih = DonemdekiBasvuruTercihleri.Where(p => p.UniqueID == item.TercihUniqueID).First();
                        bool IsSuccess = true;
                        if (IsBelgeYuklemeVar && !Kayityetki)
                        {
                            IsSuccess = BelgeYuklemeTarihleris.Any(a => a.OgrenimTipKod == Tercih.OgrenimTipKod && a.BelgeYuklemeYedekBasTar.Value <= NowDate && a.BelgeYuklemeYedekBitTar >= NowDate);
                        }
                        if (IsSuccess)
                        {
                            Tercih.IsSecilenTercih = item.IsSecildi;
                            Tercih.KayitSiraNo = item.IsSecildi ? item.Oncelik.Value : (int?)null;
                            Tercih.IslemTarihi = DateTime.Now;
                            Tercih.IslemYapanID = UserIdentity.Current.Id;
                            Tercih.IslemYapanIP = UserIdentity.Ip;
                            TercihUniqueIDs.Add(item.TercihUniqueID);
                        }
                        else
                        {
                            var OtL = OgrenimTipleriLngs.Where(p => p.OgrenimTipKod == Tercih.OgrenimTipKod).First();
                            mMessage.Messages.Add(OtL.OgrenimTipAdi + " Öğrenim seviyesi için Belge yükleme tarihi aktif olmadığından tercih seçim işlemi yapılamaz!");
                        }
                    }

                    #region sendMail
                    if (TercihUniqueIDs.Count > 0)
                    {
                        var htmlBigliRow = new List<mailTableRow>();
                        var contentBilgi = new mailTableContent();

                        foreach (var item in TercihUniqueIDs)
                        {
                            var qTercih = qRows.Where(p => p.TercihUniqueID == item).First();
                            var Tercih = DonemdekiBasvuruTercihleri.Where(p => p.UniqueID == item).First();
                            var OtL = OgrenimTipleriLngs.Where(p => p.OgrenimTipKod == Tercih.OgrenimTipKod).First();
                            var Prl = Tercih.Programlar;
                            htmlBigliRow.Add(new mailTableRow { Baslik = "Öğrenim Seviyesi", Aciklama = OtL.OgrenimTipAdi });
                            htmlBigliRow.Add(new mailTableRow { Baslik = "Program Adı", Aciklama = Prl.ProgramAdi });
                            if (qTercih.IsSecildi) htmlBigliRow.Add(new mailTableRow { Baslik = "Kayıt Seçeneği", Aciklama = "Kayıt Olmak İstenen " + qTercih.Oncelik + ". Tercih" });
                            else htmlBigliRow.Add(new mailTableRow { Baslik = "Kayıt Seçeneği", Aciklama = "Kayıt Olmak İstenmiyor" });
                            htmlBigliRow.Add(new mailTableRow { Baslik = "---------------", Aciklama = "-----------------" });
                        }

                        contentBilgi.GrupBasligi = "Lisansüstü programlarına kayıt olmak için seçtiğiniz tercihi bilgisi aşağıdaki gibidir.";
                        contentBilgi.Detaylar = htmlBigliRow;

                        var mmmC = new mdlMailMainContent();
                        var enstituAdi = db.Enstitulers.Where(p => p.EnstituKod == BasvuruSurec.EnstituKod).First().EnstituAd;
                        mmmC.EnstituAdi = enstituAdi;
                        mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
                        var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(BasvuruSurec.EnstituKod);
                        var _ea = mailBilgi.SistemErisimAdresi;
                        var WurlAddr = _ea.Split('/').ToList();
                        if (_ea.Contains("//"))
                            _ea = WurlAddr[0] + "//" + WurlAddr.Skip(2).Take(1).First();
                        else
                            _ea = "http://" + WurlAddr.First();
                        mmmC.LogoPath = _ea + "/Content/assets/images/ytu_logo_tr.png";
                        var HCB = Management.RenderPartialView("Ajax", "getMailTableContent", contentBilgi);
                        mmmC.Content = HCB;

                        string htmlMail = Management.RenderPartialView("Ajax", "getMailContent", mmmC);
                        var SendEmail = Basvuru.EMail;
                        // SendEmail = "irfansecer@gmail.com";
                        var snded = MailManager.sendMail(mailBilgi.EnstituKod, "Lisansüstü kayıt olunmak istenen tercih Hk.", htmlMail, SendEmail, null);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = BasvuruSurec.EnstituKod;
                            kModel.BasvuruID = Basvuru.BasvuruID;
                            kModel.MesajID = null;
                            kModel.Konu = "Kayıt olunmak istenen tercih seçimi (" + Basvuru.Ad + " " + Basvuru.Soyad + ")";
                            kModel.Aciklama = "";
                            kModel.AciklamaHtml = htmlMail;
                            kModel.IslemYapanID = UserIdentity.Current.Id;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Gonderildi = true;
                            kModel.GonderilenMailKullanicilars = new List<GonderilenMailKullanicilar>();
                            kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = Basvuru.EMail });
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }
                    #endregion
                }
            }


            mMessage.MessageType = Msgtype.Warning;
            var strView = Management.RenderPartialView("Ajax", "getMessage", mMessage);
            return Json(new { IsSuccess = mMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        [HttpGet]
        public ActionResult getDetailMezuniyet(int id, int? ShowDetayYayinID, int tbInx, bool IsDelete, bool GelenBasvuru = false)
        {
            var model = Management.getSecilenBasvuruMezuniyetDetay(id, null, ShowDetayYayinID);
            model.GelenBasvuru = GelenBasvuru;
            model.SelectedTabIndex = tbInx;


            var SRSonTalebi = model.MezuniyetSRModel.SalonRezervasyonlari.OrderByDescending(o => o.SRTalepID).FirstOrDefault();
            var ModelBasvuruDurum = new frMezuniyetBasvurulari
            {
                IsDanismanOnay = model.IsDanismanOnay,
                DanismanOnayTarihi = model.DanismanOnayTarihi,
                DanismanOnayAciklama = model.DanismanOnayAciklama,
                MezuniyetYayinKontrolDurumID = model.MezuniyetYayinKontrolDurumID,
                MezuniyetYayinKontrolDurumAdi = model.MezuniyetYayinKontrolDurumAdi,
                MezuniyetYayinKontrolDurumAciklamasi = model.MezuniyetYayinKontrolDurumAciklamasi,
                DurumClassName = model.DurumClassName,
                DurumColor = model.DurumColor,
                MezuniyetJuriOneriFormu = model.MezuniyetJuriOneriFormlaris.FirstOrDefault(),
                SrTalebi = SRSonTalebi,
                EYKTarihi = model.EYKTarihi,
                MezuniyetBasvurulariTezDosyasi = model.MezuniyetBasvurulariTezDosyalaris.OrderByDescending(o => o.MezuniyetBasvurulariTezDosyaID).FirstOrDefault(),
                TeslimFormDurumu = SRSonTalebi != null ? SRSonTalebi.SRTalepleriBezCiltFormus.Any() : false,
                IsMezunOldu = model.IsMezunOldu,
                MezuniyetTarihi = model.MezuniyetTarihi,

            };

            model.BasvuruDurumHtml = ModelBasvuruDurum.ToRenderPartialViewHtml("Mezuniyet", "BasvuruDurumView");

            DateTime? OnayTarihi = null;
            if (ModelBasvuruDurum.MezuniyetJuriOneriFormu != null) OnayTarihi = ModelBasvuruDurum.MezuniyetJuriOneriFormu.EYKYaGonderildi == true ? ModelBasvuruDurum.MezuniyetJuriOneriFormu.EYKYaGonderildiIslemTarihi : null;


            model.IsDelete = IsDelete;
            model.SMezuniyetYayinKontrolDurum = new SelectList(Management.cmbMezuniyetYayinDurum(false, true), "Value", "Caption", model.MezuniyetYayinKontrolDurumID);
            model.SEYKYaGonderildi = new SelectList(Management.cmbJOEykGonderimDurumData(true, OnayTarihi), "Value", "Caption", model.EYKYaGonderildi);
            model.SEYKDaOnaylandi = new SelectList(Management.cmbJOEykOnayDurumData(true), "Value", "Caption", model.EYKDaOnaylandi);

            model.SIsAsilOryedek = new SelectList(Management.cmbJOAsilYedekDurumData(true), "Value", "Caption");


            return View(model);
        }
        [HttpGet]
        public ActionResult getDetailTIBasvuru(int id, Guid? UniqueID, bool IsDelete, bool GelenBasvuru = false)
        {

            var model = Management.getSecilenBasvuruTIDetay(id, UniqueID);
            model.GelenBasvuru = GelenBasvuru;

            ViewBag.IsDelete = IsDelete;

            return View(model);
        }
        [Authorize]
        [HttpGet]
        public ActionResult getDetailTDOBasvuru(int id, Guid? UniqueID, bool IsDelete, bool GelenBasvuru = false)
        {

            var model = Management.getSecilenBasvuruTDODetay(id, UniqueID);
            model.GelenBasvuru = GelenBasvuru;

            ViewBag.IsDelete = IsDelete;

            return View(model);
        }

        [HttpGet]
        public ActionResult getGBNDetail(int id)
        {
            var model = Management.getSecilenBasvuruDetay(id);

            ViewBag.gLOgrenimDurumID = new SelectList(Management.cmbAktifOgrenimDurumu2(true, IsBasvurudaGozuksun: true), "Value", "Caption", model.LOgrenimDurumID);
            ViewBag.gLNotSistemID = new SelectList(Management.cmbGetNotSistemleri(true), "Value", "Caption", model.LNotSistemID);
            return View(model);
        }
        public ActionResult setGBNDetail(int BasvuruID, int? gLOgrenimDurumID, int? gLNotSistemID, double? gLMezuniyetNotu)
        {
            var _MmMessage = new MmMessage();
            var basvuru = db.Basvurulars.Where(p => p.BasvuruID == BasvuruID).First();
            var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == basvuru.BasvuruSurecID).First();

            if (RoleNames.GelenBasvurularKayit.InRoleCurrent() == false)
            {
                if (basvuru.KullaniciID != UserIdentity.Current.Id)
                {
                    Management.SistemBilgisiKaydet("Başka bir kullanıcıya ait başvuru üzerindeki AGNO bilgisi güncellenmek isteniyor!\nBasvuruID:" + basvuru.BasvuruID + "\nKullaniciID:" + UserIdentity.Current.Id + "\nÇağrılan Kullanıcı ID:" + basvuru.KullaniciID, "Ajax/setGBNDetail", BilgiTipi.Saldırı);
                    _MmMessage.Messages.Add("Başka bir kullanıcıya ait başvuru üzerindeki AGNO bilgisini güncelleyemezsiniz!");
                }
            }
            var nowDate = DateTime.Now;
            if (!(bsurec.AGNOGirisBaslangicTarihi <= nowDate && bsurec.AGNOGirisBitisTarihi >= nowDate))
            {
                _MmMessage.Messages.Add("Aktif bir AGNO bilgisi düzeltme süreci bulunamadı! Düzeltme işlemini yapamazsınız.");
            }

            if (_MmMessage.Messages.Count == 0)
            {
                if (!gLOgrenimDurumID.HasValue)
                {
                    _MmMessage.Messages.Add("Öğrenim durumunuzu seçiniz");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "gLOgrenimDurumID" });
                }
                else
                {
                    if (gLOgrenimDurumID.Value != OgrenimDurum.Mezun)
                    {
                        string msg = "Güncel not bilginizi girebilmeniz için öğrenim durumunuzu Mezun seçeneği olarak seçmeniz gerekmektedir!";
                        _MmMessage.Messages.Add(msg);
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "gLOgrenimDurumID" });
                    }
                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "gLOgrenimDurumID" });
                }
                if (!gLNotSistemID.HasValue)
                {
                    string msg = "Lisans Eğitimi Not Sistemi Bilgisini Seçiniz!";
                    _MmMessage.Messages.Add(msg);
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "gLNotSistemID" });
                }
                else
                {
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "gLNotSistemID" });

                    var notSistemi = Management.getNotSistemi(gLNotSistemID.Value);
                    var nots = db.NotSistemleris.Where(p => p.NotSistemID == notSistemi.NotSistemID).First();
                    if (!gLMezuniyetNotu.HasValue)
                    {
                        string msg = "Lisans Eğitimi Not Bilgisini Giriniz!";
                        _MmMessage.Messages.Add(msg);
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "gLMezuniyetNotu" });

                    }
                    else if ((double)nots.MinNot <= gLMezuniyetNotu.Value && (double)nots.MaxNot >= gLMezuniyetNotu)
                    {
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "gLMezuniyetNotu" });
                    }
                    else
                    {
                        _MmMessage.Messages.Add("Lisans Eğitimi Notu " + notSistemi.NotSistemAdi + " not sistemine göre " + nots.MinNot.ToString() + " ile " + nots.MaxNot.ToString() + " arasında bir değer olmalıdır!");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "gLMezuniyetNotu" });
                    }
                }
                if (_MmMessage.Messages.Count == 0)
                {
                    basvuru.LNotSistemID = gLNotSistemID.Value;
                    basvuru.LMezuniyetNotu = gLMezuniyetNotu.Value;
                    basvuru.LMezuniyetNotu100LukSistem = gLMezuniyetNotu.Value.ToNotCevir(gLNotSistemID.Value).Not100Luk;
                    basvuru.LOgrenimDurumID = gLOgrenimDurumID.Value;
                    basvuru.IslemTarihi = DateTime.Now;
                    basvuru.IslemYapanID = UserIdentity.Current.Id;
                    basvuru.IslemYapanIP = UserIdentity.Ip;
                    var btercih = basvuru.BasvurularTercihleris.ToList();
                    foreach (var item in btercih)
                    {
                        var mulR = db.MulakatSonuclaris.Where(p => p.BasvuruTercihID == item.BasvuruTercihID).FirstOrDefault();
                        if (mulR != null)
                        {
                            mulR.Agno = basvuru.LMezuniyetNotu100LukSistem;

                        }
                    }

                    db.SaveChanges();
                    _MmMessage.Messages.Add("Not güncelleme işlemi başarılı!");
                    _MmMessage.IsSuccess = true;
                }
            }
            return _MmMessage.toJsonResult();
        }

        public ActionResult SifreResetle(string MailAddress)
        {
            var MmMessage = new MmMessage();

            if (MailAddress.IsNullOrWhiteSpace() || MailAddress.ToIsValidEmail())
            {
                MmMessage.IsSuccess = false;
                MmMessage.Title = "Lütfen doğru bir mail formatı giriniz.";
            }
            else
            {
                var kul = db.Kullanicilars.Where(p => p.EMail.Equals(MailAddress) && p.IsAktif).FirstOrDefault();
                if (kul == null)
                {
                    MmMessage.IsSuccess = false;
                    MmMessage.Title = "Girmiş olduğunuz mail adresi ile eşleşen herhangi bir kullanıcıya rastlanmadı!";
                }
                else
                {
                    if (kul.IsActiveDirectoryUser)
                    {
                        MmMessage.IsSuccess = false;
                        MmMessage.Title = "Girmiş olduğunuz mail adresi 'Active directory' sistemine entegre çalıştığı için ve bilgi işlem tarafından belirlenen ve bazı sistemlere  (YTU Mail, EBYS, Lojman Yönetim Sistem, Lisansustu Başvuru Sistemi vb)  erişimini sağlayan ortak bir şifresi bulunmaktadır. Bu mail adresi için tanımlanmış şifre sadece bilgi işlem tarafından belirlenip değiştirilebilmektedir. '" + kul.KullaniciAdi + "' kullanıcı adı ile YTU Mail, EBYS, Lojman Yönetim Sistem, Lisansustu Başvuru Sistemi vb. programlara giriş yaptığınız şifrenizi hatırlamıyorsanız şifre değişikliği işlemi için lütfen Bilgi İşlem ile görüşünüz.";
                    }
                    else
                    {
                        var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(kul.EnstituKod);
                        var mRowModel = new List<mailTableRow>();
                        DateTime gecerlilikTarihi = DateTime.Now.AddHours(2);
                        string guid = Guid.NewGuid().ToString().Substring(0, 20);
                        mRowModel.Add(new mailTableRow { Baslik = "Şifre Sıfırlama Linki", Aciklama = "<a target='_blank' href='" + mailBilgi.SistemErisimAdresi + "/Account/ParolaSifirla?psKod=" + guid + "'>Şifrenizi sıfırlamak için tıklayınız</a>" });
                        mRowModel.Add(new mailTableRow { Baslik = "Link Geçerlilik Tarihi", Aciklama = "Yukarıdaki link '" + gecerlilikTarihi.ToFormatDateAndTime() + "' tarihine kadar geçerlidir." });

                        var mmmC = new mdlMailMainContent();
                        mmmC.EnstituAdi = db.Enstitulers.Where(p => p.EnstituKod == kul.EnstituKod).First().EnstituAd;
                        mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
                        var _ea = mailBilgi.SistemErisimAdresi;
                        var WurlAddr = _ea.Split('/').ToList();
                        if (_ea.Contains("//"))
                            _ea = WurlAddr[0] + "//" + WurlAddr.Skip(2).Take(1).First();
                        else
                            _ea = "http://" + WurlAddr.First();
                        mmmC.LogoPath = _ea + "/Content/assets/images/ytu_logo_tr.png";
                        var mtc = new mailTableContent();
                        mtc.AciklamaBasligi = "Şifre Sıfırlama İşlemi";
                        mtc.AciklamaDetayi = "Şifrenizi sıfırlamak için aşağıda bulunan linke tıklayınız ve açılan sayfa da yeni şifrenizi tanımlayınız.";
                        mtc.Detaylar = mRowModel;
                        var tavleContent = Management.RenderPartialView("Ajax", "getMailTableContent", mtc);
                        mmmC.Content = tavleContent;

                        string htmlMail = Management.RenderPartialView("Ajax", "getMailContent", mmmC);
                        var User = mailBilgi.SmtpKullaniciAdi;
                        var EMailList = new List<MailSendList>();
                        EMailList.Add(new MailSendList { EMail = kul.EMail, ToOrBcc = true });
                        var rtVal = MailManager.sendMailRetVal(kul.EnstituKod, "Şifre Sıfırlama İşlemi", htmlMail, EMailList, null);
                        if (rtVal == null)
                        {
                            MmMessage.IsSuccess = true;
                            MmMessage.Title = "Şifre sıfırlama linki '" + kul.EMail + "' adresine gönderilmiştir!";
                            kul.ParolaSifirlamaKodu = guid;
                            kul.ParolaSifirlamGecerlilikTarihi = gecerlilikTarihi;
                            db.SaveChanges();
                        }
                        else
                        {
                            MmMessage.IsSuccess = false;
                            Management.SistemBilgisiKaydet("Şifre sıfırlama! Hata: " + rtVal.ToExceptionMessage(), rtVal.ToExceptionStackTrace(), BilgiTipi.Hata, kul.KullaniciID, UserIdentity.Ip);
                            MmMessage.Title = "Şifre sıfırlama linki '" + kul.EMail + "' adresine gönderilemedi!";
                        }
                    }
                }

            }

            return MmMessage.toJsonResult();
        }

        public ActionResult pTipKontrol(int id)
        {
            var pt = db.KullaniciTipleris.Where(p => p.KullaniciTipID == id).Select(s => new
            {
                s.KullaniciTipID,
                s.KullaniciTipAdi,
                s.IsAktif,
                s.KurumIci,
                s.Yerli
            }).First();
            return pt.toJsonResult();
        }
        public ActionResult getOts(string EnstituKod, bool bosSecimVar = true, int? HaricOgreniTipKod = null)
        {
            var cmbmld = new List<CmbIntDto>();
            cmbmld = Management.cmbAktifOgrenimTipleri(EnstituKod, bosSecimVar, true, HaricOgreniTipKod);

            return cmbmld.toJsonResult();
        }
        public ActionResult getBolumler(int BasvuruSurecID, string otKod, int KTipID)
        {
            var cmbmld = new List<CmbIntDto>();

            bool IsSubOT = db.OgrenimTipleris.Where(p => p.IsAktif && p.GrupGoster && p.GrupKodu == otKod).Count() > 0;
            if (!IsSubOT)
                cmbmld = Management.cmbGetAktifBolumlerX(otKod.ToInt().Value, BasvuruSurecID, KTipID);
            else cmbmld = Management.cmbGetAktifSubOgrenimTipleri(BasvuruSurecID, otKod, true);
            bool _YLShow = false;
            if (!IsSubOT)
            {
                var bs = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();
                var _ID = otKod.ToInt().Value;
                _YLShow = db.OgrenimTipleris.Where(p => p.EnstituKod == bs.EnstituKod && p.OgrenimTipKod == _ID).First().YLEgitimBilgisiIste;
            }
            return new { IsSubOT = IsSubOT, data = cmbmld.Select(s => new { s.Value, s.Caption }), YLShow = _YLShow.ToString().ToLower() }.toJsonResult();
        }
        public ActionResult getProgramlar(int bolID, int otID, int BasvuruSurecID, int KullaniciTipID)
        {
            var bolm = Management.cmbGetAktifProgramlarX(bolID, otID, BasvuruSurecID, KullaniciTipID);
            return bolm.Select(s => new { s.Value, s.Caption }).toJsonResult();
        }

        public ActionResult OgrenimBilgisiKontrolEt(kmBasvuru kModel, int BasvuruID)
        {
            var _MmMessage = Management.obKontrol(kModel);
            if (kModel.ProgramKod.IsNullOrWhiteSpace())
            {
                _MmMessage.Messages.Add("Ekleyeceğiniz Programı Seçiniz!");
                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
            }
            else
            {
                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "ProgramKod" });
            }

            if (_MmMessage.Messages.Count == 0)
            {
                var programs = new List<CmbIntDto>();
                programs.Add(new CmbIntDto { Value = kModel.OgrenimTipKod, Caption = kModel.ProgramKod });
                var retVal = Management.programAgnoMinControl(kModel, programs);
                _MmMessage.Messages.AddRange(retVal.Messages);
                _MmMessage.MessagesDialog.AddRange(retVal.MessagesDialog);
            }
            if (_MmMessage.Messages.Count == 0)
            {
                //Düzeltilecek
                var BasvuruSureci = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID).First();
                var alanB = new kotaKontrolModel();
                if (BasvuruSureci.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru || BasvuruSureci.BasvuruSurecTipID == BasvuruSurecTipi.YTUYeniMezunDRBasvuru)
                    alanB = Management.AlanKontrol(kModel.BasvuruSurecID, kModel.LOgrenciBolumID.Value, kModel.YLOgrenciBolumID, kModel.OgrenimTipKod, kModel.ProgramKod, kModel.KullaniciID, BasvuruID);
                else
                {
                    var BolumIDs = new List<int>();
                    if (kModel.YLDurum)
                    {
                        BolumIDs.Add(kModel.LOgrenciBolumID.Value);
                        BolumIDs.Add(kModel.YLOgrenciBolumID.Value);
                        BolumIDs.Add(kModel.DROgrenciBolumID.Value);
                    }
                    else
                    {
                        BolumIDs.Add(kModel.LOgrenciBolumID.Value);
                        BolumIDs.Add(kModel.YLOgrenciBolumID.Value);
                    }
                    alanB = Management.AlanKontrolYG(kModel.BasvuruSurecID, BolumIDs, kModel.OgrenimTipKod, kModel.ProgramKod, kModel.KullaniciID, BasvuruID);
                }

                if (alanB.Kota <= 0)
                {
                    var alanTip = alanB.AlanTipID == AlanTipi.Ortak ? "Ortak" : (alanB.AlanTipID == AlanTipi.AlanIci ? "Alan İci" : "Alan Dışı");
                    _MmMessage.Messages.Add("Girmiş olduğunuz öğrenim bilgilerine göre seçtiğiniz program " + alanTip + " olarak değerlendirilmiştir fakat " + alanTip + " kota bulunmadığından programı ekleyemezsiniz! Lütfen başka bir program seçiniz.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
                }
                else if (alanB.AlanDisiProgramKisitlamasiVar)
                {
                    _MmMessage.Messages.Add(alanB.AlanDisiProgramKisitlamasiMsg);
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
                }
            }
            else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "ProgramKod" });
            _MmMessage.IsSuccess = _MmMessage.Messages.Count == 0;
            _MmMessage.Title = "Ekleme işmini yapabilmek aşağıdaki uyarıları incleyiniz!";
            _MmMessage.MessageType = _MmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            return _MmMessage.toJsonResult();
        }

        public ActionResult getBolumProgramList(string Kod, string Tip)
        {
            if (Tip == "bolum")
            {
                var lst = Management.cmbGetYetkiliAnabilimDallari(true, Kod);
                return lst.Select(s => new { s.Value, s.Caption }).toJsonResult();
            }
            else if (Tip == "program")
            {
                var lst = Management.cmbGetAktifProgramlar(true, Kod.ToInt().Value);
                return lst.Select(s => new { s.Value, s.Caption }).toJsonResult();
            }
            return null;

        }
        public ActionResult getKullaniciBolumProgramList(string EnstituKod, string BolumKod, string Tip)
        {

            if (Tip == "bolum")
            {
                var lst = Management.cmbGetYetkiliProgramAnabilimDallari(true, EnstituKod);
                return lst.Select(s => new { s.Value, s.Caption }).toJsonResult();
            }
            else if (Tip == "program")
            {
                var lstStr = Management.cmbGetKullaniciProgramlari(UserIdentity.Current.Id, EnstituKod, BolumKod, true);
                return lstStr.Select(s => new { s.Value, s.Caption }).toJsonResult();
            }
            else
            {
                return null;
            }
        }
        [ValidateInput(false)]
        public ActionResult ValidationControlSteps(kmBasvuru kModel)
        {
            var _ShowEgitimDiliIsle = false;
            var _MmMessage = new MmMessage();
            var resimBilgi = new CmbStringDto { Caption = "", Value = "" };

            if (kModel.StepNo == 1)
            {
                _MmMessage.Title = "Bir sonraki adıma geçmek için aşağıdaki uyarıları kontrol ediniz!";
                var kmM = Management.kuKontrol(kModel);
                _MmMessage.Messages.AddRange(kmM.Messages.ToList());
                _MmMessage.MessagesDialog.AddRange(kmM.MessagesDialog.ToList());
            }
            else if (kModel.StepNo == 2)
            {

                #region step2
                _MmMessage = Management.obKontrol(kModel);
                if (_MmMessage.Messages.Count == 0 && kModel.TercihSayisi > 0)
                {
                    var kulTID = db.Kullanicilars.Where(p => p.KullaniciID == kModel.KullaniciID).First();
                    kModel.KullaniciTipID = kulTID.KullaniciTipID;
                    var qOgrenimTipKod = kModel._OgrenimTipKod.Select((s, inx) => new { Index = inx, OgrenimTipKod = s }).ToList();
                    var qProgramKod = kModel._ProgramKod.Select((s, inx) => new { Index = inx, ProgramKod = s }).ToList();
                    var qIngilizce = kModel._Ingilizce.Select((s, inx) => new { Index = inx, Ingilizce = s }).ToList();
                    var qPrograms = (from s in qOgrenimTipKod
                                     join p in qProgramKod on s.Index equals p.Index
                                     join ing in qIngilizce on s.Index equals ing.Index
                                     select new { s.OgrenimTipKod, p.ProgramKod, ing.Ingilizce }).ToList();
                    var retVal = Management.programAgnoMinControl(kModel, qPrograms.Select(s => new CmbIntDto { Value = s.OgrenimTipKod, Caption = s.ProgramKod }).ToList());
                    _MmMessage.Messages.AddRange(retVal.Messages);
                    _MmMessage.MessagesDialog.AddRange(retVal.MessagesDialog);
                    var qtercihler = qPrograms.Select(item => new CmbMultyTypeDto { Value = item.OgrenimTipKod, ValueB = item.Ingilizce, ValueS2 = item.ProgramKod }).ToList();
                    var TomerVar = Management.cmbGetdAktifSinavlar(qtercihler, kModel.BasvuruSurecID, SinavTipGrup.Tomer, true).Count > 0 && kModel.KullaniciTipID == KullaniciTipBilgi.YabanciOgrenci;
                    _ShowEgitimDiliIsle = TomerVar;
                    if (TomerVar && !kModel.LEgitimDiliTurkce.HasValue)
                    {
                        _MmMessage.Messages.Add("Lisans Eğitim Dilini Seçiniz.");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "LEgitimDiliTurkce" });

                    }
                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "LEgitimDiliTurkce" });

                    if (TomerVar && kModel.YLDurum && !kModel.YLEgitimDiliTurkce.HasValue)
                    {
                        _MmMessage.Messages.Add("Yüksek Lisans Eğitim Dilini Seçiniz.");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YLEgitimDiliTurkce" });

                    }
                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YLEgitimDiliTurkce" });

                }

                if (kModel.TercihSayisi == 0)
                {
                    _MmMessage.Messages.Add("Devam edebilmek için en az bir program tercihi eklemeniz gerekmektedir!");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SubOgrenimTipKod" });
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BolumID" });
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "trch_SiraNo" });
                }
                else
                {
                    if (kModel.KotaValid)
                    {
                        _MmMessage.Messages.Add("Kontenjanı bulunmayan bir programı tercihlerinize eklediniz. Devam edebilmek için lütfen kontenjanı olmayan tercihinizi kaldırıp, duyurulan kontenjan bilgisine uygun bir tercih ekleyiniz.");
                    }
                    else
                    {
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "OgrenimTipKod" });
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "SubOgrenimTipKod" });
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "BolumID" });
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "ProgramKod" });
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "trch_SiraNo" });
                    }

                }
                #endregion
                _MmMessage.Title = "Bir sonraki adıma geçmek için aşağıdaki uyarıları kontrol ediniz!";

            }
            else if (kModel.StepNo == 3)
            {
                _MmMessage.Title = "Kayıt işlemini yapabilmek için aşağıdaki uyarıları kontrol ediniz!";
                #region step3
                var qOgrenimTipKods = kModel._OgrenimTipKod.Select((s, inx) => new { s = s, Index = inx }).ToList();
                var qIngilizces = kModel._Ingilizce.Select((s, inx) => new { s = s, Index = inx }).ToList();
                var qtercihler = (from s in qOgrenimTipKods
                                  join qi in qIngilizces on s.Index equals qi.Index
                                  select new CmbMultyTypeDto { Value = s.s, ValueB = qi.s }).ToList();

                var TomerVar = Management.cmbGetdAktifSinavlar(qtercihler, kModel.BasvuruSurecID, SinavTipGrup.Tomer, true).Count > 0 && kModel.KullaniciTipID == KullaniciTipBilgi.YabanciOgrenci;
                var tomerIstensin = TomerVar && kModel.LEgitimDiliTurkce != true;
                if (kModel.AlesIstensinmi)
                {

                    if (kModel.BasvurularSinavBilgi_A.SinavTipID <= 0)
                    {
                        _MmMessage.Messages.Add("Ales/Gre/Gmat Sınavı grubu için sınav tipi seçiniz!");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_A.SinavTipID" });
                    }
                    else
                    {
                        var kmM = Management.stKontrol(
                            kModel.BasvuruSurecID,
                            kModel._OgrenimTipKod,
                            kModel._Ingilizce,
                            kModel.BasvurularSinavBilgi_A.SinavTipID,
                            kModel.BasvurularSinavBilgi_A.WsSinavYil,
                            null,
                            kModel.BasvurularSinavBilgi_A.WsSinavDonem,
                            kModel.BasvurularSinavBilgi_A.WsXmlData,
                            kModel._ProgramKod,
                            kModel.BasvurularSinavBilgi_A.SinavTarihi,
                            kModel.BasvuruTarihi,
                            kModel.BasvurularSinavBilgi_A.SubSinavAralikID,
                            kModel.BasvurularSinavBilgi_A.BasvuruSurecSubNot,
                            kModel.BasvurularSinavBilgi_A.SinavNotu);
                        _MmMessage.Messages.AddRange(kmM.Messages.ToList());
                        _MmMessage.MessagesDialog.AddRange(kmM.MessagesDialog.ToList());
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_A.SinavTipID" });

                    }
                }

                if (kModel.DilIstensinmi)
                {
                    var gdilSinavTips = db.BasvuruSurecSinavTipleris.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID && p.SinavTipGrupID == SinavTipGrup.DilSinavlari).Select(s => s.SinavTipID).ToList();

                    if (kModel.BasvurularSinavBilgi_D.SinavTipID <= 0)
                    {
                        _MmMessage.Messages.Add("Dil Sınavı grubu için sınav tipi seçiniz!");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_D.SinavTipID" });
                    }
                    else
                    {

                        var kmM = Management.stKontrol(
                            kModel.BasvuruSurecID,
                            kModel._OgrenimTipKod,
                            kModel._Ingilizce,
                            kModel.BasvurularSinavBilgi_D.SinavTipID,
                            kModel.BasvurularSinavBilgi_D.WsSinavYil,
                            kModel.BasvurularSinavBilgi_D.SinavDilID,
                            kModel.BasvurularSinavBilgi_D.WsSinavDonem,
                            kModel.BasvurularSinavBilgi_D.WsXmlData,
                            kModel._ProgramKod,
                            kModel.BasvurularSinavBilgi_D.SinavTarihi,
                            kModel.BasvuruTarihi,
                            kModel.BasvurularSinavBilgi_D.SubSinavAralikID,
                            kModel.BasvurularSinavBilgi_D.BasvuruSurecSubNot,
                            kModel.BasvurularSinavBilgi_D.SinavNotu,
                            kModel.BasvurularSinavBilgi_D.IsTaahhutVar);
                        _MmMessage.Messages.AddRange(kmM.Messages.ToList());
                        _MmMessage.MessagesDialog.AddRange(kmM.MessagesDialog.ToList());
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_D.SinavTipID" });

                    }
                }
                if (tomerIstensin)
                {
                    var tmrSinavTips = db.BasvuruSurecSinavTipleris.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID && p.SinavTipGrupID == SinavTipGrup.Tomer).Select(s => s.SinavTipID).ToList();

                    if (kModel.BasvurularSinavBilgi_T.SinavTipID <= 0)
                    {
                        _MmMessage.Messages.Add("Tomer sınav tipi seçiniz!");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_T.SinavTipID" });
                    }
                    else
                    {
                        var kmM = Management.stKontrol(
                            kModel.BasvuruSurecID,
                            kModel._OgrenimTipKod,
                            kModel._Ingilizce,
                            kModel.BasvurularSinavBilgi_T.SinavTipID,
                            kModel.BasvurularSinavBilgi_T.WsSinavYil,
                            kModel.BasvurularSinavBilgi_T.SinavDilID,
                            kModel.BasvurularSinavBilgi_T.WsSinavDonem,
                            null,
                            kModel._ProgramKod,
                            kModel.BasvurularSinavBilgi_T.SinavTarihi,
                            kModel.BasvuruTarihi,
                            kModel.BasvurularSinavBilgi_T.SubSinavAralikID,
                            kModel.BasvurularSinavBilgi_T.BasvuruSurecSubNot,
                            kModel.BasvurularSinavBilgi_T.SinavNotu,
                            kModel.BasvurularSinavBilgi_T.IsTaahhutVar);
                        _MmMessage.Messages.AddRange(kmM.Messages.ToList());
                        _MmMessage.MessagesDialog.AddRange(kmM.MessagesDialog.ToList());
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_T.SinavTipID" });

                    }
                }
                if (kModel.BasvuruDurumID <= 0)
                {
                    _MmMessage.Messages.Add("Başvuru Durumunu Seçiniz!");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Basvuru.BasvuruDurumID" });

                }
                else
                {
                    if (kModel.BasvuruDurumID == BasvuruDurumu.Onaylandı && !kModel.Onaylandi)
                    {
                        _MmMessage.Messages.Add("Başvuru Onaylayınız!");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Onaylandi" });
                    }
                    else
                    {
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Onaylandi" });
                    }
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Basvuru.BasvuruDurumID" });
                }
                if (_MmMessage.Messages.Count == 0) kModel.sbmtForm = true;
                #endregion
            }
            _MmMessage.IsSuccess = _MmMessage.Messages.Count == 0;
            _MmMessage.MessageType = _MmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            string ekAciklama = "";
            string AnketGiris = "";
            if (kModel.sbmtForm && kModel.Onaylandi && kModel.BasvuruDurumID == BasvuruDurumu.Onaylandı)
            {

                var bSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID).First();
                if (!bSurec.AnketID.HasValue || (bSurec.AnketID.HasValue && UserIdentity.Current.Informations.Any(a => a.Key == "LUBAnket")))
                {

                    var model = new ekAciklamaContent();
                    model.Baslik = "Başvuru işleminizi tamamlamadan önce aşağıda bulunan programlar ile ilgili ek açıklamaları okuyunuz!";
                    var prgs = db.Programlars.Where(p => kModel._ProgramKod.Contains(p.ProgramKod)).ToList();
                    foreach (var item in prgs)
                    {
                        if (item.ProgramSecimiEkBilgi)
                        {
                            model.Detay.Add(new CmbStringDto { Caption = item.ProgramAdi + ":", Value = item.Aciklama });
                        }
                    }

                    if (model.Detay.Count > 0) ekAciklama = Management.RenderPartialView("Ajax", "getEkAciklamaContent", model);
                }
                else
                {
                    if (kModel.BasvuruID <= 0 || !db.AnketCevaplaris.Any(a => a.BasvuruID == kModel.BasvuruID))
                    {

                        var anketSorulari = (from bsa in db.Ankets.Where(p => p.AnketID == bSurec.AnketID)
                                             join aso in db.AnketSorus on bsa.AnketID equals aso.AnketID
                                             join sb in db.AnketCevaplaris.Where(p => p.BasvuruID == kModel.BasvuruID && p.Basvurular.KullaniciID == kModel.KullaniciID) on aso.AnketSoruID equals sb.AnketSoruID into def1
                                             from sbc in def1.DefaultIfEmpty()
                                             select new
                                             {
                                                 aso.AnketSoruID,
                                                 AnketSoruSecenekID = sbc != null ? sbc.AnketSoruSecenekID : (int?)null,
                                                 Aciklama = sbc != null ? sbc.EkAciklama : "",
                                                 aso.SiraNo,
                                                 aso.SoruAdi,
                                                 Secenekler = aso.AnketSoruSeceneks.OrderBy(o => o.SiraNo).Select(s => new
                                                 {
                                                     s.AnketSoruSecenekID,
                                                     s.AnketSoruID,
                                                     s.SiraNo,
                                                     s.IsEkAciklamaGir,
                                                     s.SecenekAdi,
                                                     s.IsYaziOrSayi

                                                 }).ToList()


                                             }).OrderBy(o => o.SiraNo).ToList();
                        var model = new kmAnketlerCevap();
                        model.AnketTipID = 1;
                        model.BasvuruSurecID = kModel.BasvuruSurecID;
                        model.AnketID = bSurec.AnketID.Value;
                        model.JsonStringData = anketSorulari.toJsonText();
                        foreach (var item in anketSorulari)
                        {
                            model.AnketCevapModel.Add(new AnketCevapModel
                            {
                                SecilenAnketSoruSecenekID = item.AnketSoruSecenekID,
                                SoruBilgi = new frAnketDetay { AnketSoruID = item.AnketSoruID, SoruAdi = item.SoruAdi, SiraNo = item.SiraNo, Aciklama = item.Aciklama },
                                SoruSecenek = item.Secenekler.Select(s => new frAnketSecenekDetay
                                {
                                    AnketSoruSecenekID = s.AnketSoruSecenekID,
                                    SiraNo = s.SiraNo,
                                    IsEkAciklamaGir = s.IsEkAciklamaGir,
                                    SecenekAdi = s.SecenekAdi
                                }).ToList(),
                                SelectListSoruSecenek = new SelectList(item.Secenekler.ToList(), "AnketSoruSecenekID", "SecenekAdi", item.AnketSoruSecenekID)
                            });
                        }

                        AnketGiris = Management.RenderPartialView("Ajax", "getAnket", model);
                    }
                }
            }
            return new { sbmtForm = kModel.sbmtForm, _MmMessage = _MmMessage, EkAciklamalar = ekAciklama, AnketGiris = AnketGiris, ShowEgitimDili = _ShowEgitimDiliIsle, kModel.YLDurum }.toJsonResult();
        }


        [ValidateInput(false)]
        public ActionResult MezuniyetValidationControlSteps(kmMezuniyetBasvuru kModel)
        {
            var _MmMessage = new MmMessage();
            if (kModel.StepNo == 1)
            {
                var tezK = Management.TezKontrol(kModel);
                _MmMessage.Messages.AddRange(tezK.Messages);
                _MmMessage.MessagesDialog.AddRange(tezK.MessagesDialog);
                _MmMessage.Title = "Bir sonraki adıma geçmek için aşağıdaki uyarıları kontrol ediniz!";
            }
            else if (kModel.StepNo == 2)
            {
                _MmMessage.Title = "Kayıt işlemini yapabilmek için aşağıdaki uyarıları kontrol ediniz!";

                if (kModel.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.Onaylandi)
                {
                    var yyK = Management.YayinKontrol(kModel);
                    _MmMessage.Messages.AddRange(yyK.Messages);
                    _MmMessage.MessagesDialog.AddRange(yyK.MessagesDialog);
                }
                if (_MmMessage.Messages.Count == 0) kModel.sbmtForm = true;
            }

            _MmMessage.IsSuccess = _MmMessage.Messages.Count == 0;
            _MmMessage.MessageType = _MmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            return new { sbmtForm = kModel.sbmtForm, _MmMessage = _MmMessage, EkAciklamalar = "" }.toJsonResult();
        }




        [Authorize]
        public ActionResult rotateImage(bool LeftOrRight, int KullaniciID)
        {
            if (RoleNames.KullanicilarKayit.InRoleCurrent() == false) KullaniciID = UserIdentity.Current.Id;
            var gelenBKayitY = RoleNames.GelenBasvurularKayit.InRoleCurrent();
            var user = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).First();
            string folname = SistemAyar.KullaniciResimYolu;
            if ((gelenBKayitY || KullaniciID == UserIdentity.Current.Id) && user.ResimAdi.IsNullOrWhiteSpace() == false)
            {
                var ImgPath = folname + "/" + user.ResimAdi;
                string pth = Server.MapPath(Management.getRoot() + ImgPath);

                using (Image img = Image.FromFile(pth))
                {
                    img.RotateFlip(LeftOrRight ? RotateFlipType.Rotate270FlipNone : RotateFlipType.Rotate90FlipNone);
                    //  var format = (System.Drawing.Imaging.ImageFormat)img.RawFormat;
                    img.Save(pth, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

            }
            return new { ResimAdi = folname + "/" + user.ResimAdi }.toJsonResult();
        }
        public ActionResult getProgramlarEkod(string EnstituKod)
        {
            var bolm = Management.cmbGetAktifProgramlar(EnstituKod, true);
            return bolm.Select(s => new { s.Value, s.Caption }).toJsonResult();
        }
        public ActionResult getMessage(MmMessage model)
        {
            return View(model);
        }

        public ActionResult getMailContent(mdlMailMainContent model)
        {

            return View(model);
        }
        public ActionResult getMailTableContent(mailTableContent model)
        {
            return View(model);
        }

        public ActionResult getEkAciklamaContent(ekAciklamaContent model)
        {

            return View(model);
        }

        public ActionResult getAnket(kmAnketlerCevap model)
        {
            return View();
        }


        public ActionResult SetAnket(kmAnketlerCevap kModel)
        {
            var mMessage = new MmMessage();
            mMessage.MessageType = Msgtype.Error;
            mMessage.IsSuccess = false;
            mMessage.Title = "Anket bilgisi oluşturulamadı. Lütfen aşağıdaki uyarıları inceleyiniz.";
            var qAnketSoruID = kModel.AnketSoruID.Select((s, inx) => new { AnketSoruID = s, inx = inx }).ToList();
            var qAnketSoruSecenekID = kModel.AnketSoruSecenekID.Select((s, inx) => new { AnketSoruSecenekID = s, inx = inx }).ToList();
            var qAnketSoruTabloVeri1 = kModel.TabloVeri1.Select((s, inx) => new { TabloVeri1 = s, inx = inx }).ToList();
            var qAnketSoruTabloVeri2 = kModel.TabloVeri2.Select((s, inx) => new { TabloVeri2 = s, inx = inx }).ToList();
            var qAnketSoruTabloVeri3 = kModel.TabloVeri3.Select((s, inx) => new { TabloVeri3 = s, inx = inx }).ToList();
            var qAnketSoruTabloVeri4 = kModel.TabloVeri4.Select((s, inx) => new { TabloVeri4 = s, inx = inx }).ToList();
            var qAnketSoruTabloVeri5 = kModel.TabloVeri5.Select((s, inx) => new { TabloVeri5 = s, inx = inx }).ToList();
            var qAnketSoruSecenekAciklama = kModel.AnketSoruSecenekAciklama.Select((s, inx) => new { AnketSoruSecenekAciklama = s, inx = inx }).ToList();

            var qGroup = new List<AnketPostGroupModel>();

            #region grupla
            qGroup = (from s in qAnketSoruID
                      join ss in qAnketSoruSecenekID on s.inx equals ss.inx
                      join ac in qAnketSoruSecenekAciklama on s.inx equals ac.inx
                      join bss in db.AnketSorus on new { s.AnketSoruID, kModel.AnketID } equals new { bss.AnketSoruID, bss.AnketID }
                      join bssx in db.AnketSoruSeceneks on new { bss.AnketSoruID, ss.AnketSoruSecenekID } equals new { bssx.AnketSoruID, AnketSoruSecenekID = (int?)bssx.AnketSoruSecenekID } into def1
                      from qs in def1.DefaultIfEmpty()
                      join v1 in qAnketSoruTabloVeri1 on s.inx equals v1.inx
                      join v2 in qAnketSoruTabloVeri2 on s.inx equals v2.inx
                      join v3 in qAnketSoruTabloVeri3 on s.inx equals v3.inx
                      join v4 in qAnketSoruTabloVeri4 on s.inx equals v4.inx
                      join v5 in qAnketSoruTabloVeri5 on s.inx equals v5.inx
                      group new
                      {
                          ss.AnketSoruSecenekID,
                          ac.AnketSoruSecenekAciklama,
                          bss.IsTabloVeriGirisi,
                          SoruCevabiYanlis = qs == null,
                          IsEkAciklamaGir = qs != null ? qs.IsEkAciklamaGir : false,
                          v1.TabloVeri1,
                          v2.TabloVeri2,
                          v3.TabloVeri3,
                          v4.TabloVeri4,
                          v5.TabloVeri5,
                      }
                     by new
                     {
                         bss.SiraNo,
                         s.AnketSoruID,
                         bss.AnketID,
                         SecenekCount = bss.AnketSoruSeceneks.Count,
                         bss.IsTabloVeriGirisi,
                         bss.IsTabloVeriMaxSatir,
                     } into g1
                      select new AnketPostGroupModel
                      {
                          inx = g1.Key.SiraNo,
                          AnketID = g1.Key.AnketID,
                          AnketSoruID = g1.Key.AnketSoruID,
                          IsTabloVeriGirisi = g1.Key.IsTabloVeriGirisi,
                          IsTabloVeriMaxSatir = g1.Key.IsTabloVeriMaxSatir,
                          AnketSoruSecenekID = g1.Select(s => s.AnketSoruSecenekID).FirstOrDefault(),
                          SecenekCount = g1.Key.SecenekCount,
                          AnketSoruSecenekAciklama = g1.Select(s => s.AnketSoruSecenekAciklama).FirstOrDefault(),
                          SoruCevabiYanlis = g1.Select(s => s.SoruCevabiYanlis).FirstOrDefault(),
                          IsEkAciklamaGir = g1.Select(s => s.IsEkAciklamaGir).FirstOrDefault(),
                          TabloVerileri = g1.Key.IsTabloVeriGirisi ? g1.Where(p => p.IsTabloVeriGirisi).Select(s => new AnketTabloVeriGirisModel
                          {
                              TabloVeri1 = s.TabloVeri1,
                              TabloVeri2 = s.TabloVeri2,
                              TabloVeri3 = s.TabloVeri3,
                              TabloVeri4 = s.TabloVeri4,
                              TabloVeri5 = s.TabloVeri5,
                          }).ToList() : new List<AnketTabloVeriGirisModel>(),
                      }).OrderBy(o => o.inx).ToList();
            #endregion



            var Hatalilar = new List<int>();




            if (qGroup.Any(p => !p.IsTabloVeriGirisi && p.AnketSoruSecenekID.HasValue == false))
            {
                var data = qGroup.Where(p => !p.IsTabloVeriGirisi && p.AnketSoruSecenekID.HasValue == false).ToList();
                mMessage.Messages.Add("Lütfen cevaplamadığınız anket sorularını cevaplayınız.");


                foreach (var item in data)
                {
                    mMessage.Messages.Add(item.inx + " Numaralı soru cevaplanmadı.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnketSoruSecenekID_" + item.AnketSoruID });
                    Hatalilar.Add(item.AnketSoruID);
                }
            }
            else if (qGroup.Any(p => !p.IsTabloVeriGirisi && p.AnketSoruSecenekID.HasValue && p.SoruCevabiYanlis))
            {
                var data = qGroup.Where(p => !p.IsTabloVeriGirisi && p.AnketSoruSecenekID.HasValue == false && p.SoruCevabiYanlis).ToList();
                mMessage.Messages.Add("Anket sorularına verdiğiniz cevaplardan bazıları sistemde bulunamadı!");
                foreach (var item in data)
                {

                    mMessage.Messages.Add(item.inx + " Numaralı sorunun cevabı hatalı");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnketSoruSecenekID_" + item.AnketSoruID });
                    Hatalilar.Add(item.AnketSoruID);
                }
            }
            else if (qGroup.Any(p => !p.IsTabloVeriGirisi && p.IsEkAciklamaGir && p.AnketSoruSecenekAciklama.IsNullOrWhiteSpace()))
            {
                var data = qGroup.Where(p => !p.IsTabloVeriGirisi && p.IsEkAciklamaGir && p.AnketSoruSecenekAciklama.IsNullOrWhiteSpace()).OrderBy(o => o.inx).ToList();
                foreach (var item in data)
                {

                    mMessage.Messages.Add(item.inx + " Numaralı sorunu için lütfen açıklama giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnketSoruSecenekID_" + item.AnketSoruID });
                    Hatalilar.Add(item.AnketSoruID);
                }
            }
            if (qGroup.Any(p => p.IsTabloVeriGirisi))
            {
                var data = qGroup.Where(p => p.IsTabloVeriGirisi).ToList();

                foreach (var item in data)
                {
                    foreach (var item2 in item.TabloVerileri)
                    {
                        var dctVal = new Dictionary<string, string>();
                        foreach (var item3 in item2.GetType().GetProperties())
                        {
                            dctVal.Add(item3.Name, item3.GetValue(item2).toStrObjEmptString());
                        }
                        if (dctVal.Take(item.SecenekCount).Any(p => !p.Value.IsNullOrWhiteSpace()) && dctVal.Take(item.SecenekCount).Any(p => p.Value.IsNullOrWhiteSpace()))
                        {
                            mMessage.Messages.Add(item.inx + " Numaralı sorunu için lütfen açıklama giriniz.");
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnketSoruSecenekID_" + item.AnketSoruID });
                            Hatalilar.Add(item.AnketSoruID);
                        }

                        if (dctVal.Take(item.SecenekCount).Count(p => !p.Value.IsNullOrWhiteSpace()) == item.SecenekCount)
                        {
                            item2.InsertTablerRow = true;
                        }
                    }

                }
            }
            mMessage.Messages = mMessage.Messages.ToList();
            if (mMessage.Messages.Count == 0)
            {


                var lstData = new List<AnketCevaplari>();
                foreach (var item in qGroup)
                {
                    if (item.IsTabloVeriGirisi)
                    {
                        foreach (var item2 in item.TabloVerileri.Where(p => p.InsertTablerRow))
                        {
                            lstData.Add(new AnketCevaplari
                            {
                                Tarih = DateTime.Now,
                                AnketID = item.AnketID,
                                AnketSoruID = item.AnketSoruID,
                                AnketSoruSecenekID = null,
                                EkAciklama = "",
                                TabloVeri1 = item2.TabloVeri1,
                                TabloVeri2 = item2.TabloVeri2,
                                TabloVeri3 = item2.TabloVeri3,
                                TabloVeri4 = item2.TabloVeri4,
                                TabloVeri5 = item2.TabloVeri5,
                            });
                        }

                    }
                    else
                    {
                        lstData.Add(new AnketCevaplari
                        {
                            Tarih = DateTime.Now,
                            AnketID = item.AnketID,
                            AnketSoruID = item.AnketSoruID,
                            AnketSoruSecenekID = item.AnketSoruSecenekID.Value,
                            EkAciklama = (item.IsEkAciklamaGir ? item.AnketSoruSecenekAciklama.Trim() : "")
                        });
                    }
                }
                if (kModel.AnketTipID == 1)
                {
                    if (!UserIdentity.Current.Informations.Where(p => p.Key == "LUBAnket").Any()) UserIdentity.Current.Informations.Add("LUBAnket", lstData);
                    else UserIdentity.Current.Informations["LUBAnket"] = lstData;
                }
                else if (kModel.AnketTipID == 2)
                {
                    if (!UserIdentity.Current.Informations.Where(p => p.Key == "BTAnket").Any()) UserIdentity.Current.Informations.Add("BTAnket", lstData);
                    else UserIdentity.Current.Informations["BTAnket"] = lstData;
                }
                else if (kModel.AnketTipID == 3)
                {
                    var nRwID = new Guid(kModel.RowID);
                    var basvuru = db.Basvurulars.Where(p => p.RowID == nRwID).FirstOrDefault();
                    if (basvuru != null && basvuru.BasvuruSurec.KayitOlmayanlarAnketID.HasValue && !basvuru.AnketCevaplaris.Where(p => p.AnketID == basvuru.BasvuruSurec.KayitOlmayanlarAnketID).Any())
                    {
                        foreach (var item in lstData)
                        {
                            item.Tarih = DateTime.Now;
                            basvuru.AnketCevaplaris.Add(item);
                        }
                        db.SaveChanges();
                    }
                }
                else if (kModel.AnketTipID == 4)
                {
                    var nRwID = new Guid(kModel.RowID);
                    var basvuru = db.MezuniyetBasvurularis.Where(p => p.RowID == nRwID).FirstOrDefault();
                    if (basvuru != null && basvuru.MezuniyetSureci.AnketID.HasValue && !basvuru.AnketCevaplaris.Where(p => p.AnketID == basvuru.MezuniyetSureci.AnketID).Any())
                    {
                        foreach (var item in lstData)
                        {
                            item.Tarih = DateTime.Now;
                            basvuru.AnketCevaplaris.Add(item);
                        }
                        db.SaveChanges();
                    }
                }
                mMessage.IsSuccess = true;
                mMessage.MessageType = Msgtype.Success;
                //mMessage.Title = "Anket bilgileri doldurduğunuz için teşekkür ederiz.";

            }
            var Hatasizlar = qGroup.Where(p => Hatalilar.Contains(p.AnketSoruID) == false).Select(s => s.AnketSoruID).ToList();
            foreach (var item in Hatasizlar)
            {
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AnketSoruSecenekID_" + item });

            }
            return mMessage.toJsonResult();

        }

        public ActionResult GetAnketCevap(frAnketDetay model)
        {
            return View();
        }
        [Authorize]
        public ActionResult dAktifSinavlar(tercihSTKontrolModel model)
        {
            var qOgrenimTipKods = model.OgrenimTipKods.Select((s, inx) => new { s = s, Index = inx }).ToList();
            var qProgramKods = model.ProgramKods.Select((s, inx) => new { s = s, Index = inx }).ToList();
            var qIngilizces = model.Ingilizces.Select((s, inx) => new { s = s, Index = inx }).ToList();
            var qtercihler = (from s in qOgrenimTipKods
                              join qp in qProgramKods on s.Index equals qp.Index
                              join qi in qIngilizces on s.Index equals qi.Index
                              select new CmbMultyTypeDto { Value = s.s, ValueB = qi.s, ValueS2 = qp.s }).ToList();
            var data = Management.cmbGetdAktifSinavlar(qtercihler, model.BasvuruSurecID, model.SinavTipGrupID, true);
            return data.Select(s => new { s.Value, s.Caption }).toJsonResult();
        }
        [Authorize]
        public ActionResult getSalonlar(string EnstituKod, int SRTalepTipID, int? TalepYapanID = null, int? id = null)
        {
            var cmbmld = Management.cmbSalonlar(EnstituKod, SRTalepTipID, true);
            var ttip = db.SRTalepTipleris.Where(p => p.SRTalepTipID == SRTalepTipID).First();

            var kotaBilgi = new CmbMultyTypeDto();
            kotaBilgi.ValueB = true;
            if (TalepYapanID.HasValue)
            {
                kotaBilgi = Management.SRkotaKontrol(TalepYapanID.Value, SRTalepTipID, id);
            }
            return new { IsTezSinavi = ttip.IsTezSinavi, kotaBilgi = kotaBilgi, data = cmbmld.Select(s => new { s.Value, s.Caption }) }.toJsonResult();
        }
        [Authorize]
        public ActionResult getGunler(int SRSalonID, int SRTalepTipID, DateTime Tarih, DateTime? Tarih2 = null, int? SROzelTanimID = null)
        {

            var ttip = db.SRTalepTipleris.Where(p => p.SRTalepTipID == SRTalepTipID).First();

            var gunler = db.SRSaatlers.Where(p => p.SRSalonID == SRSalonID).Select(s => s.HaftaGunID).Distinct();

            var gunL = db.HaftaGunleris.Where(p => gunler.Contains(p.HaftaGunID)).Select(s => new CmbIntDto { Value = s.HaftaGunID, Caption = s.HaftaGunAdi }).ToList();

            for (DateTime date = Tarih; date <= Tarih2.Value; date = date.AddDays(1.0))
            {
                var nTarih = date.ToShortDateString().ToDate().Value;
                var dofW = nTarih.DayOfWeek.ToString("d").ToInt().Value;

                var salon = db.SRSalonlars.Where(p => p.SRSalonID == SRSalonID).First();

                var haftaGunu = db.HaftaGunleris.Where(p => p.HaftaGunID == dofW).First();
                var ResmiTatilDegisen = db.SROzelTanimlars.Where(p => p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilDegisen && p.BasTarih.Value <= nTarih && p.BitTarih >= nTarih).FirstOrDefault();
                var ResmiTatilSabit = db.SROzelTanimlars.Where(p => p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilSabit && p.Ay.Value == nTarih.Month && p.Gun == nTarih.Day).FirstOrDefault();
                var Rezervasyonlar = db.SROzelTanimlars.Where(p => p.SROzelTanimID != (SROzelTanimID.HasValue ? SROzelTanimID.Value : 0) && p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.Rezervasyon && p.SRSalonID == SRSalonID && p.Tarih == nTarih).ToList();
                var Rezerve = db.SROzelTanimlars.Where(p => p.SROzelTanimID != (SROzelTanimID.HasValue ? SROzelTanimID.Value : 0) && p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.Rezerve && p.SRSalonID == SRSalonID && p.Tarih == nTarih).ToList();
                var tTip = db.SRTalepTipleris.Where(p => p.SRTalepTipID == SRTalepTipID).First();
                bool IsSuccess = true;
                var qTalepEslesen = db.SRTalepleris.Where(a => a.SRSalonID == SRSalonID && a.Tarih == nTarih).Any(p => p.SRDurumID == SRTalepDurum.Onaylandı || p.SRDurumID == SRTalepDurum.TalepEdildi);
                if (qTalepEslesen)
                {
                    IsSuccess = false;
                }
                else if (ResmiTatilDegisen != null || ResmiTatilSabit != null)
                {
                    IsSuccess = false;
                }
                else if (Rezerve.Count > 0)
                {
                    foreach (var itemRO in Rezerve.Where(p => p.SROzelTanimGunlers.Any(a => a.HaftaGunID == dofW)))
                    {
                        if (Rezerve.Any(p => p.SROzelTanimGunlers.Any(a => a.HaftaGunID == dofW)))
                        {
                            IsSuccess = false;
                        }
                    }
                }
                else if (Rezervasyonlar.Count > 0)
                {
                    IsSuccess = false;

                }
                var sgun = gunL.Where(p => p.Value == dofW).FirstOrDefault();
                if (sgun != null && IsSuccess == false) gunL.Remove(sgun);
            }


            return View(gunL);
        }

        [Authorize]
        public ActionResult getSaatList(int SRSalonID, bool IsPopupFrame, int SRTalepTipID, DateTime Tarih, int? SRTalepID, string MzRowID = null)
        {

            DateTime? MinTarih = null;
            if (MzRowID.IsNullOrWhiteSpace() == false)
            {
                var RwId = new Guid(MzRowID);
                MinTarih = db.MezuniyetBasvurularis.Where(p => p.RowID == RwId).Select(s => s.EYKTarihi).FirstOrDefault();
            }
            var data = Management.getSalonBosSaatler(SRSalonID, SRTalepTipID, Tarih, SRTalepID, null, MinTarih);
            data.IsPopupFrame = IsPopupFrame;
            var HCB = Management.RenderPartialView("Ajax", "getSaatlerView", data);
            return new { Deger = HCB }.toJsonResult();
        }
        [Authorize]
        public ActionResult getSaatlerView(SRSalonSaatlerModel model)
        {
            return View(model);
        }

        [Authorize]
        public ActionResult getJuriMailContent(mailSRjuriModel mdl)
        {
            return View(mdl);
        }
        [Authorize]

        public ActionResult getJuriEkleKontrol(string JuriAdi, string Email)
        {
            var mmMessage = new MmMessage();
            mmMessage.Title = "Jüri bilgisi eklenemedi!";

            if (JuriAdi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Jüri adı boş bırakılamaz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_JuriAdi" });
            }
            if (Email.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("E-Posta Bilgisi Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_Email" });
            }
            else if (Email.ToIsValidEmail())
            {
                mmMessage.Messages.Add("E-Posta Formatı Uygun Değildir.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_Email" });
            }
            var strView = "";
            if (mmMessage.Messages.Count > 0)
            {
                mmMessage.IsSuccess = mmMessage.Messages.Count == 0;
                mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
                strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            }
            else
            {
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_MulakatSinavTurID" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_JuriAdi" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_Email" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_YerAdi" });
            }

            return Json(new
            {
                IsSuccess = mmMessage.Messages.Count == 0 ? true : false,
                Messages = strView,
                mmMessage = mmMessage
            }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = RoleNames.MailGonder)]
        [ValidateInput(false)]
        public ActionResult MailGonder(KmMailGonder model, string EKD = "")
        {

            if (model.BasvuruSurecID.HasValue)
            {
                model.EnstituKod = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == model.BasvuruSurecID.Value).First().EnstituKod;
                ViewBag.IsBolumOrOgrenci = new SelectList(Management.cmbBolumOrOgrenci(false), "Value", "Caption", model.IsBolumOrOgrenci);
                var CmbOgrenimTipleris = Management.cmbGetAktifOgrenimTipleri(model.BasvuruSurecID.Value, false);
                ViewBag.OgrenimTipKods = new SelectList(CmbOgrenimTipleris, "Value", "Caption", model.OgrenimTipKods);

                ViewBag.ProgramKods = new SelectList(new List<CmbBoolDto>(), "Value", "Caption", model.ProgramKods);
                ViewBag.BasvuruDurumID = new SelectList(Management.cmbBasvuruDurumListe(false), "Value", "Caption");
                ViewBag.KayitDurumIDs = new SelectList(Management.cmbKayitDurum(), "Value", "Caption");
                ViewBag.MulakatSonucTipIDs = new SelectList(Management.cmbMulakatSonucTip(false), "Value", "Caption");

            }
            else
            {
                model.EnstituKod = Management.getSelectedEnstitu(EKD);
            }
            var SecilenKullaniciIDs = model.SecilenAlicilars.Where(p => p.IsNumber()).Select(s => s.ToInt().Value).ToList();
            var SecilenEmails = model.SecilenAlicilars.Where(p => p.IsNumber() == false).Select(s => new CmbStringDto { Value = s, Caption = s }).ToList();

            if (!model.IsTopluMail)
            {
                if (SecilenKullaniciIDs.Any()) model.EMails = db.Kullanicilars.Where(p => SecilenKullaniciIDs.Contains(p.KullaniciID)).Select(s => new CmbStringDto { Value = (s.KullaniciID + ""), Caption = s.EMail }).ToList();
                if (SecilenEmails.Any()) model.EMails.AddRange(SecilenEmails);
            }
            if (!model.BasvuruRowID.IsNullOrWhiteSpace())
            {
                var Basvuru = db.Basvurulars.Where(p => p.RowID == new Guid(model.BasvuruRowID)).FirstOrDefault();
                if (Basvuru != null) model.EMails.Add(new CmbStringDto { Value = Basvuru.EMail.Trim(), Caption = Basvuru.EMail.Trim() });
                else model.BasvuruRowID = null;
            }

            ViewBag.MailSablonlariID = new SelectList(Management.cmbMailSablonlari(model.EnstituKod, true, false), "Value", "Caption");

            ViewBag.MmMessage = new MmMessage();
            return View(model);
        }




        [HttpPost]
        [ValidateInput(false)]
        [Authorize(Roles = RoleNames.MailGonder)]
        public ActionResult MailGonderPost(KmMailGonder model, List<HttpPostedFileBase> DosyaEki, List<string> DosyaEkiAdi, List<string> EkYolu, string EKD)
        {
            var mmMessage = new MmMessage();
            mmMessage.Title = "Mail gönderme işlemi";
            string _EnstituKod = Management.getSelectedEnstitu(EKD);

            DosyaEki = DosyaEki == null ? new List<HttpPostedFileBase>() : DosyaEki;
            DosyaEkiAdi = DosyaEkiAdi == null ? new List<string>() : DosyaEkiAdi;
            EkYolu = EkYolu == null ? new List<string>() : EkYolu;

            var secilenAlicilar = new List<string>();
            if (model.Alici.IsNullOrWhiteSpace() == false) model.Alici.Split(',').ToList().ForEach((itm) => { secilenAlicilar.Add(itm); });
            if (model.Aciklama.IsNullOrWhiteSpace() == false)
            {
                var cevapA = "";
                var geriDonusLink = "";
                if (model.MesajID.HasValue)
                {
                    var enstitu = db.Enstitulers.Where(p => p.EnstituKod == _EnstituKod).First();
                    var mesaj = db.Mesajlars.Where(p => p.MesajID == model.MesajID.Value).First();
                    if (mesaj.Mesajlar2 != null) mesaj = mesaj.Mesajlar2;
                    model.MesajID = mesaj.MesajID;
                    var cevapAdresi = enstitu.SistemErisimAdresi + "/Home/Index?MesajGroupID=" + mesaj.GroupID;
                    cevapA = "<div style='color:#A9A9A9;'>" + mesaj.AciklamaHtml + "</div>";
                    geriDonusLink = "<a target='_blank' href='" + cevapAdresi + "' style='color:green;font-size:12pt;'> >> Bu maile sistem üzerinden cevap yazmak için lütfen tıklayınız << </a>";
                }
                var nAck = "<br/><p><span style='color:red'>Not: Cevaplama İşlemini Lütfen Sistem Üzerinden Yapınız. Bu mail sistem maili olduğundan yazılan cevaplar okunmamaktadır.</span><br/><span style='color:red'>------------------------------<wbr>------------------------------<wbr>------------------------------<wbr>------------------------------<wbr>------------------</span></p> " + cevapA;

                model.AciklamaHtml += geriDonusLink + nAck;
            }
            if (model.BasvuruSurecID.HasValue)
            {
                if (model.IsBolumOrOgrenci)
                {
                    var Surec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == model.BasvuruSurecID.Value).First();
                    //Yetkili Kullanıcılara MAİLGÖNDER
                    var rollers = new List<string>();
                    if (Surec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) rollers = new List<string>() { RoleNames.MulakatSureci, RoleNames.MulakatKayıt };
                    else if (Surec.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru) rollers = new List<string>() { RoleNames.YGMulakatSureci, RoleNames.YGMulakatKayıt };

                    var qBolumlerEmailList = db.KullaniciProgramlaris.Where(p => (p.Kullanicilar.Rollers.Any(a => rollers.Contains(a.RolAdi))
                                                                                || p.Kullanicilar.YetkiGruplari.YetkiGrupRolleris.Any(a => rollers.Contains(a.Roller.RolAdi))) &&
                                                                                model.ProgramKods.Contains(p.ProgramKod)).Select(s => s.Kullanicilar.EMail).ToList();

                    var Kullanicilar = db.KullaniciProgramlaris.Where(p => (p.Kullanicilar.Rollers.Any(a => rollers.Contains(a.RolAdi))
                                                                                  || p.Kullanicilar.YetkiGruplari.YetkiGrupRolleris.Any(a => rollers.Contains(a.Roller.RolAdi))) &&
                                                                                  model.ProgramKods.Contains(p.ProgramKod)).ToList().Select(s => s.Kullanicilar).Distinct().Select(s => new
                                                                                  {
                                                                                      s.Ad,
                                                                                      s.Soyad,
                                                                                      s.YetkiGruplari.YetkiGrupAdi,
                                                                                      ProgramYetkileri = s.KullaniciProgramlaris.ToList()
                                                                                  }).ToList();

                    secilenAlicilar.AddRange(qBolumlerEmailList);
                }
                else
                {
                    var qogrenciEmailList = db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruSurecID == model.BasvuruSurecID && model.ProgramKods.Contains(p.ProgramKod)).AsQueryable();

                    if (model.OgrenimTipKods.Count > 0) qogrenciEmailList = qogrenciEmailList.Where(p => model.OgrenimTipKods.Contains(p.OgrenimTipKod));
                    if (model.KayitDurumIDs.Count > 0) qogrenciEmailList = qogrenciEmailList.Where(p => p.MulakatSonuclaris.Any(a => model.KayitDurumIDs.Contains(a.KayitDurumID)));
                    if (model.MulakatSonucTipIDs.Count > 0) qogrenciEmailList = qogrenciEmailList.Where(p => p.MulakatSonuclaris.Any(a => model.MulakatSonucTipIDs.Contains(a.MulakatSonucTipID)));
                    var data = qogrenciEmailList.Select(s => new { s.Basvurular.EMail, kEmail = s.Basvurular.EMail }).ToList();
                    var tempL = new List<string>();
                    tempL.AddRange(data.Select(s => s.EMail));
                    tempL.AddRange(data.Select(s => s.kEmail));
                    secilenAlicilar.AddRange(tempL.Distinct());
                }
            }
            if (model.IsTopluMail && !model.SecilenTopluAlicilar.IsNullOrWhiteSpace())
            {
                secilenAlicilar.AddRange(model.SecilenTopluAlicilar.Split(',').ToList());
            }

            var qDosyaEkAdi = DosyaEkiAdi.Select((s, inx) => new { s, inx }).ToList();
            var qDosyaEki = DosyaEki.Select((s, inx) => new { s, inx }).ToList();
            var qDosyaEkYolu = EkYolu.Select((s, inx) => new { s, inx }).ToList();

            var qDosyalar = (from EkGirilenAd in qDosyaEkAdi
                             join EklenenEk in qDosyaEki on EkGirilenAd.inx equals EklenenEk.inx
                             join VarolanEkYolu in qDosyaEkYolu on EkGirilenAd.inx equals VarolanEkYolu.inx
                             select new
                             {
                                 EkGirilenAd.inx,
                                 Dosya = EklenenEk.s,
                                 FExtension = EklenenEk.s != null ? (EkGirilenAd.s + EklenenEk.s.FileName.GetFileExtension()) : (EkGirilenAd.s),
                                 DosyaYolu = EklenenEk.s != null ? ("/MailDosyalari/" + EkGirilenAd.s.ToFileNameAddGuid(EklenenEk.s.FileName.GetFileExtension())) : (VarolanEkYolu.s)
                             }).ToList();
            var kModel = new GonderilenMailler();
            #region Kontrol 
            if (!model.BasvuruRowID.IsNullOrWhiteSpace())
            {
                var RowID = new Guid(model.BasvuruRowID);
                var Basvuru = db.Basvurulars.Where(p => p.RowID == RowID).FirstOrDefault();
                if (Basvuru != null)
                {
                    secilenAlicilar.Add(Basvuru.EMail);
                    kModel.BasvuruID = Basvuru.BasvuruID;
                }
            }
            if (secilenAlicilar.Count == 0)
            {
                string msg = "Mail Gönderilecek Hiçbir Alıcı Belirlenemedi!";
                mmMessage.Messages.Add(msg);
            }

            if (model.Konu.IsNullOrWhiteSpace())
            {
                string msg = "Konu Giriniz.";
                mmMessage.Messages.Add(msg);
            }

            if (model.Aciklama.IsNullOrWhiteSpace() && model.AciklamaHtml.IsNullOrWhiteSpace())
            {
                string msg = "İçerik Giriniz.";
                mmMessage.Messages.Add(msg);
            }
            #endregion

            kModel.Tarih = DateTime.Now;
            kModel.EnstituKod = Management.getSelectedEnstitu(EKD);
            if (mmMessage.Messages.Count == 0)
            {

                kModel.EnstituKod = _EnstituKod;
                kModel.MesajID = model.MesajID;
                kModel.IslemTarihi = DateTime.Now;
                kModel.Konu = model.Konu;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.Aciklama = model.Aciklama ?? "";
                kModel.AciklamaHtml = model.AciklamaHtml ?? "";

                var eklenen = db.GonderilenMaillers.Add(kModel);

                foreach (var item in qDosyalar)
                {
                    if (item.Dosya != null)
                        item.Dosya.SaveAs(Server.MapPath("~" + item.DosyaYolu));
                    eklenen.GonderilenMailEkleris.Add(new GonderilenMailEkleri
                    {
                        EkAdi = item.FExtension,
                        EkDosyaYolu = item.DosyaYolu
                    });
                }
                if (model.MesajID.HasValue)
                {
                    var mesaj = db.Mesajlars.Where(p => p.MesajID == model.MesajID.Value).FirstOrDefault();
                    if (mesaj != null)
                    {
                        mesaj.IsAktif = true;


                    }
                }


                var mailList = new List<GonderilenMailKullanicilar>();
                var tari = DateTime.Now;
                secilenAlicilar = secilenAlicilar.Distinct().ToList();
                if (secilenAlicilar.Count > 0)
                {
                    var qscIDs = secilenAlicilar.Where(p => p.IsNumber()).Select(s => s.ToInt().Value).ToList();
                    var qscMails = secilenAlicilar.Where(p => p.IsNumber() == false).ToList();
                    var dataqx = (from s in db.Kullanicilars
                                  where qscIDs.Contains(s.KullaniciID)
                                  select new
                                  {
                                      Email = s.EMail,
                                      GonderilenMailID = eklenen.GonderilenMailID,
                                      KullaniciID = s.KullaniciID
                                  }).ToList();
                    foreach (var item in dataqx)
                    {
                        mailList.Add(new GonderilenMailKullanicilar
                        {

                            Email = item.Email,
                            GonderilenMailID = item.GonderilenMailID,
                            KullaniciID = item.KullaniciID
                        });
                    }
                    foreach (var item in qscMails)
                    {
                        mailList.Add(new GonderilenMailKullanicilar
                        {

                            Email = item,
                            GonderilenMailID = eklenen.GonderilenMailID,
                            KullaniciID = null
                        });
                    }

                }
                eklenen.Gonderildi = true;
                mailList = mailList.Distinct().ToList();
                eklenen.GonderilenMailKullanicilars = mailList;

                db.SaveChanges();
                if (model.MesajID.HasValue)
                {
                    var mesaj = db.Mesajlars.Where(p => p.MesajID == model.MesajID).First();
                    mesaj.IsAktif = true;
                    if (mesaj.UstMesajID.HasValue)
                    {
                        var UstMesaj = mesaj.Mesajlar2;
                        UstMesaj.ToplamEkSayisi = (UstMesaj.MesajEkleris.Count + UstMesaj.Mesajlar1.Sum(s => s.MesajEkleris.Count) + UstMesaj.GonderilenMaillers.Sum(s => s.GonderilenMailEkleris.Count));
                        UstMesaj.SonMesajTarihi = mesaj.Tarih;
                    }
                    else
                    {
                        mesaj.SonMesajTarihi = mesaj.Mesajlar1.Any() ? mesaj.Mesajlar1.OrderByDescending(s2 => s2.Tarih).FirstOrDefault().Tarih : mesaj.Tarih;
                        mesaj.ToplamEkSayisi = (mesaj.MesajEkleris.Count + mesaj.Mesajlar1.Sum(s => s.MesajEkleris.Count) + mesaj.GonderilenMaillers.Sum(s => s.GonderilenMailEkleris.Count));
                    }
                }
                db.SaveChanges();
                var attach = new List<Attachment>();
                foreach (var item in qDosyalar)
                {
                    var ekTamYol = Server.MapPath("~" + item.DosyaYolu);
                    if (System.IO.File.Exists(ekTamYol))
                    {
                        var FExtension = Path.GetExtension(ekTamYol);
                        attach.Add(new Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)), item.FExtension.ToSetNameFileExtension(FExtension), MediaTypeNames.Application.Octet));
                    }
                    else Management.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + item.FExtension + " <br/>Dosya Yolu:" + ekTamYol, "Ajax/MailGonderPost", BilgiTipi.Hata);
                }


                var gidecekler = mailList.Select(s => s.Email).ToList();
                var dct = new Dictionary<int, List<MailSendList>>();
                model.IsToOrBCC = model.BasvuruSurecID.HasValue ? false : model.IsToOrBCC;
                int inx = 0;
                while (gidecekler.Count > 500)
                {
                    dct.Add(inx, gidecekler.Take(500).Select(s => new MailSendList { EMail = s, ToOrBcc = model.IsToOrBCC }).ToList());
                    gidecekler = gidecekler.Skip(500).ToList();
                    inx++;
                }
                inx++;
                dct.Add(inx, gidecekler.Select(s => new MailSendList { EMail = s, ToOrBcc = model.IsToOrBCC }).ToList());

                foreach (var item in dct)
                {
                    var excpt = MailManager.sendMailRetVal(_EnstituKod, kModel.Konu, kModel.AciklamaHtml, item.Value, attach);
                    if (excpt == null)
                    {
                        mmMessage.Messages.Add("Mail gönderildi!");
                        mmMessage.IsSuccess = true;
                        mmMessage.MessageType = Msgtype.Success;
                    }
                    else
                    {
                        var msgerr = excpt.ToExceptionMessage().Replace("\r\n", "<br/>");
                        mmMessage.Messages.Add("Mail gönderilirken bir hata oluştu! <br/>Hata:" + msgerr);
                        mmMessage.IsSuccess = false;
                        mmMessage.MessageType = Msgtype.Error;
                        try
                        {
                            db.GonderilenMaillers.Remove(eklenen);
                            //db.SaveChanges();
                            foreach (var item2 in qDosyalar)
                            {
                                if (System.IO.File.Exists(Server.MapPath("~" + item2.DosyaYolu)))
                                    System.IO.File.Delete(Server.MapPath("~" + item2.DosyaYolu));
                            }
                        }
                        catch (Exception ex)
                        {
                            Management.SistemBilgisiKaydet(ex.ToExceptionMessage(), "Ajax/MailGonderPost<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.Hata);
                        }
                    }
                }
            }
            else
            {
                mmMessage.IsSuccess = false;
                mmMessage.MessageType = Msgtype.Warning;
            }

            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            //return Content(strView, MediaTypeNames.Text.Html);
            return Json(new { success = mmMessage.IsSuccess, responseText = strView }, JsonRequestBehavior.AllowGet);
            //return new JsonResult { Data = new { IsSuccess = mmMessage.IsSuccess, Message = strView } };

        }


        [Authorize(Roles = RoleNames.MailGonder)]
        public ActionResult getTumMailListesi(string term)
        {
            var qKullanicilar = (from k in db.Kullanicilars
                                 orderby k.Ad, k.Soyad
                                 where k.EMail.Contains("@") && (k.EMail.StartsWith(term) || (k.Ad + " " + k.Soyad).Contains(term))
                                 select new
                                 {
                                     id = k.KullaniciID,
                                     AdSoyad = k.Ad + " " + k.Soyad,
                                     text = k.EMail,
                                     Images = k.ResimAdi

                                 }).Take(25).ToList();
            var kul = qKullanicilar.Select(k => new mailListModel
            {
                id = k.id.ToString(),
                AdSoyad = k.AdSoyad,
                text = k.text,
                Images = k.Images.toKullaniciResim()

            }).ToList();
            if (kul.Count == 0)
            {
                var lst = new List<mailListModel>();
                if (!term.ToIsValidEmail())
                {
                    lst.Add(new mailListModel { id = term, AdSoyad = term, text = term, Images = "".toKullaniciResim() });
                }
                return lst.toJsonResult();
            }

            else return kul.toJsonResult();
        }

        [Authorize]
        public ActionResult getProgramListesi(string term)
        {
            var Programlars = (from p in db.Programlars
                               join prl in db.Programlars on p.ProgramKod equals prl.ProgramKod
                               join abl in db.AnabilimDallaris on p.AnabilimDaliKod equals abl.AnabilimDaliKod
                               orderby prl.ProgramAdi
                               where prl.ProgramAdi.Contains(term)
                               select new
                               {
                                   id = p.ProgramKod,
                                   AnabilimDaliAdi = abl.AnabilimDaliAdi,
                                   ProgramAdi = prl.ProgramAdi + " [" + prl.ProgramKod + "]",
                                   text = p.ProgramKod,

                               }).Take(60).ToList();


            return Programlars.toJsonResult();
        }

        [Authorize]
        public ActionResult BasvuruGonderilenMailler(string RowID)
        {
            var Basvuru = db.Basvurulars.Where(p => p.RowID == new Guid(RowID)).First();

            return View(Basvuru);
        }

        [Authorize]
        public ActionResult getSablonlar(int MailSablonlariID)
        {
            var KulID = UserIdentity.Current.Id;
            var sbl = db.MailSablonlaris.Where(p => p.MailSablonlariID == MailSablonlariID).Select(s => new { s.SablonAdi, s.Sablon, s.SablonHtml, MailSablonlariEkleri = s.MailSablonlariEkleris.Select(s2 => new { s2.MailSablonlariEkiID, s2.EkAdi, s2.EkDosyaYolu }) }).First();
            return Json(new { sbl.SablonAdi, sbl.Sablon, sbl.SablonHtml, sbl.MailSablonlariEkleri }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        public ActionResult getOTSBS(int BasvuruSurecID)
        {
            var KulID = UserIdentity.Current.Id;
            var Ots = Management.cmbGetAktifOgrenimTipleri(BasvuruSurecID, false);
            return Ots.Select(s => new { s.Value, s.Caption }).toJsonResult();
        }
        [Authorize]
        public ActionResult getProgramlarBS(int BasvuruSurecID, List<int> OgrenimTipKods, bool IsBolumOrOgrenci, bool IsSonucOrMulakat)
        {
            var KulID = UserIdentity.Current.Id;
            OgrenimTipKods = OgrenimTipKods ?? new List<int>();
            OgrenimTipKods = OgrenimTipKods.Where(p => p > 0).ToList();
            var progs = Management.CmbGetBSTumProgramlar(BasvuruSurecID, IsBolumOrOgrenci, OgrenimTipKods, IsSonucOrMulakat);
            return progs.Select(s => new { s.Value, s.Caption }).toJsonResult();
        }

        [AllowAnonymous]
        public ActionResult getMsjKategoris(string EnstituKod)
        {
            var Ots = Management.cmbGetMesajKategorileri(EnstituKod, true, true);
            return Ots.Select(s => new { s.Value, s.Caption }).toJsonResult();
        }
        public ActionResult getKtNot(int MesajKategoriID)
        {
            string Not = "";
            var mkNot = db.MesajKategorileris.Where(p => p.MesajKategoriID == MesajKategoriID).FirstOrDefault();
            if (mkNot != null) Not = mkNot.KategoriAciklamasi;
            return Json(new { NotBilgisi = Not });
        }
        public ActionResult MesajKaydet(string dlgid, string GroupID, string EKD)
        {
            var model = new Mesajlar();
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            ViewBag.MmMessage = MmMessage;
            string EnstituKod = Management.getSelectedEnstitu(EKD);
            if (GroupID.IsNullOrWhiteSpace() == false)
            {
                model = db.Mesajlars.Where(p => p.GroupID == GroupID).First();
                if (UserIdentity.Current.IsAuthenticated)
                {
                    if (model.KullaniciID != UserIdentity.Current.Id)
                    {
                        model.AdSoyad = UserIdentity.Current.NameSurname;
                        model.Email = UserIdentity.Current.Description;
                    }
                }

            }
            else if (UserIdentity.Current.IsAuthenticated)
            {
                model.AdSoyad = UserIdentity.Current.NameSurname;
                model.Email = UserIdentity.Current.Description;
            }
            ViewBag.EnstituAdi = db.Enstitulers.Where(p => p.EnstituKod == EnstituKod).First().EnstituAd;
            ViewBag.MesajKategoriID = new SelectList(Management.cmbGetMesajKategorileri(EnstituKod, true, model != null), "Value", "Caption", model.MesajKategoriID);

            ViewBag.EnstituKod = new SelectList(Management.cmbGetAktifEnstituler(false), "Value", "Caption", EnstituKod);

            return View(model);
        }



        [HttpPost]
        [ValidateInput(false)]
        public ActionResult MesajKaydetPost(int MesajID, string GroupID, int MesajKategoriID, string Konu, string AdSoyad, string Email, string Aciklama, string AciklamaHtml, List<HttpPostedFileBase> DosyaEki, List<string> DosyaEkiAdi, string EKD)
        {
            Konu = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(Konu, false);
            AdSoyad = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(AdSoyad, false);
            Aciklama = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(Aciklama, false);
            Email = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(Email, false);
            GroupID = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(GroupID, false);

            //XssSaldırısı Var chec

            var mmMessage = new MmMessage();
            mmMessage.Title = "Dilek/Öneri/Şikayet gönderme işlemi";
            string _EnstituKod = Management.getSelectedEnstitu(EKD);

            DosyaEki = DosyaEki == null ? new List<HttpPostedFileBase>() : DosyaEki;
            DosyaEkiAdi = DosyaEkiAdi == null ? new List<string>() : DosyaEkiAdi;

            var qDosyaEkAdi = DosyaEkiAdi.Select((s, inx) => new { s, inx }).ToList();
            var qDosyaEki = DosyaEki.Select((s, inx) => new { s, inx }).ToList();

            var qDosyalar = (from dek in qDosyaEkAdi
                             join de in qDosyaEki on dek.inx equals de.inx
                             select new
                             {
                                 dek.inx,
                                 Dosya = de.s,
                                 FExtension = de.s.FileName.GetFileExtension(),
                                 DosyaAdi = dek.s + de.s.FileName.GetFileExtension(),
                                 DosyaYolu = "/MailDosyalari/" + de.s.FileName.ToFileNameAddGuid()
                             }).ToList();

            foreach (var item in qDosyalar)
            {
                if (!Management.FExtensions().Contains(item.FExtension))
                {
                    mmMessage.Messages.Add(item.Dosya.FileName + " dosyası " + string.Join(" , ", Management.FExtensions()).Replace(".", "") + " uzantılarından farklı bir dosya olamaz.");
                }
            }


            #region Kontrol 


            if (MesajID <= 0)
            {
                if (Konu.IsNullOrWhiteSpace())
                {
                    string msg = "Konu Giriniz.";
                    mmMessage.Messages.Add(msg);
                }
            }
            else
            {
                var mesaj = db.Mesajlars.Where(p => p.MesajID == MesajID && p.GroupID == GroupID).First();
                mesaj.IsAktif = false;
                Konu = mesaj.Konu;
                MesajKategoriID = mesaj.MesajKategoriID;
                if (UserIdentity.Current.IsAuthenticated && mesaj.KullaniciID != UserIdentity.Current.Id)
                {
                    Email = UserIdentity.Current.Description;
                    AdSoyad = UserIdentity.Current.NameSurname;
                }
                else
                {
                    Email = mesaj.Email;
                    AdSoyad = mesaj.AdSoyad;

                }
            }
            if (Aciklama.IsNullOrWhiteSpace() && AciklamaHtml.IsNullOrWhiteSpace())
            {
                string msg = "İçerik Giriniz.";
                mmMessage.Messages.Add(msg);
            }

            var kModel = new Mesajlar();
            kModel.EnstituKod = Management.getSelectedEnstitu(EKD);
            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                kModel.MesajKategoriID = MesajKategoriID;
                if (UserIdentity.Current.IsAuthenticated == false)
                {
                    kModel.AdSoyad = AdSoyad;
                    kModel.Email = Email;

                }
                else
                {
                    var kul = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First();
                    kModel.AdSoyad = kul.Ad + " " + kul.Soyad;
                    kModel.Email = kul.EMail;
                    kModel.KullaniciID = UserIdentity.Current.Id;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                }
                kModel.UstMesajID = MesajID <= 0 ? (int?)null : MesajID;
                kModel.GroupID = Guid.NewGuid().ToString();
                kModel.Tarih = DateTime.Now;
                kModel.SonMesajTarihi = kModel.Tarih;
                kModel.ToplamEkSayisi = 0;
                kModel.EnstituKod = _EnstituKod;
                kModel.IslemTarihi = DateTime.Now;
                kModel.Konu = Konu;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.Aciklama = Aciklama ?? "";
                kModel.AciklamaHtml = AciklamaHtml ?? "";

                var eklenen = db.Mesajlars.Add(kModel);
                foreach (var item in qDosyalar)
                {
                    item.Dosya.SaveAs(Server.MapPath("~" + item.DosyaYolu));
                    eklenen.MesajEkleris.Add(new MesajEkleri
                    {
                        EkAdi = item.DosyaAdi,
                        EkDosyaYolu = item.DosyaYolu
                    });
                }



                db.SaveChanges();
                if (eklenen.UstMesajID.HasValue)
                {
                    var UstMesaj = eklenen.Mesajlar2;
                    UstMesaj.ToplamEkSayisi = (UstMesaj.MesajEkleris.Count + UstMesaj.Mesajlar1.Sum(s => s.MesajEkleris.Count) + UstMesaj.GonderilenMaillers.Sum(s => s.GonderilenMailEkleris.Count));
                    UstMesaj.SonMesajTarihi = eklenen.Tarih;
                }
                else
                {
                    eklenen.SonMesajTarihi = eklenen.Mesajlar1.Any() ? eklenen.Mesajlar1.OrderByDescending(s2 => s2.Tarih).FirstOrDefault().Tarih : eklenen.Tarih;
                    eklenen.ToplamEkSayisi = (eklenen.MesajEkleris.Count + eklenen.Mesajlar1.Sum(s => s.MesajEkleris.Count) + eklenen.GonderilenMaillers.Sum(s => s.GonderilenMailEkleris.Count));
                }
                db.SaveChanges();
                mmMessage.IsSuccess = true;

                if (MesajID <= 0 && GroupID.IsNullOrWhiteSpace())
                {
                    var Sablon = db.MailSablonlaris.Where(p => p.EnstituKod == _EnstituKod && p.MailSablonTipID == MailSablonTipi.GelenIlkMesajOtoCvpMaili && p.IsAktif == true).FirstOrDefault();
                    if (Sablon != null)
                    {
                        var itemE = Sablon.Enstituler;
                        var EnstituL = Sablon.Enstituler;
                        var ParamereDegerleri = new List<MailReplaceParameterModel>();
                        var Parametreler = Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        if (Parametreler.Any(a => a == "@EnstituAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = EnstituL.EnstituAd });
                        if (Parametreler.Any(a => a == "@WebAdresi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = itemE.WebAdresi, IsLink = true });
                        if (Parametreler.Any(a => a == "@AdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AdSoyad", Value = kModel.AdSoyad });
                        var EMailList = new List<MailSendList> { new MailSendList { EMail = kModel.Email, ToOrBcc = true } };
                        if (Sablon.GonderilecekEkEpostalar.IsNullOrWhiteSpace() == false)
                            EMailList.AddRange(Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                        var mCOntent = SystemMails.GetSystemMailContent(EnstituL.EnstituAd, Sablon.SablonHtml, Sablon.SablonAdi, ParamereDegerleri);
                        var attach = new List<Attachment>();
                        foreach (var item in Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = Server.MapPath("~" + item.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var FExtension = Path.GetExtension(ekTamYol);
                                attach.Add(new Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)), item.EkAdi.ToSetNameFileExtension(FExtension), MediaTypeNames.Application.Octet));
                            }
                            else Management.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + item.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Ajax/MesajKaydetPost", BilgiTipi.Uyarı);
                        }
                        var snded = MailManager.sendMail(itemE.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, EMailList, attach);

                    }
                }
            }
            else
            {
                mmMessage.IsSuccess = false;
            }

            //var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            //return Content(strView, MediaTypeNames.Text.Html);
            return Json(new { success = mmMessage.IsSuccess, responseText = mmMessage.IsSuccess ? "Mesaj gönderme işlemi başarılı!" : "Mesaj gönderilirken bir hata oluştu!<br/>" + string.Join("<br/>", mmMessage.Messages) }, JsonRequestBehavior.AllowGet);
            //return new JsonResult { Data = new { IsSuccess = mmMessage.IsSuccess, Message = strView } };

        }

        [Authorize(Roles = RoleNames.Kullanicilar)]
        public ActionResult ObsOgrenciSorgula(string tc = "")
        {
            var obsGetData = new ObsGetData();
            var model = new ObsOgrenciSorgulaModel();
            if (!tc.IsNullOrWhiteSpace()) model = obsGetData.GetOgrenciBilgi(tc);
            model.Tc = tc;
            var view = Management.RenderPartialView("Ajax", "ObsOgrenciSorgula", model);
            return view.toJsonResult();
        }


        [Authorize]
        public ActionResult GetEnstituOgrencileri(string term, string EnstituKod)
        {

            var Kuls = (from k in db.Kullanicilars
                        join kt in db.KullaniciTipleris on k.KullaniciTipID equals kt.KullaniciTipID
                        orderby k.Ad, k.Soyad
                        where (k.TcKimlikNo == term || k.OgrenciNo == term || (k.Ad + " " + k.Soyad).Contains(term))
                        select new
                        {
                            id = k.KullaniciID + "",
                            AdSoyad = k.Ad + " " + k.Soyad,
                            text = k.Ad + " " + k.Soyad,
                            KullaniciTipAdi = kt.KullaniciTipAdi,
                            TcKimlikNo = kt.Yerli ? k.TcKimlikNo : k.PasaportNo,
                            OgrenciNo = k.OgrenciNo,
                            Images = k.ResimAdi

                        }).Take(15).ToList();

            var Kul2 = Kuls.Select(s => new
            {
                id = s.id + "",
                AdSoyad = s.AdSoyad,
                text = s.AdSoyad,
                KullaniciTipAdi = s.KullaniciTipAdi,
                TcKimlikNo = s.TcKimlikNo,
                OgrenciNo = s.OgrenciNo,
                Images = s.Images.toKullaniciResim()
            }).ToList();

            return Kul2.toJsonResult();
        }
        [Authorize]
        public ActionResult GetYTUOgretimEleman(string term)
        {
            var Data = Management.getWsPersisOE(term);
            var YtuUni = db.Universitelers.Where(p => p.UniversiteID == 67).FirstOrDefault();
            var Kul2 = Data.Table.Select(s => new
            {
                id = s.ADSOYAD,
                AdSoyad = s.ADSOYAD,
                text = s.ADSOYAD,
                BolumAdi = s.BOLUMADI.Replace("BÖLÜMÜ", ""),
                UnvanAdi = s.AKADEMIKUNVAN.ToMezuniyetJuriUnvanAdi(),
                UniversiteID = YtuUni != null ? YtuUni.UniversiteID : 67,
                UniversiteAdi = (YtuUni != null ? YtuUni.Ad : "Yıldız Teknik Üniversitesi (İstanbul)").ToUpper(),
                EMail = s.KURUMMAIL
            }).OrderBy(o => o.AdSoyad).Take(25).ToList();

            return Kul2.toJsonResult();
        }
        [Authorize]
        public ActionResult GetYTULUBOgretimEleman(string term)
        {
            var DanismanUnvanIDs = new List<int>() { 17, 42, 73 }; //Doç.Dr Prof.Dr, Dr. Öğr. Üye 
            var Kul2 = db.Kullanicilars.Where(p => p.KullaniciTipID == KullaniciTipBilgi.AkademikPersonel && DanismanUnvanIDs.Contains(p.UnvanID ?? 0) && (p.Ad + " " + p.Soyad).StartsWith(term)).OrderBy(o => o.Ad).ThenBy(t => t.Soyad).Take(25).Select(s => new
            {
                id = s.KullaniciID,
                AdSoyad = s.Ad + " " + s.Soyad,
                text = s.Ad + " " + s.Soyad,
                s.Birimler.BirimAdi,
                s.Unvanlar.UnvanAdi

            }).ToList();

            return Kul2.toJsonResult();
        }
        [Authorize]
        public ActionResult GetDxReport(int? RaporTipi, bool IsPdfStream = false)
        {
            XtraReport RprX = null;
            if (RaporTipi.HasValue == false)
            {
                Management.SistemBilgisiKaydet("Rapor almak için rapor tipinin gönderilmesi gerekmektedir!", "Ajax/GetDxReport", BilgiTipi.Hata);
            }
            else
            {
                if (RaporTipi == RaporTipleri.Basvuru)
                {
                    #region BasvuruFormu
                    var basvID = Request["BasvuruID"].toIntObj();
                    var btID = Request["BasvuruTercihID"].toIntObj();
                    var otID = Request["OgrenimTipKod"].toIntObj();


                    var gd = Guid.NewGuid().ToString().Substr(0, 5);

                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var terch = new List<BasvurularTercihleri>();
                        var basvuru = db.Basvurulars.Where(p => p.BasvuruID == basvID).First();
                        if (btID.HasValue) terch = db.BasvurularTercihleris.Where(p => p.BasvuruTercihID == btID).ToList();
                        else terch = db.BasvurularTercihleris.Where(p => p.BasvuruID == basvID).ToList();
                        rprBasvuruDoktora rprDoktora = null;
                        rprBasvuruYL rprYL = null;

                        var DoktoraTercihleri = terch.Where(p => p.OgrenimTipKod == OgrenimTipi.Doktra).ToList();
                        if (DoktoraTercihleri.Count > 0)
                        {
                            foreach (var item in DoktoraTercihleri)
                            {
                                var _rprDoktora = new rprBasvuruDoktora(item.BasvuruTercihID);
                                if (rprDoktora == null) rprDoktora = _rprDoktora;
                                else rprDoktora.Pages.AddRange(_rprDoktora.Pages);

                                rprDoktora.DisplayName = basvuru.Ad + " " + basvuru.Soyad + " DoktoraBF_" + gd;
                                RprX = rprDoktora;
                            }
                        }
                        var YLTercihleri = terch.Where(p => p.OgrenimTipKod != OgrenimTipi.Doktra).OrderBy(o => o.SiraNo).ToList();
                        if (YLTercihleri.Count > 0)
                        {
                            if (YLTercihleri.Count == 1)
                            {
                                var _rprYL = new rprBasvuruYL(YLTercihleri[0].BasvuruTercihID, null);
                                _rprYL.CreateDocument();
                                if (rprYL == null) rprYL = _rprYL;
                                else rprYL.Pages.AddRange(_rprYL.Pages);

                            }
                            else
                            {
                                var _rprYL = new rprBasvuruYL(YLTercihleri[0].BasvuruTercihID, YLTercihleri[1].BasvuruTercihID);
                                _rprYL.CreateDocument();
                                if (rprYL == null) rprYL = _rprYL;
                                else rprYL.Pages.AddRange(_rprYL.Pages);
                            }
                            rprYL.DisplayName = basvuru.Ad + " " + basvuru.Soyad + " YüksekLisansBF_" + gd;
                            RprX = rprYL;
                        }


                    }
                    #endregion
                }
                else if (RaporTipi == RaporTipleri.BasvuruOgrenciListesi)
                {
                    #region BasvuruOgrenciListesi
                    var _BasvuruSurecID = Request["BasvuruSurecID"].toIntObj();
                    var _OgrenimTipKod = Request["OgrenimTipKod"].toIntObj();
                    var _AlanTipID = Request["AlanTipID"].toIntObj();
                    var _ProgramKod = Request["ProgramKod"].toStrObjEmptString();
                    var _MulakatSonucTipID = new List<int>();

                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var q = from s in db.BasvuruSurecs
                                join b in db.Basvurulars.Where(p => p.BasvuruDurumID == BasvuruDurumu.Onaylandı) on s.BasvuruSurecID equals b.BasvuruSurecID
                                join t in db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı) on b.BasvuruID equals t.BasvuruID
                                join bsOt in db.BasvuruSurecOgrenimTipleris.Where(p => p.BasvuruSurecID == _BasvuruSurecID) on t.OgrenimTipKod equals bsOt.OgrenimTipKod
                                join ot in db.OgrenimTipleris on new { s.EnstituKod, t.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                                join at in db.AlanTipleris on t.AlanTipID equals at.AlanTipID
                                join pr in db.Programlars on t.ProgramKod equals pr.ProgramKod
                                join bl in db.AnabilimDallaris on new { pr.AnabilimDaliKod, s.EnstituKod } equals new { bl.AnabilimDaliKod, bl.EnstituKod }
                                join kot in db.BasvuruSurecKotalars.Where(p => p.ProgramKod == _ProgramKod) on new { s.BasvuruSurecID, t.OgrenimTipKod } equals new { kot.BasvuruSurecID, kot.OgrenimTipKod }
                                where t.ProgramKod == _ProgramKod && t.OgrenimTipKod == _OgrenimTipKod && s.BasvuruSurecID == _BasvuruSurecID
                                select new
                                {
                                    bl.AnabilimDaliKod,
                                    bl.AnabilimDaliAdi,
                                    bsOt.MulakatSurecineGirecek,
                                    Kota = kot.OrtakKota ? kot.OrtakKotaSayisi.Value : (t.AlanTipID == AlanTipi.AlanIci ? kot.AlanIciKota : kot.AlanDisiKota),
                                    pr.ProgramKod,
                                    pr.ProgramAdi,
                                    ot.OgrenimTipKod,
                                    ot.OgrenimTipAdi,
                                    at.AlanTipID,
                                    AlanTipAdi = at.AlanTipAdi + " Başvuran Öğrenci Listesi",
                                    AdSoyad = b.Ad + " " + b.Soyad,
                                    ProgramGrupAdi = pr.ProgramAdi + " " + ot.OgrenimTipAdi + " (" + at.AlanTipAdi + ")",
                                    t.SiraNo

                                };
                        if (_AlanTipID.HasValue) q = q.Where(p => p.AlanTipID == _AlanTipID);
                        var data = q.OrderBy(o => o.AnabilimDaliAdi).ThenBy(t => t.ProgramAdi).ThenBy(t => t.OgrenimTipAdi).ThenByDescending(t => t.AlanTipAdi).OrderBy(t => t.AdSoyad).Select(s => new rprBasvuruSonucModel
                        {
                            AnabilimDaliKod = s.AnabilimDaliKod,
                            AnabilimDaliAdi = s.AnabilimDaliAdi,
                            ProgramKod = s.ProgramKod,
                            ProgramAdi = s.ProgramAdi,
                            OgrenimTipKod = s.OgrenimTipKod,
                            OgrenimTipAdi = s.OgrenimTipAdi,
                            Kota = s.Kota,
                            AlanTipAdi = s.AlanTipAdi,
                            AdSoyad = s.AdSoyad,
                            ProgramGrupAdi = s.ProgramGrupAdi,
                            TercihNo = s.SiraNo
                        }).ToList();

                        rprBasvuruMulakatsizOgrenciList rpr = new rprBasvuruMulakatsizOgrenciList(_BasvuruSurecID.Value);
                        rpr.DataSource = data;
                        var BasvuruSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == _BasvuruSurecID).First();
                        if (BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) rpr.DisplayName = "Lisansüstü Başvuran Öğrenci Listesi";
                        else rpr.DisplayName = "Lisansüstü Yatay Geçiş Başvuran Öğrenci Listesi";
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFilePageByPage;

                        RprX = rpr;

                    }
                    #endregion
                }
                else if (RaporTipi == RaporTipleri.BasvuruSonucListesi)
                {
                    #region bSonucListesi
                    var _BasvuruSurecID = Request["BasvuruSurecID"].toIntObj();
                    var _AnabilimdaliKod = Request["AnabilimdaliKod"].toStrObj();
                    var _ProgramKod = Request["ProgramKod"].toStrObj();
                    var _OgrenimTipKod = Request["OgrenimTipKod"].toIntObj();
                    var _SubRaporTipID = Request["SubRaporTipID"].toIntObj();
                    var _MulakatSonucTipIDstr = Request["MulakatSonucTipID"];
                    _MulakatSonucTipIDstr = _MulakatSonucTipIDstr.toStrObjEmptString();
                    var EkBilgiTipID = Request["EkBilgiTipID"].toIntObj();
                    var _KayitDurumID = Request["KayitDurumID"].toIntObj();
                    var oTips = Request["OgrenimTips"].toStrObjEmptString();
                    var OgrenimTipKodus = new List<int>();
                    if (!oTips.IsNullOrWhiteSpace()) OgrenimTipKodus = oTips.Split(',').Select(s => s.ToInt().Value).ToList();
                    var _MulakatSonucTipID = new List<int>();
                    if (_SubRaporTipID == 1) _MulakatSonucTipIDstr.Split(',').ToList().ForEach((item) => { _MulakatSonucTipID.Add(item.ToInt().Value); });
                    else
                    {
                        _MulakatSonucTipID.AddRange(new List<int> { MulakatSonucTipi.Asil, MulakatSonucTipi.Yedek, MulakatSonucTipi.Kazanamadı });
                    }


                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var q = from s in db.BasvuruSurecs
                                join ms in db.MulakatSonuclaris on s.BasvuruSurecID equals ms.BasvuruSurecID
                                join kd in db.KayitDurumlaris on ms.KayitDurumID equals kd.KayitDurumID into defKd
                                from Kd in defKd.DefaultIfEmpty()
                                join t in db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı) on ms.BasvuruTercihID equals t.BasvuruTercihID
                                join kt in db.BasvuruSurecKotalars on new { s.BasvuruSurecID, t.ProgramKod, t.OgrenimTipKod } equals new { kt.BasvuruSurecID, kt.ProgramKod, kt.OgrenimTipKod }
                                join ot in db.OgrenimTipleris on new { s.EnstituKod, t.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                                join at in db.AlanTipleris on t.AlanTipID equals at.AlanTipID
                                join pr in db.Programlars on t.ProgramKod equals pr.ProgramKod
                                join bsOt in db.BasvuruSurecOgrenimTipleris.Where(p => p.BasvuruSurecID == _BasvuruSurecID) on t.OgrenimTipKod equals bsOt.OgrenimTipKod
                                join bl in db.AnabilimDallaris on new { pr.AnabilimDaliKod, s.EnstituKod } equals new { bl.AnabilimDaliKod, bl.EnstituKod }
                                join b in db.Basvurulars on t.BasvuruID equals b.BasvuruID
                                join bst in db.MulakatSonucTipleris on ms.MulakatSonucTipID equals bst.MulakatSonucTipID
                                where s.BasvuruSurecID == _BasvuruSurecID
                                select new
                                {
                                    s.BasvuruSurecID,
                                    b.RowID,
                                    bl.AnabilimDaliKod,
                                    bl.AnabilimDaliAdi,
                                    bsOt.MulakatSurecineGirecek,
                                    AlanKota = kt.OrtakKota ? (kt.OrtakKotaSayisi.Value + (kt.AlanDisiEkKota ?? 0)) : (t.AlanTipID == AlanTipi.AlanIci ? (kt.AlanIciKota + (kt.AlanIciEkKota ?? 0)) : (kt.AlanDisiKota + (kt.AlanDisiEkKota ?? 0))),
                                    AlanEkKota = kt.OrtakKota ? (kt.AlanDisiEkKota ?? 0) : (t.AlanTipID == AlanTipi.AlanIci ? (kt.AlanIciEkKota ?? 0) : (kt.AlanDisiEkKota ?? 0)),
                                    ms.SinavaGirmediY,
                                    ms.SinavaGirmediS,
                                    pr.ProgramKod,
                                    pr.ProgramAdi,
                                    pr.Ingilizce,
                                    ot.OgrenimTipKod,
                                    ot.OgrenimTipAdi,
                                    at.AlanTipID,
                                    at.AlanTipAdi,
                                    AdSoyad = b.Ad + " " + b.Soyad,
                                    AlesNotu = ms.AlesNotuOrDosyaNotu,
                                    ms.YaziliNotu,
                                    ms.SozluNotu,
                                    ms.GirisSinavNotu,
                                    ms.Agno,
                                    ms.GenelBasariNotu,
                                    ProgramGrupAdi = pr.ProgramAdi + " " + ot.OgrenimTipAdi + " (" + at.AlanTipAdi + ")",
                                    t.SiraNo,
                                    PuanSiraNo = ms.SiraNo,
                                    bst.MulakatSonucTipID,
                                    bst.MulakatSonucTipAdi,
                                    ms.Aciklama,
                                    Telefon = b.CepTel != null ? b.CepTel : (b.EvTel != null ? b.EvTel : (b.IsTel != null ? b.IsTel : "")),
                                    EMail = b.EMail,
                                    ms.KayitDurumID,
                                    KayitOldu = Kd != null ? Kd.IsKayitOldu : (bool?)null

                                };
                        var _RowID = Request["RowID"].toStrObj();
                        var RowID = new Guid();
                        bool IsOgrenciSonucListesindePuanGozuksun = false;
                        if (!_RowID.IsNullOrWhiteSpace())
                        {
                            RowID = new Guid(_RowID);
                            q = q.Where(p => p.RowID == RowID);

                            EkBilgiTipID = 2;
                            var Basvuru = db.Basvurulars.Where(p => p.RowID == RowID).First();
                            IsOgrenciSonucListesindePuanGozuksun = Basvuru.BasvuruSurec.IsOgrenciSonucListesindePuanGozuksun;
                            _BasvuruSurecID = Basvuru.BasvuruSurecID;
                            var ProgramKods = Basvuru.BasvurularTercihleris.Select(s => s.ProgramKod + "_" + s.OgrenimTipKod + "_" + s.AlanTipID).ToList();
                            q = q.Where(p => ProgramKods.Contains(p.ProgramKod + "_" + p.OgrenimTipKod + "_" + p.AlanTipID));
                        }
                        else
                        {
                            if (OgrenimTipKodus.Any())
                            {
                                q = q.Where(p => OgrenimTipKodus.Contains(p.OgrenimTipKod));
                            }
                            if (_AnabilimdaliKod.IsNullOrWhiteSpace() == false)
                            {
                                q = q.Where(p => p.AnabilimDaliKod == _AnabilimdaliKod);
                            }
                            if (_OgrenimTipKod.HasValue)
                            {
                                q = q.Where(p => p.OgrenimTipKod == _OgrenimTipKod);
                            }
                            if (_ProgramKod.IsNullOrWhiteSpace() == false)
                            {
                                q = q.Where(p => p.ProgramKod == _ProgramKod);
                            }
                            if (_KayitDurumID.HasValue)
                            {
                                q = q.Where(p => p.KayitDurumID == _KayitDurumID.Value);
                            }
                            q = q.Where(p => _MulakatSonucTipID.Contains(p.MulakatSonucTipID));
                        }
                        var data = q.Select(s => new rprBasvuruSonucModel
                        {
                            BasvuruSurecID = s.BasvuruSurecID,
                            RowID = s.RowID,
                            AnabilimDaliKod = s.AnabilimDaliKod,
                            AnabilimDaliAdi = s.AnabilimDaliAdi,
                            ProgramKod = s.ProgramKod,
                            ProgramAdi = s.ProgramAdi,
                            Ingilizce = s.Ingilizce,
                            OgrenimTipKod = s.OgrenimTipKod,
                            OgrenimTipAdi = s.OgrenimTipAdi,
                            Kota = s.AlanKota,
                            EkKota = s.AlanEkKota,
                            AlanTipID = s.AlanTipID,
                            AlanTipAdi = s.AlanTipAdi,
                            AdSoyad = s.AdSoyad,
                            Telefon = s.Telefon,
                            EMail = s.EMail,
                            AlesNotu = s.AlesNotu.Value,
                            GirisSinavNotu = s.GirisSinavNotu,
                            Agno = s.Agno,
                            YaziliNotu = s.YaziliNotu,
                            SozluNotu = s.SozluNotu,
                            GenelBasariNotu = s.GenelBasariNotu,
                            ProgramGrupAdi = s.ProgramGrupAdi,
                            TercihNo = s.SiraNo,
                            PSiraNo = s.PuanSiraNo,
                            MulakatSonucTipID = s.MulakatSonucTipID,
                            MulakatSonucTipAdi = s.MulakatSonucTipAdi,
                            KayitOldu = s.KayitOldu
                        }).OrderBy(o => o.AnabilimDaliAdi).ThenBy(t => t.ProgramAdi).ThenBy(t => t.OgrenimTipAdi).ThenBy(t => t.AlanTipAdi).ThenByDescending(t => t.GenelBasariNotu).ThenBy(t => (t.MulakatSonucTipID == MulakatSonucTipi.Asil ? 1 : (t.MulakatSonucTipID == MulakatSonucTipi.Yedek ? 2 : (t.MulakatSonucTipID == MulakatSonucTipi.Kazanamadı ? 3 : 4)))).ToList();


                        if (_RowID.IsNullOrWhiteSpace() && _SubRaporTipID != 1)
                        {
                            var nData = new List<rprBasvuruSonucModel>();
                            var qGrup = q.Select(s => new { s.ProgramKod, s.OgrenimTipKod, s.AlanTipID }).Distinct().ToList();

                            foreach (var item in qGrup)
                            {

                                var DataTumu = data.Where(p => p.BasvuruSurecID == _BasvuruSurecID &&
                                                                                         p.AlanTipID == item.AlanTipID &&
                                                                                         p.ProgramKod == item.ProgramKod &&
                                                                                         p.OgrenimTipKod == item.OgrenimTipKod &&
                                                                                         (p.MulakatSonucTipID == MulakatSonucTipi.Asil || p.MulakatSonucTipID == MulakatSonucTipi.Yedek)).ToList();
                                var DataYedekler = DataTumu.Where(p => p.MulakatSonucTipID == MulakatSonucTipi.Yedek).OrderBy(o => o.SiraNo).ToList();
                                var EkKota = DataTumu.Count > 0 ? DataTumu.First().EkKota : 0;

                                var toplamAsildenKalan = DataTumu.Where(p => p.MulakatSonucTipID == MulakatSonucTipi.Asil && p.KayitOldu == false).Count();
                                var toplamYedekKayit = DataTumu.Where(p => p.MulakatSonucTipID == MulakatSonucTipi.Yedek && p.KayitOldu == true).Count();
                                var kalan = (toplamAsildenKalan + EkKota) - toplamYedekKayit;


                                if (_SubRaporTipID == 2)
                                {
                                    nData.AddRange(DataYedekler);
                                }
                                else if (_SubRaporTipID == 3 && kalan > 0)
                                {
                                    nData.AddRange(DataYedekler.Where(p => p.KayitOldu == null));
                                }
                                else if (_SubRaporTipID == 4 && kalan > 0)
                                {
                                    nData.AddRange(DataYedekler.Where(p => p.KayitOldu == null).Take(kalan));
                                }


                            }
                            data = nData.OrderBy(o => o.AnabilimDaliAdi).ThenBy(t => t.ProgramAdi).ThenBy(t => t.OgrenimTipAdi).ThenBy(t => t.AlanTipAdi).ThenByDescending(t => t.GenelBasariNotu).ToList();
                        }
                        var dataD = data.Select(s => new { s.ProgramKod, s.OgrenimTipKod, s.AlanTipID }).Distinct();
                        foreach (var item in dataD)
                        {

                            int inx = 1;
                            foreach (var item2 in data.Where(p => p.OgrenimTipKod == item.OgrenimTipKod && p.ProgramKod == item.ProgramKod && p.AlanTipID == item.AlanTipID))
                            {

                                item2.SiraNo = _RowID.IsNullOrWhiteSpace() ? inx : item2.PSiraNo ?? 0;
                                inx++;
                                if (!_RowID.IsNullOrWhiteSpace())
                                {
                                    if (item2.RowID != RowID)
                                    {
                                        var ISIMS = item2.AdSoyad.Split(' ').Where(p => !p.IsNullOrWhiteSpace()).ToList();
                                        var MaskAdSoyad = "";
                                        foreach (var itemI in ISIMS)
                                        {
                                            MaskAdSoyad += itemI.Substr(0, 1) + "**** ";
                                        }

                                        item2.AdSoyad = MaskAdSoyad;
                                        if (!IsOgrenciSonucListesindePuanGozuksun) item2.GenelBasariNotu = null;
                                    }
                                }
                            }
                        }




                        if (EkBilgiTipID == 3)
                        {
                            var BasvuruSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == _BasvuruSurecID).First();
                            var qSinavOt = BasvuruSurec.BasvuruSurecSinavTipleriOTNotAraliklaris.ToList();

                            foreach (var itemP in data)
                            {
                                bool sinavYok = qSinavOt.Where(p => p.OgrenimTipKod == itemP.OgrenimTipKod && p.SinavTipleri.SinavTipGrupID == SinavTipGrup.Ales_Gree
                                         && (p.BasvuruSurecSinavTipleriOTNotAraliklariGecersizProgramlars.Any(a => a.ProgramKod == itemP.ProgramKod) == true || !p.IsGecerli || !p.IsIstensin)).Any();

                                if (sinavYok) itemP.AlesNotu = null;
                            }
                            rprBasvuruSonucPuanList rpr = new rprBasvuruSonucPuanList(_BasvuruSurecID.Value);
                            rpr.DataSource = data;
                            if (BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) rpr.DisplayName = "Lisansüstü Başvuru Sonuç Puan Listesi";
                            else rpr.DisplayName = "Lisansüstü Yatay Geçiş Başvuru Sonuç Puan Listesi";
                            rpr.PrintingSystem.ContinuousPageNumbering = true;
                            rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                            RprX = rpr;
                        }
                        else
                        {

                            rprBasvuruSonucList rpr = new rprBasvuruSonucList(_BasvuruSurecID.Value, EkBilgiTipID.Value);
                            rpr.DataSource = data;
                            var BasvuruSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == _BasvuruSurecID).First();
                            if (BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) rpr.DisplayName = "Lisansüstü Başvuru Sonuç Listesi";
                            else rpr.DisplayName = "Lisansüstü Yatay Geçiş Başvuru Sonuç Listesi";
                            rpr.PrintingSystem.ContinuousPageNumbering = true;
                            rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                            RprX = rpr;
                        }



                    }
                    #endregion
                }
                else if (RaporTipi == RaporTipleri.BasvuruSonucListesiBolum)
                {
                    #region bBSonucListesi 
                    var _MulakatID = Request["MulakatID"].toIntObj();
                    var _AlanTipID = Request["AlanTipID"].toIntObj();
                    var _MulakatSonucTipIDstr = Request["MulakatSonucTipID"];
                    var _PuanlarGozuksun = Request["PuanlarGozuksun"].toBooleanObj();
                    var _SadecePuanliListe = Request["SadecePuanliListe"].toBooleanObj();
                    var _MulakatSonucTipID = new List<int>();

                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var mulakat = db.Mulakats.Where(p => p.MulakatID == _MulakatID).First();
                        var data = new List<rprBasvuruSonucModel>();
                        if (_PuanlarGozuksun.Value)
                        {


                            var q = from s in db.BasvuruSurecs
                                    join ms in db.MulakatSonuclaris on s.BasvuruSurecID equals ms.BasvuruSurecID
                                    join t in db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı) on ms.BasvuruTercihID equals t.BasvuruTercihID
                                    join kt in db.BasvuruSurecKotalars on new { s.BasvuruSurecID, t.ProgramKod, t.OgrenimTipKod } equals new { kt.BasvuruSurecID, kt.ProgramKod, kt.OgrenimTipKod }
                                    join bsOt in db.BasvuruSurecOgrenimTipleris.Where(p => p.BasvuruSurecID == mulakat.BasvuruSurecID) on t.OgrenimTipKod equals bsOt.OgrenimTipKod
                                    join ot in db.OgrenimTipleris on new { s.EnstituKod, t.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                                    join at in db.AlanTipleris on t.AlanTipID equals at.AlanTipID
                                    join pr in db.Programlars on t.ProgramKod equals pr.ProgramKod
                                    join bl in db.AnabilimDallaris on new { pr.AnabilimDaliKod, s.EnstituKod } equals new { bl.AnabilimDaliKod, bl.EnstituKod }
                                    join b in db.Basvurulars on t.BasvuruID equals b.BasvuruID
                                    join bst in db.MulakatSonucTipleris on ms.MulakatSonucTipID equals bst.MulakatSonucTipID
                                    where ms.MulakatID == _MulakatID && s.BasvuruSurecID == mulakat.BasvuruSurecID
                                    select new
                                    {
                                        s.BasvuruSurecID,
                                        bl.AnabilimDaliKod,
                                        bl.AnabilimDaliAdi,
                                        bsOt.MulakatSurecineGirecek,
                                        ms.MulakatID,
                                        AlanKota = kt.OrtakKota ? kt.OrtakKotaSayisi.Value : (t.AlanTipID == AlanTipi.AlanIci ? kt.AlanIciKota : kt.AlanDisiKota),
                                        ms.SinavaGirmediY,
                                        ms.SinavaGirmediS,
                                        pr.ProgramKod,
                                        pr.ProgramAdi,
                                        ot.OgrenimTipKod,
                                        ot.OgrenimTipAdi,
                                        at.AlanTipID,
                                        AlanTipAdi = at.AlanTipAdi + (_PuanlarGozuksun.Value ? " Başvuran Öğrenci Sonuç Listesi" : " Başvuran Öğrenci Listesi"),
                                        AdSoyad = b.Ad + " " + b.Soyad,
                                        AlesNotu = ms.AlesNotuOrDosyaNotu,
                                        ms.GirisSinavNotu,
                                        ms.Agno,
                                        ms.YaziliNotu,
                                        ms.SozluNotu,
                                        SSiraNo = ms.SiraNo,
                                        ms.GenelBasariNotu,
                                        ProgramGrupAdi = pr.ProgramAdi + " " + ot.OgrenimTipAdi + " (" + at.AlanTipAdi + ")",
                                        TSiraNo = t.SiraNo,
                                        bst.MulakatSonucTipID,
                                        t.BasvuruTercihID,
                                        MulakatSonucTipAdi = _SadecePuanliListe.HasValue && _SadecePuanliListe.Value ? "" : bst.MulakatSonucTipAdi,
                                        kt.IsAlesYerineDosyaNotuIstensin,
                                        ms.AlesNotuOrDosyaNotu,

                                    };
                            if (_SadecePuanliListe.HasValue == false || _SadecePuanliListe.Value == false)
                            {
                                _MulakatSonucTipIDstr.Split(',').ToList().ForEach((item) => { _MulakatSonucTipID.Add(item.ToInt().Value); });
                                q = q.Where(p => _MulakatSonucTipID.Contains(p.MulakatSonucTipID));
                            }
                            //q = q.Where(p => p.MulakatSurecineGirecek && p.SinavaGirmediY == (p.SinavaGirmediY.HasValue ? false : (bool?)null) && p.SinavaGirmediS == (p.SinavaGirmediS.HasValue ? false : (bool?)null)); 
                            q = q.OrderBy(o => o.AnabilimDaliAdi).ThenBy(t => t.ProgramAdi).ThenBy(t => t.OgrenimTipAdi).ThenByDescending(t => t.AlanTipAdi).ThenByDescending(t => t.GenelBasariNotu);
                            if (_AlanTipID.HasValue) q = q.Where(p => p.AlanTipID == _AlanTipID.Value);

                            data = q.Select(s => new rprBasvuruSonucModel
                            {
                                BasvuruSurecID = s.BasvuruSurecID,
                                AnabilimDaliKod = s.AnabilimDaliKod,
                                AnabilimDaliAdi = s.AnabilimDaliAdi,
                                ProgramKod = s.ProgramKod,
                                ProgramAdi = s.ProgramAdi,
                                OgrenimTipKod = s.OgrenimTipKod,
                                OgrenimTipAdi = s.OgrenimTipAdi,
                                Kota = s.AlanKota,
                                AlanTipID = s.AlanTipID,
                                AlanTipAdi = s.AlanTipAdi,
                                AdSoyad = s.AdSoyad,
                                AlesNotu = s.AlesNotu.Value,
                                YaziliNotu = s.YaziliNotu,
                                SozluNotu = s.SozluNotu,
                                GirisSinavNotu = s.GirisSinavNotu,
                                Agno = s.Agno,
                                SiraNo = s.SSiraNo ?? 0,
                                GenelBasariNotu = s.GenelBasariNotu,
                                ProgramGrupAdi = s.ProgramGrupAdi,
                                TercihNo = s.TSiraNo,
                                TercihID = s.BasvuruTercihID,
                                MulakatSonucTipID = s.MulakatSonucTipID,
                                MulakatSonucTipAdi = s.MulakatSonucTipAdi,
                                MulakatID = s.MulakatID,
                                SinavaGirmediS = s.SinavaGirmediS,
                                SinavaGirmediY = s.SinavaGirmediY
                            }).OrderBy(o => o.AnabilimDaliAdi).ThenBy(t => t.ProgramAdi).ThenBy(t => t.OgrenimTipAdi).ThenBy(t => t.AlanTipAdi).ThenByDescending(t => t.GenelBasariNotu).ToList();

                        }
                        else
                        {
                            var q = from s in db.BasvuruSurecs
                                    join b in db.Basvurulars.Where(p => p.BasvuruDurumID == BasvuruDurumu.Onaylandı) on s.BasvuruSurecID equals b.BasvuruSurecID
                                    join t in db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı) on b.BasvuruID equals t.BasvuruID
                                    join ms in db.MulakatSonuclaris on t.BasvuruTercihID equals ms.BasvuruTercihID into defM
                                    from Ms in defM.DefaultIfEmpty()
                                    join bt in db.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.Ales_Gree) on b.BasvuruID equals bt.BasvuruID into defBt
                                    from dBT in defBt.DefaultIfEmpty()
                                    join bsOt in db.BasvuruSurecOgrenimTipleris.Where(p => p.BasvuruSurecID == mulakat.BasvuruSurecID) on t.OgrenimTipKod equals bsOt.OgrenimTipKod
                                    join ot in db.OgrenimTipleris on new { s.EnstituKod, t.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                                    join at in db.AlanTipleris on t.AlanTipID equals at.AlanTipID
                                    join pr in db.Programlars on t.ProgramKod equals pr.ProgramKod
                                    join bl in db.AnabilimDallaris on new { pr.AnabilimDaliKod, s.EnstituKod } equals new { bl.AnabilimDaliKod, bl.EnstituKod }
                                    join kot in db.BasvuruSurecKotalars.Where(p => p.ProgramKod == mulakat.ProgramKod && p.OgrenimTipKod == mulakat.OgrenimTipKod) on s.BasvuruSurecID equals kot.BasvuruSurecID
                                    where t.ProgramKod == mulakat.ProgramKod && t.OgrenimTipKod == mulakat.OgrenimTipKod && s.BasvuruSurecID == mulakat.BasvuruSurecID
                                    select new
                                    {
                                        bl.AnabilimDaliKod,
                                        bl.AnabilimDaliAdi,
                                        bsOt.MulakatSurecineGirecek,
                                        Kota = kot.OrtakKota ? kot.OrtakKotaSayisi.Value : (t.AlanTipID == AlanTipi.AlanIci ? kot.AlanIciKota : kot.AlanDisiKota),
                                        pr.ProgramKod,
                                        pr.ProgramAdi,
                                        ot.OgrenimTipKod,
                                        ot.OgrenimTipAdi,
                                        pr.AlesTipID,
                                        pr.AnabilimDallari.EnstituKod,
                                        pr.AlesNotuYuksekOlanAlinsin,
                                        WsXmlData = dBT.WsXmlData,
                                        AlesNotu = kot.IsAlesYerineDosyaNotuIstensin == true ? (Ms != null ? Ms.AlesNotuOrDosyaNotu : null) : dBT.SinavNotu,
                                        AGNO = b.YLNotSistemID.HasValue ? b.YLMezuniyetNotu100LukSistem : b.LMezuniyetNotu100LukSistem,
                                        at.AlanTipID,
                                        AlanTipAdi = at.AlanTipAdi + (_PuanlarGozuksun.Value ? " Başvuran Öğrenci Sonuç Listesi" : " Başvuran Öğrenci Listesi"),
                                        AdSoyad = b.Ad + " " + b.Soyad,
                                        ProgramGrupAdi = pr.ProgramAdi + " " + ot.OgrenimTipAdi + " (" + at.AlanTipAdi + ")",
                                        MSSiraNo = Ms.SiraNo,
                                        TSiraNo = t.SiraNo,
                                        t.BasvuruTercihID,
                                        kot.IsAlesYerineDosyaNotuIstensin,


                                    };
                            if (_AlanTipID.HasValue) q = q.Where(p => p.AlanTipID == _AlanTipID.Value);
                            data = q.Select(s => new rprBasvuruSonucModel
                            {
                                AnabilimDaliKod = s.AnabilimDaliKod,
                                AnabilimDaliAdi = s.AnabilimDaliAdi,
                                ProgramKod = s.ProgramKod,
                                ProgramAdi = s.ProgramAdi,
                                OgrenimTipKod = s.OgrenimTipKod,
                                OgrenimTipAdi = s.OgrenimTipAdi,
                                Kota = s.Kota,
                                Agno = s.AGNO,
                                WsXmlData = s.WsXmlData,
                                AlesNotuYuksekOlanAlinsin = s.AlesNotuYuksekOlanAlinsin,
                                AlesNotu = s.AlesNotu,
                                AlesTipID = s.AlesTipID,
                                AlanTipID = s.AlanTipID,
                                AlanTipAdi = s.AlanTipAdi,
                                AdSoyad = s.AdSoyad,
                                ProgramGrupAdi = s.ProgramGrupAdi,
                                PSiraNo = s.MSSiraNo,
                                TercihNo = s.TSiraNo,
                                TercihID = s.BasvuruTercihID,
                            }).OrderBy(o => o.AnabilimDaliAdi).ThenBy(t => t.ProgramAdi).ThenBy(t => t.OgrenimTipAdi).ThenByDescending(t => t.AlanTipAdi).OrderBy(t => t.AdSoyad).ToList();


                        }
                        var dataD = data.Select(s => new { s.ProgramKod, s.OgrenimTipKod, s.AlanTipID }).Distinct();
                        foreach (var item in dataD)
                        {

                            int inx = 1;
                            foreach (var item2 in data.Where(p => p.OgrenimTipKod == item.OgrenimTipKod && p.ProgramKod == item.ProgramKod && p.AlanTipID == item.AlanTipID))
                            {

                                item2.SiraNo = inx;
                                inx++;
                            }
                        }
                        var qSinavOt = db.BasvuruSurecSinavTipleriOTNotAraliklaris.Where(p => p.BasvuruSurecID == mulakat.BasvuruSurecID).ToList();

                        var kotalar = db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == mulakat.BasvuruSurecID).ToList();
                        var bsSinavBilgi = db.BasvuruSurecSinavTipleris.Where(p => p.BasvuruSurecID == mulakat.BasvuruSurecID).ToList();

                        foreach (var item in data)
                        {
                            var btercih = db.BasvurularTercihleris.Where(p => p.BasvuruTercihID == item.TercihID).First();
                            var kota = kotalar.Where(p => p.ProgramKod == btercih.ProgramKod && p.OgrenimTipKod == btercih.OgrenimTipKod).First();

                            var IsAlesYerineDosyaNotuIstensin = kota.IsAlesYerineDosyaNotuIstensin == true;
                            if (!IsAlesYerineDosyaNotuIstensin)
                            {
                                var sinavBilgi = btercih.Basvurular.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.Ales_Gree).FirstOrDefault();
                                var _snvBilgi = bsSinavBilgi.Where(p => p.SinavTipID == (sinavBilgi != null ? sinavBilgi.SinavTipID : -1)).FirstOrDefault();
                                bool sinavYok = qSinavOt.Where(p => p.OgrenimTipKod == btercih.OgrenimTipKod && p.Ingilizce == kota.Programlar.Ingilizce && p.SinavTipID == (_snvBilgi != null ? _snvBilgi.SinavTipID : p.SinavTipID) && p.SinavTipleri.SinavTipGrupID == SinavTipGrup.Ales_Gree
                                                 && (p.BasvuruSurecSinavTipleriOTNotAraliklariGecersizProgramlars.Any(a => a.ProgramKod == btercih.ProgramKod) == true || !p.IsGecerli || !p.IsIstensin)).Any();
                                if (sinavBilgi != null && sinavYok == false)
                                {

                                    if (_snvBilgi.WebService) // ales notu al
                                    {
                                        var wsxmlNot = sinavBilgi.WsXmlData.toSinavSonucAlesXmlModel();
                                        if (btercih.Programlar.AlesNotuYuksekOlanAlinsin && btercih.Programlar.AnabilimDallari.EnstituKod == EnstituKodlari.SosyalBilimleri)
                                        {
                                            var maxNot = new Dictionary<int, double>();
                                            if (btercih.Programlar.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.Sayısal)) maxNot.Add(AlesTipBilgi.Sayısal, wsxmlNot.SAY_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                                            if (btercih.Programlar.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.Sözel)) maxNot.Add(AlesTipBilgi.Sözel, wsxmlNot.SOZ_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                                            if (btercih.Programlar.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.EşitAğırlık)) maxNot.Add(AlesTipBilgi.EşitAğırlık, wsxmlNot.EA_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                                            item.AlesNotu = maxNot.Select(s => s.Value).Max();
                                        }
                                        else
                                        {
                                            if (btercih.Programlar.AlesTipID == AlesTipBilgi.Sayısal)
                                                item.AlesNotu = wsxmlNot.SAY_PUAN.ToDouble().ToString("n2").ToDouble().Value;
                                            else if (btercih.Programlar.AlesTipID == AlesTipBilgi.Sözel)
                                                item.AlesNotu = wsxmlNot.SOZ_PUAN.ToDouble().ToString("n2").ToDouble().Value;
                                            else if (btercih.Programlar.AlesTipID == AlesTipBilgi.EşitAğırlık)
                                                item.AlesNotu = wsxmlNot.EA_PUAN.ToDouble().ToString("n2").ToDouble().Value;
                                        }
                                    }
                                    else
                                        item.AlesNotu = sinavBilgi.SinavNotu;
                                }
                                else item.AlesNotu = null;//sınav istenmediği için ales null olarak eklenir 
                            }

                        }
                        var mdl = new rprBasvuruSonucBolumModel();
                        var firstD = data.FirstOrDefault();
                        if (firstD != null)
                        {
                            mdl.BolumAdi = firstD.AnabilimDaliAdi;
                            mdl.ProgramAdi = firstD.ProgramAdi + " " + firstD.OgrenimTipAdi;
                        }
                        mdl.ProgramB = data;
                        var juri = db.MulakatJuris.Where(p => p.MulakatID == _MulakatID).Select(s => new rwMulakatJuri
                        {
                            MulakatID = s.MulakatID,
                            MulakatJuriID = s.MulakatJuriID,
                            JuriAdi = s.JuriAdi,
                            SiraNo = s.SiraNo,
                            IsAsil = s.IsAsil,
                            AsilYedek = s.IsAsil ? "Asil" : "Yedek"
                        }).OrderByDescending(o => o.IsAsil).ThenBy(t => t.SiraNo).ToList();
                        mdl.MulakatJuriB = juri;

                        mdl.MulakatDetayB = (from s in db.MulakatDetays.Where(p => p.MulakatID == _MulakatID)
                                             join mst in db.MulakatSinavTurleris on s.MulakatSinavTurID equals mst.MulakatSinavTurID
                                             join kmps in db.Kampuslers on s.KampusID equals kmps.KampusID
                                             join bsst in db.BasvuruSurecMulakatSinavTurleris on new { s.Mulakat.BasvuruSurecID, s.MulakatSinavTurID } equals new { bsst.BasvuruSurecID, bsst.MulakatSinavTurID }
                                             select new krMulakatDetay
                                             {
                                                 MulakatID = s.MulakatID,
                                                 MulakatSinavTurID = s.MulakatSinavTurID,
                                                 MulakatSinavTurAdi = mst.MulakatSinavTurAdi,
                                                 YuzdeOran = bsst.YuzdeOran,
                                                 YuzdeOranStr = "%" + s.YuzdeOran,
                                                 SinavTarihi = s.SinavTarihi,
                                                 KampusAdi = kmps.KampusAdi,
                                                 KampusID = s.KampusID,
                                                 YerAdi = s.YerAdi
                                             }).ToList();


                        if (_PuanlarGozuksun.HasValue && _PuanlarGozuksun.Value)
                        {
                            rprBasvuruOgrenciPuanList rpr = new rprBasvuruOgrenciPuanList(mulakat.BasvuruSurecID);
                            rpr.DataSource = mdl;
                            if (mulakat.BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) rpr.DisplayName = "Lisansüstü Başvuru Mülakat Sonuç Puan Listesi";
                            else rpr.DisplayName = "Lisansüstü Yatay Geçiş Mülakat Sonuç Puan Listesi";
                            rpr.PrintingSystem.ContinuousPageNumbering = true;
                            rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFilePageByPage;

                            RprX = rpr;
                        }
                        else
                        {
                            rprBasvuruOgrenciList rpr = new rprBasvuruOgrenciList(mulakat.BasvuruSurecID);
                            rpr.DataSource = mdl;
                            if (mulakat.BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) rpr.DisplayName = "Lisansüstü Başvuru Mülakat Sınav Giriş Listesi";
                            else rpr.DisplayName = "Lisansüstü Yatay Geçiş Mülakat Sınav Giriş Listesi";
                            rpr.PrintingSystem.ContinuousPageNumbering = true;
                            rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFilePageByPage;

                            RprX = rpr;
                        }
                    }
                    #endregion
                }
                else if (RaporTipi == RaporTipleri.KesinKayitListesi)
                {
                    #region bBSonucListesi
                    var _BasvuruSurecID = Request["BasvuruSurecID"].toIntObj();
                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var ImgPath = SistemAyar.KullaniciResimYolu;
                        var BasvuruSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == _BasvuruSurecID).First();
                        var kayitOlanlar = BasvuruSurec.MulakatSonuclaris.Where(p => p.KayitDurumID.HasValue && p.KayitDurumlari.IsKayitOldu == true).ToList().Select(s => new rprModelKazananList
                        {
                            ProgramKodu = s.BasvurularTercihleri.ProgramKod,
                            ProgramAdi = s.BasvurularTercihleri.Programlar.ProgramAdi + " (" + s.BasvurularTercihleri.ProgramKod + ")",
                            OgrenciNo = s.Basvurular.Kullanicilar.OgrenciNo,
                            ResimAdi = System.Web.HttpContext.Current.Server.MapPath("/" + ImgPath + "/" + s.Basvurular.ResimAdi),
                            AdSoyad = s.Basvurular.Ad + " " + s.Basvurular.Soyad
                        }).OrderBy(o => o.ProgramAdi).ThenBy(t => t.OgrenciNo).ToList();

                        var rpr = new rprKesinKayitResimListesi();
                        rpr.DataSource = kayitOlanlar;
                        if (BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) rpr.DisplayName = "Lisansüstü Programlarına Kayıt Olan Öğrenci Listesi";
                        else if (BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru) rpr.DisplayName = "Lisansüstü Yatay Geçiş Programlarına Kayıt Olan Öğrenci Listesi";
                        else rpr.DisplayName = "YTU Yeni mezun Lisansüstü Programlarına Kayıt Olan Öğrenci Listesi";
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFilePageByPage;

                        RprX = rpr;

                    }
                    #endregion
                }
                else if (RaporTipi == RaporTipleri.AnabilimdaliProgramListesi)
                {
                    #region ABD_ProgramList
                    var OgrenimTipKods = Request["OgrenimTipKods"].toStrObj();
                    var EKod = Request["EKod"].toStrObj();

                    var _OgrenimTipKods = new List<int>();
                    OgrenimTipKods.Split(',').ToList().ForEach((item) => { _OgrenimTipKods.Add(item.ToInt().Value); });
                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var enst = db.Enstitulers.Where(p => p.EnstituKod == EKod).First();



                        var q = from s in db.AnabilimDallaris.Where(p => p.EnstituKod == EKod)
                                join pr in db.Programlars on s.AnabilimDaliID equals pr.AnabilimDaliID
                                join bkot in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurec.EnstituKod == EKod) on pr.ProgramKod equals bkot.ProgramKod
                                join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == EKod) on bkot.OgrenimTipKod equals ot.OgrenimTipKod
                                join otl in db.OgrenimTipleris on ot.OgrenimTipID equals otl.OgrenimTipID
                                select new
                                {

                                    AnabilimDaliKod = s.AnabilimDaliKod,
                                    AnabilimDaliAdi = s.AnabilimDaliAdi,
                                    OgrenimTipKod = ot.OgrenimTipKod,
                                    OgrenimTipAdi = otl.OgrenimTipAdi,
                                    ProgramKodu = pr.ProgramKod,
                                    ProgramAdi = pr.ProgramAdi,
                                    EgitimDili = pr.Ingilizce ? "İngilizce" : "Türkçe"
                                };
                        q = q.Where(p => _OgrenimTipKods.Contains(p.OgrenimTipKod));
                        var qGroup = from s in q
                                     group s by new { s.AnabilimDaliKod, s.AnabilimDaliAdi, s.OgrenimTipKod, s.OgrenimTipAdi, s.ProgramKodu, s.ProgramAdi, s.EgitimDili } into g1

                                     select new rprModelBolumProgramList
                                     {

                                         AnabilimDaliKod = g1.Key.AnabilimDaliKod,
                                         AnabilimDaliAdi = g1.Key.AnabilimDaliAdi,
                                         OgrenimTipKod = g1.Key.OgrenimTipKod,
                                         OgrenimTipAdi = g1.Key.OgrenimTipAdi,
                                         ProgramKodu = g1.Key.ProgramKodu,
                                         ProgramAdi = g1.Key.ProgramAdi,
                                         EgitimDili = g1.Key.EgitimDili
                                     };

                        var qdata = qGroup.OrderBy(o => o.AnabilimDaliAdi).ThenBy(t => t.OgrenimTipAdi).ThenBy(t => t.ProgramAdi).ToList();
                        rprBolumProgramListesi rpr = new rprBolumProgramListesi(enst.EnstituAd);
                        rpr.DataSource = qdata;
                        rpr.DisplayName = "Anabilim Dalı/Program Listesi";
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFilePageByPage;

                        RprX = rpr;

                    }

                    #endregion
                }
                else if (RaporTipi == RaporTipleri.BasvuruSonucSayisal)
                {
                    #region BasvuruSonucSayisal
                    var BasvuruSurecID = Request["BasvuruSurecID"].toIntObj();
                    if (RoleNames.LisansustuBasvuruRapor.InRoleCurrent() == false) BasvuruSurecID = 0;
                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var qbs = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod));
                        var bsurec = qbs.First();
                        var qx = (from s in qbs
                                  join enst in db.Enstitulers on s.EnstituKod equals enst.EnstituKod
                                  join dnm in db.Donemlers on s.DonemID equals dnm.DonemID
                                  select new raporLUBModel
                                  {
                                      EnstituAdi = enst.EnstituAd,
                                      AkademikYil = s.BaslangicYil + " / " + s.BitisYil + " " + dnm.DonemAdi,
                                      ToplamTercihSayisi = s.MulakatSonuclaris.Count,
                                      OgrenimTipleri = (from s2 in s.BasvuruSurecOgrenimTipleris.Where(p => p.IsAktif)
                                                        join otl in db.OgrenimTipleris.Where(p => s.EnstituKod == p.EnstituKod) on s2.OgrenimTipKod equals otl.OgrenimTipKod
                                                        select new raporOtipModel
                                                        {
                                                            GBNO = s2.BasariNotOrtalamasi,
                                                            OgrenimTipAdi = otl.OgrenimTipAdi,
                                                            TaslakCount = db.BasvurularTercihleris.Count(c => c.Basvurular.BasvuruSurecID == s.BasvuruSurecID && c.OgrenimTipKod == s2.OgrenimTipKod && c.Basvurular.BasvuruDurumID == BasvuruDurumu.Taslak),
                                                            OnaylananCount = db.BasvurularTercihleris.Count(c => c.Basvurular.BasvuruSurecID == s.BasvuruSurecID && c.OgrenimTipKod == s2.OgrenimTipKod && c.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı),
                                                            IptalEdilenCount = db.BasvurularTercihleris.Count(c => c.Basvurular.BasvuruSurecID == s.BasvuruSurecID && c.OgrenimTipKod == s2.OgrenimTipKod && c.Basvurular.BasvuruDurumID == BasvuruDurumu.IptalEdildi),
                                                            KayitCount = db.MulakatSonuclaris.Count(c => c.BasvuruSurecID == s.BasvuruSurecID && c.BasvurularTercihleri.OgrenimTipKod == s2.OgrenimTipKod && c.KayitDurumID.HasValue && c.KayitDurumlari.IsKayitOldu),

                                                        }),
                                      ADToplamModel = new fmMsonucOranModel
                                      {
                                          Toplam = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanDisi),
                                          Kota = s.BasvuruSurecKotalars.Sum(sm => sm.AlanDisiKota),
                                          AsilCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanDisi && c.MulakatSonucTipID == MulakatSonucTipi.Asil),
                                          YedekCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanDisi && c.MulakatSonucTipID == MulakatSonucTipi.Yedek),
                                          KazanamayanCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanDisi && c.MulakatSonucTipID == MulakatSonucTipi.Kazanamadı),
                                          KayitCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanDisi && c.KayitDurumID.HasValue && c.KayitDurumlari.IsKayitOldu),
                                      },
                                      AIToplamModel = new fmMsonucOranModel
                                      {
                                          Toplam = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanIci),
                                          Kota = s.BasvuruSurecKotalars.Sum(sm => sm.AlanIciKota),
                                          AsilCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanIci && c.MulakatSonucTipID == MulakatSonucTipi.Asil),
                                          YedekCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanIci && c.MulakatSonucTipID == MulakatSonucTipi.Yedek),
                                          KazanamayanCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanIci && c.MulakatSonucTipID == MulakatSonucTipi.Kazanamadı),
                                          KayitCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanIci && c.KayitDurumID.HasValue && c.KayitDurumlari.IsKayitOldu),
                                      },
                                      BasvuruSonuclari = (from kt in s.BasvuruSurecKotalars
                                                          join ot in db.OgrenimTipleris.Where(p => s.EnstituKod == p.EnstituKod) on kt.OgrenimTipKod equals ot.OgrenimTipKod
                                                          join prg in db.Programlars on kt.ProgramKod equals prg.ProgramKod
                                                          join abd in db.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                                                          select new frMulakatSonucDetay
                                                          {
                                                              OgrenimTipAdi = ot.OgrenimTipAdi,
                                                              AnabilimDaliAdi = abd.AnabilimDaliAdi + " / " + prg.ProgramAdi,
                                                              ToplamBasvuru = s.MulakatSonuclaris.Count(c => c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                              AIKota = kt.OrtakKota == true ? 0 : kt.AlanIciKota,
                                                              AIKayitCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanIci && c.KayitDurumID.HasValue && c.KayitDurumlari.IsKayitOldu && c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                              AIAsilCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanIci && c.MulakatSonucTipID == MulakatSonucTipi.Asil && c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                              AIYedekCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanIci && c.MulakatSonucTipID == MulakatSonucTipi.Yedek && c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                              AIKazanamayanCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanIci && c.MulakatSonucTipID == MulakatSonucTipi.Kazanamadı && c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                              ADKota = kt.OrtakKota == true ? kt.OrtakKotaSayisi.Value : kt.AlanDisiKota,
                                                              ADKayitCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanDisi && c.KayitDurumID.HasValue && c.KayitDurumlari.IsKayitOldu && c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                              ADAsilCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanDisi && c.MulakatSonucTipID == MulakatSonucTipi.Asil && c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                              ADYedekCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanDisi && c.MulakatSonucTipID == MulakatSonucTipi.Yedek && c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                              ADKazanamayanCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanDisi && c.MulakatSonucTipID == MulakatSonucTipi.Kazanamadı && c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                          }).OrderBy(o => o.AnabilimDaliAdi)

                                  });

                        var data = qx.ToList();
                        foreach (var itemD in data)
                        {
                            itemD.EnstituAdi = itemD.EnstituAdi.ToUpper();
                            itemD.SurecTarihi = bsurec.BaslangicTarihi.ToString("dd-MM-yyyy HH:mm") + " / " + bsurec.BitisTarihi.ToString("dd-MM-yyyy HH:mm");
                            var toplmMdl = new List<fmMsonucOranModel>();
                            toplmMdl.Add(itemD.AIToplamModel);
                            toplmMdl.Add(itemD.ADToplamModel);
                            foreach (var item in toplmMdl)
                            {
                                item.ToplamYuzde = item.Toplam * 100.0 / itemD.ToplamTercihSayisi;
                                item.AsilYuzde = item.AsilCount * 100.0 / itemD.ToplamTercihSayisi;
                                item.YedekYuzde = item.YedekCount * 100.0 / itemD.ToplamTercihSayisi;
                                item.KazanamayanYuzde = item.KazanamayanCount * 100.0 / itemD.ToplamTercihSayisi;
                                item.KayitYuzde = item.KayitCount * 100.0 / toplmMdl.Sum(s => s.KayitCount);
                            }
                        }

                        raporLUB rpr = new raporLUB(bsurec.BasvuruSurecTipID);
                        rpr.DataSource = data;
                        if (bsurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) rpr.DisplayName = "Lisansüstü Başvuru Süreci Sayısal Bilgisi";
                        else if (bsurec.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru) rpr.DisplayName = "Lisansüstü Yatay Geçiş Başvuru Süreci Sayısal Bilgisi";
                        else rpr.DisplayName = "YTU Yeni Mezun DR Başvuru Süreci Sayısal Bilgi";

                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                        RprX = rpr;

                    }

                    #endregion
                }
                else if (RaporTipi == RaporTipleri.BelgeTalepSayisal)
                {
                    var BaslangicT = Request["T1"].toStrObj();
                    var BitisT = Request["T2"].toStrObj();
                    var eKod = Request["eKod"].toStrObj();
                    int BaslangicYil = 0;
                    int BaslangicAy = 0;
                    int BitisYil = 0;
                    int BitisAy = 0;
                    var yilModel = new List<int>();
                    var yilAyModel = new List<raporBTSayisalModel>();
                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var btipdetayIds = db.BelgeTipDetayBelgelers.Where(p => p.BelgeTipDetay.IsAktif).Select(s => s.BelgeTipID).Distinct();
                        if (RoleNames.BelgeTalepleriRapor.InRoleCurrent())
                        {
                            BaslangicYil = BaslangicT.Split('-')[0].ToInt().Value;
                            BaslangicAy = BaslangicT.Split('-')[1].ToInt().Value;
                            BitisYil = BitisT.Split('-')[0].ToInt().Value;
                            BitisAy = BitisT.Split('-')[1].ToInt().Value;
                            var bTips = db.BelgeTipleris.Where(p => btipdetayIds.Contains(p.BelgeTipID) && p.IsAktif).ToList();
                            var t1 = Convert.ToDateTime(BaslangicT + "-01");
                            var t2 = Convert.ToDateTime(BitisT + "-01");
                            for (DateTime i = t1; i <= t2; i = i.AddMonths(1))
                            {

                                foreach (var item in bTips)
                                {
                                    yilAyModel.Add(new raporBTSayisalModel { Yil = i.Year, Ay = i.Month, BelgeTipID = item.BelgeTipID, BelgeTipAdi = item.BelgeTipAdi });
                                }
                            }
                            yilModel = yilAyModel.Select(s => s.Yil).Distinct().ToList();
                        }

                        var data = (from s in db.Enstitulers
                                    where s.EnstituKod == eKod
                                    select new raporBTModel
                                    {
                                        EnstituAdi = s.EnstituAd,
                                        SurecTarihi = BaslangicT + " / " + BitisT,

                                        YilaGoreToplam = (from ya in yilModel
                                                          select new raporBTSayisalModel
                                                          {
                                                              Yil = ya,
                                                              Toplam = db.BelgeTalepleris.Count(p => btipdetayIds.Contains(p.BelgeTipID) && p.TalepTarihi.Year == ya),
                                                              TalepEdilen = db.BelgeTalepleris.Count(p => btipdetayIds.Contains(p.BelgeTipID) && new List<int> { BelgeTalepDurum.TalepEdildi, BelgeTalepDurum.Hazirlandi, BelgeTalepDurum.Hazirlaniyor }.Contains(p.BelgeDurumID) && p.TalepTarihi.Year == ya),
                                                              Verilen = db.BelgeTalepleris.Count(p => btipdetayIds.Contains(p.BelgeTipID) && p.BelgeDurumID == BelgeTalepDurum.Verildi && p.TalepTarihi.Year == ya),
                                                              Kapatilan = db.BelgeTalepleris.Count(p => btipdetayIds.Contains(p.BelgeTipID) && p.BelgeDurumID == BelgeTalepDurum.Kapatildi && p.TalepTarihi.Year == ya),
                                                              IptalEdilen = db.BelgeTalepleris.Count(p => btipdetayIds.Contains(p.BelgeTipID) && p.BelgeDurumID == BelgeTalepDurum.IptalEdildi && p.TalepTarihi.Year == ya),

                                                          }
                                                        ).OrderBy(o => o.Yil),
                                    }).ToList();
                        foreach (var item in data)
                        {
                            item.EnstituAdi = item.EnstituAdi.ToUpper();
                            item.DetayliToplam = (from ya in yilAyModel
                                                  select new raporBTSayisalModel
                                                  {
                                                      Yil = ya.Yil,
                                                      Ay = ya.Ay,
                                                      BelgeTipAdi = ya.BelgeTipAdi,
                                                      Toplam = db.BelgeTalepleris.Count(p => p.BelgeTipID == ya.BelgeTipID && p.TalepTarihi.Year == ya.Yil && p.TalepTarihi.Month == ya.Ay),
                                                      TalepEdilen = db.BelgeTalepleris.Count(p => new List<int> { BelgeTalepDurum.TalepEdildi, BelgeTalepDurum.Hazirlandi, BelgeTalepDurum.Hazirlaniyor }.Contains(p.BelgeDurumID) && p.BelgeTipID == ya.BelgeTipID && p.TalepTarihi.Year == ya.Yil && p.TalepTarihi.Month == ya.Ay),
                                                      Verilen = db.BelgeTalepleris.Count(p => p.BelgeDurumID == BelgeTalepDurum.Verildi && p.BelgeTipID == ya.BelgeTipID && p.TalepTarihi.Year == ya.Yil && p.TalepTarihi.Month == ya.Ay),
                                                      Kapatilan = db.BelgeTalepleris.Count(p => p.BelgeDurumID == BelgeTalepDurum.Kapatildi && p.BelgeTipID == ya.BelgeTipID && p.TalepTarihi.Year == ya.Yil && p.TalepTarihi.Month == ya.Ay),
                                                      IptalEdilen = db.BelgeTalepleris.Count(p => p.BelgeDurumID == BelgeTalepDurum.IptalEdildi && p.BelgeTipID == ya.BelgeTipID && p.TalepTarihi.Year == ya.Yil && p.TalepTarihi.Month == ya.Ay),

                                                  }
                                                        ).OrderBy(o => o.Yil).ThenBy(t => t.Ay).ThenBy(t => t.BelgeTipAdi).ToList();
                        }
                        RaporBT rpr = new RaporBT();
                        rpr.DataSource = data;
                        rpr.DisplayName = "Belge Talepleri Sayısal Raporu";
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                        RprX = rpr;


                    }
                }
                else if (RaporTipi == RaporTipleri.MezuniyetBasvuruRaporu)
                {
                    var basvID = Request["MezuniyetBasvurulariID"].toIntObj();
                    rprMezuniyetYayinSartiOnayiFormu rpr = new rprMezuniyetYayinSartiOnayiFormu(basvID.Value);

                    rpr.PrintingSystem.ContinuousPageNumbering = true;
                    rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                    RprX = rpr;
                }
                else if (RaporTipi == RaporTipleri.Anketler)
                {
                    var AnketID = Request["AnketID"].toIntObj();
                    var BasTar = Request["BasTar"].ToDate();
                    var BitTar = Request["BitTar"].ToDate();
                    if (RoleNames.AnketlerRapor.InRoleCurrent() == false) AnketID = 0;
                    var t1 = DateTime.Now;
                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var anket = db.Ankets.Where(p => p.AnketID == AnketID).First();
                        var enstitu = anket.Enstituler;

                        var Anket = db.Ankets.Where(p => p.AnketID == AnketID).First();
                        var AnketSorularis = Anket.AnketSorus.ToList();
                        var AnketSoruSecenek = db.AnketSoruSeceneks.Where(p => p.AnketSoru.AnketID == AnketID).ToList();
                        var Cevaplar = db.AnketCevaplaris.Where(p => p.AnketID == AnketID && p.Tarih >= BasTar && p.Tarih <= BitTar).ToList();
                        var qModel = (from sa in AnketSorularis
                                      select new frAnketDetay
                                      {
                                          AnketSoruID = sa.AnketSoruID,
                                          AnketID = sa.AnketID,
                                          SoruAdi = sa.SoruAdi,
                                          SiraNo = sa.SiraNo,
                                          IsTabloVeriGirisi = sa.IsTabloVeriGirisi,
                                          frAnketSecenekDetay = (from ss in AnketSoruSecenek.Where(p => p.AnketSoruID == sa.AnketSoruID)
                                                                 select new frAnketSecenekDetay
                                                                 {
                                                                     AnketSoruID = ss.AnketSoruID,
                                                                     AnketSoruSecenekID = ss.AnketSoruSecenekID,
                                                                     SiraNo = ss.SiraNo,
                                                                     SecenekAdi = ss.SecenekAdi,
                                                                     IsEkAciklamaGir = ss.IsEkAciklamaGir,
                                                                     Count = Cevaplar.Where(p => p.AnketSoruSecenekID == ss.AnketSoruSecenekID).Count(),
                                                                     AnketCevaplaris = Cevaplar.Where(p => p.AnketSoruSecenekID == ss.AnketSoruSecenekID).ToList()
                                                                 }
                                                               ).OrderBy(o => o.SiraNo).ToList(),
                                          AnketCevaplaris = Cevaplar.Where(p => p.AnketSoruID == sa.AnketSoruID).ToList()
                                      }).OrderBy(o => o.SiraNo).ToList();



                        foreach (var item in qModel)
                        {
                            if (item.IsTabloVeriGirisi)
                            {

                                var tblRw = new AnketTableDetay();
                                tblRw.SiraNo = "#";
                                int i = 0;
                                foreach (var item2 in item.frAnketSecenekDetay)
                                {
                                    i++;
                                    PropertyInfo propertyInfo = tblRw.GetType().GetProperty("TabloVeri" + i);
                                    propertyInfo.SetValue(tblRw, item2.SecenekAdi, null);
                                }
                                item.TableDetay.Add(tblRw);
                                i = 0;
                                foreach (var item2 in item.AnketCevaplaris)
                                {
                                    i++;
                                    item.TableDetay.Add(new AnketTableDetay
                                    {
                                        SiraNo = (i).ToString(),
                                        TabloVeri1 = item2.TabloVeri1,
                                        TabloVeri2 = item2.TabloVeri2,
                                        TabloVeri3 = item2.TabloVeri3,
                                        TabloVeri4 = item2.TabloVeri4,

                                    });
                                }
                            }
                            else
                            {
                                var tblRw = new AnketTableDetay();
                                foreach (var item2 in item.frAnketSecenekDetay)
                                {
                                    var ekAciklamalar = "";

                                    var _ekAciklama = new Dictionary<int, string>();
                                    if (item2.IsEkAciklamaGir)
                                        foreach (var itemx in item2.AnketCevaplaris.Select((s, inx) => new { s = s, inx = inx }))
                                        {
                                            _ekAciklama.Add(itemx.inx + 1, itemx.s.EkAciklama);
                                        }
                                    item.SecenekDetay.Add(new AnketSeceneklerDetay
                                    {
                                        EkAciklama = _ekAciklama,
                                        SiraNo = item2.SiraNo,
                                        SecenekAdi = item2.SecenekAdi + " " + ekAciklamalar,
                                        Count = item2.Count,
                                    });
                                }
                            }
                        }
                        var t2 = DateTime.Now;
                        var TS = (t2 - t1).TotalSeconds;

                        Management.SistemBilgisiKaydet("'" + anket.AnketAdi + "' Anket Raporu Oluşturuldu. Oluşturulma Süresi: " + TS + " Sn.", "Ajax/GetDxReport", BilgiTipi.Bilgi);
                        rprAnket rpr = new rprAnket(enstitu.EnstituAd, anket.AnketAdi, BasTar.ToString("dd-MM-yyyy") + " - " + BitTar.ToString("dd-MM-yyyy") + " Tarih aralığındaki anket sonuçları");
                        rpr.DataSource = qModel;
                        rpr.DisplayName = BasTar.ToString("dd-MM-yyyy") + " - " + BitTar.ToString("dd-MM-yyyy") + " Tarih aralığındaki " + anket.AnketAdi + " anket sonuçları";
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                        RprX = rpr;

                    }
                }
                else if (RaporTipi == RaporTipleri.MezuniyetCiltFormuRaporu)
                {
                    var FormID = Request["ID"].toIntObj();
                    var rpr = new rprMezuniyetCiltliTezTeslimFormu_FR1243(FormID.Value);
                    rpr.PrintingSystem.ContinuousPageNumbering = true;
                    rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                    RprX = rpr;
                }
                else if (RaporTipi == RaporTipleri.MezuniyetJuriOneriFormuRaporu)
                {
                    var MezuniyetBasvurulariID = Request["ID"].toIntObj().Value;
                    var rpr = new rprMezuniyetTezJuriOneriFormu_FR0300_FR0339(MezuniyetBasvurulariID);
                    rpr.PrintingSystem.ContinuousPageNumbering = true;
                    rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                    RprX = rpr;
                }
                else if (RaporTipi == RaporTipleri.MezuniyetTezTeslimFormu)
                {
                    var _UniqueID = Request["UniqueID"].ToString();
                    var _IlkTeslim = Request["IlkTeslim"].toBooleanObj() ?? false;
                    var UniqueID = new Guid(_UniqueID);
                    var rpr = new rprMezuniyetTezTeslimFormu_FR0338(UniqueID, _IlkTeslim);

                    RprX = rpr;

                }
                else if (RaporTipi == RaporTipleri.MezuniyetTezSinavSonucFormu)
                {
                    var UniqueID = Request["UniqueID"].ToString();
                    var rpr = new rprTezSinavSonucTutanagi_FR0342_FR0377(new Guid(UniqueID));
                    RprX = rpr;

                }
                else if (RaporTipi == RaporTipleri.MezuniyetJuriUyelerineTezTeslimFormu)
                {
                    var _MezuniyetJuriOneriFormID = Request["MezuniyetJuriOneriFormID"].ToInt();
                    var rpr = new rprJuriUyelerineTezTeslimFormu_FR0341_FR0302(_MezuniyetJuriOneriFormID.Value);

                    RprX = rpr;

                }
                else if (RaporTipi == RaporTipleri.MezuniyetTezdenUretilenYayinlariDegerlendirmeFormu)
                {
                    var _MezuniyetJuriOneriFormID = Request["MezuniyetJuriOneriFormID"].ToInt();
                    var _MezuniyetJuriOneriFormuJuriID = Request["MezuniyetJuriOneriFormuJuriID"].ToInt();
                    var rpr = new rprMezuniyetTezdenUretilenYayinlariDegerlendirmeFormu_FR0304(_MezuniyetJuriOneriFormID.Value, _MezuniyetJuriOneriFormuJuriID);

                    RprX = rpr;

                }
                else if (RaporTipi == RaporTipleri.MezuniyetDoktoraTezDegerlendirmeFormu)
                {
                    var _MezuniyetJuriOneriFormID = Request["MezuniyetJuriOneriFormID"].ToInt();
                    var _MezuniyetJuriOneriFormuJuriID = Request["MezuniyetJuriOneriFormuJuriID"].ToInt();
                    var rpr = new rprMezuniyetTezDegerlendirmeFormu_FR0303(_MezuniyetJuriOneriFormID.Value, _MezuniyetJuriOneriFormuJuriID);

                    RprX = rpr;

                }
                else if (RaporTipi == RaporTipleri.MezuniyetTezKontrolFormu)
                {
                    var ID = Request["ID"].ToString();
                    var UniqueID = new Guid(ID);

                    var rpr = new rprMezuniyetTezKontrolFormu(UniqueID, null);
                    rpr.CreateDocument();
                    RprX = rpr;
                }
                else if (RaporTipi == RaporTipleri.TezIzlemeDegerlendirmeFormu)
                {
                    var ID = Request["UniqueID"].ToString();
                    var UniqueID = new Guid(ID);
                    var Rapor = db.TIBasvuruAraRapors.Where(p => p.UniqueID == UniqueID).FirstOrDefault();

                    var rpr = new rprTIDegerlendirmeFormu_FR0307(Rapor.TIBasvuruAraRaporID);
                    rpr.CreateDocument();
                    rpr.DisplayName = Rapor.TIBasvuru.Ad + " " + Rapor.TIBasvuru.Soyad + " " + rpr.DisplayName;


                    if (Rapor.TIBasvuru.KullaniciID != UserIdentity.Current.Id || RoleNames.TITezDegerlendirmeYap.InRoleCurrent() || RoleNames.TITezDegerlendirmeDuzeltme.InRoleCurrent())
                    {
                        var rpr2 = new rprTIDegerlendirmeFormuDetay_FR0307(Rapor.TIBasvuruAraRaporID);
                        rpr2.CreateDocument();
                        rpr2.DisplayName = rpr2.DisplayName;
                        rpr.Pages.AddRange(rpr2.Pages);
                    }
                    RprX = rpr;
                }
                else if (RaporTipi == RaporTipleri.TezDanismanOneriFormu)
                {
                    var ID = Request["UniqueID"].ToString();
                    var UniqueID = new Guid(ID);
                    var Rapor = db.TDOBasvuruDanismen.Where(p => p.UniqueID == UniqueID).FirstOrDefault();

                    var rpr = new rprTezDanismaniOneriFormu_FR0347(Rapor.TDOBasvuruDanismanID);
                    rpr.CreateDocument();
                    rpr.DisplayName = Rapor.TDOBasvuru.Ad + " " + Rapor.TDOBasvuru.Soyad + " " + rpr.DisplayName;

                    RprX = rpr;
                }

                else if (RaporTipi == RaporTipleri.TezEsDanismanOneriFormu)
                {
                    var ID = Request["UniqueID"].ToString();
                    var UniqueID = new Guid(ID);
                    var Rapor = db.TDOBasvuruDanismen.Where(p => p.UniqueID == UniqueID).FirstOrDefault();
                    var rpr = new rprTezEsDanismaniOneriFormu_FR0320(Rapor.TDOBasvuruDanismanID);
                    rpr.CreateDocument();
                    rpr.DisplayName = Rapor.TDOBasvuru.Ad + " " + Rapor.TDOBasvuru.Soyad + " " + rpr.DisplayName;
                    RprX = rpr;
                }
            }
            if (IsPdfStream)
            {
                var ms = new MemoryStream();
                RprX.ExportToPdf(ms);
                RprX.ExportOptions.Pdf.Compressed = true;
                ms.Seek(0, System.IO.SeekOrigin.Begin);


                Response.AddHeader("Content-Disposition", "inline;filename=\"" + RprX.DisplayName + ".pdf\"");
                return new FileStreamResult(ms, "application/pdf");


            }
            else return View(RprX);
        }


        public ActionResult GetChkList()
        {
            return View();
        }
        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
