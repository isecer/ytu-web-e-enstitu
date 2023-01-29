using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmBelgeTipDetay : PagerOption
    {
        public string BelgeTipAdi { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frBelgeTipDetay> data { get; set; }

    }
    public class frBelgeTipDetay : BelgeTipDetay
    {
        public List<string> BelgeTipAdi { get; set; }
        public List<BTSaatShowModel> Saatler { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public string IslemYapan { get; set; }

    }
}