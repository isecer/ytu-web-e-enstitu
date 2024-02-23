using Entities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{

    public static class BelgeTalepAyar
    {

        public const string BelgeTalebiAcikmi = "Belge Talebi İşlemi Açık";
        public const string YeniBelgeTalebindeMailGonder = "Yeni Belge Talebinde Mail Gönder";
        public const string BelgeAlımAdresi = "Belge Alım Adresi";
        public const string BelgeTalebiResmiTatilDurum = "Belge Talebinde Resmi Tatillere Göre İşlem Yap";
        public const string IlkBelgeTalebiAnketiAdi = "İlk Belge Talebinde İstenen Anket";
        public const string Donem4BelgeTalebiAnketiAdi = "4. Dönem İlk Belge Talebinde İstenen Anket";

        public static void SetAyarBt(string ayarAdi, string ayarDegeri, string enstituKod)
        {
            using (var entities = new LubsDbEntities())
            {
                var ayar = entities.BelgeTalepAyarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi && p.EnstituKod == enstituKod);
                if (ayar != null)
                {
                    ayar.AyarDegeri = ayarDegeri;
                }
                else
                {
                    entities.BelgeTalepAyarlars.Add(new BelgeTalepAyarlar { AyarAdi = ayarAdi, AyarDegeri = ayarDegeri });

                }
                entities.SaveChanges();
            }

        }
        public static string GetAyarBt(this string ayarAdi, string enstituKodu, string varsayilanDeger = "")
        {
            using (var entities = new LubsDbEntities())
            {
                var ayar = entities.BelgeTalepAyarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi && p.EnstituKod == enstituKodu);
                return ayar != null ? ayar.AyarDegeri : varsayilanDeger;
            }
        }
    }


}