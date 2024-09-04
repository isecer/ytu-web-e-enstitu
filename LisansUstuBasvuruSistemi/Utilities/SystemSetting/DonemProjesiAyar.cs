using Entities.Entities;
using System.Collections.Generic;
using System.Linq;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{

    public static class DonemProjesiAyar
    {

        public const string DonemProjesiDersKodu = "Dönem Projesi Ders Kodu";
        public const string DonemProjesiBasvuruAlimiAcik = "Dönem Projesi başvurusu açık";
        public const string DonemProjesiEykOgrenciDogrulamaAcik = "EYK işlemlerinde öğrenci doğrulaması açık";
        public const string OgrencininBasvuruYapabilecegiDonemler = "Öğrencinin başvuru yapabileceği dönemler";
        public const string DonemSecimiIcinBelirlenenAylarBaharDonemi = "Dönem seçimi için belirlenen aylar Bahar dönemi kabul edilecek";
        public const string OgrencininBasvuruDonemindeAlmasiGerekenDersKodlari = "Başvuru döneminde öğrencinin OBS'de alması gereken dersler";
        public const string SinavOnlineYapilabilsin = "Dönem Projesi sınavı online yapılabilsin";
        public const string SinavYuzyuzeYapilabilsin = "Dönem Projesi sınavı yüz yüze yapılabilsin";
        public const string EnFazlaTekKaynakOrani = "En fazla tek kaynak oranı";
        public const string EnFazlaToplamKaynakOrani = "En fazla toplam kaynak oranı";
        public const string BasariliKrediSayisi = "Başarılı kredi sayısı";
        public const string KontrolEdilecekMinDersNotlari = "Kontrol edilecek min ders notları";



        public static void SetAyarDp(string ayarAdi, string ayarDegeri, string enstituKod)
        {
            using (var entities = new LubsDbEntities())
            {
                var ayar = entities.DonemProjesiAyarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi && p.EnstituKod == enstituKod);
                if (ayar != null)
                {
                    ayar.AyarDegeri = ayarDegeri;
                }
                else
                {
                    entities.DonemProjesiAyarlars.Add(new DonemProjesiAyarlar { AyarAdi = ayarAdi, AyarDegeri = ayarDegeri });

                }
                entities.SaveChanges();
            }
        }
        public static string GetAyarDp(this string ayarAdi, string enstituKodu, string varsayilanDeger = "")
        {
            using (var entities = new LubsDbEntities())
            {
                var ayar = entities.DonemProjesiAyarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi && p.EnstituKod == enstituKodu);
                return ayar != null ? ayar.AyarDegeri : varsayilanDeger;
            }
        }

        public static List<string> GetBasvuruDonemindeAlmasiGerekenDersKodlari(string enstituKod)
        {
            var dersKodlariStr = OgrencininBasvuruDonemindeAlmasiGerekenDersKodlari.GetAyarDp(enstituKod);
            return dersKodlariStr.IsNullOrWhiteSpace() ? new List<string>() : dersKodlariStr.Split(',').Select(s => s.Trim()).ToList();
        }

        public static List<int> GetBasvuruYapilabilecekDonemNos(string enstituKod)
        {
            var donemNoStr = OgrencininBasvuruYapabilecegiDonemler.GetAyarDp(enstituKod);
            if (donemNoStr.IsNullOrWhiteSpace())
                return new List<int>();
            return donemNoStr.Split(',').Select(s => s.Trim().ToInt().Value).ToList();
        }
        public static List<int> GetBaharDonemiIcinSecilenAyNos(string enstituKod)
        {
            var ayNoStr = DonemSecimiIcinBelirlenenAylarBaharDonemi.GetAyarDp(enstituKod);
            if (ayNoStr.IsNullOrWhiteSpace())
                return new List<int>();
            return ayNoStr.Split(',').Select(s => s.Trim().ToInt().Value).ToList();
        }
        public static List<CmbStringDto> GetKontrolEdilecekMinDersNotlari(string enstituKod)
        {
            var dersMinNotlariStr = KontrolEdilecekMinDersNotlari.GetAyarDp(enstituKod);
            if (dersMinNotlariStr.IsNullOrWhiteSpace())
                return new List<CmbStringDto>();
            var notlars = dersMinNotlariStr.Split(',').Select(s => s.Trim()).ToList();
            return notlars.Select(s => new CmbStringDto
            {
                Value = s.Split('=')[0].Trim(),
                Caption = s.Split('=')[1].Trim()
            }).ToList();
        }
    }


}