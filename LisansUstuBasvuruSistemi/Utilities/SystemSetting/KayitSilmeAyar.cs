using System;
using System.Collections.Generic;
using Entities.Entities;
using System.Linq;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{

    public static class KayitSilmeAyar
    {
        public class KayitSilmeAyarProperty
        {
            internal string PropertyValue { get; }

            internal KayitSilmeAyarProperty(string value)
            {
                PropertyValue = value;
            }

            public static implicit operator string(KayitSilmeAyarProperty property)
            {
                return property.PropertyValue;
            }
            public override string ToString()
            {
                return PropertyValue;
            }
        }

        public static readonly KayitSilmeAyarProperty KayitSilmeBasvuruAlimiAcik =
            new KayitSilmeAyarProperty("Kayıt silme başvuru alımı açık");
        public static readonly KayitSilmeAyarProperty HarcBirimiOnaySorumlusuKullaniciId =
            new KayitSilmeAyarProperty("Harç Birimi Onay Sorumlusu");
        public static readonly KayitSilmeAyarProperty KutuphaneBirimiOnaySorumlusuKullaniciId =
            new KayitSilmeAyarProperty("Kütüphane Birimi Onay Sorumlusu");
        public static readonly KayitSilmeAyarProperty DonemGecisTarihi =
            new KayitSilmeAyarProperty("Dönem Geçiş Tarihi");
        public static string GetAyar(this KayitSilmeAyarProperty ayarProperty, string enstituKodu, string varsayilanDeger = "")
        {
            using (var entities = new LubsDbEntities())
            {
                var ayar = entities.KayitSilmeAyarlars.FirstOrDefault(p => p.AyarAdi == ayarProperty.PropertyValue && (p.EnstituKod == enstituKodu || p.EnstituKod == ""));
                return ayar != null ? ayar.AyarDegeri : varsayilanDeger;
            }
        } 
        public static int? GetDonemId(DateTime basvuruTarihi, string enstituKodu)
        {
            var maxGuzDonemiTarih = DonemGecisTarihi.GetAyar(enstituKodu);
            if (maxGuzDonemiTarih != null)
            {
                var maxDonemTarihVal = maxGuzDonemiTarih.ToDate();
                return basvuruTarihi.Date < maxDonemTarihVal.Value.Date 
                    ? AkademikDonemEnum.BaharYariyili
                    : AkademikDonemEnum.GuzYariyili;
            }

            return null;
        }

        public static List<int> GetHarcBirimiOnaySorumlusuKullaniciIds()
        {
            var strIds = GetAyar(HarcBirimiOnaySorumlusuKullaniciId, EnstituKodlariEnum.FenBilimleri);
            if (strIds.IsNullOrWhiteSpace()) return new List<int>();
            return strIds.Split(',').Select(s => s.ToInt() ?? 0).Where(p => p > 0).ToList();
        }
        public static List<int> GetKutuphaneBirimiOnaySorumlusuKullaniciIds()
        {
            var strIds = GetAyar(KutuphaneBirimiOnaySorumlusuKullaniciId, EnstituKodlariEnum.FenBilimleri);
            if (strIds.IsNullOrWhiteSpace()) return new List<int>();
            return strIds.Split(',').Select(s => s.ToInt() ?? 0).Where(p => p > 0).ToList();
        }
    }


}