using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmYabanciDilSinavTip : PagerModel
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