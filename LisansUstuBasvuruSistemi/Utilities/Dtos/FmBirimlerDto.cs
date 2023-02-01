using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmBirimlerDto : PagerOption
    {
        public string BirimKod { get; set; }
        public string BirimAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<Birimler> Birimlers { get; set; }

    }
}