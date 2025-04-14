

using BiskaUtil;
using CaptchaMvc.HtmlHelpers;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Raporlar;
using LisansUstuBasvuruSistemi.Raporlar.BelgeTalep;
using LisansUstuBasvuruSistemi.Raporlar.Genel;
using LisansUstuBasvuruSistemi.Raporlar.Mezuniyet;
using LisansUstuBasvuruSistemi.Raporlar.TezDanismanOneri;
using LisansUstuBasvuruSistemi.Raporlar.TezIzleme;
using LisansUstuBasvuruSistemi.Raporlar.TezIzlemeJuriOneri;
using LisansUstuBasvuruSistemi.Raporlar.TezOneriSavunma;
using LisansUstuBasvuruSistemi.Raporlar.Yeterlik;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemData;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Raporlar.DonemProjesi;
using LisansUstuBasvuruSistemi.Raporlar.LUB;
using LisansUstuBasvuruSistemi.WebServiceData.ObsService;
using LisansUstuBasvuruSistemi.WebServiceData.PersisService;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.WebServiceData.ObsRestData;

namespace LisansUstuBasvuruSistemi.Controllers
{

    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class AjaxController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();


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
        [Authorize]
        public ActionResult SetThemeSetting(string columnName, string value)
        {


            var kullanici = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
            if (columnName == "st_head_fixed") kullanici.FixedHeader = value.ToBoolean(false);
            if (columnName == "st_sb_fixed") kullanici.FixedSidebar = value.ToBoolean(false);
            if (columnName == "st_sb_scroll") kullanici.ScrollSidebar = value.ToBoolean(false);
            if (columnName == "st_sb_right") kullanici.RightSidebar = value.ToBoolean(false);
            if (columnName == "st_sb_custom") kullanici.CustomNavigation = value.ToBoolean(false);
            if (columnName == "st_sb_toggled") kullanici.ToggledNavigation = value.ToBoolean(false);
            if (columnName == "st_layout_boxed") kullanici.BoxedOrFullWidth = value.ToBoolean(false);
            if (columnName == "ThemeName") kullanici.ThemeName = value;
            if (columnName == "BackgroundImage") kullanici.BackgroundImage = value;
            _entities.SaveChanges();
            if (columnName == "st_head_fixed") UserIdentity.Current.Informations["FixedHeader"] = value.ToBoolean(false);
            if (columnName == "st_sb_fixed") UserIdentity.Current.Informations["FixedSidebar"] = value.ToBoolean(false);
            if (columnName == "st_sb_scroll") UserIdentity.Current.Informations["ScrollSidebar"] = value.ToBoolean(false);
            if (columnName == "st_sb_right") UserIdentity.Current.Informations["RightSidebar"] = value.ToBoolean(false);
            if (columnName == "st_sb_custom") UserIdentity.Current.Informations["CustomNavigation"] = value.ToBoolean(false);
            if (columnName == "st_sb_toggled") UserIdentity.Current.Informations["ToggledNavigation"] = value.ToBoolean(false);
            if (columnName == "st_layout_boxed") UserIdentity.Current.Informations["BoxedOrFullWidth"] = value.ToBoolean(false);
            if (columnName == "ThemeName") UserIdentity.Current.Informations["ThemeName"] = value;
            if (columnName == "BackgroundImage") UserIdentity.Current.Informations["BackgroundImage"] = value;
            return Json("true", "application/json", JsonRequestBehavior.AllowGet);
        }


