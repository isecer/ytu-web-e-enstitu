using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public class SrTalepDurumEnum
    {
        public const int TalepEdildi = 1;
        public const int Onaylandı = 2;
        public const int Reddedildi = 3;
        public const int IptalEdildi = 4;

    }
    public static class SrOzelTanimTipiEnum
    { 
        public const int ResmiTatilSabit = 2;
        public const int ResmiTatilDegisen = 3; 
    }
    public static class SrSalonDurumEnum
    {
        public const int Boş = 1;
        public const int OnTalep = 2;
        public const int Dolu = 3;
        public const int Alındı = 4;
        public const int GecmisTarih = 5;
        public const int ResmiTatil = 6;
    }
}