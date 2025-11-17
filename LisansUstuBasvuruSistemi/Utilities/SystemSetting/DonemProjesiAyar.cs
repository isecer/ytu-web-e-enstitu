using System.Collections.Generic;
using Entities.Entities;
using System.Linq;
using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{
    public static class DonemProjesiAyar
    {
        public class DonemProjesiAyarProperty
        {
            internal string PropertyValue { get; }

            internal DonemProjesiAyarProperty(string value)
            {
                PropertyValue = value;
            }

            public static implicit operator string(DonemProjesiAyarProperty property)
            {
                return property.PropertyValue;
            }
        }

        public static readonly DonemProjesiAyarProperty DonemProjesiDersKodu = new DonemProjesiAyarProperty("Dönem Projesi Ders Kodu");
        public static readonly DonemProjesiAyarProperty DonemProjesiBasvuruAlimiAcik = new DonemProjesiAyarProperty("Dönem Projesi başvurusu açık");
        public static readonly DonemProjesiAyarProperty DonemProjesiEykOgrenciDogrulamaAcik = new DonemProjesiAyarProperty("EYK işlemlerinde öğrenci doğrulaması açık");
        public static readonly DonemProjesiAyarProperty OgrencininBasvuruYapabilecegiDonemler = new DonemProjesiAyarProperty("Öğrencinin başvuru yapabileceği dönemler");
        public static readonly DonemProjesiAyarProperty DonemSecimiIcinBelirlenenAylarBaharDonemi = new DonemProjesiAyarProperty("Dönem seçimi için belirlenen aylar Bahar dönemi kabul edilecek");
        public static readonly DonemProjesiAyarProperty OgrencininBasvuruDonemindeAlmasiGerekenDersKodlari = new DonemProjesiAyarProperty("Başvuru döneminde öğrencinin OBS'de alması gereken dersler");
        public static readonly DonemProjesiAyarProperty SinavOnlineYapilabilsin = new DonemProjesiAyarProperty("Dönem Projesi sınavı online yapılabilsin");
        public static readonly DonemProjesiAyarProperty SinavYuzyuzeYapilabilsin = new DonemProjesiAyarProperty("Dönem Projesi sınavı yüz yüze yapılabilsin");
        public static readonly DonemProjesiAyarProperty EnFazlaTekKaynakOrani = new DonemProjesiAyarProperty("En fazla tek kaynak oranı");
        public static readonly DonemProjesiAyarProperty EnFazlaToplamKaynakOrani = new DonemProjesiAyarProperty("En fazla toplam kaynak oranı");
        public static readonly DonemProjesiAyarProperty BasariliKrediSayisi = new DonemProjesiAyarProperty("Başarılı kredi sayısı");
        public static readonly DonemProjesiAyarProperty KontrolEdilecekMinDersNotlari = new DonemProjesiAyarProperty("Kontrol edilecek min ders notları");
        public static readonly DonemProjesiAyarProperty DonemProjesiIlkBasvuruAnketi = new DonemProjesiAyarProperty("Dönem Projesi Başvuru Anketi");

        public static string GetAyar(this DonemProjesiAyarProperty ayarProperty, string enstituKodu, string varsayilanDeger = "")
        {
            using (var entities = new LubsDbEntities())
            {
                var ayar = entities.DonemProjesiAyarlars
                    .FirstOrDefault(p => p.AyarAdi == ayarProperty.PropertyValue && p.EnstituKod == enstituKodu);
                return ayar != null ? ayar.AyarDegeri : varsayilanDeger;
            }
        }
         

        public static List<string> GetBasvuruDonemindeAlmasiGerekenDersKodlari(string enstituKod)
        {
            var dersKodlariStr = OgrencininBasvuruDonemindeAlmasiGerekenDersKodlari.GetAyar(enstituKod);
            return string.IsNullOrWhiteSpace(dersKodlariStr)
                ? new List<string>()
                : dersKodlariStr.Split(',').Select(s => s.Trim()).ToList();
        }

        public static List<int> GetBasvuruYapilabilecekDonemNos(string enstituKod)
        {
            var donemNoStr = OgrencininBasvuruYapabilecegiDonemler.GetAyar(enstituKod);
            return string.IsNullOrWhiteSpace(donemNoStr)
                ? new List<int>()
                : donemNoStr.Split(',').Select(s => s.Trim()).Select(int.Parse).ToList();
        }

        public static List<int> GetBaharDonemiIcinSecilenAyNos(string enstituKod)
        {
            var ayNoStr = DonemSecimiIcinBelirlenenAylarBaharDonemi.GetAyar(enstituKod);
            return string.IsNullOrWhiteSpace(ayNoStr)
                ? new List<int>()
                : ayNoStr.Split(',').Select(s => s.Trim()).Select(int.Parse).ToList();
        }

        public static List<CmbStringDto> GetKontrolEdilecekMinDersNotlari(string enstituKod)
        {
            var dersMinNotlariStr = KontrolEdilecekMinDersNotlari.GetAyar(enstituKod);
            if (string.IsNullOrWhiteSpace(dersMinNotlariStr))
                return new List<CmbStringDto>();

            return dersMinNotlariStr.Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Contains('='))
                .Select(s => new CmbStringDto
                {
                    Value = s.Split('=')[0].Trim(),
                    Caption = s.Split('=')[1].Trim()
                }).ToList();
        }
    }
}

