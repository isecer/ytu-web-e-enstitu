using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmMezuniyetYayinTurleri : PagerOption
    {
        public bool? IsAktif { get; set; }
        public bool Expand { get; set; }
        public string MezuniyetYayinTurAdi { get; set; }
        public IEnumerable<FrMezuniyetYayinTurleri> Data { get; set; }

    }
    public class FrMezuniyetYayinTurleri : MezuniyetYayinTurleri
    {
        public string EnstituKod { get; set; }
        public string EnstituAd { get; set; }
        public string BelgeTurAdi { get; set; }
        public string KaynakLinkTurAdi { get; set; }
        public string MetinTurAdi { get; set; }
        public string YayinLinkTurAdi { get; set; }
        public string IslemYapan { get; set; }




    }
}