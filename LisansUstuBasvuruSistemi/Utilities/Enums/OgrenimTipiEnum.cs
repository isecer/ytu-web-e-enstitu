using System.Collections.Generic;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public class OgrenimTipi
    {
        public const int TezliYuksekLisans = 1;
        public const int TezsizYuksekLisans = 2;
        public const int Doktra = 3;
        public const int SanattaYeterlilik = 4;
        public const int ButunlesikDoktora = 5;
        public const int BilimselHazirlik = 6;

        public static List<int> DoktoraOgretimleri => new List<int> { OgrenimTipi.Doktra, OgrenimTipi.ButunlesikDoktora };

        public static bool IsDrOrYl(int ogrenimTipKod)
        {
            return DoktoraOgretimleri.Contains(ogrenimTipKod);
        }
    }
}