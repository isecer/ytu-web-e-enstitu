using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public class MezuniyetYayinKontrolDurumuEnum
    {
        public const int Taslak = 1;
        public const int Onaylandi = 2;
        public const int IptalEdildi = 4;
        public const int KabulEdildi = 5;
        public const int KaydiSilindi = 6;
    }
    public class MezuniyetJofDurumuEnum
    {
        public const int FormOlusturulmadi = 1;
        public const int FormOlusturuldu = 2;  
        public const int EykYaGonderimiOnaylandi = 4;
        public const int EykYaGonderimiOnaylanmadi = 5; 
        public const int EykYaHazirlandi = 7; 
        public const int EykDaOnaylandi = 9;
        public const int EykDaOnaylanmadi = 10;

    }
    public class MezuniyetSinavDurumEnum
    {
        public const int SonucGirilmedi = 1;
        public const int Basarili = 2;
        public const int Uzatma = 3;
        public const int Basarisiz = 4;

    }
}