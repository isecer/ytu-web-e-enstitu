
using BiskaUtil;
using CaptchaMvc.HtmlHelpers;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Filters;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Controllers
{
     
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class AccountController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Login(bool? logout, string dlgId, string returnUrl)
        {
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgId.IsNullOrWhiteSpace(),
                DialogID = dlgId
            };
            if (logout == true)
            {
                FormsAuthenticationUtil.SignOut();
                return RedirectToAction("Index", "Home");
            }
            else if (UserIdentity.Current.IsAuthenticated) return RedirectToAction("Index", "Home");
            ViewBag.UserName = "";
            mmMessage.ReturnUrl = returnUrl;
            ViewBag.MmMessage = mmMessage;
            return PartialView();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string userName, string password, string captchaInputText, bool? rememberMe, string returnUrl, string dlgId)
        {


            var mmMessage = new MmMessage
            {
                IsDialog = !dlgId.IsNullOrWhiteSpace(),
                DialogID = dlgId,
                ReturnUrl = returnUrl
            };
            ViewBag.UserName = userName;
            ViewBag.Password = password;
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
                    Kullanicilar loginUser = null;
                    if (user != null)
                    {
                        if (user.IsActiveDirectoryUser == false)
                        {
                            loginUser = UserBus.Login(userName, password);
                        }
                        else
                        {
                            LdapService.SecureSoapClient ld = new LdapService.SecureSoapClient();

                            var wsPwd = ConfigurationManager.AppSettings["ldapServicePassword"];
                            var isSueccess = ld.Login(userName, password, wsPwd);
                            if (isSueccess)
                            {
                                loginUser = user;
                            }
                            else
                            {
                                msg = "Active Directory Kontrolünden Geçilemedi!";
                                // Management.SistemBilgisiKaydet("Active Directory Kontrolünden Geçilemedi! Kullanıcı Adı: " + UserName, "Acconunt/Login", BilgiTipi.LoginHatalari, null, UserIdentity.Ip);
                            }
                        }

                        if (loginUser != null && loginUser.IsAktif == true)
                        {
                            try
                            {
                                var lastTdo = _entities.TDOBasvurus.OrderByDescending(p => p.KullaniciID == loginUser.KullaniciID).FirstOrDefault();
                                if (lastTdo != null) TezDanismanOneriBus.GetSecilenBasvuruTdoDetay(lastTdo.TDOBasvuruID, null);
                            }
                            catch (Exception ex)
                            {
                                // ignored
                            }

                            rememberMe = rememberMe ?? false;
                            FormsAuthenticationUtil.SetAuthCookie(user.KullaniciAdi, "", rememberMe.Value);
                            UserBus.SetLastLogon();
                            mmMessage.IsCloseDialog = true;
                            if (mmMessage.IsDialog)
                            {
                                if (returnUrl.IsNullOrWhiteSpace()) mmMessage.ReturnUrl = Url.Action("Index", "Home");
                            }
                            else
                            {
                                if (returnUrl.IsNullOrWhiteSpace()) return RedirectToAction("Index", "Home");
                                else return Redirect(returnUrl);
                            }

                        }
                        else
                        {
                            #region default user
                            //if (loginUser == null && UserName == "admin")
                            //{
                            //    Management.CreateAdmin();
                            //}
                            #endregion
                            if (loginUser != null && !loginUser.IsAktif) hata = "Kullanıcı Hesabı Pasif Durumda!";
                            else hata = "Kullanıcı Adı veya Şifre Hatalı. " + msg;
                        }
                    }
                    else
                    {
                        //  Management.SistemBilgisiKaydet("Kullanıcı Sistemde Bulunamadı! Kullanıcı Adı: " + UserName, "Acconunt/Login", BilgiTipi.LoginHatalari, null, UserIdentity.Ip);
                        hata = "Kullanıcı sistemde bulunamadı.";
                    }
                }

            }
            catch (Exception ex)
            {
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Sisteme Giriş Yapılırken Bir Hata Oluştu! Hata: " + ex.ToExceptionMessage());
                hata = "Sisteme Giriş Yapılırken Bir Hata Oluştu! Hata: " + ex.ToExceptionMessage();
            }
            ViewBag.Hata = hata;
            ViewBag.MmMessage = mmMessage;
            return PartialView();

        }
        [Authorize(Roles = RoleNames.KullanicilarOnlineKullanicilar)]
        public ActionResult OnlineUserCnt()
        {
            var users = OnlineUsersHelper.GetUsers;
            return users.Count().ToJsonResult();
        }

        [Authorize(Roles = RoleNames.KullanicilarOnlineKullanicilar)]
        public ActionResult GetOnlineUserList()
        {
            var users = OnlineUsersHelper.GetUsers.ToList();
            return View(users);
        }



        public ActionResult ParolaSifirla(string psKod, int? kullaniciId = null, string dlgId = "")
        {

            MmMessage msg = new MmMessage
            {
                ReturnUrlTimeOut = 4000,
                IsDialog = !dlgId.IsNullOrWhiteSpace(),
                DialogID = dlgId
            };
            if (psKod.IsNullOrWhiteSpace() && kullaniciId.HasValue == false) return RedirectToAction("Index", "Home");

            var kul = new Kullanicilar();
            if (kullaniciId.HasValue == false)
            {
                kul = _entities.Kullanicilars.FirstOrDefault(p => p.IsAktif && p.ParolaSifirlamaKodu == psKod);

                if (kul != null)
                {
                    kul.ResimAdi = kul.ResimAdi.ToKullaniciResim();
                    if (kul.ParolaSifirlamGecerlilikTarihi.HasValue && kul.ParolaSifirlamGecerlilikTarihi.Value < DateTime.Now)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Parola Sıfırlama linkinin geçerlilik süresi dolmuştur!");
                        msg.ReturnUrl = Url.Action("Index", "Home");
                    }
                }
                else
                {
                    msg.IsSuccess = false;
                    msg.Messages.Add("Şifre sıfırlama linki herhangi bir kullanıcıya eşleştirilemedi!");
                    msg.ReturnUrl = Url.Action("Index", "Home");


                }
            }
            else
            {
                if (UserIdentity.Current.IsAuthenticated)
                {
                    kullaniciId = UserIdentity.Current.Id;
                    kul = _entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == kullaniciId);
                    if (kul != null)
                    {
                        kul.ResimAdi = kul.ResimAdi.ToKullaniciResim();
                    }
                }
                else
                {
                    msg.IsSuccess = false;
                    msg.IsCloseDialog = true;
                    msg.Messages.Add("Lütfen Giriş Yapın");
                    msg.ReturnUrl = Url.Action("Index", "Home");

                }
            }
            Session["ShwMesaj"] = msg;
            ViewBag.MmMessage = msg;
            ViewBag.KullaniciID = kullaniciId;
            ViewBag.EskiSifre = "";
            ViewBag.YeniSifre = "";
            ViewBag.YeniSifreTekrar = "";
            return View(kul);
        }
        [HttpPost]
        public ActionResult ParolaSifirla(string psKod, string eskiSifre, string yeniSifre, string yeniSifreTekrar, int? kullaniciId = null, string dlgId = "")
        {
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgId.IsNullOrWhiteSpace(),
                DialogID = dlgId,
                ReturnUrlTimeOut = 4000
            };
            if (psKod.IsNullOrWhiteSpace() == true)
            {
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.Title = "Şifre değiştirme işlemi başarısız!";
                mmMessage.ReturnUrl = Url.Action("Index", "Home");
            }

            var kul = kullaniciId.HasValue ? _entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == kullaniciId)
                : _entities.Kullanicilars.FirstOrDefault(p => p.ParolaSifirlamaKodu == psKod);
            if (kul != null)
            {
                if (kullaniciId.HasValue == false)
                    if (kul.ParolaSifirlamGecerlilikTarihi.HasValue && kul.ParolaSifirlamGecerlilikTarihi.Value < DateTime.Now)
                    {
                        mmMessage.MessageType = Msgtype.Error;
                        mmMessage.Messages.Add("Parola Sıfırlama linkinin geçerlilik süresi dolmuştur!");
                        mmMessage.ReturnUrl = Url.Action("Index", "Home");
                    }
                if (kullaniciId.HasValue)
                {
                    if (eskiSifre.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Varolan şifrenizi giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EskiSifre" });
                    }
                    else if (kul.Sifre != eskiSifre.ComputeHash(Management.Tuz))
                    {
                        mmMessage.Messages.Add("Varolan şifrenizi yanlış girdiniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EskiSifre" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EskiSifre" });
                }
                if (mmMessage.Messages.Count == 0)
                {

                    if (yeniSifre.Length < 4)
                    {
                        mmMessage.Messages.Add("Yeni şifreniz en az 4 haneli olmalıdır!");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifre" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YeniSifre" });
                    if (yeniSifreTekrar.Length < 4)
                    {
                        mmMessage.Messages.Add("Yeni şifre tekrar en az 4 haneli olmalıdır!");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifreTekrar" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifreTekrar" });
                    if (mmMessage.Messages.Count == 0)
                    {
                        if (yeniSifreTekrar != yeniSifre)
                        {
                            mmMessage.Messages.Add("Yeni şifre ile yeni şifre tekrar birbiriyle uyuşmuyor!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifreTekrar" });
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifre" });
                        }
                        else
                        {
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YeniSifre" });
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YeniSifreTekrar" });
                        }
                    }
                }

                if (mmMessage.Messages.Count == 0)
                {
                    kul.Sifre = yeniSifreTekrar.ComputeHash(Management.Tuz);
                    kul.ParolaSifirlamGecerlilikTarihi = DateTime.Now;
                    _entities.SaveChanges();
                    mmMessage.MessageType = Msgtype.Success;
                    mmMessage.Title = "Şifre değiştirme işlemi";
                    if (kullaniciId.HasValue == false)
                    {
                        mmMessage.Messages.Add("Şifreniz değiştirildi! Giriş sayfasına yönlendiriliyorsunuz...");
                        mmMessage.ReturnUrl = Url.Action("Login", "Account");
                    }
                    else
                    {
                        mmMessage.IsCloseDialog = true;
                        mmMessage.Messages.Add("Şifreniz değiştirildi!");
                    }
                }
                else
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.Title = "Şifre değiştirme işlemi başarısız!";
                }
                kul.ResimAdi = kul.ResimAdi.ToKullaniciResim();
            }
            else
            {
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.Title = "Şifre değiştirme işlemi başarısız!";
            }
            if (mmMessage.Messages.Count > 0)
            {
                if (UserIdentity.Current.IsAuthenticated)
                {
                    MessageBox.Show(mmMessage.Title, mmMessage.MessageType == Msgtype.Success ? MessageBox.MessageType.Success : MessageBox.MessageType.Error, mmMessage.Messages.ToArray());
                }
                else
                {
                    Session["ShwMesaj"] = mmMessage;
                }
            }


            ViewBag.MmMessage = mmMessage;
            ViewBag.KullaniciID = kullaniciId;
            ViewBag.EskiSifre = eskiSifre;
            ViewBag.YeniSifre = yeniSifre;
            ViewBag.YeniSifreTekrar = yeniSifreTekrar;
            return View(kul);
        }




        public ActionResult HesapKayit(int? id, string ekd)
        {
            var kayitYetki = RoleNames.KullanicilarKayit.InRoleCurrent();
            var mmMessage = new MmMessage();
            var model = new Kullanicilar
            {
                EnstituKod = EnstituBus.GetSelectedEnstitu(ekd),
                IsAktif = true
            };

            bool isKurumIci = true;
            bool isYerli = true;
            bool resimVar = false;

            if (UserIdentity.Current.IsAuthenticated)
            {
                if (!id.HasValue || id <= 0) id = UserIdentity.Current.Id;
                if (!kayitYetki && id != UserIdentity.Current.Id) id = UserIdentity.Current.Id;

                var data = _entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == id);
                if (data != null)
                {
                    isKurumIci = data.KullaniciTipleri.KurumIci;
                    isYerli = data.KullaniciTipleri.Yerli;
                    resimVar = data.ResimAdi.IsNullOrWhiteSpace() == false;
                    data.ResimAdi = data.ResimAdi;
                    model = data;

                }
                mmMessage.IsSuccess = true;
                model.Sifre = "";
            }
            else
            {
                if (id > 0) id = null;
            }
            ViewBag.EnstituKod = RoleNames.KullanicilarKayit.InRoleCurrent() ? new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod)
                : new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.KullaniciTipID = new SelectList(KullanicilarBus.GetCmbKullaniciTipleri(true, (!kayitYetki)), "Value", "Caption", model.KullaniciTipID);
            ViewBag.UnvanID = new SelectList(Management.cmbUnvanlar(true), "Value", "Caption", model.UnvanID);
            ViewBag.BirimID = new SelectList(Management.cmbBirimler(true), "Value", "Caption", model.BirimID);
            ViewBag.CinsiyetID = new SelectList(Management.cmbCinsiyetler(true), "Value", "Caption", model.CinsiyetID);
            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipleri(model.EnstituKod, true), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.ProgramKod = new SelectList(Management.cmbGetAktifProgramlar(model.EnstituKod, true, true), "Value", "Caption", model.ProgramKod);
            ViewBag.OgrenimDurumID = new SelectList(Management.cmbAktifOgrenimDurumu(true, IsHesapKayittaGozuksun: true), "Value", "Caption", model.OgrenimDurumID);
            ViewBag.YetkiGrupID = new SelectList(Management.cmbYetkiGruplari(), "Value", "Caption", model.YetkiGrupID);

            var kulTipi = _entities.KullaniciTipleris.FirstOrDefault(p => p.KullaniciTipID == model.KullaniciTipID);

            if (kulTipi != null)
            {
                ViewBag.KullaniciTipAdi = kulTipi.KullaniciTipAdi;
            }
            else
            {
                ViewBag.KullaniciTipAdi = "";
            }

            ViewBag.IsKurumIci = isKurumIci;
            ViewBag.IsYerli = isYerli;
            ViewBag.ResimVar = resimVar;
            return View(model);
        }
        [HttpPost]
        public ActionResult HesapKayit(Kullanicilar kModel, string ekd, bool isKurumIci, bool isYerli)
        {

            var mmMessage = new MmMessage
            {
                Title = kModel.KullaniciID > 0 ? "Kullanıcı Hesabı Güncelleme İşlemi" : "Yeni Kullanıcı Hesabı Oluşturma İşlemi"
            };
            var kKayit = RoleNames.KullanicilarKayit.InRoleCurrent();

            var erisimYetki = RoleNames.KullanicilarIslemYetkileri.InRoleCurrent();
            kModel.KullaniciAdi = kModel.KullaniciAdi != null ? kModel.KullaniciAdi.Trim() : "";
            #region Kontrol
            if (erisimYetki)
            {
                if (kModel.YetkiGrupID <= 0)
                {
                    mmMessage.Messages.Add("Yetki Grubu Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YetkiGrupID" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YetkiGrupID" });
            }
            if (kModel.KullaniciTipID <= 0)
            {
                mmMessage.Messages.Add("Kullanıcı Tipi Seçiniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KullaniciTipID" });
            }
            else
            {
                var ktp = _entities.KullaniciTipleris.First(p => p.KullaniciTipID == kModel.KullaniciTipID);
                isKurumIci = ktp.KurumIci;
                isYerli = ktp.Yerli;

                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "KullaniciTipID" });
            }

            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Enstitü Seçiniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });
            if (kModel.KullaniciTipID > 0)
            {
                if (kModel.ResimAdi.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Profil Resmi Yükleyiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ResimAdi" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ResimAdi" });
                if (kModel.Ad.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Ad Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Ad" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Ad" });
                if (kModel.Soyad.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Soyad Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Soyad" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Soyad" });

                if (kModel.TcKimlikNo.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("T.C. kimlik Numarası Giriniz.");

                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                }
                else if (kModel.TcKimlikNo.IsNumber() == false)
                {
                    mmMessage.Messages.Add("T.C. Kimlik Numarası Sadece Sayıdan Oluşmalıdır.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });

                }
                else if (kModel.TcKimlikNo.Length != 11)
                {
                    mmMessage.Messages.Add("T.C. Kimlik Numarası uzunluğu 11 Hane Olmalıdır.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });

                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TcKimlikNo" });

                if (!kModel.CinsiyetID.HasValue)
                {
                    mmMessage.Messages.Add("Cinsiyet Bilgisini Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CinsiyetID" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "CinsiyetID" });



                if (kModel.CepTel.IsNullOrWhiteSpace())
                {

                    mmMessage.Messages.Add("Cep Telefonu Numarası Giriniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CepTel" });
                }
                else
                {
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "CepTel" });
                }

                if (kModel.EMail.IsNullOrWhiteSpace())
                {

                    mmMessage.Messages.Add("E-Posta Bilgisini Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                }
                else if (kModel.EMail.ToIsValidEmail())
                {
                    mmMessage.Messages.Add("Girilen E-Posta Formatı uygun Değildir.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                }
                else
                {

                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EMail" });
                }

                if (!isKurumIci || !isYerli)
                    if (kModel.Adres.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Açık Adres Bilgisini Giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Adres" });
                    }
                    else
                    {
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Adres" });
                    }

                if (isKurumIci)
                    if (!kModel.BirimID.HasValue)
                    {
                        mmMessage.Messages.Add("Birim Seçiniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BirimID" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BirimID" });
                if (isKurumIci)
                    if (!kModel.UnvanID.HasValue)
                    {
                        mmMessage.Messages.Add("Unvan Seçiniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "UnvanID" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "UnvanID" });
                if (isKurumIci)
                    if (kModel.SicilNo.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Sicil No Giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SicilNo" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SicilNo" });

                if (kModel.YtuOgrencisi)
                {
                    if (kModel.OgrenciNo.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Öğrenci No Bilgisini Giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenciNo" });
                    }

                    else if (kModel.OgrenciNo.Length != 8)
                    {
                        mmMessage.Messages.Add("Öğrenci Numarası 8 Haneden Oluşmalıdır.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenciNo" });

                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenciNo" });
                    if (kModel.OgrenimTipKod.HasValue == false)
                    {
                        mmMessage.Messages.Add("Öğrenim Seviyesi Seçiniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimTipKod" });
                    if (kModel.ProgramKod.IsNullOrWhiteSpace())
                    {

                        mmMessage.Messages.Add("Program Seçiniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ProgramKod" });
                    if (kModel.OgrenimDurumID.HasValue == false)
                    {
                        mmMessage.Messages.Add("Öğrenim Durumu Seçiniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimDurumID" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimDurumID" });

                }
                if (kKayit)
                {
                    if (kModel.KullaniciAdi.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Kullanıcı Adı Giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KullaniciAdi" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "KullaniciAdi" });


                    if (kModel.KullaniciID <= 0)
                    {
                        if (kModel.Sifre.IsNullOrWhiteSpace())
                        {

                            mmMessage.Messages.Add("Şifre Giriniz.");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Sifre" });
                        }
                        else if (kModel.Sifre.Length < 4)
                        {

                            mmMessage.Messages.Add("Şifre En Az 4 Haneden Oluşmalıdır.");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Sifre" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Sifre" });
                    }
                    else if (!kModel.Sifre.IsNullOrWhiteSpace())
                    {
                        if (kModel.Sifre.Length < 4 && kModel.KullaniciID > 0)
                        {
                            mmMessage.Messages.Add("Şifre En Az 4 Haneden Oluşmalıdır.");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Sifre" });
                        }
                        else if (kModel.Sifre.Length >= 4 && kModel.KullaniciID > 0) mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Sifre" });
                    }
                }
            }
            if (mmMessage.Messages.Count == 0)
            {
                var ktip = _entities.KullaniciTipleris.First(p => p.KullaniciTipID == kModel.KullaniciTipID);
                isKurumIci = ktip.KurumIci;
                isYerli = ktip.Yerli;
                var qPersonel = _entities.Kullanicilars.AsQueryable();
                var kul = _entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == kModel.KullaniciID);
                if (qPersonel.Any(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.KullaniciAdi == kModel.KullaniciAdi))
                {

                    mmMessage.Messages.Add("Tanımlamak istediğiniz kullanıcı adı sistemde zaten mevcut!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KullaniciAdi" });
                }
                if (kul == null || kul.EMail.ToLower() != kModel.EMail.Trim().ToLower())
                {
                    if (qPersonel.Any(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.EMail == kModel.EMail))
                    {

                        mmMessage.Messages.Add("Tanımlamak istediğiniz E-Posta sistemde zaten mevcut!");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                    }
                }

                if (kul == null || kul.TcKimlikNo != kModel.TcKimlikNo.Trim())
                {
                    if (qPersonel.Any(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.TcKimlikNo == kModel.TcKimlikNo))
                    {
                        mmMessage.Messages.Add("Tanımlamak istediğiniz Kimlik No sistemde zaten mevcut!");
                        mmMessage.MessagesDialog.Add(new MrMessage
                        { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                    }
                }

                if (isKurumIci)
                {
                    if (kul == null || kul.SicilNo != kModel.SicilNo.Trim())
                    {
                        if (qPersonel.Any(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.SicilNo == kModel.SicilNo))
                        {
                            mmMessage.Messages.Add("Tanımlamak istediğiniz Sicil No sistemde zaten mevcut!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SicilNo" });
                        }
                    }
                }
                if (kModel.YtuOgrencisi)
                {
                    if (kul == null || kul.OgrenciNo != kModel.OgrenciNo.Trim())
                    {
                        if (qPersonel.Any(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.OgrenciNo == kModel.OgrenciNo))
                        {
                            mmMessage.Messages.Add("Girmiş olduğunuz öğrenci numarası ile daha önceden sisteme kayıt yapılmıştır. Tekrar kayıt yapamazsınız!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenciNo" });
                        }
                    }
                    if (kModel.OgrenimDurumID != OgrenimDurum.OzelOgrenci)
                    {

                        var ogrenciBilgi = Management.StudentControl(kModel.TcKimlikNo);
                        if (ogrenciBilgi.Hata)
                        {
                            mmMessage.Messages.Add("Obs sisteminden öğrenci bilgisi sorgulanırken bir hata oluştu! " + ogrenciBilgi.HataMsj);
                        }
                        else
                        {
                            if (ogrenciBilgi.KayitVar && kModel.OgrenimTipKod == ogrenciBilgi.OgrenciInfo.OGRENIMSEVIYE_ID.ToIntObj())
                            {
                                kModel.ProgramKod = kModel.ProgramKod;
                                kModel.OgrenimTipKod = ogrenciBilgi.OgrenciInfo.OGRENIMSEVIYE_ID.ToIntObj().Value;
                                kModel.KayitTarihi = ogrenciBilgi.KayitTarihi;
                                kModel.KayitYilBaslangic = ogrenciBilgi.BaslangicYil;
                                kModel.KayitDonemID = ogrenciBilgi.DonemID;
                            }
                            else
                            {
                                mmMessage.Messages.Add(
                                    "Girdiğiniz Kimlik bilgisi OBS sisteminde doğrulanamadı.");
                                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                            }

                        }
                    }

                }
            }
            if (mmMessage.Messages.Count == 0)
            {
                if (isKurumIci && kModel.KullaniciID <= 0)
                {
                    if (kModel.EMail.Contains("@yildiz.edu.tr"))
                    {

                        kModel.IsActiveDirectoryUser = true;
                        kModel.KullaniciAdi = kModel.EMail.Split('@')[0];
                    }
                    else
                    {
                        kModel.IsActiveDirectoryUser = false;
                        kModel.KullaniciAdi = kModel.EMail;
                    }
                }

            }
            if (mmMessage.Messages.Count == 0 && isKurumIci)
            {
                if (kModel.IsActiveDirectoryUser && kModel.EMail.Contains("@yildiz.edu.tr") == false)
                {
                    mmMessage.Messages.Add("Active Directori Girişi Yapmasını İstediğiniz Kullanıcının yildiz.edu.tr uzantılı mailini tanımlamanız gerekir!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsActiveDirectoryUser" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                }

            }

            #endregion
            if (kModel.KullaniciID <= 0 && mmMessage.Messages.Count == 0)
            {
                if (kModel.KullaniciTipID == KullaniciTipBilgi.YerliOgrenci || kModel.KullaniciTipID == KullaniciTipBilgi.YabanciOgrenci)
                {
                    kModel.KullaniciAdi = kModel.EMail.Trim();
                }
                else if (kModel.KullaniciTipID == KullaniciTipBilgi.IdariPersonel || kModel.KullaniciTipID == KullaniciTipBilgi.AkademikPersonel)
                {
                    if (kModel.IsActiveDirectoryUser)
                    {
                        kModel.KullaniciAdi = kModel.EMail.Split('@')[0].Trim();
                    }
                    else kModel.KullaniciAdi = kModel.EMail;
                }
                kModel.Sifre = Guid.NewGuid().ToString().Substr(0, 6);
                var excpt = KullanicilarBus.YeniHesapMailGonder(kModel, kModel.Sifre);
                if (excpt != null)
                {
                    mmMessage.Messages.Add("Mail gönderme hatası, Hesap oluşturulamadı!  Hata" + " : " + excpt.ToExceptionMessage());
                }
            }


            if (mmMessage.Messages.Count == 0)
            {
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.Ad = kModel.Ad.Trim();
                kModel.Soyad = kModel.Soyad.Trim();
                if (!isKurumIci)
                {
                    kModel.BirimID = null;
                    kModel.UnvanID = null;
                    kModel.SicilNo = "";

                }
                if (isKurumIci)
                {
                    kModel.Adres = "";

                }
                if (!kModel.YtuOgrencisi)
                {
                    kModel.OgrenciNo = null;
                    kModel.OgrenimTipKod = null;
                    kModel.OgrenimDurumID = null;
                    kModel.ProgramKod = null;
                    kModel.KayitTarihi = null;
                    kModel.KayitYilBaslangic = null;
                    kModel.KayitDonemID = null;
                }
                var sifreUnCrypet = kModel.Sifre;
                var yeniKullanici = kModel.KullaniciID <= 0;
                if (yeniKullanici)
                {
                    kModel.YetkiGrupID = erisimYetki ? kModel.YetkiGrupID : (kModel.KullaniciTipID == KullaniciTipBilgi.AkademikPersonel && KullanicilarBus.GetDanismanUnvanIds().Contains(kModel.UnvanID ?? 0) ? 6 : 1);//danışman yetkisi vermek için
                    kModel.OlusturmaTarihi = DateTime.Now;
                    kModel.IsAktif = true;
                    kModel.FixedHeader = false;
                    kModel.FixedSidebar = false;
                    kModel.ScrollSidebar = false;
                    kModel.RightSidebar = false;
                    kModel.CustomNavigation = true;
                    kModel.ToggledNavigation = false;
                    kModel.BoxedOrFullWidth = true;
                    kModel.ThemeName = "/Content/css/theme-forest.css";
                    kModel.BackgroundImage = "wall_2";
                    kModel.Sifre = kModel.Sifre.ComputeHash(Management.Tuz);
                    kModel = _entities.Kullanicilars.Add(kModel);
                    _entities.SaveChanges();

                    _entities.KullaniciEnstituYetkileris.Add(new KullaniciEnstituYetkileri
                    {
                        EnstituKod = kModel.EnstituKod,
                        KullaniciID = kModel.KullaniciID,
                        IslemYapanID = kModel.KullaniciID,
                        IslemTarihi = DateTime.Now,
                        IslemYapanIP = UserIdentity.Ip

                    });
                    _entities.SaveChanges();

                    mmMessage.IsCloseDialog = true;
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;
                    mmMessage.Messages.Add("Kullanıcı hesabı oluşturuldu!");
                    mmMessage.Messages.Add("Hesap bilgileri " + kModel.EMail + " E-Posta adresinize gönderildi.");
                    mmMessage.Messages.Add("Not: Sistem üzerinden mail hesabınıza mail gönderilememe durumuna karşı aşağıdaki şifreyi lütfen kopyalayınız ve sisteme giriş için bu şifreyi kullanınız.");

                    if (kModel.IsActiveDirectoryUser == false) mmMessage.Messages.Add("KullanıcıAdı:" + kModel.KullaniciAdi + " Şifre: " + sifreUnCrypet);
                    else mmMessage.Messages.Add("KullanıcıAdı:" + kModel.KullaniciAdi + " Şifre: E-Posta şifreniz ile aynı");
                }
                else
                {
                    var data = _entities.Kullanicilars.First(p => p.KullaniciID == kModel.KullaniciID);
                    data.EnstituKod = kModel.EnstituKod;
                    if (erisimYetki) data.YetkiGrupID = kModel.YetkiGrupID;
                    data.KullaniciTipID = kModel.KullaniciTipID;
                    data.Ad = kModel.Ad;
                    data.Soyad = kModel.Soyad;
                    data.TcKimlikNo = kModel.TcKimlikNo;
                    data.CinsiyetID = kModel.CinsiyetID;
                    data.CepTel = kModel.CepTel;
                    data.EMail = kModel.EMail;
                    data.Adres = kModel.Adres;
                    if (data.EMail != kModel.EMail.Trim() && (data.KullaniciTipID == KullaniciTipBilgi.YerliOgrenci || data.KullaniciTipID == KullaniciTipBilgi.YabanciOgrenci)) data.KullaniciAdi = kModel.EMail.Trim();
                    data.YtuOgrencisi = kModel.YtuOgrencisi;
                    data.OgrenimDurumID = kModel.OgrenimDurumID;
                    data.OgrenimTipKod = kModel.OgrenimTipKod;
                    data.OgrenciNo = kModel.OgrenciNo;
                    data.ProgramKod = kModel.ProgramKod;
                    data.KayitTarihi = kModel.KayitTarihi;
                    data.KayitYilBaslangic = kModel.KayitYilBaslangic;
                    data.KayitDonemID = kModel.KayitDonemID;

                    data.BirimID = kModel.BirimID;
                    data.UnvanID = kModel.UnvanID;
                    data.SicilNo = kModel.SicilNo;

                    if (RoleNames.KullanicilarKayit.InRoleCurrent())
                    {
                        data.KullaniciAdi = kModel.KullaniciAdi;
                        if (!kModel.Sifre.IsNullOrWhiteSpace())
                            data.Sifre = kModel.Sifre.ComputeHash(Management.Tuz);
                        data.SifresiniDegistirsin = kModel.SifresiniDegistirsin;
                        data.Aciklama = kModel.Aciklama;
                        data.IsActiveDirectoryUser = kModel.IsActiveDirectoryUser;
                        if (UserIdentity.Current.IsAdmin)
                        {
                            data.IsAdmin = kModel.IsAdmin;
                        }
                        data.IsAktif = kModel.IsAktif;
                    }
                    data.ResimAdi = kModel.ResimAdi;
                    data.IslemYapanID = kModel.IslemYapanID;
                    data.IslemTarihi = kModel.IslemTarihi;
                    data.IslemYapanIP = kModel.IslemYapanIP;
                    if (data.KullaniciID == UserIdentity.Current.Id) { UserIdentity.Current.ImagePath = data.ResimAdi.ToKullaniciResim(); }
                    _entities.SaveChanges();
                    mmMessage.Messages.Add("'" + data.Ad + " " + data.Soyad + "' Kullanıcı hesabı güncellendi.");
                    mmMessage.IsCloseDialog = true;
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;
                    LogIslemleri.LogEkle("Kullanicilar", IslemTipi.Update, data.ToJson());
                }
            }
            else
            {
                mmMessage.Title = kModel.KullaniciID > 0 ? "Kullanıcı Hesabı Güncelleme İşlemi İçin Aşağıdaki Uyarıları Kontrol Ediniz" : "Yeni Kullanıcı Hesabı Oluşturma İşlemi İçin Aşağıdaki Uyarıları Kontrol Ediniz.";
                mmMessage.MessageType = Msgtype.Warning;
            }
            return mmMessage.ToJsonResult();
        }


        [HttpPost]
        public ActionResult ImageUploadPost(int? kullaniciId, HttpPostedFileBase kImage, string eskiResimAdi = "")
        {
            var mMessage = new MmMessage();
            string yeniResimAdi = "";
            mMessage.Title = "Profil resmi yükleme işlemi";
            mMessage.IsSuccess = false;
            mMessage.MessageType = Msgtype.Warning;
            if (kImage == null || kImage.ContentLength <= 0)
            {
                mMessage.Messages.Add("Profil Resmi Seçiniz");
                // MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProfilResmi" });
            }
            else if (RoleNames.KullanicilarKayit.InRoleCurrent() == false && kullaniciId != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Başka bir kullanıcı adına resim yüklemesi yapmaya yetkili değilsiniz.");
            }
            else
            {
                var contentlength = kImage.ContentLength;
                string uzanti = kImage.FileName.GetFileExtension();
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
                    if (UserIdentity.Current.IsAuthenticated)
                    {

                        var kul = _entities.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
                        eskiResimAdi = kul.ResimAdi;

                        kul.ResimAdi = yeniResimAdi = KullanicilarBus.ResimKaydet(kImage);
                        kul.IslemYapanID = UserIdentity.Current.Id;
                        kul.IslemYapanIP = UserIdentity.Ip;
                        kul.IslemTarihi = DateTime.Now;
                        _entities.SaveChanges();


                        if (eskiResimAdi.IsNullOrWhiteSpace() == false)
                        {
                            var resimKlasorAdi = SistemAyar.KullaniciResimYolu;
                            var rsm = Server.MapPath("~/" + resimKlasorAdi + "/" + eskiResimAdi);
                            try
                            {
                                if (System.IO.File.Exists(rsm)) System.IO.File.Delete(rsm);
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                        }
                        mMessage.IsSuccess = true;
                        mMessage.Messages.Add("profil resmi sisteme yüklendi.");
                    }
                    else
                    {
                        yeniResimAdi = KullanicilarBus.ResimKaydet(kImage);
                        if (eskiResimAdi.IsNullOrWhiteSpace() == false)
                        {
                            var resimKlasorAdi = SistemAyar.KullaniciResimYolu;
                            var rsm = Server.MapPath("~/" + resimKlasorAdi + "/" + eskiResimAdi);
                            try
                            {
                                if (System.IO.File.Exists(rsm)) System.IO.File.Delete(rsm);
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                        }
                        mMessage.IsSuccess = true;
                        mMessage.Messages.Add("profil resmi sisteme yüklendi.");
                    }

                }
            }
            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            var yeniResimYolu = yeniResimAdi.ToKullaniciResim();
            UserIdentity.Current.ImagePath = yeniResimYolu;
            return new { mMessage, YeniResimAdi = yeniResimAdi, YeniResimYolu = yeniResimYolu }.ToJsonResult();
        }
        [HttpPost]
        public ActionResult RotateImage(bool leftOrRight, string resimAdi)
        {

            if (resimAdi.IsNullOrWhiteSpace() == false)
            {
                var resimKlasorAdi = SistemAyar.KullaniciResimYolu;
                var rsm = Server.MapPath("~/" + resimKlasorAdi + "/" + resimAdi);
                if (System.IO.File.Exists(rsm))
                {
                    using (Image img = Image.FromFile(rsm))
                    {
                        img.RotateFlip(leftOrRight ? RotateFlipType.Rotate270FlipNone : RotateFlipType.Rotate90FlipNone);
                        img.Save(rsm, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                }

            }
            var YeniResimYolu = resimAdi.ToKullaniciResim();

            return new { YeniResimYolu }.ToJsonResult();
        }


        protected override void Dispose(bool disposing)
        {
            _entities.Dispose();
            base.Dispose(disposing);
        }
    }
}
