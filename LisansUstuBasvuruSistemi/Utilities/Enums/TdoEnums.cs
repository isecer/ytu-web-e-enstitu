using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public class TdoDanismanTalepTipEnum
    {
        public const int TezDanismaniOnerisi = 1;
        public const int TezDanismaniVeBaslikDegisikligi = 2;
        public const int TezBasligiDegisikligi = 3;
        public const int TezDanismaniDegisikligi = 4;
    }
    public class TdoDansimanDurumuEnum
    {
        public const int DanismanOnayiBekliyor = 1;
        public const int DanismanTarafindanOnaylandi = 2;
        public const int DanismanTarafindanOnaylanmadi = 3;
        public const int EykYaGonderimOnayiBekleniyor = 4;
        public const int EykYaGonderimiOnaylandi = 5;
        public const int EykYaGonderimiOnaylanmadi = 6;
        public const int EykYaHazirlanmaBekleniyor = 10;
        public const int EykYaHazirlandi = 11;
        public const int EykDaOnayBekleniyor = 7;
        public const int EykDaOnaylandi = 8;
        public const int EykDaOnaylanmadi = 9;

    }
}