using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;  

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmSrTalepTipleri : PagerModel
    {
        public string TalepTipAdi { get; set; }
        public bool? IsTezSinavi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<FrSrTalepTipleri> Data { get; set; }

    }
    public class FrSrTalepTipleri : SRTalepTipleri
    {
        
        public string IslemYapan { get; set; } 

    }
}