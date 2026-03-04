using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading.Tasks;
using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.WebServiceData.ObsRestData;
using LisansUstuBasvuruSistemi.WebServiceData.ObsRestData.Models;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Business
{
    /// <summary>
    /// OBS program kod eşleştirme/güncelleme iş kuralları (tek sınıf)
    /// </summary>
    public class ProgramKodGuncellemeBus
    {
        #region Enums

        /// <summary>Eşleştirme modu enum</summary>
        public enum EslestirmeModuEnum
        {
            ProgramKod_ObsProgramKod = 1,   // Sistem: Program.ProgramKod  <-> OBS: ProgramKod (baz kod)
            ProgramKod_ProgramId = 2,   // Sistem: Program.ProgramKod  <-> OBS: ProgramId
            ObsProgramKod_ObsProgramKod = 3,// Sistem: Program.ObsProgramKod <-> OBS: ProgramKod (baz kod)
            ObsProgramKod_ProgramId = 4    // Sistem: Program.ObsProgramKod <-> OBS: ProgramId
        }

        #endregion

        #region Public API - Toplu Güncelle

        /// <summary>Seçilen moda göre tüm güncelleme işlemlerini yürütür</summary>
        public static async Task<ProgramGuncellemeRaporu> TumProgramlariGuncelleAsync(EslestirmeModuEnum eslestirmeModu)
        {
            var rapor = new ProgramGuncellemeRaporu();

            try
            {
                var obsProgramlar = await ObsProgramCacheService.GetProgramsFlatAsync();
                if (obsProgramlar == null || !obsProgramlar.Any())
                {
                    rapor.Hatalar.Add("OBS servisinden program verisi alınamadı.");
                    return rapor;
                }

                using (var entities = new LubsDbEntities())
                {
                    var programlar = entities.Programlars.ToList();

                    var guncellenenler = await EslestirmeModuIleGuncelleAsync(
                        entities, programlar, obsProgramlar, eslestirmeModu, rapor.Hatalar);

                    rapor.ObsProgramKodIleGuncellenenSayisi = guncellenenler.Count(p => p.GuncellemeYontemi.Contains("ObsProgramKod"));
                    rapor.ProgramAdiIleGuncellenenSayisi = guncellenenler.Count(p => p.GuncellemeYontemi == "ProgramAdi");
                    rapor.GuncellenenProgramlar.AddRange(guncellenenler);

                    rapor.ToplamKaydedilenSayisi = await entities.SaveChangesAsync();
                    rapor.Basarili = true;
                }
            }
            catch (Exception ex)
            {
                rapor.Basarili = false;
                rapor.Hatalar.Add($"Genel hata: {ex.Message}");
            }

            return rapor;
        }

        #endregion

        #region Public API - Manuel Eşleştir

        /// <summary>Manuel olarak seçilen OBS programı ile eşleştirme yapar</summary>
        public static async Task<ManuelEslestirmeRaporu> ManuelEslestirAsync(string programKod, string secilenObsKod)
        {
            var rapor = new ManuelEslestirmeRaporu { Basarili = false };

            try
            {
                var obsProgramlar = await ObsProgramCacheService.GetProgramsFlatAsync();
                if (obsProgramlar == null || !obsProgramlar.Any())
                {
                    rapor.Mesaj = "OBS servisinden program verisi alınamadı.";
                    return rapor;
                }

                using (var entities = new LubsDbEntities())
                {
                    var program = entities.Programlars.FirstOrDefault(p => p.ProgramKod == programKod);
                    if (program == null)
                    {
                        rapor.Mesaj = "Program bulunamadı!";
                        return rapor;
                    }

                    // Seçilen değer baz kod (ProgramKod ayıklanmış) ya da ProgramId olabilir
                    var obsProgram = obsProgramlar.FirstOrDefault(
                        op => ProgramKodAyikla(op.ProgramKod) == secilenObsKod);

                    if (obsProgram == null)
                    {
                        rapor.Mesaj = "OBS programı bulunamadı!";
                        return rapor;
                    }

                    var yeniObsProgramKod = ProgramKodAyikla(obsProgram.ProgramKod);
                    var eskiObsProgramKod = program.ObsProgramKod;

                    // Her zaman OBS baz kodu yazarız
                    program.ObsProgramKod = yeniObsProgramKod;
                    program.IslemTarihi = DateTime.Now;
                    program.IslemYapanID = UserIdentity.Current.Id;
                    program.IslemYapanIP = UserIdentity.Ip;

                    await entities.SaveChangesAsync();

                    rapor.Basarili = true;
                    rapor.ProgramKod = program.ProgramKod;
                    rapor.EskiObsProgramKod = eskiObsProgramKod;
                    rapor.YeniObsProgramKod = yeniObsProgramKod;
                    rapor.ProgramAdi = program.ProgramAdi;
                    rapor.ObsProgramAdi = obsProgram.ProgramAd;
                    rapor.Mesaj = "Eşleştirme başarılı!";
                }
            }
            catch (Exception ex)
            {
                rapor.Mesaj = "Hata: " + ex.Message;
            }

            return rapor;
        }

        #endregion

        #region Public API - Tek Seferlik Düzeltme

        /// <summary>TEK SEFERLİK: Eski ProgramId formatındaki ObsProgramKod değerlerini düzeltir</summary>
        public static async Task<ObsProgramKodDuzeltmeRaporu> ObsProgramKodlariniDuzeltAsync()
        {
            var rapor = new ObsProgramKodDuzeltmeRaporu();

            try
            {
                var obsProgramlar = await ObsProgramCacheService.GetProgramsFlatAsync();
                if (obsProgramlar == null || !obsProgramlar.Any())
                {
                    rapor.Hatalar.Add("OBS servisinden program verisi alınamadı.");
                    return rapor;
                }

                using (var entities = new LubsDbEntities())
                {
                    var programlar = entities.Programlars
                        .Where(p => !string.IsNullOrEmpty(p.ObsProgramKod))
                        .ToList();

                    foreach (var program in programlar)
                    {
                        try
                        {
                            // Eski formatta (ProgramId) duranları düzelt
                            var byProgramId = obsProgramlar.FirstOrDefault(op => op.ProgramId == program.ObsProgramKod);
                            if (byProgramId != null)
                            {
                                var eskiObsKod = program.ObsProgramKod;
                                var yeniObsKod = ProgramKodAyikla(byProgramId.ProgramKod);

                                program.ObsProgramKod = yeniObsKod;
                                program.IslemTarihi = DateTime.Now;

                                rapor.DuzeltilenProgramlar.Add(new ObsKodDuzeltmeDetay
                                {
                                    ProgramAdi = program.ProgramAdi,
                                    ProgramKod = program.ProgramKod,
                                    EskiObsProgramKod = eskiObsKod,
                                    YeniObsProgramKod = yeniObsKod,
                                    Aciklama = "ProgramId formatından ProgramKod formatına çevrildi"
                                });

                                continue;
                            }

                            // Zaten doğru yerde mi?
                            var byKod = obsProgramlar.FirstOrDefault(op =>
                                ProgramKodAyikla(op.ProgramKod) == program.ObsProgramKod);

                            if (byKod != null)
                            {
                                rapor.ZatenDogruOlanlar++;
                            }
                            else
                            {
                                rapor.EslesmeYokOlanlar.Add(new ObsKodDuzeltmeDetay
                                {
                                    ProgramAdi = program.ProgramAdi,
                                    ProgramKod = program.ProgramKod,
                                    EskiObsProgramKod = program.ObsProgramKod,
                                    Aciklama = "OBS'de eşleşme bulunamadı"
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            rapor.Hatalar.Add($"{program.ProgramAdi}: {ex.Message}");
                        }
                    }

                    if (rapor.DuzeltilenProgramlar.Any())
                    {
                        rapor.ToplamDuzeltilenSayisi = await entities.SaveChangesAsync();
                        rapor.Basarili = true;
                    }
                    else
                    {
                        rapor.ToplamDuzeltilenSayisi = 0;
                        rapor.Basarili = true;
                    }
                }
            }
            catch (Exception ex)
            {
                rapor.Basarili = false;
                rapor.Hatalar.Add($"Genel hata: {ex.Message}");
            }

            return rapor;
        }

        #endregion

        #region Public API - Karşılaştırma Listesi

        /// <summary>Seçilen eşleştirme moduna göre programları karşılaştırır (ne değişecek?)</summary>
        public static async Task<List<ProgramKarsilastirmaDto>> GuncellenecekleriListeleAsync(EslestirmeModuEnum eslestirmeModu)
        {
            var liste = new List<ProgramKarsilastirmaDto>();

            try
            {
                var obsProgramlar = await ObsProgramCacheService.GetProgramsFlatAsync();
                if (obsProgramlar == null || !obsProgramlar.Any())
                    return liste;

                using (var entities = new LubsDbEntities())
                {
                    var programlar = entities.Programlars.Where(p => p.IsAktif).ToList();

                    foreach (var program in programlar)
                    {
                        ObsProgramFullDto obsProgram = null;
                        string eslesmeTipi = "Yok";

                        // 1) Seçilen moda göre
                        switch (eslestirmeModu)
                        {
                            case EslestirmeModuEnum.ProgramKod_ObsProgramKod:
                                obsProgram = obsProgramlar.FirstOrDefault(
                                    op => ProgramKodAyikla(op.ProgramKod) == program.ProgramKod);
                                eslesmeTipi = obsProgram != null ? "ProgramKod-ObsProgramKod" : "Yok";
                                break;

                            case EslestirmeModuEnum.ProgramKod_ProgramId:
                                obsProgram = obsProgramlar.FirstOrDefault(op => op.ProgramId == program.ProgramKod);
                                eslesmeTipi = obsProgram != null ? "ProgramKod-ProgramId" : "Yok";
                                break;

                            case EslestirmeModuEnum.ObsProgramKod_ObsProgramKod:
                                if (!string.IsNullOrEmpty(program.ObsProgramKod))
                                {
                                    obsProgram = obsProgramlar.FirstOrDefault(
                                        op => ProgramKodAyikla(op.ProgramKod) == program.ObsProgramKod);
                                    eslesmeTipi = obsProgram != null ? "ObsProgramKod-ObsProgramKod" : "Yok";
                                }
                                break;

                            case EslestirmeModuEnum.ObsProgramKod_ProgramId:
                                if (!string.IsNullOrEmpty(program.ObsProgramKod))
                                {
                                    obsProgram = obsProgramlar.FirstOrDefault(op => op.ProgramId == program.ObsProgramKod);
                                    eslesmeTipi = obsProgram != null ? "ObsProgramKod-ProgramId" : "Yok";
                                }
                                break;
                        }

                        // 2) Yedek: isimle
                        if (obsProgram == null)
                        {
                            obsProgram = obsProgramlar.FirstOrDefault(op =>
                                ProgramAdiTemizle(op.ProgramAd)
                                .Equals(ProgramAdiTemizle(program.ProgramAdi), StringComparison.OrdinalIgnoreCase));

                            if (obsProgram != null)
                                eslesmeTipi = "ProgramAdi";
                        }

                        if (obsProgram != null)
                        {
                            var yeniObsProgramKod = ProgramKodAyikla(obsProgram.ProgramKod);

                            liste.Add(new ProgramKarsilastirmaDto
                            {
                                ProgramKod = program.ProgramKod,
                                ProgramAdi = program.ProgramAdi,
                                MevcutObsProgramKod = program.ObsProgramKod,
                                YeniObsProgramKod = yeniObsProgramKod,
                                ObsProgramId = obsProgram.ProgramId,
                                Degisecek = string.IsNullOrEmpty(program.ObsProgramKod) ||
                                            program.ObsProgramKod != yeniObsProgramKod,
                                EslesmeTipi = eslesmeTipi,
                                ObsProgramAdi = obsProgram.ProgramAd
                            });
                        }
                        else
                        {
                            liste.Add(new ProgramKarsilastirmaDto
                            {
                                ProgramKod = program.ProgramKod,
                                ProgramAdi = program.ProgramAdi,
                                MevcutObsProgramKod = program.ObsProgramKod,
                                YeniObsProgramKod = "Eşleşme Bulunamadı",
                                Degisecek = false,
                                EslesmeTipi = "Yok"
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Gerekirse loglayın
            }

            return liste;
        }

        #endregion

        #region Public API - Dropdown (Gruplanmış + Normalize İsim + TR-duyarsız arama)

        /// <summary>
        /// Select2 için: aynı baz koda sahip (seviye farklı olsa da) programları tek satırda birleştirir,
        /// isimleri standardize eder, DisplayText’i zenginleştirir.
        /// </summary>
        public static async Task<List<ObsProgramDropdownDto>> GetObsProgramlarDropdownAsync()
            => await GetObsProgramlarDropdownAsyncInternal(null, null);

        /// <summary>
        /// İsteğe bağlı arama + limit ile (Select2’de q boşsa hepsini, doluysa filtreli döner)
        /// </summary>
        public static async Task<List<ObsProgramDropdownDto>> GetObsProgramlarDropdownAsync(string q = null, int? take = null)
            => await GetObsProgramlarDropdownAsyncInternal(q, take);

        private static async Task<List<ObsProgramDropdownDto>> GetObsProgramlarDropdownAsyncInternal(string q, int? take)
        {
            var raw = await ObsProgramCacheService.GetProgramsFlatAsync();
            if (raw == null || raw.Count == 0) return new List<ObsProgramDropdownDto>();

            var projected = raw.Select(op =>
            {
                var baseKod = ProgramKodAyikla(op.ProgramKod);     // 701-3 -> 701
                var lvlFromKod = LevelFromSuffix(op.ProgramKod);      // 3 -> DR
                var stdName = NormalizeProgramName(op.ProgramAd);  // sondaki seviye vb. temiz
                var lvlFromName = LevelFromName(op.ProgramAd);         // isimden seviye yedeği

                return new
                {
                    BaseKod = baseKod,
                    Level = lvlFromKod ?? lvlFromName, // kod yoksa isimden
                    FakulteKod = op.FakulteKod,
                    ProgramId = op.ProgramId,
                    ProgramAdiStd = stdName,
                    ProgramAdRaw = op.ProgramAd,
                    BolumAdi = op.BolumAd,
                    FakulteAdi = op.FakulteAd,
                    SearchBlob = NormSearch($"{baseKod} {stdName} {op.ProgramAd} {op.BolumAd} {op.FakulteAd}")
                };
            });

            // TR/Case duyarsız arama
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = NormSearch(q);
                projected = projected.Where(x => x.SearchBlob.Contains(term));
            }

            // Aynı baz kod + normalize ad + bölüm/fakülte -> tek satır
            var grouped = projected.GroupBy(g => new { g.BaseKod, g.ProgramAdiStd, g.BolumAdi, g.FakulteAdi, g.FakulteKod });

            var result = grouped.Select(g =>
            {
                var repr = g.OrderBy(x => LevelPriority(x.Level)).First();

                var levels = g.Select(x => x.Level)
                              .Where(l => !string.IsNullOrEmpty(l))
                              .Distinct()
                              .OrderBy(LevelPriority)
                              .ToList();

                var levelsStr = levels.Count > 0 ? $" [{string.Join(", ", levels)}]" : "";

                return new ObsProgramDropdownDto
                {
                    FakulteKod = g.Key.FakulteKod,
                    ObsProgramKod = g.Key.BaseKod,            // dropdown value (baz kod)
                    ProgramId = repr.ProgramId,           // temsilci
                    ProgramAdi = g.Key.ProgramAdiStd,      // normalize ad
                    BolumAdi = g.Key.BolumAdi,
                    FakulteAdi = g.Key.FakulteAdi,
                    DisplayText = $"{g.Key.BolumAdi} / {g.Key.ProgramAdiStd}{levelsStr} (Kod: {g.Key.BaseKod})"
                };
            })
            .OrderBy(o => o.BolumAdi)
            .ThenBy(o => o.ProgramAdi).ToList();

            if (take.HasValue && take.Value > 0)
                result = result.Take(take.Value).ToList();

            return result.ToList();
        }

        #endregion

        #region Core - Eşleştirme Motoru

        /// <summary>Seçilen eşleştirme moduna göre programları günceller (çekirdek iş)</summary>
        private static async Task<List<ProgramGuncellemeDetay>> EslestirmeModuIleGuncelleAsync(
            LubsDbEntities entities,
            List<Programlar> programlar,
            List<ObsProgramFullDto> obsProgramlar,
            EslestirmeModuEnum eslestirmeModu,
            List<string> hataListesi)
        {
            var guncellenenler = new List<ProgramGuncellemeDetay>();

            foreach (var program in programlar)
            {
                try
                {
                    ObsProgramFullDto obsProgram = null;
                    string yontem = "Yok";

                    // 1) Seçilen moda göre OBS programını bul
                    switch (eslestirmeModu)
                    {
                        case EslestirmeModuEnum.ProgramKod_ObsProgramKod:
                            obsProgram = obsProgramlar.FirstOrDefault(op =>
                                ProgramKodAyikla(op.ProgramKod) == program.ProgramKod);
                            yontem = obsProgram != null ? "ProgramKod-ObsProgramKod" : "Yok";
                            break;

                        case EslestirmeModuEnum.ProgramKod_ProgramId:
                            obsProgram = obsProgramlar.FirstOrDefault(op => op.ProgramId == program.ProgramKod);
                            yontem = obsProgram != null ? "ProgramKod-ProgramId" : "Yok";
                            break;

                        case EslestirmeModuEnum.ObsProgramKod_ObsProgramKod:
                            if (!string.IsNullOrEmpty(program.ObsProgramKod))
                            {
                                obsProgram = obsProgramlar.FirstOrDefault(op =>
                                    ProgramKodAyikla(op.ProgramKod) == program.ObsProgramKod);
                                yontem = obsProgram != null ? "ObsProgramKod-ObsProgramKod" : "Yok";
                            }
                            break;

                        case EslestirmeModuEnum.ObsProgramKod_ProgramId:
                            if (!string.IsNullOrEmpty(program.ObsProgramKod))
                            {
                                obsProgram = obsProgramlar.FirstOrDefault(op =>
                                    op.ProgramId == program.ObsProgramKod);
                                yontem = obsProgram != null ? "ObsProgramKod-ProgramId" : "Yok";
                            }
                            break;
                    }

                    // 2) Yedek: isimle eşleştir
                    if (obsProgram == null)
                    {
                        obsProgram = obsProgramlar.FirstOrDefault(op =>
                            ProgramAdiTemizle(op.ProgramAd)
                            .Equals(ProgramAdiTemizle(program.ProgramAdi), StringComparison.OrdinalIgnoreCase));
                        if (obsProgram != null)
                            yontem = "ProgramAdi";
                    }

                    // 3) Güncelle
                    if (obsProgram != null)
                    {
                        var yeniObsProgramKod = ProgramKodAyikla(obsProgram.ProgramKod);

                        if (string.IsNullOrEmpty(program.ObsProgramKod) || program.ObsProgramKod != yeniObsProgramKod)
                        {
                            var eskiObsKod = program.ObsProgramKod;

                            program.ObsProgramKod = yeniObsProgramKod; // her zaman OBS baz kod
                            program.IslemTarihi = DateTime.Now;

                            guncellenenler.Add(new ProgramGuncellemeDetay
                            {
                                ProgramAdi = program.ProgramAdi,
                                ProgramKod = program.ProgramKod,
                                EskiObsProgramKod = eskiObsKod,
                                YeniObsProgramKod = yeniObsProgramKod,
                                GuncellemeYontemi = yontem,
                                ObsProgramId = obsProgram.ProgramId,
                                ObsProgramAdi = obsProgram.ProgramAd
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    hataListesi?.Add($"ProgramKod:{program?.ProgramKod} için hata: {ex.Message}");
                }
            }

            await Task.CompletedTask;
            return guncellenenler;
        }

        #endregion

        #region Helpers (TR-duyarsız arama, isim standardizasyonu, seviye çıkarımı)

        /// <summary>Türkçe-dostu diakritik kaldırma (İ/ı vb. özel durumlar)</summary>
        private static string RemoveDiacriticsTr(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Türkçe özel normalizasyon
            input = input
                .Replace('İ', 'I')
                .Replace('I', 'I')
                .Replace('ı', 'i')
                .Replace('i', 'i');

            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(capacity: normalized.Length);
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>Arama için: diakritiksiz + lowerInvariant + tek boşluk</summary>
        private static string NormSearch(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = RemoveDiacriticsTr(s).ToLowerInvariant();
            s = Regex.Replace(s, @"\s+", " ").Trim();
            return s;
        }

        // sonda yer alan seviye/öğrenim türü ifadelerini (çeşitli varyasyonlar) temizler
        // ör: (Doktora), - Doktora Programı, (Tezsiz Yüksek Lisans) (İÖ), (DR), (YL), (TYL), (BDR) ...
        private static readonly Regex LevelTailRx = new Regex(
            @"\s*(?:[-–]\s*)?(?:\((?:Tezli|Tezsiz)?\s*(?:Y[üu]ksek\s*Lisans|YL|TYL|Doktora|DR|BDR)(?:[^()]*)?\)|(?:(?:Tezli|Tezsiz)?\s*(?:Y[üu]ksek\s*Lisans|Doktora)(?:\s*Program[ıi])?(?:\s*\(İÖ\))?))\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>İsim standardizasyonu: sondaki seviye/tür parantezlerini sök, tire/boşluk düzelt</summary>
        private static string NormalizeProgramName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;

            string s = name.Trim();

            // sondaki seviye bloklarını teker teker sök (birden fazla olabilir)
            string prev;
            do { prev = s; s = LevelTailRx.Replace(s, ""); } while (s != prev);

            // tire aralıklarını sabitle ve fazla boşlukları temizle
            s = Regex.Replace(s, @"\s*-\s*", " - ");
            s = Regex.Replace(s, @"\s{2,}", " ").Trim();
            return s;
        }

        /// <summary>Karşılaştırma için aksansız/normalize ad</summary>
        private static string ProgramAdiTemizle(string programAdi)
        {
            if (string.IsNullOrEmpty(programAdi))
                return string.Empty;

            // diakritiksiz + spacing normalize
            var s = RemoveDiacriticsTr(programAdi).Trim();
            s = Regex.Replace(s, @"\s+", " ");
            return s;
        }

        /// <summary>OBS program kodundan baz kodu ayıklar (örn. 701-3 → 701)</summary>
        public static string ProgramKodAyikla(string programKod)
        {
            if (string.IsNullOrEmpty(programKod)) return programKod;
            var idx = programKod.IndexOf('-');
            return idx > 0 ? programKod.Substring(0, idx) : programKod;
        }

        /// <summary>Koddaki seviye rakamını etikete çevirir (1:YL,2:TYL,3:DR,5:BDR)</summary>
        private static string LevelFromSuffix(string programKod)
        {
            if (string.IsNullOrWhiteSpace(programKod)) return null;
            var parts = programKod.Split('-');
            if (parts.Length < 2) return null;

            switch (parts[1].ToIntObj())
            {
                case OgrenimTipi.TezsizYuksekLisans: return "YL";
                case OgrenimTipi.TezliYuksekLisans: return "TYL";
                case OgrenimTipi.Doktra: return "DR";
                case OgrenimTipi.ButunlesikDoktora: return "BDR";
                default: return null;
            }
        }

        /// <summary>İsimden seviye çıkarımı (doktor/tezsiz/yüksek lisans/bilimsel hazırlık)</summary>
        private static string LevelFromName(string programAd)
        {
            if (string.IsNullOrWhiteSpace(programAd)) return null;

            var x = NormSearch(programAd); // diakritiksiz + lower

            // öncelik: DR > YL > BDR > TYL (temsil seçim ve gösterimde)
            if (x.Contains("doktor")) return "DR";                           // doktora / doktara vs. yakalar
            if (x.Contains("tezsiz") && (x.Contains("yuksek lisans") || Regex.IsMatch(x, @"\byl\b"))) return "TYL";
            if (x.Contains("yuksek lisans") || Regex.IsMatch(x, @"\byl\b")) return "YL";
            if (x.Contains("bilimsel hazirlik") || Regex.IsMatch(x, @"\bbdr\b")) return "BDR";

            return null;
        }

        /// <summary>Seviye önceliği (listede görünüm ve temsilci seçiminde kullanılır)</summary>
        private static int LevelPriority(string lvl)
        {
            return lvl == "DR" ? 0
                 : lvl == "YL" ? 1
                 : lvl == "BDR" ? 2
                 : lvl == "TYL" ? 3
                 : 4;
        }

        #endregion

        #region DTO’lar (Nested)

        public class ProgramGuncellemeRaporu
        {
            public ProgramGuncellemeRaporu()
            {
                GuncellenenProgramlar = new List<ProgramGuncellemeDetay>();
                Hatalar = new List<string>();
            }
            public bool Basarili { get; set; }
            public int ObsProgramKodIleGuncellenenSayisi { get; set; }
            public int ProgramAdiIleGuncellenenSayisi { get; set; }
            public int ToplamKaydedilenSayisi { get; set; }
            public List<ProgramGuncellemeDetay> GuncellenenProgramlar { get; set; }
            public List<string> Hatalar { get; set; }
        }

        public class ProgramGuncellemeDetay
        {
            public string ProgramAdi { get; set; }
            public string ProgramKod { get; set; }
            public string EskiObsProgramKod { get; set; }
            public string YeniObsProgramKod { get; set; }
            public string GuncellemeYontemi { get; set; }
            public string ObsProgramId { get; set; }
            public string ObsProgramAdi { get; set; }
        }

        public class ProgramKarsilastirmaDto
        {
            public string ProgramKod { get; set; }
            public string ProgramAdi { get; set; }
            public string MevcutObsProgramKod { get; set; }
            public string YeniObsProgramKod { get; set; }
            public string ObsProgramId { get; set; }
            public string ObsProgramAdi { get; set; }
            public bool Degisecek { get; set; }
            public string EslesmeTipi { get; set; }
        }

        public class ManuelEslestirmeRaporu
        {
            public bool Basarili { get; set; }
            public string Mesaj { get; set; }
            public string ProgramAdi { get; set; }
            public string ProgramKod { get; set; }
            public string EskiObsProgramKod { get; set; }
            public string YeniObsProgramKod { get; set; }
            public string ObsProgramAdi { get; set; }
        }

        public class ObsProgramDropdownDto
        {
            public string FakulteKod { get; set; }
            public string ObsProgramKod { get; set; } // Baz kod (dropdown value)
            public string ProgramId { get; set; }
            public string ProgramAdi { get; set; }
            public string BolumAdi { get; set; }
            public string FakulteAdi { get; set; }
            public string DisplayText { get; set; }
        }

        public class ObsProgramKodDuzeltmeRaporu
        {
            public ObsProgramKodDuzeltmeRaporu()
            {
                DuzeltilenProgramlar = new List<ObsKodDuzeltmeDetay>();
                EslesmeYokOlanlar = new List<ObsKodDuzeltmeDetay>();
                Hatalar = new List<string>();
            }

            public bool Basarili { get; set; }
            public int ToplamDuzeltilenSayisi { get; set; }
            public int ZatenDogruOlanlar { get; set; }
            public List<ObsKodDuzeltmeDetay> DuzeltilenProgramlar { get; set; }
            public List<ObsKodDuzeltmeDetay> EslesmeYokOlanlar { get; set; }
            public List<string> Hatalar { get; set; }
        }

        public class ObsKodDuzeltmeDetay
        {
            public string ProgramAdi { get; set; }
            public string ProgramKod { get; set; }
            public string EskiObsProgramKod { get; set; }
            public string YeniObsProgramKod { get; set; }
            public string Aciklama { get; set; }
        }

        #endregion
    }
}
