using LisansUstuBasvuruSistemi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{

    public static class TiAyar
    {
        public const string TikOneriAlimiAcik = "Tik öneri alımı açık"; 
        public const string TiAraRaporSinaviOnlineYapilabilsin = "Tez izleme ara rapor sınavı online yapılabilsin";
        public const string TezOneriSavunmaBasvuruAlimiAcik = "Tez öneri savunma sınavı başvuru alımı açık";
        public const string TezOneriSavunmaSinaviOnlineYapilabilsin= "Tez öneri savunma sınavı online yapılabilsin"; 
        public const string TezOneriToplamBasarisizTezOneriSavunmaHak = "Toplam başarısız tez öneri savunma sınavı hakkı";
        public const string TezOneriDuzeltmeSonrasiSavunmaHakkiAyKriter = "Düzeltme alan öğrenci yeni sınavı kaç ay içinde amalı";
        public const string TezOneriIlkSavunmaHakkiAyKriter= "ilk başvuru için 1. Savunma kaç ay içinde yapılmalı";
        public const string TezOneriIkinciSavunmaHakkiAyKriter = "ilk başvuru için 2. Savunma kaç ay içinde yapılmalı";
        
        public const string BasvurusuAcikmi = "Başvuru alımı açık";
        public const string SonDonemKayitOlunmasiGerekenDersKodlari = "Son dönem kayıt olunması gereken ders kodları";
        public const string UyelerMinSinavPuan = "Üyeler için dil sınavı kabulü min puan";
        public const string OgrenciMinSinavPuan = "Öğrenci için dil sınavı kabulü min puan";
        public const string SinavPuanGirisKontroluYapilsin = "Sınav puan giriş kontrolü yapılsın";

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