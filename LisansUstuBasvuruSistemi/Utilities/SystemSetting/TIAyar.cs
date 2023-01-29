using LisansUstuBasvuruSistemi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{

    public static class TIAyar
    {

        public const string BasvurusuAcikmi = "Başvuru Alımı Açık";
        public const string SonDonemKayitOlunmasiGerekenDersKodlari = "Son Dönem Kayıt Olunması Gereken Ders Kodları";
        public const string UyelerMinSinavPuan = "Üyeler için Dil Sınavı Kabulü Min Puan";
        public const string OgrenciMinSinavPuan = "Öğrenci için Dil Sınavı Kabulü Min Puan";
        public const string SinavPuanGirisKontroluYapilsin = "Sınav Puan Giriş Kontrolü Yapılsın";

        public static void setAyarTI(string AyarAdi, string AyarDegeri, string EnstituKod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qq = db.TIAyarlars.Where(p => p.AyarAdi == AyarAdi && p.EnstituKod == EnstituKod).FirstOrDefault();
                if (qq != null)
                {
                    qq.AyarDegeri = AyarDegeri;
                }
                else
                {
                    db.TIAyarlars.Add(new TIAyarlar { AyarAdi = AyarAdi, AyarDegeri = AyarDegeri });

                }
                db.SaveChanges();
            }

        }
        public static string getAyarTI(this string AyarAdi, string EnstituKodu, string VarsayilanDeger = "")
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qq = db.TIAyarlars.Where(p => p.AyarAdi == AyarAdi && p.EnstituKod == EnstituKodu).FirstOrDefault();
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


}