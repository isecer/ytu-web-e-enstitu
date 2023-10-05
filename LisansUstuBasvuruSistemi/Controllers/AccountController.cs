
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
using LisansUstuBasvuruSistemi.Ws_YokOgrenciSorgula;

namespace LisansUstuBasvuruSistemi.Controllers
{

    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class AccountController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();

        public ActionResult Login(bool? logout,  string returnUrl)
        {

            if (logout == true)
            {
                FormsAuthenticationUtil.SignOut();
                return RedirectToAction("Index", "Home");
            }
            if (UserIdentity.Current.IsAuthenticated) return RedirectToAction("Index", "Home");
            ViewBag.UserName = "";
            ViewBag.ReturnUrl = returnUrl;
            return PartialView();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string userName, string password, string captchaInputText, bool? rememberMe, string returnUrl)
        {


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
                                msg = "Uygulama şifresiyle Enstitü Bilgi Sistemine giriş yapılamadı! <a href='https://teknikdestek.yildiz.edu.tr/kb/faq.php?id=32' target='_blank' style='color:white;'>Detaylı bilgi almak için tıklayınız. https://teknikdestek.yildiz.edu.tr/kb/faq.php?id=32</a>";
                                // Management.SistemBilgisiKaydet("Active Directory Kontrolünden Geçilemedi! Kullanıcı Adı: " + UserName, "Acconunt/Login", BilgiTipi.LoginHatalari, null, UserIdentity.Ip);
                            }
                        }

                        if (loginUser != null && loginUser.IsAktif)
                        {
                            try
                            {
                                if (loginUser.YtuOgrencisi && loginUser.OgrenimDurumID == OgrenimDurum.HalenOğrenci)
                                {
                                    var tdoBasvuruId = _entities.TDOBasvurus
                                        .Where(p => p.KullaniciID == loginUser.KullaniciID)
                                        .OrderByDescending(o => o.TDOBasvuruID).Select(s => s.TDOBasvuruID)
                                        .FirstOrDefault();
                                    if (tdoBasvuruId > 0)
                                    {
                                        TezDanismanOneriBus.ObsDanismanBasvuruBilgiEslestir(loginUser.KullaniciID, tdoBasvuruId);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                SistemBilgilendirmeBus.SistemBilgisiKaydet(ex, LogType.Hata);
                                // ignored
                            }

                            rememberMe = rememberMe ?? false;
                            FormsAuthenticationUtil.SetAuthCookie(user.KullaniciAdi, "", rememberMe.Value);
                            UserBus.SetLastLogon();

                            if (returnUrl.IsNullOrWhiteSpace()) return RedirectToAction("Index", "Home");
                            return Redirect(returnUrl);


                        }
                        else
                        { 
                            if (loginUser != null && !loginUser.IsAktif) hata = "Kullanıcı Hesabı Pasif Durumda!";
                            else hata = "Kullanıcı Adı veya Şifre Hatalı. " + msg;
                        }
                    }
                    else
                    { 
                        hata = "Kullanıcı sistemde bulunamadı.";
                    }
                }

            }
            catch (Exception ex)
            { 
                hata = "Sisteme Giriş Yapılırken Bir Hata Oluştu! Hata: " + ex.ToExceptionMessage();
            }
            ViewBag.Hata = hata; 
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

            var messageModel = new MmMessage
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
                        messageModel.IsSuccess = false;
                        messageModel.Messages.Add("Parola Sıfırlama linkinin geçerlilik süresi dolmuştur!");
                        messageModel.ReturnUrl = Url.Action("Index", "Home");
                    }
                }
                else
                {
                    messageModel.IsSuccess = false;
                    messageModel.Messages.Add("Şifre sıfırlama linki herhangi bir kullanıcıya eşleştirilemedi!");
                    messageModel.ReturnUrl = Url.Action("Index", "Home");


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
                    messageModel.IsSuccess = false;
                    messageModel.IsCloseDialog = true;
                    messageModel.Messages.Add("Lütfen Giriş Yapın");
                    messageModel.ReturnUrl = Url.Action("Index", "Home");

                }
            }
            Session["ShwMesaj"] = messageModel;
            ViewBag.MmMessage = messageModel;
            ViewBag.KullaniciID = kullaniciId;
            ViewBag.EskiSifre = "";
            ViewBag.YeniSifre = "";
            ViewBag.YeniSifreTekrar = "";
            return View(kul);
        }
        [HttpPost]
        public ActionResult ParolaSifirla(string psKod, string eskiSifre, string yeniSifre, string yeniSifreTekrar, int? kullaniciId = null, string dlgId = "")
        {
            var messageModel = new MmMessage
            {
                IsDialog = !dlgId.IsNullOrWhiteSpace(),
                DialogID = dlgId,
                ReturnUrlTimeOut = 4000
            };
            if (psKod.IsNullOrWhiteSpace() == true)
            {
                messageModel.MessageType = Msgtype.Error;
                messageModel.Title = "Şifre değiştirme işlemi başarısız!";
                messageModel.ReturnUrl = Url.Action("Index", "Home");
            }

            var kul = kullaniciId.HasValue ? _entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == kullaniciId)
                : _entities.Kullanicilars.FirstOrDefault(p => p.ParolaSifirlamaKodu == psKod);
            if (kul != null)
            {
                if (kullaniciId.HasValue == false)
                    if (kul.ParolaSifirlamGecerlilikTarihi.HasValue && kul.ParolaSifirlamGecerlilikTarihi.Value < DateTime.Now)
                    {
                        messageModel.MessageType = Msgtype.Error;
                        messageModel.Messages.Add("Parola Sıfırlama linkinin geçerlilik süresi dolmuştur!");
                        messageModel.ReturnUrl = Url.Action("Index", "Home");
                    }
                if (kullaniciId.HasValue)
                {
                    if (eskiSifre.IsNullOrWhiteSpace())
                    {
                        messageModel.Messages.Add("Varolan şifrenizi giriniz.");
                        messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EskiSifre" });
                    }
                    else if (kul.Sifre != eskiSifre.ComputeHash(Management.Tuz))
                    {
                        messageModel.Messages.Add("Varolan şifrenizi yanlış girdiniz.");
                        messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EskiSifre" });
                    }
                    else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EskiSifre" });
                }
                if (messageModel.Messages.Count == 0)
                {

                    if (yeniSifre.Length < 4)
                    {
                        messageModel.Messages.Add("Yeni şifreniz en az 4 haneli olmalıdır!");
                        messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifre" });
                    }
                    else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YeniSifre" });
                    if (yeniSifreTekrar.Length < 4)
                    {
                        messageModel.Messages.Add("Yeni şifre tekrar en az 4 haneli olmalıdır!");
                        messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifreTekrar" });
                    }
                    else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifreTekrar" });
                    if (messageModel.Messages.Count == 0)
                    {
                        if (yeniSifreTekrar != yeniSifre)
                        {
                            messageModel.Messages.Add("Yeni şifre ile yeni şifre tekrar birbiriyle uyuşmuyor!");
                            messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifreTekrar" });
                            messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifre" });
                        }
                        else
                        {
                            messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YeniSifre" });
                            messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YeniSifreTekrar" });
                        }
                    }
                }

                if (messageModel.Messages.Count == 0)
                {
                    kul.Sifre = yeniSifreTekrar.ComputeHash(Management.Tuz);
                    kul.ParolaSifirlamGecerlilikTarihi = DateTime.Now;
                    _entities.SaveChanges();
                    messageModel.MessageType = Msgtype.Success;
                    messageModel.Title = "Şifre değiştirme işlemi";
                    if (kullaniciId.HasValue == false)
                    {
                        messageModel.Messages.Add("Şifreniz değiştirildi! Giriş sayfasına yönlendiriliyorsunuz...");
                        messageModel.ReturnUrl = Url.Action("Login", "Account");
                    }
                    else
                    {
                        messageModel.IsCloseDialog = true;
                        messageModel.Messages.Add("Şifreniz değiştirildi!");
                    }
                }
                else
                {
                    messageModel.MessageType = Msgtype.Error;
                    messageModel.Title = "Şifre değiştirme işlemi başarısız!";
                }
                kul.ResimAdi = kul.ResimAdi.ToKullaniciResim();
            }
            else
            {
                messageModel.MessageType = Msgtype.Error;
                messageModel.Title = "Şifre değiştirme işlemi başarısız!";
            }
            if (messageModel.Messages.Count > 0)
            {
                if (UserIdentity.Current.IsAuthenticated)
                {
                    MessageBox.Show(messageModel.Title, messageModel.MessageType == Msgtype.Success ? MessageBox.MessageType.Success : MessageBox.MessageType.Error, messageModel.Messages.ToArray());
                }
                else
                {
                    Session["ShwMesaj"] = messageModel;
                }
            }


            ViewBag.MmMessage = messageModel;
            ViewBag.KullaniciID = kullaniciId;
            ViewBag.EskiSifre = eskiSifre;
            ViewBag.YeniSifre = yeniSifre;
            ViewBag.YeniSifreTekrar = yeniSifreTekrar;
            return View(kul);
        }




        public ActionResult HesapKayit(int? id, string ekd)
        {
            var kayitYetki = RoleNames.KullanicilarKayit.InRoleCurrent();
            
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

            ViewBag.KullaniciTipAdi = kulTipi != null ? kulTipi.KullaniciTipAdi : "";

            ViewBag.IsKurumIci = isKurumIci;
            ViewBag.IsYerli = isYerli;
            ViewBag.ResimVar = resimVar;
            return View(model);
        }
        [HttpPost]
        public ActionResult HesapKayit(Kullanicilar kModel, bool isKurumIci, bool isYerli)
        {

            var messageModel = new MmMessage
            {
                Title = kModel.KullaniciID > 0 ? "Kullanıcı Hesabı Güncelleme İşlemi" : "Yeni Kullanıcı Hesabı Oluşturma İşlemi"
            };
            var kKayit = RoleNames.KullanicilarKayit.InRoleCurrent();
            if (!kKayit && kModel.KullaniciID > 0 && kModel.KullaniciID != UserIdentity.Current.Id)
            {
                messageModel.Messages.Add("Bu işlemi yapmaya yetkili değilsiniz.");
                return messageModel.ToJsonResult();
            }
            var erisimYetki = RoleNames.KullanicilarIslemYetkileri.InRoleCurrent();
            kModel.KullaniciAdi = kModel.KullaniciAdi != null ? kModel.KullaniciAdi.Trim() : "";
            #region Kontrol
            if (erisimYetki)
            {
                if (kModel.YetkiGrupID <= 0)
                {
                    messageModel.Messages.Add("Yetki Grubu Seçiniz.");
                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YetkiGrupID" });
                }
                else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YetkiGrupID" });
            }
            if (kModel.KullaniciTipID <= 0)
            {
                messageModel.Messages.Add("Kullanıcı Tipi Seçiniz.");
                messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KullaniciTipID" });
            }
            else
            {
                var ktp = _entities.KullaniciTipleris.First(p => p.KullaniciTipID == kModel.KullaniciTipID);
                isKurumIci = ktp.KurumIci;
                isYerli = ktp.Yerli;

                messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "KullaniciTipID" });
            }

            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                messageModel.Messages.Add("Enstitü Seçiniz.");
                messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });
            if (kModel.KullaniciTipID > 0)
            {
                if (kModel.ResimAdi.IsNullOrWhiteSpace())
                {
                    messageModel.Messages.Add("Profil Resmi Yükleyiniz.");
                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ResimAdi" });
                }
                else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ResimAdi" });
                if (kModel.Ad.IsNullOrWhiteSpace())
                {
                    messageModel.Messages.Add("Ad Giriniz.");
                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Ad" });
                }
                else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Ad" });
                if (kModel.Soyad.IsNullOrWhiteSpace())
                {
                    messageModel.Messages.Add("Soyad Giriniz.");
                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Soyad" });
                }
                else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Soyad" });

                if (kModel.TcKimlikNo.IsNullOrWhiteSpace())
                {
                    messageModel.Messages.Add("T.C. kimlik Numarası Giriniz.");

                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                }
                else if (kModel.TcKimlikNo.IsNumber() == false)
                {
                    messageModel.Messages.Add("T.C. Kimlik Numarası Sadece Sayıdan Oluşmalıdır.");
                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });

                }
                else if (kModel.TcKimlikNo.Length != 11)
                {
                    messageModel.Messages.Add("T.C. Kimlik Numarası uzunluğu 11 Hane Olmalıdır.");
                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });

                }
                else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TcKimlikNo" });

                if (!kModel.CinsiyetID.HasValue)
                {
                    messageModel.Messages.Add("Cinsiyet Bilgisini Seçiniz.");
                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CinsiyetID" });
                }
                else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "CinsiyetID" });



                if (kModel.CepTel.IsNullOrWhiteSpace())
                {

                    messageModel.Messages.Add("Cep Telefonu Numarası Giriniz");
                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CepTel" });
                }
                else
                {
                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "CepTel" });
                }

                if (kModel.EMail.IsNullOrWhiteSpace())
                {

                    messageModel.Messages.Add("E-Posta Bilgisini Giriniz.");
                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                }
                else if (kModel.EMail.ToIsValidEmail())
                {
                    messageModel.Messages.Add("Girilen E-Posta Formatı uygun Değildir.");
                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                }
                else
                {

                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EMail" });
                }

                if (!isKurumIci || !isYerli)
                    if (kModel.Adres.IsNullOrWhiteSpace())
                    {
                        messageModel.Messages.Add("Açık Adres Bilgisini Giriniz.");
                        messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Adres" });
                    }
                    else
                    {
                        messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Adres" });
                    }

                if (isKurumIci)
                    if (!kModel.BirimID.HasValue)
                    {
                        messageModel.Messages.Add("Birim Seçiniz.");
                        messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BirimID" });
                    }
                    else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BirimID" });
                if (isKurumIci)
                    if (!kModel.UnvanID.HasValue)
                    {
                        messageModel.Messages.Add("Unvan Seçiniz.");
                        messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "UnvanID" });
                    }
                    else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "UnvanID" });
                if (isKurumIci)
                    if (kModel.SicilNo.IsNullOrWhiteSpace())
                    {
                        messageModel.Messages.Add("Sicil No Giriniz.");
                        messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SicilNo" });
                    }
                    else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SicilNo" });

                if (kModel.YtuOgrencisi)
                {
                    if (kModel.OgrenciNo.IsNullOrWhiteSpace())
                    {
                        messageModel.Messages.Add("Öğrenci No Bilgisini Giriniz.");
                        messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenciNo" });
                    }

                    else if (kModel.OgrenciNo.Length != 8)
                    {
                        messageModel.Messages.Add("Öğrenci Numarası 8 Haneden Oluşmalıdır.");
                        messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenciNo" });

                    }
                    else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenciNo" });
                    if (kModel.OgrenimTipKod.HasValue == false)
                    {
                        messageModel.Messages.Add("Öğrenim Seviyesi Seçiniz.");
                        messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
                    }
                    else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimTipKod" });
                    if (kModel.ProgramKod.IsNullOrWhiteSpace())
                    {

                        messageModel.Messages.Add("Program Seçiniz.");
                        messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
                    }
                    else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ProgramKod" });
                    if (kModel.OgrenimDurumID.HasValue == false)
                    {
                        messageModel.Messages.Add("Öğrenim Durumu Seçiniz.");
                        messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimDurumID" });
                    }
                    else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimDurumID" });

                }
                if (kKayit)
                {
                    if (kModel.KullaniciAdi.IsNullOrWhiteSpace())
                    {
                        messageModel.Messages.Add("Kullanıcı Adı Giriniz.");
                        messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KullaniciAdi" });
                    }
                    else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "KullaniciAdi" });


                    if (kModel.KullaniciID <= 0)
                    {
                        if (kModel.Sifre.IsNullOrWhiteSpace())
                        {

                            messageModel.Messages.Add("Şifre Giriniz.");
                            messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Sifre" });
                        }
                        else if (kModel.Sifre.Length < 4)
                        {

                            messageModel.Messages.Add("Şifre En Az 4 Haneden Oluşmalıdır.");
                            messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Sifre" });
                        }
                        else messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Sifre" });
                    }
                    else if (!kModel.Sifre.IsNullOrWhiteSpace())
                    {
                        if (kModel.Sifre.Length < 4 && kModel.KullaniciID > 0)
                        {
                            messageModel.Messages.Add("Şifre En Az 4 Haneden Oluşmalıdır.");
                            messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Sifre" });
                        }
                        else if (kModel.Sifre.Length >= 4 && kModel.KullaniciID > 0) messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Sifre" });
                    }
                }
            }
            if (messageModel.Messages.Count == 0)
            {
                var ktip = _entities.KullaniciTipleris.First(p => p.KullaniciTipID == kModel.KullaniciTipID);
                isKurumIci = ktip.KurumIci;
                var qPersonel = _entities.Kullanicilars.AsQueryable();
                var kul = _entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == kModel.KullaniciID);
                if (qPersonel.Any(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.KullaniciAdi == kModel.KullaniciAdi))
                {

                    messageModel.Messages.Add("Tanımlamak istediğiniz kullanıcı adı sistemde zaten mevcut!");
                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KullaniciAdi" });
                }
                if (kul == null || kul.EMail.ToLower() != kModel.EMail.Trim().ToLower())
                {
                    if (qPersonel.Any(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.EMail == kModel.EMail))
                    {

                        messageModel.Messages.Add("Tanımlamak istediğiniz E-Posta sistemde zaten mevcut!");
                        messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                    }
                }

                if (kul == null || kul.TcKimlikNo != kModel.TcKimlikNo.Trim())
                {
                    if (qPersonel.Any(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.TcKimlikNo == kModel.TcKimlikNo))
                    {
                        messageModel.Messages.Add("Tanımlamak istediğiniz Kimlik No sistemde zaten mevcut!");
                        messageModel.MessagesDialog.Add(new MrMessage
                        { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                    }
                }

                if (isKurumIci)
                {
                    if (kul == null || kul.SicilNo != kModel.SicilNo.Trim())
                    {
                        if (qPersonel.Any(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.SicilNo == kModel.SicilNo))
                        {
                            messageModel.Messages.Add("Tanımlamak istediğiniz Sicil No sistemde zaten mevcut!");
                            messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SicilNo" });
                        }
                    }
                }
                if (kModel.YtuOgrencisi)
                {
                    if (kul == null || kul.OgrenciNo != kModel.OgrenciNo.Trim())
                    {
                        if (qPersonel.Any(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.OgrenciNo == kModel.OgrenciNo))
                        {
                            messageModel.Messages.Add("Girmiş olduğunuz öğrenci numarası ile daha önceden sisteme kayıt yapılmıştır. Tekrar kayıt yapamazsınız!");
                            messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenciNo" });
                        }
                    }
                    if (kModel.OgrenimDurumID != OgrenimDurum.OzelOgrenci)
                    {

                        var ogrenciBilgi = KullanicilarBus.OgrenciKontrol(kModel.TcKimlikNo);
                        if (ogrenciBilgi.Hata)
                        {
                            messageModel.Messages.Add("Obs sisteminden öğrenci bilgisi sorgulanırken bir hata oluştu! " + ogrenciBilgi.HataMsj);
                        }
                        else
                        {
                            if (ogrenciBilgi.KayitVar)
                            {
                                if (kModel.OgrenciNo != ogrenciBilgi.OgrenciInfo.OGR_NO)
                                {
                                    messageModel.Messages.Add(
                                        "Girdiğiniz Öğrenci Numarası bilgisi OBS sisteminde doğrulanamadı.");
                                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenciNo" });
                                }
                                if (kModel.OgrenimTipKod != ogrenciBilgi.OgrenciInfo.OGRENIMSEVIYE_ID.ToIntObj())
                                {
                                    messageModel.Messages.Add(
                                        "Girdiğiniz Öğrenim Seviyesi bilgisi OBS sisteminde doğrulanamadı.");
                                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
                                }
                                if (!messageModel.Messages.Any())
                                {
                                    kModel.ProgramKod = kModel.ProgramKod;
                                    kModel.OgrenimTipKod = ogrenciBilgi.OgrenciInfo.OGRENIMSEVIYE_ID.ToIntObj().Value;
                                    kModel.KayitTarihi = ogrenciBilgi.KayitTarihi;
                                    kModel.KayitYilBaslangic = ogrenciBilgi.BaslangicYil;
                                    kModel.KayitDonemID = ogrenciBilgi.DonemID;
                                }

                            }
                            else
                            {
                                messageModel.Messages.Add(
                                    "Girdiğiniz Kimlik bilgisi OBS sisteminde doğrulanamadı.");
                                messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                            }

                        }
                    }

                }
            }
            if (messageModel.Messages.Count == 0)
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
            if (messageModel.Messages.Count == 0 && isKurumIci)
            {
                if (kModel.IsActiveDirectoryUser && kModel.EMail.Contains("@yildiz.edu.tr") == false)
                {
                    messageModel.Messages.Add("Active Directori Girişi Yapmasını İstediğiniz Kullanıcının yildiz.edu.tr uzantılı mailini tanımlamanız gerekir!");
                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsActiveDirectoryUser" });
                    messageModel.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                }

            }

            #endregion
            if (kModel.KullaniciID <= 0 && messageModel.Messages.Count == 0)
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
                    messageModel.Messages.Add("Mail gönderme hatası, Hesap oluşturulamadı!  Hata" + " : " + excpt.ToExceptionMessage());
                }
            }


            if (messageModel.Messages.Count == 0)
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

                    messageModel.IsCloseDialog = true;
                    messageModel.IsSuccess = true;
                    messageModel.MessageType = Msgtype.Success;
                    messageModel.Messages.Add("Kullanıcı hesabı oluşturuldu!");
                    messageModel.Messages.Add("Hesap bilgileri " + kModel.EMail + " E-Posta adresinize gönderildi.");
                    messageModel.Messages.Add("Not: Sistem üzerinden mail hesabınıza mail gönderilememe durumuna karşı aşağıdaki şifreyi lütfen kopyalayınız ve sisteme giriş için bu şifreyi kullanınız.");

                    if (kModel.IsActiveDirectoryUser == false) messageModel.Messages.Add("KullanıcıAdı:" + kModel.KullaniciAdi + " Şifre: " + sifreUnCrypet);
                    else messageModel.Messages.Add("KullanıcıAdı:" + kModel.KullaniciAdi + " Şifre: E-Posta şifreniz ile aynı");
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
                    messageModel.Messages.Add("'" + data.Ad + " " + data.Soyad + "' Kullanıcı hesabı güncellendi.");
                    messageModel.IsCloseDialog = true;
                    messageModel.IsSuccess = true;
                    messageModel.MessageType = Msgtype.Success;
                    LogIslemleri.LogEkle("Kullanicilar", IslemTipi.Update, data.ToJson());
                }
            }
            else
            {
                messageModel.Title = kModel.KullaniciID > 0 ? "Kullanıcı Hesabı Güncelleme İşlemi İçin Aşağıdaki Uyarıları Kontrol Ediniz" : "Yeni Kullanıcı Hesabı Oluşturma İşlemi İçin Aşağıdaki Uyarıları Kontrol Ediniz.";
                messageModel.MessageType = Msgtype.Warning;
            }
            return messageModel.ToJsonResult();
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
            var yeniResimYolu = resimAdi.ToKullaniciResim();

            return new { YeniResimYolu = yeniResimYolu }.ToJsonResult();
        }


        protected override void Dispose(bool disposing)
        {
            _entities.Dispose();
            base.Dispose(disposing);
        }
    }
}
