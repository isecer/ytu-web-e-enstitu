using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmOgrenciBolumleri : PagerModel
    {
        public string EnstituKod { get; set; }
        public int? OgrenciBolumID { get; set; }
        public string BolumAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<FrOgrenciBolumleri> data { get; set; }

    }
    public class FrOgrenciBolumleri : OgrenciBolumleri
    {
        public string EnstituAd { get; set; }

        public string IslemYapan { get; set; }
    }
}