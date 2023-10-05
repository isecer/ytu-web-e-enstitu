using LisansUstuBasvuruSistemi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{

    public static class TiAyar
    {
        public const string TikOneriAlimiAcik = "Tik Öneri Alımı Açık";
        public const string TikOneriDegisiklikAlimiAcik = "Tik Öneri Değişikliği Alımı Açık";
        public const string TezOneriSavunmaBasvuruAlimiAcik = "Tez Öneri Savunması Başvuru Alımı Açık"; 

        public const string BasvurusuAcikmi = "Başvuru Alımı Açık";
        public const string SonDonemKayitOlunmasiGerekenDersKodlari = "Son Dönem Kayıt Olunması Gereken Ders Kodları";
        public const string UyelerMinSinavPuan = "Üyeler için Dil Sınavı Kabulü Min Puan";
        public const string OgrenciMinSinavPuan = "Öğrenci için Dil Sınavı Kabulü Min Puan";
        public const string SinavPuanGirisKontroluYapilsin = "Sınav Puan Giriş Kontrolü Yapılsın";

        public static void SetAyarTi(string ayarAdi, string ayarDegeri, string enstituKod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qq = db.TIAyarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi && p.EnstituKod == enstituKod);
                if (qq != null)
                {
                    qq.AyarDegeri = ayarDegeri;
                }
                else
                {
                    db.TIAyarlars.Add(new TIAyarlar { AyarAdi = ayarAdi, AyarDegeri = ayarDegeri });

                }
                db.SaveChanges();
            }

        }
        public static string GetAyarTi(this string ayarAdi, string enstituKodu, string varsayilanDeger = "")
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qq = db.TIAyarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi && p.EnstituKod == enstituKodu);
                return qq != null ? qq.AyarDegeri : varsayilanDeger;
            }
        }
    }


}