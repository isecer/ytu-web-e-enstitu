using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmAlesTipleri : PagerOption
    {
        public string AlesTipAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frAlesTipleri> data { get; set; }

    }
    public class frAlesTipleri : AlesTipleri
    {
        public string IslemYapan { get; set; }

    }
}