        public ActionResult LoginControl(string userName, string password, string captchaInputText, bool? rememberMe, string returnUrl, string ekd)
        {
            var loginAjaxDto = new LoginAjaxDto
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
                    var msg = "";
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
                                loginAjaxDto.IsSuccess = false;
                                msg = "Uygulama şifresiyle Enstitü Bilgi Sistemine giriş yapılamadı! <a href='https://teknikdestek.yildiz.edu.tr/kb/faq.php?id=32' target='_blank' style='color:white;'>Detaylı bilgi almak için tıklayınız. https://teknikdestek.yildiz.edu.tr/kb/faq.php?id=32</a>";
                            }
                        }
                        if (loginUser != null && !loginUser.IsAktif)
                        {
                            hata = "Kullanıcı Hesabı Pasif Durumda!";
                            loginAjaxDto.IsSuccess = false;
                        }
                        else if (loginUser == null)
                        {
                            hata = "Kullanıcı Adı veya Şifre Hatalı. " + msg;
                            loginAjaxDto.IsSuccess = false;
                        }
                        else
                        {
                            loginAjaxDto.IsSuccess = true;
                        }
                    }
                    else
                    {
                        loginAjaxDto.IsSuccess = false;
                        hata = "Kullanıcı sistemde bulunamadı.";
                    }
                }

            }
            catch (Exception ex)
            {
                loginAjaxDto.IsSuccess = false;
                hata = "Sisteme Giriş Yapılırken Bir Hata Oluştu! Hata: " + ex.ToExceptionMessage();
            }

            loginAjaxDto.Message = hata;
            if (loginAjaxDto.IsSuccess == false)
            {
                var newCaptcha = ViewRenderHelper.RenderPartialView("Ajax", "GetCaptcha", new UrlInfoModel());
                loginAjaxDto.NewSrc = newCaptcha;

            }
            else if (loginUser != null)
            {
                var userEnstitu = EnstituBus.GetEnstitu(loginUser.EnstituKod);
                var currentEnstitu = EnstituBus.GetEnstitu(EnstituBus.GetSelectedEnstitu(ekd));

                if (userEnstitu.EnstituKod != currentEnstitu.EnstituKod && !returnUrl.IsNullOrWhiteSpace() && !returnUrl.Contains("?"))
                {
                    loginAjaxDto.CurrentEnstituAdi = currentEnstitu.EnstituAd;
                    loginAjaxDto.KayitliEnstituAdi = userEnstitu.EnstituAd;
                    loginAjaxDto.ReturnUrlChanged = returnUrl.ToLower().Replace("/" + currentEnstitu.EnstituKisaAd.ToLower() + "/",
                        "/" + userEnstitu.EnstituKisaAd.ToLower() + "/");
                }


                if (loginUser.YtuOgrencisi && loginUser.OgrenimDurumID == OgrenimDurumEnum.HalenOğrenci)
                {
                    var tdoBasvuruId = _entities.TDOBasvurus
                        .Where(p => p.KullaniciID == loginUser.KullaniciID)
                        .OrderByDescending(o => o.TDOBasvuruID).Select(s => s.TDOBasvuruID)
                        .FirstOrDefault();
                    if (tdoBasvuruId > 0)
                    {
                        TdoBus.ObsDanismanBasvuruBilgiEslestir(loginUser.KullaniciID, tdoBasvuruId);
                    }
                }
                FormsAuthenticationUtil.SetAuthCookie(loginUser.KullaniciAdi, string.Empty, rememberMe ?? false);
                UserBus.SetLastLogon();
            }

            return loginAjaxDto.ToJsonResult();
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
            var yeniResim = "";
            mMessage.Title = "Profil resmi yükleme işlemi başarısız";
            mMessage.IsSuccess = false;
            mMessage.MessageType = MsgTypeEnum.Warning;
            var anaResmiDegistir = false;
            if (kProfilResmi == null || kProfilResmi.ContentLength <= 0)
            {
                mMessage.Messages.Add("Profil Resmi Seçiniz");
            }
            else if (RoleNames.KullanicilarKayit.InRoleCurrent() == false && kullaniciId != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Başka bir kullanıcı adına resim yüklemesi yapmaya yetkili değilsiniz.");
            }
            else
            {
                var contentlength = kProfilResmi.ContentLength;
                var uzanti = kProfilResmi.FileName.GetFileExtension();
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
                    mMessage.MessageType = MsgTypeEnum.Success;
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
                        FileHelper.Delete("/" + rsmYol + "/" + eskiResim);

                    }
                }
            }
            return new { mMessage, ResimAdi = yeniResim.ToKullaniciResim(), AnaResmiDegistir = anaResmiDegistir }.ToJsonResult();
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
        public ActionResult GetKullaniciDetay(Guid userKey)
        {
            KullanicilarBus.OgrenciBilgisiGuncelleObs(userKey);

            var data = _entities.Kullanicilars.First(p => p.UserKey == userKey);

            ViewBag.ResimVar = data.ResimAdi.IsNullOrWhiteSpace() == false;
            data.ResimAdi = data.ResimAdi.ToKullaniciResim();


            #region Enstituler
            var enstroles = EnstituBus.GetEnstituler(true);
            var userEnstRoles = UserBus.GetKullaniciEnstituler(data.KullaniciID);
            var kullanici = UserBus.GetUser(data.KullaniciID);
            var dataEnst = enstroles.Select(s => new CheckObject<Enstituler>
            {
                Value = s,
                Checked = userEnstRoles.Any(p => p.EnstituKod == s.EnstituKod)
            }).ToList();
            ViewBag.KEnstituler = dataEnst;
            #endregion
            #region yetkiler
            var roles = RollerBus.GetAllRoles().ToList();
            var userRoles = UserBus.GetUserRoles(data.KullaniciID);
            ViewBag.EkRollerCount = userRoles.EklenenRoller.Count;
            ViewBag.Kullanici = kullanici;
            var dataR = roles.Select(s => new CheckObject<Roller>
            {
                Value = s,
                Checked = userRoles.TumRoller.Any(a => a.RolID == s.RolID)
            }).ToList();
            ViewBag.KRoller = dataR;
            var kategr = roles.Select(s => s.Kategori).Distinct().ToArray();
            var menuK = MenulerBus.Menulers.Where(a => a.BagliMenuID == 0 && kategr.Contains(a.MenuAdi)).ToList();
            var dct = menuK.Select(item => new CmbIntDto { Value = item.SiraNo, Caption = item.MenuAdi }).ToList();
            ViewBag.cats = dct;
            #endregion

            #region programYetkileri
            var dataKp = KullanicilarBus.GetKullaniciProgramlari(data.KullaniciID, null);

            ViewBag.KProgramlar = dataKp.Where(p => p.YetkiVar).ToList();
            #endregion

            if (data.KayitDonemID.HasValue)
            {
                ViewBag.Donem = _entities.Donemlers.FirstOrDefault(p => p.DonemID == data.KayitDonemID.Value);

            }
            if (data.KullaniciTipID == KullaniciTipiEnum.AkademikPersonel)
            {
                var ogrenciler = _entities.Kullanicilars.Where(p => p.DanismanID == data.KullaniciID && p.YtuOgrencisi).OrderBy(o => o.Ad).ThenBy(t => t.Soyad).Select(s => new
                {
                    s.UserKey,
                    s.ResimAdi,
                    s.OgrenciNo,
                    s.Ad,
                    s.Soyad,
                    s.Programlar
                }).ToList().Select(s => new Kullanicilar
                {
                    UserKey = s.UserKey,
                    ResimAdi = s.ResimAdi,
                    Ad = s.Ad,
                    Soyad = s.Soyad,
                    OgrenciNo = s.OgrenciNo,
                    Programlar = s.Programlar

                }).ToList();
                ViewBag.Ogrenciler = ogrenciler;
            }

            var donemKey = "";
            if (data.KayitYilBaslangic.HasValue)
            {
                donemKey = data.KayitYilBaslangic + "/" + (data.KayitYilBaslangic + 1) + "/" + data.KayitDonemID;
            }
            ViewBag.KayitDonem = new SelectList(DonemlerBus.GetCmbAkademikDonemler(data.KayitYilBaslangic), "Value", "Caption", donemKey);
            ViewBag.Enstitu = _entities.Enstitulers.First(p => p.EnstituKod == data.EnstituKod);
            if (kullanici.YtuOgrencisi)
                ViewBag.OgrenimEnstitu = _entities.Enstitulers.First(p => p.EnstituKod == data.OgrenimEnstituKod);
            ViewBag.YtuOgrenimB = _entities.OgrenimTipleris.FirstOrDefault(p => p.EnstituKod == kullanici.EnstituKod && p.OgrenimTipKod == kullanici.OgrenimTipKod);
            if (data.DanismanID.HasValue)
                ViewBag.Danisman = _entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == data.DanismanID);
            return View(data);
        }
        [Authorize(Roles = RoleNames.KullanicilarKayit)]
        public ActionResult KayitDonemAyarSet(Guid userKey, string kayitDonem, bool? obsGuncelleme)
        {
            var mmMessage = new MmMessage
            {
                Title = "Kayıt Dönem Güncelleme İşlemi"
            };
            var kullanici = _entities.Kullanicilars.First(f => f.UserKey == userKey);

            kullanici.KayitYilBaslangic = kayitDonem.Split('/')[0].ToInt();
            kullanici.KayitDonemID = kayitDonem.Split('/').Last().ToInt();
            kullanici.ObsKayitDonemOtoGuncellemeKapali = obsGuncelleme;
            _entities.SaveChanges();
            mmMessage.Messages.Add("Kullanıcı kayıt dönemi bilgisi güncellendi.");
            mmMessage.IsSuccess = true;
            LogIslemleri.LogEkle("Kullanicilar", LogCrudType.Update, kullanici.ToJson());

            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mmMessage.ToJsonResult();
        }

        public ActionResult GetCaptcha()
        {
            return View();
        }


        [HttpGet]
        [Authorize]
        public ActionResult GetBasvuruDetay(int id, int tbInx)
        {
            var kullaniciId = (RoleNames.GelenBasvurular.InRoleCurrent()) ? (int?)null : UserIdentity.Current.Id;
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
            return Json(new { page, UserIdentity.Current.IsAuthenticated }, "application/json", JsonRequestBehavior.AllowGet);
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
            var kullaniciId = RoleNames.GelenBasvurular.InRoleCurrent() ? (int?)null : UserIdentity.Current.Id;
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
            mdl.KullaniciTipID = basvuru.KullaniciTipID;
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

            mdl.IsHesaplandi = basvuru.MulakatSonuclaris.Any(a => a.MulakatSonucTipID != MulakatSonucTipiEnum.Hesaplanmadı);

            mdl.IsBelgeYuklemeVar = basvuru.BasvuruSurec.IsBelgeYuklemeVar;
            var mulakatSonucTipIDs = new List<int> { MulakatSonucTipiEnum.Asil, MulakatSonucTipiEnum.Yedek };
            var mulakatSonuclaris = basvuru.BasvuruSurec.Basvurulars.Where(p => p.KullaniciID == basvuru.KullaniciID).SelectMany(s => s.MulakatSonuclaris).Where(p => p.MulakatSonucTipID == MulakatSonucTipiEnum.Yedek).ToList();
            if (mulakatSonuclaris.Count <= 1)
            {
                mulakatSonuclaris = basvuru.MulakatSonuclaris.Where(p => mulakatSonucTipIDs.Contains(p.MulakatSonucTipID)).ToList();
            }
            else mdl.IsYedekCokluTercih = true;
            mdl.IsGonderilenMaillerVar = basvuru.GonderilenMaillers.Any();
            mdl.IsKayitHakkiVar = mulakatSonuclaris.Any();
            mdl.KayitIslemiGordu = mulakatSonuclaris.Any(a => a.KayitDurumID.HasValue && a.KayitDurumlari.IsKayitOldu);

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
                                  join abd in _entities.AnabilimDallaris on pr.AnabilimDaliKod equals abd.AnabilimDaliKod
                                  select new BasvuruTercihDto
                                  {
                                      BasvuruTercihID = s.BasvuruTercihID,
                                      BasvuruID = s.BasvuruID,
                                      UniqueID = s.UniqueID,
                                      SiraNo = s.SiraNo,
                                      AlanTipID = at.AlanTipID,
                                      AlanTipAdi = at.AlanTipAdi,
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
                mdl.IsTurkceProgramVar = true;
                mdl.Sinavlars = (from s in sinavlars
                                 join bs in bSurecSinavBilgis on s.SinavTipID equals bs.SinavTipID
                                 join st in sinavTipleris on s.SinavTipID equals st.SinavTipID
                                 join stl in sinavTipleriLngs on st.SinavTipID equals stl.SinavTipID
                                 join stg in sinavTipGroups on s.SinavTipGrupID equals stg.SinavTipGrupID
                                 select new BasvuruSinavTipDto
                                 {
                                     EnstituKod = basvuruSurec.EnstituKod,
                                     IsWebService = bs.WebService,
                                     SinavTipKod = st.SinavTipKod,
                                     SinavTipID = s.SinavTipID,
                                     SinavTipGrupID = s.SinavTipGrupID,
                                     GrupAdi = stg.SinavTipGrupAdi,
                                     IsTaahhutVar = s.IsTaahhutVar ?? false,
                                     SinavAdi = stl.SinavAdi,
                                     Yil = s.WsSinavYil,
                                     DonemAdi = "",// bs.WebService ? SinavTipleriDonems.Where(p => p.WsDonemKod == s.WsSinavDonem).FirstOrDefault().WsDonemAd : "",
                                     SinavTarihi = bs.WebService ? s.WsAciklanmaTarihi : s.SinavTarihi,
                                     SinavSubPuani = s.BasvuruSurecSubNot,
                                     SinavPuani = s.SinavNotu,
                                     SinavDilID = s.SinavDilID,
                                     SinavDilAdi = s.SinavDilID.HasValue ? sinavDilleris.First(p => p.SinavDilID == s.SinavDilID).DilAdi : (bs.WebService && s.SinavTipGrupID == SinavTipGrupEnum.DilSinavlari ? s.WsSinavDili : ""),
                                     //AlesXmlModel = s.SinavTipGrupID == SinavTipGrup.Ales_Gree && bs.WebService ? s.WsXmlData.toSinavSonucAlesXmlModel() : null,
                                 }).OrderBy(o => o.SinavTipGrupID).ToList();

                #endregion
                page = ViewRenderHelper.RenderPartialView("Ajax", "GetBasvuruSinavBilgileri", mdl);
            }
            else if (tbInx == 4)
            {
                #region Belgeler
                if (mdl.IsBelgeYuklemeVar)
                {

                    var rolAdlari = new List<string> { RoleNames.GelenBasvurular };


                    if (mdl.IsHesaplandi)
                    {
                        var tumBilgileriGorsun = UserIdentity.Current.Roles.Any(a => rolAdlari.Contains(a));
                        var basvuruSurec = basvuru.BasvuruSurec;

                        if (mdl.IsKayitHakkiVar)
                        {
                            var belgeKtModel = new List<EntBegeKayitT>();
                            foreach (var item in basvuruSurec.BasvuruSurecOgrenimTipleris.Where(p => p.BelgeYuklemeAsilBasTar.HasValue))
                            {
                                belgeKtModel.Add(new EntBegeKayitT
                                {
                                    EnstituKod = basvuruSurec.EnstituKod,
                                    OgrenimTipKod = item.OgrenimTipKod,
                                    BaslangicTar = item.BelgeYuklemeAsilBasTar ?? DateTime.Now,
                                    BitisTar = item.BelgeYuklemeAsilBitTar ?? DateTime.Now,
                                    MulakatSonucTipID = MulakatSonucTipiEnum.Asil
                                });
                                belgeKtModel.Add(new EntBegeKayitT
                                {
                                    EnstituKod = basvuruSurec.EnstituKod,
                                    OgrenimTipKod = item.OgrenimTipKod,
                                    BaslangicTar = item.BelgeYuklemeYedekBasTar ?? DateTime.Now,
                                    BitisTar = item.BelgeYuklemeYedekBitTar ?? DateTime.Now,
                                    MulakatSonucTipID = MulakatSonucTipiEnum.Yedek
                                });
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
                            mdl.IsSecilenTercihVarAsil = mdl.Tercihlers.Any(a => a.MulakaSonucTipID == MulakatSonucTipiEnum.Asil && a.IsSecilenTercih == true);
                            mdl.IsSecilenTercihVarYedek = mdl.Tercihlers.Any(a => a.MulakaSonucTipID == MulakatSonucTipiEnum.Yedek && a.IsSecilenTercih == true);

                            var basvuruSurecBelgeTipleris = basvuruSurec.BasvuruSurecBelgeTipleris.ToList();

                            foreach (var item in mdl.Tercihlers.Select(s => s.BasvuruID).Distinct())
                            {
                                #region BelgelerSet
                                var basvuruB = basvuruSurec.Basvurulars.First(p => p.BasvuruID == item);
                                var basvurularYuklenenBelgelers = basvuruB.BasvurularYuklenenBelgelers.Select(s => new { s.BasvurularYuklenenBelgeID, s.BasvuruBelgeTipID, s.SinavTipID, s.BelgeAdi, s.BelgeYolu, s.IsOnaylandi, s.OnaylamaTarihi, s.IslemTarihi }).ToList();
                                var bsKimlikBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.KimlikBelgesi);
                                if (bsKimlikBelgesi != null)
                                {
                                    var kb = new BasvuruBelgeDto
                                    {
                                        SiraNo = 1,
                                        BasvuruID = basvuruB.BasvuruID,
                                        BasvuruBelgeTipID = BasvuruBelgeTipiEnum.KimlikBelgesi,
                                        BasvuruBelgeTipAdi = bsKimlikBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                    };
                                    var kimlikBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.KimlikBelgesi);
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
                                var bsLEgitimBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.LEgitimBelgesi);
                                if (bsLEgitimBelgesi != null)
                                {
                                    var kb = new BasvuruBelgeDto
                                    {
                                        SiraNo = 2,
                                        BasvuruID = basvuruB.BasvuruID,
                                        BasvuruBelgeTipID = BasvuruBelgeTipiEnum.LEgitimBelgesi,
                                        BasvuruBelgeTipAdi = bsLEgitimBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                    };
                                    var lEgitimBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.LEgitimBelgesi);
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
                                    var bsYlEgitimBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.YLEgitimBelgesi);
                                    if (bsYlEgitimBelgesi != null)
                                    {
                                        var kb = new BasvuruBelgeDto
                                        {

                                            SiraNo = 8,
                                            BasvuruID = basvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipiEnum.YLEgitimBelgesi,
                                            BasvuruBelgeTipAdi = bsYlEgitimBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };
                                        var ylEgitimBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.YLEgitimBelgesi);
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
                                var bsMezuniyetBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.MezuniyetBelgesi);
                                if (bsMezuniyetBelgesi != null)
                                {
                                    var kb = new BasvuruBelgeDto
                                    {
                                        SiraNo = !isDrBavurusuVar ? 3 : 9,
                                        BasvuruID = basvuruB.BasvuruID,
                                        BasvuruBelgeTipID = BasvuruBelgeTipiEnum.MezuniyetBelgesi,
                                        BasvuruBelgeTipAdi = bsMezuniyetBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                    };
                                    var mezuniyetBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.MezuniyetBelgesi);
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
                                    var bsMezuniyetBelgesiYl = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.YLMezuniyetBelgesi);
                                    if (bsMezuniyetBelgesiYl != null)
                                    {
                                        var kb = new BasvuruBelgeDto
                                        {
                                            SiraNo = !isDrBavurusuVar ? 4 : 10,
                                            BasvuruID = basvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipiEnum.YLMezuniyetBelgesi,
                                            BasvuruBelgeTipAdi = bsMezuniyetBelgesiYl.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };
                                        var mezuniyetBelgesiYl = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.YLMezuniyetBelgesi);
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

                                var bsTranskriptBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.TranskriptBelgesi);
                                if (bsTranskriptBelgesi != null)
                                {

                                    var belgeTipAdi = bsTranskriptBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi;
                                    if (basvuruSurec.BasvuruSurecTipID == BasvuruSurecTipiEnum.YatayGecisBasvuru)
                                    {
                                        belgeTipAdi = isDrBavurusuVar ? "YL ve DR Transkript Belgesi" : "Lisans ve YL Transkript Belgesi";
                                    }
                                    var kb = new BasvuruBelgeDto
                                    {
                                        SiraNo = !isDrBavurusuVar ? 5 : 11,
                                        BasvuruID = basvuruB.BasvuruID,
                                        BasvuruBelgeTipID = BasvuruBelgeTipiEnum.TranskriptBelgesi,
                                        BasvuruBelgeTipAdi = belgeTipAdi,
                                    };
                                    var transkriptBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.TranskriptBelgesi);
                                    if (transkriptBelgesi != null)
                                    {
                                        kb.BasvurularYuklenenBelgeID = transkriptBelgesi.BasvurularYuklenenBelgeID;
                                        kb.BelgeAdi = transkriptBelgesi.BelgeAdi;
                                        kb.BelgeYolu = transkriptBelgesi.BelgeYolu;
                                        kb.IsOnaylandi = transkriptBelgesi.IsOnaylandi;
                                        kb.OnaylamaTarihi = transkriptBelgesi.OnaylamaTarihi;
                                        kb.IslemTarihi = transkriptBelgesi.IslemTarihi;
                                    }
                                    if (basvuruSurec.BasvuruSurecTipID == BasvuruSurecTipiEnum.YatayGecisBasvuru)
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
                                    var bsTaninirlikBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.TaninirlikBelgesi);
                                    if (bsTaninirlikBelgesi != null)
                                    {
                                        var kb = new BasvuruBelgeDto
                                        {
                                            SiraNo = !isDrBavurusuVar ? 6 : 12,
                                            BasvuruID = basvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipiEnum.TaninirlikBelgesi,
                                            BasvuruBelgeTipAdi = bsTaninirlikBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };
                                        var taninirlikBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.TaninirlikBelgesi);
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
                                    var bsDenklikBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.DenklikBelgesi);
                                    if (bsDenklikBelgesi != null)
                                    {
                                        var kb = new BasvuruBelgeDto
                                        {
                                            SiraNo = !isDrBavurusuVar ? 6 : 12,
                                            BasvuruID = basvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipiEnum.DenklikBelgesi,
                                            BasvuruBelgeTipAdi = bsDenklikBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };
                                        var denklikBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.DenklikBelgesi);
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

                                if (sinavTipleris.Any(a => a.SinavTipGrupID == SinavTipGrupEnum.Ales_Gree))
                                {


                                    var bsAlesGreSinavBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.AlesGreSinaviBelgesi);
                                    if (bsAlesGreSinavBelgesi != null)
                                    {
                                        var alesGreSinaviBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.AlesGreSinaviBelgesi);
                                        var kb = new BasvuruBelgeDto
                                        {
                                            SiraNo = 13,
                                            BasvuruID = basvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipiEnum.AlesGreSinaviBelgesi,
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
                                if (sinavTipleris.Any(a => a.SinavTipGrupID == SinavTipGrupEnum.DilSinavlari))
                                {
                                    var bsDilSinaviBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.DilSinaviBelgesi);
                                    if (bsDilSinaviBelgesi != null)
                                    {
                                        var kb = new BasvuruBelgeDto
                                        {
                                            SiraNo = 14,
                                            BasvuruID = basvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipiEnum.DilSinaviBelgesi,
                                            BasvuruBelgeTipAdi = bsDilSinaviBelgesi.BasvuruBelgeTipleri.BasvuruBelgeTipAdi,
                                        };

                                        var dilSinaviBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.DilSinaviBelgesi);
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
                                if (sinavTipleris.Any(a => a.SinavTipGrupID == SinavTipGrupEnum.Tomer))
                                {
                                    var tomerBilgi = sinavTipleris.FirstOrDefault(a => a.SinavTipGrupID == SinavTipGrupEnum.Tomer);
                                    var bsTomerSinaviBelgesi = basvuruSurecBelgeTipleris.FirstOrDefault(a => a.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.TomerSinaviBelgesi);


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
                                        var tomerSinaviBelgesi = basvurularYuklenenBelgelers.FirstOrDefault(p => p.BasvuruBelgeTipID == BasvuruBelgeTipiEnum.TomerSinaviBelgesi);
                                        var kb = new BasvuruBelgeDto
                                        {
                                            SiraNo = 15,
                                            BasvuruID = basvuruB.BasvuruID,
                                            BasvuruBelgeTipID = BasvuruBelgeTipiEnum.TomerSinaviBelgesi,
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
        [HttpGet]
        public ActionResult GetDetailMezuniyet(int id, int? showDetayYayinId, int tbInx, bool isDelete, bool gelenBasvuru = false)
        {
            var model = MezuniyetBus.GetMezuniyetBasvuruDetayBilgi(id, null, showDetayYayinId);
            model.GelenBasvuru = gelenBasvuru;
            model.SelectedTabIndex = tbInx;


            var srSonTalebi = model.MezuniyetSrModel.SalonRezervasyonlari.OrderByDescending(o => o.SRTalepID).FirstOrDefault();
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
                MezuniyetBasvurulariTezDosyasi = model.MezuniyetBasvurulariTezDosyalariDtos.OrderByDescending(o => o.MezuniyetBasvurulariTezDosyaID).FirstOrDefault(),
                TeslimFormDurumu = srSonTalebi != null && model.MezuniyetBasvurulariTezTeslimFormlaris.Any(),
                IsMezunOldu = model.IsMezunOldu,
                MezuniyetTarihi = model.MezuniyetTarihi,

            };

            model.BasvuruDurumHtml = modelBasvuruDurum.ToRenderPartialViewHtml("Mezuniyet", "BasvuruDurumView");

            DateTime? onayTarihi = null;
            if (modelBasvuruDurum.MezuniyetJuriOneriFormu != null) onayTarihi = modelBasvuruDurum.MezuniyetJuriOneriFormu.EYKYaGonderildi == true ? modelBasvuruDurum.MezuniyetJuriOneriFormu.EYKYaGonderildiIslemTarihi : null;


            model.IsDelete = isDelete;
            model.SMezuniyetYayinKontrolDurum = new SelectList(MezuniyetBus.GetCmbMezuniyetYayinDurum(false, true), "Value", "Caption", model.MezuniyetYayinKontrolDurumID);
            model.SeykYaGonderildi = new SelectList(ComboData.GetCmbEykGonderimDurumData(true, onayTarihi), "Value", "Caption", model.EykYaGonderildi);
            model.SeykDaOnaylandi = new SelectList(ComboData.GetCmbEykOnayDurumData(true), "Value", "Caption", model.EykDaOnaylandi);

            model.SIsAsilOryedek = new SelectList(ComboData.GetCmbAsilYedekDurumData(true), "Value", "Caption");


            return View(model);
        }
        [Authorize]
        [HttpGet]
        public ActionResult GetDetailTijBasvuru(Guid basvuruUniqueId)
        {
            var model = TijBus.GetSecilenBasvuruTijDetay(basvuruUniqueId);
            return View(model);
        }
        [HttpGet]
        public ActionResult GetDetailTiBasvuru(int id, Guid? uniqueId)
        {
            var model = TiBus.GetSecilenBasvuruTiDetay(id, uniqueId);
            return View(model);
        }
        [HttpGet]
        public ActionResult GetDetailTosBasvuru(Guid toUniqueId, Guid? tosKomiteUniqueId)
        {
            var model = TosBus.GetSecilenBasvuruDetay(toUniqueId, tosKomiteUniqueId);
            return View(model);
        }
        [Authorize]
        [HttpGet]
        public ActionResult GetDetailTdoBasvuru(int id, Guid? uniqueId)
        {
            var model = TdoBus.GetSecilenBasvuruTdoDetay(id, uniqueId);
            ViewBag.ProgramKod = new SelectList(ProgramlarBus.CmbGetAktifProgramlar(model.EnstituKod, true, true), "Value", "Caption", model.ProgramKod);
            return View(model);
        }
        [HttpGet]
        public ActionResult GetDetailDpBasvuru(Guid donemProjesiUniqueId, Guid? showBasvuruUniqueId, Guid? uniqueId)
        {
            var model = DonemProjesiBus.GetSecilenBasvuruDetay(donemProjesiUniqueId, uniqueId);
            if (!model.DonemProjesiBasvurus.Any() || model.IsYeniBasvuruYapilabilir) model.ShowBasvuruUniqueId = showBasvuruUniqueId;
            return View(model);
        }

        public ActionResult SifreResetle(string mailAddress)
        {
            var mmMessage = new MmMessage();

            if (mailAddress.IsNullOrWhiteSpace() || !mailAddress.ToIsValidEmail())
            {
                mmMessage.IsSuccess = false;
                mmMessage.Title = "Lütfen doğru bir mail formatı giriniz.";
            }
            else
            {
                var kul = _entities.Kullanicilars.FirstOrDefault(p => p.EMail.Equals(mailAddress) && p.IsAktif);
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
                        mmMessage.Title = "Parola sıfırlama işlemi yapılamadı. Parola değişikliği işlemini yapabilmek linke tıklayıp bilgi alabilirsiniz. <a style='color:white;' href='https://teknikdestek.yildiz.edu.tr/kb/faq.php?id=32' target='_blank'>https://teknikdestek.yildiz.edu.tr/kb/faq.php?id=32</a>";

                        // "Girmiş olduğunuz mail adresi 'Active directory' sistemine entegre çalıştığı için ve bilgi işlem tarafından belirlenen ve bazı sistemlere  (YTÜ Mail, EBYS, Lojman Yönetim Sistem, Lisansustu Başvuru Sistemi vb)  erişimini sağlayan ortak bir şifresi bulunmaktadır. Bu mail adresi için tanımlanmış şifre sadece bilgi işlem tarafından belirlenip değiştirilebilmektedir. '" + kul.KullaniciAdi + "' kullanıcı adı ile YTÜ Mail, EBYS, Lojman Yönetim Sistem, Lisansustu Başvuru Sistemi vb. programlara giriş yaptığınız şifrenizi hatırlamıyorsanız şifre değişikliği işlemi için lütfen Bilgi İşlem ile görüşünüz.";
                    }
                    else
                    {
                        var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(kul.EnstituKod);
                        var mRowModel = new List<MailTableRowDto>();
                        var gecerlilikTarihi = DateTime.Now.AddHours(2);
                        var guid = Guid.NewGuid().ToString().Substring(0, 20);
                        mRowModel.Add(new MailTableRowDto { Baslik = "Şifre Sıfırlama Linki", Aciklama = "<a target='_blank' href='" + mailBilgi.SistemErisimAdresi + "/Account/ParolaSifirla?psKod=" + guid + "'>Şifrenizi sıfırlamak için tıklayınız</a>" });
                        mRowModel.Add(new MailTableRowDto { Baslik = "Link Geçerlilik Tarihi", Aciklama = "Yukarıdaki link '" + gecerlilikTarihi.ToFormatDateAndTime() + "' tarihine kadar geçerlidir." });

                        var sistemErisimAdresi = mailBilgi.SistemErisimAdresi;
                        var wurlAddr = sistemErisimAdresi.Split('/').ToList();
                        if (sistemErisimAdresi.Contains("//"))
                            sistemErisimAdresi = wurlAddr[0] + "//" + wurlAddr.Skip(2).Take(1).First();
                        else
                            sistemErisimAdresi = "https://" + wurlAddr.First();

                        var mmmC = new MailMainContentDto
                        {
                            EnstituAdi = _entities.Enstitulers.First(p => p.EnstituKod == kul.EnstituKod).EnstituAd,
                            UniversiteAdi = "Yıldız Teknik Üniversitesi",
                            WebAdresi = mailBilgi.WebAdresi,
                            SistemErisimAdresi = mailBilgi.SistemErisimAdresi,
                            Content = ViewRenderHelper.RenderPartialView("Ajax", "GetMailTableContent",
                                        new MailTableContentDto
                                        {
                                            AciklamaBasligi = "Şifre Sıfırlama İşlemi",
                                            AciklamaDetayi = "Şifrenizi sıfırlamak için aşağıda bulunan linke tıklayınız ve açılan sayfa da yeni şifrenizi tanımlayınız.",
                                            Detaylar = mRowModel
                                        }),
                            LogoPath = sistemErisimAdresi + "/Content/assets/images/ytu_logo_tr.png"

                        };
                        var htmlMail = ViewRenderHelper.RenderPartialView("Ajax", "GetMailContent", mmmC);
                        var eMailList = new List<MailSendList> { new MailSendList { EMail = kul.EMail, ToOrBcc = true, KullaniciId = kul.KullaniciID } };
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
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Şifre sıfırlama! Hata: " + rtVal.ToExceptionMessage(), rtVal.ToExceptionStackTrace(), BilgiTipiEnum.Hata, kul.KullaniciID, UserIdentity.Ip);
                            mmMessage.Title = "Şifre sıfırlama linki '" + kul.EMail + "' adresine gönderilemedi!";
                        }
                    }
                }

            }

            return mmMessage.ToJsonResult();
        }


        public ActionResult GetOts(string enstituKod, bool bosSecimVar = true, int? haricOgreniTipKod = null)
        {
            var cmbmld = OgrenimTipleriBus.CmbAktifOgrenimTipleri(enstituKod, bosSecimVar, true, haricOgreniTipKod);

            return cmbmld.ToJsonResult();
        }
        public ActionResult GetUnvanlar(int kullaniciTipId)
        {
            var isAkademik = kullaniciTipId == KullaniciTipiEnum.AkademikPersonel;
            var cmbmld = UnvanlarBus.CmbUnvanlar(true, isAkademik);

            return cmbmld.ToJsonResult();
        }

        public ActionResult GetProgramlar(int bolId, int otId, int basvuruSurecId, int kullaniciTipId)
        {
            var bolm = ProgramlarBus.CmbGetAktifProgramlarX(bolId, otId, basvuruSurecId, kullaniciTipId);
            return bolm.Select(s => new { s.Value, s.Caption }).ToJsonResult();
        }


        [ValidateInput(false)]
        public ActionResult MezuniyetValidationControlSteps(KmMezuniyetBasvuru kModel)
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

                if (kModel.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.Onaylandi)
                {
                    var yyK = MezuniyetBus.YayinKontrol(kModel);
                    mmMessage.Messages.AddRange(yyK.Messages);
                    mmMessage.MessagesDialog.AddRange(yyK.MessagesDialog);
                }
                if (mmMessage.Messages.Count == 0) kModel.SbmtForm = true;
            }

            mmMessage.IsSuccess = mmMessage.Messages.Count == 0;
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            return new { sbmtForm = kModel.SbmtForm, _MmMessage = mmMessage, EkAciklamalar = "" }.ToJsonResult();
        }




        [Authorize]
        public ActionResult RotateImage(bool? leftOrRight, int? kullaniciId)
        {
            if (!leftOrRight.HasValue || !kullaniciId.HasValue) return null;
            if (RoleNames.KullanicilarKayit.InRoleCurrent() == false) kullaniciId = UserIdentity.Current.Id;
            var user = _entities.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
            string folname = SistemAyar.KullaniciResimYolu;
            if (user.ResimAdi.IsNullOrWhiteSpace() == false)
            {
                var imgPath = folname + "/" + user.ResimAdi;
                string pth = Server.MapPath(GlobalSistemSetting.GetRoot() + imgPath);

                using (Image img = Image.FromFile(pth))
                {
                    img.RotateFlip(leftOrRight.Value ? RotateFlipType.Rotate270FlipNone : RotateFlipType.Rotate90FlipNone);
                    img.Save(pth, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

            }
            return new { ResimAdi = folname + "/" + user.ResimAdi }.ToJsonResult();
        }
        public ActionResult GetProgramlarEkod(string enstituKod)
        {
            var bolm = ProgramlarBus.CmbGetAktifProgramlar(enstituKod, true);
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

        public ActionResult GetAnket()
        {
            return View();
        }


        public ActionResult SetAnket(KmAnketlerCevap kModel)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Error,
                IsSuccess = false,
                Title = "Anket bilgisi oluşturulamadı. Lütfen aşağıdaki uyarıları inceleyiniz."
            };
            var qAnketSoruId = kModel.AnketSoruID.Select((s, inx) => new { AnketSoruID = s, inx }).ToList();
            var qAnketSoruSecenekId = kModel.AnketSoruSecenekID.Select((s, inx) => new { AnketSoruSecenekID = s, inx }).ToList();
            var qAnketSoruTabloVeri1 = kModel.TabloVeri1.Select((s, inx) => new { TabloVeri1 = s, inx }).ToList();
            var qAnketSoruTabloVeri2 = kModel.TabloVeri2.Select((s, inx) => new { TabloVeri2 = s, inx }).ToList();
            var qAnketSoruTabloVeri3 = kModel.TabloVeri3.Select((s, inx) => new { TabloVeri3 = s, inx }).ToList();
            var qAnketSoruTabloVeri4 = kModel.TabloVeri4.Select((s, inx) => new { TabloVeri4 = s, inx }).ToList();
            var qAnketSoruTabloVeri5 = kModel.TabloVeri5.Select((s, inx) => new { TabloVeri5 = s, inx }).ToList();
            var qAnketSoruSecenekAciklama = kModel.AnketSoruSecenekAciklama.Select((s, inx) => new { AnketSoruSecenekAciklama = s, inx }).ToList();


            #region grupla
            var qGroup = (from s in qAnketSoruId
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
                              IsEkAciklamaGir = qs?.IsEkAciklamaGir ?? false,
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
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "AnketSoruSecenekID_" + item.AnketSoruID });
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
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "AnketSoruSecenekID_" + item.AnketSoruID });
                    hatalilar.Add(item.AnketSoruID);
                }
            }
            else if (qGroup.Any(p => !p.IsTabloVeriGirisi && p.IsEkAciklamaGir && p.AnketSoruSecenekAciklama.IsNullOrWhiteSpace()))
            {
                var data = qGroup.Where(p => !p.IsTabloVeriGirisi && p.IsEkAciklamaGir && p.AnketSoruSecenekAciklama.IsNullOrWhiteSpace()).OrderBy(o => o.inx).ToList();
                foreach (var item in data)
                {

                    mMessage.Messages.Add(item.inx + " Numaralı soru için lütfen açıklama giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "AnketSoruSecenekID_" + item.AnketSoruID });
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
                            mMessage.Messages.Add(item.inx + " Numaralı soru içindeki tüm başlıkları cevaplayınız.");
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "AnketSoruSecenekID_" + item.AnketSoruID });
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
                            AnketSoruSecenekID = item.AnketSoruSecenekID,
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
                mMessage.MessageType = MsgTypeEnum.Success;
                //mMessage.Title = "Anket bilgileri doldurduğunuz için teşekkür ederiz.";

            }
            var hatasizlar = qGroup.Where(p => hatalilar.Contains(p.AnketSoruID) == false).Select(s => s.AnketSoruID).ToList();
            foreach (var item in hatasizlar)
            {
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "AnketSoruSecenekID_" + item });

            }
            return mMessage.ToJsonResult();

        }

        public ActionResult GetAnketCevap()
        {
            return View();
        }
        [Authorize]
        public ActionResult DAktifSinavlar(BasvuruTercihKontrolDto model)
        {
            var qOgrenimTipKods = model.OgrenimTipKods.Select((s, inx) => new { s, Index = inx }).ToList();
            var qProgramKods = model.ProgramKods.Select((s, inx) => new { s, Index = inx }).ToList();
            var qIngilizces = model.Ingilizces.Select((s, inx) => new { s, Index = inx }).ToList();
            var qtercihler = (from s in qOgrenimTipKods
                              join qp in qProgramKods on s.Index equals qp.Index
                              join qi in qIngilizces on s.Index equals qi.Index
                              select new CmbMultyTypeDto { Value = s.s, ValueB = qi.s, ValueS2 = qp.s }).ToList();
            var data = SinavTipleriBus.CmbGetdAktifSinavlar(qtercihler, model.BasvuruSurecID, model.SinavTipGrupID, true);
            return data.Select(s => new { s.Value, s.Caption }).ToJsonResult();
        }
        [Authorize]
        public ActionResult GetSalonlar(string enstituKod, int srTalepTipId, int? talepYapanId = null, int? id = null)
        {
            var cmbmld = SrTalepleriBus.GetCmbSalonlar(enstituKod, srTalepTipId, true);
            var ttip = _entities.SRTalepTipleris.First(p => p.SRTalepTipID == srTalepTipId);

            return new { ttip.IsTezSinavi, data = cmbmld.Select(s => new { s.Value, s.Caption }) }.ToJsonResult();
        }
        [Authorize]
        public ActionResult GetGunler(int srSalonId, int srTalepTipId, DateTime tarih, DateTime? tarih2 = null, int? srOzelTanimId = null)
        {


            var gunler = _entities.SRSaatlers.Where(p => p.SRSalonID == srSalonId).Select(s => s.HaftaGunID).Distinct();

            var gunL = _entities.HaftaGunleris.Where(p => gunler.Contains(p.HaftaGunID)).Select(s => new CmbIntDto { Value = s.HaftaGunID, Caption = s.HaftaGunAdi }).ToList();

            for (var date = tarih; date <= tarih2; date = date.AddDays(1.0))
            {
                var nTarih = date.Date;
                var dofW = nTarih.DayOfWeek.ToString("d").ToInt(0);


                var resmiTatilDegisen = _entities.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilDegisen && p.BasTarih.Value <= nTarih && p.BitTarih >= nTarih);
                var resmiTatilSabit = _entities.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilSabit && p.Ay.Value == nTarih.Month && p.Gun == nTarih.Day);
                var isSuccess = true;
                var qTalepEslesen = _entities.SRTalepleris.Where(a => a.SRSalonID == srSalonId && a.Tarih == nTarih).Any(p => p.SRDurumID == SrTalepDurumEnum.Onaylandı || p.SRDurumID == SrTalepDurumEnum.TalepEdildi);
                if (qTalepEslesen)
                {
                    isSuccess = false;
                }
                else if (resmiTatilDegisen != null || resmiTatilSabit != null)
                {
                    isSuccess = false;
                }

                var sgun = gunL.FirstOrDefault(p => p.Value == dofW);
                if (sgun != null && isSuccess == false) gunL.Remove(sgun);
            }


            return View(gunL);
        }

        [Authorize]
        public ActionResult GetSaatList(int srSalonId, bool isPopupFrame, int srTalepTipId, DateTime tarih, int? srTalepId, string mzRowId = null)
        {

            DateTime? minTarih = null;
            if (mzRowId != null && mzRowId.IsNullOrWhiteSpace() == false)
            {
                var rwId = new Guid(mzRowId);
                minTarih = _entities.MezuniyetBasvurularis.Where(p => p.RowID == rwId).Select(s => s.EYKTarihi).FirstOrDefault();
            }
            var data = SrTalepleriBus.GetSalonBosSaatler(srSalonId, srTalepTipId, tarih, srTalepId, null, minTarih);
            data.IsPopupFrame = isPopupFrame;
            var hcb = ViewRenderHelper.RenderPartialView("Ajax", "GetSaatlerView", data);
            return new { Deger = hcb }.ToJsonResult();
        }
        [Authorize]
        public ActionResult GetSaatlerView(SRSalonSaatlerModel model)
        {
            return View(model);
        }



        [Authorize(Roles = RoleNames.MailGonder),ValidateInput(false)] 
        public ActionResult MailGonder(KmMailGonder model, string ekd = "")
        {
            model.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var secilenKullaniciIDs = model.SecilenAlicilars.Where(p => p.IsNumber()).Select(s => s.ToInt(0)).ToList();
            var source = model.SecilenAlicilars.Where(p => !p.IsNumber()).Select(s => new CmbStringDto { Value = s, Caption = s }).ToList();

            if (!model.IsTopluMail)
            {
                if (secilenKullaniciIDs.Any())
                    model.EMails = this._entities.Kullanicilars
                        .Where(p => secilenKullaniciIDs.Contains(p.KullaniciID))
                        .Select(s => new CmbStringDto { Value = (s.KullaniciID + ""), Caption = s.EMail })
                        .ToList();

                if (source.Any())
                    model.EMails.AddRange(source);
            }

            ViewBag.MailSablonlariID = new SelectList(MailSablonTipleriBus.GetCmbMailSablonlari(model.EnstituKod, true, false), "Value", "Caption");
            ViewBag.MmMessage = new MmMessage();

            return View(model);
        } 



        [HttpPost]
        [Authorize(Roles = RoleNames.MailGonder), ValidateInput(false)]
        public ActionResult MailGonderPost2(KmMailGonder model, List<HttpPostedFileBase> dosyaEki, List<string> dosyaEkiAdi, List<string> ekYolu, string ekd)
        {
            var mmMessage = new MmMessage
            {
                Title = "Mail gönderme işlemi"
            };
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            dosyaEki = dosyaEki ?? new List<HttpPostedFileBase>();
            dosyaEkiAdi = dosyaEkiAdi ?? new List<string>();
            ekYolu = ekYolu ?? new List<string>();

            var secilenAlicilar = new List<string>();
            var secilenBccAlicilar = new List<string>();
            if (model.Alici.IsNullOrWhiteSpace() == false) model.Alici.Split(',').ToList().ForEach((itm) => { secilenAlicilar.Add(itm); });
            if (model.BccAlici.IsNullOrWhiteSpace() == false) model.BccAlici.Split(',').ToList().ForEach((itm) => { secilenBccAlicilar.Add(itm); });
            if (model.Aciklama.IsNullOrWhiteSpace() == false)
            {



                var originalMessage = "";
                var replyLink = "";
                if (model.MesajID.HasValue)
                {
                    var enstitu = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod);
                    var mesaj = _entities.Mesajlars.First(p => p.MesajID == model.MesajID.Value);
                    replyLink = $"{enstitu.SistemErisimAdresi}/Home/Index?MesajGroupID={mesaj.GroupID}";
                    if (mesaj.Mesajlar2 != null) mesaj = mesaj.Mesajlar2;
                    model.MesajID = mesaj.MesajID;
                    originalMessage = mesaj.AciklamaHtml;

                }
                var emailTemplate = new EmailTemplateModel
                {
                    CurrentMessage = model.AciklamaHtml,
                    ReplyUrl = replyLink,
                    PreviousMessage = originalMessage
                };
                var mtView = ViewRenderHelper.RenderPartialView("Ajax", "MailTemplateView", emailTemplate);


                model.AciklamaHtml = mtView;
            }
            if (model.IsTopluMail && !model.SecilenTopluAlicilar.IsNullOrWhiteSpace())
            {
                secilenAlicilar.AddRange(model.SecilenTopluAlicilar.Split(',').ToList());
            }

            var qDosyaEkAdi = dosyaEkiAdi.Select((s, inx) => new { s, inx }).ToList();
            var qDosyaEki = dosyaEki.Select((s, inx) => new { s, inx }).ToList();
            var qDosyaEkYolu = ekYolu.Select((s, inx) => new { s, inx }).ToList();


            var kModel = new GonderilenMailler();
            #region Kontrol 

            if (secilenAlicilar.Count == 0)
            {
                mmMessage.Messages.Add("Mail Gönderilecek Hiçbir Alıcı Belirlenemedi!");
            }

            if (model.Konu.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Konu Giriniz.");
            }

            if (model.Aciklama.IsNullOrWhiteSpace() && model.AciklamaHtml.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("İçerik Giriniz.");
            }
            #endregion

            kModel.Tarih = DateTime.Now;
            kModel.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            if (mmMessage.Messages.Count == 0)
            {

                kModel.EnstituKod = enstituKod;
                kModel.MesajID = model.MesajID;
                kModel.IslemTarihi = DateTime.Now;
                kModel.Konu = model.Konu;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.Aciklama = model.Aciklama ?? "";
                kModel.AciklamaHtml = model.AciklamaHtml ?? "";

                var eklenenGonderilenMail = _entities.GonderilenMaillers.Add(kModel);
                var qDosyalar = (from ekGirilenAd in qDosyaEkAdi
                                 join eklenenEk in qDosyaEki on ekGirilenAd.inx equals eklenenEk.inx
                                 join varolanEkYolu in qDosyaEkYolu on ekGirilenAd.inx equals varolanEkYolu.inx
                                 select new
                                 {
                                     ekGirilenAd.inx,
                                     Dosya = eklenenEk.s,
                                     DosyaAdi = eklenenEk.s != null ? ekGirilenAd.s + eklenenEk.s.FileName.GetFileExtension() : ekGirilenAd.s,
                                     DosyaYolu = eklenenEk.s != null ? FileHelper.SaveMailDosya(eklenenEk.s) : varolanEkYolu.s
                                 }).ToList();
                eklenenGonderilenMail.GonderilenMailEkleris = qDosyalar.Select(s => new GonderilenMailEkleri
                {
                    EkAdi = s.DosyaAdi,
                    EkDosyaYolu = s.DosyaYolu,
                }).ToList();
                if (model.MesajID.HasValue)
                {
                    var mesaj = _entities.Mesajlars.FirstOrDefault(p => p.MesajID == model.MesajID.Value);
                    if (mesaj != null)
                    {
                        mesaj.IsAktif = true;
                    }
                }


                var gonderilenMailKullanicilari = new List<GonderilenMailKullanicilar>();
                secilenAlicilar = secilenAlicilar.Distinct().ToList();
                if (secilenAlicilar.Count > 0)
                {
                    var aliciKullaniciIds = secilenAlicilar.Where(p => p.IsNumber()).Select(s => s.ToInt(0)).ToList();
                    var aliciEmails = secilenAlicilar.Where(p => p.IsNumber() == false).ToList();
                    var secilenAliciKullanicilar = (from s in _entities.Kullanicilars
                                                    where aliciKullaniciIds.Contains(s.KullaniciID)
                                                    select new
                                                    {
                                                        Email = s.EMail,
                                                        eklenenGonderilenMail.GonderilenMailID,
                                                        s.KullaniciID
                                                    }).ToList();
                    gonderilenMailKullanicilari.AddRange(secilenAliciKullanicilar.Select(item => new GonderilenMailKullanicilar { Email = item.Email, GonderilenMailID = item.GonderilenMailID, KullaniciID = item.KullaniciID }));
                    gonderilenMailKullanicilari.AddRange(aliciEmails.Select(item => new GonderilenMailKullanicilar { Email = item, GonderilenMailID = eklenenGonderilenMail.GonderilenMailID, KullaniciID = null }));
                }
                var gonderilenMailKullanicilariBcc = new List<GonderilenMailKullanicilar>();
                if (secilenBccAlicilar.Count > 0)
                {
                    var aliciKullaniciIds = secilenBccAlicilar.Where(p => p.IsNumber()).Select(s => s.ToInt(0)).ToList();
                    var aliciEmails = secilenBccAlicilar.Where(p => p.IsNumber() == false).ToList();
                    var secilenAliciKullanicilar = (from s in _entities.Kullanicilars
                                                    where aliciKullaniciIds.Contains(s.KullaniciID)
                                                    select new
                                                    {
                                                        Email = s.EMail,
                                                        eklenenGonderilenMail.GonderilenMailID,
                                                        s.KullaniciID
                                                    }).ToList();
                    gonderilenMailKullanicilariBcc.AddRange(secilenAliciKullanicilar.Select(item => new GonderilenMailKullanicilar { Email = item.Email, GonderilenMailID = item.GonderilenMailID, KullaniciID = item.KullaniciID }));
                    gonderilenMailKullanicilariBcc.AddRange(aliciEmails.Select(item => new GonderilenMailKullanicilar { Email = item, GonderilenMailID = eklenenGonderilenMail.GonderilenMailID, KullaniciID = null }));
                }
                eklenenGonderilenMail.Gonderildi = true;
                gonderilenMailKullanicilari.AddRange(gonderilenMailKullanicilariBcc);
                gonderilenMailKullanicilari = gonderilenMailKullanicilari.Distinct().ToList();
                eklenenGonderilenMail.GonderilenMailKullanicilars = gonderilenMailKullanicilari;

                _entities.SaveChanges();
                if (model.MesajID.HasValue)
                {
                    MesajlarBus.MesajUpdate(model.MesajID.Value);
                }

                var gidecekler = gonderilenMailKullanicilari.Select(s => s.Email).ToList();
                var dct = new Dictionary<int, List<MailSendList>>();
                var inx = 0;
                while (gidecekler.Count > 500)
                {
                    dct.Add(inx, gidecekler.Take(500).Select(s => new MailSendList { EMail = s, ToOrBcc = gonderilenMailKullanicilariBcc.All(a => a.Email != s) }).ToList());
                    gidecekler = gidecekler.Skip(500).ToList();
                    inx++;
                }
                inx++;
                dct.Add(inx, gidecekler.Select(s => new MailSendList { EMail = s, ToOrBcc = gonderilenMailKullanicilariBcc.Any(a => a.Email != s) }).ToList());

                var attach = eklenenGonderilenMail.GonderilenMailEkleris.Select(s => new FileAttachmentInfo { FileName = s.EkAdi, FilePath = s.EkDosyaYolu }).ToList().GetFileToAttachments();
                foreach (var item in dct)
                {
                    var excpt = MailManager.SendMailRetVal(enstituKod, kModel.Konu, kModel.AciklamaHtml, item.Value, attach);
                    if (excpt == null)
                    {
                        mmMessage.Messages.Add("Mail gönderildi!");
                        mmMessage.IsSuccess = true;
                        mmMessage.MessageType = MsgTypeEnum.Success;
                    }
                    else
                    {
                        var msgerr = excpt.ToExceptionMessage().Replace("\r\n", "<br/>");
                        mmMessage.Messages.Add("Mail gönderilirken bir hata oluştu! <br/>Hata:" + msgerr);
                        mmMessage.IsSuccess = false;
                        mmMessage.MessageType = MsgTypeEnum.Error;
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(excpt.ToExceptionMessage(), excpt.ToExceptionStackTrace(), BilgiTipiEnum.Hata);

                        try
                        {
                            _entities.GonderilenMaillers.Remove(eklenenGonderilenMail);
                            _entities.SaveChanges();
                            foreach (var item2 in qDosyalar)
                            {
                                FileHelper.Delete(item2.DosyaYolu);
                            }
                        }
                        catch (Exception ex)
                        {
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                        }
                    }
                }
            }
            else
            {
                mmMessage.IsSuccess = false;
                mmMessage.MessageType = MsgTypeEnum.Warning;
            }

            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { success = mmMessage.IsSuccess, responseText = strView }, JsonRequestBehavior.AllowGet);

        }
        [HttpPost]
        [Authorize(Roles = RoleNames.MailGonder), ValidateInput(false)]
        public ActionResult MailGonderPost(KmMailGonder model, List<HttpPostedFileBase> dosyaEki, List<string> dosyaEkiAdi, List<string> ekYolu, string ekd)
        {
            var mesaj = new MmMessage
            {
                Title = "Mail gönderme işlemi"
            };

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            // Null kontrolü yapılan listeleri ayarla
            dosyaEki = dosyaEki ?? new List<HttpPostedFileBase>();
            dosyaEkiAdi = dosyaEkiAdi ?? new List<string>();
            ekYolu = ekYolu ?? new List<string>();

            // Alıcıları ve BCC alıcılarını hazırla
            var secilenAlicilar = new List<string>();
            var secilenBccAlicilar = new List<string>();

            // Alıcıları ayarla
            if (!model.Alici.IsNullOrWhiteSpace())
            {
                secilenAlicilar.AddRange(model.Alici.Split(',').ToList());
            }

            // BCC alıcıları ayarla
            if (!model.BccAlici.IsNullOrWhiteSpace())
            {
                secilenBccAlicilar.AddRange(model.BccAlici.Split(',').ToList());
            }

            // Mesaj içeriğini hazırla
            if (!model.Aciklama.IsNullOrWhiteSpace())
            {
                string oncekiMesaj = "";
                string cevapUrl = "";

                if (model.MesajID.HasValue)
                {
                    var enstitu = _entities.Enstitulers.FirstOrDefault(p => p.EnstituKod == enstituKod);
                    var mesaj1 = _entities.Mesajlars.FirstOrDefault(p => p.MesajID == model.MesajID.Value);

                    if (enstitu != null && mesaj1 != null)
                    {
                        cevapUrl = $"{enstitu.SistemErisimAdresi}/Home/Index?MesajGroupID={mesaj1.GroupID}";

                        if (mesaj1.Mesajlar2 != null)
                            mesaj1 = mesaj1.Mesajlar2;

                        model.MesajID = mesaj1.MesajID;
                        oncekiMesaj = mesaj1.AciklamaHtml;
                    }
                }

                EmailTemplateModel templateModel = new EmailTemplateModel
                {
                    CurrentMessage = model.AciklamaHtml,
                    ReplyUrl = cevapUrl,
                    PreviousMessage = oncekiMesaj
                };

                model.AciklamaHtml = ViewRenderHelper.RenderPartialView("Ajax", "MailTemplateView", templateModel);
            }

            // Toplu mail kontrolü
            if (model.IsTopluMail && !model.SecilenTopluAlicilar.IsNullOrWhiteSpace())
            {
                secilenBccAlicilar.AddRange(model.SecilenTopluAlicilar.Split(',').ToList());
            }

            // Dosya ekleri hazırla
            var dosyaEkleri = dosyaEkiAdi.Select((ad, index) => new { Ad = ad, Index = index })
                .Join(
                    dosyaEki.Select((dosya, index) => new { Dosya = dosya, Index = index }),
                    x => x.Index,
                    y => y.Index,
                    (x, y) => new { x.Ad, y.Dosya, x.Index }
                )
                .Join(
                    ekYolu.Select((yol, index) => new { Yol = yol, Index = index }),
                    z => z.Index,
                    w => w.Index,
                    (z, w) => new { z.Ad, z.Dosya, w.Yol, z.Index }
                ).ToList();

            // Mail gönderme işlemi
            var gonderilenMail = new GonderilenMailler();

            // Validasyon kontrolleri
            if (!secilenAlicilar.Any() && !secilenBccAlicilar.Any())
                mesaj.Messages.Add("Mail Gönderilecek Hiçbir Alıcı Belirlenemedi!");

            if (model.Konu.IsNullOrWhiteSpace())
                mesaj.Messages.Add("Konu Giriniz.");

            if (model.Aciklama.IsNullOrWhiteSpace() && model.AciklamaHtml.IsNullOrWhiteSpace())
                mesaj.Messages.Add("İçerik Giriniz.");

            // Mail bilgilerini ayarla
            gonderilenMail.Tarih = DateTime.Now;
            gonderilenMail.EnstituKod = enstituKod;

            if (mesaj.Messages.Count == 0)
            {
                gonderilenMail.MesajID = model.MesajID;
                gonderilenMail.IslemTarihi = DateTime.Now;
                gonderilenMail.Konu = model.Konu;
                gonderilenMail.IslemYapanID = UserIdentity.Current.Id;
                gonderilenMail.IslemYapanIP = UserIdentity.Ip;
                gonderilenMail.Aciklama = model.Aciklama ?? "";
                gonderilenMail.AciklamaHtml = model.AciklamaHtml ?? "";

                var eklenenGonderilenMail = _entities.GonderilenMaillers.Add(gonderilenMail);

                // Ekler için kayıt
                eklenenGonderilenMail.GonderilenMailEkleris = dosyaEkleri.Select(x => new GonderilenMailEkleri
                {
                    GonderilenMailID = eklenenGonderilenMail.GonderilenMailID,
                    EkAdi = x.Ad,
                    EkDosyaYolu = x.Yol
                }).ToList();

                // Mesaj güncelleme
                if (model.MesajID.HasValue)
                {
                    var mesaj1 = _entities.Mesajlars.FirstOrDefault(p => p.MesajID == model.MesajID.Value);
                    if (mesaj1 != null)
                        mesaj1.IsAktif = true;
                }

                // Kullanıcı mail kayıtları
                var gonderilenMailKullanicilar = new List<GonderilenMailKullanicilar>();
                secilenAlicilar = secilenAlicilar.Distinct().ToList();

                if (secilenAlicilar.Count > 0)
                {
                    // Sayı olan alıcıları kullanıcı ID'lerine çevir
                    var kullaniciIDleri = secilenAlicilar.Where(x => x.IsNumber()).Select(x => x.ToInt(0)).ToList();
                    var emailAdresleri = secilenAlicilar.Where(x => !x.IsNumber()).ToList();

                    // Kullanıcı ID'lerine göre e-posta adresleri
                    var kullaniciEmailler = new List<object>();

                    // Her seferde 1500 kullanıcıyı sorgula (performans için)
                    for (int count = 0; count < kullaniciIDleri.Count; count += 1500)
                    {
                        var chunk = kullaniciIDleri.Skip(count).Take(1500).ToList();
                        var kullanicilar = _entities.Kullanicilars
                            .Where(s => chunk.Contains(s.KullaniciID))
                            .Select(s => new {
                                Email = s.EMail,
                                eklenenGonderilenMail.GonderilenMailID,
                                s.KullaniciID
                            })
                            .ToList();

                        kullaniciEmailler.AddRange(kullanicilar);
                    }

                    // Kullanıcı e-postaları ekle
                    gonderilenMailKullanicilar.AddRange(kullaniciEmailler.Select(x => new GonderilenMailKullanicilar
                    {
                        Email = ((dynamic)x).Email,
                        GonderilenMailID = ((dynamic)x).GonderilenMailID,
                        KullaniciID = ((dynamic)x).KullaniciID
                    }));

                    // E-posta adreslerini ekle
                    gonderilenMailKullanicilar.AddRange(emailAdresleri.Select(email => new GonderilenMailKullanicilar
                    {
                        Email = email,
                        GonderilenMailID = eklenenGonderilenMail.GonderilenMailID
                    }));
                }

                // BCC alıcılar için
                var gonderilenMailKullanicilariBcc = new List<GonderilenMailKullanicilar>();

                if (secilenBccAlicilar.Count > 0)
                {
                    // Benzer işlem BCC için de yapılır
                    var kullaniciIDleriBcc = secilenBccAlicilar.Where(x => x.IsNumber()).Select(x => x.ToInt(0)).ToList();
                    var emailAdresleri = secilenBccAlicilar.Where(x => !x.IsNumber()).ToList();

                    var kullaniciEmailler = new List<object>();

                    for (int count = 0; count < kullaniciIDleriBcc.Count; count += 1500)
                    {
                        var chunk = kullaniciIDleriBcc.Skip(count).Take(1500).ToList();
                        var kullanicilar = _entities.Kullanicilars
                            .Where(s => chunk.Contains(s.KullaniciID))
                            .Select(s => new {
                                Email = s.EMail,
                                eklenenGonderilenMail.GonderilenMailID,
                                s.KullaniciID
                            })
                            .ToList();

                        kullaniciEmailler.AddRange(kullanicilar);
                    }

                    gonderilenMailKullanicilariBcc.AddRange(kullaniciEmailler.Select(x => new GonderilenMailKullanicilar
                    {
                        Email = ((dynamic)x).Email,
                        GonderilenMailID = ((dynamic)x).GonderilenMailID,
                        KullaniciID = ((dynamic)x).KullaniciID 
                    }));

                    gonderilenMailKullanicilariBcc.AddRange(emailAdresleri.Select(email => new GonderilenMailKullanicilar
                    {
                        Email = email,
                        GonderilenMailID = eklenenGonderilenMail.GonderilenMailID 
                    }));
                }

                // Mail gönderildi olarak işaretle
                eklenenGonderilenMail.Gonderildi = true;

                // Tüm alıcıları birleştir
                gonderilenMailKullanicilar.AddRange(gonderilenMailKullanicilariBcc);
                var tumAlicilar = gonderilenMailKullanicilar.Distinct().ToList();
                eklenenGonderilenMail.GonderilenMailKullanicilars = tumAlicilar;

                // Veritabanı değişikliklerini kaydet
                _entities.SaveChanges();

                // Mesaj güncelleme
                if (model.MesajID.HasValue)
                {
                    MesajlarBus.MesajUpdate(model.MesajID.Value);
                }

                // Mail gönderimi
                var emailListesi = tumAlicilar.Select(x => x.Email).ToList();
                var mailGonderimListeleri = new Dictionary<int, List<MailSendList>>();

                // 500'er adresten oluşan gruplar oluştur
                int key = 0;
                while (emailListesi.Count > 500)
                {
                    mailGonderimListeleri.Add(key, emailListesi.Take(500).Select(email => new MailSendList
                    {
                        EMail = email 
                    }).ToList());

                    emailListesi = emailListesi.Skip(500).ToList();
                    key++;
                }

                // Kalan e-postaları da ekle
                mailGonderimListeleri.Add(key + 1, emailListesi.Select(email => new MailSendList
                {
                    EMail = email
                }).ToList());

                // Dosya eklerini hazırla
                var dosyaEkleriMailIcin = eklenenGonderilenMail.GonderilenMailEkleris
                    .Select(x => new FileAttachmentInfo
                    {
                        FileName = x.EkAdi,
                        FilePath = x.EkDosyaYolu
                    })
                    .ToList()
                    .GetFileToAttachments();

                int basariliGonderimSayisi = 0;
                var basarisizMailKullanicilar = new List<GonderilenMailKullanicilar>();

                // Mail gönderme işlemi
                foreach (var mailGrubu in mailGonderimListeleri)
                {
                    Exception hata = MailManager.SendMailRetVal(
                        enstituKod,
                        gonderilenMail.Konu,
                        gonderilenMail.AciklamaHtml,
                        mailGrubu.Value,
                        dosyaEkleriMailIcin
                    );

                    if (hata == null)
                    {
                        mesaj.Messages.Add(mailGrubu.Value.Count + " Kişiye Mail gönderildi!");
                        mesaj.IsSuccess = true;
                        mesaj.MessageType = MsgTypeEnum.Success;
                        basariliGonderimSayisi++;
                    }
                    else
                    {
                        // Başarısız olan e-postaları belirle
                        var basarisizEmailler = mailGrubu.Value.Select(x => x.EMail).ToList();
                        var basarisizKullanicilar = _entities.GonderilenMailKullanicilars
                            .Where(p => p.GonderilenMailID == eklenenGonderilenMail.GonderilenMailID && basarisizEmailler.Contains(p.Email))
                            .ToList();

                        basarisizMailKullanicilar.AddRange(basarisizKullanicilar);

                        // Hata mesajını kaydet
                        string hataMesaji = hata.ToExceptionMessage().Replace("\r\n", "<br/>");
                        mesaj.Messages.Add("Mail gönderilirken bir hata oluştu! <br/>Hata:" + hataMesaji);
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(hata.ToExceptionMessage(), hata.ToExceptionStackTrace(), 1);
                    }
                }

                // Hiçbir mail gönderilemedi ise kaydı temizle
                if (basariliGonderimSayisi == 0)
                {
                    try
                    {
                        _entities.GonderilenMaillers.Remove(eklenenGonderilenMail);
                        _entities.SaveChanges();

                        // Dosya eklerini temizle
                        foreach (var dosya in dosyaEkleri)
                        {
                            FileHelper.Delete(dosya.Yol);
                        }

                        mesaj.IsSuccess = false;
                        mesaj.MessageType = MsgTypeEnum.Error;
                    }
                    catch (Exception ex)
                    {
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), 1);
                    }
                }
                // Bazı mailler gönderilemedi ise onları çıkar
                else if (basarisizMailKullanicilar.Count > 0)
                {
                    try
                    {
                        mesaj.Messages.Add(basarisizMailKullanicilar.Count + " kişiye mail gönderilemedi ve listeden çıkarıldı.");
                        _entities.GonderilenMailKullanicilars.RemoveRange(basarisizMailKullanicilar);
                        _entities.SaveChanges();

                        mesaj.IsSuccess = true;
                        mesaj.MessageType = MsgTypeEnum.Warning;
                    }
                    catch (Exception ex)
                    {
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), 1);
                    }
                }
            }
            else
            {
                mesaj.IsSuccess = false;
                mesaj.MessageType = MsgTypeEnum.Warning;
            }

            // Mesaj HTML'ini render et
            string mesajHtml = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mesaj);

            // JSON olarak sonuç döndür
            return Json(new { success = mesaj.IsSuccess, responseText = mesajHtml }, JsonRequestBehavior.AllowGet);
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
            var kul = qKullanicilar.Select(k => new MailListDto
            {
                id = k.id.ToString(),
                AdSoyad = k.AdSoyad,
                text = k.text,
                Images = k.Images.ToKullaniciResim()

            }).ToList();
            if (kul.Count == 0)
            {
                var lst = new List<MailListDto>();
                if (term.ToIsValidEmail())
                {
                    lst.Add(new MailListDto { id = term, AdSoyad = term, text = term, Images = "".ToKullaniciResim() });
                }
                return lst.ToJsonResult();
            }

            return kul.ToJsonResult();
        }


        [Authorize]
        public ActionResult BasvuruGonderilenMailler(string rowId)
        {
            var basvuru = _entities.Basvurulars.First(p => p.RowID == new Guid(rowId));

            return View(basvuru);
        }

        [Authorize]
        public ActionResult GetSablonlar(int mailSablonlariId)
        {
            var sbl = _entities.MailSablonlaris.Where(p => p.MailSablonlariID == mailSablonlariId).Select(s => new { s.SablonAdi, s.Sablon, s.SablonHtml, MailSablonlariEkleri = s.MailSablonlariEkleris.Select(s2 => new { s2.MailSablonlariEkiID, s2.EkAdi, s2.EkDosyaYolu }) }).First();
            return Json(new { sbl.SablonAdi, sbl.Sablon, sbl.SablonHtml, sbl.MailSablonlariEkleri }, "application/json", JsonRequestBehavior.AllowGet);
        }


        [AllowAnonymous]
        public ActionResult GetMsjKategoris(string enstituKod)
        {
            var ots = MesajlarBus.CmbGetMesajKategorileri(enstituKod, true, true);
            return ots.Select(s => new { s.Value, s.Caption }).ToJsonResult();
        }
        public ActionResult GetKtNot(int mesajKategoriId)
        {
            var mesajKategorisi = _entities.MesajKategorileris.FirstOrDefault(p => p.MesajKategoriID == mesajKategoriId);
            return Json(new { NotBilgisi = mesajKategorisi?.KategoriAciklamasi ?? "" });
        }
        public ActionResult MesajKaydet(string dlgid, string groupId, string ekd)
        {
            var model = new Mesajlar();
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };
            ViewBag.MmMessage = mmMessage;
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            if (groupId.IsNullOrWhiteSpace() == false)
            {
                model = _entities.Mesajlars.First(p => p.GroupID == groupId);
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
            ViewBag.EnstituAdi = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod).EnstituAd;
            ViewBag.MesajKategoriID = new SelectList(MesajlarBus.CmbGetMesajKategorileri(enstituKod, true, model != null), "Value", "Caption", model?.MesajKategoriID);

            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(), "Value", "Caption", enstituKod);

            return View(model);
        }



        [HttpPost]
        [ValidateInput(false)]
        public ActionResult MesajKaydetPost(int mesajId, string groupId, int mesajKategoriId, string konu, string adSoyad, string email, string aciklama, string aciklamaHtml, List<HttpPostedFileBase> dosyaEki, List<string> dosyaEkiAdi, string ekd)
        {
            konu = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(konu, false);
            adSoyad = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(adSoyad, false);
            aciklama = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(aciklama, false);
            email = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(email, false);
            groupId = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(groupId, false);

            //XssSaldırısı Var chec

            var mmMessage = new MmMessage
            {
                Title = "Dilek/Öneri/Şikayet gönderme işlemi"
            };

            dosyaEki = dosyaEki ?? new List<HttpPostedFileBase>();
            dosyaEkiAdi = dosyaEkiAdi ?? new List<string>();

            var qDosyaEkAdi = dosyaEkiAdi.Select((s, inx) => new { s, inx }).ToList();
            var qDosyaEki = dosyaEki.Select((s, inx) => new { s, inx }).ToList();

            var qDosyalar = (from dek in qDosyaEkAdi
                             join de in qDosyaEki on dek.inx equals de.inx
                             select new
                             {
                                 dek.inx,
                                 Dosya = de.s,
                                 FExtension = de.s.FileName.GetFileExtension(),
                                 DosyaAdi = dek.s + de.s.FileName.GetFileExtension()
                             }).ToList();

            foreach (var item in qDosyalar)
            {
                if (!FileExtension.FExtensions().Contains(item.FExtension))
                {
                    mmMessage.Messages.Add(item.Dosya.FileName + " dosyası " + string.Join(" , ", FileExtension.FExtensions()).Replace(".", "") + " uzantılarından farklı bir dosya olamaz.");
                }
            }


            #region Kontrol 


            if (mesajId <= 0)
            {
                if (konu.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Konu Giriniz.");
                }
            }
            else
            {
                var mesaj = _entities.Mesajlars.First(p => p.MesajID == mesajId && p.GroupID == groupId);
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
                mmMessage.Messages.Add("İçerik Giriniz.");
            }

            var kModel = new Mesajlar
            {
                EnstituKod = EnstituBus.GetSelectedEnstitu(ekd)
            };

            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                var mesajKategorisi = _entities.MesajKategorileris.First(f => f.MesajKategoriID == mesajKategoriId);
                kModel.MesajKategoriID = mesajKategoriId;
                if (UserIdentity.Current.IsAuthenticated == false)
                {
                    kModel.AdSoyad = adSoyad;
                    kModel.Email = email;

                }
                else
                {
                    var kul = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
                    kModel.AdSoyad = kul.Ad + " " + kul.Soyad;
                    kModel.Email = kul.EMail;
                    kModel.KullaniciID = UserIdentity.Current.Id;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                }
                kModel.UstMesajID = mesajId <= 0 ? (int?)null : mesajId;
                kModel.GroupID = Guid.NewGuid().ToString();
                kModel.Tarih = DateTime.Now;
                kModel.SonMesajTarihi = kModel.Tarih;
                kModel.ToplamEkSayisi = 0;
                kModel.EnstituKod = mesajKategorisi.EnstituKod;
                kModel.IslemTarihi = DateTime.Now;
                kModel.Konu = konu;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.Aciklama = aciklama ?? "";
                kModel.AciklamaHtml = aciklamaHtml ?? "";

                var eklenen = _entities.Mesajlars.Add(kModel);
                foreach (var item in qDosyalar)
                {
                    eklenen.MesajEkleris.Add(new MesajEkleri
                    {
                        EkAdi = item.DosyaAdi,
                        EkDosyaYolu = FileHelper.SaveMesajDosya(item.Dosya)
                    });
                }



                _entities.SaveChanges();
                if (eklenen.UstMesajID.HasValue)
                {
                    var ustMesaj = eklenen.Mesajlar2;
                    ustMesaj.ToplamEkSayisi = (ustMesaj.MesajEkleris.Count + ustMesaj.Mesajlar1.Sum(s => s.MesajEkleris.Count) + ustMesaj.GonderilenMaillers.Sum(s => s.GonderilenMailEkleris.Count));
                    ustMesaj.SonMesajTarihi = eklenen.Tarih;
                }
                else
                {
                    eklenen.SonMesajTarihi = eklenen.Mesajlar1.Any() ? eklenen.Mesajlar1.OrderByDescending(s2 => s2.Tarih).First().Tarih : eklenen.Tarih;
                    eklenen.ToplamEkSayisi = (eklenen.MesajEkleris.Count + eklenen.Mesajlar1.Sum(s => s.MesajEkleris.Count) + eklenen.GonderilenMaillers.Sum(s => s.GonderilenMailEkleris.Count));
                }
                _entities.SaveChanges();
                mmMessage.IsSuccess = true;

                if (mesajId <= 0 && groupId.IsNullOrWhiteSpace())
                {
                    var item = new SablonMailModel
                    {
                        Sablon = _entities.MailSablonlaris.FirstOrDefault(p => p.IsAktif == true && p.EnstituKod == mesajKategorisi.EnstituKod && p.MailSablonTipID == MailSablonTipiEnum.GelenIlkMesajOtoCvpMaili)
                    };
                    if (item.Sablon != null)
                    {
                        var enstitu = item.Sablon.Enstituler;
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.SistemErisimAdresi = enstitu.SistemErisimAdresi;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());
                        item.EMails.Add(new MailSendList { EMail = kModel.Email, ToOrBcc = true });
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = kModel.AdSoyad });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        try
                        {
                            MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        }
                        catch (Exception e)
                        {
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gonderilirken bir hata oluştu. hata:" + e.ToExceptionMessage(), e.ToExceptionStackTrace(), BilgiTipiEnum.Kritik);
                        }
                    }
                }
            }
            else
            {
                mmMessage.IsSuccess = false;
            }
            return Json(new { success = mmMessage.IsSuccess, responseText = mmMessage.IsSuccess ? "Mesaj gönderme işlemi başarılı!" : "Mesaj gönderilirken bir hata oluştu!<br/>" + string.Join("<br/>", mmMessage.Messages) }, JsonRequestBehavior.AllowGet);

        }

        [Authorize(Roles = RoleNames.Kullanicilar)]
        public async Task<ActionResult> ObsOgrenciSorgula(string tc = "", string donemId = "")
        {
            var obsGetData = new ObsServiceData();
            var model = new ObsOgrenciSorgulaModel();
            if (!tc.IsNullOrWhiteSpace()) model = obsGetData.GetOgrenciBilgi(tc, donemId);
            model.Tc = tc;
            model.DonemId = donemId;
            if (model.Ogrenci != null)
            {
                var aktifDonem = await ObsRestApiService.GetAktifDonem();
                if (aktifDonem != null)
                    model.OgrenciDonemler =
                        DonemHelper.GetCmbAkademikTarih(model.OgrenciKayitDonem, aktifDonem.DonemId);
            }
            var view = ViewRenderHelper.RenderPartialView("Ajax", "ObsOgrenciSorgula", model);
            return view.ToJsonResult();
        }

        [Authorize]
        public ActionResult GetYtuOgretimEleman(string term)
        {
            var data = PersisServiceData.GetWsPersisOe(term);
            foreach (var item in data.Table)
            {
                item.AKADEMIKUNVAN = item.AKADEMIKUNVAN.ToJuriUnvanAdi().AddSpacesBetweenTitleAbbreviations();
            }
            var ytuUni = _entities.Universitelers.FirstOrDefault(p => p.UniversiteID == GlobalSistemSetting.UniversiteYtuKod);
            var kul2 = data.Table.Select(s => new
            {
                id = s.ADSOYAD,
                AdSoyad = s.ADSOYAD,
                text = s.ADSOYAD,
                BolumAdi = s.BOLUMADI.Replace("BÖLÜMÜ", ""),
                UnvanAdi = s.AKADEMIKUNVAN,
                UniversiteID = ytuUni?.UniversiteID ?? GlobalSistemSetting.UniversiteYtuKod,
                UniversiteAdi = (ytuUni != null ? ytuUni.Ad : "Yıldız Teknik Üniversitesi").ToUpper(),
                EMail = s.KURUMMAIL
            }).Where(p => UnvanlarBus.JuriUnvanList.Contains(p.UnvanAdi)).OrderBy(o => o.AdSoyad).Take(25).ToList();

            return kul2.ToJsonResult();
        }
        [Authorize]
        public ActionResult GetYtuOgretimElemanDp(string term)
        {
            var data = PersisServiceData.GetWsPersisOe(term);
            var ytuUni = _entities.Universitelers.FirstOrDefault(p => p.UniversiteID == GlobalSistemSetting.UniversiteYtuKod);
            foreach (var item in data.Table)
            {
                item.AKADEMIKUNVAN = item.AKADEMIKUNVAN.ToJuriUnvanAdi().AddSpacesBetweenTitleAbbreviations();
            }
            var kul2 = data.Table.Select(s => new
            {
                id = s.ADSOYAD,
                AdSoyad = s.ADSOYAD,
                text = s.ADSOYAD,
                BolumAdi = s.BOLUMADI.Replace("BÖLÜMÜ", ""),
                UnvanAdi = s.AKADEMIKUNVAN,
                UniversiteID = ytuUni?.UniversiteID ?? GlobalSistemSetting.UniversiteYtuKod,
                UniversiteAdi = (ytuUni != null ? ytuUni.Ad : "Yıldız Teknik Üniversitesi").ToUpper(),
                EMail = s.KURUMMAIL
            }).Where(p => UnvanlarBus.DpJuriUnvanList.Contains(p.UnvanAdi)).OrderBy(o => o.AdSoyad).Take(25).ToList();

            return kul2.ToJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularTezKontrolYetkiliAtama)]
        public ActionResult GetTezkontrolYetkilisi(string term, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var query = MezuniyetBus.GetAktifTezKontrolSorumlulari(enstituKod).AsQueryable();
            if (!term.IsNullOrWhiteSpace())
            {
                query = query.Where(p =>
                    (p.Ad + " " + p.Soyad).Contains(term) || p.TcKimlikNo == term || p.EMail.Contains(term));
            }
            var tezKontrolSorumlulari = query.Select(s => new
            {
                id = s.KullaniciID,
                ResimAdi = s.ResimAdi.ToKullaniciResim(),
                text = s.Ad + " " + s.Soyad
            }).ToList();

            return tezKontrolSorumlulari.ToJsonResult();
        }
        [Authorize]
        public ActionResult GetFilteredAkademisyen(string term, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            return KullanicilarBus.GetFilterAkademisyenJsonResult(term, enstituKod);
        }
        [Authorize]
        public ActionResult GetUniversiteler(string term)
        {
            var univeriteler = _entities.Universitelers.Where(p => p.UniversiteID != GlobalSistemSetting.UniversiteYtuKod && p.Ad.Contains(term)).OrderBy(o => o.Ad).Take(50).Select(s => new
            {
                id = s.UniversiteID,
                text = s.Ad

            }).ToList();

            return univeriteler.ToJsonResult();
        }
        [Authorize]
        public ActionResult GetUniversitelerYtuDahil(string term)
        {
            var univeriteler = _entities.Universitelers.Where(p => p.Ad.Contains(term)).OrderBy(o => o.Ad).Take(50).Select(s => new
            {
                id = s.UniversiteID,
                text = s.Ad

            }).ToList();

            return univeriteler.ToJsonResult();
        }
        [Authorize]
        public ActionResult GetDxReport(int? raporTipi, bool isPdfStream = false)
        {
            XtraReport rprX = null;
            if (raporTipi.HasValue == false)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Rapor almak için rapor tipinin gönderilmesi gerekmektedir!", ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Hata);
            }
            else
            {
                if (raporTipi == RaporTipiEnum.BasvuruOgrenciListesi)
                {
                    #region BasvuruOgrenciListesi
                    var basvuruSurecId = Request["BasvuruSurecID"].ToIntObj(0);
                    var ogrenimTipKod = Request["OgrenimTipKod"].ToIntObj(0);
                    var alanTipId = Request["AlanTipID"].ToIntObj();
                    var programKod = Request["ProgramKod"].ToStrObjEmptString();

                    using (var entities = new LubsDbEntities())
                    {
                        var q = from s in entities.BasvuruSurecs
                                join b in entities.Basvurulars.Where(p => p.BasvuruDurumID == BasvuruDurumuEnum.Onaylandı) on s.BasvuruSurecID equals b.BasvuruSurecID
                                join t in entities.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruDurumID == BasvuruDurumuEnum.Onaylandı) on b.BasvuruID equals t.BasvuruID
                                join bsOt in entities.BasvuruSurecOgrenimTipleris.Where(p => p.BasvuruSurecID == basvuruSurecId) on t.OgrenimTipKod equals bsOt.OgrenimTipKod
                                join ot in entities.OgrenimTipleris on new { s.EnstituKod, t.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                                join at in entities.AlanTipleris on t.AlanTipID equals at.AlanTipID
                                join pr in entities.Programlars on t.ProgramKod equals pr.ProgramKod
                                join bl in entities.AnabilimDallaris on new { pr.AnabilimDaliKod, s.EnstituKod } equals new { bl.AnabilimDaliKod, bl.EnstituKod }
                                join kot in entities.BasvuruSurecKotalars.Where(p => p.ProgramKod == programKod) on new { s.BasvuruSurecID, t.OgrenimTipKod } equals new { kot.BasvuruSurecID, kot.OgrenimTipKod }
                                where t.ProgramKod == programKod && t.OgrenimTipKod == ogrenimTipKod && s.BasvuruSurecID == basvuruSurecId
                                select new
                                {
                                    bl.AnabilimDaliKod,
                                    bl.AnabilimDaliAdi,
                                    bsOt.MulakatSurecineGirecek,
                                    Kota = kot.OrtakKota ? kot.OrtakKotaSayisi.Value : (t.AlanTipID == AlanTipiEnum.AlanIci ? kot.AlanIciKota : kot.AlanDisiKota),
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
                        if (alanTipId.HasValue) q = q.Where(p => p.AlanTipID == alanTipId);
                        var data = q.OrderBy(o => o.AnabilimDaliAdi).ThenBy(t => t.ProgramAdi).ThenBy(t => t.OgrenimTipAdi).ThenByDescending(t => t.AlanTipAdi).ThenBy(t => t.AdSoyad).Select(s => new RprBasvuruSonucModel
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

                        var rpr = new RprBasvuruMulakatsizOgrenciList(basvuruSurecId);
                        rpr.DataSource = data;
                        var basvuruSurec = _entities.BasvuruSurecs.First(p => p.BasvuruSurecID == basvuruSurecId);
                        rpr.DisplayName = basvuruSurec.BasvuruSurecTipID == BasvuruSurecTipiEnum.LisansustuBasvuru ? "Lisansüstü Başvuran Öğrenci Listesi" : "Lisansüstü Yatay Geçiş Başvuran Öğrenci Listesi";
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFilePageByPage;

                        rprX = rpr;

                    }
                    #endregion
                }
                else if (raporTipi == RaporTipiEnum.BasvuruSonucListesi)
                {
                    #region bSonucListesi
                    var basvuruSurecId = Request["BasvuruSurecID"].ToIntObj(0);
                    var anabilimdaliKod = Request["AnabilimdaliKod"].ToStrObj();
                    var programKod = Request["ProgramKod"].ToStrObj();
                    var ogrenimTipKod = Request["OgrenimTipKod"].ToIntObj();
                    var subRaporTipId = Request["SubRaporTipID"].ToIntObj();
                    var mulakatSonucTipIDstr = Request["MulakatSonucTipID"];
                    mulakatSonucTipIDstr = mulakatSonucTipIDstr.ToStrObjEmptString();
                    var ekBilgiTipId = Request["EkBilgiTipID"].ToIntObj();
                    var kayitDurumId = Request["KayitDurumID"].ToIntObj();
                    var oTips = Request["OgrenimTips"].ToStrObjEmptString();
                    var ogrenimTipKodus = new List<int>();
                    if (!oTips.IsNullOrWhiteSpace()) ogrenimTipKodus = oTips.Split(',').Select(s => s.ToInt(0)).ToList();
                    var mulakatSonucTipId = new List<int>();
                    if (subRaporTipId == 1) mulakatSonucTipIDstr.Split(',').ToList().ForEach((item) => { mulakatSonucTipId.Add(item.ToInt(0)); });
                    else
                    {
                        mulakatSonucTipId.AddRange(new List<int> { MulakatSonucTipiEnum.Asil, MulakatSonucTipiEnum.Yedek, MulakatSonucTipiEnum.Kazanamadı });
                    }


                    using (var entities = new LubsDbEntities())
                    {
                        var id = basvuruSurecId;
                        var q = from s in entities.BasvuruSurecs
                                join ms in entities.MulakatSonuclaris on s.BasvuruSurecID equals ms.BasvuruSurecID
                                join kd in entities.KayitDurumlaris on ms.KayitDurumID equals kd.KayitDurumID into defKd
                                from kd in defKd.DefaultIfEmpty()
                                join t in entities.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruDurumID == BasvuruDurumuEnum.Onaylandı) on ms.BasvuruTercihID equals t.BasvuruTercihID
                                join kt in entities.BasvuruSurecKotalars on new { s.BasvuruSurecID, t.ProgramKod, t.OgrenimTipKod } equals new { kt.BasvuruSurecID, kt.ProgramKod, kt.OgrenimTipKod }
                                join ot in entities.OgrenimTipleris on new { s.EnstituKod, t.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                                join at in entities.AlanTipleris on t.AlanTipID equals at.AlanTipID
                                join pr in entities.Programlars on t.ProgramKod equals pr.ProgramKod
                                join bsOt in entities.BasvuruSurecOgrenimTipleris.Where(p => p.BasvuruSurecID == id) on t.OgrenimTipKod equals bsOt.OgrenimTipKod
                                join bl in entities.AnabilimDallaris on new { pr.AnabilimDaliKod, s.EnstituKod } equals new { bl.AnabilimDaliKod, bl.EnstituKod }
                                join b in entities.Basvurulars on t.BasvuruID equals b.BasvuruID
                                join bst in entities.MulakatSonucTipleris on ms.MulakatSonucTipID equals bst.MulakatSonucTipID
                                where s.BasvuruSurecID == id
                                select new
                                {
                                    s.BasvuruSurecID,
                                    b.RowID,
                                    bl.AnabilimDaliKod,
                                    bl.AnabilimDaliAdi,
                                    bsOt.MulakatSurecineGirecek,
                                    AlanKota = kt.OrtakKota ? (kt.OrtakKotaSayisi.Value + (kt.AlanDisiEkKota ?? 0)) : (t.AlanTipID == AlanTipiEnum.AlanIci ? (kt.AlanIciKota + (kt.AlanIciEkKota ?? 0)) : (kt.AlanDisiKota + (kt.AlanDisiEkKota ?? 0))),
                                    AlanEkKota = kt.OrtakKota ? (kt.AlanDisiEkKota ?? 0) : (t.AlanTipID == AlanTipiEnum.AlanIci ? (kt.AlanIciEkKota ?? 0) : (kt.AlanDisiEkKota ?? 0)),
                                    ms.SinavaGirmediY,
                                    ms.SinavaGirmediS,
                                    pr.ProgramKod,
                                    pr.ProgramAdi,
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
                                    b.EMail,
                                    ms.KayitDurumID,
                                    KayitOldu = kd != null ? kd.IsKayitOldu : (bool?)null

                                };
                        var rowIDstr = Request["RowID"].ToStrObj();
                        var rowId = new Guid();
                        var isOgrenciSonucListesindePuanGozuksun = false;
                        if (!rowIDstr.IsNullOrWhiteSpace())
                        {
                            rowId = new Guid(rowIDstr);
                            q = q.Where(p => p.RowID == rowId);

                            ekBilgiTipId = 2;
                            var basvuru = _entities.Basvurulars.First(p => p.RowID == rowId);
                            isOgrenciSonucListesindePuanGozuksun = basvuru.BasvuruSurec.IsOgrenciSonucListesindePuanGozuksun;
                            basvuruSurecId = basvuru.BasvuruSurecID;
                            var programKods = basvuru.BasvurularTercihleris.Select(s => s.ProgramKod + "_" + s.OgrenimTipKod + "_" + s.AlanTipID).ToList();
                            q = q.Where(p => programKods.Contains(p.ProgramKod + "_" + p.OgrenimTipKod + "_" + p.AlanTipID));
                        }
                        else
                        {
                            if (ogrenimTipKodus.Any())
                            {
                                q = q.Where(p => ogrenimTipKodus.Contains(p.OgrenimTipKod));
                            }
                            if (anabilimdaliKod.IsNullOrWhiteSpace() == false)
                            {
                                q = q.Where(p => p.AnabilimDaliKod == anabilimdaliKod);
                            }
                            if (ogrenimTipKod.HasValue)
                            {
                                q = q.Where(p => p.OgrenimTipKod == ogrenimTipKod);
                            }
                            if (programKod.IsNullOrWhiteSpace() == false)
                            {
                                q = q.Where(p => p.ProgramKod == programKod);
                            }
                            if (kayitDurumId.HasValue)
                            {
                                q = q.Where(p => p.KayitDurumID == kayitDurumId.Value);
                            }
                            q = q.Where(p => mulakatSonucTipId.Contains(p.MulakatSonucTipID));
                        }
                        var data = q.Select(s => new RprBasvuruSonucModel
                        {
                            BasvuruSurecID = s.BasvuruSurecID,
                            RowID = s.RowID,
                            AnabilimDaliKod = s.AnabilimDaliKod,
                            AnabilimDaliAdi = s.AnabilimDaliAdi,
                            ProgramKod = s.ProgramKod,
                            ProgramAdi = s.ProgramAdi,
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
                        }).OrderBy(o => o.AnabilimDaliAdi).ThenBy(t => t.ProgramAdi).ThenBy(t => t.OgrenimTipAdi).ThenBy(t => t.AlanTipAdi).ThenByDescending(t => t.GenelBasariNotu).ThenBy(t => (t.MulakatSonucTipID == MulakatSonucTipiEnum.Asil ? 1 : (t.MulakatSonucTipID == MulakatSonucTipiEnum.Yedek ? 2 : (t.MulakatSonucTipID == MulakatSonucTipiEnum.Kazanamadı ? 3 : 4)))).ToList();


                        if (rowIDstr.IsNullOrWhiteSpace() && subRaporTipId != 1)
                        {
                            var nData = new List<RprBasvuruSonucModel>();
                            var qGrup = q.Select(s => new { s.ProgramKod, s.OgrenimTipKod, s.AlanTipID }).Distinct().ToList();

                            foreach (var item in qGrup)
                            {

                                var dataTumu = data.Where(p => p.BasvuruSurecID == basvuruSurecId &&
                                                                                         p.AlanTipID == item.AlanTipID &&
                                                                                         p.ProgramKod == item.ProgramKod &&
                                                                                         p.OgrenimTipKod == item.OgrenimTipKod &&
                                                                                         (p.MulakatSonucTipID == MulakatSonucTipiEnum.Asil || p.MulakatSonucTipID == MulakatSonucTipiEnum.Yedek)).ToList();
                                var dataYedekler = dataTumu.Where(p => p.MulakatSonucTipID == MulakatSonucTipiEnum.Yedek).OrderBy(o => o.SiraNo).ToList();
                                var ekKota = dataTumu.Count > 0 ? dataTumu.First().EkKota : 0;

                                var toplamAsildenKalan = dataTumu.Count(p => p.MulakatSonucTipID == MulakatSonucTipiEnum.Asil && p.KayitOldu == false);
                                var toplamYedekKayit = dataTumu.Count(p => p.MulakatSonucTipID == MulakatSonucTipiEnum.Yedek && p.KayitOldu == true);
                                var kalan = (toplamAsildenKalan + ekKota) - toplamYedekKayit;


                                if (subRaporTipId == 2)
                                {
                                    nData.AddRange(dataYedekler);
                                }
                                else if (subRaporTipId == 3 && kalan > 0)
                                {
                                    nData.AddRange(dataYedekler.Where(p => p.KayitOldu == null));
                                }
                                else if (subRaporTipId == 4 && kalan > 0)
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

                                item2.SiraNo = rowIDstr.IsNullOrWhiteSpace() ? inx : item2.PSiraNo ?? 0;
                                inx++;
                                if (!rowIDstr.IsNullOrWhiteSpace())
                                {
                                    if (item2.RowID != rowId)
                                    {
                                        var isims = item2.AdSoyad.Split(' ').Where(p => !p.IsNullOrWhiteSpace()).ToList();
                                        var maskAdSoyad = "";
                                        foreach (var itemI in isims)
                                        {
                                            maskAdSoyad += itemI.Substring(0, 1) + "**** ";
                                        }

                                        item2.AdSoyad = maskAdSoyad;
                                        if (!isOgrenciSonucListesindePuanGozuksun) item2.GenelBasariNotu = null;
                                    }
                                }
                            }
                        }




                        if (ekBilgiTipId == 3)
                        {
                            var basvuruSurec = _entities.BasvuruSurecs.First(p => p.BasvuruSurecID == basvuruSurecId);
                            var qSinavOt = basvuruSurec.BasvuruSurecSinavTipleriOTNotAraliklaris.ToList();

                            foreach (var itemP in data)
                            {
                                var sinavYok = qSinavOt.Any(p => p.OgrenimTipKod == itemP.OgrenimTipKod && p.SinavTipleri.SinavTipGrupID == SinavTipGrupEnum.Ales_Gree
                                    && (p.BasvuruSurecSinavTipleriOTNotAraliklariGecersizProgramlars.Any(a => a.ProgramKod == itemP.ProgramKod) || !p.IsGecerli || !p.IsIstensin));

                                if (sinavYok) itemP.AlesNotu = null;
                            }
                            var rpr = new RprBasvuruSonucPuanList(basvuruSurecId);
                            rpr.DataSource = data;
                            rpr.DisplayName = basvuruSurec.BasvuruSurecTipID == BasvuruSurecTipiEnum.LisansustuBasvuru ? "Lisansüstü Başvuru Sonuç Puan Listesi" : "Lisansüstü Yatay Geçiş Başvuru Sonuç Puan Listesi";
                            rpr.PrintingSystem.ContinuousPageNumbering = true;
                            rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                            rprX = rpr;
                        }
                        else
                        {

                            var rpr = new RprBasvuruSonucList(basvuruSurecId, ekBilgiTipId.Value);
                            rpr.DataSource = data;
                            var basvuruSurec = _entities.BasvuruSurecs.First(p => p.BasvuruSurecID == basvuruSurecId);
                            rpr.DisplayName = basvuruSurec.BasvuruSurecTipID == BasvuruSurecTipiEnum.LisansustuBasvuru ? "Lisansüstü Başvuru Sonuç Listesi" : "Lisansüstü Yatay Geçiş Başvuru Sonuç Listesi";
                            rpr.PrintingSystem.ContinuousPageNumbering = true;
                            rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                            rprX = rpr;
                        }



                    }
                    #endregion
                }
                else if (raporTipi == RaporTipiEnum.BasvuruSonucSayisal)
                {
                    #region BasvuruSonucSayisal
                    var basvuruSurecId = Request["BasvuruSurecID"].ToIntObj();
                    if (RoleNames.LisansustuBasvuruRapor.InRoleCurrent() == false) basvuruSurecId = 0;
                    using (var entities = new LubsDbEntities())
                    {
                        var bsurec = entities.BasvuruSurecs.First(p => p.BasvuruSurecID == basvuruSurecId);
                        var qx = (from s in entities.BasvuruSurecs.Where(p => p.BasvuruSurecID == basvuruSurecId && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod))
                                  join enst in entities.Enstitulers on s.EnstituKod equals enst.EnstituKod
                                  join dnm in entities.Donemlers on s.DonemID equals dnm.DonemID
                                  select new RaporLUBModel
                                  {
                                      EnstituAdi = enst.EnstituAd,
                                      AkademikYil = s.BaslangicYil + " / " + s.BitisYil + " " + dnm.DonemAdi,
                                      ToplamTercihSayisi = s.MulakatSonuclaris.Count,
                                      OgrenimTipleri = (from s2 in s.BasvuruSurecOgrenimTipleris.Where(p => p.IsAktif)
                                                        join otl in entities.OgrenimTipleris.Where(p => s.EnstituKod == p.EnstituKod) on s2.OgrenimTipKod equals otl.OgrenimTipKod
                                                        select new RaporOtipModel
                                                        {
                                                            GBNO = s2.BasariNotOrtalamasi,
                                                            OgrenimTipAdi = otl.OgrenimTipAdi,
                                                            TaslakCount = s.Basvurulars.SelectMany(sm => sm.BasvurularTercihleris).Count(c => c.OgrenimTipKod == s2.OgrenimTipKod && c.Basvurular.BasvuruDurumID == BasvuruDurumuEnum.Taslak),
                                                            OnaylananCount = s.Basvurulars.SelectMany(sm => sm.BasvurularTercihleris).Count(c => c.OgrenimTipKod == s2.OgrenimTipKod && c.Basvurular.BasvuruDurumID == BasvuruDurumuEnum.Onaylandı),
                                                            IptalEdilenCount = s.Basvurulars.SelectMany(sm => sm.BasvurularTercihleris).Count(c => c.OgrenimTipKod == s2.OgrenimTipKod && c.Basvurular.BasvuruDurumID == BasvuruDurumuEnum.IptalEdildi),
                                                            KayitCount = s.MulakatSonuclaris.Count(c => c.BasvurularTercihleri.OgrenimTipKod == s2.OgrenimTipKod && c.KayitDurumID.HasValue && c.KayitDurumlari.IsKayitOldu),

                                                        }),
                                      ADToplamModel = new FmMsonucOranModel
                                      {
                                          Toplam = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanDisi),
                                          Kota = s.BasvuruSurecKotalars.Sum(sm => sm.AlanDisiKota),
                                          AsilCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanDisi && c.MulakatSonucTipID == MulakatSonucTipiEnum.Asil),
                                          YedekCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanDisi && c.MulakatSonucTipID == MulakatSonucTipiEnum.Yedek),
                                          KazanamayanCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanDisi && c.MulakatSonucTipID == MulakatSonucTipiEnum.Kazanamadı),
                                          KayitCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanDisi && c.KayitDurumID.HasValue && c.KayitDurumlari.IsKayitOldu),
                                      },
                                      AIToplamModel = new FmMsonucOranModel
                                      {
                                          Toplam = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanIci),
                                          Kota = s.BasvuruSurecKotalars.Sum(sm => sm.AlanIciKota),
                                          AsilCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanIci && c.MulakatSonucTipID == MulakatSonucTipiEnum.Asil),
                                          YedekCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanIci && c.MulakatSonucTipID == MulakatSonucTipiEnum.Yedek),
                                          KazanamayanCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanIci && c.MulakatSonucTipID == MulakatSonucTipiEnum.Kazanamadı),
                                          KayitCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanIci && c.KayitDurumID.HasValue && c.KayitDurumlari.IsKayitOldu),
                                      },
                                      BasvuruSonuclari = (from kt in s.BasvuruSurecKotalars
                                                          join ot in entities.OgrenimTipleris.Where(p => s.EnstituKod == p.EnstituKod) on kt.OgrenimTipKod equals ot.OgrenimTipKod
                                                          join prg in entities.Programlars on kt.ProgramKod equals prg.ProgramKod
                                                          join abd in entities.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                                                          select new FrMulakatSonucDetay
                                                          {
                                                              OgrenimTipAdi = ot.OgrenimTipAdi,
                                                              AnabilimDaliAdi = abd.AnabilimDaliAdi + " / " + prg.ProgramAdi,
                                                              ToplamBasvuru = s.MulakatSonuclaris.Count(c => c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                              AIKota = kt.OrtakKota == true ? 0 : kt.AlanIciKota,
                                                              AIKayitCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanIci && c.KayitDurumID.HasValue && c.KayitDurumlari.IsKayitOldu && c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                              AIAsilCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanIci && c.MulakatSonucTipID == MulakatSonucTipiEnum.Asil && c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                              AIYedekCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanIci && c.MulakatSonucTipID == MulakatSonucTipiEnum.Yedek && c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                              AIKazanamayanCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanIci && c.MulakatSonucTipID == MulakatSonucTipiEnum.Kazanamadı && c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                              ADKota = kt.OrtakKota == true ? kt.OrtakKotaSayisi.Value : kt.AlanDisiKota,
                                                              ADKayitCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanDisi && c.KayitDurumID.HasValue && c.KayitDurumlari.IsKayitOldu && c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                              ADAsilCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanDisi && c.MulakatSonucTipID == MulakatSonucTipiEnum.Asil && c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                              ADYedekCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanDisi && c.MulakatSonucTipID == MulakatSonucTipiEnum.Yedek && c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                              ADKazanamayanCount = s.MulakatSonuclaris.Count(c => c.AlanTipID == AlanTipiEnum.AlanDisi && c.MulakatSonucTipID == MulakatSonucTipiEnum.Kazanamadı && c.BasvurularTercihleri.ProgramKod == kt.ProgramKod && c.BasvurularTercihleri.OgrenimTipKod == kt.OgrenimTipKod),
                                                          }).OrderBy(o => o.AnabilimDaliAdi)

                                  });

                        var data = qx.ToList();
                        foreach (var itemD in data)
                        {
                            itemD.EnstituAdi = itemD.EnstituAdi.ToUpper();
                            itemD.SurecTarihi = bsurec.BaslangicTarihi.ToFormatDateAndTime() + " / " + bsurec.BitisTarihi.ToFormatDateAndTime();
                            var toplmMdl = new List<FmMsonucOranModel>
                            {
                                itemD.AIToplamModel,
                                itemD.ADToplamModel
                            };
                            foreach (var item in toplmMdl)
                            {
                                item.ToplamYuzde = item.Toplam * 100.0 / itemD.ToplamTercihSayisi;
                                item.AsilYuzde = item.AsilCount * 100.0 / itemD.ToplamTercihSayisi;
                                item.YedekYuzde = item.YedekCount * 100.0 / itemD.ToplamTercihSayisi;
                                item.KazanamayanYuzde = item.KazanamayanCount * 100.0 / itemD.ToplamTercihSayisi;
                                item.KayitYuzde = item.KayitCount * 100.0 / toplmMdl.Sum(s => s.KayitCount);
                            }
                        }

                        var rpr = new raporLUB(bsurec.BasvuruSurecTipID);
                        rpr.DataSource = data;
                        switch (bsurec.BasvuruSurecTipID)
                        {
                            case BasvuruSurecTipiEnum.LisansustuBasvuru:
                                rpr.DisplayName = "Lisansüstü Başvuru Süreci Sayısal Bilgisi";
                                break;
                            case BasvuruSurecTipiEnum.YatayGecisBasvuru:
                                rpr.DisplayName = "Lisansüstü Yatay Geçiş Başvuru Süreci Sayısal Bilgisi";
                                break;
                            default:
                                rpr.DisplayName = "YTÜ Yeni Mezun DR Başvuru Süreci Sayısal Bilgi";
                                break;
                        }

                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                        rprX = rpr;

                    }

                    #endregion
                }
                else if (raporTipi == RaporTipiEnum.BelgeTalepSayisal)
                {
                    var baslangicT = Request["T1"].ToStrObj();
                    var bitisT = Request["T2"].ToStrObj();
                    var eKod = Request["eKod"].ToStrObj();
                    var yilModel = new List<int>();
                    var yilAyModel = new List<RaporBTSayisalModel>();
                    using (var entities = new LubsDbEntities())
                    {
                        var btipdetayIds = _entities.BelgeTipDetayBelgelers.Where(p => p.BelgeTipDetay.IsAktif).Select(s => s.BelgeTipID).Distinct();
                        if (RoleNames.BelgeTalepleriRapor.InRoleCurrent())
                        {

                            var bTips = _entities.BelgeTipleris.Where(p => btipdetayIds.Contains(p.BelgeTipID) && p.IsAktif).ToList();
                            var t1 = Convert.ToDateTime(baslangicT + "-01");
                            var t2 = Convert.ToDateTime(bitisT + "-01");
                            for (DateTime i = t1; i <= t2; i = i.AddMonths(1))
                            {

                                foreach (var item in bTips)
                                {
                                    yilAyModel.Add(new RaporBTSayisalModel { Yil = i.Year, Ay = i.Month, BelgeTipID = item.BelgeTipID, BelgeTipAdi = item.BelgeTipAdi });
                                }
                            }
                            yilModel = yilAyModel.Select(s => s.Yil).Distinct().ToList();
                        }

                        var data = (from s in entities.Enstitulers
                                    where s.EnstituKod == eKod
                                    select new RaporBTModel
                                    {
                                        EnstituAdi = s.EnstituAd,
                                        SurecTarihi = baslangicT + " / " + bitisT,

                                        YilaGoreToplam = (from ya in yilModel
                                                          select new RaporBTSayisalModel
                                                          {
                                                              Yil = ya,
                                                              Toplam = _entities.BelgeTalepleris.Count(p => btipdetayIds.Contains(p.BelgeTipID) && p.TalepTarihi.Year == ya),
                                                              TalepEdilen = _entities.BelgeTalepleris.Count(p => btipdetayIds.Contains(p.BelgeTipID) && new List<int> { BelgeTalepDurumEnum.TalepEdildi, BelgeTalepDurumEnum.Hazirlandi, BelgeTalepDurumEnum.Hazirlaniyor }.Contains(p.BelgeDurumID) && p.TalepTarihi.Year == ya),
                                                              Verilen = _entities.BelgeTalepleris.Count(p => btipdetayIds.Contains(p.BelgeTipID) && p.BelgeDurumID == BelgeTalepDurumEnum.Verildi && p.TalepTarihi.Year == ya),
                                                              Kapatilan = _entities.BelgeTalepleris.Count(p => btipdetayIds.Contains(p.BelgeTipID) && p.BelgeDurumID == BelgeTalepDurumEnum.Kapatildi && p.TalepTarihi.Year == ya),
                                                              IptalEdilen = _entities.BelgeTalepleris.Count(p => btipdetayIds.Contains(p.BelgeTipID) && p.BelgeDurumID == BelgeTalepDurumEnum.IptalEdildi && p.TalepTarihi.Year == ya),

                                                          }
                                                        ).OrderBy(o => o.Yil),
                                    }).ToList();
                        foreach (var item in data)
                        {
                            item.EnstituAdi = item.EnstituAdi.ToUpper();
                            item.DetayliToplam = (from ya in yilAyModel
                                                  select new RaporBTSayisalModel
                                                  {
                                                      Yil = ya.Yil,
                                                      Ay = ya.Ay,
                                                      BelgeTipAdi = ya.BelgeTipAdi,
                                                      Toplam = _entities.BelgeTalepleris.Count(p => p.BelgeTipID == ya.BelgeTipID && p.TalepTarihi.Year == ya.Yil && p.TalepTarihi.Month == ya.Ay),
                                                      TalepEdilen = _entities.BelgeTalepleris.Count(p => new List<int> { BelgeTalepDurumEnum.TalepEdildi, BelgeTalepDurumEnum.Hazirlandi, BelgeTalepDurumEnum.Hazirlaniyor }.Contains(p.BelgeDurumID) && p.BelgeTipID == ya.BelgeTipID && p.TalepTarihi.Year == ya.Yil && p.TalepTarihi.Month == ya.Ay),
                                                      Verilen = _entities.BelgeTalepleris.Count(p => p.BelgeDurumID == BelgeTalepDurumEnum.Verildi && p.BelgeTipID == ya.BelgeTipID && p.TalepTarihi.Year == ya.Yil && p.TalepTarihi.Month == ya.Ay),
                                                      Kapatilan = _entities.BelgeTalepleris.Count(p => p.BelgeDurumID == BelgeTalepDurumEnum.Kapatildi && p.BelgeTipID == ya.BelgeTipID && p.TalepTarihi.Year == ya.Yil && p.TalepTarihi.Month == ya.Ay),
                                                      IptalEdilen = _entities.BelgeTalepleris.Count(p => p.BelgeDurumID == BelgeTalepDurumEnum.IptalEdildi && p.BelgeTipID == ya.BelgeTipID && p.TalepTarihi.Year == ya.Yil && p.TalepTarihi.Month == ya.Ay),

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
                else if (raporTipi == RaporTipiEnum.MezuniyetBasvuruRaporu)
                {
                    var basvId = Request["MezuniyetBasvurulariID"].ToIntObj(0);
                    var rpr = new RprMezuniyetYayinSartiOnayiFormu(basvId);

                    rpr.PrintingSystem.ContinuousPageNumbering = true;
                    rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                    rprX = rpr;
                }
                else if (raporTipi == RaporTipiEnum.Anketler)
                {
                    var anketId = Request["AnketID"].ToIntObj();
                    var basTar = Request["BasTar"].ToDate();
                    var bitTar = Request["BitTar"].ToDate();
                    if (RoleNames.AnketlerRapor.InRoleCurrent() == false) anketId = 0;

                    var anket = _entities.Ankets.First(p => p.AnketID == anketId);
                    var enstitu = anket.Enstituler;

                    var anketSorularis = anket.AnketSorus.ToList();
                    var anketSoruSecenek = _entities.AnketSoruSeceneks.Where(p => p.AnketSoru.AnketID == anketId).ToList();
                    var cevaplar = _entities.AnketCevaplaris.Where(p => p.AnketID == anketId && p.Tarih >= basTar && p.Tarih <= bitTar).ToList();
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
                                                                 Count = cevaplar.Count(p => p.AnketSoruSecenekID == ss.AnketSoruSecenekID),
                                                                 AnketCevaplaris = cevaplar.Where(p => p.AnketSoruSecenekID == ss.AnketSoruSecenekID).ToList(),

                                                             }
                                                           ).OrderBy(o => o.SiraNo).ToList(),
                                      AnketCevaplaris = cevaplar.Where(p => p.AnketSoruID == sa.AnketSoruID).ToList()
                                  }).OrderBy(o => o.SiraNo).ToList();



                    foreach (var item in qModel)
                    {
                        if (item.IsTabloVeriGirisi)
                        {

                            var tblRw = new AnketTableDetayDto
                            {
                                SiraNo = "#"
                            };
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
                            foreach (var item2 in item.FrAnketSecenekDetay)
                            {

                                item.AnketSeceneklerDetays.Add(new AnketSeceneklerDetayDto
                                {
                                    EkAciklamas = item2.AnketCevaplaris.Where(p => !p.EkAciklama.IsNullOrWhiteSpace()).ToList(),
                                    SiraNo = item2.SiraNo,
                                    SecenekAdi = item2.SecenekAdi,
                                    Count = item2.Count,
                                });
                            }
                        }
                    }

                    var rpr = new RprAnket(enstitu.EnstituAd, anket.AnketAdi, basTar.ToFormatDate() + " - " + bitTar.ToFormatDate() + " Tarih aralığındaki anket sonuçları");
                    rpr.DataSource = qModel;
                    rpr.DisplayName = basTar.ToFormatDate() + " - " + bitTar.ToFormatDate() + " Tarih aralığındaki " + anket.AnketAdi + " anket sonuçları";
                    rpr.PrintingSystem.ContinuousPageNumbering = true;
                    rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                    rprX = rpr;
                }
                else if (raporTipi == RaporTipiEnum.MezuniyetCiltFormuRaporu)
                {
                    var formId = Request["ID"].ToIntObj(0);
                    var rpr = new RprMezuniyetCiltliTezTeslimFormu_FR1243(formId);
                    rpr.PrintingSystem.ContinuousPageNumbering = true;
                    rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                    rprX = rpr;
                }
                else if (raporTipi == RaporTipiEnum.MezuniyetJuriOneriFormuRaporu)
                {
                    var mezuniyetBasvurulariId = Request["ID"].ToIntObj(0);
                    var rpr = new RprMezuniyetTezJuriOneriFormu_FR0300_FR0339(mezuniyetBasvurulariId);
                    rpr.PrintingSystem.ContinuousPageNumbering = true;
                    rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;

                    rprX = rpr;
                }
                else if (raporTipi == RaporTipiEnum.MezuniyetEykSavunmaJurisiAtanmistirYazisi)
                {
                    var mezuniyetBasvurulariId = Request["ID"].ToIntObj(0);
                    rprX = MezuniyetBus.MezuniyetSavunmaJurisiAtanmistirYazilari(mezuniyetBasvurulariId);
                }
                else if (raporTipi == RaporTipiEnum.MezuniyetDrSinavBilgilendirmeYazilari)
                {
                    var srTalepId = Request["ID"].ToIntObj(0);
                    rprX = SrTalepleriBus.MezuniyetSinavSureciDoktoraSinavBilgilendirmeYazilari(srTalepId);
                }
                else if (raporTipi == RaporTipiEnum.MezuniyetIkinciTezTeslimTaahhutOnayYazilari)
                {
                    var srTalepId = Request["ID"].ToIntObj(0);
                    rprX = MezuniyetBus.MezuniyetIkinciTezTeslimTaahhutOnayYazilari(srTalepId);
                }
                else if (raporTipi == RaporTipiEnum.MezuniyetTezTeslimFormu)
                {
                    var ilkTeslim = Request["IlkTeslim"].ToBooleanObj() ?? false;
                    var uniqueId = new Guid(Request["UniqueID"]);
                    var mezuniyetBasvuru = _entities.MezuniyetBasvurularis.First(f => f.RowID == uniqueId);
                    var rpr = new RprMezuniyetTezTeslimFormu_FR0338(mezuniyetBasvuru.MezuniyetBasvurulariID, ilkTeslim);

                    rprX = rpr;

                }
                else if (raporTipi == RaporTipiEnum.MezuniyetTezSinavSonucFormu)
                {
                    var uniqueId = new Guid(Request["UniqueID"]);
                    var srTalebi = _entities.SRTalepleris.First(p => p.UniqueID == uniqueId);
                    var mezuniyetBasvurusu = srTalebi.MezuniyetBasvurulari;
                    var rpr = new RprTezSinavSonucTutanagi_FR0342_FR0377(srTalebi.SRTalepID);
                    rpr.CreateDocument();
                    if (mezuniyetBasvurusu.TezDanismanID == UserIdentity.Current.Id || RoleNames.MezuniyetGelenBasvurularSrTalebiYap.InRoleCurrent() || RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent())
                    {
                        var rpr2 = new RprTezSinavSonucTutanagi_Detay(srTalebi.SRTalepID);
                        rpr2.CreateDocument();
                        rpr.Pages.AddRange(rpr2.Pages);
                    }
                    rprX = rpr;

                }
                else if (raporTipi == RaporTipiEnum.MezuniyetJuriUyelerineTezTeslimFormu)
                {
                    var mezuniyetJuriOneriFormId = Request["MezuniyetJuriOneriFormID"].ToInt(0);
                    var rpr = new RprJuriUyelerineTezTeslimFormu_FR0341_FR0302(mezuniyetJuriOneriFormId);

                    rprX = rpr;

                }
                else if (raporTipi == RaporTipiEnum.MezuniyetTezdenUretilenYayinlariDegerlendirmeFormu)
                {
                    var mezuniyetJuriOneriFormId = Request["MezuniyetJuriOneriFormID"].ToInt(0);
                    var mezuniyetJuriOneriFormuJuriId = Request["MezuniyetJuriOneriFormuJuriID"].ToInt();
                    var rpr = new RprMezuniyetTezdenUretilenYayinlariDegerlendirmeFormu_FR0304(mezuniyetJuriOneriFormId, mezuniyetJuriOneriFormuJuriId);

                    rprX = rpr;

                }
                else if (raporTipi == RaporTipiEnum.MezuniyetDoktoraTezDegerlendirmeFormu)
                {
                    var mezuniyetJuriOneriFormId = Request["MezuniyetJuriOneriFormID"].ToInt(0);
                    var mezuniyetJuriOneriFormuJuriId = Request["MezuniyetJuriOneriFormuJuriID"].ToInt();
                    var rpr = new RprMezuniyetTezDegerlendirmeFormu_FR0303(mezuniyetJuriOneriFormId, mezuniyetJuriOneriFormuJuriId);

                    rprX = rpr;

                }
                else if (raporTipi == RaporTipiEnum.MezuniyetTezKontrolFormu)
                {
                    var uniqueId = new Guid(Request["ID"]);
                    var rpr = new RprMezuniyetTezKontrolFormu(uniqueId, null);
                    rpr.CreateDocument();
                    rprX = rpr;
                }
                else if (raporTipi == RaporTipiEnum.TezIzlemeDegerlendirmeFormu)
                {
                    var uniqueId = new Guid(Request["UniqueID"]);
                    var rapor = _entities.TIBasvuruAraRapors.First(p => p.UniqueID == uniqueId);
                    var kul = rapor.TIBasvuru.Kullanicilar;

                    var rpr = new RprTiDegerlendirmeFormu_FR0307(rapor.TIBasvuruAraRaporID);
                    rpr.CreateDocument();
                    rpr.DisplayName = kul.Ad + " " + kul.Soyad + " " + rpr.DisplayName;


                    if (rapor.TIBasvuru.KullaniciID != UserIdentity.Current.Id || RoleNames.TiTezDegerlendirmeYap.InRoleCurrent() || RoleNames.TiTezDegerlendirmeDuzeltme.InRoleCurrent())
                    {
                        var rpr2 = new RprTiDegerlendirmeFormuDetay_FR0307(rapor.TIBasvuruAraRaporID);
                        rpr2.CreateDocument();
                        rpr2.DisplayName = rpr2.DisplayName;
                        rpr.Pages.AddRange(rpr2.Pages);
                    }
                    rprX = rpr;
                }
                else if (raporTipi == RaporTipiEnum.TezOneriSavunmaFormu)
                {
                    var uniqueId = new Guid(Request["UniqueID"]);
                    var rapor = _entities.ToBasvuruSavunmas.First(p => p.UniqueID == uniqueId);
                    var kul = rapor.ToBasvuru.Kullanicilar;

                    var rpr = new RprToSavunmaFormu_FR0348(rapor.ToBasvuruSavunmaID);
                    rpr.CreateDocument();
                    rpr.DisplayName = kul.Ad + " " + kul.Soyad + " " + rpr.DisplayName;


                    if (RoleNames.TosDegerlendirmeYap.InRoleCurrent() || RoleNames.TosDegerlendirmeDuzeltme.InRoleCurrent())
                    {
                        var rpr2 = new RprToSavunmaFormuDetay_FR0348(rapor.ToBasvuruSavunmaID);
                        rpr2.CreateDocument();
                        rpr2.DisplayName = rpr2.DisplayName;
                        rpr.Pages.AddRange(rpr2.Pages);
                    }
                    rprX = rpr;
                }
                else if (raporTipi == RaporTipiEnum.TezOneriSavunmaAraRaporIstemiFormu)
                {
                    var uniqueId = new Guid(Request["UniqueID"]);
                    var rapor = _entities.ToBasvuruSavunmas.First(p => p.UniqueID == uniqueId);
                    rprX = TosBus.TezOneriAraRaporIstemiYazilari(rapor.ToBasvuruSavunmaID);


                }
                else if (raporTipi == RaporTipiEnum.TezDanismanOneriFormu)
                {
                    var uniqueId = new Guid(Request["UniqueID"]);
                    var rapor = _entities.TDOBasvuruDanismen.First(p => p.UniqueID == uniqueId);
                    var ogrenci = rapor.TDOBasvuru.Kullanicilar;
                    var rpr = new RprTezDanismaniOneriFormu_FR0347(rapor.TDOBasvuruDanismanID);
                    rpr.CreateDocument();
                    rpr.DisplayName = ogrenci.Ad + " " + ogrenci.Soyad + " " + rpr.DisplayName;

                    rprX = rpr;
                }
                else if (raporTipi == RaporTipiEnum.TezDanismanDegisiklikFormu)
                {
                    var uniqueId = new Guid(Request["UniqueID"]);
                    var rapor = _entities.TDOBasvuruDanismen.First(p => p.UniqueID == uniqueId);

                    var ogrenci = rapor.TDOBasvuru.Kullanicilar;
                    var rpr = new RprTezDanismaniDegisiklikFormu_FR0308(rapor.TDOBasvuruDanismanID);
                    rpr.CreateDocument();
                    rpr.DisplayName = ogrenci.Ad + " " + ogrenci.Soyad + " " + rpr.DisplayName;
                    rprX = rpr;
                }
                else if (raporTipi == RaporTipiEnum.TezEsDanismanOneriFormu)
                {
                    var uniqueId = new Guid(Request["UniqueID"]);
                    var rapor = _entities.TDOBasvuruEsDanismen.First(p => p.UniqueID == uniqueId);
                    var ogrenci = rapor.TDOBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var rpr = new RprTezEsDanismaniOneriFormu_FR0320(rapor.TDOBasvuruEsDanismanID);
                    rpr.CreateDocument();
                    rpr.DisplayName = ogrenci.Ad + " " + ogrenci.Soyad + " " + rpr.DisplayName;
                    rprX = rpr;
                }
                else if (raporTipi == RaporTipiEnum.YeterlikDoktoraSinavSonucFormu)
                {
                    var uniqueId = new Guid(Request["UniqueID"]);
                    var rapor = _entities.YeterlikBasvurus.First(p => p.UniqueID == uniqueId);
                    var rpr = new RprDrYeterlikSinavDegerlendirmeFormu_FR1227(rapor.YeterlikBasvuruID);
                    rpr.CreateDocument();
                    rpr.DisplayName = rapor.Kullanicilar.Ad + " " + rapor.Kullanicilar.Soyad + " " + rpr.DisplayName;
                    rprX = rpr;
                }
                else if (raporTipi == RaporTipiEnum.YeterlikKomiteAtamaGereklilikFormu)
                {
                    var uniqueId = new Guid(Request["UniqueID"]);
                    var yeterlikBasvuru = _entities.YeterlikBasvurus.First(p => p.UniqueID == uniqueId);
                    rprX = YeterlikBus.KomiteAtamaBilgilendirmeYazilari(yeterlikBasvuru.YeterlikBasvuruID);


                }
                else if (raporTipi == RaporTipiEnum.TezIzlemeJuriOneriFormu)
                {
                    var uniqueId = new Guid(Request["UniqueID"]);
                    var rapor = _entities.TijBasvuruOneris.First(p => p.UniqueID == uniqueId);
                    var kul = rapor.TijBasvuru.Kullanicilar;

                    if (rapor.TijFormTipID == TijFormTipiEnum.YeniForm)
                    {
                        var rpr = new RprTijOneriFormu_FR0306(rapor.TijBasvuruOneriID);
                        rpr.CreateDocument();
                        rpr.DisplayName = kul.Ad + " " + kul.Soyad + " " + rpr.DisplayName;
                        rprX = rpr;

                    }
                    else
                    {
                        var rpr = new RprTijDegisiklikFormu_FR1460(rapor.TijBasvuruOneriID);
                        rpr.CreateDocument();
                        rpr.DisplayName = kul.Ad + " " + kul.Soyad + " " + rpr.DisplayName;
                        rprX = rpr;
                    }


                }
                else if (raporTipi == RaporTipiEnum.TezIzlemeKomiteAtamaBilgilendirmeYazilari)
                {
                    var uniqueId = new Guid(Request["UniqueID"]);
                    var rapor = _entities.TijBasvuruOneris.First(p => p.UniqueID == uniqueId);
                    rprX = TijBus.TiKomiteAtamaBilgilendirmeYazilari(rapor.TijBasvuruOneriID);


                }
                else if (raporTipi == RaporTipiEnum.TezIzlemeKomiteAtamaToIkinciSavunmaBilgilendirmeYazilari)
                {
                    var uniqueId = new Guid(Request["UniqueID"]);
                    var rapor = _entities.TijBasvuruOneris.First(p => p.UniqueID == uniqueId);
                    rprX = TijBus.TiKomiteAtamaToIkinciSavunmaBilgilendirmeYazilari(rapor.TijBasvuruOneriID);


                }
                else if (raporTipi == RaporTipiEnum.DonemProjesiSinaviDegerlendirmeFormu)
                {
                    var uniqueId = new Guid(Request["UniqueID"]);
                    var rapor = _entities.DonemProjesiBasvurus.First(p => p.UniqueID == uniqueId);

                    var rpr = new RprDpSinavTutanakFormu_FR0366(rapor.DonemProjesiBasvuruID);
                    rpr.CreateDocument();
                    rpr.DisplayName = rpr.DisplayName;


                    if (rapor.DonemProjesi.KullaniciID != UserIdentity.Current.Id || RoleNames.DonemProjesiSinavDegerlendirmeDuzeltme.InRoleCurrent())
                    {
                        var rpr2 = new RprDpSinavTutanakFormuDetay_FR0366(rapor.DonemProjesiBasvuruID);
                        rpr2.CreateDocument();
                        rpr2.DisplayName = rpr2.DisplayName;
                        rpr.Pages.AddRange(rpr2.Pages);
                    }
                    rprX = rpr;
                }
            }
            if (!isPdfStream) return View(rprX);
            if (rprX == null) return null;
            var memoryStream = new MemoryStream();
            rprX.ExportToPdf(memoryStream);
            rprX.ExportOptions.Pdf.Compressed = true;
            memoryStream.Seek(0, SeekOrigin.Begin);
            Response.AddHeader("Content-Disposition", "inline;filename=\"" + rprX.DisplayName + ".pdf\"");
            return new FileStreamResult(memoryStream, "application/pdf");
        }



        protected override void Dispose(bool disposing)
        {
            _entities.Dispose();
            base.Dispose(disposing);
        }


    }
}
