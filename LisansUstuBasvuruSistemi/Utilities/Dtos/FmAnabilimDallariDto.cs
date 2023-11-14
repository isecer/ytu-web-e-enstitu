using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmAnabilimDallariDto : PagerModel
    {
        public string EnstituKod { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public bool? IsKomiteUyesiVar { get; set; }
        public bool? IsEmailVar { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<FrAnabilimDallariDto> FrAnabilimDallaris { get; set; }

    }
    public class FrAnabilimDallariDto : AnabilimDallari
    {
        public  bool IsEmailVar { get; set; }
        public List<int> KomiteIds { get; set; }
        public int YeterlikKomiteUyeCount { get; set; } 
        public string EnstituAd { get; set; }
        public string IslemYapan { get; set; }

    }
}