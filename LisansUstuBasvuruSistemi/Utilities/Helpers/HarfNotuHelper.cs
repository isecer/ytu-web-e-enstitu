using System.Collections.Generic;
using System.Linq;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class HarfNotuHelper
    {
        public static List<string> NotDegerleri = new List<string>
        {
            "F0",
            "FF",
            "DD",
            "DC",
            "CC",
            "CB",
            "BB",
            "BA",
            "AA"
        };
        public static string ToLastNot(this string not)
        {
            if (not.ToStrObj().Split(',').Length > 1)
            {
                not = not.ToStrObj().Split(',').Last();
            }
            return not;
        }
        public static bool IsHarfNotuBuyukEsit(string notKriteri, string ogrenciNotu)
        {
            var notKriteriIndex = NotDegerleri.IndexOf(notKriteri);
            var ogrenciNotuIndex = NotDegerleri.IndexOf(ogrenciNotu);
            var success = notKriteriIndex <= ogrenciNotuIndex;
            if (!success)
            {
                //eski öğrenciler için özel kontrol G notu geçerli
                success = ogrenciNotu == "G";
            }
            return success;
        }
    }
}