using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public static class TijBasvuruDurum
    {
        public static int DanismanOnayiBekliyor = 1;
        public static int DanismanTarafindanOnaylandi = 2;
        public static int DanismanTarafindanOnaylanmadi = 3;
        public static int EykYaGonderimOnayiBekleniyor = 4;
        public static int EykYaGonderimiOnaylandi = 5;
        public static int EykYaGonderimiOnaylanmadi = 6;
        public static int EykDaOnayBekleniyor = 7;
        public static int EykDaOnaylandi = 8;
        public static int EykDaOnaylanmadi = 9;

    }
    public static class TijDegisiklikTipi
    {
        public static int YtuIciDegisiklik = 1;
        public static int YtuDisiDegisiklik = 2;
        public static int YtuIciVeDisiDegisiklik = 3;

    }
    public static class TijFormTipi
    {
        public static int YeniForm = 1;
        public static int DanismanDegisikligi = 2;
        public static int TezKonusuDegisikligi = 3;
        public static int DanismanVeTezKonusuDegisikligi = 4;
        public static int Diger = 5;
        public static int TumDegisiklikler = 1000;

    }
}