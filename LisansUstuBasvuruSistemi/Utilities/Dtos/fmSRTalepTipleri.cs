using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmSRTalepTipleri : PagerOption
    {
        public string TalepTipAdi { get; set; }
        public bool? IsTezSinavi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frSRTalepTipleri> data { get; set; }

    }
    public class frSRTalepTipleri : SRTalepTipleri
    {

        public bool IsTezSinavi { get; set; }
        public int? MaxCevaplanmamisTalep { get; set; }
        public int? IstenenJuriSayisiDR { get; set; }
        public int? IstenenJuriSayisiYL { get; set; }
        public bool IsAktif { get; set; }
        public DateTime IslemTarihi { get; set; }
        public int IslemYapanID { get; set; }
        public string IslemYapanIP { get; set; }
        public string IslemYapan { get; set; }
        public List<int> TalepTipAktifAyIds { get; set; }
        public List<int> KullaniciTipIDs { get; set; }

    }
}