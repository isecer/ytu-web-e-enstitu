using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos
{
    public class fmOzelTanimlar : PagerOption
    {
        public string EnstituKod { get; set; }
        public int? SROzelTanimTipID { get; set; }
        public string Aciklama { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frOzelTanimlar> data { get; set; }

    }
    public class frOzelTanimlar : SROzelTanimlar
    {
        public string EnstituAdi { get; set; }
        public string SROzelTanimTipAdi { get; set; }
        public string TalepTipAdi { get; set; }
        public string SalonAdi { get; set; }
        public string AyAdi { get; set; }
        public string IslemYapan { get; set; }
    }
}