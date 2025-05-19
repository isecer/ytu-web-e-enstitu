using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public class DonemProjesiDurumEnum
    {
        public const byte BasvuruTamamlanmadi = 0;
        public const byte EnstituOnaySureci = 1;
        public const byte YurutucuOnaySureci = 2;
        public const byte JuriSinavOlusturmaSureci = 3;
        public const byte SinavDegerlendirmeSureci = 4; 
        public const byte EnstituYonetimKuruluSureci = 5; 
    }

    public class DpBasvuruDurumEnum
    {
        public const byte BasvuruTamamlanmadi = 0;
        public const byte EnstituOnayiBekliyor = 1;
        public const byte EnstituTarafindanOnaylandi = 2;
        public const byte EnstituTarafindanOnaylanmadi = 3;
        public const byte YurutucuOnayiBekliyor = 4;
        public const byte YurutucuTarafindanOnaylandi = 5;
        public const byte YurutucuTarafindanOnaylanmadi = 6;
        public const byte JuriSinavOlusturmaSureci = 7;
        public const byte SinavDegerlendirmeSureci = 8; 
        public const byte EykYaGonderimOnayiBekleniyor = 9;
        public const byte EykYaGonderimiOnaylandi = 10;
        public const byte EykYaGonderimiOnaylanmadi = 11;
        public const byte EykYaHazirlandi = 12; 
        public const byte EykDaOnaylandi = 13;
        public const byte EykDaOnaylanmadi = 14;
        public const byte BasariliOlanlar = 15;
        public const byte BasarisizOlanlar = 16;

    }
    public class DonemProjesiEnstituOnayDurumEnum
    {
        public const byte KabulEdildi = 1;
        public const byte Reddedildi = 2;
        public const byte IptalEdildi = 3;
    }
    public class DonemProjesiJuriOnayDurumEnum
    {
        public const byte Basarili = 1;
        public const byte Basarisiz = 2;
        public const byte BasarisizKatilmadi = 3;
    }
}