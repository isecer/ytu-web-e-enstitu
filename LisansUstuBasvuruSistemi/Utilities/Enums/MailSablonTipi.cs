using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public static class MailSablonTipi
    {
        public const byte Normal = 1;
        public const byte OtoMailAsil = 2;
        public const byte OtoMailYedek = 3;
        public const byte OtoMailKazanamadi = 4;
        public const byte OtoMailKayitOldu = 5;
        public const byte OtoMailOnKayitOldu = 71;
        public const byte OtoMailOnKayitOldu1 = 107;
        public const byte OtoMailSinavYerBilgi = 6;
        public const byte OtoMailAnketBilgi = 7;
        public const byte LisansustuSanalPosOdemeBilgisi = 8;
        public const byte Mez_EykTarihiGirildiOgrenciYL = 9;
        public const byte Mez_EykTarihiGirildiOgrenciDoktora = 63;
        public const byte Mez_EykTarihiGirildiDanismanDoktora = 53;
        public const byte Mez_EykTarihiGirildiJuriAsilDoktora = 54;
        public const byte Mez_EykTarihiGirildiDanismanYL = 56;
        public const byte Mez_EykTarihiGirildiJuriAsilYL = 57;
        public const byte Mez_SinavYerBilgisiGonderimJuriDoktora = 58;
        public const byte Mez_SinavYerBilgisiGonderimJuriYL = 59;
        public const byte Mez_SinavYerBilgisiOnaylanmadi = 67;
        public const byte Mez_EykTarihineGoreSrAlinmali = 10;
        public const byte Mez_EykTarihineGoreSrAlinmadi = 11;
        public const byte Mez_TezSinavSonucuSistemeGirilmedi = 12;
        public const byte Mez_CiltliTezTeslimYapilmali = 13;
        public const byte Mez_CiltliTezTeslimYapilmadi = 14;
        public const byte GelenIlkMesajOtoCvpMaili = 55;
        public const byte Mez_YayinSartiSaglandiDanisman = 60;
        public const byte Mez_YayinSartiSaglandiOgrenciYL = 61;
        public const byte Mez_YayinSartiSaglandiOgrenciDoktora = 62;
        public const byte Mez_YayinSartiSaglanamadiOgrenci = 64;
        public const byte Mez_SinavYerBilgisiGonderimOgrenciDoktora = 65;
        public const byte Mez_SinavYerBilgisiGonderimOgrenciYL = 66;
        public const byte Mez_SinavSonucuBasariliBilgisiGonderimDoktora = 68;
        public const byte Mez_SinavSonucuBasariliBilgisiGonderimYL = 69;
        public const byte Mez_SinavSonucuUzatmaBilgisiGonderim = 70;
        public const byte Mez_TezKontrolTezDosyasiYuklenmeli = 72;
        public const byte Mez_TezKontrolTezDosyasiYuklendi = 73;
        public const byte Mez_TezKontrolTezDosyasiBasarili = 74;
        public const byte Mez_TezKontrolTezDosyasiOnaylanmadi = 75;

        public const byte Mez_SinavDegerlendirmeHatirlantmaDanismanDR = 140;
        public const byte Mez_SinavDegerlendirmeHatirlantmaDanismanYL = 141;
        public const byte Mez_SinavDegerlendirmeDavetGonderimJuriDR = 142;
        public const byte Mez_SinavDegerlendirmeDavetGonderimJuriYL = 143;
        public const byte Mez_SinavSonucBilgiGonderimDanismanDR = 144;
        public const byte Mez_SinavSonucBilgiGonderimDanismanYL = 145;
        public const byte Mez_SinavSonucBilgiGonderimOgrenciDR = 146;
        public const byte Mez_SinavSonucBilgiGonderimOgrenciYL = 147;
        public const byte Mez_DanismanOnayladiOgrenci = 148;
        public const byte Mez_DanismanOnaylamadiOgrenci = 149;
        public const byte Mez_BasvuruYapildiDanisman = 150;
        public const byte Mez_BasvuruYapildiOgrenci = 151;


        public const byte TI_AraRaporBaslatildiOgrenci = 100;
        public const byte TI_AraRaporBaslatildiDanisman = 101;
        public const byte TI_ToplantiBilgiOgrenci = 102;
        public const byte TI_ToplantiBilgiKomite = 103;
        public const byte TI_DegerlendirmeLinkGonderimKomite = 104;
        public const byte TI_DegerlendirmeSonucGonderimDanisman = 105;
        public const byte TI_DegerlendirmeSonucGonderimOgrenci = 106;


        public const byte TDO_DanismanOnerisiYapildiOgrenci = 120;
        public const byte TDO_DanismanOnerisiYapildiDanisman = 121;
        public const byte TDO_DanismanOnerisiOnaylandiOgrenci = 122;
        public const byte TDO_DanismanOnerisiOnaylandiDanisman = 123;
        public const byte TDO_DanismanOnerisiReddedildiOgrenci = 124;
        public const byte TDO_DanismanOnerisiEYKDaOnaylandiOgrenci = 125;
        public const byte TDO_DanismanOnerisiEYKDaOnaylandiDanisman = 126;
        public const byte TDO_DanismanOnerisiEYKDaReddedildiOgrenciDanisman = 127;

        public const byte TDO_EsDanismanOnerisiYapildiOgrenci = 128;
        public const byte TDO_EsDanismanOnerisiYapildiDanisman = 129;
        public const byte TDO_EsDanismanOnerisiEYKDaOnaylandiOgrenci = 130;
        public const byte TDO_EsDanismanOnerisiEYKDaOnaylandiDanisman = 131;
        public const byte TDO_EsDanismanOnerisiEYKDaReddedildiOgrenciDanisman = 132;
        public const byte TDO_EsDanismanOnerisiEYKDaOnaylandiEsDanisman = 133;
    }
}