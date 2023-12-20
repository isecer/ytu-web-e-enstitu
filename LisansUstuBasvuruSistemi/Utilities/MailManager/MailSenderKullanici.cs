using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
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
                using (var db = new LisansustuBasvuruSistemiEntities())
                {

                    var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(kModel.EnstituKod);
                    var mRowModel = new List<MailTableRowDto>();
                    var enstitu = db.Enstitulers.First(p => p.EnstituKod == kModel.EnstituKod);


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
                        var birim = db.Birimlers.First(p => p.BirimID == kModel.BirimID);
                        mRowModel.Add(new MailTableRowDto { Baslik = "Birim", Aciklama = birim.BirimAdi });
                    }

                    if (kModel.UnvanID.HasValue)
                    {
                        var unvan = db.Unvanlars.First(p => p.UnvanID == kModel.UnvanID);
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
                    var tableContent = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", mtc);
                    var mmmC = new MailMainContentDto
                    {
                        EnstituAdi = enstitu.EnstituAd,
                        Content = tableContent,
                        LogoPath = erisimAdresi + "/Content/assets/images/ytu_logo_tr.png",
                        UniversiteAdi = "Yıldız Tekni Üniversitesi"
                    };
                    var htmlMail = ViewRenderHelper.RenderPartialView("Ajax", "getMailContent", mmmC);
                    MailManager.SendMail(enstitu.EnstituKod, "Yeni Kullanıcı Hesabınız Hakkında", htmlMail, kModel.EMail, null);
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;
                    return mmMessage;

                }
            }
            catch (Exception ex)
            {

                var message = "Mail gönderme hatası, Hesap oluşturulamadı!  Hata" + " : " + ex.ToExceptionMessage();
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                mmMessage.Messages.Add(message);
                mmMessage.MessageType = MsgTypeEnum.Error;

            }

            return mmMessage;
        }
    }
}