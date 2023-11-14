using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public class MailSablonTipiEnum
    {
        public const int Normal = 1;
        public const int OtoMailAsil = 2;
        public const int OtoMailYedek = 3;
        public const int OtoMailKazanamadi = 4;
        public const int OtoMailKayitOldu = 5;
        public const int OtoMailOnKayitOldu = 71;
        public const int OtoMailOnKayitOldu1 = 107;
        public const int OtoMailSinavYerBilgi = 6;
        public const int OtoMailAnketBilgi = 7;
        public const int LisansustuSanalPosOdemeBilgisi = 8;
        public const int MezEykTarihiGirildiOgrenciYl = 9;
        public const int MezEykTarihiGirildiOgrenciDoktora = 63;
        public const int MezEykTarihiGirildiDanismanDoktora = 53;
        public const int MezEykTarihiGirildiJuriAsilDoktora = 54;
        public const int MezEykTarihiGirildiDanismanYl = 56;
        public const int MezEykTarihiGirildiJuriAsilYl = 57;
        public const int MezSinavYerBilgisiGonderimJuriDoktora = 58;
        public const int MezSinavYerBilgisiGonderimJuriYl = 59;
        public const int MezSinavYerBilgisiOnaylanmadi = 67;
        public const int MezEykTarihineGoreSrAlinmali = 10;
        public const int MezEykTarihineGoreSrAlinmadi = 11;
        public const int MezTezSinavSonucuSistemeGirilmedi = 12;
        public const int MezCiltliTezTeslimYapilmali = 13;
        public const int MezCiltliTezTeslimYapilmadi = 14;
        public const int GelenIlkMesajOtoCvpMaili = 55;
        public const int MezYayinSartiSaglandiDanisman = 60;
        public const int MezYayinSartiSaglandiOgrenciYl = 61;
        public const int MezYayinSartiSaglandiOgrenciDoktora = 62;
        public const int MezYayinSartiSaglanamadiOgrenci = 64;
        public const int MezSinavYerBilgisiGonderimOgrenciDoktora = 65;
        public const int MezSinavYerBilgisiGonderimOgrenciYl = 66;
        public const int MezSinavSonucuBasariliBilgisiGonderimDoktora = 68;
        public const int MezSinavSonucuBasariliBilgisiGonderimYl = 69;
        public const int MezSinavSonucuUzatmaBilgisiGonderim = 70;
        public const int MezTezKontrolTezDosyasiYuklenmeli = 72;
        public const int MezTezKontrolTezDosyasiYuklendi = 73;
        public const int MezTezKontrolTezDosyasiBasarili = 74;
        public const int MezTezKontrolTezDosyasiOnaylanmadi = 75;

        public const int MezSinavDegerlendirmeHatirlantmaDanismanDr = 140;
        public const int MezSinavDegerlendirmeHatirlantmaDanismanYl = 141;
        public const int MezSinavDegerlendirmeDavetGonderimJuriDr = 142;
        public const int MezSinavDegerlendirmeDavetGonderimJuriYl = 143;
        public const int MezSinavSonucBilgiGonderimDanismanDr = 144;
        public const int MezSinavSonucBilgiGonderimDanismanYl = 145;
        public const int MezSinavSonucBilgiGonderimOgrenciDr = 146;
        public const int MezSinavSonucBilgiGonderimOgrenciYl = 147;
        public const int MezDanismanOnayladiOgrenci = 148;
        public const int MezDanismanOnaylamadiOgrenci = 149;
        public const int MezBasvuruYapildiDanisman = 150;
        public const int MezBasvuruYapildiOgrenci = 151;


        public const int TiAraRaporBaslatildiOgrenci = 100;
        public const int TiAraRaporBaslatildiDanisman = 101;
        public const int TiToplantiBilgiOgrenci = 102;
        public const int TiToplantiBilgiKomite = 103;
        public const int TiDegerlendirmeLinkGonderimKomite = 104;
        public const int TiDegerlendirmeSonucGonderimDanisman = 105;
        public const int TiDegerlendirmeSonucGonderimOgrenci = 106;


        public const int TdoDanismanOnerisiYapildiOgrenci = 120;
        public const int TdoDanismanOnerisiYapildiDanisman = 121;
        public const int TdoDanismanOnerisiOnaylandiOgrenci = 122;
        public const int TdoDanismanOnerisiOnaylandiDanisman = 123;
        public const int TdoDanismanOnerisiReddedildiOgrenci = 124;
        public const int TdoDanismanOnerisiEykDaOnaylandiOgrenci = 125;
        public const int TdoDanismanOnerisiEykDaOnaylandiDanisman = 126;
        public const int TdoDanismanOnerisiEykDaReddedildiOgrenciDanisman = 127;

        public const int TdoTezBasligiDegisikligiDanisman = 200;
        public const int TdoTezBasligiDegisikligiOgrenci = 201;
        public const int TdoTezBasligiDegisikligiOnaylandiDanisman = 202;
        public const int TdoTezBasligiDegisikligiOnaylandiOgrenci = 203;
        public const int TdoTezBasligiDegisikligiRetEdildiDanisman = 204;
        public const int TdoTezBasligiDegisikligiRetEdildiOgrenci = 205;
        public const int TdoTezBasligiDegisikligiEykDaOnaylandiDanisman = 206;
        public const int TdoTezBasligiDegisikligiEykDaOnaylandiOgrenci = 207;
        public const int TdoTezBasligiDegisikligiEykDaRetEdildiDanisman = 208;
        public const int TdoTezBasligiDegisikligiEykDaRetEdildiOgrenci = 209;

        public const int TdoTezDanismanDegisikligiVarolanDanisman = 210;
        public const int TdoTezDanismanDegisikligiYeniDanisman = 211;
        public const int TdoTezDanismanDegisikligiOgrenci = 212;
        public const int TdoTezDanismanDegisikligiOnaylandiYeniDanisman = 213;
        public const int TdoTezDanismanDegisikligiOnaylandiOgrenci = 214;
        public const int TdoTezDanismanDegisikligiRetEdildiYeniDanisman = 215;
        public const int TdoTezDanismanDegisikligiRetEdildiOgrenci = 216;
        public const int TdoTezDanismanDegisikligiEykDaOnaylandiYeniDanisman = 217;
        public const int TdoTezDanismanDegisikligiEykDaOnaylandiOgrenci = 218;
        public const int TdoTezDanismanDegisikligiEykDaRetEdildiYeniDanisman = 219;
        public const int TdoTezDanismanDegisikligiEykDaRetEdildiOgrenci = 220;

        public const int TdoTezDanismanVeBaslikDegisikligiVarolanDanisman = 221;
        public const int TdoTezDanismanVeBaslikDegisikligiYeniDanisman = 222;
        public const int TdoTezDanismanVeBaslikDegisikligiOgrenci = 223;
        public const int TdoTezDanismanVeBaslikDegisikligiOnaylandiYeniDanisman = 224;
        public const int TdoTezDanismanVeBaslikDegisikligiOnaylandiOgrenci = 225;
        public const int TdoTezDanismanVeBaslikDegisikligiRetEdildiYeniDanisman = 226;
        public const int TdoTezDanismanVeBaslikDegisikligiRetEdildiOgrenci = 227;
        public const int TdoTezDanismanVeBaslikDegisikligiEykDaOnaylandiYeniDanisman = 228;
        public const int TdoTezDanismanVeBaslikDegisikligiEykDaOnaylandiOgrenci = 229;
        public const int TdoTezDanismanVeBaslikDegisikligiEykDaRetEdildiYeniDanisman = 230;
        public const int TdoTezDanismanVeBaslikDegisikligiEykDaRetEdildiOgrenci = 231;

        public const int TdoEsDanismanOnerisiYapildiOgrenci = 128;
        public const int TdoEsDanismanOnerisiYapildiDanisman = 129;
        public const int TdoEsDanismanOnerisiEykDaOnaylandiOgrenci = 130;
        public const int TdoEsDanismanOnerisiEykDaOnaylandiDanisman = 131;
        public const int TdoEsDanismanOnerisiEykDaReddedildiOgrenciDanisman = 132;
        public const int TdoEsDanismanOnerisiEykDaOnaylandiEsDanisman = 133;

        public const int TdoEsDanismanDegisikligiYapildiOgrenci = 134;
        public const int TdoEsDanismanDegisikligiYapildiDanisman = 135;
        public const int TdoEsDanismanDegisikligiEykDaOnaylandiOgrenci = 136;
        public const int TdoEsDanismanDegisikligiEykDaOnaylandiDanisman = 137;
        public const int TdoEsDanismanDegisikligiEykDaRetEdildiOgrenciDanisman = 138;
        public const int TdoEsDanismanDegisikligiEykDaOnaylandiEsDanisman = 139;

        public const int YeterlikBasvuruOnaylandiOgrenciye = 250;
        public const int YeterlikBasvuruOnaylandiDanismana = 251;
        public const int YeterlikBasvuruRetEdildiOgrenciye = 252;
        public const int YeterlikJuriUyeleriTanimlandiKomiteyeLink = 253;
        public const int YeterlikKomiteDegerlendimreyiTamamladiDanismana = 254;

        public const int YeterlikYaziliSinavTalebiYapildiDanismana = 255;
        public const int YeterlikYaziliSinavTalebiYapildiOgrenciye = 256;
        public const int YeterlikYaziliSinavTalebiYapildiJurilere = 257;

        public const int YeterlikYaziliSinavBasariliGirisiYapidliDanismana = 258;
        public const int YeterlikYaziliSinavBasariliGirisiYapidliOgrenciye = 259;
        public const int YeterlikYaziliSinavBasariliGirisiYapidliJurilere = 260;

        public const int YeterlikYaziliSinavBasarisizOnayYapildiDanismana = 261;
        public const int YeterlikYaziliSinavBasarisizOnayYapildiOgrenciye = 262;
        public const int YeterlikYaziliSinavBasarisizGirisiYapildiJurilereLink = 263;

        public const int YeterlikYaziliSinavKatilmadiGirisiYapildiDanismana = 264;
        public const int YeterlikYaziliSinavKatilmadiGirisiYapildiOgrenciye = 265;
        public const int YeterlikYaziliSinavKatilmadiGirisiYapildiJurilereLink = 266;

        public const int YeterlikSozluSinavTalebiYapildiDanismana = 277;
        public const int YeterlikSozluSinavTalebiYapildiOgrenciye = 278;
        public const int YeterlikSozluSinavTalebiYapildiJurilere = 279;
        public const int YeterlikSozluNotGirisJurilereLink = 280;

        public const int YeterlikGenelSinavSonucuBasariliDanismana = 281;
        public const int YeterlikGenelSinavSonucuBasariliOgrenciye = 282;
        public const int YeterlikGenelSinavSonucuBasariliJurilere = 283;

        public const int YeterlikGenelSinavSonucuBasarisizDanismana = 284;
        public const int YeterlikGenelSinavSonucuBasarisizOgrenciye = 285;
        public const int YeterlikGenelSinavSonucuBasarisizJurilere = 286;

        public const int YeterlikSozluSinavKatilmadiGirisiYapildiDanismana = 287;
        public const int YeterlikSozluSinavKatilmadiGirisiYapildiOgrenciye = 288;
        public const int YeterlikSozluSinavKatilmadiGirisiYapildiJurilereLink = 289;

        public const int TijFormuOlusturulduDanismana = 300;
        public const int TijOneriFormuDanismanTarafindanOnaylandiDanismana = 301;
        public const int TijOneriFormuDanismanTarafindanRetEdildiDanismana = 302;
        public const int TijOneriFormuEykyaGonderimiRetEdildiDanismana = 303;
        public const int TijOneriFormuEykdaOnaylanmadiEdildiDanismana = 304;
        public const int TijOneriFormuEykdaOnaylandiDanismana = 305; 
        public const int TijOneriFormuEykdaOnaylandiOgrenciye = 310;
        public const int TijOneriFormuEykdaOnaylandiJuriUyelerine = 311;
        
        public const int TosBaslatildiOgrenci = 350;
        public const int TosBaslatildiDanisman = 351;
        public const int TosToplantiBilgiOgrenci = 352;
        public const int TosToplantiBilgiKomite = 353;
        public const int TosDegerlendirmeLinkGonderimKomite = 354;
        public const int TosDegerlendirmeSonucGonderimDanisman = 355;
        public const int TosDegerlendirmeSonucGonderimOgrenci = 356;

    }
}