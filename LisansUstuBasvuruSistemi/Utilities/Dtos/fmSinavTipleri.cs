using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmSinavTipleri : PagerModel
    {
        public string EnstituKod { get; set; }
        public string SinavAdi { get; set; }
        public int? SinavTipGrupID { get; set; }  
        public bool? OzelNot { get; set; } 
        public bool? IsAktif { get; set; }
        public IEnumerable<FrSinavTipleri> FrSinavTipleris { get; set; }

    }
    public class FrSinavTipleri : SinavTipleri
    {
        public int SelectedTabIndex { get; set; }
        public string EnstituAd { get; set; }
        public string SinavTipGrupAdi { get; set; }
        public string WsSinavCekimTipAdi { get; set; }
        public string IslemYapan { get; set; }

        public List<KrSinavTipleriOtNotAraliklari> SinavTipleriOtNotAraliklariList { get; set; } 

        public List<FrSinavTipleriSpa> FrSinavTipleriSpa { get; set; }

        public FrSinavTipleri()
        {
            FrSinavTipleriSpa = new List<FrSinavTipleriSpa>();
        }
    }
    public class FrSinavTipleriSpa : SinavTipleriOT_SNA
    {
        public List<KrSinavTipleriOtNotAraliklari> SinavTipleriOtNotAraliklariList { get; set; }
        public List<KrSinavTipleriDonem> KrSinavTipleriDonems { get; set; }

    }
    public class FrBsSinavTipleriSpa : BasvuruSurecSinavTipleriOT_SNA
    {
        public List<KrSinavTipleriOtNotAraliklari> SinavTipleriOtNotAraliklariList { get; set; }
    }

    public class KrSinavTipleriDonem
    {
        public int SinavTipID { get; set; }
        public int SinavTipDonemID { get; set; }
        public int? SinavDilID { get; set; }
        public int Yil   { get; set; }
        public string WsDonemKod { get; set; }
        public string WsDonemAd { get; set; }
        public bool IsTaahhutVar { get; set; }
        public string DonemAdi { get; set; }
    }
    public class KrSinavTipleri : BasvuruSurecSinavTipleri
    {
        public string EnstituAd { get; set; }
        public string SinavTipGrupAdi { get; set; }
        public string SinavAdi { get; set; }
        public string IslemYapan { get; set; }
        public List<KrSinavTipleriDonem> SinavTipleriDonems { get; set; }
        public List<KrSinavTipleriOtNotAraliklari> SinavTipleriOtNotAraliklariList { get; set; }
        public List<FrBsSinavTipleriSpa> FrBsSinavTipleriSpas { get; set; }
        public KrSinavTipleri()
        {
            FrBsSinavTipleriSpas = new List<FrBsSinavTipleriSpa>();
        }
    }
}