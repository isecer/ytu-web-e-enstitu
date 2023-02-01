using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmBelgeTipleriDto : PagerOption
    {
        public string BelgeTipAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<FrBelgeTipleriDto> BelgeTipleriDtos { get; set; }

    }
    public class FrBelgeTipleriDto : BelgeTipleri
    {
        public string IslemYapan { get; set; }

    }
}