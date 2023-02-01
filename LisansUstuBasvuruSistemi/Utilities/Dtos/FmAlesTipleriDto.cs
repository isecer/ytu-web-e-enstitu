using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmAlesTipleriDto : PagerOption
    {
        public string AlesTipAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<FrAlesTipleri> data { get; set; }

    }
    public class FrAlesTipleri : AlesTipleri
    {
        public string IslemYapan { get; set; }

    }
}