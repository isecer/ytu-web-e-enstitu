using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmMulakatSinavTurleri : PagerOption
    {
        public string MulakatSinavTurAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frMulakatSinavTurleri> data { get; set; }

    }
    public class frMulakatSinavTurleri : MulakatSinavTurleri
    {
        public string IslemYapan { get; set; }

    }
}