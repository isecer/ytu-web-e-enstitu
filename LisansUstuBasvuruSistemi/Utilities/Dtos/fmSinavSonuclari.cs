using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmSinavSonuclari : PagerOption
    {
        public string AdSoyad { get; set; }
        public int? SinavTipKod { get; set; }
        public int? SinavDilID { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frSinavSonuclari> data { get; set; }

    }
    public class frSinavSonuclari : SinavSonuclari
    {
        public string AdSoyad { get; set; }
        public string SinavTipAdi { get; set; }
        public string SinavDilAdi { get; set; }
        public string IslemYapan { get; set; }

    }
}