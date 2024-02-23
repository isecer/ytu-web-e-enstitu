using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmSehirler : PagerModel
    {
        public int? SehirKod { get; set; }
        public string Ad { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<Sehirler> data { get; set; }

    }
}