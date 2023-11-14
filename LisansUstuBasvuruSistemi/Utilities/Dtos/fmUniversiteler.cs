using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmUniversiteler : PagerModel
    {
        public int? UniversiteID { get; set; }
        public string KisaAd { get; set; }
        public string Ad { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<Universiteler> data { get; set; }
    }
}