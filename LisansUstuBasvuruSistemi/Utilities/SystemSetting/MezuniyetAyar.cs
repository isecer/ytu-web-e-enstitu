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

        // ─────────────────────────────────────────────────────
        //  Mevcut ayarlar (değişmeyen)
        // ─────────────────────────────────────────────────────

        public static readonly MezuniyetAyarProperty MezuniyetBasvurusuAcikmi =
            new MezuniyetAyarProperty("Mezuniyet Başvurusu Açık");

        public static readonly MezuniyetAyarProperty MezuniyetBasvurusunuTezSorumlusunaAta =
            new MezuniyetAyarProperty("Mezuniyet Basvurusunu Tez Sorumlusuna Ata");

        public static readonly MezuniyetAyarProperty MezuniyetBasvurusunuIlgiliTezSorumlusunaAta =
            new MezuniyetAyarProperty("Mezuniyet Basvurusunu İlgili Tez Sorumlusuna Ata");

        public static readonly MezuniyetAyarProperty YeniMezuniyetBasvurusundaMailGonder =
            new MezuniyetAyarProperty("Yeni Mezuniyet Başvurusunda Mail Gönder");

        public static readonly MezuniyetAyarProperty TezSinaviDavetKartlariniAnaSayfadaGoster =
            new MezuniyetAyarProperty("Tez Savunma Davet Kartlarını Ana Sayfada Göster");

        public static readonly MezuniyetAyarProperty TezSinaviDavetkartiPdfHaliMezuniyetSinavdanAlinabilsin =
            new MezuniyetAyarProperty("Tez Savunma Davet Kartı PDF dosyası Mezuniyet Sınav Ekranından indirilebilsin");

        public static readonly MezuniyetAyarProperty TezSinaviDavetListesindeGosterilecekKisiSayisi =
            new MezuniyetAyarProperty("Tez Savunma Davet Listesinde Gösterilecek Kişi Sayısı");

        public static readonly MezuniyetAyarProperty TezKontrolOnayTaahhutMetniLatex =
            new MezuniyetAyarProperty("Tez Kontrol Sürecinde Tez Onayı Taahhüt Metni (Latex Şablonu İçin)");

        public static readonly MezuniyetAyarProperty TezKontrolOnayTaahhutMetniWord =
            new MezuniyetAyarProperty("Tez Kontrol Sürecinde Tez Onayı Taahhüt Metni (Word Şablonu İçin)");

        // ─────────────────────────────────────────────────────
        //  V2: Yeni ve değişen ayarlar
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Dropdown — eski 3 boolean'ı (Sırayla, Dönemsel, ZamanPencereli) tek seçime toplar.
        /// Geçerli değerler:
        ///   "Sırayla Atama (Round-Robin)"
        ///   "En Az Atanan — Genel"
        ///   "En Az Atanan — Dönemsel"
        ///   "En Az Atanan — Belirlenen Gün Sayısına Göre"
        ///   "En Az Atanan — Dönemsel + Belirlenen Gün Sayısına Göre"
        /// </summary>
        public static readonly MezuniyetAyarProperty TezAtamaYontemi =
            new MezuniyetAyarProperty("Tez Sorumlusu Atama Yöntemi");

        /// <summary>
        /// Zaman penceresi gün sayısı.
        /// Sadece "Belirlenen Gün Sayısına Göre" yöntem seçildiğinde kullanılır. 0 = tüm zaman.
        /// </summary>
        public static readonly MezuniyetAyarProperty TezAtamaGunSiniri =
            new MezuniyetAyarProperty("Tez Sorumlusu Atama Gün Sınırı");

        /// <summary>
        /// Bir kişiye bir günde yapılabilecek maksimum atama sayısı.
        /// 0 = sınır yok. Hem Round-Robin hem Skorlama modunda geçerli.
        /// </summary>
        public static readonly MezuniyetAyarProperty TezAtamaGunlukTavan =
            new MezuniyetAyarProperty("Tez Sorumlusu Günlük Atama Tavanı");

        /// <summary>
        /// Açıksa, henüz kontrol edilmemiş dosya bekleyen sorumlulara
        /// daha az yeni atama yapılır (skor ağırlığı: bekleyenSayısı × 0.3).
        /// </summary>
        public static readonly MezuniyetAyarProperty TezAtamadaKontrolBekleyenleriIsYukuneDahilEt =
            new MezuniyetAyarProperty("Tez Atamada Kontrol Bekleyenleri İş Yüküne Dahil Et");

        public static readonly MezuniyetAyarProperty TezAtamadaDuzeltmedeBekleyenleriIsYukuneDahilEt =
            new MezuniyetAyarProperty("Tez Atamada Düzeltmede Bekleyenleri İş Yüküne Dahil Et");

        /// <summary>
        /// Açıksa, istatistik ekranında "Atanmamış Tezleri Dağıt" butonu gösterilir.
        /// Yetkili kullanıcı bu butonla bekleyen başvuruları mevcut algoritmaya göre toplu atar.
        /// </summary>
        public static readonly MezuniyetAyarProperty TezKontrolTopluAtamaAktif =
            new MezuniyetAyarProperty("Tez Kontrol Toplu Atama Aktif");

        // ─────────────────────────────────────────────────────
        //  KALDIRILDI (V2'de dropdown'a taşındı):
        //    TezAtamaSiraylaYap
        //    TezSorumluAtamaHesaplamasiDonemselYap
        // ─────────────────────────────────────────────────────


        /// <summary>
        /// Enstitü bazında ayar değerini okur.
        /// </summary>
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