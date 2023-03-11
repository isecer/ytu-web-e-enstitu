using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;  

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmAnabilimDallariDto : PagerOption
    {
        public string EnstituKod { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public bool? IsKomiteUyesiVar { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<FrAnabilimDallariDto> FrAnabilimDallaris { get; set; }

    }
    public class FrAnabilimDallariDto : AnabilimDallari
    {
        public int YeterlikKomiteUyeCount { get; set; }
        public string EnstituAd { get; set; }
        public string IslemYapan { get; set; }

    }
}