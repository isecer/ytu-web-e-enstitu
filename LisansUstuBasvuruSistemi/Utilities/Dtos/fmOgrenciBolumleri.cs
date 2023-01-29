using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmOgrenciBolumleri : PagerOption
    {
        public string EnstituKod { get; set; }
        public int? OgrenciBolumID { get; set; }
        public string BolumAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frOgrenciBolumleri> data { get; set; }

    }
    public class frOgrenciBolumleri : OgrenciBolumleri
    {
        public string EnstituAd { get; set; }

        public string IslemYapan { get; set; }
    }
}