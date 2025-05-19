using System.Linq;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{
    public static class SistemAyar
    {
        public class SistemAyarProperty
        {
            internal string PropertyValue { get; }

            internal SistemAyarProperty(string value)
            {
                PropertyValue = value;
            }

            public static implicit operator string(SistemAyarProperty property)
            {
                return property.PropertyValue;
            }
        }

        public const string KullaniciResimYolu = "Images/KullaniciResimleri";
        public const string KullaniciDefaultResim = "Images/whoisUsr.png"; 

        public static readonly SistemAyarProperty AyarOsymWsKullaniciAdi = new SistemAyarProperty("ÖSYM Web Servisi Kullanıcı Adı");
        public static readonly SistemAyarProperty AyarOsymWsKullaniciSifre = new SistemAyarProperty("ÖSYM Web Servisi Kullanıcı Şifre");
        public static readonly SistemAyarProperty DanismanProfilindeOgrencileriniGorsun = new SistemAyarProperty("Danışmanlar Profillerinde Öğrencilerini Görsün");
        public static readonly SistemAyarProperty AyarYokwsKullaniciAdi = new SistemAyarProperty("YÖK Web Servisi Kullanıcı Adı");
        public static readonly SistemAyarProperty AyarYokwsKullaniciSifre = new SistemAyarProperty("YÖK Web Servisi Kullanıcı Şifre");
        public static readonly SistemAyarProperty KullaniciResimKaydiBoyutlandirma = new SistemAyarProperty("Resim Kaydında Boyutlandırma Yap");
        public static readonly SistemAyarProperty KullaniciResimKaydiWidthPx = new SistemAyarProperty("Resim Kaydı Width (Px)");
        public static readonly SistemAyarProperty KullaniciResimKaydiHeightPx = new SistemAyarProperty("Resim Kaydı Height (Px)");
        public static readonly SistemAyarProperty KullaniciResimKaydiKaliteOpt = new SistemAyarProperty("Resim Kaydında Kalite Optimizasyonu Yap");
        public static readonly SistemAyarProperty RotasyonuDegisenResimleriLogla = new SistemAyarProperty("Rotasyonu Değişen Resimleri Logla");
        public static readonly SistemAyarProperty OtomatikMailBilgilendirmeServisiniCalistir = new SistemAyarProperty("Otomatik Mail Bilgilendirme Servisini Çalıştır");
        public static readonly SistemAyarProperty OtomatikObsOgrenciKontrolServisiniCalistir = new SistemAyarProperty("Otomatik Obs Öğrenci Kontrol Servisini Çalıştır");
        public static readonly SistemAyarProperty DosyalarSecilenKonumaArsivlensin = new SistemAyarProperty("Dosyalar Seçilen Konumda Arşivlensin");
        public static readonly SistemAyarProperty DosyaArsiviSunucusuErisimAdresi = new SistemAyarProperty("Dosya Arşivi Sunucusu Erişim Adresi");
        public static readonly SistemAyarProperty DosyaArsiviFizikselKayitYolu = new SistemAyarProperty("Dosya Arşivi Fiziksel Kayıt Yolu");


        public static string GetAyar(this SistemAyarProperty ayarProperty, string varsayilanDeger = "")
        {
            using (var entities = new LubsDbEntities())
            {
                var qq = entities.Ayarlars.FirstOrDefault(p => p.AyarAdi == ayarProperty.PropertyValue);
                return qq != null ? qq.AyarDegeri : varsayilanDeger;
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
