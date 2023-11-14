using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmSalonlar : PagerModel
    {
        public string EnstituKod { get; set; }
        public string SalonAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<FrSalonlar> FrSalonlars { get; set; }

    }
    public class FrSalonlar : SRSalonlar
    {
        public string EnstituAdi { get; set; }
        public string IslemYapan { get; set; }
        public List<SRSaatlerMDL> Saatler { get; set; }
        public List<SRSalonTalepTipleri> SrSalonTalepTipleris { get; set; }

    }
}