using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public class TijBasvuruDurumEnum
    {
        public const int DanismanOnayiBekliyor = 1;
        public const int DanismanTarafindanOnaylandi = 2;
        public const int DanismanTarafindanOnaylanmadi = 3;
        public const int EykYaGonderimOnayiBekleniyor = 4;
        public const int EykYaGonderimiOnaylandi = 5;
        public const int EykYaGonderimiOnaylanmadi = 6;
        public const int EykDaOnayBekleniyor = 7;
        public const int EykDaOnaylandi = 8;
        public const int EykDaOnaylanmadi = 9;

    }
    public class TijDegisiklikTipiEnum
    {
        public const int YtuIciDegisiklik = 1;
        public const int YtuDisiDegisiklik = 2;
        public const int YtuIciVeDisiDegisiklik = 3; 
    }
    public class TijFormTipiEnum
    {
        public const int YeniForm = 1;
        public const int DanismanDegisikligi = 2;
        public const int TezKonusuDegisikligi = 3;
        public const int DanismanVeTezKonusuDegisikligi = 4;
        public const int Diger = 5;
        public const int TumDegisiklikler = 1000;

    }
}