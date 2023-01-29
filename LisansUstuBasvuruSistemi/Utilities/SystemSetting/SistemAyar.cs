using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; 
using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{
    public static class SistemAyar
    {
        //public const string AyarSMTP_Host = "Smtp Host Adresi";
        //public const string AyarSMTP_Port = "Smtp Port Adresi";
        //public const string AyarSMTP_SSL = "Smtp SSL";
        //public const string AyarSMTP_Mail = "Smtp Mail Adresi";
        //public const string AyarSMTP_User = "Smtp Kullanıcı Adı";
        //public const string AyarSMTP_Password = "Smtp Şifre";
        //public const string AyarSistemErisimAdresi = "Sistem Erişim Adresi"; 
        public const string AyarYeniKullaniciMailIcerik = "Yeni Kullanıcı Mail İçeriği";
        public const string AyarYeniBasvuruYarbisPuanCek = "Başvuruda Yarbis Puanını Otomatik Çek";
        public const string AyarBasvuruEvlilikDurumuMaddeKodu = "Başvuru Evlilik Durumu Madde Kodu";
        public const string AyarBasvuruIdariGorevMaddeleri = "Başvuru İdari Görev Maddeleri";
        public const string AyarBasvuruFertSayisiMaddeleri = "Başvuru Fert Sayısı Maddeleri";
        public const string AyarYeniBasvuruDogrulukKontroluYap = "Başvuru Kaydında Doğruluk Kontrolü Yap";
        public const string AyarBasvuruHizmetYiliMaddeKodu = "Hizmet Yılı Madde Kodu";
        public const string KullaniciHesapKaydiKimlikDogrula = "Hesap Kaydında Kimlik Doğrulaması Yap";
        public const string KullaniciResimYolu = "Images/KullaniciResimleri";
        public const string KullaniciDefaultResim = "Images/whoisUsr.png";
        public const string AyarOsymWSKullaniciAdi = "ÖSYM Web Servisi Kullanıcı Adı";
        public const string AyarOsymWSKullaniciSifre = "ÖSYM Web Servisi Kullanıcı Şifre";

        public const string AyarYOKWSKullaniciAdi = "YÖK Web Servisi Kullanıcı Adı";
        public const string AyarYOKWSKullaniciSifre = "YÖK Web Servisi Kullanıcı Şifre";

        public const string KullaniciResimKaydiBoyutlandirma = "Resim Kaydında Boyutlandırma Yap";
        public const string KullaniciResimKaydiWidthPx = "Resim Kaydı Width (Px)";
        public const string KullaniciResimKaydiHeightPx = "Resim Kaydı Height (Px)";
        public const string KullaniciResimKaydiKaliteOpt = "Resim Kaydında Kalite Optimizasyonu Yap";
        public const string RotasyonuDegisenResimleriLogla = "Rotasyonu Değişen Resimleri Logla";

        public const string OtomatikMailBilgilendirmeServisiniCalistir = "Otomatik Mail Bilgilendirme Servisini Çalıştır";
        public static void setAyar(string AyarAdi, string AyarDegeri)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qq = db.Ayarlars.Where(p => p.AyarAdi == AyarAdi).FirstOrDefault();
                if (qq != null)
                {
                    qq.AyarDegeri = AyarDegeri;
                }
                else
                {
                    db.Ayarlars.Add(new Ayarlar { AyarAdi = AyarAdi, AyarDegeri = AyarDegeri });

                }
                db.SaveChanges();
            }

        }
        public static string getAyar(this string AyarAdi, string VarsayilanDeger = "")
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qq = db.Ayarlars.Where(p => p.AyarAdi == AyarAdi).FirstOrDefault();
                if (qq != null)
                {
                    return qq.AyarDegeri;
                }
                else
                {
                    return VarsayilanDeger;

                }
            }
        }
    }
    public static class EnstituMailInfo
    {
        public static Enstituler GetEnstituMailBilgisi(string EnstituKod)
        {

            return Management.Enstitulers.Where(p => p.EnstituKod == EnstituKod).First();


        }

    }
}