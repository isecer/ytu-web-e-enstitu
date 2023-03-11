
using BiskaUtil;
using CaptchaMvc.HtmlHelpers;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Models.ObsService;
using LisansUstuBasvuruSistemi.Raporlar;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Raporlar.BelgeTalep;
using LisansUstuBasvuruSistemi.Raporlar.Genel;
using LisansUstuBasvuruSistemi.Raporlar.Mezuniyet;
using LisansUstuBasvuruSistemi.Raporlar.TezDanismanOneri;
using LisansUstuBasvuruSistemi.Raporlar.TezIzleme;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Filters;
using LisansUstuBasvuruSistemi.Utilities.SystemData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class AjaxController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();


        public ActionResult GetThemeSetting()
        {
            var k = _entities.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).Select(s => new
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

            var kullanici = _entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == UserIdentity.Current.Id);
            if (columnName == "st_head_fixed") kullanici.FixedHeader = value.ToBoolean().Value;
            if (columnName == "st_sb_fixed") kullanici.FixedSidebar = value.ToBoolean().Value;
            if (columnName == "st_sb_scroll") kullanici.ScrollSidebar = value.ToBoolean().Value;
            if (columnName == "st_sb_right") kullanici.RightSidebar = value.ToBoolean().Value;
            if (columnName == "st_sb_custom") kullanici.CustomNavigation = value.ToBoolean().Value;
            if (columnName == "st_sb_toggled") kullanici.ToggledNavigation = value.ToBoolean().Value;
            if (columnName == "st_layout_boxed") kullanici.BoxedOrFullWidth = value.ToBoolean().Value;
            if (columnName == "ThemeName") kullanici.ThemeName = value;
            if (columnName == "BackgroundImage") kullanici.BackgroundImage = value;
            _entities.SaveChanges();
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

        public ActionResult LoginControl(string userName, string password, string captchaInputText, bool? rememberMe, string returnUrl, string dlgId)
        {

            var mmMessage = new LoginAjaxDto
            {
                ReturnUrl = returnUrl,
                UserName = userName,
                Password = password
            };
            rememberMe = rememberMe ?? false;

            Kullanicilar loginUser = null;
            string hata = null;
            try
            {
                if (userName.IsNullOrWhiteSpace())
                {
                    hata = "Kullanıcı Adı Boş Bırakılamaz.";
                }
                else if (password.IsNullOrWhiteSpace())
                {
                    hata = "Şifre Giriniz.";
                }
                else if (captchaInputText.IsNullOrWhiteSpace())
                {
                    hata = "Resimdeki Karakterleri Giriniz.";
                }
                else if (!this.IsCaptchaValid(""))
                {
                    hata = "Resimdeki Karakterleri Hatalı Girdiniz";
                }
                else
                {
                    string msg = "";
                    var user = UserBus.GetLoginUser(userName);

                    if (user != null)
                    {
                        if (user.IsActiveDirectoryUser == false)
                        {
                            loginUser = UserBus.Login(userName, password);
                        }
                        else
                        {
                            var ld = new LdapService.SecureSoapClient();

                            var wsPwd = ConfigurationManager.AppSettings["ldapServicePassword"];
                            var isSucces = ld.Login(userName, password, wsPwd);
                            if (isSucces)
                            {
                                loginUser = user;

                            }
                            else
                            {
                                mmMessage.IsSuccess = false;
                                msg = "Active Directory Kontrolünden Geçilemedi!";
                                // Management.SistemBilgisiKaydet("Active Directory Kontrolünden Geçilemedi! Kullanıcı Adı: " + UserName, "Acconunt/Login", BilgiTipi.LoginHatalari, null, UserIdentity.Ip);
                            }
                        }
                        if (loginUser != null && !loginUser.IsAktif)
                        {
                            hata = "Kullanıcı Hesabı Pasif Durumda!";
                            mmMessage.IsSuccess = false;
                        }
                        else if (loginUser == null)
                        {
                            hata = "Kullanıcı Adı veya Şifre Hatalı. " + msg;
                            mmMessage.IsSuccess = false;
                        }
                        else
                        {
                            mmMessage.IsSuccess = true;
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
                        mmMessage.IsSuccess = false;
                        //Management.SistemBilgisiKaydet("Kullanıcı Sistemde Bulunamadı! Kullanıcı Adı: " + UserName, "Acconunt/Login", BilgiTipi.LoginHatalari, null, UserIdentity.Ip);
                        hata = "Kullanıcı sistemde bulunamadı.";
                    }
                }

            }
            catch (Exception ex)
            {
                mmMessage.IsSuccess = false;
                hata = "Sisteme Giriş Yapılırken Bir Hata Oluştu! Hata: " + ex.ToExceptionMessage();
            }
            mmMessage.Message = hata;
            if (mmMessage.IsSuccess == false)
            {
                var newCaptcha = ViewRenderHelper.RenderPartialView("Ajax", "GetCaptcha", new UrlInfoModel());
                mmMessage.NewSrc = newCaptcha;

            }
            else
            {
                if (loginUser.YtuOgrencisi && loginUser.OgrenimDurumID == OgrenimDurum.HalenOğrenci)
                {
                    var tdoBasvuruId = _entities.TDOBasvurus
                        .Where(p => p.KullaniciID == loginUser.KullaniciID)
                        .OrderByDescending(o => o.TDOBasvuruID).Select(s => s.TDOBasvuruID)
                        .FirstOrDefault();
                    if (tdoBasvuruId > 0)
                    {
                        string hataMesaji = "";
                        var sonuc = TezDanismanOneriBus.ObsDanismanBasvuruBilgiEslestir(
                            loginUser.KullaniciID, tdoBasvuruId, out hataMesaji);
                    }
                }
                FormsAuthenticationUtil.SetAuthCookie(loginUser.KullaniciAdi, "", rememberMe.Value);
                UserBus.SetLastLogon();
            }
            return mmMessage.ToJsonResult();
        }
        public ActionResult SignOut(string returnUrl)
        {
            var mmMessage = new LoginAjaxDto();

            if (UserIdentity.Current.IsAuthenticated)
            {
                var kulId = UserIdentity.Current.Id;
                var kul = _entities.Kullanicilars.First(p => p.KullaniciID == kulId);
                kul.LastLogonDate = DateTime.Now;
                _entities.SaveChanges();
                FormsAuthenticationUtil.SignOut();
            }

            mmMessage.ReturnUrl = returnUrl.IsNullOrWhiteSpace() ? Url.Action("Index", "Home") : returnUrl;
            mmMessage.IsSuccess = true;
            return mmMessage.ToJsonResult();
        }
        [Authorize]
        public ActionResult GetImageUpload(int kullaniciId)
        {
            if (RoleNames.KullanicilarKayit.InRoleCurrent() == false) kullaniciId = UserIdentity.Current.Id;
            var kullanici = UserBus.GetUser(kullaniciId);
            return View(kullanici);
        }
        [Authorize]
        public ActionResult GetImageUploadPost(int kullaniciId, HttpPostedFileBase kProfilResmi)
        {
            var mMessage = new MmMessage();
            string yeniResim = "";
            mMessage.Title = "Profil resmi yükleme işlemi başarısız";
            mMessage.IsSuccess = false;
            mMessage.MessageType = Msgtype.Warning;
            bool anaResmiDegistir = false;
            if (kProfilResmi == null || kProfilResmi.ContentLength <= 0)
            {
                mMessage.Messages.Add("Profil Resmi Yükleyiniz");
            }
            else if (RoleNames.KullanicilarKayit.InRoleCurrent() == false && kullaniciId != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Başka bir kullanıcı adına resim yüklemesi yapmaya yetkili değilsiniz.");
            }
            else
            {
                var contentlength = kProfilResmi.ContentLength;
                string uzanti = kProfilResmi.FileName.GetFileExtension();
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
                    var kul = _entities.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
                    var eskiResim = kul.ResimAdi;
                    kul.ResimAdi = yeniResim = KullanicilarBus.ResimKaydet(kProfilResmi);
                    kul.IslemYapanID = UserIdentity.Current.Id;
                    kul.IslemYapanIP = UserIdentity.Ip;
                    kul.IslemTarihi = DateTime.Now;
                    _entities.SaveChanges();
                    mMessage.Title = "Profil Resmi başarılı bir şekilde yüklenmiştir.";
                    mMessage.IsSuccess = true;
                    mMessage.MessageType = Msgtype.Success;
                    if (kullaniciId == UserIdentity.Current.Id)
                    {
                        anaResmiDegistir = true;
                        var userIdentity = UserBus.GetUserIdentity(UserIdentity.Current.Name);
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
                            catch (Exception)
                            {
                                // ignored
                            }
                    }
                }
            }
            return new { mMessage = mMessage, ResimAdi = yeniResim.ToKullaniciResim(), AnaResmiDegistir = anaResmiDegistir }.ToJsonResult();
        }
        [Authorize]
        public ActionResult YetkiYenile(string returnUrl)
        {
            var mmMessage = new MmMessage();

            if (UserIdentity.Current.IsAuthenticated)
            {
                var userIdentity = UserBus.GetUserIdentity(UserIdentity.Current.Name);
                userIdentity.Impersonate();
                Session["UserIdentity"] = userIdentity;
                mmMessage.Messages.Add("Yetkileriniz yeniden yüklenmiştir.");
            }

            mmMessage.ReturnUrl = returnUrl.IsNullOrWhiteSpace() ? Url.Action("Index", "Home") : returnUrl;
            mmMessage.IsSuccess = true;
            return mmMessage.ToJsonResult();
        }
        [HttpGet]
        [Authorize]
        public ActionResult GetKullaniciDetay(int kullaniciId)
        {
            if (!(RoleNames.Kullanicilar.InRoleCurrent() == true
                  || RoleNames.GelenBasvurular.InRoleCurrent()
                  || RoleNames.BasvuruSureci.InRoleCurrent()
                  || RoleNames.MulakatSureci.InRoleCurrent()
                  || RoleNames.SrGelenTalepler.InRoleCurrent()
                  || RoleNames.GelenBelgeTalepleri.InRoleCurrent()))
                kullaniciId = UserIdentity.Current.Id;
            var data = _entities.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
            ViewBag.ResimVar = data.ResimAdi.IsNullOrWhiteSpace() == false;
            data.ResimAdi = data.ResimAdi.ToKullaniciResim();


            #region Enstituler
            var enstroles = EnstituBus.GetEnstituler(true);
            var userEnstRoles = UserBus.GetKullaniciEnstituler(kullaniciId);
            var kullanici = UserBus.GetUser(kullaniciId);
            var dataEnst = enstroles.Select(s => new CheckObject<Enstituler>
            {
                Value = s,
                Checked = userEnstRoles.Any(p => p.EnstituKod == s.EnstituKod)
            });
            ViewBag.KEnstituler = dataEnst;
            #endregion
            #region yetkiler
            var roles = RollerBus.GetAllRoles().ToList();
            var userRoles = UserBus.GetUserRoles(kullaniciId);
            ViewBag.EkRollerCount = userRoles.EklenenRoller.Count;
            ViewBag.Kullanici = kullanici;
            var dataR = roles.Select(s => new CheckObject<Roller>
            {
                Value = s,
                Checked = userRoles.TumRoller.Any(a => a.RolID == s.RolID)
            });
            ViewBag.KRoller = dataR;
            var kategr = roles.Select(s => s.Kategori).Distinct().ToArray();
            var menuK = _entities.Menulers.Where(a => a.BagliMenuID == 0 && kategr.Contains(a.MenuAdi)).ToList();
            var dct = new List<CmbIntDto>();
            foreach (var item in menuK)
            {
                dct.Add(new CmbIntDto { Value = item.SiraNo.Value, Caption = item.MenuAdi });
            }
            ViewBag.cats = dct;
            #endregion

            #region programYetkileri
            var dataKp = KullanicilarBus.GetKullaniciProgramlari(kullaniciId, null);
            ViewBag.KProgramlar = dataKp.Where(p => p.YetkiVar).ToList();
            #endregion
            if (data.KayitDonemID.HasValue)
            {
                ViewBag.Donem = _entities.Donemlers.FirstOrDefault(p => p.DonemID == data.KayitDonemID.Value);

            }
            ViewBag.Enstitu = _entities.Enstitulers.First(p => p.EnstituKod == data.EnstituKod);

            ViewBag.YtuOgrenimB = _entities.OgrenimTipleris.FirstOrDefault(p => p.EnstituKod == kullanici.EnstituKod && p.OgrenimTipKod == kullanici.OgrenimTipKod);
            if (data.DanismanID.HasValue)
                ViewBag.Danisman = _entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == data.DanismanID);
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
            var kullaniciId = (RoleNames.GelenBasvurular.InRoleCurrent() || RoleNames.MulakatSureci.InRoleCurrent() || RoleNames.BasvuruSureci.InRoleCurrent()) ? (int?)null : UserIdentity.Current.Id;
            var basvuru = _entities.Basvurulars.First(p => p.BasvuruID == id && p.KullaniciID == (kullaniciId ?? p.KullaniciID));


            var mdl = new BasvuruDetayDto
            {
                SelectedTabIndex = tbInx,
                BasvuruID = basvuru.BasvuruID,
                BasvuruTarihi = basvuru.BasvuruTarihi,
                KullaniciTipAdi = basvuru.KullaniciTipleri.KullaniciTipAdi,
                ResimYolu = basvuru.ResimAdi,
                AdSoyad = basvuru.Ad + " " + basvuru.Soyad,
                BasvuruDurumID = basvuru.BasvuruDurumID,
                DurumClassName = basvuru.BasvuruDurumlari.ClassName,
                DurumColor = basvuru.BasvuruDurumlari.Color,
                BasvuruDurumAdi = basvuru.BasvuruDurumlari.BasvuruDurumAdi,
                BasvuruDurumAciklamasi = basvuru.BasvuruDurumAciklamasi,

                IsBelgeYuklemeVar = basvuru.BasvuruSurec.IsBelgeYuklemeVar,
                IsYerli = basvuru.KullaniciTipleri.Yerli
            };

            mdl.SelectedTabIndex = tbInx;
            var page = ViewRenderHelper.RenderPartialView("Ajax", "GetBasvuruDetaySablon", mdl);
            return Json(new { page = page, IsAuthenticated = UserIdentity.Current.IsAuthenticated }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        public ActionResult GetBasvuruDetaySablon(BasvuruDetayDto model)
        {
            return View(model);
        }
        [Authorize]
        public ActionResult GetBasvuruSubData(int id, int tbInx, bool isSave = false)
        {

            var page = "";
            var kullaniciId = RoleNames.GelenBasvurular.InRoleCurrent() || RoleNames.MulakatSureci.InRoleCurrent() || RoleNames.BasvuruSureci.InRoleCurrent() ? (int?)null : UserIdentity.Current.Id;
            var basvuru = _entities.Basvurulars.First(p => p.BasvuruID == id && p.KullaniciID == (kullaniciId ?? p.KullaniciID));

            var mdl = new BasvuruDetayDto
            {
                BasvuruSurecID = basvuru.BasvuruSurecID,
                IsSave = isSave,
                SelectedTabIndex = tbInx
            };

            #region UstBilgi

            mdl.SelectedTabIndex = tbInx;
            mdl.BasvuruID = basvuru.BasvuruID;
            mdl.RowID = basvuru.RowID;
            mdl.BasvuruTarihi = basvuru.BasvuruTarihi;
            mdl.KullaniciTipID = basvuru.KullaniciTipID.Value;
            var kullaniciTipBilgi = basvuru.KullaniciTipleri;
            mdl.IsYerli = kullaniciTipBilgi.Yerli;
            mdl.KullaniciTipAdi = kullaniciTipBilgi.KullaniciTipAdi;
            mdl.ResimYolu = basvuru.ResimAdi;
            mdl.AdSoyad = basvuru.Ad + " " + basvuru.Soyad;
            mdl.BasvuruDurumID = basvuru.BasvuruDurumID;
            mdl.DurumClassName = basvuru.BasvuruDurumlari.ClassName;
            mdl.DurumColor = basvuru.BasvuruDurumlari.Color;
            mdl.BasvuruDurumAdi = basvuru.BasvuruDurumlari.BasvuruDurumAdi;
            mdl.BasvuruDurumAciklamasi = basvuru.BasvuruDurumAciklamasi;

            mdl.IsHesaplandi = basvuru.MulakatSonuclaris.Any(a => a.MulakatSonucTipID != MulakatSonucTipi.Hesaplanmadı);

            mdl.IsBelgeYuklemeVar = basvuru.BasvuruSurec.IsBelgeYuklemeVar;
            var mulakatSonucTipIDs = new List<int> { MulakatSonucTipi.Asil, MulakatSonucTipi.Yedek };
            var mulakatSonuclaris = basvuru.BasvuruSurec.Basvurulars.Where(p => p.KullaniciID == basvuru.KullaniciID).SelectMany(s => s.MulakatSonuclaris).Where(p => p.MulakatSonucTipID == MulakatSonucTipi.Yedek).ToList();
            if (mulakatSonuclaris.Count <= 1)
            {
                mulakatSonuclaris = basvuru.MulakatSonuclaris.Where(p => mulakatSonucTipIDs.Contains(p.MulakatSonucTipID)).ToList();
            }
            else mdl.IsYedekCokluTercih = true;
            mdl.IsGonderilenMaillerVar = basvuru.GonderilenMaillers.Any();
            mdl.IsKayitHakkiVar = mulakatSonuclaris.Any();
            mdl.KayitIslemiGordu = mulakatSonuclaris.Any(a => a.KayitDurumID.HasValue && a.KayitDurumlari.IsKayitOldu == true);

            #endregion

            if (tbInx == 1)
            {
                #region KimlikBilgisi 
                mdl.TcKimlikNo = basvuru.TcKimlikNo;
                mdl.CinsiyetAdi = basvuru.Cinsiyetler.CinsiyetAdi;
                mdl.AnaAdi = basvuru.AnaAdi;
                mdl.BabaAdi = basvuru.BabaAdi;
                mdl.DogumTarihi = basvuru.DogumTarihi;
                mdl.CiltNo = basvuru.CiltNo;
                mdl.AileNo = basvuru.AileNo;
                mdl.SiraNo = basvuru.SiraNo;

                mdl.UyrukAdi = _entities.Uyruklars.First(p => p.UyrukKod == basvuru.UyrukKod).Ad;
                var ilIlceKods = new List<int?> { basvuru.DogumYeriKod, basvuru.NufusilIlceKod, basvuru.SehirKod };
                var sehirler = _entities.Sehirlers.Where(p => ilIlceKods.Contains(p.SehirKod)).ToList();
                mdl.DogumYeriAdi = sehirler.First(p => p.SehirKod == basvuru.DogumYeriKod).Ad;
                mdl.YasadigiSehirAdi = sehirler.First(p => p.SehirKod == basvuru.SehirKod).Ad;
                if (mdl.IsYerli) mdl.NufusIlIlceAdi = sehirler.First(p => p.SehirKod == basvuru.NufusilIlceKod).Ad;

                mdl.CepTel = basvuru.CepTel;
                mdl.EMail = basvuru.EMail;
                mdl.Adres = basvuru.Adres;
                mdl.Adres2 = basvuru.Adres2;
                #endregion
                page = ViewRenderHelper.RenderPartialView("Ajax", "GetBasvuruKimlikBilgisi", mdl);
            }
            else if (tbInx == 2)
            {
                #region TercihBilgileri
                mdl.LUniversiteAdi = _entities.Universitelers.First(p => p.UniversiteID == basvuru.LUniversiteID).Ad;
                mdl.LFakulteAdi = basvuru.LFakulteAdi;
                mdl.LBolumAdi = _entities.OgrenciBolumleris.First(p => p.OgrenciBolumID == basvuru.LOgrenciBolumID).BolumAdi;
                mdl.LNotSistemi = _entities.NotSistemleris.First(p => p.NotSistemID == basvuru.LNotSistemID).NotSistemAdi;
                mdl.LMezuniyetNotu = basvuru.LMezuniyetNotu;
                mdl.LMezuniyetNotu100LukSistem = basvuru.LMezuniyetNotu100LukSistem;
                mdl.LEgitimDiliTurkce = basvuru.LEgitimDiliTurkce;
                mdl.LegitimDilAdi = basvuru.LEgitimDiliTurkce.HasValue ? (basvuru.LEgitimDiliTurkce.Value ? "Türkçe" : "İngilizce") : "";

                if (basvuru.YLUniversiteID.HasValue)
                {
                    mdl.YLUniversiteID = basvuru.YLUniversiteID;
                    mdl.YLUniversiteAdi = _entities.Universitelers.First(p => p.UniversiteID == basvuru.YLUniversiteID).Ad;
                    mdl.YLFakulteAdi = basvuru.YLFakulteAdi;
                    mdl.YLBolumAdi = _entities.OgrenciBolumleris.First(p => p.OgrenciBolumID == basvuru.YLOgrenciBolumID).BolumAdi;
                    mdl.YLNotSistemi = _entities.NotSistemleris.First(p => p.NotSistemID == basvuru.YLNotSistemID).NotSistemAdi;
                    mdl.YLMezuniyetNotu = basvuru.YLMezuniyetNotu;
                    mdl.YLMezuniyetNotu100LukSistem = basvuru.YLMezuniyetNotu100LukSistem;
                    mdl.YLEgitimDiliTurkce = basvuru.YLEgitimDiliTurkce;
                    mdl.YLegitimDilAdi = basvuru.YLEgitimDiliTurkce.HasValue ? (basvuru.YLEgitimDiliTurkce.Value ? "Türkçe" : "İngilizce") : "";
                }
                if (basvuru.DRUniversiteID.HasValue)
                {
                    mdl.DRUniversiteID = basvuru.DRUniversiteID;
                    mdl.DRUniversiteAdi = _entities.Universitelers.First(p => p.UniversiteID == basvuru.DRUniversiteID).Ad;
                    mdl.DRFakulteAdi = basvuru.DRFakulteAdi;
                    mdl.DRBolumAdi = _entities.OgrenciBolumleris.First(p => p.OgrenciBolumID == basvuru.DROgrenciBolumID).BolumAdi;
                    mdl.DRNotSistemi = _entities.NotSistemleris.First(p => p.NotSistemID == basvuru.DRNotSistemID).NotSistemAdi;
                    mdl.DRMezuniyetNotu = basvuru.DRMezuniyetNotu;
                    mdl.DRMezuniyetNotu100LukSistem = basvuru.DRMezuniyetNotu100LukSistem;
                    mdl.DREgitimDiliTurkce = basvuru.DREgitimDiliTurkce;
                    mdl.DRegitimDilAdi = basvuru.DREgitimDiliTurkce.HasValue ? (basvuru.DREgitimDiliTurkce.Value ? "Türkçe" : "İngilizce") : "";
                }
                mdl.Tercihlers = (from s in basvuru.BasvurularTercihleris
                                  join at in _entities.AlanTipleris on s.AlanTipID equals at.AlanTipID
                                  join ot in _entities.OgrenimTipleris.Where(p => p.EnstituKod == basvuru.BasvuruSurec.EnstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod
                                  join pr in _entities.Programlars on s.ProgramKod equals pr.ProgramKod
                                  join als in _entities.AlesTipleris on pr.AlesTipID equals als.AlesTipID
                                  join abd in _entities.AnabilimDallaris on pr.AnabilimDaliKod equals abd.AnabilimDaliKod
                                  select new BasvuruTercihDto
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
                page = ViewRenderHelper.RenderPartialView("Ajax", "GetBasvuruOgrenimTercihBilgisi", mdl);
            }
            else if (tbInx == 3)
            {
                #region SınavBilgileri
                var basvuruSurec = basvuru.BasvuruSurec;
                var sinavlars = basvuru.BasvurularSinavBilgis.ToList();
                var sinavTipIDs = sinavlars.Select(s => s.SinavTipID).ToList();
                var sinavDilleris = sinavlars.Where(p => p.SinavDilleri != null).Select(s => s.SinavDilleri).ToList();

                var bSurecSinavBilgis = basvuru.BasvuruSurec.BasvuruSurecSinavTipleris.Where(p => sinavTipIDs.Contains(p.SinavTipID)).ToList();
                var sinavTipleris = bSurecSinavBilgis.Select(s => s.SinavTipleri).ToList();
                var sinavTipGroups = sinavlars.Select(s => s.SinavTipGruplari).ToList();
                var sinavTipleriLngs = sinavTipleris.ToList();
                mdl.LEgitimDiliTurkce = basvuru.LEgitimDiliTurkce;
                mdl.YLEgitimDiliTurkce = basvuru.YLEgitimDiliTurkce;
                mdl.IsTurkceProgramVar = basvuru.BasvurularTercihleris.Any(a => !a.Programlar.Ingilizce);
                mdl.Sinavlars = (from s in sinavlars
                                 join bs in bSurecSinavBilgis on s.SinavTipID equals bs.SinavTipID
                                 join st in sinavTipleris on s.SinavTipID equals st.SinavTipID
                                 join stl in sinavTipleriLngs on st.SinavTipID equals stl.SinavTipID
                                 join stg in sinavTipGroups on s.SinavTipGrupID equals stg.SinavTipGrupID
                                 select new BasvuruSinavTipDto
                                 {
                                     EnstituKod = basvuruSurec.EnstituKod,
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
                                     SinavDilAdi = s.SinavDilID.HasValue ? sinavDilleris.First(p => p.SinavDilID == s.SinavDilID).DilAdi : (bs.WebService && s.SinavTipGrupID == SinavTipGrup.DilSinavlari ? s.WsSinavDili : ""),
                                     AlesXmlModel = s.SinavTipGrupID == SinavTipGrup.Ales_Gree && bs.WebService ? s.WsXmlData.toSinavSonucAlesXmlModel() : null,
                                 }).OrderBy(o => o.SinavTipGrupID).ToList();

                #endregion
                page = ViewRenderHelper.RenderPartialView("Ajax", "GetBasvuruSinavBilgileri", mdl);
            }
            else if (tbInx == 4)
            {
                #region Belgeler
                if (mdl.IsBelgeYuklemeVar)
                {

                    var rolAdlari = new List<string>();
                    rolAdlari.Add(RoleNames.GelenBasvurular);
                    rolAdlari.Add(RoleNames.BasvuruSureci);
                    rolAdlari.Add(RoleNames.MulakatSureci);
                    rolAdlari.Add(RoleNames.BasvuruSureciOgrenciKayit);


                    if (mdl.IsHesaplandi)
                    {
                        var tumBilgileriGorsun = UserIdentity.Current.Roles.Any(a => rolAdlari.Contains(a));
                        var basvuruSurec = basvuru.BasvuruSurec;

                        if (mdl.IsKayitHakkiVar)
                        {
                            var belgeKtModel = new List<EntBegeKayitT>();
                            foreach (var item in basvuruSurec.BasvuruSurecOgrenimTipleris.Where(p => p.BelgeYuklemeAsilBasTar.HasValue))
                            {
                                belgeKtModel.Add(new EntBegeKayitT { EnstituKod = basvuruSurec.EnstituKod, OgrenimTipKod = item.OgrenimTipKod, BaslangicTar = item.BelgeYuklemeAsilBasTar.Value, BitisTar = item.BelgeYuklemeAsilBitTar.Value, MulakatSonucTipID = MulakatSonucTipi.Asil });
                                belgeKtModel.Add(new EntBegeKayitT { EnstituKod = basvuruSurec.EnstituKod, OgrenimTipKod = item.OgrenimTipKod, BaslangicTar = item.BelgeYuklemeYedekBasTar.Value, BitisTar = item.BelgeYuklemeYedekBitTar.Value, MulakatSonucTipID = MulakatSonucTipi.Yedek });
                            }

                            var basvurularTercihleris = mulakatSonuclaris.Select(s => s.BasvurularTercihleri).ToList();
                            var ogrenimTipKods = basvurularTercihleris.Select(s => s.OgrenimTipKod).ToList();
                            var ogrenimTipleris = _entities.OgrenimTipleris.Where(p => p.EnstituKod == basvuruSurec.EnstituKod && ogrenimTipKods.Contains(p.OgrenimTipKod)).ToList();
                            var programlars = basvurularTercihleris.Select(s => s.Programlar).ToList();

                            var nowDate = DateTime.Now;
                            mdl.Tercihlers = (from s in basvurularTercihleris
                                              join ms in mulakatSonuclaris on s.BasvuruTercihID equals ms.BasvuruTercihID
                                              join ot in ogrenimTipleris on s.OgrenimTipKod equals ot.OgrenimTipKod
                                              join prl in programlars on s.ProgramKod equals prl.ProgramKod
                                              select new BasvuruTercihDto
                                              {
                                                  IsSeciliBasvuruyaAitTercih = s.BasvuruID == basvuru.BasvuruID,
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
                                                  IsBelgeYuklemeAktif = tumBilgileriGorsun || belgeKtModel.Any(a2 => s.OgrenimTipKod == a2.OgrenimTipKod && ms.MulakatSonucTipID == a2.MulakatSonucTipID && a2.BaslangicTar <= nowDate && a2.BitisTar >= nowDate)

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

                            var basvuruSurecBelgeTipleris = basvuruSurec.BasvuruSurecBelgeTipleris.ToList();

                            foreach (var item in mdl.Tercihlers.Select(s => s.BasvuruID).Distinct())
                            {
                                #region BelgelerSet
                                var basvuruB = basvuruSurec.Basvurulars.First(p => p.BasvuruID == item);
                                var basvurularYuklenenBelgelers = basvuruB.BasvurularYuklenenBelgelers.Select(s => new { s.BasvurularYuklenenBelgeID, s.BasvuruBelgeTipID, s.SinavTipID, s.BelgeAdi, s.BelgeYolu, s.IsOnaylandi, s.OnaylamaTarihi, s.IslemTarihi }).ToList();
                                var bsKimlikBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.KimlikBelgesi);
                                if (bsKimlikBelgesi != null)
                                {
                                    var kb = new BasvuruBelgeDto
                                    {
                                        SiraNo = 1,
                                        BasvuruID = basvuruB.BasvuruID,
                                        BasvuruBelgeTipID = BasvuruBelgeTipi.KimlikBelgesi,
                                        BasvuruBelgeTipAdi = bsKimlikBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                    };
                                    var kimlikBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.KimlikBelgesi);
                                    if (kimlikBelgesi != null)
                                    {
                                        kb.BasvurularYuklenenBelgeID = kimlikBelgesi.BasvurularYuklenenBelgeID;
                                        kb.BelgeAdi = kimlikBelgesi.BelgeAdi;
                                        kb.BelgeYolu = kimlikBelgesi.BelgeYolu;
                                        kb.IsOnaylandi = kimlikBelgesi.IsOnaylandi;
                                        kb.OnaylamaTarihi = kimlikBelgesi.OnaylamaTarihi;
                                        kb.IslemTarihi = kimlikBelgesi.IslemTarihi;

                                    }
                                    mdl.Belgelers.Add(kb);

                                }
                                var bsLEgitimBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.LEgitimBelgesi);
                                if (bsLEgitimBelgesi != null)
                                {
                                    var kb = new BasvuruBelgeDto
                                    {
                                        SiraNo = 2,
                                        BasvuruID = basvuruB.BasvuruID,
                                        BasvuruBelgeTipID = BasvuruBelgeTipi.LEgitimBelgesi,
                                        BasvuruBelgeTipAdi = bsLEgitimBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                    };
                                    var lEgitimBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.LEgitimBelgesi);
                                    if (lEgitimBelgesi != null)
                                    {
                                        kb.BasvurularYuklenenBelgeID = lEgitimBelgesi.BasvurularYuklenenBelgeID;
                                        kb.BelgeAdi = lEgitimBelgesi.BelgeAdi;
                                        kb.BelgeYolu = lEgitimBelgesi.BelgeYolu;
                                        kb.IsOnaylandi = lEgitimBelgesi.IsOnaylandi;
                                        kb.OnaylamaTarihi = lEgitimBelgesi.OnaylamaTarihi;
                                        kb.IslemTarihi = lEgitimBelgesi.IslemTarihi;

                                    }
                                    mdl.Belgelers.Add(kb);
                                    if (!basvuruB.YLUniversiteID.HasValue)
                                        kb.Not = "Diplomanız hazır değilse e-devlet doğrulanabilir mezuniyet belgenizi yüklemeniz şartıyla bu kısmı boş bırakabilir ancak yüz yüze eğitim başladıktan sonra diplomanızın aslını ibraz ederek fotokopisini Enstitümüze teslim etmeniz gerekmektedir.";


                                }
                                if (basvuruB.YLUniversiteID.HasValue)
                                {
                                    var bsYlEgitimBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.YLEgitimBelgesi);
                                    if (bsYlEgitimBelgesi != null)
                                    {
                                        var kb = new BasvuruBelgeDto
                                        {

                                            SiraNo = 8,
                                            BasvuruID = basvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipi.YLEgitimBelgesi,
                                            BasvuruBelgeTipAdi = bsYlEgitimBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };
                                        var ylEgitimBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.YLEgitimBelgesi);
                                        if (ylEgitimBelgesi != null)
                                        {
                                            kb.BasvurularYuklenenBelgeID = ylEgitimBelgesi.BasvurularYuklenenBelgeID;
                                            kb.BelgeAdi = ylEgitimBelgesi.BelgeAdi;
                                            kb.BelgeYolu = ylEgitimBelgesi.BelgeYolu;
                                            kb.IsOnaylandi = ylEgitimBelgesi.IsOnaylandi;
                                            kb.OnaylamaTarihi = ylEgitimBelgesi.OnaylamaTarihi;
                                            kb.IslemTarihi = ylEgitimBelgesi.IslemTarihi;

                                        }
                                        mdl.Belgelers.Add(kb);
                                        kb.Not = "Diplomanız hazır değilse e-devlet doğrulanabilir mezuniyet belgenizi yüklemeniz şartıyla bu kısmı boş bırakabilir ancak yüz yüze eğitim başladıktan sonra diplomanızın aslını ibraz ederek fotokopisini Enstitümüze teslim etmeniz gerekmektedir.";

                                    }
                                }
                                var isDrBavurusuVar = basvuruB.BasvurularTercihleris.ToList().Any(a => a.OgrenimTipKod.IsDoktora());
                                var bsMezuniyetBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.MezuniyetBelgesi);
                                if (bsMezuniyetBelgesi != null)
                                {
                                    var kb = new BasvuruBelgeDto
                                    {
                                        SiraNo = !isDrBavurusuVar ? 3 : 9,
                                        BasvuruID = basvuruB.BasvuruID,
                                        BasvuruBelgeTipID = BasvuruBelgeTipi.MezuniyetBelgesi,
                                        BasvuruBelgeTipAdi = bsMezuniyetBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                    };
                                    var mezuniyetBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.MezuniyetBelgesi);
                                    if (mezuniyetBelgesi != null)
                                    {
                                        kb.BasvurularYuklenenBelgeID = mezuniyetBelgesi.BasvurularYuklenenBelgeID;
                                        kb.BelgeAdi = mezuniyetBelgesi.BelgeAdi;
                                        kb.BelgeYolu = mezuniyetBelgesi.BelgeYolu;
                                        kb.IsOnaylandi = mezuniyetBelgesi.IsOnaylandi;
                                        kb.OnaylamaTarihi = mezuniyetBelgesi.OnaylamaTarihi;
                                        kb.IslemTarihi = mezuniyetBelgesi.IslemTarihi;

                                    }

                                    kb.Not = "Tercih edilen program Agno alım kriterlerine göre " + kb.BasvuruBelgeTipAdi + " yüklenecekdir.";

                                    mdl.Belgelers.Add(kb);

                                }
                                if (isDrBavurusuVar)
                                {
                                    var bsMezuniyetBelgesiYl = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.YLMezuniyetBelgesi);
                                    if (bsMezuniyetBelgesiYl != null)
                                    {
                                        var kb = new BasvuruBelgeDto
                                        {
                                            SiraNo = !isDrBavurusuVar ? 4 : 10,
                                            BasvuruID = basvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipi.YLMezuniyetBelgesi,
                                            BasvuruBelgeTipAdi = bsMezuniyetBelgesiYl.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };
                                        var mezuniyetBelgesiYl = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.YLMezuniyetBelgesi);
                                        if (mezuniyetBelgesiYl != null)
                                        {
                                            kb.BasvurularYuklenenBelgeID = mezuniyetBelgesiYl.BasvurularYuklenenBelgeID;
                                            kb.BelgeAdi = mezuniyetBelgesiYl.BelgeAdi;
                                            kb.BelgeYolu = mezuniyetBelgesiYl.BelgeYolu;
                                            kb.IsOnaylandi = mezuniyetBelgesiYl.IsOnaylandi;
                                            kb.OnaylamaTarihi = mezuniyetBelgesiYl.OnaylamaTarihi;
                                            kb.IslemTarihi = mezuniyetBelgesiYl.IslemTarihi;

                                        }

                                        kb.Not = "Tercih edilen program Agno alım kriterlerine göre " + kb.BasvuruBelgeTipAdi + " yüklenecekdir.";

                                        mdl.Belgelers.Add(kb);

                                    }
                                }

                                var bsTranskriptBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.TranskriptBelgesi);
                                if (bsTranskriptBelgesi != null)
                                {

                                    var belgeTipAdi = bsTranskriptBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi;
                                    if (basvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru)
                                    {
                                        belgeTipAdi = isDrBavurusuVar ? "YL ve DR Transkript Belgesi" : "Lisans ve YL Transkript Belgesi";
                                    }
                                    var kb = new BasvuruBelgeDto
                                    {
                                        SiraNo = !isDrBavurusuVar ? 5 : 11,
                                        BasvuruID = basvuruB.BasvuruID,
                                        BasvuruBelgeTipID = BasvuruBelgeTipi.TranskriptBelgesi,
                                        BasvuruBelgeTipAdi = belgeTipAdi,
                                    };
                                    var transkriptBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.TranskriptBelgesi);
                                    if (transkriptBelgesi != null)
                                    {
                                        kb.BasvurularYuklenenBelgeID = transkriptBelgesi.BasvurularYuklenenBelgeID;
                                        kb.BelgeAdi = transkriptBelgesi.BelgeAdi;
                                        kb.BelgeYolu = transkriptBelgesi.BelgeYolu;
                                        kb.IsOnaylandi = transkriptBelgesi.IsOnaylandi;
                                        kb.OnaylamaTarihi = transkriptBelgesi.OnaylamaTarihi;
                                        kb.IslemTarihi = transkriptBelgesi.IslemTarihi;
                                    }
                                    if (basvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru)
                                    {
                                        belgeTipAdi = isDrBavurusuVar ? "Yüksek lisans ve Doktora Eğitimi Transkript Belgesi Yüklenecektir. (Tek PDF Halinde)" : "Lisans ve Yüksek Lisans Eğitimi Transkript Belgesi Yüklenecektir. (Tek PDF Halinde)";
                                        kb.Not = belgeTipAdi;
                                    }
                                    else
                                    {
                                        kb.Not = !isDrBavurusuVar ? "Tercih edilen program Agno alım kriterlerine göre Lisans " + kb.BasvuruBelgeTipAdi + " yüklenecekdir." : "Tercih edilen program Agno alım kriterlerine göre Lisans ve Yüksek Lisans " + kb.BasvuruBelgeTipAdi + "  tek pdf dosyası halinde yüklenecekdir.";
                                    }
                                    mdl.Belgelers.Add(kb);

                                }

                                if (!mdl.IsYerli)
                                {
                                    var bsTaninirlikBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.TaninirlikBelgesi);
                                    if (bsTaninirlikBelgesi != null)
                                    {
                                        var kb = new BasvuruBelgeDto
                                        {
                                            SiraNo = !isDrBavurusuVar ? 6 : 12,
                                            BasvuruID = basvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipi.TaninirlikBelgesi,
                                            BasvuruBelgeTipAdi = bsTaninirlikBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };
                                        var taninirlikBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.TaninirlikBelgesi);
                                        if (taninirlikBelgesi != null)
                                        {
                                            kb.BasvurularYuklenenBelgeID = taninirlikBelgesi.BasvurularYuklenenBelgeID;
                                            kb.BelgeAdi = taninirlikBelgesi.BelgeAdi;
                                            kb.BelgeYolu = taninirlikBelgesi.BelgeYolu;
                                            kb.IsOnaylandi = taninirlikBelgesi.IsOnaylandi;
                                            kb.OnaylamaTarihi = taninirlikBelgesi.OnaylamaTarihi;
                                            kb.IslemTarihi = taninirlikBelgesi.IslemTarihi;

                                        }
                                        kb.Not = !isDrBavurusuVar ? "Lisans eğitimi Yurt dışından mezun olunmuş ise " + kb.BasvuruBelgeTipAdi + " yüklenecekdir." : "Yüksek Lisans eğitimi Yurt dışından mezun olunmuş ise " + kb.BasvuruBelgeTipAdi + " yüklenecekdir.";

                                        mdl.Belgelers.Add(kb);

                                    }
                                }
                                else
                                {
                                    var bsDenklikBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.DenklikBelgesi);
                                    if (bsDenklikBelgesi != null)
                                    {
                                        var kb = new BasvuruBelgeDto
                                        {
                                            SiraNo = !isDrBavurusuVar ? 6 : 12,
                                            BasvuruID = basvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipi.DenklikBelgesi,
                                            BasvuruBelgeTipAdi = bsDenklikBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };
                                        var denklikBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.DenklikBelgesi);
                                        if (denklikBelgesi != null)
                                        {
                                            kb.BasvurularYuklenenBelgeID = denklikBelgesi.BasvurularYuklenenBelgeID;
                                            kb.BelgeAdi = denklikBelgesi.BelgeAdi;
                                            kb.BelgeYolu = denklikBelgesi.BelgeYolu;
                                            kb.IsOnaylandi = denklikBelgesi.IsOnaylandi;
                                            kb.OnaylamaTarihi = denklikBelgesi.OnaylamaTarihi;
                                            kb.IslemTarihi = denklikBelgesi.IslemTarihi;

                                        }
                                        kb.Not = !isDrBavurusuVar ? "Lisans eğitimi Yurt dışından mezun olunmuş ise " + kb.BasvuruBelgeTipAdi + " yüklenecekdir." : "Yüksek Lisans eğitimi Yurt dışından mezun olunmuş ise " + kb.BasvuruBelgeTipAdi + " yüklenecekdir.";

                                        mdl.Belgelers.Add(kb);

                                    }
                                }

                                var sinavTipleris = basvuruB.BasvurularSinavBilgis.ToList();

                                if (sinavTipleris.Any(a => a.SinavTipGrupID == SinavTipGrup.Ales_Gree))
                                {


                                    var bsAlesGreSinavBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.AlesGreSinaviBelgesi);
                                    if (bsAlesGreSinavBelgesi != null)
                                    {
                                        var alesGreSinaviBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.AlesGreSinaviBelgesi);
                                        var kb = new BasvuruBelgeDto
                                        {
                                            SiraNo = 13,
                                            BasvuruID = basvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipi.AlesGreSinaviBelgesi,
                                            BasvuruBelgeTipAdi = bsAlesGreSinavBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };
                                        if (alesGreSinaviBelgesi != null)
                                        {
                                            kb.BasvurularYuklenenBelgeID = alesGreSinaviBelgesi.BasvurularYuklenenBelgeID;
                                            kb.SinavTipID = alesGreSinaviBelgesi.SinavTipID;
                                            kb.BelgeAdi = alesGreSinaviBelgesi.BelgeAdi;
                                            kb.BelgeYolu = alesGreSinaviBelgesi.BelgeYolu;
                                            kb.IsOnaylandi = alesGreSinaviBelgesi.IsOnaylandi;
                                            kb.OnaylamaTarihi = alesGreSinaviBelgesi.OnaylamaTarihi;
                                            kb.IslemTarihi = alesGreSinaviBelgesi.IslemTarihi;

                                        }
                                        var doktoraMezuniyetiVar = sinavTipleris.Any(a => a.SinavTipKod == 99);
                                        if (doktoraMezuniyetiVar)
                                        {
                                            kb.Not = "Doktora mezuniyetini gösteren belgenin yüklenmesi gerekmektedir.";
                                        }
                                        mdl.Belgelers.Add(kb);

                                    }
                                }
                                if (sinavTipleris.Any(a => a.SinavTipGrupID == SinavTipGrup.DilSinavlari))
                                {
                                    var bsDilSinaviBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.DilSinaviBelgesi);
                                    if (bsDilSinaviBelgesi != null)
                                    {
                                        var kb = new BasvuruBelgeDto
                                        {
                                            SiraNo = 14,
                                            BasvuruID = basvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipi.DilSinaviBelgesi,
                                            BasvuruBelgeTipAdi = bsDilSinaviBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };

                                        var dilSinaviBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.DilSinaviBelgesi);
                                        if (dilSinaviBelgesi != null)
                                        {
                                            kb.BasvurularYuklenenBelgeID = dilSinaviBelgesi.BasvurularYuklenenBelgeID;
                                            kb.SinavTipID = dilSinaviBelgesi.SinavTipID;
                                            kb.BelgeAdi = dilSinaviBelgesi.BelgeAdi;
                                            kb.BelgeYolu = dilSinaviBelgesi.BelgeYolu;
                                            kb.IsOnaylandi = dilSinaviBelgesi.IsOnaylandi;
                                            kb.OnaylamaTarihi = dilSinaviBelgesi.OnaylamaTarihi;
                                            kb.IslemTarihi = dilSinaviBelgesi.IslemTarihi;
                                        }
                                        mdl.Belgelers.Add(kb);

                                    }
                                }
                                if (sinavTipleris.Any(a => a.SinavTipGrupID == SinavTipGrup.Tomer))
                                {
                                    var tomerBilgi = sinavTipleris.FirstOrDefault(a => a.SinavTipGrupID == SinavTipGrup.Tomer);
                                    var bsTomerSinaviBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipi.TomerSinaviBelgesi);


                                    bool isBelgeYukleme = false;

                                    string not = "";
                                    if (tomerBilgi != null)
                                    {
                                        isBelgeYukleme = true;
                                        if (!mdl.IsTurkceProgramVar)
                                        {
                                            isBelgeYukleme = true;
                                        }
                                        else if (basvuruB.LEgitimDiliTurkce == true || basvuruB.YLEgitimDiliTurkce == true)
                                        {
                                            var belgeTuruAdi = "";
                                            if (basvuruB.LEgitimDiliTurkce == true && basvuru.YLEgitimDiliTurkce == true) belgeTuruAdi = "Lisans veya Lisansüstü";
                                            else if (basvuruB.LEgitimDiliTurkce == true) belgeTuruAdi = "Lisans";
                                            else if (basvuruB.YLEgitimDiliTurkce == true) belgeTuruAdi = "Lisansüstü";
                                            isBelgeYukleme = true;
                                            not = "Verilen Taahhüte göre Türkiye'de Türkçe eğitim veren " + (belgeTuruAdi) + " programlarından mezun olunduğunu gösteren karekodlu E'Devlet mezuniyet belgesininin yüklenmesi gerekmektedir.";

                                        }
                                    }

                                    if (bsTomerSinaviBelgesi != null && isBelgeYukleme)
                                    {
                                        var tomerSinaviBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipi.TomerSinaviBelgesi);
                                        var kb = new BasvuruBelgeDto
                                        {
                                            SiraNo = 15,
                                            BasvuruID = basvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipi.TomerSinaviBelgesi,
                                            BasvuruBelgeTipAdi = bsTomerSinaviBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };

                                        if (not != null) kb.Not = not;
                                        if (tomerSinaviBelgesi != null)
                                        {
                                            kb.BasvurularYuklenenBelgeID = tomerSinaviBelgesi.BasvurularYuklenenBelgeID;
                                            kb.SinavTipID = tomerSinaviBelgesi.SinavTipID;
                                            kb.BelgeAdi = tomerSinaviBelgesi.BelgeAdi;
                                            kb.BelgeYolu = tomerSinaviBelgesi.BelgeYolu;
                                            kb.IsOnaylandi = tomerSinaviBelgesi.IsOnaylandi;
                                            kb.OnaylamaTarihi = tomerSinaviBelgesi.OnaylamaTarihi;
                                            kb.IslemTarihi = tomerSinaviBelgesi.IslemTarihi;

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
                page = ViewRenderHelper.RenderPartialView("Ajax", "GetBasvuruBelgeYukleme", mdl);
            }



            var json = Json(new
            {
                page

            }, "application/json", JsonRequestBehavior.AllowGet);
            json.MaxJsonLength = int.MaxValue;
            return json;
        }
        [Authorize]
        public ActionResult GetBasvuruKimlikBilgisi(BasvuruDetayDto model)
        {
            return View(model);
        }
        [Authorize]
        public ActionResult GetBasvuruOgrenimTercihBilgisi(BasvuruDetayDto model)
        {
            return View(model);
        }
        [Authorize]
        public ActionResult GetBasvuruSinavBilgileri(BasvuruDetayDto model)
        {
            return View(model);
        }
        [Authorize]
        public ActionResult GetBasvuruBelgeYukleme(BasvuruDetayDto model)
        {
            return View(model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult BasvuruDosyaEklePost(int basvuruSurecId, int basvuruId, int kullaniciId, int basvuruBelgeTipId, HttpPostedFileBase belgeDosyasi)
        {
            var mMessage = new MmMessage
            {
                MessageType = Msgtype.Warning
            };
            var kayitYetki = RoleNames.BasvuruSureciOgrenciKayit.InRoleCurrent();
            var basv = _entities.Basvurulars.First(p => p.BasvuruID == basvuruId);
            var varolanBelge = basv.BasvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == basvuruBelgeTipId);
            var belgeTipi = basv.BasvuruSurec.BasvuruSurecBelgeTipleris.First(p => p.BasvuruBelgeTipID == basvuruBelgeTipId);
            string basvuruBelgeTipAdi = belgeTipi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi;
            var mSonucTip = new List<int> { MulakatSonucTipi.Asil, MulakatSonucTipi.Yedek };

            var donemdekiBasvurulari = basv.BasvuruSurec.Basvurulars.Where(a => a.KullaniciID == basv.KullaniciID).ToList();
            var donemdekiBasvuruSonuclari = donemdekiBasvurulari.SelectMany(s => s.MulakatSonuclaris).ToList();
            var basvuruSurec = basv.BasvuruSurec;

            var belgeKtModel = new List<EntBegeKayitT>();
            var isBelgeYuklemeAktif = false;
            if (kayitYetki == false)
            {
                var nowDate = DateTime.Now;
                foreach (var item in basvuruSurec.BasvuruSurecOgrenimTipleris.Where(p => p.BelgeYuklemeAsilBasTar.HasValue))
                {
                    belgeKtModel.Add(new EntBegeKayitT { EnstituKod = basvuruSurec.EnstituKod, OgrenimTipKod = item.OgrenimTipKod, BaslangicTar = item.BelgeYuklemeAsilBasTar.Value, BitisTar = item.BelgeYuklemeAsilBitTar.Value, MulakatSonucTipID = MulakatSonucTipi.Asil });
                    belgeKtModel.Add(new EntBegeKayitT { EnstituKod = basvuruSurec.EnstituKod, OgrenimTipKod = item.OgrenimTipKod, BaslangicTar = item.BelgeYuklemeYedekBasTar.Value, BitisTar = item.BelgeYuklemeYedekBitTar.Value, MulakatSonucTipID = MulakatSonucTipi.Yedek });
                }
                foreach (var itemB in basv.MulakatSonuclaris.Where(p => mSonucTip.Contains(p.MulakatSonucTipID) && p.BasvurularTercihleri.IsSecilenTercih == true))
                {
                    if (!isBelgeYuklemeAktif) isBelgeYuklemeAktif = belgeKtModel.Any(a2 => a2.EnstituKod == basvuruSurec.EnstituKod && itemB.BasvurularTercihleri.OgrenimTipKod == a2.OgrenimTipKod && itemB.MulakatSonucTipID == a2.MulakatSonucTipID && a2.BaslangicTar <= nowDate && a2.BitisTar >= nowDate);

                }



            }

            if (!kayitYetki && basv.KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Bu başvuru üstünde işlem yapmaya yetkili değilsiniz.");
            }
            else if (basv.MulakatSonuclaris.Any(a => a.KayitDurumID.HasValue && a.KayitDurumlari.IsKayitOldu == true))
            {
                mMessage.Messages.Add("Başvurunuz için kayıt işlemi süreci tamamlandığından başvuruda herhangi işlemi yapılamaz!");
            }
            else if (donemdekiBasvuruSonuclari.Count(p => mSonucTip.Contains(p.MulakatSonucTipID)) == donemdekiBasvuruSonuclari.Count(a => a.KayitDurumID.HasValue))
            {
                mMessage.Messages.Add("Başvurunuz için kayıt işlemi süreci tamamlandığından başvuruda herhangi işlemi yapılamaz!");

            }
            else if (belgeDosyasi != null && belgeDosyasi.ContentLength > (1024 * 1024 * 5))
            {
                mMessage.Messages.Add("Yükleyeceğiniz dosya boyutu en fazla 5MB olmalıdır.");
            }
            else if (!kayitYetki && !isBelgeYuklemeAktif)
            {
                mMessage.Messages.Add("Sistem belge yükleme işlemlerine kapalıdır.");
            }
            else
            {
                if (varolanBelge != null && varolanBelge.IsOnaylandi)
                {
                    mMessage.Messages.Add(varolanBelge.BelgeAdi + " isimli " + basvuruBelgeTipAdi + " dosyası onaylandığından belge kayıt işlemi yapılamaz.");
                }
                else if (belgeDosyasi == null && varolanBelge == null)
                {
                    mMessage.Messages.Add(basv.KullaniciTipleri.Yerli ? (basvuruBelgeTipAdi + " yüklemek için dosya seçiniz.") : "Pasaport belgesini yüklemek için dosya seçiniz.");
                }
                else if (belgeDosyasi != null && belgeDosyasi.FileName.Split('.').Last().ToLower() != "pdf")
                {
                    mMessage.Messages.Add("Yükleyeceğiniz " + basvuruBelgeTipAdi + " dosyası pdf türünde olmalıdır.");
                }
            }
            if (mMessage.Messages.Count == 0)
            {



                string belgeAdi = "";
                string varOlanBelgeAdi = "";
                if (belgeDosyasi != null)
                {
                    int? sinavTipId = null;
                    int? sinavTipGrupId = null;
                    if (basvuruBelgeTipId == BasvuruBelgeTipi.AlesGreSinaviBelgesi) sinavTipGrupId = SinavTipGrup.Ales_Gree;
                    else if (basvuruBelgeTipId == BasvuruBelgeTipi.DilSinaviBelgesi) sinavTipGrupId = SinavTipGrup.DilSinavlari;
                    else if (basvuruBelgeTipId == BasvuruBelgeTipi.TomerSinaviBelgesi) sinavTipGrupId = SinavTipGrup.Tomer;
                    if (sinavTipGrupId.HasValue)
                    {

                        sinavTipId = basv.BasvurularSinavBilgis.First(p => p.SinavTipGrupID == sinavTipGrupId).SinavTipID;
                    }
                    string dosyaYolu = "/BasvuruDosyalari/BasvuruKayitBelgeleri/" + belgeDosyasi.FileName.ToFileNameAddGuid(null, basv.BasvuruID.ToString());
                    belgeDosyasi.SaveAs(Server.MapPath("~" + dosyaYolu));
                    belgeAdi = belgeDosyasi.FileName.GetFileName();
                    _entities.BasvurularYuklenenBelgelers.Add(new BasvurularYuklenenBelgeler
                    {
                        BasvuruID = basvuruId,
                        BasvuruBelgeTipID = basvuruBelgeTipId,
                        SinavTipID = sinavTipId,
                        BelgeAdi = belgeAdi,
                        BelgeYolu = dosyaYolu,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip,
                    });


                }
                if (varolanBelge != null)
                {
                    varOlanBelgeAdi = varolanBelge.BelgeAdi;
                    var path = Server.MapPath("~" + varolanBelge.BelgeYolu);

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
                    _entities.BasvurularYuklenenBelgelers.Remove(varolanBelge);
                }
                _entities.SaveChanges();
                if (belgeDosyasi == null && !varOlanBelgeAdi.IsNullOrWhiteSpace())
                {
                    mMessage.Title = "Belge silme işlemi başarılı";

                    mMessage.Messages.Add(belgeAdi + " isimli " + basvuruBelgeTipAdi + " dosyası sistemden silindi.");
                }
                else
                {
                    mMessage.Title = "Belge yükleme işlemi başarılı";
                    mMessage.Messages.Add(belgeAdi + " isimli " + basvuruBelgeTipAdi + " dosyası sisteme yüklendi.");
                }

                mMessage.IsSuccess = true;
                mMessage.MessageType = Msgtype.Success;

            }
            else mMessage.Title = "Belge yükleme işlemi başarısız";
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);
            return Json(new { IsSuccess = mMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        public ActionResult BasvuruDosyaOnayla(int basvuruId, int basvuruBelgeTipId, bool isOnaylandi)
        {
            var mMessage = new MmMessage
            {
                MessageType = Msgtype.Warning
            };

            var kayitYetki = RoleNames.BasvuruSureciOgrenciKayit.InRoleCurrent();

            var basv = _entities.Basvurulars.First(p => p.BasvuruID == basvuruId);
            var varolanBelge = basv.BasvurularYuklenenBelgelers.First(p => p.BasvuruBelgeTipID == basvuruBelgeTipId);
            var belgeTipi = basv.BasvuruSurec.BasvuruSurecBelgeTipleris.First(p => p.BasvuruBelgeTipID == basvuruBelgeTipId);
            string basvuruBelgeTipAdi = belgeTipi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi;

            if (!kayitYetki)
            {
                mMessage.Messages.Add("Bu işlemi yapmaya yetkili değilsiniz.");
            }
            if (mMessage.Messages.Count == 0)
            {
                varolanBelge.IsOnaylandi = isOnaylandi;
                varolanBelge.OnaylamaTarihi = DateTime.Now;
                varolanBelge.OnaylamaYapanID = UserIdentity.Current.Id;
                varolanBelge.OnaylamaYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                mMessage.IsSuccess = true;
                mMessage.MessageType = Msgtype.Success;
                mMessage.Messages.Add(varolanBelge.BelgeAdi + " isimli " + basvuruBelgeTipAdi + " dosyası " + (isOnaylandi ? "onaylandı." : "onayı kaldırıldı"));
            }

            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);
            return Json(new { mMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        public ActionResult BasvuruKayitOlunacakTercihSet(int id, string uniqueId, bool? isSecildi)
        {

            var mMessage = new MmMessage();
            var kayityetki = RoleNames.BasvuruSureciOgrenciKayit.InRoleCurrent();

            var basvuru = _entities.Basvurulars.First(p => p.BasvuruID == id && p.KullaniciID == (!kayityetki ? UserIdentity.Current.Id : p.KullaniciID));
            var basvuruSurec = basvuru.BasvuruSurec;
            var isBelgeYuklemeAktif = true;
            var belgeKtModel = new List<EntBegeKayitT>();

            var donemdekiBasvurulari = basvuruSurec.Basvurulars.Where(a => a.KullaniciID == basvuru.KullaniciID).ToList();
            var donemdekiBasvuruSonuclari = donemdekiBasvurulari.SelectMany(s => s.MulakatSonuclaris).ToList();
            var mSonucTip = new List<int> { MulakatSonucTipi.Asil, MulakatSonucTipi.Yedek };

            if (uniqueId.IsNullOrWhiteSpace())
            {
                if (donemdekiBasvuruSonuclari.Any(a => a.KayitDurumID.HasValue && a.KayitDurumlari.IsKayitOldu == true))
                {
                    mMessage.Messages.Add("Başvurunuz için kayıt işlemi süreci tamamlandığından başvuruda herhangi işlemi yapılamaz!");
                }
                else if (donemdekiBasvuruSonuclari.Count(a => mSonucTip.Contains(a.MulakatSonucTipID)) == donemdekiBasvuruSonuclari.Count(a => a.KayitDurumID.HasValue))
                {
                    mMessage.Messages.Add("Başvurunuz için kayıt işlemi süreci tamamlandığından başvuruda herhangi işlemi yapılamaz!");
                }
                else
                {
                    foreach (var item in donemdekiBasvurulari.SelectMany(s => s.BasvurularTercihleris))
                    {
                        item.IsSecilenTercih = false;
                        item.IslemTarihi = DateTime.Now;
                        item.IslemYapanID = UserIdentity.Current.Id;
                        item.IslemYapanIP = UserIdentity.Ip;
                    }
                    _entities.SaveChanges();
                    mMessage.IsSuccess = true;
                    mMessage.Messages.Add("Tercih seçimi kaldırıldı!");
                }
            }
            else
            {
                var tGuid = new Guid(uniqueId);
                var tercih = basvuru.BasvurularTercihleris.First(p => p.UniqueID == tGuid);

                var mulakatSonucu = tercih.MulakatSonuclaris.First();

                if (kayityetki == false)
                {

                    foreach (var item in basvuruSurec.BasvuruSurecOgrenimTipleris.Where(p => p.BelgeYuklemeAsilBasTar.HasValue))
                    {
                        belgeKtModel.Add(new EntBegeKayitT { EnstituKod = basvuruSurec.EnstituKod, OgrenimTipKod = item.OgrenimTipKod, BaslangicTar = item.BelgeYuklemeAsilBasTar.Value, BitisTar = item.BelgeYuklemeAsilBitTar.Value, MulakatSonucTipID = MulakatSonucTipi.Asil });
                        belgeKtModel.Add(new EntBegeKayitT { EnstituKod = basvuruSurec.EnstituKod, OgrenimTipKod = item.OgrenimTipKod, BaslangicTar = item.BelgeYuklemeYedekBasTar.Value, BitisTar = item.BelgeYuklemeYedekBitTar.Value, MulakatSonucTipID = MulakatSonucTipi.Yedek });
                    }

                    var nowDate = DateTime.Now;
                    isBelgeYuklemeAktif = belgeKtModel.Any(a2 => a2.EnstituKod == basvuruSurec.EnstituKod && tercih.OgrenimTipKod == a2.OgrenimTipKod && mulakatSonucu.MulakatSonucTipID == a2.MulakatSonucTipID && a2.BaslangicTar <= nowDate && a2.BitisTar >= nowDate);

                    if (tercih.MulakatSonuclaris.First().KayitDurumID.HasValue)
                    {
                        mMessage.Messages.Add("Seçtiğiniz tercih işlem gördüğünden herhangi bir değişiklik yapamazsınız!");
                    }
                    else if (donemdekiBasvuruSonuclari.Any(a => a.BasvuruTercihID != tercih.BasvuruTercihID && (a.KayitDurumID == KayitDurumu.KayitOldu || a.KayitDurumID == KayitDurumu.OnKayit)))
                    {
                        mMessage.Messages.Add("Kayıt/Ön kayıt işlemi gören tercihiniz bulunduğu için başka tercih seçimi yapamazsınız!");
                    }
                }

                if (!isBelgeYuklemeAktif)
                {
                    var sonucTipAdi = mulakatSonucu.MulakatSonucTipID == MulakatSonucTipi.Asil ? "Asil" : "Yedek";
                    var tarihKriteri = belgeKtModel.FirstOrDefault(a2 => a2.EnstituKod == basvuruSurec.EnstituKod && tercih.OgrenimTipKod == a2.OgrenimTipKod && mulakatSonucu.MulakatSonucTipID == a2.MulakatSonucTipID);

                    if (tarihKriteri != null)
                    {
                        var tarih = tarihKriteri.BaslangicTar.ToString("yyyy-MM-dd HH:mm") + " / " + tarihKriteri.BitisTar.ToString("yyyy-MM-dd HH:mm");
                        mMessage.Messages.Add(sonucTipAdi + " kontenjandan kayıt hakkın kazananlar için belge yükleme işlemi " + tarih + "  tarihleri arasında yapılabilir!");
                    }
                    else mMessage.Messages.Add("Belge yükleme tarih aralığı belirlenmediği için tercih seçimi yapılamaz!");
                }

                else if (donemdekiBasvuruSonuclari.Count(a => mSonucTip.Contains(a.MulakatSonucTipID)) == donemdekiBasvuruSonuclari.Count(a => a.KayitDurumID.HasValue))
                {

                    mMessage.Messages.Add("Başvurunuz için kayıt işlemi süreci tamamlandığından başvuruda herhangi işlemi yapılamaz!");

                }

                else
                {
                    tercih.IsSecilenTercih = isSecildi == true;
                    var digerTercihler = donemdekiBasvurulari.SelectMany(s => s.BasvurularTercihleris).Where(p => p.MulakatSonuclaris.Any(a => a.MulakatSonucTipID == mulakatSonucu.MulakatSonucTipID) && p.IsSecilenTercih != null && p.UniqueID != tercih.UniqueID).ToList();
                    foreach (var item in digerTercihler)
                    {
                        item.IsSecilenTercih = false;
                        item.IslemTarihi = DateTime.Now;
                        item.IslemYapanID = UserIdentity.Current.Id;
                        item.IslemYapanIP = UserIdentity.Ip;
                    }
                    tercih.IslemTarihi = DateTime.Now;
                    tercih.IslemYapanID = UserIdentity.Current.Id;
                    tercih.IslemYapanIP = UserIdentity.Ip;
                    _entities.SaveChanges();
                    mMessage.IsSuccess = true;
                    if (isSecildi == true)
                        mMessage.Messages.Add("Kayıt olunmak istenen program seçildi.");
                    else
                    {
                        mMessage.Messages.Add("Kayıt olunmak istenen program seçimi kaldırıldı.");
                    }

                    #region sendMail
                    if (isSecildi == true)
                    {
                        var htmlBigliRow = new List<MailTableRowDto>();
                        var contentBilgi = new MailTableContentDto();

                        var otL = _entities.OgrenimTipleris.First(p => p.OgrenimTipKod == tercih.OgrenimTipKod);
                        var prl = tercih.Programlar;

                        htmlBigliRow.Add(new MailTableRowDto { Baslik = "Öğrenim Seviyesi", Aciklama = otL.OgrenimTipAdi });
                        htmlBigliRow.Add(new MailTableRowDto { Baslik = "Program Adı", Aciklama = prl.ProgramAdi });
                        if (basvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru)
                        {
                            contentBilgi.GrupBasligi = "Lisansüstü programlarına kayıt olmak için seçtiğiniz tercihi bilgisi aşağıdaki gibidir.";
                        }
                        else if (basvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru)
                        {
                            contentBilgi.GrupBasligi = "Lisansüstü yatay geçiş programlarına kayıt olmak için seçtiğiniz tercihi bilgisi aşağıdaki gibidir.";
                        }
                        else contentBilgi.GrupBasligi = "YTÜ Yeni mezun Lisansüstü programlarına kayıt olmak için seçtiğiniz tercihi bilgisi aşağıdaki gibidir.";
                        contentBilgi.Detaylar = htmlBigliRow;

                        var mmmC = new MailMainContentDto();
                        var enstituAdi = _entities.Enstitulers.First(p => p.EnstituKod == basvuruSurec.EnstituKod).EnstituAd;
                        mmmC.EnstituAdi = enstituAdi;
                        mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
                        var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(basvuruSurec.EnstituKod);
                        var sistemErisimAdresi = mailBilgi.SistemErisimAdresi;
                        var wurlAddr = sistemErisimAdresi.Split('/').ToList();
                        if (sistemErisimAdresi.Contains("//"))
                            sistemErisimAdresi = wurlAddr[0] + "//" + wurlAddr.Skip(2).Take(1).First();
                        else
                            sistemErisimAdresi = "http://" + wurlAddr.First();
                        mmmC.LogoPath = sistemErisimAdresi + "/Content/assets/images/ytu_logo_tr.png";
                        var hcb = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", contentBilgi);
                        mmmC.Content = hcb;

                        string htmlMail = ViewRenderHelper.RenderPartialView("Ajax", "getMailContent", mmmC);
                        var snded = MailManager.SendMail(mailBilgi.EnstituKod, "Lisansüstü " + (basvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru ? "" : "yatay geçiş ") + "başvurusu kayıt olunmak istenen tercih Hk.", htmlMail, basvuru.EMail, null);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = basvuruSurec.EnstituKod,
                                BasvuruID = basvuru.BasvuruID,
                                MesajID = null,
                                Konu = "Kayıt olunmak istenen tercih seçimi (" + basvuru.Ad + " " + basvuru.Soyad + ")",
                                Aciklama = "",
                                AciklamaHtml = htmlMail,
                                IslemYapanID = UserIdentity.Current.Id,
                                IslemTarihi = DateTime.Now,
                                IslemYapanIP = UserIdentity.Ip,
                                Gonderildi = true,
                                GonderilenMailKullanicilars = new List<GonderilenMailKullanicilar>()
                            };
                            kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = basvuru.EMail });
                            _entities.GonderilenMaillers.Add(kModel);
                            _entities.SaveChanges();
                        }
                    }
                    #endregion

                }
            }
            mMessage.MessageType = Msgtype.Warning;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);
            return Json(new { IsSuccess = mMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }


        [Authorize]
        public ActionResult BasvuruKayitOlunacakTercihSet2(int id, List<string> UniqueIDs, List<string> isSecildis, List<string> onceliks)
        {

            var mMessage = new MmMessage();
            var kayityetki = RoleNames.BasvuruSureciOgrenciKayit.InRoleCurrent();

            var basvuru = _entities.Basvurulars.First(p => p.BasvuruID == id && p.KullaniciID == (!kayityetki ? UserIdentity.Current.Id : p.KullaniciID));
            var basvuruSurec = basvuru.BasvuruSurec;
            var belgeKtModel = new List<EntBegeKayitT>();

            var donemdekiBasvurulari = basvuruSurec.Basvurulars.Where(a => a.KullaniciID == basvuru.KullaniciID).ToList();
            var donemdekiBasvuruSonuclari = donemdekiBasvurulari.SelectMany(s => s.MulakatSonuclaris).ToList();
            var donemdekiBasvuruTercihleri = donemdekiBasvurulari.SelectMany(s => s.BasvurularTercihleris).ToList();
            var mSonucTip = new List<int> { MulakatSonucTipi.Asil, MulakatSonucTipi.Yedek };

            UniqueIDs = UniqueIDs ?? new List<string>();
            isSecildis = isSecildis ?? new List<string>();
            onceliks = onceliks ?? new List<string>();
            var uniqueIDs = UniqueIDs.Where(p => !p.IsNullOrWhiteSpace()).Select(s => new Guid(s)).ToList();
            var isSecildiList = isSecildis.Where(p => !p.IsNullOrWhiteSpace()).Select(s => s.ToBoolean().Value).ToList();
            var oncelikList = onceliks.Select(s => s.ToIntObj()).ToList();

            var qUniqueIDs = uniqueIDs.Select((s, inx) => new { inx, UniqueID = s }).ToList();
            var qIsSecildis = isSecildiList.Select((s, inx) => new { inx, IsSecildi = s }).ToList();
            var qOnceliks = oncelikList.Select((s, inx) => new { inx, Oncelik = s }).ToList();
            var qBasvuruIDs = _entities.BasvurularTercihleris.Where(p => uniqueIDs.Contains(p.UniqueID)).Select(s => new { s.UniqueID, s.BasvuruID }).ToList();


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
                if (donemdekiBasvuruSonuclari.Any(a => a.KayitDurumID.HasValue))
                {
                    mMessage.Messages.Add("Tercihleriniz yetkili tarafından işlem gördüğü için herhangi bir değişiklik yapılamaz.");
                }
                else
                {
                    var ogrenimTipleriLngs = _entities.OgrenimTipleris.Where(p => p.EnstituKod == basvuruSurec.EnstituKod).Select(s => new { s.OgrenimTipKod, s.OgrenimTipAdi }).ToList();
                    var nowDate = DateTime.Now;
                    var isBelgeYuklemeVar = basvuruSurec.IsBelgeYuklemeVar;
                    var belgeYuklemeTarihleris = basvuruSurec.BasvuruSurecOgrenimTipleris.Where(p => p.BelgeYuklemeAsilBasTar.HasValue).ToList();
                    var tercihUniqueIDs = new List<Guid>();
                    foreach (var item in qRows.OrderBy(o => o.Oncelik ?? 5))
                    {
                        var tercih = donemdekiBasvuruTercihleri.First(p => p.UniqueID == item.TercihUniqueID);
                        bool isSuccess = true;
                        if (isBelgeYuklemeVar && !kayityetki)
                        {
                            isSuccess = belgeYuklemeTarihleris.Any(a => a.OgrenimTipKod == tercih.OgrenimTipKod && a.BelgeYuklemeYedekBasTar.Value <= nowDate && a.BelgeYuklemeYedekBitTar >= nowDate);
                        }
                        if (isSuccess)
                        {
                            tercih.IsSecilenTercih = item.IsSecildi;
                            tercih.KayitSiraNo = item.IsSecildi ? item.Oncelik.Value : (int?)null;
                            tercih.IslemTarihi = DateTime.Now;
                            tercih.IslemYapanID = UserIdentity.Current.Id;
                            tercih.IslemYapanIP = UserIdentity.Ip;
                            tercihUniqueIDs.Add(item.TercihUniqueID);
                        }
                        else
                        {
                            var otL = ogrenimTipleriLngs.First(p => p.OgrenimTipKod == tercih.OgrenimTipKod);
                            mMessage.Messages.Add(otL.OgrenimTipAdi + " Öğrenim seviyesi için Belge yükleme tarihi aktif olmadığından tercih seçim işlemi yapılamaz!");
                        }
                    }

                    #region sendMail
                    if (tercihUniqueIDs.Count > 0)
                    {
                        var htmlBigliRow = new List<MailTableRowDto>();
                        var contentBilgi = new MailTableContentDto();

                        foreach (var item in tercihUniqueIDs)
                        {
                            var qTercih = qRows.First(p => p.TercihUniqueID == item);
                            var tercih = donemdekiBasvuruTercihleri.First(p => p.UniqueID == item);
                            var otL = ogrenimTipleriLngs.First(p => p.OgrenimTipKod == tercih.OgrenimTipKod);
                            var prl = tercih.Programlar;
                            htmlBigliRow.Add(new MailTableRowDto { Baslik = "Öğrenim Seviyesi", Aciklama = otL.OgrenimTipAdi });
                            htmlBigliRow.Add(new MailTableRowDto { Baslik = "Program Adı", Aciklama = prl.ProgramAdi });
                            htmlBigliRow.Add(qTercih.IsSecildi
                                ? new MailTableRowDto
                                {
                                    Baslik = "Kayıt Seçeneği",
                                    Aciklama = "Kayıt Olmak İstenen " + qTercih.Oncelik + ". Tercih"
                                }
                                : new MailTableRowDto
                                { Baslik = "Kayıt Seçeneği", Aciklama = "Kayıt Olmak İstenmiyor" });
                            htmlBigliRow.Add(new MailTableRowDto { Baslik = "---------------", Aciklama = "-----------------" });
                        }

                        contentBilgi.GrupBasligi = "Lisansüstü programlarına kayıt olmak için seçtiğiniz tercihi bilgisi aşağıdaki gibidir.";
                        contentBilgi.Detaylar = htmlBigliRow;

                        var mmmC = new MailMainContentDto();
                        var enstituAdi = _entities.Enstitulers.First(p => p.EnstituKod == basvuruSurec.EnstituKod).EnstituAd;
                        mmmC.EnstituAdi = enstituAdi;
                        mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
                        var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(basvuruSurec.EnstituKod);
                        var sistemErisimAdresi = mailBilgi.SistemErisimAdresi;
                        var wurlAddr = sistemErisimAdresi.Split('/').ToList();
                        if (sistemErisimAdresi.Contains("//"))
                            sistemErisimAdresi = wurlAddr[0] + "//" + wurlAddr.Skip(2).Take(1).First();
                        else
                            sistemErisimAdresi = "http://" + wurlAddr.First();
                        mmmC.LogoPath = sistemErisimAdresi + "/Content/assets/images/ytu_logo_tr.png";
                        var hcb = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", contentBilgi);
                        mmmC.Content = hcb;

                        string htmlMail = ViewRenderHelper.RenderPartialView("Ajax", "getMailContent", mmmC);
                        var sendEmail = basvuru.EMail;
                        // SendEmail = "irfansecer@gmail.com";
                        var snded = MailManager.SendMail(mailBilgi.EnstituKod, "Lisansüstü kayıt olunmak istenen tercih Hk.", htmlMail, sendEmail, null);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = basvuruSurec.EnstituKod,
                                BasvuruID = basvuru.BasvuruID,
                                MesajID = null,
                                Konu = "Kayıt olunmak istenen tercih seçimi (" + basvuru.Ad + " " + basvuru.Soyad + ")",
                                Aciklama = "",
                                AciklamaHtml = htmlMail,
                                IslemYapanID = UserIdentity.Current.Id,
                                IslemTarihi = DateTime.Now,
                                IslemYapanIP = UserIdentity.Ip,
                                Gonderildi = true,
                                GonderilenMailKullanicilars = new List<GonderilenMailKullanicilar>()
                            };
                            kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = basvuru.EMail });
                            _entities.GonderilenMaillers.Add(kModel);
                            _entities.SaveChanges();
                        }
                    }
                    #endregion
                }
            }


            mMessage.MessageType = Msgtype.Warning;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);
            return Json(new { IsSuccess = mMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        [HttpGet]
        public ActionResult GetDetailMezuniyet(int id, int? showDetayYayinId, int tbInx, bool isDelete, bool gelenBasvuru = false)
        {
            var model = MezuniyetBus.GetMezuniyetBasvuruDetayBilgi(id, null, showDetayYayinId);
            model.GelenBasvuru = gelenBasvuru;
            model.SelectedTabIndex = tbInx;


            var srSonTalebi = model.MezuniyetSRModel.SalonRezervasyonlari.OrderByDescending(o => o.SRTalepID).FirstOrDefault();
            var modelBasvuruDurum = new FrMezuniyetBasvurulari
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
                SrTalebi = srSonTalebi,
                EYKTarihi = model.EYKTarihi,
                MezuniyetBasvurulariTezDosyasi = model.MezuniyetBasvurulariTezDosyalaris.OrderByDescending(o => o.MezuniyetBasvurulariTezDosyaID).FirstOrDefault(),
                TeslimFormDurumu = srSonTalebi != null && srSonTalebi.SRTalepleriBezCiltFormus.Any(),
                IsMezunOldu = model.IsMezunOldu,
                MezuniyetTarihi = model.MezuniyetTarihi,

            };

            model.BasvuruDurumHtml = modelBasvuruDurum.ToRenderPartialViewHtml("Mezuniyet", "BasvuruDurumView");

            DateTime? onayTarihi = null;
            if (modelBasvuruDurum.MezuniyetJuriOneriFormu != null) onayTarihi = modelBasvuruDurum.MezuniyetJuriOneriFormu.EYKYaGonderildi == true ? modelBasvuruDurum.MezuniyetJuriOneriFormu.EYKYaGonderildiIslemTarihi : null;


            model.IsDelete = isDelete;
            model.SMezuniyetYayinKontrolDurum = new SelectList(MezuniyetBus.GetCmbMezuniyetYayinDurum(false, true), "Value", "Caption", model.MezuniyetYayinKontrolDurumID);
            model.SEYKYaGonderildi = new SelectList(ComboData.GetCmbEykGonderimDurumData(true, onayTarihi), "Value", "Caption", model.EYKYaGonderildi);
            model.SEYKDaOnaylandi = new SelectList(ComboData.GetCmbEykOnayDurumData(true), "Value", "Caption", model.EYKDaOnaylandi);

            model.SIsAsilOryedek = new SelectList(ComboData.GetCmbAsilYedekDurumData(true), "Value", "Caption");


            return View(model);
        }
        [HttpGet]
        public ActionResult GetDetailTiBasvuru(int id, Guid? uniqueId)
        { 
            var model = TezIzlemeBus.GetSecilenBasvuruTiDetay(id, uniqueId); 
            return View(model);
        }
        [Authorize]
        [HttpGet]
        public ActionResult GetDetailTdoBasvuru(int id, Guid? uniqueId)
        { 
            var model = TezDanismanOneriBus.GetSecilenBasvuruTdoDetay(id, uniqueId); 
            ViewBag.ProgramKod = new SelectList(Management.cmbGetAktifProgramlar(model.EnstituKod, true, true), "Value", "Caption", model.ProgramKod);
            return View(model);
        }

        [HttpGet]
        public ActionResult GetGbnDetail(int id)
        {
            var model = Management.getSecilenBasvuruDetay(id);

            ViewBag.gLOgrenimDurumID = new SelectList(Management.cmbAktifOgrenimDurumu2(true, IsBasvurudaGozuksun: true), "Value", "Caption", model.LOgrenimDurumID);
            ViewBag.gLNotSistemID = new SelectList(Management.cmbGetNotSistemleri(true), "Value", "Caption", model.LNotSistemID);
            return View(model);
        }
        public ActionResult SetGbnDetail(int basvuruId, int? gLOgrenimDurumId, int? gLNotSistemId, double? gLMezuniyetNotu)
        {
            var mmMessage = new MmMessage();
            var basvuru = _entities.Basvurulars.First(p => p.BasvuruID == basvuruId);
            var bsurec = _entities.BasvuruSurecs.First(p => p.BasvuruSurecID == basvuru.BasvuruSurecID);

            if (RoleNames.GelenBasvurularKayit.InRoleCurrent() == false)
            {
                if (basvuru.KullaniciID != UserIdentity.Current.Id)
                {
                    SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait başvuru üzerindeki AGNO bilgisi güncellenmek isteniyor!\nBasvuruID:" + basvuru.BasvuruID + "\nKullaniciID:" + UserIdentity.Current.Id + "\nÇağrılan Kullanıcı ID:" + basvuru.KullaniciID, "Ajax/setGBNDetail", LogType.Saldırı);
                    mmMessage.Messages.Add("Başka bir kullanıcıya ait başvuru üzerindeki AGNO bilgisini güncelleyemezsiniz!");
                }
            }
            var nowDate = DateTime.Now;
            if (!(bsurec.AGNOGirisBaslangicTarihi <= nowDate && bsurec.AGNOGirisBitisTarihi >= nowDate))
            {
                mmMessage.Messages.Add("Aktif bir AGNO bilgisi düzeltme süreci bulunamadı! Düzeltme işlemini yapamazsınız.");
            }

            if (mmMessage.Messages.Count == 0)
            {
                if (!gLOgrenimDurumId.HasValue)
                {
                    mmMessage.Messages.Add("Öğrenim durumunuzu seçiniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "gLOgrenimDurumID" });
                }
                else
                {
                    if (gLOgrenimDurumId.Value != OgrenimDurum.Mezun)
                    {
                        string msg = "Güncel not bilginizi girebilmeniz için öğrenim durumunuzu Mezun seçeneği olarak seçmeniz gerekmektedir!";
                        mmMessage.Messages.Add(msg);
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "gLOgrenimDurumID" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "gLOgrenimDurumID" });
                }
                if (!gLNotSistemId.HasValue)
                {
                    string msg = "Lisans Eğitimi Not Sistemi Bilgisini Seçiniz!";
                    mmMessage.Messages.Add(msg);
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "gLNotSistemID" });
                }
                else
                {
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "gLNotSistemID" });

                    var notSistemi = Management.getNotSistemi(gLNotSistemId.Value);
                    var nots = _entities.NotSistemleris.First(p => p.NotSistemID == notSistemi.NotSistemID);
                    if (!gLMezuniyetNotu.HasValue)
                    {
                        string msg = "Lisans Eğitimi Not Bilgisini Giriniz!";
                        mmMessage.Messages.Add(msg);
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "gLMezuniyetNotu" });

                    }
                    else if ((double)nots.MinNot <= gLMezuniyetNotu.Value && (double)nots.MaxNot >= gLMezuniyetNotu)
                    {
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "gLMezuniyetNotu" });
                    }
                    else
                    {
                        mmMessage.Messages.Add("Lisans Eğitimi Notu " + notSistemi.NotSistemAdi + " not sistemine göre " + nots.MinNot.ToString() + " ile " + nots.MaxNot.ToString() + " arasında bir değer olmalıdır!");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "gLMezuniyetNotu" });
                    }
                }
                if (mmMessage.Messages.Count == 0)
                {
                    basvuru.LNotSistemID = gLNotSistemId.Value;
                    basvuru.LMezuniyetNotu = gLMezuniyetNotu.Value;
                    basvuru.LMezuniyetNotu100LukSistem = gLMezuniyetNotu.Value.ToNotCevir(gLNotSistemId.Value).Not100Luk;
                    basvuru.LOgrenimDurumID = gLOgrenimDurumId.Value;
                    basvuru.IslemTarihi = DateTime.Now;
                    basvuru.IslemYapanID = UserIdentity.Current.Id;
                    basvuru.IslemYapanIP = UserIdentity.Ip;
                    var btercih = basvuru.BasvurularTercihleris.ToList();
                    foreach (var item in btercih)
                    {
                        var mulR = _entities.MulakatSonuclaris.FirstOrDefault(p => p.BasvuruTercihID == item.BasvuruTercihID);
                        if (mulR != null)
                        {
                            mulR.Agno = basvuru.LMezuniyetNotu100LukSistem;

                        }
                    }

                    _entities.SaveChanges();
                    mmMessage.Messages.Add("Not güncelleme işlemi başarılı!");
                    mmMessage.IsSuccess = true;
                }
            }
            return mmMessage.ToJsonResult();
        }

        public ActionResult SifreResetle(string mailAddress)
        {
            var mmMessage = new MmMessage();

            if (mailAddress.IsNullOrWhiteSpace() || mailAddress.ToIsValidEmail())
            {
                mmMessage.IsSuccess = false;
                mmMessage.Title = "Lütfen doğru bir mail formatı giriniz.";
            }
            else
            {
                var kul = _entities.Kullanicilars.Where(p => p.EMail.Equals(mailAddress) && p.IsAktif).FirstOrDefault();
                if (kul == null)
                {
                    mmMessage.IsSuccess = false;
                    mmMessage.Title = "Girmiş olduğunuz mail adresi ile eşleşen herhangi bir kullanıcıya rastlanmadı!";
                }
                else
                {
                    if (kul.IsActiveDirectoryUser)
                    {
                        mmMessage.IsSuccess = false;
                        mmMessage.Title = "Girmiş olduğunuz mail adresi 'Active directory' sistemine entegre çalıştığı için ve bilgi işlem tarafından belirlenen ve bazı sistemlere  (YTÜ Mail, EBYS, Lojman Yönetim Sistem, Lisansustu Başvuru Sistemi vb)  erişimini sağlayan ortak bir şifresi bulunmaktadır. Bu mail adresi için tanımlanmış şifre sadece bilgi işlem tarafından belirlenip değiştirilebilmektedir. '" + kul.KullaniciAdi + "' kullanıcı adı ile YTÜ Mail, EBYS, Lojman Yönetim Sistem, Lisansustu Başvuru Sistemi vb. programlara giriş yaptığınız şifrenizi hatırlamıyorsanız şifre değişikliği işlemi için lütfen Bilgi İşlem ile görüşünüz.";
                    }
                    else
                    {
                        var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(kul.EnstituKod);
                        var mRowModel = new List<MailTableRowDto>();
                        DateTime gecerlilikTarihi = DateTime.Now.AddHours(2);
                        string guid = Guid.NewGuid().ToString().Substring(0, 20);
                        mRowModel.Add(new MailTableRowDto { Baslik = "Şifre Sıfırlama Linki", Aciklama = "<a target='_blank' href='" + mailBilgi.SistemErisimAdresi + "/Account/ParolaSifirla?psKod=" + guid + "'>Şifrenizi sıfırlamak için tıklayınız</a>" });
                        mRowModel.Add(new MailTableRowDto { Baslik = "Link Geçerlilik Tarihi", Aciklama = "Yukarıdaki link '" + gecerlilikTarihi.ToFormatDateAndTime() + "' tarihine kadar geçerlidir." });

                        var mmmC = new MailMainContentDto
                        {
                            EnstituAdi = _entities.Enstitulers.First(p => p.EnstituKod == kul.EnstituKod).EnstituAd,
                            UniversiteAdi = "Yıldız Teknik Üniversitesi"
                        };
                        var sistemErisimAdresi = mailBilgi.SistemErisimAdresi;
                        var wurlAddr = sistemErisimAdresi.Split('/').ToList();
                        if (sistemErisimAdresi.Contains("//"))
                            sistemErisimAdresi = wurlAddr[0] + "//" + wurlAddr.Skip(2).Take(1).First();
                        else
                            sistemErisimAdresi = "http://" + wurlAddr.First();
                        mmmC.LogoPath = sistemErisimAdresi + "/Content/assets/images/ytu_logo_tr.png";
                        var mtc = new MailTableContentDto
                        {
                            AciklamaBasligi = "Şifre Sıfırlama İşlemi",
                            AciklamaDetayi = "Şifrenizi sıfırlamak için aşağıda bulunan linke tıklayınız ve açılan sayfa da yeni şifrenizi tanımlayınız.",
                            Detaylar = mRowModel
                        };
                        var tavleContent = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", mtc);
                        mmmC.Content = tavleContent;

                        string htmlMail = ViewRenderHelper.RenderPartialView("Ajax", "getMailContent", mmmC);
                        var eMailList = new List<MailSendList>();
                        eMailList.Add(new MailSendList { EMail = kul.EMail, ToOrBcc = true });
                        var rtVal = MailManager.SendMailRetVal(kul.EnstituKod, "Şifre Sıfırlama İşlemi", htmlMail, eMailList, null);
                        if (rtVal == null)
                        {
                            mmMessage.IsSuccess = true;
                            mmMessage.Title = "Şifre sıfırlama linki '" + kul.EMail + "' adresine gönderilmiştir!";
                            kul.ParolaSifirlamaKodu = guid;
                            kul.ParolaSifirlamGecerlilikTarihi = gecerlilikTarihi;
                            _entities.SaveChanges();
                        }
                        else
                        {
                            mmMessage.IsSuccess = false;
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Şifre sıfırlama! Hata: " + rtVal.ToExceptionMessage(), rtVal.ToExceptionStackTrace(), LogType.Hata, kul.KullaniciID, UserIdentity.Ip);
                            mmMessage.Title = "Şifre sıfırlama linki '" + kul.EMail + "' adresine gönderilemedi!";
                        }
                    }
                }

            }

            return mmMessage.ToJsonResult();
        }

        public ActionResult PTipKontrol(int id)
        {
            var pt = _entities.KullaniciTipleris.Where(p => p.KullaniciTipID == id).Select(s => new
            {
                s.KullaniciTipID,
                s.KullaniciTipAdi,
                s.IsAktif,
                s.KurumIci,
                s.Yerli
            }).First();
            return pt.ToJsonResult();
        }
        public ActionResult GetOts(string enstituKod, bool bosSecimVar = true, int? haricOgreniTipKod = null)
        {
            var cmbmld = OgrenimTipleriBus.CmbAktifOgrenimTipleri(enstituKod, bosSecimVar, true, haricOgreniTipKod);

            return cmbmld.ToJsonResult();
        }
        public ActionResult GetBolumler(int basvuruSurecId, string otKod, int kTipId)
        {
            bool isSubOt = _entities.OgrenimTipleris.Any(p => p.IsAktif && p.GrupGoster && p.GrupKodu == otKod);
            var cmbmld = !isSubOt ? Management.cmbGetAktifBolumlerX(otKod.ToInt().Value, basvuruSurecId, kTipId) : Management.cmbGetAktifSubOgrenimTipleri(basvuruSurecId, otKod, true);
            bool ylShow = false;
            if (!isSubOt)
            {
                var bs = _entities.BasvuruSurecs.First(p => p.BasvuruSurecID == basvuruSurecId);
                ylShow = _entities.OgrenimTipleris.First(p => p.EnstituKod == bs.EnstituKod && p.OgrenimTipKod == otKod.ToInt().Value).YLEgitimBilgisiIste;
            }
            return new { IsSubOT = isSubOt, data = cmbmld.Select(s => new { s.Value, s.Caption }), YLShow = ylShow.ToString().ToLower() }.ToJsonResult();
        }
        public ActionResult GetProgramlar(int bolId, int otId, int basvuruSurecId, int kullaniciTipId)
        {
            var bolm = Management.cmbGetAktifProgramlarX(bolId, otId, basvuruSurecId, kullaniciTipId);
            return bolm.Select(s => new { s.Value, s.Caption }).ToJsonResult();
        }

        public ActionResult OgrenimBilgisiKontrolEt(kmBasvuru kModel, int basvuruId)
        {
            var mmMessage = Management.obKontrol(kModel);
            if (kModel.ProgramKod.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Ekleyeceğiniz Programı Seçiniz!");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
            }
            else
            {
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "ProgramKod" });
            }

            if (mmMessage.Messages.Count == 0)
            {
                var programs = new List<CmbIntDto> { new CmbIntDto { Value = kModel.OgrenimTipKod, Caption = kModel.ProgramKod } };
                var retVal = Management.programAgnoMinControl(kModel, programs);
                mmMessage.Messages.AddRange(retVal.Messages);
                mmMessage.MessagesDialog.AddRange(retVal.MessagesDialog);
            }
            if (mmMessage.Messages.Count == 0)
            {
                //Düzeltilecek
                var basvuruSureci = _entities.BasvuruSurecs.First(p => p.BasvuruSurecID == kModel.BasvuruSurecID);
                kotaKontrolModel alanB;
                if (basvuruSureci.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru || basvuruSureci.BasvuruSurecTipID == BasvuruSurecTipi.YTUYeniMezunDRBasvuru)
                    alanB = Management.AlanKontrol(kModel.BasvuruSurecID, kModel.LOgrenciBolumID.Value, kModel.YLOgrenciBolumID, kModel.OgrenimTipKod, kModel.ProgramKod, kModel.KullaniciID, basvuruId);
                else
                {
                    var bolumIDs = new List<int>();
                    if (kModel.YLDurum)
                    {
                        bolumIDs.Add(kModel.LOgrenciBolumID.Value);
                        bolumIDs.Add(kModel.YLOgrenciBolumID.Value);
                        bolumIDs.Add(kModel.DROgrenciBolumID.Value);
                    }
                    else
                    {
                        bolumIDs.Add(kModel.LOgrenciBolumID.Value);
                        bolumIDs.Add(kModel.YLOgrenciBolumID.Value);
                    }
                    alanB = Management.AlanKontrolYG(kModel.BasvuruSurecID, bolumIDs, kModel.OgrenimTipKod, kModel.ProgramKod, kModel.KullaniciID, basvuruId);
                }

                if (alanB.Kota <= 0)
                {
                    var alanTip = alanB.AlanTipID == AlanTipi.Ortak ? "Ortak" : (alanB.AlanTipID == AlanTipi.AlanIci ? "Alan İci" : "Alan Dışı");
                    mmMessage.Messages.Add("Girmiş olduğunuz öğrenim bilgilerine göre seçtiğiniz program " + alanTip + " olarak değerlendirilmiştir fakat " + alanTip + " kota bulunmadığından programı ekleyemezsiniz! Lütfen başka bir program seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
                }
                else if (alanB.AlanDisiProgramKisitlamasiVar)
                {
                    mmMessage.Messages.Add(alanB.AlanDisiProgramKisitlamasiMsg);
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
                }
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "ProgramKod" });
            mmMessage.IsSuccess = mmMessage.Messages.Count == 0;
            mmMessage.Title = "Ekleme işmini yapabilmek aşağıdaki uyarıları incleyiniz!";
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            return mmMessage.ToJsonResult();
        }

        public ActionResult GetBolumProgramList(string kod, string tip)
        {
            if (tip == "bolum")
            {
                var lst = Management.cmbGetYetkiliAnabilimDallari(true, kod);
                return lst.Select(s => new { s.Value, s.Caption }).ToJsonResult();
            }
            else if (tip == "program")
            {
                var lst = Management.cmbGetAktifProgramlar(true, kod.ToInt().Value);
                return lst.Select(s => new { s.Value, s.Caption }).ToJsonResult();
            }
            return null;

        }
        public ActionResult GetKullaniciBolumProgramList(string enstituKod, string bolumKod, string tip)
        {

            if (tip == "bolum")
            {
                var lst = Management.cmbGetYetkiliProgramAnabilimDallari(true, enstituKod);
                return lst.Select(s => new { s.Value, s.Caption }).ToJsonResult();
            }
            else if (tip == "program")
            {
                var lstStr = Management.cmbGetKullaniciProgramlari(UserIdentity.Current.Id, enstituKod, bolumKod, true);
                return lstStr.Select(s => new { s.Value, s.Caption }).ToJsonResult();
            }
            else
            {
                return null;
            }
        }
        [ValidateInput(false)]
        public ActionResult ValidationControlSteps(kmBasvuru kModel)
        {
            var showEgitimDiliIsle = false;
            var mmMessage = new MmMessage();
            var resimBilgi = new CmbStringDto { Caption = "", Value = "" };

            if (kModel.StepNo == 1)
            {
                mmMessage.Title = "Bir sonraki adıma geçmek için aşağıdaki uyarıları kontrol ediniz!";
                var kmM = KullanicilarBus.KullaniciKayitKontrol(kModel);
                mmMessage.Messages.AddRange(kmM.Messages.ToList());
                mmMessage.MessagesDialog.AddRange(kmM.MessagesDialog.ToList());
            }
            else if (kModel.StepNo == 2)
            {

                #region step2
                mmMessage = Management.obKontrol(kModel);
                if (mmMessage.Messages.Count == 0 && kModel.TercihSayisi > 0)
                {
                    var kulTid = _entities.Kullanicilars.First(p => p.KullaniciID == kModel.KullaniciID);
                    kModel.KullaniciTipID = kulTid.KullaniciTipID;
                    var qOgrenimTipKod = kModel._OgrenimTipKod.Select((s, inx) => new { Index = inx, OgrenimTipKod = s }).ToList();
                    var qProgramKod = kModel._ProgramKod.Select((s, inx) => new { Index = inx, ProgramKod = s }).ToList();
                    var qIngilizce = kModel._Ingilizce.Select((s, inx) => new { Index = inx, Ingilizce = s }).ToList();
                    var qPrograms = (from s in qOgrenimTipKod
                                     join p in qProgramKod on s.Index equals p.Index
                                     join ing in qIngilizce on s.Index equals ing.Index
                                     select new { s.OgrenimTipKod, p.ProgramKod, ing.Ingilizce }).ToList();
                    var retVal = Management.programAgnoMinControl(kModel, qPrograms.Select(s => new CmbIntDto { Value = s.OgrenimTipKod, Caption = s.ProgramKod }).ToList());
                    mmMessage.Messages.AddRange(retVal.Messages);
                    mmMessage.MessagesDialog.AddRange(retVal.MessagesDialog);
                    var qtercihler = qPrograms.Select(item => new CmbMultyTypeDto { Value = item.OgrenimTipKod, ValueB = item.Ingilizce, ValueS2 = item.ProgramKod }).ToList();
                    var tomerVar = Management.cmbGetdAktifSinavlar(qtercihler, kModel.BasvuruSurecID, SinavTipGrup.Tomer, true).Count > 0 && kModel.KullaniciTipID == KullaniciTipBilgi.YabanciOgrenci;
                    showEgitimDiliIsle = tomerVar;
                    if (tomerVar && !kModel.LEgitimDiliTurkce.HasValue)
                    {
                        mmMessage.Messages.Add("Lisans Eğitim Dilini Seçiniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "LEgitimDiliTurkce" });

                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "LEgitimDiliTurkce" });

                    if (tomerVar && kModel.YLDurum && !kModel.YLEgitimDiliTurkce.HasValue)
                    {
                        mmMessage.Messages.Add("Yüksek Lisans Eğitim Dilini Seçiniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YLEgitimDiliTurkce" });

                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YLEgitimDiliTurkce" });

                }

                if (kModel.TercihSayisi == 0)
                {
                    mmMessage.Messages.Add("Devam edebilmek için en az bir program tercihi eklemeniz gerekmektedir!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SubOgrenimTipKod" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BolumID" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "trch_SiraNo" });
                }
                else
                {
                    if (kModel.KotaValid)
                    {
                        mmMessage.Messages.Add("Kontenjanı bulunmayan bir programı tercihlerinize eklediniz. Devam edebilmek için lütfen kontenjanı olmayan tercihinizi kaldırıp, duyurulan kontenjan bilgisine uygun bir tercih ekleyiniz.");
                    }
                    else
                    {
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "OgrenimTipKod" });
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "SubOgrenimTipKod" });
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "BolumID" });
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "ProgramKod" });
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "trch_SiraNo" });
                    }

                }
                #endregion
                mmMessage.Title = "Bir sonraki adıma geçmek için aşağıdaki uyarıları kontrol ediniz!";

            }
            else if (kModel.StepNo == 3)
            {
                mmMessage.Title = "Kayıt işlemini yapabilmek için aşağıdaki uyarıları kontrol ediniz!";
                #region step3
                var qOgrenimTipKods = kModel._OgrenimTipKod.Select((s, inx) => new { s = s, Index = inx }).ToList();
                var qIngilizces = kModel._Ingilizce.Select((s, inx) => new { s = s, Index = inx }).ToList();
                var qtercihler = (from s in qOgrenimTipKods
                                  join qi in qIngilizces on s.Index equals qi.Index
                                  select new CmbMultyTypeDto { Value = s.s, ValueB = qi.s }).ToList();

                var tomerVar = Management.cmbGetdAktifSinavlar(qtercihler, kModel.BasvuruSurecID, SinavTipGrup.Tomer, true).Count > 0 && kModel.KullaniciTipID == KullaniciTipBilgi.YabanciOgrenci;
                var tomerIstensin = tomerVar && kModel.LEgitimDiliTurkce != true;
                if (kModel.AlesIstensinmi)
                {

                    if (kModel.BasvurularSinavBilgi_A.SinavTipID <= 0)
                    {
                        mmMessage.Messages.Add("Ales/Gre/Gmat Sınavı grubu için sınav tipi seçiniz!");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_A.SinavTipID" });
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
                        mmMessage.Messages.AddRange(kmM.Messages.ToList());
                        mmMessage.MessagesDialog.AddRange(kmM.MessagesDialog.ToList());
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_A.SinavTipID" });

                    }
                }

                if (kModel.DilIstensinmi)
                {
                    var gdilSinavTips = _entities.BasvuruSurecSinavTipleris.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID && p.SinavTipGrupID == SinavTipGrup.DilSinavlari).Select(s => s.SinavTipID).ToList();

                    if (kModel.BasvurularSinavBilgi_D.SinavTipID <= 0)
                    {
                        mmMessage.Messages.Add("Dil Sınavı grubu için sınav tipi seçiniz!");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_D.SinavTipID" });
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
                        mmMessage.Messages.AddRange(kmM.Messages.ToList());
                        mmMessage.MessagesDialog.AddRange(kmM.MessagesDialog.ToList());
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_D.SinavTipID" });

                    }
                }
                if (tomerIstensin)
                {
                    if (kModel.BasvurularSinavBilgi_T.SinavTipID <= 0)
                    {
                        mmMessage.Messages.Add("Tomer sınav tipi seçiniz!");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_T.SinavTipID" });
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
                        mmMessage.Messages.AddRange(kmM.Messages.ToList());
                        mmMessage.MessagesDialog.AddRange(kmM.MessagesDialog.ToList());
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_T.SinavTipID" });

                    }
                }
                if (kModel.BasvuruDurumID <= 0)
                {
                    mmMessage.Messages.Add("Başvuru Durumunu Seçiniz!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Basvuru.BasvuruDurumID" });

                }
                else
                {
                    if (kModel.BasvuruDurumID == BasvuruDurumu.Onaylandı && !kModel.Onaylandi)
                    {
                        mmMessage.Messages.Add("Başvuru Onaylayınız!");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Onaylandi" });
                    }
                    else
                    {
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Onaylandi" });
                    }
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Basvuru.BasvuruDurumID" });
                }
                if (mmMessage.Messages.Count == 0) kModel.sbmtForm = true;
                #endregion
            }
            mmMessage.IsSuccess = mmMessage.Messages.Count == 0;
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            string ekAciklama = "";
            string anketGiris = "";
            if (kModel.sbmtForm && kModel.Onaylandi && kModel.BasvuruDurumID == BasvuruDurumu.Onaylandı)
            {

                var bSurec = _entities.BasvuruSurecs.First(p => p.BasvuruSurecID == kModel.BasvuruSurecID);
                if (!bSurec.AnketID.HasValue || (bSurec.AnketID.HasValue && UserIdentity.Current.Informations.Any(a => a.Key == "LUBAnket")))
                {

                    var model = new EkAciklamaContentDto
                    {
                        Baslik = "Başvuru işleminizi tamamlamadan önce aşağıda bulunan programlar ile ilgili ek açıklamaları okuyunuz!"
                    };
                    var prgs = _entities.Programlars.Where(p => kModel._ProgramKod.Contains(p.ProgramKod)).ToList();
                    foreach (var item in prgs)
                    {
                        if (item.ProgramSecimiEkBilgi)
                        {
                            model.Detay.Add(new CmbStringDto { Caption = item.ProgramAdi + ":", Value = item.Aciklama });
                        }
                    }

                    if (model.Detay.Count > 0) ekAciklama = ViewRenderHelper.RenderPartialView("Ajax", "getEkAciklamaContent", model);
                }
                else
                {
                    if (kModel.BasvuruID <= 0 || !_entities.AnketCevaplaris.Any(a => a.BasvuruID == kModel.BasvuruID))
                    {

                        var anketSorulari = (from bsa in _entities.Ankets.Where(p => p.AnketID == bSurec.AnketID)
                                             join aso in _entities.AnketSorus on bsa.AnketID equals aso.AnketID
                                             join sb in _entities.AnketCevaplaris.Where(p => p.BasvuruID == kModel.BasvuruID && p.Basvurular.KullaniciID == kModel.KullaniciID) on aso.AnketSoruID equals sb.AnketSoruID into def1
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
                        var model = new KmAnketlerCevap
                        {
                            AnketTipID = 1,
                            BasvuruSurecID = kModel.BasvuruSurecID,
                            AnketID = bSurec.AnketID.Value,
                            JsonStringData = anketSorulari.ToJsonText()
                        };
                        foreach (var item in anketSorulari)
                        {
                            model.AnketCevapModel.Add(new AnketCevapDto
                            {
                                SecilenAnketSoruSecenekID = item.AnketSoruSecenekID,
                                SoruBilgi = new FrAnketDetayDto { AnketSoruID = item.AnketSoruID, SoruAdi = item.SoruAdi, SiraNo = item.SiraNo, Aciklama = item.Aciklama },
                                SoruSecenek = item.Secenekler.Select(s => new FrAnketSecenekDetayDto
                                {
                                    AnketSoruSecenekID = s.AnketSoruSecenekID,
                                    SiraNo = s.SiraNo,
                                    IsEkAciklamaGir = s.IsEkAciklamaGir,
                                    SecenekAdi = s.SecenekAdi
                                }).ToList(),
                                SelectListSoruSecenek = new SelectList(item.Secenekler.ToList(), "AnketSoruSecenekID", "SecenekAdi", item.AnketSoruSecenekID)
                            });
                        }

                        anketGiris = ViewRenderHelper.RenderPartialView("Ajax", "getAnket", model);
                    }
                }
            }
            return new { kModel.sbmtForm, _MmMessage = mmMessage, EkAciklamalar = ekAciklama, AnketGiris = anketGiris, ShowEgitimDili = showEgitimDiliIsle, kModel.YLDurum }.ToJsonResult();
        }


        [ValidateInput(false)]
        public ActionResult MezuniyetValidationControlSteps(kmMezuniyetBasvuru kModel)
        {
            var mmMessage = new MmMessage();
            if (kModel.StepNo == 1)
            {
                var tezK = MezuniyetBus.TezKontrol(kModel);
                mmMessage.Messages.AddRange(tezK.Messages);
                mmMessage.MessagesDialog.AddRange(tezK.MessagesDialog);
                mmMessage.Title = "Bir sonraki adıma geçmek için aşağıdaki uyarıları kontrol ediniz!";
            }
            else if (kModel.StepNo == 2)
            {
                mmMessage.Title = "Kayıt işlemini yapabilmek için aşağıdaki uyarıları kontrol ediniz!";

                if (kModel.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.Onaylandi)
                {
                    var yyK = MezuniyetBus.YayinKontrol(kModel);
                    mmMessage.Messages.AddRange(yyK.Messages);
                    mmMessage.MessagesDialog.AddRange(yyK.MessagesDialog);
                }
                if (mmMessage.Messages.Count == 0) kModel.sbmtForm = true;
            }

            mmMessage.IsSuccess = mmMessage.Messages.Count == 0;
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            return new { sbmtForm = kModel.sbmtForm, _MmMessage = mmMessage, EkAciklamalar = "" }.ToJsonResult();
        }




        [Authorize]
        public ActionResult RotateImage(bool leftOrRight, int kullaniciId)
        {
            if (RoleNames.KullanicilarKayit.InRoleCurrent() == false) kullaniciId = UserIdentity.Current.Id;
            var gelenBKayitY = RoleNames.GelenBasvurularKayit.InRoleCurrent();
            var user = _entities.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
            string folname = SistemAyar.KullaniciResimYolu;
            if ((gelenBKayitY || kullaniciId == UserIdentity.Current.Id) && user.ResimAdi.IsNullOrWhiteSpace() == false)
            {
                var imgPath = folname + "/" + user.ResimAdi;
                string pth = Server.MapPath(Management.getRoot() + imgPath);

                using (Image img = Image.FromFile(pth))
                {
                    img.RotateFlip(leftOrRight ? RotateFlipType.Rotate270FlipNone : RotateFlipType.Rotate90FlipNone);
                    //  var format = (System.Drawing.Imaging.ImageFormat)img.RawFormat;
                    img.Save(pth, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

            }
            return new { ResimAdi = folname + "/" + user.ResimAdi }.ToJsonResult();
        }
        public ActionResult GetProgramlarEkod(string enstituKod)
        {
            var bolm = Management.cmbGetAktifProgramlar(enstituKod, true);
            return bolm.Select(s => new { s.Value, s.Caption }).ToJsonResult();
        }
        public ActionResult GetMessage(MmMessage model)
        {
            return View(model);
        }

        public ActionResult GetMailContent(MailMainContentDto model)
        {

            return View(model);
        }
        public ActionResult GetMailTableContent(MailTableContentDto model)
        {
            return View(model);
        }

        public ActionResult GetEkAciklamaContent(EkAciklamaContentDto model)
        {

            return View(model);
        }

        public ActionResult GetAnket(KmAnketlerCevap model)
        {
            return View();
        }


        public ActionResult SetAnket(KmAnketlerCevap kModel)
        {
            var mMessage = new MmMessage
            {
                MessageType = Msgtype.Error,
                IsSuccess = false,
                Title = "Anket bilgisi oluşturulamadı. Lütfen aşağıdaki uyarıları inceleyiniz."
            };
            var qAnketSoruId = kModel.AnketSoruID.Select((s, inx) => new { AnketSoruID = s, inx = inx }).ToList();
            var qAnketSoruSecenekId = kModel.AnketSoruSecenekID.Select((s, inx) => new { AnketSoruSecenekID = s, inx = inx }).ToList();
            var qAnketSoruTabloVeri1 = kModel.TabloVeri1.Select((s, inx) => new { TabloVeri1 = s, inx = inx }).ToList();
            var qAnketSoruTabloVeri2 = kModel.TabloVeri2.Select((s, inx) => new { TabloVeri2 = s, inx = inx }).ToList();
            var qAnketSoruTabloVeri3 = kModel.TabloVeri3.Select((s, inx) => new { TabloVeri3 = s, inx = inx }).ToList();
            var qAnketSoruTabloVeri4 = kModel.TabloVeri4.Select((s, inx) => new { TabloVeri4 = s, inx = inx }).ToList();
            var qAnketSoruTabloVeri5 = kModel.TabloVeri5.Select((s, inx) => new { TabloVeri5 = s, inx = inx }).ToList();
            var qAnketSoruSecenekAciklama = kModel.AnketSoruSecenekAciklama.Select((s, inx) => new { AnketSoruSecenekAciklama = s, inx = inx }).ToList();

            var qGroup = new List<AnketPostGroupModel>();

            #region grupla
            qGroup = (from s in qAnketSoruId
                      join ss in qAnketSoruSecenekId on s.inx equals ss.inx
                      join ac in qAnketSoruSecenekAciklama on s.inx equals ac.inx
                      join bss in _entities.AnketSorus on new { s.AnketSoruID, kModel.AnketID } equals new { bss.AnketSoruID, bss.AnketID }
                      join bssx in _entities.AnketSoruSeceneks on new { bss.AnketSoruID, ss.AnketSoruSecenekID } equals new { bssx.AnketSoruID, AnketSoruSecenekID = (int?)bssx.AnketSoruSecenekID } into def1
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



            var hatalilar = new List<int>();




            if (qGroup.Any(p => !p.IsTabloVeriGirisi && p.AnketSoruSecenekID.HasValue == false))
            {
                var data = qGroup.Where(p => !p.IsTabloVeriGirisi && p.AnketSoruSecenekID.HasValue == false).ToList();
                mMessage.Messages.Add("Lütfen cevaplamadığınız anket sorularını cevaplayınız.");


                foreach (var item in data)
                {
                    mMessage.Messages.Add(item.inx + " Numaralı soru cevaplanmadı.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnketSoruSecenekID_" + item.AnketSoruID });
                    hatalilar.Add(item.AnketSoruID);
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
                    hatalilar.Add(item.AnketSoruID);
                }
            }
            else if (qGroup.Any(p => !p.IsTabloVeriGirisi && p.IsEkAciklamaGir && p.AnketSoruSecenekAciklama.IsNullOrWhiteSpace()))
            {
                var data = qGroup.Where(p => !p.IsTabloVeriGirisi && p.IsEkAciklamaGir && p.AnketSoruSecenekAciklama.IsNullOrWhiteSpace()).OrderBy(o => o.inx).ToList();
                foreach (var item in data)
                {

                    mMessage.Messages.Add(item.inx + " Numaralı sorunu için lütfen açıklama giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnketSoruSecenekID_" + item.AnketSoruID });
                    hatalilar.Add(item.AnketSoruID);
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
                            dctVal.Add(item3.Name, item3.GetValue(item2).ToStrObjEmptString());
                        }
                        if (dctVal.Take(item.SecenekCount).Any(p => !p.Value.IsNullOrWhiteSpace()) && dctVal.Take(item.SecenekCount).Any(p => p.Value.IsNullOrWhiteSpace()))
                        {
                            mMessage.Messages.Add(item.inx + " Numaralı sorunu için lütfen açıklama giriniz.");
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnketSoruSecenekID_" + item.AnketSoruID });
                            hatalilar.Add(item.AnketSoruID);
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
                    if (UserIdentity.Current.Informations.All(p => p.Key != "LUBAnket")) UserIdentity.Current.Informations.Add("LUBAnket", lstData);
                    else UserIdentity.Current.Informations["LUBAnket"] = lstData;
                }
                else if (kModel.AnketTipID == 2)
                {
                    if (UserIdentity.Current.Informations.All(p => p.Key != "BTAnket")) UserIdentity.Current.Informations.Add("BTAnket", lstData);
                    else UserIdentity.Current.Informations["BTAnket"] = lstData;
                }
                else if (kModel.AnketTipID == 3)
                {
                    var nRwId = new Guid(kModel.RowID);
                    var basvuru = _entities.Basvurulars.FirstOrDefault(p => p.RowID == nRwId);
                    if (basvuru != null && basvuru.BasvuruSurec.KayitOlmayanlarAnketID.HasValue && basvuru.AnketCevaplaris.All(p => p.AnketID != basvuru.BasvuruSurec.KayitOlmayanlarAnketID))
                    {
                        foreach (var item in lstData)
                        {
                            item.Tarih = DateTime.Now;
                            basvuru.AnketCevaplaris.Add(item);
                        }
                        _entities.SaveChanges();
                    }
                }
                else if (kModel.AnketTipID == 4)
                {
                    var nRwId = new Guid(kModel.RowID);
                    var basvuru = _entities.MezuniyetBasvurularis.FirstOrDefault(p => p.RowID == nRwId);
                    if (basvuru != null && basvuru.MezuniyetSureci.AnketID.HasValue && basvuru.AnketCevaplaris.All(p => p.AnketID != basvuru.MezuniyetSureci.AnketID))
                    {
                        foreach (var item in lstData)
                        {
                            item.Tarih = DateTime.Now;
                            basvuru.AnketCevaplaris.Add(item);
                        }
                        _entities.SaveChanges();
                    }
                }
                mMessage.IsSuccess = true;
                mMessage.MessageType = Msgtype.Success;
                //mMessage.Title = "Anket bilgileri doldurduğunuz için teşekkür ederiz.";

            }
            var hatasizlar = qGroup.Where(p => hatalilar.Contains(p.AnketSoruID) == false).Select(s => s.AnketSoruID).ToList();
            foreach (var item in hatasizlar)
            {
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AnketSoruSecenekID_" + item });

            }
            return mMessage.ToJsonResult();

        }

        public ActionResult GetAnketCevap(FrAnketDetayDto model)
        {
            return View();
        }
        [Authorize]
        public ActionResult DAktifSinavlar(BasvuruTercihKontrolDto model)
        {
            var qOgrenimTipKods = model.OgrenimTipKods.Select((s, inx) => new { s = s, Index = inx }).ToList();
            var qProgramKods = model.ProgramKods.Select((s, inx) => new { s = s, Index = inx }).ToList();
            var qIngilizces = model.Ingilizces.Select((s, inx) => new { s = s, Index = inx }).ToList();
            var qtercihler = (from s in qOgrenimTipKods
                              join qp in qProgramKods on s.Index equals qp.Index
                              join qi in qIngilizces on s.Index equals qi.Index
                              select new CmbMultyTypeDto { Value = s.s, ValueB = qi.s, ValueS2 = qp.s }).ToList();
            var data = Management.cmbGetdAktifSinavlar(qtercihler, model.BasvuruSurecID, model.SinavTipGrupID, true);
            return data.Select(s => new { s.Value, s.Caption }).ToJsonResult();
        }
        [Authorize]
        public ActionResult GetSalonlar(string enstituKod, int srTalepTipId, int? talepYapanId = null, int? id = null)
        {
            var cmbmld = SrTalepleriBus.GetCmbSalonlar(enstituKod, srTalepTipId, true);
            var ttip = _entities.SRTalepTipleris.First(p => p.SRTalepTipID == srTalepTipId);

            var kotaBilgi = new CmbMultyTypeDto
            {
                ValueB = true
            };
            if (talepYapanId.HasValue)
            {
                kotaBilgi = SrTalepleriBus.GetSrKotaBilgi(talepYapanId.Value, srTalepTipId, id);
            }
            return new { IsTezSinavi = ttip.IsTezSinavi, kotaBilgi = kotaBilgi, data = cmbmld.Select(s => new { s.Value, s.Caption }) }.ToJsonResult();
        }
        [Authorize]
        public ActionResult GetGunler(int srSalonId, int srTalepTipId, DateTime tarih, DateTime? tarih2 = null, int? srOzelTanimId = null)
        {

            var ttip = _entities.SRTalepTipleris.First(p => p.SRTalepTipID == srTalepTipId);

            var gunler = _entities.SRSaatlers.Where(p => p.SRSalonID == srSalonId).Select(s => s.HaftaGunID).Distinct();

            var gunL = _entities.HaftaGunleris.Where(p => gunler.Contains(p.HaftaGunID)).Select(s => new CmbIntDto { Value = s.HaftaGunID, Caption = s.HaftaGunAdi }).ToList();

            for (DateTime date = tarih; date <= tarih2.Value; date = date.AddDays(1.0))
            {
                var nTarih = date.ToShortDateString().ToDate().Value;
                var dofW = nTarih.DayOfWeek.ToString("d").ToInt().Value;

                var salon = _entities.SRSalonlars.First(p => p.SRSalonID == srSalonId);

                var haftaGunu = _entities.HaftaGunleris.First(p => p.HaftaGunID == dofW);
                var resmiTatilDegisen = _entities.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilDegisen && p.BasTarih.Value <= nTarih && p.BitTarih >= nTarih);
                var resmiTatilSabit = _entities.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilSabit && p.Ay.Value == nTarih.Month && p.Gun == nTarih.Day);
                var rezervasyonlar = _entities.SROzelTanimlars.Where(p => p.SROzelTanimID != (srOzelTanimId ?? 0) && p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.Rezervasyon && p.SRSalonID == srSalonId && p.Tarih == nTarih).ToList();
                var rezerve = _entities.SROzelTanimlars.Where(p => p.SROzelTanimID != (srOzelTanimId ?? 0) && p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.Rezerve && p.SRSalonID == srSalonId && p.Tarih == nTarih).ToList();
                var tTip = _entities.SRTalepTipleris.First(p => p.SRTalepTipID == srTalepTipId);
                bool isSuccess = true;
                var qTalepEslesen = _entities.SRTalepleris.Where(a => a.SRSalonID == srSalonId && a.Tarih == nTarih).Any(p => p.SRDurumID == SRTalepDurum.Onaylandı || p.SRDurumID == SRTalepDurum.TalepEdildi);
                if (qTalepEslesen)
                {
                    isSuccess = false;
                }
                else if (resmiTatilDegisen != null || resmiTatilSabit != null)
                {
                    isSuccess = false;
                }
                else if (rezerve.Count > 0)
                {
                    foreach (var itemRo in rezerve.Where(p => p.SROzelTanimGunlers.Any(a => a.HaftaGunID == dofW)))
                    {
                        if (rezerve.Any(p => p.SROzelTanimGunlers.Any(a => a.HaftaGunID == dofW)))
                        {
                            isSuccess = false;
                        }
                    }
                }
                else if (rezervasyonlar.Count > 0)
                {
                    isSuccess = false;

                }
                var sgun = gunL.FirstOrDefault(p => p.Value == dofW);
                if (sgun != null && isSuccess == false) gunL.Remove(sgun);
            }


            return View(gunL);
        }

        [Authorize]
        public ActionResult getSaatList(int srSalonId, bool isPopupFrame, int srTalepTipId, DateTime tarih, int? srTalepId, string mzRowId = null)
        {

            DateTime? minTarih = null;
            if (mzRowId.IsNullOrWhiteSpace() == false)
            {
                var rwId = new Guid(mzRowId);
                minTarih = _entities.MezuniyetBasvurularis.Where(p => p.RowID == rwId).Select(s => s.EYKTarihi).FirstOrDefault();
            }
            var data = SrTalepleriBus.GetSalonBosSaatler(srSalonId, srTalepTipId, tarih, srTalepId, null, minTarih);
            data.IsPopupFrame = isPopupFrame;
            var hcb = ViewRenderHelper.RenderPartialView("Ajax", "getSaatlerView", data);
            return new { Deger = hcb }.ToJsonResult();
        }
        [Authorize]
        public ActionResult getSaatlerView(SRSalonSaatlerModel model)
        {
            return View(model);
        }


        [Authorize]

        public ActionResult getJuriEkleKontrol(string juriAdi, string email)
        {
            var mmMessage = new MmMessage();
            mmMessage.Title = "Jüri bilgisi eklenemedi!";

            if (juriAdi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Jüri adı boş bırakılamaz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_JuriAdi" });
            }
            if (email.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("E-Posta Bilgisi Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_Email" });
            }
            else if (email.ToIsValidEmail())
            {
                mmMessage.Messages.Add("E-Posta Formatı Uygun Değildir.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_Email" });
            }
            var strView = "";
            if (mmMessage.Messages.Count > 0)
            {
                mmMessage.IsSuccess = mmMessage.Messages.Count == 0;
                mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
                strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
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
                model.EnstituKod = _entities.BasvuruSurecs.Where(p => p.BasvuruSurecID == model.BasvuruSurecID.Value).First().EnstituKod;
                ViewBag.IsBolumOrOgrenci = new SelectList(Management.cmbBolumOrOgrenci(false), "Value", "Caption", model.IsBolumOrOgrenci);
                var cmbOgrenimTipleris = Management.cmbGetAktifOgrenimTipleri(model.BasvuruSurecID.Value, false);
                ViewBag.OgrenimTipKods = new SelectList(cmbOgrenimTipleris, "Value", "Caption", model.OgrenimTipKods);

                ViewBag.ProgramKods = new SelectList(new List<CmbBoolDto>(), "Value", "Caption", model.ProgramKods);
                ViewBag.BasvuruDurumID = new SelectList(Management.cmbBasvuruDurumListe(false), "Value", "Caption");
                ViewBag.KayitDurumIDs = new SelectList(Management.cmbKayitDurum(), "Value", "Caption");
                ViewBag.MulakatSonucTipIDs = new SelectList(Management.cmbMulakatSonucTip(false), "Value", "Caption");

            }
            else
            {
                model.EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            }
            var secilenKullaniciIDs = model.SecilenAlicilars.Where(p => p.IsNumber()).Select(s => s.ToInt().Value).ToList();
            var secilenEmails = model.SecilenAlicilars.Where(p => p.IsNumber() == false).Select(s => new CmbStringDto { Value = s, Caption = s }).ToList();

            if (!model.IsTopluMail)
            {
                if (secilenKullaniciIDs.Any()) model.EMails = _entities.Kullanicilars.Where(p => secilenKullaniciIDs.Contains(p.KullaniciID)).Select(s => new CmbStringDto { Value = (s.KullaniciID + ""), Caption = s.EMail }).ToList();
                if (secilenEmails.Any()) model.EMails.AddRange(secilenEmails);
            }
            if (!model.BasvuruRowID.IsNullOrWhiteSpace())
            {
                var basvuru = _entities.Basvurulars.Where(p => p.RowID == new Guid(model.BasvuruRowID)).FirstOrDefault();
                if (basvuru != null) model.EMails.Add(new CmbStringDto { Value = basvuru.EMail.Trim(), Caption = basvuru.EMail.Trim() });
                else model.BasvuruRowID = null;
            }

            ViewBag.MailSablonlariID = new SelectList(MailSablonTipleriBus.GetCmbMailSablonlari(model.EnstituKod, true, false), "Value", "Caption");

            ViewBag.MmMessage = new MmMessage();
            return View(model);
        }




        [HttpPost]
        [ValidateInput(false)]
        [Authorize(Roles = RoleNames.MailGonder)]
        public ActionResult MailGonderPost(KmMailGonder model, List<HttpPostedFileBase> DosyaEki, List<string> dosyaEkiAdi, List<string> ekYolu, string ekd)
        {
            var mmMessage = new MmMessage();
            mmMessage.Title = "Mail gönderme işlemi";
            string _EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            DosyaEki = DosyaEki == null ? new List<HttpPostedFileBase>() : DosyaEki;
            dosyaEkiAdi = dosyaEkiAdi == null ? new List<string>() : dosyaEkiAdi;
            ekYolu = ekYolu == null ? new List<string>() : ekYolu;

            var secilenAlicilar = new List<string>();
            if (model.Alici.IsNullOrWhiteSpace() == false) model.Alici.Split(',').ToList().ForEach((itm) => { secilenAlicilar.Add(itm); });
            if (model.Aciklama.IsNullOrWhiteSpace() == false)
            {
                var cevapA = "";
                var geriDonusLink = "";
                if (model.MesajID.HasValue)
                {
                    var enstitu = _entities.Enstitulers.Where(p => p.EnstituKod == _EnstituKod).First();
                    var mesaj = _entities.Mesajlars.Where(p => p.MesajID == model.MesajID.Value).First();
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
                    var surec = _entities.BasvuruSurecs.Where(p => p.BasvuruSurecID == model.BasvuruSurecID.Value).First();
                    //Yetkili Kullanıcılara MAİLGÖNDER
                    var rollers = new List<string>();
                    if (surec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) rollers = new List<string>() { RoleNames.MulakatSureci, RoleNames.MulakatKayıt };
                    else if (surec.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru) rollers = new List<string>() { RoleNames.YgMulakatSureci, RoleNames.YgMulakatKayıt };

                    var qBolumlerEmailList = _entities.KullaniciProgramlaris.Where(p => (p.Kullanicilar.Rollers.Any(a => rollers.Contains(a.RolAdi))
                                                                                || p.Kullanicilar.YetkiGruplari.YetkiGrupRolleris.Any(a => rollers.Contains(a.Roller.RolAdi))) &&
                                                                                model.ProgramKods.Contains(p.ProgramKod)).Select(s => s.Kullanicilar.EMail).ToList();

                    var kullanicilar = _entities.KullaniciProgramlaris.Where(p => (p.Kullanicilar.Rollers.Any(a => rollers.Contains(a.RolAdi))
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
                    var qogrenciEmailList = _entities.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruSurecID == model.BasvuruSurecID && model.ProgramKods.Contains(p.ProgramKod)).AsQueryable();

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

            var qDosyaEkAdi = dosyaEkiAdi.Select((s, inx) => new { s, inx }).ToList();
            var qDosyaEki = DosyaEki.Select((s, inx) => new { s, inx }).ToList();
            var qDosyaEkYolu = ekYolu.Select((s, inx) => new { s, inx }).ToList();

            var qDosyalar = (from ekGirilenAd in qDosyaEkAdi
                             join eklenenEk in qDosyaEki on ekGirilenAd.inx equals eklenenEk.inx
                             join varolanEkYolu in qDosyaEkYolu on ekGirilenAd.inx equals varolanEkYolu.inx
                             select new
                             {
                                 ekGirilenAd.inx,
                                 Dosya = eklenenEk.s,
                                 FExtension = eklenenEk.s != null ? (ekGirilenAd.s + eklenenEk.s.FileName.GetFileExtension()) : (ekGirilenAd.s),
                                 DosyaYolu = eklenenEk.s != null ? ("/MailDosyalari/" + ekGirilenAd.s.ToFileNameAddGuid(eklenenEk.s.FileName.GetFileExtension())) : (varolanEkYolu.s)
                             }).ToList();
            var kModel = new GonderilenMailler();
            #region Kontrol 
            if (!model.BasvuruRowID.IsNullOrWhiteSpace())
            {
                var rowId = new Guid(model.BasvuruRowID);
                var basvuru = _entities.Basvurulars.FirstOrDefault(p => p.RowID == rowId);
                if (basvuru != null)
                {
                    secilenAlicilar.Add(basvuru.EMail);
                    kModel.BasvuruID = basvuru.BasvuruID;
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
            kModel.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);
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

                var eklenen = _entities.GonderilenMaillers.Add(kModel);

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
                    var mesaj = _entities.Mesajlars.FirstOrDefault(p => p.MesajID == model.MesajID.Value);
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
                    var dataqx = (from s in _entities.Kullanicilars
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

                _entities.SaveChanges();
                if (model.MesajID.HasValue)
                {
                    var mesaj = _entities.Mesajlars.First(p => p.MesajID == model.MesajID);
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
                _entities.SaveChanges();
                var attach = new List<Attachment>();
                foreach (var item in qDosyalar)
                {
                    var ekTamYol = Server.MapPath("~" + item.DosyaYolu);
                    if (System.IO.File.Exists(ekTamYol))
                    {
                        var fExtension = Path.GetExtension(ekTamYol);
                        attach.Add(new Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)), item.FExtension.ToSetNameFileExtension(fExtension), MediaTypeNames.Application.Octet));
                    }
                    else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + item.FExtension + " <br/>Dosya Yolu:" + ekTamYol, "Ajax/MailGonderPost", LogType.Hata);
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
                    var excpt = MailManager.SendMailRetVal(_EnstituKod, kModel.Konu, kModel.AciklamaHtml, item.Value, attach);
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
                            _entities.GonderilenMaillers.Remove(eklenen);
                            //db.SaveChanges();
                            foreach (var item2 in qDosyalar)
                            {
                                if (System.IO.File.Exists(Server.MapPath("~" + item2.DosyaYolu)))
                                    System.IO.File.Delete(Server.MapPath("~" + item2.DosyaYolu));
                            }
                        }
                        catch (Exception ex)
                        {
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), "Ajax/MailGonderPost<br/><br/>" + ex.ToExceptionStackTrace(), LogType.Hata);
                        }
                    }
                }
            }
            else
            {
                mmMessage.IsSuccess = false;
                mmMessage.MessageType = Msgtype.Warning;
            }

            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            //return Content(strView, MediaTypeNames.Text.Html);
            return Json(new { success = mmMessage.IsSuccess, responseText = strView }, JsonRequestBehavior.AllowGet);
            //return new JsonResult { Data = new { IsSuccess = mmMessage.IsSuccess, Message = strView } };

        }


        [Authorize(Roles = RoleNames.MailGonder)]
        public ActionResult GetTumMailListesi(string term)
        {
            var qKullanicilar = (from k in _entities.Kullanicilars
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
                Images = k.Images.ToKullaniciResim()

            }).ToList();
            if (kul.Count == 0)
            {
                var lst = new List<mailListModel>();
                if (!term.ToIsValidEmail())
                {
                    lst.Add(new mailListModel { id = term, AdSoyad = term, text = term, Images = "".ToKullaniciResim() });
                }
                return lst.ToJsonResult();
            }

            else return kul.ToJsonResult();
        }

        [Authorize]
        public ActionResult GetProgramListesi(string term)
        {
            var programlars = (from p in _entities.Programlars
                               join prl in _entities.Programlars on p.ProgramKod equals prl.ProgramKod
                               join abl in _entities.AnabilimDallaris on p.AnabilimDaliKod equals abl.AnabilimDaliKod
                               orderby prl.ProgramAdi
                               where prl.ProgramAdi.Contains(term)
                               select new
                               {
                                   id = p.ProgramKod,
                                   AnabilimDaliAdi = abl.AnabilimDaliAdi,
                                   ProgramAdi = prl.ProgramAdi + " [" + prl.ProgramKod + "]",
                                   text = p.ProgramKod,

                               }).Take(60).ToList();


            return programlars.ToJsonResult();
        }

        [Authorize]
        public ActionResult BasvuruGonderilenMailler(string RowID)
        {
            var basvuru = _entities.Basvurulars.Where(p => p.RowID == new Guid(RowID)).First();

            return View(basvuru);
        }

        [Authorize]
        public ActionResult getSablonlar(int mailSablonlariId)
        {
            var kulId = UserIdentity.Current.Id;
            var sbl = _entities.MailSablonlaris.Where(p => p.MailSablonlariID == mailSablonlariId).Select(s => new { s.SablonAdi, s.Sablon, s.SablonHtml, MailSablonlariEkleri = s.MailSablonlariEkleris.Select(s2 => new { s2.MailSablonlariEkiID, s2.EkAdi, s2.EkDosyaYolu }) }).First();
            return Json(new { sbl.SablonAdi, sbl.Sablon, sbl.SablonHtml, sbl.MailSablonlariEkleri }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        public ActionResult getOTSBS(int basvuruSurecId)
        {
            var kulId = UserIdentity.Current.Id;
            var ots = Management.cmbGetAktifOgrenimTipleri(basvuruSurecId, false);
            return ots.Select(s => new { s.Value, s.Caption }).ToJsonResult();
        }
        [Authorize]
        public ActionResult getProgramlarBS(int basvuruSurecId, List<int> ogrenimTipKods, bool isBolumOrOgrenci, bool isSonucOrMulakat)
        {
            var kulId = UserIdentity.Current.Id;
            ogrenimTipKods = ogrenimTipKods ?? new List<int>();
            ogrenimTipKods = ogrenimTipKods.Where(p => p > 0).ToList();
            var progs = Management.CmbGetBSTumProgramlar(basvuruSurecId, isBolumOrOgrenci, ogrenimTipKods, isSonucOrMulakat);
            return progs.Select(s => new { s.Value, s.Caption }).ToJsonResult();
        }

        [AllowAnonymous]
        public ActionResult getMsjKategoris(string enstituKod)
        {
            var ots = MesajlarBus.cmbGetMesajKategorileri(enstituKod, true, true);
            return ots.Select(s => new { s.Value, s.Caption }).ToJsonResult();
        }
        public ActionResult getKtNot(int mesajKategoriId)
        {
            string not = "";
            var mkNot = _entities.MesajKategorileris.Where(p => p.MesajKategoriID == mesajKategoriId).FirstOrDefault();
            if (mkNot != null) not = mkNot.KategoriAciklamasi;
            return Json(new { NotBilgisi = not });
        }
        public ActionResult MesajKaydet(string dlgid, string GroupID, string ekd)
        {
            var model = new Mesajlar();
            var mmMessage = new MmMessage();
            mmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            mmMessage.DialogID = dlgid;
            ViewBag.MmMessage = mmMessage;
            string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            if (GroupID.IsNullOrWhiteSpace() == false)
            {
                model = _entities.Mesajlars.Where(p => p.GroupID == GroupID).First();
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
            ViewBag.EnstituAdi = _entities.Enstitulers.Where(p => p.EnstituKod == enstituKod).First().EnstituAd;
            ViewBag.MesajKategoriID = new SelectList(MesajlarBus.cmbGetMesajKategorileri(enstituKod, true, model != null), "Value", "Caption", model.MesajKategoriID);

            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(false), "Value", "Caption", enstituKod);

            return View(model);
        }



        [HttpPost]
        [ValidateInput(false)]
        public ActionResult MesajKaydetPost(int MesajID, string groupId, int mesajKategoriId, string konu, string adSoyad, string email, string aciklama, string aciklamaHtml, List<HttpPostedFileBase> dosyaEki, List<string> dosyaEkiAdi, string ekd)
        {
            konu = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(konu, false);
            adSoyad = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(adSoyad, false);
            aciklama = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(aciklama, false);
            email = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(email, false);
            groupId = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(groupId, false);

            //XssSaldırısı Var chec

            var mmMessage = new MmMessage();
            mmMessage.Title = "Dilek/Öneri/Şikayet gönderme işlemi";
            string _EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            dosyaEki = dosyaEki == null ? new List<HttpPostedFileBase>() : dosyaEki;
            dosyaEkiAdi = dosyaEkiAdi == null ? new List<string>() : dosyaEkiAdi;

            var qDosyaEkAdi = dosyaEkiAdi.Select((s, inx) => new { s, inx }).ToList();
            var qDosyaEki = dosyaEki.Select((s, inx) => new { s, inx }).ToList();

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
                if (!FileExtension.FExtensions().Contains(item.FExtension))
                {
                    mmMessage.Messages.Add(item.Dosya.FileName + " dosyası " + string.Join(" , ", FileExtension.FExtensions()).Replace(".", "") + " uzantılarından farklı bir dosya olamaz.");
                }
            }


            #region Kontrol 


            if (MesajID <= 0)
            {
                if (konu.IsNullOrWhiteSpace())
                {
                    string msg = "Konu Giriniz.";
                    mmMessage.Messages.Add(msg);
                }
            }
            else
            {
                var mesaj = _entities.Mesajlars.Where(p => p.MesajID == MesajID && p.GroupID == groupId).First();
                mesaj.IsAktif = false;
                konu = mesaj.Konu;
                mesajKategoriId = mesaj.MesajKategoriID;
                if (UserIdentity.Current.IsAuthenticated && mesaj.KullaniciID != UserIdentity.Current.Id)
                {
                    email = UserIdentity.Current.Description;
                    adSoyad = UserIdentity.Current.NameSurname;
                }
                else
                {
                    email = mesaj.Email;
                    adSoyad = mesaj.AdSoyad;

                }
            }
            if (aciklama.IsNullOrWhiteSpace() && aciklamaHtml.IsNullOrWhiteSpace())
            {
                string msg = "İçerik Giriniz.";
                mmMessage.Messages.Add(msg);
            }

            var kModel = new Mesajlar();
            kModel.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                kModel.MesajKategoriID = mesajKategoriId;
                if (UserIdentity.Current.IsAuthenticated == false)
                {
                    kModel.AdSoyad = adSoyad;
                    kModel.Email = email;

                }
                else
                {
                    var kul = _entities.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First();
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
                kModel.Konu = konu;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.Aciklama = aciklama ?? "";
                kModel.AciklamaHtml = aciklamaHtml ?? "";

                var eklenen = _entities.Mesajlars.Add(kModel);
                foreach (var item in qDosyalar)
                {
                    item.Dosya.SaveAs(Server.MapPath("~" + item.DosyaYolu));
                    eklenen.MesajEkleris.Add(new MesajEkleri
                    {
                        EkAdi = item.DosyaAdi,
                        EkDosyaYolu = item.DosyaYolu
                    });
                }



                _entities.SaveChanges();
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
                _entities.SaveChanges();
                mmMessage.IsSuccess = true;

                if (MesajID <= 0 && groupId.IsNullOrWhiteSpace())
                {
                    var sablon = _entities.MailSablonlaris.Where(p => p.EnstituKod == _EnstituKod && p.MailSablonTipID == MailSablonTipi.GelenIlkMesajOtoCvpMaili && p.IsAktif == true).FirstOrDefault();
                    if (sablon != null)
                    {
                        var itemE = sablon.Enstituler;
                        var enstituL = sablon.Enstituler;
                        var paramereDegerleri = new List<MailReplaceParameterDto>();
                        var parametreler = sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        if (parametreler.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstituL.EnstituAd });
                        if (parametreler.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = itemE.WebAdresi, IsLink = true });
                        if (parametreler.Any(a => a == "@AdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = kModel.AdSoyad });
                        var eMailList = new List<MailSendList> { new MailSendList { EMail = kModel.Email, ToOrBcc = true } };
                        if (sablon.GonderilecekEkEpostalar.IsNullOrWhiteSpace() == false)
                            eMailList.AddRange(sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                        var mCOntent = SystemMails.GetSystemMailContent(enstituL.EnstituAd, sablon.SablonHtml, sablon.SablonAdi, paramereDegerleri);
                        var attach = new List<Attachment>();
                        foreach (var item in sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = Server.MapPath("~" + item.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                attach.Add(new Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)), item.EkAdi.ToSetNameFileExtension(fExtension), MediaTypeNames.Application.Octet));
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + item.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Ajax/MesajKaydetPost", LogType.Uyarı);
                        }
                        var snded = MailManager.SendMail(itemE.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, eMailList, attach);

                    }
                }
            }
            else
            {
                mmMessage.IsSuccess = false;
            }

            //var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
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
            var view = ViewRenderHelper.RenderPartialView("Ajax", "ObsOgrenciSorgula", model);
            return view.ToJsonResult();
        }


        [Authorize]
        public ActionResult GetEnstituOgrencileri(string term, string EnstituKod)
        {

            var kuls = (from k in _entities.Kullanicilars
                        join kt in _entities.KullaniciTipleris on k.KullaniciTipID equals kt.KullaniciTipID
                        orderby k.Ad, k.Soyad
                        where (k.TcKimlikNo == term || k.OgrenciNo == term || (k.Ad + " " + k.Soyad).Contains(term))
                        select new
                        {
                            id = k.KullaniciID + "",
                            AdSoyad = k.Ad + " " + k.Soyad,
                            text = k.Ad + " " + k.Soyad,
                            kt.KullaniciTipAdi,
                            k.TcKimlikNo,
                            k.OgrenciNo,
                            Images = k.ResimAdi

                        }).Take(15).ToList(); 
            return kuls.ToJsonResult();
        }
        [Authorize]
        public ActionResult GetYTUOgretimEleman(string term)
        {
            var data = Management.getWsPersisOE(term);
            var YtuUni = _entities.Universitelers.Where(p => p.UniversiteID == 67).FirstOrDefault();
            var kul2 = data.Table.Select(s => new
            {
                id = s.ADSOYAD,
                AdSoyad = s.ADSOYAD,
                text = s.ADSOYAD,
                BolumAdi = s.BOLUMADI.Replace("BÖLÜMÜ", ""),
                UnvanAdi = s.AKADEMIKUNVAN.ToJuriUnvanAdi(),
                UniversiteID = YtuUni != null ? YtuUni.UniversiteID : 67,
                UniversiteAdi = (YtuUni != null ? YtuUni.Ad : "Yıldız Teknik Üniversitesi (İstanbul)").ToUpper(),
                EMail = s.KURUMMAIL
            }).OrderBy(o => o.AdSoyad).Take(25).ToList();

            return kul2.ToJsonResult();
        }
        [Authorize]
        public ActionResult GetYTULUBOgretimEleman(string term)
        {
            var danismanUnvanIDs = new List<int>() { 17, 42, 73 }; //Doç.Dr Prof.Dr, Dr. Öğr. Üye 
            var kul2 = _entities.Kullanicilars.Where(p => p.KullaniciTipID == KullaniciTipBilgi.AkademikPersonel && danismanUnvanIDs.Contains(p.UnvanID ?? 0) && (p.Ad + " " + p.Soyad).StartsWith(term)).OrderBy(o => o.Ad).ThenBy(t => t.Soyad).Take(25).Select(s => new
            {
                id = s.KullaniciID,
                AdSoyad = s.Ad + " " + s.Soyad,
                text = s.Ad + " " + s.Soyad,
                s.Birimler.BirimAdi,
                s.Unvanlar.UnvanAdi

            }).ToList();

            return kul2.ToJsonResult();
        }
        [Authorize]
        public ActionResult GetDxReport(int? RaporTipi, bool isPdfStream = false)
        {
            XtraReport rprX = null;
            if (RaporTipi.HasValue == false)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Rapor almak için rapor tipinin gönderilmesi gerekmektedir!", "Ajax/GetDxReport", LogType.Hata);
            }
            else
            {
                if (RaporTipi == RaporTipleri.Basvuru)
                {
                    #region BasvuruFormu
                    var basvId = Request["BasvuruID"].ToIntObj();
                    var btId = Request["BasvuruTercihID"].ToIntObj();
                    var otId = Request["OgrenimTipKod"].ToIntObj();


                    var gd = Guid.NewGuid().ToString().Substr(0, 5);

                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var terch = new List<BasvurularTercihleri>();
                        var basvuru = db.Basvurulars.Where(p => p.BasvuruID == basvId).First();
                        if (btId.HasValue) terch = db.BasvurularTercihleris.Where(p => p.BasvuruTercihID == btId).ToList();
                        else terch = db.BasvurularTercihleris.Where(p => p.BasvuruID == basvId).ToList();
                        rprBasvuruDoktora rprDoktora = null;
                        rprBasvuruYL rprYl = null;

                        var doktoraTercihleri = terch.Where(p => p.OgrenimTipKod == OgrenimTipi.Doktra).ToList();
                        if (doktoraTercihleri.Count > 0)
                        {
                            foreach (var item in doktoraTercihleri)
                            {
                                var _rprDoktora = new rprBasvuruDoktora(item.BasvuruTercihID);
                                if (rprDoktora == null) rprDoktora = _rprDoktora;
                                else rprDoktora.Pages.AddRange(_rprDoktora.Pages);

                                rprDoktora.DisplayName = basvuru.Ad + " " + basvuru.Soyad + " DoktoraBF_" + gd;
                                rprX = rprDoktora;
                            }
                        }
                        var YLTercihleri = terch.Where(p => p.OgrenimTipKod != OgrenimTipi.Doktra).OrderBy(o => o.SiraNo).ToList();
                        if (YLTercihleri.Count > 0)
                        {
                            if (YLTercihleri.Count == 1)
                            {
                                var _rprYL = new rprBasvuruYL(YLTercihleri[0].BasvuruTercihID, null);
                                _rprYL.CreateDocument();
                                if (rprYl == null) rprYl = _rprYL;
                                else rprYl.Pages.AddRange(_rprYL.Pages);

                            }
                            else
                            {
                                var _rprYL = new rprBasvuruYL(YLTercihleri[0].BasvuruTercihID, YLTercihleri[1].BasvuruTercihID);
                                _rprYL.CreateDocument();
                                if (rprYl == null) rprYl = _rprYL;
                                else rprYl.Pages.AddRange(_rprYL.Pages);
                            }
                            rprYl.DisplayName = basvuru.Ad + " " + basvuru.Soyad + " YüksekLisansBF_" + gd;
                            rprX = rprYl;
                        }


                    }
                    #endregion
                }
                else if (RaporTipi == RaporTipleri.BasvuruOgrenciListesi)
                {
                    #region BasvuruOgrenciListesi
                    var _BasvuruSurecID = Request["BasvuruSurecID"].ToIntObj();
                    var _OgrenimTipKod = Request["OgrenimTipKod"].ToIntObj();
                    var _AlanTipID = Request["AlanTipID"].ToIntObj();
                    var _ProgramKod = Request["ProgramKod"].ToStrObjEmptString();
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
                        var basvuruSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == _BasvuruSurecID).First();
                        if (basvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) rpr.DisplayName = "Lisansüstü Başvuran Öğrenci Listesi";
                        else rpr.DisplayName = "Lisansüstü Yatay Geçiş Başvuran Öğrenci Listesi";
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFilePageByPage;

                        rprX = rpr;

                    }
                    #endregion
                }
                else if (RaporTipi == RaporTipleri.BasvuruSonucListesi)
                {
                    #region bSonucListesi
                    var _BasvuruSurecID = Request["BasvuruSurecID"].ToIntObj();
                    var _AnabilimdaliKod = Request["AnabilimdaliKod"].ToStrObj();
                    var _ProgramKod = Request["ProgramKod"].ToStrObj();
                    var _OgrenimTipKod = Request["OgrenimTipKod"].ToIntObj();
                    var _SubRaporTipID = Request["SubRaporTipID"].ToIntObj();
                    var _MulakatSonucTipIDstr = Request["MulakatSonucTipID"];
                    _MulakatSonucTipIDstr = _MulakatSonucTipIDstr.ToStrObjEmptString();
                    var ekBilgiTipId = Request["EkBilgiTipID"].ToIntObj();
                    var _KayitDurumID = Request["KayitDurumID"].ToIntObj();
                    var oTips = Request["OgrenimTips"].ToStrObjEmptString();
                    var ogrenimTipKodus = new List<int>();
                    if (!oTips.IsNullOrWhiteSpace()) ogrenimTipKodus = oTips.Split(',').Select(s => s.ToInt().Value).ToList();
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
                                from kd in defKd.DefaultIfEmpty()
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
                                    Telefon = b.CepTel ?? (b.EvTel ?? (b.IsTel ?? "")),
                                    EMail = b.EMail,
                                    ms.KayitDurumID,
                                    KayitOldu = kd != null ? kd.IsKayitOldu : (bool?)null

                                };
                        var _RowID = Request["RowID"].ToStrObj();
                        var rowId = new Guid();
                        bool isOgrenciSonucListesindePuanGozuksun = false;
                        if (!_RowID.IsNullOrWhiteSpace())
                        {
                            rowId = new Guid(_RowID);
                            q = q.Where(p => p.RowID == rowId);

                            ekBilgiTipId = 2;
                            var basvuru = db.Basvurulars.Where(p => p.RowID == rowId).First();
                            isOgrenciSonucListesindePuanGozuksun = basvuru.BasvuruSurec.IsOgrenciSonucListesindePuanGozuksun;
                            _BasvuruSurecID = basvuru.BasvuruSurecID;
                            var programKods = basvuru.BasvurularTercihleris.Select(s => s.ProgramKod + "_" + s.OgrenimTipKod + "_" + s.AlanTipID).ToList();
                            q = q.Where(p => programKods.Contains(p.ProgramKod + "_" + p.OgrenimTipKod + "_" + p.AlanTipID));
                        }
                        else
                        {
                            if (ogrenimTipKodus.Any())
                            {
                                q = q.Where(p => ogrenimTipKodus.Contains(p.OgrenimTipKod));
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

                                var dataTumu = data.Where(p => p.BasvuruSurecID == _BasvuruSurecID &&
                                                                                         p.AlanTipID == item.AlanTipID &&
                                                                                         p.ProgramKod == item.ProgramKod &&
                                                                                         p.OgrenimTipKod == item.OgrenimTipKod &&
                                                                                         (p.MulakatSonucTipID == MulakatSonucTipi.Asil || p.MulakatSonucTipID == MulakatSonucTipi.Yedek)).ToList();
                                var dataYedekler = dataTumu.Where(p => p.MulakatSonucTipID == MulakatSonucTipi.Yedek).OrderBy(o => o.SiraNo).ToList();
                                var ekKota = dataTumu.Count > 0 ? dataTumu.First().EkKota : 0;

                                var toplamAsildenKalan = dataTumu.Where(p => p.MulakatSonucTipID == MulakatSonucTipi.Asil && p.KayitOldu == false).Count();
                                var toplamYedekKayit = dataTumu.Where(p => p.MulakatSonucTipID == MulakatSonucTipi.Yedek && p.KayitOldu == true).Count();
                                var kalan = (toplamAsildenKalan + ekKota) - toplamYedekKayit;


                                if (_SubRaporTipID == 2)
                                {
                                    nData.AddRange(dataYedekler);
                                }
                                else if (_SubRaporTipID == 3 && kalan > 0)
                                {
                                    nData.AddRange(dataYedekler.Where(p => p.KayitOldu == null));
                                }
                                else if (_SubRaporTipID == 4 && kalan > 0)
                                {
                                    nData.AddRange(dataYedekler.Where(p => p.KayitOldu == null).Take(kalan));
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
                                    if (item2.RowID != rowId)
                                    {
                                        var isims = item2.AdSoyad.Split(' ').Where(p => !p.IsNullOrWhiteSpace()).ToList();
                                        var maskAdSoyad = "";
                                        foreach (var itemI in isims)
                                        {
                                            maskAdSoyad += itemI.Substr(0, 1) + "**** ";
                                        }

                                        item2.AdSoyad = maskAdSoyad;
                                        if (!isOgrenciSonucListesindePuanGozuksun) item2.GenelBasariNotu = null;
                                    }
                                }
                            }
                        }




                        if (ekBilgiTipId == 3)
                        {
                            var basvuruSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == _BasvuruSurecID).First();
                            var qSinavOt = basvuruSurec.BasvuruSurecSinavTipleriOTNotAraliklaris.ToList();

                            foreach (var itemP in data)
                            {
                                bool sinavYok = qSinavOt.Where(p => p.OgrenimTipKod == itemP.OgrenimTipKod && p.SinavTipleri.SinavTipGrupID == SinavTipGrup.Ales_Gree
                                         && (p.BasvuruSurecSinavTipleriOTNotAraliklariGecersizProgramlars.Any(a => a.ProgramKod == itemP.ProgramKod) == true || !p.IsGecerli || !p.IsIstensin)).Any();

                                if (sinavYok) itemP.AlesNotu = null;
                            }
                            rprBasvuruSonucPuanList rpr = new rprBasvuruSonucPuanList(_BasvuruSurecID.Value);
                            rpr.DataSource = data;
                            if (basvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) rpr.DisplayName = "Lisansüstü Başvuru Sonuç Puan Listesi";
                            else rpr.DisplayName = "Lisansüstü Yatay Geçiş Başvuru Sonuç Puan Listesi";
                            rpr.PrintingSystem.ContinuousPageNumbering = true;
                            rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                            rprX = rpr;
                        }
                        else
                        {

                            rprBasvuruSonucList rpr = new rprBasvuruSonucList(_BasvuruSurecID.Value, ekBilgiTipId.Value);
                            rpr.DataSource = data;
                            var basvuruSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == _BasvuruSurecID).First();
                            if (basvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) rpr.DisplayName = "Lisansüstü Başvuru Sonuç Listesi";
                            else rpr.DisplayName = "Lisansüstü Yatay Geçiş Başvuru Sonuç Listesi";
                            rpr.PrintingSystem.ContinuousPageNumbering = true;
                            rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                            rprX = rpr;
                        }



                    }
                    #endregion
                }
                else if (RaporTipi == RaporTipleri.BasvuruSonucListesiBolum)
                {
                    #region bBSonucListesi 
                    var _MulakatID = Request["MulakatID"].ToIntObj();
                    var _AlanTipID = Request["AlanTipID"].ToIntObj();
                    var _MulakatSonucTipIDstr = Request["MulakatSonucTipID"];
                    var _PuanlarGozuksun = Request["PuanlarGozuksun"].ToBooleanObj();
                    var _SadecePuanliListe = Request["SadecePuanliListe"].ToBooleanObj();
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
                                    from ms in defM.DefaultIfEmpty()
                                    join bt in db.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.Ales_Gree) on b.BasvuruID equals bt.BasvuruID into defBt
                                    from dBt in defBt.DefaultIfEmpty()
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
                                        dBt.WsXmlData,
                                        AlesNotu = kot.IsAlesYerineDosyaNotuIstensin == true ? (ms != null ? ms.AlesNotuOrDosyaNotu : null) : dBt.SinavNotu,
                                        AGNO = b.YLNotSistemID.HasValue ? b.YLMezuniyetNotu100LukSistem : b.LMezuniyetNotu100LukSistem,
                                        at.AlanTipID,
                                        AlanTipAdi = at.AlanTipAdi + (_PuanlarGozuksun.Value ? " Başvuran Öğrenci Sonuç Listesi" : " Başvuran Öğrenci Listesi"),
                                        AdSoyad = b.Ad + " " + b.Soyad,
                                        ProgramGrupAdi = pr.ProgramAdi + " " + ot.OgrenimTipAdi + " (" + at.AlanTipAdi + ")",
                                        MSSiraNo = ms.SiraNo,
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

                            var isAlesYerineDosyaNotuIstensin = kota.IsAlesYerineDosyaNotuIstensin == true;
                            if (!isAlesYerineDosyaNotuIstensin)
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

                            rprX = rpr;
                        }
                        else
                        {
                            rprBasvuruOgrenciList rpr = new rprBasvuruOgrenciList(mulakat.BasvuruSurecID);
                            rpr.DataSource = mdl;
                            if (mulakat.BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) rpr.DisplayName = "Lisansüstü Başvuru Mülakat Sınav Giriş Listesi";
                            else rpr.DisplayName = "Lisansüstü Yatay Geçiş Mülakat Sınav Giriş Listesi";
                            rpr.PrintingSystem.ContinuousPageNumbering = true;
                            rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFilePageByPage;

                            rprX = rpr;
                        }
                    }
                    #endregion
                }
                else if (RaporTipi == RaporTipleri.KesinKayitListesi)
                {
                    #region bBSonucListesi
                    var _BasvuruSurecID = Request["BasvuruSurecID"].ToIntObj();
                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var imgPath = SistemAyar.KullaniciResimYolu;
                        var basvuruSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == _BasvuruSurecID).First();
                        var kayitOlanlar = basvuruSurec.MulakatSonuclaris.Where(p => p.KayitDurumID.HasValue && p.KayitDurumlari.IsKayitOldu == true).ToList().Select(s => new rprModelKazananList
                        {
                            ProgramKodu = s.BasvurularTercihleri.ProgramKod,
                            ProgramAdi = s.BasvurularTercihleri.Programlar.ProgramAdi + " (" + s.BasvurularTercihleri.ProgramKod + ")",
                            OgrenciNo = s.Basvurular.Kullanicilar.OgrenciNo,
                            ResimAdi = System.Web.HttpContext.Current.Server.MapPath("/" + imgPath + "/" + s.Basvurular.ResimAdi),
                            AdSoyad = s.Basvurular.Ad + " " + s.Basvurular.Soyad
                        }).OrderBy(o => o.ProgramAdi).ThenBy(t => t.OgrenciNo).ToList();

                        var rpr = new rprKesinKayitResimListesi();
                        rpr.DataSource = kayitOlanlar;
                        if (basvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) rpr.DisplayName = "Lisansüstü Programlarına Kayıt Olan Öğrenci Listesi";
                        else if (basvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru) rpr.DisplayName = "Lisansüstü Yatay Geçiş Programlarına Kayıt Olan Öğrenci Listesi";
                        else rpr.DisplayName = "YTÜ Yeni mezun Lisansüstü Programlarına Kayıt Olan Öğrenci Listesi";
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFilePageByPage;

                        rprX = rpr;

                    }
                    #endregion
                }
                else if (RaporTipi == RaporTipleri.AnabilimdaliProgramListesi)
                {
                    #region ABD_ProgramList
                    var ogrenimTipKods = Request["OgrenimTipKods"].ToStrObj();
                    var eKod = Request["EKod"].ToStrObj();

                    var _OgrenimTipKods = new List<int>();
                    ogrenimTipKods.Split(',').ToList().ForEach((item) => { _OgrenimTipKods.Add(item.ToInt().Value); });
                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var enst = db.Enstitulers.Where(p => p.EnstituKod == eKod).First();



                        var q = from s in db.AnabilimDallaris.Where(p => p.EnstituKod == eKod)
                                join pr in db.Programlars on s.AnabilimDaliID equals pr.AnabilimDaliID
                                join bkot in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurec.EnstituKod == eKod) on pr.ProgramKod equals bkot.ProgramKod
                                join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == eKod) on bkot.OgrenimTipKod equals ot.OgrenimTipKod
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
                        RprBolumProgramListesi rpr = new RprBolumProgramListesi(enst.EnstituAd);
                        rpr.DataSource = qdata;
                        rpr.DisplayName = "Anabilim Dalı/Program Listesi";
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFilePageByPage;

                        rprX = rpr;

                    }

                    #endregion
                }
                else if (RaporTipi == RaporTipleri.BasvuruSonucSayisal)
                {
                    #region BasvuruSonucSayisal
                    var basvuruSurecId = Request["BasvuruSurecID"].ToIntObj();
                    if (RoleNames.LisansustuBasvuruRapor.InRoleCurrent() == false) basvuruSurecId = 0;
                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var qbs = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == basvuruSurecId && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod));
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
                                      ADToplamModel = new FmMsonucOranModel
                                      {
                                          Toplam = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanDisi),
                                          Kota = s.BasvuruSurecKotalars.Sum(sm => sm.AlanDisiKota),
                                          AsilCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanDisi && c.MulakatSonucTipID == MulakatSonucTipi.Asil),
                                          YedekCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanDisi && c.MulakatSonucTipID == MulakatSonucTipi.Yedek),
                                          KazanamayanCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanDisi && c.MulakatSonucTipID == MulakatSonucTipi.Kazanamadı),
                                          KayitCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipi.AlanDisi && c.KayitDurumID.HasValue && c.KayitDurumlari.IsKayitOldu),
                                      },
                                      AIToplamModel = new FmMsonucOranModel
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
                                                          select new FrMulakatSonucDetay
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
                            var toplmMdl = new List<FmMsonucOranModel>();
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
                        else rpr.DisplayName = "YTÜ Yeni Mezun DR Başvuru Süreci Sayısal Bilgi";

                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                        rprX = rpr;

                    }

                    #endregion
                }
                else if (RaporTipi == RaporTipleri.BelgeTalepSayisal)
                {
                    var baslangicT = Request["T1"].ToStrObj();
                    var bitisT = Request["T2"].ToStrObj();
                    var eKod = Request["eKod"].ToStrObj();
                    var yilModel = new List<int>();
                    var yilAyModel = new List<raporBTSayisalModel>();
                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var btipdetayIds = db.BelgeTipDetayBelgelers.Where(p => p.BelgeTipDetay.IsAktif).Select(s => s.BelgeTipID).Distinct();
                        if (RoleNames.BelgeTalepleriRapor.InRoleCurrent())
                        {

                            var bTips = db.BelgeTipleris.Where(p => btipdetayIds.Contains(p.BelgeTipID) && p.IsAktif).ToList();
                            var t1 = Convert.ToDateTime(baslangicT + "-01");
                            var t2 = Convert.ToDateTime(bitisT + "-01");
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
                                        SurecTarihi = baslangicT + " / " + bitisT,

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
                        RaporBt rpr = new RaporBt();
                        rpr.DataSource = data;
                        rpr.DisplayName = "Belge Talepleri Sayısal Raporu";
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                        rprX = rpr;


                    }
                }
                else if (RaporTipi == RaporTipleri.MezuniyetBasvuruRaporu)
                {
                    var basvId = Request["MezuniyetBasvurulariID"].ToIntObj();
                    RprMezuniyetYayinSartiOnayiFormu rpr = new RprMezuniyetYayinSartiOnayiFormu(basvId.Value);

                    rpr.PrintingSystem.ContinuousPageNumbering = true;
                    rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                    rprX = rpr;
                }
                else if (RaporTipi == RaporTipleri.Anketler)
                {
                    var anketId = Request["AnketID"].ToIntObj();
                    var basTar = Request["BasTar"].ToDate();
                    var bitTar = Request["BitTar"].ToDate();
                    if (RoleNames.AnketlerRapor.InRoleCurrent() == false) anketId = 0;
                    var t1 = DateTime.Now;
                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var anket = db.Ankets.Where(p => p.AnketID == anketId).First();
                        var enstitu = anket.Enstituler;

                        var anketSorularis = anket.AnketSorus.ToList();
                        var anketSoruSecenek = db.AnketSoruSeceneks.Where(p => p.AnketSoru.AnketID == anketId).ToList();
                        var cevaplar = db.AnketCevaplaris.Where(p => p.AnketID == anketId && p.Tarih >= basTar && p.Tarih <= bitTar).ToList();
                        var qModel = (from sa in anketSorularis
                                      select new FrAnketDetayDto
                                      {
                                          AnketSoruID = sa.AnketSoruID,
                                          AnketID = sa.AnketID,
                                          SoruAdi = sa.SoruAdi,
                                          SiraNo = sa.SiraNo,
                                          IsTabloVeriGirisi = sa.IsTabloVeriGirisi,
                                          FrAnketSecenekDetay = (from ss in anketSoruSecenek.Where(p => p.AnketSoruID == sa.AnketSoruID)
                                                                 select new FrAnketSecenekDetayDto
                                                                 {
                                                                     AnketSoruID = ss.AnketSoruID,
                                                                     AnketSoruSecenekID = ss.AnketSoruSecenekID,
                                                                     SiraNo = ss.SiraNo,
                                                                     SecenekAdi = ss.SecenekAdi,
                                                                     IsEkAciklamaGir = ss.IsEkAciklamaGir,
                                                                     Count = cevaplar.Where(p => p.AnketSoruSecenekID == ss.AnketSoruSecenekID).Count(),
                                                                     AnketCevaplaris = cevaplar.Where(p => p.AnketSoruSecenekID == ss.AnketSoruSecenekID).ToList()
                                                                 }
                                                               ).OrderBy(o => o.SiraNo).ToList(),
                                          AnketCevaplaris = cevaplar.Where(p => p.AnketSoruID == sa.AnketSoruID).ToList()
                                      }).OrderBy(o => o.SiraNo).ToList();



                        foreach (var item in qModel)
                        {
                            if (item.IsTabloVeriGirisi)
                            {

                                var tblRw = new AnketTableDetayDto();
                                tblRw.SiraNo = "#";
                                int i = 0;
                                foreach (var item2 in item.FrAnketSecenekDetay)
                                {
                                    i++;
                                    PropertyInfo propertyInfo = tblRw.GetType().GetProperty("TabloVeri" + i);
                                    propertyInfo.SetValue(tblRw, item2.SecenekAdi, null);
                                }
                                item.AnketTableDetays.Add(tblRw);
                                i = 0;
                                foreach (var item2 in item.AnketCevaplaris)
                                {
                                    i++;
                                    item.AnketTableDetays.Add(new AnketTableDetayDto
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
                                var tblRw = new AnketTableDetayDto();
                                foreach (var item2 in item.FrAnketSecenekDetay)
                                {
                                    var ekAciklamalar = "";

                                    var _ekAciklama = new Dictionary<int, string>();
                                    if (item2.IsEkAciklamaGir)
                                        foreach (var itemx in item2.AnketCevaplaris.Select((s, inx) => new { s = s, inx = inx }))
                                        {
                                            _ekAciklama.Add(itemx.inx + 1, itemx.s.EkAciklama);
                                        }
                                    item.AnketSeceneklerDetays.Add(new AnketSeceneklerDetayDto
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

                        SistemBilgilendirmeBus.SistemBilgisiKaydet("'" + anket.AnketAdi + "' Anket Raporu Oluşturuldu. Oluşturulma Süresi: " + TS + " Sn.", "Ajax/GetDxReport", LogType.Bilgi);
                        RprAnket rpr = new RprAnket(enstitu.EnstituAd, anket.AnketAdi, basTar.ToString("dd-MM-yyyy") + " - " + bitTar.ToString("dd-MM-yyyy") + " Tarih aralığındaki anket sonuçları");
                        rpr.DataSource = qModel;
                        rpr.DisplayName = basTar.ToString("dd-MM-yyyy") + " - " + bitTar.ToString("dd-MM-yyyy") + " Tarih aralığındaki " + anket.AnketAdi + " anket sonuçları";
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                        rprX = rpr;

                    }
                }
                else if (RaporTipi == RaporTipleri.MezuniyetCiltFormuRaporu)
                {
                    var formId = Request["ID"].ToIntObj();
                    var rpr = new RprMezuniyetCiltliTezTeslimFormu_FR1243(formId.Value);
                    rpr.PrintingSystem.ContinuousPageNumbering = true;
                    rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                    rprX = rpr;
                }
                else if (RaporTipi == RaporTipleri.MezuniyetJuriOneriFormuRaporu)
                {
                    var mezuniyetBasvurulariId = Request["ID"].ToIntObj().Value;
                    var rpr = new RprMezuniyetTezJuriOneriFormu_FR0300_FR0339(mezuniyetBasvurulariId);
                    rpr.PrintingSystem.ContinuousPageNumbering = true;
                    rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                    rprX = rpr;
                }
                else if (RaporTipi == RaporTipleri.MezuniyetTezTeslimFormu)
                {
                    var _UniqueID = Request["UniqueID"].ToString();
                    var _IlkTeslim = Request["IlkTeslim"].ToBooleanObj() ?? false;
                    var uniqueId = new Guid(_UniqueID);
                    var rpr = new RprMezuniyetTezTeslimFormu_FR0338(uniqueId, _IlkTeslim);

                    rprX = rpr;

                }
                else if (RaporTipi == RaporTipleri.MezuniyetTezSinavSonucFormu)
                {
                    var uniqueId = Request["UniqueID"].ToString();
                    var rpr = new RprTezSinavSonucTutanagi_FR0342_FR0377(new Guid(uniqueId));
                    rprX = rpr;

                }
                else if (RaporTipi == RaporTipleri.MezuniyetJuriUyelerineTezTeslimFormu)
                {
                    var _MezuniyetJuriOneriFormID = Request["MezuniyetJuriOneriFormID"].ToInt();
                    var rpr = new RprJuriUyelerineTezTeslimFormu_FR0341_FR0302(_MezuniyetJuriOneriFormID.Value);

                    rprX = rpr;

                }
                else if (RaporTipi == RaporTipleri.MezuniyetTezdenUretilenYayinlariDegerlendirmeFormu)
                {
                    var _MezuniyetJuriOneriFormID = Request["MezuniyetJuriOneriFormID"].ToInt();
                    var _MezuniyetJuriOneriFormuJuriID = Request["MezuniyetJuriOneriFormuJuriID"].ToInt();
                    var rpr = new RprMezuniyetTezdenUretilenYayinlariDegerlendirmeFormu_FR0304(_MezuniyetJuriOneriFormID.Value, _MezuniyetJuriOneriFormuJuriID);

                    rprX = rpr;

                }
                else if (RaporTipi == RaporTipleri.MezuniyetDoktoraTezDegerlendirmeFormu)
                {
                    var _MezuniyetJuriOneriFormID = Request["MezuniyetJuriOneriFormID"].ToInt();
                    var _MezuniyetJuriOneriFormuJuriID = Request["MezuniyetJuriOneriFormuJuriID"].ToInt();
                    var rpr = new RprMezuniyetTezDegerlendirmeFormu_FR0303(_MezuniyetJuriOneriFormID.Value, _MezuniyetJuriOneriFormuJuriID);

                    rprX = rpr;

                }
                else if (RaporTipi == RaporTipleri.MezuniyetTezKontrolFormu)
                {
                    var id = Request["ID"].ToString();
                    var uniqueId = new Guid(id);

                    var rpr = new RprMezuniyetTezKontrolFormu(uniqueId, null);
                    rpr.CreateDocument();
                    rprX = rpr;
                }
                else if (RaporTipi == RaporTipleri.TezIzlemeDegerlendirmeFormu)
                {
                    var id = Request["UniqueID"].ToString();
                    var uniqueId = new Guid(id);
                    var rapor = _entities.TIBasvuruAraRapors.Where(p => p.UniqueID == uniqueId).FirstOrDefault();

                    var rpr = new RprTiDegerlendirmeFormu_FR0307(rapor.TIBasvuruAraRaporID);
                    rpr.CreateDocument();
                    rpr.DisplayName = rapor.TIBasvuru.Ad + " " + rapor.TIBasvuru.Soyad + " " + rpr.DisplayName;


                    if (rapor.TIBasvuru.KullaniciID != UserIdentity.Current.Id || RoleNames.TiTezDegerlendirmeYap.InRoleCurrent() || RoleNames.TiTezDegerlendirmeDuzeltme.InRoleCurrent())
                    {
                        var rpr2 = new RprTiDegerlendirmeFormuDetay_FR0307(rapor.TIBasvuruAraRaporID);
                        rpr2.CreateDocument();
                        rpr2.DisplayName = rpr2.DisplayName;
                        rpr.Pages.AddRange(rpr2.Pages);
                    }
                    rprX = rpr;
                }
                else if (RaporTipi == RaporTipleri.TezDanismanOneriFormu)
                {
                    var id = Request["UniqueID"].ToString();
                    var uniqueId = new Guid(id);
                    var rapor = _entities.TDOBasvuruDanismen.First(p => p.UniqueID == uniqueId);

                    var rpr = new RprTezDanismaniOneriFormu_FR0347(rapor.TDOBasvuruDanismanID);
                    rpr.CreateDocument();
                    rpr.DisplayName = rapor.TDOBasvuru.Ad + " " + rapor.TDOBasvuru.Soyad + " " + rpr.DisplayName;

                    rprX = rpr;
                }
                else if (RaporTipi == RaporTipleri.TezDanismanDegisiklikFormu)
                {
                    var id = Request["UniqueID"].ToString();
                    var uniqueId = new Guid(id);
                    var rapor = _entities.TDOBasvuruDanismen.First(p => p.UniqueID == uniqueId);

                    var rpr = new RprTezDanismaniDegisiklikFormu_FR0308(rapor.TDOBasvuruDanismanID);
                    rpr.CreateDocument();
                    rpr.DisplayName = rapor.TDOBasvuru.Ad + " " + rapor.TDOBasvuru.Soyad + " " + rpr.DisplayName;
                    rprX = rpr;
                }
                else if (RaporTipi == RaporTipleri.TezEsDanismanOneriFormu)
                {
                    var id = Request["UniqueID"].ToString();
                    var uniqueId = new Guid(id);
                    var rapor = _entities.TDOBasvuruEsDanismen.First(p => p.UniqueID == uniqueId);
                    var rpr = new RprTezEsDanismaniOneriFormu_FR0320(rapor.TDOBasvuruEsDanismanID);
                    rpr.CreateDocument();
                    rpr.DisplayName = rapor.TDOBasvuruDanisman.TDOBasvuru.Ad + " " + rapor.TDOBasvuruDanisman.TDOBasvuru.Soyad + " " + rpr.DisplayName;
                    rprX = rpr;
                }
            }
            if (isPdfStream)
            {
                var ms = new MemoryStream();
                rprX.ExportToPdf(ms);
                rprX.ExportOptions.Pdf.Compressed = true;
                ms.Seek(0, System.IO.SeekOrigin.Begin);


                Response.AddHeader("Content-Disposition", "inline;filename=\"" + rprX.DisplayName + ".pdf\"");
                return new FileStreamResult(ms, "application/pdf");


            }
            else return View(rprX);
        }


        public ActionResult GetChkList()
        {
            return View();
        }
        protected override void Dispose(bool disposing)
        {
            _entities.Dispose();
            base.Dispose(disposing);
        }
    }
}
