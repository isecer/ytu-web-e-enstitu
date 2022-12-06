using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos
{
    public class fmBirimler : PagerOption
    {
        public string BirimKod { get; set; }
        public string BirimAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<Birimler> data { get; set; }

    }
}