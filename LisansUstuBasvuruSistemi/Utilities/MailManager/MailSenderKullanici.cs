using System;
using System.Collections.Generic;
using System.Linq;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.MailManager
{
    public class MailSenderKullanici
    {
        public static MmMessage SendMailYeniHesap(Kullanicilar kModel, string sfr)
        {
            var mmMessage = new MmMessage();
            try
            {
               using (var entities = new LubsDbEntities())
                {

                    var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(kModel.EnstituKod);
                    var mRowModel = new List<MailTableRowDto>();
                    var enstitu = entities.Enstitulers.First(p => p.EnstituKod == kModel.EnstituKod);


                    var erisimAdresi = mailBilgi.SistemErisimAdresi;
                    var erisimAdresiSpl = erisimAdresi.Split('/').ToList();
                    if (erisimAdresi.Contains("//"))
                        erisimAdresi = erisimAdresiSpl[0] + "//" + erisimAdresiSpl.Skip(2).Take(1).First();
                    else
                        erisimAdresi = "http://" + erisimAdresiSpl.First();
                    mRowModel.Add(
                        new MailTableRowDto { Baslik = "Ad Soyad", Aciklama = kModel.Ad + " " + kModel.Soyad });

                    if (kModel.BirimID.HasValue)
                    {
                        var birim = entities.Birimlers.First(p => p.BirimID == kModel.BirimID);
                        mRowModel.Add(new MailTableRowDto { Baslik = "Birim", Aciklama = birim.BirimAdi });
                    }

                    if (kModel.UnvanID.HasValue)
                    {
                        var unvan = entities.Unvanlars.First(p => p.UnvanID == kModel.UnvanID);
                        mRowModel.Add(new MailTableRowDto { Baslik = "Unvan", Aciklama = unvan.UnvanAdi });
                    }

                    if (kModel.SicilNo.IsNullOrWhiteSpace() == false)
                        mRowModel.Add(new MailTableRowDto { Baslik = "Sicil No", Aciklama = kModel.SicilNo });
                    if (kModel.TcKimlikNo.IsNullOrWhiteSpace() == false)
                        mRowModel.Add(new MailTableRowDto { Baslik = "Tc kimlik No", Aciklama = kModel.TcKimlikNo });
                    if (kModel.CepTel.IsNullOrWhiteSpace() == false)
                        mRowModel.Add(new MailTableRowDto { Baslik = "Cep Tel", Aciklama = kModel.CepTel });

                    mRowModel.Add(new MailTableRowDto { Baslik = "Kullanıcı Adı", Aciklama = kModel.KullaniciAdi });
                    mRowModel.Add(new MailTableRowDto
                    {
                        Baslik = "Şifre",
                        Aciklama = kModel.IsActiveDirectoryUser ? "Email şifreniz ile aynı" : sfr
                    });
                    mRowModel.Add(new MailTableRowDto
                    {
                        Baslik = "Sistem Erişim Adresi",
                        Aciklama = "<a href='" + mailBilgi.SistemErisimAdresi + "' target='_blank'>" +
                                   mailBilgi.SistemErisimAdresi + "</a>"
                    });
                    var mtc = new MailTableContentDto
                    {
                        AciklamaBasligi = "Kullanıcı hesabınız oluşturuldu. Sisteme Giriş Bilgisi Aşağıdaki Gibidir.",
                        Detaylar = mRowModel
                    };

                    var tableContent = ViewRenderHelper.RenderPartialView("Ajax", "GetMailTableContent", mtc);
                    var mmmC = new MailMainContentDto
                    {
                        EnstituAdi = enstitu.EnstituAd,
                        Content = tableContent,
                        LogoPath = erisimAdresi + "/Content/assets/images/ytu_logo_tr.png",
                        UniversiteAdi = "Yıldız Tekni Üniversitesi",
                        WebAdresi = mailBilgi.WebAdresi
                    };
                    var htmlMail = ViewRenderHelper.RenderPartialView("Ajax", "GetMailContent", mmmC);
                    MailManager.SendMail(enstitu.EnstituKod, "Yeni Kullanıcı Hesabınız Hakkında", htmlMail, kModel.EMail, null);
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;
                    return mmMessage;

                }
            }
            catch (Exception ex)
            {

                var message = "Mail gönderme hatası, Hesap oluşturulamadı!  Hata" + " : " + ex.ToExceptionMessage();
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message);
                mmMessage.MessageType = MsgTypeEnum.Error;

            }

            return mmMessage;
        }
        public static MmMessage SendMailSifreSifirla(int kullaniciId)
        {
            var mmMessage = new MmMessage();
            try
            {
               using (var entities = new LubsDbEntities())
                {
                    var kul = entities.Kullanicilars.First(f => f.KullaniciID == kullaniciId);
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
                        sistemErisimAdresi = "http://" + wurlAddr.First();

                    var mmmC = new MailMainContentDto
                    {
                        EnstituAdi = entities.Enstitulers.First(p => p.EnstituKod == kul.EnstituKod).EnstituAd,
                        UniversiteAdi = "Yıldız Teknik Üniversitesi",
                        WebAdresi = mailBilgi.WebAdresi,
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
                        entities.SaveChanges();
                    }
                    else
                    {
                        mmMessage.IsSuccess = false;
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Şifre sıfırlama! Hata: " + rtVal.ToExceptionMessage(), rtVal.ToExceptionStackTrace(), BilgiTipiEnum.Hata, kul.KullaniciID, UserIdentity.Ip);
                        mmMessage.Title = "Şifre sıfırlama linki '" + kul.EMail + "' adresine gönderilemedi!";
                    }

                }
            }
            catch (Exception ex)
            {

                var message = "Mail gönderme hatası, Hesap oluşturulamadı!  Hata" + " : " + ex.ToExceptionMessage();
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message);
                mmMessage.MessageType = MsgTypeEnum.Error;

            }

            return mmMessage;
        }
    }
}