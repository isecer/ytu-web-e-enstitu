using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Models
{

    public static class BelgeTalepAyar
    {

        public const string BelgeTalebiAcikmi = "Belge Talebi İşlemi Açık";
        public const string YeniBelgeTalebindeMailGonder = "Yeni Belge Talebinde Mail Gönder";
        public const string BelgeAlımAdresi = "Belge Alım Adresi";
        public const string BelgeTalebiResmiTatilDurum = "Belge Talebinde Resmi Tatillere Göre İşlem Yap";
        public const string IlkBelgeTalebiAnketiAdi = "İlk Belge Talebinde İstenen Anket";
        public const string Donem4BelgeTalebiAnketiAdi = "4. Dönem İlk Belge Talebinde İstenen Anket";

        public static void setAyarBT(string AyarAdi, string AyarDegeri, string EnstituKod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qq = db.BelgeTalepAyarlars.Where(p => p.AyarAdi == AyarAdi && p.EnstituKod == EnstituKod).FirstOrDefault();
                if (qq != null)
                {
                    qq.AyarDegeri = AyarDegeri;
                }
                else
                {
                    db.BelgeTalepAyarlars.Add(new BelgeTalepAyarlar { AyarAdi = AyarAdi, AyarDegeri = AyarDegeri });

                }
                db.SaveChanges();
            }

        }
        public static string getAyarBT(this string AyarAdi, string EnstituKodu, string VarsayilanDeger = "")
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qq = db.BelgeTalepAyarlars.Where(p => p.AyarAdi == AyarAdi && p.EnstituKod == EnstituKodu).FirstOrDefault();
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