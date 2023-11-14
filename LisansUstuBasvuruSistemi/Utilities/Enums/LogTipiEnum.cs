using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public class LogTipiEnum
    {
        public const byte Hata = 1;
        public const byte Uyarı = 2;
        public const byte Kritik = 3;
        public const byte OnemsizHata = 4;
        public const byte Saldırı = 5;
        public const byte LoginHatalari = 6;
        public const byte Bilgi = 7;
    }
}