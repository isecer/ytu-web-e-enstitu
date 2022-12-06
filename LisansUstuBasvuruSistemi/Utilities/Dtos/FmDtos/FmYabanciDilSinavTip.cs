using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos
{
    public class FmYabanciDilSinavTip : PagerOption
    {
        public int? YabanciDilSinavTipKod { get; set; }
        public string Ad { get; set; }
        public string AdEng { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<FrYabanciDilSinavTip> data { get; set; }

    }
    public class FrYabanciDilSinavTip : SinavDilleri
    {
        public string IslemYapan { get; set; }

    }
}