
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
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class AccountController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Login(bool? logout, string dlgId, string ReturnUrl)
        {
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgId.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgId;
            if (logout == true)
            {
                FormsAuthenticationUtil.SignOut();
                return RedirectToAction("Index", "Home");
            }
            else if (UserIdentity.Current.IsAuthenticated) return RedirectToAction("Index", "Home");
            ViewBag.UserName = "";
            MmMessage.ReturnUrl = ReturnUrl;
            ViewBag.MmMessage = MmMessage;
            return PartialView();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string UserName, string Password, string CaptchaInputText, bool? RememberMe, string ReturnUrl, string dlgId)
        {


            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgId.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgId;
            MmMessage.ReturnUrl = ReturnUrl;
            ViewBag.UserName = UserName;
            ViewBag.Password = Password;
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
                    var user = UserBus.GetLoginUser(UserName);
                    Kullanicilar loginUser = null;
                    if (user != null)
                    {
                        if (user.IsActiveDirectoryUser == false)
                        {
                            loginUser = UserBus.Login(UserName, Password);
                        }
                        else
                        {
                            LdapService.SecureSoapClient ld = new LdapService.SecureSoapClient();

                            var WsPwd = ConfigurationManager.AppSettings["ldapServicePassword"];
                            var IsSueccess = ld.Login(UserName, Password, WsPwd);
                            if (IsSueccess)
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
                                var lastTdo = db.TDOBasvurus.OrderByDescending(p => p.KullaniciID == loginUser.KullaniciID).FirstOrDefault();
                                if (lastTdo != null) TezDanismanOneriBus.GetSecilenBasvuruTdoDetay(lastTdo.TDOBasvuruID, null);
                            }
                            catch (Exception ex)
                            {
                            }
                            RememberMe = RememberMe ?? false;
                            FormsAuthenticationUtil.SetAuthCookie(user.KullaniciAdi, "", RememberMe.Value);
                            UserBus.SetLastLogon();
                            MmMessage.IsCloseDialog = true;
                            if (MmMessage.IsDialog)
                            {
                                if (ReturnUrl.IsNullOrWhiteSpace()) MmMessage.ReturnUrl = Url.Action("Index", "Home");
                            }
                            else
                            {
                                if (ReturnUrl.IsNullOrWhiteSpace()) return RedirectToAction("Index", "Home");
                                else return Redirect(ReturnUrl);
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
                            if (loginUser != null && !loginUser.IsAktif) Hata = "Kullanıcı Hesabı Pasif Durumda!";
                            else Hata = "Kullanıcı Adı veya Şifre Hatalı. " + msg;
                        }
                    }
                    else
                    {
                        //  Management.SistemBilgisiKaydet("Kullanıcı Sistemde Bulunamadı! Kullanıcı Adı: " + UserName, "Acconunt/Login", BilgiTipi.LoginHatalari, null, UserIdentity.Ip);
                        Hata = "Kullanıcı sistemde bulunamadı.";
                    }
                }

            }
            catch (Exception ex)
            {
                MmMessage.IsSuccess = false;
                MmMessage.Messages.Add("Sisteme Giriş Yapılırken Bir Hata Oluştu! Hata: " + ex.ToExceptionMessage());
                Hata = "Sisteme Giriş Yapılırken Bir Hata Oluştu! Hata: " + ex.ToExceptionMessage();
            }
            ViewBag.Hata = Hata;
            ViewBag.MmMessage = MmMessage;
            return PartialView();

        }
        [Authorize(Roles = RoleNames.KullanicilarOnlineKullanicilar)]
        public ActionResult OnlineUserCnt()
        {

            var users = OnlineUsers.users;
            int Count = users.Count();
            var q = Count.ToJsonResult();
            return q;

        }

        [Authorize(Roles = RoleNames.KullanicilarOnlineKullanicilar)]
        public ActionResult getOnlineUserList()
        {
            var users = OnlineUsers.users.ToList();
            return View(users);
        }



        public ActionResult ParolaSifirla(string psKod, int? KullaniciID = null, string dlgId = "")
        {

            MmMessage msg = new MmMessage();
            msg.ReturnUrlTimeOut = 4000;
            msg.IsDialog = !dlgId.IsNullOrWhiteSpace();
            msg.DialogID = dlgId;
            if (psKod.IsNullOrWhiteSpace() && KullaniciID.HasValue == false) return RedirectToAction("Index", "Home");

            //if (Lng.IsNullOrWhiteSpace()) Lng = Management.getSelectedCulture();
            //Response.Cookies["CacheLang"].Value = Lng;
            //UserIdentity.Current.SeciliSDilKodu = Lng;

            var kul = new Kullanicilar();
            if (KullaniciID.HasValue == false)
            {
                kul = db.Kullanicilars.Where(p => p.IsAktif && p.ParolaSifirlamaKodu == psKod).FirstOrDefault();

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
                    KullaniciID = UserIdentity.Current.Id;
                    kul = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).FirstOrDefault();
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
            ViewBag.KullaniciID = KullaniciID;
            ViewBag.EskiSifre = "";
            ViewBag.YeniSifre = "";
            ViewBag.YeniSifreTekrar = "";
            return View(kul);
        }
        [HttpPost]
        public ActionResult ParolaSifirla(string psKod, string EskiSifre, string YeniSifre, string YeniSifreTekrar, int? KullaniciID = null, string dlgId = "")
        {
            MmMessage MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgId.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgId;
            MmMessage.ReturnUrlTimeOut = 4000;
            if (psKod.IsNullOrWhiteSpace() == true)
            {
                MmMessage.MessageType = Msgtype.Error;
                MmMessage.Title = "Şifre değiştirme işlemi başarısız!";
                MmMessage.ReturnUrl = Url.Action("Index", "Home");
            }
            var kul = new Kullanicilar();
            if (KullaniciID.HasValue) kul = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).FirstOrDefault();
            else kul = db.Kullanicilars.Where(p => p.ParolaSifirlamaKodu == psKod).FirstOrDefault();
            if (kul != null)
            {
                if (KullaniciID.HasValue == false)
                    if (kul.ParolaSifirlamGecerlilikTarihi.HasValue && kul.ParolaSifirlamGecerlilikTarihi.Value < DateTime.Now)
                    {
                        MmMessage.MessageType = Msgtype.Error;
                        MmMessage.Messages.Add("Parola Sıfırlama linkinin geçerlilik süresi dolmuştur!");
                        MmMessage.ReturnUrl = Url.Action("Index", "Home");
                    }
                if (KullaniciID.HasValue)
                {
                    if (EskiSifre.IsNullOrWhiteSpace())
                    {
                        MmMessage.Messages.Add("Varolan şifrenizi giriniz.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EskiSifre" });
                    }
                    else if (!(kul.Sifre == EskiSifre.ComputeHash(Management.Tuz)))
                    {
                        MmMessage.Messages.Add("Varolan şifrenizi yanlış girdiniz.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EskiSifre" });
                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EskiSifre" });
                }
                if (MmMessage.Messages.Count == 0)
                {

                    if (YeniSifre.Length < 4)
                    {
                        MmMessage.Messages.Add("Yeni şifreniz en az 4 haneli olmalıdır!");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifre" });
                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YeniSifre" });
                    if (YeniSifreTekrar.Length < 4)
                    {
                        MmMessage.Messages.Add("Yeni şifre tekrar en az 4 haneli olmalıdır!");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifreTekrar" });
                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifreTekrar" });
                    if (MmMessage.Messages.Count == 0)
                    {
                        if (YeniSifreTekrar != YeniSifre)
                        {
                            MmMessage.Messages.Add("Yeni şifre ile yeni şifre tekrar birbiriyle uyuşmuyor!");
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifreTekrar" });
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifre" });
                        }
                        else
                        {
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YeniSifre" });
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YeniSifreTekrar" });
                        }
                    }
                }

                if (MmMessage.Messages.Count == 0)
                {
                    kul.Sifre = YeniSifreTekrar.ComputeHash(Management.Tuz);
                    kul.ParolaSifirlamGecerlilikTarihi = DateTime.Now;
                    db.SaveChanges();
                    MmMessage.MessageType = Msgtype.Success;
                    MmMessage.Title = "Şifre değiştirme işlemi";
                    if (KullaniciID.HasValue == false)
                    {
                        MmMessage.Messages.Add("Şifreniz değiştirildi! Giriş sayfasına yönlendiriliyorsunuz...");
                        MmMessage.ReturnUrl = Url.Action("Login", "Account");
                    }
                    else
                    {
                        MmMessage.IsCloseDialog = true;
                        MmMessage.Messages.Add("Şifreniz değiştirildi!");
                    }
                }
                else
                {
                    MmMessage.MessageType = Msgtype.Error;
                    MmMessage.Title = "Şifre değiştirme işlemi başarısız!";
                    // Management.SistemBilgisiKaydet("Şifre değiştirme işlemi başarısız! Hata:" + string.Join("\r\n", MmMessage.Messages) + "\r\n KullanıcıAdı:" + kul.KullaniciAdi, "Account/ParolaSifirla", BilgiTipi.Bilgi);
                }
                kul.ResimAdi = kul.ResimAdi.ToKullaniciResim();
            }
            else
            {
                MmMessage.MessageType = Msgtype.Error;
                MmMessage.Title = "Şifre değiştirme işlemi başarısız!";
            }
            if (MmMessage.Messages.Count > 0)
            {
                if (UserIdentity.Current.IsAuthenticated)
                {
                    MessageBox.Show(MmMessage.Title, MmMessage.MessageType == Msgtype.Success ? MessageBox.MessageType.Success : MessageBox.MessageType.Error, MmMessage.Messages.ToArray());
                }
                else
                {
                    Session["ShwMesaj"] = MmMessage;
                }
            }


            ViewBag.MmMessage = MmMessage;
            ViewBag.KullaniciID = KullaniciID;
            ViewBag.EskiSifre = EskiSifre;
            ViewBag.YeniSifre = YeniSifre;
            ViewBag.YeniSifreTekrar = YeniSifreTekrar;
            return View(kul);
        }




        public ActionResult HesapKayit(int? id, string EKD)
        {
            var KayitYetki = RoleNames.KullanicilarKayit.InRoleCurrent();
            var MmMessage = new MmMessage();
            var model = new Kullanicilar();

            model.EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            model.IsAktif = true;
            bool IsKurumIci = true;
            bool IsYerli = true;
            bool ResimVar = false;

            if (UserIdentity.Current.IsAuthenticated)
            {
                if (!id.HasValue || id <= 0) id = UserIdentity.Current.Id;
                if (!KayitYetki && id != UserIdentity.Current.Id) id = UserIdentity.Current.Id;

                var data = db.Kullanicilars.Where(p => p.KullaniciID == id).FirstOrDefault();
                if (data != null)
                {
                    IsKurumIci = data.KullaniciTipleri.KurumIci;
                    IsYerli = data.KullaniciTipleri.Yerli;
                    ResimVar = data.ResimAdi.IsNullOrWhiteSpace() == false;
                    data.ResimAdi = data.ResimAdi;
                    model = data;

                }
                MmMessage.IsSuccess = true;
                model.Sifre = "";
            }
            else
            {
                if (id > 0) id = null;
            }
            if (RoleNames.KullanicilarKayit.InRoleCurrent())
            {
                ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            }
            else
            {
                ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            }
            ViewBag.KullaniciTipID = new SelectList(KullanicilarBus.GetCmbKullaniciTipleri(true, (KayitYetki ? false : true)), "Value", "Caption", model.KullaniciTipID);
            ViewBag.UnvanID = new SelectList(Management.cmbUnvanlar(true), "Value", "Caption", model.UnvanID);
            ViewBag.BirimID = new SelectList(Management.cmbBirimler(true), "Value", "Caption", model.BirimID);
            ViewBag.CinsiyetID = new SelectList(Management.cmbCinsiyetler(true), "Value", "Caption", model.CinsiyetID);
            ViewBag.OgrenimTipKod = new SelectList(Management.cmbAktifOgrenimTipleri(model.EnstituKod, true), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.ProgramKod = new SelectList(Management.cmbGetAktifProgramlar(model.EnstituKod, true, true), "Value", "Caption", model.ProgramKod);
            ViewBag.OgrenimDurumID = new SelectList(Management.cmbAktifOgrenimDurumu(true, IsHesapKayittaGozuksun: true), "Value", "Caption", model.OgrenimDurumID);
            ViewBag.YetkiGrupID = new SelectList(Management.cmbYetkiGruplari(), "Value", "Caption", model.YetkiGrupID);

            var kulTipi = db.KullaniciTipleris.Where(p => p.KullaniciTipID == model.KullaniciTipID).FirstOrDefault();

            if (kulTipi != null)
            {
                ViewBag.KullaniciTipAdi = kulTipi.KullaniciTipAdi;
            }
            else
            {
                ViewBag.KullaniciTipAdi = "";
            }

            ViewBag.IsKurumIci = IsKurumIci;
            ViewBag.IsYerli = IsYerli;
            ViewBag.ResimVar = ResimVar;
            return View(model);
        }
        [HttpPost]
        public ActionResult HesapKayit(Kullanicilar kModel, string EKD, bool IsKurumIci, bool IsYerli)
        {

            var MmMessage = new MmMessage();
            MmMessage.Title = kModel.KullaniciID > 0 ? "Kullanıcı Hesabı Güncelleme İşlemi" : "Yeni Kullanıcı Hesabı Oluşturma İşlemi";
            var resimBilgi = new CmbStringDto { Caption = "", Value = "" };
            var kKayit = RoleNames.KullanicilarKayit.InRoleCurrent();

            var _EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            var ErisimYetki = RoleNames.KullanicilarIslemYetkileri.InRoleCurrent();
            kModel.KullaniciAdi = kModel.KullaniciAdi != null ? kModel.KullaniciAdi.Trim() : "";
            #region Kontrol
            if (ErisimYetki)
            {
                if (kModel.YetkiGrupID <= 0)
                {
                    MmMessage.Messages.Add("Yetki Grubu Seçiniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YetkiGrupID" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YetkiGrupID" });
            }
            if (kModel.KullaniciTipID <= 0)
            {
                MmMessage.Messages.Add("Kullanıcı Tipi Seçiniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KullaniciTipID" });
            }
            else
            {
                var ktp = db.KullaniciTipleris.Where(p => p.KullaniciTipID == kModel.KullaniciTipID).First();
                IsKurumIci = ktp.KurumIci;
                IsYerli = ktp.Yerli;

                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "KullaniciTipID" });
            }

            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Enstitü Seçiniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });
            if (kModel.KullaniciTipID > 0)
            {
                if (kModel.ResimAdi.IsNullOrWhiteSpace())
                {
                    MmMessage.Messages.Add("Profil Resmi Yükleyiniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ResimAdi" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ResimAdi" });
                if (kModel.Ad.IsNullOrWhiteSpace())
                {
                    MmMessage.Messages.Add("Ad Giriniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Ad" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Ad" });
                if (kModel.Soyad.IsNullOrWhiteSpace())
                {
                    MmMessage.Messages.Add("Soyad Giriniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Soyad" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Soyad" });

                if (kModel.TcKimlikNo.IsNullOrWhiteSpace())
                {
                    MmMessage.Messages.Add("T.C. kimlik Numarası Giriniz.");

                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                }
                else if (kModel.TcKimlikNo.IsNumber() == false)
                {
                    MmMessage.Messages.Add("T.C. Kimlik Numarası Sadece Sayıdan Oluşmalıdır.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });

                }
                else if (kModel.TcKimlikNo.Length != 11)
                {
                    MmMessage.Messages.Add("T.C. Kimlik Numarası uzunluğu 11 Hane Olmalıdır.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });

                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TcKimlikNo" });
                if (!IsYerli)
                    if (kModel.PasaportNo.IsNullOrWhiteSpace())
                    {
                        MmMessage.Messages.Add("Pasaport No Giriniz.");

                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "PasaportNo" });
                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "PasaportNo" });
                if (!kModel.CinsiyetID.HasValue)
                {
                    MmMessage.Messages.Add("Cinsiyet Bilgisini Seçiniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CinsiyetID" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "CinsiyetID" });



                if (kModel.CepTel.IsNullOrWhiteSpace())
                {

                    MmMessage.Messages.Add("Cep Telefonu Numarası Giriniz");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CepTel" });
                }
                else
                {
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "CepTel" });
                }

                if (kModel.EMail.IsNullOrWhiteSpace())
                {

                    MmMessage.Messages.Add("E-Posta Bilgisini Giriniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                }
                else if (kModel.EMail.ToIsValidEmail())
                {
                    MmMessage.Messages.Add("Girilen E-Posta Formatı uygun Değildir.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                }
                else
                {

                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EMail" });
                }

                if (!IsKurumIci || !IsYerli)
                    if (kModel.Adres.IsNullOrWhiteSpace())
                    {
                        MmMessage.Messages.Add("Açık Adres Bilgisini Giriniz.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Adres" });
                    }
                    else
                    {
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Adres" });
                    }

                if (IsKurumIci)
                    if (!kModel.BirimID.HasValue)
                    {
                        MmMessage.Messages.Add("Birim Seçiniz.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BirimID" });
                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BirimID" });
                if (IsKurumIci)
                    if (!kModel.UnvanID.HasValue)
                    {
                        MmMessage.Messages.Add("Unvan Seçiniz.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "UnvanID" });
                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "UnvanID" });
                if (IsKurumIci)
                    if (kModel.SicilNo.IsNullOrWhiteSpace())
                    {
                        MmMessage.Messages.Add("Sicil No Giriniz.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SicilNo" });
                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SicilNo" });

                if (kModel.YtuOgrencisi)
                {
                    if (kModel.OgrenciNo.IsNullOrWhiteSpace())
                    {
                        MmMessage.Messages.Add("Öğrenci No Bilgisini Giriniz.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenciNo" });
                    }

                    else if (kModel.OgrenciNo.Length != 8)
                    {
                        MmMessage.Messages.Add("Öğrenci Numarası 8 Haneden Oluşmalıdır.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenciNo" });

                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenciNo" });
                    if (kModel.OgrenimTipKod.HasValue == false)
                    {
                        MmMessage.Messages.Add("Öğrenim Seviyesi Seçiniz.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimTipKod" });
                    if (kModel.ProgramKod.IsNullOrWhiteSpace())
                    {

                        MmMessage.Messages.Add("Program Seçiniz.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ProgramKod" });
                    if (kModel.OgrenimDurumID.HasValue == false)
                    {
                        MmMessage.Messages.Add("Öğrenim Durumu Seçiniz.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimDurumID" });
                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimDurumID" });

                }
                if (kKayit)
                {
                    if (kModel.KullaniciAdi.IsNullOrWhiteSpace())
                    {
                        MmMessage.Messages.Add("Kullanıcı Adı Giriniz.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KullaniciAdi" });
                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "KullaniciAdi" });


                    if (kModel.KullaniciID <= 0)
                    {
                        if (kModel.Sifre.IsNullOrWhiteSpace())
                        {

                            MmMessage.Messages.Add("Şifre Giriniz.");
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Sifre" });
                        }
                        else if (kModel.Sifre.Length < 4)
                        {

                            MmMessage.Messages.Add("Şifre En Az 4 Haneden Oluşmalıdır.");
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Sifre" });
                        }
                        else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Sifre" });
                    }
                    else if (!kModel.Sifre.IsNullOrWhiteSpace())
                    {
                        if (kModel.Sifre.Length < 4 && kModel.KullaniciID > 0)
                        {
                            MmMessage.Messages.Add("Şifre En Az 4 Haneden Oluşmalıdır.");
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Sifre" });
                        }
                        else if (kModel.Sifre.Length >= 4 && kModel.KullaniciID > 0) MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Sifre" });
                    }
                }
            }
            if (MmMessage.Messages.Count == 0)
            {
                var ktip = db.KullaniciTipleris.Where(p => p.KullaniciTipID == kModel.KullaniciTipID).First();
                IsKurumIci = ktip.KurumIci;
                IsYerli = ktip.Yerli;
                var qPersonel = db.Kullanicilars.AsQueryable();
                var Kul = db.Kullanicilars.Where(p => p.KullaniciID == kModel.KullaniciID).FirstOrDefault();

                var cUserName = qPersonel.Where(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.KullaniciAdi == kModel.KullaniciAdi).Count();
                if (cUserName > 0)
                {

                    MmMessage.Messages.Add("Tanımlamak istediğiniz kullanıcı adı sistemde zaten mevcut!");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KullaniciAdi" });
                }
                if (Kul == null || Kul.EMail.ToLower() != kModel.EMail.Trim().ToLower())
                {
                    var cEmail = qPersonel.Where(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.EMail == kModel.EMail).Count();
                    if (cEmail > 0)
                    {

                        MmMessage.Messages.Add("Tanımlamak istediğiniz E-Posta sistemde zaten mevcut!");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                    }
                }

                if (Kul == null || Kul.TcKimlikNo != kModel.TcKimlikNo.Trim())
                {
                    var cTc = qPersonel.Where(p =>
                            p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.TcKimlikNo == kModel.TcKimlikNo)
                        .Count();
                    if (cTc > 0)
                    {
                        MmMessage.Messages.Add("Tanımlamak istediğiniz Kimlik No sistemde zaten mevcut!");
                        MmMessage.MessagesDialog.Add(new MrMessage
                        { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                    }
                }

                if (kModel.KullaniciTipID == KullaniciTipBilgi.YabanciOgrenci)
                {
                    if (Kul == null || Kul.PasaportNo != kModel.PasaportNo.Trim())
                    {
                        var cTc = qPersonel.Where(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.PasaportNo == kModel.PasaportNo).Count();
                        if (cTc > 0)
                        {
                            MmMessage.Messages.Add("Tanımlamak istediğiniz Pasaport No sistemde zaten mevcut!");
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "PasaportNo" });
                        }
                    }
                }
                if (IsKurumIci)
                {
                    if (Kul == null || Kul.SicilNo != kModel.SicilNo.Trim())
                    {
                        var cSicil = qPersonel.Where(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.SicilNo == kModel.SicilNo).Count();
                        if (cSicil > 0)
                        {
                            MmMessage.Messages.Add("Tanımlamak istediğiniz Sicil No sistemde zaten mevcut!");
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SicilNo" });
                        }
                    }
                }
                if (kModel.YtuOgrencisi)
                {
                    if (Kul == null || Kul.OgrenciNo != kModel.OgrenciNo.Trim())
                    {
                        var cOgrNo = qPersonel.Where(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.OgrenciNo == kModel.OgrenciNo).Count();
                        if (cOgrNo > 0)
                        {
                            MmMessage.Messages.Add("Girmiş olduğunuz öğrenci numarası ile daha önceden sisteme kayıt yapılmıştır. Tekrar kayıt yapamazsınız!");
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenciNo" });
                        }
                    }
                    if (kModel.OgrenimDurumID != OgrenimDurum.OzelOgrenci)
                    {
                        var ogrenciBilgi = Management.StudentControl(kModel.TcKimlikNo);
                        if (ogrenciBilgi.Hata)
                        {
                            MmMessage.Messages.Add("Obs sisteminden öğrenci bilgisi sorgulanırken bir hata oluştu! " + ogrenciBilgi.HataMsj);
                        }
                        else
                        {
                            if (ogrenciBilgi.KayitVar && kModel.OgrenimTipKod == ogrenciBilgi.OgrenciInfo.OGRENIMSEVIYE_ID.toIntObj())
                            {
                                var Program = db.Programlars.Where(p => p.ProgramKod == kModel.ProgramKod).First();
                                kModel.ProgramKod = Program.ProgramKod;
                                kModel.OgrenimTipKod = ogrenciBilgi.OgrenciInfo.OGRENIMSEVIYE_ID.toIntObj().Value;
                                kModel.KayitTarihi = ogrenciBilgi.KayitTarihi;
                                kModel.KayitYilBaslangic = ogrenciBilgi.BaslangicYil;
                                kModel.KayitDonemID = ogrenciBilgi.DonemID;
                            }
                            else
                            {
                                MmMessage.Messages.Add(
                                    "Girdiğiniz Kimlik bilgisi OBS sisteminde doğrulanamadı.");
                                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                            }

                        }
                    }

                }
            }
            if (MmMessage.Messages.Count == 0)
            {
                if (IsKurumIci && kModel.KullaniciID <= 0)
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
            if (MmMessage.Messages.Count == 0 && IsKurumIci)
            {
                if (kModel.IsActiveDirectoryUser && kModel.EMail.Contains("@yildiz.edu.tr") == false)
                {
                    MmMessage.Messages.Add("Active Directori Girişi Yapmasını İstediğiniz Kullanıcının yildiz.edu.tr uzantılı mailini tanımlamanız gerekir!");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsActiveDirectoryUser" });
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                }

            }
            if (MmMessage.Messages.Count == 0 && kModel.KullaniciTipID == KullaniciTipBilgi.YerliOgrenci)
            {
                //var kimlikDogrulama = SistemAyar.getAyar(SistemAyar.KullaniciHesapKaydiKimlikDogrula).ToBoolean().Value;
                //if (kimlikDogrulama && kModel.DogumYeriKod != 9100)
                //{
                //    Ws_KimlikDogrula.KPSPublicV2SoapClient kimlik = new Ws_KimlikDogrula.KPSPublicV2SoapClient();
                //    long tc = long.Parse(kModel.TcKimlikNo);
                //    string adi = kModel.Ad.ToUpper();
                //    string soyadi = kModel.Soyad.ToUpper();
                //    var dogumTar = kModel.DogumTarihi.Value;
                //    try
                //    {
                //        var sonuc = kimlik.KisiVeCuzdanDogrula(tc, adi, soyadi, false, dogumTar.Day, false, dogumTar.Month, false, dogumTar.Year, null, null, null);
                //        if (!sonuc)
                //        {
                //            MmMessage.Messages.Add("tckimlik.nvi.gov.tr tarafından sağlanan kimlik doğrulama sevisine göre girmiş olduğunuz bilgiler doğrulanamadı. Lütfen TC kimlik no, Ad, Soyad ve Doğum tarihi bilgilerini doğru girdiğinizden emin olunuz.");
                //            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Ad" });
                //            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Soyad" });
                //            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                //            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DogumTarihi" });
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        var msg = "'tckimlik.nvi.gov.tr' aracılığı ile Kimlik bilgisi doğrulanırken bir hata oluştu. <br/>" + "Hata" + ", " + ex.ToExceptionMessage().Trim();
                //        MmMessage.Messages.Add(msg);
                //        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                //    }

                //}
            }
            #endregion
            if (kModel.KullaniciID <= 0 && MmMessage.Messages.Count == 0)
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
                    MmMessage.Messages.Add("Mail gönderme hatası, Hesap oluşturulamadı!  Hata" + " : " + excpt.ToExceptionMessage());
                }
            }


            if (MmMessage.Messages.Count == 0)
            {
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.Ad = kModel.Ad.Trim();
                kModel.Soyad = kModel.Soyad.Trim();
                if (!IsKurumIci)
                {
                    kModel.BirimID = null;
                    kModel.UnvanID = null;
                    kModel.SicilNo = "";

                }
                if (IsKurumIci)
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
                var _SifreUnCrypet = kModel.Sifre;
                var YeniKullanici = kModel.KullaniciID <= 0;
                if (YeniKullanici)
                {
                    var DanismanUnvanIDs = new List<int>() { 17, 42, 73 }; //Doç.Dr Prof.Dr, Dr. Öğr. Üye
                    kModel.YetkiGrupID = ErisimYetki ? kModel.YetkiGrupID : (kModel.KullaniciTipID == KullaniciTipBilgi.AkademikPersonel && DanismanUnvanIDs.Contains(kModel.UnvanID ?? 0) ? 6 : 1);//danışman yetkisi vermek için
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
                    kModel = db.Kullanicilars.Add(kModel);
                    db.SaveChanges();

                    db.KullaniciEnstituYetkileris.Add(new KullaniciEnstituYetkileri
                    {
                        EnstituKod = kModel.EnstituKod,
                        KullaniciID = kModel.KullaniciID,
                        IslemYapanID = kModel.KullaniciID,
                        IslemTarihi = DateTime.Now,
                        IslemYapanIP = UserIdentity.Ip

                    });
                    db.SaveChanges();

                    MmMessage.IsCloseDialog = true;
                    MmMessage.IsSuccess = true;
                    MmMessage.MessageType = Msgtype.Success;
                    MmMessage.Messages.Add("Kullanıcı hesabı oluşturuldu!");
                    MmMessage.Messages.Add("Hesap bilgileri " + kModel.EMail + " E-Posta adresinize gönderildi.");
                    MmMessage.Messages.Add("Not: Sistem üzerinden mail hesabınıza mail gönderilememe durumuna karşı aşağıdaki şifreyi lütfen kopyalayınız ve sisteme giriş için bu şifreyi kullanınız.");

                    if (kModel.IsActiveDirectoryUser == false) MmMessage.Messages.Add("KullanıcıAdı:" + kModel.KullaniciAdi + " Şifre: " + _SifreUnCrypet);
                    else MmMessage.Messages.Add("KullanıcıAdı:" + kModel.KullaniciAdi + " Şifre: E-Posta şifreniz ile aynı");
                }
                else
                {
                    var data = db.Kullanicilars.Where(p => p.KullaniciID == kModel.KullaniciID).First();
                    data.EnstituKod = kModel.EnstituKod;
                    if (ErisimYetki) data.YetkiGrupID = kModel.YetkiGrupID;
                    data.KullaniciTipID = kModel.KullaniciTipID;
                    data.Ad = kModel.Ad;
                    data.Soyad = kModel.Soyad;
                    data.TcKimlikNo = kModel.TcKimlikNo;
                    data.PasaportNo = kModel.PasaportNo;
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
                    db.SaveChanges();
                    MmMessage.Messages.Add("'" + data.Ad + " " + data.Soyad + "' Kullanıcı hesabı güncellendi.");
                    MmMessage.IsCloseDialog = true;
                    MmMessage.IsSuccess = true;
                    MmMessage.MessageType = Msgtype.Success;
                    LogIslemleri.LogEkle("Kullanicilar", IslemTipi.Update, data.ToJson());
                }
            }
            else
            {
                MmMessage.Title = kModel.KullaniciID > 0 ? "Kullanıcı Hesabı Güncelleme İşlemi İçin Aşağıdaki Uyarıları Kontrol Ediniz" : "Yeni Kullanıcı Hesabı Oluşturma İşlemi İçin Aşağıdaki Uyarıları Kontrol Ediniz.";
                MmMessage.MessageType = Msgtype.Warning;
            }
            return MmMessage.ToJsonResult();
        }


        [HttpPost]
        public ActionResult ImageUploadPost(int? KullaniciID, HttpPostedFileBase KImage, string EskiResimAdi = "")
        {
            var mMessage = new MmMessage();
            string YeniResimAdi = "";
            mMessage.Title = "Profil resmi yükleme işlemi";
            mMessage.IsSuccess = false;
            mMessage.MessageType = Msgtype.Warning;
            if (KImage == null || KImage.ContentLength <= 0)
            {
                mMessage.Messages.Add("Profil Resmi Seçiniz");
                // MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProfilResmi" });
            }
            else if (RoleNames.KullanicilarKayit.InRoleCurrent() == false && KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Başka bir kullanıcı adına resim yüklemesi yapmaya yetkili değilsiniz.");
            }
            else
            {
                var contentlength = KImage.ContentLength;
                string uzanti = KImage.FileName.GetFileExtension();
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

                        var kul = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).First();
                        EskiResimAdi = kul.ResimAdi;

                        kul.ResimAdi = YeniResimAdi = KullanicilarBus.ResimKaydet(KImage);
                        kul.IslemYapanID = UserIdentity.Current.Id;
                        kul.IslemYapanIP = UserIdentity.Ip;
                        kul.IslemTarihi = DateTime.Now;
                        db.SaveChanges();


                        if (EskiResimAdi.IsNullOrWhiteSpace() == false)
                        {
                            var ResimKlasorAdi = SistemAyar.KullaniciResimYolu;
                            var rsm = Server.MapPath("~/" + ResimKlasorAdi + "/" + EskiResimAdi);
                            try
                            {
                                if (System.IO.File.Exists(rsm)) System.IO.File.Delete(rsm);
                            }
                            catch (Exception)
                            {
                            }
                        }
                        mMessage.IsSuccess = true;
                        mMessage.Messages.Add("profil resmi sisteme yüklendi.");
                    }
                    else
                    {
                        YeniResimAdi = KullanicilarBus.ResimKaydet(KImage);
                        if (EskiResimAdi.IsNullOrWhiteSpace() == false)
                        {
                            var ResimKlasorAdi = SistemAyar.KullaniciResimYolu;
                            var rsm = Server.MapPath("~/" + ResimKlasorAdi + "/" + EskiResimAdi);
                            try
                            {
                                if (System.IO.File.Exists(rsm)) System.IO.File.Delete(rsm);
                            }
                            catch (Exception)
                            {
                            }

                        }
                        mMessage.IsSuccess = true;
                        mMessage.Messages.Add("profil resmi sisteme yüklendi.");
                    }

                }
            }
            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            var YeniResimYolu = YeniResimAdi.ToKullaniciResim();
            UserIdentity.Current.ImagePath = YeniResimYolu;
            return new { mMessage, YeniResimAdi, YeniResimYolu }.ToJsonResult();
        }
        [HttpPost]
        public ActionResult RotateImage(bool LeftOrRight, string ResimAdi)
        {

            if (ResimAdi.IsNullOrWhiteSpace() == false)
            {
                var ResimKlasorAdi = SistemAyar.KullaniciResimYolu;
                var rsm = Server.MapPath("~/" + ResimKlasorAdi + "/" + ResimAdi);
                if (System.IO.File.Exists(rsm))
                {
                    using (Image img = Image.FromFile(rsm))
                    {
                        img.RotateFlip(LeftOrRight ? RotateFlipType.Rotate270FlipNone : RotateFlipType.Rotate90FlipNone);
                        img.Save(rsm, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                }

            }
            var YeniResimYolu = ResimAdi.ToKullaniciResim();

            return new { YeniResimYolu }.ToJsonResult();
        }


        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
