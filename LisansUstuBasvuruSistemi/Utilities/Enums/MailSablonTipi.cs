using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public static class MailSablonTipi
    {
        public static int Normal = 1;
        public static int OtoMailAsil = 2;
        public static int OtoMailYedek = 3;
        public static int OtoMailKazanamadi = 4;
        public static int OtoMailKayitOldu = 5;
        public static int OtoMailOnKayitOldu = 71;
        public static int OtoMailOnKayitOldu1 = 107;
        public static int OtoMailSinavYerBilgi = 6;
        public static int OtoMailAnketBilgi = 7;
        public static int LisansustuSanalPosOdemeBilgisi = 8;
        public static int Mez_EykTarihiGirildiOgrenciYL = 9;
        public static int Mez_EykTarihiGirildiOgrenciDoktora = 63;
        public static int Mez_EykTarihiGirildiDanismanDoktora = 53;
        public static int Mez_EykTarihiGirildiJuriAsilDoktora = 54;
        public static int Mez_EykTarihiGirildiDanismanYL = 56;
        public static int Mez_EykTarihiGirildiJuriAsilYL = 57;
        public static int Mez_SinavYerBilgisiGonderimJuriDoktora = 58;
        public static int Mez_SinavYerBilgisiGonderimJuriYL = 59;
        public static int Mez_SinavYerBilgisiOnaylanmadi = 67;
        public static int Mez_EykTarihineGoreSrAlinmali = 10;
        public static int Mez_EykTarihineGoreSrAlinmadi = 11;
        public static int Mez_TezSinavSonucuSistemeGirilmedi = 12;
        public static int Mez_CiltliTezTeslimYapilmali = 13;
        public static int Mez_CiltliTezTeslimYapilmadi = 14;
        public static int GelenIlkMesajOtoCvpMaili = 55;
        public static int Mez_YayinSartiSaglandiDanisman = 60;
        public static int Mez_YayinSartiSaglandiOgrenciYL = 61;
        public static int Mez_YayinSartiSaglandiOgrenciDoktora = 62;
        public static int Mez_YayinSartiSaglanamadiOgrenci = 64;
        public static int Mez_SinavYerBilgisiGonderimOgrenciDoktora = 65;
        public static int Mez_SinavYerBilgisiGonderimOgrenciYL = 66;
        public static int Mez_SinavSonucuBasariliBilgisiGonderimDoktora = 68;
        public static int Mez_SinavSonucuBasariliBilgisiGonderimYL = 69;
        public static int Mez_SinavSonucuUzatmaBilgisiGonderim = 70;
        public static int Mez_TezKontrolTezDosyasiYuklenmeli = 72;
        public static int Mez_TezKontrolTezDosyasiYuklendi = 73;
        public static int Mez_TezKontrolTezDosyasiBasarili = 74;
        public static int Mez_TezKontrolTezDosyasiOnaylanmadi = 75;

        public static int Mez_SinavDegerlendirmeHatirlantmaDanismanDR = 140;
        public static int Mez_SinavDegerlendirmeHatirlantmaDanismanYL = 141;
        public static int Mez_SinavDegerlendirmeDavetGonderimJuriDR = 142;
        public static int Mez_SinavDegerlendirmeDavetGonderimJuriYL = 143;
        public static int Mez_SinavSonucBilgiGonderimDanismanDR = 144;
        public static int Mez_SinavSonucBilgiGonderimDanismanYL = 145;
        public static int Mez_SinavSonucBilgiGonderimOgrenciDR = 146;
        public static int Mez_SinavSonucBilgiGonderimOgrenciYL = 147;
        public static int Mez_DanismanOnayladiOgrenci = 148;
        public static int Mez_DanismanOnaylamadiOgrenci = 149;
        public static int Mez_BasvuruYapildiDanisman = 150;
        public static int Mez_BasvuruYapildiOgrenci = 151;


        public static int TI_AraRaporBaslatildiOgrenci = 100;
        public static int TI_AraRaporBaslatildiDanisman = 101;
        public static int TI_ToplantiBilgiOgrenci = 102;
        public static int TI_ToplantiBilgiKomite = 103;
        public static int TI_DegerlendirmeLinkGonderimKomite = 104;
        public static int TI_DegerlendirmeSonucGonderimDanisman = 105;
        public static int TI_DegerlendirmeSonucGonderimOgrenci = 106;


        public static int TDO_DanismanOnerisiYapildiOgrenci = 120;
        public static int TDO_DanismanOnerisiYapildiDanisman = 121;
        public static int TDO_DanismanOnerisiOnaylandiOgrenci = 122;
        public static int TDO_DanismanOnerisiOnaylandiDanisman = 123;
        public static int TDO_DanismanOnerisiReddedildiOgrenci = 124;
        public static int TDO_DanismanOnerisiEYKDaOnaylandiOgrenci = 125;
        public static int TDO_DanismanOnerisiEYKDaOnaylandiDanisman = 126;
        public static int TDO_DanismanOnerisiEYKDaReddedildiOgrenciDanisman = 127;

        public static int TDO_TezBasligiDegisikligiDanisman = 200;
        public static int TDO_TezBasligiDegisikligiOgrenci = 201;
        public static int TDO_TezBasligiDegisikligiOnaylandiDanisman = 202;
        public static int TDO_TezBasligiDegisikligiOnaylandiOgrenci = 203;
        public static int TDO_TezBasligiDegisikligiRetEdildiDanisman = 204;
        public static int TDO_TezBasligiDegisikligiRetEdildiOgrenci = 205;
        public static int TDO_TezBasligiDegisikligiEYKDaOnaylandiDanisman = 206;
        public static int TDO_TezBasligiDegisikligiEYKDaOnaylandiOgrenci = 207;
        public static int TDO_TezBasligiDegisikligiEYKDaRetEdildiDanisman = 208;
        public static int TDO_TezBasligiDegisikligiEYKDaRetEdildiOgrenci = 209;

        public static int TDO_TezDanismanDegisikligiVarolanDanisman = 210;
        public static int TDO_TezDanismanDegisikligiYeniDanisman = 211;
        public static int TDO_TezDanismanDegisikligiOgrenci = 212;
        public static int TDO_TezDanismanDegisikligiOnaylandiYeniDanisman = 213;
        public static int TDO_TezDanismanDegisikligiOnaylandiOgrenci = 214;
        public static int TDO_TezDanismanDegisikligiRetEdildiYeniDanisman = 215;
        public static int TDO_TezDanismanDegisikligiRetEdildiOgrenci = 216;
        public static int TDO_TezDanismanDegisikligiEYKDaOnaylandiYeniDanisman = 217;
        public static int TDO_TezDanismanDegisikligiEYKDaOnaylandiOgrenci = 218;
        public static int TDO_TezDanismanDegisikligiEYKDaRetEdildiYeniDanisman = 219;
        public static int TDO_TezDanismanDegisikligiEYKDaRetEdildiOgrenci = 220;

        public static int TDO_TezDanismanVeBaslikDegisikligiVarolanDanisman = 221;
        public static int TDO_TezDanismanVeBaslikDegisikligiYeniDanisman = 222;
        public static int TDO_TezDanismanVeBaslikDegisikligiOgrenci = 223;
        public static int TDO_TezDanismanVeBaslikDegisikligiOnaylandiYeniDanisman = 224;
        public static int TDO_TezDanismanVeBaslikDegisikligiOnaylandiOgrenci = 225;
        public static int TDO_TezDanismanVeBaslikDegisikligiRetEdildiYeniDanisman = 226;
        public static int TDO_TezDanismanVeBaslikDegisikligiRetEdildiOgrenci = 227;
        public static int TDO_TezDanismanVeBaslikDegisikligiEYKDaOnaylandiYeniDanisman = 228;
        public static int TDO_TezDanismanVeBaslikDegisikligiEYKDaOnaylandiOgrenci = 229;
        public static int TDO_TezDanismanVeBaslikDegisikligiEYKDaRetEdildiYeniDanisman = 230;
        public static int TDO_TezDanismanVeBaslikDegisikligiEYKDaRetEdildiOgrenci = 231;


        public static int TDO_EsDanismanOnerisiYapildiOgrenci = 128;
        public static int TDO_EsDanismanOnerisiYapildiDanisman = 129;
        public static int TDO_EsDanismanOnerisiEYKDaOnaylandiOgrenci = 130;
        public static int TDO_EsDanismanOnerisiEYKDaOnaylandiDanisman = 131;
        public static int TDO_EsDanismanOnerisiEYKDaReddedildiOgrenciDanisman = 132;
        public static int TDO_EsDanismanOnerisiEYKDaOnaylandiEsDanisman = 133;

        public static int TDO_EsDanismanDegisikligiYapildiOgrenci = 134;
        public static int TDO_EsDanismanDegisikligiYapildiDanisman = 135;
        public static int TDO_EsDanismanDegisikligiEYKDaOnaylandiOgrenci = 136;
        public static int TDO_EsDanismanDegisikligiEYKDaOnaylandiDanisman = 137;
        public static int TDO_EsDanismanDegisikligiEYKDaRetEdildiOgrenciDanisman = 138;
        public static int TDO_EsDanismanDegisikligiEYKDaOnaylandiEsDanisman = 139;

        public static int Yeterlik_BasvuruOnaylandiOgrenciye = 250;
        public static int Yeterlik_BasvuruOnaylandiDanismana = 251;
        public static int Yeterlik_BasvuruRetEdildiOgrenciye = 252;
        public static int Yeterlik_JuriUyeleriTanimlandiKomiteyeLink = 253;
        public static int Yeterlik_KomiteDegerlendimreyiTamamladiDanismana = 254;

        public static int Yeterlik_YaziliSinavTalebiYapildiDanismana = 255;
        public static int Yeterlik_YaziliSinavTalebiYapildiOgrenciye = 256;
        public static int Yeterlik_YaziliSinavTalebiYapildiJurilere = 257;

        public static int Yeterlik_YaziliSinavBasariliGirisiYapidliDanismana = 258;
        public static int Yeterlik_YaziliSinavBasariliGirisiYapidliOgrenciye = 259;
        public static int Yeterlik_YaziliSinavBasariliGirisiYapidliJurilere = 260;

        public static int Yeterlik_YaziliSinavBasarisizOnayYapildiDanismana = 261;
        public static int Yeterlik_YaziliSinavBasarisizOnayYapildiOgrenciye = 262;
        public static int Yeterlik_YaziliSinavBasarisizGirisiYapildiJurilereLink = 263;

        public static int Yeterlik_YaziliSinavKatilmadiGirisiYapildiDanismana = 264;
        public static int Yeterlik_YaziliSinavKatilmadiGirisiYapildiOgrenciye = 265;
        public static int Yeterlik_YaziliSinavKatilmadiGirisiYapildiJurilereLink = 266;

        public static int Yeterlik_SozluSinavTalebiYapildiDanismana = 277;
        public static int Yeterlik_SozluSinavTalebiYapildiOgrenciye = 278;
        public static int Yeterlik_SozluSinavTalebiYapildiJurilere = 279;
        public static int Yeterlik_SozluNotGirisJurilereLink = 280;

        public static int Yeterlik_GenelSinavSonucuBasariliDanismana = 281;
        public static int Yeterlik_GenelSinavSonucuBasariliOgrenciye = 282;
        public static int Yeterlik_GenelSinavSonucuBasariliJurilere = 283;

        public static int Yeterlik_GenelSinavSonucuBasarisizDanismana = 284;
        public static int Yeterlik_GenelSinavSonucuBasarisizOgrenciye = 285;
        public static int Yeterlik_GenelSinavSonucuBasarisizJurilere = 286;

        public static int Yeterlik_SozluSinavKatilmadiGirisiYapildiDanismana = 287;
        public static int Yeterlik_SozluSinavKatilmadiGirisiYapildiOgrenciye = 288;
        public static int Yeterlik_SozluSinavKatilmadiGirisiYapildiJurilereLink = 289; 

         
    }
}