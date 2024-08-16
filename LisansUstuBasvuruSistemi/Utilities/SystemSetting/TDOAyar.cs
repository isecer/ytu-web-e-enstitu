using Entities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{

    public static class TdoAyar
    {

        public const string BasvurusuAcikmi = "Başvuru Alımı Açık";
        public const string TezBaslikKarakterKisitlamaAyar = "Tez Başlığı Karakter Kısıtlaması";
        public const string DanismanMaxOgrenciKayitKriter = "Danışman YL + DR maksimum kayıtlı öğrenci sayısı";
        public const string DanismanMinSinavPuanKabulKriter = "Danışman için Dil Sınavı kabulü min puan";

        public static void SetAyarTdo(string ayarAdi, string ayarDegeri, string enstituKod)
        {
            using (var entities = new LubsDbEntities())
            {
                var ayar = entities.TDOAyarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi && p.EnstituKod == enstituKod);
                if (ayar != null)
                {
                    ayar.AyarDegeri = ayarDegeri;
                }
                else
                {
                    entities.TDOAyarlars.Add(new TDOAyarlar { AyarAdi = ayarAdi, AyarDegeri = ayarDegeri });

                }
                entities.SaveChanges();
            }
        }
        public static string GetAyarTdo(this string ayarAdi, string enstituKodu, string varsayilanDeger = "")
        {
            using (var entities = new LubsDbEntities())
            {
                var ayar = entities.TDOAyarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi && p.EnstituKod == enstituKodu);
                return ayar != null ? ayar.AyarDegeri : varsayilanDeger;
            }
        }
    }


}