using Entities.Entities;
using System.Linq; 

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{
    public static class TdoAyar
    {
        public class TdoAyarProperty
        {
            internal string PropertyValue { get; }
            internal TdoAyarProperty(string value)
            {
                PropertyValue = value;
            }
            public static implicit operator string(TdoAyarProperty prop) => prop.PropertyValue;
        }

        public static readonly TdoAyarProperty BasvurusuAcikmi = new TdoAyarProperty("Başvuru Alımı Açık");
        public static readonly TdoAyarProperty DanismanMaxOgrenciKayitKriter = new TdoAyarProperty("Danışman YL + DR maksimum kayıtlı öğrenci sayısı");
        public static readonly TdoAyarProperty DanismanMinSinavPuanKabulKriter = new TdoAyarProperty("Danışman için Dil Sınavı kabulü min puan");

        public static readonly TdoAyarProperty IlkDanismanOnerisindeIstenenAnket = new TdoAyarProperty("İlk Danışman Önerisinde İstenen Anket");
        public static string GetAyar(this TdoAyarProperty ayarProperty, string enstituKodu, string varsayilanDeger = "")
        {
            using (var entities = new LubsDbEntities())
            {
                var ayar = entities.TDOAyarlars.FirstOrDefault(p => p.AyarAdi == ayarProperty.PropertyValue && p.EnstituKod == enstituKodu);
                return ayar != null ? ayar.AyarDegeri : varsayilanDeger;
            }
        }
    }
}
