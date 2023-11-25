using LisansUstuBasvuruSistemi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{

    public static class MezuniyetAyar
    {

        public const string MezuniyetBasvurusuAcikmi = "Mezuniyet Başvurusu Açık";
        public const string TezDosyasiYuklendigindeSorumluyaAta = "Tez Dosyası Yüklendiğinde Tez Sorumlusuna Ata";
        public const string YeniMezuniyetBasvurusundaMailGonder = "Yeni Mezuniyet Başvurusunda Mail Gönder"; 

        public static void SetAyarMz(string ayarAdi, string ayarDegeri, string enstituKod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var ayar = db.MezuniyetAyarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi && p.EnstituKod == enstituKod);
                if (ayar != null)
                {
                    ayar.AyarDegeri = ayarDegeri;
                }
                else
                {
                    db.MezuniyetAyarlars.Add(new MezuniyetAyarlar { AyarAdi = ayarAdi, AyarDegeri = ayarDegeri });

                }
                db.SaveChanges();
            }

        }
        public static string GetAyarMz(this string ayarAdi, string enstituKodu, string varsayilanDeger = "")
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var ayar = db.MezuniyetAyarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi && p.EnstituKod == enstituKodu);
                return ayar != null ? ayar.AyarDegeri : varsayilanDeger;
            }
        }
    }


}