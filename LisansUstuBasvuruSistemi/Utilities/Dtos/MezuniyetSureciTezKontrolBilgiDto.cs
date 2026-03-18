using System;
using System.Collections.Generic;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    /// <summary>
    /// Dashboard ana model — tez kontrol ekranına gönderilir.
    /// </summary>
    public class MezuniyetSureciTezKontrolDto
    {
        public int MezuniyetSurecId { get; set; }
        public string DonemAdi { get; set; }

        public List<MezuniyetSureciTezKontrolBilgiDto> AktifMezuniyetSureciTezKontrolBilgiDtos { get; set; }
        public List<MezuniyetSureciTezKontrolBilgiDto> PasifMezuniyetSureciTezKontrolBilgiDtos { get; set; }

        /// <summary>
        /// Dropdown'dan okunan atama yöntemi ham değeri.
        /// Örn: "En Az Atanan — Dönemsel + Belirlenen Gün Sayısına Göre"
        /// </summary>
        public string AtamaYontemi { get; set; }

        /// <summary>
        /// Strateji + kapsam + ek mekanizmaların okunabilir özeti.
        /// Örn: "En Az Atanan — Aktif Süreç, Son 7 Gün | İzin Normalizasyonu | Günlük Tavan: 5 | Bekleyen İş: Aktif"
        /// </summary>
        public string AktifStratejiAciklama { get; set; }

        public int GunSiniri { get; set; }
        public int GunlukTavan { get; set; }
        public bool IsDonemsel { get; set; }
        public bool IsSiraylaAtama { get; set; }
        public bool IsProgramOnceliklendirme { get; set; }
        public bool IsBekleyenIsYukuAktif { get; set; }
        public string BekleyenAciklama { get; set; }
        public bool IsZamanPencereli { get; set; }

        // ── V2: Atanmamış tez istatistiği ──

        /// <summary>
        /// Bu süreçte tez kontrol sorumlusu atanmamış başvuru sayısı.
        /// Tez dosyası yüklenmiş ama TezKontrolKullaniciID == null olanlar.
        /// </summary>
        public int AtanmamisTezSayisi { get; set; }

        /// <summary>
        /// Toplu atama butonu gösterilsin mi?
        /// Ayardan okunur: "Tez Kontrol Toplu Atama Aktif"
        /// </summary>
        public bool IsTopluAtamaAktif { get; set; }

        /// <summary>
        /// Kullanıcının bu ekrandaki işlemleri yapma yetkisi var mı?
        /// RoleNames.MezuniyetSureciKayıt yetkisi kontrol edilir.
        /// </summary>
        public bool IsYetkili { get; set; }

    }

    /// <summary>
    /// Dashboard tablo satırı — her bir tez kontrol yetkilisinin istatistikleri.
    /// </summary>
    public class MezuniyetSureciTezKontrolBilgiDto
    {
        public int KullaniciId { get; set; }
        public bool IsTezAtamaAcik { get; set; }
        public string ResimAdi { get; set; }
        public Guid UserKey { get; set; }
        public string AdSoyad { get; set; }

        // ── Süreç bazlı sayılar ──

        /// <summary>Aktif süreçte bu kişiye atanan toplam başvuru sayısı</summary>
        public int SurecToplamAtanan { get; set; }

        /// <summary>Aktif süreçte bu kişinin kendi onayladığı başvuru sayısı</summary>
        public int SurecToplamKendiOnayi { get; set; }

        /// <summary>Aktif süreçte (herhangi biri tarafından) onaylanan toplam</summary>
        public int SurecToplamOnay { get; set; }

        // ── Genel (tüm süreçler) sayılar ──

        public int GenelToplamAtanan { get; set; }
        public int GenelToplamKendiOnayi { get; set; }
        public int GenelToplamOnay { get; set; }

        // ── V2: Yeni alanlar ──

        /// <summary>
        /// Zaman penceresi içindeki atama sayısı (gün sınırı varsa).
        /// Kriter kolonu olarak işaretlenebilir.
        /// </summary>
        public int GunPenceresiAtanan { get; set; }

        /// <summary>
        /// Pencere içindeki aktif gün sayısı (izin günleri çıkarılmış).
        /// Sadece Belirlenen Gün Sayısına Göre modda anlamlı.
        /// </summary>
        public int AktifGunSayisi { get; set; }

        /// <summary>
        /// Normalize atama oranı = GunPenceresiAtanan / AktifGunSayisi.
        /// Belirlenen Gün Sayısına Göre modda gösterilir.
        /// </summary>
        public double NormalizeOran { get; set; }

        /// <summary>
        /// Kişinin üzerinde kontrol edilmemiş dosya bekleyen başvuru sayısı.
        /// </summary>
        public int BekleyenIsYuku { get; set; }

        /// <summary>
        /// Bugün yapılan atama sayısı (günlük tavan kontrolü için).
        /// </summary>
        public int BugunkuAtamaSayisi { get; set; }

        /// <summary>
        /// Final skor = normalizeOran + (bekleyenİşYükü × 0.3).
        /// En düşük skor = bir sonraki atamayı alacak kişi.
        /// Sadece skorlama modunda (Round-Robin değil) gösterilir.
        /// </summary>
        public double FinalSkor { get; set; }

        // ── Durum bayrakları ──

        public bool IsIzinde { get; set; }
        public DateTime? IzinBaslamaTarihi { get; set; }
        public DateTime? IzinBitisTarihi { get; set; }

        /// <summary>Round-Robin modunda sıradaki kişi</summary>
        public bool IsSiradaki { get; set; }

        /// <summary>Günlük tavana ulaşmış mı</summary>
        public bool IsTavanda { get; set; }
    }
}