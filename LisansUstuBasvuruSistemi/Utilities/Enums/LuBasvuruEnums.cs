using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public class AlanTipiEnum
    {
        public const int AlanIci = 1;
        public const int AlanDisi = 2;
        public const int Ortak = 3; 
    }
    public class BasvuruBelgeTipiEnum
    {
        public const int KimlikBelgesi = 1;
        public const int LEgitimBelgesi = 2;
        public const int YLEgitimBelgesi = 3;
        public const int MezuniyetBelgesi = 4;
        public const int YLMezuniyetBelgesi = 11;
        public const int TranskriptBelgesi = 5;
        public const int AlesGreSinaviBelgesi = 6;
        public const int DilSinaviBelgesi = 7;
        public const int TomerSinaviBelgesi = 8;
        public const int TaninirlikBelgesi = 9;
        public const int DenklikBelgesi = 10;
    }
    public class BasvuruDurumuEnum
    {
        public const int Taslak = 1;
        public const int Onaylandı = 2;
        public const int IptalEdildi = 4;
        public const int Gonderildi = 5;


    }
    public static class BasvuruSurecTipiEnum
    {
        public const int LisansustuBasvuru = 1;
        public const int YatayGecisBasvuru = 2;
        public const int YTUYeniMezunDRBasvuru = 3;

    }
}