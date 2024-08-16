using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public class BelgeTalepDurumEnum
    {
        public const int TalepEdildi = 1;
        public const int Hazirlaniyor = 2;
        public const int Hazirlandi = 3;
        public const int Verildi = 4;
        public const int Kapatildi = 5;
        public const int IptalEdildi = 6;

    }
    public class BelgeTalepTipiEnum
    {
        public const int Transkript = 1;
        public const int ÖğrenciBelgesi = 2;
        public const int ÖğrenimBelgesi = 3;
        public const int İlgiliMakama = 4;
        public const int Diğer = 5;

    }
}