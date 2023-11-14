using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmUnvanlar : PagerModel
    {
        public int? UnvanSiraNo { get; set; }
        public string UnvanAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<Unvanlar> data { get; set; }

    }
}