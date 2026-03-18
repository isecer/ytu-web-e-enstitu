using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class TezKontrolYetkilisiAtama
    {

        // ══════════════════════════════════════════════════════════════
        //  SABİT KATSAYILAR
        //  İş kararı değil teknik parametre — admin'e açılmaz.
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Bekleyen iş yükü skora bu katsayıyla eklenir.
        /// 0.3 = 3 bekleyen iş ≈ 1 günlük ek atama yüküne denk gelir.
        /// </summary>
        public const double BekleyenIsKatsayisi = 0.3;

        // ─────────────────────────────────────────────────────────────
        //  Tez Kontrol Sorumlusu Atama Servisi (V2)
        //
        //  Değişiklik Özeti (V0/V1 → V2):
        //  ─────────────────────────────────────────────────────────
        //  • Atama Yöntemi dropdown ile tek ayardan strateji belirlenir
        //  • İzin normalizasyonu: Belirlenen Gün Sayısına Göre modda aktif gün hesabı
        //  • Günlük atama tavanı: Bir kişiye günde max N atama
        //  • Bekleyen iş yükü: Kontrol edilmemiş dosya sayısı skora eklenir
        //  • Eski-yeni yetkili fark yığılması pencere ile çözülür
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Yetkili havuzundaki her bir kişinin skor hesabına esas bilgilerini taşır.
        /// </summary>
        private class TezAtamaYetkiliDto
        {
            public int KullaniciId { get; set; }
            public string KullaniciAdi { get; set; }

            /// <summary>
            /// Program önceliklendirme sırası:
            /// 0 = Bu programda yetkili, 1 = Genel yetkili, -1 = Havuzdan çıkacak
            /// </summary>
            public int ProgramOncelik { get; set; }

            /// <summary>
            /// Zaman penceresi içindeki atama sayısı
            /// </summary>
            public int PencereAtamaSayisi { get; set; }

            /// <summary>
            /// Pencere içindeki aktif (izinde olmayan) gün sayısı.
            /// Normalizasyon için kullanılır.
            /// </summary>
            public int AktifGunSayisi { get; set; }

            /// <summary>
            /// Henüz kontrol edilmemiş dosya bekleyen başvuru sayısı
            /// </summary>
            public int BekleyenIsYuku { get; set; }

            /// <summary>
            /// Bugün yapılan atama sayısı (günlük tavan kontrolü için)
            /// </summary>
            public int BugunkuAtamaSayisi { get; set; }

            /// <summary>
            /// Nihai skor — düşük olan önceliklidir.
            /// normalizeOran + (bekleyenİş × katsayı)
            /// </summary>
            public double FinalSkor { get; set; }
        }

        /// <summary>
        /// Atama yöntemi ayar değerlerini parse eder.
        /// Dropdown'daki 5 seçenek buradan yorumlanır.
        /// </summary>
        private class AtamaYontemiAyar
        {
            public bool IsSiraylaAtama { get; set; }
            public bool IsDonemsel { get; set; }
            public bool IsZamanPencereli { get; set; }

            public static AtamaYontemiAyar Parse(string ayarDegeri)
            {
                // Dropdown seçenekleri:
                // "Sırayla Atama (Round-Robin)"
                // "En Az Atanan — Genel"
                // "En Az Atanan — Dönemsel"
                // "En Az Atanan — Belirlenen Gün Sayısına Göre"
                // "En Az Atanan — Dönemsel + Belirlenen Gün Sayısına Göre"
                return new AtamaYontemiAyar
                {
                    IsSiraylaAtama = (ayarDegeri ?? "").StartsWith("Sırayla"),
                    IsDonemsel = (ayarDegeri ?? "").Contains("Dönemsel"),
                    IsZamanPencereli = (ayarDegeri ?? "").Contains("Belirlenen Gün Sayısına Göre")
                };
            }
        }



        // ═══════════════════════════════════════════════════════════════
        //  ANA METOD
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Tez dosyası yüklendiğinde uygun tez kontrol yetkilisini belirleyip başvuruya atar.
        ///
        /// Kontrol Katmanları:
        ///   1. Sistem seviyesi: Atama aktif mi, mevcut yetkili geçerli mi
        ///   2. Yetkili havuzu: Aktif, izinde olmayan, atamaya açık yetkililer
        ///   3. Program önceliklendirme: İlgili programdaki yetkililer öncelikli
        ///   4. Strateji: Round-Robin veya Skorlama (normalize oran + iş yükü + tavan)
        /// </summary>
        public static void TezDosyasiKontrolYetkilisiAta(int mezuniyetBasvurulariId)
        {
            using (var entities = new LubsDbEntities())
            {
                var basvuru = entities.MezuniyetBasvurularis
                    .First(f => f.MezuniyetBasvurulariID == mezuniyetBasvurulariId);

                var enstituKod = basvuru.MezuniyetSureci.EnstituKod;
                var nowDate = DateTime.Now;

                // ─────────────────────────────────────────────────
                // KATMAN 1: Sistem seviyesi kontroller
                // ─────────────────────────────────────────────────

                // Tez atama sistemi aktif mi?
                if (!MezuniyetAyar.MezuniyetBasvurusunuTezSorumlusunaAta
                        .GetAyar(enstituKod).ToBoolean(false))
                    return;

                // Başvuruda zaten geçerli bir yetkili atanmış mı?
                if (basvuru.TezKontrolKullaniciID.HasValue &&
                    entities.Kullanicilars.Any(a =>
                        a.KullaniciID == basvuru.TezKontrolKullaniciID &&
                        a.YetkiGrupID == YetkiGrupBus.TezKontrolYetkiGrupId &&
                        a.IsAktif &&
                        a.IsTezAtamaAcik == true &&
                        (!a.IzinBaslamaTarihi.HasValue ||
                         a.IzinBaslamaTarihi > nowDate ||
                         a.IzinBitisTarihi < nowDate)))
                    return;

                // ─────────────────────────────────────────────────
                // Ayarları oku
                // ─────────────────────────────────────────────────

                var yontem = AtamaYontemiAyar.Parse(
                    MezuniyetAyar.TezAtamaYontemi.GetAyar(enstituKod));

                var isProgramOnceliklendirme = MezuniyetAyar.MezuniyetBasvurusunuIlgiliTezSorumlusunaAta
                    .GetAyar(enstituKod).ToBoolean(false);

                var gunSiniri = yontem.IsZamanPencereli
                    ? MezuniyetAyar.TezAtamaGunSiniri.GetAyar(enstituKod).ToInt(7)
                    : 0;

                var gunlukTavan = MezuniyetAyar.TezAtamaGunlukTavan
                    .GetAyar(enstituKod).ToInt(0);

                var isKontrolBekleyenDahil = MezuniyetAyar.TezAtamadaKontrolBekleyenleriIsYukuneDahilEt
                    .GetAyar(enstituKod).ToBoolean(false);


                var isDuzeltmeBekleyenDahil = MezuniyetAyar.TezAtamadaDuzeltmedeBekleyenleriIsYukuneDahilEt
                    .GetAyar(enstituKod).ToBoolean(false);

                var isBekleyenIsYukuAktif = isKontrolBekleyenDahil || isDuzeltmeBekleyenDahil;

                // ─────────────────────────────────────────────────
                // KATMAN 2: Yetkili havuzu oluştur
                // ─────────────────────────────────────────────────

                var aktifYetkililer = entities.Kullanicilars.Where(kul =>
                    kul.YetkiGrupID == YetkiGrupBus.TezKontrolYetkiGrupId &&
                    kul.IsAktif &&
                    kul.IsTezAtamaAcik == true &&
                    kul.EnstituKod == enstituKod &&
                    !(
                        kul.IzinBaslamaTarihi.HasValue &&
                        kul.IzinBaslamaTarihi <= nowDate &&
                        (!kul.IzinBitisTarihi.HasValue || kul.IzinBitisTarihi >= nowDate)
                    ));

                // ─────────────────────────────────────────────────
                // KATMAN 3: Program önceliklendirme
                // ─────────────────────────────────────────────────

                var yetkiliHavuzu = aktifYetkililer.Select(kul => new TezAtamaYetkiliDto
                {
                    KullaniciId = kul.KullaniciID,
                    KullaniciAdi = kul.KullaniciAdi,
                    ProgramOncelik = isProgramOnceliklendirme
                            ? (kul.KullaniciProgramlaris.Any(a => a.ProgramKod == basvuru.ProgramKod)
                                ? 0
                                : (!kul.KullaniciProgramlaris.Any()
                                    ? 1
                                    : -1))
                            : 1
                })
                    .Where(p => p.ProgramOncelik >= 0)
                    .ToList();

                if (!yetkiliHavuzu.Any()) return;

                // ─────────────────────────────────────────────────
                // KATMAN 4: Strateji ile yetkili seç
                // ─────────────────────────────────────────────────

                int seciliKullaniciId;

                if (yontem.IsSiraylaAtama)
                {
                    seciliKullaniciId = SiraylaAtamaYap(
                        entities, enstituKod, yetkiliHavuzu, gunlukTavan, nowDate);
                }
                else
                {
                    seciliKullaniciId = SkorlamaIleAtamaYap(
                        entities, enstituKod, basvuru, yetkiliHavuzu,
                        yontem.IsDonemsel, gunSiniri, gunlukTavan,
                        isKontrolBekleyenDahil, isDuzeltmeBekleyenDahil, nowDate);
                }

                // ─────────────────────────────────────────────────
                // Atamayı kaydet
                // ─────────────────────────────────────────────────

                basvuru.TezKontrolKullaniciID = seciliKullaniciId;
                basvuru.TezKontrolAtamaTarihi = DateTime.Now;
                entities.SaveChanges();
            }
        }


        // ═══════════════════════════════════════════════════════════════
        //  ROUND-ROBİN STRATEJİSİ
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Sırayla atama: Son atanan kişiden sonraki sıradakini seçer.
        /// Günlük tavan aktifse, tavana ulaşan kişi atlanır.
        /// </summary>
        private static int SiraylaAtamaYap(
            LubsDbEntities entities,
            string enstituKod,
            List<TezAtamaYetkiliDto> yetkiliHavuzu,
            int gunlukTavan,
            DateTime nowDate)
        {
            var siraliListe = yetkiliHavuzu
                .OrderBy(o => o.ProgramOncelik)
                .ThenBy(o => o.KullaniciId)
                .ToList();

            // Günlük tavan kontrolü: Bugün tavana ulaşanları hesapla
            var bugunBaslangic = nowDate.Date;
            Dictionary<int, int> bugunkuAtamalar;

            if (gunlukTavan > 0)
            {
                bugunkuAtamalar = entities.MezuniyetBasvurularis
                    .Where(m =>
                        m.TezKontrolKullaniciID.HasValue &&
                        m.MezuniyetSureci.EnstituKod == enstituKod &&
                        m.TezKontrolAtamaTarihi >= bugunBaslangic)
                    .GroupBy(m => m.TezKontrolKullaniciID.Value)
                    .Select(g => new { Id = g.Key, Sayi = g.Count() })
                    .ToDictionary(x => x.Id, x => x.Sayi);

                // Tavana ulaşanları listeden çıkar
                var tavanAsmayanlar = siraliListe
                    .Where(y => !bugunkuAtamalar.ContainsKey(y.KullaniciId) ||
                                bugunkuAtamalar[y.KullaniciId] < gunlukTavan)
                    .ToList();

                // Herkes tavanda ise tavanı görmezden gel (atama durmamalı)
                if (tavanAsmayanlar.Any())
                    siraliListe = tavanAsmayanlar;
            }

            // Son atanan kişiyi bul
            var sonAtananId = entities.MezuniyetBasvurularis
                .Where(m =>
                    m.TezKontrolKullaniciID.HasValue &&
                    m.MezuniyetSureci.EnstituKod == enstituKod)
                .OrderByDescending(m => m.MezuniyetBasvurulariID)
                .Select(m => m.TezKontrolKullaniciID)
                .FirstOrDefault();

            if (!sonAtananId.HasValue)
                return siraliListe.First().KullaniciId;

            var sonAtananIndex = siraliListe.FindIndex(x => x.KullaniciId == sonAtananId.Value);

            if (sonAtananIndex < 0 || sonAtananIndex >= siraliListe.Count - 1)
                return siraliListe.First().KullaniciId;

            return siraliListe[sonAtananIndex + 1].KullaniciId;
        }


        // ═══════════════════════════════════════════════════════════════
        //  SKORLAMA İLE ATAMA STRATEJİSİ (En Az Atanan V2)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Her yetkili için normalize skor hesaplar ve en düşük skorlu kişiyi seçer.
        ///
        /// Skor = (atamaSayısı / aktifGünSayısı) + (bekleyenİşYükü × 0.3)
        ///
        /// Kombinasyonlar (dropdown'dan gelir):
        ///   Genel                      → tüm süreçler, tüm zaman, normalizasyon yok
        ///   Dönemsel                   → aktif süreç, tüm zaman, normalizasyon yok
        ///   Belirlenen Gün Sayısına Göre            → tüm süreçler, son X gün, normalizasyon aktif
        ///   Dönemsel + Belirlenen Gün Sayısına Göre → aktif süreç, son X gün, normalizasyon aktif
        /// </summary>
        private static int SkorlamaIleAtamaYap(
            LubsDbEntities entities,
            string enstituKod,
            MezuniyetBasvurulari basvuru,
            List<TezAtamaYetkiliDto> yetkiliHavuzu,
            bool isDonemsel,
            int gunSiniri,
            int gunlukTavan,
            bool isKontrolBekleyenDahil,
            bool isDuzeltmeBekleyenDahil,
            DateTime nowDate)
        {
            var bugunBaslangic = nowDate.Date;

            // ── 1. Atama sayılarını hesapla (pencere + dönemsel filtreli) ──

            var atamaQuery = entities.MezuniyetBasvurularis
                .Where(m =>
                    m.TezKontrolKullaniciID.HasValue &&
                    m.MezuniyetSureci.EnstituKod == enstituKod);

            if (isDonemsel)
            {
                atamaQuery = atamaQuery.Where(m =>
                    m.MezuniyetSurecID == basvuru.MezuniyetSurecID);
            }

            DateTime? pencereBaslangic = null;
            if (gunSiniri > 0)
            {
                pencereBaslangic = nowDate.AddDays(-gunSiniri);
                atamaQuery = atamaQuery.Where(m =>
                    m.TezKontrolAtamaTarihi >= pencereBaslangic);
            }

            var atamaSayilari = atamaQuery
                .GroupBy(m => m.TezKontrolKullaniciID.Value)
                .Select(g => new { Id = g.Key, Sayi = g.Count() })
                .ToDictionary(x => x.Id, x => x.Sayi);

            // ── 2. Bugünkü atama sayıları (günlük tavan için) ──

            Dictionary<int, int> bugunkuAtamalar = null;
            if (gunlukTavan > 0)
            {
                bugunkuAtamalar = entities.MezuniyetBasvurularis
                    .Where(m =>
                        m.TezKontrolKullaniciID.HasValue &&
                        m.MezuniyetSureci.EnstituKod == enstituKod &&
                        m.TezKontrolAtamaTarihi >= bugunBaslangic)
                    .GroupBy(m => m.TezKontrolKullaniciID.Value)
                    .Select(g => new { Id = g.Key, Sayi = g.Count() })
                    .ToDictionary(x => x.Id, x => x.Sayi);
            }

            // ── 3. Bekleyen iş yükü (kontrol edilmemiş dosya bekleyen başvuru sayısı) ──
            var isBekleyenIsYukuAktif = isKontrolBekleyenDahil || isDuzeltmeBekleyenDahil;
            Dictionary<int, int> bekleyenIsler = null;
            if (isBekleyenIsYukuAktif)
            {
                var bekleyenDegerler = new List<bool?>();
                if (isKontrolBekleyenDahil) bekleyenDegerler.Add(null);      // Yeni yüklenen, kontrol edilmemiş
                if (isDuzeltmeBekleyenDahil) bekleyenDegerler.Add(false);     // Düzeltme verilmiş, öğrenci yeni dosya bekleniyor

                bekleyenIsler = entities.MezuniyetBasvurularis
                    .Where(m =>
                        m.TezKontrolKullaniciID.HasValue &&
                        m.MezuniyetSureci.EnstituKod == enstituKod &&
                        m.MezuniyetBasvurulariTezDosyalaris
                            .OrderByDescending(d => d.SiraNo)
                            .FirstOrDefault() != null &&
                        bekleyenDegerler.Contains(m.MezuniyetBasvurulariTezDosyalaris
                            .OrderByDescending(d => d.SiraNo)
                            .FirstOrDefault().IsOnaylandiOrDuzeltme))
                    .GroupBy(m => m.TezKontrolKullaniciID.Value)
                    .Select(g => new { Id = g.Key, Sayi = g.Count() })
                    .ToDictionary(x => x.Id, x => x.Sayi);
            }

            // ── 4. İzin normalizasyonu: Pencere içindeki aktif gün sayısı ──
            //    Sadece Belirlenen Gün Sayısına Göre modlarda uygulanır.
            //    İzin bilgisi anlık (tek kayıt), pencere ile kesişimi hesaplanır.

            var yetkiliIdler = yetkiliHavuzu.Select(y => y.KullaniciId).ToList();
            Dictionary<int, int> izinliGunler = null;

            if (gunSiniri > 0)
            {
                // Her yetkili için izin-pencere kesişimini hesapla
                izinliGunler = entities.Kullanicilars
                    .Where(k => yetkiliIdler.Contains(k.KullaniciID) &&
                                k.IzinBaslamaTarihi.HasValue &&
                                k.IzinBitisTarihi.HasValue)
                    .Select(k => new
                    {
                        k.KullaniciID,
                        k.IzinBaslamaTarihi,
                        k.IzinBitisTarihi
                    })
                    .ToList()
                    .Select(k =>
                    {
                        // Pencere ile izin aralığının kesişimi
                        var kesisimBaslangic = k.IzinBaslamaTarihi.Value > pencereBaslangic.Value
                            ? k.IzinBaslamaTarihi.Value
                            : pencereBaslangic.Value;

                        var kesisimBitis = k.IzinBitisTarihi.Value < nowDate
                            ? k.IzinBitisTarihi.Value
                            : nowDate;

                        var gun = (int)Math.Max(0, (kesisimBitis - kesisimBaslangic).TotalDays + 1);
                        return new { k.KullaniciID, IzinliGun = gun };
                    })
                    .Where(x => x.IzinliGun > 0)
                    .ToDictionary(x => x.KullaniciID, x => x.IzinliGun);
            }

            // ── 5. Skorları hesapla ──

            foreach (var yetkili in yetkiliHavuzu)
            {
                var pencereAtama = atamaSayilari.ContainsKey(yetkili.KullaniciId)
                    ? atamaSayilari[yetkili.KullaniciId]
                    : 0;

                yetkili.PencereAtamaSayisi = pencereAtama;

                // Aktif gün hesabı
                if (gunSiniri > 0)
                {
                    var izinGun = (izinliGunler != null && izinliGunler.ContainsKey(yetkili.KullaniciId))
                        ? izinliGunler[yetkili.KullaniciId]
                        : 0;

                    // Aktif gün = pencere - izinli günler (0 olabilir)
                    yetkili.AktifGunSayisi = gunSiniri - izinGun;
                }
                else
                {
                    // Tüm zaman modunda normalizasyon yok, 1'e bölerek mutlak sayı kullan
                    yetkili.AktifGunSayisi = 1;
                }

                // Normalize oran: Aktif günü 0 olan kişi için skor hesaplanmaz,
                // aşağıda havuzdan çıkarılacak.
                double normalizeOran = yetkili.AktifGunSayisi > 0
                    ? (double)pencereAtama / yetkili.AktifGunSayisi
                    : double.MaxValue;

                // Bekleyen iş yükü
                double bekleyenSkor = 0;
                if (isBekleyenIsYukuAktif && bekleyenIsler != null)
                {
                    yetkili.BekleyenIsYuku = bekleyenIsler.TryGetValue(yetkili.KullaniciId, out var bekleyenIsYuku)
                        ? bekleyenIsYuku
                        : 0;

                    bekleyenSkor = yetkili.BekleyenIsYuku * BekleyenIsKatsayisi;
                }

                // Bugünkü atama
                if (bugunkuAtamalar != null && bugunkuAtamalar.TryGetValue(yetkili.KullaniciId, out var bugunkuAtamaSayisi))
                {
                    yetkili.BugunkuAtamaSayisi = bugunkuAtamaSayisi;
                }

                yetkili.FinalSkor = normalizeOran + bekleyenSkor;
            }

            // ── 6. Pencerede aktif günü olmayan kişileri çıkar ──
            //    (İzinden yeni dönmüş ama penceredeki tüm günler izinli)

            var adaylar = yetkiliHavuzu;
            if (gunSiniri > 0)
            {
                var aktifGunuOlanlar = adaylar
                    .Where(y => y.AktifGunSayisi > 0)
                    .ToList();

                // Herkesin aktif günü 0 → normalizasyonu devre dışı bırak, mutlak sayıya düş
                if (aktifGunuOlanlar.Any())
                {
                    adaylar = aktifGunuOlanlar;
                }
                else
                {
                    // Fallback: Herkesi dahil et, skoru atama sayısı olarak kullan
                    foreach (var y in adaylar)
                    {
                        y.FinalSkor = y.PencereAtamaSayisi + (y.BekleyenIsYuku * BekleyenIsKatsayisi);
                    }
                }
            }

            // ── 7. Günlük tavan filtresi ──

            if (gunlukTavan > 0)
            {
                var tavanAsmayanlar = adaylar
                    .Where(y => y.BugunkuAtamaSayisi < gunlukTavan)
                    .ToList();

                // Herkes tavanda → tavanı görmezden gel, atama durmamalı
                if (tavanAsmayanlar.Any())
                    adaylar = tavanAsmayanlar;
            }

            // ── 8. Seçim: ProgramÖncelik → FinalSkor → Random ──

            var secilen = adaylar
                .OrderBy(o => o.ProgramOncelik)
                .ThenBy(o => o.FinalSkor)
                .ThenBy(_ => Guid.NewGuid())
                .First();

            return secilen.KullaniciId;
        }
    }
}