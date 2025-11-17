using Entities.Entities;
using System.Linq;

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{
    public static class MezuniyetAyar
    {
        public class MezuniyetAyarProperty
        {
            internal string PropertyValue { get; }

            internal MezuniyetAyarProperty(string value)
            {
                PropertyValue = value;
            }

            public static implicit operator string(MezuniyetAyarProperty property)
            {
                return property.PropertyValue;
            }
        }

        public static readonly MezuniyetAyarProperty MezuniyetBasvurusuAcikmi = new MezuniyetAyarProperty("Mezuniyet Başvurusu Açık");
        public static readonly MezuniyetAyarProperty MezuniyetBasvurusunuTezSorumlusunaAta = new MezuniyetAyarProperty("Mezuniyet Basvurusunu Tez Sorumlusuna Ata");
        public static readonly MezuniyetAyarProperty MezuniyetBasvurusunuIlgiliTezSorumlusunaAta = new MezuniyetAyarProperty("Mezuniyet Basvurusunu İlgili Tez Sorumlusuna Ata");
        public static readonly MezuniyetAyarProperty TezSorumluAtamaHesaplamasiDonemselYap = new MezuniyetAyarProperty("Tez Sorumlusu Atama Hesaplaması Dönemsel Yapılsın");
        public static readonly MezuniyetAyarProperty YeniMezuniyetBasvurusundaMailGonder = new MezuniyetAyarProperty("Yeni Mezuniyet Başvurusunda Mail Gönder");
        public static readonly MezuniyetAyarProperty TezSinaviDavetKartlariniAnaSayfadaGoster = new MezuniyetAyarProperty("Tez Savunma Davet Kartlarını Ana Sayfada Göster");
        public static readonly MezuniyetAyarProperty TezSinaviDavetkartiPdfHaliMezuniyetSinavdanAlinabilsin = new MezuniyetAyarProperty("Tez Savunma Davet Kartı PDF dosyası Mezuniyet Sınav Ekranından indirilebilsin");
        public static readonly MezuniyetAyarProperty TezSinaviDavetListesindeGosterilecekKisiSayisi = new MezuniyetAyarProperty("Tez Savunma Davet Listesinde Gösterilecek Kişi Sayısı");
        public static readonly MezuniyetAyarProperty TezKontrolOnayTaahhutMetniLatex = new MezuniyetAyarProperty("Tez Kontrol Sürecinde Tez Onayı Taahhüt Metni (Latex Şablonu İçin)");
        public static readonly MezuniyetAyarProperty TezKontrolOnayTaahhutMetniWord = new MezuniyetAyarProperty("Tez Kontrol Sürecinde Tez Onayı Taahhüt Metni (Word Şablonu İçin)");

        public static string GetAyar(this MezuniyetAyarProperty ayarProperty, string enstituKodu, string varsayilanDeger = "")
        {
           
            using (var entities = new LubsDbEntities())
            {
                var ayar = entities.MezuniyetAyarlars
                    .FirstOrDefault(p => p.AyarAdi == ayarProperty.PropertyValue && p.EnstituKod == enstituKodu);
                return ayar != null ? ayar.AyarDegeri : varsayilanDeger;
            }
        }
         
    }
}
