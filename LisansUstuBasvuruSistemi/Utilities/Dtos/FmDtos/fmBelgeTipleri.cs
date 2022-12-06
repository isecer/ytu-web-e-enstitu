using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos
{
    public class fmBelgeTipleri : PagerOption
    {
        public string BelgeTipAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frBelgeTipleri> data { get; set; }

    }
    public class frBelgeTipleri : BelgeTipleri
    {
        public string IslemYapan { get; set; }

    }
}