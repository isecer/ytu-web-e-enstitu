using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmOgrenimTipleri : PagerOption
    {
        public string EnstituKod { get; set; }
        public int? OgrenimTipKod { get; set; }
        public string OgrenimTipAd { get; set; }
        public string GrupAd { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<FrOgrenimTipleri> data { get; set; }
    }
    public class FrOgrenimTipleri : OgrenimTipleri
    {
        public string EnstituAd { get; set; }
        public string IslemYapan { get; set; } 

    }
}