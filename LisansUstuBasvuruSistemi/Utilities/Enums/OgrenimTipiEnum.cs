using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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


        public bool IsDrOrYl(int ogrenimTipKod)
        {
            return ogrenimTipKod == Doktra || ogrenimTipKod == ButunlesikDoktora;
        }

    }
}