using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public class KayitSilmeDurumEnums
    {
        public const int HarcBirimiOnaySureci = 1;
        public const int KutuphaneBirimiOnaySureci = 2;
        public const int EnstituYonetimKuruluSureci = 3; 
    }

    public class KsFilterDurumEnums
    {
        public const int HarcBirimiOnayBekleniyor = 1;
        public const int HarcBirimiTarafindanOnaylandi = 2;
        public const int HarcBirimiTarafindanReddedildi = 3;
        public const int KutuphaneBirimiOnayBekleniyor = 4;
        public const int KutuphaneBirimiTarafindanOnaylandi = 5; 
        public const int KutuphaneBirimiTarafindanReddedildi = 6;
        public const int OnayMakaminaGonderimOnayiBekleniyor = 7;
        public const int OnayMakaminaGonderimiOnaylandi = 8;
        public const int OnayMakamınaGonderimiOnaylanmadi = 9;
        public const int OnayMakaminaHazirlandi = 10;
        public const int OnayMakamindaOnaylandi = 11;
        public const int OnayMakamindaOnaylanmadi = 12; 
    }

}