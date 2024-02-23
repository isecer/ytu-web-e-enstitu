using Entities.Entities;
using System.Linq;

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{

    public static class TiAyar
    {
        public const string TikOneriAlimiAcik = "Tik öneri alımı açık";

        public const string TezOneriSavunmaBasvuruAlimiAcik = "Tez öneri savunma sınavı başvuru alımı açık";
        public const string TezOneriSavunmaSinaviOnlineYapilabilsin = "Tez öneri savunma sınavı online yapılabilsin";
        public const string TezOneriSavunmaSinaviYuzYuzeYapilabilsin = "Tez öneri savunma sınavı yüz yüze yapılabilsin";
        public const string TezOneriToplamBasarisizTezOneriSavunmaHak = "Toplam başarısız tez öneri savunma sınavı hakkı";
        public const string TezOneriDuzeltmeSonrasiSavunmaHakkiAyKriter = "Düzeltme alan öğrenci yeni sınavı kaç ay içinde amalı";
        public const string TezOneriIlkSavunmaHakkiAyKriter = "ilk başvuru için 1. Savunma kaç ay içinde yapılmalı";
        public const string TezOneriIkinciSavunmaHakkiAyKriter = "ilk başvuru için 2. Savunma kaç ay içinde yapılmalı";

        public const string TiBasvurusuAcikmi = "Ara rapor başvuru alımı açık";

        public const string TiSonDonemKayitOlunmasiGerekenDersKodlari = "Son dönem kayıt olunması gereken ders kodları";
        public const string TiUyelerMinSinavPuan = "Üyeler için dil sınavı kabulü min puan";
        public const string TiOgrenciMinSinavPuan = "Öğrenci için dil sınavı kabulü min puan";
        public const string TiSinavPuanGirisKontroluYapilsin = "Sınav puan giriş kontrolü yapılsın";
        public const string TiAraRaporSinaviOnlineYapilabilsin = "Tez izleme ara rapor sınavı online yapılabilsin";
        public const string TiAraRaporSinaviYuzYuzeYapilabilsin = "Tez izleme ara rapor sınavı yüz yüze yapılabilsin";

        public static void SetAyarTi(string ayarAdi, string ayarDegeri, string enstituKod)
        {
            using (var entities = new LubsDbEntities())
            {
                var qq = entities.TIAyarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi && p.EnstituKod == enstituKod);
                if (qq != null)
                {
                    qq.AyarDegeri = ayarDegeri;
                }
                else
                {
                    entities.TIAyarlars.Add(new TIAyarlar { AyarAdi = ayarAdi, AyarDegeri = ayarDegeri });

                }
                entities.SaveChanges();
            }

        }
        public static string GetAyarTi(this string ayarAdi, string enstituKodu, string varsayilanDeger = "")
        {
            using (var entities = new LubsDbEntities())
            {
                var qq = entities.TIAyarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi && p.EnstituKod == enstituKodu);
                return qq != null ? qq.AyarDegeri : varsayilanDeger;
            }
        }


    }


}