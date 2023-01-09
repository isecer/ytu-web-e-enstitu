using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Models
{

    public static class SayacTipi
    {
        public static string SANALPOS_OID = "SANALPOS_OID";
    }
    public static class TabAdlari
    {
        public static string SinavTipTN = "SnvTpTab";
        public static string MezuniyetSureciTN = "MezSrcTab";
        public static string BasvuruSureciTN = "BasSrcTab";
        public static string MezuniyetBasvurulariTN = "MezBDetTab";
        public static string MulakatDetayTN = "MulBDetTab";
        public static string BasvuruSonucDetayTN = "BSncDetTab";
        public static string BasvuruDetayTN = "BsvrDetTab";
        public static string TDOFormTN = "TDODetTab";
    }
    public static class RenkTiplier
    {
        public static string Primary = "#33414e";
        public static string Info = "#3fbae4";
        public static string Warning = "#fea223";
        public static string Success = "#95b75d";
        public static string Danger = "#b64645";
    }
    public static class HttpDurumKod
    {
        public const int Continue = 100;
        public const int SwitchingProtocols = 101;
        public const int Processing = 102;
        public const int OK = 200;
        public const int Created = 201;
        public const int Accepted = 202;
        public const int NonAuthoritativeInformation = 203;
        public const int NoContent = 204;
        public const int ResetContent = 205;
        public const int PartialContent = 206;
        public const int MultiStatus = 207;
        public const int ContentDifferent = 210;
        public const int MultipleChoices = 300;
        public const int MovedPermanently = 301;
        public const int MovedTemporarily = 302;
        public const int SeeOther = 303;
        public const int NotModified = 304;
        public const int UseProxy = 305;
        public const int TemporaryRedirect = 307;
        public const int BadRequest = 400;
        public const int Unauthorized = 401;
        public const int PaymentRequired = 402;
        public const int Forbidden = 403;
        public const int NotFound = 404;
        public const int NotAccessMethod = 405;
        public const int NotAcceptable = 406;
        public const int UnLoginToProxyServer = 407;
        public const int RequestTimeOut = 408;
        public const int Conflict = 409;
        public const int Gone = 410;
        public const int LengthRequired = 411;
        public const int PreconditionAiled = 412;
        public const int RequestEntityTooLarge = 413;
        public const int RequestURITooLong = 414;
        public const int UnsupportedMediaType = 415;
        public const int RequestedrangeUnsatifiable = 416;
        public const int Expectationfailed = 417;
        public const int Unprocessableentity = 422;
        public const int Locked = 423;
        public const int Methodfailure = 424;
        public const int InternalServerError = 500;
        public const int Uygulanmamış = 501;
        public const int GeçersizAğGeçidi = 502;
        public const int HizmetYok = 503;
        public const int GatewayTimeout = 504;
        public const int HTTPVersionNotSupported = 505;
        public const int InsufficientStorage = 507;

    }
    public static class BasvuruSurecTipi
    {
        public const int LisansustuBasvuru = 1;
        public const int YatayGecisBasvuru = 2;
        public const int YTUYeniMezunDRBasvuru = 3; 

    }
    public static class BasvuruBelgeTipi
    {
        public static int KimlikBelgesi = 1;
        public static int LEgitimBelgesi = 2;
        public static int YLEgitimBelgesi = 3;
        public static int MezuniyetBelgesi = 4;
        public static int YLMezuniyetBelgesi = 11;
        public static int TranskriptBelgesi = 5; 
        public static int AlesGreSinaviBelgesi = 6;
        public static int DilSinaviBelgesi = 7;
        public static int TomerSinaviBelgesi = 8;
        public static int TaninirlikBelgesi = 9;
        public static int DenklikBelgesi = 10; 
    }
    public static class BelgeTipi
    {
        public static int Transkript = 1;
        public static int OgrenciBelgesi = 2;
        public static int OgrenimBelgesi = 3;
        public static int IlgiliMakama = 4;
        public static int Diger = 5;
    }
    public static class OsymSonucTip
    {
        public const int VeritabaniBaglantiHatasi = -1;
        public const int ParametrelereKarsilikGelenSinavYok = 0;
        public const int SinavSonucuMevcut = 1;
        public const int BuAdayaAitSinavSonucuYok = 2;
        public const int BuAdayaAitSinavSonucuMevcut = 3;

    }
    public static class OzelTarihTip
    {
        public const int BelirliTarihdenOncesi = 1;
        public const int BelirliTarhidenSonrasi = 2;
        public const int IkiTarihArasi = 3;
        public const int BelirliTarihler = 4;
        public const int MaksGecmisYil = 5;

    }
    public static class OzelNotTip
    {
        public const int SeciliNotlar = 1;
        public const int SeciliNotAraliklari = 2;

    }
    public static class KullaniciTipBilgi
    {
        public const int IdariPersonel = 1;
        public const int AkademikPersonel = 2;
        public const int YerliOgrenci = 3;
        public const int YabanciOgrenci = 4;

    }
    public static class BasvuruAgnoAlimTipi
    {
        public const int LisansAlinsin = 1;
        public const int YLisansAlinsin = 2;
        public const int L_YLYuzdeBelirlensin = 3;

    }

   
    public static class DonemBilgi
    {
        public const int GuzYariyili = 1;
        public const int BaharYariyili = 2;

    }
    public static class EnstituKodlari
    {
        public const string FenBilimleri = "010";
        public const string SosyalBilimleri = "020";

    }
  
   
    public static class MulakatSinavTur
    {
        public const int Yazili = 1;
        public const int Sozlu = 2;

    }
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


        public const byte TDO_TezKonuDilDegisikligiOgrenci = 152;
        public const byte TDO_TezKonuDilDegisikligiVarolanDanisman = 153;
        public const byte TDO_TezKonuDilDegisikligiVarolanDanismanOnayladiYeniDanisman = 154; 
        public const byte TDO_TezKonuDilDegisikligiVarolanDanismanRetEttiOgrenci = 155;
        public const byte TDO_TezKonuDilDegisikligiYeniDanismanOnayladiOgrenci = 156;
        public const byte TDO_TezKonuDilDegisikligiYeniDanismanRetEttiOgrenci = 157;
        public const byte TDO_TezKonuDilDegisikligiEYKDaOnaylandiOgrenci = 158;
        public const byte TDO_TezKonuDilDegisikligiEYKDaOnaylandiYeniDanisman = 159;
        public const byte TDO_TezKonuDilDegisikligiEYKDaOnaylandiOgrenciDanisman = 160; 

        public const byte TDO_EsDanismanOnerisiYapildiOgrenci = 128;
        public const byte TDO_EsDanismanOnerisiYapildiDanisman = 129; 
        public const byte TDO_EsDanismanOnerisiEYKDaOnaylandiOgrenci = 130;
        public const byte TDO_EsDanismanOnerisiEYKDaOnaylandiDanisman = 131;
        public const byte TDO_EsDanismanOnerisiEYKDaReddedildiOgrenciDanisman = 132;
        public const byte TDO_EsDanismanOnerisiEYKDaOnaylandiEsDanisman = 133;
    }
    public static class ModalSizeClass
    {

        public const string Small = "modal-dialog modal-sm";
        public const string Basic = "modal-dialog";
        public const string Large = "modal-dialog modal-lg";
    }
    public static class TarihKriterSecim
    {
        public const byte SecilenTarihVeOncesi = 1;
        public const byte SecilenTarihAraligi = 2;
        public const byte SecilenTarihVeSonrasi = 3;
    }
   
    public static class SRSalonDurum
    {
        public const int Boş = 1;
        public const int OnTalep = 2;
        public const int Dolu = 3;
        public const int Alındı = 4;
        public const int GecmisTarih = 5;
        public const int ResmiTatil = 6;
    }
    public static class SROzelTanimTip
    {
        public const int Rezervasyon = 1;
        public const int ResmiTatilSabit = 2;
        public const int ResmiTatilDegisen = 3;
        public const int Rezerve = 4;
    }

    public static class NotSistemi
    {
        public const byte Not1LikSistem = 1;
        public const byte Not4LükSistem = 4;
        public const byte Not5LikSistem = 5;
        public const byte Not20LikSistem = 20;
        public const byte Not100LükSistem = 100;
    }
    public static class BilgiTipi
    {
        public const byte Hata = 1;
        public const byte Uyarı = 2;
        public const byte Kritik = 3;
        public const byte OnemsizHata = 4;
        public const byte Saldırı = 5;
        public const byte LoginHatalari = 6;
        public const byte Bilgi = 7;
    }
    public static class SinavTipGrup
    {
        public const byte Ales_Gree = 1;
        public const byte DilSinavlari = 2;
        public const byte Tomer = 3;
    }
    public static class WsCekimTipi
    {
        public const byte Donemsel = 1;
        public const byte Aylik = 2;
        public const byte Tarih = 3;
    }
   
    public static class ZamanTipi
    {
        public const byte Yil = 1;
        public const byte Ay = 2;
        public const byte Gun = 3;
        public const byte Saat = 4;
        public const byte Dakika = 4;
    }
  
    public static class AlesTipBilgi
    {
        public const byte Sayısal = 1;
        public const byte Sözel = 2;
        public const byte EşitAğırlık = 3;
    }
    public static class RaporTipleri
    {
        public const byte Basvuru = 1;
        public const byte BasvuruSonucListesi = 2;
        public const byte BasvuruSonucListesiBolum = 3;
        public const byte BasvuruOgrenciListesi = 4;
        public const byte KesinKayitListesi = 5;
        public const byte AnabilimdaliProgramListesi = 6;
        public const byte BasvuruSonucSayisal = 7;
        public const byte BelgeTalepSayisal = 8;

        public const byte MezuniyetBasvuruRaporu = 9;
        public const byte Anketler = 10;
        public const byte MezuniyetCiltFormuRaporu = 11; 

        public const byte MezuniyetJuriOneriFormuRaporu = 13;

        public const byte MezuniyetTezJuriTutanakRaporu = 14;
        public const byte MezuniyetTutanakRaporu = 15;

        public const byte MezuniyetTezDuzeltmeVeJuriUyelerineTezTeslimTutanagi = 16;
        public const byte MezuniyetTezSinavSonucFormu = 17;
        public const byte MezuniyetJuriUyelerineTezTeslimFormu = 18;
        public const byte MezuniyetTezdenUretilenYayinlariDegerlendirmeFormu = 19;
        public const byte MezuniyetDoktoraTezDegerlendirmeFormu = 20;
        public const byte MezuniyetTezDegerlendirmeFormu = 21;
        public const byte MezuniyetTezKontrolFormu = 22;

        public const byte MezuniyetTezTeslimFormu = 23;



        public const byte TezIzlemeDegerlendirmeFormu = 30;
        public const byte TezDanismanOneriFormu = 35;
        public const byte TezDanismanDegisiklikFormu = 38;
        public const byte TezEsDanismanOneriFormu = 36; 
        public const byte TezEsDanismanOneriTutanakRaporu = 37;


    }
    public static class BasvuruDurumu
    {
        public const byte Taslak = 1;
        public const byte Onaylandı = 2; 
        public const byte IptalEdildi = 4;
        public const byte Gonderildi = 5;
         

    }
    public static class TIAraRaporDurumu
    {
        public const byte ToplantiBilgileriGirilmedi = 1;
        public const byte ToplantiBilgileriGirildi = 2;
        public const byte DegerlendirmeSureciBaslatildi = 3;
        public const byte DegerlendirmeSureciTamamlandi = 4;


    }
    public static class MezuniyetYayinKontrolDurumu
    {
        public const byte Taslak = 1;
        public const byte Onaylandi = 2;
        public const byte IptalEdildi = 4;
        public const byte KabulEdildi = 5;

    }
    public static class AlanTipi
    {
        public const byte AlanIci = 1;
        public const byte AlanDisi = 2;
        public const byte Ortak = 3;

    }
    public static class BelgeTalepDurum
    {
        public const byte TalepEdildi = 1;
        public const byte Hazirlaniyor = 2;
        public const byte Hazirlandi = 3;
        public const byte Verildi = 4;
        public const byte Kapatildi = 5;
        public const byte IptalEdildi = 6;

    }
    public static class BelgeTalepTip
    {
        public const byte Transkript = 1;
        public const byte ÖğrenciBelgesi = 2;
        public const byte ÖğrenimBelgesi = 3;
        public const byte İlgiliMakama = 4;
        public const byte Diğer = 5;

    }
    public static class TalepDurumu
    {
        public const byte TalepYapildi = 1;
        public const byte Onaylandi = 2;
        public const byte Rededildi = 3;

    }
    public static class TalepTipi
    {
        public const byte LisansustuSureUzatmaTalebi = 1;
        public const byte YDSSinavSonucuBelgesiYukleme = 2;
        public const byte KayitSildirmeBelgesiYukleme = 3;
        public const byte Covid19KayitDondurmaTalebi = 4;

    }
    public static class SRTalepDurum
    {
        public const byte TalepEdildi = 1;
        public const byte Onaylandı = 2;
        public const byte Reddedildi = 3;
        public const byte IptalEdildi = 4;

    }
    public static class MezuniyetSinavDurum
    {
        public const byte SonucGirilmedi = 1;
        public const byte Basarili = 2;
        public const byte Uzatma = 3;
        public const byte Basarisiz = 4;

    }
    public static class OgrenimTipi
    {
        public const byte TezliYuksekLisans = 1;
        public const byte TezsizYuksekLisans = 2;
        public const byte Doktra = 3;
        public const byte SanattaYeterlilik = 4;
        public const byte ButunlesikDoktora = 5;
        public const byte BilimselHazirlik = 6;

    }
    public static class OgrenimDurum
    {
        public const byte HalenOğrenci = 1;
        public const byte Mezun = 2;
        public const byte OzelOgrenci = 3;

    }
    public static class DuyuruPopupTipleri
    {
        public const byte AnaSayfa = 1;
        public const byte LisansustuBasvuru = 2;
        public const byte MezuniyetBasvuru = 3;
        public const byte TalepYap = 4;
        public const byte TIBasvuru = 5;
        public const byte TDOBasvuru = 6;

    }
    public static class KotaHesapTipleri
    {
        public const byte SeciliBasvuruSureci = 1;
        public const byte YilveDonemToplam = 2;


    }

    public static class MulakatSonucTipi
    {
        public const byte Hesaplanmadı = 0;
        public const byte Asil = 1;
        public const byte Yedek = 2;
        public const byte Kazanamadı = 3;

    }
    public static class KayitDurumu
    {
        public const byte OnKayit = 0;
        public const byte KayitOldu = 1;
        public const byte KayitOlmadi = 2; 

    }
    public static class TDODansimanDurumu
    {
        public const byte DanismanOnayiBekliyor = 1;
        public const byte DanismanTarafindanOnaylandi = 2;
        public const byte DanismanTarafindanOnaylanmadi = 3;
        public const byte EYKYaGonderimOnayiBekleniyor = 4;
        public const byte EYKYaGonderimiOnaylandi = 5;
        public const byte EYKYaGonderimiOnaylanmadi = 6;
        public const byte EYKDaOnayBekleniyor = 7;
        public const byte EYKDaOnaylandi = 8;
        public const byte EYKDaOnaylanmadi = 9;

    }
    public static class TDODanismanTalepTip
    {
        public const byte TezDanismaniOnerisi = 1;
        public const byte TezDanismaniVeBaslikDegisikligi = 2;
        public const byte TezBasligiDegisikligi = 3;
        public const byte TezDanismaniDegisikligi = 4;

    }

    public enum Msgtype
    {
        Success, Error, Warning, Information, Nothing
    }
}