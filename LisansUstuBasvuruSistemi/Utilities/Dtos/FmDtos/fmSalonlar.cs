using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos
{
    public class fmSalonlar : PagerOption
    {
        public string EnstituKod { get; set; }
        public string SalonAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frSalonlar> data { get; set; }

    }
    public class frSalonlar : SRSalonlar
    {
        public string EnstituAdi { get; set; }
        public string IslemYapan { get; set; }
        public List<SRSaatlerMDL> Saatler { get; set; }
        public List<SRSalonTalepTipleri> SRSalonTalepTipleri { get; set; }

    }
}