using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class raporOtipModel
    {
        public string OgrenimTipAdi { get; set; }
        public double GBNO { get; set; }
        public int TaslakCount { get; set; }
        public int OnaylananCount { get; set; }
        public int IptalEdilenCount { get; set; }
        public int KayitCount { get; set; }
    }
}