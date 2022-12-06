using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public static class TDODansimanDurumu
    {
        public const byte DanismanOnayiBekliyor = 1;
        public const byte DanismanTarafindanOnaylandi = 2;
        public const byte DanismanTarafindanOnaylanmadi = 3;
        public const byte EYKYaGonderimOnayiBekleniyor = 4;
        public const byte EYKYaGonderimiOnaylandi = 5;
        public const byte EYKYaGonderimiOnaylanmadi = 6;
        public const byte EYKDaOnayBekleniyor = 7;
        public const byte EYKDaOnaylandi = 8;
        public const byte EYKDaOnaylanmadi = 9;

    }
}