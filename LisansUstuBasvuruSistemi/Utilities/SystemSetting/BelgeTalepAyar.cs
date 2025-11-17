using Entities.Entities;
using System.Linq; 

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{

    public static class BelgeTalepAyar
    {
        public class BelgeTalepAyarProperty
        {
            internal string PropertyValue { get; }

            internal BelgeTalepAyarProperty(string value)
            {
                PropertyValue = value;
            }

            public static implicit operator string(BelgeTalepAyarProperty property)
            {
                return property.PropertyValue;
            }
        }
        public static readonly BelgeTalepAyarProperty BelgeTalebiAcikmi = new BelgeTalepAyarProperty("Belge Talebi İşlemi Açık");
        public static readonly BelgeTalepAyarProperty YeniBelgeTalebindeMailGonder = new BelgeTalepAyarProperty("Yeni Belge Talebinde Mail Gönder");
        public static readonly BelgeTalepAyarProperty BelgeAlımAdresi = new BelgeTalepAyarProperty("Belge Alım Adresi");
        public static readonly BelgeTalepAyarProperty BelgeTalebiResmiTatilDurum = new BelgeTalepAyarProperty("Belge Talebinde Resmi Tatillere Göre İşlem Yap");
        public static readonly BelgeTalepAyarProperty IlkBelgeTalebiAnketi = new BelgeTalepAyarProperty("İlk Belge Talebinde İstenen Anket");
        public static readonly BelgeTalepAyarProperty Donem4BelgeTalebiAnketi = new BelgeTalepAyarProperty("4. Dönem İlk Belge Talebinde İstenen Anket");
  
        public static string GetAyar(this BelgeTalepAyarProperty ayarProperty, string enstituKodu, string varsayilanDeger = "")
        {
            using (var entities = new LubsDbEntities())
            {
                var ayar = entities.BelgeTalepAyarlars.FirstOrDefault(p => p.AyarAdi == ayarProperty.PropertyValue && p.EnstituKod == enstituKodu);
                return ayar != null ? ayar.AyarDegeri : varsayilanDeger;
            }
        } 
    }


}