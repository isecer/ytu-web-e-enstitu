using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public class RaporTipiEnum
    { 
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
        public const byte MezuniyetEykSavunmaJurisiAtanmistirYazisi = 24;
        public const byte MezuniyetDrSinavBilgilendirmeYazilari = 25;
        public const byte MezuniyetIkinciTezTeslimTaahhutOnayYazilari = 26;
        public const byte MezuniyetCiltliTezTeslikEkSureTalebiTutanak = 27;



        public const byte TezIzlemeDegerlendirmeFormu = 30;
        public const byte TezDanismanOneriFormu = 35;
        public const byte TezEsDanismanOneriFormu = 36;
        public const byte TezEsDanismanOneriTutanakRaporu = 37;
        public const byte TezDanismanDegisiklikFormu = 38;

        public const byte YeterlikDoktoraSinavSonucFormu = 50;

        public const byte YeterlikKomiteAtamaGereklilikFormu = 51;

        public const byte TezIzlemeJuriOneriFormu = 60;
        public const byte TezIzlemeJuriDegisiklikFormu = 61;
        public const byte TezIzlemeKomiteAtamaBilgilendirmeYazilari = 62;
        public const byte TezIzlemeKomiteAtamaToIkinciSavunmaBilgilendirmeYazilari = 63;


        public const byte TezOneriSavunmaFormu = 70; 
        public const byte TezOneriSavunmaAraRaporIstemiFormu = 71;

        public const byte DonemProjesiSinaviDegerlendirmeFormu = 80;

        public static byte KayitSilmeTalepFormu = 90;
    }
}