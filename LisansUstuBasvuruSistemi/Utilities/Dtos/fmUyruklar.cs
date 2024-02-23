using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmUyruklar : PagerModel
    {
        public int? UyrukKod { get; set; }
        public string KisaAd { get; set; }
        public string Ad { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<Uyruklar> data { get; set; }
    }
}