using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos
{
    public class fmSinavTipleri : PagerOption
    {
        public string EnstituKod { get; set; }
        public string SinavAdi { get; set; }
        public int? SinavTipGrupID { get; set; }
        public bool? WebService { get; set; }
        public bool? OzelTarih { get; set; }
        public bool? OzelNot { get; set; }
        public bool? KusuratVar { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frSinavTipleri> data { get; set; }

    }
    public class frSinavTipleri : SinavTipleri
    {
        public int SelectedTabIndex { get; set; }
        public string EnstituAd { get; set; }
        public string SinavTipGrupAdi { get; set; }
        public string WsSinavCekimTipAdi { get; set; }
        public string IslemYapan { get; set; }

        public List<krSinavTipleriOTNotAraliklari> SinavTipleriOTNotAraliklariList { get; set; }
        public List<krSinavTipleriDonems> krSinavTipleriDonems { get; set; }

        public List<frSinavTipleriSPA> frSinavTipleriSPA { get; set; }

        public frSinavTipleri()
        {
            frSinavTipleriSPA = new List<frSinavTipleriSPA>();
        }
    }
    public class frSinavTipleriSPA : SinavTipleriOT_SNA
    {
        public List<krSinavTipleriOTNotAraliklari> SinavTipleriOTNotAraliklariList { get; set; }
        public List<krSinavTipleriDonems> krSinavTipleriDonems { get; set; }

    }
    public class frBsSinavTipleriSPA : BasvuruSurecSinavTipleriOT_SNA
    {
        public List<krSinavTipleriOTNotAraliklari> SinavTipleriOTNotAraliklariList { get; set; }
    }
}