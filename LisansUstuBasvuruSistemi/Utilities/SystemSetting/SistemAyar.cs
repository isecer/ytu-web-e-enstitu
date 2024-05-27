using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;

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
        public const string AyarOsymWsKullaniciAdi = "ÖSYM Web Servisi Kullanıcı Adı";
        public const string AyarOsymWsKullaniciSifre = "ÖSYM Web Servisi Kullanıcı Şifre";


        public const string DanismanProfilindeOgrencileriniGorsun = "Danışmanlar Profillerinde Öğrencilerini Görsün";


        public const string AyarYokwsKullaniciAdi = "YÖK Web Servisi Kullanıcı Adı";
        public const string AyarYokwsKullaniciSifre = "YÖK Web Servisi Kullanıcı Şifre";

        public const string KullaniciResimKaydiBoyutlandirma = "Resim Kaydında Boyutlandırma Yap";
        public const string KullaniciResimKaydiWidthPx = "Resim Kaydı Width (Px)";
        public const string KullaniciResimKaydiHeightPx = "Resim Kaydı Height (Px)";
        public const string KullaniciResimKaydiKaliteOpt = "Resim Kaydında Kalite Optimizasyonu Yap";
        public const string RotasyonuDegisenResimleriLogla = "Rotasyonu Değişen Resimleri Logla";

        public const string OtomatikMailBilgilendirmeServisiniCalistir = "Otomatik Mail Bilgilendirme Servisini Çalıştır";
        public const string OtomatikObsOgrenciKontrolServisiniCalistir = "Otomatik Obs Öğrenci Kontrol Servisini Çalıştır";



        public const string DosyalarSecilenKonumaArsivlensin = "Dosyalar Seçilen Konumda Arşivlensin";
        public const string DosyaArsiviSunucusuErisimAdresi = "Dosya Arşivi Sunucusu Erişim Adresi";
        public const string DosyaArsiviFizikselKayitYolu = "Dosya Arşivi Fiziksel Kayıt Yolu";
        public static void SetAyar(string ayarAdi, string ayarDegeri)
        {
            using (var entities = new LubsDbEntities())
            {
                var qq = entities.Ayarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi);
                if (qq != null)
                {
                    qq.AyarDegeri = ayarDegeri;
                }
                else
                {
                    entities.Ayarlars.Add(new Ayarlar { AyarAdi = ayarAdi, AyarDegeri = ayarDegeri });

                }
                entities.SaveChanges();
            }

        }
        public static string GetAyar(this string ayarAdi, string varsayilanDeger = "")
        {
            using (var entities = new LubsDbEntities())
            {
                var qq = entities.Ayarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi);
                if (qq != null)
                {
                    return qq.AyarDegeri;
                }
                else
                {
                    return varsayilanDeger;

                }
            }
        }
    }
    public static class EnstituMailInfo
    {
        public static Enstituler GetEnstituMailBilgisi(string enstituKod)
        {

            return EnstituBus.Enstitulers.First(p => p.EnstituKod == enstituKod);


        }

    }
}