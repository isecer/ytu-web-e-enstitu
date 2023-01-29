using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class krSinavTipleriDonems : SinavTipleriDonem
    {
        public string DonemAdi { get; set; }
    }
    public class krSinavTipleri : BasvuruSurecSinavTipleri
    {
        public string EnstituAd { get; set; }
        public string SinavTipGrupAdi { get; set; }
        public string SinavAdi { get; set; }
        public string IslemYapan { get; set; }
        public List<krSinavTipleriDonems> SinavTipleriDonems { get; set; }
        public List<krSinavTipleriOTNotAraliklari> SinavTipleriOTNotAraliklariList { get; set; }
        public List<frBsSinavTipleriSPA> frSinavTipleriSPA { get; set; }
        public krSinavTipleri()
        {
            frSinavTipleriSPA = new List<frBsSinavTipleriSPA>();
        }
    }
}