using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public class YeterlikBasvuruFilterEnum
    {
        public const int IslemGormeyenler = 0;
        public const int Onaylananlar = 1;
        public const int IptalEdilenler = 2;
        public const int JuriOlusturulmayanlar = 3;
        public const int KomiteOnayiBekleyenler = 4;
        public const int KomiteOnayiTamamlananlar = 5;
        public const int SinavSureciniBaslatilmayanlar = 6;
        public const int SinavSurecindeOlanlar = 7;
        public const int BasariliOlanlar = 8;
        public const int BasarisizOlanlar = 9;

    }
}