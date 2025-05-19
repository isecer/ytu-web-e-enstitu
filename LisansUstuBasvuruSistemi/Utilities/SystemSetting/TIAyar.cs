using Entities.Entities;
using System.Linq;
using System;
using System.Collections.Generic;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{
    public static class TiAyar
    {
        public class TiAyarProperty
        {
            internal string PropertyValue { get; }
            internal TiAyarProperty(string value)
            {
                PropertyValue = value;
            }
            public static implicit operator string(TiAyarProperty prop) => prop.PropertyValue;
        }

        public static readonly TiAyarProperty TikOneriAlimiAcik = new TiAyarProperty("Tik öneri alımı açık");

        public static readonly TiAyarProperty TezOneriSavunmaBasvuruAlimiAcik = new TiAyarProperty("Tez öneri savunma sınavı başvuru alımı açık");
        public static readonly TiAyarProperty TezOneriSavunmaSinaviOnlineYapilabilsin = new TiAyarProperty("Tez öneri savunma sınavı online yapılabilsin");
        public static readonly TiAyarProperty TezOneriSavunmaSinaviYuzYuzeYapilabilsin = new TiAyarProperty("Tez öneri savunma sınavı yüz yüze yapılabilsin");
        public static readonly TiAyarProperty TezOneriToplamBasarisizTezOneriSavunmaHak = new TiAyarProperty("Toplam başarısız tez öneri savunma sınavı hakkı");
        public static readonly TiAyarProperty TezOneriDuzeltmeSonrasiSavunmaHakkiAyKriter = new TiAyarProperty("Düzeltme alan öğrenci yeni sınavı kaç ay içinde amalı");
        public static readonly TiAyarProperty TezOneriIlkSavunmaHakkiAyKriter = new TiAyarProperty("ilk başvuru için 1. Savunma kaç ay içinde yapılmalı");
        public static readonly TiAyarProperty TezOneriIkinciSavunmaHakkiAyKriter = new TiAyarProperty("ilk başvuru için 2. Savunma kaç ay içinde yapılmalı");

        public static readonly TiAyarProperty TiBasvurusuAcikmi = new TiAyarProperty("Ara rapor başvuru alımı açık");
        public static readonly TiAyarProperty TiGecmisAraRaporBasvurulariDegerlendirilebilsin = new TiAyarProperty("Geçmiş ara rapor başvuruları değerlendirilebilsin");

        public static readonly TiAyarProperty TiSonDonemKayitOlunmasiGerekenDersKodlari = new TiAyarProperty("Son dönem kayıt olunması gereken ders kodları");
        public static readonly TiAyarProperty TiAraRaporSinaviOnlineYapilabilsin = new TiAyarProperty("Tez izleme ara rapor sınavı online yapılabilsin");
        public static readonly TiAyarProperty TiAraRaporSinaviYuzYuzeYapilabilsin = new TiAyarProperty("Tez izleme ara rapor sınavı yüz yüze yapılabilsin");
        public static readonly TiAyarProperty TiAraRaporBaharDonemiAylari = new TiAyarProperty("Ara Rapor başvurusunda Dönem seçimi için belirlenen aylar Bahar dönemi kabul edilecek");

        public static void SetAyarTi(string ayarAdi, string ayarDegeri, string enstituKod)
        {
            using (var entities = new LubsDbEntities())
            {
                var ayar = entities.TIAyarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi && p.EnstituKod == enstituKod);
                if (ayar != null)
                {
                    ayar.AyarDegeri = ayarDegeri;
                }
                else
                {
                    entities.TIAyarlars.Add(new TIAyarlar
                    {
                        AyarAdi = ayarAdi,
                        AyarDegeri = ayarDegeri,
                        EnstituKod = enstituKod
                    });
                }
                entities.SaveChanges();
            }
        }

        public static string GetAyar(this TiAyarProperty ayarProperty, string enstituKodu, string varsayilanDeger = "")
        {
            using (var entities = new LubsDbEntities())
            {
                var ayar = entities.TIAyarlars.FirstOrDefault(p => p.AyarAdi == ayarProperty.PropertyValue && p.EnstituKod == enstituKodu);
                return ayar != null ? ayar.AyarDegeri : varsayilanDeger;
            }
        }

        public static List<int> GetBaharDonemiIcinSecilenAyNos(string enstituKod)
        {
            var ayNoStr = TiAraRaporBaharDonemiAylari.GetAyar(enstituKod);
            if (ayNoStr.IsNullOrWhiteSpace())
                return new List<int>();
            return ayNoStr.Split(',').Select(s => s.Trim().ToInt().Value).ToList();
        }
    }
}
