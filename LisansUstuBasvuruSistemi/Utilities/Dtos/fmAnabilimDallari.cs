using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;  

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmAnabilimDallari : PagerOption
    {
        public string EnstituKod { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frAnabilimDallari> data { get; set; }

    }
    public class frAnabilimDallari : AnabilimDallari
    {
        public string EnstituAd { get; set; }
        public string IslemYapan { get; set; }

    }
}