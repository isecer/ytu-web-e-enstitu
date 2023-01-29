using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmEnstituler : PagerOption
    {
        public string EnstituKod { get; set; }
        public string EnstituAd { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frEnstituler> data { get; set; }

    }
    public class frEnstituler : Enstituler
    {
        public string IslemYapan { get; set; }
    }
}