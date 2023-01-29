using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmMsonucOranModel
    {
        public bool MulakatSurecineGirecek { get; set; }
        public int AlanTipID { get; set; }
        public int Toplam { get; set; }
        public double ToplamYuzde { get; set; }
        public int Kota { get; set; }
        public double KotaYuzde { get; set; }
        public int KayitCount { get; set; }
        public double KayitYuzde { get; set; }
        public int AsilCount { get; set; }
        public double AsilYuzde { get; set; }
        public int YedekCount { get; set; }
        public double YedekYuzde { get; set; }
        public int KazanamayanCount { get; set; }
        public double KazanamayanYuzde { get; set; }
    }
}